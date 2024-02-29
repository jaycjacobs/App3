
using System.Collections.Generic;
namespace Cirros.Dxf
{
    class DxfLwpolylineEntity : DxfEntity
    {
        public DxfLwpolylineEntity(DxfReader reader)
            : base(reader)
        {
            Type = "LWPOLYLINE";
        }

         //100 Subclass marker (AcDbPolyline) 
         //39  Thickness (optional; default = 0)
         //10 Vertex coordinates (in OCS), multiple entries; one entry for each vertexDXF: X value; APP: 2D point 
         //20 DXF: Y value of vertex coordinates (in OCS), multiple entries; one entry for each vertex
         //210 Extrusion direction (optional; default = 0, 0, 1)DXF: X value; APP: 3D vector
         //220, 230 DXF: Y and Z values of extrusion direction (optional)

         //70 Polyline flag (bit-coded); default is 0:1 = Closed; 128 = Plinegen
         //40  Starting width (multiple entries; one entry for each vertex) (optional; default = 0; multiple entries). Not used if constant width (code 43) is set
         //41  End width (multiple entries; one entry for each vertex) (optional; default = 0; multiple entries). Not used if constant width (code 43) is set
         //42  Bulge (multiple entries; one entry for each vertex) (optional; default = 0)
         //90 Number of vertices
         //43  Constant width (optional; default = 0). Not used if variable width (codes 40 and/or 41) is set
         //38  Elevation (optional; default = 0)

        public int Flags;               //70
        public float ConstantWidth;     //43
        public float Elevation;         //38
        public int VertexCount;         //90

        public List<DxfLwpolylineVertex> VertexList = new List<DxfLwpolylineVertex>();

        private DxfLwpolylineVertex _vertex = null;

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 40:
                    if (_vertex != null)
                    {
                        _vertex.StartWidth = float.Parse(group.Value);
                    }
                    break;
                case 10:
                    // Override DxfEntity
                    _vertex = new DxfLwpolylineVertex();
                    VertexList.Add(_vertex);
                    _vertex.X0 = float.Parse(group.Value);
                    break;
                case 20:
                    // Override DxfEntity
                    if (_vertex != null)
                    {
                        _vertex.Y0 = float.Parse(group.Value);
                    }
                    break;
                case 41:
                    if (_vertex != null)
                    {
                        _vertex.EndWidth = float.Parse(group.Value);
                    }
                    break;
                case 42:
                    if (_vertex != null)
                    {
                        _vertex.Bulge = float.Parse(group.Value);
                    }
                    break;
                case 70:
                    Flags = int.Parse(group.Value);
                    break;
                case 90:
                    VertexCount = int.Parse(group.Value);
                    break;
                case 43:
                    ConstantWidth = float.Parse(group.Value);
                    break;
                case 38:
                    Elevation = float.Parse(group.Value);
                    break;
                default:
                    base.SetValue(group);
                    break;
            }
        }
    }

    public class DxfLwpolylineVertex
    {
        public float X0 = 0;
        public float Y0 = 0;
        public float Bulge = 0;
        public float StartWidth = 0;
        public float EndWidth = 0;

        public DxfLwpolylineVertex()
        {
        }

        public DxfLwpolylineVertex(float x, float y, float bulge)
        {
            X0 = x;
            Y0 = y;
            Bulge = bulge;
        }
    }
}
