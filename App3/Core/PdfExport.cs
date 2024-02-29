using Cirros;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Cirros.Primitives;
using Microsoft.UI.Xaml;
using Cirros.Drawing;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Cirros.Utility;
using Windows.UI;

namespace Cirros.Pdf
{
    public class PdfExport
    {
        public PdfExport()
        {
        }

        static Dictionary<int, double[]> LineTypes = null;
        static Dictionary<string, string> _opacityDictionary = new Dictionary<string, string>();
        static double _docHeight;
        static double _docWidth;

        public static async Task ExportDrawingToPdfAsync(StorageFile file, bool showFrame)
        {
            try
            {
                LineTypes = new Dictionary<int, double[]>();

                foreach (LineType type in Globals.LineTypeTable.Values)
                {
                    double[] dashArray = null;

                    if (type.StrokeDashArray != null && type.StrokeDashArray.Count > 1)
                    {
                        dashArray = new double[type.StrokeDashArray.Count];

                        for (int i = 0; i < type.StrokeDashArray.Count; i++)
                        {
                            dashArray[i] = type.StrokeDashArray[i] * 72;
                        }
                    }

                    LineTypes.Add(type.Id, dashArray);
                }

                CatalogDictionary catalogDictionary = new CatalogDictionary();

                PageTreeDictionary pageTreeDictionary = new PageTreeDictionary();

                FontDictionary TimesRoman = new FontDictionary();
                FontDictionary TimesItalic = new FontDictionary();
                FontDictionary TimesBold = new FontDictionary();
                FontDictionary Courier = new FontDictionary();
                FontDictionary Helvetica = new FontDictionary();
                FontDictionary HelveticaBold = new FontDictionary();
                FontDictionary HelveticaOblique = new FontDictionary();
                FontDictionary HelveticaBoldOblique = new FontDictionary();

                InfoDictionary infoDictionary = new InfoDictionary();

                TimesRoman.CreateFontDictionary("TR", "Times-Roman");
                TimesItalic.CreateFontDictionary("TI", "Times-Italic");
                TimesBold.CreateFontDictionary("TB", "Times-Bold");
                Courier.CreateFontDictionary("CR", "Courier");
                Helvetica.CreateFontDictionary("HV", "Helvetica");
                HelveticaBold.CreateFontDictionary("HB", "Helvetica-Bold");
                HelveticaOblique.CreateFontDictionary("HO", "Helvetica-Oblique");
                HelveticaBoldOblique.CreateFontDictionary("HBO", "Helvetica-BoldOblique");

                infoDictionary.SetInfo("doc.dbfx", "BTTDB", "High Camp Software LLC");

                Cirros.Pdf.Utility pdfUtility = new Cirros.Pdf.Utility();

                PageDictionary page = new PageDictionary();
                ContentDictionary content = new ContentDictionary();

                DrawingDocument dc = Globals.ActiveDrawing;
                if (dc != null)
                {
                    _docHeight = dc.PaperSize.Height * 72;
                    _docWidth = dc.PaperSize.Width * 72;

                    PageSize pSize = new PageSize((uint)_docWidth, (uint)_docHeight);
                    pSize.SetMargins(0, 0, 0, 0);

                    page.CreatePage(pageTreeDictionary.ObjectNum, pSize, content.ObjectNum);

                    pageTreeDictionary.AddPage(page.ObjectNum);

                    page.AddResource(TimesRoman);
                    page.AddResource(TimesItalic);
                    page.AddResource(TimesBold);
                    page.AddResource(Courier);
                    page.AddResource(Helvetica);
                    page.AddResource(HelveticaBold);
                    page.AddResource(HelveticaOblique);
                    page.AddResource(HelveticaBoldOblique);

                    if (Globals.ActiveDrawing.Theme.BackgroundColor != Colors.White)
                    {
                        PdfColor bgcolor = new PdfColor(Globals.ActiveDrawing.Theme.BackgroundColor);
                        PdfColor frameColor = showFrame ? new PdfColor(Globals.ActiveDrawing.Theme.BorderColor) : bgcolor;
                        PdfRectangle bg = new PdfRectangle(0, 0, _docWidth, _docHeight, .5, frameColor, bgcolor, null);
                        content.SetStream(bg.ToString());
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
                        await exportEntity(ve, content, page);
                    }
                }

                GraphicStateDictionary xd = new GraphicStateDictionary();
                xd.CreateGraphicStateDictionary(_opacityDictionary);

                page.AddResource(xd);

                using (var stream = await file.OpenStreamForWriteAsync())
                {
                    int size;
                    await stream.WriteAsync(pdfUtility.GetHeader("1.5", out size), 0, size);

                    await stream.WriteAsync(xd.GetXDictionary(stream.Length, out size), 0, size);

                    await stream.WriteAsync(page.GetPageDictionary(stream.Length, out size), 0, size);
                    await stream.WriteAsync(content.GetContentDictionary(stream.Length, out size), 0, size);

                    await stream.WriteAsync(catalogDictionary.GetCatalogDictionary(pageTreeDictionary.ObjectNum, stream.Length, out size), 0, size);
                    await stream.WriteAsync(pageTreeDictionary.GetPageTree(stream.Length, out size), 0, size);

                    await stream.WriteAsync(TimesRoman.GetFontDictionary(stream.Length, out size), 0, size);
                    await stream.WriteAsync(TimesItalic.GetFontDictionary(stream.Length, out size), 0, size);
                    await stream.WriteAsync(TimesBold.GetFontDictionary(stream.Length, out size), 0, size);
                    await stream.WriteAsync(Courier.GetFontDictionary(stream.Length, out size), 0, size);
                    await stream.WriteAsync(Helvetica.GetFontDictionary(stream.Length, out size), 0, size);
                    await stream.WriteAsync(HelveticaBold.GetFontDictionary(stream.Length, out size), 0, size);
                    await stream.WriteAsync(HelveticaOblique.GetFontDictionary(stream.Length, out size), 0, size);
                    await stream.WriteAsync(HelveticaBoldOblique.GetFontDictionary(stream.Length, out size), 0, size);

                    foreach (ImageDictionary id in page.ImageDictionaries)
                    {
                        await stream.WriteAsync(id.GetImageDictionary(stream.Length, out size), 0, size);
                    }

                    await stream.WriteAsync(infoDictionary.GetInfoDictionary(stream.Length, out size), 0, size);
                    await stream.WriteAsync(pdfUtility.CreateXrefTable(stream.Length, out size), 0, size);
                    await stream.WriteAsync(pdfUtility.GetTrailer(catalogDictionary.ObjectNum, infoDictionary.ObjectNum, out size), 0, size);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private static async Task exportEntity(VectorEntity ve, ContentDictionary content, PageDictionary page)
        {
            List<List<Point>> polygonFigures = new List<List<Point>>();

            double lineWidth = ve.LineWidth * 72;
            PdfColor color = new PdfColor(ve.Color);

            if (ve.Children != null)
            {
                foreach (object o in ve.Children)
                {
                    if (o is List<Point>)
                    {
                        List<Point> pc = o as List<Point>;
                        List<Point> pdfPoints = new List<Point>();

                        foreach (Point p in pc)
                        {
                            pdfPoints.Add(new Point(p.X * 72, _docHeight - p.Y * 72));
                        }

                        PdfShape shape;
                        double[] dashArray = LineTypes[ve.LineType];

                        if (ve.Fill)
                        {
#if true
                            // Overlappling polygons with holes must be drawn atomically - add figure to the list
                            polygonFigures.Add(pdfPoints);
#else
                            shape = new PdfPolygon(pdfPoints, lineWidth, color, new PdfColor(ve.FillColor), dashArray, ve.FillEvenOdd);

                            content.SetStream(GetGraphicState(ve.Color, ve.FillColor));
                            content.SetStream(shape.ToString());
#endif
                        }
                        else if (pdfPoints.Count == 2)
                        {
                            shape = new PdfLine(pdfPoints[0].X, pdfPoints[0].Y, pdfPoints[1].X, pdfPoints[1].Y, lineWidth, color, dashArray);

                            content.SetStream(GetGraphicState(ve.Color, ve.FillColor));
                            content.SetStream(shape.ToString());
                        }
                        else
                        {
                            shape = new PdfPolyline(pdfPoints, lineWidth, color, dashArray);

                            content.SetStream(GetGraphicState(ve.Color, ve.FillColor));
                            content.SetStream(shape.ToString());
                        }
                    }
                    else if (o is VectorEntity)
                    {
                        await exportEntity(o as VectorEntity, content, page);
                    }
                    else if (o is VectorImageEntity)
                    {
                        VectorImageEntity vi = o as VectorImageEntity;

                        StorageFile file = await Utilities.GetImageSourceFileAsync(vi.ImageId);

                        if (file != null)
                        {
                            ImageDictionary imageDictionary = null;
                            foreach (ImageDictionary d in page.ImageDictionaries)
                            {
                                if (d.ImageName == vi.ImageId)
                                {
                                    imageDictionary = d;
                                }
                            }

                            if (imageDictionary == null)
                            {
                                imageDictionary = new ImageDictionary();
                                await imageDictionary.CreateImageDictionary(vi.ImageId, file);

                                page.AddImageResource(imageDictionary.ImageName, imageDictionary, content.ObjectNum);
                            }

                            Rect srcRect = new Rect(0, 0, imageDictionary.Width, imageDictionary.Height);
                            Rect dstRect = new Rect(vi.Origin.X, vi.Origin.Y, vi.Width, vi.Height);

                            dstRect = Cirros.Utility.Utilities.AdjustRectForAspect(srcRect, dstRect, Stretch.Fill);

                            double x = dstRect.X * 72;
                            double y = dstRect.Y * 72;
                            double w = dstRect.Width * 72;
                            double h = dstRect.Height * 72;

                            string gs = GetGraphicState(vi.Opacity);

                            PdfImage pi = new PdfImage(vi.ImageId, x, _docHeight - y, w, h, vi.Matrix, gs);
                            content.SetStream(pi.ToString());
                        }
                    }
                    else if (o is VectorTextEntity)
                    {
                        VectorTextEntity vt = o as VectorTextEntity;

                        string[] lines = vt.Text.Split(new[] { '\n' });

                        double tx = vt.Location.X * 72;
                        double ty = vt.Location.Y * 72;
                        double th = vt.TextHeight * 1.35 * 72;
                        double lh = vt.TextHeight * 72 * vt.LineSpacing;

                        string font;

                        if (vt.FontFamily == "Consolas" || vt.FontFamily == "Courier New")
                        {
                            font = "CR";
                        }
                        else if (vt.FontFamily == "Times New Roman" || vt.FontFamily == "Georgia")
                        {
                            font = "TR";
                        }
                        else
                        {
                            font = "HV";
                        }

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

                        PdfAlign palign;

                        switch (vt.TextAlignment)
                        {
                            default:
                            case TextAlignment.Left:
                                palign = PdfAlign.Left;
                                break;

                            case TextAlignment.Center:
                            case TextAlignment.Justify:
                                palign = PdfAlign.Center;
                                break;

                            case TextAlignment.Right:
                                palign = PdfAlign.Right;
                                break;
                        }

                        content.SetStream(GetGraphicState(ve.Color, ve.FillColor));

                        foreach (string s in lines)
                        {
                            PdfText text = new PdfText(tx, _docHeight - ty, th, vt.CharacterSpacing * 90, -vt.Angle / Construct.cRadiansToDegrees, font, palign, color, s.TrimEnd());
                            content.SetStream(text.ToString());
                            ty += lh;
                        }
                    }
                }
            }

            if (polygonFigures.Count > 0)
            {
                double[] dashArray = LineTypes[ve.LineType];
                PdfShape shape = new PdfPolygonList(polygonFigures, lineWidth, color, new PdfColor(ve.FillColor), dashArray, ve.FillEvenOdd);

                content.SetStream(GetGraphicState(ve.Color, ve.FillColor));
                content.SetStream(shape.ToString());
            }
        }

        private static string GetGraphicState(double opacity)
        {
            byte b = (byte)(255 * opacity);

            string key = string.Format("{0:X2}{1:X2}", b, b);

            if (_opacityDictionary.ContainsKey(key) == false)
            {
                string gs = string.Format("/CA {0:0.000} /ca {1:0.000}", opacity, opacity);
                _opacityDictionary.Add(key, gs);
            }

            return string.Format("/T{0} gs ", key);
        }

        private static string GetGraphicState(Color color, Color fillColor)
        {
            string key = string.Format("{0:X2}{1:X2}", color.A, fillColor.A);

            if (_opacityDictionary.ContainsKey(key) == false)
            {
                string gs = string.Format("/CA {0:0.000} /ca {1:0.000}", (double)color.A / 255, (double)fillColor.A / 255);
                _opacityDictionary.Add(key, gs);
            }

            return string.Format("/T{0} gs ", key);
        }
    }
}
