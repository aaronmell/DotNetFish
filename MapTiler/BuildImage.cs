using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using GMap.NET;
using GMap.NET.WindowsForms;
using System.Drawing.Imaging;


namespace MapTiler
{
    public class BuildImage
    {
        private BackgroundWorker _backgroundWorker;
        private List<PointLatLng> _points;
        private string _filename;
        private Bitmap _blackTile;
        private Bitmap _whiteTile;



        public BackgroundWorker BackgroundWorker
        {
            get
            {
                return _backgroundWorker;
            }
        }

        public BuildImage(List<PointLatLng> points, string filename)
        {
            _backgroundWorker = new System.ComponentModel.BackgroundWorker();
            _filename = filename;
            _points = points;
            _blackTile = new Bitmap(256, 256);
            _whiteTile = new Bitmap(256, 256);

            using (Graphics gfx = Graphics.FromImage(_blackTile))
            {
                SolidBrush b = new SolidBrush(Color.Black);
                gfx.FillRectangle(b, new Rectangle(0, 0,256, 256));
            }

            using (Graphics gfx = Graphics.FromImage(_whiteTile))
            {
                SolidBrush b = new SolidBrush(Color.White);
                gfx.FillRectangle(b, new Rectangle(0, 0, 256, 256));
            }

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
            int zoom = 19;
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
            GPoint startTile = getStartTile(gPoints);
            GPoint endTile = getEndTile(gPoints);

            int tilesHigh = startTile.Y - endTile.Y;
            int tilesWide = endTile.X - startTile.X;
            int tilesprocessed = 0;
            
            //Loop through each tile and add it to the bitmap
            using (Bitmap bmp = new Bitmap(tilesWide * 256, tilesHigh * 256))
            {
                for (int x = 0; x < tilesWide; x++)
                {
                    for (int y = 0; y < tilesHigh; y++)
                    {
                        tilesprocessed++;
                        _backgroundWorker.ReportProgress(1, "Stitching Tile: " + tilesprocessed + " of " + tilesHigh*tilesWide );
                        
                        using (Graphics gfx = Graphics.FromImage(bmp))
                        {
                            Exception ex;
                            WindowsFormsImage tile = GMaps.Instance.GetImageFrom(type, new GPoint(startTile.X + x, startTile.Y - y), zoom, out ex) as WindowsFormsImage;

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
                                        gfx.DrawImage(ColorTile(bitmap), x * 256, (tilesHigh - y - 1) * 256); 
                                    }                                    
                                }
                            }
                        }
                    }
                }                         
                bmp.Save(_filename,ImageFormat.Jpeg);                    
            }                
        }

        private Bitmap ColorTile(Bitmap bitmap)
        {
            //TO reduce the processing time when coloring, we want to check the edges of the tile for water. If all of the tile is water we can return a white bitmap
            //If none of the tiles are water, we can return a black bitmap. We only need to check every pixel if there is water and land. This will also clean up
            //any small tiles of land/water since we are only checking the edge.

            bool HasLand = false;
            bool HasWater = false;

            //Check the Top and bottom row\
            for (int y = 0; y < bitmap.Height; y+=255)
            {
                for (int x = 0; x < bitmap.Width; x+=3 )
                {
                    if (bitmap.GetPixel(x, y).Name == "ff99b3cc")
                    {
                        HasWater = true;                    
                    }
                    else
                    {
                        HasLand = true;
                    }
                    if (HasLand && HasWater)
                        break;
                }

                if (HasLand && HasWater)
                        break;
            }

            for (int x = 0; x < bitmap.Width; x+=255 )
                {
                    for (int y = 0; y < bitmap.Height; y+=3)
                    {

                        if (bitmap.GetPixel(x, y).Name == "ff99b3cc")
                        {
                            HasWater = true;
                        }
                        else
                        {
                            HasLand = true;
                        }
                        if (HasLand && HasWater)
                            break;
                    }

                if (HasLand && HasWater)
                        break;
            }


            if (HasLand && !HasWater)
                return _blackTile;
            else if (!HasLand && HasWater)
                return _whiteTile;
            else
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        if (bitmap.GetPixel(x, y).Name == "ff99b3cc")
                        {
                            bitmap.SetPixel(x, y, Color.White);
                        }
                        else
                        {
                            bitmap.SetPixel(x, y, Color.Black);
                        }
                    }                   
                }             
                return bitmap;
            }
        }        

        private GPoint getEndTile(List<GPoint> points)
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

        private GPoint getStartTile(List<GPoint> points)
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
