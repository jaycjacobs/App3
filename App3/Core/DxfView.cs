
namespace Cirros.Dxf
{
    public class DxfView
    {
        DxfReader _reader;

        public DxfView(DxfReader reader)
        {
            _reader = reader;
        }

        // 100  Subclass marker (AcDbViewTableRecord)
        //   2  Name of view
        //  70  Standard flag values (bit-coded values):
        //         1 = If set, this is a paper space view.
        //         16 = If set, table entry is externally dependent on an xref.
        //         32 = If this bit and bit 16 are both set, the externally dependent xref has been successfully resolved.
        //         64 = If set, the table entry was referenced by at least one entity in the drawing the last time the drawing was edited. 
        //               (This flag is for the benefit of AutoCAD commands. It can be ignored by most programs that read DXF files and 
        //                   need not be set by programs that write DXF files.)
        //  40  View height (in DCS)
        //  10  DXF: X value of view center point (in DCS) 2D
        //  20  DXF: Y value of view center point (in DCS) 2D
        //  41  View width (in DCS)
        //  11  View direction from target (in WCS) DXF: X value; APP: 3D vector
        //  21, 31 DXF: Y and Z values of view direction from target (in WCS)
        //  12  Target point (in WCS) DXF: X value; APP: 3D point
        //  22, 32 DXF: Y and Z values of target point (in WCS)
        //  42  Lens length
        //  43  Front clipping plane (offset from target point)
        //  44  Back clipping plane (offset from target point)
        //  50  Twist angle
        //  71  View mode (see VIEWMODE system variable)
        // 281  Render mode:
        //         0 = 2D Optimized (classic 2D)
        //         1 = Wireframe
        //         2 = Hidden line
        //         3 = Flat shaded
        //         4 = Gouraud shaded
        //         5 = Flat shaded with wireframe
        //         6 = Gouraud shaded with wireframe
        //       All rendering modes other than 2D Optimized engage the new 3D graphics pipeline. 
        //       These values directly correspond to the SHADEMODE command and the AcDbAbstractViewTableRecord::RenderMode enum.
        //  72  1 if there is a UCS associated to this view, 0 otherwise.

        public string Name;             //   2
        public int Flags;               //  70
        public float X0;                //  10  DXF: X value of view center point (in DCS) 2D
        public float Y0;                //  20  DXF: Y value of view center point (in DCS) 2D
        public float X1;                //  11  View direction from target (in WCS) DXF: X value; APP: 3D vector
        public float Y1;                //  21, 31 DXF: Y and Z values of view direction from target (in WCS)
        public float Z1;
        public float X2;                //  12  Target point (in WCS) DXF: X value; APP: 3D point
        public float Y2;                //  22, 32 DXF: Y and Z values of target point (in WCS)
        public float Z2;
        public float X3;                // 13
        public float Y3;                // 23
        public float Z3;
        public float ViewHeight;        //  40  View height (in DCS)
        public float ViewWidth;         //  41  View width (in DCS)
        public object Tag;              // Reader defined flag

        public void SetGroup(DxfGroup group)
        {
            switch (group.Code)
            {
                case 2:
                    Name = group.Value;
                    break;
                case 10:
                    X0 = float.Parse(group.Value);
                    break;
                case 20:
                    Y0 = float.Parse(group.Value);
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
                case 40:
                    ViewHeight = float.Parse(group.Value);
                    break;
                case 41:
                    ViewWidth = float.Parse(group.Value);
                    break;
                case 70:
                    Flags = int.Parse(group.Value);
                    break;
                case 42:
                    //  42  Lens length
                    break;
                case 43:
                    //  43  Front clipping plane (offset from target point)
                    break;
                case 44:
                    //  44  Back clipping plane (offset from target point)
                    break;
                case 50:
                    //  50  Twist angle
                    break;
                case 71:
                    //  71  View mode (see VIEWMODE system variable)
                    break;
                case 72:
                    //  72  1 if there is a UCS associated to this view, 0 otherwise.
                    break;
                case 281:
                    // 281  Render mode:
                    //         0 = 2D Optimized (classic 2D)
                    //         1 = Wireframe
                    //         2 = Hidden line
                    //         3 = Flat shaded
                    //         4 = Gouraud shaded
                    //         5 = Flat shaded with wireframe
                    //         6 = Gouraud shaded with wireframe
                    //       All rendering modes other than 2D Optimized engage the new 3D graphics pipeline. 
                    //       These values directly correspond to the SHADEMODE command and the AcDbAbstractViewTableRecord::RenderMode enum.
                    break;
                case 100:
                    // 100	Subclass marker (AcDbViewportTableRecord)
                    break;
                case 5:
                    // Handle 
                    break;
                default:
                    string msg = string.Format("DXF parse: Unexpected group in VIEW definition: code={0}, value={1}", group.Code, group.Value);
                    //System.Diagnostics.Debug.WriteLine(msg);
                    break;
            }
        }
    }
}
