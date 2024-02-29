using Cirros.Drawing;
using Cirros.Primitives;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;

namespace Cirros.Core.Display
{
    public sealed partial class Win2dVectorListControl : UserControl, IVectorCanvas
    {
        bool _showGrid = false;
        double _gridSpacing = 1;
        uint _gridDivisions = 1;
        double _displayGridSpacing = 1;
        uint _displayGridDivisions = 1;

        Size _paperSize = Size.Empty;

        double _cursorSize = 25;
        bool _cursorIsVisible = true;
        Point _cursorLocation = new Point();

        int _fixupLevel = 2;

        private VectorList _vectorList = new VectorList();
        private VectorList _overlayList = new VectorList();

        Win2DVectorRenderer _renderer;
        Win2DVectorRenderer _mg_renderer;
        Rect _mg_rect = new Rect(0, 0, 200, 200);
        Color _mg_frameColor = Colors.Gray;
        double _mg_magnification = 2;

        bool _showMagnifier = false;

        public Win2dVectorListControl()
        {
            this.InitializeComponent();

            _magnifierCanvas.Width = _mg_rect.Width;
            _magnifierCanvas.Height = _mg_rect.Height;

            _renderer = new Win2DVectorRenderer(_vectorCanvas, new Rect(0, 0, ActualWidth, ActualHeight));
            _mg_renderer = new Win2DVectorRenderer(_vectorCanvas, _mg_rect);
            _mg_renderer.Magnification = _mg_magnification;

            _magnifierCanvas.Visibility = _showMagnifier ? Visibility.Visible : Visibility.Collapsed;

            _vectorCanvas.Draw += _vectorCanvas_Draw;
            _overlayCanvas.Draw += _overlayCanvas_Draw;
            _magnifierCanvas.Draw += _magnifierCanvas_Draw;

            this.SizeChanged += Win2dVectorListControl_SizeChanged;
        }

        public ICanvasResourceCreator ResourceCreator
        {
            get { return _vectorCanvas; }
        }
            
        public bool ShowMagnifier
        {
            get { return _showMagnifier; }
            set
            {
                if (_showMagnifier != value)
                {
                    _showMagnifier = value;
                    _magnifierCanvas.Visibility = _showMagnifier ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        public void UpdateLineStyles()
        {
            _renderer.UpdateLineStyles();
            _mg_renderer.UpdateLineStyles();
        }

        public async void Regenerate()
        {
            await _renderer.Regenerate(_vectorList);
            await _mg_renderer.Regenerate(_vectorList);
        }

        private void Win2dVectorListControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //if (e.PreviousSize == e.NewSize) return;
            //if (Cirros.Utility.Utilities.__checkSizeChanged(51, sender)) return;

            _renderer.DestinationRect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
        }

        public int FixupLevel
        {
            get { return _fixupLevel; }
            set { _fixupLevel = value; }
        }

        public Size ViewPortSize
        {
            get
            {
                return new Size(Width, Height);
            }
            set
            {
                Width = value.Width;
                Height = value.Height;

                _renderer.DestinationRect = new Rect(0, 0, Width, Height);
            }
        }

        public double DisplayToPaper(double v)
        {
            return v / _renderer.Scale;
        }

        public Size DisplayToPaper(Size size)
        {
            double scale = _renderer.Scale;
            return new Size(size.Width / scale, size.Height / scale);
        }

        public double PaperToDisplay(double paper)
        {
            return paper * _renderer.Scale;
        }

        public Size PaperToDisplay(Size size)
        {
            double scale = _renderer.Scale;
            return new Size(size.Width * scale, size.Height * scale);
        }

        public Point DisplayToPaper(Point display)
        {
            double scale = _renderer.Scale;
            double xoffset = _renderer.XOffset;
            double yoffset = _renderer.YOffset;

            return new Point((display.X - xoffset) / scale, (display.Y - yoffset) / scale);
        }

        public Point PaperToDisplay(Point paper)
        {
            double scale = _renderer.Scale;
            double xoffset = _renderer.XOffset;
            double yoffset = _renderer.YOffset;

            return new Point(paper.X * scale + xoffset, paper.Y * scale + yoffset);
        }

        private void DrawBorder(CanvasDrawingSession ds, Win2DVectorRenderer renderer)
        {
            Color gridColor = Globals.ActiveDrawing.Theme.BorderColor;
            gridColor.A = (byte)(Globals.GridIntensity * 128 + 127);
            Rect b = new Rect(renderer.XOffset, renderer.YOffset, _paperSize.Width * renderer.Scale, _paperSize.Height * renderer.Scale);
            ds.DrawRectangle(b, gridColor, .32f);
        }

        private void DrawGrid(CanvasDrawingSession ds, Win2DVectorRenderer renderer)
        {
            if (_displayGridDivisions > 1 && !_paperSize.IsEmpty && renderer.Scale != 0)
            {
                Rect destRect = renderer.DestinationRect;

                double scale = renderer.Scale;
                double xoffset = renderer.XOffset;
                double yoffset = renderer.YOffset;

                float minorSpacing = (float)_displayGridSpacing / _displayGridDivisions;

                Color gridColor = Globals.ActiveDrawing.Theme.GridColor;
                gridColor.A = (byte)(Globals.GridIntensity < 1 ? (Globals.GridIntensity * 160 + 95) : 255);
                float majorThickness = .32f;
                float minorThickness = .16f;

                double ox = 0;
                double oy = 0;

                if ((Globals.ActiveDrawing.Origin.X % _displayGridSpacing) != 0)
                {
                    ox = -(_displayGridSpacing - (Globals.ActiveDrawing.Origin.X - (Math.Floor(Globals.ActiveDrawing.Origin.X / _displayGridSpacing) * _displayGridSpacing)));
                }
                if ((Globals.ActiveDrawing.Origin.Y % _displayGridSpacing) != 0)
                {
                    oy = -(_displayGridSpacing - (Globals.ActiveDrawing.Origin.Y - (Math.Floor(Globals.ActiveDrawing.Origin.Y / _displayGridSpacing) * _displayGridSpacing)));
                }

                Point min = Globals.ActiveDrawing.PaperToModel(new Point(0, Globals.ActiveDrawing.PaperSize.Height));
                Point max = Globals.ActiveDrawing.PaperToModel(new Point(Globals.ActiveDrawing.PaperSize.Width, 0));

                int iminx = (int)((min.X + ox) / _displayGridSpacing);
                int iminy = (int)((min.Y + oy) / _displayGridSpacing);
                min = new Point((double)iminx * _displayGridSpacing - ox, (double)iminy * _displayGridSpacing - oy);


                ds.Antialiasing = CanvasAntialiasing.Antialiased;

                float sx, sy, ex, ey;

                for (double s = min.Y + oy; s < max.Y; s += _displayGridSpacing)
                {
                    Point start = Globals.ActiveDrawing.ModelToPaper(new Point(min.X, s));
                    Point end = Globals.ActiveDrawing.ModelToPaper(new Point(max.X, s));
                    sx = (float)(start.X * scale + xoffset);
                    sy = ey = (float)(start.Y * scale + yoffset);
                    ex = (float)(end.X * scale + xoffset);

                    sx = (float)Math.Max(destRect.Left, sx);
                    ex = (float)Math.Min(destRect.Right, ex);

                    if (s > min.Y && sy > destRect.Top)
                    {
                        ds.DrawLine(sx, sy, ex, ey, gridColor, majorThickness);
                    }

                    for (int n = 1; n < _displayGridDivisions; n++)
                    {
                        double gy = s + minorSpacing * n;

                        if (gy < min.Y)
                            continue;
                        if (gy > max.Y)
                            break;

                        start = Globals.ActiveDrawing.ModelToPaper(new Point(min.X, gy));
                        sy = ey = (float)(start.Y * scale + yoffset);

                        if (sy < destRect.Top)
                            continue;
                        if (sy > destRect.Bottom)
                            continue;

                        ds.DrawLine(sx, sy, ex, ey, gridColor, minorThickness);
                    }
                }

                for (double s = min.X + ox; s < max.X; s += _displayGridSpacing)
                {
                    Point start = Globals.ActiveDrawing.ModelToPaper(new Point(s, min.Y));
                    Point end = Globals.ActiveDrawing.ModelToPaper(new Point(s, max.Y));
                    sx = ex = (float)(start.X * scale + xoffset);
                    sy = (float)(start.Y * scale + yoffset);
                    ey = (float)(end.Y * scale + yoffset);

                    sy = (float)Math.Min(destRect.Bottom, sy);
                    ey = (float)Math.Max(destRect.Top, ey);

                    if (s > min.X && sx > destRect.Left)
                    {
                        ds.DrawLine(sx, sy, ex, ey, gridColor, majorThickness);
                    }

                    for (int n = 1; n < _displayGridDivisions; n++)
                    {
                        double gx = s + minorSpacing * n;

                        if (gx < min.X)
                            continue;
                        if (gx > max.X)
                            break;

                        start = Globals.ActiveDrawing.ModelToPaper(new Point(gx, min.Y));
                        sx = ex = (float)(start.X * scale + xoffset);

                        if (sx < destRect.Left)
                            continue;
                        if (sx > destRect.Right)
                            continue;

                        ds.DrawLine(sx, sy, ex, ey, gridColor, minorThickness);
                    }
                }

                ds.Antialiasing = CanvasAntialiasing.Antialiased;
            }
        }

        bool _magnifierLeft = true;

        async void _magnifierCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            double left = _magnifierLeft ? _cursorLocation.X - _mg_rect.Width - 30 : _cursorLocation.X + 30;
            double top = _cursorLocation.Y - _mg_rect.Height - 30;
            if (_magnifierLeft)
            {
                if (left < 30)
                {
                    left = 30;
                    if (top <= 30)
                    {
                        top = top = _cursorLocation.Y + 30;
                    }
                }
                else if (top < 30)
                {
                    top = 30;
                }
            }
            else
            {
                if (top < 30)
                {
                    top = 30;
                }
                if ((left + _mg_rect.Width + 30) > this.Width)
                {
                    if (top == 30)
                    {
                        left = _cursorLocation.X - _mg_rect.Width - 30;
                    }
                    else
                    {
                        left = this.Width - _mg_rect.Width - 30;
                    }
                }
            }
            _magnifierCanvas.Margin = new Thickness(left,top,0,0);

            double vp_xoff = _mg_rect.Width / 2;
            double vp_yoff = _mg_rect.Height / 2;

            _mg_renderer.Scale = _renderer.Scale * _mg_magnification;
            _mg_renderer.XOffset = (_renderer.XOffset - _cursorLocation.X) * _mg_magnification + vp_xoff;
            _mg_renderer.YOffset = (_renderer.YOffset - _cursorLocation.Y) * _mg_magnification + vp_yoff;

            Color color = Globals.ActiveDrawing.Theme.BackgroundColor;

            args.DrawingSession.Clear(color);

            DrawBorder(args.DrawingSession, _mg_renderer);

            if (_showGrid)
            {
                DrawGrid(args.DrawingSession, _mg_renderer);
            }

            if (_vectorList != null)
            {
                await _mg_renderer.RenderVectorList(args.DrawingSession, _vectorList.AsList);
                await _mg_renderer.RenderVectorList(args.DrawingSession, _overlayList.AsList);
            }

            if (_cursorIsVisible)
            {
                if (_cursorSize == 0)
                {
                    args.DrawingSession.DrawLine((float)vp_xoff, 0, (float)vp_xoff, (float)Height, _cursorColor, _cursorThickness);
                    args.DrawingSession.DrawLine(0, (float)vp_yoff, (float)Width, (float)vp_yoff, _cursorColor, _cursorThickness);
                }
                else
                {

                }
            }

            Rect r = new Rect(_mg_rect.Left + 2, _mg_rect.Top + 2, _mg_rect.Width - 4, _mg_rect.Height - 4);
            args.DrawingSession.DrawRoundedRectangle(r, 3, 3, _mg_frameColor, 4f);
        }

        async void _vectorCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            Color color = Globals.ActiveDrawing == null || Globals.ActiveDrawing.Theme == null ? Colors.White : Globals.ActiveDrawing.Theme.BackgroundColor;

            if (_paperSize.IsEmpty == false)
            {
                try
                {
                    args.DrawingSession.Clear(color);

                    //DateTime then = DateTime.Now;

                    DrawBorder(args.DrawingSession, _renderer);

                    if (_showGrid)
                    {
                        DrawGrid(args.DrawingSession, _renderer);
                    }

                    if (_vectorList != null)
                    {
                        await _renderer.RenderVectorList(args.DrawingSession, _vectorList.AsList);
                    }

                    if (_showItemBoxes)
                    {
                        if (_vectorList != null)
                        {
                            await _renderer.RenderVectorList(args.DrawingSession, _vectorList.AsList);
                        }
                        RenderItemBoxes(args.DrawingSession);
                    }

                    //TimeSpan ts = DateTime.Now - then;
                    //await Analytics.TraceAsync("_vectorCanvas_Draw", ts.TotalSeconds.ToString());
                }
                catch // (Exception ex)
                {
                    //System.Diagnostics.Debugger.Break();
                    //System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        private Color _cursorColor = Colors.Orange;
        private float _cursorThickness = .5f;
        private bool _showItemBoxes;

        async void _overlayCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            await _renderer.RenderVectorList(args.DrawingSession, _overlayList.AsList);

            if (_cursorIsVisible)
            {
                if (_cursorSize == 0)
                {
                    args.DrawingSession.DrawLine((float)_cursorLocation.X, 0, (float)_cursorLocation.X, (float)Height, _cursorColor, _cursorThickness);
                    args.DrawingSession.DrawLine(0, (float)_cursorLocation.Y, (float)Width, (float)_cursorLocation.Y, _cursorColor, _cursorThickness);
                }
                else
                {
                    args.DrawingSession.DrawLine((float)(_cursorLocation.X - _cursorSize), (float)_cursorLocation.Y, (float)(_cursorLocation.X + _cursorSize), (float)_cursorLocation.Y, _cursorColor, _cursorThickness);
                    args.DrawingSession.DrawLine((float)_cursorLocation.X, (float)(_cursorLocation.Y - _cursorSize), (float)_cursorLocation.X, (float)(_cursorLocation.Y + _cursorSize), _cursorColor, _cursorThickness);
                }
            }
        }

        public VectorList VectorList
        {
            get
            {
                return _vectorList;
            }
            set
            {
                _vectorList = value;
            }
        }

        public Size PaperSize
        {
            get
            {
                return _paperSize;
            }

            set
            {
                _paperSize = value;
            }
        }

        public double GridSpacing
        {
            get
            {
                return _gridSpacing;
            }

            set
            {
                // spacing is in paper units
                _gridSpacing = _displayGridSpacing = value;
            }
        }

        public uint GridDivisions
        {
            get
            {
                return _gridDivisions;
            }

            set
            {
                _gridDivisions = _displayGridDivisions = value;
            }
        }

        public double DisplayGridSpacing
        {
            get
            {
                return _displayGridSpacing;
            }
        }

        public uint DisplayGridDivisions
        {
            get
            {
                return _displayGridDivisions;
            }
        }

        public void Redraw(Rect rect)
        {
            _vectorCanvas.Invalidate();
        }

        public void Redraw()
        {
            _vectorCanvas.Invalidate();

            if (_showMagnifier)
            {
                _magnifierCanvas.Invalidate();
            }
        }

        public void RedrawOverlay()
        {
            _overlayCanvas.Invalidate();

            if (_showMagnifier)
            {
                _magnifierCanvas.Invalidate();
            }
        }

        const double _minGridSpacing = 4;

        public void AdjustGridForWindow(double gridSpacing, uint gridDivisions, out double displayGridSpacing, out uint displayGridDivisions)
        {
            double minorSpacing = (Globals.ActiveDrawing.ModelToPaper(gridSpacing) / gridDivisions) * _renderer.Scale; 

            displayGridDivisions = gridDivisions;
            displayGridSpacing = gridSpacing;

            if (minorSpacing < _minGridSpacing)
            {
                if (Globals.ActiveDrawing.IsArchitecturalScale)
                {
                    if (gridSpacing == 1 && gridDivisions == 12)
                    {
                        minorSpacing *= 2;
                        displayGridDivisions = 6;

                        while (minorSpacing < _minGridSpacing)
                        {
                            displayGridDivisions = 10;
                            displayGridSpacing *= 10;
                            minorSpacing = (Globals.ActiveDrawing.ModelToPaper(displayGridSpacing) / displayGridDivisions) * _renderer.Scale;

                            if (minorSpacing < _minGridSpacing)
                            {
                                minorSpacing *= 2;
                                displayGridDivisions = 5;
                            }
                        }
                    }
                }
                else if (gridDivisions % 4 == 0)
                {
                    if (gridSpacing == 1)
                    {
                        while (minorSpacing < _minGridSpacing && displayGridDivisions > 4)
                        {
                            displayGridDivisions /= 2;
                            minorSpacing *= 2;
                        }
                    }
                    if (minorSpacing < _minGridSpacing)
                    {
                        displayGridDivisions = 10;
                        displayGridSpacing = 1;
                        minorSpacing = (Globals.ActiveDrawing.ModelToPaper(displayGridSpacing) / displayGridDivisions) * _renderer.Scale;

                        while (minorSpacing < _minGridSpacing)
                        {
                            displayGridDivisions = 10;
                            displayGridSpacing *= 10;
                            minorSpacing = (Globals.ActiveDrawing.ModelToPaper(displayGridSpacing) / displayGridDivisions) * _renderer.Scale;

                            if (minorSpacing < _minGridSpacing)
                            {
                                minorSpacing *= 2;
                                displayGridDivisions = 5;
                            }
                        }
                    }
                }
                else // if (gridDivisions == 10)
                {
                    while (minorSpacing < _minGridSpacing)
                    {
                        displayGridDivisions = 10;
                        displayGridSpacing *= 10;
                        minorSpacing = (Globals.ActiveDrawing.ModelToPaper(displayGridSpacing) / displayGridDivisions) * _renderer.Scale;

                        if (minorSpacing < _minGridSpacing)
                        {
                            minorSpacing *= 2;
                            displayGridDivisions = 5;
                        }
                    }
                }
            }
        }

        public void SetWindow(Point p1, Point p2)
        {
            _renderer.SetWindow(p1, p2);

            AdjustGridForWindow(_gridSpacing, _gridDivisions, out _displayGridSpacing, out _displayGridDivisions);

            Redraw();
            RedrawOverlay();
        }

        public void SetWindow(Rect r)
        {
            if (r.IsEmpty)
            {
                return;
            }
            SetWindow(new Point(r.Left, r.Top), new Point(r.Right, r.Bottom));
        }

        public Rect GetWindow()
        {
            Point p = new Point(-_renderer.XOffset / _renderer.Scale, -_renderer.YOffset / _renderer.Scale);
            Size s = DisplayToPaper(new Size(_renderer.DestinationRect.Width, _renderer.DestinationRect.Height));

            return new Rect(p, s);
        }

        //public double Scale
        //{
        //    get
        //    {
        //        return _renderer.Scale;
        //    }
        //    set
        //    {
        //        _renderer.Scale = value;

        //        Redraw();
        //        RedrawOverlay();
        //    }
        //}

        public void ActualSizeWindow(double cx, double cy)
        {
            //Size w = new Size(ActualWidth / _vectorCanvas.Dpi, ActualHeight / _vectorCanvas.Dpi);
            Size w = new Size(ActualWidth / Globals.DPI, ActualHeight / Globals.DPI);

            Point p1 = new Point(cx - w.Width / 2, cy - w.Height / 2);
            Point p2 = new Point(p1.X + w.Width, p1.Y + w.Height);

            SetWindow(p1, p2);
        }

        public void ActualSizeWindow()
        {
            //_renderer.Scale = _vectorCanvas.Dpi;
            _renderer.Scale = Globals.DPI;

            Redraw();
            RedrawOverlay();
        }

        public void Zoom(double factor, double cx, double cy)
        {
            Point a = DisplayToPaper(new Point(0, 0));
            Point b = DisplayToPaper(new Point(ActualWidth, ActualHeight));
            Point c = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);

            Size w = new Size(b.X - a.X, b.Y - a.Y);
            w.Width /= factor;
            w.Height /= factor;

            Point p1 = new Point(c.X - w.Width / 2, c.Y - w.Height / 2);
            Point p2 = new Point(p1.X + w.Width, p1.Y + w.Height);

            SetWindow(p1, p2);

            //Redraw();
        }

        public void Zoom(double factor)
        {
            Point a = DisplayToPaper(new Point(0, 0));
            Point b = DisplayToPaper(new Point(ActualWidth, ActualHeight));
            Point c = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);

            Size w = new Size(b.X - a.X, b.Y - a.Y);
            w.Width /= factor;
            w.Height /= factor;

            Point p1 = new Point(c.X - w.Width / 2, c.Y - w.Height / 2);
            Point p2 = new Point(p1.X + w.Width, p1.Y + w.Height);

            SetWindow(p1, p2);

            //Redraw();
        }

        public void PanToPoint(double cx, double cy)
        {
            Point d = PaperToDisplay(new Point(cx, cy));
            d.X -= ActualWidth / 2;
            d.Y -= ActualHeight / 2;

            _renderer.Pan(-d.X, -d.Y);

            Redraw();

            CursorLocation = PaperToDisplay(Globals.DrawingCanvas.CursorLocation);
        }

        public void Pan(double dx, double dy)
        {
            _renderer.Pan(dx, dy);

            Redraw();

            CursorLocation = PaperToDisplay(Globals.DrawingCanvas.CursorLocation);
        }

        public async Task LoadImage(PImage pi)
        {
            VectorEntity ve = _vectorList.UpdateSegment(pi);
            await _renderer.LoadImages(ve);
            RedrawSegment(ve);
        }

        public async void LoadImage(string imageId)
        {
            await _renderer.LoadImage(imageId);
            Redraw();
        }

        public void ShowItemBoxes(bool show)
        {
            _showItemBoxes = show;
            Redraw();
        }

        private void RenderItemBoxes(CanvasDrawingSession drawingSession)
        {
            if (drawingSession != null)
            {
                double scale = _renderer.Scale;
                double xoffset = _renderer.XOffset;
                double yoffset = _renderer.YOffset;

                List<VectorEntity> vlist = _vectorList.AsList;

                foreach (VectorEntity ve in vlist)
                {
                    if (ve.ItemBox.IsEmpty == false)
                    {
                        Rect r = new Rect();
                        r.X = ve.ItemBox.X * scale + xoffset;
                        r.Y = ve.ItemBox.Y * scale + yoffset;
                        r.Width = ve.ItemBox.Width * scale;
                        r.Height = ve.ItemBox.Height * scale;

                        drawingSession.DrawRectangle(r, Colors.MidnightBlue, .5f);
                    }
                    else
                    {
                    }
                }
            }
        }

        public void ShowGrid(bool show)
        {
            _showGrid = show;

            if (_showGrid)
            {
                AdjustGridForWindow(_gridSpacing, _gridDivisions, out _displayGridSpacing, out _displayGridDivisions);
            }

            Redraw();
        }

        public Color CursorColor
        {
            get { return _cursorColor; }
            set
            {
                if (_cursorColor != value)
                {
                    _cursorColor = value;
                    if (_cursorIsVisible)
                    {
                        _overlayCanvas.Invalidate();
                    }
                }
            }
        }

        public double CursorSize
        {
            get { return _cursorSize; }
            set { _cursorSize = value; }
        }

        public Point CursorLocation
        {
            get
            {
                return _cursorLocation;
            }
            set
            {
                _cursorLocation = value;
                _overlayCanvas.Invalidate();

                if (_showMagnifier)
                {
                    _magnifierCanvas.Invalidate();
                }
            }
        }

        public void ShowCursor(bool show)
        {
            if (show != _cursorIsVisible)
            {
                _cursorIsVisible = show;
                _overlayCanvas.Invalidate();
            }
        }

        public uint PickSegment(Point paper)
        {
            if (_vectorList == null)
            {
                return 0;
            }
            return _vectorList.Pick(paper);
        }

        public int GetMemberIndex(uint segmentId, Point paper)
        {
            if (_vectorList != null)
            {
                double t = Globals.DrawingCanvas.DisplayToPaper(Globals.hitTolerance);
                uint memberId = 0;
                VectorEntity ve = _vectorList.GetSegment(segmentId);
                if (ve.Pick(paper, t, ref memberId) < t)
                {
                    for (int i = 0; i < ve.Children.Count; i++)
                    {
                        if (ve.Children[i] is VectorEntity)
                        {
                            if (((VectorEntity)ve.Children[i]).SegmentId == memberId)
                            {
                                return i;
                            }
                        }
                    }
                }
            }

            return -1;
        }

        public void HideSegment(uint segmentId, bool flag)
        {
            bool visible = !flag;

            if (segmentId > 0)
            {
                VectorEntity ve = _vectorList.GetSegment(segmentId);
                if (ve != null)
                {
                    if (ve.IsVisible != visible)
                    {
                        ve.IsVisible = visible;
                        if (_fixupLevel > 1)
                        {
                            RedrawSegment(ve);
                        }
                    }
                }
            }
        }

        private void HighlightVE(VectorEntity ve, bool flag)
        {
            ve.IsHighlighted = flag;

            foreach (object o in ve.Children)
            {
                if (o is VectorEntity)
                {
                    HighlightVE(o as VectorEntity, flag);
                }
            }
        }

        public void HighlightSegment(uint segmentId, bool flag)
        {
            if (segmentId > 0)
            {
                VectorEntity ve = _vectorList.GetSegment(segmentId);
                if (ve != null)
                {
                    //if (ve.IsHighlighted != flag)
                    {
                        HighlightVE(ve, flag);

                        if (_fixupLevel > 1)
                        {
                            RedrawSegment(ve);
                        }
                    }
                }
            }
        }

        public void HighlightMember(uint segmentId, int index)
        {
            if (segmentId > 0)
            {
                VectorEntity ve = _vectorList.GetSegment(segmentId);
                if (ve != null)
                {
                    for (int i = 0; i < ve.Children.Count; i++)
                    {
                        if (ve.Children[i] is VectorEntity)
                        {
                            HighlightVE(ve.Children[i] as VectorEntity, i == index);
                        }
                    }
                    if (_fixupLevel > 1)
                    {
                        RedrawSegment(ve);
                    }
                }
            }
        }

        public void SetSegmentZIndex(uint segmentId, int zIndex)
        {
            if (segmentId > 0)
            {
                VectorEntity ve = _vectorList.GetSegment(segmentId);
                if (ve != null)
                {
                    if (ve.ZIndex != zIndex)
                    {
                        ve.ZIndex = zIndex;

                        if (_fixupLevel > 1)
                        {
                            RedrawSegment(ve);
                        }
                    }
                }
            }
        }

        private void SetVEOpacity(VectorEntity ve, double opacity)
        {
            ve.Opacity = opacity;

            foreach (object o in ve.Children)
            {
                if (o is VectorEntity)
                {
                    SetVEOpacity(o as VectorEntity, opacity);
                }
                else if (o is VectorImageEntity)
                {
                    ((VectorImageEntity)o).Opacity = opacity;
                }
            }
        }

        public void SetSegmentOpacity(uint segmentId, double opacity)
        {
            if (segmentId > 0)
            {
                VectorEntity ve = _vectorList.GetSegment(segmentId);
                if (ve != null)
                {
                    SetVEOpacity(ve, opacity);

                    if (_fixupLevel > 1)
                    {
                        RedrawSegment(ve);
                    }
                }
            }
        }

        private void RedrawSegment(VectorEntity ve)
        {
            _vectorList.UpdateSegment(ve);
            Redraw();
        }

        public void MoveSegmentBy(uint segmentId, double dx, double dy)
        {
            _vectorList.MoveSegmentBy(segmentId, dx, dy);
            Redraw();
        }

        public VectorEntity UpdateSegment(Primitive p)
        {
            VectorEntity ve = _vectorList.UpdateSegment(p);

            if (_fixupLevel > 1)
            {
                RedrawSegment(ve);
            }

            return ve;
        }

        public void AddSegment(VectorEntity ve)
        {
            _vectorList.AddSegment(ve);

            if (_fixupLevel > 1)
            {
                RedrawSegment(ve);
            }
        }

        public void AddOverlaySegment(VectorEntity ve)
        {
            _overlayList.AddSegment(ve);
        }

        public void RemoveSegment(uint segmentId)
        {
            _vectorList.RemoveSegment(segmentId);

            if (_fixupLevel > 1)
            {
                Redraw();
            }
        }

        public void RemoveOverlaySegment(uint segmentId)
        {
            _overlayList.RemoveSegment(segmentId);
        }
    }
}
