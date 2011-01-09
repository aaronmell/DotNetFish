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

namespace LevelBuilder
{
    public class BuildMap
    {
        private BackgroundWorker _backgroundWorker;
        private List<PointLatLng> _points;
		private GameWorld _gameWorld;
		private MapGraphicsTileSet _mapGraphicsTileSet;

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
			_mapGraphicsTileSet = new MapGraphicsTileSet(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\MapTiles.csv", 64, 64);
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
            int gmapTilesHeight = gmapStartTile.Y - gmapEndTile.Y + 1;
            int gmapTilesWidth = gmapEndTile.X - gmapStartTile.X + 1;
            int gmapTilesProcessed = 0;

			_gameWorld.GameMap = new MapTile[gmapTilesWidth * 16, gmapTilesHeight * 16];


            //Loop through each tile and add it to the array         
            for (int x = 0; x < gmapTilesWidth; x++)
            {
                for (int y = 0; y < gmapTilesHeight; y++)
                {
                    gmapTilesProcessed++;
                    _backgroundWorker.ReportProgress(1, "Generating Tile: " + gmapTilesProcessed + " of " + gmapTilesHeight*gmapTilesWidth );
                        
                    Exception ex;
                    WindowsFormsImage tile = GMaps.Instance.GetImageFrom(type, new GPoint(gmapStartTile.X + x, gmapStartTile.Y - y), zoom, out ex) as WindowsFormsImage;

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
								//foreach (string s in Directory.GetFiles("tiles"))
								//{
								//    File.Delete(s);
								//}
								ProcessGmapTiles(x,y, bitmap);							
                            }                                    
                        }
                    }                        
                }
            }

			e.Result = _gameWorld;
        }

		private void ProcessGmapTiles(int gameworldX, int gameworldY, Bitmap bitmap)
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
							gfx.DrawImage(bitmap,new Rectangle(0,0,16,16),tileX * tileSize,tileY * tileSize,16,16,GraphicsUnit.Pixel);
							//smallBmp.Save("tiles\\tile" + tileX + tileY + ".jpg");
						}

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

						if (hasLand && hasWater)
						{
							_gameWorld.GameMap[(gameworldX * tileSize) + tileX, (gameworldY * tileSize) + tileY] = DetermineTiletoUse(bmpData, rgbValues, tileSize);
						}
						else
						{
							if (hasLand)
								_gameWorld.GameMap[(gameworldX * tileSize) + tileX, (gameworldY * tileSize) + tileY] = new MapTile(_mapGraphicsTileSet.LandTile);
							else
								_gameWorld.GameMap[(gameworldX * tileSize) + tileX, (gameworldY * tileSize) + tileY] = new MapTile(_mapGraphicsTileSet.WaterTile);
						}
						smallBmp.UnlockBits(bmpData);
					}					
				}
			}
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

		private MapTile DetermineTiletoUse(BitmapData bmpData, byte[] rgbValues,int tileSize)
		{
			List<Point> edgePoints = new List<Point>();
			
			//Loop though all of the pixels on the Y edge
			for (int y = 0; y < tileSize; y+=tileSize - 1)
			{
				Color newColor = new Color();
				Color previousColor = new Color();
				for (int x = 0; x < tileSize; x++)
				{
					int position = (y * bmpData.Stride) + (x * 4);

					newColor = SetColor(rgbValues[position], rgbValues[position + 1], rgbValues[position + 2]);

					if (previousColor != new Color() && IsLandWaterTransition(newColor,previousColor))
					{
						edgePoints.Add(new Point(x, y));						
					}
					previousColor = newColor;
				}
			}
			
			//Loop though all of the pixels on the X edge
			for (int x = 0; x < tileSize; x+=tileSize - 1)
			{
				Color newColor = new Color();
				Color previousColor = new Color();

				for (int y = 0; y < tileSize; y++)
				{
					int position = (y * bmpData.Stride) + (x * 4);

					newColor = SetColor(rgbValues[position], rgbValues[position + 1], rgbValues[position + 2]);

					if (previousColor != new Color() && IsLandWaterTransition(newColor, previousColor))
					{
						edgePoints.Add(new Point(x, y));
					}
					previousColor = newColor;
				}
			}
			if (edgePoints.Count == 2)
				return GetTileThatMatches(edgePoints);
			else
				return new MapTile(_mapGraphicsTileSet.ErrorTile);
		}

		private bool IsLandWaterTransition(Color newColor, Color previousColor)
		{
			bool isNewColorWater = IsWater(newColor);
			bool isPreviousColorWater = IsWater(previousColor);

			if (isNewColorWater && !isPreviousColorWater || isPreviousColorWater && !isNewColorWater)
				return true;
			else
				return false;

		}

		private bool IsWater(Color color)
		{		
			if (color.R == 153 && color.B == 204 && color.G == 179)
				return true;
			else
				return false;
		}

		private  Color SetColor(byte blue, byte green, byte red)
		{
			return Color.FromArgb(255,red,green,blue);	
		}

		private MapTile GetTileThatMatches(List<Point> edgePoints)
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

			if (tileEdgePoints.Count != 2)
			{
				//This could occur still, so lets continue to check for it just in case, so we can find any errors.
				throw new Exception("Uh oh, we have a tile with the wrong number of edges");
			}

			return _mapGraphicsTileSet.GetMatchingTile(new System.Windows.Point(tileEdgePoints[0],tileEdgePoints[1]));
		} 
               
        private GPoint getGmapEndTile(List<GPoint> points)
        {
            int x = int.MinValue;
            int y = int.MaxValue;

            foreach (GPoint p in points)
            {
                x = p.X > x ? p.X : x;
                y = p.Y < y ? p.Y : y;
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
            int y = int.MinValue;

            foreach (GPoint p in points)
            {
                x = p.X < x ? p.X : x;
                y = p.Y > y ? p.Y : y;
            }
            return new GPoint(x, y);
        }
	}
}
