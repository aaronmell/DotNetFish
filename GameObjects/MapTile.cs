﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;


namespace GameObjects
{
	/// <summary>
	/// Holds all of the information needed on one tile of the map on the game world
	/// </summary>
    public class MapTile
    {
		private MapGraphicsTile _graphicsTile;

		public MapGraphicsTile GraphicsTile
		{
			get
			{
				return _graphicsTile;
			}
		}

		public MapTile(MapGraphicsTile tile)
		{
			_graphicsTile = tile;
		}
    }
}