using System.Collections.Generic;
namespace Cirros.Dxf
{
    public class DxfHatchEntity : DxfEntity
    {
        public DxfHatchEntity(DxfReader reader)
            : base(reader)
        {
            Type = "HATCH";
        }

        // 100	Subclass marker (AcDbHatch) 
        //  10	Elevation point (in OCS)
        //          DXF: X value = 0; APP: 3D point (X and Y always equal 0, Z represents the elevation)
        //  20,30	DXF: Y and Z values of elevation point (in OCS)
        //               Y value = 0, Z represents the elevation
        // 210	Extrusion direction (optional; default = 0, 0, 1)
        //          DXF: X value; APP: 3D vector
        // 220,230	DXF: Y and Z values of extrusion direction
        //   2	Hatch pattern name
        //  70	Solid fill flag (solid fill = 1; pattern fill = 0)
        //  71	Associativity flag (associative = 1; non-associative = 0)
        //  91	Number of boundary paths (loops)
        //      varies 	Boundary path data. Repeats number of times specified by code 91. See "Boundary Path Data"
        //  75	Hatch style:
        //         0 = Hatch "odd parity" area (Normal style)
        //         1 = Hatch outermost area only (Outer style)
        //         2 = Hatch through entire area (Ignore style)
        //  76	Hatch pattern type:
        //         0 = User-defined; 1 = Predefined; 2 = Custom
        //  52	Hatch pattern angle (pattern fill only)
        //  41	Hatch pattern scale or spacing (pattern fill only)
        //  77	Hatch pattern double flag (pattern fill only):
        //         0 = not double; 1 = double 
        //  78	Number of pattern definition lines
        //      varies 	Pattern line data. Repeats number of times specified by code 78. See "Pattern Data"
        //  47	Pixel size 
        //  98	Number of seed points
        //  10	Seed point (in OCS)
        //          DXF: X value; APP: 2D point (multiple entries)
        //  20	    DXF: Y value of seed point (in OCS); (multiple entries)

        public string HatchPattern;         //   2	Hatch pattern name
        public int SolidFlag;               //  70	Solid fill flag (solid fill = 1; pattern fill = 0)
        public int AssocFlag;               //  71	Associativity flag (associative = 1; non-associative = 0)
        public int BndryPathCount;          //  91	Number of boundary paths (loops)
        public int HatchStyle;              //  75	Hatch style
        public int PatternType;             //  76	Hatch pattern type
        public float PatternAngle;          //  52	Hatch pattern angle (pattern fill only)
        public float PatternScale;          //  41	Hatch pattern scale or spacing (pattern fill only)
        public float PixelSize;             //  47	Pixel size 
        public int SeedCount;               //  98	Number of seed points

        public List<DxfHatchBoundaryPath> BoundaryList = new List<DxfHatchBoundaryPath>();
        public List<DxfPoint3> SeedList = new List<DxfPoint3>();
        public List<DxfHatchPatternDefinition> PatternList = new List<DxfHatchPatternDefinition>();

        private DxfHatchPatternDefinition _currentPattern = null;

        private DxfHatchBoundaryPath _currentBoundary = null;
        private DxfHatchEdge _currentEdge = null;

        public DxfPoint3 Elevation = null;
        private DxfPoint3 _vertex = null;

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 10:
                    // Not sure why, but there are two kinds of 10/20/30 groups for HATCHes.
                    // From the documentation I assume the first is the Elevation and the rest are 2D Seed points (no 30 groups)
                    // Then there are the embeded boundary edge lists.
                    _vertex = new DxfPoint3();
                    _vertex.X = float.Parse(group.Value);
                    break;
                case 20:
                    if (_vertex != null)
                    {
                        _vertex.Y = float.Parse(group.Value);
                        
                        if (Elevation != null)
                        {
                            if (_currentEdge == null)
                            {
                                //  10	Seed point (in OCS)
                                //          DXF: X value; APP: 2D point (multiple entries)
                                //  20	    DXF: Y value of seed point (in OCS); (multiple entries)
                                SeedList.Add(_vertex);
                            }
                            else if (_currentEdge is DxfHatchArcEdge)
                            {
                                ((DxfHatchArcEdge)_currentEdge).CX = _vertex.X;
                                ((DxfHatchArcEdge)_currentEdge).CY = _vertex.Y;
                            }
                            else if (_currentEdge is DxfHatchEllipticEdge)
                            {
                                ((DxfHatchEllipticEdge)_currentEdge).CX = _vertex.X;
                                ((DxfHatchEllipticEdge)_currentEdge).CY = _vertex.Y;
                            }
                            else if (_currentEdge is DxfHatchLineEdge)
                            {
                                ((DxfHatchLineEdge)_currentEdge).X0 = _vertex.X;
                                ((DxfHatchLineEdge)_currentEdge).Y0 = _vertex.Y;
                            }
                            else if (_currentEdge is DxfHatchPolylineEdge)
                            {
                                DxfLwpolylineVertex v = new DxfLwpolylineVertex(_vertex.X, _vertex.Y, 0);
                                ((DxfHatchPolylineEdge)_currentEdge).VertexList.Add(v);
                            }
                            else if (_currentEdge is DxfHatchSplineEdge)
                            {
                                DxfSplineVertex v = new DxfSplineVertex();
                                v.X0 = _vertex.X;
                                v.Y0 = _vertex.Y;
                                ((DxfHatchSplineEdge)_currentEdge).VertexList.Add(v);
                            }
                            else
                            {
                                //  10	Seed point (in OCS)
                                //          DXF: X value; APP: 2D point (multiple entries)
                                //  20	    DXF: Y value of seed point (in OCS); (multiple entries)
                                SeedList.Add(_vertex);
                            }
                            _vertex = null;
                        }
                    }
                    break;
                case 30:
                    //  30	(Override) DXF: Y and Z values of vertex coordinates  DXF: Z value; APP: 3D point
                    if (_vertex != null)
                    {
                        _vertex.Z = float.Parse(group.Value);

                        if (Elevation == null)
                        {
                            //  10	Elevation point (in OCS)
                            //          DXF: X value = 0; APP: 3D point (X and Y always equal 0, Z represents the elevation)
                            //  20,30	DXF: Y and Z values of elevation point (in OCS)
                            //               Y value = 0, Z represents the elevation
                            Elevation = _vertex;
                        }
                    }
                    break;

                case 11:
                    X1 = float.Parse(group.Value);
                    break;
                case 21:
                    Y1 = float.Parse(group.Value);
                    if (_currentEdge == null)
                    {
                    }
                    else if (_currentEdge is DxfHatchEllipticEdge)
                    {
                        ((DxfHatchEllipticEdge)_currentEdge).AX = X1;
                        ((DxfHatchEllipticEdge)_currentEdge).AY = Y1;
                    }
                    else if (_currentEdge is DxfHatchLineEdge)
                    {
                        ((DxfHatchLineEdge)_currentEdge).X1 = X1;
                        ((DxfHatchLineEdge)_currentEdge).Y1 = Y1;
                    }
                    break;

                case 2:
                    //   2	Hatch pattern name
                    HatchPattern = group.Value;
                    break;

                case 70:
                    //  70	Solid fill flag (solid fill = 1; pattern fill = 0)
                    SolidFlag = int.Parse(group.Value);
                    break;
                case 71:
                    //  71	Associativity flag (associative = 1; non-associative = 0)
                    AssocFlag = int.Parse(group.Value);
                    break;
                case 91:
                    //  91	Number of boundary paths (loops)
                    //      varies 	Boundary path data. Repeats number of times specified by code 91. See "Boundary Path Data"
                    BndryPathCount = int.Parse(group.Value);
                    break;
                case 75:
                    //  75	Hatch style:
                    //         0 = Hatch "odd parity" area (Normal style)
                    //         1 = Hatch outermost area only (Outer style)
                    //         2 = Hatch through entire area (Ignore style)
                    HatchStyle = int.Parse(group.Value);
                    break;
                case 76:
                    //  76	Hatch pattern type:
                    //         0 = User-defined; 1 = Predefined; 2 = Custom
                    PatternType = int.Parse(group.Value);
                    break;

                case 52:
                    //  52	Hatch pattern angle (pattern fill only)
                    PatternAngle = float.Parse(group.Value);
                    break;
                case 41:
                    //  41	Hatch pattern scale or spacing (pattern fill only)
                    PatternScale = float.Parse(group.Value);
                    break;

                case 77:
                    //  77	Hatch pattern double flag (pattern fill only):
                    //         0 = not double; 1 = double 
                    break;
                case 78:
                    //  78	Number of pattern definition lines
                    //      varies 	Pattern line data. Repeats number of times specified by code 78. See "Pattern Data"
                    break;

                case 47:
                    //  47	Pixel size 
                    PixelSize = float.Parse(group.Value);
                    break;

                case 98:
                    //  98	Number of seed points
                    _currentEdge = null;    // (free up the 10/20 groups)
                    SeedCount = int.Parse(group.Value);
                    break;

                case 92:
                    //  92	Boundary path type flag (bit coded):
                    //        0 = Default; 1 = External; 2 = Polyline;
                    //        4 = Derived; 8 = Textbox; 16 = Outermost

                    _currentEdge = null;

                    _currentBoundary = new DxfHatchBoundaryPath();
                    BoundaryList.Add(_currentBoundary);

                    //if (int.Parse(group.Value) == 0)
                    //{
                    //    _currentEdge = new DxfHatchMlineEdge();
                    //    _currentBoundary.EdgeList.Add(_currentEdge);
                    //    _currentBoundary.EdgeCount = 1;
                    //}
                    //else 
                    if ((int.Parse(group.Value) & 0x2) == 0x2)
                    {
                        _currentEdge = new DxfHatchPolylineEdge();
                        _currentBoundary.EdgeList.Add(_currentEdge);
                        _currentBoundary.EdgeCount = 1;
                    }
                    break;

                case 93:
                    //  93	Number of edges in this boundary path (only if boundary is not a polyline)
                    if (_currentBoundary != null)
                    {
                        _currentBoundary.EdgeCount = int.Parse(group.Value);
                    }
                    break;

                case 72:
                    int flag72 = int.Parse(group.Value);
                    if (_currentEdge != null && _currentEdge is DxfHatchPolylineEdge && ((DxfHatchPolylineEdge)_currentEdge).VertexList.Count == 0)
                    {
                        //  72	Has bulge flag
                        ((DxfHatchPolylineEdge)_currentEdge).HasBulgeFlag = flag72;
                    }
                    else if (_currentBoundary != null)
                    {
                        //  72	Edge type (only if boundary is not a polyline):
                        //        1 = Line; 2 = Circular arc; 3 = Elliptic arc; 4 = Spline
                        switch (flag72)
                        {
                            case 1:
                                _currentEdge = new DxfHatchLineEdge();
                                break;
                            case 2:
                                _currentEdge = new DxfHatchArcEdge();
                                break;
                            case 3:
                                _currentEdge = new DxfHatchEllipticEdge();
                                break;
                            case 4:
                                _currentEdge = new DxfHatchSplineEdge();
                                break;
                            default:
                                _currentEdge = null;    // Shouldn't happen
                                break;
                        }

                        if (_currentEdge != null)
                        {
                            _currentBoundary.EdgeList.Add(_currentEdge);
                        }
                    }
                    break;

                case 97:
                    if (_currentBoundary != null)
                    {
                        //  97	Number of source boundary objects
                    }
                    // 330	Reference to source boundary objects (multiple entries)
                    break;

                case 330:
                    if (_currentBoundary != null)
                    {
                        // 330	Reference to source boundary objects (multiple entries)
                    }
                    break;

                case 53:
                    //  53	Pattern line angle

                    // IMPORTANT ASSUMPTION: 53 is always present and is always the first group in a pattern definition
                    _currentPattern = new DxfHatchPatternDefinition();
                    PatternList.Add(_currentPattern);

                    _currentPattern.Angle = float.Parse(group.Value);
                    break;
                case 43:
                    //  43	Pattern line base point, X component
                    if (_currentPattern != null)
                    {
                        _currentPattern.BaseX = float.Parse(group.Value);
                    }
                    break;
                case 44:
                    //  44	Pattern line base point, Y component
                    if (_currentPattern != null)
                    {
                        _currentPattern.BaseY = float.Parse(group.Value);
                    }
                    break;
                case 45:
                    //  45	Pattern line offset, X component
                    if (_currentPattern != null)
                    {
                        _currentPattern.BaseX = float.Parse(group.Value);
                    }
                    break;
                case 46:
                    //  46	Pattern line offset, Y component
                    if (_currentPattern != null)
                    {
                        _currentPattern.BaseY = float.Parse(group.Value);
                    }
                    break;
                case 49:
                    //  49	Dash length (multiple entries)
                    if (_currentPattern != null)
                    {
                        _currentPattern.DashList.Add(float.Parse(group.Value));
                    }
                    break;
                case 79:
                    //  79	Number of dash length items 
                    if (_currentPattern != null)
                    {
                        _currentPattern.DashCount = int.Parse(group.Value);
                    }
                    break;

                default:
                    base.SetValue(group);
                    break;
            }
        }
    }

    public abstract class DxfHatchEdge
    {
        public int EdgeType;        //  72	Edge type (only if boundary is not a polyline):
                                    //        1 = Line; 2 = Circular arc; 3 = Elliptic arc; 4 = Spline
    }

    public class DxfHatchPolylineEdge : DxfHatchEdge
    {
        public DxfHatchPolylineEdge()
        {
            EdgeType = 0;
        }

        public int HasBulgeFlag;    //  72	Has bulge flag
        public int IsClosedFlag;    //  73	Is closed flag
        public int VertexCount;     //  93	Number of polyline vertices

        //  10	Vertex location (in OCS)
        //          DXF: X value; APP: 2D point (multiple entries)
        //  20   	DXF: Y value of vertex location (in OCS) (multiple entries)
        //  42	Bulge (optional, default = 0)
        public List<DxfLwpolylineVertex> VertexList = new List<DxfLwpolylineVertex>();     // 10/20/42
    }

    public class DxfHatchLineEdge : DxfHatchEdge
    {
        public DxfHatchLineEdge()
        {
            EdgeType = 1;
        }

        public float X0;            //  10	Start point (in OCS)   DXF: X value; APP: 2D point 
        public float Y0;            //  20   	DXF: Y value of start point (in OCS)
        public float X1;            //  11	End point (in OCS)     DXF: X value; APP: 2D point 
        public float Y1;            //  21	    DXF: Y value of end point (in OCS)
    }

    public class DxfHatchArcEdge : DxfHatchEdge
    {
        public DxfHatchArcEdge()
        {
            EdgeType = 2;
        }

        public float CX;            //  10	Center point (in OCS)     DXF: X value; APP: 2D point 
        public float CY;            //  20	    DXF: Y value of center point (in OCS) 
        public float Radius;        //  40	Radius
        public float Start;         //  50	Start angle
        public float End;           //  51	End angle
        public int CCWFlag;         //  73	Is counterclockwise flag
    }

    public class DxfHatchEllipticEdge : DxfHatchEdge
    {
        public DxfHatchEllipticEdge()
        {
            EdgeType = 3;
        }

        public float CX;            //  10	Center point (in OCS)     DXF: X value; APP: 2D point 
        public float CY;            //  20	    DXF: Y value of center point (in OCS) 
        public float AX;            //  11	End point of major axis relative to center point (in OCS)  DXF: X value; APP: 2D point
        public float AY;            //  21	    DXF: Y value of end point of major axis (in OCS)
        public float Ratio;         //  40	Length of minor axis (percentage of major axis length)
        public float Start;         //  50	Start angle
        public float End;           //  51	End angle
        public int CCWFlag;         //  73	Is counterclockwise flag
    }

    public class DxfHatchSplineEdge : DxfHatchEdge
    {
        public DxfHatchSplineEdge()
        {
            EdgeType = 4;
        }

        public int Degree;          //  94	Degree
        public int Rational;        //  73	Rational
        public int Periodic;        //  74	Periodic
        public int KnotCount;       //  95	Number of knots
        public int CPCount;         //  96	Number of control points

        public List<float> KnotList = new List<float>();        //  40	Knot values (multiple entries)

        //  10	Control point (in OCS)
        //          DXF: X value; APP: 2D point 
        //  20	    DXF: Y value of control point (in OCS)
        //  42	Weights (optional, default = 1) 
        public List<DxfSplineVertex> VertexList = new List<DxfSplineVertex>();  // 10/20/42
    }

    public class DxfHatchBoundaryPath
    {
        public int BndryPathType;   //  92	Boundary path type flag (bit coded):
                                    //        0 = Default; 1 = External; 2 = Polyline;
                                    //        4 = Derived; 8 = Textbox; 16 = Outermost
        //  varies 	Polyline boundary type data (only if boundary = polyline).
        //          See Polyline boundary data table below.
        public int EdgeCount;       //  93	Number of edges in this boundary path (only if boundary is not a polyline)
        public List<DxfHatchEdge> EdgeList = new List<DxfHatchEdge>();      //  varies 	Edge type data (only if boundary is not a polyline). See appropriate Edge data table below.
        //  97	Number of source boundary objects
        // 330	Reference to source boundary objects (multiple entries)
    }

    public class DxfHatchPatternDefinition
    {
        public float Angle;         //  53	Pattern line angle
        public float BaseX;         //  43	Pattern line base point, X component
        public float BaseY;         //  44	Pattern line base point, Y component
        public float OffsetX;       //  45	Pattern line offset, X component
        public float OffsetY;       //  46	Pattern line offset, Y component
        public int DashCount;       //  79	Number of dash length items 

        public List<float> DashList = new List<float>();    //  49	Dash length (multiple entries)
    }
}
