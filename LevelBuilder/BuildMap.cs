using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using GMap.NET;
using GMap.NET.WindowsForms;
using System.Drawing.Imaging;
using GameObjects;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using Point = System.Windows.Point;

namespace LevelBuilder
{
    public class BuildMap
    {
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
                    _backgroundWorker.ReportProgress(1, "Generating Tile: " + gmapTilesProcessed + " of " + gmapTilesHeight*gmapTilesWidth );
                        
                    Exception ex;
                    WindowsFormsImage tile = GMaps.Instance.GetImageFrom(type, new GPoint(gmapStartTile.X + x, gmapStartTile.Y + y), zoom, out ex) as WindowsFormsImage;

                    if (ex != null)
                    {
                        e.Cancel = true;
                        return;  
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
								ProcessGmapTiles(x,y, bitmap);							
                            }                                    
                        }
                    }                        
                }
            }

			e.Result = _gameWorld;
        }

		private void ProcessGmapTiles(int gmapX, int gmapY, Bitmap gmapBitmap)
		{		
			const int tileSize = 16;
			//The bitmap coming in is 256x256 This needs to be broken down further into 16x16 sized
			//tiles in order to get the proper size of the map

			for (int tileX = 0; tileX < tileSize; tileX++)
			{
				for (int tileY = 0; tileY < tileSize; tileY++)
				{
					using (Bitmap smallBmp = new Bitmap(tileSize, tileSize))
					{
						using (Graphics gfx = Graphics.FromImage(smallBmp))
						{
							gfx.DrawImage(gmapBitmap,new Rectangle(0,0,16,16),tileX * tileSize,tileY * tileSize,16,16,GraphicsUnit.Pixel);
							
						}
#if DEBUG
						smallBmp.Save("C:\\tiles\\smalltile" + ((gmapX * tileSize) + tileX) + "-" + ((gmapY * tileSize) + tileY) + ".jpg");
#endif

						//To reduce the processing time when determining the tile type we are going to use BitmapData here and an array, since it is much faster that using getpixel and setpixel.
						//We will check each edge of the tile for water and land. If only water is found we will set the tile to the water tile. If only land is found we will set the tile 
						//to the land tile. If both are found then we will call a routine that will figure out which tile to use.
						BitmapData bmpData = smallBmp.LockBits(
							new Rectangle(0,0, tileSize, tileSize),
							ImageLockMode.ReadOnly,
							PixelFormat.Format32bppArgb);

						//This is a pointere that referenece the location of the first pixel of data 
						System.IntPtr Scan0 = bmpData.Scan0;

						//calculate the number of bytes
						int bytes = (bmpData.Stride * bmpData.Height);

						//An array of bytes. Just remember that each pixel format has a different number of bytes.
						//In our case, the number of bytes is 4 per pixel or RGBA. 
						byte[] rgbValues = new byte[bytes];

						//Safely copying the data to a managed array
						System.Runtime.InteropServices.Marshal.Copy(Scan0,
									   rgbValues, 0, bytes);

						bool hasWater = false;
						bool hasLand = false;
						CheckEdges(bmpData.Stride, rgbValues,tileSize, out hasWater, out hasLand);


						MapGraphicsTile mapGraphicsTile = new MapGraphicsTile();

						if ((gmapX * tileSize) + tileX == 5 && (gmapY * tileSize) + tileY == 26)
						{
							int me = 1;
							me = me + 1;
						}

						//Add the edgepoints that each neighbor tile already has
						mapGraphicsTile = GetNeighborTileEdgePoints(mapGraphicsTile, (gmapX * tileSize) + tileX, (gmapY * tileSize) + tileY);

						if (hasLand && hasWater)
						{							
							//Add any additional tiles that the neighbor didnt have.
							mapGraphicsTile = GenerateMapGraphicsTile(bmpData, rgbValues, tileSize, mapGraphicsTile);							
						}

						if (hasLand && !hasWater && mapGraphicsTile.ShoreEdgePoints.Count == 0)
						{
							_gameWorld.GameMap[(gmapX * tileSize) + tileX, (gmapY * tileSize) + tileY] = new MapTile(_mapGraphicsTileSet.LandTile);

						}
						else if (hasWater && !hasLand && mapGraphicsTile.ShoreEdgePoints.Count == 0)
						{
							_gameWorld.GameMap[(gmapX * tileSize) + tileX, (gmapY * tileSize) + tileY] = new MapTile(_mapGraphicsTileSet.WaterTile);
						}
						else
						{
							//Set the Tile edge to the neighbors edge
							mapGraphicsTile = SetTileEdgeByNeighbor(mapGraphicsTile, (gmapX * tileSize) + tileX, (gmapY * tileSize) + tileY);
							_gameWorld.GameMap[(gmapX * tileSize) + tileX, (gmapY * tileSize) + tileY] = _mapGraphicsTileSet.GetMatchingTile(mapGraphicsTile);
						}		

						smallBmp.UnlockBits(bmpData);
					}					
				}
			}
		}

		private MapGraphicsTile GetNeighborTileEdgePoints(MapGraphicsTile mapGraphicsTile, int x, int y)
		{
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

			mapGraphicsTile.ShoreEdgePoints = TranslatePoints(foundPoints);

			return mapGraphicsTile;
		}

		private List<byte> TranslatePoints(List<byte> foundPoints)
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

		private void CheckEdges(int stride, byte[] rgbValues, int tileSize, out bool hasWater, out bool hasLand)
		{
			hasWater = false;
			hasLand = false;
			bool retval;
			//Loop though all of the pixels on the Y edge
			for (int y = 0; y < tileSize; y++)
			{
				for (int x = 0; x < tileSize; x++)
				{
					int position = (y * stride) + (x * 4);

					Color color = SetColor(rgbValues[position], rgbValues[position + 1], rgbValues[position + 2]);
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

		private MapGraphicsTile GenerateMapGraphicsTile(BitmapData bmpData, byte[] rgbValues, int tileSize, MapGraphicsTile currentGraphicsTile)
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
					int position = (y * bmpData.Stride) + (x * 4);
					newColor = SetColor(rgbValues[position], rgbValues[position + 1], rgbValues[position + 2]);

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
					int position = (y * bmpData.Stride) + (x * 4);

					newColor = SetColor(rgbValues[position], rgbValues[position + 1], rgbValues[position + 2]);

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
				currentGraphicsTile.ShoreEdgePoints.AddRange(ConvertEdgeCoordinatesToEdgePoints(edgePoints));
			
			return currentGraphicsTile;
		}

		private GameObjects.Enums.EdgeType GetTileEdgeType(bool hasLand, bool hasWater)
		{			

			if (hasLand && hasWater)
				return Enums.EdgeType.Both;
			else if (hasLand)
				return Enums.EdgeType.Land;
			else if (hasWater)
				return Enums.EdgeType.Water;
			else
				throw new ArgumentOutOfRangeException("tile Doesn't have land or water");
		}

		private bool IsLandWaterTransition(Color newColor, Color previousColor)
		{
			bool isNewColorWater = IsWater(newColor);
			bool isPreviousColorWater = IsWater(previousColor);

			if (isNewColorWater != isPreviousColorWater)
				return true;
			else
				return false;

		}

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

		private  Color SetColor(byte blue, byte green, byte red)
		{
			return Color.FromArgb(255,red,green,blue);	
		}

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
