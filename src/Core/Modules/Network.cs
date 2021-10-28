using SS.Core.ComponentInterfaces;
using SS.Core.Packets;
using SS.Utilities;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SS.Core.Modules
{
    /// <summary>
    /// TODO: revisit this module, it is basically 95% a direct port from asss
    /// </summary>
    [CoreModuleInfo]
    public sealed class Network : IModule, IModuleLoaderAware, INetwork, INetworkEncryption, INetworkClient, IDisposable
    {
        /// <summary>
        /// specialized data buffer which keeps track of what connection it is for and other useful info
        /// </summary>
        private class SubspaceBuffer : DataBuffer
        {
            internal ConnData Conn;

            public byte Tries;
            public NetSendFlags Flags;

            public DateTime LastRetry;

            public IReliableCallbackInvoker CallbackInvoker;

            public override void Clear()
            {
                Conn = null;
                Tries = 0;
                Flags = NetSendFlags.None;
                LastRetry = DateTime.MinValue;

                if (CallbackInvoker != null)
                {
                    CallbackInvoker.Dispose();
                    CallbackInvoker = null;
                }

                base.Clear();
            }
        }
        
        private interface IReliableCallbackInvoker : IDisposable
        {
            void Invoke(Player p, bool success);
        }

        private class ReliableCallbackInvoker : PooledObject, IReliableCallbackInvoker
        {
            private ReliableDelegate _callback;

            public void SetCallback(ReliableDelegate callback)
            {
                _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            }

            #region IReliableDelegateInvoker Members

            public void Invoke(Player p, bool success)
            {
                _callback?.Invoke(p, success);
            }

            #endregion

            protected override void Dispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    _callback = null;
                }

                base.Dispose(isDisposing);
            }
        }

        private class ReliableCallbackInvoker<T> : PooledObject, IReliableCallbackInvoker
        {
            private ReliableDelegate<T> _callback;
            private T _clos;

            public void SetCallback(ReliableDelegate<T> callback, T clos)
            {
                _callback = callback ?? throw new ArgumentNullException(nameof(callback));
                _clos = clos;
            }

            #region IReliableDelegateInvoker Members

            public void Invoke(Player p, bool success)
            {
                _callback?.Invoke(p, success, _clos);
            }

            #endregion

            protected override void Dispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    _callback = null;
                    _clos = default;
                }

                base.Dispose(isDisposing);
            }
        }
        
        private IPool<SubspaceBuffer> _bufferPool = Pool<SubspaceBuffer>.Default;

        private interface ISizedSendData
        {
            /// <summary>
            /// Requests the sender to provide data.
            /// </summary>
            /// <param name="offset">The position of the data to retrieve.</param>
            /// <param name="dataSpan">The buffer to fill.</param>
            void RequestData(int offset, Span<byte> dataSpan);

            /// <summary>
            /// Total # of bytes of the data to send.
            /// </summary>
            int TotalLength
            {
                get;
            }

            /// <summary>
            /// The current position within the data.
            /// </summary>
            int Offset
            {
                get;
                set;
            }
        }

        private class SizedSendData<T> : ISizedSendData
        {
            private readonly GetSizedSendDataDelegate<T> _requestDataCallback;
            private readonly T _clos;
            private readonly int _totalLength;
            private int _offset;

            public SizedSendData(GetSizedSendDataDelegate<T> requestDataCallback, T clos, int totalLength, int offset)
            {
                _requestDataCallback = requestDataCallback ?? throw new ArgumentNullException(nameof(requestDataCallback));
                _clos = clos;
                _totalLength = totalLength;
                _offset = offset;
            }

            void ISizedSendData.RequestData(int offset, Span<byte> dataSpan)
            {
                _requestDataCallback(_clos, offset, dataSpan);
            }

            int ISizedSendData.TotalLength
            {
                get { return _totalLength; }
            }

            int ISizedSendData.Offset
            {
                get { return _offset; }
                set { _offset = value; }
            }
        }

        /// <summary>
        /// Helper for receiving of 'Big' data streams of the 'Core' protocol, which is the subspace protocol's transport layer.
        /// "Big" data streams consist of zero or more 0x00 0x08 packets followed by a single 0x00 0x09 packet indicating the end.
        /// These packets are sent reliably and therefore are processed in order, effectively being a stream.
        /// </summary>
        /// <remarks>
        /// This class is similar to a <see cref="System.IO.MemoryStream"/>. Though not a stream, and it rents arrays using <see cref="ArrayPool{byte}"/>.
        /// </remarks>
        private class BigReceive : PooledObject
        {
            public byte[] Buffer { get; private set; } = null;
            public int Size { get; private set; } = 0;

            public void Append(ReadOnlySpan<byte> data)
            {
                if (Buffer == null)
                {
                    Buffer = ArrayPool<byte>.Shared.Rent(data.Length);
                }
                else if (Buffer.Length < data.Length)
                {
                    byte[] newBuffer = ArrayPool<byte>.Shared.Rent(Size + data.Length);
                    Array.Copy(Buffer, newBuffer, Size);
                    ArrayPool<byte>.Shared.Return(Buffer, true);
                    Buffer = newBuffer;
                }

                data.CopyTo(Buffer.AsSpan(Size));
                Size += data.Length;
            }

            public void Clear()
            {
                if (Buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(Buffer, true);
                    Buffer = null;
                }

                Size = 0;
            }

            protected override void Dispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    Clear();
                }

                base.Dispose(isDisposing);
            }
        }

        private sealed class ConnData : IDisposable
        {
            /// <summary>
            /// The player this connection is for, or <see langword="null"/> for a client connection.
            /// </summary>
            public Player p;

            /// <summary>
            /// The client this is a part of, or <see langword="null"/> for a player connection.
            /// </summary>
            public NetClientConnection cc;

            /// <summary>
            /// The remote address to communicate with.
            /// </summary>
            public IPEndPoint RemoteEndpoint;

            /// <summary>
            /// which of our sockets to use when sending
            /// </summary>
            public Socket whichSock;

            /// <summary>
            /// sequence number for reliable packets
            /// </summary>
            public int s2cn;

            /// <summary>
            /// sequence number for reliable packets
            /// </summary>
            public int c2sn;

            /// <summary>
            /// time of last packet recvd and of initial connection
            /// </summary>
            public DateTime lastPkt;

            /// <summary>
            /// # of packets sent
            /// </summary>
            public uint pktSent;

            /// <summary>
            /// # of packets received
            /// </summary>
            public uint pktReceived;

            /// <summary>
            /// # of bytes sent
            /// </summary>
            public ulong bytesSent;

            /// <summary>
            /// # of bytes received
            /// </summary>
            public ulong bytesReceived;

            /// <summary>
            /// # of duplicate reliable packets
            /// </summary>
            public uint relDups;

            /// <summary>
            /// # of reliable retries
            /// </summary>
            public uint retries;

            /// <summary>
            /// # of dropped packets
            /// </summary>
            public ulong pktdropped;

            /// <summary>
            /// Whether a reliable packet was retried the maximum # of times.
            /// When set to true, it means the connection should be dropped.
            /// </summary>
            public bool HitMaxRetries;

            /// <summary>
            /// Whether there are too many packets pending in a connection's outgoing queues.
            /// Packets that are considered as being pending include:
            /// <list type="bullet">
            /// <item>packets that have not yet been sent (due to bandwidth limits)</item>
            /// <item>reliable packets that have been sent but have not yet been acknowledged</item>
            /// </list>
            /// When set to true, it means the connection should be dropped.
            /// </summary>
            public bool HitMaxOutlist;

            /// <summary>
            /// average roundtrip time
            /// </summary>
            public int avgrtt;

            /// <summary>
            /// average roundtrip deviation
            /// </summary>
            public int rttdev;

            /// <summary>
            /// encryption type
            /// </summary>
            public IEncrypt enc;

            /// <summary>
            /// The name of the IEncrypt interface for <see cref="enc"/>.
            /// </summary>
            public string iEncryptName;

            internal class SizedReceive
            {
                public int type;
                public int totallen, offset;
            }

            /// <summary>
            /// For receiving sized packets, synchronized with <see cref="bigmtx"/>.
            /// </summary>
            public SizedReceive sizedrecv = new();

            /// <summary>
            /// For receiving big packets, synchronized with <see cref="bigmtx"/>.
            /// </summary>
            public BigReceive BigRecv;

            /// <summary>
            /// For sending sized packets, synchronized with <see cref="olmtx"/>.
            /// </summary>
            public LinkedList<ISizedSendData> sizedsends = new();

            /// <summary>
            /// bandwidth limiting
            /// </summary>
            public IBWLimit bw;

            /// <summary>
            /// Array of outgoing lists.  Indexed by <see cref="BandwidthPriorities"/>.
            /// </summary>
            public LinkedList<SubspaceBuffer>[] outlist;

            /// <summary>
            /// Incoming reliable packets
            /// </summary>
            public SubspaceBuffer[] relbuf = new SubspaceBuffer[Constants.CFG_INCOMING_BUFFER];

            /// <summary>
            /// mutex for <see cref="outlist"/>
            /// </summary>
            public object olmtx = new();

            /// <summary>
            /// mutex for <see cref="relbuf"/>
            /// </summary>
            public object relmtx = new();

            /// <summary>
            /// mutex for (<see cref="BigRecv"/> and <see cref="sizedrecv"/>)
            /// </summary>
            public object bigmtx = new();

            public ConnData()
            {
                outlist = new LinkedList<SubspaceBuffer>[(int)Enum.GetValues<BandwidthPriorities>().Max() + 1];
            }

            public void Initalize(IEncrypt enc, string iEncryptName, IBWLimit bw)
            {
                this.enc = enc;
                this.iEncryptName = iEncryptName;
                this.bw = bw;
                avgrtt = 200; // an initial guess
                rttdev = 100;
                lastPkt = DateTime.UtcNow;
                sizedsends.Clear();

                for (int x = 0; x < outlist.Length; x++)
                {
                    outlist[x] = new LinkedList<SubspaceBuffer>();
                }
            }

            public void Dispose()
            {
                if (BigRecv != null)
                {
                    // make sure any rented arrays are returned to their pool
                    BigRecv.Dispose();
                    BigRecv = null;
                }
            }
        }

        private class NetClientConnection : ClientConnection
        {
            public NetClientConnection(IPEndPoint remoteEndpoint, Socket socket, IClientConnectionHandler handler, IClientEncrypt encryptor, string encryptorName, IBWLimit bwLimit)
                : base(handler, encryptorName)
            {
                ConnData = new();
                ConnData.cc = this;
                ConnData.RemoteEndpoint = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));
                ConnData.whichSock = socket ?? throw new ArgumentNullException(nameof(socket));
                ConnData.Initalize(null, null, bwLimit);

                Encryptor = encryptor;
            }

            public ConnData ConnData { get; private set; }

            public IClientEncrypt Encryptor;

            public override EndPoint ServerEndpoint => ConnData.RemoteEndpoint;
        }

        private ComponentBroker _broker;
        private IBandwidthLimit _bandwithLimit;
        private IConfigManager _configManager;
        private ILagCollect _lagCollect;
        private ILogManager _logManager;
        private IMainloop _mainloop;
        private IMainloopTimer _mainloopTimer;
        private IObjectPoolManager _objectPoolManager;
        private IPlayerData _playerData;
        private IPrng _prng;
        private InterfaceRegistrationToken _iNetworkToken;
        private InterfaceRegistrationToken _iNetworkClientToken;
        private InterfaceRegistrationToken _iNetworkEncryptionToken;

        [Flags]
        private enum PingPopulationMode
        {
            Total = 1,
            Playing = 2,
        }

        private class Config
        {
            /// <summary>
            /// How long to get no data from a client before disconnecting him (in ticks).
            /// </summary>
            [ConfigHelp("Net", "DropTimeout", ConfigScope.Global, typeof(int), DefaultValue = "3000",
                Description = "How long to get no data from a client before disconnecting him (in ticks).")]
            public TimeSpan DropTimeout;

            /// <summary>
            /// How many S2C packets the server will buffer for a client before dropping him.
            /// </summary>
            [ConfigHelp("Net", "MaxOutlistSize", ConfigScope.Global, typeof(int), DefaultValue = "500",
                Description = "How many S2C packets the server will buffer for a client before dropping him.")]
            public int MaxOutlistSize;

            /// <summary>
            /// if we haven't sent a reliable packet after this many tries, drop the connection
            /// </summary>
            public int MaxRetries;

            /// <summary>
            /// For sized sends (sending files), the threshold to start queuing up more packets.
            /// </summary>
            public int PresizedQueueThreshold;

            /// <summary>
            /// # of additional packets that can be queued up when below the <see cref="PresizedQueueThreshold"/>.
            /// Note: This means, the maximum # of packets that will be queued up at a given time is <see cref="PresizedQueueThreshold"/> - 1 + <see cref="PresizedQueuePackets"/>.
            /// </summary>
            public int PresizedQueuePackets;

            /// <summary>
            /// ip/udp overhead, in bytes per physical packet
            /// </summary>
            public int PerPacketOverhead;

            /// <summary>
            /// How often to refresh the ping packet data.
            /// </summary>
            public TimeSpan PingRefreshThreshold;

            /// <summary>
            /// display total or playing in simple ping responses
            /// </summary>
            [ConfigHelp("Net", "SimplePingPopulationMode", ConfigScope.Global, typeof(int), DefaultValue = "1",
                Description = 
                "Display what value in the simple ping reponse (used by continuum)?" +
                "1 = display total player count(default);" +
                "2 = display playing count(in ships);" +
                "3 = alternate between 1 and 2")]
            public PingPopulationMode simplepingpopulationmode;
        }

        private readonly Config _config = new();

        private class PopulationStats
        {
            /// <summary>
            /// Total # of players for the 'virtual' zone.
            /// </summary>
            public uint Total = 0;

            /// <summary>
            /// Total # of players for the 'virtual' zone.
            /// </summary>
            public uint Playing = 0;

            public uint TempTotal = 0;
            public uint TempPlaying = 0;
        }

        private class PingData
        {
            public DateTime? LastRefresh = null;

            public readonly PopulationStats Global = new();

            /// <summary>
            /// Key: connectAs
            /// </summary>
            public readonly Dictionary<string, PopulationStats> ConnectAsPopulationStats = new(StringComparer.OrdinalIgnoreCase);

            public void Clear()
            {
                ConnectAsPopulationStats.Clear();
            }
        }

        private readonly PingData _pingData = new();

        /// <summary>
        /// per player data key to ConnData
        /// </summary>
        private int _connKey;

        private readonly ConcurrentDictionary<EndPoint, Player> _clienthash = new();

        private readonly Dictionary<EndPoint, NetClientConnection> _clientConnections = new();
        private readonly ReaderWriterLockSlim _clientConnectionsLock = new(LockRecursionPolicy.NoRecursion);

        /*
        /// <summary>
        /// A helper to group up packets to be send out together as 1 combined packet.
        /// This implementation uses Span<byte>, so the buffer can be stackallocated.
        /// Unfortunately, since Socket does not provide a Span version of SentTo(), an extra copy to a byte[] is required.
        /// This means, the byte[] implementation should perform better.
        /// Leaving this here in hope that Socket's Span support will be enhanced.
        /// </summary>
        private ref struct GroupedPacketManager
        {
            private readonly Network network;
            private readonly Span<byte> bufferSpan;
            private Span<byte> remainingSpan;
            private int count;
            private int numBytes;

            public GroupedPacketManager(Network network, Span<byte> bufferSpan)
            {
                if (bufferSpan.Length < 4)
                    throw new ArgumentException("Needs a minimum length of 4 bytes.", nameof(bufferSpan));

                this.network = network;
                this.bufferSpan = bufferSpan;

                this.bufferSpan[0] = 0x00;
                this.bufferSpan[1] = 0x0E;
                remainingSpan = this.bufferSpan.Slice(2, Math.Min(bufferSpan.Length, Constants.MaxPacket) - 2);
                count = 0;
                numBytes = 2;
            }

            public void Initialize()
            {
                remainingSpan = this.bufferSpan.Slice(2, Math.Min(bufferSpan.Length, Constants.MaxPacket) - 2);
                count = 0;
                numBytes = 2;
            }

            public void Flush(ConnData conn)
            {
                if (conn == null)
                    throw new ArgumentNullException(nameof(conn));

                if (count == 1)
                {
                    // there's only one in the group, so don't send it in a group. 
                    // +3 to skip past the 00 0E and size of first packet
                    network.SendRaw(conn, bufferSpan.Slice(3, numBytes - 3));
                }
                else if (count > 1)
                {
                    network.SendRaw(conn, bufferSpan.Slice(0, numBytes));
                }

                // record stats about grouped packets
                if (count > 0)
                {
                    network._globalStats.grouped_stats[Math.Min((count - 1), network._globalStats.grouped_stats.Length - 1)]++;
                }

                Initialize();
            }

            public void Send(SubspaceBuffer bufferToSend, ConnData conn)
            {
                if (bufferToSend == null)
                    throw new ArgumentNullException(nameof(bufferToSend));

                if (conn == null)
                    throw new ArgumentNullException(nameof(conn));

                if (bufferToSend.NumBytes <= 0)
                    throw new ArgumentException("At least one byte of data is required.", nameof(bufferToSend));

                Send(new Span<byte>(bufferToSend.Bytes, 0, bufferToSend.NumBytes), conn);
            }

            public void Send(Span<byte> dataToSend, ConnData conn)
            {
                if (dataToSend.Length <= 0)
                    throw new ArgumentException("At least one byte of data is required.", nameof(dataToSend));

                if (conn == null)
                    throw new ArgumentNullException(nameof(conn));

                if (dataToSend.Length <= 255) // 255 is the size limit a grouped packet can store (max 1 byte can represent for the length)
                {
                    int lengthWithHeader = dataToSend.Length + 1; // +1 is for the byte that specifies the length
                    if (remainingSpan.Length < lengthWithHeader)
                        Flush(conn); // not enough room in the grouped packet, send it out first, to start with a fresh grouped packet

                    remainingSpan[0] = (byte)dataToSend.Length;
                    dataToSend.CopyTo(remainingSpan.Slice(1));

                    remainingSpan = remainingSpan.Slice(lengthWithHeader);
                    numBytes += lengthWithHeader;
                    count++;
                }
                else
                {
                    // can't fit into a grouped packet, send immediately
                    network.SendRaw(conn, dataToSend);
                }
            }
        }
        */

        /// <summary>
        /// A helper to group up packets to be send out together as 1 combined packet.
        /// </summary>
        private class GroupedPacketManager
        {
            private readonly Network _network;
            private readonly byte[] _buffer = new byte[Constants.MaxPacket];
            private ArraySegment<byte> _remainingSegment;
            private int _count;

            public GroupedPacketManager(Network network)
            {
                _network = network ?? throw new ArgumentNullException(nameof(network));

                // grouped packet header
                _buffer[0] = 0x00;
                _buffer[1] = 0x0E;

                Initialize();
            }

            public void Initialize()
            {
                _remainingSegment = new ArraySegment<byte>(_buffer, 2, _buffer.Length - 2);
                _count = 0;
            }

            public void Flush(ConnData conn)
            {
                if (conn == null)
                    throw new ArgumentNullException(nameof(conn));

                if (_count == 1)
                {
                    // there's only one in the group, so don't send it in a group. 
                    // +3 to skip past the 00 0E and size of first packet
                    _network.SendRaw(conn, _buffer.AsSpan(3, _remainingSegment.Offset - 3));
                }
                else if (_count > 1)
                {
                    _network.SendRaw(conn, _buffer.AsSpan(0, _remainingSegment.Offset));
                }

                // record stats about grouped packets
                if (_count > 0)
                    Interlocked.Increment(ref _network._globalStats.grouped_stats[Math.Min((_count - 1), _network._globalStats.grouped_stats.Length - 1)]);

                Initialize();
            }

            public void Send(SubspaceBuffer buffer, ConnData conn)
            {
                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer));

                if (conn == null)
                    throw new ArgumentNullException(nameof(conn));

#if DISABLE_GROUPED_SEND
                _network.sendRaw(conn, buffer.Bytes, buffer.NumBytes);
#else
                if (buffer.NumBytes <= 255) // 255 is the size limit a grouped packet can store (max 1 byte can represent for the length)
                {
                    // TODO: Find out why ASSS subtracts 10 (MAXPACKET - 10 - buf->len).  For now, ignoring that and just ensuring the data fits.
                    if (_remainingSegment.Count < (buffer.NumBytes + 1)) // +1 is for the byte that specifies the length
                        Flush(conn); // not enough room in the grouped packet, send it out first to start with a fresh grouped packet

                    _remainingSegment.Array[_remainingSegment.Offset] = (byte)buffer.NumBytes;
                    Array.Copy(buffer.Bytes, 0, _remainingSegment.Array, _remainingSegment.Offset + 1, buffer.NumBytes);

                    _remainingSegment = new ArraySegment<byte>(_remainingSegment.Array, _remainingSegment.Offset + (buffer.NumBytes + 1), _remainingSegment.Count - (buffer.NumBytes + 1));
                    _count++;
                }
                else
                {
                    // can't fit into a grouped packet, send immediately
                    _network.SendRaw(conn, buffer.Bytes.AsSpan(0, buffer.NumBytes));
                }
#endif
            }
        }

        private delegate void oohandler(SubspaceBuffer buffer);

        /// <summary>
        /// Handlers for 'core' packets (ss protocol's network/transport layer).
        /// </summary>
        /// <remarks>
        /// The first byte of these packets is 0x00.
        /// The second byte identifies the type and is the index into this array.
        /// </remarks>
        private readonly oohandler[] _oohandlers;

        private const int MAXTYPES = 64;

        /// <summary>
        /// Handlers for 'game' packets.
        /// </summary>
        private readonly PacketDelegate[] _handlers = new PacketDelegate[MAXTYPES];
        private readonly PacketDelegate[] _nethandlers = new PacketDelegate[0x14];
        private readonly SizedPacketDelegate[] _sizedhandlers = new SizedPacketDelegate[MAXTYPES];

        private const int MICROSECONDS_PER_MILLISECOND = 1000;

        private readonly MessagePassingQueue<ConnData> _relqueue = new();

        private readonly CancellationTokenSource _stopCancellationTokenSource = new();
        private CancellationToken _stopToken;

        private readonly List<Thread> _threadList = new();
        private readonly List<Thread> _reliableThreads = new();

        /// <summary>
        /// info about sockets this object has created, etc...
        /// </summary>
        private readonly List<ListenData> _listenDataList;
        private readonly ReadOnlyCollection<ListenData> _readOnlyListenData;

        private Socket _clientSocket;

        private class NetStats : IReadOnlyNetStats
        {
            public ulong pcountpings, pktsent, pktrecvd;
            public ulong bytesent, byterecvd;
            public ulong buffercount, buffersused;
            public readonly ulong[] grouped_stats = new ulong[8];
            public readonly ulong[] pri_stats = new ulong[(int)Enum.GetValues<BandwidthPriorities>().Max() + 1];

            public ulong PingsReceived => Interlocked.Read(ref pcountpings);

            public ulong PacketsSent => Interlocked.Read(ref pktsent);

            public ulong PacketsReceived => Interlocked.Read(ref pktrecvd);

            public ulong BytesSent => Interlocked.Read(ref bytesent);

            public ulong BytesReceived => Interlocked.Read(ref byterecvd);

            public ulong BuffersTotal => Interlocked.Read(ref buffercount);

            public ulong BuffersUsed => Interlocked.Read(ref buffersused);

            public ulong GroupedStats0 => Interlocked.Read(ref grouped_stats[0]);
            public ulong GroupedStats1 => Interlocked.Read(ref grouped_stats[1]);
            public ulong GroupedStats2 => Interlocked.Read(ref grouped_stats[2]);
            public ulong GroupedStats3 => Interlocked.Read(ref grouped_stats[3]);
            public ulong GroupedStats4 => Interlocked.Read(ref grouped_stats[4]);
            public ulong GroupedStats5 => Interlocked.Read(ref grouped_stats[5]);
            public ulong GroupedStats6 => Interlocked.Read(ref grouped_stats[6]);
            public ulong GroupedStats7 => Interlocked.Read(ref grouped_stats[7]);

            public ulong PriorityStats0 => Interlocked.Read(ref pri_stats[0]);
            public ulong PriorityStats1 => Interlocked.Read(ref pri_stats[1]);
            public ulong PriorityStats2 => Interlocked.Read(ref pri_stats[2]);
            public ulong PriorityStats3 => Interlocked.Read(ref pri_stats[3]);
            public ulong PriorityStats4 => Interlocked.Read(ref pri_stats[4]);
        }

        private readonly NetStats _globalStats = new();

        // delegates to prevent allocating a new delegate object per call
        private readonly Action<BigPacketWork> mainloopWork_CallBigPacketHandlersAction;
        private readonly Action<SubspaceBuffer> mainloopWork_CallPacketHandlersAction;
        private readonly Action<InvokeReliableCallbackDTO> mainloopWork_InvokeReliableCallbackAction;

        public Network()
        {
            // Create callback delegates once rather than each time they're used.
            mainloopWork_CallBigPacketHandlersAction = MainloopWork_CallBigPacketHandlers;
            mainloopWork_CallPacketHandlersAction = MainloopWork_CallPacketHandlers;
            mainloopWork_InvokeReliableCallbackAction = MainloopWork_InvokeReliableCallback;

            _oohandlers = new oohandler[20];

            _oohandlers[0] = null; //00 - nothing
            _oohandlers[1] = null; //01 - key initiation
            _oohandlers[2] = ProcessKeyResponse; //02 - key response
            _oohandlers[3] = ProcessReliable; //03 - reliable
            _oohandlers[4] = ProcessAck; //04 - reliable response
            _oohandlers[5] = ProcessSyncRequest; //05 - time sync request
            _oohandlers[6] = null; //06 - time sync response
            _oohandlers[7] = ProcessDrop; //07 - close connection
            _oohandlers[8] = ProcessBigData; //08 - bigpacket
            _oohandlers[9] = ProcessBigData; //09 - bigpacket2
            _oohandlers[10] = ProcessPresize; //0A - presized data (file transfer)
            _oohandlers[11] = ProcessCancelReq;  //0B - cancel presized
            _oohandlers[12] = ProcessCancel; //0C - presized has been cancelled
            _oohandlers[13] = null; //0D - nothing
            _oohandlers[14] = ProcessGrouped; //0E - grouped
            _oohandlers[15] = null; // 0x0F
            _oohandlers[16] = null; // 0x10
            _oohandlers[17] = null; // 0x11
            _oohandlers[18] = null; // 0x12
            _oohandlers[19] = ProcessSpecial; // 0x13 - cont key response

            _listenDataList = new List<ListenData>();
            _readOnlyListenData = new ReadOnlyCollection<ListenData>(_listenDataList);
        }

        #region Socket initialization

        private Socket CreateSocket(int port, IPAddress bindAddress)
        {
            Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // For Windows (Winsock) only,
                // to prevent the exception "An existing connection was forcibly closed by the remote host"
                // http://support.microsoft.com/kb/263823
                const int SIO_UDP_CONNRESET = -1744830452;  // since IOControl() takes int instead of uint
                byte[] optionInValue = new byte[] { 0, 0, 0, 0 };
                socket.IOControl(SIO_UDP_CONNRESET, optionInValue, null);
            }

            try
            {
                socket.Blocking = false;
            }
            catch (Exception ex)
            {
                // not fatal, just warn
                _logManager.LogM(LogLevel.Warn, nameof(Network), $"Can't make socket nonblocking. {ex.Message}");
            }

            if (bindAddress == null)
                bindAddress = IPAddress.Any;

            try
            {
                socket.Bind(new IPEndPoint(bindAddress, port));
            }
            catch (Exception ex)
            {
                socket.Close();
                throw new Exception($"Cannot bind socket to {bindAddress}:{port}.", ex);
            }

            return socket;
        }

        [ConfigHelp("Listen", "Port", ConfigScope.Global, typeof(int),
            "The port that the game protocol listens on. Sections named " +
            "Listen1, Listen2, ... are also supported. All Listen " +
            "sections must contain a port setting.")]
        [ConfigHelp("Listen", "BindAddress", ConfigScope.Global, typeof(string),
            "The interface address to bind to. This is optional, and if " +
            "omitted, the server will listen on all available interfaces.")]
        private ListenData CreateListenDataSockets(int configIndex)
        {
            string configSection = (configIndex == 0) ? "Listen" : $"Listen{configIndex}";

            int gamePort = _configManager.GetInt(_configManager.Global, configSection, "Port", -1);
            if (gamePort == -1)
                return null;

            string bindAddressStr = _configManager.GetStr(_configManager.Global, configSection, "BindAddress");
            IPAddress bindAddress = IPAddress.Any;
            if (string.IsNullOrEmpty(bindAddressStr) == false)
            {
                try
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(bindAddressStr);
                    if (addresses.Length > 0)
                        bindAddress = addresses[0];
                }
                catch
                {
                    // ignore and just stick with IPAddress.Any
                }
            }

            int pingPort = gamePort + 1;

            Socket gameSocket;
            Socket pingSocket;

            try
            {
                gameSocket = CreateSocket(gamePort, bindAddress);
            }
            catch (Exception ex)
            {
                _logManager.LogM(LogLevel.Error, nameof(Network), $"Unable to create game socket. {ex.Message}");
                return null;
            }

            try
            {
                pingSocket = CreateSocket(pingPort, bindAddress);
            }
            catch (Exception ex)
            {
                gameSocket.Close();
                _logManager.LogM(LogLevel.Error, nameof(Network), $"Unable to create ping socket: {ex.Message}");
                return null;
            }

            ListenData listenData = new(gameSocket, pingSocket)
            {
                ConnectAs = _configManager.GetStr(_configManager.Global, configSection, "ConnectAs"),
                AllowVIE = _configManager.GetInt(_configManager.Global, configSection, "AllowVIE", 1) > 0,
                AllowContinuum = _configManager.GetInt(_configManager.Global, configSection, "AllowCont", 1) > 0,
            };

            return listenData;
        }

        [ConfigHelp("Net", "InternalClientPort", ConfigScope.Global, typeof(int),
            Description = "The bind port for the internal client socket (used to communicate with biller and dirserver).")]
        private bool InitializeSockets()
        {
            //
            // Listen sockets (pairs of game and ping sockets)
            //

            int x = 0;
            ListenData listenData;

            while ((listenData = CreateListenDataSockets(x++)) != null)
            {
                _listenDataList.Add(listenData);

                if (string.IsNullOrWhiteSpace(listenData.ConnectAs) == false)
                {
                    if (!_pingData.ConnectAsPopulationStats.ContainsKey(listenData.ConnectAs))
                    {
                        _pingData.ConnectAsPopulationStats.Add(listenData.ConnectAs, new PopulationStats());
                    }
                }

                _logManager.LogM(LogLevel.Drivel, nameof(Network), $"Listening on {listenData.GameSocket.LocalEndPoint}.");
            }

            //
            // Client socket (for communicating with the biller and directory server)
            //

            int bindPort = _configManager.GetInt(_configManager.Global, "Net", "InternalClientPort", 0);

            try
            {
                _clientSocket = CreateSocket(bindPort, IPAddress.Any);
            }
            catch (Exception ex)
            {
                _logManager.LogM(LogLevel.Error, nameof(Network), $"Unable to create socket for client connections. {ex.Message}");
            }

            return true;
        }

        #endregion

        private void ReceiveThread()
        {
            List<Socket> socketList = new(_listenDataList.Count * 2 + 1);
            List<Socket> checkReadList = new(_listenDataList.Count * 2 + 1);
            
            Dictionary<EndPoint, (char Type, ListenData ListenData)> endpointLookup 
                = new(_listenDataList.Count * 2);

            foreach (ListenData ld in _listenDataList)
            {
                if (ld.GameSocket != null)
                {
                    socketList.Add(ld.GameSocket);
                    endpointLookup.Add(ld.GameSocket.LocalEndPoint, ('G', ld));
                }

                if (ld.PingSocket != null)
                {
                    socketList.Add(ld.PingSocket);
                    endpointLookup.Add(ld.PingSocket.LocalEndPoint, ('P', ld));
                }
            }

            if (_clientSocket != null)
            {
                socketList.Add(_clientSocket);
            }

            while (true)
            {
                try
                {
                    if (_stopToken.IsCancellationRequested)
                        return;

                    checkReadList.Clear();
                    checkReadList.AddRange(socketList);

                    Socket.Select(checkReadList, null, null, MICROSECONDS_PER_MILLISECOND * 1000);

                    if (_stopToken.IsCancellationRequested)
                        return;

                    foreach (Socket socket in checkReadList)
                    {
                        if (endpointLookup.TryGetValue(socket.LocalEndPoint, out var tuple))
                        {
                            if (tuple.Type == 'G')
                                HandleGamePacketReceived(tuple.ListenData);
                            else if (tuple.Type == 'P')
                                HandlePingPacketReceived(tuple.ListenData);
                        }
                        else if (socket == _clientSocket)
                        {
                            HandleClientPacketReceived();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(LogManager), $"Caught an exception in ReceiveThread. {ex}");
                }
            }
        }

        private void SendThread()
        {
            GroupedPacketManager groupedPacketManager = new(this);
            //GroupedPacketManager groupedPacketManager = new GroupedPacketManager(this, new byte[Constants.MaxPacket]);

            List<Player> toKick = new();
            List<Player> toFree = new();

            while (_stopToken.IsCancellationRequested == false)
            {
                // first send outgoing packets (players)
                _playerData.Lock();

                try
                {
                    foreach (Player p in _playerData.PlayerList)
                    {
                        if (p.Status >= PlayerState.Connected
                            && p.Status < PlayerState.TimeWait
                            && IsOurs(p))
                        {
                            if (p[_connKey] is not ConnData conn)
                                continue;

                            if (Monitor.TryEnter(conn.olmtx))
                            {
                                try
                                {
                                    //SendOutgoing(conn, in groupedPacketManager);
                                    SendOutgoing(conn, groupedPacketManager);
                                    SubmitRelStats(p);
                                }
                                finally
                                {
                                    Monitor.Exit(conn.olmtx);
                                }
                            }

                        }
                    }
                }
                finally
                {
                    _playerData.Unlock();
                }

                // process lagouts and timewait
                _playerData.Lock();
                DateTime now = DateTime.UtcNow;
                try
                {
                    foreach (Player p in _playerData.PlayerList)
                    {
                        if (p.Status >= PlayerState.Connected
                            && IsOurs(p))
                        {
                            ProcessLagouts(p, now, toKick, toFree);
                        }
                    }
                }
                finally
                {
                    _playerData.Unlock();
                }

                // now kick the ones we needed to above
                foreach (Player p in toKick)
                {
                    _playerData.KickPlayer(p);
                }
                toKick.Clear();

                // and free ...
                foreach (Player p in toFree)
                {
                    if (p[_connKey] is not ConnData conn)
                        continue;

                    // one more time, just to be sure
                    ClearBuffers(p);

                    // TODO: _bandwithLimit.Free(conn.bw);

                    _playerData.FreePlayer(p);
                }
                toFree.Clear();

                // outgoing packets and lagouts for client connections
                now = DateTime.UtcNow;

                _clientConnectionsLock.EnterUpgradeableReadLock();

                try
                {
                    foreach (NetClientConnection cc in _clientConnections.Values)
                    {
                        ConnData conn = cc.ConnData;
                        lock (conn.olmtx)
                        {
                            SendOutgoing(conn, groupedPacketManager);
                        }

                        if (conn.HitMaxRetries)
                        {
                            _logManager.LogM(LogLevel.Warn, nameof(Network), "Client connection hit max retries.");
                        }

                        if (now - conn.lastPkt > (conn.pktReceived > 0 ? TimeSpan.FromSeconds(65) : TimeSpan.FromSeconds(10)))
                        {
                            _logManager.LogM(LogLevel.Warn, nameof(Network), $"Client connection {now - conn.lastPkt} > {(conn.pktReceived > 0 ? TimeSpan.FromSeconds(65) : TimeSpan.FromSeconds(10))}.");
                        }

                        // Special limit of 65 seconds, unless we haven't gotten any packets, then use 10.
                        if (conn.HitMaxRetries
                            || now - conn.lastPkt > (conn.pktReceived > 0 ? TimeSpan.FromSeconds(65) : TimeSpan.FromSeconds(10)))
                        {
                            cc.Handler.Disconnected();
                            DropConnection(cc);
                        }
                    }
                }
                finally
                {
                    _clientConnectionsLock.ExitUpgradeableReadLock();
                }

                if (_stopToken.IsCancellationRequested)
                    return;

                // TODO: Figure out a way that's better than a plain Sleep.
                // Maybe separate out lagout/kick/free logic to a threadpool timer? leaving only outgoing/stats logic?
                // Then maybe an AutoResetEvent for whenever a new outgoing packet is queued?
                // and wait on the AutoResetEvent/stopToken.WaitHandle? 
                // How to tell how long until a player's bandwidth limits pass a threshold that allows sending more?
                // Need to investigate bandwidth limiting logic before deciding on this.
                Thread.Sleep(10); // 1/100 second
            }
        }

        private void RelThread()
        {
            while (true)
            {
                // get the next connection that might have packets to process
                ConnData conn = _relqueue.Dequeue();
                if (conn == null)
                    return; // null means stop the thread (module is unloading)

                // then process as much as we can

                lock (conn.relmtx)
                {
                    int processedCount = 0;
                    SubspaceBuffer buf;
                    for (int spot = conn.c2sn % Constants.CFG_INCOMING_BUFFER;
                        (buf = conn.relbuf[spot]) != null && processedCount <= Constants.CFG_INCOMING_BUFFER;
                        spot = conn.c2sn % Constants.CFG_INCOMING_BUFFER)
                    {
                        conn.c2sn++;
                        conn.relbuf[spot] = null;
                        processedCount++;

                        // don't need the mutex while doing the actual processing
                        Monitor.Exit(conn.relmtx);

                        // process it
                        buf.NumBytes -= Constants.ReliableHeaderLen;
                        Array.Copy(buf.Bytes, Constants.ReliableHeaderLen, buf.Bytes, 0, buf.NumBytes);
                        ProcessBuffer(buf);

                        Monitor.Enter(conn.relmtx);
                    }

                    if (processedCount > Constants.CFG_INCOMING_BUFFER)
                    {
                        if (conn.relbuf[conn.c2sn % Constants.CFG_INCOMING_BUFFER] != null)
                        {
                            // there is more to process, but we don't want to hog all the processing time on 1 connection
                            // re-queue and we'll get back to it
                            _relqueue.Enqueue(conn);
                        }
                    }
                }
            }
        }

        private byte[] _queueDataBuffer = null;
        private readonly byte[] _queueDataHeader = new byte[6] { 0x00, 0x0A, 0, 0, 0, 0 }; // size of a presized data packet header

        /// <summary>
        /// Attempts to queue up sized send data for each UDP player.
        /// <para>
        /// For each player, it will check if there is sized send work that can be processed (work exists and is not past sized queue limits).
        /// If so, it will retrieve data from the sized send callback (can be partial data), 
        /// break the data into sized send packets (0x0A), 
        /// and add those packets to player's outgoing reliable queue.
        /// </para>
        /// <para>Sized packets are sent reliably so that the other end can reconstruct the data properly.</para>
        /// </summary>
        /// <returns>True, meaning it wants the timer to call it again.</returns>
        private bool MainloopTimer_QueueSizedData()
        {
            int requestAtOnce = _config.PresizedQueuePackets * Constants.ChunkSize;

            byte[] buffer = _queueDataBuffer;
            if (buffer == null || buffer.Length < requestAtOnce + _queueDataHeader.Length)
                buffer = _queueDataBuffer = new byte[requestAtOnce + _queueDataHeader.Length];

            Span<byte> sizedLengthSpan = new(_queueDataHeader, 2, 4);
            _playerData.Lock();

            try
            {
                foreach (Player p in _playerData.PlayerList)
                {
                    if(!IsOurs(p) || p.Status >= PlayerState.TimeWait)
                        continue;

                    if (p[_connKey] is not ConnData conn)
                        continue;

                    if (Monitor.TryEnter(conn.olmtx) == false)
                        continue;

                    if (conn.sizedsends.First != null
                        && conn.outlist[(int)BandwidthPriorities.Reliable].Count < _config.PresizedQueueThreshold)
                    {
                        ISizedSendData sd = conn.sizedsends.First.Value;

                        // unlock while we get the data
                        Monitor.Exit(conn.olmtx);

                        // prepare the header (already has type bytes set, only need to set the length field)
                        BinaryPrimitives.WriteInt32LittleEndian(sizedLengthSpan, sd.TotalLength);

                        // get needed bytes
                        int needed = requestAtOnce;
                        if (sd.TotalLength - sd.Offset < needed)
                            needed = sd.TotalLength - sd.Offset;

                        sd.RequestData(sd.Offset, new Span<byte>(buffer, 6, needed)); // skipping the first 6 bytes for the header
                        sd.Offset += needed;

                        // now lock while we buffer it
                        Monitor.Enter(conn.olmtx);

                        // break the data into sized send (0x0A) packets and queue them up
                        int bufferIndex = 0;
                        while (needed > Constants.ChunkSize)
                        {
                            Array.Copy(_queueDataHeader, 0, buffer, bufferIndex, 6); // write the header in front of the data
                            BufferPacket(conn, new ReadOnlySpan<byte>(buffer, bufferIndex, Constants.ChunkSize + 6), NetSendFlags.PriorityN1 | NetSendFlags.Reliable);
                            bufferIndex += Constants.ChunkSize;
                            needed -= Constants.ChunkSize;
                        }

                        Array.Copy(_queueDataHeader, 0, buffer, bufferIndex, 6); // write the header in front of the data
                        BufferPacket(conn, new ReadOnlySpan<byte>(buffer, bufferIndex, needed + 6), NetSendFlags.PriorityN1 | NetSendFlags.Reliable);

                        // check if we need more
                        if (sd.Offset >= sd.TotalLength)
                        {
                            // notify sender that this is the end
                            sd.RequestData(sd.Offset, Span<byte>.Empty);
                            conn.sizedsends.RemoveFirst();
                        }
                    }

                    Monitor.Exit(conn.olmtx);
                }
            }
            finally
            {
                _playerData.Unlock();
            }

            return true;
        }

        private void ClearBuffers(Player p)
        {
            if (p == null)
                return;

            if (p[_connKey] is not ConnData conn)
                return;

            lock (conn.olmtx)
            {
                // handle the regular outgoing queues
                for (int i = 0; i < conn.outlist.Length; i++)
                {
                    LinkedList<SubspaceBuffer> outlist = conn.outlist[i];

                    LinkedListNode<SubspaceBuffer> nextNode;
                    for (LinkedListNode<SubspaceBuffer> node = outlist.First; node != null; node = nextNode)
                    {
                        nextNode = node.Next;

                        SubspaceBuffer b = node.Value;
                        if (b.CallbackInvoker != null)
                        {
                            QueueMainloopWorkItem(b.CallbackInvoker, p, false);
                            b.CallbackInvoker = null; // the workitem is now responsible for disposing the callback invoker
                        }

                        outlist.Remove(node);
                        b.Dispose();
                    }
                }

                // and presized outgoing
                foreach (ISizedSendData sd in conn.sizedsends)
                {
                    sd.RequestData(0, Span<byte>.Empty);
                }

                conn.sizedsends.Clear();
            }

            // now clear out the connection's incoming rel buffer
            lock (conn.relmtx)
            {
                for (int i = 0; i < Constants.CFG_INCOMING_BUFFER; i++)
                {
                    if (conn.relbuf[i] != null)
                    {
                        conn.relbuf[i].Dispose();
                        conn.relbuf[i] = null;
                    }
                }
            }

            // and remove from relible signaling queue
            _relqueue.ClearOne(conn);
        }

        // call with player status locked
        private void ProcessLagouts(Player p, DateTime now, List<Player> toKick, List<Player> toFree)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));

            if (toKick == null)
                throw new ArgumentNullException(nameof(toKick));

            if (toFree == null)
                throw new ArgumentNullException(nameof(toFree));

            if (p[_connKey] is not ConnData conn)
                return;

            // this is used for lagouts and also for timewait
            TimeSpan diff = now - conn.lastPkt;

            // process lagouts
            if (p.WhenLoggedIn == PlayerState.Uninitialized // acts as flag to prevent dups
                && p.Status < PlayerState.LeavingZone // don't kick them if they're already on the way out
                && (diff > _config.DropTimeout || conn.HitMaxRetries || conn.HitMaxOutlist)) // these three are our kicking conditions, for now
            {
                // manually create an unreliable chat packet because we won't have time to send it properly
                string reason;
                if (conn.HitMaxRetries)
                    reason = "too many reliable retries";
                else if (conn.HitMaxOutlist)
                    reason = "too many outgoing packets";
                else
                    reason = "no data";

                string message = "You have been disconnected because of lag (" + reason + ").\0"; // it looks like asss sends a null terminated string

                using SubspaceBuffer buf = _bufferPool.Get();
                buf.Bytes[0] = (byte)S2CPacketType.Chat;
                buf.Bytes[1] = (byte)ChatMessageType.SysopWarning;
                buf.Bytes[2] = 0x00;
                buf.Bytes[3] = 0x00;
                buf.Bytes[4] = 0x00;
                buf.NumBytes = 5 + Encoding.ASCII.GetBytes(message, 0, message.Length, buf.Bytes, 5);

                lock (conn.olmtx)
                {
                    SendRaw(conn, buf.Bytes.AsSpan(0, buf.NumBytes));
                }

                _logManager.LogM(LogLevel.Info, nameof(Network), $"[{p.Name}] [pid={p.Id}] Player kicked for {reason}.");

                toKick.Add(p);
            }

            // process timewait state
            // status is locked (shared) in here
            if (p.Status == PlayerState.TimeWait)
            {
                // finally, send disconnection packet
                Span<byte> disconnectSpan = stackalloc byte[] { 0x00, 0x07 };
                SendRaw(conn, disconnectSpan);

                // clear all our buffers
                lock (conn.bigmtx)
                {
                    EndSized(p, false);
                }

                ClearBuffers(p);

                // tell encryption to forget about him
                if (conn.enc != null)
                {
                    conn.enc.Void(p);
                    _broker.ReleaseInterface(ref conn.enc, conn.iEncryptName);
                }

                // log message
                _logManager.LogM(LogLevel.Info, nameof(Network), $"[{p.Name}] [pid={p.Id}] Disconnected.");

                if (_clienthash.TryRemove(conn.RemoteEndpoint, out _) == false)
                    _logManager.LogM(LogLevel.Error, nameof(Network), "Established connection not in hash table.");

                toFree.Add(p);
            }
        }

        private void SubmitRelStats(Player p)
        {
            if (p == null)
                return;

            if (_lagCollect == null)
                return;

            if (p[_connKey] is not ConnData conn)
                return;

            ReliableLagData rld = new()
            {
                RelDups = conn.relDups,
                C2SN = (uint)conn.c2sn,
                Retries = conn.retries,
                S2CN = (uint)conn.s2cn,
            };

            _lagCollect.RelStats(p, in rld);
        }

        private void SendRaw(ConnData conn, ReadOnlySpan<byte> data)
        {
            if (conn == null)
                return;

            int len = data.Length;

            Player p = conn.p;

#if CFG_DUMP_RAW_PACKETS
            if(p != null)
                DumpPk($"SEND: {len} bytes to {p.Id}", data);
            else if(conn.cc != null)
                DumpPk($"SEND: {len} bytes to client connection {conn.cc.ServerEndpoint}", data);
#endif

            Span<byte> encryptedBuffer = stackalloc byte[Constants.MaxPacket + 4];
            data.CopyTo(encryptedBuffer);

            if ((p != null) && (conn.enc != null))
            {
                len = conn.enc.Encrypt(p, encryptedBuffer, len);
            }
            else if ((conn.cc != null) && (conn.cc.Encryptor != null))
            {
                len = conn.cc.Encryptor.Encrypt(conn.cc, encryptedBuffer, len);
            }

            if (len == 0)
                return;

            encryptedBuffer = encryptedBuffer.Slice(0, len);

#if CFG_DUMP_RAW_PACKETS
            if(p != null)
                DumpPk($"SEND: {len} bytes to pid {p.Id} (after encryption)", encryptedBuffer);
            else if(conn.cc != null)
                DumpPk($"SEND: {len} bytes to client connection {conn.cc.ServerEndpoint} (after encryption)", encryptedBuffer);
#endif

            // FUTURE: Change this when/if Microsoft adds a Socket.SendTo(ReadOnlySpan<byte>,...) overload. For now, need to copy to a byte[].
            using (SubspaceBuffer buffer = _bufferPool.Get())
            {
                encryptedBuffer.CopyTo(new Span<byte>(buffer.Bytes, 0, len));

                try
                {
                    conn.whichSock.SendTo(buffer.Bytes, 0, len, SocketFlags.None, conn.RemoteEndpoint);
                }
                catch (SocketException ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"SocketException with error code {ex.ErrorCode} when sending to {conn.RemoteEndpoint} with game socket {conn.whichSock.LocalEndPoint}. {ex}");
                    return;
                }
                catch (Exception ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"Exception when sending to {conn.RemoteEndpoint} with game socket {conn.whichSock.LocalEndPoint}. {ex}");
                    return;
                }
            }

            conn.bytesSent += (ulong)len;
            conn.pktSent++;
            Interlocked.Add(ref _globalStats.bytesent, (ulong)len);
            Interlocked.Increment(ref _globalStats.pktsent);
        }

        //private void SendOutgoing(ConnData conn, in GroupedPacketManager groupedPacketManager)
        private void SendOutgoing(ConnData conn, GroupedPacketManager groupedPacketManager)
        {
            DateTime now = DateTime.UtcNow;

            // use an estimate of the average round-trip time to figure out when to resend a packet
            uint timeout = (uint)(conn.avgrtt + (4 * conn.rttdev));
            Clip(ref timeout, 250, 2000);

            // update the bandwidth limiter's counters
            conn.bw.Iter(now);

            // find smallest seqnum remaining in outlist
            int minSeqNum = int.MaxValue;
            LinkedList<SubspaceBuffer> outlist = conn.outlist[(int)BandwidthPriorities.Reliable];
            foreach (SubspaceBuffer buffer in outlist)
            {
                ref ReliableHeader rp = ref MemoryMarshal.AsRef<ReliableHeader>(buffer.Bytes);

                if (rp.SeqNum < minSeqNum)
                    minSeqNum = rp.SeqNum;
            }

            int canSend = conn.bw.GetCanBufferPackets();
            int maxSeqNumToSend = minSeqNum + (canSend > 0 ? canSend : 0);
            int retries = 0;
            int outlistlen = 0;

            groupedPacketManager.Initialize();

            // process the highest priority first
            for (int pri = conn.outlist.Length - 1; pri >= 0; pri--)
            {
                outlist = conn.outlist[pri];

                LinkedListNode<SubspaceBuffer> nextNode;
                for (LinkedListNode<SubspaceBuffer> node = outlist.First; node != null; node = nextNode)
                {
                    nextNode = node.Next;

                    SubspaceBuffer buf = node.Value;
                    outlistlen++;

                    // check some invariants
                    ref ReliableHeader rp = ref MemoryMarshal.AsRef<ReliableHeader>(buf.Bytes);

                    if (rp.T1 == 0x00 && rp.T2 == 0x03)
                        Debug.Assert(pri == (int)BandwidthPriorities.Reliable);
                    else if (rp.T1 == 0x00 && rp.T2 == 0x04)
                        Debug.Assert(pri == (int)BandwidthPriorities.Ack);
                    else
                        Debug.Assert((pri != (int)BandwidthPriorities.Reliable) && (pri != (int)BandwidthPriorities.Ack));

                    // check if it's time to send this yet (use linearly increasing timeouts)
                    if ((buf.Tries != 0) && ((now - buf.LastRetry).TotalMilliseconds <= (timeout * buf.Tries)))
                        continue;

                    // only send a maximum range of rel packets
                    if ((pri == (int)BandwidthPriorities.Reliable) && rp.SeqNum > maxSeqNumToSend)
                        continue;

                    // if we've retried too many times, kick the player
                    if (buf.Tries > _config.MaxRetries)
                    {
                        conn.HitMaxRetries = true;
                        return;
                    }

                    // at this point, there's only one more check to determine if we're sending this packet now: bandwidth limiting.
                    if (!conn.bw.Check(
                        buf.NumBytes + ((buf.NumBytes <= 255) ? 1 : _config.PerPacketOverhead),
                        pri))
                    {
                        // try dropping it, if we can
                        if ((buf.Flags & NetSendFlags.Droppable) != 0)
                        {
                            Debug.Assert(pri < (int)BandwidthPriorities.Reliable);
                            outlist.Remove(node);
                            buf.Dispose();
                            conn.pktdropped++;
                            outlistlen--;
                        }

                        // but in either case, skip it
                        continue;
                    }

                    if (buf.Tries > 0)
                    {
                        // this is a retry, not an initial send. record it for
                        // lag stats and also reduce bw limit (with clipping)
                        retries++;
                        conn.bw.AdjustForRetry();
                    }

                    buf.LastRetry = DateTime.UtcNow;
                    buf.Tries++;

                    // this sends it or adds it to a pending grouped packet
                    groupedPacketManager.Send(buf, conn);

                    // if we just sent an unreliable packet, free it so we don't send it again
                    if (pri != (int)BandwidthPriorities.Reliable)
                    {
                        outlist.Remove(node);
                        buf.Dispose();
                        outlistlen--;
                    }
                }
            }

            // flush the pending grouped packet
            groupedPacketManager.Flush(conn);

            conn.retries += (uint)retries;

            if (outlistlen > _config.MaxOutlistSize)
                conn.HitMaxOutlist = true;
        }

        private static void Clip(ref uint timeout, uint low, uint high)
        {
            if(timeout > high)
                timeout = high;
            else if(timeout < low)
                timeout = low;
        }

        private static bool IsOurs(Player p)
        {
            return p.Type == ClientType.Continuum || p.Type == ClientType.VIE;
        }

        private void HandleGamePacketReceived(ListenData ld)
        {
            SubspaceBuffer buffer = _bufferPool.Get();
            IPEndPoint remoteIPEP = _objectPoolManager.IPEndPointPool.Get();
            Player p;
            ConnData conn;

            try
            {
                EndPoint remoteEP = remoteIPEP;

                try
                {
                    buffer.NumBytes = ld.GameSocket.ReceiveFrom(buffer.Bytes, buffer.Bytes.Length, SocketFlags.None, ref remoteEP);
                }
                catch (SocketException ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"SocketException with error code {ex.ErrorCode} when receiving from game socket {ld.GameSocket.LocalEndPoint}. {ex}");
                    buffer.Dispose();
                    return;
                }
                catch (Exception ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"Exception when receiving from game socket {ld.GameSocket.LocalEndPoint}. {ex}");
                    buffer.Dispose();
                    return;
                }

                if (buffer.NumBytes <= 0)
                {
                    buffer.Dispose();
                    return;
                }

#if CFG_DUMP_RAW_PACKETS
                DumpPk($"RECV: {buffer.NumBytes} bytes", buffer.Bytes.AsSpan(0, buffer.NumBytes));
#endif

                if (!ReferenceEquals(remoteIPEP, remoteEP))
                {
                    // Note: This seems to be the normal behavior.
                    // No idea why it makes you pass in a reference to an EndPoint if it's just going to replace it, but perhaps it isn't always the case.
                    _objectPoolManager.IPEndPointPool.Return(remoteIPEP);
                    remoteIPEP = null;
                }

                if (remoteEP is not IPEndPoint remoteEndPoint)
                {
                    buffer.Dispose();
                    return;
                }

                if (_clienthash.TryGetValue(remoteEndPoint, out p) == false)
                {
                    // this might be a new connection. make sure it's really a connection init packet
                    if (IsConnectionInitPacket(buffer.Bytes))
                    {
                        ProcessConnectionInit(remoteEndPoint, buffer.Bytes, buffer.NumBytes, ld);
                    }
#if CFG_LOG_STUPID_STUFF
                    else if (buffer.NumBytes > 1)
                    {
                        _logManager.LogM(LogLevel.Drivel, nameof(Network), $"Received data ({buffer.Bytes[0]:X2} {buffer.Bytes[1]:X2} ; {buffer.NumBytes} bytes) before connection established.");
                    }
                    else
                    {
                        _logManager.LogM(LogLevel.Drivel, nameof(Network), $"Received data ({buffer.Bytes[0]:X2} ; {buffer.NumBytes} bytes) before connection established.");
                    }
#endif
                    buffer.Dispose();
                    return;
                }

                conn = p[_connKey] as ConnData;
                if (conn == null)
                {
                    buffer.Dispose();
                    return;
                }

                if (IsConnectionInitPacket(buffer.Bytes))
                {
                    // here, we have a connection init, but it's from a
                    // player we've seen before. there are a few scenarios:
                    if (p.Status == PlayerState.Connected)
                    {
                        // if the player is in PlayerState.Connected, it means that
                        // the connection init response got dropped on the
                        // way to the client. we have to resend it.
                        ProcessConnectionInit(remoteEndPoint, buffer.Bytes, buffer.NumBytes, ld);
                    }
                    else
                    {
                        /* otherwise, he probably just lagged off or his
                         * client crashed. ideally, we'd postpone this
                         * packet, initiate a logout procedure, and then
                         * process it. we can't do that right now, so drop
                         * the packet, initiate the logout, and hope that
                         * the client re-sends it soon. */
                        _playerData.KickPlayer(p);
                    }

                    buffer.Dispose();
                    return;
                }
            }
            finally
            {
                if (remoteIPEP != null)
                    _objectPoolManager.IPEndPointPool.Return(remoteIPEP);
            }

            if (buffer.NumBytes > Constants.MaxPacket)
            {
                buffer.NumBytes = Constants.MaxPacket;
            }

            // we shouldn't get packets in this state, but it's harmless if we do
            if (p.Status >= PlayerState.LeavingZone || p.WhenLoggedIn >= PlayerState.LeavingZone)
            {
                buffer.Dispose();
                return;
            }

            if (p.Status > PlayerState.TimeWait)
            {
                _logManager.LogM(LogLevel.Warn, nameof(Network), $"[pid={p.Id}] Packet received from bad state {p.Status}.");

                // don't set lastpkt time here

                buffer.Dispose();
                return;
            }

            buffer.Conn = conn;
            conn.lastPkt = DateTime.UtcNow;
            conn.bytesReceived += (ulong)buffer.NumBytes;
            conn.pktReceived++;
            Interlocked.Add(ref _globalStats.byterecvd, (ulong)buffer.NumBytes);
            Interlocked.Increment(ref _globalStats.pktrecvd);

            IEncrypt enc = conn.enc;
            if (enc != null)
            {
                buffer.NumBytes = enc.Decrypt(p, buffer.Bytes, buffer.NumBytes);
            }

            if (buffer.NumBytes == 0)
            {
                // bad crc, or something
                _logManager.LogM(LogLevel.Malicious, nameof(Network), $"[pid={p.Id}] Failure decrypting packet.");
                buffer.Dispose();
                return;
            }

#if CFG_DUMP_RAW_PACKETS
            DumpPk($"RECV: about to process {buffer.NumBytes} bytes", buffer.Bytes.AsSpan(0, buffer.NumBytes));
#endif

            ProcessBuffer(buffer);
        }

        private void CallPacketHandlers(ConnData conn, byte[] bytes, int len)
        {
            if (conn == null)
                throw new ArgumentNullException(nameof(conn));

            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (len < 1)
                throw new ArgumentOutOfRangeException(nameof(len));

            byte packetType = bytes[0];

            if (conn.p != null)
            {
                // player connection
                PacketDelegate handler = null;

                if (packetType < _handlers.Length)
                    handler = _handlers[packetType];

                if (handler == null)
                {
                    _logManager.LogM(LogLevel.Drivel, nameof(Network), $"No handler for packet type 0x{packetType:X2}.");
                    return;
                }

                try
                {
                    handler(conn.p, bytes, len);
                }
                catch (Exception ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"Handler for packet type 0x{packetType:X2} threw an exception! {ex}.");
                }
            }
            else if (conn.cc != null)
            {
                // client connection
                try
                {
                    conn.cc.Handler.HandlePacket(bytes, len);
                }
                catch (Exception ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"(client connection) Handler for packet type 0x{packetType:X2} threw an exception! {ex}.");
                }
            }
            else
            {
                _logManager.LogM(LogLevel.Drivel, nameof(Network), $"No player or client connection, but got packet type [0x{packetType:X2}] of length {len}.");
            }
        }

        private void MainloopWork_CallPacketHandlers(SubspaceBuffer buffer)
        {
            if (buffer == null)
                return;

            try
            {
                ConnData conn = buffer.Conn;
                if (conn == null)
                    return;

                CallPacketHandlers(conn, buffer.Bytes, buffer.NumBytes);
            }
            finally
            {
                buffer.Dispose();
            }
        }

        /// <summary>
        /// unreliable packets will be processed before the call returns and freed.
        /// network packets will be processed by the appropriate network handler,
        /// which may free it or not.
        /// </summary>
        /// <param name="buffer"></param>
        private void ProcessBuffer(SubspaceBuffer buffer)
        {
            ConnData conn = buffer.Conn;
            byte t1 = buffer.Bytes[0];

            if (t1 == 0x00)
            {
                byte t2 = buffer.Bytes[1];

                // 'core' packet
                if ((t2 < _oohandlers.Length) && (_oohandlers[t2] != null))
                {
                    _oohandlers[t2](buffer);
                }
                else
                {
                    if (conn.p != null)
                    {
                        _logManager.LogM(LogLevel.Malicious, nameof(Network), $"[{conn.p.Name}] [pid={conn.p.Id}] Unknown network subtype {t2}.");
                    }
                    else
                    {
                        _logManager.LogM(LogLevel.Malicious, nameof(Network), $"(client connection) Unknown network subtype {t2}.");
                    }
                    buffer.Dispose();
                }
            }
            else if (t1 < MAXTYPES)
            {
                _mainloop.QueueMainWorkItem(mainloopWork_CallPacketHandlersAction, buffer);
            }
            else
            {
                try
                {
                    if (conn.p != null)
                        _logManager.LogM(LogLevel.Malicious, nameof(Network), $"[{conn.p.Name}] [pid={conn.p.Id}] Unknown packet type {t1}.");
                    else
                        _logManager.LogM(LogLevel.Malicious, nameof(Network), $"(client connection) Unknown packet type {t1}.");
                }
                finally
                {
                    buffer.Dispose();
                }
            }
        }

        private static bool IsConnectionInitPacket(byte[] data)
        {
            return data != null
                && data.Length >= 2
                && data[0] == 0x00 
                && ((data[1] == 0x01) || (data[1] == 0x11));
        }

        private void HandlePingPacketReceived(ListenData ld)
        {
            if (ld == null)
                return;

            using SubspaceBuffer buffer = _bufferPool.Get();
            IPEndPoint remoteIPEP = _objectPoolManager.IPEndPointPool.Get();

            try
            {
                EndPoint receivedFrom = new IPEndPoint(IPAddress.Any, 0);

                try
                {
                    buffer.NumBytes = ld.PingSocket.ReceiveFrom(buffer.Bytes, 4, SocketFlags.None, ref receivedFrom);
                }
                catch (SocketException ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"SocketException with error code {ex.ErrorCode} when receiving from ping socket {ld.PingSocket.LocalEndPoint}. {ex}");
                    buffer.Dispose();
                    return;
                }
                catch (Exception ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"Exception when receiving from ping socket {ld.PingSocket.LocalEndPoint}. {ex}");
                    buffer.Dispose();
                    return;
                }

                if (receivedFrom is not IPEndPoint remoteEndPoint)
                    return;

                if (buffer.NumBytes <= 0)
                    return;

                //
                // Refresh data (if needed)
                //

                if (_pingData.LastRefresh == null
                    || (DateTime.UtcNow - _pingData.LastRefresh) > _config.PingRefreshThreshold)
                {
                    foreach (PopulationStats stats in _pingData.ConnectAsPopulationStats.Values)
                    {
                        stats.TempTotal = stats.TempPlaying = 0;
                    }

                    IArenaManager aman = _broker.GetInterface<IArenaManager>();
                    if (aman == null)
                        return;

                    try
                    {
                        aman.Lock();

                        try
                        {
                            aman.GetPopulationSummary(out int total, out int playing);

                            // global
                            _pingData.Global.Total = (uint)total;
                            _pingData.Global.Playing = (uint)playing;

                            // arenas that are associated with ListenData
                            foreach (Arena arena in aman.ArenaList)
                            {
                                if (_pingData.ConnectAsPopulationStats.TryGetValue(arena.BaseName, out PopulationStats stats))
                                {
                                    stats.TempTotal += (uint)arena.Total;
                                    stats.TempPlaying += (uint)arena.Playing;
                                }
                            }

                            _pingData.LastRefresh = DateTime.UtcNow;
                        }
                        finally
                        {
                            aman.Unlock();
                        }
                    }
                    finally
                    {
                        _broker.ReleaseInterface(ref aman);
                    }

                    foreach (PopulationStats stats in _pingData.ConnectAsPopulationStats.Values)
                    {
                        stats.Total = stats.TempTotal;
                        stats.Playing = stats.TempPlaying;
                    }
                }

                //
                // Respond
                //

                if (buffer.NumBytes == 4)
                {
                    // bytes from receive
                    buffer.Bytes[4] = buffer.Bytes[0];
                    buffer.Bytes[5] = buffer.Bytes[1];
                    buffer.Bytes[6] = buffer.Bytes[2];
                    buffer.Bytes[7] = buffer.Bytes[3];

                    // # of clients
                    // Note: ASSS documentation says it's a UInt32, but it appears Continuum looks at only the first 2 bytes as an UInt16.
                    Span<byte> span = new(buffer.Bytes, 0, 4);

                    if (string.IsNullOrWhiteSpace(ld.ConnectAs))
                    {
                        // global
                        BinaryPrimitives.WriteUInt32LittleEndian(span, _pingData.Global.Total);
                    }
                    else
                    {
                        // specific arena/zone
                        BinaryPrimitives.WriteUInt32LittleEndian(span, _pingData.ConnectAsPopulationStats[ld.ConnectAs].Total);
                    }

                    try
                    {
                        int bytesSent = ld.PingSocket.SendTo(buffer.Bytes, 8, SocketFlags.None, remoteEndPoint);
                    }
                    catch (SocketException ex)
                    {
                        _logManager.LogM(LogLevel.Error, nameof(Network), $"SocketException with error code {ex.ErrorCode} when sending to {remoteEndPoint} with ping socket {ld.PingSocket.LocalEndPoint}. {ex}");
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logManager.LogM(LogLevel.Error, nameof(Network), $"Exception when sending to {remoteEndPoint} with ping socket {ld.PingSocket.LocalEndPoint}. {ex}");
                        return;
                    }
                }
                else if (buffer.NumBytes == 8)
                {
                    // TODO: add the ability handle ASSS' extended ping packets
                }
            }
            finally
            {
                if (remoteIPEP != null)
                    _objectPoolManager.IPEndPointPool.Return(remoteIPEP);
            }

            Interlocked.Increment(ref _globalStats.pcountpings);
        }

        private void HandleClientPacketReceived()
        {
            SubspaceBuffer buffer = _bufferPool.Get();
            IPEndPoint remoteIPEP = _objectPoolManager.IPEndPointPool.Get();

            try
            {
                EndPoint receivedFrom = remoteIPEP;

                try
                {
                    buffer.NumBytes = _clientSocket.ReceiveFrom(buffer.Bytes, buffer.Bytes.Length, SocketFlags.None, ref receivedFrom);
                }
                catch (SocketException ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"SocketException with error code {ex.ErrorCode} when receiving from client socket {_clientSocket.LocalEndPoint}. {ex}");
                    buffer.Dispose();
                    return;
                }
                catch (Exception ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"Exception when receiving from client socket {_clientSocket.LocalEndPoint}. {ex}");
                    buffer.Dispose();
                    return;
                }

                if (buffer.NumBytes < 1)
                {
                    buffer.Dispose();
                    return;
                }

#if CFG_DUMP_RAW_PACKETS
                DumpPk($"RAW CLIENT DATA: {buffer.NumBytes} bytes", buffer.Bytes.AsSpan(0, buffer.NumBytes));
#endif

                bool found;
                NetClientConnection cc;

                _clientConnectionsLock.EnterReadLock();

                try
                {
                    found = _clientConnections.TryGetValue(receivedFrom, out cc);
                }
                finally
                {
                    _clientConnectionsLock.ExitReadLock();
                }

                if (found)
                {
                    ConnData conn = cc.ConnData;

                    if (cc.Encryptor != null)
                    {
                        buffer.NumBytes = cc.Encryptor.Decrypt(cc, buffer.Bytes, buffer.NumBytes);

#if CFG_DUMP_RAW_PACKETS
                        DumpPk($"DECRYPTED CLIENT DATA: {buffer.NumBytes} bytes", new ReadOnlySpan<byte>(buffer.Bytes, 0, buffer.NumBytes));
#endif
                    }

                    if (buffer.NumBytes > 0)
                    {
                        buffer.Conn = conn;
                        conn.lastPkt = DateTime.UtcNow;
                        conn.bytesReceived += (ulong)buffer.NumBytes;
                        conn.pktReceived++;
                        Interlocked.Add(ref _globalStats.byterecvd, (ulong)buffer.NumBytes);
                        Interlocked.Increment(ref _globalStats.pktrecvd);

                        ProcessBuffer(buffer);
                    }
                    else
                    {
                        buffer.Dispose();
                        _logManager.LogM(LogLevel.Malicious, nameof(Network), $"(client connection) Failed to decrypt packet.");
                    }

                    return;
                }

                buffer.Dispose();
                _logManager.LogM(LogLevel.Warn, nameof(Network), $"Got data on the client port that was not from any known connection ({receivedFrom}).");
            }
            finally
            {
                _objectPoolManager.IPEndPointPool.Return(remoteIPEP);
            }
        }

        private SubspaceBuffer BufferPacket(ConnData conn, ReadOnlySpan<byte> data, NetSendFlags flags, IReliableCallbackInvoker callbackInvoker = null)
        {
            int len = data.Length;

            // data has to be able to fit into a reliable packet
            Debug.Assert(len <= Constants.MaxPacket - Constants.ReliableHeaderLen);

            // you can't buffer already-reliable packets
            Debug.Assert(!(data.Length >= 2 && data[0] == 0x00 && data[1] == 0x03));

            // reliable packets can't be droppable
            Debug.Assert((flags & (NetSendFlags.Reliable | NetSendFlags.Droppable)) != (NetSendFlags.Reliable | NetSendFlags.Droppable));

            BandwidthPriorities pri;

            if ((flags & NetSendFlags.Ack) == NetSendFlags.Ack)
            {
                pri = BandwidthPriorities.Ack;
            }
            else if ((flags & NetSendFlags.Reliable) == NetSendFlags.Reliable)
            {
                pri = BandwidthPriorities.Reliable;
            }
            else
            {
                // figure out priority (ignoring the reliable, droppable, and urgent flags)
                pri = ((int)flags & 0x70) switch
                {
                    (int)NetSendFlags.PriorityN1 & 0x70 => BandwidthPriorities.UnreliableLow,
                    (int)NetSendFlags.PriorityP4 & 0x70 or (int)NetSendFlags.PriorityP5 & 0x70 => BandwidthPriorities.UnreliableHigh,
                    _ => BandwidthPriorities.Unreliable,
                };
            }

            // update global stats based on requested priority
            Interlocked.Add(ref _globalStats.pri_stats[(int)pri], (ulong)len);

            // try the fast path
            if ((flags & (NetSendFlags.Urgent | NetSendFlags.Reliable)) == NetSendFlags.Urgent)
            {
                // urgent and not reliable
                if (conn.bw.Check(len + _config.PerPacketOverhead, (int)pri))
                {
                    SendRaw(conn, data);
                    return null;
                }
                else
                {
                    if ((flags & NetSendFlags.Droppable) == NetSendFlags.Droppable)
                    {
                        conn.pktdropped++;
                        return null;
                    }
                }
            }

            SubspaceBuffer buf = _bufferPool.Get();
            buf.Conn = conn;
            buf.LastRetry = DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 10));
            buf.Tries = 0;
            buf.CallbackInvoker = callbackInvoker;
            buf.Flags = flags;

            // get data into packet
            if ((flags & NetSendFlags.Reliable) == NetSendFlags.Reliable)
            {
                buf.NumBytes = len + Constants.ReliableHeaderLen;
                ref ReliableHeader rp = ref MemoryMarshal.AsRef<ReliableHeader>(buf.Bytes);
                rp.T1 = 0x00;
                rp.T2 = 0x03;
                rp.SeqNum = conn.s2cn++;
                data.CopyTo(buf.Bytes.AsSpan(ReliableHeader.Length, len));
            }
            else
            {
                buf.NumBytes = len;
                data.CopyTo(buf.Bytes);
            }

            conn.outlist[(int)pri].AddLast(buf);

            return buf;
        }

        #region oohandlers (network layer header handling)

        private void ProcessKeyResponse(SubspaceBuffer buffer)
        {
            if (buffer == null)
                return;

            try
            {
                if (buffer.NumBytes != 6)
                    return;

                ConnData conn = buffer.Conn;
                if (conn == null)
                    return;

                if (conn.cc != null)
                    conn.cc.Handler.Connected();
                else if (conn.p != null)
                    _logManager.LogP(LogLevel.Malicious, nameof(Network), conn.p, "Got key response packet.");
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private void ProcessReliable(SubspaceBuffer buffer)
        {
            if (buffer == null)
                return;

            if (buffer.NumBytes < 7)
            {
                buffer.Dispose();
                return;
            }

            ref ReliableHeader rp = ref MemoryMarshal.AsRef<ReliableHeader>(buffer.Bytes);
            int sn = rp.SeqNum;

            ConnData conn = buffer.Conn;
            if (conn == null)
            {
                buffer.Dispose();
                return;
            }

            Monitor.Enter(conn.relmtx);

            if ((sn - conn.c2sn) >= Constants.CFG_INCOMING_BUFFER || sn < 0)
            {
                Monitor.Exit(conn.relmtx);

                // just drop it
                if (conn.p != null)
                    _logManager.LogM(LogLevel.Drivel, nameof(Network), $"[{conn.p.Name}] [pid={conn.p.Id}] Reliable packet with too big delta ({sn} - {conn.c2sn}).");
                else
                    _logManager.LogM(LogLevel.Drivel, nameof(Network), $"(client connection) Reliable packet with too big delta ({sn} - {conn.c2sn}).");

                buffer.Dispose();
            }
            else
            {
                // store it and ack
                bool canProcess = false;

                // add to rel stuff to be processed
                int spot = sn % Constants.CFG_INCOMING_BUFFER;
                if ((sn < conn.c2sn) || (conn.relbuf[spot] != null))
                {
                    // a dup
                    conn.relDups++;
                    buffer.Dispose();
                }
                else
                {
                    conn.relbuf[spot] = buffer;
                    canProcess = (sn == conn.c2sn);
                }

                Monitor.Exit(conn.relmtx);

                // send the ack
                AckPacket ap = new(sn);

                lock (conn.olmtx)
                {
                    BufferPacket(
                        conn, 
                        MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref ap, 1)), 
                        NetSendFlags.Ack);
                }

                if (canProcess)
                {
                    // add to global rel list for processing
                    _relqueue.Enqueue(conn);
                }
            }
        }

        private void ProcessAck(SubspaceBuffer buffer)
        {
            if (buffer == null)
                return;

            using (buffer)
            {
                if (buffer.NumBytes != 6) // ack packets are 6 bytes long
                    return;

                ConnData conn = buffer.Conn;
                if (conn == null)
                    return;

                ref AckPacket ack = ref MemoryMarshal.AsRef<AckPacket>(buffer.Bytes);
                int seqNum = ack.SeqNum;

                Monitor.Enter(conn.olmtx);

                LinkedList<SubspaceBuffer> outlist = conn.outlist[(int)BandwidthPriorities.Reliable];
                LinkedListNode<SubspaceBuffer> nextNode = null;
                for (LinkedListNode<SubspaceBuffer> node = outlist.First; node != null; node = nextNode)
                {
                    nextNode = node.Next;

                    SubspaceBuffer b = node.Value;
                    ref ReliableHeader brp = ref MemoryMarshal.AsRef<ReliableHeader>(b.Bytes);
                    if (seqNum == brp.SeqNum)
                    {
                        outlist.Remove(node);
                        Monitor.Exit(conn.olmtx);

                        if (b.CallbackInvoker != null)
                        {
                            QueueMainloopWorkItem(b.CallbackInvoker, conn.p, true);
                            b.CallbackInvoker = null; // the workitem is now responsible for disposing the callback invoker
                        }

                        if (b.Tries == 1)
                        {
                            int rtt = (int)DateTime.UtcNow.Subtract(b.LastRetry).TotalMilliseconds;
                            if (rtt < 0)
                            {
                                _logManager.LogM(LogLevel.Error, nameof(Network), $"Negative rtt ({rtt}); clock going backwards.");
                                rtt = 100;
                            }

                            int dev = conn.avgrtt - rtt;
                            if (dev < 0)
                                dev = -dev;

                            conn.rttdev = (conn.rttdev * 3 + dev) / 4;
                            conn.avgrtt = (conn.avgrtt * 7 + rtt) / 8;

                            if (_lagCollect != null && conn.p != null)
                                _lagCollect.RelDelay(conn.p, rtt);
                        }

                        b.Dispose();

                        // handle limit adjustment
                        conn.bw.AdjustForAck();

                        return;
                    }
                }

                Monitor.Exit(conn.olmtx);
            }
        }

        private void QueueMainloopWorkItem(IReliableCallbackInvoker callbackInvoker, Player p, bool success)
        {
            if (callbackInvoker == null)
                throw new ArgumentNullException(nameof(callbackInvoker));

            if (p == null)
                throw new ArgumentNullException(nameof(p));

            _mainloop.QueueMainWorkItem(
                mainloopWork_InvokeReliableCallbackAction,
                new InvokeReliableCallbackDTO()
                {
                    CallbackInvoker = callbackInvoker,
                    Player = p,
                    Success = success,
                });
        }

        private struct InvokeReliableCallbackDTO
        {
            public IReliableCallbackInvoker CallbackInvoker;
            public Player Player;
            public bool Success;
        }

        private void MainloopWork_InvokeReliableCallback(InvokeReliableCallbackDTO dto)
        {
            if (dto.CallbackInvoker == null)
                return;

            using (dto.CallbackInvoker)
            {
                if (dto.Player == null)
                    return;

                dto.CallbackInvoker.Invoke(dto.Player, dto.Success);
            }
        }

        private void ProcessSyncRequest(SubspaceBuffer buffer)
        {
            if (buffer == null)
                return;

            try
            {
                if (buffer.NumBytes != TimeSyncC2SPacket.Length)
                    return;

                ConnData conn = buffer.Conn;
                if (conn == null)
                    return;

                ref readonly TimeSyncC2SPacket cts = ref MemoryMarshal.AsRef<TimeSyncC2SPacket>(new ReadOnlySpan<byte>(buffer.Bytes, 0, buffer.NumBytes));
                uint clientTime = cts.Time;
                uint serverTime = ServerTick.Now;

                TimeSyncS2CPacket ts = new();
                ts.Initialize(clientTime, serverTime);

                lock (conn.olmtx)
                {
                    // note: this bypasses bandwidth limits
                    SendRaw(conn, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref ts, 1)));

                    // submit data to lagdata
                    if (_lagCollect != null && conn.p != null)
                    {
                        TimeSyncData data = new()
                        {
                            ServerPacketsReceived = conn.pktReceived,
                            ServerPacketsSent = conn.pktSent,
                            ClientPacketsReceived = cts.PktRecvd,
                            ClientPacketsSent = cts.PktSent,
                            ServerTime = serverTime,
                            ClientTime = clientTime,
                        };
                        
                        _lagCollect.TimeSync(conn.p, in data);
                    }
                }
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private void ProcessDrop(SubspaceBuffer buffer)
        {
            if (buffer == null)
                return;

            try
            {
                if (buffer.NumBytes != 2)
                    return;

                ConnData conn = buffer.Conn;
                if (conn == null)
                    return;

                if (conn.p != null)
                {
                    _playerData.KickPlayer(conn.p);
                }
                else if (conn.cc != null)
                {
                    //buf->conn->cc->i->Disconnected();
                    /* FIXME: this sends an extra 0007 to the client. that should
                        * probably go away. */
                    //DropClientConnection(buf->conn->cc);
                }
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private struct BigPacketWork
        {
            public ConnData ConnData;
            public BigReceive BigReceive;
        }

        private void MainloopWork_CallBigPacketHandlers(BigPacketWork work)
        {
            try
            {
                if (work.ConnData == null || work.BigReceive == null || work.BigReceive.Buffer == null || work.BigReceive.Size < 1)
                    return;

                CallPacketHandlers(work.ConnData, work.BigReceive.Buffer, work.BigReceive.Size);
            }
            finally
            {
                if (work.BigReceive != null)
                {
                    // return the buffer to its pool
                    work.BigReceive.Dispose();
                }
            }
        }

        private void ProcessBigData(SubspaceBuffer buffer)
        {
            if (buffer == null)
                return;

            try
            {
                if (buffer.NumBytes < 3) // 0x00, [0x08 or 0x09], and then at least one byte of data
                    return;

                ConnData conn = buffer.Conn;
                if (conn == null)
                    return;

                lock (conn.bigmtx)
                {
                    if (conn.BigRecv == null)
                    {
                        // Get a BigReceive object and give it to the ConnData object to 'own'.
                        conn.BigRecv = _objectPoolManager.Get<BigReceive>();
                    }

                    int newSize = conn.BigRecv.Size + buffer.NumBytes - 2;

                    if (newSize <= 0 || newSize > Constants.MaxBigPacket)
                    {
                        if (conn.p != null)
                            _logManager.LogP(LogLevel.Malicious, nameof(Network), conn.p, $"Refusing to allocate {newSize} bytes (> {Constants.MaxBigPacket}).");
                        else
                            _logManager.LogM(LogLevel.Malicious, nameof(Network), $"(client connection) Refusing to allocate {newSize} bytes (> {Constants.MaxBigPacket}).");

                        conn.BigRecv.Dispose();
                        conn.BigRecv = null;
                        return;
                    }

                    // Append the data.
                    conn.BigRecv.Append(new(buffer.Bytes, 2, buffer.NumBytes - 2)); // data only, header removed

                    if (buffer.Bytes[1] == 0x08)
                        return;

                    // Getting here means the we got 0x09 (end of "Big" data packet stream), so we should process it now.
                    if (conn.BigRecv.Buffer[0] > 0 && conn.BigRecv.Buffer[0] < MAXTYPES)
                    {
                        // Take ownership of the BigReceive object from the ConnData object.
                        BigReceive bigReceive = conn.BigRecv;
                        conn.BigRecv = null;

                        // Process it on the mainloop thread.
                        // Ownership of the BigReceive object is transferred to the workitem. The workitem is responsible for disposing it.
                        _mainloop.QueueMainWorkItem(
                            mainloopWork_CallBigPacketHandlersAction,
                            new BigPacketWork()
                            {
                                ConnData = conn,
                                BigReceive = bigReceive,
                            });
                    }
                    else
                    {
                        if (conn.p != null)
                            _logManager.LogP(LogLevel.Warn, nameof(Network), conn.p, $"Bad type for bigpacket: {conn.BigRecv.Buffer[0]}.");
                        else
                            _logManager.LogM(LogLevel.Warn, nameof(Network), $"(client connection) Bad type for bigpacket: {conn.BigRecv.Buffer[0]}.");

                        conn.BigRecv.Dispose();
                        conn.BigRecv = null;
                    }
                }
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private void ProcessPresize(SubspaceBuffer buffer)
        {
            if (buffer == null)
                return;

            try
            {
                if (buffer.NumBytes < 7)
                    return;

                ConnData conn = buffer.Conn;
                if (conn == null)
                    return;

                ref PresizedHeader header = ref MemoryMarshal.AsRef<PresizedHeader>(buffer.Bytes);
                int size = header.Size;
                Span<byte> data = buffer.Bytes.AsSpan(PresizedHeader.Length, buffer.NumBytes - PresizedHeader.Length);

                lock (conn.bigmtx)
                {
                    // only handle presized packets for player connections, not client connections
                    if (conn.p == null)
                        return;

                    if (conn.sizedrecv.offset == 0)
                    {
                        // first packet

                        int type = data[0];
                        if (type < MAXTYPES)
                        {
                            conn.sizedrecv.type = type;
                            conn.sizedrecv.totallen = size;
                        }
                        else
                        {
                            EndSized(conn.p, false);
                        }
                    }

                    if (conn.sizedrecv.totallen != size)
                    {
                        _logManager.LogP(LogLevel.Malicious, nameof(Network), conn.p, "Length mismatch in sized packet.");
                        EndSized(conn.p, false);
                    }
                    else if ((conn.sizedrecv.offset + buffer.NumBytes - 6) > size)
                    {
                        _logManager.LogP(LogLevel.Malicious, nameof(Network), conn.p, "Sized packet overflow.");
                        EndSized(conn.p, false);
                    }
                    else
                    {
                        _sizedhandlers[conn.sizedrecv.type]?.Invoke(conn.p, data, conn.sizedrecv.offset, size);

                        conn.sizedrecv.offset += data.Length;

                        if (conn.sizedrecv.offset >= size)
                            EndSized(conn.p, true); // sized receive is complete
                    }
                }
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private void ProcessCancelReq(SubspaceBuffer buffer)
        {
            if (buffer == null)
                return;

            try
            {
                if (buffer.NumBytes != 2)
                    return;

                ConnData conn = buffer.Conn;
                if (conn == null)
                    return;

                // the client has requested a cancel for the file transfer
                lock (conn.olmtx)
                {
                    // cancel current presized transfer
                    LinkedListNode<ISizedSendData> node = conn.sizedsends.First;
                    if (node != null)
                    {
                        ISizedSendData sd = node.Value;
                        if (sd != null)
                        {
                            sd.RequestData(0, Span<byte>.Empty); // notify transfer complete
                        }
                        conn.sizedsends.RemoveFirst();
                    }

                    ReadOnlySpan<byte> cancelPresizedAckSpan = stackalloc byte[2] { 0x00, 0x0C };
                    BufferPacket(conn, cancelPresizedAckSpan, NetSendFlags.Reliable);
                }
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private void ProcessCancel(SubspaceBuffer buffer)
        {
            if (buffer == null)
                return;

            try
            {
                if (buffer.NumBytes != 2)
                    return;

                ConnData conn = buffer.Conn;
                if (conn == null)
                    return;

                if (conn.p != null)
                {
                    lock (conn.bigmtx)
                    {
                        EndSized(conn.p, false);
                    }
                }
            }
            finally
            {
                buffer.Dispose();
            }
        }

        // call with bigmtx locked
        private void EndSized(Player p, bool success)
        {
            if (p == null)
                return;

            if (p[_connKey] is not ConnData conn)
                return;

            if (conn.sizedrecv.offset != 0)
            {
                int type = conn.sizedrecv.type;
                int arg = success ? conn.sizedrecv.totallen : -1;

                // tell listeners that they're cancelled
                if (type < MAXTYPES)
                {
                    _sizedhandlers[type]?.Invoke(p, Span<byte>.Empty, arg, arg);
                }

                conn.sizedrecv.type = 0;
                conn.sizedrecv.totallen = 0;
                conn.sizedrecv.offset = 0;
            }
        }

        private void ProcessGrouped(SubspaceBuffer buffer)
        {
            if (buffer == null)
                return;

            try
            {
                if (buffer.NumBytes < 4)
                    return;

                ConnData conn = buffer.Conn;
                if (conn == null)
                    return;

                int pos = 2, len = 1;

                while (pos < buffer.NumBytes && len > 0)
                {
                    len = buffer.Bytes[pos++];
                    if (pos + len <= buffer.NumBytes)
                    {
                        SubspaceBuffer b = _bufferPool.Get();
                        b.Conn = conn;
                        b.NumBytes = len;
                        Array.Copy(buffer.Bytes, pos, b.Bytes, 0, len);
                        ProcessBuffer(b);
                    }

                    pos += len;
                }
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private void ProcessSpecial(SubspaceBuffer buffer)
        {
            if (buffer == null || buffer.NumBytes < 2)
                return;

            try
            {
                Player p = buffer.Conn?.p;
                if (p == null)
                    return;

                int t2 = buffer.Bytes[1];

                if (t2 < _nethandlers.Length)
                {
                    _nethandlers[t2]?.Invoke(p, buffer.Bytes, buffer.NumBytes);
                }
            }
            finally
            {
                buffer.Dispose();
            }
        }

        #endregion

        #region IModule Members

        public bool Load(
            ComponentBroker broker,
            IBandwidthLimit bandwidthLimit,
            IConfigManager configManager,
            ILagCollect lagCollect,
            ILogManager logManager,
            IMainloop mainloop,
            IMainloopTimer mainloopTimer,
            IObjectPoolManager objectPoolManager,
            IPlayerData playerData,
            IPrng prng)
        {
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _bandwithLimit = bandwidthLimit ?? throw new ArgumentNullException(nameof(bandwidthLimit));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _lagCollect = lagCollect ?? throw new ArgumentNullException(nameof(lagCollect));
            _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
            _mainloop = mainloop ?? throw new ArgumentNullException(nameof(mainloop));
            _mainloopTimer = mainloopTimer ?? throw new ArgumentNullException(nameof(mainloopTimer));
            _objectPoolManager = objectPoolManager ?? throw new ArgumentNullException(nameof(objectPoolManager));
            _playerData = playerData ?? throw new ArgumentNullException(nameof(playerData));
            _prng = prng ?? throw new ArgumentNullException(nameof(prng));

            _connKey = _playerData.AllocatePlayerData<ConnData>();

            _config.DropTimeout = TimeSpan.FromMilliseconds(_configManager.GetInt(_configManager.Global, "Net", "DropTimeout", 3000) * 10);
            _config.MaxOutlistSize = _configManager.GetInt(_configManager.Global, "Net", "MaxOutlistSize", 500);

            // (deliberately) undocumented settings
            _config.MaxRetries = _configManager.GetInt(_configManager.Global, "Net", "MaxRetries", 15);
            _config.PresizedQueueThreshold = _configManager.GetInt(_configManager.Global, "Net", "PresizedQueueThreshold", 5);
            _config.PresizedQueuePackets = _configManager.GetInt(_configManager.Global, "Net", "PresizedQueuePackets", 25);
            int reliableThreadCount = _configManager.GetInt(_configManager.Global, "Net", "ReliableThreads", 1);
            _config.PerPacketOverhead = _configManager.GetInt(_configManager.Global, "Net", "PerPacketOverhead", 28);
            _config.PingRefreshThreshold = TimeSpan.FromMilliseconds(10 * _configManager.GetInt(_configManager.Global, "Net", "PingDataRefreshTime", 200));
            _config.simplepingpopulationmode = (PingPopulationMode)_configManager.GetInt(_configManager.Global, "Net", "SimplePingPopulationMode", 1);

            _bufferPool = objectPoolManager.GetPool<SubspaceBuffer>();

            if (InitializeSockets() == false)
                return false;

            _stopToken = _stopCancellationTokenSource.Token;

            // receive thread
            Thread thread = new(ReceiveThread);
            thread.Name = "network-recv";
            thread.Start();
            _threadList.Add(thread);

            // send thread
            thread = new Thread(SendThread)
            {
                Name = "network-send"
            };
            thread.Start();
            _threadList.Add(thread);

            // reliable threads
            for (int i = 0; i < reliableThreadCount; i++)
            {
                thread = new Thread(RelThread)
                {
                    Name = "network-rel-" + i
                };
                thread.Start();
                _reliableThreads.Add(thread);
                _threadList.Add(thread);
            }

            // queue more data thread (sends sized data)
            _mainloopTimer.SetTimer(MainloopTimer_QueueSizedData, 200, 110, null); // TODO: maybe change it to be in its own thread?

            _iNetworkToken = broker.RegisterInterface<INetwork>(this);
            _iNetworkClientToken = broker.RegisterInterface<INetworkClient>(this);
            _iNetworkEncryptionToken = broker.RegisterInterface<INetworkEncryption>(this);
            return true;
        }

        bool IModule.Unload(ComponentBroker broker)
        {
            if (broker.UnregisterInterface<INetwork>(ref _iNetworkToken) != 0)
                return false;

            if (broker.UnregisterInterface<INetworkClient>(ref _iNetworkClientToken) != 0)
                return false;

            if (broker.UnregisterInterface<INetworkEncryption>(ref _iNetworkEncryptionToken) != 0)
                return false;

            _mainloopTimer.ClearTimer(MainloopTimer_QueueSizedData, null);

            // stop threads
            _stopCancellationTokenSource.Cancel();

            for (int x = 0; x < _reliableThreads.Count; x++)
            {
                _relqueue.Enqueue(null);
            }

            foreach (Thread thread in _threadList)
            {
                thread.Join();
            }
            _threadList.Clear();
            _reliableThreads.Clear();

            _mainloop.WaitForMainWorkItemDrain();

            Array.Clear(_handlers, 0, _handlers.Length);
            Array.Clear(_nethandlers, 0, _nethandlers.Length); // for some reason ASSS doesn't clear this?
            Array.Clear(_sizedhandlers, 0, _sizedhandlers.Length);
            _relqueue.Clear();

            // close all sockets
            foreach (ListenData listenData in _listenDataList)
            {
                listenData.GameSocket.Close();
                listenData.PingSocket.Close();
            }

            _listenDataList.Clear();
            _pingData.ConnectAsPopulationStats.Clear();

            _clientSocket.Close();
            _clientSocket = null;

            _playerData.FreePlayerData(_connKey);
            
            return true;
        }

        #endregion

        #region IModuleLoaderAware Members

        bool IModuleLoaderAware.PostLoad(ComponentBroker broker)
        {
            // NOOP
            return true;
        }

        bool IModuleLoaderAware.PreUnload(ComponentBroker broker)
        {
            ReadOnlySpan<byte> disconnectSpan = stackalloc byte[] { 0x00, 0x07 };

            //
            // Disconnect all clients nicely
            //

            _playerData.Lock();

            try
            {
                foreach (Player player in _playerData.PlayerList)
                {
                    if (IsOurs(player))
                    {
                        if (player[_connKey] is not ConnData conn)
                            continue;

                        SendRaw(conn, disconnectSpan);

                        if (conn.enc != null)
                        {
                            conn.enc.Void(player);
                            broker.ReleaseInterface(ref conn.enc, conn.iEncryptName);
                        }
                    }
                }
            }
            finally
            {
                _playerData.Unlock();
            }

            //
            // And disconnect our client connections
            //

            _clientConnectionsLock.EnterWriteLock();

            try
            {
                foreach (NetClientConnection cc in _clientConnections.Values)
                {
                    SendRaw(cc.ConnData, disconnectSpan);
                    cc.Handler.Disconnected();

                    if (cc.Encryptor != null)
                    {
                        cc.Encryptor.Void(cc);
                        broker.ReleaseInterface(ref cc.Encryptor, cc.EncryptorName);
                    }
                }

                _clientConnections.Clear();
            }
            finally
            {
                _clientConnectionsLock.ExitWriteLock();
            }

            return true;
        }

        #endregion

        #region INetworkEncryption Members

        private readonly List<ConnectionInitHandler> _connectionInitHandlers = new();
        private readonly ReaderWriterLockSlim _connectionInitLock = new(LockRecursionPolicy.NoRecursion);

        private bool ProcessConnectionInit(IPEndPoint remoteEndpoint, byte[] buffer, int len, ListenData ld)
        {
            _connectionInitLock.EnterReadLock();

            try
            {
                foreach (ConnectionInitHandler handler in _connectionInitHandlers)
                {
                    if (handler(remoteEndpoint, buffer, len, ld))
                        return true;
                }
            }
            finally
            {
                _connectionInitLock.ExitReadLock();
            }

            _logManager.LogM(LogLevel.Info, nameof(Network), $"Got a connection init packet, but no handler processed it.  Please verify that an encryption module is loaded or the {nameof(EncryptionNull)} module if no encryption is desired.");
            return false;
        }

        void INetworkEncryption.AppendConnectionInitHandler(ConnectionInitHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _connectionInitLock.EnterWriteLock();

            try
            {
                _connectionInitHandlers.Add(handler);
            }
            finally
            {
                _connectionInitLock.ExitWriteLock();
            }
        }

        bool INetworkEncryption.RemoveConnectionInitHandler(ConnectionInitHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _connectionInitLock.EnterWriteLock();

            try
            {
                return _connectionInitHandlers.Remove(handler);
            }
            finally
            {
                _connectionInitLock.ExitWriteLock();
            }
        }

        void INetworkEncryption.ReallyRawSend(IPEndPoint remoteEndPoint, ReadOnlySpan<byte> data, ListenData ld)
        {
            if (remoteEndPoint == null)
                throw new ArgumentNullException(nameof(remoteEndPoint));

            if (data.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(data), "There needs to be at least 1 byte to send.");

            if (ld == null)
                throw new ArgumentNullException(nameof(ld));

#if CFG_DUMP_RAW_PACKETS
            DumpPk($"SENDRAW: {data.Length} bytes", data);
#endif

            // FUTURE: Change this when/if Microsoft adds a Socket.SendTo(ReadOnlySpan<byte>,...) overload. For now, need to copy to a byte[].
            using (SubspaceBuffer buffer = _bufferPool.Get())
            {
                data.CopyTo(new Span<byte>(buffer.Bytes, 0, data.Length));

                try
                {
                    ld.GameSocket.SendTo(buffer.Bytes, 0, data.Length, SocketFlags.None, remoteEndPoint);
                }
                catch (SocketException ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"SocketException with error code {ex.ErrorCode} when sending to {remoteEndPoint} with game socket {ld.GameSocket.LocalEndPoint}. {ex}");
                    return;
                }
                catch (Exception ex)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"Exception when sending to {remoteEndPoint} with game socket {ld.GameSocket.LocalEndPoint}. {ex}");
                    return;
                }
            }

            Interlocked.Add(ref _globalStats.bytesent, (ulong)data.Length);
            Interlocked.Increment(ref _globalStats.pktsent);
        }

        Player INetworkEncryption.NewConnection(ClientType clientType, IPEndPoint remoteEndpoint, string iEncryptName, ListenData ld)
        {
            if (ld == null)
                throw new ArgumentNullException(nameof(ld));

            // certain ports may have restrictions on client types
            if ((clientType == ClientType.VIE && !ld.AllowVIE)
                || (clientType == ClientType.Continuum && !ld.AllowContinuum))
            {
                return null;
            }

            // try to find a matching player for the endpoint
            if (remoteEndpoint != null && _clienthash.TryGetValue(remoteEndpoint, out Player p))
            {
                // We found it. If its status is Connected, just return the pid.
                // It means we have to redo part of the connection init.

                if (p.Status <= PlayerState.Connected)
                {
                    return p;
                }
                else
                {
                    // otherwise, something is horribly wrong. make a note to this effect
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"[pid={p.Id}] NewConnection called for an established address.");
                    return null;
                }
            }

            p = _playerData.NewPlayer(clientType);
            ConnData conn = p[_connKey] as ConnData;

            IEncrypt enc = null;
            if (iEncryptName != null)
            {
                enc = _broker.GetInterface<IEncrypt>(iEncryptName);

                if (enc == null)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"[pid={p.Id}] NewConnection called to use IEncrypt '{iEncryptName}', but not found.");
                    return null;
                }
            }
            
            conn.Initalize(enc, iEncryptName, _bandwithLimit.New());
            conn.p = p;

            // copy data from ListenData
            conn.whichSock = ld.GameSocket;
            p.ConnectAs = ld.ConnectAs;

            p.IpAddress = remoteEndpoint.Address;

            p.ClientName = clientType switch
            {
                ClientType.VIE => "<ss/vie client>",
                ClientType.Continuum => "<continuum>",
                _ => "<unknown game client>",
            };

            if (remoteEndpoint != null)
            {
                conn.RemoteEndpoint = remoteEndpoint;

                _clienthash[remoteEndpoint] = p;
            }

            _playerData.WriteLock();
            try
            {
                p.Status = PlayerState.Connected;
            }
            finally
            {
                _playerData.WriteUnlock();
            }

            if (remoteEndpoint != null)
            {
                _logManager.LogM(LogLevel.Drivel, nameof(Network), $"[pid={p.Id}] New connection from {remoteEndpoint}.");
            }
            else
            {
                _logManager.LogM(LogLevel.Drivel, nameof(Network), $"[pid={p.Id}] New internal connection.");
            }

            return p;
        }

        #endregion

        #region INetwork Members

        void INetwork.SendToOne(Player p, ReadOnlySpan<byte> data, NetSendFlags flags)
        {
            if (p == null)
                return;

            if (data.Length < 1)
                return;

            if (!IsOurs(p))
                return;

            if (p[_connKey] is not ConnData conn)
                return;

            // see if we can do it the quick way
            if (data.Length <= (Constants.MaxPacket - Constants.ReliableHeaderLen))
            {
                lock (conn.olmtx)
                {
                    BufferPacket(conn, data, flags);
                }
            }
            else
            {
                HashSet<Player> set = _objectPoolManager.PlayerSetPool.Get();

                try
                {
                    set.Add(p);
                    SendToSet(set, data, flags);
                }
                finally
                {
                    _objectPoolManager.PlayerSetPool.Return(set);
                }
            }
        }

        void INetwork.SendToOne<TData>(Player p, ref TData data, NetSendFlags flags) where TData : struct
        {
            ((INetwork)this).SendToOne(p, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref data, 1)), flags);
        }

        void INetwork.SendToArena(Arena arena, Player except, ReadOnlySpan<byte> data, NetSendFlags flags)
        {
            if (data.Length < 1)
                return;

            HashSet<Player> set = _objectPoolManager.PlayerSetPool.Get();

            try
            {
                _playerData.Lock();

                try
                {
                    foreach (Player p in _playerData.PlayerList)
                    {
                        if (p.Status == PlayerState.Playing
                            && (p.Arena == arena || arena == null)
                            && p != except
                            && IsOurs(p))
                        {
                            set.Add(p);
                        }
                    }
                }
                finally
                {
                    _playerData.Unlock();
                }

                SendToSet(set, data, flags);
            }
            finally
            {
                _objectPoolManager.PlayerSetPool.Return(set);
            }
        }

        void INetwork.SendToArena<TData>(Arena arena, Player except, ref TData data, NetSendFlags flags) where TData : struct
        {
            ((INetwork)this).SendToArena(arena, except, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref data, 1)), flags);
        }

        void INetwork.SendToSet(HashSet<Player> set, ReadOnlySpan<byte> data, NetSendFlags flags)
        {
            SendToSet(set, data, flags);
        }

        void INetwork.SendToSet<TData>(HashSet<Player> set, ref TData data, NetSendFlags flags) where TData : struct
        {
            ((INetwork)this).SendToSet(set, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref data, 1)), flags);
        }

        private void SendToSet(HashSet<Player> set, ReadOnlySpan<byte> data, NetSendFlags flags)
        {
            if (set == null)
                return;

            int len = data.Length;

            if (len < 1)
                return;

            if (len > Constants.MaxPacket - Constants.ReliableHeaderLen)
            {
                // use 00 08/9 packets (big data packets)
                // send these reliably (to maintain ordering with sequence #)
                Span<byte> bufferSpan = stackalloc byte[Constants.ChunkSize + 2];
                Span<byte> bufferDataSpan = bufferSpan[2..];
                bufferSpan[0] = 0x00;
                bufferSpan[1] = 0x08;

                int position = 0;

                // first send the 08 packets
                while (len > Constants.ChunkSize)
                {
                    data.Slice(position, Constants.ChunkSize).CopyTo(bufferDataSpan);
                    SendToSet(set, bufferSpan, flags | NetSendFlags.Reliable); // ASSS only sends the 09 reliably, but I think 08 needs to also be reliable too.  So I added Reliable here.
                    position += Constants.ChunkSize;
                    len -= Constants.ChunkSize;
                }

                // final packet is the 09 (signals the end of the big data)
                bufferSpan[1] = 0x09;
                data.Slice(position, len).CopyTo(bufferDataSpan);
                SendToSet(set, bufferSpan.Slice(0, len + 2), flags | NetSendFlags.Reliable);
            }
            else
            {
                foreach (Player p in set)
                {
                    if (p[_connKey] is not ConnData conn)
                        continue;

                    if (!IsOurs(p))
                        continue;

                    lock (conn.olmtx)
                    {
                        BufferPacket(conn, data, flags);
                    }
                }
            }
        }

        void INetwork.SendWithCallback(Player p, ReadOnlySpan<byte> data, ReliableDelegate callback)
        {
            ReliableCallbackInvoker invoker = _objectPoolManager.Get<ReliableCallbackInvoker>();
            invoker.SetCallback(callback);
            SendWithCallback(p, data, invoker);
        }

        void INetwork.SendWithCallback<TData>(Player p, ref TData data, ReliableDelegate callback)
        {
            ((INetwork)this).SendWithCallback(p, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref data, 1)), callback);
        }
        
        void INetwork.SendWithCallback<TState>(Player p, ReadOnlySpan<byte> data, ReliableDelegate<TState> callback, TState clos)
        {
            ReliableCallbackInvoker<TState> invoker = _objectPoolManager.Get<ReliableCallbackInvoker<TState>>();
            invoker.SetCallback(callback, clos);
            SendWithCallback(p, data, invoker);
        }

        void INetwork.SendWithCallback<TData, TState>(Player p, ref TData data, ReliableDelegate<TState> callback, TState clos)
        {
            ((INetwork)this).SendWithCallback(p, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref data, 1)), callback, clos);
        }

        private void SendWithCallback(Player p, ReadOnlySpan<byte> data, IReliableCallbackInvoker callbackInvoker)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(data), "Length must be >= 1.");

            if (callbackInvoker == null)
                throw new ArgumentNullException(nameof(callbackInvoker));

            if (p[_connKey] is not ConnData conn)
                return;

            // we can't handle big packets here
            Debug.Assert(data.Length <= (Constants.MaxPacket - Constants.ReliableHeaderLen));

            if (!IsOurs(p))
                return;

            lock (conn.olmtx)
            {
                BufferPacket(conn, data, NetSendFlags.Reliable, callbackInvoker);
            }
        }

        void INetwork.SendToTarget(ITarget target, ReadOnlySpan<byte> data, NetSendFlags flags)
        {
            if (data.Length < 1)
                return;

            HashSet<Player> set = _objectPoolManager.PlayerSetPool.Get();

            try
            {
                _playerData.TargetToSet(target, set);
                SendToSet(set, data, flags);
            }
            finally
            {
                _objectPoolManager.PlayerSetPool.Return(set);
            }
        }

        void INetwork.SendToTarget<TData>(ITarget target, ref TData data, NetSendFlags flags)
        {
            ((INetwork)this).SendToTarget(target, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref data, 1)), flags);
        }

        bool INetwork.SendSized<T>(Player p, int len, GetSizedSendDataDelegate<T> requestCallback, T clos)
        {
            if (p == null)
            {
                return false;
            }

            if (len <= 0)
            {
                return false;
            }

            if (!IsOurs(p))
            {
                _logManager.LogP(LogLevel.Drivel, nameof(Network), p, "Tried to send sized data to non-udp client.");
                return false;
            }

            if (p[_connKey] is not ConnData conn)
                return false;

            SizedSendData<T> sd = new(requestCallback, clos, len, 0);

            lock (conn.olmtx)
            {
                conn.sizedsends.AddLast(sd);
            }

            return true;
        }

        void INetwork.AddPacket(C2SPacketType pktype, PacketDelegate func)
        {
            AddPacket((int)pktype, func);
        }

        private void AddPacket(int pktype, PacketDelegate func)
        {
            if (func == null)
                return;

            if (pktype >= 0 && pktype < MAXTYPES)
            {
                PacketDelegate d = _handlers[pktype];
                _handlers[pktype] = (d == null) ? func : (d += func);
            }
            else if ((pktype & 0xFF) == 0)
            {
                int b2 = pktype >> 8;

                if (b2 >= 0 && b2 < _nethandlers.Length && _nethandlers[b2] == null)
                {
                    _nethandlers[b2] = func;
                }
            }
        }

        void INetwork.RemovePacket(C2SPacketType pktype, PacketDelegate func)
        {
            RemovePacket((int)pktype, func);
        }

        private void RemovePacket(int pktype, PacketDelegate func)
        {
            if (func == null)
                return;

            if (pktype >= 0 && pktype < MAXTYPES)
            {
                PacketDelegate d = _handlers[pktype];
                if (d != null)
                {
                    _handlers[pktype] = (d -= func);
                }
            }
            else if ((pktype & 0xFF) == 0)
            {
                int b2 = pktype >> 8;

                if (b2 >= 0 && b2 < _nethandlers.Length && _nethandlers[b2] == func)
                {
                    _nethandlers[b2] = null;
                }
            }
        }

        void INetwork.AddSizedPacket(C2SPacketType pktype, SizedPacketDelegate func)
        {
            AddSizedPacket((int)pktype, func);
        }

        private void AddSizedPacket(int pktype, SizedPacketDelegate func)
        {
            if (func == null)
                return;

            if (pktype >= 0 && pktype < MAXTYPES)
            {
                SizedPacketDelegate d = _sizedhandlers[pktype];
                _sizedhandlers[pktype] = (d == null) ? func : (d += func);
            }
        }

        void INetwork.RemoveSizedPacket(C2SPacketType pktype, SizedPacketDelegate func)
        {
            RemoveSizedPacket((int)pktype, func);
        }

        private void RemoveSizedPacket(int pktype, SizedPacketDelegate func)
        {
            if (func == null)
                return;

            if (pktype >= 0 && pktype < MAXTYPES)
            {
                SizedPacketDelegate d = _sizedhandlers[pktype];
                if (d != null)
                {
                    _sizedhandlers[pktype] = (d -= func);
                }
            }
        }

        IReadOnlyNetStats INetwork.GetStats()
        {
            ulong objectsCreated = Convert.ToUInt64(_bufferPool.ObjectsCreated);
            Interlocked.Exchange(ref _globalStats.buffercount, objectsCreated);
            Interlocked.Exchange(ref _globalStats.buffersused, objectsCreated - Convert.ToUInt64(_bufferPool.ObjectsAvailable));

            return _globalStats;
        }

        NetClientStats INetwork.GetClientStats(Player p)
        {
            if (p[_connKey] is not ConnData conn)
                return null;

            NetClientStats stats = new();
            stats.s2cn = conn.s2cn;
            stats.c2sn = conn.c2sn;
            stats.PacketsSent = conn.pktSent;
            stats.PacketsReceived = conn.pktReceived;
            stats.BytesSent = conn.bytesSent;
            stats.BytesReceived = conn.bytesReceived;
            stats.PacketsDropped = conn.pktdropped;

            if (conn.enc != null)
            {
                stats.EncryptionName = conn.iEncryptName;
            }

            stats.IPEndPoint = conn.RemoteEndpoint;

            //TODO: _bandwithLimit.

            return stats;
        }

        bool INetwork.TryGetListenData(int index, out IPEndPoint endPoint, out string connectAs)
        {
            if (index >= _listenDataList.Count)
            {
                endPoint = default;
                connectAs = default;
                return false;
            }

            ListenData ld = _listenDataList[index];
            endPoint = ld.GameSocket.LocalEndPoint as IPEndPoint;

            if (endPoint == null)
            {
                connectAs = default;
                return false;
            }

            connectAs = ld.ConnectAs;
            return true;
        }

        bool INetwork.TryGetPopulationStats(string connectAs, out uint total, out uint playing)
        {
            if (!_pingData.ConnectAsPopulationStats.TryGetValue(connectAs, out var stats))
            {
                total = default;
                playing = default;
                return false;
            }

            total = stats.Total;
            playing = stats.Playing;
            return true;
        }

        IReadOnlyList<ListenData> INetwork.Listening => _readOnlyListenData;

        #endregion

        #region INetworkClient Members

        
        ClientConnection INetworkClient.MakeClientConnection(string address, int port, IClientConnectionHandler handler, string iClientEncryptName)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Cannot be null or white-space.", nameof(address));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                throw new ArgumentOutOfRangeException(nameof(port));

            if (!IPAddress.TryParse(address, out IPAddress ipAddress))
                throw new ArgumentException("Unable to parse as an IP address.", nameof(address));

            IPEndPoint remoteEndpoint = new(ipAddress, port);

            IClientEncrypt encryptor = null;
            if (!string.IsNullOrWhiteSpace(iClientEncryptName))
            {
                encryptor = _broker.GetInterface<IClientEncrypt>(iClientEncryptName);
                if (encryptor == null)
                {
                    _logManager.LogM(LogLevel.Error, nameof(Network), $"Unable to find an {nameof(IClientEncrypt)} named {iClientEncryptName}.");
                    return null;
                }
            }

            NetClientConnection cc = new(remoteEndpoint, _clientSocket, handler, encryptor, iClientEncryptName, _bandwithLimit.New());

            encryptor?.Initialze(cc);

            bool added;

            _clientConnectionsLock.EnterWriteLock();

            try
            {
                added = _clientConnections.TryAdd(remoteEndpoint, cc);
            }
            finally
            {
                _clientConnectionsLock.ExitWriteLock();
            }

            if (!added)
            {
                _logManager.LogM(LogLevel.Error, nameof(Network), $"Attempt to make a client connection to {remoteEndpoint} when one already exists.");
                return null;
            }

            ConnectionInitPacket packet = new((int)(_prng.Get32() | 0x80000000), 1);
            SendRaw(cc.ConnData, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref packet, 1)));

            return cc;
        }

        void INetworkClient.SendPacket(ClientConnection cc, ReadOnlySpan<byte> data, NetSendFlags flags)
        {
            if (cc == null)
                return;

            if (cc is not NetClientConnection ncc)
                throw new ArgumentException("Unsupported client connection. It must be created by this module.", nameof(cc));

            if (data.Length < 1)
                return;

            lock (ncc.ConnData.olmtx)
            {
                if (data.Length > Constants.MaxPacket - Constants.ReliableHeaderLen)
                {
                    int len = data.Length;

                    // use 00 08 and 00 09 packets (big data packets)
                    // send these reliably (to maintain ordering with sequence #)
                    Span<byte> bufferSpan = stackalloc byte[Constants.ChunkSize + 2];
                    Span<byte> bufferDataSpan = bufferSpan[2..];
                    bufferSpan[0] = 0x00;
                    bufferSpan[1] = 0x08;

                    int position = 0;

                    // first send the 08 packets
                    while (len > Constants.ChunkSize)
                    {
                        data.Slice(position, Constants.ChunkSize).CopyTo(bufferDataSpan);
                        BufferPacket(ncc.ConnData, bufferSpan, flags | NetSendFlags.Reliable);
                        position += Constants.ChunkSize;
                        len -= Constants.ChunkSize;
                    }

                    // final packet is the 09 (signals the end of the big data)
                    bufferSpan[1] = 0x09;
                    data.Slice(position, len).CopyTo(bufferDataSpan);
                    BufferPacket(ncc.ConnData, bufferSpan.Slice(0, len + 2), flags | NetSendFlags.Reliable);
                }
                else
                {
                    BufferPacket(ncc.ConnData, data, flags);
                }
            }
        }

        void INetworkClient.SendPacket<T>(ClientConnection cc, ref T data, NetSendFlags flags)
        {
            if (cc is not NetClientConnection)
                throw new ArgumentException("Unsupported client connection. It must be created by this module.", nameof(cc));

            ((INetworkClient)this).SendPacket(cc, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref data, 1)), flags);
        }

        void INetworkClient.DropConnection(ClientConnection cc)
        {
            if (cc is not NetClientConnection ncc)
                throw new ArgumentException("Unsupported client connection. It must be created by this module.", nameof(cc));

            DropConnection(ncc);
        }

        private void DropConnection(NetClientConnection cc)
        {
            if (cc == null)
                return;

            ReadOnlySpan<byte> disconnectSpan = stackalloc byte[] { 0x00, 0x07 };
            SendRaw(cc.ConnData, disconnectSpan);

            if (cc.Encryptor != null)
            {
                cc.Encryptor.Void(cc);
                _broker.ReleaseInterface(ref cc.Encryptor, cc.EncryptorName);
            }

            _clientConnectionsLock.EnterWriteLock();

            try
            {
                _clientConnections.Remove(cc.ConnData.RemoteEndpoint);
            }
            finally
            {
                _clientConnectionsLock.ExitWriteLock();
            }
        }

        #endregion

#if CFG_DUMP_RAW_PACKETS
        private static void DumpPk(string description, ReadOnlySpan<byte> d)
        {
            StringBuilder sb = new(description.Length + 2 + (int)Math.Ceiling(d.Length / 16d) * 67);
            sb.AppendLine(description);

            int pos = 0;
            StringBuilder asciiBuilder = new(16);

            while (pos < d.Length)
            {
                int c;

                for (c = 0; c < 16 && pos < d.Length; c++, pos++)
                {
                    if (c > 0)
                        sb.Append(' ');

                    asciiBuilder.Append(!char.IsControl((char)d[pos]) ? (char)d[pos] : '.');
                    sb.Append(d[pos].ToString("X2"));
                }

                for (; c < 16; c++)
                {
                    sb.Append("   ");
                }

                sb.Append("  ");
                sb.AppendLine(asciiBuilder.ToString());
                asciiBuilder.Length = 0;
            }

            Debug.Write(sb.ToString());
        }
#endif

        public void Dispose()
        {
            _stopCancellationTokenSource.Dispose();
            _clientConnectionsLock.Dispose();
        }
    }
}
