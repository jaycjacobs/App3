using Cirros.Core;
using Cirros.Drawing;
using Cirros.Utility;
using CirrosCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cirros.Primitives;
using System.Windows;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using App3;




#if UWP
using Cirros.Commands;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Xaml;
#else
using System.Windows.Media;
using static CirrosCore.WpfStubs;
#endif

namespace Cirros
{
    public class Globals
    {
        public enum MouseButtonType
        {
            Left,
            Middle,
            Right,
            Button1,
            Button2
        }

        public static void Reset()
        {
#if UWP
            CommandProcessor = null;
            workCanvas = null;
#else
#endif
            Events = new Events();
            Header = new DrawingHeader();
            LineTypeTable = new Dictionary<int, LineType>();
            LayerTable = new Dictionary<int, Layer>();
            TextStyleTable = new Dictionary<int, TextStyle>();
            ArrowStyleTable = new Dictionary<int, ArrowStyle>();
        }

        // Important note about global event handlers in Globals.Events (Globals.Events.On... = <handler>):
        // These handlers must be removed when a calling non-static object is unloaded
        public static Events Events = new Events();
        public static uint UIVersion = 0;
        public static bool UIPreview = true;
        public static double DPI = 96;
#if UWP
        public static UIDataContext UIDataContext = new UIDataContext();
        public static double UIFontSize = 0;

        public static WorkCanvas workCanvas;
        public static DrawingCanvas DrawingCanvas;
        public static DrawingTools DrawingTools;
#else
#endif
        public static IDrawingInput Input;
        public static IDrawingView View;
        public static DrawingDocument ActiveDrawing;

        public static double xSnap = .0625;
        public static double ySnap = .0625;
        public static double hitTolerance = 16.0;

        public static double GridSpacing = 1;
        public static int GridDivisions = 8;
        public static bool GridIsVisible = true;
        public static double GridIntensity = .57;

        public static int ExportImageSize = 4000;
        public static double ExportResolution = 300;
        public static bool ExportShowFrame = true;
        public static bool ExportShowGrid = false;
        public static string ExportFormat = "JPG";

        public static double ImageOpacity = 1;
        public static Dictionary<string, object> ImageDictionary = null;     // RedDog

        public static bool SelectResizeIsotropic = true;

        public static CoordinateMode CoordinateMode = CoordinateMode.Absolute;

#if UWP
        public static CommandDispatcher CommandDispatcher;
        public static CommandProcessor CommandProcessor;
        public static SelectSubCommand SelectSubCommand = Commands.SelectSubCommand.Move;
        public static EditSubCommand EditSubCommand = Commands.EditSubCommand.MovePoint;
        public static object CommandProcessorParameter = null;
        public static CommandType ReturnToCommand = CommandType.none;
        public static Commands.CopyRepeatType RadialCopyRepeatType = Commands.CopyRepeatType.Space;
        public static Commands.CopyRepeatType LinearCopyRepeatType = Commands.CopyRepeatType.Space;
#else
#endif

        public static bool SelectMoveByOffset = false;
        public static double SelectMoveOffsetX = 0;
        public static double SelectMoveOffsetY = 0;

        public static bool LinearCopyRepeatConnect = true;
        public static bool LinearCopyRepeatAtEnd = false;
        public static double LinearCopyRepeatDistance = 1;
        public static int LinearCopyRepeatCount = 3;
        public static string LinearCopyGroupName = null;
        public static double LinearCopyGroupScale = 1;

        public static bool RadialCopyRepeatConnect = true;
        public static bool RadialCopyRepeatAtEnd = false;
        public static double RadialCopyRepeatAngle = Math.PI / 6;
        public static int RadialCopyRepeatCount = 3;
        public static string RadialCopyGroupName = null;
        public static double RadialCopyGroupScale = 1;

        public static double EditOffsetLineDistance = 1;

        public static DrawingHeader Header = new DrawingHeader();

        public static Dictionary<int, LineType> LineTypeTable = new Dictionary<int, LineType>();
        public static Dictionary<int, Layer> LayerTable = new Dictionary<int, Layer>();
        public static Dictionary<int, TextStyle> TextStyleTable = new Dictionary<int, TextStyle>();
        public static Dictionary<int, ArrowStyle> ArrowStyleTable = new Dictionary<int, ArrowStyle>();
        public static Dictionary<string, Theme> Themes = new Dictionary<string, Theme>();

        public static List<string> LayerNames
        {
            get
            {
                List<string> names = new List<string>();
                foreach (Layer layer in LayerTable.Values)
                {
                    names.Add(layer.Name.ToLower());
                }
                return names;
            }
        }

        public static List<string> LineTypeNames
        {
            get
            {
                List<string> names = new List<string>();
                foreach (LineType linetype in LineTypeTable.Values)
                {
                    names.Add(linetype.Name.ToLower());
                }
                return names;
            }
        }

        public static List<string> TextStyleNames
        {
            get
            {
                List<string> names = new List<string>();
                foreach (TextStyle style in TextStyleTable.Values)
                {
                    names.Add(style.Name.ToLower());
                }
                return names;
            }
        }

        public static List<string> ArrowStyleNames
        {
            get
            {
                List<string> names = new List<string>();
                foreach (ArrowStyle style in ArrowStyleTable.Values)
                {
                    names.Add(style.Name.ToLower());
                }
                return names;
            }
        }

        public static List<uint> RecentColors = new List<uint>();

        public static Theme Theme;

        public static uint ColorSpec = (uint) ColorCode.ByLayer;

        public static Color HighlightColor = Colors.Transparent;

        private static int _layerId = 0;
        private static int _lineWeightId = -1;
        private static int _lineTypeId = -1;
        private static int _textStyleId = 0;
        public static int _textLayerId = 0;
        public static int _dimensionLayerId = 0;

        // RedDog layers
        public static int ActiveLayerId = 0;
        public static int ActiveLineLayerId = -1;
        public static int ActiveDoubleLineLayerId = -1;
        public static int ActiveRectangleLayerId = -1;
        public static int ActivePolygonLayerId = -1;
        public static int ActiveCircleLayerId = -1;
        public static int ActiveArcLayerId = -1;
        public static int ActiveEllipseLayerId = -1;
        public static int ActiveCurveLayerId = -1;
        public static int ActiveTextLayerId = -1;
        public static int ActiveDimensionLayerId = -1;
        public static int ActiveArrowLayerId = -1;
        public static int ActiveImageLayerId = -1;
        public static int ActiveInstanceLayerId = -1;

        public static uint LineColorSpec = 0;
        public static uint DoubleLineColorSpec = 0;
        public static uint RectangleColorSpec = 0;
        public static uint PolygonColorSpec = 0;
        public static uint CircleColorSpec = 0;
        public static uint ArcColorSpec = 0;
        public static uint EllipseColorSpec = 0;
        public static uint CurveColorSpec = 0;
        public static uint TextColorSpec = 0;
        public static uint DimensionColorSpec = 0;
        public static uint ArrowColorSpec = 0;

        public static int LineLineTypeId = -1;
        public static int DoubleLineLineTypeId = -1;
        public static int RectangleLineTypeId = -1;
        public static int PolygonLineTypeId = -1;
        public static int CircleLineTypeId = -1;
        public static int ArcLineTypeId = -1;
        public static int EllipseLineTypeId = -1;
        public static int CurveLineTypeId = -1;
        public static int DimensionLineTypeId = -1;
        public static int ArrowLineTypeId = -1;

        public static int LineLineWeightId = -1;
        public static int DoubleLineLineWeightId = -1;
        public static int RectangleLineWeightId = -1;
        public static int PolygonLineWeightId = -1;
        public static int CircleLineWeightId = -1;
        public static int ArcLineWeightId = -1;
        public static int EllipseLineWeightId = -1;
        public static int CurveLineWeightId = -1;
        public static int DimensionLineWeightId = -1;
        public static int ArrowLineWeightId = -1;

        public static int LayerId
        {
            get
            {
                if (LayerTable.Count > 0 && LayerTable.ContainsKey(_layerId) == false)
                {
                    _layerId = LayerTable.Keys.GetEnumerator().Current;
                } 
                return _layerId;
            }
            set
            {
                if (_layerId != value && (LayerTable.Count == 0 || LayerTable.ContainsKey(_layerId)))
                {
                    _layerId = value;
                    Globals.Events.LayerSelectionChanged();
                }
            }
        }

        public static int LineWeightId
        {
            get
            {
                return _lineWeightId;
            }
            set
            {
                _lineWeightId = value;
            }
        }

        public static int LineTypeId
        {
            get
            {
                return _lineTypeId;
            }
            set
            {
                _lineTypeId = value;
            }
        }

        public static int TextStyleId
        {
            get
            {
                if (TextStyleTable.Count > 0 && TextStyleTable.ContainsKey(_textStyleId) == false)
                {
                    _textStyleId = TextStyleTable.Keys.GetEnumerator().Current;
                }
                return _textStyleId;
            }
            set
            {
                _textStyleId = value;
            }
        }

        public static int TextLayerId
        {
            get
            {
                if (_textLayerId < 0)
                {
                    return LayerId;
                }
                else if (LayerTable.Count > 0 && LayerTable.ContainsKey(_textLayerId) == false)
                {
                    _textLayerId = LayerTable.Keys.GetEnumerator().Current;
                }
                return _textLayerId;
            }
            set
            {
                if (_textLayerId != value && (LayerTable.Count == 0 ||  value < 0 || LayerTable.ContainsKey(value)))
                {
                    _textLayerId = value;
                    Globals.Events.LayerSelectionChanged();
                }
            }
        }

        public static int DimensionLayerId
        {
            get
            {
                if (_dimensionLayerId < 0)
                {
                    return LayerId;
                }
                else if (LayerTable.Count > 0 && LayerTable.ContainsKey(_dimensionLayerId) == false)
                {
                    _dimensionLayerId = LayerTable.Keys.GetEnumerator().Current;
                }
                return _dimensionLayerId;
            }
            set
            {
                if (_dimensionLayerId != value && (LayerTable.Count == 0 || value < 0 || LayerTable.ContainsKey(value)))
                {
                    _dimensionLayerId = value;
                    Globals.Events.LayerSelectionChanged();
                }
            }
        }

        public static string PolygonPattern = "Solid";
        public static double PolygonPatternScale = 1;
        public static double PolygonPatternAngle = 0;

        public static string RectanglePattern = "Solid";
        public static double RectanglePatternScale = 1;
        public static double RectanglePatternAngle = 0;

        public static string ArcPattern = "Solid";
        public static double ArcPatternScale = 1;
        public static double ArcPatternAngle = 0;

        public static string CirclePattern = "Solid";
        public static double CirclePatternScale = 1;
        public static double CirclePatternAngle = 0;

        public static string EllipsePattern = "Solid";
        public static double EllipsePatternScale = 1;
        public static double EllipsePatternAngle = 0;

        public static string DoublelinePattern = "Solid";
        public static double DoublelinePatternScale = 1;
        public static double DoublelinePatternAngle = 0;

        private static uint _polygonFill = (uint)ColorCode.SameAsOutline;
        private static uint _ellipseFill = (uint)ColorCode.NoFill;
        private static uint _arcFill = (uint)ColorCode.NoFill;
        private static uint _circleFill = (uint)ColorCode.NoFill;
        private static uint _rectangleFill = (uint)ColorCode.NoFill;
        private static uint _doublelineFill = (uint)ColorCode.NoFill;

        public static uint PolygonFillSpec = (uint)ColorCode.ThemeForeground;
        public static uint EllipseFillSpec = (uint)ColorCode.ThemeForeground;
        public static uint ArcFillSpec = (uint)ColorCode.ThemeForeground;
        public static uint CircleFillSpec = (uint)ColorCode.ThemeForeground;
        public static uint RectangleFillSpec = (uint)ColorCode.ThemeForeground;
        public static uint DoublelineFillSpec = (uint)ColorCode.ThemeForeground;

        public static bool PolygonFillEvenOdd = false;

        public static uint PolygonFill { get { return _polygonFill; } set { _polygonFill = value; if (value > (uint)ColorCode.SetColor) PolygonFillSpec = value; } }
        public static uint EllipseFill { get { return _ellipseFill; } set { _ellipseFill = value; if (value > (uint)ColorCode.SetColor) EllipseFillSpec = value; } }
        public static uint ArcFill { get { return _arcFill; } set { _arcFill = value; if (value > (uint)ColorCode.SetColor) ArcFillSpec = value; } }
        public static uint CircleFill { get { return _circleFill; } set { _circleFill = value; if (value > (uint)ColorCode.SetColor) CircleFillSpec = value; } }
        public static uint RectangleFill { get { return _rectangleFill; } set { _rectangleFill = value; if (value > (uint)ColorCode.SetColor) RectangleFillSpec = value; } }
        //public static uint RectangleFill
        //{ 
        //    get {
        //        return _rectangleFill;
        //    } 
        //    set
        //    {
        //        _rectangleFill = value;
        //        if (value > (uint)ColorCode.SetColor)
        //            RectangleFillSpec = value;
        //    }
        //}
        public static uint DoublelineFill { get { return _doublelineFill; } set { _doublelineFill = value; if (value > (uint)ColorCode.SetColor) DoublelineFillSpec = value; } }

        public static LineCommandType LineCommandType = LineCommandType.Multi;
        public static PolygonCommandType PolygonCommandType = PolygonCommandType.Irregular;

        public static RectangleCommandType RectangleType = RectangleCommandType.Corners;
        public static EllipseCommandType EllipseCommandType = EllipseCommandType.Box;

        public static uint PolygonSides = 3;

        public static double FilletRadius = 1;
        public static double PolygonFilletRadius = 0;

        public static ArcCommandType ArcCommandType = ArcCommandType.CenterStartEnd;
        public static double ArcRadius = 1;
        public static double ArcStartAngle = 0;                 // In radians
        public static double ArcIncludedAngle = Math.PI;        // In radians

        public static ArcCommandType CircleCommandType = ArcCommandType.CenterStartEnd;
        public static double CircleRadius = 1;

        public static double RectangleWidth = 1;
        public static double RectangleHeight = 1;

        public static double EllipseMajorLength = 1;
        public static double EllipseMajorMinorRatio = 2;
        public static double EllipseAxisAngle = 0;                  // In radians
        public static double EllipseStartAngle = 0;                 // In radians
        public static double EllipseIncludedAngle = Math.PI * 2;    // In radians

        //public static int ArrowStyleId = 0;
        public static ArrowLocation ArrowLocation = ArrowLocation.Start;

        public static int DimensionRoundArchitectDefault = 0;
        public static int DimensionRoundEngineerDefault = 0;
        public static int DimensionRoundMetricDefault = 0;

        public static int DimensionRound = 4;
        public static bool ShowDimensionText = true;
        public static bool ShowDimensionExtension = true;
        public static bool ShowDimensionUnit = true;
        public static PDimension.DimType DimensionType = PDimension.DimType.Baseline;
        public static int DimArrowStyleId = 0;
        public static int DimTextStyleId = 0;

        public static double DoubleLineWidth = .2;
        public static DbEndStyle DoublelineEndStyle = DbEndStyle.None;

        public static double TransformScale = 1.0;
        public static double TransformAngle = 0.0;

        public static double TextHeight = 0;        // 0: use textstyle value
        public static double TextLineSpacing = 0;       // 0: use textstyle value
        public static double TextSpacing = 0;       // 0: use textstyle value
        public static string TextFont = "";         // empty: use textstyle value
        public static double TextAngle = 0;
        public static TextAlignment TextAlign = TextAlignment.Left;
        public static TextPosition TextPosition = TextPosition.Above;
        public static bool TextSinglePoint = true;

        public static string GroupName = null;
        public static GroupInsertLocation GroupInsertLocation = GroupInsertLocation.None;
        public static double GroupScale = 1;

        public static int InsertRepeatCount = 1;
        public static double InsertRepeatAngle = Math.PI / 6;
        public static double InsertRepeatDistance = 1;
        public static bool InsertRepeatConnectInstances = false;
        public static bool InsertRepeatAtEnds = true;

        public static bool ShowRulers = true;
        public static bool ShowDrawingTools = true;
        public static bool ShowControlPanel = true;
        public static bool ShowToolTips = true;

        public static bool EnablePinchZoom = true;
        public static bool EnableTouchMagnifer = true;
        public static bool EnableStylusMagnifer = true;
        public static MouseButtonType MousePanButton = MouseButtonType.Right;

        public static int _arrowStyleId = 0;

        public static int ArrowStyleId
        {
            // An invalid ArrowStyleId value was detected in the RC for 1.0
            // Adding an extra layer of safety for now but we need to track this down post-release
            get
            {
                if (Globals.ArrowStyleTable.Count == 0)
                {
                    return 0;
                }
                else if (Globals.ArrowStyleTable.ContainsKey(_arrowStyleId) == false)
                {
                    _arrowStyleId = Globals.ArrowStyleTable[0].Id;
                }
                return _arrowStyleId;
            }
            set
            {
                if (ArrowStyleTable.ContainsKey(value))
                {
                    _arrowStyleId = value;
                }
            }
        }

#if UWP
        private static StorageFolder _temporaryImageFolder = null;
        private static StorageFolder _symbolThumbnailFolder = null;
        private static StorageFolder _systemSymbolFolder = null;

        public static StorageFolder TemporaryImageFolder
        {
            get
            {
                if (_temporaryImageFolder == null)
                {
                    _temporaryImageFolder = ApplicationData.Current.TemporaryFolder;
                }

                return _temporaryImageFolder;
            }
        }

        public static StorageFolder SymbolThumbnailFolder
        {
            get
            {
                if (_symbolThumbnailFolder == null)
                {
                    _symbolThumbnailFolder = ApplicationData.Current.TemporaryFolder;
                }

                return _symbolThumbnailFolder;
            }
        }

        public static StorageFolder SystemSymbolFolder
        {
            get
            {
                if (_systemSymbolFolder == null)
                {
                    _systemSymbolFolder = ApplicationData.Current.LocalFolder;
                }

                return _systemSymbolFolder;
            }
        }
#else
#endif

        const int cRecentColorCount = 24;

        public static bool PushRecentColor(uint colorSpec)
        {
            bool listChanged = false;

            if (colorSpec > (uint)ColorCode.SetColor)
            {
                if (RecentColors.Contains(colorSpec))
                {
                    if (RecentColors[0] != colorSpec)
                    {
                        RecentColors.Remove(colorSpec);
                        RecentColors.Insert(0, colorSpec);
                        listChanged = true;
                    }
                }
                else
                {
                    RecentColors.Insert(0, colorSpec);
                    if (RecentColors.Count > cRecentColorCount)
                    {
                        RecentColors.RemoveAt(cRecentColorCount);
                    }
                    listChanged = true;
                }
            }

            return listChanged;
        }

        static bool _enableBetaFeatures = false;

        public static bool EnableBetaFeatures
        { 
            get { return _enableBetaFeatures; }
            set { _enableBetaFeatures = value; } 
        }

#if UWP
        public static FrameworkElement RootVisual
        {
            get
            {
                Frame frame = App.Window.Frame;
                return frame == null ? null : frame.Content as FrameworkElement;
            }
        }

        static int _windowsVersion = 0;
        internal static int Instance;

        public static int WindowsVersion
        {
            get
            {
                if (_windowsVersion == 0)
                {
                    _windowsVersion = Dx.FontExists("Segoe MDL2 Assets") ? 10 : 8;
                }

                return _windowsVersion;
            }
        }


        private static async Task CreateTemporaryImageFolder()
        {
            try
            {
                _temporaryImageFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("Images", CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                Analytics.ReportError("Globals:CreateTemporaryImageFolder", ex, 4, 316);
            }

            if (_temporaryImageFolder == null)
            {
                await Windows.Storage.ApplicationData.Current.TemporaryFolder.DeleteAsync();
                _temporaryImageFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("Images", CreationCollisionOption.OpenIfExists);
            }
        }

        private static async Task CreateSymbolThumbnailFolder()
        {
            try
            {
                _symbolThumbnailFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("Symbols", CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                Analytics.ReportError("Globals:CreateTemporaryImageFolder", ex, 4, 317);
            }

            if (_symbolThumbnailFolder == null)
            {
                await Windows.Storage.ApplicationData.Current.TemporaryFolder.DeleteAsync();
                _symbolThumbnailFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("Symbols", CreationCollisionOption.OpenIfExists);
            }
        }

        private static async Task CreateSystemSymbolFolder()
        {
            try
            {
                _systemSymbolFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Symbols", CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                Analytics.ReportError("Globals:CreateSystemSymbolFolder", ex, 4, 318);
            }

            if (_systemSymbolFolder == null)
            {
                await Windows.Storage.ApplicationData.Current.LocalFolder.DeleteAsync();
                _systemSymbolFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Symbols", CreationCollisionOption.OpenIfExists);
            }
        }

        private static async void InitializeGlobalsAsync()
        {
            await CreateTemporaryImageFolder();
            await CreateSymbolThumbnailFolder();
            await CreateSystemSymbolFolder();
            await Patterns.InitializeAsync();
        }
#else
        private static void InitializeGlobalsAsync()
        {
            Patterns.Initialize();
        }
#endif

        static Globals()
        {
            InitializeGlobalsAsync();

            Color VeryDarkGray = Utilities.ColorFromARGB(255, 60, 60, 60);
            Color OxfordBlue = Utilities.ColorFromARGB(255, 0, 33, 71);
            Color MediumOxfordBlue = Utilities.ColorFromARGB(255, 16, 60, 109);
            Color LightOxfordBlue = Utilities.ColorFromARGB(255, 84, 131, 186);
            //Color ByouBlue = Utilities.ColorFromARGB(255, 188, 212, 230);
            Color ByouBlue = Utilities.ColorFromARGB(255, 185, 195, 215);
            Color PaleCornflowerBlue = Utilities.ColorFromARGB(255, 171, 205, 239);
            Color LightPaleCornflowerBlue = Utilities.ColorFromARGB(255, 221, 238, 255);
            Color Sepia = Utilities.ColorFromARGB(255, 112, 66, 20);
            Color LightSepia = Utilities.ColorFromARGB(255, 216, 192, 168);
            Color LightSepia1 = Utilities.ColorFromARGB(255, 197, 179, 169);
            Color LightSepia2 = Utilities.ColorFromARGB(255, 236, 218, 204);
            Color LightSepia3 = Utilities.ColorFromARGB(255, 226, 203, 186);
            Color LightSepia4 = Utilities.ColorFromARGB(255, 217, 187, 165);
            Color LightSepia5 = Utilities.ColorFromARGB(255, 233, 224, 208);
            Color LightSepia6 = Utilities.ColorFromARGB(255, 242, 221, 206);
            Color SealBrown = Utilities.ColorFromARGB(255, 50, 20, 20);

            Themes["light"] = new Theme("Light", Colors.White, Colors.Black, Colors.MidnightBlue, Colors.MidnightBlue, Colors.Blue, Colors.Blue, Colors.Black, Colors.LightGray, Colors.OrangeRed, Colors.Black);
            Themes["dark"] = new Theme("Dark", Colors.Black, Colors.White, Colors.WhiteSmoke, Colors.WhiteSmoke, Colors.WhiteSmoke, Colors.Blue, Colors.WhiteSmoke, Colors.DarkGray, Colors.LimeGreen, Colors.White);
            Themes["blueline"] = new Theme("Blueline", Colors.White, MediumOxfordBlue, Colors.Blue, Colors.Blue, OxfordBlue, Colors.Blue, Colors.Black, Colors.LightGray, Colors.Red, OxfordBlue);
            Themes["blueprint"] = new Theme("Blueprint", OxfordBlue, Colors.White, Colors.SkyBlue, Colors.SkyBlue, Colors.White, Colors.Blue, Colors.WhiteSmoke, Colors.SlateGray, Colors.LimeGreen, Colors.WhiteSmoke);
            Themes["sepia"] = new Theme("Sepia", Colors.White, Sepia, Sepia, Sepia, Colors.Black, Colors.Blue, Colors.Black, Colors.LightGray, Colors.Red, Sepia);

            Theme = Themes["light"];

            RecentColors.Add(0xff000000);
            RecentColors.Add(0xffff0000);
            RecentColors.Add(0xff8b0000);
            RecentColors.Add(0xff008000);
            RecentColors.Add(0xff006400);
            RecentColors.Add(0xff0000ff);
            RecentColors.Add(0xff00008b);
            RecentColors.Add(0xffffff00);
            RecentColors.Add(0xff00ffff);
            RecentColors.Add(0xffff00ff);
        }
    }
}
