namespace Cirros.Dxf
{
    class DxfTraceEntity : DxfEntity
    {
        public DxfTraceEntity(DxfReader reader)
            : base(reader)
        {
            Type = "TRACE";
        }

        // 100	Subclass marker (AcDbTrace) 
        //  10	First corner (in OCS)
        //          DXF: X value; APP: 3D point
        //  20,30	DXF: Y and Z values of first corner (in OCS)
        //  11	Second corner (in OCS)
        //          DXF: X value; APP: 3D point
        //  21,31	DXF: Y and Z values of second corner (in OCS)
        //  12	Third corner (in OCS)
        //          DXF: X value; APP: 3D point
        //  22,32	DXF: Y and Z values of third corner (in OCS)
        //  13	Fourth corner (in OCS)
        //          DXF: X value; APP: 3D point
        //  23,33	DXF: Y and Z values of fourth corner (in OCS)
        //  39	Thickness (optional; default = 0)
        // 210	Extrusion direction (optional; default = 0, 0, 1)
        //          DXF: X value; APP: 3D vector
        // 220, 230	DXF: Y and Z values of extrusion direction (optional)

        public float X2;            //12
        public float Y2;            //22
        public float Z2;            //32
        public float X3;            //13
        public float Y3;            //23
        public float Z3;            //33

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
                default:
                    base.SetValue(group);
                    break;
            }
        }
    }
}
