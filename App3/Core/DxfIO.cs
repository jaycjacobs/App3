using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Dxf
{
    public class DxfIO
    {
        public static void ImportDxfEntities(DxfDocument dxfDoc, DxfContext dxc, List<DxfEntity> entities, Group group)
        {
            List<CPoint> cpoints = new List<CPoint>();
            DxfEntity cpEntity = null;
            Point cpBase = new Point();

            foreach (DxfEntity entity in entities)
            {
                //System.Diagnostics.Debug.WriteLine("Handle={0}", entity.Handle);
                Primitive p = null;

                if (entity is DxfLineEntity)
                {
                    DxfLineEntity e = entity as DxfLineEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);
                    Point p1 = dxc.WcsToModel(e.X1, e.Y1, e.Space);

                    if (p0.X != p1.X || p0.Y != p1.Y)
                    {
                        if (group != null)
                        {
                            if (cpEntity == null)
                            {
                                cpBase = p0;
                                cpoints.Add(new CPoint(p1.X - cpBase.X, p1.Y - cpBase.Y, 1));
                                cpEntity = entity;
                            }
                            else if (cpEntity.Layer != entity.Layer || cpEntity.Color != entity.Color || cpEntity.Ltype != entity.Ltype)
                            {
                                PLine pline = new PLine(cpBase);
                                pline.CPoints = cpoints;
                                AddPrimitive(dxfDoc, cpEntity, pline, group);

                                cpoints.Clear();

                                cpBase = p0;
                                cpoints.Add(new CPoint(p1.X - cpBase.X, p1.Y - cpBase.Y, 1));
                                cpEntity = entity;
                            }
                            else
                            {
                                cpoints.Add(new CPoint(p0.X - cpBase.X, p0.Y - cpBase.Y, 0));
                                cpoints.Add(new CPoint(p1.X - cpBase.X, p1.Y - cpBase.Y, 1));
                            }
                        }
                        else
                        {
                            p = new PLine(p0, p1);
                        }
                    }
                }
                else if (entity is DxfSolidEntity)
                {
                    DxfSolidEntity e = entity as DxfSolidEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);
                    Point p1 = dxc.WcsToModel(e.X1, e.Y1, e.Space);
                    Point p2 = dxc.WcsToModel(e.X2, e.Y2, e.Space);
                    Point p3 = dxc.WcsToModel(e.X3, e.Y3, e.Space);

                    p = new PPolygon(p0);
                    ((PPolygon)p).AddPoint(p1.X, p1.Y, false);
                    ((PPolygon)p).AddPoint(p3.X, p3.Y, false);
                    ((PPolygon)p).AddPoint(p2.X, p2.Y, false);
                    ((PPolygon)p).Fill = 0x00000002;
                }
                else if (entity is DxfArcEntity)
                {
                    DxfArcEntity e = entity as DxfArcEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);

                    double start = (-e.StartAngle) / Construct.cRadiansToDegrees;
                    double included = (-e.EndAngle - (-e.StartAngle)) / Construct.cRadiansToDegrees;

                    if (e.EndAngle < e.StartAngle)
                    {
                        included = (-e.EndAngle - 360 - (-e.StartAngle)) / Construct.cRadiansToDegrees;
                    }

                    p = new PArc(p0, dxc.WcsToModel(e.Radius, e.Space), start, included);
                }
                else if (entity is DxfCircleEntity)
                {
                    DxfCircleEntity e = entity as DxfCircleEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);

                    p = new PArc(p0, dxc.WcsToModel(e.Radius, e.Space), 0, 0);
                }
                else if (entity is DxfTextEntity)
                {
                    DxfTextEntity e = entity as DxfTextEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);
                    Point p1 = dxc.WcsToModel(e.X1, e.Y1, e.Space);

                    TextAlignment align = TextAlignment.Left;
                    TextPosition position = TextPosition.Above;

                    float size = dxc.WcsToModel(e.Height, e.Space);

                    //string style = e.Style == null ? "STANDARD" : e.Style.ToUpper();
                    string style = e.Style == null ? "STANDARD" : DxfExport.FixAcName(e.Style);
                    if (dxfDoc.STYLEList.ContainsKey(style) == false)
                    {
                        style = "STANDARD";
                    }
                    DxfStyle acStyle = dxfDoc.STYLEList[style];
                    //int textStyle = TextStyleFromSize(acStyle, size);
                    int textStyleId = (int)acStyle.Tag;

                    string text = ACText(e.Text);

                    if (e.VertJust == 0 && e.HorizJust == 0)
                    {
                        // Use first alignment point, ignore second
                        p = new PText(p0, -e.Rotation, textStyleId, TextAlignment.Left, TextPosition.Above, text);
                    }
                    else
                    {
                        // Use second alignment point, ignore first
                        switch (e.VertJust)
                        {
                            case 0:     // Baseline
                            case 1:     // Bottom
                            default:
                                position = TextPosition.Above;
                                break;

                            case 2:     // Middle
                                position = TextPosition.On;
                                break;

                            case 3:     // Top
                                position = TextPosition.Below;
                                break;
                        }

                        switch (e.HorizJust)
                        {
                            case 0:     // Left
                            default:
                                align = TextAlignment.Left;
                                p = new PText(p1, -e.Rotation, textStyleId, align, position, text);
                                break;

                            case 1:     // Center
                                align = TextAlignment.Center;
                                p = new PText(p1, -e.Rotation, textStyleId, align, position, text);
                                break;

                            case 2:     // Right
                                align = TextAlignment.Right;
                                p = new PText(p1, -e.Rotation, textStyleId, align, position, text);
                                break;

                            case 3:     // Aligned (drawn from first point to second, height is adjusted to fit)
                                align = TextAlignment.Center;
                                p = new PText(p0, p1, textStyleId, align, position, text);
                                break;

                            case 4:     // Middle
                                align = TextAlignment.Center;
                                position = TextPosition.On;
                                p = new PText(p1, -e.Rotation, textStyleId, align, position, text);
                                break;

                            case 5:     // Fit (drawn from first point to second, character width is adjusted to fit)
                                align = TextAlignment.Center;
                                p = new PText(p0, p1, textStyleId, align, position, text);
                                break;
                        }
                    }

                    TextStyle textStyle = Globals.TextStyleTable[textStyleId];
                    if (size != textStyle.Size)
                    {
                        ((PText)p).Size = size;
                    }
                      
                    //((PText)p).CharacterSpacing = e.HzScale == textStyle.CharacterSpacing ? 0 : e.HzScale;
                }
                else if (entity is DxfMTextEntity)
                {
                    DxfMTextEntity e = entity as DxfMTextEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);
                    //Point p1 = dxc.WcsToModel(e.X1, e.Y1, e.Space);
                    Point p1 = dxc.WcsToModelVector(e.X1, e.Y1);

                    TextAlignment align;
                    TextPosition position;
                    //  71  Attachment point:
                    //        1 = Top left; 2 = Top center; 3 = Top right; 
                    //        4 = Middle left; 5 = Middle center; 6 = Middle right;
                    //        7 = Bottom left; 8 = Bottom center; 9 = Bottom right

                    switch ((e.AttachPoint - 1) % 3)
                    {
                        case 0:
                        default:
                            align = TextAlignment.Left;
                            break;
                        case 1:
                            align = TextAlignment.Center;
                            break;
                        case 2:
                            align = TextAlignment.Right;
                            break;
                    }

                    switch ((e.AttachPoint - 1) / 3)
                    {
                        case 0:
                        default:
                            position = TextPosition.Below;
                            break;
                        case 1:
                            position = TextPosition.On;
                            break;
                        case 2:
                            position = TextPosition.Above;
                            break;
                    }

                    float size = dxc.WcsToModel(e.Height, e.Space);

                    //string style = e.Style == null ? "STANDARD" : e.Style.ToUpper();
                    string style = e.Style == null ? "STANDARD" : DxfExport.FixAcName(e.Style);
                    if (dxfDoc.STYLEList.ContainsKey(style) == false)
                    {
                        style = "STANDARD";
                    }
                    DxfStyle acStyle = dxfDoc.STYLEList[style];
                    //int textStyleId = TextStyleFromSize(acStyle, size);
                    int textStyleId = (int)acStyle.Tag;

                    string text = ACText(e.Text);

                    if (e.SecondPointDefined)
                    {
                        double angle = Construct.Angle(new Point(0, 0), p1) * Construct.cRadiansToDegrees;
                        p = new PText(p0, angle, textStyleId, align, position, text);
                    }
                    else
                    {
                        p = new PText(p0, -e.Rotation, textStyleId, align, position, text);
                    }

                    TextStyle textStyle = Globals.TextStyleTable[textStyleId];
                    if (size != textStyle.Size)
                    {
                        ((PText)p).Size = size;
                    }
                }
                else if (entity is DxfInsertEntity)
                {
                    DxfInsertEntity e = entity as DxfInsertEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);

                    p = new PInstance(p0, e.BlockName);

                    // Need to account for scale and rotation
                    if (e.XScale != 1 || e.YScale != 1)
                    {
                        p.Scale(p.Origin, e.XScale, e.YScale);
                    }
                    if (e.Rotation != 0)
                    {
                        p.Rotate(p.Origin, -e.Rotation);
                    }
                }
                else if (entity is DxfViewportEntity)
                {
                    DxfViewportEntity e = entity as DxfViewportEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);
                    double width = dxc.WcsToModel(e.Width, e.Space);
                    double height = dxc.WcsToModel(e.Height, e.Space);

                    //p = new PRectangle(new Point(p0.X - width / 2, p0.Y - height / 2), width, height);
                }
                else if (entity is DxfPolylineEntity)
                {
                    DxfPolylineEntity e = entity as DxfPolylineEntity;

                    List<DxfLwpolylineVertex> vertexList = new List<DxfLwpolylineVertex>();

                    float maxwidth = 0;

                    foreach (DxfVertexEntity vertex in e.VertexList)
                    {
                        Point pt = dxc.WcsToModel(vertex.X0, vertex.Y0, e.Space);

                        DxfLwpolylineVertex v = new DxfLwpolylineVertex((float)pt.X, (float)pt.Y, vertex.Bulge);
                        v.StartWidth = dxc.WcsToModel(vertex.StartWidth, e.Space);
                        v.EndWidth = dxc.WcsToModel(vertex.EndWidth, e.Space);

                        maxwidth = Math.Max(v.StartWidth, maxwidth);
                        maxwidth = Math.Max(v.EndWidth, maxwidth);

                        vertexList.Add(v);
                    }

                    if ((e.Flags & 0x1) == 0x1)
                    {
                        vertexList.Add(new DxfLwpolylineVertex(vertexList[0].X0, vertexList[0].Y0, vertexList[0].Bulge));
                    }

                    p = DrawPolyline(dxfDoc, group, entity, p, vertexList, maxwidth);
                }
                else if (entity is DxfLwpolylineEntity)
                {
                    DxfLwpolylineEntity e = entity as DxfLwpolylineEntity;

                    List<DxfLwpolylineVertex> vertexList = new List<DxfLwpolylineVertex>();

                    float maxwidth = 0;

                    foreach (DxfLwpolylineVertex vertex in e.VertexList)
                    {
                        Point pt = dxc.WcsToModel(vertex.X0, vertex.Y0, e.Space);

                        DxfLwpolylineVertex v = new DxfLwpolylineVertex((float)pt.X, (float)pt.Y, vertex.Bulge);
                        v.StartWidth = dxc.WcsToModel(vertex.StartWidth, e.Space);
                        v.EndWidth = dxc.WcsToModel(vertex.EndWidth, e.Space);

                        maxwidth = Math.Max(v.StartWidth, maxwidth);
                        maxwidth = Math.Max(v.EndWidth, maxwidth);

                        vertexList.Add(v);   
                    }

                    if ((e.Flags & 0x1) == 0x1)
                    {
                        vertexList.Add(new DxfLwpolylineVertex(vertexList[0].X0, vertexList[0].Y0, vertexList[0].Bulge));
                    }

                    if (e.ConstantWidth > 0)
                    {
                        double width = dxc.WcsToModel(e.ConstantWidth, e.Space);

                        PDoubleline pdb = null;

                        for (int i = 0; i < vertexList.Count; i++)
                        {
                            Point A = new Point(vertexList[i].X0, vertexList[i].Y0);

                            if (vertexList[i].Bulge == 0)
                            {
                                // Doulbleline segment
                                if (pdb == null)
                                {
                                    if ((i + 1) <= e.VertexCount)
                                    {
                                        Point B = new Point(vertexList[i + 1].X0, vertexList[i + 1].Y0);
                                        pdb = new PDoubleline(new CPoint(A, 0), new CPoint(B, 3));
                                        pdb.Width = width;
                                        pdb.Fill = (uint)ColorCode.SameAsOutline;
                                        i++;
                                    }
                                }
                                else
                                {
                                    pdb.AddPoint(A.X, A.Y, false);
                                }
                            }
                            else if ((i + 1) < vertexList.Count)
                            {
                                Point B = new Point(vertexList[i + 1].X0, vertexList[i + 1].Y0);

                                // Arc segment
                                if (pdb != null)
                                {
                                    pdb.AddPoint(A.X, A.Y, false);
                                    AddPrimitive(dxfDoc, entity, pdb, group);
                                    pdb = null;
                                }

                                double bulge = vertexList[i].Bulge;

                                Primitive pa = PolyArc(A, B, bulge, width, width);
                                AddPrimitive(dxfDoc, entity, pa, group);
                            }
                        }

                        p = pdb;
                    }
                    else
                    {
                        p = DrawPolyline(dxfDoc, group, entity, p, vertexList, maxwidth);
                    }
                }
                else if (entity is DxfSplineEntity)
                {
                    DxfSplineEntity e = entity as DxfSplineEntity;

                    List<Point> points = new List<Point>();
                    foreach (DxfSplineVertex vertex in e.ControlPointList)
                    {
                        points.Add(dxc.WcsToModel(vertex.X0, vertex.Y0, e.Space));
                    }

                    PBSpline ps = new PBSpline(points[0], points[1]);

                    for (int i = 2; i < e.ControlPointList.Count; i++)
                    {
                        ps.AddPoint(points[i].X, points[i].Y, false);
                    }

                    p = ps;
                }
                else if (entity is DxfDimensionEntity)
                {
                    DxfDimensionEntity e = entity as DxfDimensionEntity;

                    Point p0 = dxc.WcsToModel(e.InsertionPoint.X, e.InsertionPoint.Y, e.Space);
                    p = new PInstance(p0, e.BlockName);
                }
                else if (entity is DxfEllipseEntity)
                {
                    DxfEllipseEntity e = entity as DxfEllipseEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);
                    
                    Point m = new Point(dxc.WcsToModel(e.X1, e.Space), dxc.WcsToModel(e.Y1, e.Space));
                    double major = Construct.Distance(new Point(), m);
                    double angle = -Construct.Angle(new Point(), m);
                    double included = Construct.IncludedAngle(e.Start, e.End, true);

                    //if (e.ExtrudeZ < 0)
                    //{
                    //    // based on comments from stack overflow, this should be correct
                    //    // need to verify
                    //    included = -included;
                    //}

                    p = new PEllipse(p0, major, major * e.Ratio, angle, -e.Start, -included);
                }
                else if (entity is Dxf3DFaceEntity)
                {
                    Dxf3DFaceEntity e = entity as Dxf3DFaceEntity;
                    // Not implemented
                }
                else if (entity is DxfTraceEntity)
                {
                    DxfTraceEntity e = entity as DxfTraceEntity;
                    // Not implemented
                }
                else if (entity is DxfViewportEntity)
                {
                    DxfViewportEntity e = entity as DxfViewportEntity;
                    // Not implemented
                }
                else if (entity is DxfPointEntity)
                {
                    DxfPointEntity e = entity as DxfPointEntity;
                    // Not implemented
                }
                else if (entity is DxfHatchEntity)
                {
                    DxfHatchEntity e = entity as DxfHatchEntity;
                    // Not implemented
                }
                else if (entity is DxfLeaderEntity)
                {
                    DxfLeaderEntity e = entity as DxfLeaderEntity;
                    // Not implemented
                }
                else if (entity is DxfAttdefEntity)
                {
                    DxfAttdefEntity e = entity as DxfAttdefEntity;
                    // Not implemented
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("DXF import: Unimplemented DXF entity: " + entity.Type);
                }

                if (p != null)
                {
                    AddPrimitive(dxfDoc, entity, p, group);
                }
            }

            if (cpEntity != null)
            {
                PLine pline = new PLine(cpBase);
                pline.CPoints = cpoints;
                AddPrimitive(dxfDoc, cpEntity, pline, group);
            }
        }

        private static Primitive DrawPolyline(DxfDocument dxfDoc, Group group, DxfEntity entity, Primitive p, List<DxfLwpolylineVertex> vertexList, float maxwidth)
        {
            if (maxwidth == 0)
            {
                PLine pline = null;

                for (int i = 0; i < vertexList.Count; i++)
                {
                    Point A = new Point(vertexList[i].X0, vertexList[i].Y0);

                    if (vertexList[i].Bulge == 0)
                    {
                        // Line segment
                        if (pline == null)
                        {
                            if ((i + 1) < vertexList.Count)
                            {
                                Point B = new Point(vertexList[i + 1].X0, vertexList[i + 1].Y0);
                                pline = new PLine(A, B);
                                i++;
                            }
                        }
                        else
                        {
                            pline.AddPoint(A.X, A.Y, false);
                        }
                    }
                    else if ((i + 1) < vertexList.Count)
                    {
                        // Arc segment
                        Point B = new Point(vertexList[i + 1].X0, vertexList[i + 1].Y0);

                        if (pline != null)
                        {
                            pline.AddPoint(A.X, A.Y, false);
                            AddPrimitive(dxfDoc, entity, pline, group);
                            pline = null;
                        }

                        double bulge = vertexList[i].Bulge;

                        Primitive pa = PolyArc(A, B, bulge, 0, 0);
                        AddPrimitive(dxfDoc, entity, pa, group);
                    }
                }

                p = pline;
            }
            else
            {
                for (int i = 0; i < vertexList.Count - 1; i++)
                {
                    Point A = new Point(vertexList[i].X0, vertexList[i].Y0);
                    Point C = new Point(vertexList[i + 1].X0, vertexList[i + 1].Y0);

                    double bulge = vertexList[i].Bulge;

                    if (bulge == 0)
                    {
                        Point B = A;
                        Point D = C;

                        if (i > 0 && vertexList[i - 1].Bulge == 0)
                        {
                            A = new Point(vertexList[i - 1].X0, vertexList[i - 1].Y0);
                        }

                        if (i < (vertexList.Count - 2) && vertexList[i + 1].Bulge == 0)
                        {
                            D = new Point(vertexList[i + 2].X0, vertexList[i + 2].Y0);
                        }

                        Primitive ps = PolySegment(A, B, C, D, vertexList[i].StartWidth, vertexList[i].EndWidth);
                        AddPrimitive(dxfDoc, entity, ps, group);
                    }
                    else if (i > 0)
                    {
                        Primitive pa = PolyArc(A, C, bulge, vertexList[i].StartWidth, vertexList[i].EndWidth);
                        AddPrimitive(dxfDoc, entity, pa, group);
                    }
                }
            }
            return p;
        }

        private static string ACText(string p)
        {
            if (p.Contains("%%"))
            {
                p = p.Replace("%%d", "°");
                p = p.Replace("%%D", "°");
                p = p.Replace("%%p", "±");
                p = p.Replace("%%P", "±");
                p = p.Replace("%%c", "Ø");
                p = p.Replace("%%C", "Ø");
                p = p.Replace("%%%", "%");

                // TrueView underlines text beginning with %%U.  We'll ignore it.
                p = p.Replace("%%U", "");
                p = p.Replace("%%u", "");

                // %% followed by a number converts should be converted to 
                // the character with that value in a given font (%%65='A' %%66='B' ...)
                // For now, we convert these characters to "?"

                string pattern = @"%%([0-9]+)";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(p, pattern);
                while (match.Success)
                {
                    p = p.Replace(match.Value, "?");
                    match = match.NextMatch();
                }

                // TrueView removes %% in (most?) other cases and leaves the following character as visible text.
                p = p.Replace("%%", "");
            }

            if (p.Contains(@"\U+"))
            {
                //p = p.Replace(@"\U+00B0", "°");
                //p = p.Replace(@"\U+00B1", "±");
                //p = p.Replace(@"\U+2205", "Ø");

                string pattern = @"\\U\+([0-9]+)";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(p, pattern);
                while (match.Success)
                {
                    string s = match.Value.Length > 7 ? match.Value.Substring(0, 7) : match.Value;

                    switch (s)
                    {
                        case @"\U+00B0":
                            p = p.Replace(s, "°");
                            break;

                        case @"\U+00B1":
                            p = p.Replace(s, "±");
                            break;

                        case @"\U+00B2":
                            p = p.Replace(s, "²");
                            break;

                        case @"\U+00B3":
                            p = p.Replace(s, "³");
                            break;

                        case @"\U+0278":
                            p = p.Replace(s, "ᛰ");      // Not Segoe UI equivalent
                            break;

                        case @"\U+0394":
                            p = p.Replace(s, "Δ");
                            break;

                        case @"\U+03a9":
                            p = p.Replace(s, "Ω");
                            break;

                        case @"\U+2082":
                            p = p.Replace(s, "₂");
                            break;

                        case @"\U+2104":
                            p = p.Replace(s, "℄");
                            break;

                        case @"\U+2126":
                            p = p.Replace(s, "Ω");
                            break;

                        case @"\U+2220":
                            p = p.Replace(s, "∠");
                            break;

                        case @"\U+2205":
                            p = p.Replace(s, "Ø");
                            break;

                        case @"\U+2248":
                            p = p.Replace(s, "≈");
                            break;

                        case @"\U+2260":
                            p = p.Replace(s, "≠");
                            break;

                        case @"\U+2261":
                            p = p.Replace(s, "≡");
                            break;

                        default:
                            p = p.Replace(s, "?");
                            break;
                    }

                    match = match.NextMatch();
                }
            }

            if (p.Contains("{"))
            {
                int i = 0;

                while ((i = p.IndexOf("{", i)) >= 0)
                {
                    if (i == 0 || p[i - 1] != '\\')
                    {
                        p = p.Remove(i, 1);
                    }
                }

                i = 0;

                while ((i = p.IndexOf("}", i)) >= 0)
                {
                    char c1 = p[i];
                    char c2 = p[i - 1];

                    if (i == 0 || p[i - 1] != '\\')
                    {
                        p = p.Remove(i, 1);
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            if (p.Contains(@"\"))
            {
                p = p.Replace(@"\A1;", "");
                p = p.Replace("\\P", "\n");
            }

            return p;
        }

        protected static void AddPrimitive(DxfDocument dxfDoc, DxfEntity entity, Primitive p, Group group)
        {
            if (group != null && entity.Layer == "0")
            {
                // I think this means "layer BYBLOCK" but I can't find support in the documentation
                p.LayerId = 0;
            }
            else
            {
                //p.LayerId = (int)dxfDoc.LAYERList[entity.Layer.ToUpper()].Tag;
                string layerName = DxfExport.FixAcName(entity.Layer);
                p.LayerId = dxfDoc.LAYERList.ContainsKey(layerName) ? (int)dxfDoc.LAYERList[layerName].Tag : 0;
            }

            if (entity.Color == 0 && group != null && p.LayerId != 0)
            {
                // For entities, color = means theme forground
                // For group members, if the member layer is BYBLOCK we use the layer color
                p.ColorSpec = (uint)ColorCode.ThemeForeground;
            }
            else
            {
                p.ColorSpec = entity.Color > 0 ? Utilities.ColorSpecFromAutoCadColor(entity.Color) : (uint)ColorCode.ByLayer;
            }

            //p.LineTypeId = ltypeName == "BYLAYER" ? -1 : (int)dxfDoc.LTYPEList[ltypeName].Tag;
            string ltypeName = DxfExport.FixAcName(entity.Ltype);
            if (ltypeName == "BYLAYER")
            {
                p.LineTypeId = -1;
            }
            else if (dxfDoc.LTYPEList.ContainsKey(ltypeName))
            {
                p.LineTypeId = (int)dxfDoc.LTYPEList[ltypeName].Tag;
            }
            else
            {
                p.LineTypeId = -1;
            }

            p.LineWeightId = -1;


            if (group == null)
            {
                p.AddToContainer(Globals.ActiveDrawing);
            }
            else
            {
                p.IsGroupMember = true;
                group.AddMember(p);
            }
        }

        protected static Primitive PolySegment(Point A, Point B, Point C, Point D, double sw, double ew)
        {
            if (sw == 0 && ew == 0)
            {
                return new PLine(B, C);
            }

            bool start = (A.X == B.X && A.Y == B.Y) || sw == 0;
            bool end = (C.X == D.X && C.Y == D.Y) || ew == 0;

            double a0 = Construct.Angle(B, C) + Math.PI / 2;
            double a1 = Construct.Angle(B, C) + Math.PI / 2;

            double hsw = sw / 2;
            double hew = ew / 2;

            Point p0 = new Point();
            Point p1 = new Point();
            Point p2 = new Point();
            Point p3 = new Point();

            if (sw == 0)
            {
                p0 = p3 = B;
            }
            else
            {
                p0 = Construct.PolarOffset(B, hsw, a0);
                p3 = Construct.PolarOffset(B, -hsw, a0);
            }

            if (ew == 0)
            {
                p1 = p2 = C;
            }
            else
            {
                p1 = Construct.PolarOffset(C, hew, a1);
                p2 = Construct.PolarOffset(C, -hew, a1);
            }

            if (!start)
            {
                double a0a = Construct.Angle(A, B) + Math.PI / 2;
                double a0b = (a0 + a0a) / 2;
                Point p0a = Construct.PolarOffset(B, hsw, a0b);
                Point p3a = Construct.PolarOffset(B, -hsw, a0b);

                Point p0b = Construct.IntersectLineLine(p0a, p3a, p1, p0);
                Point p3b = Construct.IntersectLineLine(p0a, p3a, p2, p3);

                p0 = p0b;
                p3 = p3b;
            }

            if (!end)
            {
                double a1a = Construct.Angle(C, D) + Math.PI / 2;
                double a1b = (a1 + a1a) / 2;
                Point p1a = Construct.PolarOffset(C, hew, a1b);
                Point p2a = Construct.PolarOffset(C, -hew, a1b);

                Point p1b = Construct.IntersectLineLine(p1a, p2a, p1, p0);
                Point p2b = Construct.IntersectLineLine(p1a, p2a, p2, p3);

                p1 = p1b;
                p2 = p2b;
            }

            PPolygon p = new PPolygon(p0);
            p.AddPoint(p1.X, p1.Y, false);
            p.AddPoint(p2.X, p2.Y, false);
            p.AddPoint(p3.X, p3.Y, false);
            p.AddPoint(p0.X, p0.Y, false);

            return p;
        }

        protected static Primitive PolyArc(Point B, Point C, double bulge, double sw, double ew)
        {
            Primitive p = null;

	        double a = -Math.Atan(bulge) * 2;
	        double d = Construct.Distance(B, C);
	        double r = Math.Abs(d / (2 * Math.Sin(a)));

            Point center = new Point();

            bool left = bulge < 0; // Math.Abs(bulge) > 1 ? bulge < 0 : bulge > 0;    // Is bulge ever > 1?
	
	        if (Math.Abs(bulge) == 1)
            {
                center.X = (B.X + C.X) / 2;
                center.Y = (B.Y + C.Y) / 2;
            }
            else if (B.X != C.X || B.Y != C.Y)
            {
                center = PolyArcCenter(B, C, r, left);
            }

            double startAngle = Construct.Angle(center, B);
	        double inclAngle = a * 2;

            int n = Math.Max(CGeometry.ArcChordCount(r, inclAngle), 2);
	
            double ainc = inclAngle / n;
            double rinc = (ew - sw) / (2 * (n - 1));

            if (sw == 0 && ew == 0)
            {
                p = new PArc(center, r, startAngle, inclAngle);
            }
            else
            {
                double inside = r - sw / 2;
                double outside = r + sw / 2;

                List<Point> pci = new List<Point>();
                List<Point> pco = new List<Point>();

                pci.Add(new Point(center.X + inside * Math.Cos(startAngle), center.Y + inside * Math.Sin(startAngle)));
                pco.Add(new Point(center.X + outside * Math.Cos(startAngle), center.Y + outside * Math.Sin(startAngle)));

                for (int j = 0; j < n; ++j)
                {
                    startAngle += ainc;

                    pci.Add(new Point(center.X + inside * Math.Cos(startAngle), center.Y + inside * Math.Sin(startAngle)));
                    pco.Add(new Point(center.X + outside * Math.Cos(startAngle), center.Y + outside * Math.Sin(startAngle)));
		
                    inside -= rinc;
                    outside += rinc;
                }

                //pco = Construct.Reverse(pco);

                PPolygon pgon = new PPolygon(pci[0]);

                for (int i = 1; i < pci.Count; i++)
                {
                    pgon.AddPoint(pci[i].X, pci[i].Y, false);
                }

                for (int i = pco.Count - 1; i >= 0; --i)
                {
                    pgon.AddPoint(pco[i].X, pco[i].Y, false);
                }
               
                pgon.AddPoint(pci[0].X, pci[0].Y, false);

                p = pgon;
            }

            return p;
        }

        protected static Point PolyArcCenter(Point a, Point b, double radius, bool left)
        {
            double A, B, D, K, R;

            R = (double)radius;
            A = b.X - a.X;
            B = b.Y - a.Y;
            D = A * A + B * B;
            K = Math.Sqrt((R * R) / D - .25);

            Point c;

            if (left)
            {
                c = new Point(a.X + .5 * A - K * B, a.Y + .5 * B + K * A);
            }
            else
            {
                c = new Point(a.X + .5 * A + K * B, a.Y + .5 * B - K * A);
            }

            return c;
        }


        protected static Point _ll = new Point(double.MaxValue, double.MaxValue);
        protected static Point _ur = new Point(double.MinValue, double.MinValue);

        protected static void initLimits()
        {
            _ll = new Point(double.MaxValue, double.MaxValue);
            _ur = new Point(double.MinValue, double.MinValue);
        }

        protected static void addToLimits(Point p)
        {
            _ll.X = Math.Min(p.X, _ll.X);
            _ll.Y = Math.Min(p.Y, _ll.Y);
            _ur.X = Math.Max(p.X, _ur.X);
            _ur.Y = Math.Max(p.Y, _ur.Y);
        }

        public static Rect ScanForModelExtents(DxfContext dxc, List<DxfEntity> entities)
        {
            initLimits();

            foreach (DxfEntity entity in entities)
            {
                if (entity is DxfLineEntity)
                {
                    DxfLineEntity e = entity as DxfLineEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);
                    Point p1 = dxc.WcsToModel(e.X1, e.Y1, e.Space);

                    addToLimits(p0);
                    addToLimits(p1);
                }
                else if (entity is DxfSolidEntity)
                {
                    DxfSolidEntity e = entity as DxfSolidEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);
                    Point p1 = dxc.WcsToModel(e.X1, e.Y1, e.Space);
                    Point p2 = dxc.WcsToModel(e.X2, e.Y2, e.Space);
                    Point p3 = dxc.WcsToModel(e.X3, e.Y3, e.Space);

                    addToLimits(p0);
                    addToLimits(p1);
                    addToLimits(p2);
                    addToLimits(p3);
                }
                else if (entity is DxfArcEntity)
                {
                    DxfArcEntity e = entity as DxfArcEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);

                    double start = (-e.StartAngle) / Construct.cRadiansToDegrees;
                    double included = (-e.EndAngle - (-e.StartAngle)) / Construct.cRadiansToDegrees;

                    if (e.EndAngle < e.StartAngle)
                    {
                        included = (-e.EndAngle - 360 - (-e.StartAngle)) / Construct.cRadiansToDegrees;
                    }

                    addToLimits(new Point(p0.X - e.Radius, p0.Y - e.Radius));
                    addToLimits(new Point(p0.X + e.Radius, p0.Y + e.Radius));
                }
                else if (entity is DxfCircleEntity)
                {
                    DxfCircleEntity e = entity as DxfCircleEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);

                    addToLimits(new Point(p0.X - e.Radius, p0.Y - e.Radius));
                    addToLimits(new Point(p0.X + e.Radius, p0.Y + e.Radius));
                }
                else if (entity is DxfTextEntity)
                {
                    DxfTextEntity e = entity as DxfTextEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);
                    Point p1 = dxc.WcsToModel(e.X1, e.Y1, e.Space);

                    //TextAlignment align = TextAlignment.Left;
                    //TextPosition position = TextPosition.Above;

                    addToLimits(p0);
                    addToLimits(p1);
                    // Add text box
                }
                else if (entity is DxfMTextEntity)
                {
                    DxfMTextEntity e = entity as DxfMTextEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);
                    Point p1 = dxc.WcsToModel(e.X1, e.Y1, e.Space);

                    addToLimits(p0);
                    addToLimits(p1);
                    // Add text box
                }
                else if (entity is DxfInsertEntity)
                {
                    DxfInsertEntity e = entity as DxfInsertEntity;
                    Point p0 = dxc.WcsToModel(e.X0, e.Y0, e.Space);

                    addToLimits(p0);
                    // Add group limits
                }
                else if (entity is DxfLwpolylineEntity)
                {
                    DxfLwpolylineEntity e = entity as DxfLwpolylineEntity;

                    foreach (DxfLwpolylineVertex vertex in e.VertexList)
                    {
                        Point pt = dxc.WcsToModel(vertex.X0, vertex.Y0, e.Space);
                        addToLimits(pt);
                    }
                }
                else
                {
                    // Unimplemented DXF entity - assume that is is not important to the extent calculation
                }
            }

            return new Rect(_ll, _ur);
        }

        public static async Task<int> ImportDxfAsync(StorageFile file)
        {
            int result = 1;

            if (file != null)
            {
                BasicProperties props = await file.GetBasicPropertiesAsync();
                Globals.Events.DrawingLoading(file.Name, props.Size);

                string fileContent = "";

                try
                {
                    using (Stream fileStream = await file.OpenStreamForReadAsync())
                    {
                        System.IO.StreamReader reader = new System.IO.StreamReader(fileStream);
                        fileContent = reader.ReadToEnd();
                        reader.Dispose();
                    }

                    //int crashcount = 0;

                    try
                    {
                        DxfReader dr = new DxfReader(fileContent);
                        dr.ReadDocument();

                        DxfDocument dxfDoc = dr.Document;
                        DxfContext dxc = new DxfContext(dxfDoc);

                        if (dxc.ModelExtents.IsEmpty)
                        {
                            dxc.ModelExtents = ScanForModelExtents(dxc, dxfDoc.ENTITYList);
                        }

                        if (dxc.Metric)
                        {
                            Globals.ActiveDrawing.NewEmptyDrawing(dxc.DwgSize.Width, dxc.DwgSize.Height, dxc.DrawingScale, Unit.Millimeters, Unit.Millimeters, false, Globals.Themes["dark"]);
                            Globals.ActiveDrawing.Origin = new Point(-dxc.ModelOrigin.X, -dxc.ModelOrigin.Y);
                        }
                        else
                        {
                            //Globals.ActiveDrawing.NewEmptyDrawing(dxc.DwgSize.Width, dxc.DwgSize.Height, 1, Unit.Inches, Unit.Inches, false, Globals.Themes["dark"]);
                            Globals.ActiveDrawing.NewEmptyDrawing(dxc.DwgSize.Width, dxc.DwgSize.Height, dxc.DrawingScale, Unit.Inches, Unit.Inches, false, Globals.Themes["dark"]);
                        }

                        Globals.LayerTable.Clear();
                        Globals.LineTypeTable.Clear();

                        Globals.ActiveDrawing.AddLayer("Unassigned", (uint)ColorCode.ThemeForeground, 0, 10);
                        Globals.ActiveDrawing.AddLineType("Solid", null);

                        //System.Diagnostics.Debug.WriteLine("DxfIO: crashcount = {0}", ++crashcount);


                        try
                        {
                            foreach (DxfLtype ltype in dxfDoc.LTYPEList.Values)
                            {
                                DoubleCollection dc = new DoubleCollection();
                                double min = dxc.Metric ? .008 : .02;

                                foreach (float f in ltype.LTypeElements)
                                {
                                    if (Math.Abs(f) < min)
                                    {
                                        dc.Add(min);
                                    }
                                    else if (dxc.Metric)
                                    {
                                        dc.Add(Math.Abs(f / 25.4));
                                    }
                                    else
                                    {
                                        dc.Add(Math.Abs(f));
                                    }
                                }

                                int id = Globals.LineTypeTable.Count;
                                ltype.Tag = id;
                                Globals.LineTypeTable.Add(id, new LineType(id, DxfExport.FixAcName(ltype.Name), dc));
                            }

                            foreach (DxfLayer dlayer in dxfDoc.LAYERList.Values)
                            {
                                int colorId = dlayer.Color;
                                bool isVisible = true;
                                if (colorId < 0)
                                {
                                    colorId = -colorId;
                                    isVisible = false;
                                }
                                uint colorSpec = Utilities.ColorSpecFromAutoCadColor(colorId);

                                int lineWeight = 8;

                                if (dlayer.Lineweight != null)
                                {
                                    try
                                    {
                                        int aw = int.Parse(dlayer.Lineweight);
                                        lineWeight = aw < 1 ? 8 : (int)Math.Ceiling(aw * 0.00254 * 100);
                                    }
                                    catch
                                    {
                                    }
                                }

                                dlayer.Ltype = DxfExport.FixAcName(dlayer.Ltype);
                                int lineTypeId = 0;
                                if (dxfDoc.LTYPEList.ContainsKey(dlayer.Ltype))
                                {
                                    lineTypeId = (int)dxfDoc.LTYPEList[dlayer.Ltype].Tag;
                                }
                                int layerId = -1;

                                string fixedName = DxfExport.FixAcName(dlayer.Name);
                                foreach (Layer layer in Globals.LayerTable.Values)
                                {
                                    if (DxfExport.FixAcName(layer.Name) == fixedName)
                                    {
                                        layerId = layer.Id;
                                        break;
                                    }
                                }

                                if (layerId == -1)
                                {
                                    layerId = Globals.LayerTable.Count;
                                    Globals.LayerTable.Add(layerId, new Layer(layerId, dlayer.Name, colorSpec, lineTypeId, lineWeight));
                                }
                                else
                                {
                                    Globals.LayerTable[layerId].ColorSpec = colorSpec;
                                    Globals.LayerTable[layerId].LineTypeId = lineTypeId;
                                    Globals.LayerTable[layerId].LineWeightId = lineWeight;
                                }

                                dlayer.Tag = layerId;
                                Globals.LayerTable[layerId].Visible = isVisible;
                            }

                            foreach (DxfStyle dstyle in dxfDoc.STYLEList.Values)
                            {
                                dstyle.Tag = null;

                                foreach (TextStyle style in Globals.TextStyleTable.Values)
                                {
                                    if (style.Name == dstyle.Name)
                                    {
                                        dstyle.Tag = style.Id;
                                    }
                                }

                                if (dstyle.Tag == null)
                                {
                                    // Select appropriate font
                                    // Set approprate offset
                                    double height = dxc.WcsToModel(dstyle.Height == 0 ? dstyle.LastHeight : dstyle.Height, 0);
                                    height = height == 0 ? .125 : height;
                                    dstyle.Tag = Globals.ActiveDrawing.AddTextStyle(dstyle.Name, "Arial", height, 0, 1.5, 1);
                                }
                            }

                            foreach (DxfBlock block in dxfDoc.BLOCKList.Values)
                            {
                                DxfContext groupContext = new DxfContext(dxc);

                                //System.Diagnostics.Debug.WriteLine("Block: {0}", block.Name);

                                //TODO
                                // Group name needs to be unique - verify
                                Group group = new Group(block.Name);

                                group.PaperBounds = groupContext.ModelExtents;

                                Globals.ActiveDrawing.AddGroup(group);

                                ImportDxfEntities(dxfDoc, groupContext, block.Entities, group);

                                groupContext.ModelExtents = ScanForModelExtents(groupContext, block.Entities);
                                group.PaperBounds = groupContext.ModelExtents;
                                group.UpdateStyleLists();
                                //group.IncludeInLibrary = group.Name.StartsWith("*") == false;
                            }

                            ImportDxfEntities(dxfDoc, dxc, dxfDoc.ENTITYList, null);

                            //TODO
                            // Need to verify that each of the lists is non-empty - there needs to be a valid setting at this point
                            Globals.ActiveDrawing.AttributeListsChanged();

                            Globals.Events.GridChanged();
                            Globals.Events.ShowRulers(Globals.ShowRulers);
                            Globals.View.DisplayAll();

                            Globals.Events.DrawingLoaded(file.Name);
                        }
                        catch (Exception ex)
                        {
                            // Catch DXF data errors
                            Analytics.ReportError("DxfContentError", ex, 2, 219);
                            result = -3;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Catch DXF format errors
                        Analytics.ReportError("DxfFormatError", ex, 2, 220);
                        result = -2;
                    }
                }
                catch (Exception ex)
                {
                    // Catch IO errors
                    Analytics.ReportError("DxfIOError", ex, 2, 221);
                    result = -1;
                }
            }
            else
            {
                result = 0; // No file returned from picker, assume cancelled
            }

            switch (result)
            {
                case -3:
                    await Cirros.Alerts.StandardAlerts.DxfContentError();
                    break;

                case -2:
                    await Cirros.Alerts.StandardAlerts.DxfFormatError();
                    break;

                case -1:
                    await Cirros.Alerts.StandardAlerts.DxfIOError();
                    break;

                case 0:
                default:
                    break;
            }

            List<Primitive> list = new List<Primitive>();
            Rect bounds = Rect.Empty;

            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                bounds.Union(p.Box);
            }

            if (bounds.Left < 0 || bounds.Right > Globals.ActiveDrawing.PaperSize.Width || bounds.Top < 0 || bounds.Bottom > Globals.ActiveDrawing.PaperSize.Height)
            {
                if (bounds.Width < 60 && bounds.Height < 60)
                {
                    //System.Diagnostics.Debug.WriteLine("DXF bounds: x:{0}; y:{1}; width:{2}; height{3};", bounds.X, bounds.Y, bounds.Width, bounds.Height);
                    //System.Diagnostics.Debug.WriteLine("Drawing size: width:{0}; height{1}   Scale:{2};", Globals.ActiveDrawing.PaperSize.Width, Globals.ActiveDrawing.PaperSize.Height, dxc.Scale);

                    Globals.ActiveDrawing.PaperSize = new Size(Math.Max(Globals.ActiveDrawing.PaperSize.Width, bounds.Right - bounds.Left),
                        Math.Max(Globals.ActiveDrawing.PaperSize.Height, bounds.Bottom - bounds.Top));

                    double bcx = (bounds.Left + bounds.Right) / 2;
                    double bcy = (bounds.Bottom + bounds.Top) / 2;
                    double dcx = Globals.ActiveDrawing.PaperSize.Width / 2;
                    double dcy = Globals.ActiveDrawing.PaperSize.Height / 2;

                    double offsetX = dcx - bcx;
                    double offsetY = dcy - bcy;

                    foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
                    {
                        p.MoveByDelta(offsetX, offsetY);
                        p.Draw();
                    }

                    Point po = Globals.ActiveDrawing.ModelToPaper(Globals.ActiveDrawing.Origin);
                    Point mo = Globals.ActiveDrawing.PaperToModel(new Point(po.X + offsetX, po.Y - offsetY));
                    Globals.ActiveDrawing.Origin = mo;

                    Analytics.ReportEvent("DXFImport-shifted-drawing-content");
                }
                else
                {
                    Analytics.ReportError("DXF bounds error", null, 4, 222);
                }
            }

            return result;
        }
    }
}
