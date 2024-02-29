using System;
using Cirros.Primitives;
using System.Collections.Generic;
using Cirros.Core.Primitives;
#if UWP
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
#else
using System.Windows;
using CirrosCore;
using System.Windows.Media;
using static CirrosCore.WpfStubs;
#endif

namespace Cirros.Utility
{
    public class CGeometry
    {
        public static void BSplineSegment(List<Point> pc, Point p1, Point p2, Point p3, Point p4)
        {
            Point m1 = Construct.Midpoint(p1, p2);
            Point m2 = Construct.Midpoint(p2, p3);
            Point m3 = Construct.Midpoint(p3, p4);

            double d1 = Construct.Distance(m1, m2);
            double d2 = Construct.Distance(m2, m3);

            int divisions = (int)(d1 + d2);

            if (divisions < 40)
            {
                divisions = 40;
            }

            double[] a = new double[5];
            double[] b = new double[5];

            a[0] = (-p1.X + 3 * p2.X - 3 * p3.X + p4.X) / 6.0;
            a[1] = (3 * p1.X - 6 * p2.X + 3 * p3.X) / 6.0;
            a[2] = (-3 * p1.X + 3 * p3.X) / 6.0;
            a[3] = (p1.X + 4 * p2.X + p3.X) / 6.0;
            b[0] = (-p1.Y + 3 * p2.Y - 3 * p3.Y + p4.Y) / 6.0;
            b[1] = (3 * p1.Y - 6 * p2.Y + 3 * p3.Y) / 6.0;
            b[2] = (-3 * p1.Y + 3 * p3.Y) / 6.0;
            b[3] = (p1.Y + 4 * p2.Y + p3.Y) / 6.0;

            pc.Add(new Point(a[3], b[3]));

            for (int i = 1; i <= divisions; i++)
            {
                double t = (double)i / (double)divisions;
                pc.Add(new Point((a[2] + t * (a[1] + t * a[0])) * t + a[3], (b[2] + t * (b[1] + t * b[0])) * t + b[3]));
            }
        }

        public static PathGeometry BSplineGeometry(Point p1, Point p2, Point p3, Point p4)
        {
            PathGeometry bsplineGeometry = new PathGeometry();
            {
                PathFigure vFigure = new PathFigure();
                {
                    Point m1 = Construct.Midpoint(p1, p2);
                    Point m2 = Construct.Midpoint(p2, p3);
                    Point m3 = Construct.Midpoint(p3, p4);

                    double d1 = Construct.Distance(m1, m2);
                    double d2 = Construct.Distance(m2, m3);

                    int divisions = (int)(d1 + d2);

                    double[] a = new double[5];
                    double[] b = new double[5];

                    a[0] = (-p1.X + 3 * p2.X - 3 * p3.X + p4.X) / 6.0;
                    a[1] = (3 * p1.X - 6 * p2.X + 3 * p3.X) / 6.0;
                    a[2] = (-3 * p1.X + 3 * p3.X) / 6.0;
                    a[3] = (p1.X + 4 * p2.X + p3.X) / 6.0;
                    b[0] = (-p1.Y + 3 * p2.Y - 3 * p3.Y + p4.Y) / 6.0;
                    b[1] = (3 * p1.Y - 6 * p2.Y + 3 * p3.Y) / 6.0;
                    b[2] = (-3 * p1.Y + 3 * p3.Y) / 6.0;
                    b[3] = (p1.Y + 4 * p2.Y + p3.Y) / 6.0;
                    vFigure.StartPoint = new Point(a[3], b[3]);

                    PolyLineSegment polySegment = new PolyLineSegment();

                    if (p1.X == p2.X && p1.Y == p2.Y)
                    {
                        polySegment.Points.Add(p1);
                    }

                    for (int i = 1; i <= divisions; i++)
                    {
                        double t = (double)i / (double)divisions;
                        polySegment.Points.Add(new Point((a[2] + t * (a[1] + t * a[0])) * t + a[3], (b[2] + t * (b[1] + t * b[0])) * t + b[3]));
                    }

                    if (p3.X == p4.X && p3.Y == p4.Y)
                    {
                        polySegment.Points.Add(p4);
                    }

                    vFigure.Segments.Add(polySegment);
                }
                bsplineGeometry.Figures.Add(vFigure);
            }
            return bsplineGeometry;
        }

        public static List<Point> ArrowPointCollection(Point c, double radius, double start, double direction, ArrowType type, double length, double aspect)
        {
            double ainc = radius > 0 ? length / radius : .1;

            Point s = Construct.PolarOffset(c, radius, start);
            Point e = Construct.PolarOffset(c, radius, start + ainc * Math.Sign(direction));

            return ArrowPointCollection(s, e, type, length, aspect);
        }

        public static List<Point> ArrowPointCollection(Point s, Point e, ArrowType type, double length, double aspect)
        {
            List<Point> pc = new List<Point>();

            double width = length * aspect;
            double angle = Cirros.Utility.Construct.Angle(s, e) + Math.PI;

            double ldx = length * Math.Cos(angle);
            double ldy = length * Math.Sin(angle);
            double wdx = width * Math.Sin(angle);
            double wdy = width * Math.Cos(angle);

            double ax = s.X - ldx + wdx;
            double ay = s.Y - ldy - wdy;
            double bx = s.X - ldx - wdx;
            double by = s.Y - ldy + wdy;
            double cx = s.X - ldx;
            double cy = s.Y - ldy;

            Point a = new Point(ax, ay);
            Point b = new Point(bx, by);
            Point c = new Point(cx, cy);

            if (type == ArrowType.Ellipse || type == ArrowType.Dot)
            {
                pc = EllipsePointCollection(s, length / 2, length * aspect / 2, angle, 0, PI2, false, Matrix.Identity);
            }
            else if (type == ArrowType.Filled || type == ArrowType.Outline)
            {
                pc.Add(s);
                pc.Add(a);
                pc.Add(b);
                pc.Add(s);
            }
            else
            {
                pc.Add(a);
                pc.Add(s);
                pc.Add(b);
            }

            return pc;
        }

        public static PathGeometry ArrowGeometry(Point s, Point e, ArrowType type, double length, double aspect)
        {
            PathGeometry pathGeometry = new PathGeometry();

            List<Point> pc = new List<Point>();

            double width = length * aspect;
            double angle = Cirros.Utility.Construct.Angle(s, e) + Math.PI;

            double ldx = length * Math.Cos(angle);
            double ldy = length * Math.Sin(angle);
            double wdx = width * Math.Sin(angle);
            double wdy = width * Math.Cos(angle);
            double cx = s.X - ldx;
            double cy = s.Y - ldy;

            double ax = s.X - ldx + wdx;
            double ay = s.Y - ldy - wdy;
            double bx = s.X - ldx - wdx;
            double by = s.Y - ldy + wdy;

            Point a = new Point(ax, ay);
            Point b = new Point(bx, by);
            Point c = new Point(cx, cy);

            PathFigure vFigure = new PathFigure();
            {
                PolyLineSegment arrowSegment = new PolyLineSegment();

                if (type == ArrowType.Ellipse || type == ArrowType.Dot)
                {
                    pc = EllipsePointCollection(s, length / 2, length * aspect / 2, angle, 0, PI2, false, Matrix.Identity);
                    vFigure.StartPoint = pc[0];
                }
                else if (type == ArrowType.Filled || type == ArrowType.Outline)
                {
                    vFigure.StartPoint = s;
                    pc.Add(s);
                    pc.Add(a);
                    pc.Add(b);
                    pc.Add(s);
                }
                else
                {
                    vFigure.StartPoint = a;
                    pc.Add(s);
                    pc.Add(b);
                }

                arrowSegment.Points = PointCollectionFromList(pc);
                vFigure.Segments.Add(arrowSegment);

                vFigure.IsFilled = type == ArrowType.Filled || type == ArrowType.Dot;
            }
            pathGeometry.Figures.Add(vFigure);

            return pathGeometry;
        }

        public static PointCollection PointCollectionFromList(List<Point> points)
        {
            PointCollection pc = new PointCollection();
            foreach (Point point in points)
            {
                pc.Add(point);
            }
            return pc;
        }

        public static GeometryGroup ArrowGeometryGroup(double length, double angle, double size)
        {
            //TODO
            // Fix inconsistencty - ArrowGeometry renders at arrow position, ArrowGeometryGroup renders at origin
            GeometryGroup geomGroup = new GeometryGroup();

            double dotSize = 0;

            geomGroup.Children.Add(ArrowGeometry(new Point(length, 0), new Point(0, 0), ArrowType.Filled, size * 1.5, .25));

            LineGeometry shaftGeom = new LineGeometry();
            shaftGeom.StartPoint = new Point(dotSize, 0);
            shaftGeom.EndPoint = new Point(length, 0);
            geomGroup.Children.Add(shaftGeom);

            RotateTransform rtxf = new RotateTransform();
            rtxf.Angle = (angle * Construct.cRadiansToDegrees);
            geomGroup.Transform = rtxf;

            return geomGroup;
        }

        public static List<Point> TextBoxPointCollection(Point anchor, double length, double height, double offset, TextPosition position, TextAlignment alignment, double angle)
        {
            double dxa = 0;
            double dxb = length;

            if (length > height * 6)
            {
                double tl = height * 6;

                if (alignment == TextAlignment.Left)
                {
                    dxb = tl;
                }
                else
                {
                    double td = length - tl;

                    if (alignment == TextAlignment.Center)
                    {
                        dxa = td / 2;
                        dxb = length - dxa;
                    }
                    else
                    {
                        dxa = td;
                        dxb = length;
                    }
                }
            }

            double dyb;

            switch (position)
            {
                case TextPosition.On:
                    dyb = -height / 2;
                    break;

                case TextPosition.Below:
                    dyb = 0 + offset;
                    break;

                default:
                case TextPosition.Above:
                    dyb = -height - offset;
                    break;
            }

            double dya = dyb + height;

            List<Point> pc = new List<Point>();

            if (angle == 0)
            {
                pc.Add(new Point(dxa, dyb));
                pc.Add(new Point(dxb, dyb));
                pc.Add(new Point(dxb, dya));
                pc.Add(new Point(dxa, dya));
                pc.Add(new Point(dxa, dyb));
            }
            else
            {
                RotateTransform rtxf = new RotateTransform();
                rtxf.Angle = angle * Construct.cRadiansToDegrees;

                pc.Add(Utilities.TransformPoint(rtxf, new Point(dxa, dyb)));
                pc.Add(Utilities.TransformPoint(rtxf, new Point(dxb, dyb)));
                pc.Add(Utilities.TransformPoint(rtxf, new Point(dxb, dya)));
                pc.Add(Utilities.TransformPoint(rtxf, new Point(dxa, dya)));
                pc.Add(Utilities.TransformPoint(rtxf, new Point(dxa, dyb)));
            }

            for (int i = 0; i < pc.Count; i++)
            {
                Point p = pc[i];
                p.X += anchor.X;
                p.Y += anchor.Y;
                pc[i] = p;
            }

            return pc;
        }

        public static GeometryGroup TextBoxGeometryGroup(double length, double height, double offset, TextPosition position, TextAlignment alignment, double angle)
        {
            double dxa = 0;
            double dxb = length;

            if (length > height * 6)
            {
                double tl = height * 6;

                if (alignment == TextAlignment.Left)
                {
                    dxb = tl;
                }
                else
                {
                    double td = length - tl;

                    if (alignment == TextAlignment.Center)
                    {
                        dxa = td / 2;
                        dxb = length - dxa;
                    }
                    else
                    {
                        dxa = td;
                        dxb = length;
                    }
                }
            }

            double dyb;

            switch (position)
            {
                case TextPosition.On:
                    dyb = -height / 2;
                    break;

                case TextPosition.Below:
                    dyb = 0 + offset;
                    break;

                default:
                case TextPosition.Above:
                    dyb = -height - offset;
                    break;
            }

            double dya = dyb + height;

            GeometryGroup geomGroup = new GeometryGroup();

            LineGeometry btmGeom = new LineGeometry();
            btmGeom.StartPoint = new Point(dxa, dyb);
            btmGeom.EndPoint = new Point(dxb, dyb);
            geomGroup.Children.Add(btmGeom);

            LineGeometry farGeom = new LineGeometry();
            farGeom.StartPoint = new Point(dxb, dyb);
            farGeom.EndPoint = new Point(dxb, dya);
            geomGroup.Children.Add(farGeom);

            LineGeometry topGeom = new LineGeometry();
            topGeom.StartPoint = new Point(dxb, dya);
            topGeom.EndPoint = new Point(dxa, dya);
            geomGroup.Children.Add(topGeom);

            LineGeometry nearGeom = new LineGeometry();
            nearGeom.StartPoint = new Point(dxa, dya);
            nearGeom.EndPoint = new Point(dxa, dyb);
            geomGroup.Children.Add(nearGeom);

            RotateTransform rtxf = new RotateTransform();
            rtxf.Angle = angle * Construct.cRadiansToDegrees;
            geomGroup.Transform = rtxf;

            return geomGroup;
        }

        public static Rect ArcBox(Point c, double radius, double startAngle, double inclAngle, Matrix matrix)
        {
            Rect box = Rect.Empty;

            double a = startAngle;
            double r = radius;
            if (matrix.IsIdentity == false)
            {
                Point pr = matrix.Transform(new Point(radius, 0));
                r = Construct.Distance(new Point(), pr);
            }
            int n = ArcChordCount(r, inclAngle);
            double ainc = inclAngle / n;

            for (int j = 0; j <= n; ++j)
            {
                double dx = radius * Math.Cos(a);
                double dy = radius * Math.Sin(a);

                Point p = matrix.Transform(new Point(dx, dy));
                p.X += c.X;
                p.Y += c.Y;

                box.Union(p);

                a += ainc;
            }

            return box;
        }

        public static List<Point> ArcPointCollection(Point c, double radius, double startAngle, double inclAngle, bool wedge, Matrix matrix)
        {
            List<Point> pc = new List<Point>();

            double a = startAngle;
            double r = Math.Max(.1, radius);
            if (matrix.IsIdentity == false)
            {
                Point pr = matrix.Transform(new Point(r, 0));
                r = Construct.Distance(new Point(), pr);
            }
            int n = ArcChordCount(r, inclAngle);
            double ainc = inclAngle / n;

            if (wedge)
            {
                pc.Add(c);
            }

            for (int j = 0; j <= n; ++j)
            {
                double dx = radius * Math.Cos(a);
                double dy = radius * Math.Sin(a);

                Point p = matrix.Transform(new Point(dx, dy));
                p.X += c.X;
                p.Y += c.Y;

                pc.Add(p);

                a += ainc;
            }

            if (wedge)
            {
                pc.Add(c);
            }

            return pc;
        }

        public static List<Point> ArcPointCollection(Point s, Point m, Point e, bool wedge, Matrix matrix)
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

            return ArcPointCollection(c, radius, startAngle, inclAngle, wedge, matrix);
        }


        public static List<Point> CirclePointCollection(Point s, Point m, Point e, Matrix matrix)
        {
            Point c = Construct.Equidistant(s, m, e);

            double startAngle = Construct.Angle(c, s);
            double radius = Construct.Distance(c, s);
            double inclAngle = Construct.IncludedAngle(s, c, e);

            return ArcPointCollection(c, radius, startAngle, PI2, false, matrix);
        }

        public static PathGeometry ArcGeometry(Point c, double radius, double startAngle, double inclAngle, bool wedge, Matrix matrix)
        {
            double a = startAngle;
            double r = radius;
            if (matrix.IsIdentity == false)
            {
                Point pr = matrix.Transform(new Point(radius, 0));
                r = Construct.Distance(new Point(), pr);
            }
            int n = ArcChordCount(r, inclAngle);
            double ainc = inclAngle / n;
            PathGeometry arcGeometry = new PathGeometry();
            {
                PathFigure vFigure = new PathFigure();
                {
                    double dx = radius * Math.Cos(a);
                    double dy = radius * Math.Sin(a);
                    double x = c.X + dx;
                    double y = c.Y + dy;
                    a += ainc;

                    PolyLineSegment polySegment = new PolyLineSegment();

                    if (wedge)
                    {
                        vFigure.StartPoint = matrix.Transform(c);
                        polySegment.Points.Add(matrix.Transform(new Point(x, y)));
                    }
                    else
                    {
                        vFigure.StartPoint = matrix.Transform(new Point(x, y));
                    }

                    for (int j = 0; j < n; ++j)
                    {
                        dx = radius * Math.Cos(a);
                        dy = radius * Math.Sin(a);
                        x = c.X + dx;
                        y = c.Y + dy;
                        a += ainc;

                        polySegment.Points.Add(matrix.Transform(new Point(x, y)));
                    }

                    if (wedge)
                    {
                        polySegment.Points.Add(matrix.Transform(c));
                    }

                    vFigure.Segments.Add(polySegment);
                }
                arcGeometry.Figures.Add(vFigure);
            }
            return arcGeometry;
        }

        const double PI2 = Math.PI * 2;

        public static PathGeometry ArcGeometry(Point s, Point m, Point e, bool wedge, Matrix matrix)
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

            return ArcGeometry(c, radius, startAngle, inclAngle, wedge, matrix);
        }

        public static PathGeometry CircleGeometry(Point s, Point m, Point e, Matrix matrix)
        {
            Point c = Construct.Equidistant(s, m, e);

            double startAngle = Construct.Angle(c, s);
            double midAngle = Construct.Angle(c, m);
            double radius = Construct.Distance(c, s);
            double inclAngle = Construct.IncludedAngle(s, c, e);

            return ArcGeometry(c, radius, startAngle, PI2, false, matrix);
        }

        public static PathGeometry EllipseGeometry(Point center, double major, double minor,
            double axisAngle, double startAngle, double inclAngle, bool wedge, Matrix matrix)
        {
            double s = Math.Sin(-axisAngle);
            double c = Math.Cos(-axisAngle);
            double r = (major + minor) / 2;
            double angle = startAngle;
            if (matrix.IsIdentity == false)
            {
                Point pr = matrix.Transform(new Point(r, 0));
                r = Construct.Distance(new Point(), pr);
            }
            int n = ArcChordCount(r, inclAngle);
            double ainc = inclAngle / n;

            PathGeometry arcGeometry = new PathGeometry();
            {
                PathFigure vFigure = new PathFigure();
                {
                    double a = major * Math.Cos(angle);
                    double b = minor * Math.Sin(angle);
                    double x = center.X + a * c + b * s;
                    double y = center.Y - a * s + b * c;
                    angle += ainc;

                    PolyLineSegment polySegment = new PolyLineSegment();

                    if (wedge)
                    {
                        vFigure.StartPoint = matrix.Transform(center);
                        polySegment.Points.Add(matrix.Transform(new Point(x, y)));
                    }
                    else
                    {
                        vFigure.StartPoint = matrix.Transform(new Point(x, y));
                    }

                    for (int j = 0; j <= n; ++j)
                    {
                        a = major * Math.Cos(angle);
                        b = minor * Math.Sin(angle);
                        x = center.X + a * c + b * s;
                        y = center.Y - a * s + b * c;
                        angle += ainc;

                        polySegment.Points.Add(matrix.Transform(new Point(x, y)));
                    }

                    if (wedge)
                    {
                        polySegment.Points.Add(matrix.Transform(center));
                    }

                    vFigure.Segments.Add(polySegment);
                }
                arcGeometry.Figures.Add(vFigure);
            }
            return arcGeometry;
        }

        public static List<Point> EllipsePointCollection(Point center, double major, double minor,
            double axisAngle, double startAngle, double inclAngle, bool wedge, Matrix matrix)
        {
            if (inclAngle == 0)
            {
                inclAngle = Math.PI * 2;
            }

            double s = Math.Sin(-axisAngle);
            double c = Math.Cos(-axisAngle);
            double r = (major + minor) / 2;
            double angle = startAngle;
            if (matrix.IsIdentity == false)
            {
                Point pr = matrix.Transform(new Point(r, 0));
                r = Construct.Distance(new Point(), pr);
            }
            int n = ArcChordCount(r, inclAngle);
            double ainc = inclAngle / n;

            List<Point> pc = new List<Point>();

            if (wedge)
            {
                pc.Add(center);
            }

            for (int j = 0; j <= n; ++j)
            {
                double a = major * Math.Cos(angle);
                double b = minor * Math.Sin(angle);
                double x = center.X + a * c + b * s;
                double y = center.Y - a * s + b * c;
                angle += ainc;

                pc.Add(matrix.Transform(new Point(x, y)));
            }

            if (wedge)
            {
                pc.Add(center);
            }


            return pc;
        }

        public static double cCurveQuality = .03;
        public static int cMaxLineSegments = 256;

        public static int ArcChordCount(double radius, double inclAngle)
        {
#if UWP
            double cRadius = Globals.View.PaperToDisplay(radius);
#else
            double cRadius = CirrosCore.WpfStubs.PaperToDisplay(radius);
#endif
            double a1 = (cRadius - cCurveQuality) / cRadius;
	        double a2 = Math.Sqrt(1.0 - (a1 * a1));
	        double ainc = 2 * Math.Atan2(a2, a1);

	        int n = (int) Math.Round(Math.Abs(inclAngle / ainc));

            n = n < 1 ? 1 : n > cMaxLineSegments ? cMaxLineSegments : n;

            return n;
        }

        public static void JoinWalls(PDoubleline ps, uint segment, PDoubleline pv, uint vertex)
        {
            if (ps != null && pv != null && segment < ps.Points.Count)
            {
                if (ps.Id == 0)
                {
                    ps.Id = Globals.ActiveDrawing.NewObjectId();
                }
                if (pv.Id == 0)
                {
                    pv.Id = Globals.ActiveDrawing.NewObjectId();
                }

                CPoint jv = pv.GetVertex((int)vertex);

                if (vertex == 0)
                {
                    pv.JoinStart = ps.Id;
                }
                else if (vertex == pv.Points.Count)
                {
                    pv.JoinEnd = ps.Id;
                }
                else
                {
                    return;
                }
            }
        }

        public static List<CPoint> JoinWallPoints(PDoubleline ps, uint segment, List<WallJoint> nodes)
        {
            SortedDictionary<double, CPoint> sd = new SortedDictionary<double, CPoint>();

            foreach (WallJoint node in nodes)
            {
                if (node.FromDb != null)
                {
                    if (node.ToId == ps.Id)
                    {
                        if (node.ToSegment == segment)
                        {
                            Dictionary<double, CPoint> d0 = CGeometry.JoinWallPoints(node.ToDb, node.ToSegment, node.FromDb, node.FromVertex);
                            if (d0.Count == 2)
                            {
                                foreach (double key in d0.Keys)
                                {
                                    //sd.Add(key, d0[key]);
                                    sd[key] = d0[key];
                                }
                            }
                        }
                        else if (node.ToSegment > segment)
                        {
                            //break;
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine($"JoinWallPoints: ps.Id={ps.Id}; segment={segment}; count={sd.Values.Count}");
            return new List<CPoint>(sd.Values);
        }

        private static Dictionary<double, CPoint> JoinWallPoints(PDoubleline toDb, uint toSegment, PDoubleline fromDb, uint toVertex)
        {
            Dictionary<double, CPoint> cpd = new Dictionary<double, CPoint>();

            List<Point> fromPoints = fromDb.GetSegment((int)toVertex);
#if false
            double pv;
            Point p = fromPoints[0];
            int segment = toDb.PickSegment(ref p, out pv);
            if (segment >= 0)
            {
                toSegment = (uint)segment;
            }
#endif
            List<Point> toPoints = toDb.GetSegment((int)toSegment);

            if (fromPoints.Count == 2 && toPoints.Count == 2)
            {
                Point A = Construct.NormalPointToLine(fromPoints[0], toPoints[0], toPoints[1]);
                double d = Construct.Distance(A, fromPoints[0]);

                if (d < toDb.Width / 2)
                {
                    List<Point> v0 = new List<Point>();
                    List<Point> v1 = new List<Point>();
                    v0.Add(fromPoints[0]);
                    v0.Add(fromPoints[1]);
                    v1.Add(fromPoints[0]);
                    v1.Add(fromPoints[1]);
                    Construct.MoveLineParallel(v0, fromDb.Width / 2);
                    Construct.MoveLineParallel(v1, -fromDb.Width / 2);

                    Point i0, i1;
                    double pv0, pv1;
                    uint M = 3;

                    if (Construct.WhichSide(toPoints[0], toPoints[1], fromPoints[1]) > 0)
                    {
                        List<Point> s0 = new List<Point>();
                        s0.Add(toPoints[0]);
                        s0.Add(toPoints[1]);
                        Construct.MoveLineParallel(s0, toDb.Width / 2);

                        i0 = Construct.IntersectLineLine(s0[0], s0[1], v0[0], v0[1]);
                        i1 = Construct.IntersectLineLine(s0[0], s0[1], v1[0], v1[1]);
                        pv0 = Construct.PointValue(s0[0], s0[1], i0);
                        pv1 = Construct.PointValue(s0[0], s0[1], i1);
                        M = 2;
                    }
                    else
                    {
                        List<Point> s1 = new List<Point>();
                        s1.Add(toPoints[0]);
                        s1.Add(toPoints[1]);
                        Construct.MoveLineParallel(s1, -toDb.Width / 2);

                        i0 = Construct.IntersectLineLine(s1[0], s1[1], v0[0], v0[1]);
                        i1 = Construct.IntersectLineLine(s1[0], s1[1], v1[0], v1[1]);
                        pv0 = Construct.PointValue(s1[0], s1[1], i0);
                        pv1 = Construct.PointValue(s1[0], s1[1], i1);
                        M = 1;
                    }

                    i0 = Construct.NormalPointToLine(i0, toPoints[0], toPoints[1]);
                    i1 = Construct.NormalPointToLine(i1, toPoints[0], toPoints[1]);

                    if (pv1 > pv0)
                    {
                        if (pv0 > 0 && pv1 < 1)
                        {
                            cpd.Add(pv0, new CPoint(i0, 3));
                            cpd.Add(pv1, new CPoint(i1, M));
                        }
                    }
                    else
                    {
                        if (pv1 > 0 && pv0 < 1)
                        {
                            cpd.Add(pv1, new CPoint(i1, 3));
                            cpd.Add(pv0, new CPoint(i0, M));
                        }
                    }
                }
            }

            return cpd;
        }

        public static Matrix IdentityMatrix()
        {
#if UWP
            Matrix m = new Matrix();
            m = Matrix.Identity;
            return m;
#else
            return Matrix.Identity;
#endif
        }

        public static Matrix InvertMatrix(Matrix m)
        {
            double determinant = (m.M11 * m.M22) - (m.M12 * m.M21);

            if (determinant == 0.0)
            {
                return m;
            }

            double invdet = 1.0 / determinant;

            return new Matrix(
                m.M22 * invdet, -m.M12 * invdet,
                -m.M21 * invdet, m.M11 * invdet,
                (m.M21 * m.OffsetY - m.OffsetX * m.M22) * invdet,
                (m.OffsetX * m.M12 - m.M11 * m.OffsetY) * invdet);
        }

        public static Matrix MultiplyMatrix(Matrix matrix1, Matrix matrix2)
        {
            return new Matrix(
                matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21,
                matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22,

                matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21,
                matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22,

                matrix1.OffsetX * matrix2.M11 + matrix1.OffsetY * matrix2.M21 + matrix2.OffsetX,
                matrix1.OffsetX * matrix2.M12 + matrix1.OffsetY * matrix2.M22 + matrix2.OffsetY);
        }

        public static Matrix RotateMatrixAboutZ(Matrix m, double radians)
        {
            double s = Math.Sin(radians);
            double c = Math.Cos(radians);

            Matrix r = new Matrix(c, s, -s, c, 0, 0);
            return MultiplyMatrix(m, r);
        }

        public static Matrix RotateMatrixAboutZ(double degrees)
        {
            double radians = degrees / Construct.cRadiansToDegrees;

            double s = Math.Sin(radians);
            double c = Math.Cos(radians);

            return new Matrix(c, s, -s, c, 0, 0);
        }

        public static Matrix ScaleMatrix(Matrix m, double xscale, double yscale)
        {
            Matrix s = new Matrix(xscale, 0, 0, yscale, 0, 0);
            return MultiplyMatrix(m, s);
        }

        public static Matrix ScaleMatrix(double xscale, double yscale)
        {
            return new Matrix(xscale, 0, 0, yscale, 0, 0);
        }

        public static Matrix TranslateMatrix(Matrix m, double dx, double dy)
        {
            Matrix s = new Matrix(1, 0, dx, 1, 0, dy);
            return MultiplyMatrix(m, s);
        }
    }
}
