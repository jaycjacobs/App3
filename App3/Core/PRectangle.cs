using Cirros.Core;
using Cirros.Drawing;
using Cirros.Utility;
using System.Collections.Generic;
using Cirros.Display;
using System;

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
    public class PRectangle : PPolygon
    {
        protected double _height;
        protected double _width;

        public PRectangle(Point leftTop, double width, double height)
            : base(leftTop)
        {
            _width = width;
            _height = height;
            _fill = Globals.RectangleFill;
            _fillPattern = Globals.RectanglePattern;
            _fillScale = Globals.RectanglePatternScale;
            _fillAngle = Globals.RectanglePatternAngle;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.RectangleLineWeightId;
                _colorSpec = Globals.RectangleColorSpec;
                _lineTypeId = Globals.RectangleLineTypeId;
            }
        }

        public PRectangle(PRectangle original)
            : base(original)
        {
            _width = original._width;
            _height = original._height;
            _fill = original._fill;
            _fillPattern = original._fillPattern;
            _fillScale = original._fillScale;
            _fillAngle = original._fillAngle;
        }

        public PRectangle(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            _lineTypeId = e.LineTypeId;
            _lineWeightId = e.LineWeightId;

            Point size = _container.ModelToPaperDelta(new Point((double)e.Width, (double)e.Height));
            _width = size.X;
            _height = size.Y;

            _fill = e.Fill;
            _fillPattern = e.FillPattern;
            _fillScale = e.FillScale;
            _fillAngle = e.FillAngle;
        }

        protected override void initializeFromEntity(Entity e)
        {
            // Do not initialize the points collection
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Rectangle;
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
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveRectangleLayerId))
                    {
                        return Globals.ActiveRectangleLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        public override Entity Serialize()
        {
            Entity e = base.Serialize();

            e.LineTypeId = LineTypeId;
            e.LineWeightId = LineWeightId;

            Point size = _container.PaperToModelDelta(new Point(_width, _height));
            e.Width = PrimitiveUtilities.SerializeDoubleAsFloat(size.X);
            e.Height = PrimitiveUtilities.SerializeDoubleAsFloat(size.Y);

            e.Fill = _fill;
            e.FillPattern = _fillPattern;
            e.FillScale = (float)_fillScale;
            e.FillAngle = (float)_fillAngle;

            return e;
        }

        public double Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = value;
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

        public override Primitive Clone()
        {
            return new PRectangle(this);
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
                    Point p = new Point(_width, _height);
                    p = _matrix.Transform(p);
                    _width = p.X;
                    _height = p.Y;
                    _matrix = Matrix.Identity;
                }
            }
            return undid;
        }

        public override bool UnNormalize(Matrix m)
        {
            bool change = false;

            if (m.IsIdentity == false)
            {
                Matrix inverse = CGeometry.InvertMatrix(m);

                Point p = new Point(_width, _height);
                p = inverse.Transform(p);
                _width = p.X;
                _height = p.Y;

                _matrix = m;
                change = true;
            }

            return change;
        }

        public override List<ConstructNode> ConstructNodes
        {
            get
            {
                List<ConstructNode> nodes = _staticConstructNodes;

                if (_constructEnabled && nodes.Count == 0)
                {
                    nodes = base.ConstructNodes;

                    if (IsTransformed == false)
                    {
                        Point center = new Point(_origin.X + _width / 2, _origin.Y + _height / 2);
                        nodes.Add(new ConstructNode(center, "center"));
                        _staticConstructNodes = nodes;
                    }
                }

                return nodes;
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
                handles.Attach(this);
                handles.AddHandle(1, _origin.X, _origin.Y);
                handles.AddHandle(2, _origin.X + _width, _origin.Y);
                handles.AddHandle(3, _origin.X + _width, _origin.Y + _height);
                handles.AddHandle(4, _origin.X, _origin.Y + _height);
                handles.Draw();
            }
        }

        protected override void _moveHandle(Handles handles, int id, double dx, double dy)
        {
            ClearStaticConstructNodes();

            if (id == 1)
            {
                _origin.X += dx;
                _origin.Y += dy;
                _width -= dx;
                _height -= dy;
            }
            else if (id == 2)
            {
                _origin.Y += dy;
                _width += dx;
                _height -= dy;
            }
            else if (id == 3)
            {
                _width += dx;
                _height += dy;
            }
            else if (id == 4)
            {
                _origin.X += dx;
                _width -= dx;
                _height += dy;
            }
        }

        public override CPoint GetHandlePoint(int handleId)
        {
            CPoint cp = null;

            switch (handleId)
            {
                case 1:
                default:
                    cp = new CPoint(_origin.X, _origin.Y);
                    break;

                case 2:
                    cp = new CPoint(_origin.X + _width, _origin.Y);
                    break;

                case 3:
                    cp = new CPoint(_origin.X + _width, _origin.Y + _height);
                    break;

                case 4:
                    cp = new CPoint(_origin.X, _origin.Y + _height);
                    break;
            }

            return cp;
        }

        public override void MoveHandlePoint(int handleId, Point p)
        {
            ClearStaticConstructNodes();

            double dx, dy;

            if (handleId == 1)
            {
                dx = p.X - _origin.X;
                dy = p.Y - _origin.Y;

                _origin = p;
                _width -= dx;
                _height -= dy;
            }
            else if (handleId == 2)
            {
                dx = p.X - (_origin.X + _width);
                dy = p.Y - _origin.Y;

                _origin.Y += dy;
                _width += dx;
                _height -= dy;
            }
            else if (handleId == 3)
            {
                dx = p.X - (_origin.X + _width);
                dy = p.Y - (_origin.Y + _height);

                _width += dx;
                _height += dy;
            }
            else if (handleId == 4)
            {
                dx = p.X - _origin.X;
                dy = p.Y - (_origin.Y + _height);

                _origin.X += dx;
                _width -= dx;
                _height += dy;
            }

            Draw();
        }

#if true//UWP
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
            List<Point> pc = new List<Point>();


            if (_matrix.IsIdentity)
            {
                pc.Add(_origin);
                pc.Add(new Point(_origin.X + _width, _origin.Y));
                pc.Add(new Point(_origin.X + _width, _origin.Y + _height));
                pc.Add(new Point(_origin.X, _origin.Y + _height));
                pc.Add(_origin);
            }
            else
            {
                Point p0 = _matrix.Transform(new Point(0, 0));
                pc.Add(new Point(_origin.X + p0.X, _origin.Y + p0.Y));

                Point p1 = _matrix.Transform(new Point(_width, 0.0));
                pc.Add(new Point(_origin.X + p1.X, _origin.Y + p1.Y));

                Point p2 = _matrix.Transform(new Point(_width, _height));
                pc.Add(new Point(_origin.X + p2.X, _origin.Y + p2.Y));

                Point p3 = _matrix.Transform(new Point(0.0, _height));
                pc.Add(new Point(_origin.X + p3.X, _origin.Y + p3.Y));

                pc.Add(new Point(_origin.X + p0.X, _origin.Y + p0.Y));
            }

            ve.AddChild(pc);
            ve.SetFillFromPrimitive(this);

            if (_fill != (uint)ColorCode.NoFill)
            {
                FillVE(ve);
            }

            _ve = ve;

            return ve;
        }
#else
#endif
    }
}
