using System.Collections.Generic;
namespace Cirros.Dxf
{
    class DxfLeaderEntity : DxfEntity
    {
        public DxfLeaderEntity(DxfReader reader)
            : base(reader)
        {
            Type = "LEADER";
        }

        // 100	Subclass marker (AcDbLeader) 
        //   3	Dimension style name
        //  71	Arrowhead flag: 0 = Disabled; 1 = Enabled
        //  72	Leader path type: 0 = Straight line segments; 1 = Spline
        //  73	Leader creation flag (default = 3):
        //        0 = Leader created with text annotation
        //        1 = Created with tolerance annotation
        //        2 = Created with block reference annotation
        //        3 = Created without any annotation
        //  74	Hook line direction flag:
        //        0 = Hook line (or end of tangent for a splined leader) is the opposite direction from the horizontal vector.
        //        1 = Hook line (or end of tangent for a splined leader) is the same direction as horizontal vector (see code 75).
        //  75	Hook line flag: 0 = No hookline; 1 = Has a hookline
        //  40	Text annotation height
        //  41	Text annotation width
        //  76	Number of vertices in leader (ignored for OPEN)
        //  10	Vertex coordinates (one entry for each vertex)
        //          DXF: X value; APP: 3D point
        //  20,30	DXF: Y and Z values of vertex coordinates 
        //  77	Color to use if leader's DIMCLRD = BYBLOCK
        // 340	Hard reference to associated annotation (mtext, tolerance, or insert entity)
        // 210	Normal vector
        //          DXF: X value; APP: 3D vector
        // 220,230	DXF: Y and Z values of normal vector
        // 211	"Horizontal" direction for leader
        //          DXF: X value; APP: 3D vector
        // 221,231	DXF: Y and Z values of "horizontal" direction for leader
        // 212	Offset of last leader vertex from block reference insertion point
        //          DXF: X value; APP: 3D vector
        // 222,232	DXF: Y and Z values of offset
        // 213	Offset of last leader vertex from annotation placement point
        //          DXF: X value; APP: 3D vector
        // 223,233	DXF: Y and Z values of offset

        public float XNormal;       //  210	Normal vector  DXF: X value; APP: 3D vector
        public float YNormal;       //  220,230	DXF: Y and Z values of normal vector
        public float ZNormal;       //  220,230	DXF: Y and Z values of normal vector
        public float XHoriz;        //  211	"Horizontal" direction for leader  DXF: X value; APP: 3D vector
        public float YHoriz;        //  221,231	DXF: Y and Z values of "horizontal" direction for leader
        public float ZHoriz;        //  221,231	DXF: Y and Z values of "horizontal" direction for leader
        public float XOffset;       //  212	Offset of last leader vertex from block reference insertion point
        public float YOffset;       //  222,232	DXF: Y and Z values of offset
        public float ZOffset;       //  222,232	DXF: Y and Z values of offset
        public float XTOffset;      //  213	Offset of last leader vertex from annotation placement point
        public float YTOffset;      //  223,233	DXF: Y and Z values of offset
        public float ZTOffset;      //  223,233	DXF: Y and Z values of offset

        public int Arrowhead = 0;   //  71	Arrowhead flag: 0 = Disabled; 1 = Enabled
        public int LeaderPath = 0;  //  72	Leader path type: 0 = Straight line segments; 1 = Spline
        public float THeight;       //  40	Text annotation height
        public float TWidth;        //  41	Text annotation width
        public int VertexCount = 0; //  76	Number of vertices in leader (ignored for OPEN)

        public List<DxfPoint3> VertexList = new List<DxfPoint3>();

        private DxfPoint3 _vertex = null;

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 10:
                    _vertex = new DxfPoint3();
                    _vertex.X = float.Parse(group.Value);
                    //  10	(Override) Vertex coordinates (one entry for each vertex)  DXF: X value; APP: 3D point
                    break;
                case 20:
                    //  20	(Override) DXF: Y and Z values of vertex coordinates  DXF: Y value; APP: 3D point
                    if (_vertex != null)
                    {
                        _vertex.Y = float.Parse(group.Value);
                    }
                    break;
                case 30:
                    //  30	(Override) DXF: Y and Z values of vertex coordinates  DXF: Z value; APP: 3D point
                    if (_vertex != null)
                    {
                        _vertex.Z = float.Parse(group.Value);
                        VertexList.Add(_vertex);
                    }
                    break;

                case 210:
                    //  210	Normal vector  DXF: X value; APP: 3D vector
                    XNormal = float.Parse(group.Value);
                    break;
                case 220:
                    //  220,230	DXF: Y and Z values of normal vector
                    YNormal = float.Parse(group.Value);
                    break;
                case 230:
                    //  220,230	DXF: Y and Z values of normal vector
                    ZNormal = float.Parse(group.Value);
                    break;

                case 211:
                    //  211	"Horizontal" direction for leader  DXF: X value; APP: 3D vector
                    XHoriz = float.Parse(group.Value);
                    break;
                case 221:
                    //  221,231	DXF: Y and Z values of "horizontal" direction for leader
                    YHoriz = float.Parse(group.Value);
                    break;
                case 231:
                    //  221,231	DXF: Y and Z values of "horizontal" direction for leader
                    ZHoriz = float.Parse(group.Value);
                    break;

                case 212:
                    //  212	Offset of last leader vertex from block reference insertion point
                    XOffset = float.Parse(group.Value);
                    break;
                case 222:
                    //  222,232	DXF: Y and Z values of offset
                    YOffset = float.Parse(group.Value);
                    break;
                case 232:
                    //  222,232	DXF: Y and Z values of offset
                    ZOffset = float.Parse(group.Value);
                    break;

                case 213:
                    //  213	Offset of last leader vertex from annotation placement point
                    XTOffset = float.Parse(group.Value);
                    break;
                case 223:
                    //  223,233	DXF: Y and Z values of offset
                    YTOffset = float.Parse(group.Value);
                    break;
                case 233:
                    //  223,233	DXF: Y and Z values of offset
                    ZTOffset = float.Parse(group.Value);
                    break;

                case 71:
                    //  71	Arrowhead flag: 0 = Disabled; 1 = Enabled
                    Arrowhead = int.Parse(group.Value);
                    break;
                case 72:
                    //  72	Leader path type: 0 = Straight line segments; 1 = Spline
                    LeaderPath = int.Parse(group.Value);
                    break;
                case 73:
                    //  73	Leader creation flag (default = 3):
                    //        0 = Leader created with text annotation
                    //        1 = Created with tolerance annotation
                    //        2 = Created with block reference annotation
                    //        3 = Created without any annotation
                    break;
                case 74:
                    //  74	Hook line direction flag:
                    //        0 = Hook line (or end of tangent for a splined leader) is the opposite direction from the horizontal vector.
                    //        1 = Hook line (or end of tangent for a splined leader) is the same direction as horizontal vector (see code 75).
                    break;
                case 75:
                    //  75	Hook line flag: 0 = No hookline; 1 = Has a hookline
                    break;
                case 42:
                    //  76	Number of vertices in leader (ignored for OPEN)
                    break;
                case 77:
                    //  77	Color to use if leader's DIMCLRD = BYBLOCK
                    break;
                case 340:
                    // 340	Hard reference to associated annotation (mtext, tolerance, or insert entity)
                    break;
                case 40:
                    //  40	Text annotation height
                    THeight = float.Parse(group.Value);
                    break;
                case 41:
                    //  41	Text annotation width
                    TWidth = float.Parse(group.Value);
                    break;

                default:
                    base.SetValue(group);
                    break;
            }
        }
    }
}
