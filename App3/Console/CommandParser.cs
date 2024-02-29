using Cirros;
using Cirros.Core;
using Cirros.Utility;
using KT22.Console;
using RedDog;
using RedDog.Console;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml.Media;

namespace KT22.Console
{
    public class CommandParser
    {
        CommandEntryControl _console;
        ScriptEngine _engine;

        public void Initialize(CommandEntryControl console, ScriptEngine engine)
        {
            _console = console;
            _engine = engine;
        }

        protected bool GetNumericValueParameter(Match m, bool allowArchitect, out double value)
        {
            bool valid = false;

            if (m.Groups["expression"].Success)
            {
                string expression = m.Groups["expression"].Value;
                if (_engine.EvaluateNumericExpression(expression, out value))
                {
                    valid = true;
                }
            }
            else if (m.Groups["feetinches"].Success && allowArchitect)
            {
                string feetstring = m.Groups["feet"].Value;
                string inchstring = m.Groups["inches"].Value;
                string numeratorstring = m.Groups["numerator"].Value;
                string denominatorstring = m.Groups["denominator"].Value;

                int feet = feetstring == "" ? 0 : int.Parse(feetstring);
                int inches = inchstring == "" ? 0 : int.Parse(inchstring);
                int numerator = numeratorstring == "" ? 0 : int.Parse(numeratorstring);
                int denominator = denominatorstring == "" ? 0 : int.Parse(denominatorstring);

                if ((numeratorstring != "" && numerator == 0) || (denominatorstring != "" && denominator == 0))
                {
                    // invalid fraction
                    value = double.NaN;
                }
                else
                {
                    double fraction = denominator == 0 ? 0.0 : (double)numerator / (double)denominator;
                    double sign = feetstring.StartsWith("-") ? -1 : 1;
                    value = (double)feet + (((double)inches + fraction) / 12.0) * sign;
                    valid = true;
                }
            }
            else if (m.Groups["number"].Success)
            {
                value = double.Parse(m.Groups["number"].Value);
                valid = true;
            }
            else if (m.Groups["integer"].Success)
            {
                value = int.Parse(m.Groups["integer"].Value);
                valid = true;
            }
            else
            {
                value = double.NaN;
            }

            return valid;
        }

        public bool GetStringValueParameter(Match m, out string value)
        {
            bool valid = false;

            if (m.Groups["token"].Success)
            {
                value = m.Groups["token"].Value;
                valid = true;
            }
            else if (m.Groups["quote"].Success)
            {
                value = m.Groups["quote"].Value;
                valid = true;
            }
            else if (m.Groups["squote"].Success)
            {
                value = m.Groups["squote"].Value;
                valid = true;
            }
            else
            {
                value = null;
            }

            return valid;
        }

        private bool GetStringColorParameter(Match m, out uint colorSpec)
        {
            colorSpec = 0x1;

            string value;

            if (GetStringValueParameter(m, out value))
            {
                colorSpec = ConsoleUtilities.ColorSpecFromString(value);
            }
            else if (m.Groups["hex"].Success)
            {
                value = m.Groups["hex"].Value;
                colorSpec = ConsoleUtilities.ColorSpecFromString(value);
            }

            return colorSpec != 0x1;
        }

        protected Dictionary<string, object> ParseCoordinate(string s)
        {
            Dictionary<string, object> coord_block = new Dictionary<string, object>();

            string[] pa = s.Split(new[] { ',' });

            string expr = @"(""(?<quote>[^""]*)"")|(\'(?<squote>[^\']*)\')|(\{(?<expression>[^\}]*)\})|(\[(?<coordinates>[^\]]*)\])|(?<feetinches>(?<feet>\-?\d+)'(?<inchesfraction>(?<inches>\d+)(\+(?<fraction>(?<numerator>\d+)/(?<denominator>\d+)))?)?)|(?<delimiter>[=:])|(?<number>(\-?\d+\.\d*)|(\-?\.\d+))|(?<integer>\-?\d+)|(?<token>[\-\w]+)|(?<invalid>\S)";

            List<MatchCollection> matchCollections = new List<MatchCollection>();

            string type = "d";
            string key = " ";
            double f1 = 0;
            double f2 = 0;

            bool have_type = false;
            bool have_key = false;
            bool have_f1 = false;
            bool have_f2 = false;

            foreach (string p in pa)
            {
                MatchCollection mc = Regex.Matches(p.Trim(), expr);
                if (mc.Count == 1)
                {
                    matchCollections.Add(mc);

                    if (!have_type && mc[0].Groups["token"].Success)
                    {
                        type = mc[0].Groups["token"].Value.ToLower();
                        have_type = true;
                    }
                    else
                    {
                        if (!have_type)
                        {
                            type = "a";
                            have_type = true;
                        }

                        if (!have_f1)
                        {
                            if (GetNumericValueParameter(mc[0], true, out f1))
                            {
                                have_f1 = true;
                            }
                            else
                            {
                                coord_block = new Dictionary<string, object>() { { "error", string.Format("Invalid parameter in coordinate [{0}]:  {1}", s, mc[0].Value) } };
                                break;
                            }
                        }
                        else if (!have_f2)
                        {
                            if (GetNumericValueParameter(mc[0], true, out f2))
                            {
                                have_f2 = true;
                            }
                            else
                            {
                                coord_block = new Dictionary<string, object>() { { "error", string.Format("Invalid parameter in coordinate [{0}]:  {1}", s, mc[0].Value) } };
                                break;
                            }
                        }
                        else if (!have_key && mc[0].Groups["token"].Success)
                        {
                            key = mc[0].Groups["token"].Value.ToLower();
                            have_key = true;
                        }
                        else
                        {
                            coord_block = new Dictionary<string, object>() { { "error", string.Format("Unexpected input in coordinate ({0}):  {1}", s, mc[0].Value) } };
                            break;
                        }
                    }
                }
                else
                {
                    coord_block = new Dictionary<string, object>() { { "error", string.Format("Invalid coordinate:  {0}", s) } };
                    break;
                }
            }

            if (coord_block.Count == 0)
            {
                coord_block.Add("v1", f1);
                coord_block.Add("v2", f2);
                coord_block.Add("key", key);

                if ("absolute".StartsWith(type))
                {
                    coord_block.Add("type", CoordinateMode.Absolute);
                }
                else if ("delta".StartsWith(type))
                {
                    coord_block.Add("type", CoordinateMode.Delta);
                }
                else if ("polar".StartsWith(type))
                {
                    coord_block.Add("type", CoordinateMode.Polar);
                }
                else if ("end".StartsWith(type)) 
                {
                    coord_block.Add("type", CoordinateMode.End);
                }
                else
                {
                    coord_block = new Dictionary<string, object>() { { "error", string.Format("Invalid coordinate:  {0}", s) } };
                }
            }

            return coord_block;
        }

        bool _multilineInput = false;
        StringBuilder _multilineInputBuilder = new StringBuilder();

        public Dictionary<string, object> ParseCommand(string command)
        {
            string expr = @"(""(?<quote>[^""]*)"")|(\'(?<squote>[^\']*)\')|(\{(?<expression>[^\}]*)\})|(\[(?<coordinates>[^\]]*)\])|(?<feetinches>(?<feet>\-?\d+)'(?<inchesfraction>(?<inches>\d+)(\+(?<fraction>(?<numerator>\d+)/(?<denominator>\d+)))?)?)|(?<delimiter>[=:])|(?<number>(\-?\d+\.\d*)|(\-?\.\d+))|(?<integer>\-?\d+)|(?<hex>#[abcdefABCDEF\d]+)|(?<token>[\-\w]+)|(?<invalid>\S)";

            command = command.Trim();

            Dictionary<string, object> commandBlock = new Dictionary<string, object>();
            List<Dictionary<string, object>> points = new List<Dictionary<string, object>>();

            int commentIndex = command.IndexOf("//");
            if (commentIndex >= 0)
            {
                command = command.Substring(0, commentIndex);
            }

            if (_multilineInput)
            {
                if (command == "")
                {
                    commandBlock = new Dictionary<string, object>() { { "command", "js" }, { "js", _multilineInputBuilder.ToString() } };
                    _multilineInputBuilder.Clear();

                    _multilineInput = false;
                    _console.PromptString = "% ";
                }
                else
                {
                    _multilineInputBuilder.AppendLine(command);
                }
            }
            else if (command == ";")
            {
                _multilineInput = true;
                _console.PromptString = "> ";
            }
            else if (command.StartsWith(";"))
            {
                string js = command.Substring(1).Trim();
                commandBlock = new Dictionary<string, object>() { { "command", "js" }, { "js", js } };
            }
            else if (command.StartsWith("/"))
            {
                string filename = command.Substring(1).Trim();
                commandBlock = new Dictionary<string, object>() { { "command", "script" }, { "file", filename } };
            }
            else
            {
                int index = 0;

                MatchCollection mc = Regex.Matches(command, expr);

                if (mc.Count > 0)
                {
                    Match m = mc[index++];
                    if (m.Groups["token"].Success)
                    {
                        string token = m.Groups["token"].Value.ToLower();
                        if (token == "")
                        {
                            commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid input: {0}", command) } };
                        }
                        else if (KTCommandLanguage.Commands.ContainsKey(token))
                        {
                            string commandName = token;

                            Dictionary<string, object> options = KTCommandLanguage.Commands[commandName];

                            commandBlock = new Dictionary<string, object>() { { "command", commandName } };

                            while (index < mc.Count)
                            {
                                m = mc[index++];

                                if (m.Groups["token"].Success)
                                {
                                    token = m.Groups["token"].Value.ToLower();

                                    if (options == null)
                                    {
                                        // An option was supplied to a command with no options
                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid parameter: {0}", token) } };
                                        break;
                                    }
                                    else if (commandBlock.ContainsKey(token))
                                    {
                                        // Duplicate option
                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("Duplicate definition for {0}", token) } };
                                        break;
                                    }
                                    else if (options.ContainsKey(token))
                                    {
                                        // Valid option key
                                        string key = token;
                                        object value_type = options[key];

                                        if (value_type is string && (string)value_type == "subcommand_type")
                                        {
                                            if (commandBlock.ContainsKey("subcommand"))
                                            {
                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("Can't combine {0} and {1}", commandBlock["subcommand"], key) } };
                                                break;
                                            }
                                            else
                                            {
                                                commandBlock.Add("subcommand", token);
                                                continue;
                                            }
                                        }
                                        else if (value_type is Dictionary<string, object>)
                                        {
                                            if (commandBlock.ContainsKey("subcommand"))
                                            {
                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("Can't combine {0} and {1}", commandBlock["subcommand"], key) } };
                                                break;
                                            }
                                            else
                                            {
                                                commandBlock.Add("subcommand", token);
                                                options = value_type as Dictionary<string, object>;
                                                continue;
                                            }
                                        }
                                        else if (value_type is string && (string)value_type == "null_type")
                                        {
                                            commandBlock.Add(token, "");
                                            continue;
                                        }
                                        else if ((index + 1) >= mc.Count)
                                        {
                                            if (value_type is string && ((string)value_type).Contains("_null_"))
                                            {
                                                if (commandBlock.ContainsKey("subcommand"))
                                                {
                                                    commandBlock = new Dictionary<string, object>() { { "error", string.Format("Can't combine {0} and {1}", commandBlock["subcommand"], key) } };
                                                    break;
                                                }
                                                else
                                                {
                                                    commandBlock.Add("subcommand", token);
                                                    continue;
                                                }
                                            }
                                            commandBlock = new Dictionary<string, object>() { { "error", string.Format("Missing value for {0}", key) } };
                                            break;
                                        }

                                        m = mc[index++];
                                        if (m.Groups["delimiter"].Value == "=")
                                        {
                                            m = mc[index++];

                                            if (value_type is string)
                                            {
                                                if ((string)value_type == "float_array_type")
                                                {
                                                    string value;
                                                    if (GetStringValueParameter(m, out value))
                                                    {
                                                        DoubleCollection dc = ConsoleUtilities.LengthCollectionFromString(value);
                                                        if (dc.Count > 0)
                                                        {
                                                            commandBlock.Add(key, value);
                                                        }
                                                        else
                                                        {
                                                            commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}", key) } };
                                                        }
                                                    }
                                                    else
                                                    {
                                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}", key) } };
                                                    }
                                                }
                                                else if (((string)value_type).StartsWith("float_"))
                                                {
                                                    double value;
                                                    bool allowArchitect = ((string)value_type).Contains("length") || ((string)value_type).Contains("size");

                                                    if (GetNumericValueParameter(m, allowArchitect, out value))
                                                    {
                                                        if (((string)value_type).Contains("positive"))
                                                        {
                                                            if (value > 0)
                                                            {
                                                                commandBlock.Add(key, value);
                                                            }
                                                            else
                                                            {
                                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("{0} must be positive", key) } };
                                                                break;
                                                            }
                                                        }
                                                        else if (((string)value_type).Contains("fraction"))
                                                        {
                                                            if (value >= 0 && value <= 1)
                                                            {
                                                                commandBlock.Add(key, value);
                                                            }
                                                            else
                                                            {
                                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("{0} must be between 0 and 1", key) } };
                                                                break;
                                                            }
                                                        }
                                                        else if (((string)value_type).Contains("nonnegative"))
                                                        {
                                                            if (value >= 0)
                                                            {
                                                                commandBlock.Add(key, value);
                                                            }
                                                            else
                                                            {
                                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("{0} can not be negative", key) } };
                                                                break;
                                                            }
                                                        }
                                                        else if (((string)value_type).Contains("nonzero"))
                                                        {
                                                            if (value != 0)
                                                            {
                                                                commandBlock.Add(key, value);
                                                            }
                                                            else
                                                            {
                                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("{0} can not be zero", key) } };
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            commandBlock.Add(key, value);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}: {1}", key, m.Value) } };
                                                        break;
                                                    }
                                                }
                                                else if (((string)value_type).StartsWith("integer_"))
                                                {
                                                    double value;
                                                    if (GetNumericValueParameter(m, false, out value))
                                                    {
                                                        if (((string)value_type).Contains("positive"))
                                                        {
                                                            if (value > 0)
                                                            {
                                                                commandBlock.Add(key, value);
                                                            }
                                                            else
                                                            {
                                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("{0} must be positive", key) } };
                                                                break;
                                                            }
                                                        }
                                                        else if (((string)value_type).Contains("nonnegative"))
                                                        {
                                                            if (value >= 0)
                                                            {
                                                                commandBlock.Add(key, value);
                                                            }
                                                            else
                                                            {
                                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("{0} can not be negative", key) } };
                                                                break;
                                                            }
                                                        }
                                                        else if (((string)value_type).Contains("minimum_3"))
                                                        {
                                                            if (value >= 3)
                                                            {
                                                                commandBlock.Add(key, value);
                                                            }
                                                            else
                                                            {
                                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("{0} must be >= 3", key) } };
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            commandBlock.Add(key, value);
                                                        }
                                                    }
                                                }
                                                else if (((string)value_type) == "color_type")
                                                {
                                                    uint colorSpec;

                                                    if (GetStringColorParameter(m, out colorSpec))
                                                    {
                                                        commandBlock.Add(key, Utilities.ColorNameFromColorSpec(colorSpec));
                                                    }
                                                    else
                                                    {
                                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid color value: {0}", m.Value) } };
                                                    }
                                                }
                                                else if (((string)value_type) == "fill_type")
                                                {
                                                    uint colorSpec;

                                                    if (m.Value == "none")
                                                    {
                                                        commandBlock.Add(key, RedDogGlobals.GS_None);
                                                    }
                                                    else if (m.Value == "layer")
                                                    {
                                                        commandBlock.Add(key, RedDogGlobals.GS_Layer);
                                                    }
                                                    else if (m.Value == "outline")
                                                    {
                                                        commandBlock.Add(key, RedDogGlobals.GS_Outline);
                                                    }
                                                    else if (GetStringColorParameter(m, out colorSpec))
                                                    {
                                                        commandBlock.Add(key, Utilities.ColorNameFromColorSpec(colorSpec));
                                                    }
                                                    else
                                                    {
                                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid color value: {0}", m.Value) } };
                                                    }
                                                }
                                                else if (((string)value_type).Contains("string"))
                                                {
                                                   if (m.Groups["quote"].Success)
                                                    {
                                                        commandBlock.Add(key, m.Groups["quote"].Value);
                                                    }
                                                     else if (m.Groups["token"].Success)
                                                    {
                                                        commandBlock.Add(key, m.Groups["token"].Value);
                                                    }
                                                    else
                                                    {
                                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}: {1}", key, m.Value) } };
                                                    }
                                                }
                                                else
                                                {
                                                    string value;

                                                    if (GetStringValueParameter(m, out value))
                                                    {
                                                        if ((string)value_type == "layer_type")
                                                        {
                                                            int layerId = ConsoleUtilities.LayerIdFromName(value);

                                                            if (layerId >= 0)
                                                            {
                                                                commandBlock.Add(key, value);
                                                            }
                                                            else
                                                            {
                                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid layer name: {0}", m.Value) } };
                                                                break;
                                                            }
                                                        }
                                                        else if ((string)value_type == "linetype_type")
                                                        {
                                                            int linetypeId = ConsoleUtilities.LineTypeIdFromName(value);

                                                            if (linetypeId >= 0)
                                                            {
                                                                commandBlock.Add(key, value);
                                                            }
                                                            else
                                                            {
                                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid linetype name: {0}", m.Value) } };
                                                                break;
                                                            }
                                                        }
                                                        else if ((string)value_type == "bool_type")
                                                        {
                                                            value = value.ToLower();

                                                            if (value == "true")
                                                            {
                                                                commandBlock.Add(key, true);
                                                            }
                                                            else if (value == "false")
                                                            {
                                                                commandBlock.Add(key, false);
                                                            }
                                                            else if (value == "yes")
                                                            {
                                                                commandBlock.Add(key, true);
                                                            }
                                                            else if (value == "no")
                                                            {
                                                                commandBlock.Add(key, false);
                                                            }
                                                            else
                                                            {
                                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}: {1}", key, m.Value) } };
                                                                break;
                                                            }
                                                        }
                                                        else if ((string)value_type == "unit_type")
                                                        {
                                                            switch (value.ToLower())
                                                            {
                                                                case "inches":
                                                                case "feet":
                                                                case "millimeters":
                                                                case "centimeters":
                                                                case "meters":
                                                                case "in":
                                                                case "ft":
                                                                case "mm":
                                                                case "cm":
                                                                    commandBlock.Add(key, value);
                                                                    break;

                                                                default:
                                                                    commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}: {1}", key, m.Value) } };
                                                                    break;
                                                            }
                                                        }
                                                        else if ((string)value_type == "unit_paper_type")
                                                        {
                                                            switch (value.ToLower())
                                                            {
                                                                case "inches":
                                                                case "millimeters":
                                                                case "in":
                                                                case "mm":
                                                                    commandBlock.Add(key, value);
                                                                    break;

                                                                default:
                                                                    commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}: {1}", key, m.Value) } };
                                                                    break;
                                                            }
                                                        }
                                                        else if (((string)value_type) == "ratio_type")
                                                        {
                                                            if (Utilities.ParseRatio(value, out double ratio))
                                                            {
                                                                commandBlock.Add(key, ratio);
                                                            }
                                                            else
                                                            {
                                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}: {1}", key, m.Value) } };
                                                            }
                                                        }
                                                        else if (((string)value_type).StartsWith("name_"))
                                                        {
                                                            commandBlock.Add(key, value);
                                                        }
                                                        else
                                                        {
                                                            commandBlock.Add(key, value);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}: {1}", key, m.Value) } };
                                                        break;
                                                    }

                                                    if (commandBlock.ContainsKey("error"))
                                                    {
                                                        break;
                                                    }
                                                }
                                            }
                                            else if (value_type is Dictionary<string, object>)
                                            {
                                                if (m.Groups["token"].Success)
                                                {
                                                    token = m.Groups["token"].Value;

                                                    if (((Dictionary<string, object>)value_type).ContainsKey(token))
                                                    {
                                                        commandBlock.Add(key, token);
                                                    }
                                                    else
                                                    {
                                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}: {1}", key, m.Value) } };
                                                        break;
                                                    }
                                                }
                                            }
                                            else if (value_type is List<string>)
                                            {
                                                if (m.Groups["token"].Success)
                                                {
                                                    token = m.Groups["token"].Value;

                                                    if (((List<string>)value_type).Contains(token))
                                                    {
                                                        commandBlock.Add(key, token);
                                                    }
                                                    else
                                                    {
                                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}: {1}", key, m.Value) } };
                                                        break;
                                                    }
                                                }
                                                else if (m.Groups["integer"].Success)
                                                {
                                                    token = m.Groups["integer"].Value;

                                                    if (((List<string>)value_type).Contains(token))
                                                    {
                                                        commandBlock.Add(key, token);
                                                    }
                                                    else
                                                    {
                                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}: {1}", key, m.Value) } };
                                                        break;
                                                    }
                                                }
                                                else if (m.Groups["quote"].Success)
                                                {
                                                    token = m.Groups["quote"].Value;

                                                    if (((List<string>)value_type).Contains(token))
                                                    {
                                                        commandBlock.Add(key, token);
                                                    }
                                                    else
                                                    {
                                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid value for {0}: {1}", key, m.Value) } };
                                                        break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                commandBlock = new Dictionary<string, object>() { { "error", string.Format("internal error - invalid value type: {0}", value_type) } };
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            commandBlock = new Dictionary<string, object>() { { "error", string.Format("expected '=' after {0}", key) } };
                                            break;
                                        }
                                    }
                                    else if (options.ContainsKey("*") && (string)options["*"] == "subcommand_type" && commandBlock.ContainsKey("subcommand") == false)
                                    {
                                        commandBlock.Add("subcommand", token);
                                    }
                                    else if (options.ContainsKey("**") && (string)options["**"] == "subcommand_type" && commandBlock.ContainsKey("subcommand2") == false)
                                    {
                                        commandBlock.Add("subcommand2", token);
                                    }
                                    else
                                    {
                                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid parameter: {0}", token) } };
                                        break;
                                    }
                                }
                                else if (m.Groups["coordinates"].Success)
                                {
                                    string rawpoints = m.Groups["coordinates"].Value;
                                    string[] pa = rawpoints.Split(new[] { ';' });
                                    foreach (string p in pa)
                                    {
                                        Dictionary<string, object> coord = ParseCoordinate(p);
                                        if (coord.ContainsKey("error"))
                                        {
                                            commandBlock = coord;
                                            break;
                                        }
                                        else
                                        {
                                            points.Add(coord);
                                        }
                                    }

                                    if (commandBlock.ContainsKey("error"))
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    commandBlock = new Dictionary<string, object>() { { "error", string.Format("unexpected input: {0}", m.Value) } };
                                    break;
                                }
                            }
                        }
                        else
                        {
                            commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid command: {0}", token) } };
                        }
                    }
                    else
                    {
                        commandBlock = new Dictionary<string, object>() { { "error", string.Format("invalid input: {0}", command) } };
                    }
                }

                if (commandBlock.ContainsKey("command"))
                {
                    if (points.Count > 0)
                    {
                        commandBlock.Add("points", points);
                    }
                }
            }

            return commandBlock;
        }
    }
}
