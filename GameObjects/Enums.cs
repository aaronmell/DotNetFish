using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameObjects
{
	/// <summary>
	/// Contains all of the enums used in the game. 
	/// </summary>
    public class Enums
    {
		public enum TileEdgeDirection
		{
			Up = 1,
			Right = 2,
			Down = 3,
			Left = 4
		}

		public enum EdgeType
		{
			Both = 1,
			Land = 2,
			Water = 3	
		}
	}

	
}
