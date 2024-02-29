using System.Collections.Generic;

namespace Cirros.Dxf
{
    public class DxfLtype
    {
        DxfReader _reader;

        public DxfLtype(DxfReader reader)
        {
            _reader = reader;
        }

        // 100  Subclass marker (AcDbLinetypeTableRecord)
        //   2  Linetype name
        //  70  Standard flag values (bit-coded values):
        //         16 = If set, table entry is externally dependent on an xref.
        //         32 = If this bit and bit 16 are both set, the externally dependent xref has been successfully resolved.
        //         64 = If set, the table entry was referenced by at least one entity in the drawing the last time the drawing was edited. 
        //                 (This flag is for the benefit of AutoCAD commands. 
        //                 It can be ignored by most programs that read DXF files and need not be set by programs that write DXF files.)
        //   3  Descriptive text for linetype
        //  72  Alignment code; value is always 65, the ASCII code for A
        //  73  The number of linetype elements
        //  40  Total pattern length
        //  49  Dash, dot or space length (one entry per element)
        //  74  Complex linetype element type (one per element). Default is 0 (no embedded shape/text).
        //         The following codes are bit values:
        //         1 = If set, code 50 specifies an absolute rotation; if not set, code 50 specifies a relative rotation.
        //         2 = Embedded element is a text string.
        //         4 = Embedded element is a shape.
        //  75  Shape number (one per element) if code 74 specifies an embedded shape
        //         If code 74 specifies an embeded text string, this value is set to 0
        //         If code 74 is set to 0, code 75 is omitted
        // 340  Pointer to STYLE object (one per element if code 74 > 0)
        //  46  S = Scale value (optional); multiple entries can exist 
        //  50  R = (relative) or A = (absolute) rotation value in radians of embedded shape or text; one per element if code 74 specifies an embeded shape or text string
        //  44  X = X offset value (optional); multiple entries can exist 
        //  45  Y = Y offset value (optional); multiple entries can exist
        //   9  Text string (one per element if code 74 = 2)
 
        public string Name;             // 2
        public int Flags;               // 70
        public string Description;      // 3
        public string Alignment;        // 72
        public int LtypeElementCount;   // 73
        public float PatternLength;     // 74
        public int ShapeNumber;         // 75
        public string Style;            // 340
        public string Scale;            // 46
        public string RotationValue;    // 50
        public float X;                 // 44
        public float Y;                 // 45
        public string Text;             // 9
        public object Tag;              // Reader defined flag

        public List<double> LTypeElements = new List<double>();

        public void SetGroup(DxfGroup group)
        {
            switch (group.Code)
            {
                case 2:
                    Name = group.Value;
                    break;
                case 70:
                    Flags = int.Parse(group.Value);
                    break;
                case 3:
                    Description = group.Value;
                    break;
                case 72:
                    Alignment = group.Value;
                    break;
                case 73:
                    LtypeElementCount = int.Parse(group.Value);
                    break;
                case 74:
                    //complex line type ?
                    break;
                case 75:
                    ShapeNumber = int.Parse(group.Value);
                    break;
                case 340:
                    Style = group.Value;
                    break;
                case 50:
                    RotationValue = group.Value;
                    break;
                case 44:
                    X = float.Parse(group.Value);
                    break;
                case 45:
                    Y = float.Parse(group.Value);
                    break;
                case 46:
                    Scale = group.Value;
                    break;
                case 9:
                    Text = group.Value;
                    break;
                case 330:
                    // Soft-pointer ID/handle to owner object
                    break;
                case 100:
                    // Subclass marker (AcDbSymbolTableRecord)
                    break;
                case 5:
                    // Handle 
                    break;
                case 40:
                    PatternLength = float.Parse(group.Value); 
                    break;
                case 49:
                    LTypeElements.Add(float.Parse(group.Value)); 
                    break;
                case 102:
                    _reader.PushGroup(group);
                    List<DxfGroup> list = _reader.Read102Group();
                    break;
                default:
#if DEBUG
                    string msg = string.Format("DXF parse: Unexpected group in LTYPE definition: code={0}, value={1}", group.Code, group.Value);
                    System.Diagnostics.Debug.WriteLine(msg);
#endif
                    break;
            }
        }
    }
}
