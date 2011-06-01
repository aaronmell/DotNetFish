using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight;
using DotNetFish.GameObjects;
using System.Windows;
using System.Diagnostics;

namespace DotNetFish.Wpf.LevelDesigner.ViewModel
{
    public class BaseEditorViewModel : ViewModelBase
    {
        private GameWorld _gameWorld;
        private Point _currentPoint;
        private MapGraphicsTileSet _mapGraphicsTileSet;

        public GameWorld GameWorld
        {
            get
            {
                return _gameWorld;
            }
            set
            {
                _gameWorld = value;
                RaisePropertyChanged("GameWorld");
            }
        }
        public Point CurrentPoint
        {
            get
            {
                return _currentPoint;
            }
            set
            {
                _currentPoint = value;
                RaisePropertyChanged("CurrentPoint");
            }
        }
        
        public MapGraphicsTileSet MapGraphicsTileSet
        {
            get
            {  
                return _mapGraphicsTileSet;
            }
            set
            {
                _mapGraphicsTileSet = value;
                RaisePropertyChanged("MapGraphicsTileSet");
            }
        }
    }
}
