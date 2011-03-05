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
using System.Windows.Navigation;
using System.Windows.Shapes;
using GameObjects;

namespace MapTileBuilder
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{		
		Point _currentPoint;
		BitmapImage _tileImage;
		Dictionary<Point,MapGraphicsTile> _graphicsTiles;
		string _filename;

		public MainWindow()
		{
			InitializeComponent();
			next.Click += new RoutedEventHandler(next_Click);
			previous.Click +=new RoutedEventHandler(previous_Click);
			_graphicsTiles = new Dictionary<Point,MapGraphicsTile>();

			SetupDropDowns(new List<ComboBox> {
				{top},
				{bottom},
				{left},
				{right}
			});
			LoadFile();

			_currentPoint = new Point(0, 0);

			specialTiles.Items.Add(GameObjects.Enums.TileType.Edge.ToString());
			specialTiles.Items.Add(GameObjects.Enums.TileType.Special.ToString());
			specialTiles.Items.Add(GameObjects.Enums.TileType.Land.ToString());
			specialTiles.Items.Add(GameObjects.Enums.TileType.Water.ToString());
			specialTiles.Items.Add(GameObjects.Enums.TileType.Error.ToString());
			specialTiles.Items.Add(GameObjects.Enums.TileType.Blank.ToString());
			specialTiles.SelectedIndex = 0;
		}

		void next_Click(object sender, RoutedEventArgs e)
		{
			SaveValues();

			if (!previous.IsEnabled)
				previous.IsEnabled = true;

			if (_currentPoint.X + 66 > _tileImage.Width)
			{
				_currentPoint.X = 0;
				_currentPoint.Y += 64;
			}
			else
				_currentPoint.X += 64;

			if (_currentPoint.Y + 64 > _tileImage.Height)
			{
				SaveFile();
				MessageBox.Show("MapTiles Have been processed");
				this.Close();
			}
			else
			{
				LoadValues();
				DrawImageOnCanvas(_currentPoint);
			}						
		}

		private void SaveFile()
		{
			string filename = _filename.Remove(_filename.Count() - 3);
			filename += "xml";
			MapGraphicsTileSet.SaveTileSet(_graphicsTiles.Values.ToList(),filename);
		}

		private void SaveValues()
		{
			MapGraphicsTile mapGraphicsTile = new MapGraphicsTile
			{
				//ShoreEdgePoint = new Point(int.Parse(edge1.Text), int.Parse(edge2.Text)),
				ShoreEdgePoints = GetShoreEdgePoints(),
				TileStartPoint = _currentPoint,
				TileType = GetTileType()
			};
			mapGraphicsTile = GetTileSides(mapGraphicsTile);
			_graphicsTiles[_currentPoint] = mapGraphicsTile;
		}

		private Enums.TileType GetTileType()
		{
			switch (specialTiles.SelectedItem.ToString())
			{
				case "Edge":
					return Enums.TileType.Edge;					
				case "Special":
					return Enums.TileType.Special;
				case "Water":
					return Enums.TileType.Water;
				case "Land":
					return Enums.TileType.Land;
				case "Error":
					return Enums.TileType.Error;
				case "Blank":
					return Enums.TileType.Blank;
			}

			throw new ArgumentOutOfRangeException("No Tile Type Found");
		}

		private List<byte> GetShoreEdgePoints()
		{
			List<byte> edgePoints = new List<byte>();

			if (specialTiles.SelectedItem.ToString() == GameObjects.Enums.TileType.Edge.ToString())
			{
				if ((bool)button1.IsChecked)
					edgePoints.Add(1);

				if ((bool)button2.IsChecked)
					edgePoints.Add(2);

				if ((bool)button3.IsChecked)
					edgePoints.Add(3);

				if ((bool)button4.IsChecked)
					edgePoints.Add(4);

				if ((bool)button5.IsChecked)
					edgePoints.Add(5);

				if ((bool)button6.IsChecked)
					edgePoints.Add(6);

				if ((bool)button7.IsChecked)
					edgePoints.Add(7);

				if ((bool)button8.IsChecked)
					edgePoints.Add(8);

				if ((bool)button9.IsChecked)
					edgePoints.Add(9);

				if ((bool)button10.IsChecked)
					edgePoints.Add(10);

				if ((bool)button11.IsChecked)
					edgePoints.Add(11);

				if ((bool)button12.IsChecked)
					edgePoints.Add(12);
			}
			return edgePoints;			
		}

		private void LoadValues()
		{
			if (_graphicsTiles.ContainsKey(_currentPoint))
			{
				MapGraphicsTile mapGraphicsTile = _graphicsTiles[_currentPoint];

				SetShoreEdgePoints(mapGraphicsTile.ShoreEdgePoints);
				specialTiles.SelectedValue = mapGraphicsTile.TileType.ToString();

				top.SelectedIndex = (int)mapGraphicsTile.TopEdgeType - 1;
				bottom.SelectedIndex = (int)mapGraphicsTile.BottomEdgeType - 1;
				left.SelectedIndex = (int)mapGraphicsTile.LeftEdgeType - 1;
				right.SelectedIndex = (int)mapGraphicsTile.RightEdgeType - 1;
			}
			else
			{
				ShiftEdgeValues();

				int tempIndex = left.SelectedIndex;
				left.SelectedIndex = bottom.SelectedIndex;
				bottom.SelectedIndex = right.SelectedIndex;
				right.SelectedIndex = top.SelectedIndex;
				top.SelectedIndex = tempIndex;
			}
		}

		

		private void SetShoreEdgePoints(List<byte> shoreEdgePoints)
		{
			button1.IsChecked = shoreEdgePoints.Contains(1);
			button2.IsChecked = shoreEdgePoints.Contains(2);
			button3.IsChecked = shoreEdgePoints.Contains(3);
			button4.IsChecked = shoreEdgePoints.Contains(4);
			button5.IsChecked = shoreEdgePoints.Contains(5);
			button6.IsChecked = shoreEdgePoints.Contains(6);
			button7.IsChecked = shoreEdgePoints.Contains(7);
			button8.IsChecked = shoreEdgePoints.Contains(8);
			button9.IsChecked = shoreEdgePoints.Contains(9);
			button10.IsChecked = shoreEdgePoints.Contains(10);
			button11.IsChecked = shoreEdgePoints.Contains(11);
			button12.IsChecked = shoreEdgePoints.Contains(12);			
		}

		private void ShiftEdgeValues()
		{
			List<byte> edgePoints = new List<byte>();

			if ((bool)button1.IsChecked)
				edgePoints.Add(10);

			if ((bool)button2.IsChecked)
				edgePoints.Add(11);

			if ((bool)button3.IsChecked)
				edgePoints.Add(12);

			if ((bool)button4.IsChecked)
				edgePoints.Add(1);

			if ((bool)button5.IsChecked)
				edgePoints.Add(2);

			if ((bool)button6.IsChecked)
				edgePoints.Add(3);

			if ((bool)button7.IsChecked)
				edgePoints.Add(4);

			if ((bool)button8.IsChecked)
				edgePoints.Add(5);

			if ((bool)button9.IsChecked)
				edgePoints.Add(6);

			if ((bool)button10.IsChecked)
				edgePoints.Add(7);

			if ((bool)button11.IsChecked)
				edgePoints.Add(8);

			if ((bool)button12.IsChecked)
				edgePoints.Add(9);

			SetShoreEdgePoints(edgePoints);	
		}

		private MapGraphicsTile GetTileSides(MapGraphicsTile mapGraphicsTile)
		{
			mapGraphicsTile.TopEdgeType = (Enums.EdgeType)top.SelectedIndex + 1;
			mapGraphicsTile.BottomEdgeType = (Enums.EdgeType)bottom.SelectedIndex + 1;
			mapGraphicsTile.LeftEdgeType = (Enums.EdgeType)left.SelectedIndex + 1;
			mapGraphicsTile.RightEdgeType = (Enums.EdgeType)right.SelectedIndex + 1;

			return mapGraphicsTile;
		}

		void previous_Click(object sender, RoutedEventArgs e)
		{
			SaveValues();

			if (_currentPoint.X - 64 < 0)
			{
				_currentPoint.X = _tileImage.Width - 64;
				_currentPoint.Y -= 64;
			}
			else
				_currentPoint.X -= 64;

			if (_currentPoint.Y < 0)
				_currentPoint.Y = 0;

			if (_currentPoint.X - 64 < 0 && _currentPoint.Y - 64 < 0)
				previous.IsEnabled = false;

			LoadValues();
			DrawImageOnCanvas(_currentPoint);
		}

		private void SetupDropDowns(List<ComboBox> comboBoxes)
		{
			foreach (ComboBox comboBox in comboBoxes)
			{
				comboBox.Items.Add(GameObjects.Enums.EdgeType.Both.ToString());
				comboBox.Items.Add(GameObjects.Enums.EdgeType.Land.ToString());
				comboBox.Items.Add(GameObjects.Enums.EdgeType.Water.ToString());
				comboBox.SelectedIndex = 0;
			}			
		}

		private void LoadFile()
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Valid Files (.png, *.xml)|*.png;*.xml";

            Nullable<bool> result = dlg.ShowDialog();
          
            if (result == true)
            {
				_filename = dlg.FileName;
				if (_filename.Contains(".xml"))
				{
					MapGraphicsTileSet mapGraphicsTileSet = new MapGraphicsTileSet(_filename);
					_graphicsTiles = mapGraphicsTileSet.MapTiles.ToDictionary(instance => instance.TileStartPoint);
					LoadValues();
					_tileImage = new BitmapImage(new Uri(_filename.Replace(".xml",".png")));
				
				}				
				else
					_tileImage = new BitmapImage(new Uri(_filename));
				
				DrawImageOnCanvas(_currentPoint);
            }
			else
				this.Close();
        }

		private void DrawImageOnCanvas(Point point)
		{
			BitmapSource bmpSource = new CroppedBitmap(_tileImage, new Int32Rect((int)point.X, (int)point.Y, 64, 64));
			tile.Source = bmpSource;
			_currentPoint = point;
		}		
	}
}
