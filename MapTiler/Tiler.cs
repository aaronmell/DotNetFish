using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
        private GMapMarker center;
        private GMapOverlay top;
        private bool isMouseDown;
        private PointLatLng startPosition;
        private PointLatLng endPosition;

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
                MainMap.MaxZoom = 17;
                MainMap.Zoom = 10;

                
                latitude.Text = MainMap.Position.Lat.ToString(CultureInfo.InvariantCulture);
                longitude.Text = MainMap.Position.Lng.ToString(CultureInfo.InvariantCulture);

                top = new GMapOverlay(MainMap, "top");
                MainMap.Overlays.Add(top);


                // map center
                center = new GMapMarkerCross(MainMap.Position);
                top.Markers.Add(center);

                // map events
                MainMap.OnCurrentPositionChanged += new CurrentPositionChanged(MainMap_OnCurrentPositionChanged);               
                MainMap.MouseMove += new MouseEventHandler(MainMap_MouseMove);
                MainMap.MouseDown += new MouseEventHandler(MainMap_MouseDown);
                MainMap.MouseUp += new MouseEventHandler(MainMap_MouseUp);

                MainMap.MapType = MapType.GoogleMap;

                MainMap.ShowTileGridLines = true;
            }
        }

        #region Events       
        
        // current point changed
        void MainMap_OnCurrentPositionChanged(PointLatLng point)
        {
            center.Position = point;
            latitude.Text = point.Lat.ToString(CultureInfo.InvariantCulture);
            longitude.Text = point.Lng.ToString(CultureInfo.InvariantCulture);
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
                isMouseDown = false;
                endPosition = MainMap.FromLocalToLatLng(e.X, e.Y);
            }
        }

        void MainMap_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = true;

                startPosition = MainMap.FromLocalToLatLng(e.X, e.Y);

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
            if (e.Button == MouseButtons.Left && isMouseDown)
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
    }
}
