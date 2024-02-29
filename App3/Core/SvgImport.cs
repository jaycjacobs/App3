using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Svg;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Windows.Foundation;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Cirros
{
    public class SvgImport
    {
        SvgDom _dom = null;
        List<string> _missingFonts = new List<string>();

        public async Task<int> CreateDrawing(SvgDom dom)
        {
            _dom = dom;

            _missingFonts.Clear();

            foreach (string imageId in _dom.ImageDictionary.Keys)
            {
                await Utilities.CreateTemporaryImageFromUriAsync(imageId, Globals.TemporaryImageFolder, _dom.ImageDictionary[imageId]);
            }

            Unit paperUnit = _dom.Unit == "in" ? Unit.Inches : Unit.Millimeters;
            Unit modelUnit = paperUnit;
            double drawingScale = 1;

            Globals.ActiveDrawing.NewEmptyDrawing(_dom.Size.Width, _dom.Size.Height, drawingScale, paperUnit, modelUnit, false, Globals.Theme);

            if (_dom.Entities.Count > 0 && _dom.Entities[0] is SvgRectType)
            {
                SvgRectType brect = _dom.Entities[0] as SvgRectType;
                if (Math.Round(brect.Width, 5) == Math.Round(_dom.Size.Width, 5) && Math.Round(brect.Height, 5) == Math.Round(_dom.Size.Height, 5) && brect.Fill)
                {
                    Theme theme = null;
                    int difference = 5 * 3;

                    foreach (Theme t in Globals.Themes.Values)
                    {
                        int d = Math.Abs(t.BackgroundColor.R - brect.FillColor.R) + Math.Abs(t.BackgroundColor.G - brect.FillColor.G) + Math.Abs(t.BackgroundColor.B - brect.FillColor.B);
                        if (d < difference)
                        {
                            difference = d;
                            theme = t;

                            if (difference == 0)
                            {
                                break;
                            }
                        }
                    }

                    if (theme != null)
                    {
                        Globals.ActiveDrawing.Theme = theme;
                        _dom.Entities.RemoveAt(0);
                    }
                }
            }
            AddEntities(_dom, null);

            return 1;
        }

        public List<string> MissingFonts
        {
            get
            {
                return _missingFonts;
            }
        }

        private bool TransformIncludesTranslate(Transform tf)
        {
            bool translate = false;

            if (tf != null)
            {
                Point p0 = tf.TransformPoint(new Point(0, 0));
                translate = p0.X != 0 || p0.Y != 0;
            }

            return translate;
        }

        private bool TransformIsComplexTranslate(Transform tf)
        {
            bool complex = false;

            if (tf != null)
            {
                Point p0 = tf.TransformPoint(new Point(0, 0));
                if (p0.X != 0 || p0.Y != 0)
                {
                    // transform includes translation
                    Point p1 = tf.TransformPoint(new Point(1, 1));
                    if ((p1.X - p0.X) != 1 || (p1.Y - p0.Y) != 1)
                    {
                        // transform includes scaling or rotation
                        complex = true;
                    }
                }
            }

            return complex;
        }

        private double TransformScalar(Transform tf, double scalar)
        {
            if (tf is ScaleTransform)
            {
                ScaleTransform ts = (ScaleTransform)tf;
                if (ts.ScaleX == ts.ScaleY)
                {
                    scalar = scalar * ts.ScaleY;
                }
                else
                {
                    double scale = Math.Sqrt(ts.ScaleX * ts.ScaleX + ts.ScaleY * ts.ScaleY);
                    scalar = scalar * scale;
                }
            }
            else if (tf is MatrixTransform)
            {
                Matrix m = ((MatrixTransform)tf).Matrix;
                scalar = Math.Abs(scalar * m.M11) + Math.Abs(scalar * m.M12);
            }
            else if (tf is TransformGroup)
            {
                Matrix m = ((TransformGroup)tf).Value;
                scalar = Math.Abs(scalar * m.M11) + Math.Abs(scalar * m.M12);

            }

            return scalar;
        }

        private void AddEntities(ISvgContainer container, Group parent)
        {
            Point ctp = new Point();

            foreach (SvgShapeType shape in container.Entities)
            {
                if (shape.dbHint == "background")
                {
                    continue;
                }

                Primitive primitive = null;
                List<CPoint> cpoints = null;
                List<Point> points = null;

                bool isComplexTranslateTransform = TransformIsComplexTranslate(shape.Transform);

                if (shape is SvgCircleType)
                {
                    SvgCircleType circle = shape as SvgCircleType;

                    if (isComplexTranslateTransform)
                    {
                        points = CGeometry.ArcPointCollection(new Point(circle.Cx, circle.Cy), circle.R, 0, Math.PI * 2, false, Matrix.Identity);
                    }
                    else
                    {
                        primitive = new PArc(new Point(circle.Cx, circle.Cy), circle.R, 0, 0);
                    }
                }
                else if (shape is SvgEllipseType)
                {
                    SvgEllipseType ellipse = shape as SvgEllipseType;

                    if (isComplexTranslateTransform)
                    {
                        points = CGeometry.EllipsePointCollection(new Point(ellipse.Cx, ellipse.Cy), ellipse.Rx, ellipse.Ry, 0, 0, 2 * Math.PI, false, Matrix.Identity);
                    }
                    else
                    {
                        primitive = new PEllipse(new Point(ellipse.Cx, ellipse.Cy), ellipse.Rx, ellipse.Ry, 0, 0, 2 * Math.PI);
                    }
                }
                else if (shape is SvgLineType)
                {
                    SvgLineType line = shape as SvgLineType;

                    cpoints = new List<CPoint>();
                    cpoints.Add(new CPoint(new Point(line.X1, line.Y1), 1));
                    cpoints.Add(new CPoint(new Point(line.X2, line.Y2), 1));

                    // create line
                    //primitive = new PLine(new Point(line.X1, line.Y1), new Point(line.X2, line.Y2));
                }
                else if (shape is SvgPathType)
                {
                    SvgPathType path = shape as SvgPathType;

                    // create path

                    cpoints = new List<CPoint>();

                    Point cp = new Point(0, 0);
                    Point subpathOrigin = new Point(0, 0);

                    Point q1 = new Point(0, 0);
                    Point q2 = new Point(0, 0);
                    Point q3 = new Point(0, 0);

                    Point c1 = new Point(0, 0);
                    Point c2 = new Point(0, 0);
                    Point c3 = new Point(0, 0);
                    Point c4 = new Point(0, 0);

                    foreach (SvgPathData d in path.D)
                    {
                        switch (d.Command)
                        {
                            case "z":
                            case "Z":
                                // close path
                                cpoints.Add(new CPoint(subpathOrigin, 0));
                                break;

                            case "m":   // relative move
                                for (int i = 0; i < d.Data.Count; i += 2)
                                {
                                    double dx = d.Data[i];
                                    double dy = d.Data[i + 1];
                                    cp.X += dx;
                                    cp.Y += dy;

                                    if (i == 0)
                                    {
                                        subpathOrigin.X = cp.X;
                                        subpathOrigin.Y = cp.Y;
                                        cpoints.Add(new CPoint(subpathOrigin, 0));
                                    }
                                    else
                                    {
                                        cpoints.Add(new CPoint(cp, 1));
                                    }
                                }
                                break;

                            case "M":   // absolute move
                                for (int i = 0; i < d.Data.Count; i += 2)
                                {
                                    cp.X = d.Data[i];
                                    cp.Y = d.Data[i + 1];

                                    if (i == 0)
                                    {
                                        subpathOrigin.X = cp.X;
                                        subpathOrigin.Y = cp.Y;
                                        cpoints.Add(new CPoint(subpathOrigin, 0));
                                    }
                                    else
                                    {
                                        cpoints.Add(new CPoint(cp, 1));
                                    }
                                }
                                break;

                            case "h":   // relative draw
                                {
                                    // the SVG spec allows for repeated values - ignore all but the last one
                                    double dx = d.Data[d.Data.Count - 1];
                                    cp.X += dx;

                                    cpoints.Add(new CPoint(cp, 1));
                                }
                                break;

                            case "H":   // absolute draw
                                {
                                    // the SVG spec allows for repeated values - ignore all but the last one
                                    cp.X = d.Data[d.Data.Count - 1];

                                    cpoints.Add(new CPoint(cp, 1));
                                }
                                break;

                            case "v":   // relative draw
                                {
                                    // the SVG spec allows for repeated values - ignore all but the last one
                                    double dy = d.Data[d.Data.Count - 1];
                                    cp.Y += dy;

                                    cpoints.Add(new CPoint(cp, 1));
                                }
                                break;

                            case "V":   // absolute draw
                                {
                                    // the SVG spec allows for repeated values - ignore all but the last one
                                    cp.Y = d.Data[d.Data.Count - 1];

                                    cpoints.Add(new CPoint(cp, 1));
                                }
                                break;

                            case "l":   // relative draw
                                for (int i = 0; i < d.Data.Count; i += 2)
                                {
                                    double dx = d.Data[i];
                                    double dy = d.Data[i + 1];
                                    cp.X += dx;
                                    cp.Y += dy;

                                    cpoints.Add(new CPoint(cp, 1));
                                }
                                break;

                            case "L":   // absolute draw
                                for (int i = 0; i < d.Data.Count; i += 2)
                                {
                                    cp.X = d.Data[i];
                                    cp.Y = d.Data[i + 1];

                                    cpoints.Add(new CPoint(cp, 1));
                                }
                                break;

                            case "t":   // relative quadratic bezier

                                // Draws a quadratic Bézier curve from the current point to (x,y). 
                                // The control point is assumed to be the reflection of the control point on the previous command relative to the current point. 
                                // (If there is no previous command or if the previous command was not a Q, q, T or t, assume the control point is coincident with the current point.) 
                                // T (uppercase) indicates that absolute coordinates will follow; 
                                // t (lowercase) indicates that relative coordinates will follow. At the end of the command, 
                                // the new current point becomes the final (x,y) coordinate pair used in the polybézier. 

                                for (int i = 0; i < d.Data.Count; i += 2)
                                {
                                    double dx = d.Data[i];
                                    double dy = d.Data[i + 1];
                                    double x2 = cp.X + dx;
                                    double y2 = cp.Y + dy;

                                    if (cp.X == q3.X && cp.Y == q3.Y)
                                    {
                                        // control point: reflection of the control point on the previous point
                                        q2 = Reflect(q1, q3, q2, new Point(x2, y2));
                                    }
                                    else
                                    {
                                        // no previous control point - use the current point
                                        q2 = q1;
                                    }

                                    q1 = cp;
                                    q3 = new Point(x2, y2);

                                    List<Point> pc = QuadraticBezierSegment(q1, q2, q3);

                                    foreach (Point p in pc)
                                    {
                                        cpoints.Add(new CPoint(p, 1));
                                    }

                                    cp.X = x2;
                                    cp.Y = y2;
                                }
                                break;

                            case "T":   // absolute quadratic bezier

                                // Draws a quadratic Bézier curve from the current point to (x,y). 
                                // The control point is assumed to be the reflection of the control point on the previous command relative to the current point. 
                                // (If there is no previous command or if the previous command was not a Q, q, T or t, assume the control point is coincident with the current point.) 
                                // T (uppercase) indicates that absolute coordinates will follow; 
                                // t (lowercase) indicates that relative coordinates will follow. At the end of the command, 
                                // the new current point becomes the final (x,y) coordinate pair used in the polybézier. 

                                for (int i = 0; i < d.Data.Count; i += 2)
                                {
                                    double x2 = d.Data[i];
                                    double y2 = d.Data[i + 1];

                                    if (cp.X == q3.X && cp.Y == q3.Y)
                                    {
                                        // control point: reflection of the control point on the previous point
                                        q2 = Reflect(q1, q3, q2, new Point(x2, y2));
                                    }
                                    else
                                    {
                                        // no previous control point - use the current point
                                        q2 = q1;
                                    }

                                    q1 = cp;
                                    q3 = new Point(x2, y2);

                                    List<Point> pc = QuadraticBezierSegment(q1, q2, q3);

                                    foreach (Point p in pc)
                                    {
                                        cpoints.Add(new CPoint(p, 1));
                                    }

                                    cp.X = x2;
                                    cp.Y = y2;
                                }
                                break;

                            case "q":   // relative quadratic bezier

                                // Draws a quadratic Bézier curve from the current point to (x,y) using (x1,y1) as the control point. 
                                // Q (uppercase) indicates that absolute coordinates will follow; 
                                // q (lowercase) indicates that relative coordinates will follow.
                                // Multiple sets of coordinates may be specified to draw a polybézier. At the end of the command, 
                                // the new current point becomes the final (x,y) coordinate pair used in the polybézier.

                                for (int i = 0; i < d.Data.Count; i += 4)
                                {
                                    double x1 = cp.X + d.Data[i];
                                    double y1 = cp.Y + d.Data[i + 1];
                                    double x2 = cp.X + d.Data[i + 2];
                                    double y2 = cp.Y + d.Data[i + 3];

                                    q1 = cp;
                                    q2 = new Point(x1, y1);
                                    q3 = new Point(x2, y2);

                                    List<Point> pc = QuadraticBezierSegment(q1, q2, q3);

                                    foreach (Point p in pc)
                                    {
                                        cpoints.Add(new CPoint(p, 1));
                                    }

                                    cp.X = x2;
                                    cp.Y = y2;
                                }
                                break;

                            case "Q":   // absolute quadratic bezier

                                // Draws a quadratic Bézier curve from the current point to (x,y) using (x1,y1) as the control point. 
                                // Q (uppercase) indicates that absolute coordinates will follow; 
                                // q (lowercase) indicates that relative coordinates will follow.
                                // Multiple sets of coordinates may be specified to draw a polybézier. At the end of the command, 
                                // the new current point becomes the final (x,y) coordinate pair used in the polybézier.

                                for (int i = 0; i < d.Data.Count; i += 4)
                                {
                                    double x1 = d.Data[i];
                                    double y1 = d.Data[i + 1];
                                    double x2 = d.Data[i + 2];
                                    double y2 = d.Data[i + 3];

                                    q1 = cp;
                                    q2 = new Point(x1, y1);
                                    q3 = new Point(x2, y2);

                                    List<Point> pc = QuadraticBezierSegment(q1, q2, q3);

                                    foreach (Point p in pc)
                                    {
                                        cpoints.Add(new CPoint(p, 1));
                                    }

                                    cp.X = x2;
                                    cp.Y = y2;
                                }
                                break;

                            case "s":   // relative cubic bezier

                                // Draws a cubic Bézier curve from the current point to (x,y).
                                // The first control point is assumed to be the reflection of the second control point on the previous command relative to the current point.
                                // (If there is no previous command or if the previous command was not an C, c, S or s, 
                                // assume the first control point is coincident with the current point.) 
                                // (x2,y2) is the second control point (i.e., the control point at the end of the curve). 
                                // S (uppercase) indicates that absolute coordinates will follow; 
                                // s (lowercase) indicates that relative coordinates will follow. 
                                // Multiple sets of coordinates may be specified to draw a polybézier. 
                                // At the end of the command, the new current point becomes the final (x,y) coordinate pair used in the polybézier.

                                for (int i = 0; i < d.Data.Count; i += 4)
                                {
                                    double x2 = cp.X + d.Data[i];
                                    double y2 = cp.Y + d.Data[i + 1];
                                    double x3 = cp.X + d.Data[i + 2];
                                    double y3 = cp.Y + d.Data[i + 3];

                                    if (cp.X == c4.X && cp.Y == c4.Y)
                                    {
                                        // first control point: reflection of the second control point on the previous cubic bezier segment
                                        c2 = Reflect(c1, c4, c3, new Point(x3, y3));
                                    }
                                    else
                                    {
                                        // no previous control point - use the current point
                                        c2 = cp;
                                    }

                                    c1 = cp;
                                    c3 = new Point(x2, y2);
                                    c4 = new Point(x3, y3);

                                    List<Point> pc = CubicBezierSegment(c1, c2, c3, c4);

                                    foreach (Point p in pc)
                                    {
                                        cpoints.Add(new CPoint(p, 1));
                                    }

                                    cp.X = x3;
                                    cp.Y = y3;
                                }
                                break;

                            case "S":   // absolute cubic bezier

                                // Draws a cubic Bézier curve from the current point to (x,y).
                                // The first control point is assumed to be the reflection of the second control point on the previous command relative to the current point.
                                // (If there is no previous command or if the previous command was not an C, c, S or s, 
                                // assume the first control point is coincident with the current point.) 
                                // (x2,y2) is the second control point (i.e., the control point at the end of the curve). 
                                // S (uppercase) indicates that absolute coordinates will follow; 
                                // s (lowercase) indicates that relative coordinates will follow. 
                                // Multiple sets of coordinates may be specified to draw a polybézier. 
                                // At the end of the command, the new current point becomes the final (x,y) coordinate pair used in the polybézier.

                                for (int i = 0; i < d.Data.Count; i += 4)
                                {
                                    double x2 = d.Data[i];
                                    double y2 = d.Data[i + 1];
                                    double x3 = d.Data[i + 2];
                                    double y3 = d.Data[i + 3];

                                    if (cp.X == c4.X && cp.Y == c4.Y)
                                    {
                                        // first control point: reflection of the second control point on the previous cubic bezier segment
                                        c2 = Reflect(c1, c4, c3, new Point(x3, y3));
                                    }
                                    else
                                    {
                                        // no previous control point - use the current point
                                        c2 = cp;
                                    }

                                    c1 = cp;
                                    c3 = new Point(x2, y2);
                                    c4 = new Point(x3, y3);

                                    List<Point> pc = CubicBezierSegment(c1, c2, c3, c4);

                                    foreach (Point p in pc)
                                    {
                                        cpoints.Add(new CPoint(p, 1));
                                    }

                                    cp.X = x3;
                                    cp.Y = y3;
                                }
                                break;

                            case "c":   // relative cubic bezier

                                // Draws a cubic Bézier curve from the current point to (x3,y3) using (x1,y1) as the control point 
                                // at the beginning of the curve and (x2,y2) as the control point at the end of the curve. 
                                // C (uppercase) indicates that absolute coordinates will follow; 
                                // c (lowercase) indicates that relative coordinates will follow.
                                // Multiple sets of coordinates may be specified to draw a polybézier.
                                // At the end of the command, the new current point becomes the final (x,y) coordinate pair used in the polybézier.

                                for (int i = 0; i < d.Data.Count; i += 6)
                                {
                                    double x1 = cp.X + d.Data[i];
                                    double y1 = cp.Y + d.Data[i + 1];
                                    double x2 = cp.X + d.Data[i + 2];
                                    double y2 = cp.Y + d.Data[i + 3];
                                    double x3 = cp.X + d.Data[i + 4];
                                    double y3 = cp.Y + d.Data[i + 5];

                                    c1 = cp;
                                    c2 = new Point(x1, y1);
                                    c3 = new Point(x2, y2);
                                    c4 = new Point(x3, y3);

                                    List<Point> pc = CubicBezierSegment(c1, c2, c3, c4);

                                    foreach (Point p in pc)
                                    {
                                        cpoints.Add(new CPoint(p, 1));
                                    }

                                    cp.X = x3;
                                    cp.Y = y3;
                                }
                                break;

                            case "C":   // absolute cubic bezier

                                // Draws a cubic Bézier curve from the current point to (x3,y3) using (x1,y1) as the control point 
                                // at the beginning of the curve and (x2,y2) as the control point at the end of the curve. 
                                // C (uppercase) indicates that absolute coordinates will follow; 
                                // c (lowercase) indicates that relative coordinates will follow.
                                // Multiple sets of coordinates may be specified to draw a polybézier.
                                // At the end of the command, the new current point becomes the final (x,y) coordinate pair used in the polybézier.

                                for (int i = 0; i < d.Data.Count; i += 6)
                                {
                                    double x1 = d.Data[i];
                                    double y1 = d.Data[i + 1];
                                    double x2 = d.Data[i + 2];
                                    double y2 = d.Data[i + 3];
                                    double x3 = d.Data[i + 4];
                                    double y3 = d.Data[i + 5];

                                    c1 = cp;
                                    c2 = new Point(x1, y1);
                                    c3 = new Point(x2, y2);
                                    c4 = new Point(x3, y3);

                                    List<Point> pc = CubicBezierSegment(c1, c2, c3, c4);

                                    foreach (Point p in pc)
                                    {
                                        cpoints.Add(new CPoint(p, 1));
                                    }

                                    cp.X = x3;
                                    cp.Y = y3;
                                }
                                break;

                            case "a":   // relative elliptical arc

                                // Draws an elliptical arc from the current point to (x, y).
                                // The size and orientation of the ellipse are defined by two radii (rx, ry) and an x-axis-rotation,
                                // which indicates how the ellipse as a whole is rotated relative to the current coordinate system.
                                // The center (cx, cy) of the ellipse is calculated automatically to satisfy the constraints imposed by the other parameters.
                                // large-arc-flag and sweep-flag contribute to the automatic calculations and help determine how the arc is drawn.

                                for (int i = 0; i < d.Data.Count; i += 7)
                                {
                                    double rx = d.Data[i];
                                    double ry = d.Data[i + 1];
                                    double r = d.Data[i + 2] / Construct.cRadiansToDegrees;
                                    double laf = d.Data[i + 3];
                                    double sf = d.Data[i + 4];
                                    double x = cp.X + d.Data[i + 5];
                                    double y = cp.Y + d.Data[i + 6];

                                    List<Point> pc = EllipticalArcSegment(cp, new Point(x, y), rx, ry, r, laf != 0, sf == 0);

                                    foreach (Point p in pc)
                                    {
                                        cpoints.Add(new CPoint(p, 1));
                                    }

                                    cp.X = x;
                                    cp.Y = y;
                                    cpoints.Add(new CPoint(cp, 1));
                                }
                                break;

                            case "A":   // absolute elliptical arc

                                // Draws an elliptical arc from the current point to (x, y).
                                // The size and orientation of the ellipse are defined by two radii (rx, ry) and an x-axis-rotation,
                                // which indicates how the ellipse as a whole is rotated relative to the current coordinate system.
                                // The center (cx, cy) of the ellipse is calculated automatically to satisfy the constraints imposed by the other parameters.
                                // large-arc-flag and sweep-flag contribute to the automatic calculations and help determine how the arc is drawn.

                                for (int i = 0; i < d.Data.Count; i += 7)
                                {
                                    double rx = d.Data[i];
                                    double ry = d.Data[i + 1];
                                    double r = d.Data[i + 2] / Construct.cRadiansToDegrees;
                                    double laf = d.Data[i + 3];
                                    double sf = d.Data[i + 4];
                                    double x = d.Data[i + 5];
                                    double y = d.Data[i + 6];

                                    List<Point> pc = EllipticalArcSegment(cp, new Point(x, y), rx, ry, r, laf != 0, sf == 0);

                                    foreach (Point p in pc)
                                    {
                                        cpoints.Add(new CPoint(p, 1));
                                    }

                                    cp.X = x;
                                    cp.Y = y;
                                    cpoints.Add(new CPoint(cp, 1));
                                }
                                break;
                        }
                    }
                }
                else if (shape is SvgPolylineType)
                {
                    SvgPolylineType polyline = shape as SvgPolylineType;

                    PLine pline = new PLine(polyline.Points[0]);

                    for (int i = 1; i < polyline.Points.Count; i++)
                    {
                        pline.AddPoint(polyline.Points[i].X, polyline.Points[i].Y, false);
                    }

                    primitive = pline;
                }
                else if (shape is SvgPolygonType)
                {
                    SvgPolygonType polygon = shape as SvgPolygonType;

                    cpoints = new List<CPoint>();

                    foreach (Point p in polygon.Points)
                    {
                        cpoints.Add(new CPoint(p, 1));
                    }
                }
                else if (shape is SvgRectType)
                {
                    SvgRectType rect = shape as SvgRectType;

                    // draw dectangle
                    
                    if (rect.Rx == 0 && rect.Ry == 0)
                    {
                        if (isComplexTranslateTransform)
                        {
                            cpoints = new List<CPoint>();

                            cpoints.Add(new CPoint(rect.X, rect.Y, 1));
                            cpoints.Add(new CPoint(rect.X + rect.Width, rect.Y, 1));
                            cpoints.Add(new CPoint(rect.X + rect.Width, rect.Y + rect.Height, 1));
                            cpoints.Add(new CPoint(rect.X, rect.Y + rect.Height, 1));
                            cpoints.Add(new CPoint(rect.X, rect.Y, 1));
                        }
                        else
                        {
                            primitive = new PRectangle(new Point(rect.X, rect.Y), rect.Width, rect.Height);
                        }
                    }
                    else
                    {
                        // round corner rectangle
                        List<Point> pc = new List<Point>();

                        Point p0 = new Point(rect.X + rect.Rx, rect.Y);

                        pc.Add(p0);
                        pc.Add(new Point(p0.X + rect.Width - rect.Rx - rect.Rx, p0.Y));
                      
                        pc.AddRange(CGeometry.EllipsePointCollection(new Point(p0.X + rect.Width - rect.Rx - rect.Rx, p0.Y + rect.Ry), rect.Rx, rect.Ry, 0, -Math.PI / 2, Math.PI / 2, false, Matrix.Identity));

                        pc.Add(new Point(p0.X + rect.Width - rect.Rx, p0.Y + rect.Ry));
                        pc.Add(new Point(p0.X + rect.Width - rect.Rx, p0.Y + rect.Height - rect.Ry));
                
                        pc.AddRange(CGeometry.EllipsePointCollection(new Point(p0.X + rect.Width - rect.Rx - rect.Rx, p0.Y + rect.Height - rect.Ry), rect.Rx, rect.Ry, 0, 0, Math.PI / 2, false, Matrix.Identity));

                        pc.Add(new Point(p0.X + rect.Width - rect.Rx - rect.Rx, p0.Y + rect.Height));
                        pc.Add(new Point(p0.X + rect.Rx - rect.Rx, p0.Y + rect.Height));
          
                        pc.AddRange(CGeometry.EllipsePointCollection(new Point(p0.X + 0, p0.Y + rect.Height - rect.Ry), rect.Rx, rect.Ry, 0, Math.PI / 2, Math.PI / 2, false, Matrix.Identity));

                        pc.Add(new Point(p0.X + -rect.Rx, p0.Y + rect.Height - rect.Ry));
                        pc.Add(new Point(p0.X + -rect.Rx, p0.Y + rect.Ry));
            
                        pc.AddRange(CGeometry.EllipsePointCollection(new Point(p0.X + 0, p0.Y + rect.Ry), rect.Rx, rect.Ry, 0, Math.PI, Math.PI / 2, false, Matrix.Identity));

                        cpoints = new List<CPoint>();

                        foreach (Point p in pc)
                        {
                            cpoints.Add(new CPoint(p, 1));
                        }
                    }
                }
                else if (shape is SvgImageType)
                {
                    SvgImageType image = shape as SvgImageType;

                    Point p = new Point(image.X, image.Y);
                    double width = image.Width;
                    double height = image.Height;

                    if (isComplexTranslateTransform)
                    {
                        p = shape.Transform.TransformPoint(p);
                        Matrix m;

                        if (shape.Transform is TransformGroup)
                        {
                            m = ((TransformGroup)shape.Transform).Value;
                        }
                        else if (shape.Transform is MatrixTransform)
                        {
                            m = ((MatrixTransform)shape.Transform).Matrix;
                        }
                        else
                        {
                            TransformGroup tg = new TransformGroup();
                            tg.Children.Add(shape.Transform);
                            m = tg.Value;
                        }

                        m.OffsetX = 0;
                        m.OffsetY = 0;
                        shape.Transform = null;
                        shape.Transform = new MatrixTransform();
                        ((MatrixTransform)shape.Transform).Matrix = m;
                    }

                    Point zero = new Point(0, 0);
                    Point c1 = p;
                    Point c2 = new Point(p.X + width, p.Y + height);
                    primitive = new PImage(p, c1, c2, image.Opacity, zero, zero, new Size(0, 0), image.ImageId, image.Name);
                }
                else if (shape is SvgTextType)
                {
                    SvgTextType text = shape as SvgTextType;

                    int styleId = StyleIdFromSvgAttributes(shape);
                    TextAlignment align;

                    Point p0 = text.PositionIsSet ? new Point(text.X, text.Y) : ctp;

                    switch (text.TextAnchor)
                    {
                        case "start":
                        default:
                            align = TextAlignment.Left;
                            break;

                        case "middle":
                            align = TextAlignment.Center;
                            p0.X -= .5;
                            break;

                        case "end":
                            align = TextAlignment.Right;
                            p0.X = p0.X - 1;
                            p0.Y = p0.Y;
                            break;
                    }

                    Point p1 = new Point(p0.X + 1, p0.Y);
                    double tr = 0;

                    if (shape.Transform != null)
                    {
                        // svg text is rotated by applying a rotation transform to the text object
                        // here we rotate PText objects by setting transforming the alignment point
                        p0 = shape.Transform.TransformPoint(p0);
                        p1 = shape.Transform.TransformPoint(p1);

                        tr = Construct.Angle(p0, p1);

                        text.FontSize = TransformScalar(shape.Transform, text.FontSize);

                        shape.Transform = null;
                    }

                    // draw text
                    PText ptext = new PText(p0, p1, styleId, align, TextPosition.Above, text.Text);
                    ptext.CharacterSpacing = 1 + (text.LetterSpacing / .8) / text.FontSize;

                    primitive = ptext;

                    // all svg user agents seem to add a trailing space after text object when calculating the "current text position"
                    // I can't find any explicit support for this behavior in the spec, but we'll do it here to be consistent
                    // note that calculating the width of a space can be problematic, so we use a period to make sure it counts
                    string s = text.Text + ".";

                    double sw;

                    try
                    {
                        sw = Cirros.TextUtilities.FontInfo.StringWidth(s, text.FontFamily, text.FontSize);
                    }
                    catch
                    {
                        sw = text.FontSize * .6 * s.Length;
                    }

                    ctp = Construct.PolarOffset(p0, sw * ptext.CharacterSpacing, tr);
                }
                else if (shape is SvgGroupType)
                {
                    SvgGroupType svgGroup = shape as SvgGroupType;

                    // create group
                    // TODO
                    Group pGroup = null;

                    AddEntities(svgGroup, pGroup);
                }
                else if (shape is SvgSymbolType)
                {
                    SvgSymbolType symbol = shape as SvgSymbolType;

                    // create group
                    // TODO
                    Group pGroup = null;

                    AddEntities(symbol, pGroup);
                }
                else if (shape is SvgDefsType)
                {
                    // defs should not be rendered
                }
                else
                {
                    // unknown svg group type - this shouldn't happen
                }

                double lw = shape.StrokeWidth;

                if (shape.Opacity == 0)
                {
                    // some authors use opacity == 0 or strokewidth == 0 to denote hidden elements
                    continue;
                }
                else if (shape.Visible == false)
                {
                    continue;
                }
                else if ((primitive is PText) == false)
                {
                    if (shape.Fill)
                    {
                        if (shape.Stroke == false)
                        {
                            lw = 0;
                            shape.StrokeColor = shape.FillColor;
                        }
                    }
                    else if (shape.Stroke == false)
                    {
                        continue;
                    }
                    else if (shape.StrokeWidth == 0)
                    {
                        continue;
                    }
                }

                if (primitive == null)
                {
                    if (cpoints == null && points != null)
                    {
                        cpoints = new List<CPoint>();

                        foreach (Point p in points)
                        {
                            cpoints.Add(new CPoint(p, 1));
                        }
                    }

                    if (cpoints != null && cpoints.Count > 1)
                    {
                        if (TransformIncludesTranslate(shape.Transform))
                        {
                            foreach (CPoint cp in cpoints)
                            {
                                cp.Point = shape.Transform.TransformPoint(cp.Point);
                            }

                            lw = TransformScalar(shape.Transform, lw);

                            shape.Transform = null;
                        }

                        if (shape.Fill && cpoints.Count > 2)
                        {
                            PPolygon ppoly = new PPolygon(cpoints[0].Point);
                            ppoly.FillEvenOdd = shape.FillEvenOdd;

                            for (int i = 1; i < cpoints.Count; i++)
                            {
                                if (cpoints[i] != cpoints[i-1])
                                {
                                    ppoly.AddPoint(cpoints[i], false);
                                }
                            }

                            primitive = ppoly;
                        }
                        else
                        {
                            PLine pline = new PLine(cpoints[0], cpoints[1]);

                            for (int i = 2; i < cpoints.Count; i++)
                            {
                                pline.AddPoint(cpoints[i], false);
                            }

                            primitive = pline;
                        }
                    }
                }

                if (primitive != null)
                {
                    if (shape.Transform != null)
                    {
                        if (shape.Transform is TransformGroup)
                        {
                            primitive.Matrix = ((TransformGroup)shape.Transform).Value;
                        }
                        else
                        {
                            TransformGroup tg = new TransformGroup();
                            tg.Children.Add(shape.Transform);
                            primitive.Matrix = tg.Value;
                        }
                        
                        lw = shape.StrokeWidth * primitive.Matrix.M11 + shape.StrokeWidth * primitive.Matrix.M12;
                    }

                    if (primitive is PText)
                    {
                        primitive.ColorSpec = Utilities.ColorSpecFromColor(shape.FillColor);
                    }
                    else
                    {
                        primitive.Fill = shape.Fill ? Utilities.ColorSpecFromColor(shape.FillColor) : (uint)ColorCode.NoFill;
                        primitive.ColorSpec = Utilities.ColorSpecFromColor(shape.StrokeColor);
                        primitive.LineWeightId = (int)Math.Ceiling(lw * 1000);
                    }

                    if (parent == null)
                    {
#if true
                        primitive.MoveByDelta(_dom.XOffset, _dom.YOffset);
                        primitive.Draw();
#else
                        if (container is SvgDom)
                        {
                            primitive.Move(((SvgDom)container).XOffset, ((SvgDom)container).YOffset);
                        }
#endif
                        primitive.AddToContainer(Globals.ActiveDrawing);
                    }
                    else
                    {
                        primitive.IsGroupMember = true;
                        parent.AddMember(primitive);
                    }
                }
            }
        }

        private Point Reflect(Point A, Point B, Point R, Point C)
        {
            // point r is offset from segment ab
            // return reflected point off segment bc

            double d = Construct.Distance(B, R);
            double i = Construct.IncludedAngle(A, B, R);
            double a1 = Construct.Angle(B, C);
            double a2 = a1 + i;

            return Construct.PolarOffset(B, d, a2);
        }

        private string trimFont(string font)
        {
            font = font.Trim();

            if (font.StartsWith("'") && font.EndsWith("'"))
            {
                font = font.Substring(1, font.Length - 2);
            }

            return font.Trim();
        }

        private int StyleIdFromSvgAttributes(SvgShapeType shape)
        {
            TextStyle textStyle = null;

            List<string> fonts = new List<string>();

            if (shape.FontFamily.Contains(","))
            {
                string[] sa = shape.FontFamily.Split(new char[] { ',' });
                foreach (string font in sa)
                {
                    fonts.Add(trimFont(font));
                }
            }
            else
            {
                fonts.Add(trimFont(shape.FontFamily));
            }

            if (fonts.Count == 0)
            {
                return 0;
            }

            string fontFamily = fonts[0];

            double shapeSize = Math.Round(shape.FontSize, 3);
            string name = string.Format("{0}-{1}", fontFamily.Replace(" ", ""), shapeSize);

            foreach (TextStyle style in Globals.TextStyleTable.Values)
            {
                if (style.Name == name)
                {
                    textStyle = style;
                    break;
                }
            }

            if (textStyle == null)
            {
                if (Dx.FontExists(fontFamily) == false)
                {
                    bool exists = false;
                    for (int i = 1; i < fonts.Count; i++)
                    {
                        fontFamily = fonts[i];

                        if (Dx.FontExists(fontFamily))
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (exists == false)
                    {
                        if (_missingFonts.Contains(fontFamily) == false)
                        {
                            _missingFonts.Add(fontFamily);
                        }

                        fontFamily = "Segoe UI";
                    }
                }

                if (SvgExport.FontHeightAdj.ContainsKey(fontFamily))
                {
                    double adj = SvgExport.FontHeightAdj[fontFamily];
                    shapeSize = Math.Round(shape.FontSize / adj, 3);
                }
                else if (shapeSize > 0)
                {
                    double descent;
                    double h;
                    TextUtilities.FontInfo.FontHeight(fontFamily, shapeSize, out h, out descent);
                    if (h > 0)
                    {
                        //double adj = h / shapeSize;
                        //shapeSize = Math.Round(shape.FontSize / adj, 3);
                        shapeSize = Math.Round(h, 3);
                    }
                }

                int id = Globals.ActiveDrawing.AddTextStyle(name, fontFamily, shapeSize, 0, 1, 1);
                textStyle = Globals.TextStyleTable[id];
            }

            return textStyle.Id;
        }

        class Vector2
        {
            double _dx;
            double _dy;

            public Vector2(double dx, double dy)
            {
                _dx = dx;
                _dy = dy;
            }

            public double X
            {
                get
                {
                    return _dx;
                }
            }

            public double Y
            {
                get
                {
                    return _dy;
                }
            }

            public double Length
            {
                get
                {
                    return Math.Sqrt(_dx * _dx + _dy * _dy);
                }
            }

            public void Normalize()
            {
                double distance = Math.Sqrt(_dx * _dx + _dy * _dy);
                if (distance != 0)
                {
                    _dx = _dx / distance;
                    _dy = _dy / distance;
                }
            }

            public static Vector2 operator *(double s2, Vector2 v1)
            {
                return new Vector2(v1.X * s2, v1.Y * s2);
            }
        }

        public static List<Point> EllipticalArcSegment(Point pt1, Point pt2,
                        double radiusX, double radiusY, double angleRotation,
                        bool isLargeArc, bool isCounterclockwise)
        {
            List<Point> points = new List<Point>();

            if (pt1.X == pt2.X && pt1.Y == pt2.Y)
            {
                // per the SVC spec, if the endpoints are identical the arc should be ignored
                return points;
            }

            // Adjust for different radii
            Matrix matx = Matrix.Identity;
            matx = CGeometry.RotateMatrixAboutZ(matx, -angleRotation);
            matx = CGeometry.ScaleMatrix(matx, radiusY / radiusX, 1);

            pt1 = matx.Transform(pt1);
            pt2 = matx.Transform(pt2);

            // Get info about chord that connects both points
            Point midPoint = new Point((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2);

            Vector2 vect = new Vector2(pt2.X - pt1.X, pt2.Y - pt1.Y);
            double halfChord = vect.Length / 2;

            // Get vector from chord to center
            Vector2 vectRotated;

            if (isLargeArc == isCounterclockwise)
            {
                vectRotated = new Vector2(-vect.Y, vect.X);
            }
            else
            {
                vectRotated = new Vector2(vect.Y, -vect.X);
            }

            vectRotated.Normalize();

            Point center;

            if (radiusY > halfChord)
            {
                // Distance from chord to center 
                double centerDistance = radiusY > halfChord ? Math.Sqrt(radiusY * radiusY - halfChord * halfChord) : 0;

                // Calculate center point
                Vector2 vr = centerDistance * vectRotated;
                center = new Point(midPoint.X + vr.X, midPoint.Y + vr.Y);
            }
            else
            {
                center = new Point(midPoint.X, midPoint.Y);
            }

            // Get angles from center to the two points
            double angle1 = Math.Atan2(pt1.Y - center.Y, pt1.X - center.X);
            double angle2 = Math.Atan2(pt2.Y - center.Y, pt2.X - center.X);

            if (isCounterclockwise) 
            {
                while (angle1 > 0)
                {
                    angle1 -= 2 * Math.PI;
                }
                while (angle2 > angle1)
                {
                    angle2 -= 2 * Math.PI;
                }
            }
            else 
            {
                while (angle1 < 0)
                {
                    angle1 += 2 * Math.PI;
                }
                while (angle2 < angle1)
                {
                    angle2 += 2 * Math.PI;
                }
            }

            // Invert matrix for final point calculation
            matx = CGeometry.InvertMatrix(matx);

            // Calculate number of points for polyline approximation
            int max = (int)((4 * (radiusX + radiusY) * Math.Abs(angle2 - angle1) / (2 * Math.PI)) / (CGeometry.cCurveQuality * 1));
            if (max > 0)
            {
                // Loop through the points
                for (int i = 0; i <= max; i++)
                {
                    double angle = ((max - i) * angle1 + i * angle2) / max;
                    double x = center.X + radiusY * Math.Cos(angle);
                    double y = center.Y + radiusY * Math.Sin(angle);
                    // Transform the point back
                    Point pt = matx.Transform(new Point(x, y));
                    points.Add(pt);
                }
            }

            return points;
        }

        private static Point CubicBezierPoint(double t, Point p0, Point p1, Point p2, Point p3)
        {
#if true
            double t2 = t * t;
            double t3 = t2 * t;

            double[] vx = new double[] { p0.X, p1.X, p2.X, p3.X };
            double[] vy = new double[] { p0.Y, p1.Y, p2.Y, p3.Y };
            double[] vt = new double[] { (1 - t) * (1 - t) * (1 - t), 3 * (1 - t) * (1 - t) * t, 3 * (1 - t) * t2, t3 };

            double x = dot(vx, vt);
            double y = dot(vy, vt);
#else
            double cube = t * t * t;
            double square = t * t;
            double ax = 3 * (p1.X - p0.X);
            double ay = 3 * (p1.Y - p0.Y);
            double bx = 3 * (p2.X - p1.X) - ax;
            double by = 3 * (p2.Y - p1.Y) - ay;
            double cx = p3.X - p0.X - ax - bx;
            double cy = p3.Y - p0.Y - ay - by;
            double x = (cx * cube) + (bx * square) + (ax * t) + p0.X;
            double y = (cy * cube) + (by * square) + (ay * t) + p0.Y;
#endif
            return new Point(x, y);
        }

        private static double dot(double [] v1, double [] v2)
        {
            double sum = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                sum += v1[i]*v2[i];
            }
            return sum;
        }

        private static Point QuadraticBezierPoint(double t, Point p0, Point p1, Point p2)
        {
            double t2 = t * t;

            double [] vx = new double[] { p0.X, p1.X, p2.X };
            double [] vy = new double[] { p0.Y, p1.Y, p2.Y };
            double [] vt = new double[] { (1 - t)  *(1 - t), 2 * t * (1 - t), t2 };

            double x = dot(vx, vt);
            double y = dot(vy, vt);

            return new Point(x ,y);
        }

        private static int BezierIncrement(double chordLength, double chordOffset)
        {
            int m = 0;

            if (chordLength < 4 * CGeometry.cCurveQuality)
            {
                if (chordOffset > .03)
                {
                    m = 4;
                }
                else if (chordOffset > .02)
                {
                    m = 3;
                }
                else if (chordOffset > .01)
                {
                    m = 2;
                }
                else
                {
                    m = 1;
                }
                double d = chordOffset / chordLength;
                int m2 = (int)(chordLength / (CGeometry.cCurveQuality * 1));
                if (d > .01 && m2 > m)
                {
                    m = m2;
                }
            }
            else if (chordOffset > .5)
            {
                m = (int)(chordLength / (CGeometry.cCurveQuality * 2));
            }
            else if (chordOffset > .15)
            {
                m = (int)(chordLength / (CGeometry.cCurveQuality * 1.5));
            }
            else if (chordOffset > .02)
            {
                m = (int)(chordLength / (CGeometry.cCurveQuality * 1));
            }

            return m;
        }

        public static List<Point> CubicBezierSegment(Point a, Point c1, Point c2, Point b)
        {
            List<Point> pc = new List<Point>();

            //pc.Add(a);

            double o1 = Construct.DistancePointToLine(c1, a, b);
            double o2 = Construct.DistancePointToLine(c2, a, b);
            double chordOffset = Math.Max(o1, o2);
            double chordLength = Construct.Distance(a, b);
            double m = BezierIncrement(chordLength, chordOffset);

            if (m > 1)
            {
                double dd = 1.0 / m;

                for (double s = dd; s < 1; s += dd)
                {
                    pc.Add(CubicBezierPoint(s, a, c1, c2, b));
                }
            }

            pc.Add(b);

            return pc;
        }

        public static List<Point> QuadraticBezierSegment(Point a, Point c1, Point b)
        {
            List<Point> pc = new List<Point>();

            //pc.Add(a);

            double chordOffset = Construct.DistancePointToLine(c1, a, b);
            double chordLength = Construct.Distance(a, b);
            double m = BezierIncrement(chordLength, chordOffset);

            if (m > 1)
            {
                double dd = 1.0 / m;

                for (double s = dd; s < 1; s += dd)
                {
                    pc.Add(QuadraticBezierPoint(s, a, c1, b));
                }
            }

            pc.Add(b);

            return pc;
        }
    }
}
