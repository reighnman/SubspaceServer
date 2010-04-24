﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS.Core.ComponentInterfaces
{
    [Flags]
    public enum NetSendFlags
    {
        /// <summary>
        /// Same as Unreliable
        /// </summary>
        None = 0x00,
        Unreliable = 0x00,
        Reliable = 0x01,
        Dropabble = 0x02,
        Urgent = 0x04,

        PriorityN1 = 0x10,
        PriorityDefault = 0x20,
        PriorityP1 = 0x30,
        PriorityP2 = 0x40,
        PriorityP3 = 0x50,
        PriorityP4 = 0x64, // includes urgent flag
        PriorityP5 = 0x74, // includes urgent flag

        /// <summary>
        /// this if for use in the Network module only, do not use it directly
        /// </summary>
        Ack = 0x0100,
    }

    public delegate void PacketDelegate(Player p, byte[] data, int length);
    public delegate void SizedPacketDelegate(Player p, ArraySegment<byte>? data, int offset, int totallen);
    public delegate void ReliableDelegate(Player p, bool success, object clos);
    //public delegate void ReliableDelegate<T>(Player p, bool success, T clos);
    public delegate void GetSizedSendDataDelegate<T>(T clos, int offset, byte[] buf, int bufStartIndex, int bytesNeeded);

    public interface INetwork : IComponentInterface
    {
        /// <summary>
        /// To send data to a single player
        /// </summary>
        /// <param name="p">player to send to</param>
        /// <param name="data">data to send</param>
        /// <param name="len">length of data to send</param>
        /// <param name="flags">flags specifying options for the send</param>
        void SendToOne(Player p, byte[] data, int len, NetSendFlags flags);

        /// <summary>
        /// To send data to players in a specific arena or
        /// To send data to players in all arenas.
        /// A specified person can be excluded from the send.
        /// </summary>
        /// <param name="arena">arena to send data to, null for all arenas</param>
        /// <param name="except">player to exclude from the send</param>
        /// <param name="data">data to send</param>
        /// <param name="len">length of data to send</param>
        /// <param name="flags">flags specifying options for the send</param>
        void SendToArena(Arena arena, Player except, byte[] data, int len, NetSendFlags flags);

        /// <summary>
        /// To send data to a set of players.
        /// </summary>
        /// <param name="set">players to send to</param>
        /// <param name="data">data to send</param>
        /// <param name="len">length of data to send</param>
        /// <param name="flags">flags specifying options for the send</param>
        void SendToSet(IEnumerable<Player> set, byte[] data, int len, NetSendFlags flags);

        /// <summary>
        /// To send data to a target of players
        /// </summary>
        /// <param name="target">target describing what players to send data to</param>
        /// <param name="data">array containing the data</param>
        /// <param name="len">the length of the data</param>
        /// <param name="flags">flags specifying options for the send</param>
        void SendToTarget(ITarget target, byte[] data, int len, NetSendFlags flags);

        /// <summary>
        /// To send data to a player and recieve a callback after the data has been sent.
        /// </summary>
        /// <param name="p">player sending data to</param>
        /// <param name="data">array conaining the data</param>
        /// <param name="len">number of bytes to send</param>
        /// <param name="callback">the callback which will be called after the data has been sent</param>
        /// <param name="clos">argument to use when calling the callback</param>
        void SendWithCallback(Player p, byte[] data, int len, ReliableDelegate callback, object clos);

        /// <summary>
        /// To send sized data to a player.
        /// <remarks>used for sending files to players such as map/news/updates</remarks>
        /// </summary>
        /// <typeparam name="T">type of the argument used in the callback to retrieve data to send</typeparam>
        /// <param name="p">player sending data to</param>
        /// <param name="clos">argument to use when calling the callback</param>
        /// <param name="len">total number of bytes to send in the transfer</param>
        /// <param name="requestCallback">callback that is used to retrieve data for each piece of the transfer</param>
        /// <returns></returns>
        bool SendSized<T>(Player p, T clos, int len, GetSizedSendDataDelegate<T> requestCallback);

        /// <summary>
        /// To add a handler for a packet of a certain type.
        /// <remarks>
        /// This is usually used to register handlers for game packets.
        /// Note, this can also be used to register a handler for 'core' network level packets.  
        /// However, registering 'core' handlers doesn't appear to be used in asss.</remarks>
        /// </summary>
        /// <param name="pktype">type of packet</param>
        /// <param name="func">the handler to call when a packet is recieved</param>
        void AddPacket(int pktype, PacketDelegate func);

        /// <summary>
        /// To unregister a handler for a given packet type.
        /// </summary>
        /// <param name="pktype"></param>
        /// <param name="func"></param>
        void RemovePacket(int pktype, PacketDelegate func);

        /// <summary>
        /// To add a handler for a sized packet recieved from a client.
        /// <remarks>This is used for recieving file uploads.  Includes voices (wave messages in macros).</remarks>
        /// </summary>
        /// <param name="pktype"></param>
        /// <param name="func"></param>
        void AddSizedPacket(int pktype, SizedPacketDelegate func);

        /// <summary>
        /// To unregister a handler for a sized packet.
        /// </summary>
        /// <param name="pktype"></param>
        /// <param name="func"></param>
        void RemoveSizedPacket(int pktype, SizedPacketDelegate func);
    }
}
