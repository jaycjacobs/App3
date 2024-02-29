
namespace Cirros.Dxf
{
    class Dxf3DFaceEntity : DxfEntity
    {
        public Dxf3DFaceEntity(DxfReader reader)
            : base(reader)
        {
            Type = "3DFACE";
        }

        // 100	Subclass marker (AcDbFace) 
        //  10	First corner (in WCS)
        //          DXF: X value; APP: 3D point
        //  20,30	DXF: Y and Z values of first corner (in WCS) 
        //  11	Second corner (in WCS)
        //          DXF: X value; APP: 3D point
        //  21,31	DXF: Y and Z values of second corner (in WCS)
        //  12	Third corner (in WCS)
        //          DXF: X value; APP: 3D point
        //  22,32	DXF: Y and Z values of third corner (in WCS) 
        //  13	Fourth corner (in WCS). If only three corners are entered, this is the same as the third corner
        //          DXF: X value; APP: 3D point
        //  23,33	DXF: Y and Z values of fourth corner (in WCS) 
        //  70	Invisible edge flags (optional; default = 0):
        //        1 = First edge is invisible
        //        2 = Second edge is invisible
        //        4 = Third edge is invisible
        //        8 = Fourth edge is invisible

        public float X2;            //12
        public float Y2;            //22
        public float Z2;            //32
        public float X3;            //13
        public float Y3;            //23
        public float Z3;            //33
        public float Flags;         //70

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
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

                case 70:
                    //  70	Invisible edge flags (optional; default = 0):
                    //        1 = First edge is invisible
                    //        2 = Second edge is invisible
                    //        4 = Third edge is invisible
                    //        8 = Fourth edge is invisible
                    Flags = float.Parse(group.Value);
                    break;

                default:
                    base.SetValue(group);
                    break;
            }
        }
    }
}
