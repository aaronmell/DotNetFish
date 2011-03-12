using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using GMap.NET;
using GMap.NET.WindowsForms;
using System.Drawing.Imaging;
using DotNetFish.GameObjects;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using Point = System.Windows.Point;
using DotNetFish.GameObjects.Enums;
using System.Collections;

namespace DotNetFish.LevelBuilder
{
    public class BuildMap
    {
		//The default size of the tile we are checking from gmap. we are taking the 16x16 tile and replacing
		//it with a 64x64 tile. This allows us to get the size map we want. 
		const int _graphicsTileSize = 16;

        private BackgroundWorker _backgroundWorker;
        private List<PointLatLng> _points;
		private GameWorld _gameWorld;
		private MapGraphicsTileSet _mapGraphicsTileSet;
		private int _gameWorldWidth;
		private int _gameWorldHeight;		

        public BackgroundWorker BackgroundWorker
        {
            get
            {
                return _backgroundWorker;
            }
        }

        public GameWorld GameWorld
        {
			get
			{
				return _gameWorld;
			}
        }

        public BuildMap(List<PointLatLng> points)
        {
            _backgroundWorker = new System.ComponentModel.BackgroundWorker();
            _points = points;
			_mapGraphicsTileSet = new MapGraphicsTileSet(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\MapTiles.xml");
			_gameWorld = new GameWorld();
			
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += new DoWorkEventHandler(_backgroundWorker_DoWork);
        }	

        private void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)  
        {
			e.Cancel = Main();
			e.Result = _gameWorld;
        }

		/// <summary>
		/// Main function called by the background worker.
		/// </summary>
		/// <returns></returns>
		private bool Main()
		{
			//Gmap Setup stuff
			MapType type = MapType.GoogleMap;
			PureProjection prj = null;
			int maxZoom;
			int zoom = 22;
			GMaps.Instance.AdjustProjection(type, ref prj, out maxZoom);
			GMaps.Instance.Mode = AccessMode.ServerOnly;
			GMaps.Instance.ImageProxy = new WindowsFormsImageProxy();

			//Convert the PointLatLng to GPoints
			List<GPoint> gPoints = new List<GPoint>();
			foreach (PointLatLng p in _points)
			{
				gPoints.Add(prj.FromPixelToTileXY(prj.FromLatLngToPixel(p.Lat, p.Lng, zoom)));
			}

			//Get the Start and End Tile. Start Tile must be upper left tile, and end tile must be lower right
			GPoint gmapStartTile = getGmapStartTile(gPoints);
			GPoint gmapEndTile = getGmapEndTile(gPoints);

			//The gmap tile stuff
			int gmapTilesHeight = gmapEndTile.Y - gmapStartTile.Y + 1;
			int gmapTilesWidth = gmapEndTile.X - gmapStartTile.X + 1;
			int gmapTilesProcessed = 0;

			_gameWorldWidth = gmapTilesWidth * 16;
			_gameWorldHeight = gmapTilesHeight * 16;
			_gameWorld.GameMap = new MapTile[_gameWorldWidth, _gameWorldHeight];

#if DEBUG
			if (!Directory.Exists("C:\\tiles"))
				Directory.CreateDirectory("C:\\tiles");

			foreach (string s in Directory.GetFiles("c:\\tiles"))
			{
				File.Delete(s);
			}
#endif

			//Loop through each tile and add it to the array         
			for (int x = 0; x < gmapTilesWidth; x++)
			{
				for (int y = 0; y < gmapTilesHeight; y++)
				{
					gmapTilesProcessed++;
					_backgroundWorker.ReportProgress(1, "Generating Tile: " + gmapTilesProcessed + " of " + gmapTilesHeight * gmapTilesWidth);

					Exception ex;
					WindowsFormsImage tile = GMaps.Instance.GetImageFrom(type, new GPoint(gmapStartTile.X + x, gmapStartTile.Y + y), zoom, out ex) as WindowsFormsImage;

					if (ex != null)
					{						
						return true;
					}
					else if (tile != null)
					{
						using (tile)
						{
							using (Bitmap bitmap = new Bitmap(tile.Img))
							{
#if DEBUG
								bitmap.Save("C:\\tiles\\Largetile" + x + "-" + y + ".jpg");
#endif
								ProcessGmapTiles(x, y, bitmap);
							}
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Processes all of the Large Gmap Tiles
		/// </summary>
		/// <param name="gmapX"></param>
		/// <param name="gmapY"></param>
		/// <param name="gmapBitmap"></param>
		private void ProcessGmapTiles(int gmapX, int gmapY, Bitmap gmapBitmap)
		{	
			//The bitmap coming in is 256x256 This needs to be broken down further into 16x16 sized
			//tiles in order to get the proper size of the map
			for (int tileX = 0; tileX < _graphicsTileSize; tileX++)
			{
				for (int tileY = 0; tileY < _graphicsTileSize; tileY++)
				{
					using (Bitmap smallBmp = new Bitmap(_graphicsTileSize, _graphicsTileSize))
					{
						using (Graphics gfx = Graphics.FromImage(smallBmp))
						{
							gfx.DrawImage(gmapBitmap,new Rectangle(0,0,16,16),tileX * _graphicsTileSize,tileY * _graphicsTileSize,16,16,GraphicsUnit.Pixel);
						}
#if DEBUG
						smallBmp.Save("C:\\tiles\\smalltile" + ((gmapX * _graphicsTileSize) + tileX) + "-" + ((gmapY * _graphicsTileSize) + tileY) + ".jpg");
#endif
						BmpData bmpData = new BmpData(smallBmp, _graphicsTileSize);
						
						List<Point> edgeTileLocations = ProcessTile(bmpData, (gmapX * _graphicsTileSize) + tileX, (gmapY * _graphicsTileSize) + tileY);
						
						ProcessEdgeTiles(edgeTileLocations, bmpData);

						bmpData.Dispose();
					}					
				}
			}
		}

		/// <summary>
		/// Processes all of the Tiles and determines if they are an edge or not. Edgetiles get added to the list, and the 
		/// other tiles get water / land added to them. 
		/// </summary>
		/// <param name="bmpData"></param>
		/// <param name="gameWorldX"></param>
		/// <param name="gameWorldY"></param>
		/// <returns></returns>
		private List<Point> ProcessTile(BmpData bmpData, int gameWorldX, int gameWorldY)
		{			
			List<Point> edgeTileLocations = new List<Point>();

			bool hasWater = false;
			bool hasLand = false;

			CheckEdges(bmpData, _graphicsTileSize, out hasWater, out hasLand);

			if (hasLand && hasWater)
				edgeTileLocations.Add(new Point(gameWorldX, gameWorldY));
			else if (hasLand)
				_gameWorld.GameMap[gameWorldX, gameWorldY] = new MapTile(_mapGraphicsTileSet.LandTile);
			else
				_gameWorld.GameMap[gameWorldX, gameWorldY] = new MapTile(_mapGraphicsTileSet.WaterTile);

			//Need to release the locked bits here
			bmpData.Dispose();
			return edgeTileLocations;
		}

		/// <summary>
		/// Process all of the Edge Tiles
		/// </summary>
		/// <param name="edgeTilePoints"></param>
		/// <param name="bmpData"></param>
		private void ProcessEdgeTiles(List<Point> edgeTilePoints, BmpData bmpData)
		{
			foreach (Point gameWorldPoint in edgeTilePoints)
			{
				//Add the edgepoints that each neighbor tile already has
				MapGraphicsTile mapGraphicsTile = GetNeighborTileEdgePoints((int)gameWorldPoint.X, (int)gameWorldPoint.Y);

				//Set the Tile edge to the neighbors edge
				mapGraphicsTile = SetTileEdgeByNeighbor(mapGraphicsTile, (int)gameWorldPoint.X, (int)gameWorldPoint.Y);
				//Add any additional tiles that the neighbor didnt have.
				mapGraphicsTile.ShoreEdgePoints.AddRange(GenerateMapGraphicsTileEdgePoints(bmpData, _graphicsTileSize, mapGraphicsTile));
				//Add the tile to the map
				_gameWorld.GameMap[(int)gameWorldPoint.X, (int)gameWorldPoint.Y] = _mapGraphicsTileSet.GetMatchingTile(mapGraphicsTile);
			}
		}

		/// <summary>
		/// Gets the edge points from the neighbors
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		private MapGraphicsTile GetNeighborTileEdgePoints(int x, int y)
		{
			MapGraphicsTile mapGraphicsTile = new MapGraphicsTile();

			List<byte> foundPoints = new List<byte>();
			//Left Side
			if (x - 1 > 0 && _gameWorld.GameMap[x - 1, y] != null && _gameWorld.GameMap[x - 1, y].GraphicsTile != null)
			{
				foundPoints.AddRange(
					_gameWorld.GameMap[x - 1, y].GraphicsTile.ShoreEdgePoints.FindAll(
					instance => instance > 3 && instance < 7));		
			}					

			//Top Side
			if (y - 1 > 0 && _gameWorld.GameMap[x, y - 1] != null && _gameWorld.GameMap[x, y - 1].GraphicsTile != null)
				foundPoints.AddRange(
					_gameWorld.GameMap[x, y - 1].GraphicsTile.ShoreEdgePoints.FindAll(
					instance => instance > 0 && instance < 4));	

			//right Side
			if (x + 1 < _gameWorldWidth && _gameWorld.GameMap[x + 1, y] != null && _gameWorld.GameMap[x + 1, y].GraphicsTile != null)
				foundPoints.AddRange(
					_gameWorld.GameMap[x + 1, y].GraphicsTile.ShoreEdgePoints.FindAll(
					instance => instance > 9 && instance < 13));	

			//Bottom Side
			if (y + 1 < _gameWorldHeight && _gameWorld.GameMap[x, y + 1] != null && _gameWorld.GameMap[x, y + 1].GraphicsTile != null)
				foundPoints.AddRange(
					_gameWorld.GameMap[x, y + 1].GraphicsTile.ShoreEdgePoints.FindAll(
					instance => instance > 6 && instance < 10));

			mapGraphicsTile.ShoreEdgePoints = TranslateNeighborPoints(foundPoints);

			return mapGraphicsTile;
		}

		/// <summary>
		/// Translates the Points that come from a neighbors edge
		/// </summary>
		/// <param name="foundPoints"></param>
		/// <returns></returns>
		private List<byte> TranslateNeighborPoints(List<byte> foundPoints)
		{
			List<byte> translatedPoints = new List<byte>();

			foreach (byte b in foundPoints)
			{
				switch (b)
				{
					case 1:
						translatedPoints.Add(9);
						break;
					case 2:
						translatedPoints.Add(8);
						break;
					case 3:
						translatedPoints.Add(7);
						break;
					case 4:
						translatedPoints.Add(12);
						break;
					case 5:
						translatedPoints.Add(11);
						break;
					case 6:
						translatedPoints.Add(10);
						break;
					case 7:
						translatedPoints.Add(3);
						break;
					case 8:
						translatedPoints.Add(2);
						break;
					case 9:
						translatedPoints.Add(1);
						break;
					case 10:
						translatedPoints.Add(6);
						break;
					case 11:
						translatedPoints.Add(5);
						break;
					case 12:
						translatedPoints.Add(4);
						break;
				}
			}

			return translatedPoints;
		}

		/// <summary>
		/// Sets the Edge of the tile based on the neighbors edge
		/// </summary>
		/// <param name="mapGraphicsTile"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		private MapGraphicsTile SetTileEdgeByNeighbor(MapGraphicsTile mapGraphicsTile, int x, int y)
		{
			//Left Side
			if (x - 1 > 0 && _gameWorld.GameMap[x - 1, y] != null && _gameWorld.GameMap[x - 1, y].GraphicsTile != null)
				mapGraphicsTile.LeftEdgeType = _gameWorld.GameMap[x - 1, y].GraphicsTile.RightEdgeType;

			//Top Side
			if (y - 1 > 0 && _gameWorld.GameMap[x, y - 1] != null && _gameWorld.GameMap[x, y - 1].GraphicsTile != null)
				mapGraphicsTile.TopEdgeType = _gameWorld.GameMap[x, y - 1].GraphicsTile.BottomEdgeType;

			//right Side
			if (x + 1 < _gameWorldWidth && _gameWorld.GameMap[x + 1, y] != null && _gameWorld.GameMap[x + 1, y].GraphicsTile != null)
				mapGraphicsTile.RightEdgeType = _gameWorld.GameMap[x + 1, y].GraphicsTile.LeftEdgeType;
			
			//Bottom Side
			if (y + 1 < _gameWorldHeight && _gameWorld.GameMap[x, y + 1] != null && _gameWorld.GameMap[x, y + 1].GraphicsTile != null)
				mapGraphicsTile.BottomEdgeType = _gameWorld.GameMap[x, y + 1].GraphicsTile.TopEdgeType;

			return mapGraphicsTile;
		}	

		/// <summary>
		/// Checks the Edges of a Tile to determine if the tile contains land,water or both
		/// </summary>
		/// <param name="bmpData"></param>
		/// <param name="tileSize"></param>
		/// <param name="hasWater"></param>
		/// <param name="hasLand"></param>
		private void CheckEdges(BmpData bmpData, int tileSize, out bool hasWater, out bool hasLand)
		{
			hasWater = false;
			hasLand = false;
			bool retval;
			//Loop though all of the pixels on the Y edge
			for (int y = 0; y < tileSize; y++)
			{
				for (int x = 0; x < tileSize; x++)
				{
					int position = (y * bmpData.BitmapData.Stride) + (x * 4);

					Color color = SetColor(bmpData.RgbValues[position], bmpData.RgbValues[position + 1], bmpData.RgbValues[position + 2]);
					retval = IsWater(color);

					if (retval == true)
						hasWater = true;
					else
						hasLand = true;

					if (hasLand && hasWater)
						break;

					//only check the left and right edges.
					if (y != 0 && y != tileSize - 1)
						x+= tileSize - 2;
				}
				if (hasLand && hasWater)
					return;
			}
		}

		/// <summary>
		/// Generates the Edge Points for a MapGraphics Tile
		/// </summary>
		/// <param name="bmpData"></param>
		/// <param name="tileSize"></param>
		/// <param name="currentGraphicsTile"></param>
		/// <returns></returns>
		private List<byte> GenerateMapGraphicsTileEdgePoints(BmpData bmpData, int tileSize, MapGraphicsTile currentGraphicsTile)
		{			
			List<Point> edgePoints = new List<Point>();
			//Loop though all of the pixels on the Y edge
			for (int y = 0; y < tileSize; y+=tileSize - 1)
			{
				if (y ==0 && currentGraphicsTile.ShoreEdgePoints.Count(instance => instance > 6 && instance < 10) > 0)
					continue;
				if (y != 0 && currentGraphicsTile.ShoreEdgePoints.Count(instance => instance > 0 && instance < 4) > 0)
					continue;

				//Using these two values to determine if the edge is water, land, or both
				bool hasLand = false;
				bool hasWater = false;

				Color newColor = new Color();
				Color previousColor = new Color();
				for (int x = 0; x < tileSize; x++)
				{
					int position = (y * bmpData.BitmapData.Stride) + (x * 4);
					newColor = SetColor(bmpData.RgbValues[position], bmpData.RgbValues[position + 1], bmpData.RgbValues[position + 2]);

					if (!hasLand && !IsWater(newColor))
						hasLand = true;

					if (!hasWater && IsWater(newColor))
						hasWater = true;

					if (previousColor != new Color() && IsLandWaterTransition(newColor,previousColor))
					{
						//If the point is on a corner of the tile, we want to move
						//it off the corner so the correct Tile Edge is selected. 
						if (x == 15)
							edgePoints.Add(new Point(x -1, y));						
						else
							edgePoints.Add(new Point(x, y));
					}
					previousColor = newColor;
				}

				if (y == 0)
					currentGraphicsTile.TopEdgeType = GetTileEdgeType(hasLand, hasWater);
				else
					currentGraphicsTile.BottomEdgeType = GetTileEdgeType(hasLand, hasWater);

			}
			
			//Loop though all of the pixels on the X edge
			for (int x = 0; x < tileSize; x+=tileSize - 1)
			{
				if (x != 0 && currentGraphicsTile.ShoreEdgePoints.Count(instance => instance > 3 && instance < 7) > 0)
					continue;
				if (x == 0 && currentGraphicsTile.ShoreEdgePoints.Count(instance => instance > 9 && instance < 13) > 0)
					continue;

				//Using these two values to determine if the edge is water, land, or both
				bool hasLand = false;
				bool hasWater = false;

				Color newColor = new Color();
				Color previousColor = new Color();				

				//Starting and ending the count early. Because we dont want to check the first and
				//last positions twice.
				for (int y = 0; y < tileSize; y++)
				{
					int position = (y * bmpData.BitmapData.Stride) + (x * 4);

					newColor = SetColor(bmpData.RgbValues[position], bmpData.RgbValues[position + 1], bmpData.RgbValues[position + 2]);

					if (!hasLand && !IsWater(newColor))
						hasLand = true;

					if (!hasWater && IsWater(newColor))
						hasWater = true;

					if (previousColor != new Color() && IsLandWaterTransition(newColor, previousColor))
					{
						//If the point is on a corner of the tile, we want to move
						//it off the corner so the correct Tile Edge is selected. 
						if (y == 15)
							edgePoints.Add(new Point(x, y - 1));						
						else
							edgePoints.Add(new Point(x, y));
					}
					previousColor = newColor;
				}

				if (x == 0)
					currentGraphicsTile.LeftEdgeType = GetTileEdgeType(hasLand, hasWater);
				else
					currentGraphicsTile.RightEdgeType = GetTileEdgeType(hasLand, hasWater);
			}	
			return ConvertEdgeCoordinatesToEdgePoints(edgePoints);
		}

		/// <summary>
		/// Returns the EdgeType of a tile
		/// </summary>
		/// <param name="hasLand"></param>
		/// <param name="hasWater"></param>
		/// <returns></returns>
		private EdgeType GetTileEdgeType(bool hasLand, bool hasWater)
		{			

			if (hasLand && hasWater)
				return EdgeType.Both;
			else if (hasLand)
				return EdgeType.Land;
			else if (hasWater)
				return EdgeType.Water;
			else
				throw new ArgumentOutOfRangeException("tile Doesn't have land or water");
		}

		/// <summary>
		/// Determines if two colors are a land to water transition
		/// </summary>
		/// <param name="newColor"></param>
		/// <param name="previousColor"></param>
		/// <returns></returns>
		private bool IsLandWaterTransition(Color newColor, Color previousColor)
		{
			bool isNewColorWater = IsWater(newColor);
			bool isPreviousColorWater = IsWater(previousColor);

			if (isNewColorWater != isPreviousColorWater)
				return true;
			else
				return false;

		}

		/// <summary>
		/// Determines if a Color is water based on the color
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		private bool IsWater(Color color)
		{		
			if (color.R == 153 && color.B == 204 && color.G == 179 ||
				color.R == 175 && color.B == 207 && color.G == 189||
				color.R == 196 && color.B == 210 && color.G == 204||
				color.R == 217 && color.B == 217 && color.G == 217)
				return true;
			else
				return false;
		}

		/// <summary>
		/// Returns a color based on the RBG values
		/// </summary>
		/// <param name="blue"></param>
		/// <param name="green"></param>
		/// <param name="red"></param>
		/// <returns></returns>
		private  Color SetColor(byte blue, byte green, byte red)
		{
			return Color.FromArgb(255,red,green,blue);	
		}

		/// <summary>
		/// Converts the X,Y coordinate where there is a shift from land to water into an EdgePoint
		/// </summary>
		/// <param name="edgePoints"></param>
		/// <returns></returns>
		private List<byte> ConvertEdgeCoordinatesToEdgePoints(List<Point> edgePoints)
		{
			List<byte> tileEdgePoints = new List<byte>();

			foreach (Point p in edgePoints)
			{
				if (p.X == 0)
				{
					if (p.Y <= 4)
						tileEdgePoints.Add(10);
					else if (p.Y > 4 && p.Y <= 9)
						tileEdgePoints.Add(11);
					else
						tileEdgePoints.Add(12);
				}
				else if (p.X == 15)
				{
					if (p.Y <= 4)
						tileEdgePoints.Add(6);
					else if (p.Y > 4 && p.Y <= 9)
						tileEdgePoints.Add(5);
					else
						tileEdgePoints.Add(4);
				}
				else if (p.Y == 0)
				{
					if (p.X <= 4)
						tileEdgePoints.Add(9);
					else if (p.X > 4 && p.X <= 9)
						tileEdgePoints.Add(8);
					else
						tileEdgePoints.Add(7);
				}
				else if (p.Y == 15)
				{
					if (p.X <= 4)
						tileEdgePoints.Add(1);
					else if (p.X > 4 && p.X <= 9)
						tileEdgePoints.Add(2);
					else
						tileEdgePoints.Add(3);
				}				
			}

			return tileEdgePoints;
		} 
            
   		/// <summary>
   		/// Gets to End Gmap Tile (Bottom Right)
   		/// </summary>
   		/// <param name="points"></param>
   		/// <returns></returns>
        private GPoint getGmapEndTile(List<GPoint> points)
        {
            int x = int.MinValue;
            int y = int.MinValue;

            foreach (GPoint p in points)
            {
                x = p.X > x ? p.X : x;
                y = p.Y > y ? p.Y : y;
            }
            return new GPoint(x, y);
        }

        /// <summary>
        /// Gets the start tile of the selected region. Top left
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private GPoint getGmapStartTile(List<GPoint> points)
        {
            int x = int.MaxValue;
            int y = int.MaxValue;

            foreach (GPoint p in points)
            {
                x = p.X < x ? p.X : x;
                y = p.Y < y ? p.Y : y;
            }
            return new GPoint(x, y);
        }
	}
}
