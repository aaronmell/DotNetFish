using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using GMap.NET;
using System.Globalization;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace MapTiler
{
    public partial class Tiler : Form
    {
        private GMapMarker _center;
        private GMapOverlay _top;
        private bool _isMouseDown;
        private PointLatLng _startPosition;
        private PointLatLng _endPosition;        

        public Tiler()
        {
            InitializeComponent();

            if (!DesignMode)
            {
                // add your custom map db provider
                //GMap.NET.CacheProviders.MySQLPureImageCache ch = new GMap.NET.CacheProviders.MySQLPureImageCache();
                //ch.ConnectionString = @"server=sql2008;User Id=trolis;Persist Security Info=True;database=gmapnetcache;password=trolis;";
                //MainMap.Manager.ImageCacheSecond = ch;

                // set your proxy here if need
                //MainMap.Manager.Proxy = new WebProxy("10.2.0.100", 8080);
                //MainMap.Manager.Proxy.Credentials = new NetworkCredential("ogrenci@bilgeadam.com", "bilgeada");

                // set cache mode only if no internet avaible
                try
                {
                    System.Net.IPHostEntry e = System.Net.Dns.GetHostEntry("www.google.com");
                }
                catch
                {
                    MainMap.Manager.Mode = AccessMode.CacheOnly;
                    MessageBox.Show("No internet connection avaible, going to CacheOnly mode.", "GMap.NET - Demo.WindowsForms", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                // config map 
                MainMap.Position = new PointLatLng(35.2276723549358, -97.22351074);

                MainMap.MapType = MapType.GoogleMap;
                MainMap.MinZoom = 1;
                MainMap.MaxZoom = 15;
                MainMap.Zoom = 10; 

                _top = new GMapOverlay(MainMap, "top");
                MainMap.Overlays.Add(_top);


                // map center
                _center = new GMapMarkerCross(MainMap.Position);
                _top.Markers.Add(_center);

                // map events
                MainMap.OnCurrentPositionChanged += new CurrentPositionChanged(MainMap_OnCurrentPositionChanged);               
                MainMap.MouseMove += new MouseEventHandler(MainMap_MouseMove);
                MainMap.MouseDown += new MouseEventHandler(MainMap_MouseDown);
                MainMap.MouseUp += new MouseEventHandler(MainMap_MouseUp);
                                
                MainMap.ShowTileGridLines = true;
            }
        }

        #region Events       
        
        // current point changed
        void MainMap_OnCurrentPositionChanged(PointLatLng point)
        {
            _center.Position = point;           
        }        

        // ensure focus on map, trackbar can have it too
        private void MainMap_MouseEnter(object sender, EventArgs e)
        {
            MainMap.Focus();
        }

        void MainMap_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isMouseDown = false;
                _endPosition = MainMap.FromLocalToLatLng(e.X, e.Y);
            }
        }

        void MainMap_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isMouseDown = true;

                _startPosition = MainMap.FromLocalToLatLng(e.X, e.Y);
                

                //if (currentMarker.IsVisible)
                //{
                //    currentMarker.Position = MainMap.FromLocalToLatLng(e.X, e.Y);

                //    var px = MainMap.Projection.FromLatLngToPixel(currentMarker.Position.Lat, currentMarker.Position.Lng, (int)MainMap.Zoom);
                //    var tile = MainMap.Projection.FromPixelToTileXY(px);

                //    Debug.WriteLine("marker: " + currentMarker.LocalPosition + " | geo: " + currentMarker.Position + " | px: " + px + " | tile: " + tile);
                //}
            }
        }

        // move current marker with left holding
        void MainMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _isMouseDown)
            {
                //if (CurentRectMarker == null)
                //{
                //    if (currentMarker.IsVisible)
                //    {
                //        currentMarker.Position = MainMap.FromLocalToLatLng(e.X, e.Y);
                //    }
                //}
                //else // move rect marker
                //{
                //    PointLatLng pnew = MainMap.FromLocalToLatLng(e.X, e.Y);

                //    int? pIndex = (int?)CurentRectMarker.Tag;
                //    if (pIndex.HasValue)
                //    {
                //        if (pIndex < polygon.Points.Count)
                //        {
                //            polygon.Points[pIndex.Value] = pnew;
                //            MainMap.UpdatePolygonLocalPosition(polygon);
                //        }
                //    }

                //    if (currentMarker.IsVisible)
                //    {
                //        currentMarker.Position = pnew;
                //    }
                //    CurentRectMarker.Position = pnew;

                //    if (CurentRectMarker.InnerMarker != null)
                //    {
                //        CurentRectMarker.InnerMarker.Position = pnew;
                //    }
                //}
            }
        }

        #endregion     

        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.AddExtension = true;
            saveFileDialog.Title = "Save Map";
            saveFileDialog.DefaultExt = ".bmp";
            saveFileDialog.Filter = "Bitmap Files (*.bmp)|*.bmp";

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (MainMap.SelectedArea.Size.HeightLat > 0 && MainMap.SelectedArea.Size.WidthLng > 0)
                {
                    BuildImage(saveFileDialog.FileName);
                }
                else
                {
                    MessageBox.Show("You must select a region. Use SHIFT + Left Mouse Button to select a region");
                }

                
                
            }
        }

        private void BuildImage(string filename)
        {
            BuildImage buildImage = new BuildImage(new List<PointLatLng> { _startPosition, _endPosition }, filename);
            buildImage.BackgroundWorker.ProgressChanged +=new ProgressChangedEventHandler(BackgroundWorker_ProgressChanged);
            buildImage.BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorker_RunWorkerCompleted);           
            buildImage.BackgroundWorker.RunWorkerAsync();
          
           
        }

        void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {              
             if (e.Cancelled)
             {  
                 MessageBox.Show("There was an error when retrieving tiles from the map server");  
             }  
             else if (e.Error != null)  
             {                  
                 MessageBox.Show("Error. Details: " + (e.Error as Exception).ToString());  
             }  
             else
             {  
                 MessageBox.Show("The File has been created successfully");  
             }
             status.Text = "";
        }

        void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            status.Text = (string)e.UserState;
        }
    }
}
