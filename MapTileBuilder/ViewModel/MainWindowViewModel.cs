using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;
using DotNetFish.BaseMvvm;
using DotNetFish.GameObjects;
using DotNetFish.GameObjects.Enums;

namespace DotNetFish.Wpf.MapTileBuilder.ViewModel
{
	public class MainWindowViewModel : ViewModelBase
	{
		#region [Declarations]
		private RelayCommand _backCommand;
		private RelayCommand _nextCommand;
		private RelayCommand _drawCanvas;
		private RelayCommand _closeCommand;
		private EdgeType[] _edgeTypes;
		private TileType[] _tileTypes;
		private string _filename;
		private bool _previousButtonActive;
		private Point _currentPoint;
		private BitmapImage _mapTiles;
		private BitmapSource _currentTile;
		private Dictionary<Point, MapGraphicsTile> _graphicsTiles;
		private EdgeType _topEdge;
		private EdgeType _bottomEdge;
		private EdgeType _leftEdge;
		private EdgeType _rightEdge;
		private TileType _tileType;

		private bool _edge1;
		private bool _edge2;
		private bool _edge3;
		private bool _edge4;
		private bool _edge5;
		private bool _edge6;
		private bool _edge7;
		private bool _edge8;
		private bool _edge9;
		private bool _edge10;
		private bool _edge11;
		private bool _edge12;

		public event EventHandler RequestClose;
		#endregion	

		public MainWindowViewModel()
		{
			_graphicsTiles = new Dictionary<Point, MapGraphicsTile>();
			LoadFile();
			_currentPoint = new Point(0, 0);
			DrawImageOnCanvas(_currentPoint);
		}
		
		#region [Properties]
		
		public ICommand NextCommand
		{
			get
			{
				if (_nextCommand == null)
					_nextCommand = new RelayCommand(param => this.OnNextCommand());

				return _nextCommand;
			}
		}

		public ICommand BackCommand
		{
			get
			{
				if (_backCommand == null)
					_backCommand = new RelayCommand(param => this.OnPreviousCommand());

				return _backCommand;
			}
		}

		public ICommand DrawCanvas
		{
			get
			{
				if (_drawCanvas == null)
					_drawCanvas = new RelayCommand(param => this.DrawImageOnCanvas(_currentPoint));

				return _drawCanvas;
			}

		}

		/// <summary>
		/// Returns the command that, when invoked, attempts
		/// to remove this workspace from the user interface.
		/// </summary>
		public ICommand CloseCommand
		{
			get
			{
				if (_closeCommand == null)
					_closeCommand = new RelayCommand(param => this.OnRequestClose());

				return _closeCommand;
			}
		}
		
		public EdgeType[] EdgeTypes
		{
			get
			{
				if (_edgeTypes == null)
					_edgeTypes = new EdgeType[]
					{
						EdgeType.Both,
						EdgeType.Land,
						EdgeType.Water,
						EdgeType.Undefined
					};
				return _edgeTypes;
			}
		}

		public TileType[] TileTypes
		{
			get
			{
				if (_tileTypes == null)
					_tileTypes = new TileType[]
					{
						TileType.Edge,
						TileType.Special,
						TileType.Land,
						TileType.Water,
						TileType.Error,
						TileType.Blank
					};
				return _tileTypes;
			}
		}

		public BitmapSource CurrentTile
		{
			get
			{
				return _currentTile;
			}
			set
			{
				_currentTile = value;

				OnPropertyChanged("CurrentTile");
			}
		}

		public bool PreviousButtonActive
		{
			get
			{
				return _previousButtonActive;
			}
			set
			{
				_previousButtonActive = value;
				OnPropertyChanged("PreviousButtonActive");
			}
		}

		public EdgeType TopEdge
		{
			get
			{
				return _topEdge;
			}
			set
			{
				_topEdge = value;

				OnPropertyChanged("TopEdge");
			}
		}

		public EdgeType BottomEdge
		{
			get
			{
				return _bottomEdge;
			}
			set
			{
				_bottomEdge = value;

				OnPropertyChanged("BottomEdge");
			}
		}

		public EdgeType LeftEdge
		{
			get
			{
				return _leftEdge;
			}
			set
			{
				_leftEdge = value;

				OnPropertyChanged("LeftEdge");
			}
		}

		public EdgeType RightEdge
		{
			get
			{
				return _rightEdge;
			}
			set
			{
				_rightEdge = value;

				OnPropertyChanged("RightEdge");
			}
		}

		public TileType TileType
		{
			get
			{
				return _tileType;
			}
			set
			{
				_tileType = value;

				OnPropertyChanged("TileType");
			}
		}

		public bool IsSelectionValid
		{
			get
			{
				if (TopEdge == 0 || BottomEdge == 0 || LeftEdge == 0 || RightEdge == 0 || TileType == 0)
					return false;

				return true;			
			}
			
		}

		public bool Edge1
		{
			get
			{
				return _edge1;
			}
			set
			{
				_edge1 = value;

				OnPropertyChanged("Edge1");
			}
		}

		public bool Edge2
		{
			get
			{
				return _edge2;
			}
			set
			{
				_edge2 = value;

				OnPropertyChanged("Edge2");
			}
		}

		public bool Edge3
		{
			get
			{
				return _edge3;
			}
			set
			{
				_edge3 = value;

				OnPropertyChanged("Edge3");
			}
		}

		public bool Edge4
		{
			get
			{
				return _edge4;
			}
			set
			{
				_edge4 = value;

				OnPropertyChanged("Edge4");
			}
		}		

		public bool Edge5
		{
			get
			{
				return _edge5;
			}
			set
			{
				_edge5 = value;

				OnPropertyChanged("Edge5");
			}
		}

		public bool Edge6
		{
			get
			{
				return _edge6;
			}
			set
			{
				_edge6 = value;

				OnPropertyChanged("Edge6");
			}
		}

		public bool Edge7
		{
			get
			{
				return _edge7;
			}
			set
			{
				_edge7 = value;

				OnPropertyChanged("Edge7");
			}
		}

		public bool Edge8
		{
			get
			{
				return _edge8;
			}
			set
			{
				_edge8 = value;

				OnPropertyChanged("Edge8");
			}
		}

		public bool Edge9
		{
			get
			{
				return _edge9;
			}
			set
			{
				_edge9 = value;

				OnPropertyChanged("Edge9");
			}
		}

		public bool Edge10
		{
			get
			{
				return _edge10;
			}
			set
			{
				_edge10 = value;

				OnPropertyChanged("Edge10");
			}
		}

		public bool Edge11
		{
			get
			{
				return _edge11;
			}
			set
			{
				_edge11 = value;

				OnPropertyChanged("Edge11");
			}
		}

		public bool Edge12
		{
			get
			{
				return _edge12;
			}
			set
			{
				_edge12 = value;

				OnPropertyChanged("Edge12");
			}
		}

		#endregion

		#region [Methods]
		private void OnPreviousCommand()
		{
			SaveValues();

			if (_currentPoint.X - 64 < 0)
			{
				_currentPoint.X = _mapTiles.Width - 64;
				_currentPoint.Y -= 64;
			}
			else
				_currentPoint.X -= 64;

			if (_currentPoint.Y < 0)
				_currentPoint.Y = 0;

			if (_currentPoint.X - 64 < 0 && _currentPoint.Y - 64 < 0)
				PreviousButtonActive = false;

			LoadValues();
			DrawImageOnCanvas(_currentPoint);
		}

		private void OnNextCommand()
		{
			if (!IsSelectionValid)
			{
				MessageBox.Show("Invalid Selection");
				return;
			}
			SaveValues();

			if (!PreviousButtonActive)
				PreviousButtonActive = true;

			if (_currentPoint.X + 66 > _mapTiles.Width)
			{
				_currentPoint.X = 0;
				_currentPoint.Y += 64;
			}
			else
				_currentPoint.X += 64;

			if (_currentPoint.Y + 64 > _mapTiles.Height)
			{
				SaveFile();
				MessageBox.Show("MapTiles Have been processed");
				OnRequestClose();
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
			MapGraphicsTileSet.SaveTileSet(_graphicsTiles.Values.ToList(), filename);
		}

		private void SaveValues()
		{
			MapGraphicsTile mapGraphicsTile = new MapGraphicsTile
			{
				ShoreEdgePoints = GetShoreEdgePoints(),
				TileStartPoint = _currentPoint,
				TileType = TileType
			};
			mapGraphicsTile = GetTileSides(mapGraphicsTile);
			_graphicsTiles[_currentPoint] = mapGraphicsTile;
		}

		private List<EdgeConnection> GetShoreEdgePoints()
		{
			List<EdgeConnection> edgePoints = new List<EdgeConnection>();

			if (TileType == DotNetFish.GameObjects.Enums.TileType.Edge)
			{
				if (Edge1)
					edgePoints.Add(new EdgeConnection(1));

				if (Edge2)
					edgePoints.Add(new EdgeConnection(2));

				if (Edge3)
					edgePoints.Add(new EdgeConnection(3));

				if (Edge4)
					edgePoints.Add(new EdgeConnection(4));

				if (Edge5)
					edgePoints.Add(new EdgeConnection(5));

				if (Edge6)
					edgePoints.Add(new EdgeConnection(6));

				if (Edge7)
					edgePoints.Add(new EdgeConnection(7));

				if (Edge8)
					edgePoints.Add(new EdgeConnection(8));

				if (Edge9)
					edgePoints.Add(new EdgeConnection(9));

				if (Edge10)
					edgePoints.Add(new EdgeConnection(10));

				if (Edge11)
					edgePoints.Add(new EdgeConnection(11));

				if (Edge12)
					edgePoints.Add(new EdgeConnection(12));
			}
			return edgePoints;
		}

		private void LoadValues()
		{
			if (_graphicsTiles.ContainsKey(_currentPoint))
			{
				MapGraphicsTile mapGraphicsTile = _graphicsTiles[_currentPoint];

				SetShoreEdgePoints(mapGraphicsTile.ShoreEdgePoints);
				TileType = mapGraphicsTile.TileType;

				TopEdge = mapGraphicsTile.TopEdgeType;
				BottomEdge = mapGraphicsTile.BottomEdgeType;
				LeftEdge = mapGraphicsTile.LeftEdgeType;
				RightEdge = mapGraphicsTile.RightEdgeType;
			}
			else
			{
				ShiftEdgeValues();

				EdgeType temp = LeftEdge;
				LeftEdge = BottomEdge;
				BottomEdge = RightEdge;
				RightEdge = TopEdge;
				TopEdge = temp;
			}
		}

		private void SetShoreEdgePoints(List<EdgeConnection> shoreEdgePoints)
		{
			Edge1 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 1);
			Edge2 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 2);
			Edge3 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 3);
			Edge4 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 4);
			Edge5 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 5);
			Edge6 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 6);
			Edge7 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 7);
			Edge8 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 8);
			Edge9 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 9);
			Edge10 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 10);
			Edge11 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 11);
			Edge12 = shoreEdgePoints.Exists(instance => instance.EdgePosition == 12);
		}

		private void ShiftEdgeValues()
		{
			List<EdgeConnection> edgePoints = new List<EdgeConnection>();

			if (Edge1)
				edgePoints.Add(new EdgeConnection(10));

			if (Edge2)
				edgePoints.Add(new EdgeConnection(11));

			if (Edge3)
				edgePoints.Add(new EdgeConnection(12));

			if (Edge4)
				edgePoints.Add(new EdgeConnection(1));

			if (Edge5)
				edgePoints.Add(new EdgeConnection(2));

			if (Edge6)
				edgePoints.Add(new EdgeConnection(3));

			if (Edge7)
				edgePoints.Add(new EdgeConnection(4));

			if (Edge8)
				edgePoints.Add(new EdgeConnection(5));

			if (Edge9)
				edgePoints.Add(new EdgeConnection(6));

			if (Edge10)
				edgePoints.Add(new EdgeConnection(7));

			if (Edge11)
				edgePoints.Add(new EdgeConnection(8));

			if (Edge12)
				edgePoints.Add(new EdgeConnection(9));

			SetShoreEdgePoints(edgePoints);
		}

		private MapGraphicsTile GetTileSides(MapGraphicsTile mapGraphicsTile)
		{
			mapGraphicsTile.TopEdgeType = TopEdge;
			mapGraphicsTile.BottomEdgeType = BottomEdge;
			mapGraphicsTile.LeftEdgeType = LeftEdge;
			mapGraphicsTile.RightEdgeType = RightEdge;

			return mapGraphicsTile;
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
					_mapTiles = new BitmapImage(new Uri(_filename.Replace(".xml", ".png")));
				}
				else
					_mapTiles = new BitmapImage(new Uri(_filename));

				DrawImageOnCanvas(_currentPoint);
			}
			else
				OnRequestClose();
		}

		private void DrawImageOnCanvas(Point point)
		{
			CurrentTile = new CroppedBitmap(_mapTiles, new Int32Rect((int)point.X, (int)point.Y, 64, 64));
			_currentPoint = point;
		}		

		void OnRequestClose()
		{
			EventHandler handler = this.RequestClose;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}
		#endregion		
	}
}
