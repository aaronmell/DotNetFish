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
using GameObjects;
using System.Diagnostics;

namespace LevelDesigner
{
	/// <summary>
	/// Interaction logic for EditLevel.xaml
	/// </summary>
	public partial class EditLevel : Window
	{
		//private GameWorld _gameWorld;

		public EditLevel(GameWorld gameWorld)
		{			
			InitializeComponent();
			MapGraphicsTileSet mapGraphicsTileSet = new MapGraphicsTileSet(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\MapTiles.csv", 64, 64);

			mapCanvas.LoadWorld(gameWorld,new Point(gameWorld.GameMap.GetUpperBound(0) / 2,gameWorld.GameMap.GetLength(0) /2), mapGraphicsTileSet);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			Point newTile = mapCanvas.CurrentTile;
			if (e.Key == Key.Down)			
				newTile.Y+=1;			
			if (e.Key == Key.Up)
				newTile.Y-=1;
			if (e.Key == Key.Left)
				newTile.X-=1;
			if (e.Key == Key.Right)
				newTile.X+=1;
			
			mapCanvas.CurrentTile = newTile;
			mapCanvas.InvalidateVisual();
			//mapCanvas.
		}
	}
}
