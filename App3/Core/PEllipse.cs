using Cirros.Core;
using Cirros.Display;
using Cirros.Drawing;
using Cirros.Utility;
using System;
using System.Collections.Generic;
#if UWP
using Cirros.Actions;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
#else
using System.Windows.Media;
using System.Windows;
#endif

namespace Cirros.Primitives
{
    public class PEllipse : PPolygon
    {
        protected double _major;
        protected double _minor;
        protected double _axisAngle;
        protected double _startAngle;
        protected double _includedAngle;
        bool _isFullEllipse = false;

        public PEllipse(Point c, double major, double minor, double axisAngle, double startAngle, double includedAngle)
            : base(c)
        {
            _major = Math.Abs(major);
            _minor = Math.Abs(minor);
            _axisAngle = axisAngle;
            _startAngle = startAngle;
            _fill = Globals.EllipseFill;
            _fillPattern = Globals.EllipsePattern;
            _fillScale = Globals.EllipsePatternScale;
            _fillAngle = Globals.EllipsePatternAngle;

            IncludedAngle = includedAngle == 0 ? PArc.PI2 : includedAngle;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.EllipseLineWeightId;
                _colorSpec = Globals.EllipseColorSpec;
                _lineTypeId = Globals.EllipseLineTypeId;
            }
        }

        public PEllipse(PEllipse original)
            : base(original)
        {
            _major = original._major;
            _minor = original._minor;
            _axisAngle = original._axisAngle;
            _startAngle = original._startAngle;
            _fill = original._fill;
            _fillPattern = original._fillPattern;
            _fillScale = original._fillScale;
            _fillAngle = original._fillAngle;

            IncludedAngle = original._includedAngle;
        }

        public PEllipse(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            _lineTypeId = e.LineTypeId;
            _lineWeightId = e.LineWeightId;

            _major = _container.ModelToPaper((double)Math.Abs(e.Width)) / 2;
            _minor = _container.ModelToPaper((double)Math.Abs(e.Height)) / 2;
            _axisAngle = (double)e.Angle;
            _startAngle = (double)e.StartAngle;
            _fill = e.Fill;
            _fillPattern = e.FillPattern;
            _fillScale = e.FillScale;
            _fillAngle = e.FillAngle;

            IncludedAngle = (double)e.IncludedAngle;
        }

        public override Entity Serialize()
        {
            Entity e = base.Serialize();

            e.LineTypeId = LineTypeId;
            e.LineWeightId = LineWeightId;

            e.Width = PrimitiveUtilities.SerializeDoubleAsFloat(_container.PaperToModel(_major) * 2);
            e.Height = PrimitiveUtilities.SerializeDoubleAsFloat(_container.PaperToModel(_minor) * 2);
            e.Angle = PrimitiveUtilities.SerializeDoubleAsFloat(_axisAngle);
            e.StartAngle = PrimitiveUtilities.SerializeDoubleAsFloat(_startAngle);
            e.IncludedAngle = PrimitiveUtilities.SerializeDoubleAsFloat(_includedAngle);
            e.Fill = _fill;
            e.FillPattern = _fillPattern;
            e.FillScale = (float)_fillScale;
            e.FillAngle = (float)_fillAngle;

            return e;
        }

        protected override void initializeFromEntity(Entity e)
        {
            // Do not initialize the points collection
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Ellipse;
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
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveEllipseLayerId))
                    {
                        return Globals.ActiveEllipseLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        public double Major
        {
            get
            {
                return _major;
            }
            set
            {
                _major = Math.Abs(value);
            }
        }

        public double Minor
        {
            get
            {
                return _minor;
            }
            set
            {
                _minor = Math.Abs(value);
            }
        }

        public double AxisAngle
        {
            get
            {
                return _axisAngle;
            }
            set
            {
                _axisAngle = value;
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
                if (ia < .001)
                {
                    ia = _includedAngle = PArc.PI2;
                }
                _isFullEllipse = Math.Abs(ia - PArc.PI2) < .001;
            }
        }

        public override bool Normalize(bool undoable)
        {
            bool undid = false;
            if (_matrix.IsIdentity == false)
            {
                if (Math.Round(_matrix.M12, 6) == 0 && Math.Round(_matrix.M21, 6) == 0 && Math.Round(_matrix.OffsetX, 6) == 0 && Math.Round(_matrix.OffsetX, 6) == 0)
                {
#if UWP
                    if (undoable)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.UnNormalize, this, _matrix);
                        undid = true;
                    }
#else
#endif
                    Point p = new Point(_major, _minor);
                    p = _matrix.Transform(p);
                    _major = p.X;
                    _minor = p.Y;
                    _matrix = Matrix.Identity;
                }
            }
            return undid;
        }

        public override Primitive Clone()
        {
            return new PEllipse(this);
        }

        public override List<ConstructNode> ConstructNodes
        {
            get
            {
                if (_constructEnabled && _staticConstructNodes.Count == 0)
                {
                    double majorDx = _major * Math.Cos(_axisAngle);
                    double majorDy = _major * Math.Sin(_axisAngle);
                    double minorDx = _minor * Math.Cos((Math.PI / 2) + _axisAngle);
                    double minorDy = _minor * Math.Sin((Math.PI / 2) + _axisAngle);

                    if (IsTransformed)
                    {
                        Point p0 = _matrix.Transform(new Point(0, 0));
                        Point p1 = _matrix.Transform(new Point(minorDx, minorDy));
                        Point p2 = _matrix.Transform(new Point(majorDx, majorDy));
                        Point p3 = _matrix.Transform(new Point(-minorDx, -minorDy));
                        Point p4 = _matrix.Transform(new Point(-majorDx, -majorDy));

                        p0.X += _origin.X;
                        p0.Y += _origin.Y;
                        p1.X += _origin.X;
                        p1.Y += _origin.Y;
                        p2.X += _origin.X;
                        p2.Y += _origin.Y;
                        p3.X += _origin.X;
                        p3.Y += _origin.Y;
                        p4.X += _origin.X;
                        p4.Y += _origin.Y;

                        _staticConstructNodes.Add(new ConstructNode(p0, "center"));
                        _staticConstructNodes.Add(new ConstructNode(p1, "axis"));
                        _staticConstructNodes.Add(new ConstructNode(p2, "axis"));
                        _staticConstructNodes.Add(new ConstructNode(p3, "axis"));
                        _staticConstructNodes.Add(new ConstructNode(p4, "axis"));
                    }
                    else
                    {
                        _staticConstructNodes.Add(new ConstructNode(_origin, "center"));
                        Point p1 = new Point(_origin.X + minorDx, _origin.Y + minorDy);
                        Point p2 = new Point(_origin.X + majorDx, _origin.Y + majorDy);
                        Point p3 = new Point(_origin.X - minorDx, _origin.Y - minorDy);
                        Point p4 = new Point(_origin.X - majorDx, _origin.Y - majorDy);

                        _staticConstructNodes.Add(new ConstructNode(p1, "axis"));
                        _staticConstructNodes.Add(new ConstructNode(p2, "axis"));
                        _staticConstructNodes.Add(new ConstructNode(p3, "axis"));
                        _staticConstructNodes.Add(new ConstructNode(p4, "axis"));
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
                double majorDx = _major * Math.Cos(_axisAngle);
                double majorDy = _major * Math.Sin(_axisAngle);
                double minorDx = _minor * Math.Cos((Math.PI / 2) + _axisAngle);
                double minorDy = _minor * Math.Sin((Math.PI / 2) + _axisAngle);

                handles.Attach(this);
                handles.AddHandle(11, _origin.X + minorDx, _origin.Y + minorDy);
                handles.AddHandle(12, _origin.X + majorDx, _origin.Y + majorDy);
                handles.AddHandle(13, _origin.X - minorDx, _origin.Y - minorDy);
                handles.AddHandle(14, _origin.X - majorDx, _origin.Y - majorDy);

                if (Math.Abs(_axisAngle) < .001)
                {
                    handles.AddHandle(1, _origin.X - _major, _origin.Y - _minor, HandleType.Square);
                    handles.AddHandle(2, _origin.X + _major, _origin.Y - _minor, HandleType.Square);
                    handles.AddHandle(3, _origin.X + _major, _origin.Y + _minor, HandleType.Square);
                    handles.AddHandle(4, _origin.X - _major, _origin.Y + _minor, HandleType.Square);
                    handles.AddFrame(_bbox);
                }

                handles.Draw();
            }
        }

        protected override void _moveHandle(Handles handles, int id, double dx, double dy)
        {
            ClearStaticConstructNodes();

            Point p = handles.GetHandlePoint(id);
            Point q = new Point();

            if (id == 1)
            {
                q = handles.GetHandlePoint(3);
                _major = Math.Abs(p.X - q.X) / 2;
                _minor = Math.Abs(p.Y - q.Y) / 2;
            }
            else if (id == 2)
            {
                q = handles.GetHandlePoint(4);
                _major = Math.Abs(p.X - q.X) / 2;
                _minor = Math.Abs(p.Y - q.Y) / 2;
            }
            else if (id == 3)
            {
                q = handles.GetHandlePoint(1);
                _major = Math.Abs(p.X - q.X) / 2;
                _minor = Math.Abs(p.Y - q.Y) / 2;
            }
            else if (id == 4)
            {
                q = handles.GetHandlePoint(2);
                _major = Math.Abs(p.X - q.X) / 2;
                _minor = Math.Abs(p.Y - q.Y) / 2;
            }
            else if (id == 11)
            {
                // End of minor axis
                q = handles.GetHandlePoint(13);
                _minor = Construct.Distance(q, p) / 2;
                _axisAngle = Construct.Angle(q, p) - (Math.PI / 2);
            }
            else if (id == 12)
            {
                // End of major axis
                q = handles.GetHandlePoint(14);
                _major = Construct.Distance(q, p) / 2;
                _axisAngle = Construct.Angle(q, p);
            }
            else if (id == 13)
            {
                // End of minor axis
                q = handles.GetHandlePoint(11);
                _minor = Construct.Distance(q, p) / 2;
                _axisAngle = Construct.Angle(q, p) + (Math.PI / 2);
            }
            else if (id == 14)
            {
                // End of major axis
                q = handles.GetHandlePoint(12);
                _major = Construct.Distance(q, p) / 2;
                _axisAngle = Construct.Angle(q, p) - Math.PI;
            }

            _origin.X = (p.X + q.X) / 2;
            _origin.Y = (p.Y + q.Y) / 2;
        }

        public override CPoint GetHandlePoint(int handleId)
        {
            CPoint cp = null;
            if (IsTransformed)
            {
                cp = base.GetHandlePoint(handleId);
            }
            else
            {
                double majorDx = _major * Math.Cos(_axisAngle);
                double majorDy = _major * Math.Sin(_axisAngle);
                double minorDx = _minor * Math.Cos((Math.PI / 2) + _axisAngle);
                double minorDy = _minor * Math.Sin((Math.PI / 2) + _axisAngle);

                switch (handleId)
                {
                    case 1:
                    default:
                        cp = new CPoint(_origin.X - _major, _origin.Y - _minor);
                        break;

                    case 2:
                        cp = new CPoint(_origin.X + _major, _origin.Y - _minor);
                        break;

                    case 3:
                        cp = new CPoint(_origin.X + _major, _origin.Y + _minor);
                        break;

                    case 4:
                        cp = new CPoint(_origin.X - _major, _origin.Y - _minor);
                        break;

                    case 11:
                        cp = new CPoint(_origin.X + minorDx, _origin.Y + minorDy);
                        break;

                    case 12:
                        cp = new CPoint(_origin.X + majorDx, _origin.Y + majorDy);
                        break;

                    case 13:
                        cp = new CPoint(_origin.X - minorDx, _origin.Y - minorDy);
                        break;

                    case 14:
                        cp = new CPoint(_origin.X - majorDx, _origin.Y - majorDy);
                        break;
                }
            }

            return cp;
        }

        public override void MoveHandlePoint(int handleId, Point p)
        {
            double majorDx = _major * Math.Cos(_axisAngle);
            double majorDy = _major * Math.Sin(_axisAngle);
            double minorDx = _minor * Math.Cos((Math.PI / 2) + _axisAngle);
            double minorDy = _minor * Math.Sin((Math.PI / 2) + _axisAngle);

            Point[] pa = new Point[4];
            pa[0] = new Point(_origin.X + minorDx, _origin.Y + minorDy);
            pa[1] = new Point(_origin.X + majorDx, _origin.Y + majorDy);
            pa[2] = new Point(_origin.X - minorDx, _origin.Y - minorDy);
            pa[3] = new Point(_origin.X - majorDx, _origin.Y - majorDy);


            ClearStaticConstructNodes();

            Point q = new Point();

            if (handleId == 1)
            {
                q = new Point(_origin.X + _major, _origin.Y + _minor);
                _major = Math.Abs(p.X - q.X) / 2;
                _minor = Math.Abs(p.Y - q.Y) / 2;
            }
            else if (handleId == 2)
            {
                q = new Point(_origin.X - _major, _origin.Y + _minor);
                _major = Math.Abs(p.X - q.X) / 2;
                _minor = Math.Abs(p.Y - q.Y) / 2;
            }
            else if (handleId == 3)
            {
                q = new Point(_origin.X - _major, _origin.Y - _minor);
                _major = Math.Abs(p.X - q.X) / 2;
                _minor = Math.Abs(p.Y - q.Y) / 2;
            }
            else if (handleId == 4)
            {
                q = new Point(_origin.X + _major, _origin.Y - _minor);
                _major = Math.Abs(p.X - q.X) / 2;
                _minor = Math.Abs(p.Y - q.Y) / 2;
            }
            else if (handleId == 11)
            {
                // End of minor axis
                q = pa[2];
                _minor = Construct.Distance(q, p) / 2;
                _axisAngle = Construct.Angle(q, p) - (Math.PI / 2);
            }
            else if (handleId == 12)
            {
                // End of major axis
                q = pa[3];
                _major = Construct.Distance(q, p) / 2;
                _axisAngle = Construct.Angle(q, p);
            }
            else if (handleId == 13)
            {
                // End of minor axis
                q = pa[0];
                _minor = Construct.Distance(q, p) / 2;
                _axisAngle = Construct.Angle(q, p) + (Math.PI / 2);
            }
            else if (handleId == 14)
            {
                // End of major axis
                q = pa[1];
                _major = Construct.Distance(q, p) / 2;
                _axisAngle = Construct.Angle(q, p) - Math.PI;
            }

            _origin.X = (p.X + q.X) / 2;
            _origin.Y = (p.Y + q.Y) / 2;

            Draw();
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
            bool wedge = _isFullEllipse == false && _fill != (uint)ColorCode.NoFill;

            if (_matrix.IsIdentity)
            {
                ve.AddChild(CGeometry.EllipsePointCollection(_origin, _major, _minor, _axisAngle, _startAngle, _includedAngle, wedge, _matrix));
            }
            else
            {
                List<Point> p0 = CGeometry.EllipsePointCollection(new Point(0, 0), _major, _minor, _axisAngle, _startAngle, _includedAngle, wedge, _matrix);
                List<Point> p1 = new List<Point>();

                foreach (Point p in p0)
                {
                    p1.Add(new Point(p.X + _origin.X, p.Y + _origin.Y));
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
