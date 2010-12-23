using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO;
using System.Diagnostics;

namespace MapBuilder
{
    public class LevelBuilder
    {
        private BitmapImage maptiles;

        public LevelBuilder()
        {
            //maptiles = new Bitmap(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\maptiles.png"));
        } 

        public void BuildWorldObject(string filename)
        {
            //BitmapImage originalMap = new BitmapImage(new Uri(filename));
             
        }
    }
}
