#define sharpdx3
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Microsoft.UI.Xaml.Media;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Bitmap = SharpDX.WIC.Bitmap;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;
#if sharpdx3
using SharpDX.Mathematics.Interop;
using System.Numerics;
#else
using RawColor4 = SharpDX.Color4;
using RawVector2 = SharpDX.Vector2;
using RawMatrix3x2 = SharpDX.Matrix3x2;
using RawRectangleF = SharpDX.RectangleF;
#endif

namespace Cirros.Core
{
    public class Dx
    {
        private static ImagingFactory _wicFactory = new ImagingFactory();
        private static SharpDX.DirectWrite.Factory _dwFactory = new SharpDX.DirectWrite.Factory();
        private static SharpDX.Direct2D1.Factory _d2dFactory = new SharpDX.Direct2D1.Factory();

        static Dx()
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
        public static Dictionary<int, StrokeStyle> LineTypes = null;

        public static FontCollection _fontCollection = null;
        public static List<string> _fontNames = null;

        public static FontCollection SystemFontCollection
        {
            get
            {
                if (_fontCollection == null)
                {
                    try
                    {
                        _fontCollection = _dwFactory.GetSystemFontCollection(false);
                    }
                    catch (Exception ex)
                    {
                        Analytics.ReportError(ex, new Dictionary<string, string> {
                            { "method", "Dx.SystemFontCollection" },
                        }, 210);
                    }
                }

                return _fontCollection;
            }
        }

        public static List<string> FontNames
        {
            get
            {
                if (_fontNames == null)
                {
                    try
                    {
                        _fontNames = new List<string>();

                        FontCollection fontCollection = SystemFontCollection;

                        for (int i = 0; i < fontCollection.FontFamilyCount; i++)
                        {
                            SharpDX.DirectWrite.FontFamily writeFontFamily = fontCollection.GetFontFamily(i);

                            int index;

                            if (writeFontFamily.FamilyNames.FindLocaleName("en-us", out index))
                            {
                                _fontNames.Add(writeFontFamily.FamilyNames.GetString(index));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Analytics.ReportError(ex, new Dictionary<string, string> {
                            { "method", "Dx.FontNames" },
                        }, 211);
                    }
                }

                return _fontNames;
            }
        }

        public static bool FontExists(string fontFamilyName)
        {
            bool exists = false;
            int index;
            try
            {
                if (SystemFontCollection.FindFamilyName(fontFamilyName, out index))
                {
                    exists = true;
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                    { "method", "Dx.FontExists" },
                }, 212);
            }
            return exists;
        }

        public static double GetFontSizeFromHeight(string fontFamilyName, double height)
        {
            double fontSize = height;

            try
            {
                int index;
                if (SystemFontCollection.FindFamilyName(fontFamilyName, out index))
                {
                    SharpDX.DirectWrite.FontFamily writeFontFamily = SystemFontCollection.GetFontFamily(index);
                    Font writeFont = writeFontFamily.GetFirstMatchingFont(FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
                    FontMetrics fontMetrics = writeFont.Metrics;

                    fontSize = height * fontMetrics.DesignUnitsPerEm / fontMetrics.CapHeight;
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                    { "method", "Dx.GetFontSizeFromHeight" },
                }, 213);
            }

            return fontSize;
        }

        public static void GetFontHeight(string fontFamilyName, double fontSize, out double capHeight, out double descent)
        {
            int index;

            try
            {
                if (SystemFontCollection.FindFamilyName(fontFamilyName, out index))
                {
                    SharpDX.DirectWrite.FontFamily writeFontFamily = SystemFontCollection.GetFontFamily(index);
                    Font writeFont = writeFontFamily.GetFirstMatchingFont(FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
                    FontMetrics fontMetrics = writeFont.Metrics;

                    //double ascent = fontSize * fontMetrics.Ascent / fontMetrics.DesignUnitsPerEm;
                    //double capsHeight = fontSize * fontMetrics.CapHeight / fontMetrics.DesignUnitsPerEm;
                    //double xHeight = fontSize * fontMetrics.XHeight / fontMetrics.DesignUnitsPerEm;
                    //double descent = fontSize * fontMetrics.Descent / fontMetrics.DesignUnitsPerEm;
                    //double lineGap = fontSize * fontMetrics.LineGap / fontMetrics.DesignUnitsPerEm;

                    capHeight = fontSize * fontMetrics.CapHeight / fontMetrics.DesignUnitsPerEm;
                    descent = fontSize * fontMetrics.Descent / fontMetrics.DesignUnitsPerEm;
                }
                else
                {
                    capHeight = fontSize;
                    descent = fontSize;
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                    { "method", "Dx.FontNames" },
                }, 214);
                capHeight = fontSize;
                descent = fontSize;
            }
        }

        private static RawColor4 ColorToColor4(Windows.UI.Color color)
        {
            return new RawColor4((float)color.R / 255.0F, (float)color.G / 255.0F, (float)color.B / 255.0F, (float)color.A / 255.0F);
        }

        private static void DrawTextLayout(RenderTarget renderTarget, RawVector2 location, string text, string font, Microsoft.UI.Xaml.TextAlignment alignment,
            float size, float characterSpacing, float angle, SharpDX.Direct2D1.SolidColorBrush brush)
        {
            if (size < .001)
            {
                size = .001f;
            }

            TextFormat textFormat = new TextFormat(_dwFactory, font, size);

            float charSpacing = (characterSpacing - 1F) * .8F * size;
            float width = text.Length * size * characterSpacing;

            switch (alignment)
            {
                case Microsoft.UI.Xaml.TextAlignment.Left:
                    textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
                    break;

                case Microsoft.UI.Xaml.TextAlignment.Center:
                    textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
                    break;

                case Microsoft.UI.Xaml.TextAlignment.Right:
                    textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
                    break;
            }

            textFormat.ParagraphAlignment = ParagraphAlignment.Near;

            TextLayout textLayout = new TextLayout(_dwFactory, text, textFormat, width, size);
            LineMetrics[] lineMetrics = textLayout.GetLineMetrics();

            TextLayout1 t1 = textLayout.QueryInterface<TextLayout1>();
            t1.SetCharacterSpacing(0F, charSpacing, 0F, new TextRange(0, text.Length));

            RawVector2 origin = new RawVector2();
            origin.X = 0;
            origin.Y = location.Y - lineMetrics[0].Baseline;

            switch (alignment)
            {
                case Microsoft.UI.Xaml.TextAlignment.Left:
                    origin.X = location.X;
                    break;

                case Microsoft.UI.Xaml.TextAlignment.Center:
                    origin.X = location.X - width / 2;
                    break;

                case Microsoft.UI.Xaml.TextAlignment.Right:
                    origin.X = location.X - width;
                    break;
            }
#if sharpdx3
            if (angle == 0)
            {
                renderTarget.DrawTextLayout(origin, textLayout, brush);
            }
            else
            {
                RawMatrix3x2 saveTransform = renderTarget.Transform;

                Matrix3x2 m1 = Matrix3x2.CreateTranslation((float)-location.X, (float)-location.Y);
                Matrix3x2 m2 = Matrix3x2.CreateRotation(angle);
                Matrix3x2 m3 = Matrix3x2.CreateTranslation((float)location.X, (float)location.Y);

                renderTarget.Transform = MatrixToRawMatrix3x2(m1 * m2 * m3);
                renderTarget.DrawTextLayout(origin, textLayout, brush);

                renderTarget.Transform = saveTransform;
            }
#else
            Vector2 center = new Vector2(location.X, location.Y);

            renderTarget.Transform = SharpDX.Matrix.Transformation2D(center, 1, new Vector2(1), center, angle, new Vector2(0));
            renderTarget.DrawTextLayout(origin, textLayout, brush);
            renderTarget.Transform = SharpDX.Matrix.Identity;
#endif
            textLayout.Dispose();
            textFormat.Dispose();
        }

        private static RawMatrix3x2 MatrixToRawMatrix3x2(Matrix3x2 m)
        {
            return new RawMatrix3x2((float)m.M11, (float)m.M12, (float)m.M21, (float)m.M22, (float)m.M31, (float)m.M32);
        }

        private static void DrawPolygon(RenderTarget renderTarget, List<RawVector2[]> fpointlist, float width,
            SharpDX.Direct2D1.SolidColorBrush lineBrush, SharpDX.Direct2D1.SolidColorBrush fillBrush, StrokeStyle strokeStyle, bool fillEvenOdd)
        {
            SharpDX.Direct2D1.PathGeometry pathGeometry = new SharpDX.Direct2D1.PathGeometry(renderTarget.Factory);
            var pathSink = pathGeometry.Open();

            pathSink.SetFillMode(fillEvenOdd ? FillMode.Alternate : FillMode.Winding);

            foreach (RawVector2[] fpoints in fpointlist)
            {
                RawVector2 startingPoint = fpoints[0];
                pathSink.BeginFigure(startingPoint, FigureBegin.Filled);

                foreach (RawVector2 p in fpoints)
                {
                    pathSink.AddLine(p);
                }

                pathSink.EndFigure(FigureEnd.Closed);
            }

            pathSink.Close();
            pathSink.Dispose();

            renderTarget.FillGeometry(pathGeometry, fillBrush);

            if (width > 0)
            {
                if (strokeStyle == null)
                {
                    renderTarget.DrawGeometry(pathGeometry, lineBrush, width);
                }
                else
                {
                    renderTarget.DrawGeometry(pathGeometry, lineBrush, width, strokeStyle);
                }
            }

            pathGeometry.Dispose();
        }

        private static void DrawPolygon(RenderTarget renderTarget, RawVector2[] fpoints, float width,
            SharpDX.Direct2D1.SolidColorBrush lineBrush, SharpDX.Direct2D1.SolidColorBrush fillBrush, StrokeStyle strokeStyle, bool fillEvenOdd)
        {
            SharpDX.Direct2D1.PathGeometry pathGeometry = new SharpDX.Direct2D1.PathGeometry(renderTarget.Factory);
            var pathSink = pathGeometry.Open();

            RawVector2 startingPoint = fpoints[0];

            pathSink.SetFillMode(fillEvenOdd ? FillMode.Alternate : FillMode.Winding);
            pathSink.BeginFigure(startingPoint, FigureBegin.Filled);

            foreach (RawVector2 p in fpoints)
            {
                pathSink.AddLine(p);
            }

            pathSink.EndFigure(FigureEnd.Closed);
            pathSink.Close();
            pathSink.Dispose();

            renderTarget.FillGeometry(pathGeometry, fillBrush);

            if (width > 0)
            {
                if (strokeStyle == null)
                {
                    renderTarget.DrawGeometry(pathGeometry, lineBrush, width);
                }
                else
                {
                    renderTarget.DrawGeometry(pathGeometry, lineBrush, width, strokeStyle);
                }
            }

            pathGeometry.Dispose();
        }

        private static void DrawImage(RenderTarget renderTarget, string path, double x, double y, double width, double height, double opacity, Matrix matrix)
        {
            SharpDX.WIC.BitmapDecoder bitmapDecoder = new SharpDX.WIC.BitmapDecoder(_wicFactory, path, SharpDX.IO.NativeFileAccess.Read, SharpDX.WIC.DecodeOptions.CacheOnDemand);

            BitmapFrameDecode frame = bitmapDecoder.GetFrame(0);

            FormatConverter converter = new FormatConverter(_wicFactory);
            converter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppPRGBA);

            SharpDX.Direct2D1.Bitmap newBitmap = SharpDX.Direct2D1.Bitmap1.FromWicBitmap(renderTarget, converter);

            Rect srcRect = new Rect(x, y, newBitmap.Size.Width, newBitmap.Size.Height);
            Rect dstRect = new Rect(x, y, width, height);

            dstRect = Cirros.Utility.Utilities.AdjustRectForAspect(srcRect, dstRect, Stretch.Fill);
            RawRectangleF destinationRectangle = new RawRectangleF((float)dstRect.Left, (float)dstRect.Top, (float)dstRect.Right, (float)dstRect.Bottom);

            if (matrix.IsIdentity == false)
            {
                RawMatrix3x2 saveTransform = renderTarget.Transform;

#if sharpdx3
                Matrix3x2 m1 = Matrix3x2.CreateTranslation((float)-x, (float)-y);
                Matrix3x2 m2 = new Matrix3x2((float)matrix.M11, (float)matrix.M12, (float)matrix.M21, (float)matrix.M22, (float)matrix.OffsetX, (float)matrix.OffsetY);
                Matrix3x2 m3 = Matrix3x2.CreateTranslation((float)x, (float)y);
#else
                Matrix3x2 m1 = Matrix3x2.Translation((float)-x, (float)-y);
                Matrix3x2 m2 = new Matrix3x2((float)matrix.M11, (float)matrix.M12, (float)matrix.M21, (float)matrix.M22, (float)matrix.OffsetX, (float)matrix.OffsetY);
                Matrix3x2 m3 = Matrix3x2.Translation((float)x, (float)y);
#endif

                renderTarget.Transform = MatrixToRawMatrix3x2(m1 * m2 * m3);
                renderTarget.DrawBitmap(newBitmap, destinationRectangle, (float)opacity, SharpDX.Direct2D1.BitmapInterpolationMode.NearestNeighbor);
                renderTarget.Transform = saveTransform;
            }
            else
            {
                renderTarget.DrawBitmap(newBitmap, destinationRectangle, (float)opacity, SharpDX.Direct2D1.BitmapInterpolationMode.NearestNeighbor);
            }
        }

        private static void DrawPolyline(RenderTarget renderTarget, RawVector2[] fpoints, float width, SharpDX.Direct2D1.SolidColorBrush lineBrush, StrokeStyle strokeStyle)
        {
            SharpDX.Direct2D1.PathGeometry pathGeometry = new SharpDX.Direct2D1.PathGeometry(renderTarget.Factory);
            var pathSink = pathGeometry.Open();

            RawVector2 startingPoint = fpoints[0];

            pathSink.BeginFigure(startingPoint, FigureBegin.Hollow);

            foreach (RawVector2 p in fpoints)
            {
                pathSink.AddLine(p);
            }

            pathSink.EndFigure(FigureEnd.Open);
            pathSink.Close();
            pathSink.Dispose();

            if (strokeStyle == null)
            {
                renderTarget.DrawGeometry(pathGeometry, lineBrush, width);
            }
            else
            {
                renderTarget.DrawGeometry(pathGeometry, lineBrush, width, strokeStyle);
            }

            pathGeometry.Dispose();
        }

        private static void DrawLine(RenderTarget renderTarget, float fx, float fy, float tx, float ty, float width, SharpDX.Direct2D1.SolidColorBrush lineBrush)
        {
            RawVector2 rvFrom = new RawVector2();
            RawVector2 rvTo = new RawVector2();
            rvFrom.X = fx;
            rvFrom.Y = fy;
            rvTo.X = tx;
            rvTo.Y = ty;
            renderTarget.DrawLine(rvFrom, rvTo, lineBrush, width);
        }

        private static void DrawLine(RenderTarget renderTarget, Windows.Foundation.Point from, Windows.Foundation.Point to, float width, SharpDX.Direct2D1.SolidColorBrush lineBrush)
        {
            RawVector2 rvFrom = new RawVector2();
            RawVector2 rvTo = new RawVector2();
            rvFrom.X = (float)from.X;
            rvFrom.Y = (float)from.Y;
            rvTo.X = (float)to.X;
            rvTo.Y = (float)to.Y;
            renderTarget.DrawLine(rvFrom, rvTo, lineBrush, width);
        }

        protected static void exportEntity(RenderTarget renderTarget, VectorEntity ve, double scale, double modelScale, double constantLineWidth = 0)
        {
            float lineWidth = (float)(constantLineWidth == 0 ? ve.LineWidth * scale : constantLineWidth);
            SharpDX.Direct2D1.SolidColorBrush lineBrush = new SharpDX.Direct2D1.SolidColorBrush(renderTarget, ColorToColor4(ve.Color));

            List<RawVector2[]> polygonFigures = new List<RawVector2[]>();

            if (ve.Children != null)
            {
                foreach (object o in ve.Children)
                {
                    if (o is List<Point>)
                    {
                        List<Point> pc = o as List<Point>;

                        RawVector2[] fpoints = new RawVector2[pc.Count];

                        for (int i = 0; i < pc.Count; i++)
                        {
                            RawVector2 rv = new RawVector2();
                            rv.X = (float)(pc[i].X * scale * modelScale);
                            rv.Y = (float)(pc[i].Y * scale * modelScale);
                            fpoints[i] = rv;
                        }

                        if (ve.Fill)
                        {
                            // Overlappling polygons with holes must be drawn atomically - add figure to the list
                            polygonFigures.Add(fpoints);
                        }
                        else
                        {
                            DrawPolyline(renderTarget, fpoints, lineWidth, lineBrush, LineTypes[ve.LineType]);
                        }
                    }
                    else if (o is VectorEntity)
                    {
                        exportEntity(renderTarget, o as VectorEntity, scale, modelScale, constantLineWidth);
                    }
                    else if (o is VectorImageEntity)
                    {
                        VectorImageEntity vi = o as VectorImageEntity;

                        string path = Cirros.Utility.Utilities.GetImageSourcePath(vi.ImageId);

                        if (string.IsNullOrEmpty(path) == false)
                        {
                            double x = vi.Origin.X * scale;
                            double y = vi.Origin.Y * scale;
                            double w = vi.Width * scale;
                            double h = vi.Height * scale;

                            try
                            {
                                DrawImage(renderTarget, path, x, y, w, h, vi.Opacity, vi.Matrix);
                            }
                            catch
                            {
                                // file is missing
                            }
                        }
                    }
                    else if (o is VectorTextEntity)
                    {
                        VectorTextEntity vt = o as VectorTextEntity;

                        string[] lines = vt.Text.Split(new[] { '\n' });

                        float tx = (float)(vt.Origin.X * scale * modelScale + vt.PsOffset.X * scale);
                        float ty = (float)(vt.Origin.Y * scale * modelScale + vt.PsOffset.Y * scale);
                        float th = (float)(vt.TextHeight * scale);
                        float lh = (float)(vt.TextHeight * scale * vt.LineSpacing);

                        if (FontHeightAdj.ContainsKey(vt.FontFamily))
                        {
                            th *= (float)FontHeightAdj[vt.FontFamily];
                        }
                        else
                        {
                            th = (float)GetFontSizeFromHeight(vt.FontFamily, (double)th);
                        }

                        if (vt.TextPosition == TextPosition.Above)
                        {
                            if (vt.Angle == 0)
                            {
                                ty -= (lines.Length - 1) * lh;
                            }
                            else
                            {
                                Windows.Foundation.Point td = Construct.PolarOffset(new Windows.Foundation.Point(0, 0), (lines.Length - 1) * lh, (vt.Angle + 90) / Construct.cRadiansToDegrees);
                                tx -= (float)td.X;
                                ty -= (float)td.Y;
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
                                Windows.Foundation.Point td = Construct.PolarOffset(new Windows.Foundation.Point(0, 0), (lines.Length - 1) * lh / 2, (vt.Angle + 90) / Construct.cRadiansToDegrees);
                                tx -= (float)td.X;
                                ty -= (float)td.Y;
                            }
                        }

                        Windows.Foundation.Point to = Construct.PolarOffset(new Windows.Foundation.Point(0, 0), lh, (vt.Angle + 90) / Construct.cRadiansToDegrees);

                        foreach (string s in lines)
                        {
                            float fangle = (float)(vt.Angle / Construct.cRadiansToDegrees);
                            DrawTextLayout(renderTarget, new RawVector2(tx, ty), s, vt.FontFamily, vt.TextAlignment, th, (float)vt.CharacterSpacing, fangle, lineBrush);
                            tx += (float)to.X;
                            ty += (float)to.Y;
                        }
                    }
                }
            }

            if (polygonFigures.Count > 0)
            {
                SharpDX.Direct2D1.SolidColorBrush fillBrush = new SharpDX.Direct2D1.SolidColorBrush(renderTarget, ColorToColor4(ve.FillColor));
                DrawPolygon(renderTarget, polygonFigures, lineWidth, lineBrush, fillBrush, LineTypes[ve.LineType], ve.FillEvenOdd);
                fillBrush.Dispose();
            }

            lineBrush.Dispose();
        }

        private static void DrawGrid(WicRenderTarget renderTarget, double scale, Rect destRect)
        {
            uint _displayGridDivisions = (uint)Globals.GridDivisions;
            double _displayGridSpacing = Globals.GridSpacing;

            if (_displayGridDivisions > 1)
            {
                //Rect destRect = renderer.DestinationRect;

                //double scale = renderer.Scale;
                //double xoffset = renderer.XOffset;
                //double yoffset = renderer.YOffset;
                double xoffset = 0;
                double yoffset = 0;

                float minorSpacing = (float)_displayGridSpacing / _displayGridDivisions;

                Windows.UI.Color gridColor = Globals.ActiveDrawing.Theme.GridColor;
                //gridColor.A = (byte)(Globals.GridIntensity < 1 ? (Globals.GridIntensity * 160 + 95) : 255);
                gridColor.A = 255;

                SharpDX.Direct2D1.SolidColorBrush majorBrush = 
                    new SharpDX.Direct2D1.SolidColorBrush(renderTarget, ColorToColor4(gridColor));

                //gridColor.A /= 2;
                gridColor.A = 180;

                SharpDX.Direct2D1.SolidColorBrush minorBrush = 
                    new SharpDX.Direct2D1.SolidColorBrush(renderTarget, ColorToColor4(gridColor));

                float majorThickness = .32f;
                //float minorThickness = .16f;
                float minorThickness = .32f;

                double ox = 0;
                double oy = 0;

                if ((Globals.ActiveDrawing.Origin.X % _displayGridSpacing) != 0)
                {
                    ox = -(_displayGridSpacing - (Globals.ActiveDrawing.Origin.X - (Math.Floor(Globals.ActiveDrawing.Origin.X / _displayGridSpacing) * _displayGridSpacing)));
                }
                if ((Globals.ActiveDrawing.Origin.Y % _displayGridSpacing) != 0)
                {
                    oy = -(_displayGridSpacing - (Globals.ActiveDrawing.Origin.Y - (Math.Floor(Globals.ActiveDrawing.Origin.Y / _displayGridSpacing) * _displayGridSpacing)));
                }

                Point min = Globals.ActiveDrawing.PaperToModel(new Point(0, Globals.ActiveDrawing.PaperSize.Height));
                Point max = Globals.ActiveDrawing.PaperToModel(new Point(Globals.ActiveDrawing.PaperSize.Width, 0));

                int iminx = (int)((min.X + ox) / _displayGridSpacing);
                int iminy = (int)((min.Y + oy) / _displayGridSpacing);
                min = new Point((double)iminx * _displayGridSpacing - ox, (double)iminy * _displayGridSpacing - oy);


                //ds.Antialiasing = CanvasAntialiasing.Antialiased;
                AntialiasMode aa = renderTarget.AntialiasMode;
                renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

                float sx, sy, ex, ey;

                for (double s = min.Y + oy; s < max.Y; s += _displayGridSpacing)
                {
                    Point start = Globals.ActiveDrawing.ModelToPaper(new Point(min.X, s));
                    Point end = Globals.ActiveDrawing.ModelToPaper(new Point(max.X, s));
                    sx = (float)(start.X * scale + xoffset);
                    sy = ey = (float)(start.Y * scale + yoffset);
                    ex = (float)(end.X * scale + xoffset);

                    sx = (float)Math.Max(destRect.Left, sx);
                    ex = (float)Math.Min(destRect.Right, ex);

                    if (s > min.Y && sy > destRect.Top)
                    {
                        DrawLine(renderTarget, sx, sy, ex, ey, majorThickness, majorBrush);
                        //ds.DrawLine(sx, sy, ex, ey, gridColor, majorThickness);
                    }

                    for (int n = 1; n < _displayGridDivisions; n++)
                    {
                        double gy = s + minorSpacing * n;

                        if (gy < min.Y)
                            continue;
                        if (gy > max.Y)
                            break;

                        start = Globals.ActiveDrawing.ModelToPaper(new Point(min.X, gy));
                        sy = ey = (float)(start.Y * scale + yoffset);

                        if (sy < destRect.Top)
                            continue;
                        if (sy > destRect.Bottom)
                            continue;

                        DrawLine(renderTarget, sx, sy, ex, ey, minorThickness, minorBrush);
                        //ds.DrawLine(sx, sy, ex, ey, gridColor, minorThickness);
                    }
                }

                for (double s = min.X + ox; s < max.X; s += _displayGridSpacing)
                {
                    Point start = Globals.ActiveDrawing.ModelToPaper(new Point(s, min.Y));
                    Point end = Globals.ActiveDrawing.ModelToPaper(new Point(s, max.Y));
                    sx = ex = (float)(start.X * scale + xoffset);
                    sy = (float)(start.Y * scale + yoffset);
                    ey = (float)(end.Y * scale + yoffset);

                    sy = (float)Math.Min(destRect.Bottom, sy);
                    ey = (float)Math.Max(destRect.Top, ey);

                    if (s > min.X && sx > destRect.Left)
                    {
                        DrawLine(renderTarget, sx, sy, ex, ey, majorThickness, majorBrush);
                        //ds.DrawLine(sx, sy, ex, ey, gridColor, majorThickness);
                    }

                    for (int n = 1; n < _displayGridDivisions; n++)
                    {
                        double gx = s + minorSpacing * n;

                        if (gx < min.X)
                            continue;
                        if (gx > max.X)
                            break;

                        start = Globals.ActiveDrawing.ModelToPaper(new Point(gx, min.Y));
                        sx = ex = (float)(start.X * scale + xoffset);

                        if (sx < destRect.Left)
                            continue;
                        if (sx > destRect.Right)
                            continue;

                        DrawLine(renderTarget, sx, sy, ex, ey, minorThickness, minorBrush);
                        //ds.DrawLine(sx, sy, ex, ey, gridColor, minorThickness);
                    }

                    renderTarget.AntialiasMode = aa;
                }

                majorBrush.Dispose();
                minorBrush.Dispose();
            }
        }

        private static void Render(WicRenderTarget renderTarget, double scale, DrawingDocument dc, bool showFrame, bool showGrid)
        {
            if (dc != null)
            {
                int width = (int)Math.Round(Globals.ActiveDrawing.PaperSize.Width * scale);
                int height = (int)Math.Round(Globals.ActiveDrawing.PaperSize.Height * scale);

                renderTarget.BeginDraw();
                renderTarget.Clear(ColorToColor4(Globals.ActiveDrawing.Theme.BackgroundColor));

                if (showFrame)
                {
                    SharpDX.Direct2D1.SolidColorBrush borderbrush = new SharpDX.Direct2D1.SolidColorBrush(renderTarget, ColorToColor4(Globals.ActiveDrawing.Theme.BorderColor));
                    RawRectangleF borderrect = new RawRectangleF(0, 0, width, height);

                    renderTarget.DrawRectangle(borderrect, borderbrush, (float)(.008 * scale));

                    borderbrush.Dispose();
                }

                if (showGrid)
                {
                    Rect frameRect = new Rect(0, 0, Globals.ActiveDrawing.PaperSize.Width * scale, Globals.ActiveDrawing.PaperSize.Height * scale);
                    DrawGrid(renderTarget, scale, frameRect);
                }

                LineTypes = new Dictionary<int, StrokeStyle>();

                foreach (LineType type in Globals.LineTypeTable.Values)
                {
                    if (type.StrokeDashArray != null && type.StrokeDashArray.Count > 1)
                    {
                        StrokeStyleProperties strokeProps = new StrokeStyleProperties();
                        strokeProps.DashStyle = DashStyle.Custom;

                        float[] dashes = new float[type.StrokeDashArray.Count];

                        for (int i = 0; i < type.StrokeDashArray.Count; i++)
                        {
                            dashes[i] = (float)(type.StrokeDashArray[i] * scale);
                        }

                        StrokeStyle strokeStyle = new StrokeStyle(_d2dFactory, strokeProps, dashes);

                        LineTypes.Add(type.Id, strokeStyle);
                    }
                    else
                    {
                        StrokeStyleProperties strokeProps = new StrokeStyleProperties();
                        strokeProps.DashStyle = DashStyle.Solid;

                        switch (Globals.ActiveDrawing.LineEndCap)
                        {
                            case PenLineCap.Round:
                            default:
                                strokeProps.EndCap = CapStyle.Round;
                                break;

                            case PenLineCap.Flat:
                                strokeProps.EndCap = CapStyle.Flat;
                                break;

                            case PenLineCap.Square:
                                strokeProps.EndCap = CapStyle.Square;
                                break;

                            case PenLineCap.Triangle:
                                strokeProps.EndCap = CapStyle.Triangle;
                                break;
                        }

                        StrokeStyle strokeStyle = new StrokeStyle(_d2dFactory, strokeProps);

                        LineTypes.Add(type.Id, strokeStyle);
                    }
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
                    exportEntity(renderTarget, ve, scale, 1);
                }

                foreach (StrokeStyle ss in LineTypes.Values)
                {
                    if (ss != null)
                    {
                        ss.Dispose();
                    }
                }

                renderTarget.EndDraw();
            }
        }

        private static void RenderGroup(Group group, WicRenderTarget renderTarget, double scale, double modelScale, Windows.Foundation.Point offset)
        {
            renderTarget.BeginDraw();
            renderTarget.Clear(ColorToColor4(Globals.ActiveDrawing.Theme.BackgroundColor));

            LineTypes = new Dictionary<int, StrokeStyle>();

            foreach (LineType type in Globals.LineTypeTable.Values)
            {
                if (type.StrokeDashArray != null && type.StrokeDashArray.Count > 1)
                {
                    StrokeStyleProperties strokeProps = new StrokeStyleProperties();
                    strokeProps.DashStyle = DashStyle.Custom;

                    float[] dashes = new float[type.StrokeDashArray.Count];

                    for (int i = 0; i < type.StrokeDashArray.Count; i++)
                    {
                        dashes[i] = (float)(type.StrokeDashArray[i] * scale);
                    }

                    StrokeStyle strokeStyle = new StrokeStyle(_d2dFactory, strokeProps, dashes);

                    LineTypes.Add(type.Id, strokeStyle);
                }
                else
                {
                    StrokeStyleProperties strokeProps = new StrokeStyleProperties();
                    strokeProps.DashStyle = DashStyle.Solid;

                    switch (Globals.ActiveDrawing.LineEndCap)
                    {
                        case PenLineCap.Round:
                        default:
                            strokeProps.EndCap = CapStyle.Round;
                            break;

                        case PenLineCap.Flat:
                            strokeProps.EndCap = CapStyle.Flat;
                            break;

                        case PenLineCap.Square:
                            strokeProps.EndCap = CapStyle.Square;
                            break;

                        case PenLineCap.Triangle:
                            strokeProps.EndCap = CapStyle.Triangle;
                            break;
                    }

                    StrokeStyle strokeStyle = new StrokeStyle(_d2dFactory, strokeProps);

                    LineTypes.Add(type.Id, strokeStyle);
                }
            }

            List<Primitive> primitives = new List<Primitive>();

            foreach (Primitive p in group.Items)
            {
                Primitive p0 = p.Clone();
                p0.MoveByDelta(offset.X, offset.Y);

                if (Globals.LayerTable[p.LayerId].Visible)
                {
                    primitives.Add(p0);
                }
            }

            primitives.Sort();

            VectorContext context = new VectorContext(false, true, true);
            
            foreach (Primitive p in primitives)
            {
                VectorEntity ve = p.Vectorize(context);
                exportEntity(renderTarget, ve, scale, modelScale, 1);
            }

            foreach (StrokeStyle ss in LineTypes.Values)
            {
                if (ss != null)
                {
                    ss.Dispose();
                }
            }

            renderTarget.EndDraw();
        }

        public static async Task ExportDrawingToImageFileAsync(StorageFile file, int size, double resolution = 72, bool showFrame = true, bool showGrid = false)
        {
            try
            {
                double scale = size / Math.Max(Globals.ActiveDrawing.PaperSize.Width, Globals.ActiveDrawing.PaperSize.Height);
                int width = (int)Math.Round(Globals.ActiveDrawing.PaperSize.Width * scale);
                int height = (int)Math.Round(Globals.ActiveDrawing.PaperSize.Height * scale);

                var wicBitmap = new Bitmap(_wicFactory, width, height, SharpDX.WIC.PixelFormat.Format32bppBGR, BitmapCreateCacheOption.CacheOnLoad);
                var renderTargetProperties = new RenderTargetProperties(RenderTargetType.Default, new PixelFormat(Format.Unknown, AlphaMode.Unknown), 0, 0, RenderTargetUsage.None, FeatureLevel.Level_DEFAULT);
                var renderTarget = new WicRenderTarget(_d2dFactory, wicBitmap, renderTargetProperties);

                await ExecuteOnUIThread(() =>
                {
                    Render(renderTarget, scale, Globals.ActiveDrawing, showFrame, showGrid);
                });

                using (Stream sstream = await file.OpenStreamForWriteAsync())
                {
                    var stream = new WICStream(_wicFactory, sstream);

                    string filetype = file.FileType.ToLower();

                    BitmapEncoder encoder;
                    if (filetype == ".png")
                    {
                        encoder = new PngBitmapEncoder(_wicFactory);
                    }
                    else if (filetype == ".jpg")
                    {
                        encoder = new JpegBitmapEncoder(_wicFactory);
                    }
                    else if (filetype == ".bmp")
                    {
                        encoder = new BmpBitmapEncoder(_wicFactory);
                    }
                    else
                    {
                        throw new Exception("Invalid image file type: " + filetype);
                    }

                    encoder.Initialize(stream);

                    // Create a Frame encoder
                    var bitmapFrameEncode = new BitmapFrameEncode(encoder);
                    bitmapFrameEncode.Initialize();
                    bitmapFrameEncode.SetSize(width, height);
                    bitmapFrameEncode.SetResolution(resolution, resolution);
                    //if (encoder is JpegBitmapEncoder)
                    //{
                    //    bitmapFrameEncode.Options.ImageQuality = 0.5f;
                    //}

                    var pixelFormatGuid = SharpDX.WIC.PixelFormat.FormatDontCare;
                    bitmapFrameEncode.SetPixelFormat(ref pixelFormatGuid);
                    bitmapFrameEncode.WriteSource(wicBitmap);

                    bitmapFrameEncode.Commit();
                    encoder.Commit();

                    bitmapFrameEncode.Dispose();
                    encoder.Dispose();
                    stream.Dispose();

                    await sstream.FlushAsync();
                    sstream.Dispose();
                }

                wicBitmap.Dispose();
                renderTarget.Dispose();
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                    { "method", "Dx.ExportDrawingToImageFileAsync" },
                }, 215);
            }
        }

        public static async Task ExportGroupToImageFileAsync(Group group, StorageFile file, int size, int margin)
        {
            double ms = 1;

            margin = 10;
            double scale = (size - margin) / Math.Max(1.0, Math.Max(group.PaperBounds.Width, group.PaperBounds.Height));

            var wicBitmap = new Bitmap(_wicFactory, size, size, SharpDX.WIC.PixelFormat.Format32bppBGR, BitmapCreateCacheOption.CacheOnLoad);
            var renderTargetProperties = new RenderTargetProperties(RenderTargetType.Default, new PixelFormat(Format.Unknown, AlphaMode.Unknown), 0, 0, RenderTargetUsage.None, FeatureLevel.Level_DEFAULT);
            var renderTarget = new WicRenderTarget(_d2dFactory, wicBitmap, renderTargetProperties);

            double off = (margin / 2) / (scale * ms);
            double xoff, yoff;

            double ss = size / (scale * ms);
            xoff = (ss - group.PaperBounds.Width) / 2;
            yoff = (ss - group.PaperBounds.Height) / 2;

            Windows.Foundation.Point offset = new Windows.Foundation.Point(-group.PaperBounds.Left + xoff, -group.PaperBounds.Top + yoff);
            System.Diagnostics.Debug.WriteLine($"Name: {group.Name}, Scale={scale}, ss={ss}");
            await ExecuteOnUIThread(() =>
            {
                RenderGroup(group, renderTarget, scale, ms, offset);
            });

            Stream sstream = await file.OpenStreamForWriteAsync();
            var stream = new WICStream(_wicFactory, sstream);

            string filetype = file.FileType.ToLower();

            BitmapEncoder encoder;
            if (filetype == ".png")
            {
                encoder = new PngBitmapEncoder(_wicFactory);
            }
            else if (filetype == ".jpg")
            {
                encoder = new JpegBitmapEncoder(_wicFactory);
            }
            else if (filetype == ".bmp")
            {
                encoder = new BmpBitmapEncoder(_wicFactory);
            }
            else
            {
                throw new Exception("Invalid image file type: " + filetype);
            }

            encoder.Initialize(stream);

            // Create a Frame encoder
            var bitmapFrameEncode = new BitmapFrameEncode(encoder);
            bitmapFrameEncode.Initialize();
            bitmapFrameEncode.SetSize(size, size);
            var pixelFormatGuid = SharpDX.WIC.PixelFormat.FormatDontCare;
            bitmapFrameEncode.SetPixelFormat(ref pixelFormatGuid);
            bitmapFrameEncode.WriteSource(wicBitmap);

            bitmapFrameEncode.Commit();
            encoder.Commit();

            bitmapFrameEncode.Dispose();
            encoder.Dispose();
            stream.Dispose();

            await sstream.FlushAsync();
            sstream.Dispose();

            wicBitmap.Dispose();
            renderTarget.Dispose();
        }

        private static IAsyncAction ExecuteOnUIThread(Windows.UI.Core.DispatchedHandler action)
        {
            return Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, action);
        }
    }
}
