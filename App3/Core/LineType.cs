using Cirros.Primitives;
using System.Collections.Generic;
using System.Xml.Serialization;
#if UWP
using Microsoft.UI.Xaml.Media;
#else
using System.Windows.Media;
#endif

namespace Cirros.Drawing
{
    public class LineType
    {
        int _id;
        protected string _name = "";
        protected DoubleCollection _strokeDashArray;

        public LineType(int id, string name, DoubleCollection strokeDashArray)
        {
            _id = id;
            _name = name;
            _strokeDashArray = strokeDashArray;
        }

        public LineType(LineType type)
        {
            _id = type._id;
            _name = type._name;

            if (type._strokeDashArray == null)
            {
                _strokeDashArray = null;
            }
            else
            {
                _strokeDashArray = new DoubleCollection();

                foreach (double d in type._strokeDashArray)
                {
                    _strokeDashArray.Add(d);
                }
            }
        }

        public LineType()
        {
        }

        public int Id
        {
            get
            {
                return _id;

            }
            set
            {
                _id = value;
            }
        }

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

        [XmlIgnore]
        public DoubleCollection StrokeDashArray
        {
            get
            {
                if (_strokeDashArray == null)
                {
                    return null;
                }
                return _strokeDashArray;
            }
            set
            {
                _strokeDashArray = value;
            }
        }

        public float[] DashList
        {
            get
            {
                float[] dashList = null;

                if (_strokeDashArray != null)
                {
                    dashList = new float[_strokeDashArray.Count];

                    for (int i = 0; i < _strokeDashArray.Count; i++)
                    {
                        dashList[i] = (float)_strokeDashArray[i];
                    }
                }
                return dashList;
            }
            set
            {
                if (_strokeDashArray == null)
                {
                    _strokeDashArray = new DoubleCollection();
                }
                else
                {
                    _strokeDashArray.Clear();
                }

                foreach (float f in value)
                {
                    _strokeDashArray.Add((double)f);
                }
            }
        }

        public override string ToString()
        {
            return _name;
        }

        public static int GetContainingLayerCount(int lineTypeId)
        {
            int count = 0;

            foreach (Layer layer in Globals.LayerTable.Values)
            {
                if (layer.LineTypeId == lineTypeId)
                {
                    count++;
                }
            }

            return count;
        }

        public static int GetInstanceCount(int lineTypeId)
        {
            int count = 0;

            Dictionary<int, Layer> layers = new Dictionary<int, Layer>();
            foreach (Layer l in Globals.LayerTable.Values)
            {
                if (l.LineTypeId == lineTypeId)
                {
                    layers.Add(l.Id, l);
                }
            }

            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                if (p is PInstance)
                {
                    if (((PInstance)p).ContainsLineType(lineTypeId))
                    {
                        count++;
                    }
                }
                else if (p.LineTypeId == lineTypeId)
                {
                    count++;
                }
                else if (p.LineTypeId == -1 && layers.ContainsKey(p.LayerId))
                {
                    count++;
                }
            }

            return count;
        }

        public static void PropagateLineTypeChanges(int lineTypeId)
        {
#if UWP
            Globals.View.VectorListControl.UpdateLineStyles();
#endif

            Dictionary<int, Layer> layers = new Dictionary<int, Layer>();
            foreach (Layer l in Globals.LayerTable.Values)
            {
                if (l.LineTypeId == lineTypeId)
                {
                    layers.Add(l.Id, l);
                }
            }

            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                if (p is PInstance)
                {
                    if (((PInstance)p).ContainsLineType(lineTypeId))
                    {
                        p.LineTypeChanged();
                        p.Draw();
                    }
                }
                else if (p.LineTypeId == lineTypeId)
                {
                    p.LineTypeChanged();
                    p.Draw();
                }
                else if (p.LineTypeId == -1 && layers.ContainsKey(p.LayerId))
                {
                    p.LayerChanged();
                    p.Draw();
                }
            }
        }
    }
}
