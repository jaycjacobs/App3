namespace Cirros.Dxf
{
    public class DxfEllipseEntity : DxfEntity
    {
        public DxfEllipseEntity(DxfReader reader)
            : base(reader)
        {
            Type = "ELLIPSE";
        }

        // 100	Subclass marker (AcDbEllipse) 
        //  10	Center point (in WCS)
        //          DXF: X value; APP: 3D point
        //  20,30	DXF: Y and Z values of center point (in WCS) 
        //  11	Endpoint of major axis, relative to the center (in WCS)
        //          DXF: X value; APP: 3D point
        //  21,31	DXF: Y and Z values of endpoint of major axis, relative to the center (in WCS)
        // 210	Extrusion direction (optional; default = 0, 0, 1)
        //          DXF: X value; APP: 3D vector
        // 220,230	DXF: Y and Z values of extrusion direction  (optional)
        //  40	Ratio of minor axis to major axis
        //  41	Start parameter (this value is 0.0 for a full ellipse)
        //  42	End parameter (this value is 2pi for a full ellipse)

        public float Ratio;         //  40	Ratio of minor axis to major axis
        public float Start;         //  41	Start parameter (this value is 0.0 for a full ellipse)
        public float End;           //  42	End parameter (this value is 2pi for a full ellipse)

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 40:
                    //  40	Ratio of minor axis to major axis
                    Ratio = float.Parse(group.Value);
                    break;

                case 41:
                    //  41	Start parameter (this value is 0.0 for a full ellipse)
                    Start = float.Parse(group.Value);
                    break;

                case 42:
                    //  42	End parameter (this value is 2pi for a full ellipse)
                    End = float.Parse(group.Value);
                    break;

                default:
                    base.SetValue(group);
                    break;
            }
        }
    }
}
