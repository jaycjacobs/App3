
namespace Cirros.Dxf
{
    public class DxfVertexEntity : DxfEntity
    {
        public DxfVertexEntity(DxfReader reader)
            : base(reader)
        {
            Type = "VERTEX";
        }

        //100 Subclass marker (AcDbVertex) 
        //100 Subclass marker (AcDb2dVertex or AcDb3dPolylineVertex) 
        // 10 Location point (in OCS when 2D, and WCS when 3D)DXF: X value; APP: 3D point 
        // 20, 30 DXF: Y and Z values of location point (in OCS when 2D, and WCS when 3D)

        // 40 Starting width (optional; default is 0)
        // 41 Ending width (optional; default is 0)
        // 42 Bulge (optional; default is 0). The bulge is the tangent of one fourth the included angle for an arc segment, made negative if the arc goes clockwise from the start point to the endpoint. A bulge of 0 indicates a straight segment, and a bulge of 1 is a semicircle.
        // 70 Vertex flags:
        //        1 = Extra vertex created by curve-fitting
        //        2 = Curve-fit tangent defined for this vertex. A curve-fit tangent direction of 0 may be omitted from DXF output but is significant if this bit is set.
        //        4 = Not used
        //        8 = Spline vertex created by spline-fitting
        //        16 = Spline frame control point
        //        32 = 3D polyline vertex
        //        64 = 3D polygon mesh
        //        128 = Polyface mesh vertex
        // 50 Curve fit tangent direction
        // 71 Polyface mesh vertex index. Optional. Present only if nonzero
        // 72 Polyface mesh vertex index. Optional. Present only if nonzero
        // 73 Polyface mesh vertex index. Optional. Present only if nonzero
        // 74  Polyface mesh vertex index. Optional. Present only if nonzero

        public float StartWidth;        //40
        public float EndWidth;          //41
        public float Bulge;             //42
        public int Flags;               //70
        public int VertexIndex1;        //71
        public int VertexIndex2;        //72
        public int VertexIndex3;        //73
        public int VertexIndex4;        //74
        public float TangentDir;        //50

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 40:
                    StartWidth = float.Parse(group.Value);
                    break;
                case 41:
                    EndWidth = float.Parse(group.Value);
                    break;
                case 42:
                    Bulge = float.Parse(group.Value);
                    break;
                case 70:
                    Flags = int.Parse(group.Value);
                    break;
                case 71:
                    VertexIndex1 = int.Parse(group.Value);
                    break;
                case 72:
                    VertexIndex2 = int.Parse(group.Value);
                    break;
                case 73:
                    VertexIndex3 = int.Parse(group.Value);
                    break;
                case 74:
                    VertexIndex4 = int.Parse(group.Value);
                    break;
                case 50:
                    TangentDir = float.Parse(group.Value);
                    break;
                default:
                    base.SetValue(group);
                    break;
            }
        }
    }
}
