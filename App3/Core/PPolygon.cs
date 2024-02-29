using Cirros.Drawing;
using System.Collections.Generic;
#if UWP
using Windows.Foundation;
#else
using System.Windows;
#endif

namespace Cirros.Primitives
{
    public class PPolygon : PLine
    {
        public PPolygon(PPolygon original)
            : base(original)
        {
            _fill = original._fill;
            _radius = original._radius;
            _fillEvenOdd = original._fillEvenOdd;
            _fillPattern = original._fillPattern; 
            _fillScale = original._fillScale;
            _fillAngle = original._fillAngle;
        }

        public PPolygon(Point origin)
            : base(origin)
        {
            _fill = Globals.PolygonFill;
            _radius = Globals.PolygonFilletRadius;
            _fillEvenOdd = Globals.PolygonFillEvenOdd;
            _fillPattern = Globals.PolygonPattern;
            _fillScale = Globals.PolygonPatternScale;
            
            _fillAngle = Globals.PolygonPatternAngle;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.PolygonLineWeightId;
                _colorSpec = Globals.PolygonColorSpec;
                _lineTypeId = Globals.PolygonLineTypeId;
            }
        }

        public PPolygon(Point s, Point e)
            : base(s)
        {
            Point p = new Point(e.X - s.X, e.Y - s.Y);
            _points.Add(p);
            _rf.Add(_drawMode);
            _fill = Globals.PolygonFill;
            _radius = Globals.PolygonFilletRadius;
            _fillEvenOdd = Globals.PolygonFillEvenOdd;
            _fillPattern = Globals.PolygonPattern;
            _fillScale = Globals.PolygonPatternScale;
            _fillAngle = Globals.PolygonPatternAngle;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.PolygonLineWeightId;
                _colorSpec = Globals.PolygonColorSpec;
                _lineTypeId = Globals.PolygonLineTypeId;
            }
        }

        public PPolygon(Point s, List<CPoint> cpoints)
            : base(s)
        {
            for (int i = 0; i < cpoints.Count; i++)
            {
                _points.Add(cpoints[i].Point);
                _rf.Add(cpoints[i].M);
            }

            _fill = Globals.PolygonFill;
            _radius = Globals.PolygonFilletRadius;
            _fillEvenOdd = Globals.PolygonFillEvenOdd;
            _fillPattern = Globals.PolygonPattern;
            _fillScale = Globals.PolygonPatternScale;
            _fillAngle = Globals.PolygonPatternAngle;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.PolygonLineWeightId;
                _colorSpec = Globals.PolygonColorSpec;
                _lineTypeId = Globals.PolygonLineTypeId;
            }
        }

        public PPolygon(List<Point> pc)
            : base(pc)
        {
            _fill = Globals.PolygonFill;
            _radius = 0;
            _fillEvenOdd = Globals.PolygonFillEvenOdd;
            _fillPattern = Globals.PolygonPattern;
            _fillScale = Globals.PolygonPatternScale;
            _fillAngle = Globals.PolygonPatternAngle;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.PolygonLineWeightId;
                _colorSpec = Globals.PolygonColorSpec;
                _lineTypeId = Globals.PolygonLineTypeId;
            }
        }

        public PPolygon(Point s, List<Point> points)
            : base(s)
        {
            _points = points;

            for (int i = 0; i < _points.Count; i++)
            {
                _rf.Add(_drawMode);
            }

            _fill = Globals.PolygonFill;
            _radius = Globals.PolygonFilletRadius;
            _fillEvenOdd = Globals.PolygonFillEvenOdd;
            _fillPattern = Globals.PolygonPattern;
            _fillScale = Globals.PolygonPatternScale;
            _fillAngle = Globals.PolygonPatternAngle;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.PolygonLineWeightId;
                _colorSpec = Globals.PolygonColorSpec;
                _lineTypeId = Globals.PolygonLineTypeId;
            }
        }

        public PPolygon(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            _fill = e.Fill;
            _fillPattern = e.FillPattern;
            _fillEvenOdd = e.FillEvenOdd;

            if (string.IsNullOrEmpty(_fillPattern) || _fillPattern == "Solid")
            {
                _fillPattern = null;
                _fillScale = 1;
                _fillAngle = 0;
            }
            else
            {
                _fillScale = e.FillScale;
                _fillAngle = e.FillAngle;
            }
        }

        public override Entity Serialize()
        {
            Entity e = base.Serialize();

            e.Fill = _fill;
            e.Radius = PrimitiveUtilities.SerializeDoubleAsFloat(_container.PaperToModel(_radius));
            e.FillPattern = _fillPattern;
            e.FillEvenOdd = _fillEvenOdd;

            if (string.IsNullOrEmpty(_fillPattern) || _fillPattern == "Solid")
            {
                e.FillPattern = "Solid";
            }
            else
            {
                e.FillScale = (float)_fillScale;
                e.FillAngle = (float)_fillAngle;
            }

            return e;
        }

        public override Primitive Clone()
        {
            return new PPolygon(this);
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Polygon;
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
                    if (Globals.LayerTable.ContainsKey(Globals.ActivePolygonLayerId))
                    {
                        return Globals.ActivePolygonLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        public override VectorEntity Vectorize(VectorContext context, bool ignoreGaps)
        {
            VectorEntity ve = base.Vectorize(context, ignoreGaps);

            FillVE(ve);

            return ve;
        }
    }
}
