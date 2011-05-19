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
        const MapType _mapType = MapType.GoogleMap;

		Random rnd = new Random();
        private BackgroundWorker _backgroundWorker;
        private List<PointLatLng> _points;
		private GameWorld _gameWorld;
		private MapGraphicsTileSet _mapGraphicsTileSet;
		private int _gameWorldWidth;
		private int _gameWorldHeight;
        private int _gmapTilesProcessed;
		Dictionary<Point,MapGraphicsTile> _edgeTiles;
		Dictionary<Point, MapGraphicsTile> _errorTiles;
        Dictionary<Point, MapGraphicsTile> _noNeighborTiles;
       

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
			int zoom = 18;
			GMaps.Instance.AdjustProjection(type, ref prj, out maxZoom);
			GMaps.Instance.Mode = AccessMode.ServerOnly;
			GMaps.Instance.ImageProxy = new WindowsFormsImageProxy();

			_edgeTiles = new Dictionary<Point, MapGraphicsTile>();
            _noNeighborTiles = new Dictionary<Point, MapGraphicsTile>();
			_errorTiles = new Dictionary<Point, MapGraphicsTile>();
            _gmapTilesProcessed = 0;

			//Convert the PointLatLng to GPoints
			List<GPoint> gPoints = new List<GPoint>();
			foreach (PointLatLng p in _points)
			{
				gPoints.Add(prj.FromPixelToTileXY(prj.FromLatLngToPixel(p.Lat, p.Lng, zoom)));
			}

			//Get the Start and End Tile. Start Tile must be upper left tile, and end tile must be lower right
			GPoint gmapStartTile = getGmapStartTile(gPoints);
			GPoint gmapEndTile = getGmapEndTile(gPoints);

            _gameWorldWidth = (gmapEndTile.X - gmapStartTile.X + 1) * 16 * 16;
            _gameWorldHeight = (gmapEndTile.Y - gmapStartTile.Y + 1) * 16 * 16;
			_gameWorld.GameMap = new MapTile[_gameWorldWidth, _gameWorldHeight];

#if DEBUG
			if (!Directory.Exists("C:\\tiles"))
				Directory.CreateDirectory("C:\\tiles");

			foreach (string s in Directory.GetFiles("c:\\tiles"))
			{
				File.Delete(s);
			}

            if (LoopThroughTiles(gmapStartTile,gmapEndTile,new Point(0,0),18))
                return true;
#endif
            			
			ProcessEdgeTiles(_edgeTiles);
			return false;
		}

        //Recursive Function
        private bool LoopThroughTiles(GPoint gmapStartTile, GPoint gmapEndTile ,Point absolutePosition, int zoomLevel)
        {
            int currentOffset = getOffSet(zoomLevel);

            //Loop through each tile and add it to the array         
            for (int x = 0; x < gmapEndTile.X - gmapStartTile.X + 1; x++)
            {
                for (int y = 0; y <  gmapEndTile.Y - gmapStartTile.Y + 1; y++)
                {
                    if (zoomLevel == 18)
                    {
						_gmapTilesProcessed++;
                        _backgroundWorker.ReportProgress(1, "ProcessingTile: " + _gmapTilesProcessed + " of " + (gmapEndTile.X - gmapStartTile.X + 1) * (gmapEndTile.Y - gmapStartTile.Y + 1));
                    }

					Point currentAbsolutePosition = new Point(absolutePosition.X + (currentOffset * x), absolutePosition.Y + (currentOffset * y));

                    Exception ex;
                    WindowsFormsImage tile = GMaps.Instance.GetImageFrom(_mapType, new GPoint(gmapStartTile.X + x, gmapStartTile.Y + y), zoomLevel, out ex) as WindowsFormsImage;

                    if (ex != null)
                        return true;
                    else if (tile != null)
                    {
                        using (tile)
                        {
                            using (Bitmap bitmap = new Bitmap(tile.Img))
                            {
#if DEBUG
                                //bitmap.Save("C:\\tiles\\Largetile" + currentAbsolutePosition.X + "-" + currentAbsolutePosition.Y + "ZoomLevel" + zoomLevel + "Gmap Coordinates " + gmapStartTile.X + x + "-" + gmapStartTile.Y + y + ".jpg");

#endif
                                bool hasTransition = CheckGmapTilesForTransitions(x, y, bitmap, zoomLevel, currentAbsolutePosition);

                                if (!hasTransition)
                                    continue;
                                else
                                {
                                    if (zoomLevel != 22)
                                    {
                                        GPoint newStartTile = new GPoint((gmapStartTile.X + x)  * 2, (gmapStartTile.Y + y) * 2);
                                        GPoint newEndTile = new GPoint(newStartTile.X + 1, newStartTile.Y + 1);
										LoopThroughTiles(newStartTile, newEndTile, currentAbsolutePosition, zoomLevel + 1);
                                    }                                        
                                    else
                                        ProcessGmapTiles(currentAbsolutePosition.X, currentAbsolutePosition.Y, bitmap);
                                }
                                
                            }
                        }
                    }
                }
            }

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
//#if DEBUG
						//smallBmp.Save("C:\\tiles\\smalltile" + ((gmapX * _graphicsTileSize) + tileX) + "-" + ((gmapY * _graphicsTileSize) + tileY) + ".jpg");
//#endif
						BmpData bmpData = new BmpData(smallBmp, _graphicsTileSize);						
						
						ProcessTile(bmpData, (gmapX * _graphicsTileSize) + tileX, (gmapY * _graphicsTileSize) + tileY);
						bmpData.Dispose();
					}					
				}
			}
		}

        /// <summary>
        /// Detemines if a GmapTile contains a transition in it. 
        /// </summary>
        /// <param name="gmapX"></param>
        /// <param name="gmapY"></param>
        /// <param name="gmapBitmap"></param>
        /// <returns></returns>
        private bool CheckGmapTilesForTransitions(int gmapX, int gmapY, Bitmap gmapBitmap, int zoomLevel, Point currentAbsolutePosition)
        {
            int offset = getOffSet(zoomLevel);
            bool hasWater = false;
            bool hasLand = false;
            using (BmpData largeBmpData = new BmpData(gmapBitmap, 256))
                CheckEdges(largeBmpData, 256, out hasWater, out hasLand);

            //if we have land and water on the tile, then we need to Process it normally, otherwise we can skip all of that. 
            if (hasLand && hasWater)
                return true;

            MapGraphicsTile mapGraphicsTile = new MapGraphicsTile();           

            if (hasLand)
                 mapGraphicsTile= _mapGraphicsTileSet.LandTile;
            else
                mapGraphicsTile= _mapGraphicsTileSet.WaterTile;

                MapTile mapTile = new MapTile(mapGraphicsTile);

            for (int x = currentAbsolutePosition.X * _graphicsTileSize; x < (currentAbsolutePosition.X * _graphicsTileSize) + (_graphicsTileSize * offset); x++)
            {
                for (int y = currentAbsolutePosition.Y * _graphicsTileSize; y < (currentAbsolutePosition.Y * _graphicsTileSize) + (_graphicsTileSize * offset); y++)
                {
                    _gameWorld.GameMap[x,y] = mapTile;					
                }
            }

            return false;
        }

        private int getOffSet(int zoomLevel)
        {
            if (zoomLevel == 18)
                return 16;
            else if (zoomLevel == 19)
                return 8;
            else if (zoomLevel == 20)
                return 4;
            else if (zoomLevel == 21)
                return 2;
            else if (zoomLevel == 22)
                return 1;

            throw new Exception("Incorrect Zoom Level");
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
				_edgeTiles.Add(new Point(gameWorldX, gameWorldY), mapGraphicsTile);
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
		private void ProcessEdgeTiles(Dictionary<Point,MapGraphicsTile> tilesToProcess)
		{
			int count = 0;
            foreach (KeyValuePair<Point, MapGraphicsTile> mapGraphicsTile in tilesToProcess)
            {
                if (count % 20 == 0)
                    _backgroundWorker.ReportProgress(1, "Processing EdgeTile: " + count + " of " + _edgeTiles.Count);
                count++;

                //First Set the neighborConnections on the tile.
                CheckNeighborsForEdgeConnections(mapGraphicsTile);
                SetTileEdgeTypeByNeighbor(mapGraphicsTile);

                //If a tile has less than two connections, then we will add it to the list of non-neighborTiles
                if (NumberOfSidesWithConnections(mapGraphicsTile.Value) < 2)
                {
                    _noNeighborTiles.Add(mapGraphicsTile.Key, mapGraphicsTile.Value);
                }
                else
                {
                    //Add the mapTile to the GameWorld
                    MapTile mapTile = _mapGraphicsTileSet.GetMatchingTile(mapGraphicsTile.Value);
                    _gameWorld.GameMap[mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y] = mapTile;
                }

            }

            foreach (KeyValuePair<Point, MapGraphicsTile> mapGraphicsTile in _noNeighborTiles)
            {
                if (NumberOfSidesWithConnections(mapGraphicsTile.Value) < 2)
                {
                    CalculateTilePath(mapGraphicsTile);
                }                
            }
		}

        /// <summary>
        /// Determines how Connections with other tiles a side has. 
        /// </summary>
        /// <param name="mapGraphicsTile"></param>
        /// <returns></returns>
        private int NumberOfSidesWithConnections(MapGraphicsTile mapGraphicsTile)
        {
            int count = 0;

            if (mapGraphicsTile.BottomEdgeType == EdgeType.Both)
                count++;
            if  (mapGraphicsTile.LeftEdgeType == EdgeType.Both)
                count++;
            if (mapGraphicsTile.RightEdgeType == EdgeType.Both)
                count++;
            if (mapGraphicsTile.TopEdgeType == EdgeType.Both) 
                count++;

            return count;           
        }

		/// <summary>
		/// Check each direction for EdgeConnection. If it has an edge Connection then set it
		/// </summary>
		/// <param name="mapGraphicsTile"></param>
		private void CheckNeighborsForEdgeConnections(KeyValuePair<Point, MapGraphicsTile> mapGraphicsTile)
		{
			//CheckDown
            if (_edgeTiles.ContainsKey(new Point(mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y + 1)))
                GetNeighborsEdgeConnection(mapGraphicsTile.Value, _edgeTiles[new Point(mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y + 1)], EdgeDirection.Bottom);
            else if (mapGraphicsTile.Key.Y == _gameWorld.GameMap.GetLength(1) - 1)
            {
                mapGraphicsTile.Value.ShoreEdgePoints.Add(new EdgeConnection(CreateEdgeConnection(EdgeDirection.Bottom), true));
                mapGraphicsTile.Value.BottomEdgeType = EdgeType.Both;
            }
                
			//Check Up
			if (_edgeTiles.ContainsKey(new Point(mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y - 1)))
				GetNeighborsEdgeConnection(mapGraphicsTile.Value,_edgeTiles[new Point(mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y - 1)],EdgeDirection.Top);
            else if (mapGraphicsTile.Key.Y == 0)
            {
                mapGraphicsTile.Value.ShoreEdgePoints.Add(new EdgeConnection(CreateEdgeConnection(EdgeDirection.Top), true));
                mapGraphicsTile.Value.TopEdgeType = EdgeType.Both;
            }
			//Check Left
			if (_edgeTiles.ContainsKey(new Point(mapGraphicsTile.Key.X - 1, mapGraphicsTile.Key.Y)))
				GetNeighborsEdgeConnection(mapGraphicsTile.Value,_edgeTiles[new Point(mapGraphicsTile.Key.X - 1, mapGraphicsTile.Key.Y)],EdgeDirection.Left);
            else if (mapGraphicsTile.Key.X == 0)
            {
                mapGraphicsTile.Value.ShoreEdgePoints.Add(new EdgeConnection(CreateEdgeConnection(EdgeDirection.Left), true));
                mapGraphicsTile.Value.LeftEdgeType = EdgeType.Both;
            }
			//Check Right
			if (_edgeTiles.ContainsKey(new Point(mapGraphicsTile.Key.X + 1, mapGraphicsTile.Key.Y)))
				GetNeighborsEdgeConnection(mapGraphicsTile.Value, _edgeTiles[new Point(mapGraphicsTile.Key.X + 1, mapGraphicsTile.Key.Y)],EdgeDirection.Right);
            else if (mapGraphicsTile.Key.X == _gameWorld.GameMap.GetLength(0) - 1)
            {
                mapGraphicsTile.Value.ShoreEdgePoints.Add(new EdgeConnection(CreateEdgeConnection(EdgeDirection.Right), true));
                mapGraphicsTile.Value.RightEdgeType = EdgeType.Both;
            }
		}

        /// <summary>
        ///  Creates a random edge connection
        /// </summary>
        /// <param name="mapGraphicsTile"></param>
        /// <param name="edgeDirection"></param>
        private byte CreateEdgeConnection(EdgeDirection edgeDirection)
        {
            int min = 0;
            int max = 0;

            if (edgeDirection == EdgeDirection.Bottom)
            {
                min = 1;
                max = 3;
            }
            else if (edgeDirection == EdgeDirection.Top)
            {
                min = 7;
                max = 9;
            }
            else if (edgeDirection == EdgeDirection.Right)
            {
                min = 4;
                max = 6;
            }
            else if (edgeDirection == EdgeDirection.Left)
            {
                min = 10;
                max = 12;
            }

            return (byte)rnd.Next(min, max);
        }

        /// <summary>
        /// Sets the Neighbors Edge Connection based on 
        /// </summary>
        /// <param name="currentMapGraphicsTile"></param>
        /// <param name="neighborsMapGraphicsTile"></param>
        /// <param name="edgeDirection"></param>
		private void GetNeighborsEdgeConnection(MapGraphicsTile currentMapGraphicsTile, MapGraphicsTile neighborsMapGraphicsTile, EdgeDirection edgeDirection)
		{
            if (edgeDirection == EdgeDirection.Top && !neighborsMapGraphicsTile.ShoreEdgePoints.Exists(x => x.EdgePosition > 0 && x.EdgePosition < 3))
            {
                CreateEdgeConnectionBetweenNeighbors(currentMapGraphicsTile, neighborsMapGraphicsTile, edgeDirection);
                currentMapGraphicsTile.TopEdgeType = EdgeType.Both;
            }

            else if (edgeDirection == EdgeDirection.Bottom && !neighborsMapGraphicsTile.ShoreEdgePoints.Exists(x => x.EdgePosition > 6 && x.EdgePosition < 10))
            {
                CreateEdgeConnectionBetweenNeighbors(currentMapGraphicsTile, neighborsMapGraphicsTile, edgeDirection);
                currentMapGraphicsTile.BottomEdgeType = EdgeType.Both;
            }

            else if (edgeDirection == EdgeDirection.Left && !neighborsMapGraphicsTile.ShoreEdgePoints.Exists(x => x.EdgePosition > 3 && x.EdgePosition < 7))
            {
                CreateEdgeConnectionBetweenNeighbors(currentMapGraphicsTile, neighborsMapGraphicsTile, edgeDirection);
                currentMapGraphicsTile.BottomEdgeType = EdgeType.Both;
            }               

            else if (edgeDirection == EdgeDirection.Right && !neighborsMapGraphicsTile.ShoreEdgePoints.Exists(x => x.EdgePosition > 9 && x.EdgePosition < 13))
            {
                CreateEdgeConnectionBetweenNeighbors(currentMapGraphicsTile, neighborsMapGraphicsTile, edgeDirection);
                currentMapGraphicsTile.BottomEdgeType = EdgeType.Both;
            }               
		}

        /// <summary>
        /// Creates an edge connection between two neighbors. 
        /// </summary>
        /// <param name="currentMapGraphicsTile"></param>
        /// <param name="neighborsMapGraphicsTile"></param>
        /// <param name="edgeDirection"></param>
        private void CreateEdgeConnectionBetweenNeighbors(MapGraphicsTile currentMapGraphicsTile, MapGraphicsTile neighborsMapGraphicsTile, EdgeDirection edgeDirection)
        {
            EdgeConnection edgeConnection = new EdgeConnection(CreateEdgeConnection(edgeDirection),true);
            currentMapGraphicsTile.ShoreEdgePoints.Add(edgeConnection);
            neighborsMapGraphicsTile.ShoreEdgePoints.Add(new EdgeConnection(edgeConnection.TranslateConnectionForNeighbor(), true));
        }

		private bool SetNeighborEdgeConnections(KeyValuePair<Point,MapGraphicsTile> mapGraphicsTile, EdgeConnection edgeConnection)
		{
			//CheckDown
			if (edgeConnection.EdgePosition > 0 &&
					edgeConnection.EdgePosition < 4)
					//_edgeTiles.ContainsKey())
			{
				CheckNeighborForMatchingEdgePoint(_edgeTiles[new Point(mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y + 1)], edgeConnection);
				return true;
			}
			else if (edgeConnection.EdgePosition > 3 &&
						edgeConnection.EdgePosition < 7 &&
						_edgeTiles.ContainsKey(new Point(mapGraphicsTile.Key.X + 1, mapGraphicsTile.Key.Y)))
			{
				CheckNeighborForMatchingEdgePoint(_edgeTiles[new Point(mapGraphicsTile.Key.X + 1, mapGraphicsTile.Key.Y)], edgeConnection);
				return true;
			}
			else if (edgeConnection.EdgePosition > 6 &&
						edgeConnection.EdgePosition < 10 &&
						_edgeTiles.ContainsKey(new Point(mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y - 1)))
			{
				CheckNeighborForMatchingEdgePoint(_edgeTiles[new Point(mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y - 1)], edgeConnection);
				return true;
			}
			else if (edgeConnection.EdgePosition > 6 &&
						edgeConnection.EdgePosition < 10 &&
						_edgeTiles.ContainsKey(new Point(mapGraphicsTile.Key.X - 1, mapGraphicsTile.Key.Y)))
			{
				CheckNeighborForMatchingEdgePoint(_edgeTiles[new Point(mapGraphicsTile.Key.X - 1, mapGraphicsTile.Key.Y)], edgeConnection);
				return true;
			}

			return false;
		}		

		/// <summary>
		/// Calculates the best path available for tiles that need to be connected. 
		/// </summary>
		/// <param name="currentTile"></param>
		private void CalculateTilePath(KeyValuePair<Point,MapGraphicsTile> currentTile)
		{	
			List<Point> targetTiles = GetTargetPathEndingTiles(currentTile.Key);				

			TilePath bestTilePath = null;
			foreach (Point point in targetTiles)
			{
				TilePath currentTilePath = FindPath(currentTile.Key, point);

				if (currentTilePath != null && (bestTilePath== null || currentTilePath.Score < bestTilePath.Score))
					bestTilePath = currentTilePath;
			}

			if (bestTilePath != null)
				BuildPath(bestTilePath);
			else
            {
                _gameWorld.ErrorTiles.Add(currentTile.Key);
				_gameWorld.GameMap[currentTile.Key.X, currentTile.Key.Y] = new MapTile(_mapGraphicsTileSet.ErrorTile);							
            }	
		}

		/// <summary>
		/// Returns a list of tiles that are Targets for tha Pathfinding routine.
		/// </summary>
		/// <param name="edgeConnection"></param>
		/// <param name="currentTileLocation"></param>
		/// <returns></returns>
		private List<Point> GetTargetPathEndingTiles(Point currentTileLocation)
		{
			List<Point> targetPoints = new List<Point>();

            targetPoints = (from tiles in _noNeighborTiles
                                where tiles.Key.X > (currentTileLocation.X - 20) &&
                                tiles.Key.X < (currentTileLocation.X + 20) &&
                                tiles.Key.Y > (currentTileLocation.Y - 20) &&
                                tiles.Key.Y < (currentTileLocation.Y + 20) &&
                                tiles.Key.Y != currentTileLocation.Y &&
                                tiles.Key.X != currentTileLocation.X
                                select tiles.Key).ToList();	

			return targetPoints;
		}

		/// <summary>
		/// Generates the tiles that occur along a path that has been found between two tiles that do not connecte.
		/// </summary>
		/// <param name="bestPath"></param>
		private void BuildPath(TilePath bestPath)
		{
			TilePath currentPathTile = bestPath;
			EdgeConnection parentEdgeConnection = null;
			Dictionary<Point,MapGraphicsTile> tilesToAdd = new Dictionary<Point,MapGraphicsTile>();

			while (true)
			{
				MapGraphicsTile currentMapTile;
				//create a new mapTile
                if (_noNeighborTiles.ContainsKey(currentPathTile.TileLocation))
				{
                    currentMapTile = _noNeighborTiles[currentPathTile.TileLocation];
					//foreach (EdgeConnection edgeConnection in currentMapTile.ShoreEdgePoints)
					//	SetNeighborEdgeConnections(new KeyValuePair<Point, MapGraphicsTile>(currentPathTile.TileLocation, _edgeTiles[currentPathTile.TileLocation]), edgeConnection);

					//get rid of all of the edgepoints that are not a match, as we cannot used them. 		
					//currentMapTile.ShoreEdgePoints.RemoveAll(i => i.IsConnected == false);							
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

			foreach (KeyValuePair<Point,MapGraphicsTile> mapGraphicsTile in tilesToAdd)
			{
				SetTileEdgeTypeByNeighbor(mapGraphicsTile);
				MapTile mapTile = _mapGraphicsTileSet.GetMatchingTile(mapGraphicsTile.Value);

				if (mapTile.GraphicsTile.TileType != TileType.Error)
					_gameWorld.GameMap[mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y] = mapTile;
				else
				{
					if (_errorTiles.ContainsKey(mapGraphicsTile.Key))
					{
						_gameWorld.ErrorTiles.Add(mapGraphicsTile.Key);
						_gameWorld.GameMap[mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y] = new MapTile(_mapGraphicsTileSet.ErrorTile);
					}
					else
						_errorTiles.Add(mapGraphicsTile.Key, mapGraphicsTile.Value);
				}
			}
		}

		private EdgeConnection GetParentTileEdgeConnection(TilePath currentPathTile)
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

		/// <summary>
		/// Finds the best path between two tiles that are not connected. 
		/// </summary>
		/// <param name="startTilePoint"></param>
		/// <param name="targetTilePoint"></param>
		/// <returns></returns>
		private TilePath FindPath(Point startTilePoint, Point targetTilePoint)
		{
			TilePath currentTile = new TilePath(startTilePoint, 10);

			List<TilePath> openTiles = new List<TilePath>();
			List<TilePath> closedTiles = new List<TilePath>();

			while (true)
			{
				closedTiles.Add(currentTile);

				openTiles.AddRange(GetOpenPathTiles(currentTile, targetTilePoint, openTiles,closedTiles));

				if (openTiles.Count == 0)
					return null;

				openTiles.Sort();

				currentTile = openTiles[0];
				openTiles.RemoveAt(0);

				if (currentTile.TileLocation == targetTilePoint)
					return currentTile;
			}
		}

		/// <summary>
		/// Determines which directions have open tiles available for the path. 
		/// </summary>
		/// <param name="currentTile"></param>
		/// <param name="targetTile"></param>
		/// <param name="openTiles"></param>
		/// <param name="closedTiles"></param>
		/// <returns></returns>
		private List<TilePath> GetOpenPathTiles(TilePath currentTile, Point targetTile, List<TilePath> openTiles, List<TilePath> closedTiles)
		{
			List<Point> openPoints = new List<Point>();
			
			//North Tile
			if (currentTile.TileLocation.Y - 1 >= 0 &&
				!closedTiles.Exists(i => i.TileLocation.X ==  currentTile.TileLocation.X && i.TileLocation.Y == currentTile.TileLocation.Y - 1))			
				openPoints.Add(new Point(currentTile.TileLocation.X, currentTile.TileLocation.Y - 1));

			//south Tile
			if (currentTile.TileLocation.Y + 1 < _gameWorld.GameMap.GetLength(1) &&
				!closedTiles.Exists(i => i.TileLocation.X == currentTile.TileLocation.X && i.TileLocation.Y == currentTile.TileLocation.Y + 1))			
				openPoints.Add(new Point(currentTile.TileLocation.X, currentTile.TileLocation.Y + 1));

			//East Tile
			if (currentTile.TileLocation.X - 1 >= 0 &&
				!closedTiles.Exists(i => i.TileLocation.X ==  currentTile.TileLocation.X - 1 && i.TileLocation.Y == currentTile.TileLocation.Y))
				openPoints.Add(new Point(currentTile.TileLocation.X - 1, currentTile.TileLocation.Y));

			//West Tile
			if (currentTile.TileLocation.X + 1 < _gameWorld.GameMap.GetLength(0) &&
				!closedTiles.Exists(i => i.TileLocation.X ==  currentTile.TileLocation.X + 1 && i.TileLocation.Y == currentTile.TileLocation.Y)) 
				openPoints.Add(new Point(currentTile.TileLocation.X + 1, currentTile.TileLocation.Y));

			List<TilePath> tiles = new List<TilePath>();
			foreach (Point location in openPoints)
			{
				int score = CalculateScore(currentTile.ParentTile, location, targetTile);

				//If it doesnt exist on the open list we want to create a new item.
				if (!openTiles.Exists(i => i.TileLocation.X == location.X && i.TileLocation.Y == location.Y))
				{					
					TilePath potentialPathTile = new TilePath(location, score, currentTile);
					tiles.Add(potentialPathTile);
				}
				else
				{
					TilePath existingTile = openTiles.First(i => i.TileLocation.X == location.X && i.TileLocation.Y == location.Y);
					existingTile.ParentTile = currentTile;
					existingTile.Score = score;
				}
				
			}
			return tiles;
		}

		/// <summary>
		/// Calculates the path score. 
		/// </summary>
		/// <param name="parentTile"></param>
		/// <param name="location"></param>
		/// <param name="targetTile"></param>
		/// <returns></returns>
		private int CalculateScore(TilePath parentTile, Point location, Point targetTile)
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
		/// <param name="TileX"></param>
		/// <param name="TileY"></param>
		/// <returns></returns>
		private void SetTileEdgeTypeByNeighbor(KeyValuePair<Point,MapGraphicsTile> mapGraphicsTile)
		{
			//Left Side
			if (mapGraphicsTile.Value.ShoreEdgePoints.Exists(x => x.EdgePosition > 9 && x.EdgePosition < 13))
				mapGraphicsTile.Value.LeftEdgeType = EdgeType.Both;
			else if (mapGraphicsTile.Key.X - 1 > 0 && _gameWorld.GameMap[mapGraphicsTile.Key.X - 1, mapGraphicsTile.Key.Y] != null && _gameWorld.GameMap[mapGraphicsTile.Key.X - 1, mapGraphicsTile.Key.Y].GraphicsTile != null)
				mapGraphicsTile.Value.LeftEdgeType = _gameWorld.GameMap[mapGraphicsTile.Key.X - 1, mapGraphicsTile.Key.Y].GraphicsTile.RightEdgeType;

			//Top Side
			if (mapGraphicsTile.Value.ShoreEdgePoints.Exists(x => x.EdgePosition > 6 && x.EdgePosition < 10))
				mapGraphicsTile.Value.TopEdgeType = EdgeType.Both;
			else if (mapGraphicsTile.Key.Y - 1 > 0 && _gameWorld.GameMap[mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y - 1] != null && _gameWorld.GameMap[mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y - 1].GraphicsTile != null)
				mapGraphicsTile.Value.TopEdgeType = _gameWorld.GameMap[mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y - 1].GraphicsTile.BottomEdgeType;

			//right Side
			if (mapGraphicsTile.Value.ShoreEdgePoints.Exists(x => x.EdgePosition > 3 && x.EdgePosition < 7))
				mapGraphicsTile.Value.RightEdgeType = EdgeType.Both;
			else if (mapGraphicsTile.Key.X + 1 < _gameWorldWidth && _gameWorld.GameMap[mapGraphicsTile.Key.X + 1, mapGraphicsTile.Key.Y] != null && _gameWorld.GameMap[mapGraphicsTile.Key.X + 1, mapGraphicsTile.Key.Y].GraphicsTile != null)
				mapGraphicsTile.Value.RightEdgeType = _gameWorld.GameMap[mapGraphicsTile.Key.X + 1, mapGraphicsTile.Key.Y].GraphicsTile.LeftEdgeType;

			//Bottom Side
			if (mapGraphicsTile.Value.ShoreEdgePoints.Exists(x => x.EdgePosition > 0 && x.EdgePosition < 4))
				mapGraphicsTile.Value.BottomEdgeType = EdgeType.Both;
			else if (mapGraphicsTile.Key.Y + 1 < _gameWorldHeight && _gameWorld.GameMap[mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y + 1] != null && _gameWorld.GameMap[mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y + 1].GraphicsTile != null)
				mapGraphicsTile.Value.BottomEdgeType = _gameWorld.GameMap[mapGraphicsTile.Key.X, mapGraphicsTile.Key.Y + 1].GraphicsTile.TopEdgeType;
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