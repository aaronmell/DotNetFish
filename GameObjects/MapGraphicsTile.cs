using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using EdgeType = DotNetFish.GameObjects.Enums.EdgeType;

namespace DotNetFish.GameObjects
{
	[Serializable()]
	public class MapGraphicsTile : IEquatable<MapGraphicsTile>
	{
		public MapGraphicsTile()
		{
			ShoreEdgePoints = new List<EdgeConnection>();
			TopEdgeType = EdgeType.Undefined;
			BottomEdgeType = EdgeType.Undefined;
			LeftEdgeType = EdgeType.Undefined;
			RightEdgeType = EdgeType.Undefined;
		}
		
		/// <summary>
		/// A list of integers that makes up the shore edge points
		/// </summary>
		public List<EdgeConnection> ShoreEdgePoints { get; set; }
		public DotNetFish.GameObjects.Enums.TileType TileType { get; set; }
		/// <summary>
		/// Represents the Starting Point of the Tile on the Maptiles.Png
		/// </summary>
		public Point TileStartPoint { get; set; }
		public EdgeType TopEdgeType { get; set; }
		public EdgeType BottomEdgeType { get; set; }
		public EdgeType LeftEdgeType { get; set; }
		public EdgeType RightEdgeType { get; set; }

		public bool Equals(MapGraphicsTile tile)
		{
			if (tile.RightEdgeType != EdgeType.Undefined && tile.RightEdgeType != this.RightEdgeType)
				return false;

			if (tile.LeftEdgeType != EdgeType.Undefined && tile.LeftEdgeType != this.LeftEdgeType)
				return false;

			if (tile.TopEdgeType != EdgeType.Undefined && tile.TopEdgeType != this.TopEdgeType)
				return false;

			if (tile.BottomEdgeType != EdgeType.Undefined && tile.BottomEdgeType != this.BottomEdgeType)
				return false;

			foreach (EdgeConnection edgeConnection in tile.ShoreEdgePoints)
				if (!this.ShoreEdgePoints.Exists(i => i.EdgePosition == edgeConnection.EdgePosition))
					return false;

			return true;
		}
	}
}
