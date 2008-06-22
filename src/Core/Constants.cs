using System;
using System.Collections.Generic;
using System.Text;

namespace SS.Core
{
    /// <summary>
    /// equivalent of param.h
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// the search path for config files
        /// </summary>
        public const string CFG_CONFIG_SEARCH_PATH = "arenas/%b/%n:conf/%n:%n:arenas/(default)/%n";

        /// <summary>
        /// how many incoming rel packets to buffer for a client
        /// </summary>
        public const int CFG_INCOMING_BUFFER = 32;

        public const int MaxPacket = 512;

        /// <summary>
        /// maximum size of a "big packet" allowed to recieve
        /// </summary>
        public const int CFG_MAX_BIG_PACKET = 65536;
        public const int MaxBigPacket = CFG_MAX_BIG_PACKET;

        /// <summary>
        /// how many bytes to 'chunk' data into when sending "big packets"
        /// (this includes sized send data (eg, map/news/lvz downloads)
        /// </summary>
        public const int ChunkSize = 480;

        public const int ReliableHeaderLen = 6;

        /// <summary>
        /// callbacks / events
        /// </summary>
        public static class Events
        {
            public const string ConnectionInit = "conninit";
            public const string PlayerAction = "playeraction";
            public const string ArenaAction = "ArenaAction";
            public const string ChatMessage = "chatmessage";
        }
    }
}
