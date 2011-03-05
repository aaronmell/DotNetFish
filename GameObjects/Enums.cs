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
		public enum EdgeType
		{
			Both = 1,
			Land = 2,
			Water = 3,
			Undefined = 4
		}

		public enum TileType
		{
			Edge = 1,
			Special = 2,
			Water = 3,
			Land = 4,
			Error = 5,
			Blank = 6
		}
	}

	
}
