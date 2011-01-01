using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using LevelBuilder;
using System.ComponentModel;

namespace LevelDesigner
{
    /// <summary>
    /// Interaction logic for SelectMapRegion.xaml
    /// </summary>
    public partial class SelectMapRegion : Window
    {
        private PointLatLng _startPosition;
        private PointLatLng _endPosition; 

        public SelectMapRegion()
        {
            InitializeComponent();

            try
            {
                System.Net.IPHostEntry e = System.Net.Dns.GetHostEntry("www.google.com");
            }
            catch
            {
                gmapControl.Manager.Mode = AccessMode.CacheOnly;
                MessageBox.Show("No internet connection avaible, going to CacheOnly mode.", "GMap.NET - Demo.WindowsPresentation", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // config map
            gmapControl.Position = new PointLatLng(35.2276723549358, -97.22351074);
            gmapControl.MapType = MapType.GoogleMap;
            gmapControl.MinZoom = 1;
            gmapControl.MaxZoom = 15;
            gmapControl.Zoom = 10;

            gmapControl.MouseUp += new MouseButtonEventHandler(gmapControl_MouseUp);
            gmapControl.MouseDown += new MouseButtonEventHandler(gmapControl_MouseDown);

        }

        void gmapControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {               
                Point position = e.GetPosition(this);
                _startPosition = gmapControl.FromLocalToLatLng((int)position.X, (int)position.Y);
            }
        }

        void gmapControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {               
                Point position = e.GetPosition(this);
                _endPosition = gmapControl.FromLocalToLatLng((int)position.X, (int)position.Y);
            }
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            if (gmapControl.SelectedArea.Size.HeightLat > 0 && gmapControl.SelectedArea.Size.WidthLng > 0)
            {
                
                BuildMap BuildMap = new BuildMap(new List<PointLatLng> { _startPosition, _endPosition });
                BuildMap.BackgroundWorker.ProgressChanged += new ProgressChangedEventHandler(BackgroundWorker_ProgressChanged);
                BuildMap.BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorker_RunWorkerCompleted);
                BuildMap.BackgroundWorker.RunWorkerAsync();
            }
            else
            {
                MessageBox.Show("You must select a region. Use SHIFT + Left Mouse Button to select a region");
            }  

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
                 MessageBox.Show("The Tiling has been completed successfully");  
             }
             status.Content = "";
        }

        void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            status.Content = (string)e.UserState;
        }
    }     
}
