using System;
using System.Collections.Generic;
using System.Numerics;
#if UWP
using Windows.Foundation;
#else
using System.Windows;
#endif

namespace Cirros.Utility
{
    public class ConstructNode
    {
        public ConstructNode(Point location, string name)
        {
            Location = location;
            Name = name;
        }

        public void Move(double dx, double dy)
        {
            Location.X += dx;
            Location.Y += dy;
        }

        public Point Location;
        public string Name;
    }

    public class Construct
    {
        public const double cRadiansToDegrees = 57.295779513082320876798154814105;

        public static double Distance(Point s, Point e)
        {
            double dx = Math.Abs(e.X - s.X);
            double dy = Math.Abs(e.Y - s.Y);
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Angle(Point s, Point e)
        {
            double dx = e.X - s.X;
            double dy = e.Y - s.Y;
            return Math.Atan2(dy, dx);
        }

        public static Point PolarOffset(Point c, double distance, double angle)
        {
            Point p = new Point();

            p.X = c.X + distance * Math.Cos(angle);
            p.Y = c.Y + distance * Math.Sin(angle);

            return p;
        }

        public static Point OffsetAlongLine(Point start, Point end, double offset)
        {
            double pc = offset / Distance(start, end);
            return new Point(pc * end.X + (1 - pc) * start.X, pc * end.Y + (1 - pc) * start.Y);
        }

        public static Point Midpoint(Point a, Point b)
        {
            return new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
        }

        public static Point IntersectLineLine(Point a, Point b, Point c, Point d)
        {
            Point p = new Point();
            double dd = (a.X - b.X) * (d.Y - c.Y) - (a.Y - b.Y) * (d.X - c.X);
            if (dd == 0.0)
            {
                // Lines are parallel
                p = a;
            }
            else
            {
                double d0 = (d.Y - c.Y) * (d.X - b.X) - (d.X - c.X) * (d.Y - b.Y);
                double d1 = d0 / dd;
                p.X = d1 * a.X + (1.0 - d1) * b.X;
                p.Y = d1 * a.Y + (1.0 - d1) * b.Y;
            }
            return p;
        }

        public static bool IntersectLineLine(Point a, Point b, Point c, Point d, out Point p)
        {
            bool interescts = false;
            p = new Point();

            double dd = (a.X - b.X) * (d.Y - c.Y) - (a.Y - b.Y) * (d.X - c.X);
            if (dd == 0.0)
            {
                // Lines are parallel
            }
            else
            {
                double d0 = (d.Y - c.Y) * (d.X - b.X) - (d.X - c.X) * (d.Y - b.Y);
                double d1 = d0 / dd;
                p.X = d1 * a.X + (1.0 - d1) * b.X;
                p.Y = d1 * a.Y + (1.0 - d1) * b.Y;

                if (d1 >= 0 && d1 <= 1)
                {
                    // point is between a and b
                    interescts = p.X <= Math.Max(c.X, d.X) && p.X >= Math.Min(c.X, d.X) && p.Y <= Math.Max(c.Y, d.Y) && p.Y >= Math.Min(c.Y, d.Y);
                }
            }

            return interescts;
        }

        public static Point NormalPointToLine(Point p, Point s, Point e)
        {
            Point q = new Point(p.X + e.Y - s.Y, p.Y - e.X + s.X);
            return (IntersectLineLine(s, e, p, q));
        }

        public static double DistancePointToLine(Point p, Point s, Point e)
        {
            Point n = NormalPointToLine(p, s, e);
            double d = Distance(p, n);

            return (d);
        }

        public static double DirectedDistancePointToLine(Point p, Point s, Point e)
        {
            Point n = NormalPointToLine(p, s, e);
            double d = Distance(p, n);

            if (Construct.WhichSide(s, e, p) < 0)
            {
                d = -d;
            }

            return (d);
        }

        public static Point PointAlongLine(Point f, Point t, double distance)
        {	
	        double pc = distance / Distance(f, t);
            return new Point(pc * t.X + (1 - pc) * f.X, pc * t.Y + (1 - pc) * f.Y);
        }

        public static double DistancePointToLine(Point p, Point s, Point e, out Point n)
        {
            n = NormalPointToLine(p, s, e);
            double d = Distance(p, n);

            return (d);
        }
        
        public static double PointValue(Point s, Point e, Point p)
        {
            //	Returns the value of Point P on Line SE as a ratio of the directed distance from S to E.
            //	Does not check whether P is on SE.
            
            double d;
            
            if (s.X == e.X && s.Y == e.Y)
            {
                d = 0.0;
            }
            else if (Math.Abs(e.X - s.X) > Math.Abs(e.Y - s.Y))
            {
                d = (p.X - s.X) / (e.X - s.X);
            }
            else
            {
                d = (p.Y - s.Y) / (e.Y - s.Y);
            }

            d = Math.Round(d, 5);
            return d;
        }

        public static int WhichSide(Point a, Point b, Point p)
        {
            // Calculates on which side of line AB point P lies:
            //		1  :	Left
            //		0  :	On	
            //		-1 :	Right

            double c1 = (p.X - b.X) * (a.Y - b.Y);
            double c2 = (p.Y - b.Y) * (a.X - b.X);

            return Math.Sign(c1 - c2);
        }

        const double PI2 = 2 * Math.PI;

        public static double IncludedAngle(double start, double end, bool clockwise)
        {
            double included = end - start;

            if (clockwise)
            {
                if (included > PI2)
                {
                    included -= PI2;
                }
                else if (included < 0)
                {
                    included = PI2 + included;
                }
            }
            else
            {
                if (included < -PI2)
                {
                    included += PI2;
                }
                else if (included > 0)
                {
                    included = -PI2 + included;
                }
            }

            return included;
        }

        public static double MinorIncludedAngle(double start, double end)
        {
            double included = end - start;

            if (included > Math.PI)
            {
                included = -(PI2 - included);
            }
            else if (included < -Math.PI)
            {
                included = PI2 + included;
            }
            return included;
        }

        public static double IncludedAngle(Point s, Point c, Point e)
        {
            double startAngle = Angle(c, s);
            double endAngle = Angle(c, e);

            return MinorIncludedAngle(startAngle, endAngle);
        }

        public static Point Equidistant(Point j, Point k, Point l)
        {
            Point a = new Point((k.X + j.X) / 2, (k.Y + j.Y) / 2);
            Point b = new Point(a.X + k.Y - j.Y, a.Y - k.X + j.X);
            Point c = new Point((k.X + l.X) / 2, (k.Y + l.Y) / 2);
            Point d = new Point(c.X + k.Y - l.Y, c.Y - k.X + l.X);

            try
            {
                return Intersect(a, b, c, d);
            }
            catch
            {
                return k;
            }
        }

        public static Point Intersect(Point a, Point b, Point c, Point d)
        {
	        double d1 = (a.X - b.X) * (d.Y - c.Y) - (a.Y - b.Y) * (d.X - c.X);
	
            if (d1 == 0) 
            {
                //throw new Exception("Lines are parallel");
                return b;
            }
            else if (Math.Abs(d1) < .00001)
            {
                //throw new Exception("Lines are really close to being parallel");
                return b;
            }
		
            double d0 = (d.Y - c.Y) * (d.X - b.X) - (d.X - c.X) * (d.Y - b.Y);
            double r = d0 / d1;
		
            Point i = new Point(r * a.X + (1 - r) * b.X, r * a.Y + (1 - r) * b.Y);

            return i;
        }

        public static bool NormalArcToPoint(Point c, double radius, Point p, out Point n)
        {
            bool success = false;
            n = new Point();

	        double r0 = Construct.Distance(p, c);
            if (r0 > 0)
            {
	            double r1 = radius / r0;
	            r0 = 1 - r1;
	            n.X = r1 * p.X + r0 * c.X;
                n.Y = r1 * p.Y + r0 * c.Y;

                success = true;
            }

            return (success);
        }

        public static bool TangentArcToPoint(Point c, double radius, Point p, out Point t0, out Point t1)
        {
            bool success = false;

	        double r0 = Construct.Distance(p, c);
            if (r0 > 0)
            {
                double r1 = (Math.PI / 2) - Math.Asin((double)radius / r0);
                r0 = Construct.Angle(c, p) + r1;
                r1 = r0 - (2 * r1);
                t0 = PolarOffset(c, radius, r0);
                t1 = PolarOffset(c, radius, r1);

                success = true;
            }
            else
            {
                t0 = new Point();
                t1 = new Point();
            }

            return (success);
        }

        public static Point RoundXY(Point c)
        {
            Point m = Globals.ActiveDrawing.PaperToModel(c);

            Point r = new Point(
                ((int)Math.Round((m.X / Globals.xSnap)) * Globals.xSnap), 
                ((int)Math.Round((m.Y / Globals.ySnap)) * Globals.ySnap));

            return Globals.ActiveDrawing.ModelToPaper(r);
        }

        public static double DistancePointToPolyline(Point p, List<Point> pc)
        {
            double distance = double.MaxValue;

            Point s = pc[0];

            for (int i = 1; i < pc.Count; i++)
            {
                Point n;
                Point e = pc[i];
                double ds = Construct.DistancePointToLine(p, s, e, out n);
                if (ds < distance)
                {
                    double pv = Construct.PointValue(s, e, p);
                    if (pv >= 0 && pv <= 1)
                    {
                        distance = ds;
                    }
                }
                s = e;
            }

            return distance;
        }

        public static bool ArcIncludesAngle(double start, double included, double angle, double tolerance = 0)
        {
            bool includes = true;
            double PI2 = Math.PI * 2;

            if (included > 0)
            {
                while (start < 0)
                {
                    start += PI2;
                }
                while (angle < start)
                {
                    angle += PI2;
                }

                includes = (angle - start) <= included || Math.Abs(included - start) < tolerance;
            }
            else
            {
                while (start > 0)
                {
                    start -= PI2;
                }
                while (angle > start)
                {
                    angle -= PI2;
                }

                includes = (angle - start) >= included || Math.Abs(included - start) < tolerance;
            }

            return includes;
        }

        //public static void RoundAngle(double sx, double sy, ref double ex, ref double ey)
        //{
        //    double dx = Math.Abs(ex - sx);
        //    double dy = Math.Abs(ey - sy);

        //    if (dx != 0.0 && dy != 0.0)
        //    {
        //        double d = dy / dx;
        //        if (dx > dy)
        //        {
        //            if (d < .1)
        //            {
        //                ey = sy;
        //            }
        //        }
        //        else if ((1.0 / d) < .1)
        //        {
        //            ex = sx;
        //        }
        //    }
        //}

        public static void Parallel(Point a, Point b, double offset, out Point c, out Point d)
        {
            double angle = Angle(a, b) + Math.PI / 2;

            Point r = new Point(offset * Math.Cos(angle), offset * Math.Sin(angle));

            c = new Point(a.X + r.X, a.Y + r.Y);
            d = new Point(b.X + r.X, b.Y + r.Y);
        }
    
        public static void DoublelineSegment(Point a, Point b, Point c, Point d, double offset, out Point e, out Point f)
        {
            Point j, k, l, m;

            Parallel(a, b, offset, out j, out k);
            Parallel(b, c, offset, out l, out m);

            try
            {
                e = Intersect(j, k, l, m);
            }
            catch
            {
                e = k;
            }

            Parallel(c, d, offset, out j, out k);

            try
            {
                f = Intersect(l, m, j, k);
            }
            catch
            {
                f = m;
            }
        }

        public static void DoublelineEndSegment(Point a, Point b, Point c, double offset, out Point e, out Point f)
        {
            Point j, k, l, m;

            Parallel(a, b, offset, out j, out k);
            e = j;

            Parallel(b, c, offset, out l, out m);

            try
            {
                f = Intersect(j, k, l, m);
            }
            catch
            {
                f = k;
            }
        }

        public static List<Point> Reverse(List<Point> pc)
        {
            List<Point> rpc = new List<Point>();

            for (int i = 0; i < pc.Count; ++i)
            {
                rpc.Add(pc[pc.Count - i - 1]);
            }

            return rpc;
        }

        public static void MoveLineParallel(List<Point> pc, double offset)
        {
            Point a, b;
            Point i, j, k, l;

            bool open;

            if (pc.Count >= 2)
            {
                if (pc.Count > 2 && pc[0].X == pc[pc.Count - 1].X && pc[0].Y == pc[pc.Count - 1].Y)
                {
                    open = false;
                    l = new Point(pc[pc.Count - 2].X, pc[pc.Count - 2].Y);
                }
                else
                {
                    open = true;
                    l = new Point(pc[0].X, pc[0].Y);
                }

                j = new Point(l.X, l.Y);
                k = new Point(l.X, l.Y);

                for (int n = 0; n < pc.Count; n++)
                {
                    i = j;
                    j = k;
                    k = l;
                    l = new Point(pc[n].X, pc[n].Y);

                    if (n > 1)
                    {
                        if (n == 2 && open)
                        {
                            DoublelineEndSegment(j, k, l, offset, out a, out b);
                        }
                        else
                        {
                            DoublelineSegment(i, j, k, l, offset, out a, out b);
                        }
                        if (n == 2)
                        {
                            pc[0] = new Point(a.X, a.Y);
                        }
                        pc[n - 1] = new Point(b.X, b.Y);
                    }
                }

                if (open)
                {
                    Parallel(k, l, offset, out a, out b);
                    if (pc.Count == 2)
                    {
                        pc[0] = new Point(a.X, a.Y);
                    }
                }
                else
                {
                    b = new Point(pc[0].X, pc[0].Y);
                }

                pc[pc.Count - 1] = new Point(b.X, b.Y);
            }
        }

        //static double GetDeterminant(double x1, double y1, double x2, double y2)
        //{
        //    return x1 * y2 - x2 * y1;
        //}

        //static double GetArea(IList<Vertex> vertices)
        //{
        //    if(vertices.Count < 3)
        //    {
        //        return 0;
        //    }
        //    double area = GetDeterminant(vertices[vertices.Count - 1].X, vertices[vertices.Count - 1].Y, vertices[0].X, vertices[0].Y);
        //    for (int i = 1; i < vertices.Count; i++)
        //    {
        //        area += GetDeterminant(vertices[i - 1].X, vertices[i - 1].Y, vertices[i].X, vertices[i].Y);
        //    }
        //    return area / 2;
        //}

        public static void AreaPerimeter(List<Point> pc, out double area, out double perimeter)
        {
	        perimeter = 0;

	        double areaCW = pc[pc.Count - 1].X * pc[0].Y;
            double areaCC = pc[pc.Count - 1].Y * pc[0].X;

            for (int i = 0; i < pc.Count - 1; i++)
            {
		        areaCW += pc[i].X * pc[i + 1].Y;
                areaCC += pc[i].Y * pc[i + 1].X;
                perimeter += Construct.Distance(pc[i], pc[i + 1]);
	        }

	        area = Math.Abs(areaCC - areaCW) / 2;
            perimeter += Construct.Distance(pc[0], pc[pc.Count - 1]);
        }

        public static void FilletPoints(Point start, Point vertex, Point end, double radius, out Point center, out double startAngle, out double includedAngle)
        {
            double angle = Construct.IncludedAngle(start, vertex, end);

            int side = WhichSide(start, vertex, end);

            if (side == 0)
            {
                center = new Point();
                startAngle = 0;
                includedAngle = 0;
            }
            else
            {
                double offset = side * radius;

                Point s1, v1;
                Point v2, e2;

                Parallel(start, vertex, offset, out s1, out v1);
                Parallel(vertex, end, offset, out v2, out e2);

                try
                {
                    center = Intersect(s1, v1, v2, e2);
                    if (center == v1)
                    {
                        center = new Point();
                        startAngle = 0;
                        includedAngle = 0;
                    }
                    else
                    {
                        Point arcStart = NormalPointToLine(center, start, vertex);
                        Point arcEnd = NormalPointToLine(center, vertex, end);

                        startAngle = Angle(center, arcStart);
                        includedAngle = IncludedAngle(arcStart, center, arcEnd);
                    }
                }
                catch
                {
                    center = new Point();
                    startAngle = 0;
                    includedAngle = 0;
                }
            }
        }

        public static void FilletPoints(Point start, Point vertex, Point end, out double radius, out Point center, out double startAngle, out double includedAngle)
        {
            double angle = Construct.IncludedAngle(start, vertex, end);
            double distance = Distance(vertex, start);
            radius = Math.Abs(Math.Tan(angle / 2) * distance);

            int side = WhichSide(start, vertex, end);

            if (side == 0)
            {
                center = new Point();
                startAngle = 0;
                includedAngle = 0;
            }
            else
            {
                double offset = side * radius;

                Point s1, v1;
                Point v2, e2;

                Parallel(start, vertex, offset, out s1, out v1);
                Parallel(vertex, end, offset, out v2, out e2);

                try
                {
                    center = Intersect(s1, v1, v2, e2);

                    Point arcStart = NormalPointToLine(center, start, vertex);
                    Point arcEnd = NormalPointToLine(center, vertex, end);

                    startAngle = Angle(center, arcStart);
                    includedAngle = IncludedAngle(arcStart, center, arcEnd);
                }
                catch
                {
                    center = new Point();
                    startAngle = 0;
                    includedAngle = 0;
                }
            }
        }

        private const int csInside  = 0;    // 0000
        private const int csLeft    = 1;    // 0001
        private const int csRight   = 2;    // 0010
        private const int csBottom  = 4;    // 0100
        private const int csTop     = 8;    // 1000
 
        private static int csComputeOutCode(Point p, Rect r)
        {
	        int code = csInside;
 
	        if ((r.Left - p.X) > .0001) // if (p.X < r.Left)
            {
                code |= csLeft;
            }
            else if ((p.X - r.Right) > .0001) // if (p.X > r.Right)
            {
                code |= csRight;
            }

	        if ((r.Top - p.Y) > .0001)  // if (p.Y < r.Top)
            {
                code |= csBottom;
            }
            else if ((p.Y - r.Bottom) > .0001) // if (p.Y > r.Bottom)
            {
                code |= csTop;
            }
 
	        return code;
        }
 
        public static bool Clip(ref Point a, ref Point b, Rect r)
        {
            // Cohen–Sutherland clipping algorithm clips a line from a to b against rectangle r

	        int outcodeA = csComputeOutCode(a, r);
            int outcodeB = csComputeOutCode(b, r);
	        bool accept = false;
 
	        while (true)
            {
		        if ((outcodeA | outcodeB) == 0)
                {
                    // Bitwise OR is 0. Trivially accept and get out of loop
			        accept = true;
			        break;
		        }
                else if ((outcodeA & outcodeB) != 0)
                { 
                    // Bitwise AND is not 0. Trivially reject and get out of loop
                    // Constrain a and b to the edges and return false (for clipping filled boundaries)

                    // (this part doesn't work yet)
                    if (outcodeA != 0)
                    {
                        if ((outcodeA & csTop) != 0)
                        {
                            a.Y = r.Bottom;
                        }
                        else if ((outcodeA & csBottom) != 0)
                        {
                            a.Y = r.Top;
                        }
                        if ((outcodeA & csRight) != 0)
                        {
                            a.X = r.Right;
                        }
                        else if ((outcodeA & csLeft) != 0)
                        {
                            a.X = r.Left;
                        }
                    }
                    if (outcodeB != 0)
                    {
                        if ((outcodeB & csTop) != 0)
                        {
                            b.Y = r.Bottom;
                        }
                        else if ((outcodeB & csBottom) != 0)
                        {
                            b.Y = r.Top;
                        }
                        if ((outcodeB & csRight) != 0)
                        {
                            b.X = r.Right;
                        }
                        else if ((outcodeB & csLeft) != 0)
                        {
                            b.X = r.Left;
                        }
                    }
                    break;
                }
                else
                {
			        // failed both tests, so calculate the line segment to clip
			        // from an outside point to an intersection with clip edge
			        Point p = new Point();
 
			        // At least one endpoint is outside the clip rectangle; pick it.
			        int outcodeOut = outcodeA != 0 ? outcodeA : outcodeB;
 
			        // Now find the intersection point;
                    // use formulas y =  a.Y + slope * (x - a.X); x = a.X + (1 / slope) * (y -  a.Y);
			        if ((outcodeOut & csTop) != 0)
                    { 
                        // point is above the clip rectangle
                        p.X = a.X + (b.X - a.X) * (r.Bottom - a.Y) / (b.Y - a.Y);
                        p.Y = r.Bottom;
			        }
                    else if ((outcodeOut & csBottom) != 0)
                    {
                        // point is below the clip rectangle
                        p.X = a.X + (b.X - a.X) * (r.Top - a.Y) / (b.Y - a.Y);
                        p.Y = r.Top;
			        }
                    else if ((outcodeOut & csRight) != 0)
                    { 
                        // point is to the right of clip rectangle
                        p.Y = a.Y + (b.Y - a.Y) * (r.Right - a.X) / (b.X - a.X);
                        p.X = r.Right;
			        }
                    else if ((outcodeOut & csLeft) != 0)
                    {  
                        // point is to the left of clip rectangle
                        p.Y = a.Y + (b.Y - a.Y) * (r.Left - a.X) / (b.X - a.X);
                        p.X = r.Left;
			        }
 
			        // Now we move outside point to intersection point to clip
			        // and get ready for next pass.
			        if (outcodeOut == outcodeA)
                    {
                        a = p;
                        outcodeA = csComputeOutCode(a, r);
			        }
                    else
                    {
                        b = p;
                        outcodeB = csComputeOutCode(b, r);
			        }
		        }
	        }
            return accept;
        }

        internal static bool IntersectLineArc(Point from, Point through, Point center, double radius, out Point i0, out Point i1)
        {
            bool doesIntersect = true;

            double dx = through.X - from.X;
            double dy = through.Y - from.Y;
            double A = dx * dx + dy * dy;
            double B = 2 * (dx * (from.X - center.X) + dy * (from.Y - center.Y));
            double C = (from.X - center.X) * (from.X - center.X) + (from.Y - center.Y) * (from.Y - center.Y) - radius * radius;
            double det = B * B - 4 * A * C;

            if (A <= 0.0000001 || det < 0) 
            {
                // No intersection
                i0 = i1 = new Point(double.NaN, double.NaN);
                doesIntersect = false;
            } 
            else if (det == 0) 
            {
                // Intersects at tangent point
                double t = -B / (2 * A);
                i0 = new Point(from.X + t * dx, from.Y + t * dy);
                i1 = i0;
            }
            else 
            {   
                // Intesects at two points  
                double t0 = (float)((-B + Math.Sqrt(det)) / (2 * A));
                i0 = new Point(from.X + t0 * dx, from.Y + t0 * dy); 

                double t1 = (float)((-B - Math.Sqrt(det)) / (2 * A));
                i1 = new Point(from.X + t1 * dx, from.Y + t1 * dy);
            }

            return doesIntersect;
        }

        public static bool PointInsideTriangle(Point p, Point a, Point b, Point c)
        {
            Vector2 P = new Vector2((float)p.X, (float)p.Y);
            Vector2 A = new Vector2((float)a.X, (float)a.Y);
            Vector2 B = new Vector2((float)b.X, (float)b.Y);
            Vector2 C = new Vector2((float)c.X, (float)c.Y);

            // Compute vectors        
            Vector2 v0 = C - A;
            Vector2 v1 = B - A;
            Vector2 v2 = P - A;

            // Compute dot products
            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            // Compute barycentric coordinates
            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // Check if point is in triangle
            return (u >= 0) && (v >= 0) && (u + v < 1);
        }
#if true
        public static bool PointInPolygon(Point p, List<Point> pc)
        {
            double angle = 0;
            Point p1 = new Point();
            Point p2 = new Point();

            for (int i = 0; i < pc.Count; i++)
            {
                p1.X = pc[i].X - p.X;
                p1.Y = pc[i].Y - p.Y;
                p2.X = pc[(i + 1) % pc.Count].X - p.X;
                p2.Y = pc[(i + 1) % pc.Count].Y - p.Y;

                angle += Angle2D(p1.X, p1.Y, p2.X, p2.Y);
            }

            return Math.Abs(angle) >= Math.PI;
        }
#else
        // from StackOverflow

        public static bool PointInPolygon(Point p, List<Point> poly)
        {
            Point p1, p2;
            bool inside = false;

            if (poly.Length < 3)
            {
                return inside;
            }

            Point oldPoint = new Point(poly[poly.Length - 1].X, poly[poly.Length - 1].Y);

            for (int i = 0; i < poly.Length; i++)
            {
                Point newPoint = new Point(poly[i].X, poly[i].Y);

                if (newPoint.X > oldPoint.X)
                {
                    p1 = oldPoint;
                    p2 = newPoint;
                }
                else
                {
                    p1 = newPoint;
                    p2 = oldPoint;
                }

                if ((newPoint.X < p.X) == (p.X <= oldPoint.X)
                        && ((long)p.Y - (long)p1.Y) * (long)(p2.X - p1.X) < ((long)p2.Y - (long)p1.Y) * (long)(p.X - p1.X))
                {
                    inside = !inside;
                }

                oldPoint = newPoint;
            }

            return inside;
        }
#endif
        /*
           Return the angle between two vectors on a plane
           The angle is from vector 1 to vector 2, positive anticlockwise
           The result is between -pi -> pi
        */
        public static double Angle2D(double x1, double y1, double x2, double y2)
        {
            double dtheta, theta1, theta2;

            theta1 = Math.Atan2(y1, x1);
            theta2 = Math.Atan2(y2, x2);
            dtheta = theta2 - theta1;

            while (dtheta > Math.PI)
            {
                dtheta -= (Math.PI + Math.PI);
            }

            while (dtheta < -Math.PI)
            {
                dtheta += Math.PI + Math.PI;
            }

            return (dtheta);
        }
    }
}
