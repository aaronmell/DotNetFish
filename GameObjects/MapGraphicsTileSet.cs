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
using System.Xml.Serialization;

namespace GameObjects
{
	/// <summary>
	/// Holds all of the methods and properties for returning a set of tiles from a sprite
	/// </summary>
	public class MapGraphicsTileSet
	{
		private List <MapGraphicsTile> _mapTiles;
		private string _filename;		
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

		public MapGraphicsTileSet(string filename)
		{
			_filename = filename;			
			_mapTiles = new List<MapGraphicsTile>();
			_errorPoint = new System.Windows.Point(-1, -1);
			_mapTiles = LoadMapTileSet(_filename);
			SetSpecialTiles();
		}

		private void SetSpecialTiles()
		{
			_errorTile = _mapTiles.First(instance => instance.TileType == Enums.TileType.Error);
			_landTile = _mapTiles.First(instance => instance.TileType == Enums.TileType.Land);
			_waterTile = _mapTiles.First(instance => instance.TileType == Enums.TileType.Water);
		}

		public static void SaveTileSet(List<MapGraphicsTile> mapGraphicsTile, string filename)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(List<MapGraphicsTile>));
			TextWriter textWriter = new StreamWriter(filename);
			serializer.Serialize(textWriter, mapGraphicsTile);
			textWriter.Close();
		}

		public static List<MapGraphicsTile> LoadMapTileSet(string filename)
		{
			List<MapGraphicsTile> mapTiles = new List<MapGraphicsTile>();
			XmlSerializer deserializer = new XmlSerializer(typeof(List<MapGraphicsTile>));
			TextReader textReader = new StreamReader(filename);
			
			mapTiles = (List<MapGraphicsTile>)deserializer.Deserialize(textReader);
			textReader.Close();
			
			return mapTiles;
		}		

		public MapTile GetMatchingTile(MapGraphicsTile mapGraphicsTile)
		{
			//Getting a list of matching tiles. In the future we will have several tiles
			//That might fit, so we will take a random one from the list. 
			var matchingTiles = (from maptile in _mapTiles
							where
								maptile.ShoreEdgePoints.OrderBy(i => i).SequenceEqual(mapGraphicsTile.ShoreEdgePoints.OrderBy(i => i)) &&
								(mapGraphicsTile.LeftEdgeType == Enums.EdgeType.Undefined || maptile.LeftEdgeType == mapGraphicsTile.LeftEdgeType) &&
								(mapGraphicsTile.RightEdgeType == Enums.EdgeType.Undefined || maptile.RightEdgeType == mapGraphicsTile.RightEdgeType) &&
								(mapGraphicsTile.TopEdgeType == Enums.EdgeType.Undefined || maptile.TopEdgeType == mapGraphicsTile.TopEdgeType) &&
								(mapGraphicsTile.BottomEdgeType == Enums.EdgeType.Undefined || maptile.BottomEdgeType == mapGraphicsTile.BottomEdgeType)
							select maptile).ToList();

			if (matchingTiles.Count == 0)
				return new MapTile(_errorTile);

			Random rnd = new Random();
			return new MapTile(matchingTiles[rnd.Next(0, matchingTiles.Count)]);
		}

		public Dictionary<System.Windows.Point,BitmapSource> GetTileImages()
		{
			Dictionary<System.Windows.Point, BitmapSource> retval = new Dictionary<System.Windows.Point, BitmapSource>();
			
			if (_mapTiles.Count == 0)
			{
				_mapTiles = LoadMapTileSet(_filename);
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
