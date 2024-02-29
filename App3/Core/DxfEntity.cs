using System.Collections.Generic;
using Windows.Foundation;

namespace Cirros.Dxf
{
    public class DxfEntity
    {
        DxfReader _reader;

        public DxfEntity(DxfReader reader)
        {
            _reader = reader;
            Color = -1;
        }

         // -1  APP: entity name (changes each time a  drawing is opened)  [not omitted]
         //  0  Entity type  [not omitted]
         //  5  Handle    [not omitted]
         //102  Start of application defined group "{application_name"  (optional) [no default]
         //           application-defined codes 
         //           Codes and values within the 102 groups are application-defined  (optional) [no default]
         //102  End of group, "}"  (optional)  [no default]
         //102  "{ACAD_REACTORS" indicates the start of the AutoCAD persistent reactors group. 
         //           This group exists only if persistent reactors have been attached to this object  (optional). [no default]
         //330  Soft pointer ID/handle to owner dictionary  (optional) [no default]
         //102  End of group, "}"  (optional) [no default]
         //102  "{ACAD_XDICTIONARY" indicates the start of an extension dictionary group. 
         //           This group exists only if persistent reactors have been attached to this object  (optional). [no default]
         //360  Hard owner ID/handle to owner dictionary  (optional)  [no default]
         //102  End of group, "}"  (optional)  [no default]
         //330  Soft-pointer ID/handle to owner BLOCK_RECORD object  [not omitted]
         //100  Subclass marker (AcDbEntity)  [not omitted]
         // 67  Absent or zero indicates entity is in model space. 1 indicates entity is in paper space (optional). [0]
         //410  APP: layout tab name   [not omitted]
         //  8  Layer name  [not omitted]
         //  6  Linetype name (present if not BYLAYER). 
         //           The special name BYBLOCK indicates a floating linetype (optional). [BYLAYER]
         // 62  Color number (present if not BYLAYER); zero indicates the BYBLOCK (floating) color; 
         //       256 indicates BYLAYER; a negative value indicates that the layer is turned off (optional).  [BYLAYER]
         // 48  Linetype scale (optional) [1.0 ]
         // 60  Object visibility (optional): 0 = Visible, 1 = Invisible [0 ]
         // 92  The number of bytes in the image (and subsequent binary chunk records) (optional) [no default]
         //310  Preview image data (multiple lines; 256 charaters max. per line) (optional) [no default]

        public string Type          = null;         //0
        public string Handle        = null;         //5
        public string SoftPointer   = null;         //330
        public string SubClass      = null;         //100
        public string LayoutTab     = null;         //410
        public int Space            = 0;            //67
        public string Layer         = null;         //8
        public string Ltype         = "BYLAYER";    //6
        public int Color            = 256;          //62
        public float LtypeScale     = 1;            //48
        public int ByteCount        = 0;            //92
        public int Visible          = 0;            //60
        public string Preview       = null;         //310

        public float X0             = 0;            //10
        public float Y0             = 0;            //20
        public float Z0             = 0;            //30
        public float X1             = 0;            //11
        public float Y1             = 0;            //21
        public float Z1             = 0;            //31
        public float Thickness      = 0;            //39
        public float ExtrudeX       = 0;            //210
        public float ExtrudeY       = 0;            //220
        public float ExtrudeZ       = 0;            //230

        public List<DxfGroup> ExtendedEntityData = null;

        public Rect Box = Rect.Empty;

        public DxfEntity()
        {
            Color = -1;
        }

        public virtual void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 8:
                    Layer = group.Value;
                    break;
                case 6:
                    Ltype = group.Value;
                    break;
                case 10:
                    X0 = float.Parse(group.Value);
                    break;
                case 20:
                    Y0 = float.Parse(group.Value);
                    break;
                case 30:
                    Z0 = float.Parse(group.Value);
                    break;
                case 11:
                    X1 = float.Parse(group.Value);
                    break;
                case 21:
                    Y1 = float.Parse(group.Value);
                    break;
                case 31:
                    Z1 = float.Parse(group.Value);
                    break;
                case 210:
                    ExtrudeX = float.Parse(group.Value);
                    break;
                case 220:
                    ExtrudeY = float.Parse(group.Value);
                    break;
                case 230:
                    ExtrudeZ = float.Parse(group.Value);
                    break;
                case 39:
                    Thickness = float.Parse(group.Value);
                    break;
                case 100:
                    SubClass = group.Value;
                    break;
                case 5:
                    Handle = group.Value;
                    break;
                case 102:
                    _reader.PushGroup(group);
                    List<DxfGroup> list = _reader.Read102Group();
                    break;
                case 330:
                    SoftPointer = group.Value;
                    break;
                case 360:
                    //360  Hard owner ID/handle to owner dictionary  (optional)  [no default]
                    break;
                case 410:
                    LayoutTab = group.Value;
                    break;
                case 310:
                    Preview = group.Value;
                    break;
                case 67:
                    Space = int.Parse(group.Value);
                    break;
                case 62:
                    Color = int.Parse(group.Value);
                    break;
                case 92:
                    ByteCount = int.Parse(group.Value);
                    break;
                case 60:
                    Visible = int.Parse(group.Value);
                    break;
                case 48:
                    LtypeScale = float.Parse(group.Value);
                    break;
                case 1002:
                    _reader.PushGroup(group);
                    ExtendedEntityData = _reader.Read1002Group();
                    break;
            }
        }

        public virtual string DxfTextString(string text)
        {
            List<string> entities = new List<string>();

            while (text.Contains(@"\"))
            {
                int es = text.IndexOf(@"\");
                if (es >= 0)
                {
                    int ee = text.IndexOf(";");
                    if (ee > es)
                    {
                        string entity = text.Substring(es, ee - es + 1);
                        entities.Add(entity);
                        text = text.Remove(es, ee - es + 1);
                    }
                    else
                    {
                        // invalid text entity
                        break;
                    }
                }
            }

            return text;
        }
    }
}
