namespace Cirros.Dxf
{
    public class DxfVport
    {
        DxfReader _reader;

        public DxfVport(DxfReader reader)
        {
            _reader = reader;
        }

        // 100	Subclass marker (AcDbViewportTableRecord)
        //   2	Viewport name
        //  70	Standard flag values (bit-coded values):
        //        16 = If set, table entry is externally dependent on an xref.
        //        32 = If this bit and bit 16 are both set, the externally dependent xref has been successfully resolved.
        //        64 = If set, the table entry was referenced by at least one entity in the drawing the last time the drawing was edited. (This flag is for the benefit of AutoCAD commands. It can be ignored by most programs that read DXF files and need not be set by programs that write DXF files.)
        //  10	Lower-left corner of viewport
        //        DXF: X value; APP: 2D point
        //  20	DXF: Y value of lower-left corner of viewport
        //  11	Upper-right corner of viewport
        //        DXF: X value; APP: 2D point
        //  21	DXF: Y value of upper-right corner of viewport 
        //  12	View center point (in DCS)
        //        DXF: X value; APP: 2D point
        //  22	DXF: Y value of view center point (in DCS)
        //  13	Snap base point
        //        DXF: X value; APP: 2D point
        //  23	DXF: Y value of snap base point
        //  14	Snap spacing X and Y
        //        DXF: X value; APP: 2D point
        //  24	DXF: Y value of snap spacing X and Y 
        //  15	Grid spacing X and Y
        //        DXF: X value; APP: 2D point
        //  25	DXF: Y value of grid spacing X and Y 
        //  16	View direction from target point (in WCS)
        //          DXF: X value; APP: 3D point
        // 26,36	DXF: Y and Z values of view direction from target point
        //          (in WCS) 
        //  17	View target point (in WCS)
        //          DXF: X value; APP: 3D point
        // 27,37	DXF: Y and Z values of view target point (in WCS) 
        //  40	View height
        //  41	Viewport aspect ratio
        //  42	Lens length
        //  43	Front clipping plane (offset from target point)
        //  44	Back clipping plane (offset from target point)
        //  50	Snap rotation angle
        //  51	View twist angle
        //  68	APP: Status field (never saved in DXF)
        //  69	APP: ID (never saved in DXF)
        //  71	View mode (see VIEWMODE system variable)
        //  72	Circle zoom percent
        //  73	Fast zoom setting
        //  74	UCSICON setting
        //  75	Snap on/off
        //  76	Grid on/off
        //  77	Snap style
        //  78	Snap isopair
        // 281	Render mode:
        //        0 = 2D Optimized (classic 2D)
        //        1 = Wireframe
        //        2 = Hidden line
        //        3 = Flat shaded
        //        4 = Gouraud shaded
        //        5 = Flat shaded with wireframe
        //        6 = Gouraud shaded with wireframe
	
        //      All rendering modes other than 2D Optimized engage the new 3D graphics pipeline. These values directly correspond to the SHADEMODE command and the AcDbAbstractViewTableRecord::RenderMode enum.
        //  65	Value of UCSVP for this viewport. If set to 1, then viewport stores its own UCS which will become the current UCS whenever the viewport is activated. If set to 0, UCS will not change when this viewport is activated.
        // 110	UCS origin
        //          DXF: X value; APP: 3D point
        // 120,130	  DXF: Y and Z values of UCS origin
        // 111	UCS X-axis
        //          DXF: X value; APP: 3D vector
        // 121,131	DXF: Y and Z values of UCS X-axis
        // 112	UCS Y-axis
        //          DXF: X value; APP: 3D vector
        // 122,132	DXF: Y and Z values of UCS Y-axis
        //  79	Orthographic type of UCS
        //        0 = UCS is not orthographic;
        //        1 = Top; 2 = Bottom;
        //        3 = Front; 4 = Back;
        //        5 = Left; 6 = Right
        // 146	Elevation
        // 345	ID/handle of AcDbUCSTableRecord if UCS is a named UCS.  If not present, then UCS is unnamed.
        // 346	ID/handle of AcDbUCSTableRecord of base UCS if UCS is orthographic (79 code is non-zero).  If not present and 79 code is non-zero, then base UCS is taken to be WORLD.

        public string Name;             // 2 - Viewport name
        public int Flags;               // 70
        public float X0;                // 10
        public float Y0;                // 20
        public float X1;                // 11
        public float Y1;                // 21
        public float X2;                // 12
        public float Y2;                // 22
        public float X3;                // 13
        public float Y3;                // 23
        public float X4;                // 14
        public float Y4;                // 24
        public float X5;                // 15
        public float Y5;                // 25
        public float X6;                // 16
        public float Y6;                // 26
        public float Z6;                // 36
        public float X7;                // 17
        public float Y7;                // 27
        public float Z7;                // 37
        public float ViewHeight;        //  40	View height
        public float ViewAspect;        //  41	Viewport aspect ratio
        public int SnapFlag;            //  75	Snap on/off
        public int GridFlag;            //  76	Grid on/off
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
                case 12:
                    X2 = float.Parse(group.Value);
                    break;
                case 22:
                    Y2 = float.Parse(group.Value);
                    break;
                case 13:
                    X3 = float.Parse(group.Value);
                    break;
                case 23:
                    Y3 = float.Parse(group.Value);
                    break;
                case 14:
                    X4 = float.Parse(group.Value);
                    break;
                case 24:
                    Y4 = float.Parse(group.Value);
                    break;
                case 15:
                    X5 = float.Parse(group.Value);
                    break;
                case 25:
                    Y5 = float.Parse(group.Value);
                    break;
                case 16:
                    X6 = float.Parse(group.Value);
                    break;
                case 26:
                    Y6 = float.Parse(group.Value);
                    break;
                case 36:
                    Z6 = float.Parse(group.Value);
                    break;
                case 17:
                    X7 = float.Parse(group.Value);
                    break;
                case 27:
                    Y7 = float.Parse(group.Value);
                    break;
                case 37:
                    Z7 = float.Parse(group.Value);
                    break;
                case 40:
                    ViewHeight = float.Parse(group.Value);
                    break;
                case 41:
                    ViewAspect = float.Parse(group.Value);
                    break;
                case 70:
                    Flags = int.Parse(group.Value);
                    break;
                case 42:
                    //  42	Lens length
                    break;
                case 43:
                    //  43	Front clipping plane (offset from target point)
                    break;
                case 44:
                    //  44	Back clipping plane (offset from target point)
                    break;
                case 50:
                    //  50	Snap rotation angle
                    break;
                case 51:
                    //  51	View twist angle
                    break;
                case 68:
                    //  68	APP: Status field (never saved in DXF)
                    break;
                case 69:
                    //  69	APP: ID (never saved in DXF)
                    break;
                case 71:
                    //  71	View mode (see VIEWMODE system variable)
                    break;
                case 72:
                    //  72	Circle zoom percent
                    break;
                case 73:
                    //  73	Fast zoom setting
                    break;
                case 74:
                    //  74	UCSICON setting
                    break;
                case 75:
                    SnapFlag = int.Parse(group.Value);
                    break;
                case 76:
                    GridFlag = int.Parse(group.Value);
                    break;
                case 77:
                    //  77	Snap style
                    break;
                case 78:
                    //  78	Snap isopair
                    break;
                case 281:
                    // 281	Render mode:
                    //        0 = 2D Optimized (classic 2D)
                    //        1 = Wireframe
                    //        2 = Hidden line
                    //        3 = Flat shaded
                    //        4 = Gouraud shaded
                    //        5 = Flat shaded with wireframe
                    //        6 = Gouraud shaded with wireframe
                    //      All rendering modes other than 2D Optimized engage the new 3D graphics pipeline. These values directly correspond to the SHADEMODE command and the AcDbAbstractViewTableRecord::RenderMode enum.
                    break;
                case 65:
                    //  65	Value of UCSVP for this viewport. If set to 1, then viewport stores its own UCS which will become the current UCS whenever the viewport is activated. If set to 0, UCS will not change when this viewport is activated.
                    break;
                case 110:
                    // 110	UCS origin
                    //          DXF: X value; APP: 3D point
                    break;
                case 120:
                    // 120,130	  DXF: Y and Z values of UCS origin
                    break;
                case 130:
                    // 120,130	  DXF: Y and Z values of UCS origin
                    break;
                case 100:
                    // 100	Subclass marker (AcDbViewportTableRecord)
                    break;
                case 111:
                    // 111	UCS X-axis
                    //          DXF: X value; APP: 3D vector
                    break;
                case 121:
                    // 121,131	DXF: Y and Z values of UCS X-axis
                    break;
                case 131:
                    // 121,131	DXF: Y and Z values of UCS X-axis
                    break;
                case 112:
                    // 112	UCS Y-axis
                    //          DXF: X value; APP: 3D vector
                    break;
                case 122:
                    // 122,132	DXF: Y and Z values of UCS Y-axis
                    break;
                case 132:
                    //  79	Orthographic type of UCS
                    //        0 = UCS is not orthographic;
                    //        1 = Top; 2 = Bottom;
                    //        3 = Front; 4 = Back;
                    //        5 = Left; 6 = Right
                    break;
                case 146:
                    // 146	Elevation
                    break;
                case 345:
                    // 345	ID/handle of AcDbUCSTableRecord if UCS is a named UCS.  If not present, then UCS is unnamed.
                    break;
                case 346:
                    // 346	ID/handle of AcDbUCSTableRecord of base UCS if UCS is orthographic (79 code is non-zero).  If not present and 79 code is non-zero, then base UCS is taken to be WORLD.
                    break;
                case 5:
                    // Handle 
                    break;
                default:
                    string msg = string.Format("DXF parse: Unexpected group in VPORT definition: code={0}, value={1}", group.Code, group.Value);
                    //System.Diagnostics.Debug.WriteLine(msg);
                    break;
            }
        }
    }
}
