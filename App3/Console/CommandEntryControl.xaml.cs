using Cirros;
using RedDog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.Windows.System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using App3;
using Windows.System;
using Microsoft.UI;

namespace KT22.Console
{
    public sealed partial class CommandEntryControl : UserControl
    {
        string _promptString = "% ";
        int _currentLineStart = 0;

        List<string> _commandHistory = new List<string>();
        List<HistoryEntry> _rawHistory = new List<HistoryEntry>();

        int _bufferLength = 60;

        KTCommandProcessor _commandProcessor;
        CommandParser _parser;
        ScriptEngine _scriptEngine;

        ModalCommand _modalCommand = null;

        public CommandEntryControl()
        {
            this.InitializeComponent();

            _commandProcessor = KTCommandProcessor.CommandProcessor;
            _commandProcessor.Initialize(this);

            _parser = _commandProcessor.Parser;
            _scriptEngine = _commandProcessor.ScriptEngine;

            this.Loaded += CommandEntryControl_Loaded;
            _consoleInputTextBox.TextChanged += _consoleInputTextBox_TextChanged;
            _consoleInputTextBox.KeyDown += _consoleInputTextBox_KeyDown;
            _consoleInputTextBox.KeyUp += _consoleInputTextBox_KeyUp;
            _consoleInputTextBox.GotFocus += _consoleInputTextBox_GotFocus;

            _historyScrollViewer.SizeChanged += _historyScrollViewer_SizeChanged;
            _historyGrid.PointerPressed += _historyGrid_PointerPressed;
            _historyPanel.GotFocus += _historyPanel_GotFocus;

            Prompt(PromptString);
        }

        public void DoModalCommand(ModalCommand modalCommand)
        {
            if (modalCommand != null && modalCommand.Finished == false)
            {
                _modalCommand = modalCommand;
            }
        }

        public void Activate()
        {
            _consoleInputTextBox.Focus(FocusState.Programmatic);
        }

        private void _consoleInputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Prompt(PromptString);
        }

        private void _historyPanel_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Globals.CommandProcessor != null)
            {
                Globals.Events.PointAdded(Globals.workCanvas.CursorLocation, "enter");

                Globals.CommandProcessor.Finish();
                Globals.CommandProcessor = null;
            }
            Globals.Input.SelectCursor(Cirros.Core.CursorType.Arrow);

            _consoleInputTextBox.Focus(FocusState.Keyboard);
        }

        public string PromptString
        {
            get
            {
                return _modalCommand == null ? _promptString : _modalCommand.Prompt;
            }
            set
            {
                _promptString = value;
            }
        }

        void _historyScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize == e.NewSize) return;
            if (Cirros.Utility.Utilities.__checkSizeChanged(52, sender)) return;

            _historyGrid.Width = e.NewSize.Width;
            
            if (_historyGrid.Width > 50)
            {
                _consoleInputTextBox.Focus(FocusState.Keyboard);
            }
        }

        void _historyGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _consoleInputTextBox.Focus(FocusState.Keyboard);
        }

        void _historyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _consoleInputTextBox.Focus(FocusState.Keyboard);
        }

        void CommandEntryControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        enum ACTYPE
        {
            none,
            command,
            parameter,
            option
        }

        ACTYPE _acType = ACTYPE.none;

        string _acString = "";
        Dictionary<string, object> _acParams = null;
        ICollection<string> _acOptions = null;
        int _acMatchStart = 0;
        int _acMatchLength = 0;

        private void AcceptSuggestion()
        {
            if (_acString.Length > 0)
            {
                _consoleInputTextBox.Text += _acString;
                _acString = "";

                string match = _consoleInputTextBox.Text.Substring(_acMatchStart).ToLower();
                if (match.Contains(' '))
                {
                    _consoleInputTextBox.Text = _consoleInputTextBox.Text.Substring(0, _acMatchStart);
                    _consoleInputTextBox.Text += "\"" + match + "\"";
                }

                _consoleInputTextBox.Select(_consoleInputTextBox.Text.Length, 0);

                if (_acType == ACTYPE.command)
                {
                    if (KT22.Console.KTCommandLanguage.Commands.ContainsKey(match))
                    {
                        _acParams = KT22.Console.KTCommandLanguage.Commands[match];
                    }
                    else
                    {
                        _acParams = null;
                    }
                    _acType = ACTYPE.parameter;
                }
                else if (_acType == ACTYPE.parameter)
                {
                    if (_acParams != null && _acParams.ContainsKey(match))
                    {
                        object optionType = _acParams[match];

                        System.Diagnostics.Debug.WriteLine("{0}", optionType.GetType().ToString());
                        if (optionType is List<string>)
                        {
                            _acOptions = optionType as List<string>;
                        }
                        else if (optionType is Dictionary<string, object> d)
                        {
                            _acParams = d;
                        }
                        else if (optionType is string)
                        {
                            switch (optionType as string)
                            {
                                case "bool_type":
                                    _acOptions = new List<string> { "true", "false", "yes", "no" };
                                    break;

                                case "unit_type":
                                    _acOptions = new List<string> { "inches", "in", "feet", "ft", "millimeters", "mm", "centimeters", "cm", "meters" };
                                    break;

                                case "unit_paper_type":
                                    _acOptions = new List<string> { "inches", "in", "millimeters", "mm" };
                                    break;

                                case "layer_type":
                                    _acOptions = Globals.LayerNames;
                                    break;

                                case "color_type":
                                    List<string> list = Cirros.Core.StandardColors.ColorNames.Keys.ToList<string>();
                                    list.Sort();
                                    _acOptions = list;
                                    break;

                                case "linetype_type":
                                    _acOptions = Globals.LineTypeNames;
                                    break;

                                case "fill_type":
                                    List<string> fo = new List<string>() { RedDogGlobals.GS_None, RedDogGlobals.GS_Layer, RedDogGlobals.GS_Outline };
                                    fo.Sort();
                                    fo.AddRange(Cirros.Core.StandardColors.ColorNames.Keys);
                                    _acOptions = fo;
                                    break;

                                case "text_style_type":
                                    _acOptions = Globals.TextStyleNames;
                                    break;

                                case "arrow_style_type":
                                    _acOptions = Globals.ArrowStyleNames;
                                    break;

                                default:
                                    _acOptions = null;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        _acOptions = null;
                    }
                    _acType = ACTYPE.parameter;
                }
                else if (_acType == ACTYPE.option)
                {
                    _acType = ACTYPE.parameter;
                }
            }
            else
            {
                _acType = ACTYPE.none;
            }
        }

        List<string> _optionSuggestions = new List<string>();
        int _optionSuggestionIndex = 0;

        private bool NextSuggestion(int increment = 1)
        {
            bool suggest = false;
            if (_acType == ACTYPE.option && _optionSuggestions != null && _optionSuggestions.Count > 1)
            {
                if (_consoleInputTextBox.Text.Length > 0)
                {
                    int leq = _consoleInputTextBox.Text.LastIndexOf('=');
                    _acMatchStart = leq + 1;
                    _acMatchLength = _consoleInputTextBox.Text.Length - _acMatchStart;

                    if (increment > 0)
                    {
                        _optionSuggestionIndex = ++_optionSuggestionIndex % _optionSuggestions.Count;
                    }
                    else if (_optionSuggestionIndex > 0)
                    {
                        _optionSuggestionIndex = --_optionSuggestionIndex;
                    }
                    else
                    {
                        _optionSuggestionIndex = _optionSuggestions.Count - 1;
                    }

                    string suggestion = _optionSuggestions[_optionSuggestionIndex];
                    int suggestionlength = suggestion.Length - _acMatchLength;
                    int suggestionstart = _acMatchStart + _acMatchLength;

                    int length = _consoleInputTextBox.Text.Length + suggestionlength;
                    string format = "{0," + length.ToString() + "}";

                    _acString = suggestion.Substring(_acMatchLength);
                    _consoleShadowTextBox.Text = string.Format(format, _acString);
                    suggest = true;
                }
            }

            return suggest;
        }

        private bool PrevousSuggestion()
        {
            return NextSuggestion(-1);
        }

        private void ShowSuggestion()
        {
            _consoleShadowTextBox.Text = "";
            _optionSuggestions.Clear();

            if (_consoleInputTextBox.Text.Length > 0 && _consoleInputTextBox.Text.IndexOf("//") < 0)
            {
                int lsp = _consoleInputTextBox.Text.LastIndexOf(' ');
                int leq = _consoleInputTextBox.Text.LastIndexOf('=');

                System.Diagnostics.Debug.WriteLine(_acType);
                System.Diagnostics.Debug.WriteLine("ShowSuggestion: {0} {1} {2}", _acType, lsp, leq);

                _acString = "";

                //if (leq > lsp || _acType == ACTYPE.option)
                if (leq > lsp && lsp > 0)
                {
                    _acType = ACTYPE.option;

                    _acMatchStart = leq + 1;
                    _acMatchLength = _consoleInputTextBox.Text.Length - _acMatchStart;

                    if (_acMatchLength > 0 && _acOptions != null)
                    {
                        string partial = _consoleInputTextBox.Text.Substring(_acMatchStart, _acMatchLength).ToLower();

                        foreach (string s in _acOptions)
                        {
                            if (s.StartsWith(partial))
                            {
                                _optionSuggestions.Add(s);
                            }
                        }

                        if (_optionSuggestions.Count > 0)
                        {
                            _optionSuggestionIndex = 0;
                            string suggestion = _optionSuggestions[_optionSuggestionIndex];
                            int suggestionlength = suggestion.Length - _acMatchLength;
                            int suggestionstart = _acMatchStart + _acMatchLength;

                            int length = _consoleInputTextBox.Text.Length + suggestionlength;

                            string format = "{0," + length.ToString() + "}";
                            _acString = suggestion.Substring(_acMatchLength);
                            _consoleShadowTextBox.Text = string.Format(format, _acString);
                        }
                    }
                    else if (_acMatchLength == 0 && _acOptions != null)
                    {
                        foreach (string s in _acOptions)
                        {
                            _optionSuggestions.Add(s);
                        }

                        _optionSuggestionIndex = 0;

                        string suggestion = _optionSuggestions[_optionSuggestionIndex];
                        int length = _consoleInputTextBox.Text.Length + suggestion.Length;

                        string format = "{0," + length.ToString() + "}";
                        _acString = suggestion;
                        _consoleShadowTextBox.Text = string.Format(format, _acString);
                    }
                }
                else if (lsp < _consoleInputTextBox.Text.Length)
                {
                    _acMatchStart = lsp + 1;
                    _acMatchLength = _consoleInputTextBox.Text.Length - _acMatchStart;

                    if (_acMatchLength > 0)
                    {
                        string partial = _consoleInputTextBox.Text.Substring(_acMatchStart, _acMatchLength).ToLower();
                        ICollection<string> suggestions;

                        if (_acMatchStart == _promptString.Length)
                        {
                            _acType = ACTYPE.command;
                            suggestions = KT22.Console.KTCommandLanguage.Commands.Keys;
                        }
                        else if (_acParams != null)
                        {
                            _acType = ACTYPE.parameter;
                            suggestions = _acParams.Keys;
                        }
                        else 
                        {
                            int fsp = _consoleInputTextBox.Text.IndexOf(' ', _promptString.Length);
                            string match = _consoleInputTextBox.Text.Substring(_promptString.Length, fsp - _promptString.Length);
                            if (KT22.Console.KTCommandLanguage.Commands.ContainsKey(match))
                            {
                                _acParams = KT22.Console.KTCommandLanguage.Commands[match];
                            }
                            else
                            {
                                _acParams = null;
                                return;
                            }

                            _acType = ACTYPE.parameter;
                            suggestions = _acParams.Keys;
                        }

                        foreach (string suggestion in suggestions)
                        {
                            if (suggestion.StartsWith(partial))
                            {
                                int suggestionlength = suggestion.Length - _acMatchLength;
                                int suggestionstart = _acMatchStart + _acMatchLength;

                                int length = _consoleInputTextBox.Text.Length + suggestionlength;

                                string format = "{0," + length.ToString() + "}";
                                _acString = suggestion.Substring(_acMatchLength);
                                _consoleShadowTextBox.Text = string.Format(format, _acString);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                _acString = "";
            }
        }

        public void Prompt(string promptString)
        {
            _acString = "";
            _acParams = null;
            _acType = ACTYPE.none;

            _consoleInputTextBox.Text = promptString;
            _currentLineStart = _consoleInputTextBox.Text.Length;
            _consoleInputTextBox.Select(_currentLineStart, 0);

            if (FocusManager.GetFocusedElement() == _consoleInputTextBox)
            {
                _consoleShadowTextBox.Text = "";
                _consoleShadowTextBox.FontStyle = Windows.UI.Text.FontStyle.Normal;
                _consoleShadowTextBox.Foreground = new SolidColorBrush(_messageColor);
            }
            else
            {
                _consoleInputTextBox.Text = "";
                _consoleShadowTextBox.Text =  "  -- enter points --";
                _consoleShadowTextBox.FontStyle = Windows.UI.Text.FontStyle.Italic;
                _consoleShadowTextBox.Foreground = new SolidColorBrush(_promptColor);
            }
        }

        void _consoleInputTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {

        }

        //private List<string> _history = new List<string>();
        private int _currentHistoryEntry = 0;
        private string _savedCommand = null;

        async void _consoleInputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_modalCommand != null)
            {
                if (e.Key == VirtualKey.Enter)
                {
                    e.Handled = true;

                    AppendToHistory(_consoleInputTextBox.Text, _consoleColor);

                    string ps = PromptString;
                    string command = _consoleInputTextBox.Text.StartsWith(ps) ? _consoleInputTextBox.Text.Substring(ps.Length) : _consoleInputTextBox.Text;

                    await Execute(command);

                    Prompt(PromptString);
                }
                else
                {
                    _historyScrollViewer.UpdateLayout();
                    _historyScrollViewer.ChangeView(0, _historyScrollViewer.ScrollableHeight, 1, true);
                }
            }
            else if (e.Key == VirtualKey.Back)
            {
                if (_consoleInputTextBox.Text.Length <= _currentLineStart)
                {
                    e.Handled = true;

                    if (_consoleInputTextBox.Text.Length == (_currentLineStart - 1))
                    {
                        Prompt(PromptString);
                    }
                }
            }
            else if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;

                if (_modalCommand == null)
                {
                    AcceptSuggestion();
                }

                AppendToHistory(_consoleInputTextBox.Text, _consoleColor);

                string ps = PromptString;
                string command = _consoleInputTextBox.Text.StartsWith(ps) ? _consoleInputTextBox.Text.Substring(ps.Length) : _consoleInputTextBox.Text;
                //string command = _consoleInputTextBox.Text.Substring(2);

                _consoleInputTextBox.Text = "";
                await Execute(command);

                if (_modalCommand == null && command.Length > 0)
                {
                    _commandHistory.Add(_consoleInputTextBox.Text);
                }

                Prompt(PromptString);
            }
            else if (e.Key == VirtualKey.Up)
            {
                e.Handled = true;

                if (PrevousSuggestion() == false)
                {
                    if (_savedCommand == null)
                    {
                        _currentHistoryEntry = _commandHistory.Count;
                        _savedCommand = _consoleInputTextBox.Text;
                    }

                    if (_currentHistoryEntry > 0)
                    {
                        _consoleInputTextBox.Text = _commandHistory[--_currentHistoryEntry];
                        _consoleInputTextBox.Select(_consoleInputTextBox.Text.Length, 0);
                    }
                }
            }
            else if (e.Key == VirtualKey.Down)
            {
                e.Handled = true;

                if (NextSuggestion() == false)
                {
                    if (_savedCommand == null)
                    {
                        _currentHistoryEntry = _commandHistory.Count;
                        _savedCommand = _consoleInputTextBox.Text;
                    }

                    if (_currentHistoryEntry < (_commandHistory.Count - 1))
                    {
                        _currentHistoryEntry++;

                        _consoleInputTextBox.Text = _commandHistory[_currentHistoryEntry];
                        _consoleInputTextBox.Select(_consoleInputTextBox.Text.Length, 0);
                    }
                    else
                    {
                        _consoleInputTextBox.Text = _promptString;
                        _consoleInputTextBox.Select(_consoleInputTextBox.Text.Length, 0);
                    }
                }

                if (_savedCommand != null && _currentHistoryEntry == (_rawHistory.Count - 1))
                {
                    _consoleInputTextBox.Text = _savedCommand;
                    _consoleInputTextBox.Select(_consoleInputTextBox.Text.Length, 0);
                    _savedCommand = null;
                }
            }
            else if (e.Key == VirtualKey.Space)
            {
                if (_acString.StartsWith(" "))
                {
                }
                else
                {
                    AcceptSuggestion();
                }
            }
            else if ((int)e.Key == 187)
            {
                AcceptSuggestion();
            }
            else if (e.Key == VirtualKey.Tab)
            {
                e.Handled = true;
                AcceptSuggestion();
            }
            else
            {
                _historyScrollViewer.UpdateLayout();
                _historyScrollViewer.ChangeView(0, _historyScrollViewer.ScrollableHeight, 1, true);
            }
        }

        private async Task<string> Execute(string command)
        {
            string error = null;

            if (_modalCommand != null)
            {
                await _modalCommand.DoCommand(command);
                
                if (_modalCommand.Finished)
                {
                    _modalCommand = null;

                    Prompt(PromptString);
                }
            }
            else
            {
                Dictionary<string, object> parserResult = _parser.ParseCommand(command);

                if (parserResult.Count > 0)
                {
                    if (parserResult.ContainsKey("command"))
                    {
                        switch ((string)parserResult["command"])
                        {
                            case "console":
                                {
                                    if (parserResult.ContainsKey("subcommand") && (string)parserResult["subcommand"] == "clear")
                                    {
                                        Clear();
                                    }
                                    if (parserResult.ContainsKey("buffer-length"))
                                    {
                                        _bufferLength = (int)Math.Min((double)parserResult["buffer-length"], 1000.0);
                                    }
                                }
                                break;

                            case "help":
                                {
                                    if (parserResult.ContainsKey("subcommand"))
                                    {
                                        if (parserResult.ContainsKey("subcommand2"))
                                        {
                                            ShowHelp((string)parserResult["subcommand"], (string)parserResult["subcommand2"]);
                                        }
                                        else
                                        {
                                            ShowHelp((string)parserResult["subcommand"]);
                                        }
                                    }
                                    else
                                    {
                                        ShowHelp(null);
                                    }
                                }
                                break;

                            case "log":
                                {
                                    string subcommand = parserResult.ContainsKey("subcommand") ? (string)parserResult["subcommand"] : "";

                                    switch (subcommand)
                                    {
                                        case "start":
                                            await _commandProcessor.StartLoggingAsync();
                                            break;

                                        case "stop":
                                            await _commandProcessor.StopLogging();
                                            break;

                                        case "save":
                                            await _commandProcessor.SaveLogAsync();
                                            break;

                                        case "saveas":
                                            await SaveLogAsAsync();
                                            break;

                                        case "print":
                                            _commandProcessor.PrintLog();
                                            break;

                                        case "clear":
                                            _commandProcessor.ClearLog();
                                            break;
                                    }
                                }
                                break;

                            case "macro":
                                {
                                    bool verbose = false;

                                    if (parserResult.ContainsKey("verbose"))
                                    {
                                        verbose = (bool)parserResult["verbose"];
                                    }

                                    if (parserResult.ContainsKey("file"))
                                    {
                                        string file = (string)parserResult["file"];
                                        await RunMacroAsync(file, verbose);
                                    }
                                    else
                                    {
                                        string subcommand = parserResult.ContainsKey("subcommand") ? (string)parserResult["subcommand"] : "";

                                        if (subcommand == "run")
                                        {
                                            await SelectAndRunMacroAsync(verbose);
                                        }
                                    }
                                }
                                break;

                            default:
                                {
                                    error = await _commandProcessor.Execute(parserResult);
                                }
                                break;
                        }
                    }
                    else if (parserResult.ContainsKey("error"))
                    {
                        error = parserResult["error"] as string;
                    }
                    else
                    {
                        error = "invalid command";
                    }
                }
            }

            if (error != null)
            {
                Error(error);
            }

            return error;
        }

        public async Task SaveLogAsAsync()
        {
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
            FileSavePicker savePicker = InitializeWithWindow(new FileSavePicker(),App.WindowHandle);
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            savePicker.FileTypeChoices.Add("dbmacro", new List<string>() { ".dbmacro" });

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    StringBuilder sb = _commandProcessor.StringifyLog();
                    await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
                }
                catch (Exception ex)
                {
                    Analytics.ReportError("SaveLogAsAsync", ex, 1, 706);
                }
            }
            else
            {
                // cancelled
            }
        }

        private static FileSavePicker InitializeWithWindow(FileSavePicker obj, IntPtr windowHandle)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
            return obj;
        }

        public async Task SelectAndRunMacroAsync(bool verbose)
        {
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
            FileOpenPicker openPicker = InitializeWithWindow(new FileOpenPicker(),App.WindowHandle);
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".dbmacro");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                await RunMacroAsync(file, verbose);
            }
        }

        private static FileOpenPicker InitializeWithWindow(FileOpenPicker obj, IntPtr windowHandle)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
            return obj;
        }

        public async Task RunMacroAsync(string filename, bool verbose)
        {
            if (filename.EndsWith(".dbmacro") == false)
            {
                filename = filename + ".dbmacro";
            }

            StorageFolder folder = await _commandProcessor.GetLogFolder();
            StorageFile file = await folder.TryGetItemAsync(filename) as StorageFile;
            if (file != null)
            {
                await RunMacroAsync(file, verbose);
            }
        }

        public async Task RunMacroAsync(StorageFile file, bool verbose)
        {
            if (file != null)
            {
                string filetype = file.FileType.ToLower();

                if (filetype == ".dbmacro")
                {
                    var readFile = await Windows.Storage.FileIO.ReadLinesAsync(file);
                    foreach (var line in readFile)
                    {
                        if (verbose)
                        {
                            AppendToHistory(line, _messageColor);
                        }
                        if (await Execute(line) != null)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void ShowHelp(string p, string p1 = null)
        {
            if (p == null)
            {
                string [] commands = KTCommandLanguage.Commands.Keys.ToArray<string>();

                AppendToHistory("usage: help [[command] | expression | coordinate]", _messageColor);
                AppendToHistory(string.Format("command: {0}", commands[0]), _messageColor);

                for (int i = 1; i < KTCommandLanguage.Commands.Keys.Count; i++)
                {
                    AppendToHistory(string.Format("         {0}", commands[i]), _messageColor);
                }
            }
            else if (p == "expression")
            {
                AppendToHistory("numeric value: [constant] | {numeric expression}", _messageColor);
            }
            else if (p == "coordinate")
            {
                AppendToHistory("coordinate: [absolute coordinate | delta coordinate | polar coordinate; [coordinate]],", _messageColor);
            }
            else if (KTCommandLanguage.Commands.ContainsKey(p))
            {
                Dictionary<string, object> options = KTCommandLanguage.Commands[p];

                if (options == null || options.Count == 0)
                {
                    AppendToHistory(string.Format("usage: {0}", p), _messageColor);
                }
                else
                {
                    if (p1 != null && options.ContainsKey(p1) && options[p1] is Dictionary<string, object>)
                    {
                        AppendToHistory(string.Format("usage:   {0} {1} [options]", p, p1), _messageColor);
                        options = options[p1] as Dictionary<string, object>;
                    }
                    else
                    {
                        AppendToHistory(string.Format("usage:   {0} [options]", p), _messageColor);
                    }

                    string h = "options: ";

                    foreach (string key in options.Keys)
                    {
                        string value;

                        if (options[key] is string)
                        {
                            value = (string)options[key];

                            switch (value)
                            {
                                case "layer_type":
                                    value = "Layer name";
                                    break;

                                case "linetype_type":
                                    value = "Linetype name";
                                    break;

                                case "color_type":
                                    value = "Color name or color spec (#AARRGGBB)";
                                    break;

                                case "bool_type":
                                    value = "'true' | 'false'";
                                    break;

                                default:
                                    if (value.StartsWith("float_"))
                                    {
                                        if (value.Contains("size"))
                                        {
                                            value = "size";
                                        }
                                        else if (value.Contains("length"))
                                        {
                                            value = "length";
                                        }
                                        else if (value.Contains("angle"))
                                        {
                                            value = "angle";
                                        }
                                        else 
                                        {
                                            value = "float";
                                        }
                                    }
                                    break;
                            }
                        }
                        else if (options[key] is List<string>)
                        {
                            List<string> list = options[key] as List<string>;
                            StringBuilder sb = new StringBuilder();

                            sb.AppendFormat("'{0}'", list[0]);

                            for (int i = 1; i < list.Count; i++)
                            {
                                sb.AppendFormat(" | '{0}'", list[i]);
                            }

                            value = sb.ToString();
                        }
                        else if (options[key] is Dictionary<string, object>)
                        {
                            value = "options";
                        }
                        else
                        {
                            value = "object";
                        }

                        AppendToHistory(string.Format("{0}{1} = [{2}]", h, key, value), _messageColor);
                        h = "         ";
                    }
                }
            }
            else
            {
                AppendToHistory(string.Format("no help for {0}", p), _errorColor);
            }
        }

        private void Clear()
        {
            _rawHistory.Clear();

            List<FrameworkElement> deleteList = new List<FrameworkElement>();

            foreach (FrameworkElement fe in _historyGrid.Children)
            {
                if (fe is TextBlock)
                {
                    deleteList.Add(fe);
                }
            }

            foreach (FrameworkElement fe in deleteList)
            {
                _historyGrid.Children.Remove(fe);
            }

            _historyGrid.RowDefinitions.Clear();
            _historyGrid.RowDefinitions.Add(new RowDefinition());
        }

        class HistoryEntry
        {
            string _prompt;
            string _content;
            Color _color;

            public HistoryEntry(string prompt, string content, Color color)
            {
                _prompt = prompt;
                _content = content.TrimEnd();
                _color = color;
            }

            public string Prompt
            {
                get { return _prompt; }
            }

            public string Content
            {
                get { return _content; }
            }

            public Color Color
            {
                get { return _color; }
            }
        }

        private void AppendToHistory(string content, Color color)
        {
            _rawHistory.Add(new HistoryEntry(_promptString, content, color));
            _savedCommand = null;

            if (_historyGrid.RowDefinitions.Count < _bufferLength)
            {
                int row = _historyGrid.RowDefinitions.Count;

                RowDefinition rd = new RowDefinition();
                rd.Height = new GridLength(15);
                _historyGrid.RowDefinitions.Add(rd);

                TextBlock tb = new TextBlock();
                tb.Style = (Style)(this.Resources["HistoryText"]);
                tb.Text = content;
                tb.Foreground = new SolidColorBrush(color);
                tb.SetValue(Grid.RowProperty, row);
                tb.SelectionChanged += tb_SelectionChanged;
                tb.KeyDown += tb_KeyDown;
                _historyGrid.Children.Add(tb);
            }
            else
            {
                if (_rawHistory.Count > 0)
                {
                    _rawHistory.RemoveAt(0);
                }

                foreach (FrameworkElement fe in _historyGrid.Children)
                {
                    if (fe is TextBlock)
                    {
                        int row = (int)fe.GetValue(Grid.RowProperty);
                        if (row < _rawHistory.Count)
                        {
                            ((TextBlock)fe).Text = _rawHistory[row].Content;
                            ((TextBlock)fe).Foreground = new SolidColorBrush(_rawHistory[row].Color);
                        }
                    }
                }
            }
      
            _historyScrollViewer.UpdateLayout();
            _historyScrollViewer.ChangeView(0, _historyScrollViewer.ScrollableHeight, 1, true);
        }

        void tb_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            _consoleInputTextBox.Focus(FocusState.Keyboard);
        }

        void tb_SelectionChanged(object sender, RoutedEventArgs e)
        {
        }

        void _consoleInputTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
#if SIBERIA
            if (FocusManager.GetFocusedElement() == _consoleInputTextBox)
            {
                if (_modalCommand == null)
                {
                    ShowSuggestion();
                }
            }
#endif
        }

        Color _consoleColor = Colors.Black;
        Color _messageColor = Colors.Green;
        Color _errorColor = Colors.Red;
        Color _promptColor = Colors.Gray;

        public void Print(string s)
        {
            AppendToHistory(s, _consoleColor);
        }

        public void Print(object o)
        {
            AppendToHistory(string.Format("{0}", o), _consoleColor);
        }

        public void PrintResult(string s)
        {
            AppendToHistory(s, _messageColor);
        }

        public void Error(string s)
        {
            AppendToHistory(s, _errorColor);
        }
    }
}
