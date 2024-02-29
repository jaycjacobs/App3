using System.Collections.Generic;
using System.Xml.Serialization;
using Cirros.Primitives;
using CirrosCore;
using System.Threading.Tasks;
using Cirros.Utility;
#if UWP
using Windows.Storage;
#else
#endif

namespace Cirros.Drawing
{
    public class SymbolFile
    {
        private List<Layer> _layers = new List<Layer>();
        private List<LineType> _lineTypes = new List<LineType>();
        private List<TextStyle> _textStyles = new List<TextStyle>();
        private List<ArrowStyle> _arrowStyles = new List<ArrowStyle>();
        private List<CrosshatchPattern> _patterns = new List<CrosshatchPattern>();
        private List<ImageRef> _images = new List<ImageRef>();
        private List<Group> _groups = new List<Group>();

        SymbolHeader _header = new SymbolHeader();

        public void AddLayer(Layer layer)
        {
            _layers.Add(layer);
        }

        public void AddLineType(LineType lineType)
        {
            _lineTypes.Add(lineType);
        }

        public void AddTextStyle(TextStyle style)
        {
            _textStyles.Add(style);
        }

        public void AddArrowStyle(ArrowStyle style)
        {
            _arrowStyles.Add(style);
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

#if UWP
        public async Task AddImageRefs(List<string> imageIds)
        {
            foreach (string imageId in imageIds)
            {
                bool dup = false;

                foreach (ImageRef symbolImageRef in _images)
                {
                    if (symbolImageRef.ImageId == imageId)
                    {
                        dup = true;
                        break;
                    }
                }
                if (dup == false)
                {
                    try
                    {
                        StorageFile file = await Utilities.GetImageSourceFileAsync(imageId);

                        if (file != null)
                        {
                            ImageRef ir = new ImageRef();
                            ir.ImageId = imageId;
                            ir.Contents = await Utilities.EncodeImage(file);

                            _images.Add(ir);
                        }
                    }
                    catch
                    {
                        // image is missing - don't crash
                    }
                }
            }
        }
#else
#endif

        public void AddGroup(Group g)
        {
            if (_groups.Contains(g) == false)
            {
                _groups.Add(g);
            }
        }

        public SymbolHeader Header
        {
            get
            {
                return _header;
            }
            set
            {
                _header = value;
            }
        }

        [XmlArrayItem("Layer")]
        public Layer[] Layers
        {
            get
            {
                int index = 0;
                Layer[] array = new Layer[_layers.Count];

                foreach (Layer layer in _layers)
                {
                    array[index++] = layer;
                }

                return array;
            }
            set
            {
                _layers.Clear();

                foreach (Layer layer in value)
                {
                    _layers.Add(layer);
                }
            }
        }

        [XmlArrayItem("LineType")]
        public LineType[] LineTypes
        {
            get
            {
                int index = 0;
                LineType[] array = new LineType[_lineTypes.Count];

                foreach (LineType lineType in _lineTypes)
                {
                    array[index++] = lineType;
                }

                return array;
            }
            set
            {
                _lineTypes.Clear();

                foreach (LineType lineType in value)
                {
                    _lineTypes.Add(lineType);
                }
            }
        }

        [XmlArrayItem("TextStyle")]
        public TextStyle[] TextStyles
        {
            get
            {
                int index = 0;
                TextStyle[] array = new TextStyle[_textStyles.Count];

                foreach (TextStyle style in _textStyles)
                {
                    array[index++] = style;
                }

                return array;
            }
            set
            {
                _textStyles.Clear();

                foreach (TextStyle style in value)
                {
                    _textStyles.Add(style);
                }
            }
        }

        [XmlArrayItem("ArrowStyle")]
        public ArrowStyle[] ArrowStyles
        {
            get
            {
                int index = 0;
                ArrowStyle[] array = new ArrowStyle[_arrowStyles.Count];

                foreach (ArrowStyle style in _arrowStyles)
                {
                    array[index++] = style;
                }

                return array;
            }
            set
            {
                _arrowStyles.Clear();

                foreach (ArrowStyle style in value)
                {
                    _arrowStyles.Add(style);
                }
            }
        }

        [XmlArrayItem("CrosshatchPattern")]
        public CrosshatchPattern[] CrosshatchPatterns
        {
            get
            {
                int index = 0;
                CrosshatchPattern[] array = new CrosshatchPattern[_patterns.Count];

                foreach (CrosshatchPattern pattern in _patterns)
                {
                    array[index++] = pattern;
                }

                return array;
            }
            set
            {
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
    }
}
