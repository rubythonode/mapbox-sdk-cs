﻿//https://github.com/BruTile/BruTile
// Copyright (c) BruTile developers team. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Mapbox.Map {

	public class MemoryCache<T> : IMemoryCache<T>, INotifyPropertyChanged, IDisposable {
		private readonly Dictionary<CanonicalTileId, T> _bitmaps = new Dictionary<CanonicalTileId, T>();
		private readonly Dictionary<CanonicalTileId, DateTime> _touched = new Dictionary<CanonicalTileId, DateTime>();
		private readonly object _syncRoot = new object();
		private bool _disposed;
		private readonly Func<CanonicalTileId, bool> _keepTileInMemory;

		public int TileCount { get { return _bitmaps.Count; } }

		public int MinTiles { get; set; }
		public int MaxTiles { get; set; }


		public MemoryCache(int minTiles = 50, int maxTiles = 100, Func<CanonicalTileId, bool> keepTileInMemory = null) {
			if(minTiles >= maxTiles)
				throw new ArgumentException("minTiles should be smaller than maxTiles");
			if(minTiles < 0)
				throw new ArgumentException("minTiles should be larger than zero");
			if(maxTiles < 0)
				throw new ArgumentException("maxTiles should be larger than zero");

			MinTiles = minTiles;
			MaxTiles = maxTiles;
			_keepTileInMemory = keepTileInMemory;
		}


		public void Add(CanonicalTileId index, T item) {
			lock(_syncRoot) {
				if(_bitmaps.ContainsKey(index)) {
					_bitmaps[index] = item;
					_touched[index] = DateTime.Now;
				} else {
					_touched.Add(index, DateTime.Now);
					_bitmaps.Add(index, item);
					CleanUp();
					OnNotifyPropertyChange("TileCount");
				}
			}
		}


		public void Remove(CanonicalTileId index) {
			lock(_syncRoot) {

				if(!_bitmaps.ContainsKey(index)) {
					return;
				}

				var disposable = _bitmaps[index] as IDisposable;
				if(null != disposable) {
					disposable.Dispose();
					disposable = null;
				}

				T bm = _bitmaps[index];
				if(null != bm) { bm = default(T); }

				_touched.Remove(index);
				_bitmaps.Remove(index);

				OnNotifyPropertyChange("TileCount");
			}
		}


		public T Get(CanonicalTileId index) {
			lock(_syncRoot) {
				if(!_bitmaps.ContainsKey(index))
					return default(T);

				_touched[index] = DateTime.Now;
				return _bitmaps[index];
			}
		}


		public void Clear() {
			lock(_syncRoot) {
				DisposeTilesIfDisposable();
				_touched.Clear();
				_bitmaps.Clear();
				OnNotifyPropertyChange("TileCount");
			}
		}


		void CleanUp() {
			if(_bitmaps.Count <= MaxTiles)
				return;

			var numberOfTilesToKeepInMemory = 0;
			if(_keepTileInMemory != null) {
				var tilesToKeep = _touched.Keys.Where(_keepTileInMemory).ToList();
				foreach(var index in tilesToKeep)
					_touched[index] = DateTime.Now; // touch tiles to keep
				numberOfTilesToKeepInMemory = tilesToKeep.Count;
			}
			var numberOfTilesToRemove = _bitmaps.Count - Math.Max(MinTiles, numberOfTilesToKeepInMemory);

			var oldItems = _touched.OrderBy(p => p.Value).Take(numberOfTilesToRemove);

			foreach(var oldItem in oldItems) {
				Remove(oldItem.Key);
			}
		}


		protected virtual void OnNotifyPropertyChange(string propertyName) {
			var handler = PropertyChanged;
			if(null != handler) {
				handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}


		public event PropertyChangedEventHandler PropertyChanged;


		public void Dispose() {
			if(_disposed)
				return;
			DisposeTilesIfDisposable();
			_touched.Clear();
			_bitmaps.Clear();
			_disposed = true;
		}


		private void DisposeTilesIfDisposable() {
			foreach(var index in _bitmaps.Keys) {
				var bitmap = _bitmaps[index] as IDisposable;
				if(null != bitmap) {
					bitmap.Dispose();
				}
			}
		}



	}
}
