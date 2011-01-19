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
		/// <summary>
		/// A point that contains the two edges of the tile.
		/// </summary>
		public Point ShoreEdgePoint { get; set; }

		/// <summary>
		/// Represents the Starting Point of the Tile on the Maptiles.Png
		/// </summary>
		public Point TileStartPoint { get; set; }
	}
}
