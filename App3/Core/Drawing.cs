using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Cirros.Primitives;
using Cirros.Core;
using System.Windows;
using CirrosCore;
#if UWP
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
#else
using System.Windows.Media;
using static CirrosCore.WpfStubs;
#endif

namespace Cirros.Drawing
{
    public class DrawingFile
    {
        //private List<Layer> _layers = new List<Layer>();
        //private List<LineType> _lineTypes = new List<LineType>();
        private List<Entity> _entities = new List<Entity>();
        private List<ImageRef> _images = new List<ImageRef>();
        private List<Group> _groups = new List<Group>();
        private List<CrosshatchPattern> _patterns = new List<CrosshatchPattern>();

        public void AddEntity(Entity e)
        {
            _entities.Add(e);
        }

        public void AddGroup(Group g)
        {
            _groups.Add(g);
        }

        public void AddImageRefs(List<ImageRef> imageRefs)
        {
            foreach (ImageRef imageRef in imageRefs)
            {
                bool dup = false;

                foreach (ImageRef drawingImageRef in _images)
                {
                    if (drawingImageRef.ImageId == imageRef.ImageId)
                    {
                        dup = true;
                        break;
                    }
                }
                if (dup == false)
                {
                    _images.Add(imageRef);
                }
            }
        }

        public void AddPatterns(List<string> patternNames)
        {
            foreach (string key in patternNames)
            {
                if (Patterns.PatternDictionary.ContainsKey(key))
                {
                    CrosshatchPattern pattern = Patterns.PatternDictionary[key];
                    if (_patterns.Contains(pattern) == false)
                    {
                        _patterns.Add(pattern);
                    }
                }
            }
        }

        public DrawingHeader Header
        {
            get
            {
                return Globals.Header;
            }
            set
            {
                Globals.Header = value;
            }
        }

        [XmlArrayItem("Layer")]
        public Layer[] Layers
        {
            get
            {
                int index = 0;
                Layer[] array = new Layer[Globals.LayerTable.Count];

                foreach (Layer layer in Globals.LayerTable.Values)
                {
                    array[index++] = layer;
                }

                //System.Diagnostics.Debug.Assert(index == Globals.LayerTable.Count, "index is not equal to Globals.LayerTable.Count");

                return array;
            }
            set
            {
                Globals.LayerTable.Clear();

                foreach (Layer layer in value)
                {
                    try
                    {
                        Globals.LayerTable.Add(layer.Id, layer);
                    }
                    catch
                    {
                        if (layer.Id == 0)
                        {
                            // If this is a legacy drawing file with no id, set the id to the index
                            Globals.LayerTable.Add(Globals.LayerTable.Count, layer);
                        }
                    }
                }
            }
        }

        [XmlArrayItem("LineType")]
        public LineType[] LineTypes

        {
            get
            {
                int index = 0;
                LineType[] array = new LineType[Globals.LineTypeTable.Count];

                foreach (LineType style in Globals.LineTypeTable.Values)
                {
                    array[index++] = style;
                }

                //System.Diagnostics.Debug.Assert(index == Globals.LineTypeTable.Count, "index is not equal to Globals.LineTypeTable.Count");

                return array;
            }
            set
            {
                Globals.LineTypeTable.Clear();

                foreach (LineType style in value)
                {
                    try
                    {
                        Globals.LineTypeTable.Add(style.Id, style);
                    }
                    catch 
                    {
                        if (style.Id == 0)
                        {
                            // If this is a legacy drawing file with no id, set the id to the index
                            Globals.LineTypeTable.Add(Globals.LineTypeTable.Count, style);
                        }
                    }
                }
            }
        }

        [XmlArrayItem("TextStyle")]
        public TextStyle[] TextStyles
        {
            get
            {
                int index = 0;
                TextStyle[] array = new TextStyle[Globals.TextStyleTable.Count];

                foreach (TextStyle style in Globals.TextStyleTable.Values)
                {
                    array[index++] = style;
                }

                //System.Diagnostics.Debug.Assert(index == Globals.TextStyleTable.Count, "index is not equal to Globals.TextStyleTable.Count");

                return array;
            }
            set
            {
                Globals.TextStyleTable.Clear();

                foreach (TextStyle style in value)
                {
                    try
                    {
                        Globals.TextStyleTable.Add(style.Id, style);
                    }
                    catch 
                    {
                        if (style.Id == 0)
                        {
                            // If this is a legacy drawing file with no id, set the id to the index
                            Globals.TextStyleTable.Add(Globals.TextStyleTable.Count, style);
                        }
                    }
                }
            }
        }

        [XmlArrayItem("ArrowStyle")]
        public ArrowStyle[] ArrowStyles
        {
            get
            {
                List<ArrowStyle> list = new List<ArrowStyle>();
                foreach (ArrowStyle style in Globals.ArrowStyleTable.Values)
                {
                    list.Add(style);
                }
                return list.ToArray();
            }
            set
            {
                Globals.ArrowStyleTable.Clear();

                foreach (ArrowStyle style in value)
                {
                    Globals.ArrowStyleTable.Add(style.Id, style);
                }
            }
        }

        [XmlArrayItem("ColorSpec")]
        public uint[] RecentColors
        {
            get
            {
                return Globals.RecentColors.ToArray();
            }
            set
            {
                Globals.RecentColors.Clear();

                foreach (uint cs in value)
                {
                    Globals.RecentColors.Add(cs);
                }
            }
        }

        [XmlArrayItem("Pattern")]
        public CrosshatchPattern[] CrosshatchPatterns
        {
            get
            {
                return _patterns.ToArray();
            }
            set
            {
                Patterns.ResetPatternList();

                foreach (CrosshatchPattern pattern in value)
                {
                    Patterns.AddPattern(pattern);
                }
            }
        }

        [XmlArrayItem("ImageRef")]
        public ImageRef[] Images
        {
            get
            {
                return _images.ToArray();
            }
            set
            {
                _images.Clear();

                foreach (ImageRef image in value)
                {
                    _images.Add(image);
                }
            }
        }

        [XmlArrayItem("Group")]
        public Group[] Groups
        {
            get
            {
                return _groups.ToArray();
            }
            set
            {
                _groups.Clear();

                foreach (Group g in value)
                {
                    _groups.Add(g);
                }
            }
        }

        [XmlArrayItem("Entity")]
        public Entity[] Entities
        {
            get
            {
                return _entities.ToArray();
            }
            set
            {
                _entities.Clear();

                foreach (Entity e in value)
                {
                    _entities.Add(e);
                }
            }
        }
    }

    public class TextAttributes
    {
        public TextAttributes()
        {
        }

        public TextAlignment TextAlignment;
        public TextPosition TextPosition;
        public int TextStyleId;
        public string Text;
    }

    public class FPoint
    {
        public FPoint(Point p)
        {
            X = (float)Math.Round(p.X, 5);
            Y = (float)Math.Round(p.Y, 5);
        }

        public FPoint(double x, double y)
        {
            X =  (float)Math.Round(x, 5);
            Y = (float)Math.Round(y, 5);
        }

        public FPoint()
        {
        }

        public Point ToPoint()
        {
            return new Point((double)X, (double)Y);
        }

        [XmlAttribute]
        public float X = 0;

        [XmlAttribute]
        public float Y = 0;

        uint _m = 3;

        [XmlAttribute]
        public uint M
        {
            get { return _m; }
            set { _m = value; MSpecified = true; }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool MSpecified = false;
    }

    public class FSize
    {
        public FSize(Size s)
        {
            Width = (float)Math.Round(s.Width, 5);
            Height = (float)Math.Round(s.Height, 5);
        }

        public FSize(double width, double height)
        {
            Width = (float)Math.Round(width, 5);
            Height = (float)Math.Round(height, 5);
        }

        public FSize()
        {
        }

        public Size ToSize()
        {
            Size size = new Size(0, 0);
            if (Width > 0 && Height > 0)
            {
                double dw = Math.Round((double)Width, 5);
                double dh = Math.Round((double)Height, 5);
                size = new Size(dw, dh);
            }
            return size;
        }

        [XmlAttribute]
        public float Width = 0;

        [XmlAttribute]
        public float Height = 0;
    }

    public class Entity
    {
        // Note: Silverlight stores all numeric values internally as single-precision values
        // If double values are serialzed the precision will be incorrect
        PrimitiveType _type;
        float _x;
        float _y;
        int _layerId;
        uint _colorSpec;
        int _lineTypeId;
        int _lineWeightId;
        int _textStyleId;
        int _arrowStyleId;
        int _zIndex;
        uint _flip;
        uint _flags;
        uint _joinStart;
        uint _joinEnd;
        uint _objectId;
        float _dbWidth;
        float _width;
        float _height;
        float _radius;
        float _angle;
        float _startAngle;
        float _includedAngle;
        float _horizSpacing;
        float _verticalSpacing;
        float _opacity;
        float _wallSize;
        bool _showText = true;
        bool _showExtension = true;
        uint _fill = (uint)ColorCode.NoFill;
        bool _fillEvenOdd = false;
        bool _isGroupMember = false;
        FPoint _p1 = new FPoint();
        FPoint _p2 = new FPoint();
        FPoint _refP1 = new FPoint();
        FPoint _refP2 = new FPoint();
        FSize _refSize = new FSize();
        Matrix _matrix = Matrix.Identity;
        string _font;
        string _name;
        string _imageId;
        string _fillPattern = null;
        float _fillScale = 1;
        float _fillAngle = 0;

        List<FPoint> _points;
        TextAttributes _textAttributes;
        PDimension.DimType _dimType;
        ArrowLocation _arrowLocation = ArrowLocation.Both;
        DbEndStyle _dbEndStyle;

        [XmlAttribute]
        public PrimitiveType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        [System.Xml.Serialization.XmlIgnore]
        public bool IsGroupMember
        {
            get
            {
                return _isGroupMember;
            }
            set
            {
                _isGroupMember = value;
            }
        }

        [XmlAttribute]
        public float X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        [XmlAttribute]
        public float Y
        {
            get { return _y; }
            set { _y = value; }
        }

        [XmlAttribute]
        public int LayerId
        {
            get { return _layerId; }
            set { _layerId = value; }
        }

        [XmlAttribute]
        public uint ColorSpec
        {
            get { return _colorSpec; }
            set { _colorSpec = value; }
        }

        [XmlAttribute]
        public int LineTypeId
        {
            get { return _lineTypeId; }
            set { _lineTypeId = value; LineTypeSpecified = true; }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool LineTypeSpecified = false;

        [XmlAttribute]
        public int LineWeightId
        {
            get { return _lineWeightId; }
            set { _lineWeightId = value; LineWeightSpecified = true; }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool LineWeightSpecified = false;

        [XmlAttribute]
        public string Name
        {
            get { return _name; }
            set { _name = value; NameSpecified = true; }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool NameSpecified = false;

        public Matrix Matrix
        {
            get
            {
                return _matrix;
            }
            set
            {
                _matrix = value;
                MatrixSpecified = _matrix.IsIdentity == false;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool MatrixSpecified = false;

        public float Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = value;
                WidthSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool WidthSpecified = false;

        public float DBWidth
        {
            get
            {
                return _dbWidth;
            }
            set
            {
                _dbWidth = value;
                DBWidthSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool DBWidthSpecified = false;

        public float Height
        {
            get
            {
                return _height;
            }
            set
            {
                _height = value;
                HeightSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool HeightSpecified = false;

        public float Radius
        {
            get
            {
                return _radius < 0 ? -_radius : _radius;
            }
            set
            {
                _radius = value;
                RadiusSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool RadiusSpecified = false;

        public float Angle
        {
            get
            {
                return _angle;
            }
            set
            {
                _angle = value;
                AngleSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool AngleSpecified = false;

        public float IncludedAngle
        {
            get
            {
                return _includedAngle;
            }
            set
            {
                _includedAngle = value;
                IncludedAngleSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool IncludedAngleSpecified = false;

        public float StartAngle
        {
            get
            {
                return _startAngle;
            }
            set
            {
                _startAngle = value;
                StartAngleSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool StartAngleSpecified = false;

        public float HorizontalSpacing
        {
            get
            {
                return _horizSpacing;
            }
            set
            {
                _horizSpacing = value;
                HorizontalSpacingSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool HorizontalSpacingSpecified = false;

        public float VerticalSpacing
        {
            get
            {
                return _verticalSpacing;
            }
            set
            {
                _verticalSpacing = value;
                VerticalSpacingSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool VerticalSpacingSpecified = false;

        public string Font
        {
            get
            {
                return _font;
            }
            set
            {
                _font = value;
                FontSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool FontSpecified = false;

        public float Opacity
        {
            get
            {
                return _opacity;
            }
            set
            {
                _opacity = value;
                OpacitySpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool OpacitySpecified = false;

        public float WallSize
        {
            get
            {
                return _wallSize;
            }
            set
            {
                _wallSize = value;
                WallSizeSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool WallSizeSpecified = false;

        public string ImageId
        {
            get
            {
                return _imageId;
            }
            set
            {
                _imageId = value;
                ImageIdSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool ImageIdSpecified = false;

        public FPoint P1
        {
            get
            {
                return _p1;
            }
            set
            {
                _p1 = value;
                P1Specified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool P1Specified = false;

        public FPoint P2
        {
            get
            {
                return _p2;
            }
            set
            {
                _p2 = value;
                P2Specified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool P2Specified = false;
        public FPoint RefP1
        {
            get
            {
                return _refP1;
            }
            set
            {
                _refP1 = value;
                RefP1Specified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool RefP1Specified = false;

        public FPoint RefP2
        {
            get
            {
                return _refP2;
            }
            set
            {
                _refP2 = value;
                RefP2Specified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool RefP2Specified = false;

        public FSize RefSize
        {
            get
            {
                return _refSize;
            }
            set
            {
                _refSize = value;
                RefSizeSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool RefSizeSpecified = false;

        public int TextStyleId
        {
            get
            {
                return _textStyleId;
            }
            set
            {
                _textStyleId = value;
                TextStyleIdSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool TextStyleIdSpecified = false;

        public int ArrowStyleId
        {
            get
            {
                return _arrowStyleId;
            }
            set
            {
                _arrowStyleId = value;
                ArrowStyleIdSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool ArrowStyleIdSpecified = false;

        public uint ObjectId
        {
            get
            {
                return _objectId;
            }
            set
            {
                _objectId = value;
                ObjectIdSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool ObjectIdSpecified = false;

        public int ZIndex
        {
            get
            {
                return _zIndex;
            }
            set
            {
                _zIndex = value;
                ZIndexSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool ZIndexSpecified = false;

        public uint Flip
        {
            get
            {
                return _flip;
            }
            set
            {
                _flip = value;
                FlipSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool FlipSpecified = false;

        public uint Flags
        {
            get
            {
                return _flags;
            }
            set
            {
                _flags = value;
                FlagsSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool FlagsSpecified = false;

        public uint JoinStart
        {
            get
            {
                return _joinStart;
            }
            set
            {
                _joinStart = value;
                JoinStartSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool JoinStartSpecified = false;

        public uint JoinEnd
        {
            get
            {
                return _joinEnd;
            }
            set
            {
                _joinEnd = value;
                JoinEndSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool JoinEndSpecified = false;

        public bool FillEvenOdd
        {
            get
            {
                return _fillEvenOdd;
            }
            set
            {
                _fillEvenOdd = value;
                FillEvenOddSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool FillEvenOddSpecified = false;

        public string FillPattern
        {
            get
            {
                return _fillPattern;
            }
            set
            {
                _fillPattern = value;
                FillPatternSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool FillPatternSpecified = false;

        public float FillScale
        {
            get
            {
                return _fillScale;
            }
            set
            {
                _fillScale = value;
                FillScaleSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool FillScaleSpecified = false;

        public float FillAngle
        {
            get
            {
                return _fillAngle;
            }
            set
            {
                _fillAngle = value;
                FillAngleSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool FillAngleSpecified = false;

        public bool ShowText
        {
            get
            {
                return _showText;
            }
            set
            {
                _showText = value;
                ShowTextSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool ShowTextSpecified = false;

        public bool ShowExtension
        {
            get
            {
                return _showExtension;
            }
            set
            {
                _showExtension = value;
                ShowExtensionSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool ShowExtensionSpecified = false;

        public DbEndStyle DoublelineEndStyle
        {
            get
            {
                return _dbEndStyle;
            }
            set
            {
                _dbEndStyle = value;
                DoublelineEndStyleSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool DoublelineEndStyleSpecified = false;

        public uint Fill
        {
            get
            {
                return FillSpecified ? _fill : (uint)ColorCode.NoFill;
            }
            set
            {
                _fill = value;
                FillSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool FillSpecified = false;

        public ArrowLocation ArrowLocation
        {
            get
            {
                return _arrowLocation;
            }
            set
            {
                _arrowLocation = value;
                ArrowLocationSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool ArrowLocationSpecified = false;

        public PDimension.DimType DimensionType
        {
            get
            {
                return _dimType;
            }
            set
            {
                _dimType = value;
                DimensionTypeSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool DimensionTypeSpecified = false;

        public TextAttributes TextAttributes
        {
            get
            {
                return _textAttributes;
            }
            set
            {
                _textAttributes = value;
                TextAttributesSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool TextAttributesSpecified = false;

        [XmlArrayItem("Point")]
        public FPoint[] Points
        {
            get
            {
                return _points.ToArray();
            }
            set
            {
                _points = new List<FPoint>();

                foreach (FPoint item in value)
                {
                    _points.Add(item);
                }
                PointsSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool PointsSpecified = false;

        public void AddPoint(FPoint p)
        {
            if (_points == null)
            {
                _points = new List<FPoint>();
                PointsSpecified = true;
            }
            _points.Add(p);
        }

        List<GroupAttribute> _attributes;

        public List<GroupAttribute> Attributes
        {
            get
            {
                return _attributes;
            }
            set
            {
                _attributes = value;
                AttributesSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool AttributesSpecified = false;

        public void SetTextAttributes(TextAlignment alignment, TextPosition position, int textstyleid, string text)
        {
            _textAttributes = new TextAttributes();
            _textAttributes.TextStyleId = textstyleid;
            _textAttributes.TextAlignment = alignment;
            _textAttributes.TextPosition = position;
            _textAttributes.Text = text;

            TextAttributesSpecified = true;
        }
    }

    public class ImageRef
    {
        private string _name;
        private string _imageId;
        private string _contents;

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public string ImageId
        {
            get
            {
                return _imageId;
            }
            set
            {
                _imageId = value;
            }
        }

        public string Contents
        {
            get
            {
                return _contents;
            }
            set
            {
                _contents = value;
            }
        }
    }
}
