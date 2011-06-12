using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using DotNetFish.GameObjects;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Input;

namespace DotNetFish.Wpf.LevelDesigner
{
	public class MainEditorCanvas : Canvas
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
            typeof(MainEditorCanvas),
            new PropertyMetadata(new PropertyChangedCallback(OnPropertyChanges))
            );

        public static readonly DependencyProperty GameWorldCommandProperty =
        DependencyProperty.Register(
            "GameWorldObject",
            typeof(GameWorld),
            typeof(MainEditorCanvas),
            new PropertyMetadata(new PropertyChangedCallback(OnPropertyChanges))
        );

        public static readonly DependencyProperty TileSetCommandProperty =
            DependencyProperty.Register(
                "TileSetObject",
                typeof(MapGraphicsTileSet),
                typeof(MainEditorCanvas),
                 new PropertyMetadata(new PropertyChangedCallback(OnPropertyChanges))
            );

        public static readonly DependencyProperty TilesHighCommandProperty =
            DependencyProperty.Register(
                "TilesHighObject",
                typeof(int),
                typeof(MainEditorCanvas)
            );

        public static readonly DependencyProperty TilesWideCommandProperty =
           DependencyProperty.Register(
               "TilesWideObject",
               typeof(int),
               typeof(MainEditorCanvas)
           );
        #endregion

        #region [Events]
        private static void OnPropertyChanges(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            (obj as MainEditorCanvas).InvalidateVisual();
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
            TilesWideObject = (int)(this.ActualWidth / 64) + 1;
            TilesHighObject = (int)(this.ActualHeight / 64) + 1;

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
                           new Rect(CountX * 64, CountY * 64, 64, 64)
                           ));

                    dg.Children.Add(
                        new GeometryDrawing(
                            null,
                            new Pen(
                                new SolidColorBrush(
                                    Color.FromRgb(255, 0, 20)), .3),
                                    new RectangleGeometry(
                                        new Rect(CountX * 64, CountY * 64, 64, 64)
                                    )
                                )
                            );
#if DEBUG
                    if (x % 5 == 0 && y % 5 == 0)
                    {
                        FormattedText text = new FormattedText("X:" + x + " Y:" + y,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("arial"),
                        12,
                        new SolidColorBrush(Color.FromRgb(255, 0, 20)));

                        Geometry geometry = text.BuildGeometry(new Point(CountX * 64, CountY * 64));
                        GeometryDrawing gd = new GeometryDrawing(new SolidColorBrush(Color.FromRgb(255, 0, 20)), new Pen(), geometry);
                        dg.Children.Add(gd);
                    }                    
#endif                  

                    CountY++;
                }
                CountY = 0;
                CountX++;
            }
            dg.Freeze();
            dc.DrawDrawing(dg);
        }
        #endregion


        
    }
}
