using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using GameObjects;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Windows.Media;

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

		public Point CurrentTile { get { return _currentTile; } set { _currentTile = value; } }

		protected override void OnRender(System.Windows.Media.DrawingContext dc)
		{
			base.OnRender(dc);

			DrawTiles(dc);
		}

		private void DrawTiles(System.Windows.Media.DrawingContext dc)
		{

			int tilesWide = (int)(this.ActualWidth / 64) + 1;
			int tilesHigh = (int)(this.ActualHeight/ 64) + 1;
			int startX = (int)_currentTile.X - (tilesWide / 2);
			int endX = startX + tilesWide;

			int startY = (int)_currentTile.Y - (tilesHigh / 2);
			int endY = startY + tilesHigh;

            if (startX < 0)
                startX = 0;

            if (startY < 0)
                startY = 0;

			if (endX > _gameWorld.GameMap.GetUpperBound(0))
			{
				endX = _gameWorld.GameMap.GetUpperBound(0);
				startX = endX - tilesWide;
			}
			else if (startX < 0)
			{
				startX = 0;
				endX = tilesWide;
			}

			if (endY > _gameWorld.GameMap.GetUpperBound(1))
			{
				endY = _gameWorld.GameMap.GetUpperBound(1);
				startY = endY - tilesHigh;
			}
			else if (startY < 0)
			{
				startY = 0;
				endY = tilesHigh;
			}                

            int CountX = 0;
            int CountY = 0;

			for (int x = startX; x < endX; x++)
			{
			    for (int y = startY; y < endY; y++)
			    {
			        dc.DrawImage(_tileSet[_gameWorld.GameMap[x,y].GraphicsTile.TileStartPoint],new Rect(CountX * 64,CountY*64,64,64));
#if DEBUG
					dc.DrawText(
						new System.Windows.Media.FormattedText(
							"X:" + x + " Y:" + y,
							CultureInfo.CurrentCulture,
							FlowDirection.LeftToRight,
							new System.Windows.Media.Typeface("arial"),
							12,
							new SolidColorBrush(Color.FromRgb(255, 0, 20))
						),
						new Point((CountX * 64), (CountY * 64) + 31));
#endif
					CountY++;
			    }
                CountY = 0;
                CountX++;
			}


		}	
	}
}
