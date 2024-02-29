using Cirros.Drawing;
using Cirros.Utility;
using CirrosCore;
using System;
using System.Collections.Generic;
using System.Linq;
#if UWP
using Windows.Foundation;
#else
using System.Windows;
#endif

namespace Cirros.Primitives
{
    public partial class PrimitiveUtilities
    {
        public static List<List<Point>> Crosshatch(VectorEntity ve, CrosshatchPattern pattern, double scale, double rotation, bool nonZero = false)
        {
            List<List<Point>> pc = new List<List<Point>>();

            if (scale == 1 && rotation == 0)
            {
                foreach (CrosshatchPatternItem item in pattern.Items)
                {
                    List<List<Point>> line = Crosshatch(ve, item, nonZero);
                    if (line != null && line.Count > 0)
                    {
                        pc.AddRange(line);
                    }
                }
            }
            else
            {
                foreach (CrosshatchPatternItem i in pattern.Items)
                {
                    CrosshatchPatternItem item = new CrosshatchPatternItem(i, scale, rotation);
                    List<List<Point>> line = Crosshatch(ve, item, nonZero);
                    if (line != null && line.Count > 0)
                    {
                        pc.AddRange(line);
                    }
                }
            }

            return pc;
        }

        public static List<List<Point>> Crosshatch(VectorEntity ve, CrosshatchPatternItem pattern, bool nonZero)
        {
            List<double> nodeValues = new List<double>();
            List<List<Point>> xhpoints = new List<List<Point>>();
            List<List<Point>> boundary = new List<List<Point>>();

            if (pattern.Offset.Y == 0)
            {
                return xhpoints;
            }
            if (ve.ItemBox.IsEmpty)
            {
                return xhpoints;
            }

            double angle = pattern.Angle / Construct.cRadiansToDegrees;
            Point origin = pattern.Origin.ToPoint();
            Point offset = pattern.Offset.ToPoint();

            angle = -angle;
            origin.Y = -origin.Y;
            offset.Y = -offset.Y;

            foreach (object obj in ve.Children)
            {
                if (obj is List<Point> pc)
                {
                    List<Point> list = pc.ToList<Point>();
                    if (list[0] == list[list.Count - 1])
                    {
                        list.RemoveAt(list.Count - 1);
                    }
                    if (list.Count > 2)
                    {
                        boundary.Add(list);
                    }
                }
            }

            double sina = Math.Sin(angle);
            double cosa = Math.Cos(angle);

            double dx = offset.X * cosa - offset.Y * sina;
            double dy = offset.X * sina + offset.Y * cosa;

            Point pt1 = new Point(ve.ItemBox.Left, ve.ItemBox.Top);
            Point pt2 = new Point(ve.ItemBox.Right, ve.ItemBox.Top);
            Point pt3 = new Point(ve.ItemBox.Right, ve.ItemBox.Bottom);
            Point pt4 = new Point(ve.ItemBox.Left, ve.ItemBox.Bottom);

            Point pt0 = Construct.PolarOffset(origin, 1, angle);

            List<double> dl = new List<double>();
            dl.Add(Construct.DirectedDistancePointToLine(pt1, origin, pt0));
            dl.Add(Construct.DirectedDistancePointToLine(pt2, origin, pt0));
            dl.Add(Construct.DirectedDistancePointToLine(pt3, origin, pt0));
            dl.Add(Construct.DirectedDistancePointToLine(pt4, origin, pt0));
            dl.Sort();

            //int js = (int)((origin.Y + dl[0]) / offset.Y);
            //int je = (int)((origin.Y + dl[3]) / offset.Y);
            int js = (int)(dl[0] / offset.Y);
            int je = (int)(dl[3] / offset.Y);
            if (js < je)
            {
                --js;
            }
            else
            {
                js++;
            }

            List<double> dashArray = new List<double>();

            if (pattern.DashArray != null && pattern.DashArray.Count > 1)
            {
                double dd = 0;
                if (pattern.DashArray[0] < 0)
                {
                    origin = Construct.PolarOffset(origin, -pattern.DashArray[0], angle);

                    for (int i = 1; i < pattern.DashArray.Count; i++)
                    {
                        dd += Math.Abs(pattern.DashArray[i]);
                        dashArray.Add(dd);
                    }

                    dd += Math.Abs(pattern.DashArray[0]);
                    dashArray.Add(dd);
                }
                else
                {
                    foreach (double d in pattern.DashArray)
                    {
                        dd += Math.Abs(d);
                        dashArray.Add(dd);
                    }
                }
            }

            int reps = Math.Abs(je - js) + 1;
            int inc = js < je ? 1 : -1;
            int n = js;
            //System.Diagnostics.Debug.WriteLine("{0} -> {1}", js, je);

            for (int ii = 0; ii < reps; ii++)
            {
                n = js + ii * inc;

                double rx1 = origin.X + dx * n;
                double ry1 = origin.Y + dy * n;
                double rx0 = rx1 + cosa;
                double ry0 = ry1 + sina;

                nodeValues.Clear();

                foreach (List<Point> pc in boundary)
                {
                    if (pc.Count < 3)
                    {
                        continue;
                    }

                    Point p0 = new Point(rx0, ry0);
                    Point p1 = new Point(rx1, ry1);

                    int lr = Construct.WhichSide(p0, p1, pc[pc.Count - 2]);
                    int cr = Construct.WhichSide(p0, p1, pc[pc.Count - 1]);
                    int nr = Construct.WhichSide(p0, p1, pc[0]);
                    int nnr = Construct.WhichSide(p0, p1, pc[1]);

                    double  ex = pc[pc.Count - 1].X;
                    double  ey = pc[pc.Count - 1].Y;

                    for (int i = 0; i < pc.Count; i++)
                    {
                        int j = (i + 2) % pc.Count;

                        lr = cr;
                        cr = nr;
                        nr = nnr;
                        nnr = Construct.WhichSide(p0, p1, pc[j]);
                        //System.Diagnostics.Debug.WriteLine("{0},{1},{2},{3}   {4},{5}", lr, cr, nr, nnr, i, j);

                        double sx = ex;
                        double sy = ey;
                        ex = pc[i].X;
                        ey = pc[i].Y;

                        if (sx == ex && sy == ey)
                        {
                            continue;
                        }

                        //	(x3, y3) & (x4, y4) are the end points of the current boundary segment
                        //	RX will be the relative position of the intersection of the CH line with this segment 
                        //  If R1 is 0 they don't intersect
                        //	If RX is > 1. or < 0. the intersection is not on the segment
                        //	If RX is = 1. or = 0. the intersection is at an endpoint	

                        double rx = 0;
                        double r0 = 0;

                        double r1 = (sx - ex) * (ry1 - ry0) - (sy - ey) * (rx1 - rx0);
                        if (r1 != 0)
                        {
                            r0 = (ry1 - ry0) * (rx1 - ex) - (rx1 - rx0) * (ry1 - ey);
                            rx = r0 / r1;
                        }
                        else if (cr == 0)
                        {
                            rx = 0;
                        }
                        else
                        {
                            continue;
                        }

                        rx =  Math.Round(rx, 12);

                        //	If the boundary line segment doesn't intersect the CH line, ignore it
                        if (rx > 1)
                        {
                            continue;
                        }
                        else if (rx == 1)
                        {
                            //if (lr == nr)
                            {
                                continue;
                            }
                        }
                        else if (rx < 0)
                        {
                            continue;
                        }
                        else if (rx == 0)
                        {
                            //	If the intersection is at the endpoint of a boundary segment:
                            //		Ignore if the next & last boundary points are the on same side of the CH line
                            //		Ignore if the next & last boundary points are on the CH line 
                            //		Ignore if the current point is the beginning of boundary & the next point is also on the CH line

                            if (lr == nr)
                            {
                                continue;
                            }
                        }

                        r1 = (rx0 - rx1) * (ey - sy) - (ry0 - ry1) * (ex - sx);
                        if (r1 != 0)
                        {
                            r0 = (ey - sy) * (ex - rx1) - (ex - sx) * (ey - ry1);
                        }
                        else if (rx0 != rx1)
                        {
                            r0 = ex - rx1;
                            r1 = rx0 - rx1;
                        }
                        else
                        {
                            r0 = ey - ry1;
                            r1 = ry0 - ry1;
                        }

                        double r = r0 / r1;
                        if (rx != 0)
                        {
                            // Does not intersect at boundary vertex
                        }
                        else if (nr != 0 && (nr + lr) == 0)
                        {
                            // Does intersect, but last & next vertices are on opposite of CH line
                        }
                        else if (cr == 0 && lr == 0)
                        {
                            // Current & last point are on CH line and next & next to last are on the same side of the line -- ignore current
                            // Current & last point are on CH line and next & next to last are on opposite sides of the line -- ignore current
                            continue;
                        }
                        else if (cr == 0 && nr == 0 && lr == nnr)
                        {
                            // Current & next point are on CH line and last & next to next are on the same side of the line -- ignore current
                            continue;
                        }
                        else
                        {

                        }

                        nodeValues.Add(r);
                    }
                }

                if (nodeValues.Count > 1)
                {
                    nodeValues.Sort();

                    for (int i = 1; i < nodeValues.Count; i++)
                    {
                        if (nodeValues[i - 1] == nodeValues[i])
                        {
                            // coincident boundary intersections - remove?
                            nodeValues.RemoveAt(i);
                        }
                    }

                    if ((nodeValues.Count % 2) != 0)
                    {
                        // odd number of intersections - why?
                        nodeValues.RemoveAt(nodeValues.Count - 1);
                    }

                    if (nonZero && nodeValues.Count > 2)
                    {
                        double[] oldNodeValues = new double[nodeValues.Count];
                        nodeValues.CopyTo(oldNodeValues);
                        nodeValues.Clear();

                        double rs = oldNodeValues[0];
                        nodeValues.Add(rs);
                        nodeValues.Add(rs);

                        for (int i = 1; i < oldNodeValues.Length; i++)
                        {
                            double re = oldNodeValues[i];

                            Point s = new Point(rx0 * rs + rx1 * (1 - rs), ry0 * rs + ry1 * (1 - rs));
                            Point e = new Point(rx0 * re + rx1 * (1 - re), ry0 * re + ry1 * (1 - re));
                            Point m = Construct.Midpoint(s, e);
                            bool inside = false;
                            foreach (List<Point> b in boundary)
                            {
                                if (Construct.PointInPolygon(m, b))
                                {
                                    inside = true;
                                    break;
                                }
                            }

                            if (inside)
                            {
                                nodeValues[nodeValues.Count - 1] = re;
                            }
                            else
                            {
                                nodeValues.Add(re);
                                nodeValues.Add(re);
                            }
                            rs = re;
                        }
                    }

                    if (pattern.DashArray == null || pattern.DashArray.Count < 2)
                    {
                        // solid lines	

                        for (int j = 0; j < nodeValues.Count; j += 2)
                        {
                            double rs = nodeValues[j];
                            double re = nodeValues[j + 1];

                            Point s = new Point(rx0 * rs + rx1 * (1 - rs), ry0 * rs + ry1 * (1 - rs));
                            Point e = new Point(rx0 * re + rx1 * (1 - re), ry0 * re + ry1 * (1 - re));

                            //System.Diagnostics.Debug.WriteLine("({0}, {1}), ({2}, {3})", s.X, s.Y, e.X, e.Y);

                            List<Point> pc = new List<Point>();
                            pc.Add(s);
                            pc.Add(e);
                            xhpoints.Add(pc);
                        }
                    }
                    else
                    {
                        Point from = new Point();
                        Point to = new Point();

                        double patternLength = dashArray[dashArray.Count - 1];

                        for (int j = 0; j < nodeValues.Count; j += 2)
                        {
                            double rs = nodeValues[j];
                            double re = nodeValues[j + 1];

                            from.X = rx0 * rs + rx1 * (1 - rs);
                            from.Y = ry0 * rs + ry1 * (1 - rs);

                            double rb = patternLength * (long)(rs / patternLength);

                            if (rs < 0)
                            {
                                rb -= patternLength;
                            }

                            double ri = rs % patternLength;
                            int pi = 0;

                            for (int k = 0; k < dashArray.Count; ++k)
                            {
                                if (dashArray[k] > ri)
                                {
                                    pi = k;
                                    break;
                                }
                            }

                            while ((ri = rb + dashArray[pi]) < re)
                            {
                                if (ri >= rs)
                                {
                                    if ((pi % 2) == 0)
                                    {
                                        to.X = rx0 * ri + rx1 * (1 - ri);
                                        to.Y = ry0 * ri + ry1 * (1 - ri);

                                        List<Point> pc = new List<Point>();
                                        pc.Add(from);
                                        pc.Add(to);
                                        xhpoints.Add(pc);
                                    }
                                    else
                                    {
                                        from.X = rx0 * ri + rx1 * (1 - ri);
                                        from.Y = ry0 * ri + ry1 * (1 - ri);
                                    }
                                }

                                if (++pi >= dashArray.Count)
                                {
                                    rb += patternLength;
                                    pi = 0;
                                }
                            }

                            if ((pi % 2) == 0)
                            {
                                to.X = rx0 * re + rx1 * (1 - re);
                                to.Y = ry0 * re + ry1 * (1 - re);

                                List<Point> pc = new List<Point>();
                                pc.Add(from);
                                pc.Add(to);
                                xhpoints.Add(pc);
                            }
                        }
                    }
                }
            }
            return xhpoints;
        }
    }
}
