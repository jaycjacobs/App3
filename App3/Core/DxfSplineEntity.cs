using System.Collections.Generic;
namespace Cirros.Dxf
{
    class DxfSplineEntity : DxfEntity
    {
        public DxfSplineEntity(DxfReader reader)
            : base(reader)
        {
            Type = "SPLINE";
        }

        // 100	Subclass marker (AcDbSpline) 
        // 210	Normal vector (omitted if the spline is nonplanar)
        //          DXF: X value; APP: 3D vector
        // 220,230	DXF: Y and Z values of normal vector (optional)
        //  70	Spline flag (bit coded):
        //        1 = Closed spline
        //        2 = Periodic spline
        //        4 = Rational spline
        //        8 = Planar
        //        16 = Linear (planar bit is also set) 
        //  71	Degree of the spline curve
        //  72	Number of knots
        //  73	Number of control points
        //  74	Number of fit points (if any)
        //  42	Knot tolerance (default = 0.0000001)
        //  43	Control-point tolerance (default = 0.0000001)
        //  44	Fit tolerance (default = 0.0000000001)
        //  12	Start tangent-may be omitted (in WCS)
        //          DXF: X value; APP: 3D point
        //  22,32	DXF: Y and Z values of start tangent-may be omitted (in WCS)
        //  13	End tangent-may be omitted (in WCS)
        //          DXF: X value; APP: 3D point
        //  23,33	DXF: Y and Z values of end tangent-may be omitted (in WCS)
        //  40	Knot value (one entry per knot)
        //  41	Weight (if not 1); with multiple group pairs, are present if all are not 1
        //  10	Control points (in WCS), one entry per control point
        //          DXF: X value; APP: 3D point
        //  20,30	DXF: Y and Z values of control points (in WCS), one entry per control point
        //  11	Fit points (in WCS), one entry per fit point
        //          DXF: X value; APP: 3D point
        //  21,31	DXF: Y and Z values of fit points (in WCS), one entry per fit point

        public float X2;            //  12	Start tangent-may be omitted (in WCS)
        public float Y2;            //  22,32	DXF: Y and Z values of start tangent-may be omitted (in WCS)
        public float Z2;            //  22,32	DXF: Y and Z values of start tangent-may be omitted (in WCS)
        public float X3;            //  13	End tangent-may be omitted (in WCS)
        public float Y3;            //  23,33	DXF: Y and Z values of end tangent-may be omitted (in WCS)
        public float Z3;            //  23,33	DXF: Y and Z values of end tangent-may be omitted (in WCS)
        public float XNormal;       //  210	Normal vector (omitted if the spline is nonplanar)
        public float YNormal;       //  220,230	DXF: Y and Z values of normal vector (optional)
        public float ZNormal;       //  220,230	DXF: Y and Z values of normal vector (optional)

        public int CPCount = 0;     //  73	Number of control points
        public int KnotCount = 0;   //  72	Number of knots
        public int FitCount = 0;    //  74	Number of fit points (if any)
        public int Degree = 0;      //  71	Degree of the spline curve

        public List<DxfSplineVertex> ControlPointList = new List<DxfSplineVertex>();
        public List<DxfSplineVertex> FitPointList = new List<DxfSplineVertex>();
        public List<float> KnotList = new List<float>();

        private DxfSplineVertex _control = null;
        private DxfSplineVertex _fit = null;

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 10:
                    _control = new DxfSplineVertex();
                    _control.X0 = float.Parse(group.Value);
                    //  10	(Override) Control points (in WCS), one entry per control point  DXF: X value; APP: 3D point
                    break;
                case 20:
                    //  20	(Override) Control points (in WCS), one entry per control point  DXF: Y value; APP: 3D point
                    if (_control != null)
                    {
                        _control.Y0 = float.Parse(group.Value);
                    }
                    break;
                case 30:
                    //  30	(Override) Control points (in WCS), one entry per control point  DXF: Z value; APP: 3D point
                    if (_control != null)
                    {
                        _control.Z0 = float.Parse(group.Value);
                        ControlPointList.Add(_control);
                    }
                    break;
                case 11:
                    //  11	(Override) Fit points (in WCS), one entry per fit point  DXF: X value; APP: 3D point
                    _fit = new DxfSplineVertex();
                    _fit.X0 = float.Parse(group.Value);
                    break;
                case 21:
                    //  21	(Override) Fit points (in WCS), one entry per fit point  DXF: X value; APP: 3D point
                    if (_fit != null)
                    {
                        _fit.Y0 = float.Parse(group.Value);
                    }
                    break;
                case 31:
                    //  31	(Override) Fit points (in WCS), one entry per fit point  DXF: X value; APP: 3D point
                    if (_fit != null)
                    {
                        _fit.Z0 = float.Parse(group.Value);
                        FitPointList.Add(_fit);
                    }
                    break;
                case 210:
                    //  210	Normal vector (omitted if the spline is nonplanar)
                    XNormal = float.Parse(group.Value);
                    break;
                case 220:
                    //  220,230	DXF: Y and Z values of normal vector (optional)
                    YNormal = float.Parse(group.Value);
                    break;
                case 230:
                    //  220,230	DXF: Y and Z values of normal vector (optional)
                    ZNormal = float.Parse(group.Value);
                    break;
                case 12:
                    //  12	Start tangent-may be omitted (in WCS)
                    X2 = float.Parse(group.Value);
                    break;
                case 22:
                    //  22,32	DXF: Y and Z values of start tangent-may be omitted (in WCS)
                    Y2 = float.Parse(group.Value);
                    break;
                case 32:
                    //  22,32	DXF: Y and Z values of start tangent-may be omitted (in WCS)
                    Z2 = float.Parse(group.Value);
                    break;
                case 13:
                    //  13	End tangent-may be omitted (in WCS)
                    X3 = float.Parse(group.Value);
                    break;
                case 23:
                    //  23,33	DXF: Y and Z values of end tangent-may be omitted (in WCS)
                    Y3 = float.Parse(group.Value);
                    break;
                case 33:
                    //  23,33	DXF: Y and Z values of end tangent-may be omitted (in WCS)
                    Z3 = float.Parse(group.Value);
                    break;
                case 71:
                    //  71	Degree of the spline curve
                    Degree = int.Parse(group.Value);
                    break;
                case 72:
                    //  72	Number of knots
                    KnotCount = int.Parse(group.Value);
                    break;
                case 73:
                    //  73	Number of control points
                    CPCount = int.Parse(group.Value);
                    break;
                case 74:
                    //  74	Number of fit points (if any)
                    FitCount = int.Parse(group.Value);
                    break;
                case 42:
                    //  42	Knot tolerance (default = 0.0000001)
                    break;
                case 43:
                    //  43	Control-point tolerance (default = 0.0000001)
                    break;
                case 40:
                    //  40	Knot value (one entry per knot)
                    KnotList.Add(float.Parse(group.Value));
                    break;
                default:
                    base.SetValue(group);
                    break;
            }
        }
    }

    public class DxfSplineVertex
    {
        public float X0 = 0;
        public float Y0 = 0;
        public float Z0 = 0;
        public float Knot = 0;

        public DxfSplineVertex()
        {
        }

        //public DxfSplineVertex(float x, float y, float z)
        //{
        //    X0 = x;
        //    Y0 = y;
        //    Z0 = z;
        //}
    }
}
