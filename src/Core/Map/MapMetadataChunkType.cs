﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS.Utilities;

namespace SS.Core.Map
{
    public static class MapMetadataChunkType
    {
        /// <summary>
        /// these define miscelaneous textual attributes. the format is ascii text,
        /// not null terminated, in this form: "&lt;key&gt;=&lt;value&gt;". each "ATTR" chunk
        /// should contain just one key/value pair. multiple chunks of this type may
        /// be present in one file.
        /// 
        /// some keys that might be typically found in a level file are:
        /// 
        /// "NAME" - a descriptive name for this map
        /// "VERSION" - a version number for this map
        /// "ZONE" - the zone this file is intended to be used with
        /// "MAPCREATOR" - the person who created this map (the format of the value
        /// should be "name &lt;email&gt;")
        /// "TILESETCREATOR" - the person who created the tileset
        /// "PROGRAM" - the name of the program that was used to create this level
        /// file
        /// </summary>
        internal static uint ATTR = LittleEndianBitConverter.ToUInt32(Encoding.ASCII.GetBytes("ATTR"), 0);

        /// <summary>
        /// these chunks define regions. to recap, a region is a set of tiles,
        /// usually but not always contiguous, with certain properties. asss
        /// understands regions and can implement some advanced features using them.
        /// currently continuum doesn't understand regions, but it would be nice if
        /// it did, and we'll be able to do even cooler things when it does.
        /// 
        /// there's a lot of stuff that you might want to say about a region, so to
        /// support all the varied uses, and also future uses, we'll use the chunk
        /// model again: each region gets its own set of "subchunks" describing its
        /// function. to avoid confusion, all sub-chunk types that go inside the
        /// "REGN" superchunk start with "r". the data of the big "REGN" chunk is
        /// simply a series of subchunks.
        /// 
        /// here are some defined sub-chunk types:
        /// 
        /// "rNAM" - a descriptive name for the region
        /// "rTIL" - tile data, the definition of the region
        /// "rBSE" - whether the region represents a base in a flag game
        /// "rNAW" - no antiwarp
        /// "rNWP" - no weapons
        /// "rNFL" - no flag drops
        /// "rAWP" - auto-warp
        /// "rPYC" - code to be executed when a player enters or leaves this region
        /// </summary>
        internal static uint REGN = LittleEndianBitConverter.ToUInt32(Encoding.ASCII.GetBytes("REGN"), 0);

        /// <summary>
        /// the format of a "TSET" chunk is a windows format bitmap, _without_ the
        /// bitmapfileheader stuff (because its function is taken care of by the
        /// chunk header). that is, it starts with a bitmapinfoheader, which is
        /// followed directly by the color table, which is followed directly by the
        /// bitmap data. the bitmap data should be in 8-bit format with no
        /// compression.
        /// <remarks>for future use when ELVL format is standard and backwards compatability is no longer required</remarks>
        /// </summary>
        internal static uint TSET = LittleEndianBitConverter.ToUInt32(Encoding.ASCII.GetBytes("TSET"), 0);

        /// <summary>
        /// the format of a "TILE" chunk is just tile data, in the same format it's
        /// always been in
        /// <remarks>for future use when ELVL format is standard and backwards compatability is no longer required</remarks>
        /// </summary>
        internal static uint TILE = LittleEndianBitConverter.ToUInt32(Encoding.ASCII.GetBytes("TILE"), 0);

        /// <summary>
        /// other chunk types used specially for regions
        /// </summary>
        internal static class RegionChunkType
        {
            /// <summary>
            /// name of the region
            /// this is just a plain ascii string, not null terminated. every chunk
            /// should have exactly one of these.
            /// </summary>
            internal static uint rNAM = LittleEndianBitConverter.ToUInt32(Encoding.ASCII.GetBytes("rNAM"), 0);

            /// <summary>
            /// this subchunk describes the tiles that make up the region. it's stored
            /// in a compact rle-ish representation.
            /// </summary>
            internal static uint rTIL = LittleEndianBitConverter.ToUInt32(Encoding.ASCII.GetBytes("rTIL"), 0);
        }
    }
}
