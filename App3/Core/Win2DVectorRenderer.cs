using Cirros.Display;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Text;

namespace Cirros.Core.Display
{
    public class Win2DVectorRenderer
    {
        double _scale = 90;
        double _xoffset = 0;
        double _yoffset = 0;
        double _magnification = 1;
        Rect _destinationRect = Rect.Empty;      // device units

        ICanvasResourceCreator _resourceCreator;

        const float _minLineWidth = .01f;

        public double Scale
        {
            get { return _scale; }
            set
            {
                Point c = new Point((_destinationRect.Width / 2 - _xoffset) / _scale, (_destinationRect.Height / 2 - _yoffset) / _scale);

                _scale = value;

                if (_destinationRect.Width > 0 && _destinationRect.Height > 0)
                {
                    Point dc = new Point(c.X * _scale, c.Y * _scale);
                    _xoffset = -(dc.X - _destinationRect.Width / 2);
                    _yoffset = -(dc.Y - _destinationRect.Height / 2);
                }
            }
        }

        public double Magnification
        {
            get { return _magnification; }
            set { _magnification = value; }
        }

        public double XOffset
        {
            get { return _xoffset; }
            set { _xoffset = value; }
        }

        public double YOffset
        {
            get { return _yoffset; }
            set { _yoffset = value; }
        }

        public void Pan(double ddx, double ddy)
        {
            _xoffset += ddx;
            _yoffset += ddy;
        }

        public Rect DestinationRect
        {
            get { return _destinationRect; }
            set { _destinationRect = value; }
        }

        public Win2DVectorRenderer(ICanvasResourceCreator resourceCreator, Rect deviceRect)
        {
            _resourceCreator = resourceCreator;
            _destinationRect = deviceRect;
        }

        Dictionary<string, CanvasBitmap> _bitmapDictionary = new Dictionary<string, CanvasBitmap>();
        Dictionary<int, CanvasStrokeStyle> _strokeStyleDictionary = new Dictionary<int, CanvasStrokeStyle>();

        public void SetWindow(Point p1, Point p2)
        {
            if (_destinationRect.Width > 0 && _destinationRect.Height > 0)
            {
                double width = Math.Abs(p2.X - p1.X);
                double height = Math.Abs(p2.Y - p1.Y);

                if (width <= 0 || height <= 0 || double.IsNaN(width) || double.IsNaN(height))
                {
                    return;
                }
                double xs = _destinationRect.Width / width;
                double ys = _destinationRect.Height / height;

                _scale = Math.Min(xs, ys);

                _xoffset = -Math.Min(p1.X, p2.X) * _scale;
                _yoffset = -Math.Min(p1.Y, p2.Y) * _scale;
                _xoffset += (_destinationRect.Width - width * _scale) / 2;
                _yoffset += (_destinationRect.Height - height * _scale) / 2;
            }
        }

        internal Rect PaperToDisplay(Rect paper)
        {
            if (paper.IsEmpty)
            {
                return paper;
            }
            Point p = new Point(paper.X * _scale + _xoffset, paper.Y * _scale + _yoffset);
            return new Rect(p.X, p.Y, paper.Width * _scale, paper.Height * _scale);
        }

        public async Task RenderVectorList(CanvasDrawingSession ds, List<VectorEntity> vlist)
        {
            ds.Antialiasing = CanvasAntialiasing.Antialiased;

            foreach (VectorEntity ve in vlist)
            {
                if (ve != null)
                {
                    Rect test = _destinationRect;
                    test.Intersect(PaperToDisplay(ve.ItemBox));
                    if (test.IsEmpty == false && ve.IsVisible)
                    {
                        try
                        {
                            await RenderVectorEntity(ds, ve);
                        }
                        catch (Exception ex)
                        {
                            Dictionary<string, string> errorData = new Dictionary<string, string> { { "type", ve.GetType().ToString() } };
                            foreach (object o in ve.Children)
                            {
                                errorData.Add("child", o.GetType().ToString());
                            }
                            Analytics.ReportError("RenderVectorList", ex, 2, errorData, 414);
                        }
                    }
                }
            }

            ds.Antialiasing = CanvasAntialiasing.Antialiased;
        }

        public async Task<bool> LoadImage(string imageId)
        {
            int trace = 0;
            Dictionary<string, string> ed = new Dictionary<string, string> {
                                    { "scope", "VectorImageEntity" },
                                    { "_bitmapDictionary.Count 1", _bitmapDictionary.Count.ToString() },
                                    { "trace", trace.ToString() }
                                };
            bool wasLoaded = false;
            bool outOfMemory = false;

            if (_bitmapDictionary.ContainsKey(imageId))
            {
                trace = 1;
                ed["trace"] = trace.ToString();
                wasLoaded = true;
            }
            else
            {
                trace = 2;
                ed["trace"] = trace.ToString();
                _bitmapDictionary[imageId] = null;

                StorageFile file = await Utilities.GetImageSourceFileAsync(imageId);
                trace = 3;
                ed["trace"] = trace.ToString();

                if (file != null)
                {
                    trace = 4;
                    ed["trace"] = trace.ToString();
                    using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        if (_resourceCreator != null)
                        {
                            trace = 5;
                            ed["trace"] = trace.ToString();
                            try
                            {
                                trace = 6;
                                ed["trace"] = trace.ToString();
                                CanvasBitmap cbm = await CanvasBitmap.LoadAsync(_resourceCreator, fileStream);
                                _bitmapDictionary[imageId] = cbm;
                                trace = 7;
                                ed["trace"] = trace.ToString();
                            }
                            catch (OutOfMemoryException)
                            {
                                outOfMemory = true;
                            }
                            catch (Exception ex)
                            {
                                trace = 8;
                                ed["trace"] = trace.ToString();
                                ed["exception"] = ex.Message;
                                Analytics.ReportError("Win2DVectorRenderer_LoadAsync_failed", ex, 4, 415);
                            }
                        }
                        else
                        {
                            trace = 9;
                            ed["trace"] = trace.ToString();
                            Analytics.ReportEvent("Win2DVectorRenderer_LoadImage_no_resourceCreator");
                        }
                        fileStream.Dispose();
                    }
                }

                if (outOfMemory)
                {
                    trace = 9;
                    ed["trace"] = trace.ToString();
                    ed["outofmemory"] = "true";

                    Size size = await Utilities.GetImageSizeAsync(imageId);
                    trace = 10;
                    ed["trace"] = trace.ToString();
                    ed["width"] = size.Width.ToString();
                    ed["height"] = size.Height.ToString();

                    if (size.Width > 1024 || size.Height > 1024)
                    {
                        await Utilities.ResizeJpegFile(file, 1024);
                        using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                        {
                            trace = 11;
                            ed["trace"] = trace.ToString();
                            try
                            {
                                trace = 12;
                                ed["trace"] = trace.ToString();
                                CanvasBitmap cbm = await CanvasBitmap.LoadAsync(_resourceCreator, fileStream);
                                _bitmapDictionary[imageId] = cbm;
                                trace = 13;
                                ed["trace"] = trace.ToString();
                            }
                            catch (Exception ex)
                            {
                                trace = 14;
                                ed["trace"] = trace.ToString();
                                ed["exception"] = ex.Message;
                                Analytics.ReportError("Win2DVectorRenderer_LoadAsync_failed", ex, 4, 416);
                            }
                            fileStream.Dispose();
                        }
                    }
                }
            }

            if (trace > 8)
            {
                ed["wasLoaded"] = wasLoaded.ToString();
                ed["_bitmapDictionary.Count 2"] = _bitmapDictionary.Count.ToString();
                Analytics.ReportEvent("LoadImage", ed);
            }

            return wasLoaded;
        }

        public async Task LoadImages(VectorEntity ve)
        {
            foreach (object o in ve.Children)
            {
                if (o is VectorImageEntity)
                {
                    string imageId = ((VectorImageEntity)o).ImageId;
                    if (imageId != null)
                    {
                        await LoadImage(imageId);
                    }
                }
                else if (o is VectorEntity)
                {
                    await LoadImages(o as VectorEntity);
                }
            }
        }

        public void UpdateLineStyles()
        {
            _strokeStyleDictionary.Clear();

            foreach (LineType lineType in Globals.LineTypeTable.Values)
            {
                CanvasStrokeStyle css = new CanvasStrokeStyle();

                if (lineType.StrokeDashArray != null && lineType.StrokeDashArray.Count > 1 && lineType.StrokeDashArray.Count % 2 == 0)
                {
                    float[] dashStyle = new float[lineType.StrokeDashArray.Count];
                    for (int i = 0; i < lineType.StrokeDashArray.Count; i++)
                    {
                        //dashStyle[i] = (float)(lineType.StrokeDashArray[i] * _scale);
                        dashStyle[i] = (float)lineType.StrokeDashArray[i];
                    }

                    css.CustomDashStyle = dashStyle;
                }

                switch (Globals.ActiveDrawing.LineEndCap)
                {
                    case PenLineCap.Flat:
                        css.DashCap = CanvasCapStyle.Flat;
                        css.LineJoin = CanvasLineJoin.Miter;
                        css.StartCap = CanvasCapStyle.Flat;
                        css.EndCap = CanvasCapStyle.Flat;
                        break;
                    case PenLineCap.Round:
                        css.DashCap = CanvasCapStyle.Round;
                        css.LineJoin = CanvasLineJoin.Round;
                        css.StartCap = CanvasCapStyle.Round;
                        css.EndCap = CanvasCapStyle.Round;
                        break;
                    case PenLineCap.Square:
                        css.DashCap = CanvasCapStyle.Square;
                        css.LineJoin = CanvasLineJoin.Miter;
                        css.StartCap = CanvasCapStyle.Square;
                        css.EndCap = CanvasCapStyle.Square;
                        break;
                    case PenLineCap.Triangle:
                        css.DashCap = CanvasCapStyle.Triangle;
                        css.LineJoin = CanvasLineJoin.Bevel;
                        css.StartCap = CanvasCapStyle.Triangle;
                        css.EndCap = CanvasCapStyle.Triangle;
                        break;
                }

                _strokeStyleDictionary.Add(lineType.Id, css);
            }
        }

        public async Task Regenerate(VectorList vectorList)
        {
            if (vectorList != null)
            {
                _bitmapDictionary.Clear();

                UpdateLineStyles();

                vectorList.Regenerate();

                foreach (VectorEntity ve in vectorList.AsList)
                {
                    await LoadImages(ve);
                }
            }
        }

        private async Task RenderVectorEntity(CanvasDrawingSession ds, VectorEntity ve)
        {
            try
            {
                float lineWidth = (float)(ve.ScaleLineWidth ? Math.Max(ve.LineWidth * _scale, _minLineWidth) : ve.LineWidth);
                CanvasStrokeStyle linetype;
                Color color = ve.Color;
                Color fillcolor = ve.FillColor;

                float xoffset = (float)_xoffset;
                float yoffset = (float)_yoffset;

                if (ve.Opacity < 1)
                {
                    color.A = (byte)(color.A * ve.Opacity);
                    fillcolor.A = (byte)(fillcolor.A * ve.Opacity);
                }

                if (ve.IsHighlighted)
                {
                    if (Globals.HighlightColor == Colors.Transparent)
                    {
                        color.A = (byte)(color.A / 3);
                        fillcolor.A = (byte)(fillcolor.A / 3);
                        //lineWidth = Math.Max(lineWidth * 3, 4);
                    }
                    else
                    {
                        color = Globals.HighlightColor;
                        fillcolor = Globals.HighlightColor;
                    }
                }

                if (ve.IsVisible)
                {
                    CanvasPathBuilder pathBuilder = null;

                    if (ve.Children != null)
                    {
                        foreach (object o in ve.Children)
                        {
                            Matrix3x2 saveMatrix = ds.Transform;

                            if (o is List<Point> pc)
                            {
                                if (pc.Count > 1)
                                {
                                    try
                                    {
                                        if (pathBuilder == null)
                                        {
                                            pathBuilder = new CanvasPathBuilder(ds.Device);
                                            pathBuilder.SetFilledRegionDetermination(ve.FillEvenOdd ? CanvasFilledRegionDetermination.Alternate : CanvasFilledRegionDetermination.Winding);
                                        }

                                        linetype = _strokeStyleDictionary.ContainsKey(ve.LineType) ? _strokeStyleDictionary[ve.LineType] : null;

                                        float x0 = (float)Math.Round(pc[0].X * _scale + xoffset, 1);
                                        float y0 = (float)Math.Round(pc[0].Y * _scale + yoffset, 1);

                                        pathBuilder.BeginFigure((float)x0, (float)y0);

                                        for (int i = 1; i < pc.Count; i++)
                                        {
                                            float x1 = (float)Math.Round(pc[i].X * _scale + xoffset, 1);
                                            float y1 = (float)Math.Round(pc[i].Y * _scale + yoffset, 1);
#if false    // eliminate coincident points
                                            if (x0 != x1 || y0 != y1)
                                            {
                                                pathBuilder.AddLine(x1, y1);

                                                x0 = x1;
                                                y0 = y1;
                                            }
#else       // eliminate points within a lineweight of each other
                                            if (Math.Abs(x0 - x1) > (lineWidth / 2) || Math.Abs(y0 - y1) > (lineWidth / 2))
                                            {
                                                pathBuilder.AddLine(x1, y1);

                                                x0 = x1;
                                                y0 = y1;
                                            }
                                            //else if (i == (pc.Count - 1) && (x0 != x1 || y0 != y1))
                                            else if (i == (pc.Count - 1))
                                            {
                                                pathBuilder.AddLine(x1, y1);
                                            }
#endif
                                        }

                                        pathBuilder.EndFigure(ve.Fill ? CanvasFigureLoop.Closed : CanvasFigureLoop.Open);
                                    }
                                    catch (Exception ex)
                                    {
                                        Analytics.ReportError("RenderVectorEntity", ex, 4, 417);
                                    }
                                }
                            }
                            else if (o is VectorEntity ve1)
                            {
                                await RenderVectorEntity(ds, ve1);
                            }
                            else if (o is VectorMarkerEntity vm)
                            {
                                if (pathBuilder == null)
                                {
                                    pathBuilder = new CanvasPathBuilder(ds.Device);
                                }

                                float x = (float)(vm.Location.X * _scale + xoffset);
                                float y = (float)(vm.Location.Y * _scale + yoffset);
                                float s = (float)(vm.Size * _magnification);
                                float hs = s / 2;

                                switch (vm.Type)
                                {
                                    case HandleType.Square:
                                        pathBuilder.BeginFigure(x - hs, y - hs);
                                        pathBuilder.AddLine(x - hs, y + hs);
                                        pathBuilder.AddLine(x + hs, y + hs);
                                        pathBuilder.AddLine(x + hs, y - hs);
                                        pathBuilder.AddLine(x - hs, y - hs);
                                        pathBuilder.EndFigure(CanvasFigureLoop.Closed);
                                        break;

                                    case HandleType.Circle:
                                        Vector2 cv = new Vector2(x, y);
                                        pathBuilder.BeginFigure(x + hs, y);
                                        pathBuilder.AddArc(cv, hs, hs, 0, (float)Math.PI * 2);
                                        pathBuilder.EndFigure(CanvasFigureLoop.Closed);
                                        break;

                                    case HandleType.Diamond:
                                        pathBuilder.BeginFigure(x - hs, y);
                                        pathBuilder.AddLine(x, y + hs);
                                        pathBuilder.AddLine(x + hs, y);
                                        pathBuilder.AddLine(x, y - hs);
                                        pathBuilder.AddLine(x - hs, y);
                                        pathBuilder.EndFigure(CanvasFigureLoop.Closed);
                                        break;

                                    case HandleType.Triangle:
                                        float ts = s * .866f;
                                        pathBuilder.BeginFigure(x + hs - hs, y - hs);
                                        pathBuilder.AddLine(x - hs, y + ts - hs);
                                        pathBuilder.AddLine(x + s - hs, y + ts - hs);
                                        pathBuilder.AddLine(x + hs - hs, y - hs);
                                        pathBuilder.EndFigure(CanvasFigureLoop.Closed);
                                        break;
                                }

                                lineWidth = .5f;
                            }
                            else if (o is VectorArcEntity va)
                            {
                                float cx = (float)(va.Center.X * _scale + xoffset);
                                float cy = (float)(va.Center.Y * _scale + yoffset);
                                float radius = (float)(va.Radius * _scale);

                                linetype = _strokeStyleDictionary.ContainsKey(ve.LineType) ? _strokeStyleDictionary[ve.LineType] : null;

                                if (va.IsCircle && (linetype == null || linetype.CustomDashStyle.Length == 0))
                                {
                                    CanvasGeometry pg = CanvasGeometry.CreateCircle(ds, cx, cy, radius);
                                    if (ve.Fill)
                                    {
                                        ds.FillGeometry(pg, ve.FillColor);
                                    }
                                    ds.DrawGeometry(pg, color, lineWidth, linetype);
                                }
                                else
                                {
                                    Vector2 cv = new Vector2(cx, cy);

                                    float startAngle = (float)va.StartAngle;
                                    float sweepAngle = (float)va.IncludedAngle;

                                    if (va.IsCircle)
                                    {
                                        startAngle = 0;
                                        sweepAngle = (float)(2 * Math.PI);
                                    }

                                    if (pathBuilder == null)
                                    {
                                        pathBuilder = new CanvasPathBuilder(ds.Device);
                                    }

                                    Point start = Construct.PolarOffset(new Point(cx, cy), radius, startAngle);

                                    pathBuilder.BeginFigure((float)start.X, (float)start.Y);
                                    pathBuilder.AddArc(cv, radius, radius, startAngle, sweepAngle);
                                    pathBuilder.EndFigure(CanvasFigureLoop.Open);
                                }
                            }
                            else if (o is VectorTextEntity vt)
                            {
                                string[] lines = vt.Text.Split(new[] { '\n' });

                                double tx = vt.Location.X * _scale + _xoffset;
                                double ty = vt.Location.Y * _scale + _yoffset;
                                double th = vt.TextHeight * _scale;
                                double lh = vt.TextHeight * _scale * vt.LineSpacing;

                                CanvasTextFormat ctf = new CanvasTextFormat();
                                ctf.FontFamily = vt.FontFamily;
                                ctf.VerticalAlignment = CanvasVerticalAlignment.Top;
                                ctf.LineSpacing = (float)lh;
                                if (ve.IsHighlighted && Globals.HighlightColor == Colors.Transparent)
                                {
                                    ctf.FontWeight = FontWeights.Bold;
                                }
                                else
                                {
                                    ctf.FontWeight = FontWeights.Normal;
                                }

                                Matrix3x2 spacingMatrix = Matrix3x2.Identity;
                                Matrix3x2 rotationMatrix = Matrix3x2.Identity;

                                //if (vt.CharacterSpacing != 1)
                                //{
                                //    spacingMatrix = Matrix3x2.CreateScale((float)vt.CharacterSpacing, 1);
                                //}

                                switch (vt.TextAlignment)
                                {
                                    default:
                                    case TextAlignment.Left:
                                        ctf.HorizontalAlignment = CanvasHorizontalAlignment.Left;
                                        break;

                                    case TextAlignment.Center:
                                        ctf.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                                        break;

                                    case TextAlignment.Right:
                                        ctf.HorizontalAlignment = CanvasHorizontalAlignment.Right;
                                        break;
                                }

                                double fs = Dx.GetFontSizeFromHeight(vt.FontFamily, th);
                                ctf.FontSize = (float)fs;

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

                                float mx = ds.Device.MaximumBitmapSizeInPixels;
                                float my = ds.Device.MaximumBitmapSizeInPixels;

                                float baseline = 0;
                                CanvasTextLayout textLayout = new CanvasTextLayout(ds, vt.Text, ctf, mx, my);
                                CanvasLineMetrics[] lm = textLayout.LineMetrics;
                                baseline = lm[0].Baseline;

                                if (vt.Angle != 0)
                                {
                                    rotationMatrix = Matrix3x2.CreateRotation((float)(vt.Angle / Construct.cRadiansToDegrees));

                                    Vector2 v = new Vector2(0, -baseline);
                                    v = Vector2.Transform(v, rotationMatrix);
                                    tx += v.X;
                                    ty += v.Y;
                                }
                                else
                                {
                                    ty -= baseline;
                                }

                                if (spacingMatrix != Matrix3x2.Identity || rotationMatrix != Matrix3x2.Identity)
                                {
                                    Matrix3x2 matrix = Matrix3x2.CreateTranslation((float)-tx, (float)-ty);
                                    matrix = matrix * spacingMatrix;
                                    matrix = matrix * rotationMatrix;
                                    matrix = matrix * Matrix3x2.CreateTranslation((float)tx, (float)ty);

                                    ds.Transform *= matrix;
                                }

                                if (vt.CharacterSpacing == 1)
                                {
                                    ds.DrawText(vt.Text, (float)tx, (float)ty, color, ctf);
                                }
                                else
                                {
                                    CanvasSolidColorBrush brush = new CanvasSolidColorBrush(_resourceCreator, color);
                                    Win2DTextRenderer textRenderer = new Win2DTextRenderer(ds, brush, (float)vt.CharacterSpacing);

                                    if (ctf.HorizontalAlignment == CanvasHorizontalAlignment.Left)
                                    {
                                        textLayout.DrawToTextRenderer(textRenderer, (float)tx, (float)ty);
                                    }
                                    else
                                    {
                                        ctf.HorizontalAlignment = CanvasHorizontalAlignment.Left;

                                        float fangle = (float)(vt.Angle / Construct.cRadiansToDegrees);

                                        foreach (string s in lines)
                                        {
                                            CanvasTextLayout tl = new CanvasTextLayout(ds, s, ctf, mx, my);

                                            double nominalWidth = ctf.FontSize * .8f;
                                            double space = nominalWidth * (vt.CharacterSpacing - 1);
                                            double w = 0;

                                            foreach (CanvasClusterMetrics cm in tl.ClusterMetrics)
                                            {
                                                w += cm.Width;
                                            }

                                            w += space * (tl.ClusterMetrics.Length - 1);

                                            if (vt.TextAlignment == TextAlignment.Center)
                                            {
                                                w /= 2;
                                            }

                                            tl.DrawToTextRenderer(textRenderer, (float)(tx + - w), (float)ty);
                                            ty += lh;
                                        }
                                    }
                                }
                            }
                            else if (o is VectorImageEntity vi)
                            {
                                try
                                {
                                    if (_bitmapDictionary.ContainsKey(vi.ImageId))
                                    {
                                        CanvasBitmap cbm = _bitmapDictionary[vi.ImageId];
                                        if (cbm != null)
                                        {
                                            double x = vi.Origin.X * _scale + _xoffset;
                                            double y = vi.Origin.Y * _scale + _yoffset;
                                            double w = vi.Width * _scale;
                                            double h = vi.Height * _scale;
                                            Rect src = cbm.Bounds;
                                            Rect dest = new Rect(x, y, w, h);

                                            if (vi.Matrix != null && vi.Matrix.IsIdentity == false)
                                            {
                                                Matrix3x2 m = new Matrix3x2(
                                                    (float)vi.Matrix.M11, (float)vi.Matrix.M12,
                                                    (float)vi.Matrix.M21, (float)vi.Matrix.M22,
                                                    (float)vi.Matrix.OffsetX, (float)vi.Matrix.OffsetY);

                                                Matrix3x2 matrix = System.Numerics.Matrix3x2.CreateTranslation((float)-x, (float)-y);
                                                matrix = matrix * m;
                                                matrix = matrix * System.Numerics.Matrix3x2.CreateTranslation((float)x, (float)y);
                                                ds.Transform *= matrix;
                                            }

                                            ds.DrawImage(cbm, dest, src, (float)vi.Opacity);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Analytics.ReportError("RenderVectorEntity", ex, 4, 418);
                                }
                            }
                            ds.Transform = saveMatrix;
                        }
                    }

                    if (pathBuilder != null)
                    {
                        CanvasGeometry pg = CanvasGeometry.CreatePath(pathBuilder);
                        linetype = _strokeStyleDictionary.ContainsKey(ve.LineType) ? _strokeStyleDictionary[ve.LineType] : null;
                        if (ve.Fill)
                        {
                            ds.FillGeometry(pg, fillcolor);
                        }

                        if (linetype == null)
                        {
                            // fix for appcenter exception-419 - null object in GetCanvasStrokeStyleForLineWidth
                            ds.DrawGeometry(pg, color);
                        }
                        else
                        {
                            linetype = GetCanvasStrokeStyleForLineWidth(linetype, lineWidth);
                            ds.DrawGeometry(pg, color, lineWidth, linetype);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Dictionary<string, string> d = new Dictionary<string, string> {
                                    { "method", "VectorImageEntity" },
                                    { "scope", "outer" },
                                };
                Analytics.ReportError(e, d, 419);
            }
        }

        CanvasStrokeStyle GetCanvasStrokeStyleForLineWidth(CanvasStrokeStyle css, float width)
        {
            if (css.CustomDashStyle == null || css.CustomDashStyle.Length == 0)
            {
                return css;
            }

            CanvasStrokeStyle css1 = new CanvasStrokeStyle();
            css1.DashCap = css.DashCap;
            css1.LineJoin = css.LineJoin;
            css1.StartCap = css.StartCap;
            css1.EndCap = css.EndCap;

            float[] da = new float[css.CustomDashStyle.Length];
            for (int i = 0; i < da.Length; i++)
            {
                da[i] = css.CustomDashStyle[i] * (float)_scale;

                if (css1.DashCap == CanvasCapStyle.Flat)
                {
                    da[i] /= width;
                }
                else 
                {
                    if ((i % 2) == 0)
                    {
                        da[i] = (da[i] - width) / width;
                    }
                    else
                    {
                        da[i] = (da[i] + width) / width;
                    }
                }
            }

            css1.CustomDashStyle = da;

            return css1;
        }
    }
}
