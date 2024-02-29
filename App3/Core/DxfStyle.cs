
namespace Cirros.Dxf
{
    public class DxfStyle
    {
        DxfReader _reader;

        public DxfStyle(DxfReader reader)
        {
            _reader = reader;
        }

        public DxfStyle(string name)
        {
            Name = name;
            Flags = 0;
            Height = 0;
            Width = 1;
            LastHeight = 0;
            Slant = 0;
            Font = "txt";
            BigFont = "";
            Tag = null;
        }

        // 100  Subclass marker (AcDbTextStyleTableRecord)
        //   2  Style name
        //  70  Standard flag values (bit-coded values):
        //       1 = If set, this entry describes a shape. 
        //       4 = Vertical text.
        //       16 = If set, table entry is externally dependent on an xref.
        //       32 = If this bit and bit 16 are both set, the externally dependent xref has been successfully resolved.
        //       64 = If set, the table entry was referenced by at least one entity in the drawing the last time the drawing was edited. 
        //               (This flag is for the benefit of AutoCAD commands. It can be ignored by most programs that read DXF files and need
        //               not be set by programs that write DXF files.)
        //  40  Fixed text height; 0 if not fixed
        //  41  Width factor   
        //  50  Oblique angle  
        //  71  Text generation flags:
        //       2 = Text is backward (mirrored in X)
        //       4 = Text is upside down (mirrored in Y)  
        //  42  Last height used
        //   3  Primary font file name
        //   4  Bigfont file name; blank if none

        public string Name;             // 2
        public int Flags;               // 70
        public float Height;            // 40
        public float Width;             // 41
        public float LastHeight;        // 42
        public float Slant;             // 50
        public string Font;             // 3
        public string BigFont;          // 4
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
                case 100:
                    // 100	Subclass marker (AcDbViewportTableRecord)
                    break;
                case 40:
                    //  40  Fixed text height; 0 if not fixed
                    Height = float.Parse(group.Value);
                    break;
                case 41:
                    //  41  Width factor   
                    Width = float.Parse(group.Value);
                    break;
                case 50:
                    //  50  Oblique angle  
                    Slant = float.Parse(group.Value);
                    break;
                case 71:
                    //  71  Text generation flags:
                    //       2 = Text is backward (mirrored in X)
                    //       4 = Text is upside down (mirrored in Y)  
                    break;
                case 42:
                    //  42  Last height used
                    LastHeight = float.Parse(group.Value);
                    break;
                case 3:
                    //   3  Primary font file name
                    Font = group.Value;
                    break;
                case 4:
                    //   4  Bigfont file name; blank if none
                    BigFont = group.Value;
                    break;
                case 5:
                    // Handle 
                    break;

                case 1000:
                case 1001:
                case 1071:
                    // Undocumented groups that have been seen in DXF files
                    break;

                default:
                    string msg = string.Format("DXF parse: Unexpected group in STYLE definition: code={0}, value={1}", group.Code, group.Value);
                    //System.Diagnostics.Debug.WriteLine(msg);
                    break;
            }
        }
    }
}
