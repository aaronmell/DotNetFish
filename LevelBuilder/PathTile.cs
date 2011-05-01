using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace DotNetFish.LevelBuilder
{
	public class PathTile : IComparable<PathTile>
	{
		public Point TileLocation { get; set; }
		public int Score { get; set; }
		public PathTile ParentTile { get; set; }
	
		public PathTile(Point location, int score, PathTile parent)
		{
			TileLocation = location;
			Score = score;
			ParentTile = parent;
		}

		public PathTile(Point location, int score)
		{
			TileLocation = location;
			Score = score;
			ParentTile = null;
		}

		public int CompareTo(PathTile obj)
		{
			return Score.CompareTo(obj.Score);
		}
	}

}
