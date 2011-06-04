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
using DotNetFish.GameObjects.Enums;

namespace DotNetFish.GameObjects
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
				if (_errorTile == null)
					SetSpecialTiles();

				return _waterTile;
			}
		}

		public MapGraphicsTile LandTile
		{
			get
			{
				if (_errorTile == null)
					SetSpecialTiles();

				return _landTile;
			}
		}

		public MapGraphicsTile ErrorTile 
		{ 
			get
			{
 				if (_errorTile == null)
					SetSpecialTiles();
				return _errorTile;
			}
		}

		public MapGraphicsTileSet(string filename)
		{
			_filename = filename;			
			_mapTiles = new List<MapGraphicsTile>();			
			_mapTiles = LoadMapTileSet(_filename);
			
		}

		private void SetSpecialTiles()
		{
			if (!MapTiles.Exists(instance => instance.TileType == TileType.Error))
				throw new Exception("TileSet Does not have an error Tile");
			if (!MapTiles.Exists(instance => instance.TileType == TileType.Land))
				throw new Exception("TileSet Does not have a Land Tile");
			if (!MapTiles.Exists(instance => instance.TileType == TileType.Water))
				throw new Exception("TileSet Does not have a Water Tile");

			_errorTile = _mapTiles.First(instance => instance.TileType == TileType.Error);
			_landTile = _mapTiles.First(instance => instance.TileType == TileType.Land);

			_landTile.LeftEdgeType = EdgeType.Land;
			_landTile.RightEdgeType = EdgeType.Land;
			_landTile.TopEdgeType = EdgeType.Land;
			_landTile.BottomEdgeType = EdgeType.Land;

			_waterTile = _mapTiles.First(instance => instance.TileType == TileType.Water);

			_waterTile.LeftEdgeType = EdgeType.Water;
			_waterTile.RightEdgeType = EdgeType.Water;
			_waterTile.TopEdgeType = EdgeType.Water;
			_waterTile.BottomEdgeType = EdgeType.Water;
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
								mapGraphicsTile.Equals(maptile)
							select maptile).ToList();

			if (matchingTiles.Count == 0)
				return new MapTile(_errorTile);

			Random rnd = new Random();
			return new MapTile(matchingTiles[rnd.Next(0, matchingTiles.Count)]);
		}

		public Dictionary<System.Windows.Point,CachedBitmap> GetTileImages()
		{
			Dictionary<System.Windows.Point, CachedBitmap> retval = new Dictionary<System.Windows.Point, CachedBitmap>();
			
			if (_mapTiles.Count == 0)
			{
				_mapTiles = LoadMapTileSet(_filename);
			}
			BitmapImage map = new BitmapImage(new Uri(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\MapTiles.png"));

			foreach (MapGraphicsTile tile in _mapTiles)
			{            
				BitmapSource bmpSource = new CroppedBitmap(map, new Int32Rect((int)tile.TileStartPoint.X, (int)tile.TileStartPoint.Y, 64, 64));

                CachedBitmap cachedBitmap = new CachedBitmap(bmpSource, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                retval.Add(tile.TileStartPoint, cachedBitmap);
			}

			return retval;
		}
	}
}
