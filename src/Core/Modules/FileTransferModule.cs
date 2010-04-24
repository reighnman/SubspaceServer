﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS.Core.ComponentInterfaces;
using System.IO;
using SS.Core.Packets;
using SS.Core.ComponentCallbacks;

namespace SS.Core.Modules
{
    public class FileTransferModule : IModule, IFileTransfer
    {
        private ModuleManager _mm;
        private INetwork _network;
        private ILogManager _logManager;
        private ICapabilityManager _capabilityManager;
        private IPlayerData _playerData;

        private class DownloadDataContext
        {
            public readonly FileStream Stream;
            public readonly string Filename;
            public readonly string DeletePath;

            public DownloadDataContext(FileStream stream, string filename, string deletePath)
            {
                if (stream == null)
                    throw new ArgumentNullException("stream");

                if (filename == null) // can be empty
                    throw new ArgumentNullException("filename");

                Stream = stream;
                Filename = filename;
                DeletePath = deletePath;
            }
        }

        private int _udKey;

        private class UploadDataContext
        {

        }

        #region IModule Members

        Type[] IModule.InterfaceDependencies
        {
            get
            {
                return new Type[] {
                    typeof(INetwork), 
                    typeof(ILogManager), 
                    typeof(ICapabilityManager), 
                    typeof(IPlayerData), 
                };
            }
        }

        bool IModule.Load(ModuleManager mm, Dictionary<Type, IComponentInterface> interfaceDependencies)
        {
            _mm = mm;
            _network = interfaceDependencies[typeof(INetwork)] as INetwork;
            _logManager = interfaceDependencies[typeof(ILogManager)] as ILogManager;
            _capabilityManager = interfaceDependencies[typeof(ICapabilityManager)] as ICapabilityManager;
            _playerData = interfaceDependencies[typeof(IPlayerData)] as IPlayerData;

            _udKey = _playerData.AllocatePlayerData<UploadDataContext>();
            PlayerActionCallback.Register(_mm, playerAction);
            _mm.RegisterInterface<IFileTransfer>(this);
            return true;
        }

        bool IModule.Unload(ModuleManager mm)
        {
            _mm.UnregisterInterface<IFileTransfer>();
            PlayerActionCallback.Unregister(_mm, playerAction);
            _playerData.FreePlayerData(_udKey);

            return true;
        }

        #endregion

        #region IFileTransfer Members

        bool IFileTransfer.SendFile(Player p, string path, string filename, bool deleteAfter)
        {
            if (p == null)
                return false;

            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                FileInfo fileInfo = new FileInfo(path);
                if (!fileInfo.Exists)
                {
                    _logManager.Log(LogLevel.Warn, "<FileTransferModule> file '{0}' does not exist", path);
                    return false;
                }

                FileStream fileStream = fileInfo.OpenRead();
                DownloadDataContext dd = new DownloadDataContext(fileStream, filename, deleteAfter ? path : null);
                _network.SendSized<DownloadDataContext>(p, dd, (int)fileInfo.Length + 17, getSizedSendData);
                return true;
            }
            catch(Exception ex)
            {
                _logManager.Log(LogLevel.Warn, "<FileTransferModule> error opening file '{0}' - {1}", path, ex.Message);
                return false;
            }
        }

        #endregion

        private void playerAction(Player p, PlayerAction action, Arena arena)
        {
            if (p == null)
                return;

            UploadDataContext ud = p[_udKey] as UploadDataContext;
            if (ud == null)
                return;

            if (action == PlayerAction.Connect)
            {
                
            }
            else if(action == PlayerAction.Disconnect)
            {
                
            }
        }

        private void getSizedSendData(DownloadDataContext dd, int offset, byte[] buf, int bufStartIndex, int bytesNeeded)
        {
            if (dd == null)
                return;

            if (bytesNeeded == 0)
            {
                _logManager.Log(LogLevel.Info, "<FileTransferModule> completed send of {0}", dd.Filename);
                dd.Stream.Dispose();
                if (!string.IsNullOrEmpty(dd.DeletePath))
                {
                    try
                    {
                        File.Delete(dd.DeletePath);
                        _logManager.Log(LogLevel.Info, "<FileTransferModule> deleted {0} after completed send", dd.Filename);
                    }
                    catch(Exception ex)
                    {
                        _logManager.Log(LogLevel.Warn, "<FileTransferModule> failed to delete {0} after completed send.  {1}", dd.Filename, ex.Message);
                    }
                }
            }
            else if (offset == 0 && bytesNeeded >= 17)
            {
                buf[bufStartIndex++] = (byte)S2CPacketType.IncomingFile;
                Encoding.ASCII.GetBytes(dd.Filename, 0, 16, buf, bufStartIndex);
                bufStartIndex += 16;
                dd.Stream.Read(buf, bufStartIndex, bytesNeeded - 17);
            }
            else if(offset > 0)
            {
                dd.Stream.Read(buf, bufStartIndex, bytesNeeded);
            }
        }
    }
}
