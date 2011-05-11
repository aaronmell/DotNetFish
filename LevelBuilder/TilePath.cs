using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace DotNetFish.LevelBuilder
{
	public class TilePath : IComparable<TilePath>
	{
		public Point TileLocation { get; set; }
		public int Score { get; set; }
		public TilePath ParentTile { get; set; }
	
		public TilePath(Point location, int score, TilePath parent)
		{
			TileLocation = location;
			Score = score;
			ParentTile = parent;
		}

		public TilePath(Point location, int score)
		{
			TileLocation = location;
			Score = score;
			ParentTile = null;
		}

		public int CompareTo(TilePath obj)
		{
			return Score.CompareTo(obj.Score);
		}
	}

}
