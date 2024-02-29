
namespace Cirros.Dxf
{
    class DxfLineEntity : DxfEntity
    {
        public DxfLineEntity(DxfReader reader)
            : base(reader)
        {
            Type = "LINE";
        }

         //100  Subclass marker (AcDbLine) 
         // 39  Thickness (optional; default = 0)
         // 10  Start point (in WCS) DXF: X value; APP: 3D point
         // 20, 30 DXF: Y and Z values of start point (in WCS)
         // 11  End point (in WCS) DXF: X value; APP: 3D point
         // 21, 31 DXF: Y and Z values of end point (in WCS)
         //210  Extrusion direction (optional; default = 0, 0, 1)DXF: X value; APP: 3D vector
         //220, 230  DXF: Y and Z values of extrusion direction (optional)

        public override void SetValue(DxfGroup group)
        {
            base.SetValue(group);
        }
    }
}
