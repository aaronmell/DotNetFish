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
            MapGraphicsTileSet = new MapGraphicsTileSet(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\MapTiles.xml");
            _errorTilePosition = -1;
        }

        private RelayCommand<KeyEventArgs> _onKeyDown;
        private RelayCommand<KeyEventArgs> _onKeyUp;
        private RelayCommand _nextErrorTile;
        private RelayCommand _lastErrorTile;
        private bool _isShiftKeyDown;
        private bool _isCtrlKeyDown;
        private int _errorTilePosition;

        public ICommand OnKeyDown
        {
            get
            {
                if (_onKeyDown == null)
                    _onKeyDown = new RelayCommand<KeyEventArgs>(param => OnKeyDownCommand(param));

                return _onKeyDown;
            }
        }

        public ICommand OnKeyUp
        {
            get
            {
                if (_onKeyUp == null)
                    _onKeyUp = new RelayCommand<KeyEventArgs>(param => OnKeyUpCommand(param));

                return _onKeyUp;
            }
        }

        public ICommand NextErrorTile
        {
            get
            {
                if (_nextErrorTile == null)
                    _nextErrorTile= new RelayCommand(() => CycleErrorTiles(1));

                return _nextErrorTile;
            }
        }

        public ICommand LastErrorTile
        {
            get
            {
                if (_lastErrorTile == null)
                    _lastErrorTile = new RelayCommand(() => CycleErrorTiles(-1));

                return _lastErrorTile;
            }
        }

        private void CycleErrorTiles(int direction)
        {
            _errorTilePosition += direction;

            if (_errorTilePosition < 0)
                _errorTilePosition = GameWorld.ErrorTiles.Count - 1;

            if (_errorTilePosition > GameWorld.ErrorTiles.Count - 1)
                _errorTilePosition = 0;

            CurrentPoint = new Point(GameWorld.ErrorTiles[_errorTilePosition].X,GameWorld.ErrorTiles[_errorTilePosition].Y);
        }

        public int MapCanvasTilesWidth {get; set;}
        public int MapCanvasTilesHeight { get; set; }

        private void OnKeyDownCommand(KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right)
                MoveCanvas(e.Key);

            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                _isCtrlKeyDown = true;

            if (e.Key == Key.RightShift || e.Key == Key.LeftShift)
                _isShiftKeyDown = true;
        }

        private void OnKeyUpCommand(KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                _isCtrlKeyDown = false;

            if (e.Key == Key.RightShift || e.Key == Key.LeftShift)
                _isShiftKeyDown = false;
        }

        private void MoveCanvas(Key key)
        {
            int multiplier = 1;

            if (_isShiftKeyDown)
                multiplier = 8;
            if (_isCtrlKeyDown)
                multiplier = 32;

            if (key == Key.Down && (int)CurrentPoint.Y + (MapCanvasTilesHeight / 2) + multiplier < GameWorld.GameMapHeight)
                CurrentPoint = new Point(CurrentPoint.X, CurrentPoint.Y + multiplier);                
            if (key == Key.Up && (int)CurrentPoint.Y - (MapCanvasTilesHeight / 2) - multiplier >= 0)
                CurrentPoint = new Point(CurrentPoint.X, CurrentPoint.Y - multiplier);
			if (key == Key.Left && (int)CurrentPoint.X - (MapCanvasTilesWidth / 2) - multiplier >= 0)
                CurrentPoint = new Point(CurrentPoint.X - multiplier, CurrentPoint.Y);
            if (key == Key.Right && (int)CurrentPoint.X + (MapCanvasTilesWidth / 2) + multiplier < GameWorld.GameMapWidth)
                CurrentPoint = new Point(CurrentPoint.X + multiplier, CurrentPoint.Y);
        }
	}
}
