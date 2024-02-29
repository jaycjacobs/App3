#define VARIABLE_FILLET

using Cirros.Drawing;
using Cirros.Display;
using Cirros.Utility;
using System;
using System.Collections.Generic;
#if UWP
using Cirros.Actions;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
#else
using System.Windows;
using System.Windows.Media;
using CirrosCore;
#endif

namespace Cirros.Primitives
{
    public class CPoint
    {
        public double X;
        public double Y;
        public uint M;

        public CPoint()
        {
        }

        public CPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public CPoint(Point p, uint m)
        {
            X = p.X;
            Y = p.Y;
            M = m;
        }

        public CPoint(double x, double y, uint m)
        {
            X = x;
            Y = y;
            M = m;
        }

        public Point Point
        {
            get
            {
                return new Point(X, Y);
            }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public override bool Equals(object obj)
        {
            // if this method is not overridden when overriding the == operator, an compiler warning is issued
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            // if this method is not overridden when overriding the == operator, an compiler warning is issued
            return base.GetHashCode();
        }

        public static bool operator ==(CPoint a, CPoint b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.X == b.X && a.Y == b.Y && a.M == b.M;
        }

        public static bool operator !=(CPoint a, CPoint b)
        {
            return !(a == b);
        }
    }

    public class PLine : Primitive
    {
        protected List<Point> _points = new List<Point>();
        protected List<uint> _rf = new List<uint>();
        protected double _radius = 0;

        protected virtual uint _drawMode
        {
            get
            {
#if VARIABLE_FILLET
                if (_radius > 0)
                {
                    return (uint)(_radius * 10000);
                }
#endif
                return 1;
            }
        }

        public PLine(Point s, Point e)
            : base(s)
        {
            Point p = new Point(e.X - s.X, e.Y - s.Y);
            _points.Add(p);
            _rf.Add(_drawMode);

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.LineLineWeightId;
                _colorSpec = Globals.LineColorSpec;
                _lineTypeId = Globals.LineLineTypeId;
            }
        }

        public PLine(CPoint s, CPoint e)
            : base(s.Point)
        {
            Point p = new Point(e.X - s.X, e.Y - s.Y);
            _points.Add(p);
            _rf.Add(e.M);

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.LineLineWeightId;
                _colorSpec = Globals.LineColorSpec;
                _lineTypeId = Globals.LineLineTypeId;
            }
        }

        public PLine(List<CPoint> cpc, double radius = 0) : base(cpc[0].Point)
        {
            for (int i = 1; i < cpc.Count; i++)
            {
                CPoint cp = cpc[i];
                _points.Add(new Point(cp.X - _origin.X, cp.Y - _origin.Y));
                _rf.Add(cp.M);
            }

            _radius = radius;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.LineLineWeightId;
                _colorSpec = Globals.LineColorSpec;
                _lineTypeId = Globals.LineLineTypeId;
            }
        }

        public PLine(Point origin)
            : base(origin)
        {
            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.LineLineWeightId;
                _colorSpec = Globals.LineColorSpec;
                _lineTypeId = Globals.LineLineTypeId;
            }
        }

        protected PLine(PLine original)
            : base(original)
        {
            foreach (Point p in original._points)
            {
                _points.Add(new Point(p.X, p.Y));
            }
            foreach (uint m in original._rf)
            {
                _rf.Add(m);
            }

            _radius = original._radius;
        }

        public PLine(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            _lineTypeId = e.LineTypeId;
            _lineWeightId = e.LineWeightId;
            _radius = _container.ModelToPaper((double)e.Radius);

            initializeFromEntity(e);
        }

        public PLine(Point s, List<Point> points)
            : base(s)
        {
            _points = points;

            for (int i = 0; i < _points.Count; i++)
            {
                _rf.Add(_drawMode);
            }
        }

        public PLine(List<Point> pc)
            : base(pc[0])
        {
            // constructor sets first point

            for (int i = 1; i < pc.Count; i++)
            {
                _points.Add(new Point(pc[i].X - pc[0].X, pc[i].Y - pc[0].Y));
                _rf.Add(_drawMode);
            }
        }

        protected virtual void initializeFromEntity(Entity e)
        {
            foreach (FPoint fp in e.Points)
            {
                Point p = _container.ModelToPaperDelta(fp);
                _points.Add(p);
                _rf.Add(fp.M);
            }
        }

        public override Entity Serialize()
        {
            Entity e = base.Serialize();

            e.LineTypeId = LineTypeId;
            e.LineWeightId = LineWeightId;

            for (int i = 0; i < _points.Count; i++ )
            {
                FPoint fp = _container.PaperToModelDeltaF(_points[i]);
#if VARIABLE_FILLET
                if (i < _rf.Count && (_radius > 0 || _rf[i] != _drawMode))
                {
                    fp.M = _rf[i];
                }
#else
                if (i < _rf.Count && _rf[i] != _drawMode)
                {
                    fp.M = _rf[i];
                }
#endif
                e.AddPoint(fp);
            }

            if (_radius > 0)
            {
                e.Radius = PrimitiveUtilities.SerializeDoubleAsFloat(_container.PaperToModel(_radius));
            }

            return e;
        }

        public double Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
            }
        }

        public void AddPoint(double x, double y, bool draw)
        {
            _points.Add(new Point(x - _origin.X, y - _origin.Y));
            _rf.Add(_drawMode);

            if (draw)
            {
                Draw();
            }
        }

        public void AddPoint(CPoint cp, bool draw)
        {
            _points.Add(new Point(cp.X - _origin.X, cp.Y - _origin.Y));
            _rf.Add(cp.M);

            if (draw)
            {
                Draw();
            }
        }

        public void ReplaceEndPoint(double x, double y, bool draw)
        {
            if (_points.Count > 0)
            {
                _points[_points.Count - 1] = new Point(x - _origin.X, y - _origin.Y);
            }

            if (draw)
            {
                Draw();
            }
        }

        public virtual List<Point> GetTangent(Point p)
        {
            List<Point> pc = null;

            if (_ve != null && (_points == null || _points.Count == 0 || this is PDoubleline))
            {
#if UWP
                double d = Globals.DrawingCanvas.DisplayToPaper(Globals.hitTolerance / 2);
#else
                double d = Globals.View.DisplayToPaper(Globals.hitTolerance / 2);
#endif
                Point p1 = _origin;
                Point p0 = new Point(p.X, p.Y);

                foreach (object o in _ve.Children)
                {
                    List<Point> vpc = null;

                    if (o is List<Point>)
                    {
                        vpc = o as List<Point>;
                    }
                    else if (o is VectorArcEntity)
                    {
                        VectorArcEntity va = o as VectorArcEntity;
                        vpc = CGeometry.ArcPointCollection(va.Center, va.Radius, va.StartAngle, va.IncludedAngle, false, Matrix.Identity);
                    }

                    if (vpc != null)
                    {
                        for (int i = 0; i < vpc.Count; i++)
                        {
                            Point p2 = vpc[i];

                            Point n;
                            double d1 = Construct.DistancePointToLine(p, p1, p2, out n);

                            if (d1 < d)
                            {
                                // Is the intercept is actually on the segment?
                                double pv = Construct.PointValue(p1, p2, n);

                                if (pv >= 0 & pv <= 1)
                                {
                                    d = d1;
                                    pc = new List<Point>();
                                    pc.Add(p1);
                                    pc.Add(p2);
                                }
                            }

                            p1 = p2;
                        }
                    }
                }
            }
            else
            {
                Point p0 = p;
                double pv;
                int s = this.PickSegment(ref p0, out pv);
                if (s >= 0)
                {
                    pc = this.GetSegment(s);
                }
            }

            return pc;
        }

        public List<Point> GetSegment(int segment)
        {
            List<Point> spoints = new List<Point>();

            if (segment == 0)
            {
                spoints.Add(_origin);
                spoints.Add(new Point(_points[0].X + _origin.X, _points[0].Y + _origin.Y));
            }
            else if (segment < _points.Count)
            {
                spoints.Add(new Point(_points[segment - 1].X + _origin.X, _points[segment - 1].Y + _origin.Y));
                spoints.Add(new Point(_points[segment].X + _origin.X, _points[segment].Y + _origin.Y));
            }
            else if (segment == _points.Count)
            {
                // if the segment index is equal to the point count (one more than the segment count) reverse the last segment
                if (segment == 1)
                {
                    spoints.Add(new Point(_points[0].X + _origin.X, _points[0].Y + _origin.Y));
                    spoints.Add(_origin);
                }
                else
                {
                    spoints.Add(new Point(_points[segment - 1].X + _origin.X, _points[segment - 1].Y + _origin.Y));
                    spoints.Add(new Point(_points[segment - 2].X + _origin.X, _points[segment - 2].Y + _origin.Y));
                }
            }

            return spoints;
        }

        public virtual void InsertHandlePoint(int beforeHandleId, CPoint cp)
        {
            ClearStaticConstructNodes();

            if (beforeHandleId == 1)
            {
                double dx = cp.X - _origin.X;
                double dy = cp.Y - _origin.Y;

                _origin = cp.Point;

                _points.Insert(0, new Point(0, 0));
                _rf.Insert(0, _drawMode);
                
                for (int i = 0; i < _points.Count; i++)
                {
                    _points[i] = new Point(_points[i].X - dx, _points[i].Y - dy); ;
                }
            }
            else
            {
                int index = beforeHandleId - 2;

                Point p1 = new Point(cp.X - _origin.X, cp.Y - _origin.Y);

                if (index >= _points.Count)
                {
                    _points.Add(p1);
                    _rf.Add(cp.M);
                }
                else if (index >= 0)
                {
                    _points.Insert(index, p1);
                    _rf.Insert(index, cp.M);
                }
            }
        
            Draw();
        }

        public void RemoveColinearPoints()
        {
            if (_points.Count > 3)
            {
                List<Point> pc = new List<Point>();
                Point v0 = _points[0];
                Point v1 = _points[1];

                pc.Add(v0);
                pc.Add(v1);

                for (int i = 2; i < _points.Count; i++)
                {
                    Point v = _points[i];

                    if (v.X == v1.X && v.Y == v1.Y)
                    {
                        continue;
                    }
                    else if (Construct.DistancePointToLine(v0, v1, v) > .01)
                    {
                        pc.Add(v);
                    }
                    else
                    {
                        pc[pc.Count - 1] = v;
                    }

                    v0 = v1;
                    v1 = v;
                }

                if (pc.Count < _points.Count)
                {
                    _points = pc;
                    this.ClearStaticConstructNodes();
                }
            }
        }

        public virtual void MoveParallel(double distance)
        {
            List<Point> pc = new List<Point>();

            pc.Add(_origin);

            foreach (Point p in _points)
            {
                pc.Add(new Point(p.X + _origin.X, p.Y + _origin.Y));
            }

            if (this is PPolygon)
            {
                if (pc[0].X != pc[pc.Count - 1].X || pc[0].Y != pc[pc.Count - 1].Y)
                {
                    pc.Add(pc[0]);
                }
            }

            Construct.MoveLineParallel(pc, distance);

            _origin = pc[0];
            int ids = (int)(distance * 10000);

            Point a = new Point();
            Point b = a;
            Point c = _points[0];

            for (int i = 0; i < _points.Count; i++)
            {
                if (i < (_points.Count - 1))
                {
                    a = b;
                    b = c;
                    c = _points[i + 1];

                    if (_radius > 0)
                    {
                        int side = Construct.WhichSide(a, b, c);
                        if (side > 0)
                        {
                            _rf[i + 1] = (uint)Math.Max((int)_rf[i + 1] - ids, 2);
                        }
                        else if (side < 0)
                        {
                            _rf[i + 1] = (uint)Math.Max((int)_rf[i + 1] + ids, 2);
                        }
                    }
                }

                Point op = pc[i + 1];
                _points[i] = new Point(op.X - _origin.X, op.Y - _origin.Y);
            }

            Draw();
        }

        public override void MoveHandlePoint(int handleId, Point p)
        {
            ClearStaticConstructNodes();

            if (handleId == 1)
            {
                double dx = p.X - _origin.X;
                double dy = p.Y - _origin.Y;

                _origin = p;

                for (int i = 0; i < _points.Count; i++)
                {
                    _points[i] = new Point(_points[i].X - dx, _points[i].Y - dy); ;
                }
            }
            else
            {
                int index = handleId - 2;
                if (index >= 0 && _points.Count > index)
                {
                    _points[index] = new Point(p.X - _origin.X, p.Y - _origin.Y);
                }
            }
            Draw();
        }

        public CPoint GetVertex(int index)
        {
            CPoint cp = null;

            if (index == 0)
            {
                cp = new CPoint(_origin, 0);
            }
            else if (index > 0)
            {
                Point p = _points[index - 1];

                cp = new CPoint(p.X + _origin.X, p.Y + _origin.Y, _rf[index - 1]);
            }

            return cp;
        }

        public override CPoint GetHandlePoint(int handleId)
        {
            CPoint cp = null;

            if (handleId == 1)
            {
                cp = new CPoint(_origin, 0);
            }
            else if (handleId > 1)
            {
                Point p = _points[handleId - 2];

                cp = new CPoint(p.X + _origin.X, p.Y + _origin.Y, _rf[handleId - 2]);
            }

            return cp;
        }

        public virtual int RemoveHandlePoint(int handleId)
        {
            ClearStaticConstructNodes();

            if (_points.Count < 2)
            {
                // Lines must have at least 2 points (_points.Count >= 1)
            }
            else if (handleId == 1)
            {
                double dx = -_points[0].X;
                double dy = -_points[0].Y;

                _origin.X = _points[0].X + _origin.X;
                _origin.Y = _points[0].Y + _origin.Y;

                for (int i = 1; i < _points.Count; i++)
                {
                    _points[i-1] = new Point(_points[i].X + dx, _points[i].Y + dy);
                }

                _points.RemoveAt(_points.Count - 1);
                // TODO: Verify that removing the first _rf node works correctly in this case
                _rf.RemoveAt(0);
            }
            else
            {
                int index = handleId - 2;

                if (index >= 0 && _points.Count > index)
                {
                    _points.RemoveAt(index);
                    _rf.RemoveAt(index);
                }
            }

            Draw();

            return _points.Count;
        }

        public virtual int RemovePoint(bool draw = true)
        {
            if (_points.Count > 0)
            {
                _points.RemoveAt(_points.Count - 1);
                _rf.RemoveAt(_rf.Count - 1);

                if (draw)
                {
                    Draw();
                }
            }

            return _points.Count;
        }

        public void Gap(int startSegment, CPoint startPoint, int endSegment, CPoint endPoint)
        {
            if (endSegment > startSegment)
            {
                for (int i = startSegment; i < endSegment; i++)
                {
                    _rf[i] = endPoint.M;
                }
            }

            // Check this on doublelines with variable draw modes
            startPoint.M = _drawMode;
            int index = endSegment;
            if (index >= 0 && index < _rf.Count)
            {
                startPoint.M = _rf[index];
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine("Line.Gap() - invalid index");
            }

            InsertHandlePoint(endSegment + 2, endPoint);
            InsertHandlePoint(startSegment + 2, startPoint);

            Draw();
        }

        public virtual Point EndPoint
        {
            get
            {
                Point p = _origin;

                if (_points.Count > 0)
                {
                    p.X += _points[_points.Count - 1].X;
                    p.Y += _points[_points.Count - 1].Y;
                }
                return p;
            }
        }

        public virtual List<Point> Points
        {
            get
            {
                return _points;
            }
            set
            {
                _points = value;
            }
        }

        public void InitWithPointCollection(List<Point> pc)
        {
            _points.Clear();

            _origin = pc[0];

            for (int i = 1; i < pc.Count; i++)
            {
                _points.Add(new Point(pc[i].X - pc[0].X, pc[i].Y - pc[0].Y));
                _rf.Add(_drawMode);
            }

            Draw();
        }

        public virtual List<CPoint> CPoints
        {
            get
            {
                List<CPoint> _cPoints = new List<CPoint>();

                for (int i = 0; i < _points.Count; i++)
                {
                    _cPoints.Add(new CPoint(_points[i], _rf[i]));
                }

                return _cPoints;
            }
            set
            {
                _points.Clear();
                _rf.Clear();

                foreach (CPoint cp in value)
                {
                    _points.Add(new Point(cp.X, cp.Y));
                    _rf.Add(cp.M);
                }
                ClearStaticConstructNodes();
            }
        }

        public void Reverse()
        {
            List<CPoint> cps = new List<CPoint>();

            for (int i = _points.Count - 2; i >= 0; --i)
            {
                Point p = _points[i];
                p.X += _origin.X;
                p.Y += _origin.Y;
                uint m = _rf[i + 1];
                cps.Add(new CPoint(p, m));
            }

            cps.Add(new CPoint(_origin, _rf[0]));

            _origin = EndPoint;

            for (int i = 0; i < cps.Count; i++)
            {
                cps[i].X -= _origin.X;
                cps[i].Y -= _origin.Y;
            }

            CPoints = cps;

            Draw();
        }

        public override Primitive Clone()
        {
            return new PLine(this);
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Line;
            }
        }

        public override int ActiveLayer
        {
            get
            {
                if (Globals.UIVersion == 0)
                {
                    return Globals.LayerId;
                }
                else
                {
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveLineLayerId))
                    {
                        return Globals.ActiveLineLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        private Point _dynamicFromAnchor = new Point();
        private Point _dynamicThroughAnchor = new Point();

        public override List<ConstructNode> DynamicConstructNodes(Point from, Point through)
        {
            if (_constructEnabled)
            {
                if (from != _dynamicFromAnchor || through != _dynamicThroughAnchor)
                {
                    _dynamicFromAnchor = from;
                    _dynamicThroughAnchor = through;

                    _dynamicConstructNodes.Clear();

                    if (_ve == null)
                    {
                        Vectorize(new VectorContext(false, false, false));
                    }

                    if (_ve.Children != null)
                    {
                        foreach (object o in _ve.Children)
                        {
                            if (o is List<Point>)
                            {
                                List<Point> pc = o as List<Point>;
                                Point v0 = pc[0];

                                for (int i = 1; i < pc.Count; i++)
                                {
                                    Point v1 = pc[i];

                                    if (from == v0 || from == v1)
                                    {
                                        continue;
                                    }

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
                }
            }

            return _dynamicConstructNodes;
        }

        public override List<ConstructNode> ConstructNodes
        {
            get
            {
                if (_constructEnabled && _staticConstructNodes.Count == 0)
                {
                    if (_ve == null)
                    {
                        Vectorize(new VectorContext(false, false, false));
                    }

                    if (_ve.Children != null)
                    {
                        foreach (object o in _ve.Children)
                        {
                            if (o is List<Point>)
                            {
                                List<Point> pc = o as List<Point>;

                                Point v0 = pc[0];

                                _staticConstructNodes.Add(new ConstructNode(v0, "origin"));

                                for (int i = 1; i < pc.Count; i++)
                                {
                                    Point v1 = pc[i];

                                    _staticConstructNodes.Add(new ConstructNode(new Point((v0.X + v1.X) / 2, (v0.Y + v1.Y) / 2), "midpoint"));
                                    _staticConstructNodes.Add(new ConstructNode(v1, "vertex"));

                                    v0 = v1;
                                }
                            }
                        }
                    }
                }

                return _constructEnabled ? _staticConstructNodes : new List<ConstructNode>();
            }
        }

        public override void MoveTo(double x, double y)
        {
            _ve = null;

            base.MoveTo(x, y);
        }

        public override void MoveByDelta(double dx, double dy)
        {
            ClearStaticConstructNodes();

            _origin.X += dx;
            _origin.Y += dy;

            foreach (ConstructNode node in _staticConstructNodes)
            {
                node.Move(dx, dy);
            }

            _ve = null;
        }

        public override bool Normalize(bool undoable)
        {
            bool undid = false;

            if (_matrix.IsIdentity == false)
            {
#if UWP
                if (undoable)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.UnNormalize, this, _matrix);
                    undid = true;
                }
#else
#endif

                for (int i = 0; i < _points.Count; i++)
                {
                    _points[i] = _matrix.Transform(_points[i]);
                }

                _matrix = CGeometry.IdentityMatrix();
            }

            return undid;
        }

        public override bool UnNormalize(Matrix m)
        {
            if (m.IsIdentity == false)
            {
                Matrix inverse = CGeometry.InvertMatrix(m);

                for (int i = 0; i < _points.Count; i++)
                {
                    _points[i] = inverse.Transform(_points[i]);
                }

                _matrix = m;
            }
            return true;
        }

        protected override void _drawHandles(Handles handles)
        {
            handles.Attach(this);
            handles.AddHandle(1, _origin.X, _origin.Y);

            int index = 2;

            foreach (Point pt in _points)
            {
                if (_matrix.IsIdentity == false)
                {
                    Point p = _matrix.Transform(pt);
                    handles.AddHandle(index++, _origin.X + p.X, _origin.Y + p.Y);
                }
                else
                {
                    handles.AddHandle(index++, _origin.X + pt.X, _origin.Y + pt.Y);
                }
            }

            handles.Draw();
        }

        protected override void _moveHandle(Handles handles, int id, double dx, double dy)
        {
            ClearStaticConstructNodes();

            if (id == 1)
            {
                _origin.X += dx;
                _origin.Y += dy;

                for (int i = 0; i < _points.Count; i++)
                {
                    Point p = new Point(_points[i].X - dx, _points[i].Y - dy);
                    _points[i] = p;
                }
            }
            else 
            {
                int i = id - 2;
                Point p = new Point(_points[i].X + dx, _points[i].Y + dy);
                _points[i] = p;
            }
        }

        public int PickSegment(ref Point p, out double pv)
        {
            int segment = -1;
#if UWP
            double d = Globals.DrawingCanvas.DisplayToPaper(Globals.hitTolerance / 2);
#else
            double d = Globals.View.DisplayToPaper(Globals.hitTolerance / 2);
#endif
            Point p1 = _origin;
            Point p0 = new Point(p.X, p.Y);
            double pv0 = 0;
            double pv1 = 0;

            if (this is PDoubleline pd)
            {
                d = Math.Max(d, Globals.ActiveDrawing.ModelToPaper(pd.Width) + d / 2);
            }

            if (this.TypeName == PrimitiveType.Polygon)
            {
                int i = this.Points.Count - 1;
                Point p2 = new Point(_points[i].X + _origin.X, _points[i].Y + _origin.Y);

                Point n;
                double d1 = Construct.DistancePointToLine(p, p1, p2, out n);

                if (d1 < d)
                {
                    // Is the intercept is actually on the segment?
                    pv1 = Construct.PointValue(p1, p2, n);

                    if (pv1 >= 0 & pv1 <= 1)
                    {
                        d = d1;
                        segment = this.Points.Count;
                        p0.X = n.X;
                        p0.Y = n.Y;
                        pv0 = pv1;
                    }
                }
            }

            for (int i = 0; i < _points.Count; i++)
            {
                Point p2 = new Point(_points[i].X + _origin.X, _points[i].Y + _origin.Y);

                if (_rf[i] != 0)
                {
                    Point n;
                    double d1 = Construct.DistancePointToLine(p, p1, p2, out n);

                    if (d1 < d)
                    {
                        // Is the intercept is actually on the segment?
                        pv1 = Construct.PointValue(p1, p2, n);

                        if (pv1 >= 0 & pv1 <= 1)
                        {
                            d = d1;
                            segment = i;
                            p0.X = n.X;
                            p0.Y = n.Y;
                            pv0 = pv1;
                        }
                    }
                }

                p1 = p2;
            }

            if (segment >= 0)
            {
                p.X = p0.X;
                p.Y = p0.Y;
                pv = pv0;
            }
            else
            {
                pv = -1;
            }

            return segment;
        }

        public virtual void GetSegment(int segment, out Point start, out Point end)
        {
            if (segment == 0)
            {
                start = _origin;
                end = new Point(_points[0].X + _origin.X, _points[0].Y + _origin.Y);
            }
            else if (segment < _points.Count)
            {
                start = new Point(_points[segment - 1].X + _origin.X, _points[segment - 1].Y + _origin.Y);
                end = new Point(_points[segment].X + _origin.X, _points[segment].Y + _origin.Y);
            }
            else if (segment == _points.Count && this.TypeName == PrimitiveType.Polygon)
            {
                start = new Point(_points[segment - 1].X + _origin.X, _points[segment - 1].Y + _origin.Y);
                end = _origin;
            }
            else
            {
                start = new Point();
                end = new Point();
            }
        }

        public virtual void MoveSegmentBy(int segment, double dx, double dy)
        {
            if (segment == 0)
            {
                _origin.X += dx;
                _origin.Y += dy;

                for (int i = 1; i < _points.Count; i++)
                {
                    Point s = _points[i];
                    s.X -= dx;
                    s.Y -= dy;
                    _points[i] = s;
                }
            }
            else if (segment < _points.Count)
            {
                Point s = _points[segment - 1];
                s.X += dx;
                s.Y += dy;
                _points[segment - 1] = s;

                Point e = _points[segment];
                e.X += dx;
                e.Y += dy;
                _points[segment] = e;
            }
            else if (segment == _points.Count && this.TypeName == PrimitiveType.Polygon)
            {
                _origin.X += dx;
                _origin.Y += dy;

                for (int i = 0; i < _points.Count - 1; i++)
                {
                    Point s = _points[i];
                    s.X -= dx;
                    s.Y -= dy;
                    _points[i] = s;
                }
            }
        }

        public override VectorEntity Vectorize(VectorContext context)
        {
            return Vectorize(context, false);
        }

        public virtual VectorEntity Vectorize(VectorContext context, bool ignoreGaps)
        {
            int index = 0;

            Point lastPoint = _origin;

            _ve = base.Vectorize(context);

            while (index < _points.Count)
            {
                List<Point> pc = new List<Point>();

                while (_rf[index] == 0)
                {
                    Point pt = _matrix.Transform(_points[index++]);
                    lastPoint = new Point(_origin.X + pt.X, _origin.Y + pt.Y);

                    if (index >= _points.Count)
                    {
                        break;
                    }
                }

                pc.Add(lastPoint);

                for (; index < _points.Count; index++)
                {
                    Point pt = _matrix.Transform(_points[index]);
                    Point p = new Point(_origin.X + pt.X, _origin.Y + pt.Y);

                    if (p.X != lastPoint.X || p.Y != lastPoint.Y)
                    {
                        lastPoint = p;

                        if (ignoreGaps == false && _rf[index] == 0)
                        {
                            break;
                        }

                        pc.Add(p);
                    }
                }

                if (this is PPolygon)
                {
                    // close the shape
                    pc.Add(pc[0]);
                    _ve.SetFillFromPrimitive(this);
                    _ve.FillEvenOdd = _fillEvenOdd;
                }

                if (pc.Count < 2)
                {

                }
                else
                {
                    _ve.AddChild(pc);
                }
            }

            if (_radius > 0 && _points.Count > 1)
            {
                // if this is a fillet re-render the VectorEntity with the arcs
                // leave _ve as is - it will be used as the construction point list

                double radius = _radius;

                VectorEntity ve = base.Vectorize(context);

                List<CPoint> sourcePoints = this.CPoints;
                sourcePoints.Insert(0, new CPoint(0, 0, _drawMode));

                if (this is PPolygon)
                {
                    int i1 = sourcePoints.Count - 1;

                    // if this is a polygon, close it
                    ve.SetFillFromPrimitive(this);

                    CPoint mp = new CPoint((sourcePoints[i1].X + sourcePoints[0].X) / 2, (sourcePoints[i1].Y + sourcePoints[0].Y) / 2, _drawMode);
                    sourcePoints.Add(mp);
                    sourcePoints.Insert(0, mp);
                }

                index = 0;
                Point po = _matrix.Transform(sourcePoints[index++].Point);
                lastPoint = new Point(_origin.X + po.X, _origin.Y + po.Y);

                while (index < sourcePoints.Count)
                {
                    while (sourcePoints[index].M == 0)
                    {
                        Point pt = _matrix.Transform(sourcePoints[index++].Point);
                        lastPoint = new Point(_origin.X + pt.X, _origin.Y + pt.Y);

                        if (index >= sourcePoints.Count)
                        {
                            break;
                        }
                    }

                    Point p0 = lastPoint;
                    Point p1 = lastPoint;

                    double d0 = 0;
                    double d1 = 0;

                    List<Point> pc = new List<Point>();
                    for (; index < sourcePoints.Count; index++)
                    {
                        Point pt = _matrix.Transform(sourcePoints[index].Point);
                        Point p = new Point(_origin.X + pt.X, _origin.Y + pt.Y);

                        if (p.X != lastPoint.X || p.Y != lastPoint.Y)
                        {
                            if (ignoreGaps == false && sourcePoints[index].M == 0)
                            {
                                if (this is PPolygon)
                                {
#if DEBUG
                                    //System.Diagnostics.Debugger.Break();
#endif
                                }
                                break;
                            }

                            d0 = d1;
                            d1 = Construct.Distance(lastPoint, p);
                            double dm = Math.Min(d0, d1);

                            if (dm > 0)
                            {
                                Point center;
                                double startAngle, includedAngle;
#if VARIABLE_FILLET
                                radius = sourcePoints[index].M > 3 ? (double) sourcePoints[index].M / 10000 : _radius;
#endif
                                Construct.FilletPoints(p0, lastPoint, p, radius, out center, out startAngle, out includedAngle);

                                if (includedAngle == 0)
                                {
                                    // if includedAngle is 0 the points are co-linear

                                    pc.Add(p0);
                                    pc.Add(p1);
                                    //ve.AddChild(pc);
                                }
                                else
                                {
                                    Point ps = Construct.PolarOffset(center, radius, startAngle);
                                    Point pe = Construct.PolarOffset(center, radius, startAngle + includedAngle);
                                    double pvs = Construct.PointValue(p0, lastPoint, ps);
                                    double pve = Construct.PointValue(lastPoint, p, pe);

                                    if (pvs < 0 || pvs > 1 || pve < 0 || pve > 1)
                                    {
                                        // the fillet doesn't fit on the line segment
                                        if (index == 2)
                                        {
                                            pc.Add(p0);
                                        }

                                        pc.Add(lastPoint);
                                    }
                                    else
                                    {
                                        p1 = Construct.PolarOffset(center, radius, startAngle);

                                        if (p0.X != p1.X || p0.Y != p1.Y)
                                        {
                                            pc.Add(p0);
                                            pc.Add(p1);
                                            //ve.AddChild(pc);
                                        }

                                        List<Point> arcPoints = CGeometry.ArcPointCollection(center, radius, startAngle, includedAngle, false, Matrix.Identity);
                                        foreach (Point ap in arcPoints)
                                        {
                                            pc.Add(ap);
                                        }

                                        p1 = Construct.PolarOffset(center, radius, startAngle + includedAngle);
                                    }
                                }
                            }
                            else if (d0 > 0)
                            {
                                // draw line
                                pc.Add(p0);
                                pc.Add(p1);
                                //ve.AddChild(pc);
                            }

                            p0 = p1;
                            p1 = lastPoint = p;
                        }
                    }

                    if (d1 > 0)
                    {
                        // draw last line
                        pc.Add(p0);
                        pc.Add(p1);
                    }
                   
                    if (pc != null && pc.Count > 0)
                    {
                        ve.AddChild(pc);
                    }
                }

                // return the local VectorEntity - _ve contains the construction points
                return ve;
            }

            return _ve;
        }
    }
}
