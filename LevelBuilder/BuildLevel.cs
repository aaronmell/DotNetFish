using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GameObjects;
using System.Diagnostics;
using System.IO;

namespace TileBuilder
{
    public class BuildLevel
    {
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
                        if (row.Count() > 0)
                        {
                            Point startPoint = new Point(rowCount * 64, columnCount * 64);
                            int edge1 = int.Parse(row[c]);
                            int edge2 = int.Parse(row[c + 1]);

                            tileMap.Add(startPoint, new GraphicTile { TileStartPoint = startPoint, ShoreEdgePoints = new List<int> { edge1, edge2 } });
                        }
                    }
                    rowCount++;
                }
            }
            return tileMap;
        }            
    }
}
