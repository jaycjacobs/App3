using Cirros.Utility;
using Cirros.TextUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Pdf
{
    public class FontIndex
    {
        public static Dictionary<string, string> Font = new Dictionary<string, string>();
    }

    public struct PdfColor
    {
        private double _red;
        private double _green;
        private double _blue;
        private double _alpha;

        public PdfColor(uint red, uint green, uint blue, uint alpha = 255)
        {
            _red = Math.Round(red / 255.0, 3);
            _green = Math.Round(green / 255.0, 3);
            _blue = Math.Round(blue / 255.0, 3);
            _alpha = Math.Round(alpha / 255.0, 3);
        }

        public PdfColor(Color color)
        {
            _red = Math.Round(color.R / 255.0, 3);
            _green = Math.Round(color.G / 255.0, 3);
            _blue = Math.Round(color.B / 255.0, 3);
            _alpha = Math.Round(color.A / 255.0, 3);
        }

        public string Red
        {
            get { return _red.ToString(); }
        }

        public string Green
        {
            get { return _green.ToString(); }
        }

        public string Blue
        {
            get { return _blue.ToString(); }
        }

        public string Alpha
        {
            get { return _alpha.ToString(); }
        }
    }

    public struct PageSize
    {
        public uint Width;
        public uint Height;
        public uint LeftMargin;
        public uint RightMargin;
        public uint TopMargin;
        public uint BottomMargin;

        public PageSize(uint width, uint height)
        {
            Width = width;
            Height = height;
            LeftMargin = 0;
            RightMargin = 0;
            TopMargin = 0;
            BottomMargin = 0;
        }

        public void SetMargins(uint left, uint top, uint right, uint bottom)
        {
            LeftMargin = left;
            RightMargin = right;
            TopMargin = top;
            BottomMargin = bottom;
        }
    }

    public enum PdfAlign
    {
        Left,
        Center,
        Right
    }

    internal class XrefEntries
    {
        internal static List<ObjectList> OffsetArray;

        internal XrefEntries()
        {
            OffsetArray = new List<ObjectList>();
        }
    }

    internal class ObjectList : IComparable
    {
        internal long Offset;
        internal uint ObjectNum;

        internal ObjectList(uint objectNum, long fileOffset)
        {
            Offset = fileOffset;
            ObjectNum = objectNum;
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {

            int result = 0;
            result = (this.ObjectNum.CompareTo(((ObjectList)obj).ObjectNum));
            return result;
        }

        #endregion
    }

    public class PdfObject
    {
        internal static uint CurrentObjectNum;
        public uint ObjectNum;
    
        private XrefEntries Xref;

        protected PdfObject()
        {
            if (CurrentObjectNum == 0)
            {
                Xref = new XrefEntries();
            }

            CurrentObjectNum++;
            ObjectNum = CurrentObjectNum;
        }

        protected byte[] GetUTF8Bytes(string str, long filePos, out int size)
        {
            ObjectList objectList = new ObjectList(ObjectNum, filePos);

            byte[] ubuf = Encoding.Unicode.GetBytes(str);

            Encoding encoding = Encoding.GetEncoding("Latin1");

            byte[] utf8buf = Encoding.Convert(Encoding.Unicode, encoding, ubuf);
            size = utf8buf.Length;

            XrefEntries.OffsetArray.Add(objectList);

            return utf8buf;
        }

        protected byte[] GetImageBytes(String startStr, String endStr, byte[] imageByteStream, long filePos, out int size)
        {
            ObjectList objList = new ObjectList(ObjectNum, filePos);
            byte[] s;
            byte[] e;

            // the encoding conversion probably isn't right - need to check
            Encoding enc = Encoding.UTF8;

            s = Encoding.Unicode.GetBytes(startStr);
            s = Encoding.Convert(Encoding.Unicode, enc, s);

            e = Encoding.Unicode.GetBytes(endStr);
            e = Encoding.Convert(Encoding.Unicode, enc, e);

            XrefEntries.OffsetArray.Add(objList);

            size = s.Length + imageByteStream.Length + e.Length;

            byte[] abuf = new byte[size];

            int count = 0;
            int i = 0;

            while (count < s.Length)
            {
                abuf[i] = s[count];
                count++;
                i++;
            }

            count = 0;
            while (count < imageByteStream.Length)
            {
                abuf[i] = imageByteStream[count];
                count++;
                i++;
            }

            count = 0;
            while (count < e.Length)
            {
                abuf[i] = e[count];
                count++;
                i++;
            }

            return abuf;
        }
    }

    public class CatalogDictionary : PdfObject
    {
        private string _catalog;

        public CatalogDictionary()
        {
        }

        public byte[] GetCatalogDictionary(uint refPageTree, long filePos, out int size)
        {
            if (refPageTree < 1)
            {
                throw new Exception("Invalid PageTree.objectNum in GetCatalogDictionary");
            }

            _catalog = string.Format("{0} 0 obj\n<</Type /Catalog/Lang(EN-US)/Pages {1} 0 R>>\nendobj\n", this.ObjectNum, refPageTree);

            return GetUTF8Bytes(_catalog, filePos, out size);
        }
    }

    public class PageTreeDictionary : PdfObject
    {
        private string _pageTree;
        private string _kids;
        private static uint _maxPages;

        public PageTreeDictionary()
        {
            _kids = "[ ";
            _maxPages = 0;
        }

        public void AddPage(uint objNum)
        {
            if (objNum < 0 || objNum > PdfObject.CurrentObjectNum)
            {
                throw new Exception("objNum invalid in PageTreeDictionary.AddPage");
            }

            _maxPages++;
            string refPage = objNum + " 0 R ";
            _kids = _kids + refPage;
        }

        public byte[] GetPageTree(long filePos, out int size)
        {
            _pageTree = string.Format("{0} 0 obj\n<</Count {1}/Kids {2}]>>\nendobj\n", this.ObjectNum, _maxPages, _kids);
            return this.GetUTF8Bytes(_pageTree, filePos, out size);
        }
    }

    public class PageDictionary : PdfObject
    {
        private string _page;
        private string _fontRef;
        private string _gsRef;
        private string _imageRef;
        private string _contentRef;

        private List<ImageDictionary> _imageDictionaries = new List<ImageDictionary>();

        public List<ImageDictionary> ImageDictionaries
        {
            get { return _imageDictionaries; }
        }

        public PageDictionary()
        {
            _contentRef = null;
            _fontRef = null;
            _gsRef = null;
            _imageRef = null;
        }

        public void CreatePage(uint refParent, PageSize pageSize, uint contentRef)
        {
            if (refParent < 1 || refParent > PdfObject.CurrentObjectNum)
            {
                throw new Exception("refParent invalid in PageDictionary.CreatePage");
            }

            string _pageSize = string.Format("[0 0 {0} {1}]", pageSize.Width, pageSize.Height);
            _page = string.Format("{0} 0 obj\n<</Type /Page/Parent {1} 0 R/Rotate 0/MediaBox {2}/CropBox {2}\n/Resources<</ProcSet[/PDF/Text]\n", this.ObjectNum, refParent, _pageSize);
            _contentRef = string.Format("/Contents {0} 0 R", contentRef);
        }

        public void AddResource(FontDictionary font)
        {
            _fontRef += string.Format("/{0} {1} 0 R", font.Font, font.ObjectNum);
        }

        public void AddResource(GraphicStateDictionary gsd)
        {
            _gsRef += string.Format("/{0} {1} 0 R", "ExtGState", gsd.ObjectNum);
        }

        public void AddImageResource(String pdfImageName, ImageDictionary imageDictionary, uint contentRef)
        {
            _imageDictionaries.Add(imageDictionary);

            _imageRef += string.Format("/{0} {1} 0 R ", pdfImageName, imageDictionary.ObjectNum);
        }

        public byte[] GetPageDictionary(long filePos, out int size)
        {
            StringBuilder resourceDictionary = new StringBuilder();

            resourceDictionary.AppendFormat("/Font<<{0}>>", _fontRef);
            resourceDictionary.Append(_gsRef);

            //add in Xobject reference (if any)
            if (_imageRef != null)
            {
                resourceDictionary.AppendFormat("\n/XObject <<{0}>>", _imageRef.ToString());
            }

            resourceDictionary.Append(">>");
            resourceDictionary.Append(_contentRef);
            resourceDictionary.Append(">>\nendobj\n");

            _page += resourceDictionary;

            return this.GetUTF8Bytes(_page, filePos, out size);
        }
    }

    public class ContentDictionary : PdfObject
    {
        private StringBuilder _contentStreamSb;

        public ContentDictionary()
        {
            _contentStreamSb = new StringBuilder();
        }

        public void SetStream(string stream)
        {
            _contentStreamSb.Append(stream);
        }

        public byte[] GetContentDictionary(long filePos, out int size)
        {
            bool flate = false;

            string s = string.Format("{0} 0 obj\n<</Length {1}>>\nstream\n{2}\nendstream\nendobj\n", this.ObjectNum, _contentStreamSb.Length, _contentStreamSb.ToString());

            byte[] content = GetUTF8Bytes(s, filePos, out size);

            if (flate)
            {
                // flate compression goes here
            }

            return content;
        }
    }

    public class FontDictionary : PdfObject
    {
        private string _fontDictionary;

        public string Font;

        public FontDictionary()
        {
            _fontDictionary = null;
        }

        public void CreateFontDictionary(string fontName, string baseFont)
        {
            Font = fontName;

            _fontDictionary = string.Format("{0} 0 obj\n<</Type/Font/Name /{1}/BaseFont/{2}/Subtype/Type1/Encoding /WinAnsiEncoding>>\nendobj\n", this.ObjectNum, fontName, baseFont);

            if (FontIndex.Font.ContainsKey(fontName) == false)
            {
                FontIndex.Font.Add(fontName, baseFont);
            }
        }

        public byte[] GetFontDictionary(long filePos, out int size)
        {
            return this.GetUTF8Bytes(_fontDictionary, filePos, out size);
        }
    }

    public class GraphicStateDictionary : PdfObject
    {
        private string _gsDictionary;

        public GraphicStateDictionary()
        {
            _gsDictionary = null;
        }

        public void CreateGraphicStateDictionary(Dictionary<string, string> opacityDictionary)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0} 0 obj\n<<", this.ObjectNum);

            foreach (string key in opacityDictionary.Keys)
            {
                sb.AppendFormat("/T{0} <</Type/ExtGState {1}>>", key, opacityDictionary[key]);
            }

            sb.Append(">>\nendobj\n");

            _gsDictionary = sb.ToString();
        }

        public byte[] GetXDictionary(long filePos, out int size)
        {
            return this.GetUTF8Bytes(_gsDictionary, filePos, out size);
        }
    }

    public class ImageDictionary : PdfObject
    {
        private string _imageDictionaryStart;
        private string _imageDictionaryEnd;
        private byte[] _imagebytes;

        public string ImageName;
        public int Width;
        public int Height;

        public ImageDictionary()
        {
            ImageName = null;

            _imagebytes = null;
            _imageDictionaryEnd = null;
            _imageDictionaryStart = null;
        }

        public async Task CreateImageDictionary(string imageName, StorageFile imageFile)
        {
            Width = 0;
            Height = 0;

            using (var stream = await imageFile.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                Width = (int)decoder.PixelWidth;
                Height = (int)decoder.PixelHeight;

                Stream rs = (await imageFile.OpenReadAsync()).AsStreamForRead();
                BinaryReader reader = new BinaryReader(rs);
                _imagebytes = reader.ReadBytes((int)stream.Size);
            }

            if (Width > 0 && Height > 0 && _imagebytes != null && _imagebytes.Length > 0)
            {
                ImageName = imageName;
                _imageDictionaryStart = string.Format("{0} 0 obj\n<</Name /{1}\n /Type /XObject\n /Subtype /Image\n /Width {2}\n /Height {3}\n /Length {4}\n /Filter /DCTDecode\n /ColorSpace /DeviceRGB\n /BitsPerComponent 8\n>>\nstream\n",
                    this.ObjectNum, imageName, Width, Height, _imagebytes.Length);

                _imageDictionaryEnd = "\nendstream\nendobj\n";
            }
        }

        public byte[] GetImageDictionary(long filePos, out int size)
        {
            return this.GetImageBytes(_imageDictionaryStart, _imageDictionaryEnd, _imagebytes, filePos, out size);
        }
    }

    public class InfoDictionary : PdfObject
    {
        private string _info;

        public InfoDictionary()
        {
            _info = null;
        }

        public void SetInfo(string title, string author, string company)
        {
            _info = string.Format("{0} 0 obj\n<</ModDate({1})/CreationDate({1})/Title({2})/Creator({3})/Author(auto generated)/Producer({3})/Company({4})>>\nendobj\n", this.ObjectNum, pdfDateTime(), title, author, company);
        }

        public byte[] GetInfoDictionary(long filePos, out int size)
        {
            return GetUTF8Bytes(_info, filePos, out size);
        }

        private string pdfDateTime()
        {
            DateTime universalDate = DateTime.UtcNow;
            DateTime localDate = DateTime.Now;
            string pdfDate = string.Format("D:{0:yyyyMMddhhmmss}", localDate);
            TimeSpan span = localDate.Subtract(universalDate);

            int uHour = span.Hours;
            int uMinute = span.Minutes;
            char sign = uHour < 0 ? '-' : '+';

            uHour = Math.Abs(uHour);
            pdfDate += string.Format("{0}{1}'{2}'", sign, uHour.ToString().PadLeft(2, '0'), uMinute.ToString().PadLeft(2, '0'));

            return pdfDate;
        }
    }

    public class Utility
    {
        private uint _tableEntryCount;

        public Utility()
        {
            _tableEntryCount = 0;
        }

        internal float Round(double Value)
        {
            return ((float)Math.Round(Value, 6, MidpointRounding.AwayFromZero));
        }

        public byte[] CreateXrefTable(long fileOffset, out int size)
        {
            string table;

            ObjectList objList = new ObjectList(0, fileOffset);
            XrefEntries.OffsetArray.Add(objList);
            XrefEntries.OffsetArray.Sort();

            _tableEntryCount = (uint)XrefEntries.OffsetArray.Count;
            table = string.Format("xref\n{0} {1}\n0000000000 65535 f\n", 0, _tableEntryCount);

            for (int entries = 1; entries < _tableEntryCount; entries++)
            {
                ObjectList obj = (ObjectList)XrefEntries.OffsetArray[entries];
                table += obj.Offset.ToString().PadLeft(10, '0');
                table += " 00000 n\n";
            }

            return GetUTF8Bytes(table, out size);
        }

        public byte[] GetHeader(string version, out int size)
        {
            string header = string.Format("%PDF-{0}\n%{1}\n", version, "\x82\x82");
            return GetUTF8Bytes(header, out size);
        }

        private string CreateHashCryptographicHash()
        {
            String algName = Windows.Security.Cryptography.Core.HashAlgorithmNames.Md5;

            HashAlgorithmProvider Algorithm = HashAlgorithmProvider.OpenAlgorithm(algName);
            IBuffer vector = CryptographicBuffer.GenerateRandom(50);

            IBuffer digest = Algorithm.HashData(vector);

            return CryptographicBuffer.EncodeToHexString(digest);
        }

        public byte[] GetTrailer(uint refRoot, uint refInfo, out int size)
        {
            string trailer = null;
            string infoDictionary = null;

            if (refInfo > 0)
            {
                infoDictionary = string.Format("/Info {0} 0 R", refInfo);
            }

            ObjectList objList = (ObjectList)XrefEntries.OffsetArray[0];

            string md5 = CreateHashCryptographicHash();

            trailer = string.Format("trailer\n<</Size {0}/Root {1} 0 R {2}/ID[<{4}><{4}>]>>\nstartxref\n{3}\n%%EOF\n", _tableEntryCount, refRoot, infoDictionary, objList.Offset, md5);

            XrefEntries.OffsetArray = null;
            PdfObject.CurrentObjectNum = 0;

            return GetUTF8Bytes(trailer, out size);
        }

        private byte[] GetUTF8Bytes(string str, out int size)
        {
            byte[] abuf = null;
            size = 0;

            byte[] ubuf = Encoding.Unicode.GetBytes(str);
            Encoding enc = Encoding.GetEncoding("Latin1");

            abuf = Encoding.Convert(Encoding.Unicode, enc, ubuf);
            size = abuf.Length;

            return abuf;
        }
    }

    public class PdfShape
    {
        protected double _lineWidth;
        protected PdfColor _color;
        protected double[] _dashArray;
        protected double _phase = 0;

        public PdfShape(double lineWidth, PdfColor color, double[] dashArray)
        {
            _lineWidth = lineWidth;
            _color = color;
            _dashArray = dashArray;
        }

        public override string ToString()
        {
            string style = "";

            if (_dashArray != null)
            {
                StringBuilder sb = new StringBuilder();

                foreach (double d in _dashArray)
                {
                    sb.AppendFormat("{0:0.00} ", d);
                }

                style = sb.ToString().Trim();
            }

            return string.Format("{0} {1} {2} RG {3} w [{4}] {5} d", _color.Red, _color.Green, _color.Blue, _lineWidth, style, _phase);
        }
    }

    public class PdfText
    {
        PdfColor _color;
        double _x = 0;
        double _y = 0;
        double _alignX = 0;
        double _alignY = 0;
        double _size = 1;
        double _spacing = 1;
        double _rotate = 0;

        PdfAlign _alignment;

        string _text;
        string _font;

        public PdfText(double x, double y, double size, double spacing, double rotate, string font, PdfAlign align, PdfColor color, string text)
        {
            _x = x;
            _y = y;
            _alignX = x;
            _alignY = y;

            _size = size;
            _spacing = spacing;
            _rotate = rotate;
            _alignment = align;
            _font = font;
            _color = color;
            _text = text;
        }

        public PdfText(double x, double y, double ax, double ay, double size, double spacing, string font, PdfAlign align, PdfColor color, string text)
        {
            _x = x;
            _y = y;
            _alignX = ax;
            _alignY = ay;

            _size = size;
            _spacing = spacing;
            _alignment = align;
            _font = font;
            _color = color;
            _text = text;

            double dx = _alignX - _x;
            double dy = _alignY - _y;
            _rotate = Math.Atan2(dy, dx);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string fontName = "Courier";

            if (FontIndex.Font.ContainsKey(_font))
            {
                fontName = FontIndex.Font[_font];
            }

            double sw = FontInfo.StringWidth(_text, fontName, _size) * _spacing / 100.0;

            if (_rotate == 0)
            {
                double x = _x;

                switch (_alignment)
                {
                    case (PdfAlign.Left):
                        break;

                    case (PdfAlign.Center):
                        x = (_x + _alignX) / 2 - sw / 2;
                        break;

                    case (PdfAlign.Right):
                        x = _alignX - sw;
                        break;
                }

                sb.AppendLine("BT");
                sb.AppendFormat("/{0} {1} Tf\n", _font, _size);
                sb.AppendFormat("{0} {1} Td\n", x, _y);
                sb.AppendFormat("{0} Tz\n", _spacing);
                sb.AppendFormat("{0} {1} {2} rg\n", _color.Red, _color.Green, _color.Blue);
                sb.AppendFormat("({0}) Tj\n", _text.Replace("(", @"\(").Replace(")", @"\)"));
                sb.AppendLine("ET");
            }
            else
            {
                double dx = Math.Abs(_alignX - _x);
                double dy = Math.Abs(_alignY - _y);
                double distance = Math.Sqrt(dx * dx + dy * dy);
                double d = distance - sw;

                double x = _x;
                double y = _y;

                switch (_alignment)
                {
                    case (PdfAlign.Left):
                        break;

                    case (PdfAlign.Center):
                        d /= 2;
                        x = _x + d * Math.Cos(_rotate);
                        y = _y + d * Math.Sin(_rotate);
                        break;

                    case (PdfAlign.Right):
                        x = _x + d * Math.Cos(_rotate);
                        y = _y + d * Math.Sin(_rotate);
                        break;
                }

                sb.AppendLine("q");
                sb.AppendFormat("{2:0.000000} {3:0.000000} {4:0.000000} {2:0.000000} {0} {1} cm\n", x, y, Math.Cos(_rotate), Math.Sin(_rotate), Math.Sin(-_rotate));

                sb.AppendLine("BT");
                sb.AppendFormat("/{0} {1} Tf\n", _font, _size);
                sb.AppendFormat("{0} {1} Td\n", 0, 0);
                sb.AppendFormat("{0} Tz\n", _spacing);
                sb.AppendFormat("{0} {1} {2} rg\n", _color.Red, _color.Green, _color.Blue);
                sb.AppendFormat("({0}) Tj\n", _text.Replace("(", @"\(").Replace(")", @"\)"));
                sb.AppendLine("ET");
                sb.AppendLine("Q");
            }

            return sb.ToString();
        }
    }

    public class PdfLine : PdfShape
    {
        double _sx;
        double _sy;
        double _ex;
        double _ey;

        public PdfLine(double sx, double sy, double ex, double ey, double lineWidth, PdfColor color, double[] dashArray)
            : base(lineWidth, color, dashArray)
        {
            _sx = sx;
            _sy = sy;
            _ex = ex;
            _ey = ey;
        }

        public override string ToString()
        {
            if (_dashArray != null)
            {
                double pds = 0;

                foreach (double d in _dashArray)
                {
                    pds += d;
                }
                double lds = Construct.Distance(new Point(_sx, _sy), new Point(_ex, _ey));

                if (lds < pds)
                {
                    double s = lds / pds;
                    double[] sda = new double[_dashArray.Length];

                    for (int i = 0; i < _dashArray.Length; i++)
                    {
                        sda[i] = _dashArray[i] * s;
                    }

                    _dashArray = sda;
                    _phase = sda[0] / 2;
                }
                else
                {
                    double r = Math.Ceiling(lds / pds);
                    double s = lds / (r * pds);
                    double[] sda = new double[_dashArray.Length];

                    for (int i = 0; i < _dashArray.Length; i++)
                    {
                        sda[i] = _dashArray[i] * s;
                    }

                    _dashArray = sda;
                    _phase = sda[0] / 2;
                }
            }

            string attr = base.ToString();
            return string.Format("{0} {1} {2} m {3} {4} l S\n", attr, Math.Round(_sx, 3), Math.Round(_sy, 3), Math.Round(_ex, 3), Math.Round(_ey, 3));
        }
    }

    public class PdfPolyline : PdfShape
    {
        protected List<Point> _points;

        public PdfPolyline(List<Point> points, double lineWidth, PdfColor color, double[] dashArray)
            : base(lineWidth, color, dashArray)
        {
            _points = points;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(base.ToString());
            sb.AppendFormat("{0} {1} m\n", Math.Round(_points[0].X, 3), Math.Round(_points[0].Y, 3));

            for (int i = 1; i < _points.Count; i++)
            {
                sb.AppendFormat("{0} {1} l ", Math.Round(_points[i].X, 3), Math.Round(_points[i].Y, 3));
            }

            if (_points[0].X == _points[_points.Count - 1].X && _points[0].Y == _points[_points.Count - 1].Y)
            {
                sb.AppendLine("s");
            }
            else
            {
                sb.AppendLine("S");
            }

            return sb.ToString();
        }
    }

    public class PdfPolygon : PdfPolyline
    {
        PdfColor _fillColor;
        bool _fillEvenOdd;

        public PdfPolygon(List<Point> points, double lineWidth, PdfColor color, PdfColor fillColor, double[] dashArray, bool fillEvenOdd)
            : base(points, lineWidth, color, dashArray)
        {
            _fillColor = fillColor;
            _fillEvenOdd = fillEvenOdd;
        }

        public override string ToString()
        {
            string attribs = base.ToString();

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0} {1} {2} rg ", _fillColor.Red, _fillColor.Green, _fillColor.Blue);

            sb.Append(attribs);
            sb.AppendFormat("{0} {1} m\n", Math.Round(_points[0].X, 3), Math.Round(_points[0].Y, 3));

            for (int i = 1; i < _points.Count; i++)
            {
                sb.AppendFormat("{0} {1} l ", Math.Round(_points[i].X, 3), Math.Round(_points[i].Y, 3));
            }

            sb.AppendLine(_fillEvenOdd ? "b*" : "b");
             
            return sb.ToString();
        }
    }
    public class PdfPolygonList : PdfShape
    {
        PdfColor _fillColor;
        bool _fillEvenOdd;
        List<List<Point>> _figures;

        public PdfPolygonList(List<List<Point>> list, double lineWidth, PdfColor color, PdfColor fillColor, double[] dashArray, bool fillEvenOdd)
            : base(lineWidth, color, dashArray)
        {
            _fillColor = fillColor;
            _fillEvenOdd = fillEvenOdd;
            _figures = list;
        }

        public override string ToString()
        {
            string attribs = base.ToString();

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0} {1} {2} rg ", _fillColor.Red, _fillColor.Green, _fillColor.Blue);

            sb.AppendLine(attribs);

            foreach (List<Point> pc in _figures)
            {
                sb.AppendFormat("{0} {1} m\n", Math.Round(pc[0].X, 3), Math.Round(pc[0].Y, 3));

                for (int i = 1; i < pc.Count; i++)
                {
                    sb.AppendFormat("{0} {1} l ", Math.Round(pc[i].X, 3), Math.Round(pc[i].Y, 3));
                }
            }

            sb.AppendLine(_fillEvenOdd ? "b*" : "b");

            foreach (List<Point> pc in _figures)
            {
                sb.AppendFormat("{0} {1} m\n", Math.Round(pc[0].X, 3), Math.Round(pc[0].Y, 3));

                for (int i = 1; i < pc.Count; i++)
                {
                    sb.AppendFormat("{0} {1} l ", Math.Round(pc[i].X, 3), Math.Round(pc[i].Y, 3));
                }

                if (pc[0].X == pc[pc.Count - 1].X && pc[0].Y == pc[pc.Count - 1].Y)
                {
                    sb.AppendLine("s");
                }
                else
                {
                    sb.AppendLine("S");
                }
            }

            return sb.ToString();
        }
    }

    public class PdfRectangle : PdfShape
    {
        double _x;
        double _y;
        double _width;
        double _height;

        bool _isFilled = false;
        PdfColor _fillColor;

        public PdfRectangle(double x, double y, double width, double height, double lineWidth, PdfColor color, double[] dashArray)
            : base(lineWidth, color, dashArray)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        public PdfRectangle(double x, double y, double width, double height, double lineWidth, PdfColor strokeColor, PdfColor fillColor, double[] dashArray)
            : base(lineWidth, strokeColor, dashArray)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
            _fillColor = fillColor;
            _isFilled = true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (_isFilled)
            {
                sb.AppendFormat("{0} {1} {2} rg ", _fillColor.Red, _fillColor.Green, _fillColor.Blue);
            }

            sb.AppendLine(base.ToString());
            sb.AppendFormat("{0} {1} {2} {3} re {4}\n", Math.Round(_x, 3), Math.Round(_y, 3), Math.Round(_width, 3), Math.Round(_height, 3), _isFilled ? "b" : "S");

            return sb.ToString();
        }
    }

    public class PdfCircle : PdfShape
    {
        double _cx;
        double _cy;
        double R;

        public PdfCircle(double cx, double cy, double radius, double lineWidth, PdfColor color, double[] dashArray)
            : base(lineWidth, color, dashArray)
        {
            _cx = cx;
            _cy = cy;
            R = radius;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            double h = 0.5522422 * R; // sine of 33 deg 31 sec

            sb.AppendFormat("{0} {1} {2} m\n", base.ToString(), _cx, _cy - R);
            sb.AppendFormat("{0} {1} {2} {3} {4} {5} c\n", _cx + h, _cy - R, _cx + R, _cy - h, _cx + R, _cy);
            sb.AppendFormat("{0} {1} {2} {3} {4} {5} c\n", _cx + R, _cy + h, _cx + h, _cy + R, _cx + 0, _cy + R);
            sb.AppendFormat("{0} {1} {2} {3} {4} {5} c\n", _cx - h, _cy + R, _cx - R, _cy + h, _cx - R, _cy);
            sb.AppendFormat("{0} {1} {2} {3} {4} {5} c\n", _cx - R, _cy - h, _cx - h, _cy - R, _cx + 0, _cy - R);
            sb.AppendLine("s");

            return sb.ToString();
        }
    }

    public class PdfImage
    {
        double _x;
        double _y;
        double _width;
        double _height;
        string _gs;
        string _imageName;
        Matrix _matrix = Matrix.Identity;

        public PdfImage(string imageName, double x, double y, double width, double height, Matrix matrix, string gs)
        {
            _imageName = imageName;
            _x = x;
            _y = y;
            _width = width;
            _height = height;

            _gs = gs;

            _matrix = matrix;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("q\n");
            sb.AppendFormat("{0} 0 0 {1} {2} {3} cm\n", 1, 1, Math.Round(_x, 3), Math.Round(_y, 3));
            sb.AppendFormat("{0} {1} {2} {3} {4} {5} cm\n", Math.Round(_matrix.M11, 4), Math.Round(-_matrix.M12, 4), Math.Round(-_matrix.M21, 4), Math.Round(_matrix.M22, 4), Math.Round(_matrix.OffsetX, 3), Math.Round(_matrix.OffsetY, 3));
            sb.AppendFormat("{0} 0 0 {1} {2} {3} cm\n", Math.Round(_width, 3), Math.Round(_height, 3), 0, -Math.Round(_height, 3));
        
            if (string.IsNullOrEmpty(_gs) == false)
            {
                sb.Append(_gs);
            }

            sb.AppendFormat("/{0} Do\nQ\n", _imageName);

            return sb.ToString();
        }

	    ////////////////////////////////////////////////////////////////////
	    // Compress byte array
	    ////////////////////////////////////////////////////////////////////
	
	    protected Byte[] CompressStream(Byte[] InputBuf)
        {
		    // input length
		    Int32 inputLen = InputBuf.Length;
            Byte[] outputBuf;
            Int32 outputLen;

		    // input buffer too small to compress
		    if(inputLen < 16) return(null);

		    // create output memory stream to receive the compressed buffer
		    using (MemoryStream OutputStream = new MemoryStream())
            {
                // deflate compression object
                DeflateStream Deflate = new DeflateStream(OutputStream, CompressionMode.Compress, true);

                // load input buffer into the compression class
                Deflate.Write(InputBuf, 0, InputBuf.Length);

                // compress, flush and close
                //Deflate.Close();

                // compressed file length
                outputLen = (Int32)OutputStream.Length;

                // make sure compressed stream is shorter than input stream
                if (outputLen + 6 >= inputLen) return (null);

                // create output buffer
                outputBuf = new Byte[outputLen + 6];

                // write two bytes in most significant byte first
                outputBuf[0] = (Byte)0x78;
                outputBuf[1] = (Byte)0x9c;

                // copy the compressed result
                OutputStream.Seek(0, SeekOrigin.Begin);
                OutputStream.Read(outputBuf, 2, outputLen);
                //OutputStream.Close();
            }

		    // reset adler32 checksum
		    UInt32 ReadAdler32 = Adler32Checksum(InputBuf);

		    // ZLib checksum is Adler32 write it big endian order, high byte first
		    outputLen += 2;
		    outputBuf[outputLen++] = (Byte) (ReadAdler32 >> 24);
		    outputBuf[outputLen++] = (Byte) (ReadAdler32 >> 16);
		    outputBuf[outputLen++] = (Byte) (ReadAdler32 >> 8);
		    outputBuf[outputLen] = (Byte) ReadAdler32;

		    // successful exit
		    return(outputBuf);
        }

	    /////////////////////////////////////////////////////////////////////
	    // Accumulate Adler Checksum
	    /////////////////////////////////////////////////////////////////////

	    private UInt32 Adler32Checksum(Byte[] Buffer)
        {
		    const UInt32 Adler32Base = 65521;

		    // split current Adler checksum into two 
		    UInt32 AdlerLow = 1; // AdlerValue & 0xFFFF;
		    UInt32 AdlerHigh = 0; // AdlerValue >> 16;

		    Int32 Len = Buffer.Length;
		    Int32 Pos = 0;
		    while(Len > 0) 
            {
			    // We can defer the modulo operation:
			    // Under worst case the starting value of the two halves is 65520 = (AdlerBase - 1)
			    // each new byte is maximum 255
			    // The low half grows AdlerLow(n) = AdlerBase - 1 + n * 255
			    // The high half grows AdlerHigh(n) = (n + 1)*(AdlerBase - 1) + n * (n + 1) * 255 / 2
			    // The maximum n before overflow of 32 bit unsigned integer is 5552
			    // it is the solution of the following quadratic equation
			    // 255 * n * n + (2 * (AdlerBase - 1) + 255) * n + 2 * (AdlerBase - 1 - UInt32.MaxValue) = 0
			    Int32 n = Len < 5552 ? Len : 5552;
			    Len -= n;
			    while(--n >= 0) 
                {
				    AdlerLow += (UInt32) Buffer[Pos++];
				    AdlerHigh += AdlerLow;
                }
			    AdlerLow %= Adler32Base;
			    AdlerHigh %= Adler32Base;
            }
		    return((AdlerHigh << 16) | AdlerLow);
		}
    }
}
