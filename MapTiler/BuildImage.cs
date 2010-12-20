using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GMap.NET;
using GMap.NET.WindowsForms;
using System.IO;
using System.Windows;
using System.Drawing;
using System.ComponentModel;


namespace MapTiler
{
    public class BuildImage
    {
        private BackgroundWorker _backgroundWorker;
        private List<PointLatLng> _points;
        private string _filename;

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
            int zoom = 15;
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

            _backgroundWorker.ReportProgress(1, "Stitching Tiles ");
            //Loop through each tile and add it to the bitmap
            using (Bitmap bmp = new Bitmap(tilesWide * 256, tilesHigh * 256))
            {
                for (int x = 0; x < tilesWide; x++)
                {
                    for (int y = 0; y < tilesHigh; y++)
                    {
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
                                    gfx.DrawImage(tile.Img, x * 256, (tilesHigh - y - 1) * 256);                                    
                                   
                                }
                            }
                        }
                    }
                }

                _backgroundWorker.ReportProgress(1,"Transforming bmp");
                for (int x = 0; x < bmp.Width; x++)
                {
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        if (bmp.GetPixel(x, y).Name == "ff99b3cc")
                        {
                            bmp.SetPixel(x, y, Color.White);
                        }
                        else
                        {
                            bmp.SetPixel(x, y, Color.Black);
                        }
                    }
                   
                }  
                
                bmp.Save(_filename);
                    
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
