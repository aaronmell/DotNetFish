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
            //To reduce the processing time when coloring we are going to use BitmapData here and an array, since it is much faster that using getpixel and setpixel.
            //We will check each edge of the tile for water and land. If only water is found we will set the tile to white. If only land is found we will set the tile 
            //to black. If both are found then we will change the values depending on which is which.
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);
            
            //This is a pointere that referenece the location of the first pixel of data 
            System.IntPtr Scan0 = bmpData.Scan0;

            //calculate the number of bytes
            int bytes = bmpData.Stride * bitmap.Height;

            //An array of bytes. Just remember that each pixel format has a different number of bytes.
            //In our case, the number of bytes is 4 per pixel or RGBA. 
            byte[] rgbValues = new byte[bytes];

            //Safely copying the data to a managed array
            System.Runtime.InteropServices.Marshal.Copy(Scan0,
                           rgbValues, 0, bytes);


            bool hasWater = false;
            bool hasLand = false;
            bool retval;

            //Loop though all of the pixels on the Y edge
            for (int y = 0; y < bitmap.Height; y+=255)
            {
                for (int x = 0; x < bitmap.Width; x+=5)
                {
                    int position = (y * bmpData.Stride) + (x * 4);

                    retval = IsWater(rgbValues[position], rgbValues[position + 1], rgbValues[position + 2]);

                    if (retval == true)
                        hasWater = true;
                    else 
                        hasLand = true; 

                    if (hasLand && hasWater)
                        break;
                }
                    if (hasLand && hasWater)
                        break;
            }

            //Loop though all of the pixels on the X edge
            if (!hasLand & !hasWater)
            {
                for (int x = 0; x < bitmap.Width; x+=255)
                {
                    for (int y = 0; y < bitmap.Height; y+=5)
                    {
                        int position = (y * bmpData.Stride) + (x * 4);

                        retval = IsWater(rgbValues[position], rgbValues[position + 1], rgbValues[position + 2]);

                        if (retval == true)
                            hasWater = true;
                        else
                            hasLand = true;                      

                        if (hasLand && hasWater)
                            break;
                    }

                    if (hasLand && hasWater)
                            break;
                }
            }

            if (hasLand && hasWater)
            {
                PaintTile(bitmap.Height,bitmap.Width,ref rgbValues,bmpData.Stride);
                //Save the manipulated data back to the bitmap.
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, Scan0, bytes);
                bitmap.UnlockBits(bmpData);
                return bitmap;
            }

            else
            {
                bitmap.UnlockBits(bmpData);

                if (hasLand)
                    return _blackTile;
                else
                    return _whiteTile;
            }
        }

        /// <summary>
        /// Given RGB values determines if it is water or not. This might be unsafe depending on the map used.
        /// </summary>
        /// <param name="blue"></param>
        /// <param name="green"></param>
        /// <param name="red"></param>
        /// <returns></returns>
        private bool IsWater(byte blue, byte green, byte red)
        {
            if (blue == 204 && green == 179 && red == 153)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Paints a tile that has both water and land to black and white
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="rgbValues"></param>
        /// <param name="stride"></param>
        private void PaintTile(int height, int width, ref byte[] rgbValues, int stride)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int position = (y * stride) + (x * 4);

                    if (rgbValues[position] == 204 && rgbValues[position + 1] == 179 && rgbValues[position + 2] == 153)
                        rgbValues[position] = rgbValues[position + 1] = rgbValues[position + 2] = 255;
                    else
                        rgbValues[position] = rgbValues[position + 1] = rgbValues[position + 2] = 0;
                }
            }
        }               

        /// <summary>
        /// Gets the end tile of the selected region Bottom Right
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the start tile of the selected region. Top left
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
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
