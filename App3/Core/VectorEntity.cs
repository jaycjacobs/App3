//#define DEBUG_RECT
using Cirros.Core;
using Cirros.Display;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI;


#if UWP
using Microsoft.Graphics.Canvas.Geometry;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
#else
using CirrosCore;
using System.Windows;
using System.Windows.Media;
#endif

namespace Cirros.Drawing
{
    public class VectorEntity : IComparable
    {
        double _lineWidth = .01;
        double _opacity = 1;
        Color _color = Colors.Black;
        Color _fillColor = Colors.CadetBlue;
        DoubleCollection _dashList = null;
        private List<object> _children = new List<object>();
        bool _fill = false;
        bool _fillEvenOdd = false;
        private bool _isVisible = true;
        private bool _isSelectable = true;
        private bool _isHighlighted = false;
        private bool _scaleLineWidth = true;
        private int _lineTypeId = 0;
        private Rect _box = Rect.Empty;

        int _zindex;

        private uint _id;

        public VectorEntity(uint segmentId, int zindex)
        {
            _id = segmentId;
            _zindex = zindex;

            InitializeItemBox();
        }

        public int ZIndex
        {
            get { return _zindex; }
            set { _zindex = value; }
        }

        public uint SegmentId
        {
            get { return _id; }
        }

        private void InitializeItemBox()
        {
            _box = Rect.Empty;
        }

        private void AddToItemBox(Point p)
        {
            _box.Union(p);
        }

        private void AddToItemBox(Rect r)
        {
            _box.Union(r);
        }

        public Rect ItemBox
        {
            get
            {
                return _box;
            }
        }

        public bool ScaleLineWidth
        {
            get { return _scaleLineWidth; }
            set { _scaleLineWidth = value; }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; }
        }

        public bool IsSelectable
        {
            get { return _isSelectable; }
            set { _isSelectable = value; }
        }

        public bool IsHighlighted
        {
            get { return _isHighlighted; }
            set { _isHighlighted = value; }
        }

        public double LineWidth
        {
            get { return _lineWidth; }
            set { _lineWidth = value; }
        }

        public Color Color
        {
            get { return _color; }
            set { _color = value; }
        }

        public double Opacity
        {
            get { return _opacity; }
            set { _opacity = value; }
        }

        public Color FillColor
        {
            get { return _fillColor; }
            set { _fillColor = value; }
        }

        public bool FillEvenOdd
        {
            get { return _fillEvenOdd; }
            set { _fillEvenOdd = value; }
        }

        public int LineType
        {
            get { return _lineTypeId; }
            set { _lineTypeId = value; }
        }

        public DoubleCollection DashList
        {
            get { return _dashList; }
            set { _dashList = value; }
        }

        public List<object> Children
        {
            get { return _children; }
            private set { _children = value; }
        }

        public void RemoveChildren()
        {
            InitializeItemBox();

            _children.Clear();
        }

        private Rect GetItemBox(object child)
        {
            Rect r = Rect.Empty;

            if (child is List<Point>)
            {
                foreach (Point p in (List<Point>)child)
                {
                    r.Union(p);
                }
            }
            else if (child is VectorEntity)
            {
                VectorEntity ve = child as VectorEntity;
                if (ve.Children != null)
                {
                    foreach (object o in ve.Children)
                    {
                        r.Union(GetItemBox(o));
                    }
                }
            }
            else
            {
                Matrix3x2 matrix = Matrix3x2.Identity;

                if (child is VectorArcEntity)
                {
                    r.Union(((VectorArcEntity)child).ItemBox);
                }
                else if (child is VectorMarkerEntity)
                {
                    r.Union(((VectorMarkerEntity)child).ItemBox);
                }
                else if (child is VectorImageEntity)
                {
                    r.Union(((VectorImageEntity)child).ItemBox);
                }
                else if (child is VectorTextEntity)
                {
                    r.Union(((VectorTextEntity)child).ItemBox);
                }
            }

            return r;
        }

        public void AddChild(object child)
        {
            AddToItemBox(GetItemBox(child));
            _children.Add(child);
        }

        public bool Fill
        {
            get { return _fill; }
            set { _fill = value; }
        }

        public void SetFillFromPrimitive(Primitive p)
        {
            if (p.Fill != (uint)ColorCode.NoFill)
            {
                Color fillColor;

                if (p.Fill == (uint)ColorCode.ByLayer)
                {
                    fillColor = Utilities.ColorFromColorSpec(Globals.LayerTable[p.LayerId].ColorSpec);
                }
                else if (p.Fill == (uint)ColorCode.SameAsOutline)
                {
                    if (p.ColorSpec == (uint)ColorCode.ByLayer)
                    {
                        fillColor = Utilities.ColorFromColorSpec(Globals.LayerTable[p.LayerId].ColorSpec);
                    }
                    else
                    {
                        fillColor = this.Color;
                    }
                }
                else
                {
                    fillColor = Utilities.ColorFromColorSpec(p.Fill);
                }

                this.Fill = true;
                this.FillColor = fillColor;
            }
        }

        internal void UpdateBox()
        {
            _box = this.GetItemBox(this);
        }

        public void Move(double dx, double dy)
        {
            foreach (object o in Children)
            {
                if (o is List<Point>)
                {
                    List<Point> pc = o as List<Point>;
                    for (int i = 0; i < pc.Count; i++)
                    {
                        pc[i] = new Point(pc[i].X + dx, pc[i].Y + dy);
                    }
                }
                else if (o is VectorEntity)
                {
                    (o as VectorEntity).Move(dx, dy);
                }
                else if (o is VectorMarkerEntity)
                {
                    VectorMarkerEntity vm = o as VectorMarkerEntity;
                    vm.Move(dx, dy);
                }
                else if (o is VectorArcEntity)
                {
                    VectorArcEntity va = o as VectorArcEntity;
                    va.Move(dx, dy);
                }
                else if (o is VectorTextEntity)
                {
                    VectorTextEntity vt = o as VectorTextEntity;
                    vt.Move(dx, dy);
                }
                else if (o is VectorImageEntity)
                {
                    VectorImageEntity vi = o as VectorImageEntity;
                    vi.Move(dx, dy);
                }
            }

            if (_box.IsEmpty == false)
            {
                _box.X += dx;
                _box.Y += dy;
            }
        }

        public double Pick(Point p, double tolerance, ref uint segmentId)
        {
            double distance = tolerance;
            foreach (object o in Children)
            {
                if (o is List<Point>)
                {
                    double ds = Construct.DistancePointToPolyline(p, o as List<Point>);
                    if (ds < tolerance)
                    {
                        distance = ds;
                        segmentId = _id;
                    }
                    else if (_fill)
                    {
                        if (Construct.PointInPolygon(p, o as List<Point>))
                        {
                            distance = 0;
                            segmentId = _id;
                        }
                    }
                }
                else if (o is VectorEntity)
                {
                    double ds = (o as VectorEntity).Pick(p, tolerance, ref segmentId);
                    if (ds < tolerance)
                    {
                        distance = ds;
                        segmentId = (o as VectorEntity).SegmentId;
                    }
                }
                else if (o is VectorMarkerEntity)
                {
                    double ds = (o as VectorMarkerEntity).Pick(p, tolerance);
                    if (ds < tolerance)
                    {
                        distance = ds;
                        segmentId = _id;
                    }
                }
                else if (o is VectorArcEntity)
                {
                    double ds = (o as VectorArcEntity).Pick(p, tolerance);
                    if (ds < tolerance)
                    {
                        distance = ds;
                        segmentId = _id;
                    }
                }
                else if (o is VectorTextEntity)
                {
                    double ds = (o as VectorTextEntity).Pick(p, tolerance);
                    if (ds < tolerance)
                    {
                        distance = ds;
                        segmentId = _id;
                    }
                }
                else if (o is VectorImageEntity)
                {
                    double ds = (o as VectorImageEntity).Pick(p, tolerance);
                    if (ds < tolerance)
                    {
                        distance = ds;
                        segmentId = _id;
                    }
                }
            }

            return distance;
        }

        public int CompareTo(object obj)
        {
            return this._zindex.CompareTo(((VectorEntity)obj)._zindex);
        }
    }

    public class VectorArcEntity
    {
        private double _radius;
        private double _startAngle;
        private double _includedAngle;
        private Point _center;
        private bool _isCircle;
        private Rect _box = Rect.Empty;

        public Point Center
        {
            get { return _center; }
            set { _center = value; }
        }

        public double Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        public double StartAngle
        {
            get { return _startAngle; }
            set { _startAngle = value; }
        }

        public double IncludedAngle
        {
            get { return _includedAngle; }
            set { _includedAngle = value; }
        }

        public bool IsCircle
        {
            get { return _isCircle; }
            set { _isCircle = value; }
        }

        public Rect ItemBox
        {
            get
            {
                if (_box.IsEmpty)
                {
                    double diameter = _radius * 2;
                    if (_isCircle)
                    {
                        _box = new Rect(_center.X - _radius, _center.Y - _radius, diameter, diameter);
                    }
                    else
                    {
                        int net = 0;
                        _box.Union(Construct.PolarOffset(_center, _radius, _startAngle));
                        _box.Union(Construct.PolarOffset(_center, _radius, _startAngle + _includedAngle));

                        double pi2 = Math.PI / 2;

                        if (_includedAngle > 0)
                        {
                            double start = ((int)Math.Floor((_startAngle + pi2) / pi2)) * pi2;
                            double end = Math.Round(_startAngle + _includedAngle, 6);
                            start = Math.Round(start, 6);

                            for (double a = start; a < end; a += pi2)
                            {
                                _box.Union(Construct.PolarOffset(_center, _radius, a));
                                if (++net > 4)
                                {
                                    double a0 = Math.Abs(end - a);
                                    if (a0 > .001)
                                    {
                                        Analytics.ReportEvent("Arc positive box overflow");
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            double start = ((int)Math.Ceiling((_startAngle - pi2) / pi2)) * pi2;
                            double end = _startAngle + _includedAngle;

                            for (double a = start; a > end; a -= pi2)
                            {
                                _box.Union(Construct.PolarOffset(_center, _radius, a));
                                if (++net > 4)
                                {
                                    Analytics.ReportEvent("Arc negative box overflow");
                                    break;
                                }
                            }
                        }
                    }
                }

                return _box;
            }
        }

        public void Move(double dx, double dy)
        {
            _center.X += dx;
            _center.Y += dy;

            if (_box.IsEmpty == false)
            {
                _box.X += dx;
                _box.Y += dy;
            }
        }

        public double Pick(Point p, double distance)
        {
            double dp = Construct.Distance(_center, p);
            double ds = Math.Abs(dp - _radius);
            if (ds < distance)
            {
                if (_isCircle)
                {
                    distance = ds;
                }
                else
                {
                    double angle = Construct.Angle(_center, p);
                    if (Construct.ArcIncludesAngle(_startAngle, _includedAngle, angle, distance))
                    {
                        distance = ds;
                    }
                }
            }

            return distance;
        }
    }

    public class VectorMarkerEntity
    {
        private double _size = 8;

        private Rect _box = Rect.Empty;
        private Point _location;
        private HandleType _type = HandleType.Square;
        private double _opacity = 1;

        public Point Location
        {
            get { return _location; }
            set {
                _box = Rect.Empty;
                _location = value;
            }
        }

        public HandleType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public double Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public double Opacity
        {
            get { return _opacity; }
            set { _opacity = value; }
        }

        public Rect ItemBox
        {
            get
            {
                if (_box.IsEmpty)
                {
                    double halfSize = _size / 2;
                    _box = new Rect(_location.X - halfSize, _location.Y - halfSize, _size, _size);
                }

                return _box;
            }
        }

        public void Move(double dx, double dy)
        {
            _location.X += dx;
            _location.Y += dy;

            if (_box.IsEmpty == false)
            {
                _box.X += dx;
                _box.Y += dy;
            }
        }

        public double Pick(Point p, double distance)
        {
            return distance;
        }
    }

    public class VectorImageEntity
    {
        private double _width;
        private double _height;
        private double _opacity;
        private Point _origin;
        private Matrix _matrix;
        private string _imageId;
        private Rect _box = Rect.Empty;

        public Point Origin
        {
            get { return _origin; }
            set { _origin = value; }
        }

        public double Width
        {
            get { return _width; }
            set { _width = value; }
        }

        public double Height
        {
            get { return _height; }
            set { _height = value; }
        }

        public double Opacity
        {
            get { return _opacity; }
            set { _opacity = value; }
        }

        public Matrix Matrix
        {
            get { return _matrix; }
            set { _matrix = value; }
        }

        public string ImageId
        {
            get { return _imageId; }
            set { _imageId = value; }
        }

        public Rect ItemBox
        {
            get
            {
               if (_box.IsEmpty)
                {
                    if (_matrix.IsIdentity)
                    {
                        _box = new Rect(_origin.X, _origin.Y, _width, _height);
                    }
                    else
                    {
                        Point p1 = new Point(0, 0);
                        Point p2 = new Point(0, _height);
                        Point p3 = new Point(_width, _height);
                        Point p4 = new Point(_width, 0);

                        MatrixTransform mtf = new MatrixTransform();
                        mtf.Matrix = _matrix;
#if UWP
                        p1 = mtf.TransformPoint(p1);
                        p2 = mtf.TransformPoint(p2);
                        p3 = mtf.TransformPoint(p3);
                        p4 = mtf.TransformPoint(p4);
#else
                        p1 = mtf.Transform(p1);
                        p2 = mtf.Transform(p2);
                        p3 = mtf.Transform(p3);
                        p4 = mtf.Transform(p4);
#endif

                        p1.X += _origin.X;
                        p1.Y += _origin.Y;
                        p2.X += _origin.X;
                        p2.Y += _origin.Y;
                        p3.X += _origin.X;
                        p3.Y += _origin.Y;
                        p4.X += _origin.X;
                        p4.Y += _origin.Y;

                        _box.Union(p1);
                        _box.Union(p2);
                        _box.Union(p3);
                        _box.Union(p4);
                    }
                }

                return _box;
            }
        }

        public void Move(double dx, double dy)
        {
            _origin.X += dx;
            _origin.Y += dy;

            if (_box.IsEmpty == false)
            {
                _box.X += dx;
                _box.Y += dy;
            }
        }

        public double Pick(Point p, double distance)
        {
            //if (_box.IsEmpty)
            {
                if (_matrix.IsIdentity)
                {
                    Rect box = new Rect(_origin.X, _origin.Y, _width, _height);
                    if (box.Contains(p))
                    {
                        distance = 0;
                    }
                }
                else
                {
                    Point p1 = new Point(0, 0);
                    Point p2 = new Point(0, _height);
                    Point p3 = new Point(_width, _height);
                    Point p4 = new Point(_width, 0);

                    MatrixTransform mtf = new MatrixTransform();
                    mtf.Matrix = _matrix;

#if UWP
                    p1 = mtf.TransformPoint(p1);
                    p2 = mtf.TransformPoint(p2);
                    p3 = mtf.TransformPoint(p3);
                    p4 = mtf.TransformPoint(p4);
#else
                    p1 = mtf.Transform(p1);
                    p2 = mtf.Transform(p2);
                    p3 = mtf.Transform(p3);
                    p4 = mtf.Transform(p4);
#endif

                    List<Point> pc = new List<Point>();
                    pc.Add(new Point(p1.X + _origin.X, p1.Y + _origin.Y));
                    pc.Add(new Point(p2.X + _origin.X, p2.Y + _origin.Y));
                    pc.Add(new Point(p3.X + _origin.X, p3.Y + _origin.Y));
                    pc.Add(new Point(p4.X + _origin.X, p4.Y + _origin.Y));
                    pc.Add(pc[0]);

                    if (Construct.PointInPolygon(p, pc))
                    {
                        distance = 0;
                    }
                    else
                    {
                        double ds = Construct.DistancePointToPolyline(p, pc);
                        if (ds < distance)
                        {
                            distance = ds;
                        }
                    }
                }
            }
            return distance;
        }
    }

    public class VectorTextEntity
    {
        private string _text;
        private Point _origin;
        private double _textHeight;
        private double _angle;
        private double _lineSpacing;
        private double _charSpacing;
        private string _fontFamily;
        private TextAlignment _textAlign;
        private TextPosition _textPosition;
        private Point _msOrigin;
        private Point _psOffset;
        private Rect _box = Rect.Empty;

        public Point Location
        {
            get { return _origin; }
            set { _origin = value; }
        }

        public Point Origin
        {
            get { return _msOrigin; }
            set { _msOrigin = value; }
        }

        public Rect ItemBox
        {
            get
            {
                if (_box == Rect.Empty)
                {
                    _box = CalculateItemBox();
                }
                return _box;
            }
        }

        public Point PsOffset
        {
            get { return _psOffset; }
            set { _psOffset = value; }
        }

        public double CharacterSpacing
        {
            get { return _charSpacing; }
            set { _charSpacing = value; }
        }

        public double LineSpacing
        {
            get { return _lineSpacing; }
            set { _lineSpacing = value; }
        }

        public TextAlignment TextAlignment
        {
            get { return _textAlign; }
            set { _textAlign = value; }
        }

        public TextPosition TextPosition
        {
            get { return _textPosition; }
            set { _textPosition = value; }
        }

        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        public double TextHeight
        {
            get { return _textHeight; }
            set { _textHeight = Math.Max(value, .001); }
        }

        public string FontFamily
        {
            get { return _fontFamily; }
            set { _fontFamily = value; }
        }

        public double Angle
        {
            get { return _angle; }
            set
            {
                _angle = value;
            }
        }

        public Rect CalculateItemBox()
        {
            Rect box = Rect.Empty;
#if UWP
            double lh = _textHeight * _lineSpacing;

            string[] lines = _text.Split(new[] { '\n' });

            double fs = Dx.GetFontSizeFromHeight(_fontFamily, _textHeight);
            double height, descent;
            TextUtilities.FontInfo.FontHeight(_fontFamily, fs, out height, out descent);

            double sw = 0;

            foreach (string s in lines)
            {
                double w = 0;
                try
                {
                    w = Cirros.TextUtilities.FontInfo.StringWidth(s, _fontFamily, fs);
                }
                catch
                {
                    w = _textHeight * .6 * s.Length;
                }

                if (w > sw)
                {
                    sw = w;
                }
            }

            sw *= _charSpacing;

            double dx = 0;
            double dy = 0;

            if (_textAlign == TextAlignment.Center)
            {
                dx = -sw / 2;
            }
            else if (_textAlign == TextAlignment.Right)
            {
                dx = -sw;
            }

            if (_textPosition == TextPosition.Above)
            {
                dy = -(lines.Length - 1) * lh;
            }
            else if (_textPosition == TextPosition.On)
            {
                dy = -(lines.Length - 1) * lh / 2;
            }

            if (_angle == 0)
            {
                box = new Rect(_origin.X + dx, _origin.Y + dy - height, sw, height + (lh * (lines.Length - 1)) + descent);
            }
            else if (_angle == double.NaN)
            {

            }
            else
            {
                Point p1 = new Point(dx, dy - height);
                Point p2 = new Point(dx + sw, p1.Y);
                Point p3 = new Point(p2.X, p1.Y + height + (lh * (lines.Length - 1)) + descent);
                Point p4 = new Point(dx, p3.Y);

                RotateTransform rtf = new RotateTransform();
                rtf.Angle = _angle;

                p1 = rtf.TransformPoint(p1);
                p2 = rtf.TransformPoint(p2);
                p3 = rtf.TransformPoint(p3);
                p4 = rtf.TransformPoint(p4);

                p1.X += _origin.X;
                p1.Y += _origin.Y;
                p2.X += _origin.X;
                p2.Y += _origin.Y;
                p3.X += _origin.X;
                p3.Y += _origin.Y;
                p4.X += _origin.X;
                p4.Y += _origin.Y;

                box.Union(p1);
                box.Union(p2);
                box.Union(p3);
                box.Union(p4);
            }
#else
#endif
            return box;
        }

        public void Move(double dx, double dy)
        {
            _msOrigin.X += dx;
            _msOrigin.Y += dy;
            _origin.X += dx;
            _origin.Y += dy;

            if (_box.IsEmpty == false)
            {
                _box.X += dx;
                _box.Y += dy;
            }
        }

        public double Pick(Point p, double distance)
        {
#if UWP
            double lh = _textHeight * _lineSpacing;

            string[] lines = _text.Split(new[] { '\n' });

            double fs = Dx.GetFontSizeFromHeight(_fontFamily, _textHeight);
            double height, descent;
            TextUtilities.FontInfo.FontHeight(_fontFamily, fs, out height, out descent);

            double sw = 0;

            foreach (string s in lines)
            {
                double w = 0;
                try
                {
                    w = Cirros.TextUtilities.FontInfo.StringWidth(s, _fontFamily, fs);
                }
                catch
                {
                    w = _textHeight * .6 * s.Length;
                }

                if (w > sw)
                {
                    sw = w;
                }
            }

            sw *= _charSpacing;

            double dx = 0;
            double dy = 0;

            if (_textAlign == TextAlignment.Center)
            {
                dx = -sw / 2;
            }
            else if (_textAlign == TextAlignment.Right)
            {
                dx = -sw;
            }

            if (_textPosition == TextPosition.Above)
            {
                dy = -(lines.Length - 1) * lh;
            }
            else if (_textPosition == TextPosition.On)
            {
                dy = -(lines.Length - 1) * lh / 2;
            }

            Rect box = Rect.Empty;

            if (_angle == 0)
            {
                box = new Rect(_origin.X + dx, _origin.Y - height + dy, sw, height + (lines.Length - 1) * lh + descent);
#if DEBUG_RECT
                VectorEntity ve0 = new VectorEntity(77777, 77777);

                ve0.Color = Colors.Green;
                ve0.LineWidth = Globals.DrawingCanvas.DisplayToPaper(1);

                List<Point> pc = new List<Point>();
                pc.Add(new Point(box.X, box.Y));
                pc.Add(new Point(box.X + box.Width, box.Y));
                pc.Add(new Point(box.X + box.Width, box.Y + box.Height));
                pc.Add(new Point(box.X, box.Y + box.Height));
                pc.Add(new Point(box.X, box.Y));
                ve0.AddChild(pc);

                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(ve0);
                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
#endif
                if (box.Contains(p))
                {
                    distance = 0;
                }
                else if (p.X >= box.Left && p.X <= box.Right)
                {
                    if (p.Y <= box.Top)
                    {
                        distance = box.Top - p.Y;
                    }
                    else
                    {
                        distance = p.Y - box.Bottom;
                    }
                }
                else if (p.Y >= box.Top && p.Y <= box.Bottom)
                {
                    if (p.X <= box.Left)
                    {
                        distance = box.Left - p.X;
                    }
                    else
                    {
                        distance = p.X - box.Right;
                    }
                }
            }
            else
            {
                Point p1 = new Point(dx, dy - height);
                Point p2 = new Point(dx + sw, p1.Y);
                Point p3 = new Point(p2.X, p1.Y + height + (lh * (lines.Length - 1)) + descent);
                Point p4 = new Point(dx, p3.Y);

                RotateTransform rtf = new RotateTransform();
                rtf.Angle = _angle;

                p1 = rtf.TransformPoint(p1);
                p2 = rtf.TransformPoint(p2);
                p3 = rtf.TransformPoint(p3);
                p4 = rtf.TransformPoint(p4);

                List<Point> pc = new List<Point>();
                pc.Add(new Point(p1.X + _origin.X, p1.Y + _origin.Y));
                pc.Add(new Point(p2.X + _origin.X, p2.Y + _origin.Y));
                pc.Add(new Point(p3.X + _origin.X, p3.Y + _origin.Y));
                pc.Add(new Point(p4.X + _origin.X, p4.Y + _origin.Y));
                pc.Add(pc[0]);

#if DEBUG_RECT
                VectorEntity ve0 = new VectorEntity(77777, 77777);

                ve0.Color = Colors.Green;
                ve0.LineWidth = Globals.DrawingCanvas.DisplayToPaper(1);

                ve0.AddChild(pc);

                Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(ve0);
                Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
#endif

                if (Construct.PointInPolygon(p, pc))
                {
                    distance = 0;
                }
                else
                {
                    double ds = Construct.DistancePointToPolyline(p, pc);
                    if (ds < distance)
                    {
                        distance = ds;
                    }
                }
            }
#else
            distance = double.MaxValue;
#endif
            return distance;
        }
    }

    public class VectorContext
    {
        public VectorContext(bool hasArcPrimitive, bool canAlignText, bool textOriginLowerLeft)
        {
            HasArcPrimitive = hasArcPrimitive;
            CanAlignText = canAlignText;
            TextOriginLowerLeft = textOriginLowerLeft;
        }

        public bool HasArcPrimitive { get; private set; }
        public bool CanAlignText { get; private set; }
        public bool TextOriginLowerLeft { get; private set; }
    }
}
