﻿//https://github.com/BruTile/BruTile
// Copyright (c) BruTile developers team. All rights reserved. See License.txt in the project root for license information.

namespace Mapbox.Map
{
    interface IMemoryCache<T> : ITileCache<T>
    {
        int MinTiles { get; set; }
        int MaxTiles { get; set; }
    }
}
