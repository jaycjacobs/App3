using Cirros.Utility;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Svg
{
    public class SvgDomReader : SvgReader
    {
        SvgDom _dom = null;

        public SvgDomReader(XmlReaderSettings xrs)
            : base(xrs)
        {
            _dom = new SvgDom();
        }

        public SvgDom Dom
        {
            get
            {
                return _dom;
            }
        }

        protected override ISvgContainer ProcessSvgElement(Dictionary<string, string> attributes)
        {
            double width = 1;
            double height = 1;
            Point viewBoxOrigin = new Point();
            Size viewBoxSize = new Size();
            string preserveAspectRatio = "";

            string widthUnit = "px";
            string heightUnit = "px";

            if (attributes.ContainsKey("width"))
            {
                _dom.LengthFromParameter(attributes["width"], out width, out widthUnit);
            }

            if (attributes.ContainsKey("height"))
            {
                _dom.LengthFromParameter(attributes["height"], out height, out heightUnit);
            }

            if (attributes.ContainsKey("viewBox"))
            {
                string[] vba = ParseAttributeList(attributes["viewBox"]);
                if (vba.Length == 4)
                {
                    viewBoxOrigin.X = _dom.DoubleFromParameter(vba[0]);
                    viewBoxOrigin.Y = _dom.DoubleFromParameter(vba[1]);
                    viewBoxSize.Width = _dom.DoubleFromParameter(vba[2]);
                    viewBoxSize.Height = _dom.DoubleFromParameter(vba[3]);
                }
            }
            else
            {
                viewBoxSize.Width = width;
                viewBoxSize.Height = height;
            }

            if (widthUnit == "%")
            {
                width = width * viewBoxSize.Width / 100;
                widthUnit = "px";
            }

            if (heightUnit == "%")
            {
                height = height * viewBoxSize.Height / 100;
                heightUnit = "px";
            }

            if (attributes.ContainsKey("preserveAspectRatio"))
            {
                preserveAspectRatio = attributes["preserveAspectRatio"];
            }

            width = _dom.ApplyXScale(width, widthUnit) / 96;
            height = _dom.ApplyYScale(height, heightUnit) / 96;

            _dom.SetViewPort(width, height, viewBoxOrigin, viewBoxSize, widthUnit, heightUnit, preserveAspectRatio);

            return _dom;
        }

        protected override void UnexpectedNode()
        {
            //System.Diagnostics.Debug.WriteLine("Unimplemented: UnexpectedNode {0}", _xmlReader.Name);
        }

        protected override void ReadXmlDeclaration()
        {
        }

        protected override void SvgUnimplementedElement(Dictionary<string, string> attributes)
        {
            //System.Diagnostics.Debug.WriteLine("Unimplemented: SvgUnimplementedElement: {0}", _xmlReader.Name);
        }

        private void parseSvgShapeAttributes(Dictionary<string, string> attributes, SvgShapeType shape)
        {
            if (attributes.ContainsKey("db:type"))
            {
                shape.dbHint = attributes["db:hint"];
            }

            if (attributes.ContainsKey("id"))
            {
                shape.Id = attributes["id"];
            }

            if (attributes.ContainsKey("class"))
            {
                shape.Class = attributes["class"];
            }

            if (attributes.ContainsKey("stroke-width"))
            {
                shape.StrokeWidth = _dom.LengthFromParameter(attributes["stroke-width"]);
            }

            if (attributes.ContainsKey("stroke"))
            {
                if (attributes["stroke"] == "none")
                {
                    shape.Stroke = false;
                }
                else
                {
                    shape.Stroke = true;
                    shape.StrokeColor = ParseSvgColor(attributes["stroke"]);
                }
            }

            if (attributes.ContainsKey("stroke-opacity"))
            {
                shape.ShapeOpacity = double.Parse(attributes["stroke-opacity"]);
                shape.StrokeColor.A = (byte)(shape.StrokeColor.A * shape.ShapeOpacity);
            }

            if (attributes.ContainsKey("opacity"))
            {
                shape.Opacity = double.Parse(attributes["opacity"]);
            }

            if (attributes.ContainsKey("visibility"))
            {
                shape.Visible = attributes["visibility"] != "hidden";
            }

            if (attributes.ContainsKey("fill"))
            {
                if (attributes["fill"] == "none")
                {
                    shape.Fill = false;
                }
                else
                {
                    shape.Fill = true;
                    shape.FillColor = ParseSvgColor(attributes["fill"]);
                }
            }

            if (attributes.ContainsKey("fill-opacity"))
            {
                shape.FillOpacity = double.Parse(attributes["fill-opacity"]);
                shape.FillColor.A = (byte)(shape.FillColor.A * shape.FillOpacity);
            }

            if (attributes.ContainsKey("fill-rule"))
            {
                shape.FillEvenOdd = attributes["fill-rule"] == "evenodd";
            }

            if (attributes.ContainsKey("transform"))
            {
                ParseTransform(attributes["transform"], shape);
            }

            if (attributes.ContainsKey("font-size"))
            {
                string unit;
                double fontSize = _dom.SizeFromParameter(attributes["font-size"], out unit);

                if (unit != "em" && unit != "ex")
                {
                    _dom.CurrentFontSize = fontSize;
                }
            }
        }

        private void ParseTransform(string attributeValue, SvgShapeType shape)
        {
            string tfregex = @"(?<name>\w+)\s*\((?<parameters>[\s\d,-.eE]+)*\)";
            MatchCollection mc = Regex.Matches(attributeValue, tfregex);

            double[] da = null;
            string tftype = "";

            try
            {
                foreach (Match m in mc)
                {
                    Transform transform = null;

                    if (m.Groups["name"].Success)
                    {
                        tftype = m.Groups["name"].Value;
                    }

                    if (m.Groups["parameters"].Success)
                    {
                        string nregex = @"(?<number>(-?\d+(\.\d*([Ee][+-]\d+)?)?)|(-?\.\d*([Ee][+-]\d+)?))";
                        MatchCollection nmc = Regex.Matches(m.Groups["parameters"].Value, nregex);
                        da = new double[nmc.Count];

                        for (int i = 0; i < nmc.Count; i++)
                        {
                            if (nmc[i].Groups["number"].Success)
                            {
                                da[i] = double.Parse(nmc[i].Groups["number"].Value);
                            }
                            else
                            {
                                da[i] = 0;
                            }
                        }
                    }

                    if (da != null)
                    {
                        if (tftype == "matrix")
                        {
                            if (da.Length == 6)
                            {
                                transform = new MatrixTransform();
                                double xoffset = da[4] * _dom.XScale;
                                double yoffset = da[5] * _dom.YScale;
                                ((MatrixTransform)transform).Matrix = new Matrix(da[0], da[1], da[2], da[3], xoffset, yoffset);
                            }
                        }
                        else if (tftype == "translate")
                        {
                            if (da.Length == 1)
                            {
                                transform = new TranslateTransform();
                                ((TranslateTransform)transform).X = da[0] * _dom.XScale;
                                ((TranslateTransform)transform).Y = 0;
                            }
                            else if (da.Length == 2)
                            {
                                transform = new TranslateTransform();
                                ((TranslateTransform)transform).X = da[0] * _dom.XScale;
                                ((TranslateTransform)transform).Y = da[1] * _dom.YScale;
                            }
                        }
                        else if (tftype == "scale")
                        {
                            if (da.Length == 1)
                            {
                                transform = new ScaleTransform();
                                ((ScaleTransform)transform).ScaleX = da[0];
                                ((ScaleTransform)transform).ScaleY = da[0];
                            }
                            else if (da.Length == 2)
                            {
                                transform = new ScaleTransform();
                                ((ScaleTransform)transform).ScaleX = da[0];
                                ((ScaleTransform)transform).ScaleY = da[1];
                            }
                        }
                        else if (tftype == "rotate")
                        {

                            if (da.Length >= 1)
                            {
                                transform = new RotateTransform();
                                ((RotateTransform)transform).Angle = da[0];

                                if (da.Length == 3)
                                {
                                    ((RotateTransform)transform).CenterX = da[1] * _dom.XScale;
                                    ((RotateTransform)transform).CenterY = da[2] * _dom.YScale;
                                }
                            }
                        }
                        else if (tftype == "skewX")
                        {
                            if (da.Length == 1)
                            {
                                transform = new SkewTransform();
                                ((SkewTransform)transform).AngleX = da[0];
                            }
                        }
                        else if (tftype == "skewY")
                        {
                            if (da.Length == 1)
                            {
                                transform = new SkewTransform();
                                ((SkewTransform)transform).AngleY = da[0];
                            }
                        }
                    }

                    shape.Transform = transform;
                }
            }
            catch
            {
            }
        }

        protected override ISvgContainer ProcessSvgGroup(ISvgContainer container, Dictionary<string, string> attributes)
        {
            SvgGroupType group = new SvgGroupType(container);
            parseSvgShapeAttributes(attributes, group);

            return group as ISvgContainer;
        }

        protected override ISvgContainer ProcessSvgDefs(ISvgContainer container, Dictionary<string, string> attributes)
        {
            SvgDefsType defs = new SvgDefsType(container);
            parseSvgShapeAttributes(attributes, defs);

            return defs as ISvgContainer;
        }

        protected override ISvgContainer ProcessSvgSymbol(ISvgContainer container, Dictionary<string, string> attributes)
        {
            SvgSymbolType defs = new SvgSymbolType(container);
            parseSvgShapeAttributes(attributes, defs);

            return defs as ISvgContainer;
        }

        protected override ISvgContainer ProcessSvgMarker(ISvgContainer container, Dictionary<string, string> attributes)
        {
            SvgMarkerType defs = new SvgMarkerType(container);
            parseSvgShapeAttributes(attributes, defs);

            return defs as ISvgContainer;
        }

        private void TransformChild(SvgShapeType shape, Transform transform)
        {
            if (shape is ISvgContainer)
            {
                foreach (SvgShapeType child in ((ISvgContainer)shape).Entities)
                {
                    TransformChild(child, transform);
                }
            }
            else
            {
                shape.Transform = DeepCopyTransform(transform);
            }
        }

        public static Transform DeepCopyTransform(Transform transform)
        {
            Transform other = null;

            if (transform != null)
            {
                if (transform is TransformGroup)
                {
                    TransformGroup tg = new TransformGroup();

                    foreach (Transform ct in ((TransformGroup)transform).Children)
                    {
                        Transform t = DeepCopyTransform(ct);
                        tg.Children.Add(t);
                    }
                    other = tg;
                }
                else if (transform is TranslateTransform)
                {
                    TranslateTransform t = new TranslateTransform();
                    t.X = ((TranslateTransform)transform).X;
                    t.Y = ((TranslateTransform)transform).Y;
                    other = t;
                }
                else if (transform is RotateTransform)
                {
                    RotateTransform t = new RotateTransform();
                    t.Angle = ((RotateTransform)transform).Angle;
                    t.CenterX = ((RotateTransform)transform).CenterX;
                    t.CenterY = ((RotateTransform)transform).CenterY;
                    other = t;
                }
                else if (transform is ScaleTransform)
                {
                    ScaleTransform t = new ScaleTransform();
                    t.ScaleX = ((ScaleTransform)transform).ScaleX;
                    t.ScaleY = ((ScaleTransform)transform).ScaleY;
                    other = t;
                }
                else if (transform is MatrixTransform)
                {
                    MatrixTransform t = new MatrixTransform();
                    Matrix m = ((MatrixTransform)transform).Matrix;
                    t.Matrix = new Matrix(m.M11, m.M12, m.M21, m.M22, m.OffsetX, m.OffsetY);
                    other = t;
                }
                else if (transform is SkewTransform)
                {
                    SkewTransform t = new SkewTransform();
                    t.AngleX = ((SkewTransform)transform).AngleX;
                    t.AngleY = ((SkewTransform)transform).AngleY;
                    t.CenterX = ((SkewTransform)transform).CenterX;
                    t.CenterY = ((SkewTransform)transform).CenterY;
                    other = t;
                }
            }

            return other;
        }

        protected override void ProcessSvgUse(ISvgContainer container, Dictionary<string, string> attributes)
        {
            double x = 0;
            double y = 0;
            double width = 0;
            double height = 0;
            string href = "";

            if (attributes.ContainsKey("xlink:href"))
            {
                href = attributes["xlink:href"];
                if (href.StartsWith("#"))
                {
                    href = href.Substring(1);
                }
            }

            if (attributes.ContainsKey("x"))
            {
                x = _dom.XLengthFromParameter(attributes["x"]);
            }

            if (attributes.ContainsKey("y"))
            {
                y = _dom.YLengthFromParameter(attributes["y"]);
            }

            // unimplemented: if width and height are set, the use data should be scaled (like viewport)
            if (attributes.ContainsKey("width"))
            {
                width = _dom.XLengthFromParameter(attributes["width"]);
            }

            if (attributes.ContainsKey("height"))
            {
                height = _dom.YLengthFromParameter(attributes["height"]);
            }

            SvgShapeType shape = Find(href, _dom);
            if (shape != null)
            {
                shape = shape.DeepCopy();

                if (shape != null)
                {
                    SvgGroupType group = new SvgGroupType(container);
                    parseSvgShapeAttributes(attributes, group);
                    group.Add(shape);

                    if (x != 0 || y != 0)
                    {
                        TranslateTransform t = new TranslateTransform();
                        t.X = x;
                        t.Y = y;
                        group.Transform = t;
                    }

                    if (group.Transform != null)
                    {
                        Transform gt = DeepCopyTransform(group.Transform);
                        foreach (SvgShapeType child in group.Entities)
                        {
                            TransformChild(child, gt);
                        }
                    }
                }
            }
        }

        protected override void ProcessSvgLine(ISvgContainer container, Dictionary<string, string> attributes)
        {
            double x1 = 0;
            double y1 = 0;
            double x2 = 0;
            double y2 = 0;

            if (attributes.ContainsKey("x1"))
            {
                x1 = _dom.XLengthFromParameter(attributes["x1"]);
            }

            if (attributes.ContainsKey("y1"))
            {
                y1 = _dom.YLengthFromParameter(attributes["y1"]);
            }

            if (attributes.ContainsKey("x2"))
            {
                x2 = _dom.XLengthFromParameter(attributes["x2"]);
            }

            if (attributes.ContainsKey("y2"))
            {
                y2 = _dom.YLengthFromParameter(attributes["y2"]);
            }

            SvgLineType shape = new SvgLineType(container, x1, y1, x2, y2);
            parseSvgShapeAttributes(attributes, shape);
        }

        protected override void ProcessSvgPolyline(ISvgContainer container, Dictionary<string, string> attributes)
        {
            if (attributes.ContainsKey("points"))
            {
                string[] pa = ParseAttributeList(attributes["points"]);

                if ((pa.Length % 2) == 0)
                {
                    List<Point> points = new List<Point>();

                    for (int i = 0; i < pa.Length; i += 2)
                    {
                        Point p = new Point();
                        p.X = _dom.XLengthFromParameter(pa[i]);
                        p.Y = _dom.YLengthFromParameter(pa[i + 1]);
                        points.Add(p);
                    }

                    SvgPolylineType shape = new SvgPolylineType(container, points);
                    parseSvgShapeAttributes(attributes, shape);
                }
            }
        }

        protected override void ProcessSvgPolygon(ISvgContainer container, Dictionary<string, string> attributes)
        {
            if (attributes.ContainsKey("points"))
            {
                string[] pa = ParseAttributeList(attributes["points"]);

                if ((pa.Length % 2) == 0)
                {
                    List<Point> points = new List<Point>();

                    for (int i = 0; i < pa.Length; i += 2)
                    {
                        Point p = new Point();
                        p.X = _dom.XLengthFromParameter(pa[i]);
                        p.Y = _dom.YLengthFromParameter(pa[i + 1]);
                        points.Add(p);
                    }

                    SvgPolygonType shape = new SvgPolygonType(container, points);
                    parseSvgShapeAttributes(attributes, shape);
                }
            }
        }

        protected override void ProcessSvgEllipse(ISvgContainer container, Dictionary<string, string> attributes)
        {
            double cx = 0;
            double cy = 0;
            double rx = 0;
            double ry = 0;

            if (attributes.ContainsKey("cx"))
            {
                cx = _dom.XLengthFromParameter(attributes["cx"]);
            }

            if (attributes.ContainsKey("cy"))
            {
                cy = _dom.YLengthFromParameter(attributes["cy"]);
            }

            if (attributes.ContainsKey("rx"))
            {
                rx = _dom.XLengthFromParameter(attributes["rx"]);
            }

            if (attributes.ContainsKey("ry"))
            {
                ry = _dom.YLengthFromParameter(attributes["ry"]);
            }

            SvgEllipseType shape = new SvgEllipseType(container, cx, cy, rx, ry);
            parseSvgShapeAttributes(attributes, shape);
        }

        protected override void ProcessSvgCircle(ISvgContainer container, Dictionary<string, string> attributes)
        {
            double cx = 0;
            double cy = 0;
            double r = 0;

            if (attributes.ContainsKey("cx"))
            {
                cx = _dom.XLengthFromParameter(attributes["cx"]);
            }

            if (attributes.ContainsKey("cy"))
            {
                cy = _dom.YLengthFromParameter(attributes["cy"]);
            }

            if (attributes.ContainsKey("r"))
            {
                r = _dom.XLengthFromParameter(attributes["r"]);
            }

            SvgCircleType shape = new SvgCircleType(container, cx, cy, r);
            parseSvgShapeAttributes(attributes, shape);
        }

        protected override void ProcessSvgImage(ISvgContainer container, Dictionary<string, string> attributes)
        {
            double x = 0;
            double y = 0;
            double width = 0;
            double height = 0;
            double opacity = 1;
            string preserveAspectRatio = "";
            string href = "";
            string imageId = "";
            string name = "";

            if (attributes.ContainsKey("x"))
            {
                x = _dom.XLengthFromParameter(attributes["x"]);
            }

            if (attributes.ContainsKey("y"))
            {
                y = _dom.YLengthFromParameter(attributes["y"]);
            }

            if (attributes.ContainsKey("width"))
            {
                width = _dom.XLengthFromParameter(attributes["width"]);
            }

            if (attributes.ContainsKey("height"))
            {
                height = _dom.YLengthFromParameter(attributes["height"]);
            }

            if (attributes.ContainsKey("opacity"))
            {
                opacity = _dom.YLengthFromParameter(attributes["opacity"]);
            }

            if (attributes.ContainsKey("preserveAspectRatio"))
            {
                preserveAspectRatio = attributes["preserveAspectRatio"];
            }

            if (attributes.ContainsKey("xlink:href"))
            {
                href = attributes["xlink:href"];
                imageId = _dom.GetImageId(href);
                name = _dom.GetImageName(href);
            }

            if (width > 0 && height > 0)
            {
                SvgImageType shape = new SvgImageType(container, x, y, width, height, opacity, imageId, name, preserveAspectRatio);
                parseSvgShapeAttributes(attributes, shape);
            }
        }

        protected override void ProcessSvgRect(ISvgContainer container, Dictionary<string, string> attributes)
        {
            double x = 0;
            double y = 0;
            double width = 0;
            double height = 0;
            double rx = 0;
            double ry = 0;

            if (attributes.ContainsKey("x"))
            {
                x = _dom.XLengthFromParameter(attributes["x"]);
            }

            if (attributes.ContainsKey("y"))
            {
                y = _dom.YLengthFromParameter(attributes["y"]);
            }

            if (attributes.ContainsKey("width"))
            {
                width = _dom.XLengthFromParameter(attributes["width"]);
            }

            if (attributes.ContainsKey("height"))
            {
                height = _dom.YLengthFromParameter(attributes["height"]);
            }

            if (width > 0 && height > 0)
            {
                if (attributes.ContainsKey("rx") && attributes.ContainsKey("ry"))
                {
                    rx = _dom.XLengthFromParameter(attributes["rx"]);
                    ry = _dom.YLengthFromParameter(attributes["ry"]);
                }
                else if (attributes.ContainsKey("rx"))
                {
                    rx = ry = _dom.XLengthFromParameter(attributes["rx"]);
                }
                else if (attributes.ContainsKey("ry"))
                {
                    rx = ry = _dom.YLengthFromParameter(attributes["ry"]);
                }

                if (rx > width / 2)
                {
                    rx = width / 2;
                }

                if (ry > height / 2)
                {
                    ry = height / 2;
                }

                SvgRectType shape = new SvgRectType(container, x, y, width, height, rx, ry);
                parseSvgShapeAttributes(attributes, shape);
            }
        }

        protected override void ProcessSvgText(ISvgContainer container, Dictionary<string, string> attributes)
        {
            double x = 0;
            double y = 0;
            double fontSize = 16.0 / 96.0;
            double letterSpacing = 0;
            string textAnchor = "start";
            string fontFamily = "";
            string textValue = "";
            bool positionIsSet = false;

            if (attributes.ContainsKey("x"))
            {
                x = _dom.XLengthFromParameter(attributes["x"]);
                positionIsSet = true;
            }

            if (attributes.ContainsKey("y"))
            {
                y = _dom.YLengthFromParameter(attributes["y"]);
                positionIsSet = true;
            }

            if (attributes.ContainsKey("font-size"))
            {
                fontSize = _dom.LengthFromParameter(attributes["font-size"]);
                    _dom.CurrentFontSize = fontSize;
                }

            if (attributes.ContainsKey("letter-spacing"))
            {
                letterSpacing = _dom.LengthFromParameter(attributes["letter-spacing"]);
            }

            if (attributes.ContainsKey("text-anchor"))
            {
                textAnchor = attributes["text-anchor"];
            }

            if (attributes.ContainsKey("font-family"))
            {
                fontFamily = attributes["font-family"];
            }

            if (attributes.ContainsKey("text-value"))
            {
                textValue = attributes["text-value"];
            }

            switch (fontFamily.ToLower())
            {
                case "":
                case "sans":
                case "sans-serif":
                    fontFamily = "Segoe UI";
                    break;

                case "serif":
                    fontFamily = "Times New Roman";
                    break;

                case "cursive":
                case "fantasy":
                    fontFamily = "Segoe Script";
                    break;

                case "monospace":
                    fontFamily = "Consolas";
                    break;

                default:
                    break;
            }

            SvgTextType shape;

            if (positionIsSet)
            {
                shape = new SvgTextType(container, x, y, fontSize, letterSpacing, textAnchor, fontFamily, textValue);
            }
            else
            {
                shape = new SvgTextType(container, fontSize, letterSpacing, textAnchor, fontFamily, textValue);
            }

            parseSvgShapeAttributes(attributes, shape);
        }

        protected override void ProcessSvgTspan(ISvgContainer container, Dictionary<string, string> attributes)
        {
            ProcessSvgText(container, attributes);
        }

        protected override void ProcessSvgPath(ISvgContainer container, Dictionary<string, string> attributes)
        {
            List<SvgPathData> d = new List<SvgPathData>();
            double pathLength = 0;

            if (attributes.ContainsKey("d"))
            {
                List<List<string>> list = ParsePathList(attributes["d"]);

                foreach (List<string> strings in list)
                {
                    if (strings.Count > 0)
                    {
                        SvgPathData pd = new SvgPathData();
                        int i = 0;
                        pd.Command = strings[i++];

                        switch (pd.Command.ToLower())
                        {
                            case "z":
                                // no data
                                break;

                            case "h":
                                while (i < strings.Count)
                                {
                                    pd.Data.Add(_dom.XLengthFromParameter(strings[i++]));
                                }
                                break;

                            case "v":
                                while (i < strings.Count)
                                {
                                    pd.Data.Add(_dom.YLengthFromParameter(strings[i++]));
                                }
                                break;

                            case "m":
                            case "l":
                            case "t":
                                while ((i + 1) < strings.Count)
                                {
                                    pd.Data.Add(_dom.XLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.YLengthFromParameter(strings[i++]));
                                }
                                break;

                            case "s":
                            case "q":
                                while ((i + 3) < strings.Count)
                                {
                                    pd.Data.Add(_dom.XLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.YLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.XLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.YLengthFromParameter(strings[i++]));
                                }
                                break;

                            case "c":
                                while ((i + 5) < strings.Count)
                                {
                                    pd.Data.Add(_dom.XLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.YLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.XLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.YLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.XLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.YLengthFromParameter(strings[i++]));
                                }
                                break;

                            case "a":
                                while ((i + 6) < strings.Count)
                                {
                                    pd.Data.Add(_dom.XLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.YLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.DoubleFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.DoubleFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.DoubleFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.XLengthFromParameter(strings[i++]));
                                    pd.Data.Add(_dom.YLengthFromParameter(strings[i++]));
                                }
                                break;
                        }
                        d.Add(pd);
                    }
                }
            }

            if (attributes.ContainsKey("pathLength"))
            {
                pathLength = _dom.LengthFromParameter(attributes["pathLength"]);
            }

            SvgPathType shape = new SvgPathType(container, d, pathLength);
            parseSvgShapeAttributes(attributes, shape);
        }

        protected override void ProcessUnknownSvgShape(ISvgContainer container, Dictionary<string, string> attributes)
        {
            //System.Diagnostics.Debug.WriteLine("Unimplemented: UnknownSvgShape {0}", _xmlReader.Name);
        }

        protected override void ProcessUnexpectedSvgElement(Dictionary<string, string> attributes)
        {
            //System.Diagnostics.Debug.WriteLine("Unimplemented: SvgUnexpectedElement {0}", _xmlReader.Name);
        }

        private SvgShapeType Find(string href, ISvgContainer container)
        {
            SvgShapeType shape = null;

            if (container is SvgShapeType && ((SvgShapeType)container).Id == href)
            {
                return (SvgShapeType)container;
            }

            foreach (SvgShapeType entity in container.Entities)
            {
                if (entity.Id == href)
                {
                    shape = entity;
                    break;
                }
                else if (entity is ISvgContainer)
                {
                    if ((shape = Find(href, entity as ISvgContainer)) != null)
                    {
                        break;
                    }
                }
            }

            return shape;
        }
    }
}
