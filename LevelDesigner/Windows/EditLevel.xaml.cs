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

namespace LevelDesigner
{
	/// <summary>
	/// Interaction logic for EditLevel.xaml
	/// </summary>
	public partial class EditLevel : Window
	{
		private GameWorld _gameWorld;

		public EditLevel(GameWorld gameWorld)
		{
			_gameWorld = gameWorld;
			InitializeComponent();
		}
	}
}
