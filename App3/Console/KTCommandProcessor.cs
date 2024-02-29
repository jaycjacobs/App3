using Cirros;
using Cirros.Commands;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using Jint.Native;
using KT22.Console;
using RedDog;
using RedDog.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace KT22.Console
{
    public class KTCommandProcessor
    {
        static KTCommandProcessor _singleton = new KTCommandProcessor();

        private CommandParser _parser;
        private ScriptEngine _engine;
        private CommandEntryControl _console;

        private KTCommandProcessor()
        {
            _parser = new CommandParser();
            _engine = new ScriptEngine();

            Globals.Events.OnPointAdded += Events_OnPointAdded;
            Globals.Events.OnShowProperties += Events_OnShowProperties;
        }

        private void Events_OnShowProperties(object sender, ShowPropertiesEventArgs e)
        {
            if (e.Selection is Primitive p)
            {
                Dictionary<string, object> propertyBlock = KTUtilities.QueryPrimitive(p);
                PrintProperties(propertyBlock);
            }
        }

        private void Events_OnPointAdded(object sender, PointAddedEventArgs e)
        {
            LogPoint(Globals.ActiveDrawing.PaperToModel(e.Point), e.Key);
        }

        public static KTCommandProcessor CommandProcessor
        {
            get
            {
                return _singleton;
            }
        }

        public CommandParser Parser
        {
            get
            {
                return _parser;
            }
        }

        public void Initialize(CommandEntryControl console)
        {
            _console = console;
            _parser.Initialize(_console, _engine);
            _engine.InitializeJint(_console, this);
        }

        public ScriptEngine ScriptEngine
        {
            get
            {
                return _engine;
            }
        }

        int _layerNameField = 18;
        int _lineTypeField = 14;
        int _lineWeightField = 11;
        int _colorField = 15;

        private string ColorNameFromColorSpec(uint colorspec)
        {
            string colorname;

            if (colorspec == (uint)ColorCode.ByLayer)
            {
                colorname = "By layer";
            }
            else if (colorspec == (uint)ColorCode.ThemeForeground)
            {
                colorname = "Theme foreground";
            }
            else
            {
                colorname = Utilities.ColorNameFromColorSpec(colorspec);
            }

            return colorname;
        }

        private void CheckLayerFieldLengthsHack(List<Layer> layers)
        {
            foreach (Layer layer in layers)
            {
                string thickness = ((double)layer.LineWeightId / 1000).ToString("0.000");
                string color = ColorNameFromColorSpec(layer.ColorSpec);
                string lineTypeName = layer.LineTypeId < 0 ? "By layer" : Globals.LineTypeTable[layer.LineTypeId].Name;

                if (layer.Name.Length > _layerNameField)
                {
                    _layerNameField = layer.Name.Length;
                }
                if (thickness.Length > _lineWeightField)
                {
                    _lineWeightField = thickness.Length;
                }
                if (lineTypeName.Length > _lineTypeField)
                {
                    _lineTypeField = lineTypeName.Length;
                }
                if (color.Length > _colorField)
                {
                    _colorField = color.Length;
                }
            }
        }

        private string FormatLayer(Layer layer)
        {
            string thickness = ((double)layer.LineWeightId / 1000).ToString("0.000");
            string color = ColorNameFromColorSpec(layer.ColorSpec);
            string lineTypeName = layer.LineTypeId < 0 ? "By layer" : Globals.LineTypeTable[layer.LineTypeId].Name;

            string output = string.Format("{0}{1}{2}{3}{4}",
                layer.Name.PadRight(_layerNameField), lineTypeName.PadRight(_lineTypeField), thickness.PadRight(_lineWeightField), color.PadRight(_colorField), layer.Visible);

            return output;
        }

        protected string ExecuteLayer(Dictionary<string, object> commandBlock)
        {
            string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";
            string layerName = "";
            int layerId;

            string error = null;

            if (subcommand == "add")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    layerName = (string)commandBlock["name"];
                    layerId = ConsoleUtilities.LayerIdFromName(layerName);

                    if (layerId >= 0)
                    {
                        error = string.Format("Layer already exists: {0}", layerName);
                    }
                    else
                    {
                        try
                        {
                            Layer layer = new Layer();

                            layer.Name = layerName;

                            if (commandBlock.ContainsKey(RedDogGlobals.GS_LineType))
                            {
                                layer.LineTypeId = ConsoleUtilities.LineTypeIdFromName(commandBlock[RedDogGlobals.GS_LineType] as string);
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Color))
                            {
                                layer.ColorSpec = (uint)commandBlock[RedDogGlobals.GS_Color];
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Thickness))
                            {
                                layer.LineWeightId = ConsoleUtilities.LineTypeIdFromThickness((double)commandBlock[RedDogGlobals.GS_Thickness]);
                            }
                            if (commandBlock.ContainsKey("visible"))
                            {
                                layer.Visible = (bool)commandBlock["visible"];
                            }

                            Globals.ActiveDrawing.AddLayer(layer, true);
                        }
                        catch
                        {
                            error = "parser error";
                        }
                    }
                }
                else
                {
                    error = "layer name is required";
                }
            }
            else if (subcommand == "update")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    layerName = (string)commandBlock["name"];
                    layerId = ConsoleUtilities.LayerIdFromName(layerName);

                    if (layerId >= 0)
                    {
                        try
                        {
                            Layer layer = Globals.LayerTable[layerId];

                            if (commandBlock.ContainsKey(RedDogGlobals.GS_LineType))
                            {
                                layer.LineTypeId = ConsoleUtilities.LineTypeIdFromName(commandBlock[RedDogGlobals.GS_LineType] as string);
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Color))
                            {
                                layer.ColorSpec = (uint)commandBlock[RedDogGlobals.GS_Color];
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Thickness))
                            {
                                layer.LineWeightId = ConsoleUtilities.LineTypeIdFromThickness((double)commandBlock[RedDogGlobals.GS_Thickness]);
                            }
                            if (commandBlock.ContainsKey("visible"))
                            {
                                layer.Visible = (bool)commandBlock["visible"];
                            }
                        }
                        catch
                        {
                            error = "parser error";
                        }
                    }
                    else
                    {
                        error = string.Format("Layer not found: {0}", layerName);
                    }
                }
                else
                {
                    error = "layer name is required";
                }
            }
            else if (subcommand == "remove")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    layerName = (string)commandBlock["name"];
                    layerId = ConsoleUtilities.LayerIdFromName(layerName);

                    if (layerId >= 0)
                    {
                        Globals.LayerTable.Remove(layerId);
                    }
                    else
                    {
                        error = string.Format("Layer not found: {0}", layerName);
                    }
                }
                else
                {
                    error = "layer name is required";
                }
            }
            else if (subcommand == "list")
            {
                List<Layer> layers = new List<Layer>();

                if (commandBlock.ContainsKey("name"))
                {
                    layerName = (string)commandBlock["name"];
                    layerId = ConsoleUtilities.LayerIdFromName(layerName);
                    if (Globals.LayerTable.ContainsKey(layerId))
                    {
                        layers.Add(Globals.LayerTable[layerId]);
                    }
                    else
                    {
                        error = string.Format("Layer was not found: {0}", layerName);
                    }
                }
                else if (commandBlock.ContainsKey("contains"))
                {
                    string str = ((string)commandBlock["contains"]).ToLower();

                    foreach (Layer layer in Globals.LayerTable.Values)
                    {
                        if (layer.Name.ToLower().IndexOf(str) >= 0)
                        {
                            layers.Add(layer);
                        }
                    }

                    if (layers.Count == 0)
                    {
                        error = string.Format("No layers were found matching '{0}'", str);
                    }
                }
                else
                {
                    layers = Globals.LayerTable.Values.ToList<Layer>();
                }

                if (layers.Count > 0)
                {
                    string nameHeading = "Name";
                    string lineTypeHeading = "Linetype";
                    string lineWeightHeading = "Thickness";
                    string colorHeading = "Color";
                    string visibleHeading = "Visible";

                    _layerNameField = nameHeading.Length;
                    _lineTypeField = lineTypeHeading.Length;
                    _lineWeightField = lineWeightHeading.Length;
                    _colorField = colorHeading.Length;

                    CheckLayerFieldLengthsHack(layers);

                    _layerNameField += 2;
                    _lineTypeField += 2;
                    _lineWeightField += 2;
                    _colorField += 2;

                    List<string> list = new List<string>();

                    foreach (Layer layer in layers)
                    {
                        list.Add(FormatLayer(layer));
                    }

                    string header = string.Format("{0}{1}{2}{3}{4}",
                        nameHeading.PadRight(_layerNameField), lineTypeHeading.PadRight(_lineTypeField), lineWeightHeading.PadRight(_lineWeightField), colorHeading.PadRight(_colorField), visibleHeading);
                    _console.PrintResult(header);

                    list.Sort();

                    foreach (string s in list)
                    {
                        _console.PrintResult(s);
                    }
                }
            }

            return error;
        }

        int _linetypeNameField = 18;

        private string FormatLinetype(LineType linetype)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(linetype.Name.PadRight(_linetypeNameField));

            if (linetype.StrokeDashArray == null || linetype.StrokeDashArray.Count < 2)
            {
                sb.Append("Continuous");
            }
            else
            {
                sb.Append(linetype.StrokeDashArray[0].ToString("0.###"));

                for (int i = 1; i < linetype.StrokeDashArray.Count; i++)
                {
                    sb.AppendFormat(", {0}", linetype.StrokeDashArray[i].ToString("0.###"));
                }
            }

            return sb.ToString();
        }

        private void CheckLinetypeFieldLengthsHack(List<LineType> types)
        {
            foreach (LineType type in types)
            {
                if (type.Name.Length > _linetypeNameField)
                {
                    _linetypeNameField = type.Name.Length;
                }
            }
        }

        protected string ExecuteLinetype(Dictionary<string, object> commandBlock)
        {
            string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";
            string typeName = "";
            int typeId;

            string error = null;

            if (subcommand == "add")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    typeName = (string)commandBlock["name"];
                    typeId = ConsoleUtilities.LinetypeIdFromName(typeName);

                    if (typeId >= 0)
                    {
                        error = string.Format("Linetype already exists: {0}", typeName);
                    }
                    else
                    {
                        try
                        {
                            DoubleCollection lengthCollection = null;

                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Type))
                            {
                                lengthCollection = ConsoleUtilities.LengthCollectionFromString(commandBlock[RedDogGlobals.GS_Lengths] as string);
                            }

                            Globals.ActiveDrawing.AddLineType(typeName, lengthCollection);
                        }
                        catch
                        {
                            error = "parser error";
                        }
                    }
                }
                else
                {
                    error = "Linetype name is required";
                }
            }
            else if (subcommand == "update")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    typeName = (string)commandBlock["name"];
                    typeId = ConsoleUtilities.LinetypeIdFromName(typeName);

                    if (Globals.LineTypeTable.ContainsKey(typeId))
                    {
                        try
                        {
                            LineType type = Globals.LineTypeTable[typeId];
                            type.StrokeDashArray = ConsoleUtilities.LengthCollectionFromString(commandBlock[RedDogGlobals.GS_Lengths] as string);
                        }
                        catch
                        {
                            error = "parser error";
                        }
                    }
                    else
                    {
                        error = string.Format("Linetype not found: {0}", typeName);
                    }
                }
                else
                {
                    error = "Linetype name is required";
                }
            }
            else if (subcommand == "remove")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    typeName = (string)commandBlock["name"];
                    typeId = ConsoleUtilities.LineTypeIdFromName(typeName);

                    if (typeId >= 0)
                    {
                        Globals.LineTypeTable.Remove(typeId);
                        // error if linetype is in use?
                    }
                    else
                    {
                        error = string.Format("Linetype not found: {0}", typeName);
                    }
                }
                else
                {
                    error = "Linetype name is required";
                }
            }
            else if (subcommand == "list")
            {
                List<LineType> types = new List<LineType>();

                if (commandBlock.ContainsKey("name"))
                {
                    typeName = (string)commandBlock["name"];
                    typeId = ConsoleUtilities.LineTypeIdFromName(typeName);

                    if (Globals.LineTypeTable.ContainsKey(typeId))
                    {
                        types.Add(Globals.LineTypeTable[typeId]);
                    }
                    else
                    {
                        error = string.Format("Linetype was not found: {0}", typeName);
                    }
                }
                else if (commandBlock.ContainsKey("contains"))
                {
                    string str = ((string)commandBlock["contains"]).ToLower();

                    foreach (LineType type in Globals.LineTypeTable.Values)
                    {
                        if (type.Name.ToLower().IndexOf(str) >= 0)
                        {
                            types.Add(type);
                        }
                    }

                    if (types.Count == 0)
                    {
                        error = string.Format("No linetypes were found matching '{0}'", str);
                    }
                }
                else
                {
                    types = Globals.LineTypeTable.Values.ToList<LineType>();
                }

                if (types.Count > 0)
                {
                    string nameHeading = "Name";
                    string lengthsHeading = "Dash lengths";

                    _linetypeNameField = nameHeading.Length;

                    CheckLinetypeFieldLengthsHack(types);

                    _linetypeNameField += 2;

                    List<string> list = new List<string>();

                    foreach (LineType type in types)
                    {
                        list.Add(FormatLinetype(type));
                    }

                    string header = string.Format("{0}{1}", nameHeading.PadRight(_linetypeNameField), lengthsHeading);
                    _console.PrintResult(header);

                    list.Sort();

                    foreach (string s in list)
                    {
                        _console.PrintResult(s);
                    }
                }
            }

            return error;
        }

        int _arrowNameField = 18;
        int _arrowTypeField = 10;
        int _arrowSizeField = 10;
        int _arrowRatioField = 10;

        private string FormatArrowStyle(ArrowStyle style)
        {
            string type = style.Type.ToString();
            string size = style.Size.ToString("0.000");
            string ratio = style.Aspect.ToString("0.000");

            string output = string.Format("{0}{1}{2}{3}",
                style.Name.PadRight(_arrowNameField), type.PadRight(_arrowTypeField), size.PadRight(_arrowSizeField), ratio.PadRight(_arrowRatioField));

            return output;
        }

        private void CheckArrowStyleFieldLengthsHack(List<ArrowStyle> styles)
        {
            foreach (ArrowStyle style in styles)
            {
                string type = style.Type.ToString();
                string size = style.Size.ToString("0.000");
                string ratio = style.Aspect.ToString("0.000");

                if (style.Name.Length > _arrowNameField)
                {
                    _arrowNameField = style.Name.Length;
                }
                if (size.Length > _arrowSizeField)
                {
                    _arrowSizeField = size.Length;
                }
                if (type.Length > _arrowTypeField)
                {
                    _arrowTypeField = type.Length;
                }
                if (ratio.Length > _arrowRatioField)
                {
                    _arrowRatioField = ratio.Length;
                }
            }
        }

        protected string ExecuteArrowStyle(Dictionary<string, object> commandBlock)
        {
            string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";
            string styleName = "";
            int styleId;

            string error = null;

            if (subcommand == "add")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    styleName = (string)commandBlock["name"];
                    styleId = ConsoleUtilities.ArrowStyleIdFromName(styleName);

                    if (styleId >= 0)
                    {
                        error = string.Format("Arrow style already exists: {0}", styleName);
                    }
                    else
                    {
                        try
                        {
                            ArrowStyle style = new ArrowStyle();

                            style.Name = styleName;

                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Type))
                            {
                                style.Type = ConsoleUtilities.ArrowStyleTypeFromName(commandBlock[RedDogGlobals.GS_Type] as string);
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Size))
                            {
                                style.Size = (double)commandBlock[RedDogGlobals.GS_Size];
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Ratio))
                            {
                                style.Aspect = (double)commandBlock[RedDogGlobals.GS_Ratio];
                            }

                            Globals.ActiveDrawing.AddArrowStyle(style.Name, style.Type, style.Size, style.Aspect);
                        }
                        catch
                        {
                            error = "parser error";
                        }
                    }
                }
                else
                {
                    error = "arrow style name is required";
                }
            }
            else if (subcommand == "update")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    styleName = (string)commandBlock["name"];
                    styleId = ConsoleUtilities.ArrowStyleIdFromName(styleName);

                    if (Globals.ArrowStyleTable.ContainsKey(styleId))
                    {
                        try
                        {
                            ArrowStyle style = Globals.ArrowStyleTable[styleId];

                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Type))
                            {
                                style.Type = ConsoleUtilities.ArrowStyleTypeFromName(commandBlock[RedDogGlobals.GS_Type] as string);
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Size))
                            {
                                style.Size = (double)commandBlock[RedDogGlobals.GS_Size];
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Ratio))
                            {
                                style.Aspect = (double)commandBlock[RedDogGlobals.GS_Ratio];
                            }
                        }
                        catch
                        {
                            error = "parser error";
                        }
                    }
                    else
                    {
                        error = string.Format("Arrow style not found: {0}", styleName);
                    }
                }
                else
                {
                    error = "Arrow style name is required";
                }
            }
            else if (subcommand == "remove")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    styleName = (string)commandBlock["name"];
                    styleId = ConsoleUtilities.ArrowStyleIdFromName(styleName);

                    if (styleId >= 0)
                    {
                        Globals.ArrowStyleTable.Remove(styleId);
                    }
                    else
                    {
                        error = string.Format("Arrow style not found: {0}", styleName);
                    }
                }
                else
                {
                    error = "Arrow style name is required";
                }
            }
            else if (subcommand == "list")
            {
                List<ArrowStyle> styles = new List<ArrowStyle>();

                if (commandBlock.ContainsKey("name"))
                {
                    styleName = (string)commandBlock["name"];
                    styleId = ConsoleUtilities.ArrowStyleIdFromName(styleName);

                    if (Globals.ArrowStyleTable.ContainsKey(styleId))
                    {
                        styles.Add(Globals.ArrowStyleTable[styleId]);
                    }
                    else
                    {
                        error = string.Format("Arrow style was not found: {0}", styleName);
                    }
                }
                else if (commandBlock.ContainsKey("contains"))
                {
                    string str = ((string)commandBlock["contains"]).ToLower();

                    foreach (ArrowStyle style in Globals.ArrowStyleTable.Values)
                    {
                        if (style.Name.ToLower().IndexOf(str) >= 0)
                        {
                            styles.Add(style);
                        }
                    }

                    if (styles.Count == 0)
                    {
                        error = string.Format("No arrow styles were found matching '{0}'", str);
                    }
                }
                else
                {
                    styles = Globals.ArrowStyleTable.Values.ToList<ArrowStyle>();
                }

                if (styles.Count > 0)
                {
                    string nameHeading = "Name";
                    string arrowTypeHeading = "Type";
                    string arrowSizeHeading = "Size";
                    string arrowRatioHeading = "Ratio";

                    _arrowNameField = nameHeading.Length;
                    _arrowTypeField = arrowTypeHeading.Length;
                    _arrowSizeField = arrowSizeHeading.Length;
                    _arrowRatioField = arrowRatioHeading.Length;

                    CheckArrowStyleFieldLengthsHack(styles);

                    _arrowNameField += 2;
                    _arrowTypeField += 2;
                    _arrowSizeField += 2;
                    _arrowRatioField += 2;

                    List<string> list = new List<string>();

                    foreach (ArrowStyle style in styles)
                    {
                        list.Add(FormatArrowStyle(style));
                    }

                    string header = string.Format("{0}{1}{2}{3}",
                        nameHeading.PadRight(_arrowNameField), arrowTypeHeading.PadRight(_arrowTypeField), arrowSizeHeading.PadRight(_arrowSizeField), arrowRatioHeading.PadRight(_arrowRatioField));
                    _console.PrintResult(header);

                    list.Sort();

                    foreach (string s in list)
                    {
                        _console.PrintResult(s);
                    }
                }
            }

            return error;
        }

        int _textNameField = 18;
        int _textFontField = 18;
        int _textSizeField = 10;
        int _textOffsetField = 10;
        int _textLineSpacingField = 10;
        int _textCharSpacingField = 10;

        private string FormatTextStyle(TextStyle style)
        {
            string font = style.Font.ToString();
            string size = style.Size.ToString("0.000");
            string offset = style.Size.ToString("0.000");
            string lineSpacing = style.Spacing.ToString("0.000");
            string charSpacing = style.CharacterSpacing.ToString("0.000");

            string output = string.Format("{0}{1}{2}{3}{4}{5}",
                style.Name.PadRight(_textNameField), font.PadRight(_textFontField), size.PadRight(_textSizeField), offset.PadRight(_textOffsetField),
                lineSpacing.PadRight(_textLineSpacingField), charSpacing.PadRight(_textCharSpacingField));

            return output;
        }

        private void CheckTextStyleFieldLengthsHack(List<TextStyle> styles)
        {
            foreach (TextStyle style in styles)
            {
                string font = style.Font.ToString();
                string size = style.Size.ToString("0.000");
                string offset = style.Size.ToString("0.000");
                string lineSpacing = style.Spacing.ToString("0.000");
                string charSpacing = style.CharacterSpacing.ToString("0.000");

                if (style.Name.Length > _textNameField)
                {
                    _textNameField = style.Name.Length;
                }
                if (style.Font.Length > _textFontField)
                {
                    _textFontField = style.Font.Length;
                }
                if (size.Length > _textSizeField)
                {
                    _textSizeField = size.Length;
                }
                if (offset.Length > _textOffsetField)
                {
                    _textOffsetField = offset.Length;
                }
                if (lineSpacing.Length > _textLineSpacingField)
                {
                    _textLineSpacingField = lineSpacing.Length;
                }
                if (charSpacing.Length > _textCharSpacingField)
                {
                    _textCharSpacingField = charSpacing.Length;
                }
            }
        }

        protected string ExecuteTextStyle(Dictionary<string, object> commandBlock)
        {
            string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";
            string styleName = "";
            int styleId;

            string error = null;

            if (subcommand == "add")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    styleName = (string)commandBlock["name"];
                    styleId = ConsoleUtilities.TextStyleIdFromName(styleName);

                    if (styleId >= 0)
                    {
                        error = string.Format("Text style already exists: {0}", styleName);
                    }
                    else
                    {
                        try
                        {
                            TextStyle style = new TextStyle();

                            style.Name = styleName;

                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Font))
                            {
                                style.Font = commandBlock[RedDogGlobals.GS_Font] as string;
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Size))
                            {
                                style.Size = (double)commandBlock[RedDogGlobals.GS_Size];
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Offset))
                            {
                                style.Offset = (double)commandBlock[RedDogGlobals.GS_Offset];
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_LineSpacing))
                            {
                                style.Spacing = (double)commandBlock[RedDogGlobals.GS_LineSpacing];
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_CharSpacing))
                            {
                                style.CharacterSpacing = (double)commandBlock[RedDogGlobals.GS_CharSpacing];
                            }

                            Globals.ActiveDrawing.AddTextStyle(style.Name, style.Font, style.Size, style.Offset, style.Spacing, style.CharacterSpacing);
                        }
                        catch
                        {
                            error = "parser error";
                        }
                    }
                }
                else
                {
                    error = "Text style name is required";
                }
            }
            else if (subcommand == "update")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    styleName = (string)commandBlock["name"];
                    styleId = ConsoleUtilities.TextStyleIdFromName(styleName);

                    if (Globals.TextStyleTable.ContainsKey(styleId))
                    {
                        try
                        {
                            TextStyle style = Globals.TextStyleTable[styleId];

                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Font))
                            {
                                style.Font = commandBlock[RedDogGlobals.GS_Font] as string;
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Size))
                            {
                                style.Size = (double)commandBlock[RedDogGlobals.GS_Size];
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_Offset))
                            {
                                style.Offset = (double)commandBlock[RedDogGlobals.GS_Offset];
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_LineSpacing))
                            {
                                style.Spacing = (double)commandBlock[RedDogGlobals.GS_LineSpacing];
                            }
                            if (commandBlock.ContainsKey(RedDogGlobals.GS_CharSpacing))
                            {
                                style.CharacterSpacing = (double)commandBlock[RedDogGlobals.GS_CharSpacing];
                            }
                        }
                        catch
                        {
                            error = "parser error";
                        }
                    }
                    else
                    {
                        error = string.Format("Text style not found: {0}", styleName);
                    }
                }
                else
                {
                    error = "Text style name is required";
                }
            }
            else if (subcommand == "remove")
            {
                if (commandBlock.ContainsKey("name"))
                {
                    styleName = (string)commandBlock["name"];
                    styleId = ConsoleUtilities.TextStyleIdFromName(styleName);

                    if (styleId >= 0)
                    {
                        Globals.TextStyleTable.Remove(styleId);
                    }
                    else
                    {
                        error = string.Format("Text style not found: {0}", styleName);
                    }
                }
                else
                {
                    error = "Text style name is required";
                }
            }
            else if (subcommand == "list")
            {
                List<TextStyle> styles = new List<TextStyle>();

                if (commandBlock.ContainsKey("name"))
                {
                    styleName = (string)commandBlock["name"];
                    styleId = ConsoleUtilities.TextStyleIdFromName(styleName);

                    if (Globals.TextStyleTable.ContainsKey(styleId))
                    {
                        styles.Add(Globals.TextStyleTable[styleId]);
                    }
                    else
                    {
                        error = string.Format("Text style was not found: {0}", styleName);
                    }
                }
                else if (commandBlock.ContainsKey("contains"))
                {
                    string str = ((string)commandBlock["contains"]).ToLower();

                    foreach (TextStyle style in Globals.TextStyleTable.Values)
                    {
                        if (style.Name.ToLower().IndexOf(str) >= 0)
                        {
                            styles.Add(style);
                        }
                    }

                    if (styles.Count == 0)
                    {
                        error = string.Format("No text styles were found matching '{0}'", str);
                    }
                }
                else
                {
                    styles = Globals.TextStyleTable.Values.ToList<TextStyle>();
                }

                if (styles.Count > 0)
                {
                    string nameHeading = "Name";
                    string textFontHeading = "Font";
                    string textSizeHeading = "Size";
                    string textOffsetHeading = "Ratio";
                    string textLineSpacingHeading = "L Spc";
                    string textCharSpacingHeading = "C Spc";

                    _textNameField = nameHeading.Length;
                    _textFontField = textFontHeading.Length;
                    _textSizeField = textSizeHeading.Length;
                    _textOffsetField = textOffsetHeading.Length;
                    _textLineSpacingField = textOffsetHeading.Length;
                    _textCharSpacingField = textOffsetHeading.Length;

                    CheckTextStyleFieldLengthsHack(styles);

                    _textNameField += 2;
                    _textFontField += 2;
                    _textSizeField += 2;
                    _textOffsetField += 2;
                    _textLineSpacingField += 2;
                    _textCharSpacingField += 2;

                    List<string> list = new List<string>();

                    foreach (TextStyle style in styles)
                    {
                        list.Add(FormatTextStyle(style));
                    }

                    string header = string.Format("{0}{1}{2}{3}{4}{5}",
                        nameHeading.PadRight(_textNameField), textFontHeading.PadRight(_textFontField), textSizeHeading.PadRight(_textSizeField), textOffsetHeading.PadRight(_textOffsetField),
                        textLineSpacingHeading.PadRight(_textLineSpacingField), textCharSpacingHeading.PadRight(_textCharSpacingField));
                    _console.PrintResult(header);

                    list.Sort();

                    foreach (string s in list)
                    {
                        _console.PrintResult(s);
                    }
                }
            }

            return error;
        }

        private async void SelectScriptFolder()
        {
            _engine.UserScriptFolder = await _engine.SelectUserScriptFolder();
        }

        protected void ExecuteClear()
        {
            _console.DoModalCommand(new ClearCommand());
        }

        protected string ExecuteExport(Dictionary<string, object> commandBlock)
        {
            string error = null;

            if (Globals.RootVisual is KTDrawingPage dp)
            {
                dp.ShowExportDialog();
            }

            return error;
        }

        protected async Task ExecuteClearUI()
        {
            if (await Cirros.Alerts.StandardAlerts.ClearWarningAsync())
            {
                Globals.ActiveDrawing.Clear();
            }
        }

        protected async Task<string> ExecuteOpen(Dictionary<string, object> commandBlock)
        {
            string error = null;

            if (commandBlock.ContainsKey("file"))
            {
                string name = commandBlock["file"] as string;
                if (name.EndsWith(".dbfx") == false)
                {
                    name = name + ".dbfx";
                }

                StorageFile file = null;

                if (_drawingFolder != null)
                {
                    file = await _drawingFolder.TryGetItemAsync(name) as StorageFile;
                }
                if (file == null)
                {
                    StorageFolder logFolder = await GetLogFolder();
                    file = await logFolder.TryGetItemAsync(name) as StorageFile;
                }

                if (file == null)
                {
                    error = "File not found";
                }
                else if (file.FileType == ".dbfx")
                {
                    if (await FileHandling.LoadDrawingAsync(file) == false)
                    {
                        error = "Unable to load drawing";
                    }
                }
            }
            else
            {
                if (await FileHandling.LoadOrImportDrawingAsync() != 1)
                {
                    error = "Unable to load drawing";
                }
            }

            return error;
        }

        protected async Task<string> ExecuteSave(Dictionary<string, object> commandBlock)
        {
            string error = null;

            if (FileHandling.DrawingFileIsAvailable)
            {
                await FileHandling.SaveDrawingAsync();
            }
            else
            {
                await FileHandling.SaveDrawingAsAsync();
            }

            return error;
        }

        protected string ExecuteTriangle(Dictionary<string, object> commandBlock)
        {
            string error = null;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Show:
                            Globals.ShowDrawingTools = (bool)commandBlock[RedDogGlobals.GS_Show];
                            Globals.DrawingTools.ShowTriangle(Globals.ShowDrawingTools);
                            break;

                        case RedDogGlobals.GS_Type:
                            string type = (string)commandBlock[RedDogGlobals.GS_Type];
                            if (type == RedDogGlobals.GS_30)
                            {
                                Globals.DrawingTools.SetTriangleType(DrawingTools.TriangleType.Triangle30);
                            }
                            else if (type == RedDogGlobals.GS_45)
                            {
                                Globals.DrawingTools.SetTriangleType(DrawingTools.TriangleType.Triangle45);
                            }
                            break;

                        case RedDogGlobals.GS_Flip:
                            Globals.DrawingTools.FlipTriangle((bool)commandBlock[RedDogGlobals.GS_Flip]);
                            break;

                        case RedDogGlobals.GS_Rotate:
                            double degrees = (double)commandBlock[RedDogGlobals.GS_Rotate];
                            Globals.DrawingTools.SetTriangleRotation(degrees);
                            break;

                        case RedDogGlobals.GS_Color:
                            uint colorspec = (uint)commandBlock[RedDogGlobals.GS_Color];
                            Globals.DrawingTools.SetTriangleColor(colorspec);
                            Globals.DrawingTools.ShowTriangle(Globals.ShowDrawingTools);
                            break;

                        case RedDogGlobals.GS_Opacity:
                            double opacity = (double)commandBlock[RedDogGlobals.GS_Opacity];
                            Globals.DrawingTools.Opacity = (byte)(opacity * 255);
                            Globals.DrawingTools.ShowTriangle(Globals.ShowDrawingTools);
                            break;
                    }
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        protected string ExecuteLine(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.polyline;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveLineLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            object oc = commandBlock[RedDogGlobals.GS_Color];
                            if (oc is uint)
                            {
                                Globals.LineColorSpec = (uint)oc;
                            }
                            else if (oc is string)
                            {
                                Globals.LineColorSpec = ConsoleUtilities.ColorSpecFromString(oc as string);
                            }
                            break;

                        case RedDogGlobals.GS_Thickness:
                            if (commandBlock[RedDogGlobals.GS_Thickness] is double t)
                            {
                                if (t > 0)
                                {
                                    Globals.LineLineWeightId = (int)(t * 1000);
                                }
                                else
                                {
                                    Globals.LineLineWeightId = -1;
                                }
                            }
                            break;

                        case RedDogGlobals.GS_LineType:
                            if (commandBlock[RedDogGlobals.GS_LineType] is string lts)
                            {
                                Globals.LineLineTypeId = ConsoleUtilities.LineTypeIdFromName(lts);
                            }
                            break;

                        case RedDogGlobals.GS_Radius:
                            double radius = (double)commandBlock[RedDogGlobals.GS_Radius];
                            Globals.FilletRadius = Globals.ActiveDrawing.ModelToPaper(radius);
                            break;

                        case RedDogGlobals.GS_Construction:
                            switch ((string)commandBlock[RedDogGlobals.GS_Construction])
                            {
                                case RedDogGlobals.GS_SingleSegment:
                                    Globals.LineCommandType = LineCommandType.Single;
                                    command = CommandType.line;
                                    break;

                                case RedDogGlobals.GS_MultiSegment:
                                    Globals.LineCommandType = LineCommandType.Multi;
                                    command = CommandType.polyline;
                                    break;

                                case RedDogGlobals.GS_Fillet:
                                    Globals.LineCommandType = LineCommandType.Fillet;
                                    command = CommandType.fillet;
                                    break;

                                case RedDogGlobals.GS_Freehand:
                                    Globals.LineCommandType = LineCommandType.Freehand;
                                    command = CommandType.freehand;
                                    break;
                            }
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                    //Globals.DrawingCanvas.Focus();
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        protected string ExecuteArc(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.arc;
            string construction = RedDogGlobals.GS_Center;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveArcLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            object oc = commandBlock[RedDogGlobals.GS_Color];
                            if (oc is uint)
                            {
                                Globals.ArcColorSpec = (uint)oc;
                            }
                            else if (oc is string)
                            {
                                Globals.ArcColorSpec = ConsoleUtilities.ColorSpecFromString(oc as string);
                            }
                            break;

                        case RedDogGlobals.GS_Thickness:
                            if (commandBlock[RedDogGlobals.GS_Thickness] is double t)
                            {
                                if (t > 0)
                                {
                                    Globals.ArcLineWeightId = (int)(t * 1000);
                                }
                                else
                                {
                                    Globals.ArcLineWeightId = -1;
                                }
                            }
                            break;

                        case RedDogGlobals.GS_LineType:
                            if (commandBlock[RedDogGlobals.GS_LineType] is string lts)
                            {
                                Globals.ArcLineTypeId = ConsoleUtilities.LineTypeIdFromName(lts);
                            }
                            break;

                        case RedDogGlobals.GS_Radius:
                            double radius = (double)commandBlock[RedDogGlobals.GS_Radius];
                            Globals.ArcRadius = Globals.ActiveDrawing.ModelToPaper(radius);
                            break;

                        case RedDogGlobals.GS_StartAngle:
                            double start = (double)commandBlock[RedDogGlobals.GS_StartAngle];
                            Globals.ArcStartAngle = start / Construct.cRadiansToDegrees;
                            break;

                        case RedDogGlobals.GS_IncludedAngle:
                            double included = (double)commandBlock[RedDogGlobals.GS_IncludedAngle];
                            Globals.ArcIncludedAngle = included / Construct.cRadiansToDegrees;
                            break;

                        case RedDogGlobals.GS_Fill:
                            object o = commandBlock[RedDogGlobals.GS_Fill];
                            if (o is uint)
                            {
                                Globals.ArcFill = (uint)o;
                            }
                            else if (o is string)
                            {
                                Globals.ArcFill = ConsoleUtilities.ColorSpecFromString(o as string);
                            }
                            break;

                        case RedDogGlobals.GS_Pattern:
                            Globals.ArcPattern = (string)commandBlock[RedDogGlobals.GS_Pattern];
                            break;

                        case RedDogGlobals.GS_PatternScale:
                            Globals.ArcPatternScale = (double)commandBlock[RedDogGlobals.GS_PatternScale];
                            break;

                        case RedDogGlobals.GS_PatternAngle:
                            Globals.ArcPatternAngle = (double)commandBlock[RedDogGlobals.GS_PatternAngle];
                            break;

                        case RedDogGlobals.GS_Construction:
                            construction = (string)commandBlock[RedDogGlobals.GS_Construction];
                            break;
                    }
                }

                switch (construction)
                {
                    case RedDogGlobals.GS_CenterStartEnd:
                        Globals.ArcCommandType = ArcCommandType.CenterStartEnd;
                        command = CommandType.arc;
                        break;

                    case RedDogGlobals.GS_Radius:
                        Globals.ArcCommandType = ArcCommandType.CenterRadiusStartEnd;
                        command = CommandType.arc;
                        break;

                    case RedDogGlobals.GS_RadiusAngles:
                        Globals.ArcCommandType = ArcCommandType.CenterRadiusAngles;
                        command = CommandType.arc;
                        break;

                    case RedDogGlobals.GS_SemiCircle:
                        command = CommandType.arc2;
                        Globals.ArcCommandType = ArcCommandType.SemiCircle;
                        break;

                    case RedDogGlobals.GS_3Point:
                        command = CommandType.arc3;
                        Globals.ArcCommandType = ArcCommandType.ThreePoint;
                        break;

                    case RedDogGlobals.GS_FilletRadius:
                        Globals.ArcCommandType = ArcCommandType.FilletRadius;
                        command = CommandType.arcf;
                        break;

                    case RedDogGlobals.GS_Fillet:
                        Globals.ArcCommandType = ArcCommandType.Fillet;
                        command = CommandType.arcf;
                        break;
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                    //Globals.DrawingCanvas.Focus();
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        protected string ExecuteCircle(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.circle;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveCircleLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            object oc = commandBlock[RedDogGlobals.GS_Color];
                            if (oc is uint)
                            {
                                Globals.CircleColorSpec = (uint)oc;
                            }
                            else if (oc is string)
                            {
                                Globals.CircleColorSpec = ConsoleUtilities.ColorSpecFromString(oc as string);
                            }
                            break;

                        case RedDogGlobals.GS_Thickness:
                            if (commandBlock[RedDogGlobals.GS_Thickness] is double t)
                            {
                                if (t > 0)
                                {
                                    Globals.CircleLineWeightId = (int)(t * 1000);
                                }
                                else
                                {
                                    Globals.CircleLineWeightId = -1;
                                }
                            }
                            break;

                        case RedDogGlobals.GS_LineType:
                            if (commandBlock[RedDogGlobals.GS_LineType] is string lts)
                            {
                                Globals.CircleLineTypeId = ConsoleUtilities.LineTypeIdFromName(lts);
                            }
                            break;

                        case RedDogGlobals.GS_Radius:
                            double radius = (double)commandBlock[RedDogGlobals.GS_Radius];
                            Globals.CircleRadius = Globals.ActiveDrawing.ModelToPaper(radius);
                            break;

                        case RedDogGlobals.GS_Fill:
                            object o = commandBlock[RedDogGlobals.GS_Fill];
                            if (o is uint)
                            {
                                Globals.CircleFill = (uint)o;
                            }
                            else if (o is string)
                            {
                                Globals.CircleFill = ConsoleUtilities.ColorSpecFromString(o as string);
                            }
                            break;

                        case RedDogGlobals.GS_Pattern:
                            Globals.CirclePattern = (string)commandBlock[RedDogGlobals.GS_Pattern];
                            break;

                        case RedDogGlobals.GS_PatternScale:
                            Globals.CirclePatternScale = (double)commandBlock[RedDogGlobals.GS_PatternScale];
                            break;

                        case RedDogGlobals.GS_PatternAngle:
                            Globals.CirclePatternAngle = (double)commandBlock[RedDogGlobals.GS_PatternAngle];
                            break;

                        case RedDogGlobals.GS_Construction:
                            switch ((string)commandBlock[RedDogGlobals.GS_Construction])
                            {
                                case RedDogGlobals.GS_Center:
                                    command = CommandType.circle;
                                    break;

                                case RedDogGlobals.GS_3Point:
                                    command = CommandType.circle3;
                                    break;
                            }
                            break;
                    }
                }

                if (command == CommandType.circle)
                {
                    if (commandBlock.ContainsKey(RedDogGlobals.GS_Construction))
                    {
                        if ((string)commandBlock[RedDogGlobals.GS_Construction] == RedDogGlobals.GS_Radius)
                        {
                            Globals.CircleCommandType = ArcCommandType.CenterRadiusStartEnd;
                        }
                        else
                        {
                            Globals.CircleCommandType = ArcCommandType.CenterStartEnd;
                        }
                    }
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
                else if (command == CommandType.circle3)
                {
                    Globals.CircleCommandType = ArcCommandType.ThreePoint;
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        protected string ExecuteEllipse(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.ellipse;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveEllipseLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            object oc = commandBlock[RedDogGlobals.GS_Color];
                            if (oc is uint)
                            {
                                Globals.EllipseColorSpec = (uint)oc;
                            }
                            else if (oc is string)
                            {
                                Globals.EllipseColorSpec = ConsoleUtilities.ColorSpecFromString(oc as string);
                            }
                            break;

                        case RedDogGlobals.GS_Thickness:
                            if (commandBlock[RedDogGlobals.GS_Thickness] is double t)
                            {
                                if (t > 0)
                                {
                                    Globals.EllipseLineWeightId = (int)(t * 1000);
                                }
                                else
                                {
                                    Globals.EllipseLineWeightId = -1;
                                }
                            }
                            break;

                        case RedDogGlobals.GS_LineType:
                            if (commandBlock[RedDogGlobals.GS_LineType] is string lts)
                            {
                                Globals.EllipseLineTypeId = ConsoleUtilities.LineTypeIdFromName(lts);
                            }
                            break;

                        case RedDogGlobals.GS_Major:
                            double major = (double)commandBlock[RedDogGlobals.GS_Major];
                            Globals.EllipseMajorLength = Globals.ActiveDrawing.ModelToPaper(major);
                            break;

                        case RedDogGlobals.GS_Ratio:
                            Globals.EllipseMajorMinorRatio = (double)commandBlock[RedDogGlobals.GS_Ratio];
                            break;

                        case RedDogGlobals.GS_StartAngle:
                            Globals.EllipseStartAngle = (double)commandBlock[RedDogGlobals.GS_StartAngle] / Construct.cRadiansToDegrees;
                            break;

                        case RedDogGlobals.GS_IncludedAngle:
                            Globals.EllipseIncludedAngle = (double)commandBlock[RedDogGlobals.GS_IncludedAngle] / Construct.cRadiansToDegrees;
                            break;

                        case RedDogGlobals.GS_AxisAngle:
                            Globals.EllipseAxisAngle = (double)commandBlock[RedDogGlobals.GS_AxisAngle] / Construct.cRadiansToDegrees;
                            break;

                        case RedDogGlobals.GS_Fill:
                            object o = commandBlock[RedDogGlobals.GS_Fill];
                            if (o is uint)
                            {
                                Globals.EllipseFill = (uint)o;
                            }
                            else if (o is string)
                            {
                                Globals.EllipseFill = ConsoleUtilities.ColorSpecFromString(o as string);
                            }
                            break;

                        case RedDogGlobals.GS_Pattern:
                            Globals.EllipsePattern = (string)commandBlock[RedDogGlobals.GS_Pattern];
                            break;

                        case RedDogGlobals.GS_PatternScale:
                            Globals.EllipsePatternScale = (double)commandBlock[RedDogGlobals.GS_PatternScale];
                            break;

                        case RedDogGlobals.GS_PatternAngle:
                            Globals.EllipsePatternAngle = (double)commandBlock[RedDogGlobals.GS_PatternAngle];
                            break;

                        case RedDogGlobals.GS_Construction:
                            switch ((string)commandBlock[RedDogGlobals.GS_Construction])
                            {
                                case RedDogGlobals.GS_CenterSize:
                                    Globals.EllipseCommandType = EllipseCommandType.Center;
                                    break;

                                case RedDogGlobals.GS_Box:
                                    Globals.EllipseCommandType = EllipseCommandType.Box;
                                    break;

                                case RedDogGlobals.GS_Axis:
                                    Globals.EllipseCommandType = EllipseCommandType.Axis;
                                    break;
                            }
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        protected string ExecuteRectangle(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.rectangle;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveRectangleLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            object oc = commandBlock[RedDogGlobals.GS_Color];
                            if (oc is uint)
                            {
                                Globals.RectangleColorSpec = (uint)oc;
                            }
                            else if (oc is string)
                            {
                                Globals.RectangleColorSpec = ConsoleUtilities.ColorSpecFromString(oc as string);
                            }
                            break;

                        case RedDogGlobals.GS_Thickness:
                            if (commandBlock[RedDogGlobals.GS_Thickness] is double t)
                            {
                                if (t > 0)
                                {
                                    Globals.RectangleLineWeightId = (int)(t * 1000);
                                }
                                else
                                {
                                    Globals.RectangleLineWeightId = -1;
                                }
                            }
                            break;

                        case RedDogGlobals.GS_LineType:
                            if (commandBlock[RedDogGlobals.GS_LineType] is string lts)
                            {
                                Globals.RectangleLineTypeId = ConsoleUtilities.LineTypeIdFromName(lts);
                            }
                            break;

                        case RedDogGlobals.GS_Height:
                            Globals.RectangleHeight = Globals.ActiveDrawing.ModelToPaper((double)commandBlock[RedDogGlobals.GS_Height]);
                            break;

                        case RedDogGlobals.GS_Width:
                            Globals.RectangleWidth = Globals.ActiveDrawing.ModelToPaper((double)commandBlock[RedDogGlobals.GS_Width]);
                            break;

                        case RedDogGlobals.GS_Fill:
                            object o = commandBlock[RedDogGlobals.GS_Fill];
                            if (o is uint)
                            {
                                Globals.RectangleFill = (uint)o;
                            }
                            else if (o is string)
                            {
                                Globals.RectangleFill = ConsoleUtilities.ColorSpecFromString(o as string);
                            }
                            break;

                        case RedDogGlobals.GS_Pattern:
                            Globals.RectanglePattern = (string)commandBlock[RedDogGlobals.GS_Pattern];
                            break;

                        case RedDogGlobals.GS_PatternScale:
                            Globals.RectanglePatternScale = (double)commandBlock[RedDogGlobals.GS_PatternScale];
                            break;

                        case RedDogGlobals.GS_PatternAngle:
                            Globals.RectanglePatternAngle = (double)commandBlock[RedDogGlobals.GS_PatternAngle];
                            break;

                        case RedDogGlobals.GS_Construction:
                            switch ((string)commandBlock[RedDogGlobals.GS_Construction])
                            {
                                case RedDogGlobals.GS_Size:
                                    Globals.RectangleType = RectangleCommandType.Size;
                                    break;

                                case RedDogGlobals.GS_Corners:
                                    Globals.RectangleType = RectangleCommandType.Corners;
                                    break;
                            }
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        protected string ExecutePolygon(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.polygon;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActivePolygonLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            object oc = commandBlock[RedDogGlobals.GS_Color];
                            if (oc is uint)
                            {
                                Globals.PolygonColorSpec = (uint)oc;
                            }
                            else if (oc is string)
                            {
                                Globals.PolygonColorSpec = ConsoleUtilities.ColorSpecFromString(oc as string);
                            }
                            break;

                        case RedDogGlobals.GS_Thickness:
                            if (commandBlock[RedDogGlobals.GS_Thickness] is double t)
                            {
                                if (t > 0)
                                {
                                    Globals.PolygonLineWeightId = (int)(t * 1000);
                                }
                                else
                                {
                                    Globals.PolygonLineWeightId = -1;
                                }
                            }
                            break;

                        case RedDogGlobals.GS_LineType:
                            if (commandBlock[RedDogGlobals.GS_LineType] is string lts)
                            {
                                Globals.PolygonLineTypeId = ConsoleUtilities.LineTypeIdFromName(lts);
                            }
                            break;

                        case RedDogGlobals.GS_Radius:
                            double radius = (double)commandBlock[RedDogGlobals.GS_Radius];
                            Globals.PolygonFilletRadius = Globals.ActiveDrawing.ModelToPaper(radius);
                            break;

                        case RedDogGlobals.GS_Sides:
                            Globals.PolygonSides = (uint)(double)commandBlock[RedDogGlobals.GS_Sides];
                            break;

                        case RedDogGlobals.GS_Fill:
                            object o = commandBlock[RedDogGlobals.GS_Fill];
                            if (o is uint)
                            {
                                Globals.PolygonFill = (uint)o;
                            }
                            else if (o is string)
                            {
                                Globals.PolygonFill = ConsoleUtilities.ColorSpecFromString(o as string);
                            }
                            break;

                        case RedDogGlobals.GS_Pattern:
                            Globals.PolygonPattern = (string)commandBlock[RedDogGlobals.GS_Pattern];
                            break;

                        case RedDogGlobals.GS_PatternScale:
                            Globals.PolygonPatternScale = (double)commandBlock[RedDogGlobals.GS_PatternScale];
                            break;

                        case RedDogGlobals.GS_PatternAngle:
                            Globals.PolygonPatternAngle = (double)commandBlock[RedDogGlobals.GS_PatternAngle];
                            break;

                        case RedDogGlobals.GS_Type:
                            switch ((string)commandBlock[RedDogGlobals.GS_Type])
                            {
                                case RedDogGlobals.GS_Regular:
                                    Globals.PolygonCommandType = PolygonCommandType.Regular;
                                    break;

                                case RedDogGlobals.GS_Irregular:
                                    Globals.PolygonCommandType = PolygonCommandType.Irregular;
                                    break;
                            }
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        protected string ExecuteDoubleline(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.doubleline;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveDoubleLineLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            object oc = commandBlock[RedDogGlobals.GS_Color];
                            if (oc is uint)
                            {
                                Globals.DoubleLineColorSpec = (uint)oc;
                            }
                            else if (oc is string)
                            {
                                Globals.DoubleLineColorSpec = ConsoleUtilities.ColorSpecFromString(oc as string);
                            }
                            break;

                        case RedDogGlobals.GS_Thickness:
                            if (commandBlock[RedDogGlobals.GS_Thickness] is double t)
                            {
                                if (t > 0)
                                {
                                    Globals.DoubleLineLineWeightId = (int)(t * 1000);
                                }
                                else
                                {
                                    Globals.DoubleLineLineWeightId = -1;
                                }
                            }
                            break;

                        case RedDogGlobals.GS_LineType:
                            if (commandBlock[RedDogGlobals.GS_LineType] is string lts)
                            {
                                Globals.DoubleLineLineTypeId = ConsoleUtilities.LineTypeIdFromName(lts);
                            }
                            break;

                        case RedDogGlobals.GS_Width:
                            double width = (double)commandBlock[RedDogGlobals.GS_Width];
                            Globals.DoubleLineWidth = Globals.ActiveDrawing.ModelToPaper(width);
                            break;

                        case RedDogGlobals.GS_Fill:
                            object o = commandBlock[RedDogGlobals.GS_Fill];
                            if (o is uint)
                            {
                                Globals.DoublelineFill = (uint)o;
                            }
                            else if (o is string)
                            {
                                Globals.DoublelineFill = ConsoleUtilities.ColorSpecFromString(o as string);
                            }
                            break;

                        case RedDogGlobals.GS_Pattern:
                            Globals.DoublelinePattern = (string)commandBlock[RedDogGlobals.GS_Pattern];
                            break;

                        case RedDogGlobals.GS_PatternScale:
                            Globals.DoublelinePatternScale = (double)commandBlock[RedDogGlobals.GS_PatternScale];
                            break;

                        case RedDogGlobals.GS_PatternAngle:
                            Globals.DoublelinePatternAngle = (double)commandBlock[RedDogGlobals.GS_PatternAngle];
                            break;

                        case RedDogGlobals.GS_Cap:
                            switch ((string)commandBlock[RedDogGlobals.GS_Cap])
                            {
                                case RedDogGlobals.GS_None:
                                    Globals.DoublelineEndStyle = Cirros.Primitives.DbEndStyle.None;
                                    break;

                                case RedDogGlobals.GS_Start:
                                    Globals.DoublelineEndStyle = Cirros.Primitives.DbEndStyle.Start;
                                    break;

                                case RedDogGlobals.GS_End:
                                    Globals.DoublelineEndStyle = Cirros.Primitives.DbEndStyle.End;
                                    break;

                                case RedDogGlobals.GS_Both:
                                    Globals.DoublelineEndStyle = Cirros.Primitives.DbEndStyle.Both;
                                    break;
                            }
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        protected string ExecuteCurve(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.bspline;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveCurveLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            object oc = commandBlock[RedDogGlobals.GS_Color];
                            if (oc is uint)
                            {
                                Globals.CurveColorSpec = (uint)oc;
                            }
                            else if (oc is string)
                            {
                                Globals.CurveColorSpec = ConsoleUtilities.ColorSpecFromString(oc as string);
                            }
                            break;

                        case RedDogGlobals.GS_Thickness:
                            if (commandBlock[RedDogGlobals.GS_Thickness] is double t)
                            {
                                if (t > 0)
                                {
                                    Globals.CurveLineWeightId = (int)(t * 1000);
                                }
                                else
                                {
                                    Globals.CurveLineWeightId = -1;
                                }
                            }
                            break;

                        case RedDogGlobals.GS_LineType:
                            if (commandBlock[RedDogGlobals.GS_LineType] is string lts)
                            {
                                Globals.CurveLineTypeId = ConsoleUtilities.LineTypeIdFromName(lts);
                            }
                            break;
                    }
                }
                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }


        protected string ExecuteManageSymbols(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.managesymbols;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveCurveLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            object oc = commandBlock[RedDogGlobals.GS_Color];
                            if (oc is uint)
                            {
                                Globals.CurveColorSpec = (uint)oc;
                            }
                            else if (oc is string)
                            {
                                Globals.CurveColorSpec = ConsoleUtilities.ColorSpecFromString(oc as string);
                            }
                            break;

                        case RedDogGlobals.GS_Thickness:
                            if (commandBlock[RedDogGlobals.GS_Thickness] is double t)
                            {
                                if (t > 0)
                                {
                                    Globals.CurveLineWeightId = (int)(t * 1000);
                                }
                                else
                                {
                                    Globals.CurveLineWeightId = -1;
                                }
                            }
                            break;

                        case RedDogGlobals.GS_LineType:
                            if (commandBlock[RedDogGlobals.GS_LineType] is string lts)
                            {
                                Globals.CurveLineTypeId = ConsoleUtilities.LineTypeIdFromName(lts);
                            }
                            break;
                    }
                }
                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
#if KT22
                    Globals.DrawingCanvas.Focus();
#endif
                }
            }
            catch
            {
                error = "invalid command";
            }

            Globals.CommandDispatcher.ActiveCommand = CommandType.bspline;

            return error;
        }

        protected string ExecuteArrow(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.arrow;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveArrowLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            object oc = commandBlock[RedDogGlobals.GS_Color];
                            if (oc is uint)
                            {
                                Globals.ArrowColorSpec = (uint)oc;
                            }
                            else if (oc is string)
                            {
                                Globals.ArrowColorSpec = ConsoleUtilities.ColorSpecFromString(oc as string);
                            }
                            break;

                        case RedDogGlobals.GS_Thickness:
                            if (commandBlock[RedDogGlobals.GS_Thickness] is double t)
                            {
                                if (t > 0)
                                {
                                    Globals.ArrowLineWeightId = (int)(t * 1000);
                                }
                                else
                                {
                                    Globals.ArrowLineWeightId = -1;
                                }
                            }
                            break;

                        case RedDogGlobals.GS_LineType:
                            if (commandBlock[RedDogGlobals.GS_LineType] is string lts)
                            {
                                Globals.ArrowLineTypeId = ConsoleUtilities.LineTypeIdFromName(lts);
                            }
                            break;

                        case RedDogGlobals.GS_ArrowStyle:
                            string arrowStyle = (string)commandBlock[RedDogGlobals.GS_ArrowStyle];
                            Globals.ArrowStyleId = ConsoleUtilities.ArrowStyleIdFromName(arrowStyle);
                            break;

                        case RedDogGlobals.GS_Placement:
                            switch ((string)commandBlock[RedDogGlobals.GS_Placement])
                            {
                                case RedDogGlobals.GS_Start:
                                    Globals.ArrowLocation = Cirros.Primitives.ArrowLocation.Start;
                                    break;

                                case RedDogGlobals.GS_End:
                                    Globals.ArrowLocation = Cirros.Primitives.ArrowLocation.End;
                                    break;

                                case RedDogGlobals.GS_Both:
                                    Globals.ArrowLocation = Cirros.Primitives.ArrowLocation.Both;
                                    break;
                            }
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        protected string ExecuteDimension(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.dimension;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveDimensionLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            object oc = commandBlock[RedDogGlobals.GS_Color];
                            if (oc is uint)
                            {
                                Globals.DimensionColorSpec = (uint)oc;
                            }
                            else if (oc is string)
                            {
                                Globals.DimensionColorSpec = ConsoleUtilities.ColorSpecFromString(oc as string);
                            }
                            break;

                        case RedDogGlobals.GS_Thickness:
                            if (commandBlock[RedDogGlobals.GS_Thickness] is double t)
                            {
                                if (t > 0)
                                {
                                    Globals.DimensionLineWeightId = (int)(t * 1000);
                                }
                                else
                                {
                                    Globals.DimensionLineWeightId = -1;
                                }
                            }
                            break;

                        case RedDogGlobals.GS_LineType:
                            if (commandBlock[RedDogGlobals.GS_LineType] is string lts)
                            {
                                Globals.DimensionLineTypeId = ConsoleUtilities.LineTypeIdFromName(lts);
                            }
                            break;

                        case RedDogGlobals.GS_ArrowStyle:
                            string arrowStyle = (string)commandBlock[RedDogGlobals.GS_ArrowStyle];
                            Globals.DimArrowStyleId = ConsoleUtilities.ArrowStyleIdFromName(arrowStyle);
                            break;

                        case RedDogGlobals.GS_TextStyle:
                            string textStyle = (string)commandBlock[RedDogGlobals.GS_TextStyle];
                            Globals.DimTextStyleId = ConsoleUtilities.TextStyleIdFromName(textStyle);
                            break;

                        case RedDogGlobals.GS_ShowText:
                            Globals.ShowDimensionText = (string)commandBlock[RedDogGlobals.GS_ShowText] == "true";
                            break;

                        case RedDogGlobals.GS_ShowExtension:
                            Globals.ShowDimensionExtension = (string)commandBlock[RedDogGlobals.GS_ShowExtension] == "true";
                            break;

                        case RedDogGlobals.GS_ShowUnit:
                            Globals.ShowDimensionUnit = (string)commandBlock[RedDogGlobals.GS_ShowUnit] == "true";
                            break;

                        case RedDogGlobals.GS_Type:
                            switch ((string)commandBlock[RedDogGlobals.GS_Type])
                            {
                                case RedDogGlobals.GS_Baseline:
                                    Globals.DimensionType = Cirros.Primitives.PDimension.DimType.Baseline;
                                    break;

                                case RedDogGlobals.GS_Outside:
                                    Globals.DimensionType = Cirros.Primitives.PDimension.DimType.Outside;
                                    break;

                                case RedDogGlobals.GS_Incremental:
                                    Globals.DimensionType = Cirros.Primitives.PDimension.DimType.Incremental;
                                    break;

                                case RedDogGlobals.GS_PointToPoint:
                                    Globals.DimensionType = Cirros.Primitives.PDimension.DimType.PointToPoint;
                                    break;

                                case RedDogGlobals.GS_Angular:
                                    Globals.DimensionType = Cirros.Primitives.PDimension.DimType.IncrementalAngular;
                                    break;

                                case RedDogGlobals.GS_AngularBaseline:
                                    Globals.DimensionType = Cirros.Primitives.PDimension.DimType.BaselineAngular;
                                    break;
                            }
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        protected string ExecuteText(Dictionary<string, object> commandBlock)
        {
            string error = null;


            CommandType command = CommandType.text;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveTextLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            if (commandBlock[RedDogGlobals.GS_Color] is uint cspec)
                            {
                                Globals.TextColorSpec = cspec;
                            }
                            else if (commandBlock[RedDogGlobals.GS_Color] is string colorName)
                            {
                                Globals.TextColorSpec = Utilities.ColorSpecFromColorName(colorName);
                            }
                            break;

                        case RedDogGlobals.GS_Alignment:
                            switch ((string)commandBlock[RedDogGlobals.GS_Alignment])
                            {
                                case RedDogGlobals.GS_Left:
                                    Globals.TextAlign = TextAlignment.Left;
                                    break;

                                case RedDogGlobals.GS_Center:
                                    Globals.TextAlign = TextAlignment.Center;
                                    break;

                                case RedDogGlobals.GS_Right:
                                    Globals.TextAlign = TextAlignment.Right;
                                    break;
                            }
                            break;

                        case RedDogGlobals.GS_Position:
                            switch ((string)commandBlock[RedDogGlobals.GS_Position])
                            {
                                case RedDogGlobals.GS_Above:
                                    Globals.TextPosition = Cirros.Primitives.TextPosition.Above;
                                    break;

                                case RedDogGlobals.GS_On:
                                    Globals.TextPosition = Cirros.Primitives.TextPosition.On;
                                    break;

                                case RedDogGlobals.GS_Below:
                                    Globals.TextPosition = Cirros.Primitives.TextPosition.Below;
                                    break;
                            }
                            break;

                        case RedDogGlobals.GS_Font:
                            if (string.IsNullOrEmpty((string)commandBlock[RedDogGlobals.GS_Font]) == false)
                            {
                                Globals.TextFont = (string)commandBlock[RedDogGlobals.GS_Font];
                            }
                            break;

                        case RedDogGlobals.GS_Text:
                            // TODO
                            break;

                        case RedDogGlobals.GS_Angle:
                            double angle = (double)commandBlock[RedDogGlobals.GS_Angle];
                            //Globals.TextAngle = angle / Construct.cRadiansToDegrees;
                            Globals.TextAngle = -angle;
                            break;

                        case RedDogGlobals.GS_Size:
                            Globals.TextHeight = (double)commandBlock[RedDogGlobals.GS_Size];
                            break;

                        case RedDogGlobals.GS_Spacing:
                            Globals.TextSpacing = (double)commandBlock[RedDogGlobals.GS_Spacing];
                            break;

                        case RedDogGlobals.GS_LineSpacing:
                            Globals.TextLineSpacing = (double)commandBlock[RedDogGlobals.GS_LineSpacing];
                            break;

                        case RedDogGlobals.GS_TextStyle:
                            string textStyle = (string)commandBlock[RedDogGlobals.GS_TextStyle];
                            Globals.TextStyleId = ConsoleUtilities.TextStyleIdFromName(textStyle);
                            break;

                        case RedDogGlobals.GS_Construction:
                            switch ((string)commandBlock[RedDogGlobals.GS_Construction])
                            {
                                case RedDogGlobals.GS_1Point:
                                    Globals.TextSinglePoint = true;
                                    break;

                                case RedDogGlobals.GS_2Point:
                                    Globals.TextSinglePoint = false;
                                    break;
                            }
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        private string ExecuteInsertGroup(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.insertsymbol;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveInstanceLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Color:
                            if (commandBlock[RedDogGlobals.GS_Color] is uint cspec)
                            {
                                Globals.ColorSpec = cspec;
                            }
                            else if (commandBlock[RedDogGlobals.GS_Color] is string colorName)
                            {
                                Globals.ColorSpec = Utilities.ColorSpecFromColorName(colorName);
                            }
                            break;

                        case RedDogGlobals.GS_Scale:
                            Globals.GroupScale = (double)commandBlock[RedDogGlobals.GS_Scale];
                            break;

                        case RedDogGlobals.GS_InsertGroupName:
                            Globals.GroupName = (string)commandBlock[RedDogGlobals.GS_InsertGroupName];
                            break;

                        case RedDogGlobals.GS_InsertGroupFrom:
                            if (commandBlock[RedDogGlobals.GS_InsertGroupFrom] is string from)
                            {
                                RedDogGlobals.InsertGroupFrom = from;

                                if (from == RedDogGlobals.GS_InsertGroupFromDrawing)
                                {
                                    Globals.GroupName = null;
                                }
                            }
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        private string ExecuteInsertGroupLinear(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.copyalongline;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveInstanceLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Scale:
                            Globals.LinearCopyGroupScale = Globals.GroupScale = (double)commandBlock[RedDogGlobals.GS_Scale];
                            break;

                        case RedDogGlobals.GS_InsertGroupName:
                            Globals.LinearCopyGroupName = (string)commandBlock[RedDogGlobals.GS_InsertGroupName];
                            break;

                        case RedDogGlobals.GS_InsertGroupLinearMode:
                            switch ((string)commandBlock[RedDogGlobals.GS_InsertGroupLinearMode])
                            {
                                case RedDogGlobals.GS_InsertGroupLinearModeSpace:
                                    Globals.LinearCopyRepeatType = Cirros.Commands.CopyRepeatType.Space;
                                    break;

                                case RedDogGlobals.GS_InsertGroupLinearModeDistribute:
                                    Globals.LinearCopyRepeatType = Cirros.Commands.CopyRepeatType.Distribute;
                                    break;
                            }
                            break;

                        case RedDogGlobals.GS_InsertGroupLinearCount:
                            Globals.LinearCopyRepeatCount = (int)(double)commandBlock[RedDogGlobals.GS_InsertGroupLinearCount];
                            break;

                        case RedDogGlobals.GS_InsertGroupLinearSpacing:
                            double spacing = (double)commandBlock[RedDogGlobals.GS_InsertGroupLinearSpacing];
                            Globals.LinearCopyRepeatDistance = Globals.ActiveDrawing.ModelToPaper(spacing);
                            break;

                        case RedDogGlobals.GS_InsertGroupLinearConnect:
                            Globals.LinearCopyRepeatConnect = (bool)commandBlock[RedDogGlobals.GS_InsertGroupLinearConnect];
                            break;

                        case RedDogGlobals.GS_InsertGroupLinearEndCopy:
                            Globals.LinearCopyRepeatAtEnd = (bool)commandBlock[RedDogGlobals.GS_InsertGroupLinearEndCopy];
                            break;

                        case RedDogGlobals.GS_InsertGroupFrom:
                            if (commandBlock[RedDogGlobals.GS_InsertGroupFrom] is string from)
                            {
                                RedDogGlobals.InsertGroupLinearFrom = from;

                                if (from == RedDogGlobals.GS_InsertGroupFromDrawing)
                                {
                                    Globals.LinearCopyGroupName = null;
                                }
                            }
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        private string ExecuteInsertGroupRadial(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.copyalongarc;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveInstanceLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Scale:
                            Globals.RadialCopyGroupScale = Globals.GroupScale = (double)commandBlock[RedDogGlobals.GS_Scale];
                            break;

                        case RedDogGlobals.GS_InsertGroupName:
                            Globals.RadialCopyGroupName = (string)commandBlock[RedDogGlobals.GS_InsertGroupName];
                            break;

                        case RedDogGlobals.GS_InsertGroupRadialMode:
                            switch ((string)commandBlock[RedDogGlobals.GS_InsertGroupRadialMode])
                            {
                                case RedDogGlobals.GS_InsertGroupRadialModeSpace:
                                    Globals.RadialCopyRepeatType = Cirros.Commands.CopyRepeatType.Space;
                                    break;

                                case RedDogGlobals.GS_InsertGroupRadialModeDistribute:
                                    Globals.RadialCopyRepeatType = Cirros.Commands.CopyRepeatType.Distribute;
                                    break;
                            }
                            break;

                        case RedDogGlobals.GS_InsertGroupRadialCount:
                            Globals.RadialCopyRepeatCount = (int)(double)commandBlock[RedDogGlobals.GS_InsertGroupRadialCount];
                            break;

                        case RedDogGlobals.GS_InsertGroupRadialSpacing:
                            Globals.RadialCopyRepeatAngle = (double)commandBlock[RedDogGlobals.GS_InsertGroupRadialSpacing] / Construct.cRadiansToDegrees;
                            break;

                        case RedDogGlobals.GS_InsertGroupRadialConnect:
                            Globals.RadialCopyRepeatConnect = (bool)commandBlock[RedDogGlobals.GS_InsertGroupRadialConnect];
                            break;

                        case RedDogGlobals.GS_InsertGroupRadialEndCopy:
                            Globals.RadialCopyRepeatAtEnd = (bool)commandBlock[RedDogGlobals.GS_InsertGroupRadialEndCopy];
                            break;

                        case RedDogGlobals.GS_InsertGroupFrom:
                            if (commandBlock[RedDogGlobals.GS_InsertGroupFrom] is string from)
                            {
                                RedDogGlobals.InsertGroupRadialFrom = from;

                                if (from == RedDogGlobals.GS_InsertGroupFromDrawing)
                                {
                                    Globals.RadialCopyGroupName = null;
                                }
                            }
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        private InputPoint InputPointFromDictionary(Dictionary<string, object> d)
        {
            InputPoint ip = null;

            try
            {
                CoordinateMode mode = (CoordinateMode)d["type"];
                double v1 = (double)d["v1"];
                double v2 = (double)d["v2"];
                string key = (string)d["key"];
                ip = new InputPoint(mode, v1, v2, key);
            }
            catch
            {
            }

            return ip;
        }

        public async Task<string> Execute(Dictionary<string, object> commandBlock)
        {
            string error = null;

            Globals.ColorSpec = (uint)ColorCode.ByLayer;
            Globals.LineWeightId = -1;
            Globals.LineTypeId = -1;

            string command = commandBlock.ContainsKey("command") ? (string)commandBlock["command"] : "";
#if VERBOSE && DEBUG
                        string pre = "KT ";
                        foreach (string key in commandBlock.Keys)
                        {
                            object vo = commandBlock[key];
                            if (vo is List<Dictionary<string, object>>)
                            {
                                string attr = key;
                                List<Dictionary<string, object>> dictionaries = vo as List<Dictionary<string, object>>;
                                foreach (Dictionary<string, object> dictionary in dictionaries)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    sb.Append("[ ");
                                    foreach (string itemkey in dictionary.Keys)
                                    {
                                        sb.AppendFormat("{0}:{1} ", itemkey, dictionary[itemkey]);
                                    }
                                    sb.Append("]");
                                    _console.PrintResult(string.Format("{0}{1,-20}: {2}", pre, attr, sb.ToString()));
                                    attr = "";
                                }
                            }
                            else
                            {
                                _console.PrintResult(string.Format("{0}{1,-20}: {2}", pre, key, commandBlock[key]));
                            }
                            pre = "   ";
                        }
#endif
            LogCommand(commandBlock);

            object o;

            if (commandBlock.ContainsKey(RedDogGlobals.GS_Points) && commandBlock[RedDogGlobals.GS_Points] is List<Dictionary<string, object>>)
            {
                foreach (Dictionary<string, object> point in (List<Dictionary<string, object>>)commandBlock[RedDogGlobals.GS_Points])
                {
                    InputPoint ip = InputPointFromDictionary(point);
                    if (ip != null)
                    {
                        Globals.Input.PushPoint(ip);
                    }
                }
            }

            if (commandBlock.ContainsKey(RedDogGlobals.GS_Thickness))
            {
                double t = (double)commandBlock[RedDogGlobals.GS_Thickness];
                Globals.LineWeightId = (int)(t * 1000);
            }
            if (commandBlock.ContainsKey(RedDogGlobals.GS_LineType))
            {
                o = commandBlock[RedDogGlobals.GS_LineType];
                if (o is int)
                {
                    Globals.LineTypeId = (int)o;
                }
                else if (o is string)
                {
                    Globals.LineTypeId = ConsoleUtilities.LineTypeIdFromName(o as string);
                }
            }
            if (commandBlock.ContainsKey(RedDogGlobals.GS_Color))
            {
                o = commandBlock[RedDogGlobals.GS_Color];
                if (o is uint)
                {
                    Globals.ColorSpec = (uint)o;
                }
                else if (o is string)
                {
                    Globals.ColorSpec = ConsoleUtilities.ColorSpecFromString(o as string);
                }
            }

            switch (command)
            {
                case RedDogGlobals.GS_ClearCommand:
                    if (commandBlock.ContainsKey("source") && (string)commandBlock["source"] == "ui")
                    {
                        await ExecuteClearUI();
                    }
                    else
                    {
                        ExecuteClear();
                    }
                    break;

                case RedDogGlobals.GS_OpenCommand:
                    error = await ExecuteOpen(commandBlock);
                    break;

                case RedDogGlobals.GS_SaveCommand:
                    error = await ExecuteSave(commandBlock);
                    break;

                case RedDogGlobals.GS_SaveAsCommand:
                    await FileHandling.SaveDrawingAsAsync();
                    break;

                case RedDogGlobals.GS_SaveAsTemplateCommand:
                    await FileHandling.SaveDrawingAsTemplateAsync();
                    break;

                case RedDogGlobals.GS_ExportCommand:
                    error = ExecuteExport(commandBlock);
                    break;

                case RedDogGlobals.GS_PrintCommand:
                    // TODO Windows.Graphics.Printing.PrintManager is not yet supported in WindowsAppSDK. For more details see https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/what-is-supported
                    await Windows.Graphics.Printing.PrintManager.ShowPrintUIAsync();
                    break;

                case RedDogGlobals.GS_SettingsCommand:
                    error = ExecuteSettings(commandBlock);
                    break;

                case RedDogGlobals.GS_LayerCommand:
                    error = ExecuteLayer(commandBlock);
                    break;

                case RedDogGlobals.GS_TriangleCommand:
                    error = ExecuteTriangle(commandBlock);
                    break;

                case RedDogGlobals.GS_LinetypeCommand:
                    error = ExecuteLinetype(commandBlock);
                    break;

                case RedDogGlobals.GS_ArrowStyleCommand:
                    error = ExecuteArrowStyle(commandBlock);
                    break;

                case RedDogGlobals.GS_TextStyleCommand:
                    error = ExecuteTextStyle(commandBlock);
                    break;

                case RedDogGlobals.GS_LineCommand:
                    error = ExecuteLine(commandBlock);
                    break;

                case RedDogGlobals.GS_ArcCommand:
                    error = ExecuteArc(commandBlock);
                    break;

                case RedDogGlobals.GS_CircleCommand:
                    error = ExecuteCircle(commandBlock);
                    break;

                case RedDogGlobals.GS_EllipseCommand:
                    error = ExecuteEllipse(commandBlock);
                    break;

                case RedDogGlobals.GS_RectangleCommand:
                    error = ExecuteRectangle(commandBlock);
                    break;

                case RedDogGlobals.GS_PolygonCommand:
                    error = ExecutePolygon(commandBlock);
                    break;

                case RedDogGlobals.GS_DoublelineCommand:
                    error = ExecuteDoubleline(commandBlock);
                    break;

                case RedDogGlobals.GS_CurveCommand:
                    error = ExecuteCurve(commandBlock);
                    break;

                case RedDogGlobals.GS_ArrowCommand:
                    error = ExecuteArrow(commandBlock);
                    break;

                case RedDogGlobals.GS_DimensionCommand:
                    error = ExecuteDimension(commandBlock);
                    break;

                case RedDogGlobals.GS_TextCommand:
                    error = ExecuteText(commandBlock);
                    break;

                case RedDogGlobals.GS_PropertiesCommand:
                    error = ExecutePropertiesUI(commandBlock);
                    break;

                case RedDogGlobals.GS_OriginCommand:
                    error = ExecuteOrigin(commandBlock);
                    break;

                case RedDogGlobals.GS_DistanceCommand:
                    error = ExecuteDistance(commandBlock);
                    break;

                case RedDogGlobals.GS_AngleCommand:
                    error = ExecuteAngle(commandBlock);
                    break;

                case RedDogGlobals.GS_AreaCommand:
                    error = ExecuteArea(commandBlock);
                    break;

                case RedDogGlobals.GS_EditCommand:
                    error = ExecuteEditUI(commandBlock);
                    break;

                case RedDogGlobals.GS_EditGroupCommand:
                    error = ExecuteEditGroupUI(commandBlock);
                    break;

                case RedDogGlobals.GS_SelectCommand:
                    error = ExecuteSelectUI(commandBlock);
                    break;

                case RedDogGlobals.GS_CopyPasteCommand:
                    error = ExecuteCopyPasteUI(commandBlock);
                    break;

                case RedDogGlobals.GS_WindowCommand:
                    error = ExecuteWindow(commandBlock);
                    break;

                case RedDogGlobals.GS_PanCommand:
                    error = ExecutePan(commandBlock);
                    break;

                case RedDogGlobals.GS_PanLeftCommand:
                    Globals.View.Pan(-.1, 0);
                    break;

                case RedDogGlobals.GS_PanRightCommand:
                    Globals.View.Pan(.1, 0);
                    break;

                case RedDogGlobals.GS_PanUpCommand:
                    Globals.View.Pan(0, -.1);
                    break;

                case RedDogGlobals.GS_PanDownCommand:
                    Globals.View.Pan(0, .1);
                    break;

                case RedDogGlobals.GS_ViewActualSize:
                    Globals.View.DisplayActualSize();
                    break;
 
               case RedDogGlobals.GS_ZoomIn:
                    Globals.View.Zoom(2);
                    break;

                case RedDogGlobals.GS_ZoomOut:
                    Globals.View.Zoom(.5);
                    break;

                case RedDogGlobals.GS_ViewAll:
                    Globals.View.DisplayAll();
                    break;

                case RedDogGlobals.GS_InsertGroupCommand:
                    error = ExecuteInsertGroup(commandBlock);
                    break;

                case RedDogGlobals.GS_InsertGroupLinearCommand:
                    error = ExecuteInsertGroupLinear(commandBlock);
                    break;

                case RedDogGlobals.GS_InsertGroupRadialCommand:
                    error = ExecuteInsertGroupRadial(commandBlock);
                    break;

                case RedDogGlobals.GS_InsertImageCommand:
                    error = ExecuteInsertImage(commandBlock);
                    break;

                case RedDogGlobals.GS_ManageSymbolsCommand:
                    error = ExecuteManageSymbols(commandBlock);
                    break;

                case "js":
                    if (commandBlock.ContainsKey("js"))
                    {
                        string js = (string)commandBlock["js"];
                        JsValue jsv = _engine.Js(js, out error);
                    }
                    break;

                case "script":
                    if (commandBlock.ContainsKey("timeout"))
                    {
                        _engine.ScriptTimeout = (double)commandBlock["timeout"];
                    }
                    else if (commandBlock.ContainsKey("file"))
                    {
                        _engine.JsFile((string)commandBlock["file"]);
                    }
                    else if (commandBlock.ContainsKey("subcommand"))
                    {
                        string subcommand = (string)commandBlock["subcommand"];
                        if (subcommand == "set-folder")
                        {
                            SelectScriptFolder();
                        }
                        else if (subcommand == "folder")
                        {
                            if (_engine.UserScriptFolder == null)
                            {
                                _console.Error("The script folder has not been selected");
                            }
                            else
                            {
                                _console.Print(_engine.UserScriptFolder.Path);
                            }
                        }
                    }
                    break;

                default:
                    foreach (string key in commandBlock.Keys)
                    {
                        if (key == "points")
                        {
                            _console.PrintResult("points:");
                            foreach (Dictionary<string, object> point in (List<Dictionary<string, object>>)commandBlock[key])
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.Append("  ");
                                foreach (string pointKey in point.Keys)
                                {
                                    sb.AppendFormat("{0}: {1}; ", pointKey, point[pointKey]);
                                }
                                sb.AppendLine();
                                _console.PrintResult(sb.ToString());
                            }
                        }
                        else if (key == "error")
                        {
                            _console.Error(string.Format("{0}: {1}", key, commandBlock[key]));
                        }
                        else
                        {
                            _console.PrintResult(string.Format("{0}: {1}", key, commandBlock[key]));
                        }
                    }
                    break;
            }

            /*
             *  Should a drawing be considered 'modified' if only globals are changed?
             * 
            Globals.ActiveDrawing.IsModified = true;
            Globals.ActiveDrawing.ChangeNumber++;
             *
             */

            return error;
        }

        private string ExecuteInsertImage(Dictionary<string, object> commandBlock)
        {
            string error = null;

            CommandType command = CommandType.insertimage;

            try
            {
                foreach (string key in commandBlock.Keys)
                {
                    switch (key)
                    {
                        case RedDogGlobals.GS_Layer:
                            string layerName = (string)commandBlock[RedDogGlobals.GS_Layer];
                            Globals.ActiveImageLayerId = ConsoleUtilities.LayerIdFromName(layerName);
                            break;

                        case RedDogGlobals.GS_Opacity:
                            Globals.ImageOpacity = (double)commandBlock[RedDogGlobals.GS_Opacity];
                            break;
                    }
                }

                if (command != CommandType.none)
                {
                    Globals.CommandDispatcher.ActiveCommand = command;
                }
            }
            catch
            {
                error = "invalid command";
            }

            return error;
        }

        private string ExecutePropertiesUI(Dictionary<string, object> commandBlock)
        {
            string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";
            string error = null;

            if (subcommand == "silent" || subcommand == "print")
            {
                Globals.CommandDispatcher.ActiveCommand = CommandType.ktproperties;
            }
            else
            {
                Globals.CommandDispatcher.ActiveCommand = CommandType.properties;
            }

            return error;
        }

    private string ExecuteOrigin(Dictionary<string, object> commandBlock)
    {
        string error = null;

        Globals.CommandDispatcher.ActiveCommand = CommandType.origin;

        return error;
    }

    private string ExecuteArea(Dictionary<string, object> commandBlock)
    {
        string error = null;

        Globals.CommandDispatcher.ActiveCommand = CommandType.area;

        return error;
    }

    private string ExecuteDistance(Dictionary<string, object> commandBlock)
    {
        string error = null;

        Globals.CommandDispatcher.ActiveCommand = CommandType.distance;

        return error;
    }

    private string ExecuteAngle(Dictionary<string, object> commandBlock)
    {
        string error = null;

        Globals.CommandDispatcher.ActiveCommand = CommandType.angle;

        return error;
    }

    private string ExecuteEditUI(Dictionary<string, object> commandBlock)
        {
            //string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";
            string error = null;

            Globals.CommandDispatcher.ActiveCommand = CommandType.edit;

            return error;
        }

        private string ExecuteEditGroupUI(Dictionary<string, object> commandBlock)
        {
            //string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";
            string error = null;

            Globals.CommandDispatcher.ActiveCommand = CommandType.editgroup;

            return error;
        }

        private string ExecuteSelectUI(Dictionary<string, object> commandBlock)
        {
            //string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";
            string error = null;

            Globals.CommandDispatcher.ActiveCommand = CommandType.select;

            return error;
        }

        private string ExecuteCopyPasteUI(Dictionary<string, object> commandBlock)
        {
            //string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";
            string error = null;

            Globals.CommandDispatcher.ActiveCommand = CommandType.copypaste;

            return error;
        }

        private string ExecuteWindow(Dictionary<string, object> commandBlock)
        {
            //string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";
            string error = null;

            Globals.CommandDispatcher.ActiveCommand = CommandType.window;

            return error;
        }

        private string ExecutePan(Dictionary<string, object> commandBlock)
        {
            //string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";
            string error = null;

            Globals.CommandDispatcher.ActiveCommand = CommandType.pan;

            return error;
        }

        private void PrintProperties(Dictionary<string, object> propertyBlock)
        {
            foreach (string key in propertyBlock.Keys)
            {
                _console.PrintResult(string.Format("{0}: {1} ", key, propertyBlock[key]));
            }
        }

        private string ExecuteSettings(Dictionary<string, object> commandBlock)
        {
            string subcommand = commandBlock.ContainsKey("subcommand") ? (string)commandBlock["subcommand"] : "";

            string error = null;
            if (subcommand == "drawing")
            {
                Size newPaperSize = Globals.ActiveDrawing.PaperSize;
                double scale = Globals.ActiveDrawing.Scale;
                bool isArchitect = Globals.ActiveDrawing.IsArchitecturalScale;
                Unit modelUnit = Globals.ActiveDrawing.ModelUnit;
                Unit paperUnit = Globals.ActiveDrawing.PaperUnit;
                bool modified = false;

                if (commandBlock.ContainsKey(RedDogGlobals.GS_Width))
                {
                    try
                    {
                        newPaperSize.Width = (double)commandBlock[RedDogGlobals.GS_Width];
                        modified = true;
                    }
                    catch
                    {
                        error = "parser error";
                    }
                }
                if (commandBlock.ContainsKey(RedDogGlobals.GS_Height))
                {
                    try
                    {
                        newPaperSize.Height = (double)commandBlock[RedDogGlobals.GS_Height];
                        modified = true;
                    }
                    catch
                    {
                        error = "parser error";
                    }
                }
                if (commandBlock.ContainsKey(RedDogGlobals.GS_Scale))
                {
                    try
                    {
                        double inverse = (double)commandBlock[RedDogGlobals.GS_Scale];
                        if (inverse > 0)
                        {
                            scale = 1 / inverse;
                            modified = true;
                        }
                    }
                    catch
                    {
                        error = "parser error";
                    }
                }
                if (commandBlock.ContainsKey(RedDogGlobals.GS_PaperUnit))
                {
                    try
                    {
                        string unit = (string)commandBlock[RedDogGlobals.GS_PaperUnit];
                        switch (unit)
                        {
                            case "inches":
                            case "in":
                                paperUnit = Unit.Inches;
                                break;

                            case "millimeters":
                            case "mm":
                                paperUnit = Unit.Inches;
                                break;

                            default:
                                break;
                        }

                        if (paperUnit != Globals.ActiveDrawing.PaperUnit)
                        {
                            modified = true;
                        }
                    }
                    catch
                    {
                        error = "parser error";
                    }
                }
                if (commandBlock.ContainsKey(RedDogGlobals.GS_ModelUnit))
                {
                    try
                    {
                        string unit = (string)commandBlock[RedDogGlobals.GS_ModelUnit];
                        switch (unit)
                        {
                            case "inches":
                            case "in":
                                modelUnit = Unit.Inches;
                                break;

                            case "feet":
                            case "ft":
                                modelUnit = Unit.Feet;
                                break;

                            case "millimeters":
                            case "mm":
                                modelUnit = Unit.Inches;
                                break;

                            case "centimeters":
                            case "cm":
                                modelUnit = Unit.Centimeters;
                                break;

                            case "meters":
                                modelUnit = Unit.Meters;
                                break;

                            default:
                                break;
                        }

                        if (paperUnit != Globals.ActiveDrawing.PaperUnit)
                        {
                            modified = true;
                        }
                    }
                    catch
                    {
                        error = "parser error";
                    }
                }

                if (modified)
                {
                    Globals.ActiveDrawing.SetDrawingScaleAndUnits(scale, paperUnit, modelUnit, isArchitect);
                    Globals.ActiveDrawing.PaperSize = new Size(Globals.ActiveDrawing.UserToPaper(newPaperSize.Width), Globals.ActiveDrawing.UserToPaper(newPaperSize.Height));
                    Globals.View.DisplayAll();
                    Globals.ActiveDrawing.IsModified = true;
                    Globals.ActiveDrawing.ChangeNumber++;
                }

                if (commandBlock.ContainsKey(RedDogGlobals.GS_Print))
                {
                    _console.Print(string.Format("  {0}: {1}", RedDogGlobals.GS_Width, Globals.ActiveDrawing.PaperSize.Width.ToString()));
                    _console.Print(string.Format("  {0}: {1}", RedDogGlobals.GS_Height, Globals.ActiveDrawing.PaperSize.Height.ToString()));
                    _console.Print(string.Format("  {0}: {1}", RedDogGlobals.GS_Scale, Utilities.DoubleAsRatio(1 / Globals.ActiveDrawing.Scale)));
                    _console.Print(string.Format("  {0}: {1}", RedDogGlobals.GS_PaperUnit, Globals.ActiveDrawing.PaperUnit.ToString().ToLower()));
                    _console.Print(string.Format("  {0}: {1}", RedDogGlobals.GS_ModelUnit, Globals.ActiveDrawing.ModelUnit.ToString().ToLower()));
                }
            }
            else if (subcommand == "grid")
            {
                double spacing = Globals.GridSpacing;
                int divisions = Globals.GridDivisions;
                double intensity = Globals.GridIntensity;
                bool visible = Globals.GridIsVisible;
                bool modified = false;

                if (commandBlock.ContainsKey(RedDogGlobals.GS_Spacing))
                {
                    try
                    {
                        spacing = (double)commandBlock[RedDogGlobals.GS_Spacing];
                        modified = true;
                    }
                    catch
                    {
                        error = "parser error";
                    }
                }
                if (commandBlock.ContainsKey(RedDogGlobals.GS_Divisions))
                {
                    try
                    {
                        divisions = (int)(double)commandBlock[RedDogGlobals.GS_Divisions];
                        modified = true;
                    }
                    catch
                    {
                        error = "parser error";
                    }
                }
                if (commandBlock.ContainsKey(RedDogGlobals.GS_Intensity))
                {
                    try
                    {
                        intensity = (double)commandBlock[RedDogGlobals.GS_Intensity];
                        modified = true;
                    }
                    catch
                    {
                        error = "parser error";
                    }
                }
                if (commandBlock.ContainsKey(RedDogGlobals.GS_Visible))
                {
                    try
                    {
                        visible = (bool)commandBlock[RedDogGlobals.GS_Visible];
                        modified = true;
                    }
                    catch
                    {
                        error = "parser error";
                    }
                }

                if (modified)
                {
                    Globals.GridSpacing = spacing;
                    Globals.GridDivisions = divisions;
                    Globals.GridIntensity = intensity;
                    Globals.GridIsVisible = visible;

                    Globals.Events.GridChanged();
                    Globals.Events.ShowRulers(Globals.ShowRulers);
                    //Globals.View.VectorListControl.Redraw();
                }

                if (commandBlock.ContainsKey(RedDogGlobals.GS_Print))
                {
                    _console.Print(string.Format("  {0}: {1}", RedDogGlobals.GS_Spacing, Globals.GridSpacing.ToString()));
                    _console.Print(string.Format("  {0}: {1}", RedDogGlobals.GS_Divisions, Globals.GridDivisions.ToString()));
                    _console.Print(string.Format("  {0}: {1}", RedDogGlobals.GS_Intensity, Globals.GridIntensity.ToString()));
                    _console.Print(string.Format("  {0}: {1}", RedDogGlobals.GS_Visible, Globals.GridIsVisible.ToString().ToLower()));
                }
            }
            return error;
        }

        bool _loggingIsActive = false;
        string _logName = null;
        StorageFolder _logFolder = null;
        StorageFolder _drawingFolder = null;

        List<Dictionary<string, object>> _log = new List<Dictionary<string, object>>();

        public async Task<StorageFolder> GetLogFolder()
        {
            if (_logFolder == null)
            {
                _logFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists);
            }

            return _logFolder;
        }

        public async Task StartLoggingAsync()
        {
            if (_logName == null)
            {
                //_logName = Guid.NewGuid().ToString();
                _logName = "log";

                try
                {
                    StorageFolder logFolder = await GetLogFolder();

                    IStorageItem f = null;
                    string filename = null;
                    int index = 1;

                    do
                    {
                        _logName = string.Format("log{0}", index++);
                        filename = _logName + ".dbfx";
                        f = await logFolder.TryGetItemAsync(filename);
                    }
                    while (f != null);

                    StorageFile file = await logFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                    await FileHandling.SaveDrawingAsAsync(file, true);

                    _console.Print(string.Format("Logging to {0}", _logName));
                    _loggingIsActive = true;

                    LogCommand(new Dictionary<string, object> { { "command", "open" }, { "file", _logName } });
                }
                catch (Exception ex)
                {
                    Analytics.ReportError("KTCommandProcessor:StartLoggingAsync", ex, 4, 707);
                }
            }
            else
            {
                _console.Print("already logging");
            }
        }

        public async Task StopLogging()
        {
            if (_loggingIsActive)
            {
                await SaveLogAsync();

                _loggingIsActive = false;
                _logName = null;
            }
        }

        public void ClearLog()
        {
            _log.Clear();
        }

        public StringBuilder StringifyLog()
        {
            StringBuilder sb = new StringBuilder();

            foreach (Dictionary<string, object> d in _log)
            {
                if (d.ContainsKey("command"))
                {
                    sb.Append(d["command"]);

                    foreach (string key in d.Keys)
                    {
                        switch (key)
                        {
                            case "command":
                            case "points":
                                break;

                            case "subcommand":
                                sb.AppendFormat(" {0}", d["subcommand"]);
                                break;

                            default:
                                string s = d[key].ToString();
                                if (s != null)
                                {
                                    if (s.IndexOf(" ") < 0)
                                    {
                                        sb.AppendFormat(" {0}={1}", key, d[key]);
                                    }
                                    else
                                    {
                                        sb.AppendFormat(" {0}=\"{1}\"", key, d[key]);
                                    }
                                }
                                break;
                        }
                    }

                    if (d.ContainsKey("points"))
                    {
                        List<Dictionary<string, object>> points = d["points"] as List<Dictionary<string, object>>;

                        if (points != null)
                        {
                            sb.Append(" ");

                            foreach (Dictionary<string, object> point in points)
                            {
                                double v1 = Math.Round((double)point["v1"], 6);
                                double v2 = Math.Round((double)point["v2"], 6);
                                string key = (string)point["key"];
                                CoordinateMode mode = (CoordinateMode)point["type"];

                                if (key == " ")
                                {
                                    sb.AppendFormat("[{0},{1}]", v1, v2);
                                }
                                else if (key == "enter")
                                {
                                    sb.Append("[end]");
                                    break;
                                }
                                else
                                {
                                    sb.AppendFormat("[{0},{1},{2}]", v1, v2, key);
                                }
                            }
                        }
                    }

                    sb.AppendLine();
                }
            }

            return sb;
        }

        public async Task SaveLogAsync()
        {
            if (_logName != null)
            {
                try
                {
                    StringBuilder sb = StringifyLog();
                    StorageFolder logFolder = await GetLogFolder();
                    StorageFile file = await logFolder.CreateFileAsync(_logName + ".dbmacro");
                    if (file != null)
                    {
                        await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Analytics.ReportError("SaveLogAsync", ex, 1, 708);
                }
            }
        }

        public void PrintLog()
        {
            StringBuilder sb = StringifyLog();

            StringReader sr = new StringReader(sb.ToString());

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                _console.Print(line);
            }
        }

        private void LogCommand(Dictionary<string, object> commandBlock)
        {
            if (_loggingIsActive)
            {
                Dictionary<string, object> loggedCommand = new Dictionary<string, object>();

                foreach (string key in commandBlock.Keys)
                {
                    if (key != "points")
                    {
                        loggedCommand.Add(key, commandBlock[key]);
                    }
                }
                _log.Add(loggedCommand);
            }
        }

        private void LogPoint(Point model, string key)
        {
            if (_loggingIsActive)
            {
                if (Globals.CommandProcessor != null)
                {
                    Dictionary<string, object> current = _log.Last<Dictionary<string, object>>();
                    if (current != null)
                    {
                        List<Dictionary<string, object>> points = null;

                        if (current.ContainsKey("points") && current["points"] is List<Dictionary<string, object>>)
                        {
                            points = current["points"] as List<Dictionary<string, object>>;
                        }
                        else
                        {
                            points = new List<Dictionary<string, object>>();
                            current.Add("points", points);
                        }

                        Dictionary<string, object> point = new Dictionary<string, object>();
                        point.Add("v1", model.X);
                        point.Add("v2", model.Y);
                        point.Add("key", key);
                        point.Add("type", CoordinateMode.Absolute);
                        points.Add(point);
                    }
                }
            }
            //else
            //{
            //    System.Diagnostics.Debug.WriteLine("LogPoint: ({0}, {1}, '{2}')", model.X, model.Y, key);
            //}
        }

        private int _dbNest = 0;

        public async void Db(string input)
        {
            _dbNest++;

            if (_dbNest > 1)
            {
                --_dbNest;
                _engine.ThrowError(string.Format("[{0}] --> {1}", input, "nested calls to db are not allowed"));
            }
            else
            {
                Dictionary<string, object> result = _parser.ParseCommand(input);
                if (result != null && result.Count > 0)
                {
                    if (result.ContainsKey("error"))
                    {
                        --_dbNest;
                        _engine.ThrowError(string.Format("[{0}]  {1}", input, result["error"]));
                    }
                    else
                    {
                        string error = await Execute(result);

                        if (error != null)
                        {
                            --_dbNest;
                            _engine.ThrowError(string.Format("[{0}] --> {1}", input, error));
                        }
                    }
                }
            }

            --_dbNest;
        }

        List<string> test_results_success = new List<string>();
        List<string> test_results_fail = new List<string>();

        public string GetTestResults()
        {
            Dictionary<string, object> results = new Dictionary<string, object>() { { "success", test_results_success }, { "fail", test_results_fail } };
            return "test results";
        }

        public void ClearTestResults()
        {
            test_results_success.Clear();
            test_results_fail.Clear();
        }

        public async void DbTest(string input, string test_name, bool shouldSucceed)
        {
            //Console.WriteLine("----dbtest: {0}", input);
            string error = "";
            bool success = false;

            _dbNest++;

            if (_dbNest > 1)
            {
                error = "nested calls to db are not allowed";
            }
            else
            {
                Dictionary<string, object> result = _parser.ParseCommand(input);
                if (result != null && result.Count > 0)
                {
                    if (result.ContainsKey("error"))
                    {
                        error = (string)result["error"];
                    }
                    else
                    {
                        error = await Execute(result);
                    }
                }
            }

            --_dbNest;

            if (test_name.Length > 0)
            {
                _console.Print(test_name);
                if (shouldSucceed)
                {
                    if (error == null || error.Length == 0)
                    {
                        success = true;
                    }
                    else
                    {
                        success = false;
                    }
                }
                else
                {
                    if (error == null || error.Length == 0)
                    {
                        success = false;
                    }
                    else
                    {
                        success = true;
                    }
                }

                if (success)
                {
                    _console.PrintResult("SUCCEEDED");
                    test_results_success.Add(test_name);
                }
                else
                {
                    _console.Error("FAILED");
                    test_results_fail.Add(test_name);
                }
            }
            else if (error.Length > 0)
            {
                _console.Error(error);
            }
        }
    }
}
