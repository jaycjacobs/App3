using Cirros.Core;
using Cirros.Core.Primitives;
using Cirros.Drawing;
using Cirros.Utility;
using CirrosCore;
using System.Collections.Generic;
#if UWP
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
#else
using System.Windows;
using System.Windows.Media;
#endif

namespace Cirros.Primitives
{
    public enum DbEndStyle
    {
        None = 0,
        Start = 1,
        End = 2,
        Both = 3
    }

    public class PDoubleline : PLine
    {
        private double _width = .125;
        //private double _halfWidth = .0625;
        private DbEndStyle _endstyle = DbEndStyle.None;

        private uint _joinStart = 0;
        private uint _joinEnd = 0;

        protected override uint _drawMode
        {
            get
            {
                return 3;
            }
        }

        public PDoubleline(CPoint s, CPoint e)
            : base(s, e)
        {
            Width = Globals.DoubleLineWidth;
            _endstyle = Globals.DoublelineEndStyle;
            _fill = Globals.DoublelineFill;
            _fillPattern = Globals.DoublelinePattern;
            _fillScale = Globals.DoublelinePatternScale;
            _fillAngle = Globals.DoublelinePatternAngle;
            _joinStart = 0;
            _joinEnd = 0;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.DoubleLineLineWeightId;
                _colorSpec = Globals.DoubleLineColorSpec;
                _lineTypeId = Globals.DoubleLineLineTypeId;
            }
        }

        public PDoubleline(PDoubleline original) : base(original)
        {
            Width = original._width;
            _endstyle = original._endstyle;
            _fill = original._fill;
            _fillPattern = original._fillPattern;
            _fillScale = original._fillScale;
            _fillAngle = original._fillAngle;

            _joinStart = 0;
            _joinEnd = 0;
        }

        public PDoubleline(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            if (e.DBWidth == 0)
            {
                // prior to 4.0.44.0 serialzed width was incorrectly assumed to be in paper units
                // e.DBWidth now contains the correct model unit value
                // if e.DBWidth is 0, we will assume that this is a legacy file
                Width = (double)e.Width;
            }
            else
            {
                Width = _container.ModelToPaper((double)e.DBWidth);
            }
            _endstyle = e.DoublelineEndStyle;
            _fill = e.Fill;
            _fillPattern = e.FillPattern;
            _fillScale = e.FillScale;
            _fillAngle = e.FillAngle;
            _joinStart = e.JoinStart;
            _joinEnd = e.JoinEnd;
        }

        public override Entity Serialize()
        {
            Entity e = base.Serialize();
            {
                // prior to 4.0.44.0 serialzed width was incorrectly assumed to be in paper units
                // e.DBWidth now contains the correct model unit value
                // we will set e.Width in paper units for legacy versions of the app
                e.Width = PrimitiveUtilities.SerializeDoubleAsFloat(_width);
            }

            e.DBWidth = PrimitiveUtilities.SerializeDoubleAsFloat(_container.PaperToModel(_width));
            e.DoublelineEndStyle = _endstyle;
            e.Fill = _fill;
            e.FillPattern = _fillPattern;
            e.FillScale = (float)_fillScale;
            e.FillAngle = (float)_fillAngle;
            e.JoinStart = _joinStart;
            e.JoinEnd = _joinEnd;

            return e;
        }

        public override Primitive Clone()
        {
            return new PDoubleline(this);
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Doubleline;
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
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveDoubleLineLayerId))
                    {
                        return Globals.ActiveDoubleLineLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        public uint JoinStart
        {
            get
            {
                return _joinStart;
            }
            set
            {
                _joinStart = value;
            }
        }

        public uint JoinEnd
        {
            get
            {
                return _joinEnd;
            }
            set
            {
                _joinEnd = value;
            }
        }

        public DbEndStyle EndStyle
        {
            get
            {
                return _endstyle;
            }
            set
            {
                _endstyle = value;
            }
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
                //_halfWidth = _width / 2;
            }
        }

        public override void Transform(double cx, double cy, Matrix m)
        {
            base.Transform(cx, cy, m);

            Point mp = m.Transform(new Point(_width, _width));
            _width = mp.X;
        }

        protected void VectorizeSegment(List<Point> p0, uint mode, bool closeStart, bool closeEnd, VectorContext context, VectorEntity vo, VectorEntity vf)
        {
            List<Point> p1 = Construct.Reverse(p0);

            double width = _width;
            //if (_matrix == null || _matrix.IsIdentity == false)
            //{
            //    Point p = _matrix.Transform(new Point(_width, _width));
            //    width = (p.X + p.Y) / 2;
            //}
            Construct.MoveLineParallel(p0, width / 2);
            Construct.MoveLineParallel(p1, width / 2);

            if ((mode & 0x1) == 0x1)
            {
                vo.AddChild(p0);
            }

            if (closeEnd)
            {
                List<Point> p3 = new List<Point>();
                p3.Add(p0[p0.Count - 1]);
                p3.Add(p1[0]);
                vo.AddChild(p3);
            }

            if ((mode & 0x2) == 0x2)
            {
                vo.AddChild(p1);
            }

            if (closeStart)
            {
                List<Point> p2 = new List<Point>();
                p2.Add(p1[p1.Count - 1]);
                p2.Add(p0[0]);
                vo.AddChild(p2);
            }

            if (_fill != (uint)ColorCode.NoFill)
            {
                //VectorEntity veContainer = new VectorEntity(_objectId, _zIndex);
                List<Point> pf = new List<Point>();

                foreach (Point p in p0)
                {
                    pf.Add(p);
                }

                foreach (Point p in p1)
                {
                    pf.Add(p);
                }

                pf.Add(p0[0]);
                vf.AddChild(pf);
            }
        }

        protected void VectorizeSegment(List<Point> p0, uint mode, List<Point> clipStart, List<Point> clipEnd, VectorContext context, VectorEntity vo, VectorEntity vf)
        {
            List<Point> p1 = Construct.Reverse(p0);

            double width = _width;
            //if (_matrix == null || _matrix.IsIdentity == false)
            //{
            //    Point p = _matrix.Transform(new Point(_width, _width));
            //    width = (p.X + p.Y) / 2;
            //}
            Construct.MoveLineParallel(p0, width / 2);
            Construct.MoveLineParallel(p1, width / 2);

            if ((mode & 0x1) == 0x1)
            {
                vo.AddChild(p0);
            }

            if (clipEnd == null)
            {
                List<Point> p3 = new List<Point>();
                p3.Add(p0[p0.Count - 1]);
                p3.Add(p1[0]);
                vo.AddChild(p3);
            }
            else if (clipEnd.Count == 2)
            {
                Point a, b;
                if (Construct.IntersectLineLine(p0[p0.Count - 1], p0[p0.Count - 2], clipEnd[0], clipEnd[1], out a) &&
                    Construct.IntersectLineLine(p1[0], p1[1], clipEnd[0], clipEnd[1], out b))
                {
                    p0[p0.Count - 1] = a;
                    p1[0] = b;
                }
            }

            if ((mode & 0x2) == 0x2)
            {
                vo.AddChild(p1);
            }

            if (clipStart == null)
            {
                List<Point> p2 = new List<Point>();
                p2.Add(p1[p1.Count - 1]);
                p2.Add(p0[0]);
                vo.AddChild(p2);
            }
            else if (clipStart.Count == 2)
            {
                Point a, b;
                if (Construct.IntersectLineLine(p1[p1.Count - 1], p1[p1.Count - 2], clipStart[0], clipStart[1], out a) &&
                    Construct.IntersectLineLine(p0[0], p0[1], clipStart[0], clipStart[1], out b))
                {
                    p1[p1.Count - 1] = a;
                    p0[0] = b;
                }
            }

            if (_fill != (uint)ColorCode.NoFill)
            {
                //VectorEntity veContainer = new VectorEntity(_objectId, _zIndex);
                List<Point> pf = new List<Point>();

                foreach (Point p in p0)
                {
                    pf.Add(p);
                }

                foreach (Point p in p1)
                {
                    pf.Add(p);
                }

                pf.Add(p0[0]);
                vf.AddChild(pf);
            }
        }

        public override VectorEntity Vectorize(VectorContext context)
        {
            VectorEntity vs = base.Vectorize(context, true);    // vs is the "spine"
            VectorEntity vf = new VectorEntity(_objectId, _zIndex);               // vf is the "fill" entity
            VectorEntity ve = new VectorEntity(_objectId, _zIndex);               // ve is the "outline" entity

            if (vs.Children.Count == 0)
            {
                return vs;
            }

            List<WallJoint> joints = Globals.ActiveDrawing.GetWallJoints(_objectId);

            ve.Color = vs.Color;
            ve.DashList = vs.DashList;
            ve.Fill = vs.Fill;
            ve.FillColor = vs.FillColor;
            ve.IsVisible = vs.IsVisible;
            ve.LineType = vs.LineType;
            ve.LineWidth = vs.LineWidth;

            vf.LineWidth = vs.LineWidth;
            vf.IsVisible = vs.IsVisible;

            List<Point> p0 = vs.Children[0] as List<Point>;
            List<CPoint> cp0 = new List<CPoint>();

            List<uint>.Enumerator rfe = _rf.GetEnumerator();
            uint M = 0;
#if true
            for (int i = 0; i < p0.Count; i++)
            {
                if (joints != null && i > 0)
                {
                    List<CPoint> cpoints = CGeometry.JoinWallPoints(this, (uint)(i - 1), joints);
                    if (cpoints.Count > 0)
                    {
                        foreach (CPoint cp in cpoints)
                        {
                            cp0.Add(cp);
                        }
                    }
                }

                cp0.Add(new CPoint(p0[i], M));

                rfe.MoveNext();
                M = rfe.Current;
            }
#else
            foreach (Point p in p0)
            {
                cp0.Add(new CPoint(p, M));

                rfe.MoveNext();
                M = rfe.Current;
            }
#endif
            int index = 0;
            bool isClosed = p0[p0.Count - 1] == p0[0];

            if (isClosed)
            {
                // If the doubleline is closed and also contains gaps (not completely closed),
                // Shift the definition so that the path begins at a gap segment (and is no longer closed)

                cp0[0].M = cp0[cp0.Count - 1].M;

                for (int i = 1; i < cp0.Count; i++)
                {
                    if (cp0[i].M != _drawMode)
                    {
                        // We found a gap segment.  The path should start here.
                        int count = cp0.Count;

                        cp0.InsertRange(count, cp0);

                        if (cp0[i].M == 0)
                        {
                            // If the path now begins on a full gap segment, the gap can be ignored
                            cp0.RemoveRange(0, i);
                            cp0.RemoveRange(count, cp0.Count - count);
                        }
                        else
                        {
                            // If the path now begins on a gap left or gap right segment, add that segment to the path
                            cp0.RemoveRange(0, i - 1);
                            cp0.RemoveRange(count + 1, cp0.Count - (count + 1));
                        }

                        //isClosed = false;
                        break;
                    }
                }
            }

            List<Point> clipStart = null;
            List<Point> clipEnd = null;

            if (isClosed)
            {
                // closed figure
                // no need to cap or clip ends
                clipStart = new List<Point>();
                clipEnd = new List<Point>();
            }
            else
            {
                if (_joinStart != 0)
                {
                    uint tid = _joinStart;

                    PDoubleline toDb = Globals.ActiveDrawing.FindObjectById(tid) as PDoubleline;
                    if (toDb != null)
                    {
                        double pv;
                        Point p = _origin;
                        int segment = toDb.PickSegment(ref p, out pv);
                        if (segment >= 0)
                        {
                            clipStart = toDb.GetSegment(segment);
                            if (clipStart.Count == 2)
                            {
                                double sign = (double)Construct.WhichSide(clipStart[0], clipStart[1], cp0[1].Point);
                                Construct.MoveLineParallel(clipStart, sign * toDb.Width / 2);

                                // Add _joinStart to wall joint list
                                Globals.ActiveDrawing.AddWallJoint(new WallJoint(this.Id, 0, tid, (uint)segment));
                            }
                        }
                    }
                }

                if (_joinEnd != 0)
                {
                    uint tid = _joinEnd;

                    PDoubleline toDb = Globals.ActiveDrawing.FindObjectById(tid) as PDoubleline;
                    if (toDb != null)
                    {
                        double pv;
                        Point p = cp0[cp0.Count - 1].Point;
                        int segment = toDb.PickSegment(ref p, out pv);
                        if (segment >= 0)
                        {
                            clipEnd = toDb.GetSegment(segment);
                            if (clipEnd.Count == 2)
                            {
                                double sign = (double)Construct.WhichSide(clipEnd[0], clipEnd[1], cp0[cp0.Count - 2].Point);
                                Construct.MoveLineParallel(clipEnd, sign * toDb.Width / 2);
                            }

                            // Add _joinEnd to wall joint list
                            Globals.ActiveDrawing.AddWallJoint(new WallJoint(this.Id, (uint)p0.Count - 1, tid, (uint)segment));
                        }
                    }
                }
            }

            bool closeStart = isClosed == false && (_endstyle & DbEndStyle.Start) == DbEndStyle.Start;
            bool closeEnd;

            Point lastPoint = cp0[index++].Point;

            while (index < cp0.Count)
            {
                List<Point> pc = new List<Point>();

                pc.Add(lastPoint);
                uint mode = 0xffffffff;

                for (; index < cp0.Count; index++)
                {
                    if (cp0[index].X != lastPoint.X || cp0[index].Y != lastPoint.Y)
                    {
                        if (mode == 0xffffffff)
                        {
                            mode = cp0[index].M;
                        }
                        else if (cp0[index].M != mode)
                        {
                            if (cp0[index].M == 0)
                            {
                                lastPoint = cp0[index].Point;
                            }
                            break;
                        }

                        lastPoint = cp0[index].Point;

                        pc.Add(cp0[index].Point);
                    }
                }

                if (index < cp0.Count)
                {
                    closeEnd = false;
                }
                else
                {
                    closeEnd = isClosed == false && (_endstyle & DbEndStyle.End) == DbEndStyle.End;
                }

                if (ve.Children.Count == 0)
                {
                    // first segment
                    if (index < cp0.Count)
                    {
                        // and not the last segment
                        if (_joinStart == 0)
                        {
                            VectorizeSegment(pc, mode, closeStart, false, context, ve, vf);
                        }
                        else
                        {
                            VectorizeSegment(pc, mode, clipStart, new List<Point>(), context, ve, vf);
                        }
                    }
                    else if (_joinStart == 0 && _joinEnd == 0)
                    {
                        // only segment
                        VectorizeSegment(pc, mode, closeStart, closeEnd, context, ve, vf);
                    }
                    else
                    {
                        VectorizeSegment(pc, mode, clipStart, clipEnd, context, ve, vf);
                    }
                }
                else if (index < cp0.Count)
                {
                    // intermediate segment
                    VectorizeSegment(pc, mode, false, false, context, ve, vf);
                }
                else if (_joinEnd == 0)
                {
                    VectorizeSegment(pc, mode, false, closeEnd, context, ve, vf);
                }
                else if (clipEnd == null)
                {
                    VectorizeSegment(pc, mode, false, false, context, ve, vf);
                }
                else
                {
                    VectorizeSegment(pc, mode, new List<Point>(), clipEnd, context, ve, vf);
                }

                closeStart = false;
            }

            VectorEntity vContainer = ve;

            if (vf.Children.Count > 0)
            {
                vContainer = new VectorEntity(_objectId, _zIndex);

                if (_fill == (uint)ColorCode.ByLayer)
                {
                    vf.FillColor = Utilities.ColorFromColorSpec(Globals.LayerTable[_layerId].ColorSpec);
                }
                else if (_fill != (uint)ColorCode.SameAsOutline)
                {
                    vf.FillColor = Utilities.ColorFromColorSpec(_fill);
                }
                else
                {
                    vf.FillColor = ve.Color;
                    vContainer.LineWidth = 0;
                }

                if (_fill != (uint)ColorCode.NoFill)
                {
                    if (string.IsNullOrEmpty(_fillPattern) || _fillPattern == "Solid")
                    {
                        vf.Color = vf.FillColor;
                        vf.Fill = true;
                    }
                    else
                    {
                        if (IsDynamic)
                        {
                            Color fill = ve.FillColor;
                            fill.A = (byte)(fill.A / 2);
                            vf.Fill = true;
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
                                List<List<Point>> hatches = PrimitiveUtilities.Crosshatch(vf, pattern, _fillScale, _fillAngle, _fillEvenOdd == false);

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

                                    vf.Children.Insert(0, hve);
                                }
                            }
                        }
                    }

                }

                vContainer.AddChild(vf);
                vContainer.AddChild(ve);
            }

            _ve = vs;   // Set the construction entity to the spine

            foreach (List<Point> pc in ve.Children)
            {
                // Add the outline to the construction entity
                _ve.AddChild(pc);
            }

            return vContainer;
        }
    }
}
