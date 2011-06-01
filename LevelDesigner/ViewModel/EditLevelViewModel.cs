using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight;
using System.Windows;
using DotNetFish.GameObjects;
using System.Diagnostics;

namespace DotNetFish.Wpf.LevelDesigner.ViewModel
{
	public class EditLevelViewModel : BaseEditorViewModel
	{

        public EditLevelViewModel(GameWorld gameWorld, Point currentPoint)
        {
            CurrentPoint = currentPoint;
            GameWorld = gameWorld;
            _gameWorldHeight = gameWorld.GameMap.GetLength(0);
            _gameWorldWidth = gameWorld.GameMap.GetLength(1);
            MapGraphicsTileSet = new MapGraphicsTileSet(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\MapTiles.xml");
        }

        private RelayCommand<KeyEventArgs> _onKeyDown;
        private int _gameWorldHeight;
        private int _gameWorldWidth;

        public ICommand OnKeyDown
        {
            get
            {
                if (_onKeyDown == null)
                    _onKeyDown = new RelayCommand<KeyEventArgs>(param => OnKeyDownCommand(param));

                return _onKeyDown;
            }
        }

        public int MapCanvasTilesWidth {get; set;}
        public int MapCanvasTilesHeight { get; set; }


        private void OnKeyDownCommand(KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right)
            {
                MoveCanvas(e.Key);
            }
        }

        private void MoveCanvas(Key key)
        {
            if (key == Key.Down && (int)CurrentPoint.Y + (MapCanvasTilesHeight / 2) + 1 < _gameWorldHeight)
                CurrentPoint = new Point(CurrentPoint.X, CurrentPoint.Y + 1);                
            if (key == Key.Up && (int)CurrentPoint.Y - (MapCanvasTilesHeight / 2) - 1 >= 0)
                CurrentPoint = new Point(CurrentPoint.X, CurrentPoint.Y - 1);
			if (key == Key.Left && (int)CurrentPoint.X - (MapCanvasTilesWidth / 2) -1 >= 0)
                CurrentPoint = new Point(CurrentPoint.X - 1, CurrentPoint.Y);
            if (key == Key.Right && (int)CurrentPoint.X + (MapCanvasTilesWidth / 2) + 1 < _gameWorldWidth)
                CurrentPoint = new Point(CurrentPoint.X + 1, CurrentPoint.Y);
        }
	}
}
