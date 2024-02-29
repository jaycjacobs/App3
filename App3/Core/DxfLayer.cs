using System.Collections.Generic;

namespace Cirros.Dxf
{
    public class DxfLayer
    {
        DxfReader _reader;

        public DxfLayer(DxfReader reader)
        {
            _reader = reader;
        }

        // 100  Subclass marker (AcDbLayerTableRecord)
        //   2  Layer name
        //  70  Standard flags (bit-coded values):
        //         1 = Layer is frozen; otherwise layer is thawed. 
        //         2 = Layer is frozen by default in new viewports.
        //         4 = Layer is locked.
        //         16 = If set, table entry is externally dependent on an xref.
        //         32 = If this bit and bit 16 are both set, the externally dependent xref has been successfully resolved.
        //         64 = If set, the table entry was referenced by at least one entity in the drawing the last time the drawing was edited. 
        //                 (This flag is for the benefit of AutoCAD commands. 
        //                    It can be ignored by most programs that read DXF files and need not be set by programs that write DXF files.)
        //  62  Color number (if negative, layer is off)
        //   6  Linetype name
        // 290  Plotting flag. If set to 0, do not plot this layer
        // 370  Lineweight enum value
        // 390  Hard pointer ID/handle of PlotStyleName object
 
        public string Name;             // 2
        public int Flags;               // 70
        public int Color;               // 62 (off if negative)
        public string Ltype;            // 6
        public string Subclass;         // 100
        public int PlottingFlag;        // 290 (do not plot if 0)
        public string Lineweight;       // 370
        public string Plotstyle;        // 390
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
                case 62:
                    Color = int.Parse(group.Value);
                    break;
                case 6:
                    Ltype = group.Value;
                    break;
                case 100:
                    Subclass = group.Value;
                    break;
                case 290:
                    PlottingFlag = int.Parse(group.Value);
                    break;
                case 370:
                    Lineweight = group.Value;
                    break;
                case 390:
                    Plotstyle = group.Value;
                    break;
                case 330:
                    // Soft-pointer ID/handle to owner object
                    break;
                case 360:
                    // Hard owner ID/handle to owner dictionary (optional)
                    break;
                case 5:
                    // Handle 
                    break;
                case 102:
                    _reader.PushGroup(group);
                    List<DxfGroup> list = _reader.Read102Group();
                    break;
                default:
#if DEBUG
                    string msg = string.Format("DXF parse: Unexpected group in LAYER definition: code={0}, value={1}", group.Code, group.Value);
                    System.Diagnostics.Debug.WriteLine(msg);
#endif
                    break;
            }
        }
    }
}
