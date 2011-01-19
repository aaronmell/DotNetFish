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
using LevelBuilder;
using LevelDesigner;
using GameObjects;

namespace MapBuilder
{
    /// <summary>
    /// Interaction logic for LoadWindow.xaml
    /// </summary>
    public partial class LoadWindow : Window
    {
        public LoadWindow()
        {
            InitializeComponent();
        }

        private void uxNewLevel_Click(object sender, RoutedEventArgs e)
        {
            SelectMapRegion selectMapRegion = new SelectMapRegion();
            selectMapRegion.Show();
            this.Hide();        
        }       

        private void uxLoadLevel_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();            
            dlg.DefaultExt = ".dfl"; // Default file extension
            dlg.Filter = "DotNetFish Level (.dfl)|*.dfl";

            Nullable<bool> result = dlg.ShowDialog();
          
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
				GameWorld gameWorld = LevelBuilder.FileIO.LoadMap(filename);
				EditLevel editLevel = new EditLevel(gameWorld);
				editLevel.Show();
            }
        }
    }
}
