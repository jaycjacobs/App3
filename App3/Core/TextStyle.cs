
using Cirros.Primitives;

namespace Cirros.Drawing
{
    public class TextStyle
    {
        int _id = 0;
        protected string _name = "";
        protected string _font = "";
        protected double _size = 1;
        protected double _offset = .5;
        protected double _lineSpacing = 1.8;
        protected double _characterSpacing = 1;

        public TextStyle(int id, string name, string font, double size, double offset, double lineSpacing, double characterSpacing)
        {
            _id = id;
            _name = name;
            _font = font;
            _size = size;
            _offset = offset;
            _lineSpacing = lineSpacing;
            _characterSpacing = characterSpacing;
        }

        public TextStyle()
        {
        }

        public TextStyle(TextStyle style)
        {
            _id = style._id;
            _name = style._name;
            _font = style._font;
            _size = style._size;
            _offset = style._offset;
            _lineSpacing = style._lineSpacing;
            _characterSpacing = style._characterSpacing;
        }

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Font
        {
            get { return _font; }
            set { _font = value; }
        }

        public double Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public double Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public double Spacing
        {
            get { return _lineSpacing; }
            set { _lineSpacing = value; }
        }

        public double CharacterSpacing
        {
            get { return _characterSpacing; }
            set { _characterSpacing = value; }
        }

        public override string ToString()
        {
            return _name;
        }

        public static int GetInstanceCount(int textStyleId)
        {
            int count = 0;

            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                if (p is PText)
                {
                    if (((PText)p).TextStyleId == textStyleId)
                    {
                        count++;
                    }
                }
                else if (p is PDimension)
                {
                    if (((PDimension)p).TextStyleId == textStyleId)
                    {
                        count++;
                    }
                }
                else if (p is PInstance)
                {
                    if (((PInstance)p).ContainsTextStyle(textStyleId))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public static void PropagateTextStyleChanges(int textStyleId)
        {
            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                if (p is PText)
                {
                    if (((PText)p).TextStyleId == textStyleId)
                    {
                        //p.TextStyleChanged();
                        p.Draw();
                    }
                }
                else if (p is PDimension)
                {
                    if (((PDimension)p).TextStyleId == textStyleId)
                    {
                        //p.TextStyleChanged();
                        p.Draw();
                    }
                }
                else if (p is PInstance)
                {
                    if (((PInstance)p).ContainsTextStyle(textStyleId))
                    {
                        //p.TextStyleChanged();
                        p.Draw();
                    }
                }
            }
        }
    }
}
