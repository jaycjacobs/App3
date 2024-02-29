
namespace Cirros.Dxf
{
    public class DxfPointEntity : DxfEntity
    {
        public DxfPointEntity(DxfReader reader)
            : base(reader)
        {
            Type = "POINT";
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

        public float Angle;          //50

        public override void SetValue(DxfGroup group)
        {
            if (group.Code == 50)
            {
                Angle = float.Parse(group.Value);
            }
            else
            {
                base.SetValue(group);
            }
        }
    }
}
