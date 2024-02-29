using Cirros.Core;
using Cirros.Display;
using Cirros.Drawing;
using Cirros.Utility;
using System;
using System.Collections.Generic;
#if UWP
using Windows.Foundation;
#else
using System.Windows;
#endif

namespace Cirros.Primitives
{
    public class PArc3 : PPolygon
    {
        protected bool _isCircle = false;

        public PArc3(Point s, Point m, Point e, bool isCircle)
            : base(s)
        {
            _points.Add(new Point(m.X - s.X, m.Y - s.Y));
            _points.Add(new Point(e.X - s.X, e.Y - s.Y));

            _rf.Add(_drawMode);
            _rf.Add(_drawMode);

            _isCircle = isCircle;
            _layerId = ActiveLayer;

            if (_isCircle)
            {
                _fill = Globals.CircleFill;
                _fillPattern = Globals.CirclePattern;
                _fillScale = Globals.CirclePatternScale;
                _fillAngle = Globals.CirclePatternAngle;
            }
            else
            {
                _fill = Globals.ArcFill;
                _fillPattern = Globals.ArcPattern;
                _fillScale = Globals.ArcPatternScale;
                _fillAngle = Globals.ArcPatternAngle;
            }

            if (Globals.UIVersion > 0)
            {
                if (_isCircle)
                {
                    _lineWeightId = Globals.CircleLineWeightId;
                    _colorSpec = Globals.CircleColorSpec;
                    _lineTypeId = Globals.CircleLineTypeId;
                }
                else
                {
                    _lineWeightId = Globals.ArcLineWeightId;
                    _colorSpec = Globals.ArcColorSpec;
                    _lineTypeId = Globals.ArcLineTypeId;
                }
            }
        }

        private PArc3(PArc3 original)
            : base(original)
        {
            foreach (Point p in original._points)
            {
                _points.Add(new Point(p.X, p.Y));
                _rf.Add(_drawMode);
            }

            _fill = original._fill;
            _isCircle = original._isCircle;
            _fillPattern = original._fillPattern;
            _fillScale = original._fillScale;
            _fillAngle = original._fillAngle;
        }

        public PArc3(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            _lineTypeId = e.LineTypeId;
            _lineWeightId = e.LineWeightId;

            _fill = e.Fill;
            _fillPattern = e.FillPattern;
            _fillScale = e.FillScale;
            _fillAngle = e.FillAngle;
            _isCircle = e.IncludedAngle != 0;

            foreach (FPoint fp in e.Points)
            {
                Point p = _container.ModelToPaperDelta(fp);
                _points.Add(p);
                _rf.Add(_drawMode);
            }
        }

        public override Entity Serialize()
        {
            Entity e = base.Serialize();

            e.LineTypeId = LineTypeId;
            e.LineWeightId = LineWeightId;
            e.Fill = _fill;
            e.FillPattern = _fillPattern;
            e.FillScale = (float)_fillScale;
            e.FillAngle = (float)_fillAngle;

            if (_isCircle)
            {
                e.IncludedAngle = (float)(2 * Math.PI);
            }

            return e;
        }

        protected override void initializeFromEntity(Cirros.Drawing.Entity e)
        {
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Arc3;
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
                else if (_isCircle)
                {
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveCircleLayerId))
                    {
                        return Globals.ActiveCircleLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
                else
                {
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveArcLayerId))
                    {
                        return Globals.ActiveArcLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        public override Primitive Clone()
        {
            return new PArc3(this);
        }

        public bool IsCircle
        {
            get
            {
                return _isCircle;
            }
            set
            {
                _isCircle = value;
            }
        }

        public Point Center
        {
            get
            {
                Point s = _origin;
                Point m = new Point(_origin.X + _points[0].X, _origin.Y + _points[0].Y);
                Point e = new Point(_origin.X + _points[1].X, _origin.Y + _points[1].Y);
                return Construct.Equidistant(s, m, e);
            }
        }

        public Point IntermediatePoint
        {
            get
            {
                return new Point(_origin.X + _points[0].X, _origin.Y + _points[0].Y);
            }
        }

        public override Point EndPoint
        {
            get
            {
                return new Point(_origin.X + _points[1].X, _origin.Y + _points[1].Y);
            }
        }

        public override bool Normalize(bool undoable)
        {
            return false;
        }

        public override List<ConstructNode> DynamicConstructNodes(Point from, Point through)
        {
            List<ConstructNode> dynamicConstructNodes = new List<ConstructNode>();

            if (_constructEnabled && _matrix.IsIdentity)
            {
                Point s = _origin;
                Point m = new Point(_origin.X + _points[0].X, _origin.Y + _points[0].Y);
                Point e = new Point(_origin.X + _points[1].X, _origin.Y + _points[1].Y);
                Point c = Construct.Equidistant(s, m, e);
                double radius = Construct.Distance(c, s);

                Point normal;
                if (Construct.NormalArcToPoint(c, radius, from, out normal))
                {
                    dynamicConstructNodes.Add(new ConstructNode(normal, "normal"));
                }

                Point tangent0, tangent1;
                if (Construct.TangentArcToPoint(c, radius, from, out tangent0, out tangent1))
                {
                    dynamicConstructNodes.Add(new ConstructNode(tangent0, "tangent"));
                    dynamicConstructNodes.Add(new ConstructNode(tangent1, "tangent"));
                }

                if (from != through)
                {
                    Point i0, i1;
                    if (Construct.IntersectLineArc(from, through, c, radius, out i0, out i1))
                    {
                        dynamicConstructNodes.Add(new ConstructNode(i0, "intersect"));
                        if (i0 != i1)
                        {
                            dynamicConstructNodes.Add(new ConstructNode(i1, "intersect"));
                        }
                    }
                }
            }

            return dynamicConstructNodes;
        }

        public override List<ConstructNode> ConstructNodes
        {
            get
            {
                if (_constructEnabled && _staticConstructNodes.Count == 0)
                {

                    if (IsTransformed)
                    {
                        Point s = new Point(0, 0);
                        Point m = new Point(_points[0].X, _points[0].Y);
                        Point e = new Point(_points[1].X, _points[1].Y);
                        Point c = Construct.Equidistant(s, m, e);

                        s = _matrix.Transform(s);
                        m = _matrix.Transform(m);
                        e = _matrix.Transform(e);
                        c = _matrix.Transform(c);

                        s.X += _origin.X;
                        s.Y += _origin.Y;
                        m.X += _origin.X;
                        m.Y += _origin.Y;
                        e.X += _origin.X;
                        e.Y += _origin.Y;
                        c.X += _origin.X;
                        c.Y += _origin.Y;

                        _staticConstructNodes.Add(new ConstructNode(c, "center"));
                        _staticConstructNodes.Add(new ConstructNode(s, "start"));
                        _staticConstructNodes.Add(new ConstructNode(m, "intermediate"));
                        _staticConstructNodes.Add(new ConstructNode(e, "end"));
                    }
                    else
                    {
                        Point s = _origin;
                        Point m = new Point(_origin.X + _points[0].X, _origin.Y + _points[0].Y);
                        Point e = new Point(_origin.X + _points[1].X, _origin.Y + _points[1].Y);
                        Point c = Construct.Equidistant(s, m, e);

                        _staticConstructNodes.Add(new ConstructNode(c, "center"));
                        _staticConstructNodes.Add(new ConstructNode(s, "start"));
                        _staticConstructNodes.Add(new ConstructNode(m, "intermediate"));
                        _staticConstructNodes.Add(new ConstructNode(e, "end"));

                        double radius = Construct.Distance(c, s);

                        _staticConstructNodes.Add(new ConstructNode(new Point(c.X + radius, c.Y), "quadrant"));
                        _staticConstructNodes.Add(new ConstructNode(new Point(c.X, c.Y + radius), "quadrant"));
                        _staticConstructNodes.Add(new ConstructNode(new Point(c.X - radius, c.Y), "quadrant"));
                        _staticConstructNodes.Add(new ConstructNode(new Point(c.X, c.Y - radius), "quadrant"));
                    }
                }

                return _constructEnabled ? _staticConstructNodes : new List<ConstructNode>();
            }
        }

        protected override void _drawHandles(Handles handles)
        {
            handles.Attach(this);
            handles.AddHandle(1, _origin.X, _origin.Y);

            int index = 2;

            foreach (Point pt in _points)
            {
                handles.AddHandle(index++, _origin.X + pt.X, _origin.Y + pt.Y);
            }

            handles.Draw();
        }

        public override CPoint GetHandlePoint(int handleId)
        {
            CPoint handlePoint = new CPoint();

            if (handleId == 1)
            {
                handlePoint.Point = _origin;
            }
            else if (handleId == 2)
            {
                handlePoint.Point = new Point(_origin.X + _points[0].X, _origin.Y + _points[0].Y);
            }
            else if (handleId == 3)
            {
                handlePoint.Point = new Point(_origin.X + _points[1].X, _origin.Y + _points[1].Y);
            }

            return base.GetHandlePoint(handleId);
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
                    Point p0 = new Point(_points[i].X - dx, _points[i].Y - dy);
                    _points[i] = p0;
                }
            }
            else
            {
                _points[handleId - 2] = new Point(p.X - _origin.X, p.Y - _origin.Y);
            }

            Draw();
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

        const double PI2 = Math.PI * 2;

        public override VectorEntity Vectorize(VectorContext context)
        {
#if true
            VectorEntity ve = new VectorEntity(_objectId, _zIndex);
            SetVEAttributes(ve);
#else
            Layer layer = Globals.LayerTable[_layerId];
            int lineWeight = _lineWeightId < 0 ? layer.LineWeightId : _lineWeightId;
            int lineType = _lineTypeId < 0 ? layer.LineTypeId : _lineTypeId;

            VectorEntity ve = new VectorEntity(_objectId, _zIndex);

            ve.Color = _colorSpec == (uint)ColorCode.ByLayer ? Utilities.ColorFromColorSpec(layer.ColorSpec) : Utilities.ColorFromColorSpec(_colorSpec);
            ve.DashList = Globals.LineTypeTable[lineType].StrokeDashArray;
            ve.LineWidth = lineWeight == 0 ? .01 : (double)lineWeight / 1000;
            ve.IsVisible = _isVisible && layer.Visible;
            ve.LineType = lineType;
#endif
            bool wedge = _isCircle == false && _fill != (uint)ColorCode.NoFill;

            if (_matrix.IsIdentity)
            {
                Point s = _origin;
                Point m = new Point(_points[0].X + s.X, _points[0].Y + s.Y);
                Point e = new Point(_points[1].X + s.X, _points[1].Y + s.Y);

                if (context.HasArcPrimitive && wedge == false && _fill == (uint)ColorCode.NoFill)
                {
                    Point c = Construct.Equidistant(s, m, e);

                    double startAngle = Construct.Angle(c, s);
                    double midAngle = Construct.Angle(c, m);
                    double radius = Construct.Distance(c, s);
                    double inclAngle = Construct.IncludedAngle(s, c, e);

                    if (inclAngle < 0)
                        inclAngle += PI2;

                    double a2 = midAngle - startAngle;
                    if (a2 < 0)
                        a2 += PI2;
                    if (a2 > inclAngle)
                        inclAngle -= PI2;

                    VectorArcEntity va = new VectorArcEntity();
                    va.Center = c;
                    va.Radius = radius;
                    va.StartAngle = startAngle;
                    va.IncludedAngle = inclAngle;
                    va.IsCircle = _isCircle;
                    ve.AddChild(va);
                }
                else
                {
                    if (_isCircle)
                    {
                        ve.AddChild(CGeometry.CirclePointCollection(s, m, e, _matrix));
                    }
                    else
                    {
                        ve.AddChild(CGeometry.ArcPointCollection(s, m, e, wedge, _matrix));
                    }
                }
            }
            else
            {
                Point s = new Point(0, 0);
                Point m = new Point(_points[0].X, _points[0].Y);
                Point e = new Point(_points[1].X, _points[1].Y);

                List<Point> p0;

                if (_isCircle)
                {
                    p0 = CGeometry.CirclePointCollection(s, m, e, CGeometry.IdentityMatrix());
                }
                else
                {
                    p0 = CGeometry.ArcPointCollection(s, m, e, wedge, CGeometry.IdentityMatrix());
                }

                List<Point> p1 = new List<Point>();

                foreach (Point p in p0)
                {
                    Point pt = _matrix.Transform(p);
                    p1.Add(new Point(pt.X + _origin.X, pt.Y + _origin.Y));
                }

                ve.AddChild(p1);
            }

            ve.SetFillFromPrimitive(this);

            if (_fill != (uint)ColorCode.NoFill)
            {
                FillVE(ve);
            }

            _ve = ve;

            return ve;
        }
    }
}
