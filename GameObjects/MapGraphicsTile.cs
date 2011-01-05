using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GameObjects
{

	public class MapGraphicsTile
	{
		/// <summary>
		/// A list containing two points that represent the points on the tile where the shore's edge is at
		/// </summary>
		public List<int> ShoreEdgePoints { get; set; }

		/// <summary>
		/// Represents the Starting Point of the Tile on the Maptiles.Png
		/// </summary>
		public Point TileStartPoint { get; set; }
	}
}
