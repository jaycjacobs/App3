using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Core
{
    public class SvgExport
    {
        static SvgExport()
        {
            FontHeightAdj = new Dictionary<string, double>();

            FontHeightAdj.Add("Courier New", 1.6);
            FontHeightAdj.Add("Segoe UI", 1.4);
            FontHeightAdj.Add("Segoe Print", 1.4);
            FontHeightAdj.Add("Tahoma", 1.36);
            FontHeightAdj.Add("Verdana", 1.35);
            FontHeightAdj.Add("Trebuchet MS", 1.35);
            FontHeightAdj.Add("Calibri", 1.56);
            FontHeightAdj.Add("Arial", 1.4);
            FontHeightAdj.Add("Cambria", 1.48);
            FontHeightAdj.Add("Georgia", 1.44);
            FontHeightAdj.Add("Times New Roman", 1.48);
        }

        public static Dictionary<string, double> FontHeightAdj = null;
        public static Dictionary<int, string> LineTypes = null;

        protected static async Task exportEntityToSvg(VectorEntity ve, StringBuilder sb, double scale)
        {
            List<List<Point>> polygonFigures = new List<List<Point>>();

            double lineWidth = ve.LineWidth * scale; // Math.Max(ve.LineWidth * scale, 1);
            string color = svgColor(ve.Color);
            string linetype = "";

            if (ve.Children != null)
            {
                foreach (object o in ve.Children)
                {
                    if (o is List<Point>)
                    {
                        List<Point> pc = o as List<Point>;
                          
                        linetype = LineTypes.ContainsKey(ve.LineType) ? LineTypes[ve.LineType] : "";

                        if (ve.Fill)
                        {
#if true
                            // Overlappling polygons with holes must be drawn atomically - add figure to the list
                            polygonFigures.Add(pc);
#else
                            string fillRule = ve.FillEvenOdd ? ";fill-rule:evenodd" : ";fill-rule:nonzero";
                            sb.AppendFormat("<polygon style=\"fill:{1};stroke:{0};stroke-width:{2:0.####}{4}\"{3}", color, svgColor(ve.FillColor), lineWidth, linetype, fillRule);
                            sb.AppendFormat(" points=\"{0:0.####},{1:0.####}", pc[0].X * scale, pc[0].Y * scale);
                            for (int i = 1; i < pc.Count; i++)
                            {
                                sb.AppendFormat(" {0:0.####},{1:0.####}", pc[i].X * scale, pc[i].Y * scale);
                            }

                            sb.Append("\"/>\n");
#endif
                        }
                        else
                        {
                            sb.AppendFormat("<polyline style=\"fill:none;stroke:{0};stroke-width:{1:0.####}\"{2}", color, lineWidth, linetype);
                            sb.AppendFormat(" points=\"{0:0.####},{1:0.####}", pc[0].X * scale, pc[0].Y * scale);
                            for (int i = 1; i < pc.Count; i++)
                            {
                                sb.AppendFormat(" {0:0.####},{1:0.####}", pc[i].X * scale, pc[i].Y * scale);
                            }

                            sb.Append("\"/>\n");
                        }
                    }
                    else if (o is VectorEntity v)
                    {
                        if (v.IsVisible)
                        {
                            await exportEntityToSvg(v, sb, scale);
                        }
                    }
                    else if (o is VectorTextEntity vt)
                    {
                        string[] lines = vt.Text.Split(new[] { '\n' });

                        double tx = vt.Location.X * scale;
                        double ty = vt.Location.Y * scale;
                        double th = vt.TextHeight * scale;
                        double lh = vt.TextHeight * scale * vt.LineSpacing;

                        if (FontHeightAdj.ContainsKey(vt.FontFamily))
                        {
                            th *= FontHeightAdj[vt.FontFamily];
                        }
                        else
                        {
                            th = (float)Dx.GetFontSizeFromHeight(vt.FontFamily, (double)th);
                        }

                        double characterSpacing = (vt.CharacterSpacing - 1) * .8;

                        string anchor = svgTextAnchor(vt.TextAlignment);
                        string style = string.Format(" style=\"font-family:{0};font-size:{1:0.####}px;fill:{2};text-anchor:{3};letter-spacing:{4}em\"",
                            vt.FontFamily, th, color, anchor, characterSpacing);

                        if (vt.TextPosition == TextPosition.Above)
                        {
                            if (vt.Angle == 0)
                            {
                                ty -= (lines.Length - 1) * lh;
                            }
                            else
                            {
                                Point td = Construct.PolarOffset(new Point(0, 0), (lines.Length - 1) * lh, (vt.Angle + 90) / Construct.cRadiansToDegrees);
                                tx -= td.X;
                                ty -= td.Y;
                            }
                        }
                        else if (vt.TextPosition == TextPosition.On)
                        {
                            if (vt.Angle == 0)
                            {
                                ty -= (lines.Length - 1) * lh / 2;
                            }
                            else
                            {
                                Point td = Construct.PolarOffset(new Point(0, 0), (lines.Length - 1) * lh / 2, (vt.Angle + 90) / Construct.cRadiansToDegrees);
                                tx -= td.X;
                                ty -= td.Y;
                            }
                        }

                        string os = string.Format("x=\"{0:0.####}\" y=\"{1:0.####}\"", tx, ty);

                        string angleString = "";

                        if (vt.Angle != 0)
                        {
                            angleString = string.Format(" transform=\"rotate({0:0.####} {1:0.####},{2:0.####})\"", vt.Angle, tx, ty);
                        }

                        bool canDoTspan = false;

                        if (canDoTspan && lines.Length > 1)
                        {
                            sb.AppendFormat("<text {0} fill=\"{1}\"{2}{3}>", os, color, style, angleString);

                            foreach (string s in lines)
                            {
                                sb.AppendFormat("<tspan x=\"{0:0.####}\" y=\"{1:0.####}\">{2}</tspan>", tx, ty, WebUtility.HtmlEncode(s));
                                ty += lh;
                            }

                            sb.Append("</text>\n");
                        }
                        else
                        {
                            foreach (string s in lines)
                            {
                                //sb.AppendFormat("<circle cx = \"{0:0.####}\" cy = \"{1:0.####}\" r = \"{2:0.####}\"/>",
                                //    tx, ty, th / 5);
                                sb.AppendFormat("<text {0} fill=\"{1}\"{2}{3}>", os, color, style, angleString);
                                sb.Append(WebUtility.HtmlEncode(s));
                                sb.Append("</text>\n");
                                ty += lh;
                                os = string.Format("x=\"{0:0.####}\" y=\"{1:0.####}\"", tx, ty);
                            }
                        }
                    }
                    else if (o is VectorImageEntity)
                    {
                        VectorImageEntity vi = o as VectorImageEntity;

                        StorageFile file = await Utilities.GetImageSourceFileAsync(vi.ImageId);

                        if (file != null)
                        {
                            double x = vi.Origin.X * scale;
                            double y = vi.Origin.Y * scale;
                            double w = vi.Width * scale;
                            double h = vi.Height * scale;

                            sb.AppendFormat("<image x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" preserveAspectRatio=\"none\"", x, y, w, h);

                            if (vi.Opacity != 1)
                            {
                                sb.AppendFormat(" style=\"opacity:{0}\"", vi.Opacity);
                            }

                            if (vi.Matrix != null && vi.Matrix.IsIdentity == false)
                            {
                                sb.AppendFormat(" transform=\"translate({0}, {1}) matrix({2} {3} {4} {5} {6} {7}) translate(-{0}, -{1})\"",
                                    x, y, vi.Matrix.M11, vi.Matrix.M12, vi.Matrix.M21, vi.Matrix.M22, vi.Matrix.OffsetX, vi.Matrix.OffsetY);
                            }

                            sb.Append(" xlink:href=\"");
                            sb.Append(await Utilities.EncodeImage(file));
                            sb.Append("\"/>\n");
                        }
                    }
                }
            }

            if (polygonFigures.Count > 0)
            {
                string fillRule = ve.FillEvenOdd ? ";fill-rule:evenodd" : ";fill-rule:nonzero";
                sb.AppendFormat("<path style=\"fill:{1};stroke:{0};stroke-width:{2:0.####}{4}\"{3}", color, svgColor(ve.FillColor), lineWidth, linetype, fillRule);

                sb.AppendFormat(" d=\"");

                foreach (List<Point> pc in polygonFigures)
                {
                    sb.AppendFormat("M{0:0.####},{1:0.####} L", pc[0].X * scale, pc[0].Y * scale);
                    for (int i = 1; i < pc.Count; i++)
                    {
                        sb.AppendFormat("{0:0.####},{1:0.####} ", pc[i].X * scale, pc[i].Y * scale);
                    }
                }

                sb.Append("\"/>\n");
            }
        }

        private static string svgTextAnchor(TextAlignment textAlignment)
        {
            string anchor;

            switch (textAlignment)
            {
                default:
                case TextAlignment.Left:
                    anchor = "start";
                    break;
                case TextAlignment.Center:
                    anchor = "middle";
                    break;
                case TextAlignment.Right:
                    anchor = "end";
                    break;
            }

            return anchor;
        }

        private static string svgColor(Color color)
        {
            string s;
            if (color.A == 255)
            {
                s = string.Format("rgb({0},{1},{2})", color.R, color.G, color.B);
            }
            else
            {
                double a = Math.Round((double)color.A / 255, 3);
                s = string.Format("rgba({0},{1},{2},{3})", color.R, color.G, color.B, a);
            }

            return s;
        }

        private static string svgColorSpec(uint colorspec)
        {
            Color color = Utilities.ColorFromColorSpec(colorspec);
            return string.Format("rgb({0},{1},{2})", color.R, color.G, color.B);
        }

        public static async Task<string> ExportToSvg(bool showFrame)
        {
            StringBuilder sb = new StringBuilder();

            DrawingDocument dc = Globals.ActiveDrawing;
            if (dc != null)
            {
                string nameSpace = "xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" version=\"1.1\"";
                string svgTag = "<svg {0} width=\"{1:0.####}{3}\" height=\"{2:0.####}{3}\" viewBox=\"0,0,{1},{2}\">\n";
                string unit = "mm";
                double scale;
#if true
                if (Globals.ActiveDrawing.PaperUnit == Unit.Inches)
                {
                    scale = 1;
                    unit = "in";
                }
                else
                {
                    scale = 25.4;
                    unit = "mm";
                }
#else
                scale = 1000 / Math.Max(Globals.ActiveDrawing.PaperSize.Width, Globals.ActiveDrawing.PaperSize.Height);
#endif
                //string linecap;

                //switch (Globals.ActiveDrawing.LineEndCap)
                //{
                //    case PenLineCap.Round:
                //    default:
                //        linecap = "stroke-linecap:round;stroke-linejoin:round;";
                //        break;

                //    case PenLineCap.Flat:
                //        linecap = "stroke-linecap:butt;stroke-linejoin:bevel;";
                //        break;

                //    case PenLineCap.Square:
                //    case PenLineCap.Triangle:
                //        linecap = "stroke-linecap:square;stroke-linejoin:miter;";
                //        break;
                //}

                //System.Diagnostics.Debug.WriteLine("linecap = {0}", linecap);

                LineTypes = new Dictionary<int, string>();

                foreach (LineType type in Globals.LineTypeTable.Values)
                {
                    if (type.StrokeDashArray != null && type.StrokeDashArray.Count > 1)
                    {
                        StringBuilder ltsb = new StringBuilder();

                        ltsb.AppendFormat(" stroke-dasharray=\"{0:0.####}", type.StrokeDashArray[0] * scale);

                        for (int i = 1; i < type.StrokeDashArray.Count; i++)
                        {
                            ltsb.AppendFormat(",{0:0.####}", type.StrokeDashArray[i] * scale);
                        }

                        ltsb.Append("\"");

                        LineTypes.Add(type.Id, ltsb.ToString());
                    }
                }

                Size outputSize = new Size(Globals.ActiveDrawing.PaperSize.Width * scale, Globals.ActiveDrawing.PaperSize.Height * scale);

                // Document root
                sb.AppendFormat(svgTag, nameSpace, outputSize.Width, outputSize.Height, unit);

                // Background and border
                if (showFrame)
                {
                    //string borderColor = svgColor(showFrame ? Globals.ActiveDrawing.Theme.BorderColor : Globals.ActiveDrawing.Theme.BackgroundColor);
                    string borderColor = svgColor(Globals.ActiveDrawing.Theme.BorderColor);
                    sb.AppendFormat("<rect width=\"{0:0.####}\" height=\"{1:0.####}\" style=\"fill:{2};stroke-width:{4:0.####};stroke:{3}\"/>\n",
                        outputSize.Width, outputSize.Height,
                        svgColor(Globals.ActiveDrawing.Theme.BackgroundColor), borderColor, .008 * scale);
                }

                List<Primitive> primitives = new List<Primitive>();

                foreach (Primitive p in dc.PrimitiveList)
                {
                    if (Globals.LayerTable[p.LayerId].Visible)
                    {
                        primitives.Add(p);
                    }
                }

                primitives.Sort();

                VectorContext context = new VectorContext(false, true, true);

                foreach (Primitive p in primitives)
                {
                    VectorEntity ve = p.Vectorize(context);
                    await exportEntityToSvg(ve, sb, scale);
                }

                sb.Append("</svg>\n");
            }

            return sb.ToString();
        }
    }
}
