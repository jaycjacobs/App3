using System.Collections.Generic;

namespace Cirros.Dxf
{
    class DxfPolylineEntity : DxfEntity
    {
        public DxfPolylineEntity(DxfReader reader)
            : base(reader)
        {
            Type = "POLYLINE";
        }

        //100  Subclass marker (AcDb2dPolyline or AcDb3dPolyline) 
        // 10  DXF: always 0 APP: a "dummy" point; the X and Y values are always 0, and the Z value is the polyline's elevation (in OCS when 2D, WCS when 3D)
        // 20  DXF: always 0
        // 30  DXF: polyline's elevation (in OCS when 2D, WCS when 3D)
        // 39  Thickness (optional; default = 0)
        //210  Extrusion direction (optional; default = 0, 0, 1) DXF: X value; APP: 3D vector
        //220, 230  DXF: Y and Z values of extrusion direction (optional)

        // 70  Polyline flag (bit-coded); default is 0:
        //        1 = This is a closed polyline (or a polygon mesh closed in the 
        //        M direction).
        //        2 = Curve-fit vertices have been added.
        //        4 = Spline-fit vertices have been added.
        //        8 = This is a 3D polyline.
        //        16 = This is a 3D polygon mesh.
        //        32 = The polygon mesh is closed in the N direction.
        //        64 = The polyline is a polyface mesh.
        //        128 = The linetype pattern is generated continuously around the vertices of this polyline.
        // 40  Default start width (optional; default = 0)
        // 41  Default end width (optional; default = 0)
        // 71  Polygon mesh M vertex count (optional; default = 0)
        // 72  Polygon mesh N vertex count (optional; default = 0)
        // 73  Smooth surface M density (optional; default = 0)
        // 74  Smooth surface N density (optional; default = 0)
        // 75  Curves and smooth surface type (optional; default = 0); integer codes, not bit-coded:
        //        0 = No smooth surface fitted
        //        5 = Quadratic B-spline surface
        //        6 = Cubic B-spline surface
        //        8 = Bezier surface

        public float StartWidth;        //40
        public float EndWidth;          //41
        public int Flags;               //70
        public int MCount;              //71
        public int NCount;              //72
        public int MDensity;            //73
        public int NDensity;            //74
        public int SmoothType;          //75

        protected List<DxfVertexEntity> _vertexList = new List<DxfVertexEntity>();

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
                case 70:
                    Flags = int.Parse(group.Value);
                    break;
                case 71:
                    MCount = int.Parse(group.Value);
                    break;
                case 72:
                    NCount = int.Parse(group.Value);
                    break;
                case 73:
                    MDensity = int.Parse(group.Value);
                    break;
                case 74:
                    NDensity = int.Parse(group.Value);
                    break;
                case 75:
                    SmoothType = int.Parse(group.Value);
                    break;
                default:
                    base.SetValue(group);
                    break;
            }
        }

        public List<DxfVertexEntity> VertexList
        {
            get
            {
                return _vertexList;
            }
        }
    }
}
