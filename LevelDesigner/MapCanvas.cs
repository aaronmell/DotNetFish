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
using System.Windows.Input;

namespace LevelDesigner
{
	public class MapCanvas : Canvas
	{
		private GameWorld _gameWorld;
		private Point _currentTile;
		private Dictionary<Point,BitmapSource> _tileSet;
        private int _tilesWide;
        private int _tilesHigh;
        private int _mapWidth;
        private int _mapHeight;

		public void LoadWorld(GameWorld gameWorld, Point startPoint, MapGraphicsTileSet mapGraphicsTileSet)
		{
			_gameWorld = gameWorld;
			_currentTile = startPoint;
			_tileSet = mapGraphicsTileSet.GetTileImages();           
            _mapWidth = _gameWorld.GameMap.GetUpperBound(0);
            _mapHeight = _gameWorld.GameMap.GetUpperBound(1);
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
            _tilesWide = (int)(this.ActualWidth / 64) + 1;
            _tilesHigh = (int)(this.ActualHeight / 64) + 1;

			int startX = (int)_currentTile.X - (_tilesWide / 2);
			int startY = (int)_currentTile.Y - (_tilesHigh / 2);

            if (startX < 0)
                startX = 0;

            if (startY < 0)
                startY = 0;

            int endX = startX + _tilesWide;
            int endY = startY + _tilesHigh;

            if (endX > _mapWidth)
			{
                endX = _mapWidth;
				startX = endX - _tilesWide;
			}
			else if (startX < 0)
			{
				startX = 0;
				endX = _tilesWide;
			}

            if (endY > _mapHeight)
			{
                endY = _mapHeight;
				startY = endY - _tilesHigh;
			}
			else if (startY < 0)
			{
				startY = 0;
				endY = _tilesHigh;
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
					dc.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(255, 0, 20)), 1), new Rect(CountX * 64, CountY * 64, 64, 64));
#endif
					CountY++;
			    }
                CountY = 0;
                CountX++;
			}


		}

        internal void MoveCanvas(System.Windows.Input.Key key)
        {
            if (key == Key.Down && (int)_currentTile.Y + (_tilesHigh / 2) + 1 < _mapHeight)			
				_currentTile.Y+=1;
            if (key == Key.Up && (int)_currentTile.Y - (_tilesHigh / 2) - 1 >= 0)
				_currentTile.Y-=1;
			if (key == Key.Left && (int)_currentTile.X - (_tilesWide / 2) -1 >= 0)
				_currentTile.X-=1;
            if (key == Key.Right && (int)_currentTile.X + (_tilesWide / 2) + 1 < _mapWidth)
				_currentTile.X+=1;			
			
			this.InvalidateVisual();
        }
    }
}
