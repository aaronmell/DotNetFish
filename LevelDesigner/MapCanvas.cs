using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using GameObjects;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LevelDesigner
{
	public class MapCanvas : Canvas
	{
		private GameWorld _gameWorld;
		private Point _currentTile;
		private Dictionary<Point,BitmapSource> _tileSet;

		public void LoadWorld(GameWorld gameWorld, Point startPoint, MapGraphicsTileSet mapGraphicsTileSet)
		{
			_gameWorld = gameWorld;
			_currentTile = startPoint;
			_tileSet = mapGraphicsTileSet.GetTileImages();
		}

		public GameWorld Gameworld
		{
			get
			{
				return _gameWorld;
			}
		}	

		protected override void OnRender(System.Windows.Media.DrawingContext dc)
		{
			base.OnRender(dc);

			DrawTiles(dc);
		}

		private void DrawTiles(System.Windows.Media.DrawingContext dc)
		{

			//int tilesWide = (int)(this.Width / 64) + 1;
			//int tilesHigh = (int)(this.Height / 64) + 1;
			//int startX = (int)_currentTile.X - (tilesWide / 2);
			//int endX = startX + tilesWide;

			//int startY = (int)_currentTile.Y - (tilesHigh / 2);
			//int endY = startX + tilesHigh;

			//for (int x = startX; x < endX; x++)
			//{
			//    for (int y = startY; y < endY; y++)
			//    {
			//        dc.DrawImage(_tileSet[_gameWorld.GameMap[x,y].GraphicsTile.TileStartPoint],new Rect(0,0,64,64);
			//    }
			//}


		}	
	}
}
