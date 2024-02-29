using Cirros.Core;
using Cirros.Core.ConstructHandles;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using App3;
using Microsoft.UI;

namespace Cirros.Drawing
{
    public class DrawingTools
    {
        Point _triangleLocation = new Point(0, 0);
        CompositeTransform _triangleTransform = new CompositeTransform();
        RotateTransform _triangleRotateTransform = new RotateTransform();

        bool _triangleFirstRun = false;

        uint _fillColor = 0x4000ff00;
        uint _strokeColor = 0xff00a000;
        uint _alphaChannel = 0x40000000;

        Point _rotateHandleLocation = new Point(0, 0);
        Point _rotateLocation = new Point(0, 0);
        Point _rotateAnchor = new Point(0, 0);
        double _rotateHandleBaseAngle = 0;
        RotateTransform _rotateHandleTransform = new RotateTransform();

        VectorEntity _veTriangle = null;
        VectorEntity _veRotateHandle = null;

        VectorEntity _veHandle = null;
        VectorMarkerEntity _vmHandle = null;

        string _rotateHandleTag;
        string _handleTag;

        const double cRotateHandleSize = 18;
        const double cHandleSize = 4;

        double _scale = .5;
        double _maxScale = 1;
        double _minScale = .25;

        double _rotation = 0;
        bool _flipx = false;

        ConstructHandles _constructHandles = new ConstructHandles();

        bool _insideTools = false;
        Point _toolsCenter = new Point();
        double _showToolsDistance = 0;
        double _toolsFadeDistance = 20;

        // 30 degree triangle
        List<Point> _outerPoints30 = new List<Point>() { new Point(-192.33, -666.66), new Point(-192.33, 333.34), new Point(384.67, 333.34) };
        List<Point> _innerPoints30 = new List<Point>() { new Point(-57.33, 198.34), new Point(142.67, 198.34), new Point(-57.33, -151.66) };

        // 45 degree triangle
        List<Point> _outerPoints45 = new List<Point>() { new Point(-333.33, -666.66), new Point(-333.33, 333.34), new Point(666.67, 333.34) };
        List<Point> _innerPoints45 = new List<Point>() { new Point(-133.33, 133.34), new Point(181.67, 133.34), new Point(-133.33, -181.66) };

        List<Point> _outerPointsRaw = null;
        List<Point> _innerPointsRaw = null;

        List<Point> _outerPoints = null;
        List<Point> _innerPoints = null;

        bool _isDragging = false;
        bool _isRotating = false;

        Point _dragOffset = new Point();

        Canvas _toolsOverlay = null;
        Grid _toolsOptions = null;

        Canvas _canvas = null;

        public DrawingTools(Canvas canvas)
        {
            _canvas = canvas;
        }

        private void SetToolHandlers(FrameworkElement e)
        {
            if (e is Button)
            {
                ((Button)e).Click += Tool_Click;
                ((Button)e).Foreground = new SolidColorBrush(Globals.ActiveDrawing.Theme.ForegroundColor);
            }
            else if (e is Panel)
            {
                foreach (FrameworkElement c in ((Panel)e).Children)
                {
                    SetToolHandlers(c);
                }
            }
        }

        private TriangleType _triangleType = TriangleType.None;

        public enum TriangleType
        {
            Triangle30,
            Triangle45,
            None
        }

        private void ResetTriangle(TriangleType triType)
        {
            if (triType == TriangleType.Triangle45)
            {
                _outerPointsRaw = _outerPoints45;
                _innerPointsRaw = _innerPoints45;
                _triangleType = TriangleType.Triangle45;
            }
            else
            {
                _outerPointsRaw = _outerPoints30;
                _innerPointsRaw = _innerPoints30;
                _triangleType = TriangleType.Triangle30;
            }

            _triangleTransform.Rotation = _rotation;
        }

        private void CreateTriangle()
        {
            try
            {
                IDrawingPage dp = (IDrawingPage)App.Window.Frame;
                _toolsOverlay = dp.ToolsOverlay;
                _toolsOptions = dp.DrawingToolsTools;

                //_toolsOverlay.PointerReleased += _toolsOverlay_PointerReleased;
                _toolsOverlay.PointerMoved += _toolsOverlay_PointerMoved;
                _toolsOverlay.PointerPressed += _toolsOverlay_PointerPressed;

                if (_toolsOverlay != null)
                {
                    try
                    {
                        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                        if (localSettings.Containers.ContainsKey("drawing_tools"))
                        {
                            ApplicationDataContainer toolsSettings = localSettings.Containers["drawing_tools"];
                            _triangleLocation.X = (double)toolsSettings.Values["location_x"];
                            _triangleLocation.Y = (double)toolsSettings.Values["location_y"];
                            _scale = (double)toolsSettings.Values["scale"];
                            _rotation = (double)toolsSettings.Values["rotation"];
                            _triangleType = (string)toolsSettings.Values["type"] == "30" ? TriangleType.Triangle30 : TriangleType.Triangle45;
                            _strokeColor = (uint)toolsSettings.Values["stroke"];
                            _fillColor = (uint)toolsSettings.Values["fill"];
                            _alphaChannel = toolsSettings.Values.ContainsKey("alpha") ? (uint)toolsSettings.Values["alpha"] : 0x40000000;
                        }
                        else
                        {
                            _triangleFirstRun = true;

                            _triangleLocation.X = _toolsOverlay.ActualWidth * .3;
                            _triangleLocation.Y = _toolsOverlay.ActualHeight * .6;
                            _scale = (Math.Max(_toolsOverlay.ActualWidth, _toolsOverlay.ActualHeight) / 1000) * .3;
                            _rotation = 0;
                            _triangleType = TriangleType.Triangle30;
                        }
                    }
                    catch
                    {
                        _triangleLocation.X = _toolsOverlay.ActualWidth * .3;
                        _triangleLocation.Y = _toolsOverlay.ActualHeight * .6;
                        _scale = (Math.Max(_toolsOverlay.ActualWidth, _toolsOverlay.ActualHeight) / 1000) * .3; ;
                        _rotation = 0;
                        _triangleType = TriangleType.Triangle45;
                    }

                    _triangleTransform.Rotation = _rotation;

                    if (_toolsOptions == null)
                    {
                        //foreach (FrameworkElement e in _toolsOverlay.Children)
                        //{
                        //    if (e is Grid)
                        //    {
                        //        _toolsOptions = e as Grid;
                        //        _toolsOptions.SetValue(Canvas.ZIndexProperty, 100002);

                        //        SetToolHandlers(_toolsOptions);
                        //    }
                        //}
                    }
                    else
                    {
                        _toolsOptions.SetValue(Canvas.ZIndexProperty, 100002);
                        SetToolHandlers(_toolsOptions);
                    }

                    _maxScale = (_toolsOverlay.ActualWidth + _toolsOverlay.ActualHeight) / 2000;
                    _minScale = _maxScale / 4;

                    ResetTriangle(_triangleType);

                    _toolsOptions.SetValue(Canvas.ZIndexProperty, 100001);

                    if (_veTriangle == null)
                    {
                        _veTriangle = new VectorEntity(1100003, 1100000);

                        _veTriangle.Color = Utilities.ColorFromColorSpec(_strokeColor);
                        _veTriangle.Fill = true;
                        _veTriangle.FillColor = Utilities.ColorFromColorSpec(_fillColor);
                        _veTriangle.FillEvenOdd = true;
                        _veTriangle.LineWidth = Globals.View.DisplayToPaper(.5);
                        _veTriangle.IsVisible = false;

                        Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_veTriangle);

                        TriangleChanged();
                    }

                    if (_veHandle == null)
                    {
                        _veHandle = new VectorEntity(1100000, 1100000);

                        _veHandle.Color = Globals.ActiveDrawing.Theme.HandleColor;
                        _veHandle.Fill = true;
                        _veHandle.FillColor = Globals.ActiveDrawing.Theme.HandleFillColor;
                        _veHandle.LineWidth = Globals.View.DisplayToPaper(.5);
                        _veHandle.IsVisible = false;

                        _vmHandle = new VectorMarkerEntity();
                        _vmHandle.Type = Display.HandleType.Diamond;
                        _vmHandle.Size = cHandleSize * 2;
                        _vmHandle.Opacity = 1;
                        _veHandle.AddChild(_vmHandle);

                        Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_veHandle);
                    }

                    if (_veRotateHandle == null)
                    {
                        _veRotateHandle = new VectorEntity(1100001, 1100000);

                        _veRotateHandle.Color = Globals.ActiveDrawing.Theme.HandleColor;
                        _veRotateHandle.Fill = true;
                        _veRotateHandle.FillColor = Globals.ActiveDrawing.Theme.HandleFillColor;
                        _veRotateHandle.LineWidth = Globals.View.DisplayToPaper(.5);
                        _veRotateHandle.IsVisible = false;

                        Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_veRotateHandle);
                    }

                    Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                }
            }
            catch
            {
            }
        }

        List<object> RotateHandleObjects(Point location, Transform tf)
        {
            Point p1 = Globals.View.DisplayToPaper(location);
            Point d2 = tf.TransformPoint(new Point(Globals.View.DisplayToPaper(cRotateHandleSize - cHandleSize), 0));
            Point p2 = new Point(p1.X + d2.X, p1.Y + d2.Y);
            Point d3 = tf.TransformPoint(new Point(Globals.View.DisplayToPaper(cRotateHandleSize), 0));
            Point p3 = new Point(p1.X + d3.X, p1.Y + d3.Y);

            List<object> objects = new List<object>();

            List<Point> pc = new List<Point>();
            pc.Add(p1);
            pc.Add(p2);
            objects.Add(pc);

            VectorArcEntity va = new VectorArcEntity();
            va.Radius = Globals.View.DisplayToPaper(cHandleSize);
            va.Center = p3;
            va.IsCircle = true;
            objects.Add(va);

            return objects;
        }

        Point _rotateBaseLocation;

        public void _toolsOverlay_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_rotateHandleTag != "off")
            {
                _isRotating = true;

                _rotateBaseLocation = _triangleLocation;

                _veHandle.FillColor = Colors.LimeGreen;
                _veHandle.IsVisible = true;
                _vmHandle.Location = Globals.View.DisplayToPaper(_rotateAnchor);
                _veHandle.UpdateBox();

                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

                _triangleRotateTransform.CenterX = _rotateAnchor.X;
                _triangleRotateTransform.CenterY = _rotateAnchor.Y;
                _triangleRotateTransform.Angle = 0;

                _rotateHandleBaseAngle = Construct.Angle(_rotateAnchor, _rotateLocation) * Construct.cRadiansToDegrees;
            }
            else
            {
                PointerPoint pp = e.GetCurrentPoint(_toolsOverlay);
                Point p = pp.Position;
                bool insideTools = PointInsideTools(p);
                if (_insideTools != insideTools)
                {
                    _insideTools = insideTools;
                    ShowTools(_insideTools);
                }
                else
                {
                    _triangle_PointerPressed(sender, e);
                }
            }
        }

        public void _toolsOverlay_PointerMoved(object sender, PointerRoutedEventArgs args)
        {
            if (Globals.ShowDrawingTools)
            {
                PointerPoint pp = args.GetCurrentPoint(_toolsOverlay);
                Point p = pp.Position;

                if (_isDragging)
                {
                    _triangleLocation.X = p.X - _dragOffset.X;
                    _triangleLocation.Y = p.Y - _dragOffset.Y;

                    if (_veHandle.IsVisible)
                    {
                        _vmHandle.Location = Globals.View.DisplayToPaper(p);
                        _veHandle.UpdateBox();

                        //Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                    }

                    TriangleChanged();

                    _constructHandles.ShowHandlesNear(Globals.View.DisplayToPaper(p));
                }
                else if (_isRotating)
                {
                    double angle = Construct.Angle(_rotateAnchor, p) * Construct.cRadiansToDegrees;
                    _rotateHandleTransform.Angle = angle;

                    double d = Construct.Distance(_rotateAnchor, _rotateLocation);
                    Point b = new Point(d, 0);
                    b = _rotateHandleTransform.TransformPoint(b);
                    b.X += _rotateAnchor.X;
                    b.Y += _rotateAnchor.Y;

                    _veRotateHandle.Children.Clear();
                    _veRotateHandle.Children.AddRange(RotateHandleObjects(b, _rotateHandleTransform));

                    TriangleChanged();
                    //Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

                    _triangleRotateTransform.Angle = angle - _rotateHandleBaseAngle;

                    _triangleTransform.Rotation = _triangleRotateTransform.Angle + _rotation;

                    _triangleLocation.X = p.X - _rotateAnchor.X;
                    _triangleLocation.Y = p.Y - _rotateAnchor.Y;
                    _triangleLocation = _triangleRotateTransform.TransformPoint(_rotateBaseLocation);

                    _constructHandles.ShowHandlesNear(Globals.View.DisplayToPaper(p));
                }
                else
                {
                    bool insideTools = PointInsideTools(p);
                    if (_insideTools != insideTools)
                    {
                        _insideTools = insideTools;
                        ShowTools(_insideTools);
                    }

                    MoveHandle(p);
                }
            }
        }

        internal bool PointInsideTools(Point p)
        {
            return (Construct.Distance(p, _toolsCenter) - _showToolsDistance) < _toolsFadeDistance;
        }

        private void ShowTools(bool show)
        {
            if (_toolsOptions != null)
            {
                if (show)
                {
                    _toolsOptions.Visibility = Visibility.Visible;
                    _toolsOverlay.IsHitTestVisible = true;
                }
                else if (_toolsOptions.Visibility == Visibility.Visible)
                {
                    _toolsOptions.Visibility = Visibility.Collapsed;
                    _toolsOverlay.IsHitTestVisible = false;
                    _insideTools = false;
                }
            }
        }

        public void _toolsOverlay_PointerReleased(object sender, PointerRoutedEventArgs args)
        {
            PointerPoint pp = args.GetCurrentPoint(_toolsOverlay);
            Point p = pp.Position;

            if (_isDragging)
            {
                if (_veHandle.IsVisible)
                {
                    Point paper = Construct.RoundXY(Globals.View.DisplayToPaper(p));
                    _constructHandles.ActivePoint(ref paper);
                    _constructHandles.HideHandles();
                    p = Globals.View.PaperToDisplay(paper);
                    //Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                }

                _isDragging = false;
                _triangleLocation.X = p.X - _dragOffset.X;
                _triangleLocation.Y = p.Y - _dragOffset.Y;

                TriangleChanged();
            }
            else if (_isRotating)
            {
                if (_veHandle.IsVisible)
                {
                    Point paper = Construct.RoundXY(Globals.View.DisplayToPaper(p));
                    _constructHandles.ActivePoint(ref paper);
                    _constructHandles.HideHandles();
                    p = Globals.View.PaperToDisplay(paper);
                    //Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

                    double angle = Construct.Angle(_rotateAnchor, p) * Construct.cRadiansToDegrees;
                    double da = angle - _rotateHandleBaseAngle;
                    _triangleRotateTransform.Angle = da;
                }

                _isRotating = false;

                _triangleLocation = _triangleRotateTransform.TransformPoint(_rotateBaseLocation);
                _triangleTransform.Rotation = _rotation = _triangleRotateTransform.Angle + _rotation;

                _veRotateHandle.IsVisible = false;
                _rotateHandleTag = "off";

                TriangleChanged();
            }
        }

        private void SetTriangleColor(Color strokeColor, Color fillColor)
        {
            _strokeColor = Utilities.ColorSpecFromColor(strokeColor);
            _fillColor = Utilities.ColorSpecFromColor(fillColor);

            SaveTriangleSetting();

            if (_veTriangle != null)
            {
                _veTriangle.Color = strokeColor;
                _veTriangle.FillColor = fillColor;
            }
        }

        public void SetTriangleColor(uint colorspec)
        {
            _strokeColor = colorspec;
            _fillColor = (_strokeColor & 0x00ffffff) | _alphaChannel;

            SaveTriangleSetting();

            if (_veTriangle != null)
            {
                _veTriangle.Color = Utilities.ColorFromColorSpec(_strokeColor);
                _veTriangle.FillColor = Utilities.ColorFromColorSpec(_fillColor);
            }
        }

        public byte Opacity
        {
            get
            {
                return (byte)((_alphaChannel >> 24) & 0xff);
            }
            set
            {
                _alphaChannel = (uint)value << 24;
                _fillColor = (_strokeColor & 0x00ffffff) | _alphaChannel;
                _veTriangle.FillColor = Utilities.ColorFromColorSpec(_fillColor);

                SaveTriangleSetting();
            }
        }

        public void ZoomTriangle(double zoom)
        {
            double s = _scale * zoom;
            _scale = Math.Min(Math.Max(s, _minScale), _maxScale);
        }

        public void SetTriangleRotation(double rotation)
        {
            _triangleTransform.Rotation = _rotation = rotation;

            if (Globals.ShowDrawingTools == false)
            {
                Globals.ShowDrawingTools = true;
                ShowTriangle(Globals.ShowDrawingTools);
            }
            else
            {
                TriangleChanged();
            }
        }

        public void FlipTriangle(bool flip)
        {
            if (flip != _flipx)
            {
                _flipx = flip;
                TriangleChanged();
            }

            if (Globals.ShowDrawingTools == false)
            {
                Globals.ShowDrawingTools = true;
                ShowTriangle(Globals.ShowDrawingTools);
            }
        }

        public void SetTriangleType(TriangleType triangleType)
        {
            if (triangleType != _triangleType)
            {
                if (_veTriangle == null)
                {
                    CreateTriangle();
                }

                if (triangleType == TriangleType.Triangle30)
                {
                    //_rotation = 0;
                    ResetTriangle(TriangleType.Triangle30);
                    TriangleChanged();
                }
                else if (triangleType == TriangleType.Triangle45)
                {
                    //_rotation = 0;
                    ResetTriangle(TriangleType.Triangle45);
                    TriangleChanged();
                }
            }

            if (Globals.ShowDrawingTools == false)
            {
                Globals.ShowDrawingTools = true;
                ShowTriangle(Globals.ShowDrawingTools);
            }
        }

        void Tool_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                switch((string)((Button)sender).Tag)
                {
                    case "close":
                        Globals.ShowDrawingTools = false;
                        ShowTriangle(Globals.ShowDrawingTools);
                        break;

                    case "30":
                        if (_rotation != 0)
                        {
                            _flipx = false;
                        }
                        else if (_triangleType != TriangleType.Triangle30)
                        {
                            _flipx = false;
                        }
                        else
                        {
                            _flipx = !_flipx;
                        }

                        _rotation = 0;

                        ResetTriangle(TriangleType.Triangle30);

                        TriangleChanged();
                        break;

                    case "45":
                        if (_rotation != 0)
                        {
                            _flipx = false;
                        }
                        else if (_triangleType != TriangleType.Triangle45)
                        {
                            _flipx = false;
                        }
                        else
                        {
                            _flipx = !_flipx;
                        }
                        _rotation = 0;

                        ResetTriangle(TriangleType.Triangle45);

                        TriangleChanged();
                        break;

                    case "rotate":
                        if (_triangleType == TriangleType.Triangle30)
                        {
                            _rotation = (_rotation + 30) % 360;
                        }
                        else
                        {
                            _rotation = (_rotation + 45) % 360;
                        }
                        _triangleTransform.Rotation = _rotation;
                        TriangleChanged();
                        break;

                    case "bigger":
                        _scale = Math.Min(_scale * 1.1, _maxScale);
                        TriangleChanged();
                        break;

                    case "smaller":
                        _scale = Math.Max(_scale / 1.1, _minScale);
                        TriangleChanged();
                        break;

                    case "pink":
                        SetTriangleColor(Utilities.ColorFromARGB(255, 255, 105, 180), Utilities.ColorFromARGB(64, 255, 105, 180));
                        break;

                    case "yellow":
                        SetTriangleColor(Utilities.ColorFromARGB(255, 160, 160, 0), Utilities.ColorFromARGB(64, 255, 255, 0));
                        break;

                    case "green":
                        SetTriangleColor(Utilities.ColorFromARGB(255, 0, 160, 0), Utilities.ColorFromARGB(64, 0, 255, 0));
                        break;

                    case "orange":
                        SetTriangleColor(Utilities.ColorFromARGB(255, 255, 165, 0), Utilities.ColorFromARGB(64, 255, 165, 0));
                        break;

                    case "smoke":
                        SetTriangleColor(Utilities.ColorFromARGB(255, 133, 133, 133), Utilities.ColorFromARGB(64, 160, 160, 160));
                        break;
                }

                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
            }
        }

        double _tMinX = 0;
        double _tMinY = 0;
        double _tMaxX = 0;
        double _tMaxY = 0;

        public void TriangleChanged()
        {
            if (_outerPointsRaw != null && _outerPointsRaw.Count == 3)
            {
                _triangleTransform.ScaleX = _flipx ? -_scale : _scale;
                _triangleTransform.ScaleY = _scale;
                _triangleTransform.TranslateX = _triangleLocation.X;
                _triangleTransform.TranslateY = _triangleLocation.Y;

                _outerPoints = new List<Point>();

                double t1 = Globals.hitTolerance + cRotateHandleSize;
                double t2 = t1 + t1;
                Point tt = new Point(0, 0);

                foreach (Point p in _outerPointsRaw)
                {
                    Point t = _triangleTransform.TransformPoint(p);

                    tt.X += t.X;
                    tt.Y += t.Y;

                    if (_outerPoints.Count == 0)
                    {
                        _tMinX = t.X - t1;
                        _tMinY = t.Y - t1;
                        _tMaxX = t.X + t1;
                        _tMaxY = t.Y + t1;
                    }
                    else
                    {
                        _tMinX = Math.Min(_tMinX, t.X - t1);
                        _tMinY = Math.Min(_tMinY, t.Y - t1);
                        _tMaxX = Math.Max(_tMaxX, t.X + t1);
                        _tMaxY = Math.Max(_tMaxY, t.Y + t1);
                    }

                    _outerPoints.Add(t);
                }

                _innerPoints = new List<Point>();

                foreach (Point p in _innerPointsRaw)
                {
                    Point t = _triangleTransform.TransformPoint(p);
                    _innerPoints.Add(t);
                }

                if (_toolsOptions != null)
                {
                    tt.X /= _outerPoints.Count;
                    tt.Y /= _outerPoints.Count;
                    _toolsCenter = tt;

                    tt.X -= (_toolsOptions.Width / 2);
                    tt.Y -= (_toolsOptions.Height / 2);

                    _toolsOptions.SetValue(Canvas.LeftProperty, tt.X);
                    _toolsOptions.SetValue(Canvas.TopProperty, tt.Y);

                    _showToolsDistance = Math.Max(_toolsOptions.Width, _toolsOptions.Height) / 2;
                }

                _constructHandles.CanvasScaleChanged();

                if (_veRotateHandle != null)
                {
                    _veRotateHandle.LineWidth = Globals.View.DisplayToPaper(.5);
                }
                if (_veTriangle != null)
                {
                    _veTriangle.LineWidth = Globals.View.DisplayToPaper(.5);

                    _veTriangle.Children.Clear();

                    List<Point> pco = new List<Point>();
                    foreach (Point p in _outerPoints)
                    {
                        pco.Add(Globals.View.DisplayToPaper(p));
                    }
                    _veTriangle.AddChild(pco);

                    List<Point> pci = new List<Point>();
                    foreach (Point p in _innerPoints)
                    {
                        pci.Add(Globals.View.DisplayToPaper(p));
                    }
                    _veTriangle.AddChild(pci);
                }

                //Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

                SaveTriangleSetting();
            }

            _dynamicFromAnchor = new Point();
            _dynamicThroughAnchor = new Point();

            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
        }

        private void SaveTriangleSetting()
        {
            ApplicationDataContainer toolsSettings;
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Containers.ContainsKey("drawing_tools"))
            {
                toolsSettings = localSettings.Containers["drawing_tools"];
            }
            else
            {
                toolsSettings = localSettings.CreateContainer("drawing_tools", ApplicationDataCreateDisposition.Always);
            }

            if (toolsSettings != null)
            {
                toolsSettings.Values["location_x"] = _triangleLocation.X;
                toolsSettings.Values["location_y"] = _triangleLocation.Y;
                toolsSettings.Values["scale"] = _scale;
                toolsSettings.Values["rotation"] = _rotation;
                toolsSettings.Values["type"] = _triangleType == TriangleType.Triangle30 ? "30" : "45";
                toolsSettings.Values["stroke"] = _strokeColor;
                toolsSettings.Values["fill"] = _fillColor;
                toolsSettings.Values["alpha"] = _alphaChannel;
            }
        }

        public void _triangle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_toolsOptions.Visibility == Visibility.Visible)
            {
                ShowTools(false);
                //_toolsOptions.Visibility = Visibility.Collapsed;
            }

            PointerPoint pp = e.GetCurrentPoint(_toolsOverlay);
            Point p = pp.Position;

            MoveHandle(p);

            if ((string)_handleTag == "active" || (string)_handleTag == "inside")
            {
                p = Globals.View.PaperToDisplay(_vmHandle.Location);

                _veHandle.FillColor = Globals.ActiveDrawing.Theme.HandleFillColor;
            }
            else
            {
                _handleTag = "off";
                _veHandle.IsVisible = false;
            }
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

            _isDragging = true;
            _dragOffset.X = p.X - _triangleLocation.X;
            _dragOffset.Y = p.Y - _triangleLocation.Y;
        }

        public void _triangle_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging == false && _isRotating == false)
            {
                Point p = e.GetCurrentPoint(_canvas).Position;
                if (PointInTriangle(p) == false)
                {
                    ShowTools(false);
                }
            }
        }

        public void _triangle_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.ResetConstructHandles();
            }
        }

        public void ShowTriangle(bool show)
        {
            if (_veTriangle == null)
            {
                CreateTriangle();
            }
            if (_veTriangle != null && _toolsOverlay != null)
            {
                if (show == _veTriangle.IsVisible)
                {
                    // we're good
                }
                else if (show)
                {
                    if (_tMaxX < 0 || _tMaxY < 0 || _tMinX > Globals.workCanvas.ViewPortSize.Width || _tMinY > Globals.workCanvas.ViewPortSize.Height)
                    {
                        if (Globals.workCanvas.ViewPortSize.Width > 0 && Globals.workCanvas.ViewPortSize.Height > 0)
                        {
                            _triangleLocation.X = Globals.workCanvas.ViewPortSize.Width / 2;
                            _triangleLocation.Y = Globals.workCanvas.ViewPortSize.Height / 2;
                        }
                    }
                    _veTriangle.IsVisible = true;
                    //Analytics.ReportEvent("triangle-on");
                }
                else
                {

                    if (_triangleFirstRun && _veTriangle.IsVisible)
                    {
                        Globals.Events.ShowTriangleFirstRun();

                        _triangleFirstRun = false;
                    }

                    _veTriangle.IsVisible = false;
                    ShowTools(false);
                    _dynamicConstructNodes.Clear();
                    _constructHandles.HideHandles();
                    //Analytics.ReportEvent("triangle-off");
                }

                TriangleChanged();
            }
        }

        private bool MoveHandle(Point pd)
        {
            double d = Globals.hitTolerance;
            Point node = new Point();

            Point n0 = Construct.NormalPointToLine(pd, _outerPoints[0], _outerPoints[1]);
            Point n1 = Construct.NormalPointToLine(pd, _outerPoints[1], _outerPoints[2]);
            Point n2 = Construct.NormalPointToLine(pd, _outerPoints[2], _outerPoints[0]);

            double d0 = Construct.Distance(n0, pd);
            double d1 = Construct.Distance(n1, pd);
            double d2 = Construct.Distance(n2, pd);

            int pick = -1;
            Point v0 = new Point();
            Point v1 = new Point();

            if (d0 < d)
            {
                d = d0;
                pick = 0;
                node = n0;
                v0 = _outerPoints[0];
                v1 = _outerPoints[1];
            }
            if (d1 < d)
            {
                d = d1;
                pick = 1;
                node = n1;
                v0 = _outerPoints[1];
                v1 = _outerPoints[2];
            }
            if (d2 < d)
            {
                d = d2;
                pick = 2;
                node = n2;
                v0 = _outerPoints[2];
                v1 = _outerPoints[0];
            }

            if (pick >= 0)
            {
                double npv = Construct.PointValue(v0, v1, node);
                if (npv < 0 || npv > 1)
                {
                    string tag = "off";

                    if (npv > 1)
                    {
                        tag = string.Format("V1:{0}", pick);
                        _rotateAnchor = v0;
                        _rotateLocation = v1;
                    }
                    else
                    {
                        tag = string.Format("V0:{0}", pick);
                        _rotateAnchor = v1;
                        _rotateLocation = v0;
                    }

                    if (_rotateHandleTag != tag)
                    {
                        Point dp = _rotateHandleTransform.TransformPoint(new Point(cRotateHandleSize, 0));
                        _rotateHandleLocation = new Point(_rotateLocation.X + dp.X, _rotateLocation.Y + dp.Y);

                        _rotateHandleTransform.Angle = Construct.Angle(_rotateAnchor, _rotateLocation) * Construct.cRadiansToDegrees;
                        _veRotateHandle.Children.Clear();
                        List<object> pc = RotateHandleObjects(_rotateLocation, _rotateHandleTransform);
                        _veRotateHandle.Children.AddRange(pc);
                        _veRotateHandle.IsVisible = true;
                        _veRotateHandle.UpdateBox();
                        //Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                    }

                    double rds = Construct.Distance(pd, _rotateHandleLocation);
                    double r = cRotateHandleSize - cHandleSize * 1.5;
                    double rrr = rds - cHandleSize * 1.5;
                    double rrrr = (r - rrr) / r;

                    if (rrrr >= 1)
                    {
                        // the pointer is in the rotate handle
                        _rotateHandleTag = tag;
                        _veRotateHandle.FillColor = Colors.LimeGreen;
                        _veRotateHandle.Opacity = 1;
                    }
                    else if (rrrr > 0)
                    {
                        _veRotateHandle.FillColor = Globals.ActiveDrawing.Theme.HandleFillColor;
                        _veRotateHandle.Opacity = rrrr;
                        _rotateHandleTag = "near";
                    }
                    else if (_rotateHandleTag != "near" && _rotateHandleTag != "off")
                    {
                        _veRotateHandle.IsVisible = false;
                        _veRotateHandle.FillColor = Globals.ActiveDrawing.Theme.HandleFillColor;
                        _rotateHandleTag = "off";
                    }
                    else
                    {
                        _veRotateHandle.IsVisible = false;
                        _veRotateHandle.FillColor = Globals.ActiveDrawing.Theme.HandleFillColor;
                        _rotateHandleTag = "off";
                    }

                    d = Globals.hitTolerance;
                    pick = -1;
                }
                else if (_rotateHandleTag != "off")
                {
                    // pointer is not in the trangle or a rotate handle, but it recently was in a rotate handle
                    _veRotateHandle.IsVisible = false;
                    _veRotateHandle.FillColor = Globals.ActiveDrawing.Theme.HandleFillColor;
                    //_rotateHandle.IsHitTestVisible = false;
                    _rotateHandleTag = "off";
                }
            }
            else if (_rotateHandleTag != "off")
            {
                // pointer is not in a rotate handle but it recently was
                _veRotateHandle.IsVisible = false;
                _veRotateHandle.FillColor = Globals.ActiveDrawing.Theme.HandleFillColor;
                //_rotateHandle.IsHitTestVisible = false;
                _rotateHandleTag = "off";
            }

            double cd = Globals.hitTolerance;

            foreach (Point p in _outerPoints)
            {
                double ds = Construct.Distance(p, pd);
                if (ds < cd)
                {
                    cd = d = ds;
                    node = p;
                }
            }

            if (d == Globals.hitTolerance)
            {
                _handleTag = "off";
                _veHandle.IsVisible = false;
            }
            else if (_intersectHandleVisible)
            {
                _handleTag = "active";
                _veHandle.IsVisible = false;
            }
            else
            {
                bool inside = Construct.PointInsideTriangle(pd, _outerPoints[0], _outerPoints[1], _outerPoints[2]);

                if (d < Globals.hitTolerance / 2)
                {
                    _handleTag = "active";
                    _veHandle.FillColor = Globals.ActiveDrawing.Theme.HandleColor;

                    if (!_veHandle.IsVisible)
                    {
                        _veHandle.IsVisible = true;
                    }
                }
                else if (inside)
                {
                    _handleTag = "inside";
                    _veHandle.FillColor = Colors.LimeGreen;

                    if (!_veHandle.IsVisible)
                    {
                        _veHandle.IsVisible = true;
                    }
                }
                else
                {
                    _handleTag = "near";
                    _veHandle.FillColor = Globals.ActiveDrawing.Theme.HandleFillColor;
                }

                _vmHandle.Location = Globals.View.DisplayToPaper(node);
                _veHandle.UpdateBox();
            }
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

            return (string)_handleTag == "active";
        }

        public bool ActiveTrianglePoint(out Point p)
        {
            bool isActive = false;
            p = new Point();

            if (_vmHandle != null && (string)_handleTag == "active")
            {
                //Point display = _vm.Location;
                //p = Globals.View.DisplayToPaper(display);
                p = _vmHandle.Location;
                isActive = true;
            }

            return isActive;
        }

        private Point _dynamicFromAnchor = new Point();
        private Point _dynamicThroughAnchor = new Point();
        protected List<ConstructNode> _dynamicConstructNodes = new List<ConstructNode>();

        bool _intersectHandleVisible = false;

        public bool IntersectWithEdge(Point A, Point B, ref Point p, double tolerance)
        {
            bool on = false;

            if (_veTriangle.IsVisible)
            {
                List<Point> pc = new List<Point>();
                foreach (Point po in _outerPoints)
                {
                    pc.Add(Globals.View.DisplayToPaper(po));
                }
                pc.Add(pc[0]);

                Point v0 = pc[0];
                double d = tolerance;

                for (int i = 1; i < pc.Count; i++)
                {
                    Point v1 = pc[i];
                    Point pi;
                    if (Construct.IntersectLineLine(A, B, v0, v1, out pi))
                    {
                        double d0 = Construct.Distance(pi, p);
                        if (d0 < d)
                        {
                            p = pi;
                            on = true;
                            d = d0;
                        }
                    }

                    v0 = v1;
                }
            }

            _intersectHandleVisible = on;

            return on;
        }

        public List<ConstructNode> DynamicTriangleConstructNodes(Point from, Point through)
        {
            _intersectHandleVisible = false;

            if (_outerPoints != null && _outerPoints.Count == 3)
            {
                if (from != _dynamicFromAnchor || through != _dynamicThroughAnchor)
                {
                    _dynamicFromAnchor = from;
                    _dynamicThroughAnchor = through;

                    _dynamicConstructNodes.Clear();

                    if (_veTriangle.IsVisible)
                    {
                        List<Point> pc = new List<Point>();
                        foreach (Point p in _outerPoints)
                        {
                            pc.Add(Globals.View.DisplayToPaper(p));
                        }
                        pc.Add(pc[0]);

                        Point v0 = pc[0];

                        for (int i = 1; i < pc.Count; i++)
                        {
                            Point v1 = pc[i];

                            Point n0 = Construct.NormalPointToLine(from, v0, v1);
                            double npv = Construct.PointValue(v0, v1, n0);

                            if (npv >= 0 && npv <= 1)
                            {
                                _dynamicConstructNodes.Add(new ConstructNode(n0, "normal"));
                            }

                            if (from != through)
                            {
                                Point i0 = Construct.IntersectLineLine(from, through, v0, v1);
                                double ipv = Construct.PointValue(v0, v1, i0);
                                if (ipv >= 0 && ipv <= 1)
                                {
                                    _dynamicConstructNodes.Add(new ConstructNode(i0, "intersect"));
                                }
                            }

                            v0 = v1;
                        }
                    }
                }
            }

            return _dynamicConstructNodes;
        }

        public bool PointInTriangle(Point p)
        {
            bool inside = false;

            if (_veRotateHandle != null && Globals.ShowDrawingTools)
            {
                Point pd = p;
                if (_veRotateHandle.IsVisible && Construct.Distance(pd, _rotateHandleLocation) < cHandleSize * 1.5)
                {
                    inside = true;
                }
                else if (_outerPoints != null && _outerPoints.Count == 3)
                {
                    if (_isDragging || _isRotating)
                    {
                        inside = true;
                    }
                    else if (pd.X > _tMinX && pd.X < _tMaxX && pd.Y > _tMinY && pd.Y < _tMaxY)
                    {
                        if (MoveHandle(pd))
                        {
                            inside = false;
                        }
                        else
                        {
                            inside = Construct.PointInsideTriangle(pd, _outerPoints[0], _outerPoints[1], _outerPoints[2]);
                        }

                        if (!inside && _toolsOverlay.IsHitTestVisible)
                        {
                            ShowTools(false);
                        }
                    }
                    else
                    {
                        ShowTools(false);

                        _handleTag = "off";
                        _veHandle.IsVisible = false;
                        Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                        _veRotateHandle.IsVisible = false;
                    }
                }
            }

            return inside;
        }
    }
}
