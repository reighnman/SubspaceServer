﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS.Core.Map;

namespace SS.Core.ComponentInterfaces
{
    public interface IMapData : IComponentInterface
    {
        /// <summary>
        /// finds the file currently used as this arena's map.
        /// you should use this function and not try to figure out the map
        /// filename yourself based on arena settings.
        /// </summary>
        /// <param name="arena">the arena whose map we want</param>
        /// <param name="filename">the resulting filename</param>
        /// <param name="mapname">null if you're looking for an lvl, or the name of an lvz file.</param>
        /// <returns>true if it could find a lvl or lvz file, buf will contain the result. false if it failed.</returns>
        string GetMapFilename(Arena arena, string mapname);

        /// <summary>
        /// gets the named attribute for the arena's map.
        /// </summary>
        /// <param name="arena">the arena whose map we care about.</param>
        /// <param name="key">the attribute key to retrieve.</param>
        /// <returns>the key's value, or NULL if not present</returns>
        string GetAttribute(Arena arena, string key);

        /// <summary>
        /// To get the number of turf (static) flags on the map in an arena
        /// </summary>
        /// <param name="arena">the arena whose map we care about</param>
        /// <returns>the # of turf flags</returns>
        int GetFlagCount(Arena arena);

        /// <summary>
        /// To get the contents of a single tile of the map.
        /// </summary>
        /// <param name="arena">the arena whose map we care about</param>
        /// <param name="coord">coordinates looking at</param>
        /// <returns>the tile, null for no tile</returns>
        MapTile? GetTile(Arena arena, MapCoordinate coord);

        /// <summary>
        /// Get the map checksum
        /// <remarks>Used by Recording module to make sure the recording plays on the same map that is was recorded on.</remarks>
        /// </summary>
        /// <param name="arena"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        uint GetChecksum(Arena arena, uint key);
    }
}
