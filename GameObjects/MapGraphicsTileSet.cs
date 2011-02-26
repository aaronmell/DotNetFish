using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows;

namespace GameObjects
{
	/// <summary>
	/// Holds all of the methods and properties for returning a set of tiles from a sprite
	/// </summary>
	public class MapGraphicsTileSet
	{
		private List <MapGraphicsTile> _mapTiles;
		private string _filename;
		private int _tileWidth;
		private int _tileHeight;
		private MapGraphicsTile _waterTile;
        private MapGraphicsTile _landTile;
		private MapGraphicsTile _errorTile;
		private System.Windows.Point _errorPoint;

		public List<MapGraphicsTile> MapTiles
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

		public MapGraphicsTile ErrorTile 
		{ 
			get
			{ 
				return _errorTile;
			}
		}

		public System.Windows.Point ErrorPoint
		{
			get
			{
				return _errorPoint;
			}
		}

		public MapGraphicsTileSet(string filename, int tileWidth, int tileHeight)
		{
			_filename = filename;
			_tileWidth = tileWidth;
			_tileHeight = tileHeight;
			_mapTiles = new List<MapGraphicsTile>();
			_errorPoint = new System.Windows.Point(-1, -1);
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
							System.Windows.Point startPoint = new System.Windows.Point(columnCount * _tileWidth, rowCount * _tileHeight);
							int edge1 = int.Parse(row[c]);
							int edge2 = int.Parse(row[c + 1]);
							MapGraphicsTile tile = new MapGraphicsTile { TileStartPoint = startPoint, ShoreEdgePoint = new System.Windows.Point(edge1, edge2)};

							if (edge1 == 0 && edge2 == 0)
							{
								_landTile = tile;
							}
							else if (edge1 == 13 && edge2 == 13)
							{
								_waterTile = tile;
							}
							else if (edge1 == 14 && edge2 == 14)
							{
								_errorTile = tile;
							}

							_mapTiles.Add(tile);
							
							columnCount++;
						}
					}
					rowCount++;
				}
			}			
		}

		public MapTile GetMatchingTile(MapGraphicsTile mapGraphicsTile)
		{
			//Getting a list of matching tiles. In the future we will have several tiles
			//That might fit, so we will take a random one from the list. 
			List<MapGraphicsTile> matchingTiles = _mapTiles.FindAll(
				instance => instance.ShoreEdgePoint == mapGraphicsTile.ShoreEdgePoint ||
				instance.ShoreEdgePoint == new System.Windows.Point(mapGraphicsTile.ShoreEdgePoint.Y, mapGraphicsTile.ShoreEdgePoint.X));

			if (matchingTiles.Count == 0)
				return new MapTile(_errorTile);

			Random rnd = new Random();
			return new MapTile(matchingTiles[rnd.Next(0, matchingTiles.Count)]);


			//foreach (MapGraphicsTile tile in _mapTiles)
			//{
			//    if (tile.ShoreEdgePoint == tileEdgePoint || tile.ShoreEdgePoint == )
			//    {
			//        return new MapTile(tile);
			//    }
			//}
			
		}

		public Dictionary<System.Windows.Point,BitmapSource> GetTileImages()
		{
			Dictionary<System.Windows.Point, BitmapSource> retval = new Dictionary<System.Windows.Point, BitmapSource>();
			
			if (_mapTiles.Count == 0)
			{
				LoadMapTileSet();
			}
			BitmapImage map = new BitmapImage(new Uri(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\MapTiles64x64.png"));

			foreach (MapGraphicsTile tile in _mapTiles)
			{
				BitmapSource bmpSource = new CroppedBitmap(map, new Int32Rect((int)tile.TileStartPoint.X, (int)tile.TileStartPoint.Y, 64, 64));
				retval.Add(tile.TileStartPoint, bmpSource);
			}

			return retval;
		}
	}
}
