using Cirros.Primitives;
using Cirros.Core;
using System;
#if UWP
using Cirros.Commands;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
#else
using System.Windows;
using System.Windows.Media;
using static CirrosCore.WpfStubs;
using CirrosCore;
#endif

namespace Cirros.Drawing
{
    public class DrawingHeader
    {
        int _version = 3;

        public DrawingHeader()
        {
        }

        public int Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
            }
        }

        public Size PaperSize
        {
            get
            {
                return Globals.ActiveDrawing.PaperSize;
            }
            set
            {
                Globals.ActiveDrawing.PaperSize = value;
            }
        }

        public string Theme
        {
            get
            {
                foreach (string key in Globals.Themes.Keys)
                {
                    if (Globals.Themes[key] == Globals.ActiveDrawing.Theme)
                    {
                        return key;
                    }
                }
                return "light";
            }
            set
            {
                Globals.ActiveDrawing.Theme = Globals.Themes[value];
            }
        }

        public Unit PaperUnit
        {
            get
            {
                return Globals.ActiveDrawing.PaperUnit;
            }
            set
            {
                Globals.ActiveDrawing.PaperUnit = value;
            }
        }

        public Unit ModelUnit
        {
            get
            {
                return Globals.ActiveDrawing.ModelUnit;
            }
            set
            {
                Globals.ActiveDrawing.ModelUnit = value;
            }
        }

        public double ModelScale
        {
            get
            {
                return Globals.ActiveDrawing.Scale;
            }
            set
            {
                Globals.ActiveDrawing.Scale = value;
            }
        }

        public Point ModelOrigin
        {
            get
            {
                return Globals.ActiveDrawing.Origin;
            }
            set
            {
                Globals.ActiveDrawing.Origin = value;
            }
        }

        public bool Architect
        {
            get
            {
                return Globals.ActiveDrawing.IsArchitecturalScale;
            }
            set
            {
                Globals.ActiveDrawing.IsArchitecturalScale = value;
            }
        }

        public double GridSpacing
        {
            get
            {
                return Globals.GridSpacing;
            }
            set
            {
                Globals.GridSpacing = value;
            }
        }

        public bool GridVisible
        {
            get
            {
                return Globals.GridIsVisible;
            }
            set
            {
                Globals.GridIsVisible = value;
            }
        }

        public int GridDivisions
        {
            get
            {
                return Globals.GridDivisions;
            }
            set
            {
                Globals.GridDivisions = value;
            }
        }

        public bool GridSnap
        {
            get
            {
                return Globals.Input.GridSnap;
            }
            set
            {
                Globals.Input.GridSnap = value;
            }
        }

        public GridSnapMode GridSnapMode
        {
            get
            {
                return Globals.Input.GridSnapMode;
            }
            set
            {
                Globals.Input.GridSnapMode = value;
            }
        }

        public int DimensionRounding
        {
            get
            {
                return Globals.DimensionRound;
            }
            set
            {
                Globals.DimensionRound = value;
            }
        }

        public bool ShowDimensionUnit
        {
            get
            {
                return Globals.ShowDimensionUnit;
            }
            set
            {
                Globals.ShowDimensionUnit = value;
            }
        }

        public bool SelectMoveByOffset
        {
            // Drawing version 2
            get
            {
                return Globals.SelectMoveByOffset;
            }
            set
            {
                Globals.SelectMoveByOffset = value;
            }
        }

        public double SelectMoveOffsetX
        {
            // Drawing version 2
            get
            {
                return Globals.SelectMoveOffsetX;
            }
            set
            {
                Globals.SelectMoveOffsetX = value;
            }
        }

        public double SelectMoveOffsetY
        {
            // Drawing version 2
            get
            {
                return Globals.SelectMoveOffsetY;
            }
            set
            {
                Globals.SelectMoveOffsetY = value;
            }
        }

        public double EditOffsetLineDistance
        {
            // Drawing version 2
            get
            {
                return Globals.EditOffsetLineDistance;
            }
            set
            {
                Globals.EditOffsetLineDistance = value;
            }
        }

        public bool LinearCopyRepeatConnect
        {
            // Drawing version 2
            get
            {
                return Globals.LinearCopyRepeatConnect;
            }
            set
            {
                Globals.LinearCopyRepeatConnect = value;
            }
        }

        public bool LinearCopyRepeatAtEnd
        {
            // Drawing version 2
            get
            {
                return Globals.LinearCopyRepeatAtEnd;
            }
            set
            {
                Globals.LinearCopyRepeatAtEnd = value;
            }
        }

        public double LinearCopyRepeatDistance
        {
            // Drawing version 2
            get
            {
                return Globals.LinearCopyRepeatDistance;
            }
            set
            {
                Globals.LinearCopyRepeatDistance = value;
            }
        }

        public int LinearCopyRepeatCount
        {
            // Drawing version 2
            get
            {
                return Globals.LinearCopyRepeatCount;
            }
            set
            {
                Globals.LinearCopyRepeatCount = value;
            }
        }

        public bool RadialCopyRepeatConnect
        {
            // Drawing version 2
            get
            {
                return Globals.RadialCopyRepeatConnect;
            }
            set
            {
                Globals.RadialCopyRepeatConnect = value;
            }
        }

        public bool RadialCopyRepeatAtEnd
        {
            // Drawing version 2
            get
            {
                return Globals.RadialCopyRepeatAtEnd;
            }
            set
            {
                Globals.RadialCopyRepeatAtEnd = value;
            }
        }

        public double RadialCopyRepeatAngle
        {
            // Drawing version 2
            get
            {
                return Globals.RadialCopyRepeatAngle;
            }
            set
            {
                Globals.RadialCopyRepeatAngle = value;
            }
        }

        public int RadialCopyRepeatCount
        {
            // Drawing version 2
            get
            {
                return Globals.RadialCopyRepeatCount;
            }
            set
            {
                Globals.RadialCopyRepeatCount = value;
            }
        }

        public long ActiveTime
        {
            get
            {
                return Globals.ActiveDrawing.ActiveTime.Ticks;
            }
            set
            {
                Globals.ActiveDrawing.ActiveTime = new TimeSpan(value);
            }
        }

        public int MinZIndex
        {
            get
            {
                return Globals.ActiveDrawing.MinZIndex;
            }
            set
            {
                Globals.ActiveDrawing.MinZIndex = value;
            }
        }

        public int MaxZIndex
        {
            get
            {
                return Globals.ActiveDrawing.MaxZIndex;
            }
            set
            {
                Globals.ActiveDrawing.MaxZIndex = value;
            }
        }

        public int LayerId
        {
            get
            {
                return Globals.LayerId;
            }
            set
            {
                Globals.LayerId = value;
            }
        }

        public int TextLayerId
        {
            get
            {
                return Globals.TextLayerId;
            }
            set
            {
                Globals.TextLayerId = value;
            }
        }

        public int DimensionLayerId
        {
            get
            {
                return Globals.DimensionLayerId;
            }
            set
            {
                Globals.DimensionLayerId = value;
            }
        }

        public int ActiveLayerId
        {
            get { return Globals.ActiveLayerId; }
            set { Globals.ActiveLayerId = value; }
        }


        public int ActiveLineLayerId
        {
            get { return Globals.ActiveLineLayerId; }
            set { Globals.ActiveLineLayerId = value; }
        }


        public int ActiveDoubleLineLayerId
        {
            get { return Globals.ActiveDoubleLineLayerId; }
            set { Globals.ActiveDoubleLineLayerId = value; }
        }


        public int ActiveRectangleLayerId
        {
            get { return Globals.ActiveRectangleLayerId; }
            set { Globals.ActiveRectangleLayerId = value; }
        }


        public int ActivePolygonLayerId
        {
            get { return Globals.ActivePolygonLayerId; }
            set { Globals.ActivePolygonLayerId = value; }
        }


        public int ActiveCircleLayerId
        {
            get { return Globals.ActiveCircleLayerId; }
            set { Globals.ActiveCircleLayerId = value; }
        }


        public int ActiveArcLayerId
        {
            get { return Globals.ActiveArcLayerId; }
            set { Globals.ActiveArcLayerId = value; }
        }


        public int ActiveEllipseLayerId
        {
            get { return Globals.ActiveEllipseLayerId; }
            set { Globals.ActiveEllipseLayerId = value; }
        }


        public int ActiveCurveLayerId
        {
            get { return Globals.ActiveCurveLayerId; }
            set { Globals.ActiveCurveLayerId = value; }
        }


        public int ActiveDimensionLayerId
        {
            get { return Globals.ActiveDimensionLayerId; }
            set { Globals.ActiveDimensionLayerId = value; }
        }


        public int ActiveArrowLayerId
        {
            get { return Globals.ActiveArrowLayerId; }
            set { Globals.ActiveArrowLayerId = value; }
        }


        public int ActiveImageLayerId
        {
            get { return Globals.ActiveImageLayerId; }
            set { Globals.ActiveImageLayerId = value; }
        }


        public int ActiveInstanceLayerId
        {
            get { return Globals.ActiveInstanceLayerId; }
            set { Globals.ActiveInstanceLayerId = value; }
        }



        public int ActiveTextLayerId
        {
            get { return Globals.ActiveTextLayerId; }
            set { Globals.ActiveTextLayerId = value; }
        }

        public uint ColorSpec
        {
            get
            {
                return Globals.ColorSpec;
            }
            set
            {
                Globals.ColorSpec = value;
            }
        }

        public int LineWeightId
        {
            get
            {
                return Globals.LineWeightId;
            }
            set
            {
                Globals.LineWeightId = value;
            }
        }

        public int LineTypeId
        {
            get
            {
                return Globals.LineTypeId;
            }
            set
            {
                Globals.LineTypeId = value;
            }
        }

        public PenLineCap LineEndCap
        {
            get
            {
                return Globals.ActiveDrawing.LineEndCap;
            }
            set
            {
                Globals.ActiveDrawing.LineEndCap = value;
            }
        }

        public LineCommandType LineConstructType
        {
            get
            {
                return Globals.LineCommandType;
            }
            set
            {
                Globals.LineCommandType = value;
            }
        }

        public double FilletRadius
        {
            // Drawing version 2
            get
            {
                return Globals.FilletRadius;
            }
            set
            {
                Globals.FilletRadius = value;
            }
        }

        public double PolygonFilletRadius
        {
            // Drawing version 2
            get
            {
                return Globals.PolygonFilletRadius;
            }
            set
            {
                Globals.PolygonFilletRadius = value;
            }
        }

        public uint PolygonSides
        {
            get
            {
                return Globals.PolygonSides;
            }
            set
            {
                Globals.PolygonSides = value;
            }
        }
        public double DoublelineWidth
        {
            get
            {
                return Globals.DoubleLineWidth;
            }
            set
            {
                Globals.DoubleLineWidth = value;
            }
        }

        public DbEndStyle DoublelineEndstyle
        {
            get
            {
                return Globals.DoublelineEndStyle;
            }
            set
            {
                Globals.DoublelineEndStyle = value;
            }
        }

        public ArcCommandType ArcType
        {
            get
            {
                return Globals.ArcCommandType;
            }
            set
            {
                Globals.ArcCommandType = value;
            }
        }

        public double ArcRadius
        {
            get
            {
                return Globals.ArcRadius;
            }
            set
            {
                Globals.ArcRadius = value;
            }
        }

        public EllipseCommandType EllipseType
        {
            get
            {
                return Globals.EllipseCommandType;
            }
            set
            {
                Globals.EllipseCommandType = value;
            }
        }

        public double ArcStartAngle
        {
            get
            {
                return Globals.ArcStartAngle;
            }
            set
            {
                Globals.ArcStartAngle = value;
            }
        }

        public double ArcIncludedAngle
        {
            get
            {
                return Globals.ArcIncludedAngle;
            }
            set
            {
                Globals.ArcIncludedAngle = value;
            }
        }

        public double EllipseMajorLength
        {
            get
            {
                return Globals.EllipseMajorLength;
            }
            set
            {
                Globals.EllipseMajorLength = value;
            }
        }

        public double EllipseMajorMinorRatio
        {
            get
            {
                return Globals.EllipseMajorMinorRatio;
            }
            set
            {
                Globals.EllipseMajorMinorRatio = value;
            }
        }

        public double EllipseAxisAngle
        {
            get
            {
                return Globals.EllipseAxisAngle;
            }
            set
            {
                Globals.EllipseAxisAngle = value;
            }
        }

        public double EllipseStartAngle
        {
            get
            {
                return Globals.EllipseStartAngle;
            }
            set
            {
                Globals.EllipseStartAngle = value;
            }
        }

        public double EllipseIncludedAngle
        {
            get
            {
                return Globals.EllipseIncludedAngle;
            }
            set
            {
                Globals.EllipseIncludedAngle = value;
            }
        }

        public ArcCommandType CircleType
        {
            get
            {
                return Globals.CircleCommandType;
            }
            set
            {
                Globals.CircleCommandType = value;
            }
        }

        public double CircleRadius
        {
            get
            {
                return Globals.CircleRadius;
            }
            set
            {
                Globals.CircleRadius = value;
            }
        }

        public RectangleCommandType RectangleType
        {
            get
            {
                return Globals.RectangleType;
            }
            set
            {
                Globals.RectangleType = value;
            }
        }

        public double RectangleWidth
        {
            get
            {
                return Globals.RectangleWidth;
            }
            set
            {
                Globals.RectangleWidth = value;
            }
        }

        public double RectangleHeight
        {
            get
            {
                return Globals.RectangleHeight;
            }
            set
            {
                Globals.RectangleHeight = value;
            }
        }

        public uint LineColorSpec
        {
            get { return Globals.LineColorSpec; }
            set { Globals.LineColorSpec = value; }
        }

        public uint RectangleColorSpec
        {
            get { return Globals.RectangleColorSpec; }
            set { Globals.RectangleColorSpec = value; }
        }

        public uint DoubleLineColorSpec
        {
            get { return Globals.DoubleLineColorSpec; }
            set { Globals.DoubleLineColorSpec = value; }
        }

        public uint PolygonColorSpec
        {
            get { return Globals.PolygonColorSpec; }
            set { Globals.PolygonColorSpec = value; }
        }

        public uint CircleColorSpec
        {
            get { return Globals.CircleColorSpec; }
            set { Globals.CircleColorSpec = value; }
        }

        public uint ArcColorSpec
        {
            get { return Globals.ArcColorSpec; }
            set { Globals.ArcColorSpec = value; }
        }

        public uint EllipseColorSpec
        {
            get { return Globals.EllipseColorSpec; }
            set { Globals.EllipseColorSpec = value; }
        }

        public uint CurveColorSpec
        {
            get { return Globals.CurveColorSpec; }
            set { Globals.CurveColorSpec = value; }
        }

        public uint TextColorSpec
        {
            get { return Globals.TextColorSpec; }
            set { Globals.TextColorSpec = value; }
        }

        public uint DimensionColorSpec
        {
            get { return Globals.DimensionColorSpec; }
            set { Globals.DimensionColorSpec = value; }
        }

        public uint ArrowColorSpec
        {
            get { return Globals.ArrowColorSpec; }
            set { Globals.ArrowColorSpec = value; }
        }

        public int LineLineTypeId
        {
            get { return Globals.LineLineTypeId; }
            set { Globals.LineLineTypeId = value; }
        }

        public int DoubleLineLineTypeId
        {
            get { return Globals.DoubleLineLineTypeId; }
            set { Globals.DoubleLineLineTypeId = value; }
        }

        public int RectangleLineTypeId
        {
            get { return Globals.RectangleLineTypeId; }
            set { Globals.RectangleLineTypeId = value; }
        }

        public int PolygonLineTypeId
        {
            get { return Globals.PolygonLineTypeId; }
            set { Globals.PolygonLineTypeId = value; }
        }

        public int CircleLineTypeId
        {
            get { return Globals.CircleLineTypeId; }
            set { Globals.CircleLineTypeId = value; }
        }

        public int ArcLineTypeId
        {
            get { return Globals.ArcLineTypeId; }
            set { Globals.ArcLineTypeId = value; }
        }

        public int EllipseLineTypeId
        {
            get { return Globals.EllipseLineTypeId; }
            set { Globals.EllipseLineTypeId = value; }
        }

        public int CurveLineTypeId
        {
            get { return Globals.CurveLineTypeId; }
            set { Globals.CurveLineTypeId = value; }
        }

        public int DimensionLineTypeId
        {
            get { return Globals.DimensionLineTypeId; }
            set { Globals.DimensionLineTypeId = value; }
        }

        public int ArrowLineTypeId
        {
            get { return Globals.ArrowLineTypeId; }
            set { Globals.ArrowLineTypeId = value; }
        }

        public int LineLineWeightId
        {
            get { return Globals.LineLineWeightId; }
            set { Globals.LineLineWeightId = value; }
        }

        public int DoubleLineLineWeightId
        {
            get { return Globals.DoubleLineLineWeightId; }
            set { Globals.DoubleLineLineWeightId = value; }
        }

        public int RectangleLineWeightId
        {
            get { return Globals.RectangleLineWeightId; }
            set { Globals.RectangleLineWeightId = value; }
        }

        public int PolygonLineWeightId
        {
            get { return Globals.PolygonLineWeightId; }
            set { Globals.PolygonLineWeightId = value; }
        }

        public int CircleLineWeightId
        {
            get { return Globals.CircleLineWeightId; }
            set { Globals.CircleLineWeightId = value; }
        }

        public int ArcLineWeightId
        {
            get { return Globals.ArcLineWeightId; }
            set { Globals.ArcLineWeightId = value; }
        }

        public int EllipseLineWeightId
        {
            get { return Globals.EllipseLineWeightId; }
            set { Globals.EllipseLineWeightId = value; }
        }

        public int CurveLineWeightId
        {
            get { return Globals.CurveLineWeightId; }
            set { Globals.CurveLineWeightId = value; }
        }

        public int DimensionLineWeightId
        {
            get { return Globals.DimensionLineWeightId; }
            set { Globals.DimensionLineWeightId = value; }
        }

        public int ArrowLineWeightId
        {
            get { return Globals.ArrowLineWeightId; }
            set { Globals.ArrowLineWeightId = value; }
        }

        public uint PolygonFill
        {
            get
            {
                return Globals.PolygonFill;
            }
            set
            {
                Globals.PolygonFill = value;
            }
        }

        public string PolygonFillPattern
        {
            get { return Globals.PolygonPattern; }
            set { Globals.PolygonPattern = value; }
        }

        public double PolygonPatternScale
        {
            get { return Globals.PolygonPatternScale; }
            set { Globals.PolygonPatternScale = value; }
        }

        public double PolygonPatternAngle
        {
            get { return Globals.PolygonPatternAngle; }
            set { Globals.PolygonPatternAngle = value; }
        }

        public string RectangleFillPattern
        {
            get { return Globals.RectanglePattern; }
            set { Globals.RectanglePattern = value; }
        }

        public double RectanglePatternScale
        {
            get { return Globals.RectanglePatternScale; }
            set { Globals.RectanglePatternScale = value; }
        }

        public double RectanglePatternAngle
        {
            get { return Globals.RectanglePatternAngle; }
            set { Globals.RectanglePatternAngle = value; }
        }

        public string ArcFillPattern
        {
            get { return Globals.ArcPattern; }
            set { Globals.ArcPattern = value; }
        }

        public double ArcPatternScale
        {
            get { return Globals.ArcPatternScale; }
            set { Globals.ArcPatternScale = value; }
        }

        public double ArcPatternAngle
        {
            get { return Globals.ArcPatternAngle; }
            set { Globals.ArcPatternAngle = value; }
        }

        public string CircleFillPattern
        {
            get { return Globals.CirclePattern; }
            set { Globals.CirclePattern = value; }
        }

        public double CirclePatternScale
        {
            get { return Globals.CirclePatternScale; }
            set { Globals.CirclePatternScale = value; }
        }

        public double CirclePatternAngle
        {
            get { return Globals.CirclePatternAngle; }
            set { Globals.CirclePatternAngle = value; }
        }

        public string EllipseFillPattern
        {
            get { return Globals.EllipsePattern; }
            set { Globals.EllipsePattern = value; }
        }

        public double EllipsePatternScale
        {
            get { return Globals.EllipsePatternScale; }
            set { Globals.EllipsePatternScale = value; }
        }

        public double EllipsePatternAngle
        {
            get { return Globals.EllipsePatternAngle; }
            set { Globals.EllipsePatternAngle = value; }
        }

        public string DoublelineFillPattern
        {
            get { return Globals.DoublelinePattern; }
            set { Globals.DoublelinePattern = value; }
        }

        public double DoublelinePatternScale
        {
            get { return Globals.DoublelinePatternScale; }
            set { Globals.DoublelinePatternScale = value; }
        }

        public double DoublelinePatternAngle
        {
            get { return Globals.DoublelinePatternAngle; }
            set { Globals.DoublelinePatternAngle = value; }
        }

        public uint EllipseFill
        {
            get
            {
                return Globals.EllipseFill;
            }
            set
            {
                Globals.EllipseFill = value;
            }
        }

        public uint ArcFill
        {
            get
            {
                return Globals.ArcFill;
            }
            set
            {
                Globals.ArcFill = value;
            }
        }

        public uint CircleFill
        {
            get
            {
                return Globals.CircleFill;
            }
            set
            {
                Globals.CircleFill = value;
            }
        }

        public uint RectangleFill
        {
            get
            {
                return Globals.RectangleFill;
            }
            set
            {
                Globals.RectangleFill = value;
            }
        }

        public uint DoublelineFill
        {
            get
            {
                return Globals.DoublelineFill;
            }
            set
            {
                Globals.DoublelineFill = value;
            }
        }

        public uint PolygonFillSpec
        {
            // Drawing version 2
            get
            {
                return Globals.PolygonFill;
            }
            set
            {
                Globals.PolygonFillSpec = value;
            }
        }

        public uint EllipseFillSpec
        {
            // Drawing version 2
            get
            {
                return Globals.EllipseFillSpec;
            }
            set
            {
                Globals.EllipseFillSpec = value;
            }
        }

        public uint CircleFillSpec
        {
            // Drawing version 2
            get
            {
                return Globals.CircleFillSpec;
            }
            set
            {
                Globals.CircleFillSpec = value;
            }
        }

        public uint ArcFillSpec
        {
            // Drawing version 2
            get
            {
                return Globals.ArcFillSpec;
            }
            set
            {
                Globals.ArcFillSpec = value;
            }
        }

        public uint RectangleFillSpec
        {
            // Drawing version 2
            get
            {
                return Globals.RectangleFillSpec;
            }
            set
            {
                Globals.RectangleFillSpec = value;
            }
        }

        public uint DoublelineFillSpec
        {
            // Drawing version 2
            get
            {
                return Globals.DoublelineFillSpec;
            }
            set
            {
                Globals.DoublelineFillSpec = value;
            }
        }

        public int ArrowStyleId
        {
            get
            {
                return Globals.ArrowStyleId;
            }
            set
            {
                Globals.ArrowStyleId = value;
            }
        }

        public ArrowLocation ArrowLocation
        {
            get
            {
                return Globals.ArrowLocation;
            }
            set
            {
                Globals.ArrowLocation = value;
            }
        }

        public PDimension.DimType DimensionType
        {
            get
            {
                return Globals.DimensionType;
            }
            set
            {
                Globals.DimensionType = value;
            }
        }

        public int DimensionArrowStyleId
        {
            get
            {
                return Globals.DimArrowStyleId;
            }
            set
            {
                Globals.DimArrowStyleId = value;
            }
        }

        public int DimensionTextStyleId
        {
            get
            {
                return Globals.DimTextStyleId;
            }
            set
            {
                Globals.DimTextStyleId = value;
            }
        }

        public bool ShowAutoDimension
        {
            get
            {
                return Globals.ShowDimensionText;
            }
            set
            {
                Globals.ShowDimensionText = value;
            }
        }

        public int TextStyleId
        {
            get
            {
                return Globals.TextStyleId;
            }
            set
            {
                Globals.TextStyleId = value;
            }
        }

        public TextAlignment TextAlign
        {
            get
            {
                return Globals.TextAlign;
            }
            set
            {
                Globals.TextAlign = value;
            }
        }

        public TextPosition TextPosition
        {
            get
            {
                return Globals.TextPosition;
            }
            set
            {
                Globals.TextPosition = value;
            }
        }

        public bool SinglePointText
        {
            get
            {
                return Globals.TextSinglePoint;
            }
            set
            {
                Globals.TextSinglePoint = value;
            }
        }

        public string GroupName
        {
            get
            {
                return Globals.GroupName;
            }
            set
            {
                Globals.GroupName = value;
            }
        }

        public double OffsetDistance
        {
            get
            {
                return Globals.EditOffsetLineDistance;
            }
            set
            {
                Globals.EditOffsetLineDistance = value;
            }
        }
    }
}
