using System.Collections.Generic;

namespace Cirros.Dxf
{
    public class DxfBlockRecord
    {
        DxfReader _reader;

        public DxfBlockRecord(DxfReader reader)
        {
            _reader = reader;
        }

        //100	Subclass marker (AcDbBlockTableRecord)
        //2	Block name
        //340	Hard-pointer ID/handle to associated LAYOUT object
        //310	DXF: Binary data for bitmap preview (optional)
        //1001	Xdata application name "ACAD" (optional)
        //1000	Xdata string data "DesignCenter Data" (optional)
        //1002	Begin xdata "{" (optional)
        //1070	Autodesk Design Center version number.
        //1070	Insert units:
        //    0 = Unitless; 1 = Inches; 2 = Feet; 3 = Miles; 4 = Millimeters;
        //    5 = Centimeters; 6 = Meters; 7 = Kilometers; 8 = Microinches;
        //    9 = Mils; 10 = Yards; 11 = Angstroms; 12 = Nanometers;
        //    13 = Microns; 14 = Decimeters; 15 = Decameters;
        //    16 = Hectometers; 17 = Gigameters; 18 = Astronomical units;
        //    19 = Light years; 20 = Parsecs
        //1002	End xdata "}"

        public string Name;                 // 2
        public int Flags;                   // 70
        public string Handle = null;        //5
        public string SubClass = null;      //100
        public int InsertUnits = 0;         // Xdata.1070[1]
        public bool DesignCenter = false;

        public List<DxfGroup> ExtendedEntityData = null;

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
                case 5:
                    Handle = group.Value;
                    break;
                case 100:
                    SubClass = group.Value;
                    break;

                case 1000:
                    DesignCenter = group.Value.ToLower().Trim() == "designcenter data";
                    break;

                case 1001:
                    //1001	Xdata application name "ACAD" (optional)
                    break;

                case 1002:
                    _reader.PushGroup(group);
                    ExtendedEntityData = _reader.Read1002Group();
                    break;

                case 1004:
                case 1071:
                    // Unknown groups that have been seen in a BLOCK_RECORD
                    break;

                default:
#if DEBUG
                    string msg = string.Format("DXF parse: Unexpected group in BLOCK_RECORD definition: code={0}, value={1}", group.Code, group.Value);
                    System.Diagnostics.Debug.WriteLine(msg);
#endif
                    break;
            }
        }
    }
}
