using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Svg
{
    public interface ISvgContainer
    {
        void Add(SvgShapeType shape);
        void ResetEntities();
        List<SvgShapeType> Entities { get; }
    }

    public class SvgDom : ISvgContainer
    {
        double _width;
        double _height;

        double _documentXScale = 1;
        double _documentYScale = 1;
        double _documentXOffset = 0;
        double _documentYOffset = 0;

        string _documentUnit = "px";

        double _currentFontSize = 16.0 / 96.0;

        Point _viewBoxOrigin = new Point();
        Size _viewBoxSize = new Size();

        private Dictionary<string, string> _imageDictionary = new Dictionary<string, string>();
        private List<SvgShapeType> _entities = new List<SvgShapeType>();

        public SvgDom()
        {
        }

        public List<SvgShapeType> Entities
        {
            get { return _entities; }
        }

        public Dictionary<string, string> ImageDictionary
        {
            get { return _imageDictionary; }
        }

        public void Add(SvgShapeType shape)
        {
            _entities.Add(shape);
        }

        public Size Size
        {
            get
            {
                return new Size(_width, _height);
            }
        }

        public double CurrentFontSize
        {
            get
            {
                return _currentFontSize * _documentXScale;
            }
            set
            {
                _currentFontSize = value / _documentXScale;
            }
        }

        public string Unit
        {
            get
            {
                return _documentUnit;
            }
        }

        public double XScale
        {
            get
            {
                return _documentXScale;
            }
        }

        public double YScale
        {
            get
            {
                return _documentYScale;
            }
        }

        public double XOffset
        {
            get
            {
                return _documentXOffset;
            }
        }

        public double YOffset
        {
            get
            {
                return _documentYOffset;
            }
        }

        public string GetImageName(string imageUri)
        {
            string name = "";

            if (imageUri.StartsWith("http://"))
            {
                string[] sa = imageUri.Split(new char[] { '/' });
                if (sa.Length > 1)
                {
                    name = sa[sa.Length - 1];
                }
            }

            return name;
        }

        public string GetImageId(string imageUri)
        {
            string imageId = null;

            if (_imageDictionary.Count > 0 && imageUri.StartsWith("http://"))
            {
                if (_imageDictionary.ContainsValue(imageUri))
                {
                    foreach (string key in _imageDictionary.Keys)
                    {
                        if (_imageDictionary[key] == imageUri)
                        {
                            imageId = key;
                            break;
                        }
                    }
                }
            }

            if (imageId == null)
            {
                imageId = Guid.NewGuid().ToString();

                _imageDictionary.Add(imageId, imageUri);
            }

            return imageId;
        }

        public void SetViewPort(double width, double height, Point viewBoxOrigin, Size viewBoxSize, string widthUnit, string heightUnit, string preserveAspectRatio)
        {
            string aspectRegex = @"((?<defer>(defer))\s+)?(?<aspect>(none|(?<xaction>(xMin|xMid|xMax))(?<yaction>(YMin|YMid|YMax))))\s+(?<meet>(meet|slice))";

            _width = width;                     // document width in inches
            _height = height;                   // document height in inches

            _viewBoxOrigin = viewBoxOrigin;     // view origin in pixels
            _viewBoxSize = viewBoxSize;         // view size in pixels

            _documentXScale = _width / _viewBoxSize.Width;
            _documentYScale = _height / _viewBoxSize.Height;

            // defer and meet are not implemented - commented out to removed 'unused' compiler warnings
            //bool defer = false;
            //bool meet = true;
            bool isotropic = true;
            string xaction = "xMid";
            string yaction = "YMid";

            _documentXOffset = -_viewBoxOrigin.X * _documentXScale;
            _documentYOffset = -_viewBoxOrigin.Y * _documentYScale;

            if (preserveAspectRatio.Length > 0)
            {
                MatchCollection mc = Regex.Matches(preserveAspectRatio, aspectRegex);

                if (mc.Count == 1)
                {
                    if (mc[0].Groups["defer"].Success)
                    {
                        //defer = true;
                    }
                    if (mc[0].Groups["aspect"].Success && mc[0].Groups["aspect"].Value == "none")
                    {
                        isotropic = false;
                    }
                    else
                    {
                        if (mc[0].Groups["xaction"].Success)
                        {
                            xaction = mc[0].Groups["xaction"].Value;
                        }
                        if (mc[0].Groups["yaction"].Success)
                        {
                            yaction = mc[0].Groups["yaction"].Value;
                        }
                        if (mc[0].Groups["meet"].Success && mc[0].Groups["meet"].Value == "slice")
                        {
                            //meet = false;
                        }
                    }
                }
            }

            if (isotropic)
            {
                double xslop = 0;
                double yslop = 0;

                if (_documentXScale > _documentYScale)
                {
                    _documentXScale = _documentYScale;
                    xslop = _width - _viewBoxSize.Width * _documentXScale;
                }
                else if (_documentXScale < _documentYScale)
                {
                    _documentYScale = _documentXScale;
                    yslop = _height - _viewBoxSize.Height * _documentYScale;
                }

                if (xslop != 0)
                {
                    switch (xaction)
                    {
                        default:
                        case "xMin":
                            _documentXOffset = -_viewBoxOrigin.X * _documentXScale;
                            break;

                        case "xMid":
                            _documentXOffset = -_viewBoxOrigin.X * _documentXScale + xslop / 2;
                            break;

                        case "xMax":
                            _documentXOffset = -_viewBoxOrigin.X * _documentXScale + xslop;
                            break;
                    }
                }

                if (yslop != 0)
                {
                    switch (yaction)
                    {
                        default:
                        case "YMin":
                            _documentYOffset = -_viewBoxOrigin.Y * _documentYScale;
                            break;

                        case "YMid":
                            _documentYOffset = -_viewBoxOrigin.Y * _documentYScale + yslop / 2;
                            break;

                        case "YMax":
                            _documentYOffset = -_viewBoxOrigin.Y * _documentYScale + yslop;
                            break;
                    }
                }
            }

            if (widthUnit == "mm" || widthUnit == "cm")
            {
                _documentUnit = widthUnit;
            }
            else
            {
                _documentUnit = "in";
            }
        }

        public double DoubleFromParameter(string parameter)
        {
            double d = 0;

            try
            {
                d = double.Parse(parameter);
            }
            catch
            {
                //System.Diagnostics.Debug.WriteLine("Invalid double parameter: {0}", parameter);
            }

            return d;
        }

        public double ApplyXScale(double length, string unit)
        {
            double scaledLength = length;

            switch (unit)
            {
                case "in":
                    scaledLength *= 96;
                    break;

                case "em":
                    scaledLength *= _currentFontSize;
                    break;

                case "ex":
                    scaledLength *= _currentFontSize / 2;
                    break;

                default:
                case "px":
                    break;

                case "cm":
                    scaledLength *= 96 / 2.54;
                    break;

                case "mm":
                    scaledLength *= 96 / 25.4;
                    break;

                case "pt":
                    scaledLength *= 96 / 72;
                    break;

                case "pc":
                    scaledLength *= 96 / 6;
                    break;

                case "%":
                    scaledLength = (length / 100) * _viewBoxSize.Width;
                    break;
            }

            return scaledLength * _documentXScale;
        }

        public double ApplyYScale(double length, string unit)
        {
            double scaledLength = length;

            switch (unit)
            {
                case "in":
                    scaledLength *= 96;
                    break;

                case "em":
                    scaledLength *= _currentFontSize;
                    break;

                case "ex":
                    scaledLength *= _currentFontSize / 2;
                    break;

                default:
                case "px":
                    break;

                case "cm":
                    scaledLength *= 96 / 2.54;
                    break;

                case "mm":
                    scaledLength *= 96 / 25.4;
                    break;

                case "pt":
                    scaledLength *= 96 / 72;
                    break;

                case "pc":
                    scaledLength *= 96 / 6;
                    break;

                case "%":
                    scaledLength = (length / 100) * _viewBoxSize.Height;
                    break;
            }

            return scaledLength * _documentYScale;
        }

        //const string lengthRegEx = @"(?<number>(-?\d+\.?\d*)|(-?\.?\d+))(?<unit>(em|ex|px|in|cm|mm|pt|pc|%)?)";
        const string lengthRegEx = @"(?<number>(-?\d+\.?\d*([Ee][+-]\d+)?)|(-?\.?\d+([Ee][+-]\d+)?))(?<unit>(em|ex|px|in|cm|mm|pt|pc|%)?)";

        public double XLengthFromParameter(string parameter)
        {
            string number = "";
            string unit = "";
            double d = 0;

            MatchCollection mc = Regex.Matches(parameter.Trim(), lengthRegEx);

            if (mc.Count == 1)
            {
                if (mc[0].Groups["number"].Success)
                {
                    number = mc[0].Groups["number"].Value;
                }
                if (mc[0].Groups["unit"].Success)
                {
                    unit = mc[0].Groups["unit"].Value;
                }
            }

            try
            {
                d = ApplyXScale(double.Parse(number), unit);
            }
            catch
            {
                //System.Diagnostics.Debug.WriteLine("Invalid length parameter: {0}", parameter);
            }

            return d;
        }

        public double YLengthFromParameter(string parameter)
        {
            string number = "";
            string unit = "";
            double d = 0;

            MatchCollection mc = Regex.Matches(parameter.Trim(), lengthRegEx);

            if (mc.Count == 1)
            {
                if (mc[0].Groups["number"].Success)
                {
                    number = mc[0].Groups["number"].Value;
                }
                if (mc[0].Groups["unit"].Success)
                {
                    unit = mc[0].Groups["unit"].Value;
                }
            }

            try
            {
                d = ApplyYScale(double.Parse(number), unit);
            }
            catch
            {
                //System.Diagnostics.Debug.WriteLine("Invalid length parameter: {0}", parameter);
            }

            return d;
        }

        public void LengthFromParameter(string parameter, out double length, out string unit)
        {
            string str = "";
            length = 0;
            unit = "";

            MatchCollection mc = Regex.Matches(parameter.Trim(), lengthRegEx);

            if (mc.Count == 1)
            {
                if (mc[0].Groups["number"].Success)
                {
                    str = mc[0].Groups["number"].Value;
                }
                if (mc[0].Groups["unit"].Success)
                {
                    unit = mc[0].Groups["unit"].Value;
                }
            }

            double scaledLength = 0;

            try
            {
                length = double.Parse(str);
                scaledLength = ApplyXScale(length, unit);
            }
            catch
            {
                //System.Diagnostics.Debug.WriteLine("Invalid length parameter: {0}", parameter);
            }
        }

        public double LengthFromParameter(string parameter)
        {
            string str = "";
            string unit = "";

            MatchCollection mc = Regex.Matches(parameter.Trim(), lengthRegEx);

            if (mc.Count == 1)
            {
                if (mc[0].Groups["number"].Success)
                {
                    str = mc[0].Groups["number"].Value;
                }
                if (mc[0].Groups["unit"].Success)
                {
                    unit = mc[0].Groups["unit"].Value;
                }
            }

            double scaledLength = 0;

            try
            {
                double length = double.Parse(str);
                scaledLength = ApplyXScale(length, unit);
            }
            catch
            {
                //System.Diagnostics.Debug.WriteLine("Invalid length parameter: {0}", parameter);
            }

            return scaledLength;
        }

        public double SizeFromParameter(string parameter, out string unit)
        {
            string str = "";
            unit = "";

            MatchCollection mc = Regex.Matches(parameter.Trim(), lengthRegEx);

            if (mc.Count == 1)
            {
                if (mc[0].Groups["number"].Success)
                {
                    str = mc[0].Groups["number"].Value;
                }
                if (mc[0].Groups["unit"].Success)
                {
                    unit = mc[0].Groups["unit"].Value;
                }
            }

            double scaledLength = 0;

            try
            {
                double length = double.Parse(str);
                scaledLength = ApplyXScale(length, unit);
            }
            catch
            {
                //System.Diagnostics.Debug.WriteLine("Invalid length parameter: {0}", parameter);
            }

            return scaledLength;
        }

        public void ResetEntities()
        {
            _entities = new List<SvgShapeType>();
        }
    }

    public class SvgShapeType
    {
        ISvgContainer _container;

        public string Id = "";
        public string Class = "";

        public double StrokeWidth = 1 / 96;
        public Color StrokeColor = Colors.Black;
        public Color FillColor = Colors.Black;
        public bool Fill = true;
        public bool Stroke = false;
        public double Opacity = 1;
        public double ShapeOpacity = 1;
        public double FillOpacity = 1;
        public string FontFamily;
        public double FontSize = 16 / 96;
        public double LetterSpacing = 0;
        public string TextAnchor = "start";
        public bool Visible = true;
        public bool FillEvenOdd = false;

        public string dbHint = "";

        private Transform _transform = null;

        public Transform Transform
        {
            get
            {
                return _transform;
            }
            set
            {
                if (value == null)
                {
                    _transform = null;
                }
                else if (_transform is TransformGroup && value is TransformGroup)
                {
                    TransformGroup currentTransform = _transform as TransformGroup;
                    _transform = value;

                    foreach (Transform t in currentTransform.Children)
                    {
                        ((TransformGroup)_transform).Children.Insert(0, t);
                    }
                }
                else if (_transform == null)
                {
                    _transform = value;
                }
                else if (_transform is TransformGroup)
                {
                    ((TransformGroup)_transform).Children.Insert(0, value);
                }
                else if (value is TransformGroup)
                {
                    Transform currentTransform = _transform;
                    _transform = value;
                    ((TransformGroup)_transform).Children.Insert(0, currentTransform);
                    //((TransformGroup)_transform).Children.Add(currentTransform);
                }
                else
                {
                    Transform currentTransform = _transform;

                    _transform = new TransformGroup();
                    ((TransformGroup)_transform).Children.Add(value);
                    ((TransformGroup)_transform).Children.Add(currentTransform);
                }
            }
        }

        public SvgShapeType(ISvgContainer container)
        {
            _container = container;
            _container.Add(this);
        }

        public SvgShapeType DeepCopy()
        {
            SvgShapeType other = (SvgShapeType)this.MemberwiseClone();

            // copy referenced objects (is it necessary to copy strings?)
            if (this.Transform != null)
            {
                Transform t = SvgDomReader.DeepCopyTransform(this.Transform);
                other.Transform = null;     // default behavior on assignment is to apply new transform to existing transform
                other.Transform = t;
            }

            if (this is ISvgContainer)
            {
                ((ISvgContainer)other).ResetEntities();

                foreach (SvgShapeType entity in ((ISvgContainer)this).Entities)
                {
                    // deep copy child entity
                    SvgShapeType child = entity.DeepCopy();
                    ((ISvgContainer)other).Add(child);
                }
            }

            return other;
        }
    }

    public class SvgGroupType : SvgShapeType, ISvgContainer
    {
        private List<SvgShapeType> _entities = new List<SvgShapeType>();

        public List<SvgShapeType> Entities
        {
            get
            {
                return _entities;
            }
        }

        public void Add(SvgShapeType shape)
        {
            _entities.Add(shape);
        }

        public SvgGroupType(ISvgContainer container)
            : base(container)
        {
        }


        public void ResetEntities()
        {
            _entities = new List<SvgShapeType>();
        }
    }

    public class SvgSymbolType : SvgShapeType, ISvgContainer
    {
        private List<SvgShapeType> _entities = new List<SvgShapeType>();

        public List<SvgShapeType> Entities
        {
            get
            {
                return _entities;
            }
        }

        public void Add(SvgShapeType shape)
        {
            _entities.Add(shape);
        }

        public SvgSymbolType(ISvgContainer container)
            : base(container)
        {
        }


        public void ResetEntities()
        {
            _entities = new List<SvgShapeType>();
        }
    }

    public class SvgMarkerType : SvgShapeType, ISvgContainer
    {
        private List<SvgShapeType> _entities = new List<SvgShapeType>();

        public List<SvgShapeType> Entities
        {
            get
            {
                return _entities;
            }
        }

        public void Add(SvgShapeType shape)
        {
            _entities.Add(shape);
        }

        public SvgMarkerType(ISvgContainer container)
            : base(container)
        {
        }


        public void ResetEntities()
        {
            _entities = new List<SvgShapeType>();
        }
    }

    public class SvgDefsType : SvgShapeType, ISvgContainer
    {
        private List<SvgShapeType> _entities = new List<SvgShapeType>();

        public List<SvgShapeType> Entities
        {
            get
            {
                return _entities;
            }
        }

        public void Add(SvgShapeType shape)
        {
            _entities.Add(shape);
        }

        public SvgDefsType(ISvgContainer container)
            : base(container)
        {
        }

        public void ResetEntities()
        {
            _entities = new List<SvgShapeType>();
        }
    }

    public class SvgLineType : SvgShapeType
    {
        public double X1;
        public double Y1;
        public double X2;
        public double Y2;

        public SvgLineType(ISvgContainer container, double x1, double y1, double x2, double y2)
            : base(container)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
    }

    public class SvgPolylineType : SvgShapeType
    {
        public List<Point> Points;

        public SvgPolylineType(ISvgContainer container, List<Point> points)
            : base(container)
        {
            Points = points;
        }
    }

    public class SvgPolygonType : SvgShapeType
    {
        public List<Point> Points;

        public SvgPolygonType(ISvgContainer container, List<Point> points)
            : base(container)
        {
            Points = points;
        }
    }

    public class SvgRectType : SvgShapeType
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
        public double Rx;
        public double Ry;

        public SvgRectType(ISvgContainer container, double x, double y, double width, double height, double rx, double ry)
            : base(container)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Rx = rx;
            Ry = ry;
        }
    }

    public class SvgImageType : SvgShapeType
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
        public string ImageId;
        public string Name;

        public SvgImageType(ISvgContainer container, double x, double y, double width, double height, double opacity, string imageId, string name, string preserveAspectRatio)
            : base(container)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Opacity = opacity;
            ImageId = imageId;
            Name = name;

            if (preserveAspectRatio.Length > 0)
            {
                string aspectRegex = @"((?<defer>(defer))\s+)?(?<aspect>(none|(?<xaction>(xMin|xMid|xMax))(?<yaction>(YMin|YMid|YMax))))\s+(?<meet>(meet|slice))";
                //bool isotropic = true;
                string xaction = "xMid";
                string yaction = "YMid";

                MatchCollection mc = Regex.Matches(preserveAspectRatio, aspectRegex);

                if (mc.Count == 1)
                {
                    if (mc[0].Groups["defer"].Success)
                    {
                        //defer = true;
                    }
                    if (mc[0].Groups["aspect"].Success && mc[0].Groups["aspect"].Value == "none")
                    {
                        //isotropic = false;
                    }
                    else
                    {
                        if (mc[0].Groups["xaction"].Success)
                        {
                            xaction = mc[0].Groups["xaction"].Value;
                        }
                        if (mc[0].Groups["yaction"].Success)
                        {
                            yaction = mc[0].Groups["yaction"].Value;
                        }
                        if (mc[0].Groups["meet"].Success && mc[0].Groups["meet"].Value == "slice")
                        {
                            //meet = false;
                        }
                    }
                }
            }
        }
    }

    public class SvgCircleType : SvgShapeType
    {
        public double Cx;
        public double Cy;
        public double R;

        public SvgCircleType(ISvgContainer container, double cx, double cy, double r)
            : base(container)
        {
            Cx = cx;
            Cy = cy;
            R = r;
        }
    }

    public class SvgEllipseType : SvgShapeType
    {
        public double Cx;
        public double Cy;
        public double Rx;
        public double Ry;

        public SvgEllipseType(ISvgContainer container, double cx, double cy, double rx, double ry)
            : base(container)
        {
            Cx = cx;
            Cy = cy;
            Rx = rx;
            Ry = ry;
        }
    }

    public class SvgPathData
    {
        public string Command;
        public List<double> Data = new List<double>();
    }

    public class SvgPathType : SvgShapeType
    {
        public List<SvgPathData> D;
        public double PathLength;

        public SvgPathType(ISvgContainer container, List<SvgPathData> d, double pathLength)
            : base(container)
        {
            D = d;
            PathLength = pathLength;
        }
    }

    public class SvgTextType : SvgShapeType
    {
        public double X;
        public double Y;
        public string Text;
        public bool PositionIsSet = false;

        public SvgTextType(ISvgContainer container, double fontSize, double letterSpacing, string textAnchor, string fontFamily, string text)
            : base(container)
        {
            FontSize = fontSize;
            LetterSpacing = letterSpacing;
            TextAnchor = textAnchor;
            FontFamily = fontFamily;
            Text = text;
        }

        public SvgTextType(ISvgContainer container, double x, double y, double fontSize, double letterSpacing, string textAnchor, string fontFamily, string text)
            : base(container)
        {
            X = x;
            Y = y;
            FontSize = fontSize;
            LetterSpacing = letterSpacing;
            TextAnchor = textAnchor;
            FontFamily = fontFamily;
            Text = text;
            PositionIsSet = true;
        }
    }
}
