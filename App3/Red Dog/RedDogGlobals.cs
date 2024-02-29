using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedDog
{
    class RedDogGlobals
    {
        public static bool RedDogFirstRun = false;
        public static bool HUIDialogFirstRun = false;

        public const string GS_ClearCommand = "clear";
        public const string GS_OpenCommand = "open";
        public const string GS_SaveCommand = "save";
        public const string GS_SaveAsCommand = "save_as";
        public const string GS_SaveAsTemplateCommand = "save_template";
        public const string GS_ExportCommand = "export";
        public const string GS_PrintCommand = "print";
        public const string GS_ArcCommand = "arc";
        public const string GS_ArrowCommand = "arrow";
        public const string GS_CircleCommand = "circle";
        public const string GS_CurveCommand = "curve";
        public const string GS_DimensionCommand = "dimension";
        public const string GS_DoublelineCommand = "doubleline";
        public const string GS_EllipseCommand = "ellipse";
        public const string GS_InsertGroupCommand = "insert_group";
        public const string GS_InsertGroupLinearCommand = "insert_group_linear";
        public const string GS_InsertGroupRadialCommand = "insert_group_radial";
        public const string GS_InsertImageCommand = "insert_image";
        public const string GS_LineCommand = "line";
        public const string GS_PolygonCommand = "polygon";
        public const string GS_RectangleCommand = "rectangle";
        public const string GS_TextCommand = "text";
        public const string GS_ArrowStyleCommand = "arrowstyle";
        public const string GS_TextStyleCommand = "textstyle";
        public const string GS_LayerCommand = "layer";
        public const string GS_LinetypeCommand = "linetype";
        public const string GS_ManageSymbolsCommand = "manage_symbols";
        public const string GS_SettingsCommand = "settings";
        public const string GS_SettingsApplicationCommand = "settings_application";
        public const string GS_SettingsDrawingCommand = "settings_drawing";
        public const string GS_SettingsLayersCommand = "settings_layers";
        public const string GS_SettingsLineTypesCommand = "settings_linetypes";
        public const string GS_SettingsTextStylesCommand = "settings_textstyles";
        public const string GS_SettingsArrowStylesCommand = "settings_arrowstyles";
        public const string GS_SettingsPatternsCommand = "settings_patterns";
        public const string GS_SettingsSupportCommand = "settings_support";
        public const string GS_TriangleCommand = "triangle";
        public const string GS_PropertiesCommand = "properties";
        public const string GS_OriginCommand = "origin";
        public const string GS_DistanceCommand = "distance";
        public const string GS_AngleCommand = "angle";
        public const string GS_AreaCommand = "area";
        public const string GS_EditCommand = "edit_object";
        public const string GS_EditGroupCommand = "edit_group";
        public const string GS_SelectCommand = "select_objects";
        public const string GS_CopyPasteCommand = "copy_paste";

        public const string GS_WindowCommand = "window";
        public const string GS_PanCommand = "pan";
        public const string GS_PanLeftCommand = "pan_left";
        public const string GS_PanRightCommand = "pan_right";
        public const string GS_PanUpCommand = "pan_up";
        public const string GS_PanDownCommand = "pan_down";
        public const string GS_ViewActualSize = "actual_size";
        public const string GS_ZoomIn = "zoom_in";
        public const string GS_ZoomOut = "zoom_out";
        public const string GS_ViewAll = "view_all";

        public const string GS_Points = "points";

        public const string GS_Show = "show";
        public const string GS_Flip = "flip";
        public const string GS_Rotate = "rotate";

        public const string GS_30 = "30";
        public const string GS_45 = "45";

        public const string GS_Bigger = "bigger";
        public const string GS_Smaller = "smaller";

        public const string GS_Name = "name";
        public const string GS_Visible = "visible";
        public const string GS_Contains = "contains";
        public const string GS_Add = "add";
        public const string GS_Update = "update";
        public const string GS_Remove = "remove";
        public const string GS_List = "list";

        public const string GS_Lengths = "lengths";

        public const string GS_Font = "font";
        //public const string GS_FontSize = "font-size";
        public const string GS_Offset = "offset";
        public const string GS_LineSpacing = "line-spacing";
        public const string GS_CharSpacing = "character-spacing";

        public const string GS_ShapeLayerKey = "shape-layer";
        public const string GS_TextLayerKey = "text-layer";
        public const string GS_DimensionLayerKey = "dimension-layer";

        public const string GS_ByLayer = "by-layer";
        public const string GS_Layer = "layer";
        public const string GS_Color = "color";
        public const string GS_LineType = "linetype";
        public const string GS_Thickness = "thickness";
        public const string GS_Outline = "outline";
        public const string GS_Opacity = "opacity";

        public const string GS_Pattern = "pattern";
        public const string GS_PatternScale = "pattern-scale";
        public const string GS_PatternAngle = "pattern-angle";

        public const string GS_CenterStartEnd = "center-start-end";
        public const string GS_SemiCircle = "semi-circle";
        public const string GS_FilletRadius = "fillet-radius";
        public const string GS_RadiusAngles = "radius-angles";

        public const string GS_StartAngle = "start-angle";
        public const string GS_IncludedAngle = "included-angle";
        public const string GS_Fill = "fill";

        public const string GS_Start = "start";
        public const string GS_End = "end";
        public const string GS_Both = "both";
        public const string GS_None = "none";

        public const string GS_Placement = "placement";
        public const string GS_ArrowStyle = "arrow-style";

        public const string GS_Center = "center";

        public const string GS_Incremental = "incremental";
        public const string GS_Baseline = "baseline";
        public const string GS_Outside = "outside";
        public const string GS_PointToPoint = "point-to-point";
        public const string GS_Angular = "angular";
        public const string GS_AngularBaseline = "angular-baseline";

        public const string GS_Cap = "cap";

        public const string GS_Box = "box";
        public const string GS_Axis = "axis";
        public const string GS_CenterSize = "center-size";

        public const string GS_Major = "major";
        public const string GS_Ratio = "ratio";
        public const string GS_AxisAngle = "axis-angle";

        public const string GS_Print = "print";
        public const string GS_Drawing = "drawing";
        public const string GS_PaperUnit = "paper-unit";
        public const string GS_ModelUnit = "model-unit";

        public const string GS_PaperWidth = "paper_width";
        public const string GS_PaperHeight = "paper_height";

        public const string GS_Theme = "theme";
        public const string GS_Background = "background";
        public const string GS_Foreground = "foreground";

        public const string GS_Grid = "grid";
        public const string GS_Divisions = "divisions";
        public const string GS_Intensity = "intensity";

        public const string GS_Colors = "colors";

        public const string GS_InsertGroupFromFile = "insert_group_from_file";
        public const string GS_InsertGroupFromLibrary = "insert_group_from_library";
        public const string GS_InsertGroupFromDrawing = "insert_group_from_drawing";
        public const string GS_InsertGroupPredefined = "insert_group_predefined";

        public const string GS_InsertGroupName = "insert_group_name";

        public const string GS_InsertGroupFrom = "insert_group_from";
        public const string GS_InsertGroupSource = "insert_group_source";

        public const string GS_InsertGroupLinearModeSpace = "insert_group_linear_mod_space";
        public const string GS_InsertGroupLinearModeDistribute = "insert_group_linear_mod_distribute";

        public const string GS_InsertGroupLinearMode = "insert_group_linear_mode";
        public const string GS_InsertGroupLinearCount = "insert_group_linear_count";
        public const string GS_InsertGroupLinearSpacing = "insert_group_linear_spacing";
        public const string GS_InsertGroupLinearConnect = "insert_group_linear_connect";
        public const string GS_InsertGroupLinearEndCopy = "insert_group_linear_end_copy";

        public const string GS_InsertGroupRadialModeSpace = "insert_group_radial_mod_space";
        public const string GS_InsertGroupRadialModeDistribute = "insert_group_radial_mod_distribute";

        public const string GS_InsertGroupRadialMode = "insert_group_radial_mode";
        public const string GS_InsertGroupRadialCount = "insert_group_radial_count";
        public const string GS_InsertGroupRadialSpacing = "insert_group_radial_spacing";
        public const string GS_InsertGroupRadialConnect = "insert_group_radial_connect";
        public const string GS_InsertGroupRadialEndCopy = "insert_group_radial_end_copy";

        public const string GS_InsertImageFromFile = "insert_image_from_file";
        public const string GS_InsertImageFromCamera = "insert_image_from_camera";

        public const string GS_InsertImageName = "insert_image_name";
        public const string GS_InsertImageFrom = "insert_image_from";
        public const string GS_InsertImageSource = "insert_image_source";
        public const string GS_InsertImageOpacity = "insert_image_opacity";

        public const string GS_SingleSegment = "single-segment";
        public const string GS_MultiSegment = "multi-segment";
        public const string GS_Fillet = "fillet";
        public const string GS_Freehand = "freehand";

        public const string GS_Construction = "construct";
        public const string GS_Radius = "radius";

        public const string GS_Regular = "regular";
        public const string GS_Irregular = "irregular";

        public const string GS_Sides = "sides";
        public const string GS_Type = "type";

        public const string GS_Corners = "corners";
        public const string GS_Size = "size";

        public const string GS_Width = "width";
        public const string GS_Height = "height";

        public const string GS_Text = "text";

        public const string GS_1Point = "1-point";
        public const string GS_2Point = "2-point";
        public const string GS_3Point = "three-point";

        public const string GS_Left = "left";
        public const string GS_Right = "right";
        public const string GS_Above = "above";
        public const string GS_On = "on";
        public const string GS_Below = "below";

        public const string GS_Alignment = "alignment";
        public const string GS_Position = "position";
        public const string GS_TextStyle = "text-style";
        public const string GS_Spacing = "spacing";
        public const string GS_Angle = "angle";

        public const string GS_Scale = "scale";
        public const string GS_XScale = "x-scale";
        public const string GS_YScale = "y-scale";

        public const string GS_ShowText = "show-text";
        public const string GS_ShowExtension = "show-extension";
        public const string GS_ShowUnit = "show-unit";

        //public static int ActiveLayerId = 0;
        //public static int ActiveLineLayerId = -1;
        //public static int ActiveDoubleLineLayerId = -1;
        //public static int ActiveRectangleLayerId = -1;
        //public static int ActivePolygonLayerId = -1;
        //public static int ActiveCircleLayerId = -1;
        //public static int ActiveArcLayerId = -1;
        //public static int ActiveEllipseLayerId = -1;
        //public static int ActiveCurveLayerId = -1;
        //public static int ActiveTextLayerId = -1;
        //public static int ActiveDimensionLayerId = -1;
        //public static int ActiveArrowLayerId = -1;

        public static int SelectedLayerId = 0;
        public static int SelectedLineTypeId = 0;
        public static int SelectedTextStyleId = 0;
        public static int SelectedArrowStyleId = 0;

        /*
        public static string ShapeLayer = "";
        public static string TextLayer = "";
        public static string DimensionLayer = "";
        public static string LayerColor = "";
        public static string LayerLineType = "";
        public static double LayerThickness = 0;

        public static string ArcConstruction = GS_CenterStartEnd;
        public static double ArcRadius = 1;
        public static double ArcStartAngle = 0;
        public static double ArcIncludedAngle = 180;
        public static string ArcFill = "none";

        public static string ArrowPlacement = GS_Start;
        public static string ArrowArrowStyle = "";

        public static string CircleConstruction = GS_Center;
        public static double CircleRadius = 1;
        public static string CircleFill = "none";

        public static string EllipseConstruction = GS_Box;
        public static double EllipseMajor = 1;
        public static double EllipseRatio = 2;
        public static double EllipseAxisAngle = 0;
        public static double EllipseStartAngle = 0;
        public static double EllipseIncludedAngle = 360;
        public static string EllipseFill = "none";

        public static string DoublelineCap = GS_None;
        public static double DoublelineWidth = .2;
        public static string DoublelineFill = "none";

        public static string DimensionType = GS_Incremental;
        public static string DimensionTextStyle = "";
        public static string DimensionArrowStyle = "";
        public static bool DimensionShowText = true;
        public static bool DimensionShowExtension = true;

        public static string LineConstruction = GS_MultiSegment;
        public static double LineFilletRadius = 1;

        public static string PolygonConstruction = GS_Irregular;
        public static double PolygonFilletRadius = 1;
        public static int PolygonSides = 3;
        public static string PolygonFill = "none";

        public static string RectangleConstruction = GS_Corners;
        public static double RectangleWidth = 1;
        public static double RectangleHeight = 1;
        public static string RectangleFill = "none";

        public static string TextConstruction = GS_2Point;
        public static string TextAlignment = GS_Left;
        public static string TextPosition = GS_Above;
        public static string TextStyle = "";
        public static double TextHeight = .1;
        public static double TextSpacing = 1;
        public static double TextAngle = 0;
        */
        public static string InsertGroupFrom = GS_InsertGroupFromDrawing;
        public static string InsertGroupSource = "";

        public static string InsertGroupLinearFrom = GS_InsertGroupFromDrawing;
        public static string InsertGroupLinearMode = GS_InsertGroupLinearModeDistribute;
        public static int InsertGroupLinearCount = 3;
        public static double InsertGroupLinearSpacing = 1;
        public static bool InsertGroupLinearConnect = false;
        public static bool InsertGroupLinearEndCopy = false;

        public static string InsertGroupRadialFrom = GS_InsertGroupFromDrawing;
        public static string InsertGroupRadialMode = GS_InsertGroupRadialModeDistribute;
        public static int InsertGroupRadialCount = 3;
        public static double InsertGroupRadialSpacing = 15;
        public static bool InsertGroupRadialConnect = false;
        public static bool InsertGroupRadialEndCopy = false;

        public static string InsertImageFrom = GS_InsertImageFromFile;
        public static double InsertImageOpacity = 1;
        public static string InsertImageSource = "";
    }
}
