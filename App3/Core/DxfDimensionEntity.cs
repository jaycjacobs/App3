
namespace Cirros.Dxf
{
    public class DxfDimensionEntity : DxfEntity
    {
        public DxfDimensionEntity(DxfReader reader)
            : base(reader)
        {
            Type = "DIMENSION";
        }

        //100  Subclass marker (AcDbLine) 
        // 39  Thickness (optional; default = 0)
        // 10  Start point (in WCS) DXF: X value; APP: 3D point
        // 20, 30 DXF: Y and Z values of start point (in WCS)
        // 11  End point (in WCS) DXF: X value; APP: 3D point
        // 21, 31 DXF: Y and Z values of end point (in WCS)
        //210  Extrusion direction (optional; default = 0, 0, 1)DXF: X value; APP: 3D vector
        //220, 230  DXF: Y and Z values of extrusion direction (optional)
        // 50  Angle of the X axis for the UCS in effect when the point was drawn (optional, default = 0); used when PDMODE is nonzero

        //  2  Name of the block that contains the entities that make up the dimension picture
        // 70  Dimension type.
        //      Values 0-6 are integer values that represent the dimension type. Values 32, 64, and 128 are bit values, which are added to the integer values (value 32 is always set in R13 and later releases).
        //      0 = Rotated, horizontal, or vertical; 1 = Aligned;
        //      2 = Angular; 3 = Diameter; 4 = Radius; 
        //      5 = Angular 3 point; 6 = Ordinate;
        //      32 = Indicates that the block reference (group code 2) is referenced by this dimension only.
        //      64 = Ordinate type. This is a bit value (bit 7) used only with integer value 6. If set, ordinate is X-type; if not set, ordinate is Y-type. 
        //      128 = This is a bit value (bit 8) added to the other group 70 values if the dimension text has been positioned at a user-defined location rather than at the default location.
        // 71  Attachment point:
        //      1 = Top left; 2 = Top center; 3 = Top right; 
        //      4 = Middle left; 5 = Middle center; 6 = Middle right;
        //      7 = Bottom left; 8 = Bottom center; 9 = Bottom right
 
        // 72  Dimension text line spacing style (optional): 
        //      1(or missing) = At least (taller characters will override)
        //      2 = Exact (taller characters will not override)
 
        // 41  Dimension text line spacing factor (optional): 
        //      Percentage of default (3-on-5) line spacing to be applied. Valid values range from 0.25 to 4.00.
 
        // 42  Actual measurement (optional; read-only value)
        //  1  Dimension text explicitly entered by the user. Optional; default is the measurement. If null or "<>", the dimension measurement is drawn as the text, if " " (one blank space), the text is suppressed. Anything else is drawn as the text.
        // 53  The optional group code 53 is the rotation angle of the dimension text away from its default orientation (the direction of the dimension line)  (optional).
        // 51  All dimension types have an optional 51 group code, which indicates the horizontal direction for the dimension entity. The dimension entity determines the orientation of dimension text and lines for horizontal, vertical, and rotated linear dimensions. 
        //      This group value is the negative of the angle between the OCS X axis and the UCS X axis. It is always in the XY plane of the OCS.
        //  3  Dimension style name

        public string Text;         //1
        public string BlockName;    //2
        public string StyleName;    //3
        public float TextSpacing;   //41
        public float ActualMeasure; //42
        public float Direction;     //51
        public float Rotation;      //53
        public int DimensionType;   //70
        public int AttachmentPoint; //71
        public int TextSpacingMode; //72

        public DxfPoint3 InsertionPoint = new DxfPoint3();    // 12,22,32

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 1:
                    //  1  Dimension text explicitly entered by the user. Optional; default is the measurement. If null or "<>", the dimension measurement is drawn as the text, if " " (one blank space), the text is suppressed. Anything else is drawn as the text.
                    Text = group.Value;
                    break;
                case 2:
                    //  2  Name of the block that contains the entities that make up the dimension picture
                    BlockName = group.Value;
                    break;
                case 3:
                    //  3  Dimension style name
                    StyleName = group.Value;
                    break;
                case 41:
                    // 41  Dimension text line spacing factor (optional): 
                    //      Percentage of default (3-on-5) line spacing to be applied. Valid values range from 0.25 to 4.00.
                    TextSpacing = float.Parse(group.Value);
                    break;
                case 42:
                    // 42  Actual measurement (optional; read-only value)
                    ActualMeasure = float.Parse(group.Value);
                    break;
                case 51:
                    // 51  All dimension types have an optional 51 group code, which indicates the horizontal direction for the dimension entity. The dimension entity determines the orientation of dimension text and lines for horizontal, vertical, and rotated linear dimensions. 
                    //      This group value is the negative of the angle between the OCS X axis and the UCS X axis. It is always in the XY plane of the OCS.
                    Direction = float.Parse(group.Value);
                    break;
                case 53:
                    // 53  The optional group code 53 is the rotation angle of the dimension text away from its default orientation (the direction of the dimension line)  (optional).
                    Rotation = float.Parse(group.Value);
                    break;
                case 70:
                    // 70  Dimension type.
                    //      Values 0-6 are integer values that represent the dimension type. Values 32, 64, and 128 are bit values, which are added to the integer values (value 32 is always set in R13 and later releases).
                    //      0 = Rotated, horizontal, or vertical; 1 = Aligned;
                    //      2 = Angular; 3 = Diameter; 4 = Radius; 
                    //      5 = Angular 3 point; 6 = Ordinate;
                    //      32 = Indicates that the block reference (group code 2) is referenced by this dimension only.
                    //      64 = Ordinate type. This is a bit value (bit 7) used only with integer value 6. If set, ordinate is X-type; if not set, ordinate is Y-type. 
                    //      128 = This is a bit value (bit 8) added to the other group 70 values if the dimension text has been positioned at a user-defined location rather than at the default location.
                    DimensionType = int.Parse(group.Value);
                    break;
                case 71:
                    // 71  Attachment point:
                    //      1 = Top left; 2 = Top center; 3 = Top right; 
                    //      4 = Middle left; 5 = Middle center; 6 = Middle right;
                    //      7 = Bottom left; 8 = Bottom center; 9 = Bottom right
                    AttachmentPoint = int.Parse(group.Value);
                    break;
                case 72:
                    // 72  Dimension text line spacing style (optional): 
                    //      1(or missing) = At least (taller characters will override)
                    //      2 = Exact (taller characters will not override)
                    TextSpacingMode = int.Parse(group.Value);
                    break;
                case 12:
                    InsertionPoint.X = float.Parse(group.Value);
                    break;
                case 22:
                    InsertionPoint.Y = float.Parse(group.Value);
                    break;
                case 32:
                    InsertionPoint.Z = float.Parse(group.Value);
                    break;

                default:
                    base.SetValue(group);
                    break;
            }
        }
    }
}
