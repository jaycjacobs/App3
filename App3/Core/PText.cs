using Cirros.Drawing;
using Cirros.Display;
using Cirros.Utility;
using System.Text.RegularExpressions;
#if UWP
using Windows.Foundation;
using Cirros.Actions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
#else
using CirrosCore;
using System.Windows;
using System.Windows.Media;
#endif

namespace Cirros.Primitives
{
    public enum TextPosition
    {
        Above = 0,
        On,
        Below
    }

    public class PText : Primitive
    {
        protected double _angle;
        protected string _text;
        protected int _textstyleid;
        protected string _fontName = "";

        protected string _attributeName = null;
        string _attributeValue = null;

        protected double _size = 0;
        protected double _charSpacing = 0;
        protected double _lineSpacing = 0;

        protected TextAlignment _hAlign;
        protected TextPosition _vAlign;

        protected double _sx;
        protected double _sy;
        protected double _ex;
        protected double _ey;
#if UWP
        private double _baselineOffset = 0;
        private VectorEntity _rendered_ve = null;
#endif

        public PText(Point s, double angle, int textstyle, TextAlignment hAlign, TextPosition vAlign, string text)
            : base(s)
        {
            //_layerId = Globals.TextLayerId;
            _layerId = ActiveLayer;

            _angle = angle;
            _size = Globals.TextHeight;
            _charSpacing = Globals.TextSpacing;
            _lineSpacing = Globals.TextLineSpacing;
            _fontName = Globals.TextFont;
            _textstyleid = textstyle;
            _hAlign = hAlign;
            _vAlign = vAlign;
            _text = text;

            _sx = _ex = 0;
            _sy = _ey = 0;
        }

        public PText(Point s, Point e, int textstyle, TextAlignment hAlign, TextPosition vAlign, string text)
            : base(s)
        {
            //_layerId = Globals.TextLayerId;
            _layerId = ActiveLayer;

            _textstyleid = textstyle;
            _hAlign = hAlign;
            _vAlign = vAlign;
            _text = text;
            _angle = Globals.TextAngle;
            _size = Globals.TextHeight;
            _charSpacing = Globals.TextSpacing;
            _lineSpacing = Globals.TextLineSpacing;
            _fontName = Globals.TextFont;

            _sx = 0;
            _sy = 0;
            _ex = e.X - s.X;
            _ey = e.Y - s.Y;
        }

        public PText(PText original)
            : base(original)
        {
            _angle = original._angle;
            _size = original._size;
            _charSpacing = original._charSpacing;
            _lineSpacing = original._lineSpacing;
            _fontName = original._fontName;
            _textstyleid = original._textstyleid;
            _hAlign = original._hAlign;
            _vAlign = original._vAlign;
            _text = original._text;
            _sx = original._sx;
            _attributeName = original._attributeName;
            _attributeValue = original._attributeValue;
            _attributeLines = original._attributeLines;
            _sy = original._sy;
            _ex = original._ex;
            _ey = original._ey;
        }

        // The unescaped regex pattern is {{([^:]+):([^{}]*)({(\d+)})*}}
        static string _attrregex = Regex.Escape("{{") + "([^:]+):([^" + Regex.Escape("{}") + ">]*)(" + Regex.Escape("{") + @"(\d+)" + Regex.Escape("}") + ")*" + Regex.Escape("}}");

        public static GroupAttribute ResolveAttributeString(string text)
        {
            GroupAttribute ga = new GroupAttribute();

            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(text, _attrregex);
            if (match.Success)
            {
                if (match.Groups != null)
                {
                    if (match.Groups.Count > 1)
                    {
                        ga.Prompt = match.Groups[1].Value;
                    }
                    if (match.Groups.Count > 2)
                    {
                        ga.Value = match.Groups[2].Value;
                    }
                    if (match.Groups.Count > 4)
                    {
                        try
                        {
                            ga.MaxLines = int.Parse(match.Groups[4].Value);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return ga;
        }

        public PText(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            _angle = (double)e.Angle;
            _size = (double)e.Height;
            _charSpacing = (double)e.HorizontalSpacing;
            _lineSpacing = (double)e.VerticalSpacing;
            _textstyleid = (int)e.TextAttributes.TextStyleId;
            _hAlign = e.TextAttributes.TextAlignment;
            _vAlign = e.TextAttributes.TextPosition;
            _text = e.TextAttributes.Text;

            if (string.IsNullOrEmpty(e.Font) == false)
            {
                _fontName = (string)e.Font;
            }

            Point start = _container.ModelToPaperDelta(e.Points[0]);
            Point end = _container.ModelToPaperDelta(e.Points[1]);
            _sx = start.X;
            _sy = start.Y;
            _ex = end.X;
            _ey = end.Y;

            if (_text.StartsWith("{{") && _text.EndsWith("}}"))
            {
                GroupAttribute ga = ResolveAttributeString(_text);
                _attributeLines = ga.MaxLines;
                _attributeName = ga.Prompt;
                _attributeValue = ga.Value;
            }
        }

        public override Entity Serialize()
        {
            Entity e = base.Serialize();

            e.Angle = PrimitiveUtilities.SerializeDoubleAsFloat(_angle);
            e.Height = PrimitiveUtilities.SerializeDoubleAsFloat(_size);
            e.HorizontalSpacing = PrimitiveUtilities.SerializeDoubleAsFloat(_charSpacing);
            e.VerticalSpacing = PrimitiveUtilities.SerializeDoubleAsFloat(_lineSpacing);
            e.SetTextAttributes(_hAlign, _vAlign, _textstyleid, _text);

            if (string.IsNullOrEmpty(_fontName) == false)
            {
                e.Font = _fontName;
            }

            e.AddPoint(_container.PaperToModelDeltaF(new Point(_sx, _sy)));
            e.AddPoint(_container.PaperToModelDeltaF(new Point(_ex, _ey)));

            return e;
        }

        public override bool IsGroupMember
        {
            get
            {
                return base.IsGroupMember;
            }
            set
            {
                if (value)
                {
                    if (_text.StartsWith("{{") && _text.EndsWith("}}"))
                    {
                        GroupAttribute ga = ResolveAttributeString(_text);
                        _attributeLines = ga.MaxLines;
                        _attributeName = ga.Prompt;
                        _attributeValue = ga.Value;
                    }
                }
                base.IsGroupMember = value;
            }
        }

        public int TextStyleId
        {
            get
            {
                return _textstyleid;
            }
            set
            {
                _textstyleid = value;
            }
        }

        public double Angle
        {
            get
            {
                return _angle;
            }
            set
            {
                _angle = value;
            }
        }

        public double Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }

        public double CharacterSpacing
        {
            get
            {
                return _charSpacing;
            }
            set
            {
                _charSpacing = value;
            }
        }

        public double LineSpacing
        {
            get
            {
                return _lineSpacing;
            }
            set
            {
                _lineSpacing = value;
            }
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Text;
            }
        }

        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value.Replace("\r\n", "\n");
                _text = value.Replace("\r", "\n");

                while (_text.EndsWith("\n") || _text.EndsWith("\r"))
                {
                    _text = _text.Remove(_text.Length - 1);
                }

                _attributeName = _attributeValue = null;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public string AttributeName
        {
            get
            {
                return _attributeName;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public string AttributeValue
        {
            get
            {
                return _attributeValue;
            }
            set
            {
                _attributeValue = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public int AttributeLines
        {
            get
            {
                return _attributeLines;
            }
        }

        public TextAlignment Alignment
        {
            get
            {
                return _hAlign;
            }
            set
            {
                _hAlign = value;
            }
        }

        public TextPosition Position
        {
            get
            {
                return _vAlign;
            }
            set
            {
                _vAlign = value;
            }
        }

        public Point P1
        {
            get
            {
                return new Point(_origin.X + _sx, _origin.Y + _sy);
            }
        }

        public Point P2
        {
            get
            {
                return new Point(_origin.X + _ex, _origin.Y + _ey);
            }
        }

        public override int ActiveLayer
        {
            get
            {
                if (Globals.UIVersion == 0)
                {
                    return Globals.TextLayerId;
                }
                else
                {
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveTextLayerId))
                    {
                        return Globals.ActiveTextLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        public override Primitive Clone()
        {
            return new PText(this);
        }

        public override bool Normalize(bool undoable)
        {
            bool undid = false;

            if (IsTransformed)
            {
#if UWP
                if (undoable)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.UnNormalize, this, _matrix);
                    undid = true;
                }
#else
#endif

                Point s = _matrix.Transform(new Point(_sx, _sy));
                Point e = _matrix.Transform(new Point(_ex, _ey));

                _sx = s.X;
                _sy = s.Y;
                _ex = e.X;
                _ey = e.Y;

                _matrix = CGeometry.IdentityMatrix();
            }

            return undid;
        }

        public override bool UnNormalize(Matrix m)
        {
            if (m.IsIdentity == false)
            {
                Matrix inverse = CGeometry.InvertMatrix(m);

                Point s = inverse.Transform(new Point(_sx, _sy));
                Point e = inverse.Transform(new Point(_ex, _ey));

                _sx = s.X;
                _sy = s.Y;
                _ex = e.X;
                _ey = e.Y;

                _matrix = m;
            }
            return true;
        }

        //public override void MoveTo(double x, double y)
        //{
        //    _origin = new Point(x, y);
        //}

        //public override void MoveByDelta(double dx, double dy)
        //{
        //    ClearStaticConstructNodes();

        //    _origin.X += dx;
        //    _origin.Y += dy;

        //    //Draw();
        //}

        protected override void _drawHandles(Handles handles)
        {
            handles.Attach(this);
            handles.AddHandle(2, _ex + _origin.X, _ey + _origin.Y, HandleType.Diamond);
            handles.AddHandle(1, _sx + _origin.X, _sy + _origin.Y, HandleType.Triangle);

            if (_ex != _sx || _ey != _sy)
            {
                handles.ArrowFrom = 1;
                handles.ArrowTo = 2;
            }

            handles.Draw();
        }

        public override CPoint GetHandlePoint(int handleId)
        {
            CPoint handlePoint = new CPoint();

            if (handleId == 1)
            {
                handlePoint.Point = new Point(_origin.X + _sx, _origin.Y + _sy);
            }
            else if (handleId == 2)
            {
                handlePoint.Point = new Point(_origin.X + _ex, _origin.Y + _ey);
            }

            return handlePoint;
        }

        protected override void _moveHandle(Handles handles, int id, double dx, double dy)
        {
            ClearStaticConstructNodes();

            if (id == 1)
            {
                _sx += dx;
                _sy += dy;
            }
            else if (id == 2)
            {
                _ex += dx;
                _ey += dy;
            }

            Point s = new Point(_sx + _origin.X, _sy + _origin.Y);
            Point e = new Point(_ex + _origin.X, _ey + _origin.Y);

            if (_hAlign == TextAlignment.Left)
            {
                _origin = s;
            }
            else if (_hAlign == TextAlignment.Center)
            {
                _origin.X = (s.X + e.X) / 2;
                _origin.Y = (s.Y + e.Y) / 2;
            }
            else
            {
                _origin = e;
            }

            _sx = s.X - _origin.X;
            _sy = s.Y - _origin.Y;
            _ex = e.X - _origin.X;
            _ey = e.Y - _origin.Y;
        }

        public override void MoveHandlePoint(int handleId, Point p)
        {
            ClearStaticConstructNodes();

            if (handleId == 1)
            {
                _sx = p.X - _origin.X;
                _sy = p.Y - _origin.Y;
            }
            else if (handleId == 2)
            {
                _ex = p.X - _origin.X;
                _ey = p.Y - _origin.Y;
            }

            Point s = new Point(_sx + _origin.X, _sy + _origin.Y);
            Point e = new Point(_ex + _origin.X, _ey + _origin.Y);

            if (_hAlign == TextAlignment.Left)
            {
                _origin = s;
            }
            else if (_hAlign == TextAlignment.Center)
            {
                _origin.X = (s.X + e.X) / 2;
                _origin.Y = (s.Y + e.Y) / 2;
            }
            else
            {
                _origin = e;
            }

            _sx = s.X - _origin.X;
            _sy = s.Y - _origin.Y;
            _ex = e.X - _origin.X;
            _ey = e.Y - _origin.Y;

            Draw();
        }

        private int _attributeLines;
#if UWP
        Size _actualSize = new Size();

        private Size MeasureText()
        {
            Size test = new Size(10000, 10000);

            TextBlock tb;

            TextStyle style = Globals.TextStyleTable[_textstyleid];
            double fontsize = _size == 0 ? style.Size : _size;

            tb = new TextBlock();
            tb.FontSize = fontsize * 1.35;  // tb.FontSize property is in (canvas) points, not pixels
            tb.LineHeight = style.Spacing * fontsize;
            tb.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;

            string ff = string.IsNullOrEmpty(_fontName) ? style.Font : _fontName;

            if (ff != null && ff.Length > 0)
            {
                try
                {
                    tb.FontFamily = new FontFamily(ff);
                }
                catch
                {
                }
            }
            tb.Text = (_isInstanceMember || _isGroupMember) && _attributeName != null ? _attributeValue : _text;

            if (tb.ActualHeight == 0)
            {
                tb.Measure(test);
            }
            if (tb.ActualHeight > 0)

            {
                _actualSize = new Size(tb.ActualWidth, tb.ActualHeight);
            }

            if (tb.BaselineOffset != 0)
            {
                _baselineOffset = tb.BaselineOffset;
            }

            return _actualSize;
        }

        public override VectorEntity Vectorize(VectorContext context)
        {
            double angle = _angle;

            _rendered_ve = base.Vectorize(context);
            string text = (_isInstanceMember || _isGroupMember) && _attributeName != null ? _attributeValue : _text;

            int key = _textstyleid;
            
            if (!Globals.TextStyleTable.ContainsKey(key))
            {
                key = 0;

                Analytics.ReportError("PText.Vectorize: Invalid textstyle key", null, 2, 408);
            }

            TextStyle style = Globals.TextStyleTable[key];
            double fontsize = _size == 0 ? style.Size : _size;
            double charSpacing = _charSpacing == 0 ? style.CharacterSpacing : _charSpacing;
            double lineSpacing = _lineSpacing == 0 ? style.Spacing : _lineSpacing;
            double psOffset = fontsize * style.Offset;
            double width = text.Length * fontsize * .6;

            Point msStartPoint = _matrix.Transform(new Point(_sx, _sy));
            Point msEndPoint = _matrix.Transform(new Point(_ex, _ey));

            if (msStartPoint.X != msEndPoint.X || msStartPoint.Y != msEndPoint.Y)
            {
                double a = Construct.Angle(msStartPoint, msEndPoint);
                angle = a * Construct.cRadiansToDegrees;
            }

            double textHeight = 0;
            double textWidth = 0;

            if (context.CanAlignText == false)
            {
                Size sz = MeasureText();
                textHeight = Globals.DrawingCanvas.DisplayToPaper(sz.Height);
                textWidth = Globals.DrawingCanvas.DisplayToPaper(sz.Width);
            }

            if (_matrix.IsIdentity == false && _size != 0)
            {
                Point h = _matrix.Transform(new Point(0, _size));
                if (h.X == 0)
                {
                    fontsize = h.Y;
                }
                else
                {
                    fontsize = Construct.Distance(new Point(), h);
                }
            }

            string ff = string.IsNullOrEmpty(_fontName) ? style.Font : _fontName;

            RotateTransform r = new RotateTransform();
            r.Angle = angle;

            Point psAlignmentOffset = new Point();
            Point pt = new Point();

            if (context.CanAlignText)
            {
                switch (_hAlign)
                {
                    case TextAlignment.Left:
                        pt = msStartPoint;
                        psAlignmentOffset.X = psOffset;
                        break;
                    case TextAlignment.Center:
                    default:
                        pt.X = (msStartPoint.X + msEndPoint.X) / 2;
                        pt.Y = (msStartPoint.Y + msEndPoint.Y) / 2;
                        break;
                    case TextAlignment.Right:
                        pt = msEndPoint;
                        psAlignmentOffset.X = -psOffset;
                        break;
                }
            }
            else
            {
                switch (_hAlign)
                {
                    case TextAlignment.Left:
                        pt = msStartPoint;
                        psAlignmentOffset.X = psOffset;
                        break;
                    case TextAlignment.Center:
                    default:
                        pt.X = (msStartPoint.X + msEndPoint.X) / 2;
                        pt.Y = (msStartPoint.Y + msEndPoint.Y) / 2;
                        psAlignmentOffset.X = -(textWidth / 2);
                        break;
                    case TextAlignment.Right:
                        pt = msEndPoint;
                        psAlignmentOffset.X = -(textWidth + psOffset);
                        break;
                }
            }

            if (context.TextOriginLowerLeft)
            {
                switch (_vAlign)
                {
                    case TextPosition.Below:
                        psAlignmentOffset.Y = fontsize + psOffset;
                        break;
                    case TextPosition.On:
                        psAlignmentOffset.Y = fontsize / 2;
                        break;
                    default:
                    case TextPosition.Above:
                        psAlignmentOffset.Y = -psOffset;
                        break;
                }
            }
            else
            {
                int lines = 0;
                int nli = text.IndexOf('\n');
                while (nli >= 0)
                {
                    lines++;
                    nli = text.IndexOf('\n', nli + 1);
                }

                switch (_vAlign)
                {
                    case TextPosition.Below:
                        psAlignmentOffset.Y = -(textHeight / 2) + (fontsize * lineSpacing * lines / 2) + psOffset;
                        break;
                    case TextPosition.On:
                    default:
                        psAlignmentOffset.Y = -(textHeight / 2);
                        break;
                    case TextPosition.Above:
                        psAlignmentOffset.Y = -(textHeight / 2) - (fontsize * lineSpacing * lines / 2) - psOffset;
                        break;
                }
            }

            Point psRotatedAlignmentOffset = Utilities.TransformPoint(r, psAlignmentOffset);

            VectorTextEntity vt = new VectorTextEntity();
            vt.Text = text;
            vt.TextHeight = fontsize;
            vt.Angle = angle;
            vt.FontFamily = ff;
            vt.TextAlignment = _hAlign;
            vt.TextPosition = _vAlign;
            vt.LineSpacing = lineSpacing;
            vt.CharacterSpacing = charSpacing;
            vt.Location = new Point(pt.X + psRotatedAlignmentOffset.X + _origin.X, pt.Y + psRotatedAlignmentOffset.Y + _origin.Y);
            vt.Origin = new Point(pt.X +_origin.X, pt.Y + _origin.Y);
            vt.PsOffset = psRotatedAlignmentOffset;
            _rendered_ve.AddChild(vt);

            return _rendered_ve;
        }
#else
#endif
    }
}
