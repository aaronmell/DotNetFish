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
using DotNetFish.GameObjects;
using System.Diagnostics;

namespace DotNetFish.Wpf.LevelDesigner
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
			MapGraphicsTileSet mapGraphicsTileSet = new MapGraphicsTileSet(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\MapTiles.xml");

			mapCanvas.LoadWorld(gameWorld,new Point(gameWorld.GameMap.GetUpperBound(0) / 2,gameWorld.GameMap.GetLength(0) /2), mapGraphicsTileSet);
		}

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            mapCanvas.MoveCanvas(e.Key);			
        }
	}
}
