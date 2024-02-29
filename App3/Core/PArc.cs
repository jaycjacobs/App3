using Cirros.Core;
using Cirros.Drawing;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using Cirros.Display;
#if UWP
using Windows.Foundation;
#else
using System.Windows;
using CirrosCore;
#endif

namespace Cirros.Primitives
{
    public class PArc : PPolygon
    {
        protected double _startAngle;
        protected double _includedAngle;
        bool _isCircle = false;
        bool _isWedge = true;

        private bool _lockRadius = true;

        public const double PI2 = 2 * Math.PI;

        public PArc(Point c, double radius) : base(c)
        {
            // circle constructor

            _isCircle = true;
            _layerId = ActiveLayer;

            _radius = radius;
            _startAngle = 0;
            IncludedAngle = PI2;

            _fill = Globals.CircleFill;
            _fillPattern = Globals.CirclePattern;
            _fillScale = Globals.CirclePatternScale;
            _fillAngle = Globals.CirclePatternAngle;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.CircleLineWeightId;
                _colorSpec = Globals.CircleColorSpec;
                _lineTypeId = Globals.CircleLineTypeId;
            }
        }

        public PArc(Point c, double radius, double startAngle, double includedAngle)
            : base(c)
        {
            _radius = radius;
            _startAngle = startAngle;
            IncludedAngle = includedAngle == 0 ? PI2 : includedAngle;

            _fill = Globals.ArcFill;
            _fillPattern = Globals.ArcPattern;
            _fillScale = Globals.ArcPatternScale;
            _fillAngle = Globals.ArcPatternAngle;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.ArcLineWeightId;
                _colorSpec = Globals.ArcColorSpec;
                _lineTypeId = Globals.ArcLineTypeId;
            }
        }

        public PArc(Point c, double radius, double startAngle, double includedAngle, uint fillSpec)
            : base(c)
        {
            _radius = radius;
            _startAngle = startAngle;
            IncludedAngle = includedAngle == 0 ? PI2 : includedAngle;

            _fill = Globals.ArcFill;
            _fillPattern = Globals.ArcPattern;
            _fillScale = Globals.ArcPatternScale;
            _fillAngle = Globals.ArcPatternAngle;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.ArcLineWeightId;
                _colorSpec = Globals.ArcColorSpec;
                _lineTypeId = Globals.ArcLineTypeId;
            }
        }

        public PArc(PArc3 arc3)
            : base(new Point())
        {
            Point c = arc3.Center;
            double startAngle = Construct.Angle(c, arc3.Origin);
            double radius = Construct.Distance(c, arc3.Origin);
            double inclAngle = Math.PI * 2;

            if (arc3.IsCircle == false)
            {
                double midAngle = Construct.Angle(c, arc3.IntermediatePoint);
                inclAngle = Construct.IncludedAngle(arc3.Origin, c, arc3.EndPoint);

                if (inclAngle < 0)
                    inclAngle += Math.PI * 2;

                double a2 = midAngle - startAngle;
                if (a2 < 0)
                    a2 += Math.PI * 2;
                if (a2 > inclAngle)
                    inclAngle -= Math.PI * 2;
            }

            _layerId = arc3.LayerId;
            _lineTypeId = arc3.LineTypeId;
            _lineWeightId = arc3.LineWeightId;
            _colorSpec = arc3.ColorSpec;
            _zIndex = arc3.ZIndex;

            _origin = arc3.Center;
            _fill = arc3.Fill;
            _fillPattern = arc3.FillPattern;
            _fillScale = arc3.PatternScale;
            _fillAngle = arc3.PatternAngle;
            _radius = Construct.Distance(_origin, arc3.Origin);
            _startAngle = startAngle;
            IncludedAngle = inclAngle;
        }

        public PArc(PArc original)
            : base(original)
        {
            _fill = original._fill;
            _radius = original._radius;
            _startAngle = original._startAngle;
            _fillPattern = original._fillPattern;
            _fillScale = original._fillScale;
            _fillAngle = original._fillAngle;

            IncludedAngle = original._includedAngle;
        }

        public PArc(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            _lineTypeId = e.LineTypeId;
            _lineWeightId = e.LineWeightId;
            _fill = e.Fill;
            _fillPattern = e.FillPattern;
            _fillScale = e.FillScale;
            _fillAngle = e.FillAngle;

            _radius = _container.ModelToPaper((double)e.Radius);
            _startAngle = (double)e.StartAngle;
            IncludedAngle = (double)e.IncludedAngle;

            double ia = Math.Abs(_includedAngle);
            _isCircle = ia < .00001 || Math.Abs(ia - PI2) < .00001;

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

            e.Radius = PrimitiveUtilities.SerializeDoubleAsFloat(_container.PaperToModel(_radius));
            e.StartAngle = PrimitiveUtilities.SerializeDoubleAsFloat(_startAngle);
            e.IncludedAngle = PrimitiveUtilities.SerializeDoubleAsFloat(_includedAngle);

            return e;
        }

        protected override void initializeFromEntity(Cirros.Drawing.Entity e)
        {
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Arc;
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

        public bool LockRadiusForEdit
        {
            get
            {
                return _lockRadius;
            }
            set
            {
                _lockRadius = value;
            }
        }

        public double StartAngle
        {
            get
            {
                return _startAngle;
            }
            set
            {
                _startAngle = value;
            }
        }

        public double IncludedAngle
        {
            get
            {
                return _includedAngle;
            }
            set
            {
                _includedAngle = value;
                double ia = Math.Abs(_includedAngle);
                _isCircle = ia < .00001 || Math.Abs(ia - PI2) < .00001;
            }
        }

        public bool IsWedge
        {
            get
            {
                return _isWedge;
            }
            set
            {
                _isWedge = value;
            }
        }

        public bool IsCircle
        {
            get
            {
                return _isCircle;
            }
        }

        public override Primitive Clone()
        {
            return new PArc(this);
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
                Point normal;
                if (Construct.NormalArcToPoint(_origin, _radius, from, out normal))
                {
                    dynamicConstructNodes.Add(new ConstructNode(normal, "normal"));
                }

                Point tangent0, tangent1;
                if (Construct.TangentArcToPoint(_origin, _radius, from, out tangent0, out tangent1))
                {
                    dynamicConstructNodes.Add(new ConstructNode(tangent0, "tangent"));
                    dynamicConstructNodes.Add(new ConstructNode(tangent1, "tangent"));
                }

                if (from != through)
                {
                    Point i0, i1;
                    if (Construct.IntersectLineArc(from, through, _origin, _radius, out i0, out i1))
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
                    _staticConstructNodes.Add(new ConstructNode(_origin, "origin"));

                    if (IsTransformed)
                    {
                        Point p0 = _matrix.Transform(new Point(0, 0));
                        Point p1 = _matrix.Transform(new Point(_radius, 0));
                        Point p2 = _matrix.Transform(new Point(0, _radius));
                        Point p3 = _matrix.Transform(new Point(-_radius, 0));
                        Point p4 = _matrix.Transform(new Point(0, -_radius));

                        p0.X += _origin.X;
                        p0.Y += _origin.Y;
                        _staticConstructNodes.Add(new ConstructNode(p0, "center"));

                        p1.X += _origin.X;
                        p1.Y += _origin.Y;
                        _staticConstructNodes.Add(new ConstructNode(p1, "quadrant"));

                        p2.X += _origin.X;
                        p2.Y += _origin.Y;
                        _staticConstructNodes.Add(new ConstructNode(p2, "quadrant"));

                        p3.X += _origin.X;
                        p3.Y += _origin.Y;
                        _staticConstructNodes.Add(new ConstructNode(p3, "quadrant"));

                        p4.X += _origin.X;
                        p4.Y += _origin.Y;
                        _staticConstructNodes.Add(new ConstructNode(p4, "quadrant"));

                        if (_isCircle == false)
                        {
                            Point p5 = _matrix.Transform(Construct.PolarOffset(new Point(0, 0), _radius, _startAngle));
                            Point p6 = _matrix.Transform(Construct.PolarOffset(new Point(0, 0), _radius, _startAngle + _includedAngle));

                            p5.X += _origin.X;
                            p5.Y += _origin.Y;
                            p6.X += _origin.X;
                            p6.Y += _origin.Y;

                            _staticConstructNodes.Add(new ConstructNode(p5, "end"));
                            _staticConstructNodes.Add(new ConstructNode(p6, "end"));
                        }
                    }
                    else
                    {
                        if (_isCircle == false)
                        {
                            _staticConstructNodes.Add(new ConstructNode(Construct.PolarOffset(_origin, _radius, _startAngle), "end"));
                            _staticConstructNodes.Add(new ConstructNode(Construct.PolarOffset(_origin, _radius, _startAngle + _includedAngle), "end"));
                        }

                        if (_isCircle || Construct.ArcIncludesAngle(_startAngle, _includedAngle, 0))
                        {
                            _staticConstructNodes.Add(new ConstructNode(new Point(_origin.X + _radius, _origin.Y), "quadrant"));
                        }
                        if (_isCircle || Construct.ArcIncludesAngle(_startAngle, _includedAngle, Math.PI / 2))
                        {
                            _staticConstructNodes.Add(new ConstructNode(new Point(_origin.X, _origin.Y + _radius), "quadrant"));
                        }
                        if (_isCircle || Construct.ArcIncludesAngle(_startAngle, _includedAngle, Math.PI))
                        {
                            _staticConstructNodes.Add(new ConstructNode(new Point(_origin.X - _radius, _origin.Y), "quadrant"));
                        }
                        if (_isCircle || Construct.ArcIncludesAngle(_startAngle, _includedAngle, 3 * Math.PI / 2))
                        {
                            _staticConstructNodes.Add(new ConstructNode(new Point(_origin.X, _origin.Y - _radius), "quadrant"));
                        }
                    }
                }

                return _constructEnabled ? _staticConstructNodes : new List<ConstructNode>();
            }
        }

        protected override void _drawHandles(Handles handles)
        {
            if (IsTransformed)
            {
                base._drawHandles(handles);
            }
            else
            {
                Point p = Construct.PolarOffset(_origin, _radius, _startAngle);

                handles.Attach(this);
                handles.AddHandle(1, _origin.X, _origin.Y);
                handles.AddHandle(2, p.X, p.Y);

                if (_isCircle == false)
                {
                    Point e = Construct.PolarOffset(_origin, _radius, _startAngle + _includedAngle);
                    handles.AddHandle(3, e.X, e.Y);
                }
                handles.ArrowTo = handles.SelectedHandleID;
                handles.Draw();
            }
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
                handlePoint.Point = Construct.PolarOffset(_origin, _radius, _startAngle);
            }
            else if (handleId == 3)
            {
                handlePoint.Point = Construct.PolarOffset(_origin, _radius, _startAngle + _includedAngle);
            }

            return handlePoint;
        }

        public override void MoveHandlePoint(int handleId, Point p)
        {
            ClearStaticConstructNodes();

            if (_isCircle)
            {
                _startAngle = Construct.Angle(_origin, p);

                if (handleId == 2)
                {
                    _radius = Construct.Distance(_origin, p);
                }
            }
            else if (handleId == 2)
            {
                int direction = Math.Sign(_includedAngle);
                double ea = _startAngle + _includedAngle;
                _startAngle = Construct.Angle(_origin, p);
                IncludedAngle = Construct.IncludedAngle(_startAngle, ea, direction >= 0);
            }
            else if (handleId == 3)
            {
                int direction = Math.Sign(_includedAngle);
                double ea = Construct.Angle(_origin, p);
                IncludedAngle = Construct.IncludedAngle(_startAngle, ea, direction >= 0);
            }

            Draw();
        }

        protected override void _moveHandle(Handles handles, int id, double dx, double dy)
        {
            ClearStaticConstructNodes();

            if (_isCircle)
            {
                Point s = handles.GetHandlePoint(2);

                _startAngle = Construct.Angle(_origin, s);

                if (id == 2)
                {
                    _radius = Construct.Distance(_origin, s);
                }
            }
            else
            {
                Point s = handles.GetHandlePoint(2);
                Point e = handles.GetHandlePoint(3);
                int direction = Math.Sign(_includedAngle);

                _startAngle = Construct.Angle(_origin, s);
                double ea = Construct.Angle(_origin, e);
 
                double ia = Construct.IncludedAngle(_startAngle, ea, direction >= 0);
                if (Math.Abs(ia) > .001)
                {
                    _includedAngle = ia;
                }

                if (!_lockRadius)
                {
                    if (id == 2)
                    {
                        _radius = Construct.Distance(_origin, s);
                    }
                    else if (id == 3)
                    {
                        _radius = Construct.Distance(_origin, e);
                    }
                }
            }
        }

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
            if (context.HasArcPrimitive && _matrix.IsIdentity && _fill == (uint)ColorCode.NoFill)
            {
                VectorArcEntity va = new VectorArcEntity();
                va.Center = _origin;
                va.Radius = _radius;
                va.StartAngle = _startAngle;
                va.IncludedAngle = _includedAngle;
                va.IsCircle = _isCircle;
                ve.AddChild(va);
            }
            else
            {
                bool wedge = _isWedge && (Math.Abs(_includedAngle) < 6.2831 && _fill != (uint)ColorCode.NoFill);

                ve.AddChild(CGeometry.ArcPointCollection(_origin, _radius, _startAngle, _includedAngle, wedge, _matrix));
                ve.SetFillFromPrimitive(this);
                FillVE(ve);
            }

            _ve = ve;

            return ve;
        }
    }
}
