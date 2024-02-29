using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;
using Windows.UI;

namespace Cirros.Svg
{
    public abstract class SvgReader
    {
        // Attribute priorities (lowest to highest)
        //
        //  1. group attribute (on g element)
        //  2. resentation attribute (on shape element)
        //  3. css type attribute (in style element)
        //  4. css class attribute (in style element)
        //  5. css inline style attribute (on shape element)
        //

        protected XmlReaderSettings _xmlReaderSettings;
        protected XmlReader _xmlReader;

        Dictionary<string, SvgDefsType> _defs = new Dictionary<string, SvgDefsType>();
 
        Dictionary<string, string> _classRules = new Dictionary<string, string>();
        Dictionary<string, string> _shapeRules = new Dictionary<string, string>();

        public SvgReader(XmlReaderSettings xrs)
        {
            _xmlReaderSettings = xrs;
        }

        public string[] ParseAttributeList(string attributeValue)
        {
            string s = SvgString(attributeValue, false);
            return s.Split(new char[] { ' ', ',' });
        }

        public List<List<string>> ParsePathList(string attributeValue)
        {
            List<List<string>> list = new List<List<string>>();

            const string setExpr = @"(?<set>(?<command>[zZhHvVmMlLtTsSqQcCaA])\s*(((-?\d+\.?\d*)|(-?\.?\d+))*([Ee][+-]\d+)?\s*,?\s*)+)";
            const string cmdExpr = @"((?<number>((-?\d+\.?\d*([Ee][+-]\d+)?)|(-?\.?\d+([Ee][+-]\d+)?))+\s*,?\s*))";

            MatchCollection smc = Regex.Matches(attributeValue, setExpr);

            foreach (Match m in smc)
            {
                if (m.Groups["set"].Success)
                {
                    string set = m.Groups["set"].Value;

                    if (m.Groups["command"].Success)
                    {
                        List<string> strings = new List<string>();

                        strings.Add(m.Groups["command"].Value);

                        MatchCollection cmc = Regex.Matches(set, cmdExpr);

                        foreach (Match cm in cmc)
                        {
                            if (cm.Groups["number"].Success)
                            {
                                strings.Add(cm.Groups["number"].Value);
                            }
                        }

                        list.Add(strings);
                    }
                }
            }

            return list;
        }

        public Dictionary<string, string> ParseStyleList(string attributeValue)
        {
            Dictionary<string, string> list = new Dictionary<string, string>();

            string [] styles = attributeValue.Split(new char [] {';'});

            foreach (string style in styles)
            {
                string[] kv = style.Split(new char[] { ':' });

                if (kv.Length == 2)
                {
                    SetValueForKey(list, kv[0].Trim(), kv[1]);
                }
            }

            return list;
        }

        internal byte ParseColorComponent(string s)
        {
            byte c = 0;

            if (s.EndsWith("%"))
            {
                double pc = double.Parse(s.Replace("%", ""));
                c = (byte)(pc * 2.55);
            }
            else
            {
                c = byte.Parse(s);
            }

            return c;
        }

        private Dictionary<string, string> _svgColorName = null;

        public Dictionary<string, string> SvgColorName
        {
            get
            {
                if (_svgColorName == null)
                {
                    _svgColorName = new Dictionary<string, string>();

                    _svgColorName.Add("aliceblue", "rgb(240, 248, 255)");
                    _svgColorName.Add("antiquewhite", "rgb(250, 235, 215)");
                    _svgColorName.Add("aqua", "rgb( 0, 255, 255)");
                    _svgColorName.Add("aquamarine", "rgb(127, 255, 212)");
                    _svgColorName.Add("azure", "rgb(240, 255, 255)");
                    _svgColorName.Add("beige", "rgb(245, 245, 220)");
                    _svgColorName.Add("bisque", "rgb(255, 228, 196)");
                    _svgColorName.Add("black", "rgb( 0, 0, 0)");
                    _svgColorName.Add("blanchedalmond", "rgb(255, 235, 205)");
                    _svgColorName.Add("blue", "rgb( 0, 0, 255)");
                    _svgColorName.Add("blueviolet", "rgb(138, 43, 226)");
                    _svgColorName.Add("brown", "rgb(165, 42, 42)");
                    _svgColorName.Add("burlywood", "rgb(222, 184, 135)");
                    _svgColorName.Add("cadetblue", "rgb( 95, 158, 160)");
                    _svgColorName.Add("chartreuse", "rgb(127, 255, 0)");
                    _svgColorName.Add("chocolate", "rgb(210, 105, 30)");
                    _svgColorName.Add("coral", "rgb(255, 127, 80)");
                    _svgColorName.Add("cornflowerblue", "rgb(100, 149, 237)");
                    _svgColorName.Add("cornsilk", "rgb(255, 248, 220)");
                    _svgColorName.Add("crimson", "rgb(220, 20, 60)");
                    _svgColorName.Add("cyan", "rgb( 0, 255, 255)");
                    _svgColorName.Add("darkblue", "rgb( 0, 0, 139)");
                    _svgColorName.Add("darkcyan", "rgb( 0, 139, 139)");
                    _svgColorName.Add("darkgoldenrod", "rgb(184, 134, 11)");
                    _svgColorName.Add("darkgray", "rgb(169, 169, 169)");
                    _svgColorName.Add("darkgreen", "rgb( 0, 100, 0)");
                    _svgColorName.Add("darkgrey", "rgb(169, 169, 169)");
                    _svgColorName.Add("darkkhaki", "rgb(189, 183, 107)");
                    _svgColorName.Add("darkmagenta", "rgb(139, 0, 139)");
                    _svgColorName.Add("darkolivegreen", "rgb( 85, 107, 47)");
                    _svgColorName.Add("darkorange", "rgb(255, 140, 0)");
                    _svgColorName.Add("darkorchid", "rgb(153, 50, 204)");
                    _svgColorName.Add("darkred", "rgb(139, 0, 0)");
                    _svgColorName.Add("darksalmon", "rgb(233, 150, 122)");
                    _svgColorName.Add("darkseagreen", "rgb(143, 188, 143)");
                    _svgColorName.Add("darkslateblue", "rgb( 72, 61, 139)");
                    _svgColorName.Add("darkslategray", "rgb( 47, 79, 79)");
                    _svgColorName.Add("darkslategrey", "rgb( 47, 79, 79)");
                    _svgColorName.Add("darkturquoise", "rgb( 0, 206, 209)");
                    _svgColorName.Add("darkviolet", "rgb(148, 0, 211)");
                    _svgColorName.Add("deeppink", "rgb(255, 20, 147)");
                    _svgColorName.Add("deepskyblue", "rgb( 0, 191, 255)");
                    _svgColorName.Add("dimgray", "rgb(105, 105, 105)");
                    _svgColorName.Add("dimgrey", "rgb(105, 105, 105)");
                    _svgColorName.Add("dodgerblue", "rgb( 30, 144, 255)");
                    _svgColorName.Add("firebrick", "rgb(178, 34, 34)");
                    _svgColorName.Add("floralwhite", "rgb(255, 250, 240)");
                    _svgColorName.Add("forestgreen", "rgb( 34, 139, 34)");
                    _svgColorName.Add("fuchsia", "rgb(255, 0, 255)");
                    _svgColorName.Add("gainsboro", "rgb(220, 220, 220)");
                    _svgColorName.Add("ghostwhite", "rgb(248, 248, 255)");
                    _svgColorName.Add("gold", "rgb(255, 215, 0)");
                    _svgColorName.Add("goldenrod", "rgb(218, 165, 32)");
                    _svgColorName.Add("gray", "rgb(128, 128, 128)");
                    _svgColorName.Add("grey", "rgb(128, 128, 128)");
                    _svgColorName.Add("green", "rgb( 0, 128, 0)");
                    _svgColorName.Add("greenyellow", "rgb(173, 255, 47)");
                    _svgColorName.Add("honeydew", "rgb(240, 255, 240)");
                    _svgColorName.Add("hotpink", "rgb(255, 105, 180)");
                    _svgColorName.Add("indianred", "rgb(205, 92, 92)");
                    _svgColorName.Add("indigo", "rgb( 75, 0, 130)");
                    _svgColorName.Add("ivory", "rgb(255, 255, 240)");
                    _svgColorName.Add("khaki", "rgb(240, 230, 140)");
                    _svgColorName.Add("lavender", "rgb(230, 230, 250)");
                    _svgColorName.Add("lavenderblush", "rgb(255, 240, 245)");
                    _svgColorName.Add("lawngreen", "rgb(124, 252, 0)");
                    _svgColorName.Add("lemonchiffon", "rgb(255, 250, 205)");
                    _svgColorName.Add("lightblue", "rgb(173, 216, 230)");
                    _svgColorName.Add("lightcoral", "rgb(240, 128, 128)");
                    _svgColorName.Add("lightcyan", "rgb(224, 255, 255)");
                    _svgColorName.Add("lightgoldenrodyellow", "rgb(250, 250, 210)");
                    _svgColorName.Add("lightgray", "rgb(211, 211, 211)");
                    _svgColorName.Add("lightgreen", "rgb(144, 238, 144)");
                    _svgColorName.Add("lightgrey", "rgb(211, 211, 211)");
                    _svgColorName.Add("lightpink", "rgb(255, 182, 193)");
                    _svgColorName.Add("lightsalmon", "rgb(255, 160, 122)");
                    _svgColorName.Add("lightseagreen", "rgb( 32, 178, 170)");
                    _svgColorName.Add("lightskyblue", "rgb(135, 206, 250)");
                    _svgColorName.Add("lightslategray", "rgb(119, 136, 153)");
                    _svgColorName.Add("lightslategrey", "rgb(119, 136, 153)");
                    _svgColorName.Add("lightsteelblue", "rgb(176, 196, 222)");
                    _svgColorName.Add("lightyellow", "rgb(255, 255, 224)");
                    _svgColorName.Add("lime", "rgb( 0, 255, 0)");
                    _svgColorName.Add("limegreen", "rgb( 50, 205, 50)");
                    _svgColorName.Add("linen", "rgb(250, 240, 230)");
                    _svgColorName.Add("magenta", "rgb(255, 0, 255)");
                    _svgColorName.Add("maroon", "rgb(128, 0, 0)");
                    _svgColorName.Add("mediumaquamarine", "rgb(102, 205, 170)");
                    _svgColorName.Add("mediumblue", "rgb( 0, 0, 205)");
                    _svgColorName.Add("mediumorchid", "rgb(186, 85, 211)");
                    _svgColorName.Add("mediumpurple", "rgb(147, 112, 219)");
                    _svgColorName.Add("mediumseagreen", "rgb( 60, 179, 113)");
                    _svgColorName.Add("mediumslateblue", "rgb(123, 104, 238)");
                    _svgColorName.Add("mediumspringgreen", "rgb( 0, 250, 154)");
                    _svgColorName.Add("mediumturquoise", "rgb( 72, 209, 204)");
                    _svgColorName.Add("mediumvioletred", "rgb(199, 21, 133)");
                    _svgColorName.Add("midnightblue", "rgb( 25, 25, 112)");
                    _svgColorName.Add("mintcream", "rgb(245, 255, 250)");
                    _svgColorName.Add("mistyrose", "rgb(255, 228, 225)");
                    _svgColorName.Add("moccasin", "rgb(255, 228, 181)");
                    _svgColorName.Add("navajowhite", "rgb(255, 222, 173)");
                    _svgColorName.Add("navy", "rgb( 0, 0, 128)");
                    _svgColorName.Add("oldlace", "rgb(253, 245, 230)");
                    _svgColorName.Add("olive", "rgb(128, 128, 0)");
                    _svgColorName.Add("olivedrab", "rgb(107, 142, 35)");
                    _svgColorName.Add("orange", "rgb(255, 165, 0)");
                    _svgColorName.Add("orangered", "rgb(255, 69, 0)");
                    _svgColorName.Add("orchid", "rgb(218, 112, 214)");
                    _svgColorName.Add("palegoldenrod", "rgb(238, 232, 170)");
                    _svgColorName.Add("palegreen", "rgb(152, 251, 152)");
                    _svgColorName.Add("paleturquoise", "rgb(175, 238, 238)");
                    _svgColorName.Add("palevioletred", "rgb(219, 112, 147)");
                    _svgColorName.Add("papayawhip", "rgb(255, 239, 213)");
                    _svgColorName.Add("peachpuff", "rgb(255, 218, 185)");
                    _svgColorName.Add("peru", "rgb(205, 133, 63)");
                    _svgColorName.Add("pink", "rgb(255, 192, 203)");
                    _svgColorName.Add("plum", "rgb(221, 160, 221)");
                    _svgColorName.Add("powderblue", "rgb(176, 224, 230)");
                    _svgColorName.Add("purple", "rgb(128, 0, 128)");
                    _svgColorName.Add("red", "rgb(255, 0, 0)");
                    _svgColorName.Add("rosybrown", "rgb(188, 143, 143)");
                    _svgColorName.Add("royalblue", "rgb( 65, 105, 225)");
                    _svgColorName.Add("saddlebrown", "rgb(139, 69, 19)");
                    _svgColorName.Add("salmon", "rgb(250, 128, 114)");
                    _svgColorName.Add("sandybrown", "rgb(244, 164, 96)");
                    _svgColorName.Add("seagreen", "rgb( 46, 139, 87)");
                    _svgColorName.Add("seashell", "rgb(255, 245, 238)");
                    _svgColorName.Add("sienna", "rgb(160, 82, 45)");
                    _svgColorName.Add("silver", "rgb(192, 192, 192)");
                    _svgColorName.Add("skyblue", "rgb(135, 206, 235)");
                    _svgColorName.Add("slateblue", "rgb(106, 90, 205)");
                    _svgColorName.Add("slategray", "rgb(112, 128, 144)");
                    _svgColorName.Add("slategrey", "rgb(112, 128, 144)");
                    _svgColorName.Add("snow", "rgb(255, 250, 250)");
                    _svgColorName.Add("springgreen", "rgb( 0, 255, 127)");
                    _svgColorName.Add("steelblue", "rgb( 70, 130, 180)");
                    _svgColorName.Add("tan", "rgb(210, 180, 140)");
                    _svgColorName.Add("teal", "rgb( 0, 128, 128)");
                    _svgColorName.Add("thistle", "rgb(216, 191, 216)");
                    _svgColorName.Add("tomato", "rgb(255, 99, 71)");
                    _svgColorName.Add("turquoise", "rgb( 64, 224, 208)");
                    _svgColorName.Add("violet", "rgb(238, 130, 238)");
                    _svgColorName.Add("wheat", "rgb(245, 222, 179)");
                    _svgColorName.Add("white", "rgb(255, 255, 255)");
                    _svgColorName.Add("whitesmoke", "rgb(245, 245, 245)");
                    _svgColorName.Add("yellow", "rgb(255, 255, 0)");
                    _svgColorName.Add("yellowgreen", "rgb(154, 205, 50)");
                }
                return _svgColorName;
            }
        }

        public Color ParseSvgColor(string svgColor)
        {
            if (svgColor == null || svgColor == "none" || svgColor == "")
            {
                return Colors.Black;
            }

            if (SvgColorName.ContainsKey(svgColor.ToLower()))
            {
                svgColor = SvgColorName[svgColor.ToLower()];
            }

            Color color = new Color();
            color.A = 255;

            //const string rgbRegEx = @"(?<rgb>[rR][gG][bB]\(\s?(?<red>\d{1,3}%?)\s?,\s?(?<green>\d{1,3}%?)\s?,\s?(?<blue>\d{1,3}%?)\s?\))|(?<rrggbb>#(?<xxred>[A-Fa-f0-9]{2})(?<xxgreen>[A-Fa-f0-9]{2})(?<xxblue>[A-Fa-f0-9]{2}))|(?<xxx>#(?<xred>[A-Fa-f0-9])(?<xgreen>[A-Fa-f0-9])(?<xblue>[A-Fa-f0-9]))";
            //const string rgbRegEx = @"(?<rgba>[rR][gG][bB][aA]\(\s?(?<ared>\d{1,3}%?)\s?,\s?(?<agreen>\d{1,3}%?)\s?,\s?(?<ablue>\d{1,3}%?)\s?,(?<alpha>[01].\d{0,3}%?)\s?\))|(?<rgb>[rR][gG][bB]\(\s?(?<red>\d{1,3}%?)\s?,\s?(?<green>\d{1,3}%?)\s?,\s?(?<blue>\d{1,3}%?)\s?\))|(?<rrggbb>#(?<xxred>[A-Fa-f0-9]{2})(?<xxgreen>[A-Fa-f0-9]{2})(?<xxblue>[A-Fa-f0-9]{2}))|(?<xxx>#(?<xred>[A-Fa-f0-9])(?<xgreen>[A-Fa-f0-9])(?<xblue>[A-Fa-f0-9]))";
            const string rgbRegEx = @"(?<rgba>[rR][gG][bB][aA]\(\s?(?<ared>\d{1,3}%?)\s?,\s?(?<agreen>\d{1,3}%?)\s?,\s?(?<ablue>\d{1,3}%?)\s?,\s?(?<alpha>[01]?.\d{0,3}%?)\s?\))|(?<rgb>[rR][gG][bB]\(\s?(?<red>\d{1,3}%?)\s?,\s?(?<green>\d{1,3}%?)\s?,\s?(?<blue>\d{1,3}%?)\s?\))|(?<rrggbb>#(?<xxred>[A-Fa-f0-9]{2})(?<xxgreen>[A-Fa-f0-9]{2})(?<xxblue>[A-Fa-f0-9]{2}))|(?<xxx>#(?<xred>[A-Fa-f0-9])(?<xgreen>[A-Fa-f0-9])(?<xblue>[A-Fa-f0-9]))";
            MatchCollection mc = Regex.Matches(svgColor.Trim(), rgbRegEx);

            if (mc.Count == 1)
            {
                if (mc[0].Groups["red"].Success)
                {
                    color.R = ParseColorComponent(mc[0].Groups["red"].Value);
                }
                else if (mc[0].Groups["ared"].Success)
                {
                    color.R = ParseColorComponent(mc[0].Groups["ared"].Value);
                }

                if (mc[0].Groups["green"].Success)
                {
                    color.G = ParseColorComponent(mc[0].Groups["green"].Value);
                }
                else if (mc[0].Groups["agreen"].Success)
                {
                    color.G = ParseColorComponent(mc[0].Groups["agreen"].Value);
                }

                if (mc[0].Groups["blue"].Success)
                {
                    color.B = ParseColorComponent(mc[0].Groups["blue"].Value);
                }
                else if (mc[0].Groups["ablue"].Success)
                {
                    color.B = ParseColorComponent(mc[0].Groups["ablue"].Value);
                }

                if (mc[0].Groups["alpha"].Success)
                {
                    double a;
                    string s = mc[0].Groups["alpha"].Value;

                    if (s.EndsWith("%"))
                    {
                        if (double.TryParse(s.Replace("%", ""), out a))
                        {
                            color.A = (byte)((a / 100) * 255);
                        }
                    }
                    if (double.TryParse(s, out a))
                    {
                        color.A = (byte)(a * 255);
                    }
                }

                if (mc[0].Groups["xred"].Success)
                {
                    string xx = mc[0].Groups["xred"].Value + mc[0].Groups["xred"].Value;
                    color.R = byte.Parse(xx, NumberStyles.AllowHexSpecifier);
                }
                if (mc[0].Groups["xgreen"].Success)
                {
                    string xx = mc[0].Groups["xgreen"].Value + mc[0].Groups["xgreen"].Value;
                    color.G = byte.Parse(xx, NumberStyles.AllowHexSpecifier);
                }
                if (mc[0].Groups["xblue"].Success)
                {
                    string xx = mc[0].Groups["xblue"].Value + mc[0].Groups["xblue"].Value;
                    color.B = byte.Parse(xx, NumberStyles.AllowHexSpecifier);
                }

                if (mc[0].Groups["xxred"].Success)
                {
                    color.R = byte.Parse(mc[0].Groups["xxred"].Value, NumberStyles.AllowHexSpecifier);
                }
                if (mc[0].Groups["xxgreen"].Success)
                {
                    color.G = byte.Parse(mc[0].Groups["xxgreen"].Value, NumberStyles.AllowHexSpecifier);
                }
                if (mc[0].Groups["xxblue"].Success)
                {
                    color.B = byte.Parse(mc[0].Groups["xxblue"].Value, NumberStyles.AllowHexSpecifier);
                }
            }

            return color;
        }

        public async Task<string> Import(StorageFile file)
        {
            string error = null;

            try
            {
                using (Stream fileStream = await file.OpenStreamForReadAsync())
                {
                    using (_xmlReader = XmlReader.Create(fileStream, _xmlReaderSettings))
                    {
                        while (await _xmlReader.ReadAsync())
                        {
                            if (_xmlReader.NodeType == XmlNodeType.Element && _xmlReader.Name == "svg")
                            {
                                Dictionary<string, string> svgAttributes = new Dictionary<string, string>();
                                ProcessAttributes(svgAttributes);
                                await ReadSvg(svgAttributes);
                            }
                            else if (_xmlReader.NodeType == XmlNodeType.XmlDeclaration)
                            {
                                ReadXmlDeclaration();
                            }
                            else if (_xmlReader.NodeType == XmlNodeType.Whitespace)
                            {
                                // ignore whitespace
                            }
                            else if (_xmlReader.NodeType == XmlNodeType.SignificantWhitespace)
                            {
                                // ignore significant whitespace?
                            }
                            else if (_xmlReader.NodeType == XmlNodeType.Comment)
                            {
                                // ignore comment
                            }
                            else if (_xmlReader.NodeType == XmlNodeType.Element)
                            {
                                Dictionary<string, string> attributes = new Dictionary<string, string>();
                                ProcessAttributes(attributes);
                                SetValueForKey(attributes, "node-type", "root");
                                ProcessUnexpectedSvgElement(attributes);
                            }
                            else
                            {
                                UnexpectedNode();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                //System.Diagnostics.Debug.WriteLine(e.Message);
            }

            return error;
        }

        void SetValueForKey(Dictionary<string, string> dictionary, string key, string value)
        {
            if (dictionary.ContainsKey(key))
            {
                if (key == "transform")
                {
                    dictionary[key] = dictionary[key] + " " + value;
                }
                else
                {
                    dictionary[key] = value;
                }
            }
            else
            {
                dictionary.Add(key, value);
            }
        }

        private void CopyAttributes(Dictionary<string, string> srcAttributes, Dictionary<string, string> dstAttributes)
        {
            foreach (string key in srcAttributes.Keys)
            {
                switch (key)
                {
                    case "font":
                    case "font-family":
                    case "font-size":
                    case "font-size-adjust":
                    case "font-stretch":
                    case "font-style":
                    case "font-variant":
                    case "font-weight":
                                        //Text properties:
                    case "direction":
                    case "letter-spacing":
                    case "text-decoration":
                    case "unicode-bidi":
                    case "word-spacing":
                                        //Other properties for visual media:
                    case "color":       // used to provide a potential indirect value (currentColor) for the 'fill':, 'stroke':, 'stop-color':, 'flood-color': and 'lighting-color': properties. 
                                        // (The SVG properties which support color allow a color specification which is extended from CSS2 to accommodate color definitions 
                                        // in arbitrary color spaces. See Color profile descriptions.)
                    case "cursor":
                    case "display":
                    case "overflow":    // only applicable to elements which establish a new viewport.
                    case "visibility":
                                        //The following SVG properties are not defined in CSS2. The complete normative definitions for these properties are found in this specification:
                                        //Clipping, Masking and Compositing properties:
                    case "clip-path":
                    case "clip-rule":
                    case "mask":
                    case "opacity":
                                        //Filter Effects properties:
                    case "enable-background":
                    case "filter":
                    case "flood-color":
                    case "flood-opacity":
                    case "lighting-color":
                                        //Gradient properties:
                    case "stop-color":
                    case "stop-opacity":
                                        //Interactivity properties:
                    case "pointer-events":
                                        //Color and Painting properties:
                    case "color-interpolation":
                    case "color-interpolation-filters":
                    case "color-profile":
                    case "color-rendering":
                    case "fill":
                    case "fill-opacity":
                    case "fill-rule":
                    case "image-rendering":
                    case "marker":
                    case "marker-end":
                    case "marker-mid":
                    case "marker-start":
                    case "shape-rendering":
                    case "stroke":
                    case "stroke-dasharray":
                    case "stroke-dashoffset":
                    case "stroke-linecap":
                    case "stroke-linejoin":
                    case "stroke-miterlimit":
                    case "stroke-opacity":
                    case "stroke-width":
                    case "text-rendering":
                                        //Text properties:
                    case "alignment-baseline":
                    case "baseline-shift":
                    case "dominant-baseline":
                    case "glyph-orientation-horizontal":
                    case "glyph-orientation-vertical":
                    case "kerning":
                    case "text-anchor":
                    case "writing-mode":
                                        // xml properties
                    case "xml:space":
                        SetValueForKey(dstAttributes, key, srcAttributes[key]);
                        break;

                    case "transform":
                    case "class":
                    case "style":
                        SetValueForKey(dstAttributes, key, srcAttributes[key]);
                        break;

                    default:
                    case "clip":        // only applicable to outermost svg element.
                        break;
                }
            }
        }

        private void ProcessAttributes(Dictionary<string, string> attributes)
        {
            if (_xmlReader.HasAttributes)
            {
                for (int attInd = 0; attInd < _xmlReader.AttributeCount; attInd++)
                {
                    _xmlReader.MoveToAttribute(attInd);
                    SetValueForKey(attributes, _xmlReader.Name, _xmlReader.Value);
                }

                _xmlReader.MoveToElement();
            }
        }

        protected virtual async Task ReadSvgContainer(ISvgContainer container, Dictionary<string, string> containerAttributes)
        {
            if (_xmlReader.IsEmptyElement == false)
            {
                while (await _xmlReader.ReadAsync())
                {
                    if (_xmlReader.NodeType == XmlNodeType.Element)
                    {
                        Dictionary<string, string> attributes = new Dictionary<string, string>();

                        // copy container attributes to the child attribute list
                        CopyAttributes(containerAttributes, attributes);

                        // add attributes from the current element to the child attribute list
                        ProcessAttributes(attributes);

                        string id = attributes.ContainsKey("id") ? attributes["id"] : "none";

                        switch (_xmlReader.Name)
                        {
                            case "g":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ISvgContainer group = ProcessSvgGroup(container, attributes);
                                await ReadSvgContainer(group, attributes);
                                break;

                            case "defs":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ISvgContainer defs = ProcessSvgDefs(container, attributes);
                                await ReadSvgContainer(defs, attributes);
                                break;

                            case "symbol":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ISvgContainer symbol = ProcessSvgSymbol(container, attributes);
                                await ReadSvgContainer(symbol, attributes);
                                break;

                            case "marker":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ISvgContainer marker = ProcessSvgMarker(container, attributes);
                                await ReadSvgContainer(marker, attributes);
                                break;

                            case "svg":
                                // the spec allows for nested <svg> elements
                                // need to find a sample to see how that is supposed to work
#if DEBUG
                                System.Diagnostics.Debugger.Break();
#endif
                                break;

                            case "line":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ProcessSvgLine(container, attributes);
                                break;

                            case "rect":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ProcessSvgRect(container, attributes);
                                break;

                            case "circle":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ProcessSvgCircle(container, attributes);
                                break;

                            case "ellipse":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ProcessSvgEllipse(container, attributes);
                                break;

                            case "polyline":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ProcessSvgPolyline(container, attributes);
                                break;

                            case "polygon":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ProcessSvgPolygon(container, attributes);
                                break;

                            case "path":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ProcessSvgPath(container, attributes);
                                break;

                            case "use":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ProcessSvgUse(container, attributes);
                                break;

                            case "text":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                await ReadSvgText(container, attributes);
                                break;

                            case "tspan":
                                // must be a child of a text element
                                break;

                            case "image":
                                AddStyleAttributesForShape(_xmlReader.Name, attributes);
                                AddAttributesFromStyle(attributes);
                                ProcessSvgImage(container, attributes);
                                break;

                            case "style":
                                await ReadSvgStyle(attributes);
                                break;

                            case "desc":
                            case "metadata":
                            case "title":
                            case "a":
                                // unimplemented svg elements (there are others)
                                await ReadUnimplementedElement(attributes);
                                break;

                            case "sodipodi:namedview":
                            case "inkscape:perspective":
                                // inkscape extensions
                                await ReadUnimplementedElement(attributes);
                                break;

                            case "rdf:RDF":
                            case "cc:Work":
                            case "dc:format":
                            case "dc:type":
                                // other extensions
                                await ReadUnimplementedElement(attributes);
                                break;

                            default:
                                string type = container.GetType().ToString();
                                SetValueForKey(attributes, "parent-element", type);
                                await ReadUnimplementedElement(attributes);
                                break;
                        }
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.EndElement)
                    {
                        // end of container definition
                        break;
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Whitespace)
                    {
                        // ignore whitespace
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.SignificantWhitespace)
                    {
                        // ignore significant whitespace?
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Comment)
                    {
                        // ignore comment
                    }
                    else
                    {
                        UnexpectedNode();
                    }
                }
            }
        }

        protected async virtual Task ReadSvg(Dictionary<string, string> svgAttributes)
        {
            AddAttributesFromStyle(svgAttributes);

            ISvgContainer container = ProcessSvgElement(svgAttributes);

            Dictionary<string, string> attributes = new Dictionary<string, string>();
            CopyAttributes(svgAttributes, attributes);

            await ReadSvgContainer(container, attributes);
        }

        private async Task ReadSvgStyle(Dictionary<string, string> styleAttributes)
        {
            string name = _xmlReader.Name;

            if (_xmlReader.IsEmptyElement == false)
            {
                while (await _xmlReader.ReadAsync())
                {
                    if (_xmlReader.NodeType == XmlNodeType.Text)
                    {
                        // ignore text
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.EndElement && _xmlReader.Name == name)
                    {
                        break;
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.CDATA)
                    {
                        ProcessSvgStyles(_xmlReader.Value);
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Element)
                    {
                        Dictionary<string, string> attributes = new Dictionary<string, string>();
                        ProcessAttributes(attributes);
                        await ReadUnimplementedElement(attributes);
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Whitespace)
                    {
                        // ignore whitespace
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.SignificantWhitespace)
                    {
                        // ignore significant whitespace?
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Comment)
                    {
                        // ignore comment
                    }
                    else
                    {
                        UnexpectedNode();
                    }
                }
            }
        }

        private void AddStyleAttributesForShape(string type, Dictionary<string, string> attributes)
        {
            // add style attributes for shape to dictionary
            if (_shapeRules.ContainsKey(type))
            {
                Dictionary<string, string> styles = ParseStyleList(_shapeRules[type]);
                foreach (string key in styles.Keys)
                {
                    attributes[key] = styles[key];
                }
            }
        }

        private void AddAttributesFromStyle(Dictionary<string, string> attributes)
        {
            // add attributes to dictionary from class
            if (attributes.ContainsKey("class"))
            {
                string className = attributes["class"];
                string[] ca = className.Split(new char[] { ' ' });
                int classCount = 0;

                foreach (string c in ca)
                {
                    if (_classRules.ContainsKey(c))
                    {
                        Dictionary<string, string> styles = ParseStyleList(_classRules[c]);
                        foreach (string key in styles.Keys)
                        {
                            attributes[key] = styles[key];
                        }
                        classCount++;
                    }
                }

                if (classCount == ca.Length)
                {
                    attributes.Remove("class");
                }
            }

            // add attributes to dictionary from style
            if (attributes.ContainsKey("style"))
            {
                Dictionary<string, string> styles = ParseStyleList(attributes["style"]);
                foreach (string key in styles.Keys)
                {
                    attributes[key] = styles[key];
                }

                attributes.Remove("style");
            }
        }

        protected virtual async Task ReadSvgTspan(ISvgContainer container, Dictionary<string, string> tspanAttributes)
        {
            if (_xmlReader.IsEmptyElement)
            {
                // InkScape creates empty tspan elements - why? 
            }
            else
            {
                bool preserve = tspanAttributes.ContainsKey("xml:space") && tspanAttributes["xml:space"] == "preserve";

                while (await _xmlReader.ReadAsync())
                {
                    if (_xmlReader.NodeType == XmlNodeType.Text)
                    {
                        SetValueForKey(tspanAttributes, "text-value", SvgString(_xmlReader.Value, preserve));
                        ProcessSvgTspan(container, tspanAttributes);
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.EndElement && _xmlReader.Name == "tspan")
                    {
                        break;
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Element && _xmlReader.Name == "tspan")
                    {
                        // nested tspans - this is ok, right?
                        Dictionary<string, string> attributes = new Dictionary<string, string>();
                        ProcessAttributes(attributes);
                        AddStyleAttributesForShape(_xmlReader.Name, attributes);
                        AddAttributesFromStyle(attributes);
                        await ReadSvgTspan(container, attributes);
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Whitespace)
                    {
                        // ignore whitespace
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.SignificantWhitespace)
                    {
                        // ignore significant whitespace?
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Comment)
                    {
                        // ignore comment
                    }
                    else
                    {
                        UnexpectedNode();
                    }
                }
            }
        }

        private string SvgString(string p, bool preserve)
        {
            string s = p;

            if (preserve)
            {
                s = s.Replace("\t", " ");
            }
            else
            {
                s = s.Replace("\n", "").Replace("\t", " ").Trim();

                while (s.Contains("  "))
                {
                    s = s.Replace("  ", " ");
                }
            }

            return s;
        }

        protected virtual async Task ReadSvgText(ISvgContainer container, Dictionary<string, string> textAttributes)
        {
            bool fired = false;

            if (_xmlReader.IsEmptyElement)
            {
                ProcessSvgText(container, textAttributes);
                fired = true;
            }
            else
            {
                bool preserve = textAttributes.ContainsKey("xml:space") && textAttributes["xml:space"] == "preserve";

                while (await _xmlReader.ReadAsync())
                {
                    if (_xmlReader.NodeType == XmlNodeType.Text)
                    {
                        if (fired)
                        {
                            if (textAttributes.ContainsKey("x"))
                            {
                                textAttributes.Remove("x");
                            }
                            if (textAttributes.ContainsKey("y"))
                            {
                                textAttributes.Remove("y");
                            }
                            SetValueForKey(textAttributes, "text-value", SvgString(_xmlReader.Value, preserve));
                            ProcessSvgTspan(container, textAttributes);
                        }
                        else
                        {
                            SetValueForKey(textAttributes, "text-value", SvgString(_xmlReader.Value, preserve));
                            ProcessSvgText(container, textAttributes);
                            fired = true;
                        }
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.EndElement && _xmlReader.Name == "text")
                    {
                        break;
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Element && _xmlReader.Name == "tspan")
                    {
                        if (!fired)
                        {
                            ProcessSvgText(container, textAttributes);
                            fired = true;
                        }

                        Dictionary<string, string> attributes = new Dictionary<string, string>();
                        CopyAttributes(textAttributes, attributes);

                        ProcessAttributes(attributes);
                        AddStyleAttributesForShape(_xmlReader.Name, attributes);
                        AddAttributesFromStyle(attributes);
                        await ReadSvgTspan(container, attributes);
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Whitespace)
                    {
                        // ignore whitespace
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.SignificantWhitespace)
                    {
                        // ignore significant whitespace?
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Comment)
                    {
                        // ignore comment
                    }
                    else
                    {
                        UnexpectedNode();
                    }
                }
            }
        }

        protected virtual async Task ReadUnimplementedElement(Dictionary<string, string> uAttributes)
        {
            SvgUnimplementedElement(uAttributes);

            string name = _xmlReader.Name;

            if (_xmlReader.IsEmptyElement == false)
            {
                while (await _xmlReader.ReadAsync())
                {
                    if (_xmlReader.NodeType == XmlNodeType.Text)
                    {
                        // ignore text
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.EndElement && _xmlReader.Name == name)
                    {
                        break;
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Element)
                    {
                        Dictionary<string, string> attributes = new Dictionary<string, string>();
                        ProcessAttributes(attributes);
                        await ReadUnimplementedElement(attributes);
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Whitespace)
                    {
                        // ignore whitespace
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.SignificantWhitespace)
                    {
                        // ignore significant whitespace?
                    }
                    else if (_xmlReader.NodeType == XmlNodeType.Comment)
                    {
                        // ignore comment
                    }
                    else
                    {
                        UnexpectedNode();
                    }
                }
            }
        }

        protected virtual void ProcessSvgStyles(string cdata)
        {
            string styleregex = @"((?<comment>[;]?)\s*(?<name>[\.]?\w+)\s*\{(?<rule>[^\}]*)\})";
            MatchCollection mc = Regex.Matches(cdata, styleregex);

            foreach (Match match in mc)
            {
                if (match.Groups["name"].Success)
                {
                    if (match.Groups["comment"].Success && match.Groups["comment"].Value == ";")
                    {
                        // comment - ignore
                    }
                    else if (match.Groups["rule"].Success)
                    {
                        string name = match.Groups["name"].Value;
                        if (name.StartsWith("."))
                        {
                            // class
                            _classRules[name.Substring(1)] = match.Groups["rule"].Value.Trim();
                        }
                        else
                        {
                            // shape
                            _shapeRules[name] = match.Groups["rule"].Value.Trim();
                        }
                    }
                }
            }
        }

        protected abstract void UnexpectedNode();

        protected abstract void ReadXmlDeclaration();

        protected abstract void SvgUnimplementedElement(Dictionary<string, string> attributes);

        protected abstract ISvgContainer ProcessSvgElement(Dictionary<string, string> attributes);

        protected abstract ISvgContainer ProcessSvgGroup(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract ISvgContainer ProcessSvgDefs(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract ISvgContainer ProcessSvgSymbol(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract ISvgContainer ProcessSvgMarker(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessSvgPath(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessSvgPolygon(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessSvgPolyline(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessSvgLine(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessSvgEllipse(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessSvgCircle(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessSvgRect(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessSvgImage(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessSvgText(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessSvgTspan(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessSvgUse(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessUnknownSvgShape(ISvgContainer container, Dictionary<string, string> attributes);

        protected abstract void ProcessUnexpectedSvgElement(Dictionary<string, string> attributes);
    }
}
