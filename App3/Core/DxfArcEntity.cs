
namespace Cirros.Dxf
{
    public class DxfArcEntity : DxfCircleEntity
    {
        public DxfArcEntity(DxfReader reader) : base(reader)
        {
            Type = "ARC";
        }

        //100  Subclass marker (AcDbCircle) 
        // 39  Thickness (optional; default = 0)
        // 10  Center point (in OCS) DXF: X value; APP: 3D point
        // 20, 30  DXF: Y and Z values of center point (in OCS) 
        // 40  Radius
        //100  Subclass marker (AcDbArc) 
        // 50  Start angle
        // 51  End angle
        // 210  Extrusion direction. (optional; default = 0, 0, 1  DXF: X value; APP: 3D vector 
        // 220, 230  DXF: Y and Z values of extrusion direction (optional)

        public float StartAngle;        //50
        public float EndAngle;          //51

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 50:
                    // 50  Start angle
                    StartAngle = float.Parse(group.Value);
                    break;

                case 51:
                    // 51  End angle
                    EndAngle = float.Parse(group.Value);
                    break;

                default:
                    base.SetValue(group);
                    break;
            }
        }
    }
}
