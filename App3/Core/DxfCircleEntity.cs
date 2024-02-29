
namespace Cirros.Dxf
{
    public class DxfCircleEntity : DxfEntity
    {
        public DxfCircleEntity(DxfReader reader)
            : base(reader)
        {
            Type = "CIRCLE";
        }

         //100  Subclass marker (AcDbCircle) 
         // 39  Thickness (optional; default = 0)
         // 10  Center point (in OCS) DXF: X value; APP: 3D point
         // 20, 30 DXF: Y and Z values of center point (in OCS) 
         // 40  Radius
         //210  Extrusion direction (optional; default = 0, 0, 1) DXF: X value; APP: 3D vector
         //220, 230  DXF: Y and Z values of extrusion direction  (optional)

        public float Radius;        //40

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 40:
                    // 40  Radius
                    Radius = float.Parse(group.Value);
                    break;

                default:
                    base.SetValue(group);
                    break;
            }
        }
    }
}
