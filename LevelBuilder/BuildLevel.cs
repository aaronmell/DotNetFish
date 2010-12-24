using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GameObjects;
using System.Diagnostics;
using System.IO;
using System.Drawing.Imaging;

namespace LevelBuilder
{
    public class BuildLevel
    {
        private Bitmap _dfmImage;
        private Bitmap _mapTiles;
        private Bitmap _levelImage;
        private Dictionary<Point, GraphicTile> _tileMap;
        private GraphicTile _waterTile;
        private GraphicTile _landTile;


        public void Build(string filename)
        {
           _tileMap = LoadTileMap();
         
           _dfmImage = new Bitmap(filename);
           _levelImage = new Bitmap(_dfmImage.Width, _dfmImage.Height);
           _mapTiles = new Bitmap(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\maptiles.png");
            
            ProcessMap();
           _levelImage.Save("level.bmp");
        }

        /// <summary>
        /// Creates a Dictonary of Graphics Tiles and where they are located in the sprite
        /// </summary>
        private Dictionary<Point, GraphicTile> LoadTileMap()
        {
            Dictionary<Point, GraphicTile> tileMap = new Dictionary<Point, GraphicTile>();

            using (StreamReader readFile = new StreamReader(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\maptiles.csv"))
            {
                string line;
                string[] row;
                int rowCount = 0;

                while ((line = readFile.ReadLine()) != null)
                {
                    row = line.Split(',');
                    int columnCount = 0;

                    for (int c = 0; c < row.Count(); c+=2)                   
                    {
                        if (row.Count() > 0 && row[c] != "" && row[c+1] != "")
                        {
                            Point startPoint = new Point(columnCount * 64, rowCount * 64);
                            int edge1 = int.Parse(row[c]);
                            int edge2 = int.Parse(row[c + 1]);
                            GraphicTile tile = new GraphicTile  { TileStartPoint = startPoint, ShoreEdgePoints = new List<int> { edge1, edge2 } };

                            if (edge1 == 0 && edge2 == 0)
                            {
                                _landTile = tile;
                            }
                            else if (edge1 == 13 && edge2 == 13)
                            {
                                _waterTile = tile;
                            }
                            else
                            {
                                tileMap.Add(startPoint, tile);
                            }                           
                            columnCount++;
                        }
                    }
                    rowCount++;
                }
            }
            return tileMap;
        } 

        private void ProcessMap()
        {
            for (int x = 0; x < _dfmImage.Width / 64; x++)
            {
                for (int y = 0; y < _dfmImage.Height / 64; y++)
                {
                    Rectangle rect = new Rectangle(0,0, 64, 64);

                    using (Bitmap bmpTile = new Bitmap(64,64))
                    {
                        using (Graphics gfx = Graphics.FromImage(bmpTile))
                        {
                            gfx.DrawImage(_dfmImage, rect, x * 64, y * 64,64, 64, GraphicsUnit.Pixel);
                            //bmpTile.Save("Test.bmp");
                            BitmapData tile = bmpTile.LockBits(new Rectangle(0,0,64,64), ImageLockMode.ReadOnly, bmpTile.PixelFormat);

                            int color = GetColor(tile);
                            GraphicTile tileToUse;

                            if (color == 0)
                            {
                                tileToUse = _landTile;
                            }
                            else if (color == 2)
                            {
                                tileToUse = _waterTile;
                            }
                            else
                            {
                                tileToUse = new GraphicTile();
                                //bmpTile.Save("mixed.bmp");   
                            }

                            bmpTile.UnlockBits(tile);
                            DrawTile(x*64,y*64, tileToUse);
                        }
                    }
                }
            }
        
        }

        private void DrawTile(int x, int y, GraphicTile tileToUse)
        {
            using (Graphics gfx = Graphics.FromImage(_levelImage))
            {
                Rectangle rect = new Rectangle(x, y, 64, 64);
                gfx.DrawImage(_mapTiles, rect, tileToUse.TileStartPoint.X, tileToUse.TileStartPoint.Y, 64, 64, GraphicsUnit.Pixel);                   
            }
        }           

        //private GraphicTile GetTile(BitmapData tile)
        //{           
           
        //}

        //Returns a value between 0 and 2 depending on how much water is on the map, returns 1 if tile is all water and 0 if tile is all land
        private int GetColor(BitmapData tile)
        {
            // Get the address of the first line.
            IntPtr ptr = tile.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes  = Math.Abs(tile.Stride) * 64;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            bool hasLand = false;
            bool hasWater = false;

            for (int x = 0; x < rgbValues.Count(); x+=4)
            {
                int r = rgbValues[x];
                int g = rgbValues[x +1];
                int b = rgbValues[x+2];
                int a = rgbValues[x+3];

                if (r == 0 && g == 0 && b == 0)
                {
                    hasLand = true;
                
                }
                else if (r > 128 && g> 128  && b > 128)
                {
                    hasWater = true;
                }

                if (hasLand && hasWater)
                    return 1;
            }

            if (hasWater)
                return 2;
            else
                return 0;
        }        
    }
}
