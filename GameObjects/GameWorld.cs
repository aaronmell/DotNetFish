using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameObjects
{
	/// <summary>
	/// Holds all of the inforamation about the Gameworld itself. 
	/// </summary>
    public class GameWorld
    {
        public MapTile[,] GameMap { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public string Description { get; set; }


    }
}
