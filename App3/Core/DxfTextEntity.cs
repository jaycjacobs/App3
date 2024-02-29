
using System.Text;
namespace Cirros.Dxf
{
    public class DxfTextEntity : DxfEntity
    {
        public DxfTextEntity(DxfReader reader)
            : base(reader)
        {
            Type = "TEXT";
        }

         //100  Subclass marker (AcDbText) 
         // 39  Thickness (optional; default = 0)
         // 10  First alignment point (in OCS)  DXF: X value; APP: 3D point
         // 20, 30 DXF: Y and Z values of first alignment point (in OCS) 
         // 11  Second alignment point (in OCS) (optional) DXF: X value; APP: 3D point
         //       This value is meaningful only if the value of a 72 or 73 group is nonzero (if the justification is anything other than baseline/left).
         // 21, 31  DXF: Y and Z values of second alignment point (in OCS) (optional)
         //210  Extrusion direction (optional; default = 0, 0, 1) DXF: X value; APP: 3D vector
         //220, 230  DXF: Y and Z values of extrusion direction (optional)

         //  1  Default value (the string itself)
         // 40  Text height
         // 50  Text rotation (optional; default = 0)
         // 41  Relative X scale factor-width (optional; default = 1)
         //           This value is also adjusted when fit-type text is used.
         // 51  Oblique angle (optional; default = 0)
         //  7  Text style name (optional, default = STANDARD)
         // 71  Text generation flags (optional, default = 0):
         //       2 = Text is backward (mirrored in X).
         //       4 = Text is upside down (mirrored in Y).
         // 72  Horizontal text justification type (optional, default = 0) integer codes (not bit-coded)
         //       0 = Left; 1= Center; 2 = Right
         //       3 = Aligned (if vertical alignment = 0)
         //       4 = Middle (if vertical alignment = 0)
         //       5 = Fit (if vertical alignment = 0)
         //       See the Group 72 and 73 integer codes table for clarification.
         // 73  Vertical text justification type (optional, default = 0): integer codes (not bit- coded):
         //       0 = Baseline; 1 = Bottom; 2 = Middle; 3 = Top
         //       See the Group 72 and 73 integer codes table for clarification.

        public float Height     = 0;            //40
        public float Rotation   = 0;            //50
        public float HzScale    = 1;            //41
        public float Slant      = 0;            //51
        public string Style     = "STANDARD";   //7
        public string Text      = "";           //1
        public int Mirror       = 0;            //71
        public int HorizJust    = 0;            //72
        public int VertJust     = 0;            //73

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 40:
                    Height = float.Parse(group.Value);
                    break;
                case 50:
                    Rotation = float.Parse(group.Value);
                    break;
                case 41:
                    HzScale = float.Parse(group.Value);
                    break;
                case 51:
                    Slant = float.Parse(group.Value);
                    break;
                case 1:
                    Text = DxfTextString(group.Value);
                    break;
                case 7:
                    Style = group.Value;
                    break;
                case 71:
                    Mirror = int.Parse(group.Value);
                    break;
                case 72:
                    HorizJust = int.Parse(group.Value);
                    break;
                case 73:
                    VertJust = int.Parse(group.Value);
                    break;
                default:
                    base.SetValue(group);
                    break;
            }
        }
    }

    public class DxfMTextEntity : DxfEntity
    {
        public DxfMTextEntity(DxfReader reader)
            : base(reader)
        {
            Type = "MTEXT";
        }

        // 100 Subclass marker (AcDbMText) 
        //  10 Insertion point  DXF: X value; APP: 3D point
        //  20, 30  DXF: Y and Z values of insertion point
        //  40  Nominal (initial) text height
        //  41  Reference rectangle width
        //  71  Attachment point:
        //        1 = Top left; 2 = Top center; 3 = Top right; 
        //        4 = Middle left; 5 = Middle center; 6 = Middle right;
        //        7 = Bottom left; 8 = Bottom center; 9 = Bottom right
        //  72  Drawing direction:
        //        1 = Left to right;
        //        3 = Top to bottom;
        //        5 = By style (the flow direction is inherited from the associated text style)
        //   1  Text string. If the text string is less than 250 characters, all characters appear in group 1. If the text string is greater than 250 characters, the string is divided into 250 character chunks, which appear in one or more group 3 codes. If group 3 codes are used, the last group is a group 1 and has fewer than 250 characters.
        //   3  Additional text (always in 250 character chunks) (optional)
        //   7  Text style name (STANDARD if not provided) (optional)
        // 210  Extrusion direction (optional; default = 0, 0, 1)  DXF: X value; APP: 3D vector
        // 220, 230  DXF: Y and Z values of extrusion direction (optional)
        //  11  X-axis direction vector (in WCS)  DXF: X value; APP: 3D vector
        //  21, 31  DXF: Y and Z values of X-axis direction vector (in WCS)
        //      NOTE  A group code 50 (rotation angle in radians) passed as DXF input is converted to the equivalent direction vector (if both a code 50 and codes 11, 21, 31 are passed, the last one wins). This is provided as a convenience for conversions from text objects.
        //  42  Horizontal width of the characters that make up the mtext entity. This value will always be equal to or less than the value of group code 41. (read-only, ignored if supplied)
        //  43  Vertical height of the mtext entity (read-only, ignored if supplied)
        //  50  Rotation angle in radians
        //  73  Mtext line spacing style (optional): 
        //        1 = At least (taller characters will override)
        //        2 = Exact (taller characters will not override)
        //  44  Mtext line spacing factor (optional):  Percentage of default (3-on-5) line spacing to be applied. Valid values range from 0.25 to 4.00.
        // Xdata with the "DCO15" application ID may follow an mtext entity. This contains information related to the dbConnect feature. 


        public float Height;            //40
        public float Rotation;          //50
        public float RectWidth;         //41
        public string Style;            //7
        public string Text;             //1
        public int AttachPoint;         //71
        public int Direction;           //72

        public float HorizCharWidth;            //42
        public float VerticalHeight;            //43
        public float MTextLineSpacingFactor;    //44
        public int MTextLineSpacingStyle;       //73

        public bool SecondPointDefined = false;

        StringBuilder _sb = null;

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 1:
                    if (_sb != null)
                    {
                        _sb.Append(group.Value);
                        Text = DxfTextString(_sb.ToString());
                        _sb = null;
                    }
                    else
                    {
                        Text = DxfTextString(group.Value);
                    }
                    break;
                case 3:
                    if (_sb == null)
                    {
                        _sb = new StringBuilder();
                    }
                    _sb.Append(group.Value);
                    break;
                case 7:
                    Style = group.Value;
                    break;
                case 40:
                    Height = float.Parse(group.Value);
                    break;
                case 41:
                    RectWidth = float.Parse(group.Value);
                    break;
                case 42:
                    HorizCharWidth = float.Parse(group.Value);
                    break;
                case 43:
                    VerticalHeight = float.Parse(group.Value);
                    break;
                case 44:
                    MTextLineSpacingFactor = float.Parse(group.Value);
                    break;
                case 50:
                    Rotation = float.Parse(group.Value);
                    break;
                case 71:
                    AttachPoint = int.Parse(group.Value);
                    break;
                case 72:
                    Direction = int.Parse(group.Value);
                    break;
                case 73:
                    MTextLineSpacingStyle = int.Parse(group.Value);
                    break;
                case 11:
                    SecondPointDefined = true;
                    base.SetValue(group);
                    break;
                default:
                    base.SetValue(group);
                    break;
            }
        }
    }
}
