using Cirros.Core;
using Cirros.Drawing;
using System;
using System.Collections.Generic;
using Cirros.Utility;
using Cirros.Display;
using CirrosCore;
#if UWP
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
#else
using System.Windows;
using System.Windows.Media;
using static CirrosCore.WpfStubs;
#endif

namespace Cirros.Primitives
{
    public abstract class Primitive : IComparable
    {
        protected VectorEntity _ve = null;

        protected uint _objectId = 0;

        protected int _layerId = -1;

        protected int _serializedLayerId = -1;
        protected int _serializedLineTypeId = -1;
        protected int _serializedTextStyleId = -1;
        protected int _serializedArrowStyleId = -1;

        protected Rect _bbox;

        protected uint _colorSpec = 0xff000000;
        protected uint _fill = (uint)ColorCode.ByLayer;
        protected int _lineWeightId = -1;
        protected int _lineTypeId = -1;
        protected bool _fillEvenOdd = false;

        private object _parent = null;

        protected Point _origin;
        private Point _ll;
        private Point _ur;

        protected Matrix _matrix = CGeometry.IdentityMatrix();

        protected bool _isGroupMember = false;
        protected bool _isInstanceMember = false;

        protected bool _constructEnabled = true;

        protected List<ConstructNode> _staticConstructNodes = new List<ConstructNode>();
        protected List<ConstructNode> _dynamicConstructNodes = new List<ConstructNode>();

        protected int _zIndex;

        protected double _opacity = 1;
        protected bool _isVisible = true;
        private bool _isDynamic = false;

        protected IDrawingContainer _container;

        protected string _fillPattern = null;
        protected double _fillScale = 1;
        protected double _fillAngle = 0;

        protected String _tag = null;

        public abstract Primitive Clone();

        public abstract int ActiveLayer { get; }

        protected Primitive(Point origin)
        {
            //_container = Globals.ActiveDrawing;
            //_objectId = Globals.ActiveDrawing.NewObjectId();

            _origin = origin;
            //_layerId = Globals.LayerId;
            _layerId = ActiveLayer;
            _colorSpec = Globals.ColorSpec;
            _lineWeightId = Globals.LineWeightId;
            _lineTypeId = Globals.LineTypeId;
            _zIndex = Globals.ActiveDrawing.MaxZIndex;
        }

        public Primitive(Primitive original)
        {
            //_container = Globals.ActiveDrawing;
            //_objectId = Globals.ActiveDrawing.NewObjectId();

            _origin = original._origin;
            _layerId = original._layerId;
            _matrix = original._matrix;
            _colorSpec = original._colorSpec;
            _lineWeightId = original._lineWeightId;
            _lineTypeId = original._lineTypeId;
            _zIndex = original.ZIndex;
        }

        public abstract PrimitiveType TypeName
        {
            get;
        }

        public virtual string Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        protected Primitive(Entity e, IDrawingContainer drawingCanvas)
        {
            _container = drawingCanvas;

            _origin = e.IsGroupMember ? _container.ModelToPaperDelta(new FPoint(e.X, e.Y)) : _container.ModelToPaper(new Point((double)e.X, (double)e.Y));
            _isGroupMember = e.IsGroupMember;
            _serializedLayerId = e.LayerId;
            _serializedLineTypeId = e.LineTypeId;

            if (e.TextAttributesSpecified)
            {
                _serializedTextStyleId = e.TextAttributes.TextStyleId;
            }
            if (e.ArrowStyleIdSpecified)
            {
                _serializedArrowStyleId = e.ArrowStyleId;
            }

            if (Globals.LayerTable.ContainsKey(e.LayerId))
            {
                _layerId = e.LayerId;
            }
            else
            {
                _layerId = Globals.LayerTable.Keys.GetEnumerator().Current;
            }
            _colorSpec = e.ColorSpec;
            _matrix = e.Matrix;
            _zIndex = e.ZIndex;
            _objectId = e.ObjectId == 0 ? _container.NewObjectId() : e.ObjectId;
        }

        public virtual void Draw()
        {
            if (_parent == null)
            {
                if (_container == null)
                {

                }
                else
                {
#if UWP
                    VectorEntity ve = Globals.DrawingCanvas.VectorListControl.UpdateSegment(this);
                    if (ve != null)
                    {
                        _bbox = ve.ItemBox;
                    }
#else
#endif
                }
            }
        }

        public virtual Entity Serialize()
        {
            if (_container == null)
            {
                _container = Globals.ActiveDrawing;
            }

            Entity e = new Entity();

            FPoint m = _isGroupMember ? _container.PaperToModelDeltaF(_origin) : _container.PaperToModelF(_origin);

            e.Type = TypeName;
            e.X = m.X;
            e.Y = m.Y;
            e.LayerId = LayerId;
            e.ColorSpec = _colorSpec;
            e.Matrix = _matrix;
            e.ZIndex = _zIndex;

            if (_objectId != 0)
            {
                e.ObjectId = _objectId;
            }

            return e;
        }

        public static Primitive DeserializeFromEntity(Entity e, IDrawingContainer drawingCanvas)
        {
            Primitive p = null;

            try
            {
                switch (e.Type)
                {
                    case PrimitiveType.Arc:
                        p = new PArc(e, drawingCanvas);
                        break;

                    case PrimitiveType.Arc3:
                        p = new PArc3(e, drawingCanvas);
                        break;

                    case PrimitiveType.Arrow:
                        p = new PArrow(e, drawingCanvas);
                        break;

                    //case PrimitiveType.Bezier:
                    //    p = new PBezier(e, drawingCanvas);
                    //    break;

                    case PrimitiveType.BSpline:
                        p = new PBSpline(e, drawingCanvas);
                        break;

                    case PrimitiveType.Dimension:
                        p = new PDimension(e, drawingCanvas);
                        break;

                    case PrimitiveType.Doubleline:
                        p = new PDoubleline(e, drawingCanvas);
                        break;

                    case PrimitiveType.Ellipse:
                        p = new PEllipse(e, drawingCanvas);
                        break;

                    case PrimitiveType.Instance:
                        p = new PInstance(e, drawingCanvas);
                        break;

                    case PrimitiveType.Line:
                        p = new PLine(e, drawingCanvas);
                        break;

                    case PrimitiveType.Polygon:
                        p = new PPolygon(e, drawingCanvas);
                        break;

                    case PrimitiveType.Rectangle:
                        p = new PRectangle(e, drawingCanvas);
                        break;

                    case PrimitiveType.Text:
                        p = new PText(e, drawingCanvas);
                        break;

                    case PrimitiveType.Image:
                        p = new PImage(e, drawingCanvas);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception when deserializing entity: {0}", ex.Message);
            }

            return p;
        }

        public uint Id
        {
            get
            {
                return _objectId;
            }
            set
            {
                _objectId = value;
            }
        }

        public virtual List<Primitive> Stroke()
        {
            List<Primitive> primitives = new List<Primitive>();
            VectorContext context = new VectorContext(false, true, true);

            VectorEntity ve = this.Vectorize(context);

            Primitive p = PrimitiveUtilities.PLineFromVectorEntity(ve);
            p.LayerId = this.LayerId;
            p.ColorSpec = this.ColorSpec;
            p.LineWeightId = this.LineWeightId;
            p.LineTypeId = this.LineTypeId;
            p.ZIndex = this.ZIndex;

            primitives.Add(p);

            return primitives;
        }

        public virtual bool Pick(Point paper, out double distance)
        {
            distance = 0;
            bool hit = false;
            double t = Globals.View.DisplayToPaper(Globals.hitTolerance);

            if (paper.X > (_bbox.Left - t) && paper.X < (_bbox.Right + t) && paper.Y > (_bbox.Top - t) && paper.Y < (_bbox.Bottom + t))
            {
                Point center = new Point((_bbox.Left + _bbox.Right) / 2, (_bbox.Top + _bbox.Bottom) / 2);
                distance = Construct.Distance(center, paper);

                hit = true;
            }

            return hit;
        }

        public virtual void Dispose()
        {
        }

        public object Parent
        {
            get { return _parent; }
            set
            {
                _parent = value;
                if (_parent is PInstance)
                {
                    _isInstanceMember = true;
                }
                else if (_parent is Group)
                {
                    _isGroupMember = true;
                }
            }
        }

        public Point Origin
        {
            get
            {
                return _origin;
            }
        }

        public bool IsTransformed
        {
            get
            {
                return _matrix.IsIdentity == false;
            }
        }

        public bool IsInstanceMember
        {
            get
            {
                return _isInstanceMember;
            }
            set
            {
                _isInstanceMember = value;
            }
        }

        public virtual bool IsGroupMember
        {
            get
            {
                return _isGroupMember;
            }
            set
            {
                _isGroupMember = value;
            }
        }

        public virtual bool FillEvenOdd
        {
            get
            {
                return _fillEvenOdd;
            }
            set
            {
                _fillEvenOdd = value;
            }
        }

        public bool ContainedBy(Rect r)
        {
            return _bbox.Left >= r.Left && _bbox.Top >= r.Top && _bbox.Right <= r.Right && _bbox.Bottom <= r.Bottom;
        }

        public bool IsNear(Point p, double t)
        {
            if (_bbox.IsEmpty)
            {
                return false;
            }
            double tt = t + t;
            Rect r = new Rect(_bbox.X - t, _bbox.Y - t, _bbox.Width + tt, _bbox.Height + tt);
            return r.Contains(p);
        }

        public bool Intersects(Rect r)
        {
            bool intersects = true;

            if (r.Left > _bbox.Right || r.Right < _bbox.Left)
            {
                intersects = false;
            }
            else if (r.Top > _bbox.Bottom || r.Bottom < _bbox.Top)
            {
                intersects = false;
            }

            return intersects;
        }

        public Matrix Matrix
        {
            get
            {
                return _matrix;
            }
            set
            {
                _matrix = value;
            }
        }

        public virtual void LayerChanged()
        {
            Draw();
        }

        public virtual void LineTypeChanged()
        {
            Draw();
        }

        public virtual void LineWeightChanged()
        {
            Draw();
        }

        public virtual double Opacity
        {
            get
            {
                return _opacity;
            }
            set
            {
                _opacity = value;

                Draw();
            }
        }

        private bool LayerIsVisible()
        {
            bool visible = true;

            if (Globals.LayerTable.ContainsKey(_layerId) == false)
            {
                Layer layer = Globals.LayerTable[_layerId];
                visible = layer.Visible;
            }
            return visible;
        }

        public virtual bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;

#if UWP
                    Globals.DrawingCanvas.VectorListControl.VectorList.ShowSegment(_objectId, _isVisible && LayerIsVisible());
                    Globals.DrawingCanvas.VectorListControl.Redraw();
#else
#endif
                }
            }
        }

        public virtual bool IsDynamic
        {
            get
            {
                if (_parent is Primitive parent)
                {
                    return parent.IsDynamic;
                }
                return _isDynamic;
            }
            set
            {
                _isDynamic = false;
                //if (_isDynamic != value)
                //{
                //    _isDynamic = value;
                //    if (Globals.ActiveDrawing.PrimitiveList.Contains(this))
                //    {
                //        Draw();
                //    }
                //    else
                //    {

                //    }
                //}
            }
        }

        public virtual uint ColorSpec
        {
            get
            {
                return _colorSpec;
            }
            set
            {
                if (_colorSpec != value)
                {
                    _colorSpec = value;

                    Draw();
                }
            }
        }

        public virtual int LineTypeId
        {
            get
            {
                return _lineTypeId;
            }
            set
            {
                _lineTypeId = value;

                Draw();
            }
        }

        public virtual int LineWeightId
        {
            get
            {
                return _lineWeightId;
            }
            set
            {
                _lineWeightId = value;

                Draw();
            }
        }

        public virtual int LayerId
        {
            get
            {
                return _layerId;
            }
            set
            {
                if (_layerId != value)
                {
                    _layerId = value;

                    Draw();
                }
            }
        }

        public int SerializedLayerId
        {
            get { return _serializedLayerId; }
        }

        public int SerializedLineTypeId
        {
            get { return _serializedLineTypeId; }
        }

        public int SerializedTextStyleId
        {
            get { return _serializedTextStyleId; }
        }

        public int SerializedArrowStyleId
        {
            get { return _serializedArrowStyleId; }
        }

        public uint Fill
        {
            get
            {
                return _fill;
            }
            set
            {
                _fill = value;

                Draw();
            }
        }

        public string FillPattern
        {
            get
            {
                return _fillPattern;
            }
            set
            {
                _fillPattern = value;
            }
        }

        public double PatternScale
        {
            get
            {
                return _fillScale;
            }
            set
            {
                _fillScale = value;
            }
        }

        public double PatternAngle
        {
            get
            {
                return _fillAngle;
            }
            set
            {
                _fillAngle = value;
            }
        }

        public virtual int ZIndex
        {
            get
            {
                return _zIndex;
            }
            set
            {
                _zIndex = value;

#if UWP
                Globals.DrawingCanvas.VectorListControl.SetSegmentZIndex(_objectId, _zIndex);
#else
#endif
            }
        }

        public virtual void AddToContainer(IDrawingContainer drawingCanvas)
        {
            _container = drawingCanvas;

            _objectId = _container.NewObjectId();
            _container.AddPrimitive(this);

            Draw();
        }

#if true
        public virtual void ShowHandles(Handles handles)
        {
            _drawHandles(handles);
        }
#else
        public virtual bool ShowHandles(Handles handles, bool normalize)
        {
            bool normalized = false;

            if (normalize)
            {
                if (IsTransformed)
                {
                    normalized = Normalize();
                }

                if (IsTransformed)
                {
                    // is this scope used?
                    normalized = Normalize() | normalized;
                    handles.Detach();
                    Highlight(true);
                }
                else
                {
                    _drawHandles(handles);
                }
            }
            else
            {
                _drawHandles(handles);
            }

            return normalized;
        }
#endif

        public virtual void Highlight(bool flag)
        {
            //_isDynamic = flag;
#if UWP
            Globals.DrawingCanvas.VectorListControl.HighlightSegment(_objectId, flag);
#else
#endif
        }

        public virtual void Rotate(Point c, double angle)
        {
            // Why is angle in degrees???
            Transform(c.X, c.Y, CGeometry.RotateMatrixAboutZ(angle));
        }

        public virtual void Scale(Point c, double xscale, double yscale)
        {
            Transform(c.X, c.Y, CGeometry.ScaleMatrix(xscale, yscale));
        }

        public virtual void Transform(double cx, double cy, Matrix m)
        {
            Point o = new Point(_origin.X - cx, _origin.Y - cy);
            o = m.Transform(o);
            this.MoveTo(cx + o.X, cy + o.Y);

            _matrix = CGeometry.MultiplyMatrix(_matrix, m);

            Draw();

            ClearStaticConstructNodes();
        }

        public virtual void SetTransform(double cx, double cy, Matrix m)
        {
            MoveTo(cx, cy);
            _matrix = m;

            ClearStaticConstructNodes();
        }

        public virtual bool Normalize(bool undoable = true)
        {
            return false;
        }

        public virtual bool UnNormalize(Matrix m)
        {
            return false;
        }

        protected virtual void _drawHandles(Handles handles)
        {
            handles.Attach(this);
            handles.AddHandle(1, _bbox.Left, _bbox.Top);
            handles.AddHandle(2, _bbox.Right, _bbox.Top);
            handles.AddHandle(3, _bbox.Right, _bbox.Bottom);
            handles.AddHandle(4, _bbox.Left, _bbox.Bottom);
            handles.Draw();
        }

        public Rect Box
        {
            get
            {
                return _bbox;
            }
        }

        public virtual bool ConstructEnabled
        {
            get
            {
                return _constructEnabled;
            }
            set
            {
                _constructEnabled = value;
            }
        }

        public virtual void ClearStaticConstructNodes()
        {
            _staticConstructNodes.Clear();
        }

        public virtual List<ConstructNode> ConstructNodes
        {
            get
            {
                if (_constructEnabled && _staticConstructNodes.Count == 0)
                {
                    _staticConstructNodes.Add(new ConstructNode(_origin, "origin"));
                }

                return _constructEnabled ? _staticConstructNodes : new List<ConstructNode>();
            }
        }

        public virtual List<ConstructNode> DynamicConstructNodes(Point from, Point through)
        {
            System.Diagnostics.Debug.Assert(_dynamicConstructNodes.Count == 0, "DynamicConstructNodes is not zero");
            return _dynamicConstructNodes;
        }

        public virtual void MoveTo(double x, double y)
        {
            double dx = x - _origin.X;
            double dy = y - _origin.Y;

            if (dx != 0 || dy != 0)
            {
                ClearStaticConstructNodes();

                _origin = new Point(x, y);

                _bbox.X += dx;
                _bbox.Y += dy;

#if UWP
                Globals.DrawingCanvas.VectorListControl.MoveSegmentBy(_objectId, dx, dy);
#else
#endif
            }
        }

        public virtual void MoveByDelta(double dx, double dy)
        {
            ClearStaticConstructNodes();

            _origin.X += dx;
            _origin.Y += dy;

            _bbox.X += dx;
            _bbox.Y += dy;

#if UWP
            Globals.DrawingCanvas.VectorListControl.MoveSegmentBy(_objectId, dx, dy);
#else
#endif
        }

        public virtual void MoveHandlePoint(int handleId, Point p)
        {
        }

        public virtual CPoint GetHandlePoint(int handleId)
        {
            return new CPoint();
        }

        public virtual void MoveHandleByDelta(Handles handles, int id, double dx, double dy)
        {
            _moveHandle(handles, id, dx, dy);
            Draw();
            _drawHandles(handles);
        }

        protected virtual void _moveHandle(Handles handles, int id, double dx, double dy)
        {
           MoveByDelta(dx, dy);
        }

        protected void initLimits()
        {
            _ll = new Point(double.MaxValue, double.MaxValue);
            _ur = new Point(double.MinValue, double.MinValue);
        }

        protected void addDeltaToLimits(Point p)
        {
            double x = _origin.X + p.X;
            double y = _origin.Y + p.Y;

            _ll.X = Math.Min(x, _ll.X);
            _ll.Y = Math.Min(y, _ll.Y);
            _ur.X = Math.Max(x, _ur.X);
            _ur.Y = Math.Max(y, _ur.Y);
        }

        protected void addToLimits(Point p)
        {
            _ll.X = Math.Min(p.X, _ll.X);
            _ll.Y = Math.Min(p.Y, _ll.Y);
            _ur.X = Math.Max(p.X, _ur.X);
            _ur.Y = Math.Max(p.Y, _ur.Y);
        }

        protected Rect _limits
        {
            get
            {
                return new Rect(_ll, _ur);
            }
        }

        protected void SetVEAttributes(VectorEntity ve)
        {
            if (Globals.LayerTable.ContainsKey(_layerId) == false)
            {
                Analytics.ReportError("Invalid layer key", null, 2, 404);
                _layerId = 0;
            }

            Layer layer = Globals.LayerTable[_layerId];
            int lineWeight = _lineWeightId < 0 ? layer.LineWeightId : _lineWeightId;
            int lineType = _lineTypeId < 0 ? layer.LineTypeId : _lineTypeId;

            if (Globals.LineTypeTable.ContainsKey(lineType) == false)
            {
                Analytics.ReportError("Invalid linetype key", null, 2, 405);
                lineType = _lineTypeId = 0;
            }

            ve.Color = _colorSpec == (uint)ColorCode.ByLayer ? Utilities.ColorFromColorSpec(layer.ColorSpec) : Utilities.ColorFromColorSpec(_colorSpec);
            ve.DashList = Globals.LineTypeTable[lineType].StrokeDashArray;
            ve.LineWidth = lineWeight == 0 ? .01 : (double)lineWeight / 1000;
            ve.IsVisible = _isVisible && layer.Visible;
            ve.LineType = lineType;
        }

        public virtual VectorEntity Vectorize(VectorContext context)
        {
#if true
            VectorEntity ve = new VectorEntity(_objectId, _zIndex);
            SetVEAttributes(ve);
#else
            if (Globals.LayerTable.ContainsKey(_layerId) == false)
            {
                Analytics.ReportError("Invalid layer key", null, 2, 406);

                _layerId = 0;
            }

            Layer layer = Globals.LayerTable[_layerId];
            int lineWeight = _lineWeightId < 0 ? layer.LineWeightId : _lineWeightId;
            int lineType = _lineTypeId < 0 ? layer.LineTypeId : _lineTypeId;

            if (Globals.LineTypeTable.ContainsKey(lineType) == false)
            {
                Analytics.ReportError("Invalid linetype key", null, 2, 407);

                lineType = _lineTypeId = 0;
            }

            VectorEntity ve = new VectorEntity(_objectId, _zIndex);

            ve.Color = _colorSpec == (uint)ColorCode.ByLayer ? Utilities.ColorFromColorSpec(layer.ColorSpec) : Utilities.ColorFromColorSpec(_colorSpec);
            ve.DashList = Globals.LineTypeTable[lineType].StrokeDashArray;
            ve.LineWidth = lineWeight == 0 ? .01 : (double)lineWeight / 1000;
            ve.IsVisible = _isVisible && layer.Visible;
            ve.LineType = lineType;
#endif
            ve.Opacity = _opacity;

            return ve;
        }

        protected void FillVE(VectorEntity ve)
        {
            if (_fill != (uint)ColorCode.NoFill)
            {
                if (string.IsNullOrEmpty(_fillPattern) || _fillPattern == "Solid")
                {
                    if (ve.FillColor == ve.Color)
                    {
                        ve.LineWidth = 0;
                    }
                }
                else
                { 
                    if (IsDynamic)
                    {
                        Color fill = ve.FillColor;
                        fill.A = (byte)(fill.A / 2);
                        ve.Fill = true;
                    }
                    else
                    {
                        ve.Fill = false;

                        CrosshatchPattern pattern = null;

                        if (Patterns.PatternDictionary.ContainsKey(_fillPattern.ToLower()))
                        {
                            pattern = Patterns.PatternDictionary[_fillPattern.ToLower()];
                        }

                        if (pattern != null && pattern.Items.Count > 0)
                        {
                            List<List<Point>> hatches = PrimitiveUtilities.Crosshatch(ve, pattern, _fillScale, _fillAngle, _fillEvenOdd == false);

                            if (hatches.Count > 0)
                            {
                                VectorEntity hve = new VectorEntity(ve.SegmentId + 110000, ve.ZIndex);
                                hve.Color = ve.FillColor;
                                hve.LineWidth = ve.LineWidth;
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
        }

        public int CompareTo(object obj)
        {
            return this.ZIndex < ((Primitive)obj).ZIndex ? -1 : 1;
        }
    }

    public enum PrimitiveType
    {
        Group = 0,  // 0x00000001
        Instance,   // 0x00000002
        Line,       // 0x00000004
        Polygon,    // 0x00000008
        Doubleline, // 0x00000010
        Bezier,     // 0x00000020
        BSpline,    // 0x00000040
        Rectangle,  // 0x00000080
        Arc,        // 0x00000100
        Arc3,       // 0x00000200
        Ellipse,    // 0x00000400
        Text,       // 0x00000800
        Arrow,      // 0x00001000
        Dimension,  // 0x00002000
        Image       // 0x00004000
    }
}
