using Cirros.Utility;
using System;
using System.Collections.Generic;
using Cirros.Drawing;
#if UWP
using Windows.Foundation;
#else
using System.Windows;
using CirrosCore;
#endif

namespace Cirros.Primitives
{
    public partial class PrimitiveUtilities
    {
        public static float SerializeDoubleAsFloat(double d)
        {
            return (float)Math.Round(d, 5);
        }

        public static PArc GapArc(PArc arc, Point startPoint, Point endPoint)
        {
            PArc newArc = null;

            if (arc.IsCircle)
            {
                arc.StartAngle = Construct.Angle(arc.Origin, startPoint);
                double minorAngle = Construct.MinorIncludedAngle(arc.StartAngle, Construct.Angle(arc.Origin, endPoint));
                arc.IncludedAngle = -(minorAngle < 0 ? -Math.PI * 2 - minorAngle : Math.PI * 2 - minorAngle);
                arc.Draw();
            }
            else
            {
                int direction = Math.Sign(arc.IncludedAngle);
                double a0 = Construct.IncludedAngle(arc.StartAngle, Construct.Angle(arc.Origin, startPoint), direction >= 0);
                double a1 = Construct.IncludedAngle(arc.StartAngle, Construct.Angle(arc.Origin, endPoint), direction >= 0);
                double a2 = arc.IncludedAngle;

                bool swap = false;

                if (direction < 0)
                {
                    swap = a0 < a1;
                }
                else
                {
                    swap = a0 > a1;
                }

                if (swap)
                {
                    double temp = a0;
                    a0 = a1;
                    a1 = temp;
                }

                arc.IncludedAngle = a0;
                arc.Draw();

                newArc = (PArc)arc.Clone();
                newArc.StartAngle = arc.StartAngle + a1;
                newArc.IncludedAngle = Construct.IncludedAngle(arc.StartAngle + a1, arc.StartAngle + a2, direction >= 0);
                newArc.AddToContainer(Globals.ActiveDrawing);
            }

            return newArc;
        }

#if UWP
        public static void UpdatePImageFromDictionary(PImage pimage, Dictionary<string, object> dictionary, Point origin)
        {
            pimage.ImageId = dictionary["imageId"] as string;
            pimage.SourceName = dictionary["sourceName"] as string;

            double pixelWidth = 0;
            double pixelHeight = 0;
            double originalWidth = 0;
            double originalHeight = 0;

            Size refSize = new Size(0, 0);
            Point refP1 = new Point(0, 0);
            Point refP2 = new Point(0, 0);

            if (dictionary.ContainsKey("pixelWidth") && dictionary.ContainsKey("pixelHeight"))
            {
                pixelWidth = (double)((int)dictionary["pixelWidth"]);
                pixelHeight = (double)((int)dictionary["pixelHeight"]);
            }

            if (dictionary.ContainsKey("originalWidth") && dictionary.ContainsKey("originalHeight"))
            {
                originalWidth = (double)dictionary["originalWidth"];
                originalHeight = (double)dictionary["originalHeight"];
            }

            if (dictionary.ContainsKey("refDestRect") && dictionary.ContainsKey("refDestSize"))
            {
                Rect r = (Rect)dictionary["refDestRect"];
                if (r != Rect.Empty)
                {
                    refSize = (Size)dictionary["refDestSize"];

                    refP1 = new Point(r.Left, r.Bottom);
                    refP2 = new Point(r.Right, r.Top);
                }
            }

            double s = 1;
            Point c1 = pimage.C1;
            Point c2 = pimage.C2;
            Point zero = new Point(0,0);

            if (c1 == zero && c2 == zero)
            {
                // This is a new PImage object
                // The size should be maximum of full resolution or 75% of the drawing size

                double hs = Globals.ActiveDrawing.PaperSize.Width / pixelWidth;
                double vs = Globals.ActiveDrawing.PaperSize.Height / pixelHeight;
                s = Math.Min(1.0 / 96.0, Math.Min(hs, vs) * .75);

                Size imageSize = new Size(s * pixelWidth, s * pixelHeight);

                origin = Construct.RoundXY(new Point(origin.X - imageSize.Width / 2, origin.Y + imageSize.Height / 2));

                if (refP1.X != refP2.X && refP1.Y != refP2.Y)
                {
                    if (refSize == Size.Empty || refSize.Width == 0 || refSize.Height == 0)
                    {
                        // refSize is not set
                        Point p1 = new Point(refP1.X * imageSize.Width, refP1.Y * imageSize.Height);
                        Point p2 = new Point(refP2.X * imageSize.Width, refP2.Y * imageSize.Height);

                        origin = Construct.RoundXY(new Point(origin.X + p1.X, origin.Y - (imageSize.Height - p1.Y)));
                        c1 = new Point(origin.X - p1.X, origin.Y + (imageSize.Height - p1.Y));
                        c2 = new Point(c1.X + imageSize.Width, c1.Y - imageSize.Height);
                    }
                    else
                    {
                        // refSize is set, this is a scaled image
                        Size paperRefSize = Globals.ActiveDrawing.ModelToPaperSize(refSize);
                        imageSize = new Size(paperRefSize.Width / Math.Abs(refP2.X - refP1.X), paperRefSize.Height / Math.Abs(refP2.Y - refP1.Y));

                        Point p1 = new Point(refP1.X * imageSize.Width, refP1.Y * imageSize.Height);
                        Point p2 = new Point(refP2.X * imageSize.Width, refP2.Y * imageSize.Height);

                        c1 = new Point(origin.X - p1.X, origin.Y + (imageSize.Height - p1.Y));
                        c2 = new Point(c1.X + imageSize.Width, c1.Y - imageSize.Height);
                    }
                }
                else
                {
                    c1 = origin;
                    c2 = new Point(c1.X + imageSize.Width, c1.Y - imageSize.Height);
                }
            }
            else if (refP1.X == refP2.X && refP1.Y == refP2.Y)
            {
                // This is an existing PImage with no reference points
                // The existing origin, c1 and scale should be retained

                s = Math.Min(Math.Abs(c2.X - c1.X) / originalWidth, Math.Abs(c2.Y - c1.Y) / originalHeight);

                Size imageSize = new Size(s * pixelWidth, s * pixelHeight);
                c2 = new Point(c1.X + imageSize.Width, c1.Y - imageSize.Height);
            }
            else if (refSize == Size.Empty || refSize.Width == 0 || refSize.Height == 0)
            {
                // This is an existing PImage with reference points but no refSize
                // The existing origin and scale should be retained

                s = Math.Min(Math.Abs(c2.X - c1.X) / originalWidth, Math.Abs(c2.Y - c1.Y) / originalHeight);

                Size imageSize = new Size(s * pixelWidth, s * pixelHeight);

                Point p1 = new Point(refP1.X * imageSize.Width, refP1.Y * imageSize.Height);
                Point p2 = new Point(refP2.X * imageSize.Width, refP2.Y * imageSize.Height);

                c1 = new Point(origin.X - p1.X, origin.Y + (imageSize.Height - p1.Y));
                c2 = new Point(c1.X + imageSize.Width, c1.Y - imageSize.Height);
            }
            else
            {
                // This is an existing PImage with reference points and reference size
                // The size is determined by the objects model size
                Size paperRefSize = Globals.ActiveDrawing.ModelToPaperSize(refSize);
                Size imageSize = new Size(paperRefSize.Width / Math.Abs(refP2.X - refP1.X), paperRefSize.Height / Math.Abs(refP2.Y - refP1.Y));

                Point p1 = new Point(refP1.X * imageSize.Width, refP1.Y * imageSize.Height);
                Point p2 = new Point(refP2.X * imageSize.Width, refP2.Y * imageSize.Height);

                c1 = new Point(origin.X - p1.X, origin.Y + (imageSize.Height - p1.Y));
                c2 = new Point(c1.X + imageSize.Width, c1.Y - imageSize.Height);

            }

            pimage.MoveTo(origin.X, origin.Y);
            pimage.C1 = c1;
            pimage.C2 = c2;
            pimage.RefP1 = refP1;
            pimage.RefP2 = refP2;
            pimage.RefSize = refSize;

            pimage.Draw();
        }
#else
#endif

        private static void CPointListFromVectorEntity(VectorEntity ve, List<CPoint> cpointList)
        {
            if (ve.Children != null)
            {
                foreach (object o in ve.Children)
                {
                    if (o is List<Point>)
                    {
                        List<Point> pc = o as List<Point>;

                        cpointList.Add(new CPoint(pc[0], 0));

                        for (int i = 1; i < pc.Count; i++)
                        {
                            cpointList.Add(new CPoint(pc[i], 1));
                        }
                    }
                    else if (o is VectorEntity)
                    {
                        CPointListFromVectorEntity(o as VectorEntity, cpointList);
                    }
                    else if (o is VectorImageEntity)
                    {
                        // Ignore VectorImageEntity objects
                    }
                    else if (o is VectorTextEntity)
                    {
                        // Ignore VectorImageEntity objects
                    }
                }
            }
        }

        public static PLine PLineFromVectorEntity(VectorEntity ve)
        {
            PLine p = null;

            List<CPoint> cpointList = new List<CPoint>();
            CPointListFromVectorEntity(ve, cpointList);

            if (cpointList.Count > 1)
            {
                p = new PLine(cpointList);
            }

            return p;
        }

        public static PLine ClipVectorEntity(VectorEntity ve, Rect clipRect)
        {
            PLine p = null;
            List<CPoint> cpointList = new List<CPoint>();
            List<CPoint> clippedCPointList = new List<CPoint>();

            CPointListFromVectorEntity(ve, cpointList);
             
            if (cpointList.Count > 1)
            {
                CPoint cps = cpointList[0];
                for (int i = 1; i < cpointList.Count; i++)
                {
                    CPoint cpe = cpointList[i];
                    Point s = cps.Point;
                    Point e = cpe.Point;

                    if (cpe.M != 0 && Construct.Clip(ref s, ref e, clipRect))
                    {
                        CPoint cp0 = new CPoint(s, cps.M);
                        if (clippedCPointList.Count == 0 || cps.M == 0)
                        {
                            clippedCPointList.Add(cp0);
                        }
                        else
                        {
                            CPoint last = clippedCPointList[clippedCPointList.Count - 1];
                            if (s.X != last.X || s.Y != last.Y)
                            {
                                cp0.M = 0;
                                clippedCPointList.Add(cp0);
                            }
                        }
                        CPoint cp1 = new CPoint(e, cpe.M);
                        clippedCPointList.Add(cp1);
                    }

                    cps = cpe;
                }

                if (clippedCPointList.Count > 1)
                {
                    p = new PLine(clippedCPointList);
                }
            }

            return p;
        }

        public static Primitive Clip(Primitive p, Rect clipRect)
        {
            Primitive clipped = null;

            VectorContext context = new VectorContext(false, true, true);

            VectorEntity ve = p.Vectorize(context);
            Point p0 = new Point(ve.ItemBox.Left, ve.ItemBox.Top);
            Point p1 = new Point(ve.ItemBox.Right, ve.ItemBox.Bottom);
            if (clipRect.Contains(p0) && clipRect.Contains(p1))
            {
                clipped = p;
            }
            else if (clipRect.Contains(p0) || clipRect.Contains(p1))
            {
                clipped = PrimitiveUtilities.ClipVectorEntity(ve, clipRect);
                clipped.LayerId = p.LayerId;
                clipped.ColorSpec = p.ColorSpec;
                clipped.LineWeightId = p.LineWeightId;
                clipped.LineTypeId = p.LineTypeId;
                clipped.ZIndex = p.ZIndex;
            }

            return clipped;
        }
    }
}
