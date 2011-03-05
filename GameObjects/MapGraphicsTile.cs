using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using EdgeType = GameObjects.Enums.EdgeType;

namespace GameObjects
{
	[Serializable()]
	public class MapGraphicsTile
	{		
		/// <summary>
		/// A list of integers that makes up the shore edge points
		/// </summary>
		public List<byte> ShoreEdgePoints { get; set; }
		public GameObjects.Enums.TileType TileType { get; set; }
		/// <summary>
		/// Represents the Starting Point of the Tile on the Maptiles.Png
		/// </summary>
		public Point TileStartPoint { get; set; }

		public EdgeType TopEdgeType { get; set; }
		public EdgeType BottomEdgeType { get; set; }
		public EdgeType LeftEdgeType { get; set; }
		public EdgeType RightEdgeType { get; set; }
	}
}
