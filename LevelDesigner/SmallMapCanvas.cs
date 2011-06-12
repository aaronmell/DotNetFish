using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using DotNetFish.GameObjects;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Media.Imaging;

namespace DotNetFish.Wpf.LevelDesigner
{
    public class SmallMapCanvas : Canvas
    {
        #region [Declarations]
        private Dictionary<Point, CachedBitmap> _mapTiles;
        private bool _isInitialized;
        #endregion

        #region [Properties]

        public Point CurrentPointObject
        {
            get
            {
                return (Point)GetValue(CurrentPointCommandProperty);
            }
            set
            {
                SetValue(CurrentPointCommandProperty, value);
                this.InvalidateVisual();
            }
        }

        public GameWorld GameWorldObject
        {
            get
            {
                return (GameWorld)GetValue(GameWorldCommandProperty);
            }
            set
            {
                SetValue(GameWorldCommandProperty, value);
            }
        }

        public MapGraphicsTileSet TileSetObject
        {
            get
            {
                return (MapGraphicsTileSet)GetValue(TileSetCommandProperty);
            }
            set
            {
                SetValue(TileSetCommandProperty, value);
            }
        }

        public int TilesWideObject
        {
            get
            {
                return (int)GetValue(TilesWideCommandProperty);
            }
            set
            {
                SetValue(TilesWideCommandProperty, value);
            }
        }

        public int TilesHighObject
        {
            get
            {
                return (int)GetValue(TilesHighCommandProperty);
            }
            set
            {
                SetValue(TilesHighCommandProperty, value);
            }
        }

        #endregion

        #region [DependencyProperties]
        public static readonly DependencyProperty CurrentPointCommandProperty =
        DependencyProperty.Register(
            "CurrentPointObject",
            typeof(Point),
            typeof(SmallMapCanvas),
            new PropertyMetadata(new PropertyChangedCallback(OnPropertyChanges))
            );

        public static readonly DependencyProperty GameWorldCommandProperty =
        DependencyProperty.Register(
            "GameWorldObject",
            typeof(GameWorld),
            typeof(SmallMapCanvas),
            new PropertyMetadata(new PropertyChangedCallback(OnPropertyChanges))
        );

        public static readonly DependencyProperty TileSetCommandProperty =
            DependencyProperty.Register(
                "TileSetObject",
                typeof(MapGraphicsTileSet),
                typeof(SmallMapCanvas),
                 new PropertyMetadata(new PropertyChangedCallback(OnPropertyChanges))
            );

        public static readonly DependencyProperty TilesHighCommandProperty =
            DependencyProperty.Register(
                "TilesHighObject",
                typeof(int),
                typeof(SmallMapCanvas)
            );

        public static readonly DependencyProperty TilesWideCommandProperty =
           DependencyProperty.Register(
               "TilesWideObject",
               typeof(int),
               typeof(SmallMapCanvas)
           );
        #endregion

        #region [Events]
        private static void OnPropertyChanges(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            (obj as SmallMapCanvas).InvalidateVisual();
        }

        protected override void OnRender(System.Windows.Media.DrawingContext dc)
        {
            base.OnRender(dc);

            if (GameWorldObject != null && TileSetObject != null)
            {
                if (!_isInitialized)
                    Initialize();

                DrawTiles(dc);
            }
        }
        #endregion

        #region [Methods]
        private void Initialize()
        {
            _mapTiles = TileSetObject.GetTileImages();
            _isInitialized = true;
        }

        private void DrawTiles(System.Windows.Media.DrawingContext dc)
        {
            TilesWideObject = (int)(this.ActualWidth / 4) + 1;
            TilesHighObject = (int)(this.ActualHeight / 4) + 1;

            int startX = (int)CurrentPointObject.X - (TilesWideObject / 2);
            int startY = (int)CurrentPointObject.Y - (TilesHighObject / 2);

            if (startX < 0)
                startX = 0;

            if (startY < 0)
                startY = 0;

            int endX = startX + TilesWideObject;
            int endY = startY + TilesHighObject;

            if (endX > GameWorldObject.GameMapWidth)
            {
                endX = GameWorldObject.GameMapWidth;
                startX = endX - TilesWideObject;
            }
            else if (startX < 0)
            {
                startX = 0;
                endX = TilesWideObject;
            }

            if (endY > GameWorldObject.GameMapHeight)
            {
                endY = GameWorldObject.GameMapHeight;
                startY = endY - TilesHighObject;
            }
            else if (startY < 0)
            {
                startY = 0;
                endY = TilesHighObject;
            }

            int CountX = 0;
            int CountY = 0;

            DrawingGroup dg = new DrawingGroup();

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    dg.Children.Add(
                        new ImageDrawing(_mapTiles[GameWorldObject.GameMap[x, y].GraphicsTile.TileStartPoint],
                            new Rect(CountX * 4, CountY * 4, 4, 4)
                            ));

                    dg.Children.Add(
                        new GeometryDrawing(
                            null,
                            new Pen(
                                new SolidColorBrush(
                                    Color.FromRgb(255, 0, 20)), .1),
                                    new RectangleGeometry(
                                        new Rect(CountX * 4, CountY * 4, 4, 4)
                                    )
                                )
                            );

                    CountY++;
                }
                CountY = 0;
                CountX++;
            }


            dg.Freeze();
            dc.DrawDrawing(dg);
            //dc.DrawDrawing(DrawMainMapLocation())
        }
        #endregion
    }
}
