﻿using System.Collections.Generic;

namespace SS.Core.ComponentInterfaces
{
    public struct PingSummary
    {
        public int Current, Average, Min, Max;
    }

    public struct ClientPingSummary
    {
        public int Current, Average, Min, Max;
        public uint S2CSlowTotal, S2CFastTotal;
        public ushort S2CSlowCurrent, S2CFastCurrent;
    }

    public struct PacketlossSummary
    {
        public double s2c, c2s, s2cwpn;
    }

    /// <summary>
    /// Interface for querying player lag data.
    /// </summary>
    public interface ILagQuery : IComponentInterface
    {
        /// <summary>
        /// Gets a player's ping info (from position packets).
        /// </summary>
        /// <param name="player">The player to get data about.</param>
        /// <param name="ping">The data.</param>
        void QueryPositionPing(Player player, out PingSummary ping);

        /// <summary>
        /// Get a player's ping info (reported by the client).
        /// </summary>
        /// <param name="player">The player to get data about.</param>
        /// <param name="ping">The data.</param>
        void QueryClientPing(Player player, out ClientPingSummary ping);

        /// <summary>
        /// Gets a player's ping info (from reliable packets).
        /// </summary>
        /// <param name="player">The player to get data about.</param>
        /// <param name="ping">The data.</param>
        void QueryReliablePing(Player player, out PingSummary ping);

        /// <summary>
        /// Gets a player's packetloss info.
        /// </summary>
        /// <param name="player">The player to get data about.</param>
        /// <param name="packetloss">The data</param>
        void QueryPacketloss(Player player, out PacketlossSummary packetloss);

        /// <summary>
        /// Gets a player's reliable lag info.
        /// </summary>
        /// <param name="player">The player to get data about.</param>
        /// <param name="reliableLag">The data.</param>
        void QueryReliableLag(Player player, out ReliableLagData reliableLag);

        /// <summary>
        /// Gets a player's history of time sync requests (0x00 0x05 core packet).
        /// </summary>
        /// <param name="player">The player to get data about.</param>
        /// <param name="history">A collection to be filled with a copy of the data.</param>
        void QueryTimeSyncHistory(Player player, in ICollection<(uint ServerTime, uint ClientTime)> history);

        /// <summary>
        /// Gets a player's drift in time sync request.
        /// </summary>
        /// <param name="player">The player to get data about.</param>
        int QueryTimeSyncDrift(Player player);

        // DoPHistogram

        // DoRHistogram
    }
}
