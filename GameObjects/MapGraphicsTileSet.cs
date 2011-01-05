using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace GameObjects
{
	/// <summary>
	/// Holds all of the methods and properties for returning a set of tiles from a sprite
	/// </summary>
	public class MapGraphicsTileSet
	{
		private Dictionary<Point, MapGraphicsTile> _mapTiles;
		private string _filename;
		private int _tileWidth;
		private int _tileHeight;
		private MapGraphicsTile _waterTile;
        private MapGraphicsTile _landTile;

		public Dictionary<Point, MapGraphicsTile> MapTiles
		{
			get
			{
				return _mapTiles;
			}
		}

		public MapGraphicsTile WaterTile
		{
			get
			{
				return _waterTile;
			}
		}

		public MapGraphicsTile LandTile
		{
			get
			{
				return _landTile;
			}
		}

		public MapGraphicsTileSet(string filename, int tileWidth, int tileHeight)
		{
			_filename = filename;
			_tileWidth = tileWidth;
			_tileHeight = tileHeight;
			_mapTiles = new Dictionary<Point, MapGraphicsTile>();
			LoadMapTileSet();
		}

		/// <summary>
		/// Loads the Map Tiles from a csv file into a dictionary for retrieval. 
		/// </summary>		
		private void LoadMapTileSet()
		{
			using (StreamReader readFile = new StreamReader(_filename))
			{
				string line;
				string[] row;
				int rowCount = 0;

				while ((line = readFile.ReadLine()) != null)
				{
					row = line.Split(',');
					int columnCount = 0;

					for (int c = 0; c < row.Count(); c += 2)
					{
						if (row.Count() > 0 && row[c] != "" && row[c + 1] != "")
						{
							Point startPoint = new Point(columnCount * _tileWidth, rowCount * _tileHeight);
							int edge1 = int.Parse(row[c]);
							int edge2 = int.Parse(row[c + 1]);
							MapGraphicsTile tile = new MapGraphicsTile { TileStartPoint = startPoint, ShoreEdgePoints = new List<int> { edge1, edge2 } };

							if (edge1 == 0 && edge2 == 0)
							{
								_landTile = tile;
							}
							else if (edge1 == 13 && edge2 == 13)
							{
								_waterTile = tile;
							}
							else
							{
								_mapTiles.Add(startPoint, tile);
							}
							columnCount++;
						}
					}
					rowCount++;
				}
			}			
		}

		public MapTile GetMatchingTile(List<byte> tileEdgePoints)
		{
			return new MapTile(_landTile);
		}
	}
}
