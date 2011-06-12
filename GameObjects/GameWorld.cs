using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Drawing;

namespace DotNetFish.GameObjects
{
	/// <summary>
	/// Holds all of the inforamation about the Gameworld itself. 
	/// </summary>
	[Serializable()]
    public class GameWorld : ISerializable
    {
        private int _gameWorldWidth;
        private int _gameWorldHeight;
        public MapTile[,] GameMap { get; set; } 

        public string Name { get; set; }

        public string Location { get; set; }

        public string Description { get; set; }

		public List<Point> ErrorTiles { get; set; }

        public int GameMapWidth
        {
            get
            {
                if (GameMap != null && _gameWorldWidth == 0)
                    _gameWorldWidth = GameMap.GetLength(0);

                return _gameWorldWidth;
            }
            
        }

        public int GameMapHeight
        {
            get
            {
                if (GameMap != null && _gameWorldHeight == 0)
                    _gameWorldHeight = GameMap.GetLength(1);

                return _gameWorldHeight;
            }
        }

		public GameWorld()
		{
			ErrorTiles = new List<Point>();
		}

		public GameWorld(SerializationInfo info, StreamingContext context)
		{
			Name = info.GetString("Name");
			Description = info.GetString("Location");
			GameMap = (MapTile[,])info.GetValue("GameMap", typeof(MapTile[,]));
			ErrorTiles = (List<Point>)info.GetValue("ErrorTiles", typeof(List<Point>));
		}
		
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Name", Name);
			info.AddValue("Location", Location);
			info.AddValue("Description", Description);
			info.AddValue("GameMap", GameMap);
			info.AddValue("ErrorTiles", ErrorTiles);
		}

        public void CreateTileLists()
        {
            for (int x = 0; x < GameMapWidth; x++)
            {
                for (int y = 0; y < GameMapHeight; y++)
                {
                    if (GameMap[x, y].GraphicsTile.TileType == Enums.TileType.Error)
                    {
                        ErrorTiles.Add(new Point(x, y));
                        continue;
                    }
                    else if (GameMap[x, y].GraphicsTile.TileType == Enums.TileType.Edge)
                    {
                        int numberOfEdgeConnections = 0;

                        //Check Top Tile
                        if (x > 0 && GameMap[x - 1, y].GraphicsTile.TileType == Enums.TileType.Edge)
                            numberOfEdgeConnections++;

                        //Check Left TIle
                        if (y > 0 && GameMap[x, y - 1].GraphicsTile.TileType == Enums.TileType.Edge)
                            numberOfEdgeConnections++;
                        if (x < GameMapWidth - 1 && GameMap[x + 1, y].GraphicsTile.TileType == Enums.TileType.Edge)
                            numberOfEdgeConnections++;

                        if (y < GameMapHeight - 1 && GameMap[x, y + 1].GraphicsTile.TileType == Enums.TileType.Edge)
                            numberOfEdgeConnections++;

                        //Add the tile that doesnt have 2 edge connections
                        if (numberOfEdgeConnections != 2)
                            ErrorTiles.Add(new Point(x, y));
                    }                    
                }
            }
        }  
    }
}
