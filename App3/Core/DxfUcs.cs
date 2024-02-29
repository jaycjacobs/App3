namespace Cirros.Dxf
{
    public class DxfUcs
    {
        DxfReader _reader;

        public DxfUcs(DxfReader reader)
        {
            _reader = reader;
        }

        // 100	Subclass marker (AcDbUCSTableRecord)
        //   2	UCS name
        //  70	Standard flag values (bit-coded values):
        //        16 = If set, table entry is externally dependent on an xref.
        //        32 = If this bit and bit 16 are both set, the externally dependent xref has been successfully resolved.
        //        64 = If set, the table entry was referenced by at least one entity in the drawing the last time the drawing was edited. (This flag is for the benefit of AutoCAD commands. It can be ignored by most programs that read DXF files and need not be set by programs that write DXF files.)
        //  10	Origin (in WCS)
        //          DXF: X value; APP: 3D point
        //  20,30   DXF: Y and Z values of origin (in WCS)
        //  11	X-axis direction (in WCS)
        //          DXF: X value; APP: 3D vector
        //  21,31   DXF: Y and Z values of X-axis direction (in WCS)
        //  12	Y-axis direction (in WCS)
        //          DXF: X value; APP: 3D vector
        //  22,32   DXF: Y and Z values of Y-axis direction (in WCS)
        //  79	Orthographic view type:
        //        0 = UCS is not orthographic;
        //        1 = Top; 2 = Bottom;
        //        3 = Front; 4 = Back;
        //        5 = Left; 6 = Right
        // 146	Elevation 
        // 346	ID/handle of base UCS if this is an orthographic. This code is not present if the 79 code is 0. If this code is not present and 79 code is non-zero, then base UCS is assumed to be WORLD.
        //  71	Orthographic type (optional; always appears in pairs with the 13, 23, 33 codes):
        //        1 = Top; 2 = Bottom;
        //        3 = Front; 4 = Back;
        //        5 = Left; 6 = Right
        //  13	Origin for this orthographic type relative to this UCS.
        //          DXF: X value of origin point; APP: 3D point
        //  23,33	DXF: Y and Z values of origin point

        public string Name;             // 2
        public int Flags;               // 70
        public float X0;                //  10,20,30  Origin (in WCS)
        public float Y0;                
        public float Z0;
        public float X1;                //  11,21,31  X-axis direction (in WCS)
        public float Y1;                
        public float Z1;
        public float X2;                //  12,22,32  Y-axis direction (in WCS)
        public float Y2;
        public float Z2;
        public float X3;                //  13,23,33  Origin
        public float Y3;
        public float Z3;
        public object Tag;              // Reader defined flag

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
                case 12:
                    X2 = float.Parse(group.Value);
                    break;
                case 22:
                    Y2 = float.Parse(group.Value);
                    break;
                case 32:
                    Z2 = float.Parse(group.Value);
                    break;
                case 13:
                    X3 = float.Parse(group.Value);
                    break;
                case 23:
                    Y3 = float.Parse(group.Value);
                    break;
                case 33:
                    Z3 = float.Parse(group.Value);
                    break;
                case 79:
                    //  79	Orthographic view type:
                    //        0 = UCS is not orthographic;
                    //        1 = Top; 2 = Bottom;
                    //        3 = Front; 4 = Back;
                    //        5 = Left; 6 = Right
                    break;
                case 146:
                    // 146	Elevation 
                    break;
                case 346:
                    // 346	ID/handle of base UCS if this is an orthographic. This code is not present if the 79 code is 0. If this code is not present and 79 code is non-zero, then base UCS is assumed to be WORLD.
                    break;
                case 71:
                    //  71	Orthographic type (optional; always appears in pairs with the 13, 23, 33 codes):
                    //        1 = Top; 2 = Bottom;
                    //        3 = Front; 4 = Back;
                    //        5 = Left; 6 = Right
                    break;
                case 100:
                    // 100	Subclass marker (AcDbViewportTableRecord)
                    break;
                case 5:
                    // Handle 
                    break;
                default:
                    string msg = string.Format("DXF parse: Unexpected group in UCS definition: code={0}, value={1}", group.Code, group.Value);
                    //System.Diagnostics.Debug.WriteLine(msg);
                    break;
            }
        }
    }
}
