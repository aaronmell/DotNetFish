using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace GameObjects
{
	[Serializable()]
	public class MapGraphicsTile
	{
		private MapTileSide[] _mapTileSide;
		/// <summary>
		/// A point that contains the two edges of the tile.
		/// </summary>
		public Point ShoreEdgePoint { get; set; }

		/// <summary>
		/// Represents the Starting Point of the Tile on the Maptiles.Png
		/// </summary>
		public Point TileStartPoint { get; set; }

		public MapTileSide[] TileSides
		{
			get
			{
				if (_mapTileSide == null)
					_mapTileSide = new MapTileSide[4];

				return _mapTileSide;
			}
			set
			{
				_mapTileSide = value;
			}
		}
	}
}
