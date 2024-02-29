
namespace Cirros.Dxf
{
    public class DxfAttdefEntity : DxfEntity
    {
        public DxfAttdefEntity(DxfReader reader)
            : base(reader)
        {
            Type = "ATTDEF";
        }

        // 40  Text height
        //  1  Default value (string) 
        // 50  Text rotation (optional; default = 0)
        // 41  Relative X scale factor (width) (optional; default = 1).
        //          This value is also adjusted when fit-type text is used.
        // 51  Oblique angle (optional; default = 0)
        //  7  Text style name (optional, default = STANDARD)
        // 71  Text generation flags (optional, default = 0); see TEXT group codes
        // 72  Horizontal text justification type (optional, default = 0); see TEXT group codes
        //  3  Prompt string
        //  2  Tag string
        // 70  Attribute flags:
        //          1 = Attribute is invisible (does not appear).
        //          2 = This is a constant attribute.
        //          4 = Verification is required on input of this attribute.
        //          8 = Attribute is preset (no prompt during insertion).
        // 73  Field length (optional; default = 0) (not currently used) 
        // 74  Vertical text justification type (optional, default = 0); see group code 73 in TEXT

        public string Default;          //1
        public string Tag;              //2
        public string Prompt;           //3
        public string TextStyle;        //7
        public float TextHeight;        //40
        public float XScale;            //41
        public float TextRotation;      //50
        public float ObliqueAngle;      //51
        public int AttribFlags;         //70
        public int TextGenFlags;        //71
        public int TextHorizJust;       //72
        public int FieldLength;         //73
        public int VertTextJust;        //74

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 1:
                    Default = group.Value;
                    break;
                case 2:
                    Tag = group.Value;
                    break;
                case 3:
                    Prompt = group.Value;
                    break;
                case 7:
                    TextStyle = group.Value;
                    break;
                case 40:
                    TextHeight = float.Parse(group.Value);
                    break;
                case 41:
                    XScale = float.Parse(group.Value);
                    break;
                case 50:
                    TextRotation = float.Parse(group.Value);
                    break;
                case 51:
                    ObliqueAngle = float.Parse(group.Value);
                    break;
                case 70:
                    AttribFlags = int.Parse(group.Value);
                    break;
                case 71:
                    TextGenFlags = int.Parse(group.Value);
                    break;
                case 72:
                    TextHorizJust = int.Parse(group.Value);
                    break;
                case 73:
                    FieldLength = int.Parse(group.Value);
                    break;
                case 74:
                    VertTextJust = int.Parse(group.Value);
                    break;
                default:
                    base.SetValue(group);
                    break;
            }
        }
    }

    public class DxfAttribEntity : DxfAttdefEntity
    {
        public DxfAttribEntity(DxfReader reader)
            : base(reader)
        {
            Type = "ATTRIB";
        }
    }
}
 
