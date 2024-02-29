using Cirros.Primitives;

namespace Cirros.Drawing
{
    public class ArrowStyle
    {
        protected int _id;
        protected string _name;
        protected ArrowType _type;
        protected double _size;
        protected double _aspect;

        public ArrowStyle(int id, string name, ArrowType type, double size, double aspect)
        {
            _id = id;
            _name = name;
            _type = type;
            _size = size;
            _aspect = aspect;
        }

        public ArrowStyle(ArrowStyle style)
        {
            _id = style._id;
            _name = style._name;
            _type = style._type;
            _size = style._size;
            _aspect = style._aspect;
        }

        public ArrowStyle()
        {
            if (Globals.ArrowStyleTable.ContainsKey(0))
            {
                ArrowStyle style = Globals.ArrowStyleTable[0];

                _id = style._id;
                _name = style._name;
                _type = style._type;
                _size = style._size;
                _aspect = style._aspect;
            }
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

        public ArrowType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public double Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public double Aspect
        {
            get { return _aspect; }
            set { _aspect = value; }
        }

        public override string ToString()
        {
            return _name;
        }

        public static int GetInstanceCount(int arrowStyleId)
        {
            int count = 0;

            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                if (p is PArrow)
                {
                    if (((PArrow)p).ArrowStyleId == arrowStyleId)
                    {
                        count++;
                    }
                }
                else if (p is PDimension)
                {
                    if (((PDimension)p).ArrowStyleId == arrowStyleId)
                    {
                        count++;
                    }
                }
                else if (p is PInstance)
                {
                    if (((PInstance)p).ContainsArrowStyle(arrowStyleId))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public static void PropagateArrowStyleChanges(int arrowStyleId)
        {
            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                if (p is PArrow)
                {
                    if (((PArrow)p).ArrowStyleId == arrowStyleId)
                    {
                        p.Draw();
                    }
                }
                else if (p is PDimension)
                {
                    if (((PDimension)p).ArrowStyleId == arrowStyleId)
                    {
                        p.Draw();
                    }
                }
                else if (p is PInstance)
                {
                    if (((PInstance)p).ContainsArrowStyle(arrowStyleId))
                    {
                        p.Draw();
                    }
                }
            }
        }
    }
}
