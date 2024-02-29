using Cirros.Core;
using Cirros.Display;
using Cirros.Drawing;
using Cirros.Utility;
using System.Collections.Generic;
using System.Windows;
#if UWP
using Windows.Foundation;
#else
using CirrosCore;
#endif

namespace Cirros.Primitives
{
    public class PBSpline : PLine
    {
        protected bool _drawFinalSegment = true;

        private VectorEntity _rendered_ve = null;

        public PBSpline(Point s, Point e)
            : base(s, e)
        {
            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.CurveLineWeightId;
                _colorSpec = Globals.CurveColorSpec;
                _lineTypeId = Globals.CurveLineTypeId;
            }
        }

        private PBSpline(PBSpline original)
            : base(original)
        {
        }

        public PBSpline(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
        }

        public override int RemovePoint(bool draw = true)
        {
            if (_points.Count > 0)
            {
                _points.RemoveAt(_points.Count - 1);

                if (draw)
                {
                    Draw();
                }
            }
            else
            {
                return -1;
            }

            return _points.Count;
        }

        public override List<Point> Points
        {
            get
            {
                List<Point> points = new List<Point>();

                points.Add(_origin);

                foreach (Point p in _points)
                {
                    points.Add(new Point(p.X + _origin.X, p.Y + _origin.Y));
                }
                return points;
            }
        }

        public bool DrawFinalSegment
        {
            get
            {
                return _drawFinalSegment;
            }
            set
            {
                _drawFinalSegment = value;
                if (_drawFinalSegment)
                {
                    Draw();
                }
            }
        }

        public override Primitive Clone()
        {
            return new PBSpline(this);
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.BSpline;
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
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveCurveLayerId))
                    {
                        return Globals.ActiveCurveLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        public override List<ConstructNode> DynamicConstructNodes(Point from, Point through)
        {
            System.Diagnostics.Debug.Assert(_dynamicConstructNodes.Count == 0, "DynamicConstructNodes is not zero");
            return _dynamicConstructNodes;
        }

        protected override void _drawHandles(Handles handles)
        {
            handles.Attach(this);
            handles.AddHandle(1, _origin.X, _origin.Y, HandleType.Circle);

            int index = 2;

            foreach (Point pt in _points)
            {
                handles.AddHandle(index++, _origin.X + pt.X, _origin.Y + pt.Y, HandleType.Diamond);
            }

            handles.Connect = true;
            handles.Draw();
        }

        public override void MoveByDelta(double dx, double dy)
        {
            base.MoveByDelta(dx, dy);
            _rendered_ve = null;
        }

        public override VectorEntity Vectorize(VectorContext context)
        {
            _rendered_ve = base.Vectorize(context, true);

            Layer layer = Globals.LayerTable[_layerId];
            int lineWeight = _lineWeightId < 0 ? layer.LineWeightId : _lineWeightId;
            int lineType = _lineTypeId < 0 ? layer.LineTypeId : _lineTypeId;

            _rendered_ve.Color = _colorSpec == (uint)ColorCode.ByLayer ? Utilities.ColorFromColorSpec(layer.ColorSpec) : Utilities.ColorFromColorSpec(_colorSpec);
            _rendered_ve.DashList = Globals.LineTypeTable[lineType].StrokeDashArray;
            _rendered_ve.LineWidth = lineWeight == 0 ? .01 : (double)lineWeight / 1000;
            _rendered_ve.IsVisible = _isVisible && layer.Visible;

            if (_points.Count > 1)
            {
                List<Point> pc = new List<Point>();

                pc.Add(_matrix.Transform(new Point(0.0, 0.0)));

                Point p1 = new Point(0, 0);
                Point p2 = p1;
                Point p3 = _matrix.Transform(_points[0]);
                Point p4 = _matrix.Transform(_points[1]);

                CGeometry.BSplineSegment(pc, p1, p2, p3, p4);

                for (int i = 2; i < _points.Count; i++)
                {
                    p1 = p2;
                    p2 = p3;
                    p3 = p4;
                    p4 = _matrix.Transform(_points[i]);
                    CGeometry.BSplineSegment(pc, p1, p2, p3, p4);
                }

                if (_drawFinalSegment)
                {
                    CGeometry.BSplineSegment(pc, p2, p3, p4, p4);
                    pc.Add(p4);
                }

                List<Point> pc1 = new List<Point>();

                foreach (Point p in pc)
                {
                    pc1.Add(new Point(p.X + _origin.X, p.Y + _origin.Y));
                }

                _rendered_ve.RemoveChildren();
                _rendered_ve.AddChild(pc1);
            }

            // Call base.Vectorize() again to set the _ve global (for construction points) to the spline control points 
            base.Vectorize(context, true);

            return _rendered_ve;
        }
    }
}
