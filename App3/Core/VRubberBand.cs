using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using CirrosCore;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Display
{
    public abstract class VRubberBand : ARubberBand
    {
        protected bool _isVisible = true;
        protected uint _segmentId = 1023;
        protected VectorEntity _ve;

        protected VRubberBand()
        {
            _color = Globals.ActiveDrawing.Theme.CursorColor;
        }

        protected VRubberBand(Color color)
        {
            _color = color;
        }

        private void DisposeSegment()
        {
            if (_ve != null)
            {
                Globals.DrawingCanvas.VectorListControl.RemoveOverlaySegment(_ve.SegmentId);
                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                _ve = null;
            }
        }

        public override void Reset()
        {
            _state = 0;

            if (_ve != null)
            {
                _ve.Children.Clear();
            }
        }

        public override void StartTracking(double x, double y)
        {
            _anchor = new Point(x, y);
            _isVisible = true;

            Globals.Events.CoordinateDisplay(new Point(x, y));
        }

        public override void TrackCursor(double x, double y)
        {
            Globals.Events.CoordinateDisplay(new Point(x, y));
        }

        public override void EndTracking()
        {
            DisposeSegment();
        }

        public override void Update()
        {
            // called when the display scale changes
        }

        public override double Opacity
        {
            set
            {
                _opacity = value;
                _color.A = (byte)(_opacity * 255);
            }
        }

        public override void Show()
        {
            if (_isVisible == false)
            {
                _isVisible = true;
                if (_ve != null)
                {
                    _ve.IsVisible = _isVisible;
                    Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                }
            }
        }

        public override void Hide()
        {
            if (_isVisible)
            {
                _isVisible = false;
                if (_ve != null)
                {
                    _ve.IsVisible = _isVisible;
                    Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                }
            }
        }

        protected void CrosshatchVE(VectorEntity ve, string patternName, double scale, double angle)
        {
            if (string.IsNullOrEmpty(patternName) == false)
            {
                string key = patternName.ToLower();

                ve.Fill = false;

                CrosshatchPattern pattern = null;

                if (Patterns.PatternDictionary.ContainsKey(key))
                {
                    pattern = Patterns.PatternDictionary[key];
                }

                if (pattern != null && pattern.Items.Count > 0)
                {
                    List<List<Point>> hatches = PrimitiveUtilities.Crosshatch(ve, pattern, scale, angle);

                    if (hatches.Count > 0)
                    {
                        VectorEntity hve = new VectorEntity(ve.SegmentId + 110000, ve.ZIndex);
                        hve.Color = ve.FillColor;
                        hve.LineWidth = ve.LineWidth;
                        hve.ScaleLineWidth = ve.ScaleLineWidth;
                        hve.IsSelectable = false;

                        foreach (List<Point> pc in hatches)
                        {
                            hve.Children.Add(pc);
                        }

                        ve.Children.Insert(0, hve);
                    }
                }
            }
        }
    }

    public class RubberBandNone : VRubberBand
    {
        public RubberBandNone()
        {
            Reset();
        }

        public override Color Color
        {
            set { }
        }

        public override double Opacity
        {
            set { }
        }

        public override void EndTracking()
        {
        }

        public override void Hide()
        {
        }

        public override void Show()
        {
        }

        //public override void StartTracking(double x, double y)
        //{
        //}

        public override void TrackCursor(double x, double y)
        {
        }

        public override void Update()
        {
        }
    }

    public class RubberBandBasic : RubberBandNone
    {
        public RubberBandBasic()
        {
            Reset();
        }

        bool _showCoordinateVector = false;

        public bool ShowCoordinateVector
        {
            get { return _showCoordinateVector; }
            set { _showCoordinateVector = value; }
        }

        public override void TrackCursor(double x, double y)
        {
            Point p = new Point(x, y);
            if (_state > 0 && _showCoordinateVector)
            {
                CoordinateDisplayVector(p);
            }
            else
            {
                Globals.Events.CoordinateDisplay(p);
            }
        }
    }

    public class RubberBandLine : VRubberBand
    {
        public RubberBandLine()
            : base()
        {
        }

        public RubberBandLine(Color color)
            : base(color)
        {
        }

        public override void StartTracking(double x, double y)
        {
            _ve = new VectorEntity(_segmentId, 100000);

            _ve.Color = _color;
            _ve.LineWidth = Globals.View.DisplayToPaper(.7);
            _ve.IsVisible = true;

            Point p = new Point(x, y);

            List<Point> pc = new List<Point>();
            pc.Add(p);
            pc.Add(p);
            _ve.AddChild(pc);

            Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

            base.StartTracking(x, y);
        }

        public override void TrackCursor(double x, double y)
        {
            if (_state == 1 || _state == 2)
            {
                Point p = new Point(x, y);

                if (_ve.Children.Count == 1 && _ve.Children[0] is List<Point>)
                {
                    List<Point> pc = _ve.Children[0] as List<Point>;
                    if (pc.Count == 2)
                    {
                        pc[1] = p;
                        _ve.UpdateBox();
                        Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                    }
                }

                CoordinateDisplayVector(p);
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }
    }

    public class RubberBandDoubleline : VRubberBand
    {
        public RubberBandDoubleline()
            : base()
        {
            //_halfWidth = Globals.DrawingCanvas.PaperToCanvas(Globals.DoubleLineWidth) / 2;
            _halfWidth = Globals.DoubleLineWidth / 2;
        }

        public RubberBandDoubleline(Color color)
            : base(color)
        {
            //_halfWidth = Globals.DrawingCanvas.PaperToCanvas(Globals.DoubleLineWidth) / 2;
            _halfWidth = Globals.DoubleLineWidth / 2;
        }

        public override void Reset()
        {
            base.Reset();
            //_halfWidth = Globals.DrawingCanvas.PaperToCanvas(Globals.DoubleLineWidth) / 2;
            _halfWidth = Globals.DoubleLineWidth / 2;
        }

        private double _halfWidth = 0;

        public override void TrackCursor(double x, double y)
        {
            if (_state > 0)
            {
                Point p = new Point(x, y);
                Point j, k, l, m;

                Point start = _anchor;

                Construct.Parallel(start, p, _halfWidth, out j, out k);
                Construct.Parallel(start, p, -_halfWidth, out l, out m);

                _ve = new VectorEntity(_segmentId, 100000);

                _ve.Color = _color;
                _ve.LineWidth = Globals.View.DisplayToPaper(.7);
                _ve.IsVisible = true;

                List<Point> pc0 = new List<Point>();
                pc0.Add(j);
                pc0.Add(k);
                _ve.AddChild(pc0);

                List<Point> pc1 = new List<Point>();
                pc1.Add(l);
                pc1.Add(m);
                _ve.AddChild(pc1);

                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

                CoordinateDisplayVector(p);
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }
    }
    public class RubberBandBSpline : VRubberBand
    {
        VectorEntity _ve2 = null;

        public RubberBandBSpline()
            : base()
        {
            initialize();
        }

        public RubberBandBSpline(Color color)
            : base(color)
        {
            initialize();
        }

        private void initialize()
        {
            _thickness = .75;
        }

        List<Point> _cpoints = new List<Point>();

        public override void StartTracking(double x, double y)
        {
            if (_state == 1)
            {
                _cpoints.Clear();
                //_cpoints.Add(Globals.DrawingCanvas.PaperToCanvas(new Point(x, y)));
                //_cpoints.Add(Globals.DrawingCanvas.PaperToCanvas(new Point(x, y)));
                _cpoints.Add(new Point(x, y));
                _cpoints.Add(new Point(x, y));
            }
            else if (_state > 1)
            {
                //_cpoints.Add(Globals.DrawingCanvas.PaperToCanvas(new Point(x, y)));
                _cpoints.Add(new Point(x, y));
            }

            base.StartTracking(x, y);
        }

        public override void TrackCursor(double x, double y)
        {
            if (_state > 0)
            {
                if (_ve == null)
                {
                    _ve = new VectorEntity(_segmentId, 100000);

                    _ve.Color = _color;
                    //_ve.LineWidth = Globals.View.DisplayToPaper(_thickness);
                    _ve.LineWidth = _thickness;
                    _ve.ScaleLineWidth = false;
                    _ve.IsVisible = true;

                    _ve2 = new VectorEntity(_segmentId - 1, 100000);

                    _ve2.Color = Globals.ActiveDrawing.Theme.ForegroundColor;
                    //_ve2.LineWidth = Globals.View.DisplayToPaper(_thickness / 3);
                    _ve2.LineWidth = _thickness / 3;
                    _ve2.ScaleLineWidth = false;
                    _ve2.IsVisible = true;
                    _ve2.AddChild(_cpoints);
                }
                else
                {
                    _ve.Children.Clear();
                    _ve2.Children.Clear();
                    _ve2.AddChild(_cpoints);
                }

                //_cpoints[_cpoints.Count - 1] = Globals.DrawingCanvas.PaperToCanvas(new Point(x, y));
                _cpoints[_cpoints.Count - 1] = new Point(x, y);

                if (_cpoints.Count >= 2)
                {
                    if (_cpoints.Count >= 3)
                    {
                        Point p4 = _cpoints[_cpoints.Count - 1];
                        Point p3 = _cpoints[_cpoints.Count - 2];
                        Point p2 = _cpoints[_cpoints.Count - 3];
                        Point p1 = _cpoints.Count == 3 ? p2 : _cpoints[_cpoints.Count - 4];

                        List<Point> pc = new List<Point>();

                        if (p1 == p2)
                        {
                            pc.Add(p1);
                        }

                        CGeometry.BSplineSegment(pc, p1, p2, p3, p4);
                        CGeometry.BSplineSegment(pc, p2, p3, p4, p4);

                        pc.Add(new Point(x, y));

                        _ve.AddChild(pc);
                    }

                    Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve2);
                    Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                    Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                }

                CoordinateDisplayVector(new Point(x, y));
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }

        private void DisposeSpine()
        {
            if (_ve2 != null)
            {
                Globals.DrawingCanvas.VectorListControl.RemoveOverlaySegment(_ve2.SegmentId);
                _ve2 = null;
            }
        }

        public override void Reset()
        {
            _state = 0;

            initialize();
            DisposeSpine();

            base.Reset();
        }

        public void RemovePoint(int count)
        {
            if (_cpoints.Count > 1)
            {
                for (int i = 0; i < count; i++)
                {
                    _cpoints.RemoveAt(_cpoints.Count - 1);
                }
            }
            else
            {
                _cpoints.Clear();
                _state = 0;
            }

            if (_cpoints.Count == 0)
            {
                _state = 0;
            }
        }
    }

    public class RubberBandArc : VRubberBand
    {
        VectorEntity _ve2 = null;

        public RubberBandArc()
            : base()
        {
        }

        public RubberBandArc(Color color)
            : base(color)
        {
        }

        protected bool _radiusSpecified = false;
        protected int _direction = 0;

        public override double Radius
        {
            get
            {
                if (_radiusSpecified)
                {
                    return _radius;
                }
                return Construct.Distance(_center, _start);
            }
            set
            {
                _radius = value;
                _radiusSpecified = true;
            }
        }

        public double StartAngle
        {
            get
            {
                return Construct.Angle(_center, _start);
            }
        }

        public double IncludedAngle
        {
            get
            {
                double end = Construct.Angle(_center, _end);
                return Construct.IncludedAngle(StartAngle, end, _direction >= 0);
            }
        }

        public override void Reset()
        {
            if (_ve2 != null)
            {
                Globals.DrawingCanvas.VectorListControl.RemoveOverlaySegment(_ve2.SegmentId);
                _ve2 = null;
            }

            _isVisible = false;
            _state = 0;
            _direction = 0;

            base.Reset();
        }

        // State 0: None
        // State 1: Have center point
        // State 2: Have start point
        // State 3: Have end point

        int _lastMinorDirection = 0;

        public override void TrackCursor(double x, double y)
        {
            if (_state > 0)
            {
                if (_ve == null)
                {
                    _ve = new VectorEntity(_segmentId, 100000);

                    _ve.Color = _color;
                    //_ve.LineWidth = Globals.View.DisplayToPaper(_thickness);
                    _ve.LineWidth = _thickness;
                    _ve.ScaleLineWidth = false;
                    _ve.IsVisible = true;

                    _ve2 = new VectorEntity(_segmentId - 1, 100000);

                    _ve2.Color = Globals.ActiveDrawing.Theme.ForegroundColor;
                    //_ve2.LineWidth = Globals.View.DisplayToPaper(_thickness / 3);
                    _ve2.LineWidth = _thickness / 3;
                    _ve2.ScaleLineWidth = false;
                    _ve2.IsVisible = true;
                }
                else
                {
                    _ve.Children.Clear();

                    if (_ve2 == null)
                    {
                        _ve2 = new VectorEntity(_segmentId - 1, 100000);

                        _ve2.Color = Globals.ActiveDrawing.Theme.ForegroundColor;
                        //_ve2.LineWidth = Globals.View.DisplayToPaper(_thickness / 3);
                        _ve2.LineWidth = _thickness / 3;
                        _ve2.ScaleLineWidth = false;
                        _ve2.IsVisible = true;
                    }
                    else
                    {
                        _ve2.Children.Clear();
                    }
                }

                Point p = new Point(x, y);
                double d;

                switch (_state)
                {
                    case 0:
                    default:
                        break;

                    case 1:
                        {
                            double a = Construct.Angle(_center, p);
                            if (_radiusSpecified)
                            {
                                d = _radius;
                                _start = Construct.PolarOffset(_center, _radius, a);
                            }
                            else
                            {
                                _start = p;
                                d = Construct.Distance(_center, _start);
                            }

                            double size = Globals.View.DisplayToPaper(cDotSize);

                            List<Point> pc = CGeometry.ArrowPointCollection(_start, _center, ArrowType.Outline, size, .25);
                            pc.Add(_center);
                            _ve2.AddChild(pc);

                            Globals.Events.CoordinateDisplay(new Point(x, y), x - _center.X, y - _center.Y, d, a);
                        }
                        break;

                    case 2:
                        {
                            double sa = Construct.Angle(_center, _start);
                            double a = Construct.Angle(_center, p);

                            if (_radiusSpecified)
                            {
                                d = _radius;
                                _start = Construct.PolarOffset(_center, _radius, sa);
                            }
                            else
                            {
                                d = Construct.Distance(_center, _start);
                            }

                            double size = Globals.View.DisplayToPaper(cDotSize);

                            double minorIncluded = Construct.MinorIncludedAngle(sa, a);
                            int _minorDirection = Math.Sign(minorIncluded);

                            if (_direction == 0)
                            {
                                _direction = _minorDirection;
                            }
                            else if (_lastMinorDirection != _minorDirection)
                            {
                                if (Math.Abs(minorIncluded) < .5)
                                {
                                    _direction = _minorDirection;
                                }
                                _lastMinorDirection = _minorDirection;
                            }

                            if (_ve != null)
                            {
                                _ve.Children.Clear();

                                Point p1 = Construct.PolarOffset(_center, d, a);
                                List<Point> pc = CGeometry.ArrowPointCollection(p1, _center, ArrowType.Outline, size, .25);
                                pc.Add(_center);
                                _ve2.AddChild(pc);

                                _ve.AddChild(CGeometry.ArcPointCollection(_center, d, sa, Construct.IncludedAngle(sa, a, _direction >= 0), false, CGeometry.IdentityMatrix()));
                            }

                            _isVisible = true;

                            Globals.Events.CoordinateDisplay(new Point(x, y), x - _center.X, y - _center.Y, d, a);
                        }
                        break;
                }

                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve2);
                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }
    }

    public class RubberBandArc3 : VRubberBand
    {
        public RubberBandArc3()
            : base()
        {
            //setShape(new Path());
        }

        public RubberBandArc3(Color color)
            : base(color)
        {
            //setShape(new Path());
        }

        protected Handles _handles = new Handles();

        public override void Reset()
        {
            _handles.Clear();
            _state = 0;

            base.Reset();
        }

        // State 0: None
        // State 1: Have start point
        // State 2: Have mid point
        // State 3: Have end point

        public override void TrackCursor(double x, double y)
        {
            if (_state > 0)
            {
                _handles.Clear();

                switch (_state)
                {
                    case 0:
                    case 3:
                        break;

                    case 1:
                        _handles.AddHandle(1, _start.X, _start.Y, HandleType.Circle);
                        break;

                    case 2:
                        _handles.AddHandle(1, _start.X, _start.Y, HandleType.Circle);
                        _handles.AddHandle(2, _mid.X, _mid.Y, HandleType.Circle);
                        _handles.AddHandle(3, x, y, HandleType.Circle);
                        {

                            _ve = new VectorEntity(_segmentId, 100000);

                            _ve.Color = _color;
                            //_ve.LineWidth = Globals.View.DisplayToPaper(_thickness);
                            _ve.LineWidth = _thickness;
                            _ve.ScaleLineWidth = false;
                            _ve.IsVisible = true;
                            _ve.AddChild(ArcPoints(_start, _mid, new Point(x, y)));

                            Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                        }
                        break;
                }

                _handles.Draw();

                CoordinateDisplayVector(new Point(x, y));
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }

        protected virtual List<Point> ArcPoints(Point start, Point mid, Point end)
        {
            return CGeometry.ArcPointCollection(start, mid, end, false, CGeometry.IdentityMatrix());
        }
    }

    public class RubberBandArcFillet : VRubberBand
    {
        VectorEntity _ve2 = null;

        public RubberBandArcFillet()
            : base()
        {
            initialize();
        }

        public RubberBandArcFillet(Color color)
            : base(color)
        {
            initialize();
        }

        private void initialize()
        {
            _thickness = .75;
        }

        public override void TrackCursor(double x, double y)
        {
            if (_ve == null)
            {
                _ve = new VectorEntity(_segmentId, 100000);

                _ve.Color = _color;
                //_ve.LineWidth = Globals.View.DisplayToPaper(_thickness);
                _ve.LineWidth = _thickness;
                _ve.ScaleLineWidth = false;
                _ve.IsVisible = true;

                _ve2 = new VectorEntity(_segmentId - 1, 100000);

                _ve2.Color = Globals.ActiveDrawing.Theme.ForegroundColor;
                //_ve2.LineWidth = Globals.View.DisplayToPaper(_thickness / 3);
                _ve2.LineWidth = _thickness / 3;
                _ve2.ScaleLineWidth = false;
                _ve2.IsVisible = true;
            }
            else
            {
                _ve.Children.Clear();
                _ve2.Children.Clear();
            }

            if (_state == 1)
            {
                List<Point> pc = new List<Point>();
                pc.Add(_start);
                pc.Add(new Point(x, y));
                _ve2.Children.Clear();
                _ve2.AddChild(pc);

                CoordinateDisplayVector(new Point(x, y));
            }
            else if (_state > 1)
            {
                List<Point> pc = new List<Point>();
                pc.Add(_start);
                pc.Add(_mid);
                pc.Add(new Point(x, y));
                _ve2.AddChild(pc);

                if (_radius == 0)
                {
                    double cradius;
                    _ve.AddChild(FilletPoints(_start, _mid, new Point(x, y), out cradius));
                }
                else
                {
                    _ve.AddChild(FilletPoints(_start, _mid, new Point(x, y), _radius));
                }

                CoordinateDisplayVector(new Point(x, y));
            }
            else
            {
                base.TrackCursor(x, y);
            }

            if (_ve != null)
            {
                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve2);
                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
            }
        }

        public override void Reset()
        {
            _state = 0;

            initialize();

            base.Reset();

            if (_ve2 != null)
            {
                _ve2.Children.Clear();
            }

        }

        protected virtual List<Point> FilletPoints(Point start, Point vertex, Point end, double radius)
        {
            Point center;
            double startAngle, includedAngle;

            Construct.FilletPoints(start, vertex, end, radius, out center, out startAngle, out includedAngle);

            return includedAngle == 0 ? null : CGeometry.ArcPointCollection(center, radius, startAngle, includedAngle, false, Matrix.Identity);
        }

        protected virtual List<Point> FilletPoints(Point start, Point vertex, Point end, out double radius)
        {
            Point center;
            double startAngle, includedAngle;

            Construct.FilletPoints(start, vertex, end, out radius, out center, out startAngle, out includedAngle);

            return includedAngle == 0 ? null : CGeometry.ArcPointCollection(center, radius, startAngle, includedAngle, false, Matrix.Identity);
        }
    }

    public class RubberBandArc2 : VRubberBand
    {
        public RubberBandArc2()
            : base()
        {
        }

        public RubberBandArc2(Color color)
            : base(color)
        {
        }

        // State 0: None
        // State 1: Have start point
        // State 2: Have end point

        public override void TrackCursor(double x, double y)
        {
            if (_ve == null)
            {
                _ve = new VectorEntity(_segmentId, 100000);

                _ve.Color = _color;
                //_ve.LineWidth = Globals.View.DisplayToPaper(_thickness);
                _ve.LineWidth = _thickness;
                _ve.ScaleLineWidth = false;
                _ve.IsVisible = true;
            }
            else
            {
                _ve.Children.Clear();
            }

            if (_state > 0)
            {
                switch (_state)
                {
                    case 0:
                    case 3:
                        break;

                    case 1:
                        _ve.AddChild(ArcPoints(_start, new Point(x, y)));

                        Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                        Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                        break;
                }

                CoordinateDisplayVector(new Point(x, y));
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }

        protected virtual List<Point> ArcPoints(Point start, Point end)
        {
            Point c = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            double r = Construct.Distance(c, start);
            double a = Construct.Angle(c, start);

            return CGeometry.ArcPointCollection(c, r, a, -Math.PI, false, Matrix.Identity);
        }
    }

    public class RubberBandCircle3 : RubberBandArc3
    {
        public RubberBandCircle3()
            : base()
        {
        }

        public RubberBandCircle3(Color color)
            : base(color)
        {
        }

        protected override List<Point> ArcPoints(Point start, Point mid, Point end)
        {
            return CGeometry.CirclePointCollection(start, mid, end, CGeometry.IdentityMatrix());
        }
    }

    public class RubberBandRectangle : VRubberBand
    {
        public RubberBandRectangle()
            : base()
        {
        }

        public RubberBandRectangle(Color color)
            : base(color)
        {
        }

        public override void TrackCursor(double x, double y)
        {
            if (_state > 0)
            {
                double l = Math.Min(x, _anchor.X);
                double t = Math.Min(y, _anchor.Y);
                double w = Math.Abs(x - _anchor.X);
                double h = Math.Abs(y - _anchor.Y);

                if (_ve == null)
                {
                    _ve = new VectorEntity(_segmentId, 100000);

                    _ve.Color = _color;
                    //_ve.LineWidth = Globals.View.DisplayToPaper(_thickness);
                    _ve.LineWidth = _thickness;
                    _ve.ScaleLineWidth = false;
                    _ve.IsVisible = true;
                }
                else
                {
                    _ve.Children.Clear();
                }

                List<Point> pc = new List<Point>();
                pc.Add(new Point(l, t));
                pc.Add(new Point(l + w, t));
                pc.Add(new Point(l + w, t + h));
                pc.Add(new Point(l, t + h));
                pc.Add(new Point(l, t));

                _ve.AddChild(pc);

                if (_shouldFill)
                {
                    _ve.Fill = true;
                    _ve.FillColor = _fillColor;

                    CrosshatchVE(_ve, _fillPattern, _patternScale, _patternAngle);
                }

                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

                CoordinateDisplayVector(new Point(x, y));
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }
    }

    public class RubberBandEllipse : VRubberBand
    {
        public RubberBandEllipse()
            : base()
        {
        }

        public RubberBandEllipse(Color color)
            : base(color)
        {
        }

        public override void TrackCursor(double x, double y)
        {
            if (_ve == null)
            {
                _ve = new VectorEntity(_segmentId, 100000);

                _ve.Color = _color;
                //_ve.LineWidth = Globals.View.DisplayToPaper(_thickness);
                _ve.LineWidth = _thickness;
                _ve.ScaleLineWidth = false;
                _ve.IsVisible = true;
            }
            else
            {
                _ve.Children.Clear();
            }

            if (_state > 0)
            {
                _ve.AddChild(EllipsePointCollection(_anchor, new Point(x, y)));

                if (_shouldFill)
                {
                    _ve.Fill = true;
                    _ve.FillColor = _fillColor;

                    CrosshatchVE(_ve, _fillPattern, _patternScale, _patternAngle);
                }

                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

                CoordinateDisplayVector(new Point(x, y));
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }

        protected virtual List<Point> EllipsePointCollection(Point a, Point b)
        {
            Point c = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            double major = Math.Abs(b.X - a.X) / 2;
            double minor = Math.Abs(b.Y - a.Y) / 2;

            return CGeometry.EllipsePointCollection(c, major, minor, 0, Globals.EllipseStartAngle, Globals.EllipseIncludedAngle, false, Matrix.Identity);
        }
    }

    public class RubberBandEllipseAxis : VRubberBand
    {
        public RubberBandEllipseAxis()
            : base()
        {
        }

        public RubberBandEllipseAxis(Color color)
            : base(color)
        {
        }

        public override void TrackCursor(double x, double y)
        {
            if (_ve == null)
            {
                _ve = new VectorEntity(_segmentId, 100000);

                _ve.Color = _color;
                //_ve.LineWidth = Globals.View.DisplayToPaper(_thickness);
                _ve.LineWidth = _thickness;
                _ve.ScaleLineWidth = false;
                _ve.IsVisible = true;
            }
            else
            {
                _ve.Children.Clear();
            }

            if (_state > 0)
            {
                switch (_state)
                {
                    case 0:
                    case 3:
                        break;

                    case 1:
                        _ve.AddChild(EllipsePointCollection(_start, new Point(x, y)));

                        if (_shouldFill)
                        {
                            _ve.Fill = true;
                            _ve.FillColor = _fillColor;

                            CrosshatchVE(_ve, _fillPattern, _patternScale, _patternAngle);
                        }

                        Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                        Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                        break;
                }

                CoordinateDisplayVector(new Point(x, y));
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }

        protected virtual List<Point> EllipsePointCollection(Point start, Point end)
        {
            Point c = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            double major = Construct.Distance(c, end);
            double a = Construct.Angle(c, end);

            return CGeometry.EllipsePointCollection(c, major, major / Globals.EllipseMajorMinorRatio, a, Globals.EllipseStartAngle, Globals.EllipseIncludedAngle, false, Matrix.Identity);
        }
    }

    public class RubberBandArrow : VRubberBand
    {
        protected double _size = .125;
        ArrowStyle _arrowStyle;
        ArrowLocation _arrowLocation = ArrowLocation.Start;

        public RubberBandArrow()
            : base()
        {
            ArrowStyle = Globals.ArrowStyleTable[Globals.ArrowStyleId];
        }

        public RubberBandArrow(Color color)
            : base(color)
        {
            ArrowStyle = Globals.ArrowStyleTable[Globals.ArrowStyleId];
        }

        public ArrowLocation ArrowLocation
        {
            get
            {
                return _arrowLocation;
            }
            set
            {
                _arrowLocation = value;
            }
        }

        public ArrowStyle ArrowStyle
        {
            get
            {
                return _arrowStyle;
            }
            set
            {
                _arrowStyle = value;
                //_size = Globals.DrawingCanvas.PaperToCanvas(_arrowStyle.Size);
                _size = _arrowStyle.Size;
            }
        }

        public override int State
        {
            get
            {
                return base.State;
            }

            set
            {
                base.State = value;
                if (_state == 0 && _ve != null)
                {
                    Reset();
                }
            }
        }

        public override void TrackCursor(double x, double y)
        {
            if (_state > 0)
            {
                if (_ve == null)
                {
                    _ve = new VectorEntity(_segmentId, 100000);

                    _ve.Color = _color;
                    //_ve.LineWidth = Globals.View.DisplayToPaper(_thickness);
                    _ve.LineWidth = _thickness;
                    _ve.ScaleLineWidth = false;
                    _ve.IsVisible = true;
                }
                else
                {
                    _ve.Children.Clear();
                }

                Point p = new Point(x, y);
                double d = Construct.Distance(_anchor, p);
                double a = Construct.Angle(_anchor, p);

                if (d != 0)
                {
                    if (_arrowLocation == ArrowLocation.Start || _arrowLocation == ArrowLocation.Both)
                    {
                        _ve.AddChild(CGeometry.ArrowPointCollection(_anchor, p, _arrowStyle.Type, _size, _arrowStyle.Aspect));
                    }

                    List<Point> pc = new List<Point>();
                    pc.Add(_anchor);
                    pc.Add(p);
                    _ve.AddChild(pc);

                    if (_arrowLocation == ArrowLocation.End || _arrowLocation == ArrowLocation.Both)
                    {
                        _ve.AddChild(CGeometry.ArrowPointCollection(p, _anchor, _arrowStyle.Type, _size, _arrowStyle.Aspect));
                    }

                    Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                    Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
                }

                Globals.Events.CoordinateDisplay(new Point(x, y), x - _anchor.X, y - _anchor.Y, d, a);
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }
    }

    public class RubberBandCircle : VRubberBand
    {
        VectorEntity _ve2 = null;

        public RubberBandCircle()
            : base()
        {
        }

        public RubberBandCircle(Color color)
            : base(color)
        {
        }

        public override void Reset()
        {
            base.Reset();
        }

        public override void EndTracking()
        {
            if (_ve2 != null)
            {
                Globals.DrawingCanvas.VectorListControl.RemoveOverlaySegment(_ve2.SegmentId);
                _ve2 = null;
            }
            base.EndTracking();
        }

        public override void TrackCursor(double x, double y)
        {
            if (_state > 0)
            {
                if (_ve == null)
                {
                    _ve = new VectorEntity(_segmentId, 100000);

                    _ve.Color = _color;

                    //_ve.LineWidth = Globals.View.DisplayToPaper(_thickness);
                    _ve.LineWidth = _thickness;
                    _ve.ScaleLineWidth = false;
                    _ve.IsVisible = true;

                    _ve2 = new VectorEntity(_segmentId - 1, 100000);

                    _ve2.Color = Globals.ActiveDrawing.Theme.ForegroundColor;
                    //_ve2.LineWidth = Globals.View.DisplayToPaper(_thickness / 3);
                    _ve2.LineWidth = _thickness / 3;
                    _ve2.ScaleLineWidth = false;
                    _ve2.IsVisible = true;
                }
                else
                {
                    _ve.Children.Clear();
                    _ve2.Children.Clear();
                }

                Point p = new Point(x, y);
                double r = Construct.Distance(_anchor, p);

                VectorArcEntity va = new VectorArcEntity();
                va.IsCircle = true;
                va.Radius = r;
                va.Center = _anchor;
                _ve.AddChild(va);

                if (_shouldFill)
                {
                    _ve.Fill = true;
                    _ve.FillColor = _fillColor;

                    CrosshatchVE(_ve, _fillPattern, _patternScale, _patternAngle);
                }

                double size = Globals.View.DisplayToPaper(cDotSize);
                List<Point> pc = CGeometry.ArrowPointCollection(p, _anchor, ArrowType.Filled, size * 1.5, .25);
                pc.Add(_anchor);
                _ve2.AddChild(pc);

                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve2);
                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

                CoordinateDisplayVector(new Point(x, y));
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }
    }

    public class RubberBandTextBox : VRubberBand
    {
        public RubberBandTextBox()
            : base()
        {
        }

        public RubberBandTextBox(Color color)
            : base(color)
        {
        }

        protected TextPosition _position = TextPosition.On;
        protected TextAlignment _alignment = TextAlignment.Center;
        protected double _height = 0.125;
        protected double _offset = 0;

        public TextPosition Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        public TextAlignment Alignment
        {
            get
            {
                return _alignment;
            }
            set
            {
                _alignment = value;
            }
        }

        public double Height
        {
            get
            {
                return _height;
            }
            set
            {
                _height = value;
            }
        }

        public double Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                _offset = value;
            }
        }

        public override void TrackCursor(double x, double y)
        {
            if (_state > 0)
            {
                if (_ve == null)
                {
                    _ve = new VectorEntity(_segmentId, 100000);

                    _ve.Color = _color;
                    //_ve.LineWidth = Globals.View.DisplayToPaper(_thickness);
                    _ve.LineWidth = _thickness;
                    _ve.ScaleLineWidth = false;
                    _ve.IsVisible = true;
                }
                else
                {
                    _ve.Children.Clear();
                }

                Point p = new Point(x, y);
                double d = Construct.Distance(_anchor, p);
                double a = Construct.Angle(_anchor, p);

                double size = Globals.View.DisplayToPaper(cDotSize);

                List<Point> pc = new List<Point>();
                pc.Add(_anchor);
                pc.Add(p);
                _ve.AddChild(pc);

                _ve.AddChild(CGeometry.ArrowPointCollection(p, _anchor, ArrowType.Filled, size * 1.5, .25));
                _ve.AddChild(CGeometry.TextBoxPointCollection(_anchor, d, _height, _offset, _position, _alignment, a));

                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

                Globals.Events.CoordinateDisplay(p, x - _anchor.X, y - _anchor.Y, d, a);
            }
            else
            {
                base.TrackCursor(x, y);
            }
        }
    }
}
