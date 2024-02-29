using Cirros;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KT22
{
    public class KTUtilities
    {
        public static Dictionary<string, object> QueryPrimitive(Primitive p)
        {
            Dictionary<string, object> propertyBlock = new Dictionary<string, object>();

            propertyBlock["type"] = p.TypeName.ToString();

            if (Globals.LayerTable.ContainsKey(p.LayerId))
            {
                Layer layer = Globals.LayerTable[p.LayerId];
                propertyBlock["layer"] = layer.Name;
            }

            switch (p.TypeName)
            {
                case PrimitiveType.Arc:
                case PrimitiveType.Arc3:
                case PrimitiveType.Arrow:
                //case PrimitiveType.Bezier:
                case PrimitiveType.BSpline:
                case PrimitiveType.Dimension:
                case PrimitiveType.Doubleline:
                case PrimitiveType.Ellipse:
                case PrimitiveType.Line:
                case PrimitiveType.Polygon:
                case PrimitiveType.Rectangle:
                    {
                        propertyBlock["thickness"] = p.LineWeightId < 1 ? "by layer" : ((double)(p.LineWeightId / 1000)).ToString();
                        propertyBlock["color"] = Utilities.ColorNameFromColorSpec(p.ColorSpec);
                    }
                    break;

                case PrimitiveType.Image:
                case PrimitiveType.Instance:
                    break;

                case PrimitiveType.Text:
                    break;
            }

            switch (p.TypeName)
            {
                case PrimitiveType.Arc:
                case PrimitiveType.Arc3:
                case PrimitiveType.Arrow:
                //case PrimitiveType.Bezier:
                case PrimitiveType.BSpline:
                case PrimitiveType.Dimension:
                case PrimitiveType.Doubleline:
                case PrimitiveType.Ellipse:
                case PrimitiveType.Line:
                case PrimitiveType.Polygon:
                case PrimitiveType.Rectangle:
                    break;

                case PrimitiveType.Image:
                case PrimitiveType.Instance:
                    break;

                case PrimitiveType.Text:
                    break;
            }


            return propertyBlock;
        }
    }
}
