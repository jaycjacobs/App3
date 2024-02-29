using Cirros;
using Cirros.Utility;
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml.Shapes;

using Windows.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using Microsoft.UI;

namespace CirrosUI
{
    public sealed partial class Ruler : UserControl
    {
        private Orientation _orientation;
        private Point _origin = new Point(0, 0);
        //private double _scale = 1;
        private double _gridSpacing = 100;
        private uint _gridDivisions = 10;
        private double _contentWidth = 2000;
        private double _contentHeight = 2000;
        private double _width = 0;
        private double _height = 0;
        private bool _showCursorLocation = true;

        private Line _cursorTick = null;
        CanvasControl _canvasControl = null;

        public Ruler()
        {
            this.InitializeComponent();

            this.Loaded += Ruler_Loaded;
            this.Unloaded += Ruler_Unloaded;
            this.SizeChanged += Ruler_SizeChanged;
            this.Visibility = Visibility.Collapsed;

            Globals.Events.OnCoordinateDisplay += Events_OnCoordinateDisplay;

            DataContext = Globals.UIDataContext;

            if (_canvasControl == null)
            {
                _canvas.Children.Clear();
                _canvasControl = new CanvasControl();
                _canvasControl.Draw += _canvasControl_Draw;
                _canvas.Children.Add(_canvasControl);
            }
            if (_cursorTick != null)
            {
                _canvas.Children.Add(_cursorTick);
            }
        }

        private void _canvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            _gridSpacing = Globals.DrawingCanvas.VectorListControl.DisplayGridSpacing;
            _gridDivisions = Globals.DrawingCanvas.VectorListControl.DisplayGridDivisions;

            double smallSpace = _gridSpacing / _gridDivisions;

            if (Globals.ActiveDrawing != null)
            {
                double ox = 0;
                double oy = 0;

                double dspace = Globals.View.ModelToDisplay(_gridSpacing);
                int textMod = dspace > 40 ? 1 : 2;

                if ((Globals.ActiveDrawing.Origin.X % _gridSpacing) != 0)
                {
                    ox = -(_gridSpacing - (Globals.ActiveDrawing.Origin.X - (Math.Floor(Globals.ActiveDrawing.Origin.X / _gridSpacing) * _gridSpacing)));
                }
                if ((Globals.ActiveDrawing.Origin.Y % _gridSpacing) != 0)
                {
                    oy = -(_gridSpacing - (Globals.ActiveDrawing.Origin.Y - (Math.Floor(Globals.ActiveDrawing.Origin.Y / _gridSpacing) * _gridSpacing)));
                }

                Point min = Globals.ActiveDrawing.PaperToModel(new Point(0, Globals.ActiveDrawing.PaperSize.Height));
                Point max = Globals.ActiveDrawing.PaperToModel(new Point(Globals.ActiveDrawing.PaperSize.Width, 0));

                double minorSpacing = _gridSpacing / _gridDivisions;

                Color gridColor = Colors.Black;
                float tickThickness = .3f;

                CanvasTextFormat ctf = new CanvasTextFormat();
                ctf.FontFamily = "Arial";
                ctf.HorizontalAlignment = CanvasHorizontalAlignment.Left;
                ctf.FontSize = (float)Globals.UIDataContext.UIFontSizeExtraSmall;

                bool showUnit = Globals.ActiveDrawing.ModelUnit != Cirros.Drawing.Unit.Millimeters;

                if (_orientation == Orientation.Horizontal)
                {
                    _border.BorderThickness = new Thickness(0, 0, 0, .8);
                    int textCount = 0;

                    for (double xl = min.X + ox; xl <= max.X; xl += _gridSpacing)
                    {
                        Point p = Globals.View.ModelToDisplay(new Point(xl, max.Y));
                        if (p.X > 0 && p.X < this.ActualWidth)
                        {
                            args.DrawingSession.DrawLine((float)p.X, 9, (float)p.X, 19, gridColor, tickThickness);

                            if (textCount++ % textMod == 0)
                            {
                                string s = Utilities.FormatDistance(xl, Globals.DimensionRound,
                                    Globals.ActiveDrawing.IsArchitecturalScale, showUnit, Globals.ActiveDrawing.ModelUnit, true);
                                args.DrawingSession.DrawText(s, (float)p.X + 1, 1, gridColor, ctf);
                            }
                        }

                        for (int xs = 1; xs < _gridDivisions; xs++)
                        {
                            double gx = xl + minorSpacing * xs;

                            if (gx < min.X)
                                continue;
                            if (gx > max.X)
                                break;

                            p = Globals.View.ModelToDisplay(new Point(gx, max.Y));
                            if (p.X > 0 && p.X < this.ActualWidth)
                            {
                                args.DrawingSession.DrawLine((float)p.X, 14, (float)p.X, 19, gridColor, tickThickness);
                            }
                        }
                    }
                }
                else
                {
                    _border.BorderThickness = new Thickness(0, 0, .8, 0);
                    int textCount = 0;

                    Matrix3x2 rotationMatrix = Matrix3x2.CreateRotation(-(float)(Math.PI / 2));
                    Matrix3x2 saveMatrix = args.DrawingSession.Transform;

                    for (double yl = min.Y + oy; yl < max.Y; yl += _gridSpacing)
                    {
                        Point p = Globals.View.ModelToDisplay(new Point(min.X, yl));
                        if (p.Y > 0 && p.Y < this.ActualHeight)
                        {
                            args.DrawingSession.DrawLine(9, (float)p.Y, 19, (float)p.Y, gridColor, tickThickness);

                            if (textCount++ % textMod == 0)
                            {
                                string s = Utilities.FormatDistance(yl, Globals.DimensionRound,
                                    Globals.ActiveDrawing.IsArchitecturalScale, showUnit, Globals.ActiveDrawing.ModelUnit, true);

                                Matrix3x2 matrix = Matrix3x2.CreateTranslation(1, -(float)p.Y);
                                matrix = matrix * rotationMatrix;
                                matrix = matrix * Matrix3x2.CreateTranslation(1, (float)p.Y);

                                args.DrawingSession.Transform = matrix;
                                args.DrawingSession.DrawText(s, 1, (float)p.Y + 1, gridColor, ctf);
                                args.DrawingSession.Transform = saveMatrix;
                            }
                        }

                        for (int ys = 1; ys < _gridDivisions; ys++)
                        {
                            double gy = yl + minorSpacing * ys;

                            if (gy < min.Y)
                                continue;
                            if (gy > max.Y)
                                break;

                            p = Globals.View.ModelToDisplay(new Point(min.X, gy));
                            if (p.Y > 0 && p.Y < this.ActualHeight)
                            {
                                args.DrawingSession.DrawLine(14, (float)p.Y, 19, (float)p.Y, gridColor, tickThickness);
                            }
                        }
                    }
                }
            }
            else
            {
                Analytics.ReportEvent("ruler_no_active_drawing");
            }
        }

        void Events_OnCoordinateDisplay(object sender, CoordinateDisplayEventArgs e)
        {
            if (_showCursorLocation)
            {
                if (_cursorTick == null)
                {
                    _cursorTick = new Line();

                    if (_orientation == Orientation.Horizontal)
                    {
                        _cursorTick.Style = (Style)this.Resources["CursorHorizontalTickStyle"];
                    }
                    else
                    {
                        _cursorTick.Style = (Style)this.Resources["CursorVerticalTickStyle"];
                    }
                    _canvas.Children.Add(_cursorTick);
                }

                Point p = Globals.View.PaperToDisplay(e.Point);

                try
                {
                    if (_orientation == Orientation.Horizontal)
                    {
                        _cursorTick.SetValue(Canvas.LeftProperty, Math.Round(p.X));
                    }
                    else
                    {
                        _cursorTick.SetValue(Canvas.TopProperty, Math.Round(p.Y));
                    }
                }
                catch (Exception ex)
                {
                    Analytics.ReportError(string.Format("Ruler set tick ({0},{1}", p.X, p.Y), ex, 2, 501);
                }
            }
        }

        void Ruler_Unloaded(object sender, RoutedEventArgs e)
        {
            Globals.Events.OnCoordinateDisplay -= Events_OnCoordinateDisplay;
        }

        void Ruler_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //if (e.PreviousSize == e.NewSize) return;
            //if (Cirros.Utility.Utilities.__checkSizeChanged(50, sender)) return;

            _gridSpacing = Globals.GridSpacing;
            _gridDivisions = (uint)Globals.GridDivisions;

            _width = Math.Min(_contentWidth, this.ActualWidth);
            _height = Math.Min(_contentHeight, this.ActualHeight);
            _clipRectGeometry.Rect = new Rect(0, 0, _width, _height);
            _canvasControl.Width = _width;
            _canvasControl.Height = _height;
        }

        void Ruler_Loaded(object sender, RoutedEventArgs e)
        {
            //_cursorTick = new Line();

            //if (_orientation == Orientation.Horizontal)
            //{
            //    _cursorTick.Style = (Style)this.Resources["CursorHorizontalTickStyle"];
            //}
            //else
            //{
            //    _cursorTick.Style = (Style)this.Resources["CursorVerticalTickStyle"];
            //}

            Render();
        }

        //public bool ShowCursorLocation
        //{
        //    get
        //    {
        //        return _showCursorLocation;
        //    }
        //    set
        //    {
        //        _showCursorLocation = value;
        //        _cursorTick.Visibility = _showCursorLocation ? Visibility.Visible : Visibility.Collapsed;
        //    }
        //}

        //public Point Origin
        //{
        //    get
        //    {
        //        return _origin;
        //    }
        //    set
        //    {
        //        _origin = value;
        //    }
        //}

        //public double Scale
        //{
        //    get
        //    {
        //        return _scale;
        //    }
        //    set
        //    {
        //        _scale = value;
        //    }
        //}

        //public double Spacing
        //{
        //    get
        //    {
        //        return _gridSpacing;
        //    }
        //    set
        //    {
        //        _gridSpacing = value;
        //    }
        //}

        //public double ContentWidth
        //{
        //    get
        //    {
        //        return _contentWidth;
        //    }
        //    set
        //    {
        //        _contentWidth = value;
        //    }
        //}

        //public double ContentHeight
        //{
        //    get
        //    {
        //        return _contentHeight;
        //    }
        //    set
        //    {
        //        _contentWidth = value;
        //    }
        //}

        //public int Divisions
        //{
        //    get
        //    {
        //        return _gridDivisions;
        //    }
        //    set
        //    {
        //        _gridDivisions = value;
        //    }
        //}

        public Orientation Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                _orientation = value;
            }
        }

        public void Render()
        {
            _canvasControl.Invalidate();
        }
    }
}
