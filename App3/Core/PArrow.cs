using Cirros.Drawing;
using Cirros.Utility;
using System.Collections.Generic;
#if UWP
using Windows.Foundation;
#else
using System.Windows;
#endif

namespace Cirros.Primitives
{
    public enum ArrowType
    {
        Filled = 1,
        Outline,
        Open,
        Wide,
        Ellipse,
        Dot
    }

    public enum ArrowLocation
    {
        None,
        Start,
        End,
        Both
    }

    public class PArrow : PLine
    {
        protected ArrowLocation _arrowLocation = ArrowLocation.Both;
        protected int _arrowStyleId = 0;

        public PArrow(Point s, Point e)
            : base(s, e)
        {
            //_layerId = Globals.DimensionLayerId;
            _layerId = ActiveLayer;

            _arrowLocation = Globals.ArrowLocation;
            _arrowStyleId = Globals.ArrowStyleId;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.ArrowLineWeightId;
                _colorSpec = Globals.ArrowColorSpec;
                _lineTypeId = Globals.ArrowLineTypeId;
            }
        }

        public PArrow(Point origin)
            : base(origin)
        {
            //_layerId = Globals.DimensionLayerId;
            _layerId = ActiveLayer;

            _arrowLocation = Globals.ArrowLocation;
            _arrowStyleId = Globals.ArrowStyleId;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.ArrowLineWeightId;
                _colorSpec = Globals.ArrowColorSpec;
                _lineTypeId = Globals.ArrowLineTypeId;
            }
        }

        public PArrow(PArrow original)
            : base(original)
        {
            _arrowStyleId = original._arrowStyleId;
            _arrowLocation = original._arrowLocation;
        }

        public PArrow(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            _arrowStyleId = e.ArrowStyleId;
            _arrowLocation = e.ArrowLocation;
        }

        public override Entity Serialize()
        {
            Entity e = base.Serialize();

            e.ArrowStyleId = _arrowStyleId;
            e.ArrowLocation = _arrowLocation;

            return e;
        }

        public override Primitive Clone()
        {
            return new PArrow(this);
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Arrow;
            }
        }

        public override int ActiveLayer
        {
            get
            {
                if (Globals.UIVersion == 0)
                {
                    return Globals.DimensionLayerId;
                }
                else
                {
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveArrowLayerId))
                    {
                        return Globals.ActiveArrowLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        public int ArrowStyleId
        {

            get
            {
                return _arrowStyleId;
            }
            set 
            { 
                _arrowStyleId = value;
            }
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

        public override VectorEntity Vectorize(VectorContext context)
        {
            VectorEntity ve = base.Vectorize(context, true);

            if (ve.Children.Count > 0)
            {
                ArrowStyle style = Globals.ArrowStyleTable[_arrowStyleId];
                ArrowType arrowType = style.Type;
                double csize = style.Size;
                double aspect = style.Aspect;
                bool filled = style.Type == ArrowType.Filled || style.Type == ArrowType.Dot;
                double offset = 0;
                if (arrowType == ArrowType.Outline)
                {
                    offset = csize;
                }
                else if (arrowType == ArrowType.Ellipse)
                {
                    offset = csize / 2;
                }

                List<Point> pc = ve.Children[0] as List<Point>;

                if (_arrowLocation == ArrowLocation.Start || _arrowLocation == ArrowLocation.Both)
                {
                    List<Point> spc = CGeometry.ArrowPointCollection(pc[0], pc[1], arrowType, csize, aspect);

                    if (offset > 0)
                    {
                        Point o = Construct.OffsetAlongLine(pc[0], pc[1], offset);
                        pc[0] = o;
                    }

                    if (filled)
                    {
                        VectorEntity veStart = new VectorEntity(_objectId, _zIndex);
                        veStart.Fill = true;
                        veStart.FillColor = veStart.Color = ve.Color;
                        veStart.AddChild(spc);
                        ve.AddChild(veStart);
                    }
                    else
                    {
                        ve.AddChild(spc);
                    }
                }

                if (_arrowLocation == ArrowLocation.End || _arrowLocation == ArrowLocation.Both)
                {
                    List<Point> epc = CGeometry.ArrowPointCollection(pc[pc.Count - 1], pc[pc.Count - 2], arrowType, csize, aspect);

                    if (offset > 0)
                    {
                        Point o = Construct.OffsetAlongLine(pc[pc.Count - 1], pc[pc.Count - 2], offset);
                        pc[pc.Count - 1] = o;
                    }

                    if (filled)
                    {
                        VectorEntity veEnd = new VectorEntity(_objectId, _zIndex);
                        veEnd.Fill = true;
                        veEnd.FillColor = veEnd.Color = ve.Color;
                        veEnd.AddChild(epc);
                        ve.AddChild(veEnd);
                    }
                    else
                    {
                        ve.AddChild(epc);
                    }
                }
            }

            // Call base.Vectorize() again to set the _ve global (for construction points)  
            base.Vectorize(context, true);

            return ve;
        }
    }
}
