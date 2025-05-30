﻿using SS.Core.ComponentCallbacks;
using SS.Core.ComponentInterfaces;
using SS.Core.Map;
using System;

namespace SS.Core.Modules
{
    /// <summary>
    /// Module for relocating, 'warping', players that move onto specially designated map regions defined in extended lvl files.
    /// Players can be warped to a new (x,y) coordinate on the map, and even be sent to another arena.
    /// </summary>
    [CoreModuleInfo]
    public sealed class AutoWarp(
        IArenaManager arenaManager,
        IGame game,
        IPrng prng) : IModule
    {
        private readonly IArenaManager _arenaManager = arenaManager ?? throw new ArgumentNullException(nameof(arenaManager));
        private readonly IGame _game = game ?? throw new ArgumentNullException(nameof(game));
        private readonly IPrng _prng = prng ?? throw new ArgumentNullException(nameof(prng));

        #region IModule Members

        bool IModule.Load(IComponentBroker broker)
        {
            MapRegionCallback.Register(broker, Callback_MapRegion);
            return true;
        }

        bool IModule.Unload(IComponentBroker broker)
        {
            MapRegionCallback.Unregister(broker, Callback_MapRegion);
            return true;
        }

        #endregion

        private void Callback_MapRegion(Player player, MapRegion region, short x, short y, bool entering)
        {
            if (player is null
                || region is null
                || !entering
                || region.AutoWarpDestinations.Count <= 0)
            {
                return;
            }

            var destination = region.AutoWarpDestinations.Count == 1
                ? region.AutoWarpDestinations[0]
                : region.AutoWarpDestinations[_prng.Number(0, region.AutoWarpDestinations.Count - 1)];

            if (string.IsNullOrWhiteSpace(destination.ArenaName))
            {
                _game.WarpTo(player, destination.X, destination.Y);
            }
            else
            {
                _arenaManager.SendToArena(player, destination.ArenaName, destination.X, destination.Y);
            }
        }
    }
}
