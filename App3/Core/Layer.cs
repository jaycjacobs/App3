using Cirros.Primitives;
using System;

namespace Cirros.Drawing
{
    public class Layer : IComparable
    {
        private string _name;
        private bool _isVisible = true;

        private int _id;
        private uint _colorSpec;
        private int _lineTypeId;
        private int _lineWeightId;

        public Layer()
        {
        }

        public Layer(int id, string name, uint colorSpec, int lineType, int lineWeight)
        {
            _id = id;
            _name = name;
            _colorSpec = colorSpec;
            _lineTypeId = lineType;
            _lineWeightId = lineWeight;
        }

        public Layer(Layer layer)
        {
            _id = layer._id;
            _name = layer._name;
            _colorSpec = layer._colorSpec;
            _lineTypeId = layer.LineTypeId;
            _lineWeightId = layer.LineWeightId;
        }

        public override string ToString()
        {
            return _name;
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

        public uint ColorSpec
        {
            get
            {
                return _colorSpec;
            }
            set
            {
                _colorSpec = value;
            }
        }

        public int LineTypeId
        {
            get
            {
                return _lineTypeId;
            }
            set
            {
                _lineTypeId = value;
            }
        }

        public int LineWeightId
        {
            get
            {
                return _lineWeightId;
            }
            set
            {
                _lineWeightId = value;
            }
        }

        public bool Visible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                _isVisible = value;
            }
        }

        public static void PropagateLayerChanges(int layerId)
        {
            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                if (p is PInstance)
                {
                    if (((PInstance)p).ContainsLayer(layerId))
                    {
                        p.LayerChanged();
                        p.Draw();
                    }
                }
                else if (p != null)
                {
                    if (p.LayerId == layerId)
                    {
                        p.LayerChanged();
                        p.Draw();
                    }
                }
            }
        }

        public int CompareTo(object obj)
        {
            return _name.CompareTo(((Layer)obj).Name);
        }
    }
}
