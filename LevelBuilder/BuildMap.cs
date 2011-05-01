using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using GMap.NET;
using GMap.NET.WindowsForms;
using System.Drawing.Imaging;
using DotNetFish.GameObjects;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using Point = System.Drawing.Point;
using DotNetFish.GameObjects.Enums;
using System.Collections;

namespace DotNetFish.LevelBuilder
{
    public class BuildMap
    {
		//The default size of the tile we are checking from gmap. we are taking the 16x16 tile and replacing
		//it with a 64x64 tile. This allows us to get the size map we want. 
		const int _graphicsTileSize = 16;

		Random rnd = new Random();
        private BackgroundWorker _backgroundWorker;
        private List<PointLatLng> _points;
		private GameWorld _gameWorld;
		private MapGraphicsTileSet _mapGraphicsTileSet;
		private int _gameWorldWidth;
		private int _gameWorldHeight;
		Dictionary<Point,MapGraphicsTile> _edgeTileLocations;

        public BackgroundWorker BackgroundWorker
        {
            get
            {
                return _backgroundWorker;
            }
        }

        public GameWorld GameWorld
        {
			get
			{
				return _gameWorld;
			}
        }

        public BuildMap(List<PointLatLng> points)
        {
            _backgroundWorker = new System.ComponentModel.BackgroundWorker();
            _points = points;
			_mapGraphicsTileSet = new MapGraphicsTileSet(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\MapTiles.xml");
			_gameWorld = new GameWorld();
			
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += new DoWorkEventHandler(_backgroundWorker_DoWork);
        }	

        private void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)  
        {
			e.Cancel = Main();
			e.Result = _gameWorld;
        }

		/// <summary>
		/// Main function called by the background worker.
		/// </summary>
		/// <returns></returns>
		private bool Main()
		{
			//Gmap Setup stuff
			MapType type = MapType.GoogleMap;
			PureProjection prj = null;
			int maxZoom;
			int zoom = 22;
			GMaps.Instance.AdjustProjection(type, ref prj, out maxZoom);
			GMaps.Instance.Mode = AccessMode.ServerOnly;
			GMaps.Instance.ImageProxy = new WindowsFormsImageProxy();
			_edgeTileLocations = new Dictionary<Point, MapGraphicsTile>();

			//Convert the PointLatLng to GPoints
			List<GPoint> gPoints = new List<GPoint>();
			foreach (PointLatLng p in _points)
			{
				gPoints.Add(prj.FromPixelToTileXY(prj.FromLatLngToPixel(p.Lat, p.Lng, zoom)));
			}

			//Get the Start and End Tile. Start Tile must be upper left tile, and end tile must be lower right
			GPoint gmapStartTile = getGmapStartTile(gPoints);
			GPoint gmapEndTile = getGmapEndTile(gPoints);

			//The gmap tile stuff
			int gmapTilesHeight = gmapEndTile.Y - gmapStartTile.Y + 1;
			int gmapTilesWidth = gmapEndTile.X - gmapStartTile.X + 1;
			int gmapTilesProcessed = 0;

			_gameWorldWidth = gmapTilesWidth * 16;
			_gameWorldHeight = gmapTilesHeight * 16;
			_gameWorld.GameMap = new MapTile[_gameWorldWidth, _gameWorldHeight];

#if DEBUG
			if (!Directory.Exists("C:\\tiles"))
				Directory.CreateDirectory("C:\\tiles");

			foreach (string s in Directory.GetFiles("c:\\tiles"))
			{
				File.Delete(s);
			}
#endif

			//Loop through each tile and add it to the array         
			for (int x = 0; x < gmapTilesWidth; x++)
			{
				for (int y = 0; y < gmapTilesHeight; y++)
				{
					gmapTilesProcessed++;
					_backgroundWorker.ReportProgress(1, "ProcessingTile: " + gmapTilesProcessed + " of " + gmapTilesHeight * gmapTilesWidth);

					Exception ex;
					WindowsFormsImage tile = GMaps.Instance.GetImageFrom(type, new GPoint(gmapStartTile.X + x, gmapStartTile.Y + y), zoom, out ex) as WindowsFormsImage;

					if (ex != null)
					{						
						return true;
					}
					else if (tile != null)
					{
						using (tile)
						{
							using (Bitmap bitmap = new Bitmap(tile.Img))
							{
#if DEBUG
								bitmap.Save("C:\\tiles\\Largetile" + x + "-" + y + ".jpg");
#endif
								ProcessGmapTiles(x, y, bitmap);
							}
						}
					}
				}
			}
			ProcessEdgeTiles();
			return false;
		}

		/// <summary>
		/// Processes all of the Large Gmap Tiles
		/// </summary>
		/// <param name="gmapX"></param>
		/// <param name="gmapY"></param>
		/// <param name="gmapBitmap"></param>
		private void ProcessGmapTiles(int gmapX, int gmapY, Bitmap gmapBitmap)
		{	
			//The bitmap coming in as 256x256 This needs to be broken down further into 16x16 sized
			//tiles in order to get the proper size of the map
			for (int tileX = 0; tileX < _graphicsTileSize; tileX++)
			{
				for (int tileY = 0; tileY < _graphicsTileSize; tileY++)
				{
					using (Bitmap smallBmp = new Bitmap(_graphicsTileSize, _graphicsTileSize))
					{
						using (Graphics gfx = Graphics.FromImage(smallBmp))
							gfx.DrawImage(gmapBitmap,new Rectangle(0,0,16,16),tileX * _graphicsTileSize,tileY * _graphicsTileSize,16,16,GraphicsUnit.Pixel);
#if DEBUG
						smallBmp.Save("C:\\tiles\\smalltile" + ((gmapX * _graphicsTileSize) + tileX) + "-" + ((gmapY * _graphicsTileSize) + tileY) + ".jpg");
#endif
						BmpData bmpData = new BmpData(smallBmp, _graphicsTileSize);						
						ProcessTile(bmpData, (gmapX * _graphicsTileSize) + tileX, (gmapY * _graphicsTileSize) + tileY);
						bmpData.Dispose();
					}					
				}
			}
		}

		/// <summary>
		/// Processes all of the Tiles and determines if they are an edge or not. Edgetiles get added to the list, and the 
		/// other tiles get water / land added to them. 
		/// </summary>
		/// <param name="bmpData"></param>
		/// <param name="gameWorldX"></param>
		/// <param name="gameWorldY"></param>		
		private void ProcessTile(BmpData bmpData, int gameWorldX, int gameWorldY)
		{
			bool hasWater = false;
			bool hasLand = false;

			CheckEdges(bmpData, _graphicsTileSize, out hasWater, out hasLand);

			if (hasLand && hasWater)
			{
				//We generate the edge connections for each tile just like they are by themselves.
				//After all of the tiles are processed we will do a second pass to ensure everything is connected.
				MapGraphicsTile mapGraphicsTile = new MapGraphicsTile();
				_gameWorld.GameMap[gameWorldX, gameWorldY] = new MapTile(mapGraphicsTile);
				mapGraphicsTile.TileType = TileType.Edge;
				mapGraphicsTile.ShoreEdgePoints.AddRange(GenerateMapGraphicsTileEdgePoints(bmpData, _graphicsTileSize, mapGraphicsTile));

				_edgeTileLocations.Add(new Point(gameWorldX, gameWorldY), mapGraphicsTile);
			}
				
				
			else if (hasLand)
				_gameWorld.GameMap[gameWorldX, gameWorldY] = new MapTile(_mapGraphicsTileSet.LandTile);
			else
				_gameWorld.GameMap[gameWorldX, gameWorldY] = new MapTile(_mapGraphicsTileSet.WaterTile);

			//Need to release the locked bits here
			bmpData.Dispose();
		}

		/// <summary>
		/// Process all of the Edge Tiles
		/// </summary>
		/// <param name="edgeTilePoints"></param>
		/// <param name="bmpData"></param>
		private void ProcessEdgeTiles()
		{
			int count = 0;
			foreach (KeyValuePair<Point,MapGraphicsTile> kvp in _edgeTileLocations)
			{
				
				if (count % 20 == 0)
					_backgroundWorker.ReportProgress(1, "Processing EdgeTile: " + count + " of " + _edgeTileLocations.Count);
				count++;
				
				bool isMissingEdgeConnections = false;

				//Loop through each edgepoint and calculate a path.
				foreach (EdgeConnection edgeConnection in kvp.Value.ShoreEdgePoints)
				{

					if (!edgeConnection.IsConnected)
						edgeConnection.IsConnected = IsEdgeConnectionOnMapEdge(edgeConnection.EdgePosition,kvp.Key);

					if (edgeConnection.IsConnected)
						continue;

					if (!NeighborHasEdgeConnections(kvp, edgeConnection))
					{										
						isMissingEdgeConnections = true;
						break;
					}
					else
						edgeConnection.IsConnected = true;					
						
				}
				//If we are missing edge Connections then we need to Calculate the path for the missing connections.
				if (isMissingEdgeConnections) ;

					//CalculatePath(kvp);	
				else
				{
					SetTileEdgeTypeByNeighbor(kvp.Value, kvp.Key.X, kvp.Key.Y);
					_gameWorld.GameMap[kvp.Key.X, kvp.Key.Y] = _mapGraphicsTileSet.GetMatchingTile(kvp.Value);

				}
			}
		}

		private bool NeighborHasEdgeConnections(KeyValuePair<Point,MapGraphicsTile> mapGraphicsTile, EdgeConnection edgeConnection)
		{
			//CheckDown
			if (edgeConnection.EdgePosition > 0 &&
					edgeConnection.EdgePosition < 4 &&
					_edgeTileLocations.ContainsKey(new Point(mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y + 1)))
			{
				CheckNeighborForMatchingEdgePoint(_edgeTileLocations[new Point(mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y + 1)], edgeConnection);
				return true;
			}
			else if (edgeConnection.EdgePosition > 3 &&
						edgeConnection.EdgePosition < 7 &&
						_edgeTileLocations.ContainsKey(new Point(mapGraphicsTile.Key.X + 1, mapGraphicsTile.Key.Y)))
			{
				CheckNeighborForMatchingEdgePoint(_edgeTileLocations[new Point(mapGraphicsTile.Key.X + 1, mapGraphicsTile.Key.Y)], edgeConnection);
				return true;
			}
			else if (edgeConnection.EdgePosition > 6 &&
						edgeConnection.EdgePosition < 10 &&
						_edgeTileLocations.ContainsKey(new Point(mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y - 1)))
			{
				CheckNeighborForMatchingEdgePoint(_edgeTileLocations[new Point(mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y - 1)], edgeConnection);
				return true;
			}
			else if (edgeConnection.EdgePosition > 6 &&
						edgeConnection.EdgePosition < 10 &&
						_edgeTileLocations.ContainsKey(new Point(mapGraphicsTile.Key.X - 1, mapGraphicsTile.Key.Y)))
			{
				CheckNeighborForMatchingEdgePoint(_edgeTileLocations[new Point(mapGraphicsTile.Key.X - 1, mapGraphicsTile.Key.Y)], edgeConnection);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Checks an edge connection to ensure that it doesnt border the MapEdge. 
		/// If it does, then that edge point should not be processed. 
		/// </summary>
		/// <param name="edgeConnection"></param>
		/// <param name="tileLocation"></param>
		/// <returns></returns>
		private bool IsEdgeConnectionOnMapEdge(int edgeConnection, Point tileLocation)
		{
			if (tileLocation.Y == 0 && edgeConnection > 6 && edgeConnection < 10)
				return true;
			else if (tileLocation.Y == _gameWorld.GameMap.GetLength(1) && edgeConnection > 0 && edgeConnection < 4)
				return true;
			else if (tileLocation.X == 0 && edgeConnection > 9 && edgeConnection < 13)
				return true;
			else if (tileLocation.Y == _gameWorld.GameMap.GetLength(0) && edgeConnection > 3 && edgeConnection < 7)
				return true;

			return false;
		}		

		/// <summary>
		/// Calculates the best path available for tiles that need to be connected. 
		/// </summary>
		/// <param name="currentTile"></param>
		private void CalculatePath(KeyValuePair<Point,MapGraphicsTile> currentTile)
		{
			//if (CheckTileForLoop(currentTile)

			//First we need to get a list of tiles that we can build a path to. 
			List<Point> OpenTiles = (from tiles in _edgeTileLocations
									 where tiles.Key != currentTile.Key && tiles.Value.ShoreEdgePoints.Exists(i => i.IsConnected == false) &&
									 (tiles.Key.X > (currentTile.Key.X - 5) && tiles.Key.X < (currentTile.Key.X + 5)) && (tiles.Key.Y > (currentTile.Key.Y - 5) && tiles.Key.Y < (currentTile.Key.Y + 5))
									 select tiles.Key).ToList();

			PathTile bestPath = null;
			foreach (Point point in OpenTiles)
			{				
				PathTile pathTile = FindPath(currentTile.Key, point);

				if (pathTile != null && (bestPath == null || pathTile.Score < bestPath.Score))
					bestPath = pathTile;
			}

			if (bestPath != null)
				BuildPath(bestPath);
			else
				_gameWorld.GameMap[currentTile.Key.X,currentTile.Key.Y] = new MapTile(_mapGraphicsTileSet.ErrorTile);
		}

		private void BuildPath(PathTile bestPath)
		{
			PathTile currentPathTile = bestPath;
			EdgeConnection parentEdgeConnection = null;
			Dictionary<Point,MapGraphicsTile> tilesToAdd = new Dictionary<Point,MapGraphicsTile>();

			while (true)
			{
				MapGraphicsTile currentMapTile;
				//create a new mapTile
				if (_edgeTileLocations.ContainsKey(currentPathTile.TileLocation))
				{
					currentMapTile = _edgeTileLocations[currentPathTile.TileLocation];
					foreach (EdgeConnection edgeConnection in currentMapTile.ShoreEdgePoints)
						NeighborHasEdgeConnections(new KeyValuePair<Point, MapGraphicsTile>(currentPathTile.TileLocation, _edgeTileLocations[currentPathTile.TileLocation]), edgeConnection);

					//get rid of all of the edgepoints that are not a match, as we cannot used them. 		
					currentMapTile.ShoreEdgePoints.RemoveAll(i => i.IsConnected == false);							
				}
				else
					currentMapTile = new MapGraphicsTile();

				//add the previous parent translated to they connect
				if (parentEdgeConnection != null)
					currentMapTile.ShoreEdgePoints.Add(new EdgeConnection(parentEdgeConnection.TranslateConnectionForNeighbor(), true));

				//Get the parentEdgeConnection
				parentEdgeConnection = GetParentTileEdgeConnection(currentPathTile);
			
				if (parentEdgeConnection != null)
					currentMapTile.ShoreEdgePoints.Add(parentEdgeConnection);

				tilesToAdd.Add(currentPathTile.TileLocation, currentMapTile);

				//if the current tile doesnt have a parentTile we are done.
				if (currentPathTile.ParentTile == null)
					break;				
				currentPathTile = currentPathTile.ParentTile;
			}

			foreach (KeyValuePair<Point,MapGraphicsTile> kvp in tilesToAdd)
			{
				//Add the new tile to the map
				SetTileEdgeTypeByNeighbor(kvp.Value, currentPathTile.TileLocation.X, currentPathTile.TileLocation.Y);
				kvp.Value.SetTileEdgeTypeByEdgeConnections();
				_gameWorld.GameMap[kvp.Key.X,kvp.Key.Y] = _mapGraphicsTileSet.GetMatchingTile(kvp.Value);
			}
		}

		

		private EdgeConnection GetParentTileEdgeConnection(PathTile currentPathTile)
		{
			if (currentPathTile.ParentTile == null)
				return null;

			//ParentTile is Above Current Tile
			if (currentPathTile.ParentTile.TileLocation.Y < currentPathTile.TileLocation.Y)
				return new EdgeConnection((byte)rnd.Next(7, 9),true);
			//Parent Tile is below current tile
			else if (currentPathTile.ParentTile.TileLocation.Y > currentPathTile.TileLocation.Y)
				return new EdgeConnection((byte)rnd.Next(1, 3),true);
			//Parent tile is to the left of current tile
			else if (currentPathTile.ParentTile.TileLocation.X < currentPathTile.TileLocation.X)
				return new EdgeConnection((byte)rnd.Next(10, 12),true);
			//Parent tile is to the right of current tile
			else 
				return new EdgeConnection((byte)rnd.Next(4, 6));
		}

		private PathTile FindPath(Point startTilePoint, Point targetTilePoint)
		{
			PathTile currentTile = new PathTile(startTilePoint, 10);

			List<PathTile> openTiles = new List<PathTile>();
			List<PathTile> closedTiles = new List<PathTile>();

			while (true)
			{
				closedTiles.Add(currentTile);

				openTiles.AddRange(GetOpenTiles(currentTile, targetTilePoint, openTiles,closedTiles));

				if (openTiles.Count == 0)
					return null;

				openTiles.Sort();

				currentTile = openTiles[0];
				openTiles.RemoveAt(0);

				if (currentTile.TileLocation == targetTilePoint)
					return currentTile;
			}
		}

		private List<PathTile> GetOpenTiles(PathTile currentTile, Point targetTile, List<PathTile> openTiles, List<PathTile> closedTiles)
		{
			List<Point> openPoints = new List<Point>();

			//First check the gameworl to see if the cardinal directions are open
			//North Tile
			if (currentTile.TileLocation.Y - 1 >= 0 &&
				!closedTiles.Exists(i => i.TileLocation.X ==  currentTile.TileLocation.X && i.TileLocation.Y == currentTile.TileLocation.Y - 1) &&
				_gameWorld.GameMap[currentTile.TileLocation.X, currentTile.TileLocation.Y - 1].GraphicsTile.TileType != TileType.Edge ||
					(_edgeTileLocations.ContainsKey(new Point(currentTile.TileLocation.X,currentTile.TileLocation.Y - 1)) &&
						_edgeTileLocations[new Point(currentTile.TileLocation.X,currentTile.TileLocation.Y - 1)] .ShoreEdgePoints.Exists(i => i.IsConnected == false)))
				openPoints.Add(new Point(currentTile.TileLocation.X, currentTile.TileLocation.Y - 1));

			//south Tile
			if (currentTile.TileLocation.Y + 1 < _gameWorld.GameMap.GetLength(1) &&
				!closedTiles.Exists(i => i.TileLocation.X == currentTile.TileLocation.X && i.TileLocation.Y == currentTile.TileLocation.Y + 1) &&
				_gameWorld.GameMap[currentTile.TileLocation.X, currentTile.TileLocation.Y + 1].GraphicsTile.TileType != TileType.Edge ||
					(_edgeTileLocations.ContainsKey(new Point(currentTile.TileLocation.X,currentTile.TileLocation.Y + 1)) &&
						_edgeTileLocations[new Point(currentTile.TileLocation.X,currentTile.TileLocation.Y + 1)] .ShoreEdgePoints.Exists(i => i.IsConnected == false)))
				openPoints.Add(new Point(currentTile.TileLocation.X, currentTile.TileLocation.Y + 1));

			//East Tile
			if (currentTile.TileLocation.X - 1 >= 0 &&
				!closedTiles.Exists(i => i.TileLocation.X ==  currentTile.TileLocation.X - 1 && i.TileLocation.Y == currentTile.TileLocation.Y) &&
				_gameWorld.GameMap[currentTile.TileLocation.X - 1, currentTile.TileLocation.Y].GraphicsTile.TileType != TileType.Edge ||
					(_edgeTileLocations.ContainsKey(new Point(currentTile.TileLocation.X - 1,currentTile.TileLocation.Y)) &&
						_edgeTileLocations[new Point(currentTile.TileLocation.X - 1,currentTile.TileLocation.Y)] .ShoreEdgePoints.Exists(i => i.IsConnected == false)))
				openPoints.Add(new Point(currentTile.TileLocation.X - 1, currentTile.TileLocation.Y));

			//West Tile
			if (currentTile.TileLocation.X + 1 < _gameWorld.GameMap.GetLength(0) &&
				!closedTiles.Exists(i => i.TileLocation.X ==  currentTile.TileLocation.X + 1 && i.TileLocation.Y == currentTile.TileLocation.Y) &&
				_gameWorld.GameMap[currentTile.TileLocation.X + 1, currentTile.TileLocation.Y].GraphicsTile.TileType != TileType.Edge ||
					(_edgeTileLocations.ContainsKey(new Point(currentTile.TileLocation.X + 1,currentTile.TileLocation.Y)) &&
						_edgeTileLocations[new Point(currentTile.TileLocation.X + 1,currentTile.TileLocation.Y)] .ShoreEdgePoints.Exists(i => i.IsConnected == false)))
				openPoints.Add(new Point(currentTile.TileLocation.X + 1, currentTile.TileLocation.Y));

			List<PathTile> tiles = new List<PathTile>();
			foreach (Point location in openPoints)
			{
				int score = CalculateScore(currentTile.ParentTile, location, targetTile);

				//If it doesnt exist on the open list we want to create a new item.
				if (!openTiles.Exists(i => i.TileLocation.X == location.X && i.TileLocation.Y == location.Y))
				{					
					PathTile potentialPathTile = new PathTile(location, score, currentTile);
					tiles.Add(potentialPathTile);
				}
				else
				{
					PathTile existingTile = openTiles.First(i => i.TileLocation.X == location.X && i.TileLocation.Y == location.Y);
					existingTile.ParentTile = currentTile;
					existingTile.Score = score;
				}
				
			}
			return tiles;
		}

		private int CalculateScore(PathTile parentTile, Point location, Point targetTile)
		{
			//Path Score F = G + H
			//G = the Cost to move plus the Parents cost to mvoe
			//H = the estimated cost to the targetTile
			//Using the Manhatten method to calculate the H oost

			int G = 10;
			
			if (parentTile != null)
				G+=parentTile.Score;
			
			int H = 0;

			if (location.X >= targetTile.X)
				H += location.X - targetTile.X;
			else
				H+= targetTile.X - location.X;

			if (location.Y >= targetTile.Y)
				H += location.Y - targetTile.Y;
			else
				H+= targetTile.Y - location.Y;

			return G + H;

		}	

		/// <summary>
		/// Determines if the Neighbord has matching EdgePoint. If it does, we set the current edgeConnection 
		/// to the edge connection of the neighbor and set them both to connected. 
		/// </summary>
		/// <param name="neighborTile"></param>
		/// <param name="edgeConnection"></param>
		/// <returns></returns>
		private bool CheckNeighborForMatchingEdgePoint(MapGraphicsTile neighborTile, EdgeConnection edgeConnection)
		{		
			List<EdgeConnection> availableConnections  = new List<EdgeConnection>();
		
			switch (edgeConnection.EdgePosition)
			{
				case 1:
				case 2:
				case 3:	
					//Bottom Side
						availableConnections.AddRange(
							neighborTile.ShoreEdgePoints.FindAll(
								instance => instance.EdgePosition > 6 && instance.EdgePosition < 10));					
					break;
				case 4:
				case 5:
				case 6:
					//Right Side
						availableConnections.AddRange(
							neighborTile.ShoreEdgePoints.FindAll(
								instance => instance.EdgePosition > 9 && instance.EdgePosition < 13));
					
					break;
				case 7:
				case 8:
				case 9:
					//Top Side					
						availableConnections.AddRange(
							neighborTile.ShoreEdgePoints.FindAll(
								instance => instance.EdgePosition > 0 && instance.EdgePosition < 4 && !instance.IsConnected));
					
					break;
				case 10:
				case 11:
				case 12:
					//Left Side
						availableConnections.AddRange(
							neighborTile.ShoreEdgePoints.FindAll(
								instance => instance.EdgePosition > 3 && instance.EdgePosition < 7));					
					break;
			}

			if (availableConnections.Count > 0)
			{
				//Get the closest match
				byte closestMatch = GetClosestMatchingEdgeConnection(edgeConnection, availableConnections);
				//set the current edgeconnection to the closestMatch and it is now connected
				edgeConnection.EdgePosition = closestMatch;		
				edgeConnection.IsConnected = true;
				//set the neighbortile edge point to connected as well. 
				EdgeConnection neighborConnection = neighborTile.ShoreEdgePoints.Find(instance => instance.TranslateConnectionForNeighbor() == closestMatch);
				if (neighborConnection != null)
				{
					neighborConnection.IsConnected = true;
				}
				else
				{
					int i = 0;
					i++;
				}
				return true;
			}
			//No Matches were found on the adjacent tile
			return false;
		}

		/// <summary>
		/// If the two edges do not match 1 to 1, We find the closest tile on that edge. 
		/// </summary>
		/// <param name="edgeConnection"></param>
		/// <param name="availableConnections"></param>
		/// <returns></returns>
		private byte GetClosestMatchingEdgeConnection(EdgeConnection edgeConnection, List<EdgeConnection> availableConnections)
		{
			if (availableConnections.Exists(instance => instance.TranslateConnectionForNeighbor() == edgeConnection.EdgePosition))
				return edgeConnection.EdgePosition;

			if (availableConnections.Count == 1)
				return availableConnections[0].TranslateConnectionForNeighbor();

			for (int i = 0; i < 3; i++)
			{
				EdgeConnection matchConnection = null;

				matchConnection = availableConnections.Find(instance => instance.TranslateConnectionForNeighbor() == edgeConnection.EdgePosition + i);
				
				if (matchConnection != null)
					return matchConnection.TranslateConnectionForNeighbor();

				matchConnection = availableConnections.Find(instance => instance.TranslateConnectionForNeighbor() == edgeConnection.EdgePosition - i);
					
				if (matchConnection != null)
					return matchConnection.TranslateConnectionForNeighbor();
			}
			throw new ArgumentNullException("No Match Found");
		}

		/// <summary>
		/// Sets the Edge of the tile based on the neighbors edge
		/// </summary>
		/// <param name="mapGraphicsTile"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		private MapGraphicsTile SetTileEdgeTypeByNeighbor(MapGraphicsTile mapGraphicsTile, int x, int y)
		{
			//Left Side
			if (x - 1 > 0 && _gameWorld.GameMap[x - 1, y] != null && _gameWorld.GameMap[x - 1, y].GraphicsTile != null)
				mapGraphicsTile.LeftEdgeType = _gameWorld.GameMap[x - 1, y].GraphicsTile.RightEdgeType;

			//Top Side
			if (y - 1 > 0 && _gameWorld.GameMap[x, y - 1] != null && _gameWorld.GameMap[x, y - 1].GraphicsTile != null)
				mapGraphicsTile.TopEdgeType = _gameWorld.GameMap[x, y - 1].GraphicsTile.BottomEdgeType;

			//right Side
			if (x + 1 < _gameWorldWidth && _gameWorld.GameMap[x + 1, y] != null && _gameWorld.GameMap[x + 1, y].GraphicsTile != null)
				mapGraphicsTile.RightEdgeType = _gameWorld.GameMap[x + 1, y].GraphicsTile.LeftEdgeType;

			//Bottom Side
			if (y + 1 < _gameWorldHeight && _gameWorld.GameMap[x, y + 1] != null && _gameWorld.GameMap[x, y + 1].GraphicsTile != null)
				mapGraphicsTile.BottomEdgeType = _gameWorld.GameMap[x, y + 1].GraphicsTile.TopEdgeType;

			return mapGraphicsTile;
		}	


		/// <summary>
		/// Checks the Edges of a Tile to determine if the tile contains land,water or both
		/// </summary>
		/// <param name="bmpData"></param>
		/// <param name="tileSize"></param>
		/// <param name="hasWater"></param>
		/// <param name="hasLand"></param>
		private void CheckEdges(BmpData bmpData, int tileSize, out bool hasWater, out bool hasLand)
		{
			hasWater = false;
			hasLand = false;
			bool retval;
			//Loop though all of the pixels on the Y edge
			for (int y = 0; y < tileSize; y++)
			{
				for (int x = 0; x < tileSize; x++)
				{
					int position = (y * bmpData.BitmapData.Stride) + (x * 4);

					Color color = SetColor(bmpData.RgbValues[position], bmpData.RgbValues[position + 1], bmpData.RgbValues[position + 2]);
					retval = IsWater(color);

					if (retval == true)
						hasWater = true;
					else
						hasLand = true;

					if (hasLand && hasWater)
						break;

					//only check the left and right edges.
					if (y != 0 && y != tileSize - 1)
						x+= tileSize - 2;
				}
				if (hasLand && hasWater)
					return;
			}
		}

		/// <summary>
		/// Generates the Edge Points for a MapGraphics Tile
		/// </summary>
		/// <param name="bmpData"></param>
		/// <param name="tileSize"></param>
		/// <param name="currentGraphicsTile"></param>
		/// <returns></returns>
		private List<EdgeConnection> GenerateMapGraphicsTileEdgePoints(BmpData bmpData, int tileSize, MapGraphicsTile currentGraphicsTile)
		{			
			List<Point> edgePoints = new List<Point>();
			//Loop though all of the pixels on the Y edge
			for (int y = 0; y < tileSize; y+=tileSize - 1)
			{
				if (y ==0 && currentGraphicsTile.ShoreEdgePoints.Count(instance => instance.EdgePosition > 6 && instance.EdgePosition < 10) > 0)
					continue;
				if (y != 0 && currentGraphicsTile.ShoreEdgePoints.Count(instance => instance.EdgePosition > 0 && instance.EdgePosition < 4) > 0)
					continue;

				//Using these two values to determine if the edge is water, land, or both
				bool hasLand = false;
				bool hasWater = false;

				Color newColor = new Color();
				Color previousColor = new Color();
				for (int x = 0; x < tileSize; x++)
				{
					int position = (y * bmpData.BitmapData.Stride) + (x * 4);
					newColor = SetColor(bmpData.RgbValues[position], bmpData.RgbValues[position + 1], bmpData.RgbValues[position + 2]);

					if (!hasLand && !IsWater(newColor))
						hasLand = true;

					if (!hasWater && IsWater(newColor))
						hasWater = true;

					if (previousColor != new Color() && IsLandWaterTransition(newColor,previousColor))
					{
						//If the point is on a corner of the tile, we want to move
						//it off the corner so the correct Tile Edge is selected. 
						if (x == 15)
							edgePoints.Add(new Point(x -1, y));						
						else
							edgePoints.Add(new Point(x, y));
					}
					previousColor = newColor;
				}

				if (y == 0)
					currentGraphicsTile.TopEdgeType = GetTileEdgeType(hasLand, hasWater);
				else
					currentGraphicsTile.BottomEdgeType = GetTileEdgeType(hasLand, hasWater);

			}
			
			//Loop though all of the pixels on the X edge
			for (int x = 0; x < tileSize; x+=tileSize - 1)
			{
				if (x != 0 && currentGraphicsTile.ShoreEdgePoints.Count(instance => instance.EdgePosition > 3 && instance.EdgePosition < 7) > 0)
					continue;
				if (x == 0 && currentGraphicsTile.ShoreEdgePoints.Count(instance => instance.EdgePosition > 9 && instance.EdgePosition < 13) > 0)
					continue;

				//Using these two values to determine if the edge is water, land, or both
				bool hasLand = false;
				bool hasWater = false;

				Color newColor = new Color();
				Color previousColor = new Color();				

				//Starting and ending the count early. Because we dont want to check the first and
				//last positions twice.
				for (int y = 0; y < tileSize; y++)
				{
					int position = (y * bmpData.BitmapData.Stride) + (x * 4);

					newColor = SetColor(bmpData.RgbValues[position], bmpData.RgbValues[position + 1], bmpData.RgbValues[position + 2]);

					if (!hasLand && !IsWater(newColor))
						hasLand = true;

					if (!hasWater && IsWater(newColor))
						hasWater = true;

					if (previousColor != new Color() && IsLandWaterTransition(newColor, previousColor))
					{
						//If the point is on a corner of the tile, we want to move
						//it off the corner so the correct Tile Edge is selected. 
						if (y == 15)
							edgePoints.Add(new Point(x, y - 1));						
						else
							edgePoints.Add(new Point(x, y));
					}
					previousColor = newColor;
				}

				if (x == 0)
					currentGraphicsTile.LeftEdgeType = GetTileEdgeType(hasLand, hasWater);
				else
					currentGraphicsTile.RightEdgeType = GetTileEdgeType(hasLand, hasWater);
			}	
			return ConvertEdgeCoordinatesToEdgePoints(edgePoints);
		}

		/// <summary>
		/// Returns the EdgeType of a tile
		/// </summary>
		/// <param name="hasLand"></param>
		/// <param name="hasWater"></param>
		/// <returns></returns>
		private EdgeType GetTileEdgeType(bool hasLand, bool hasWater)
		{			

			if (hasLand && hasWater)
				return EdgeType.Both;
			else if (hasLand)
				return EdgeType.Land;
			else if (hasWater)
				return EdgeType.Water;
			else
				throw new ArgumentOutOfRangeException("tile Doesn't have land or water");
		}

		/// <summary>
		/// Determines if two colors are a land to water transition
		/// </summary>
		/// <param name="newColor"></param>
		/// <param name="previousColor"></param>
		/// <returns></returns>
		private bool IsLandWaterTransition(Color newColor, Color previousColor)
		{
			bool isNewColorWater = IsWater(newColor);
			bool isPreviousColorWater = IsWater(previousColor);

			if (isNewColorWater != isPreviousColorWater)
				return true;
			else
				return false;

		}

		/// <summary>
		/// Determines if a Color is water based on the color
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		private bool IsWater(Color color)
		{		
			if (color.R == 153 && color.B == 204 && color.G == 179 ||
				color.R == 175 && color.B == 207 && color.G == 189||
				color.R == 196 && color.B == 210 && color.G == 204||
				color.R == 217 && color.B == 217 && color.G == 217)
				return true;
			else
				return false;
		}

		/// <summary>
		/// Returns a color based on the RBG values
		/// </summary>
		/// <param name="blue"></param>
		/// <param name="green"></param>
		/// <param name="red"></param>
		/// <returns></returns>
		private  Color SetColor(byte blue, byte green, byte red)
		{
			return Color.FromArgb(255,red,green,blue);	
		}

		/// <summary>
		/// Converts the X,Y coordinate where there is a shift from land to water into an EdgePoint
		/// </summary>
		/// <param name="edgePoints"></param>
		/// <returns></returns>
		private List<EdgeConnection> ConvertEdgeCoordinatesToEdgePoints(List<Point> edgePoints)
		{
			List<EdgeConnection> tileEdgeConnections = new List<EdgeConnection>();

			foreach (Point p in edgePoints)
			{
				if (p.X == 0)
				{
					if (p.Y <= 4)
						tileEdgeConnections.Add(new EdgeConnection(10));
					else if (p.Y > 4 && p.Y <= 9)
						tileEdgeConnections.Add(new EdgeConnection(11));
					else
						tileEdgeConnections.Add(new EdgeConnection(12));
				}
				else if (p.X == 15)
				{
					if (p.Y <= 4)
						tileEdgeConnections.Add(new EdgeConnection(6));
					else if (p.Y > 4 && p.Y <= 9)
						tileEdgeConnections.Add(new EdgeConnection(5));
					else
						tileEdgeConnections.Add(new EdgeConnection(4));
				}
				else if (p.Y == 0)
				{
					if (p.X <= 4)
						tileEdgeConnections.Add(new EdgeConnection(9));
					else if (p.X > 4 && p.X <= 9)
						tileEdgeConnections.Add(new EdgeConnection(8));
					else
						tileEdgeConnections.Add(new EdgeConnection(7));
				}
				else if (p.Y == 15)
				{
					if (p.X <= 4)
						tileEdgeConnections.Add(new EdgeConnection(1));
					else if (p.X > 4 && p.X <= 9)
						tileEdgeConnections.Add(new EdgeConnection(2));
					else
						tileEdgeConnections.Add(new EdgeConnection(3));
				}				
			}

			return tileEdgeConnections;
		} 
            
   		/// <summary>
   		/// Gets to End Gmap Tile (Bottom Right)
   		/// </summary>
   		/// <param name="points"></param>
   		/// <returns></returns>
        private GPoint getGmapEndTile(List<GPoint> points)
        {
            int x = int.MinValue;
            int y = int.MinValue;

            foreach (GPoint p in points)
            {
                x = p.X > x ? p.X : x;
                y = p.Y > y ? p.Y : y;
            }
            return new GPoint(x, y);
        }

        /// <summary>
        /// Gets the start tile of the selected region. Top left
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private GPoint getGmapStartTile(List<GPoint> points)
        {
            int x = int.MaxValue;
            int y = int.MaxValue;

            foreach (GPoint p in points)
            {
                x = p.X < x ? p.X : x;
                y = p.Y < y ? p.Y : y;
            }
            return new GPoint(x, y);
        }
	}
}
