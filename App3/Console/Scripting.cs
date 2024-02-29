using App3;
using Jint;
using Jint.Native;
using Jint.Runtime;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace KT22.Console
{
    public class ScriptEngine
    {
        Engine _jint_engine;
        CommandEntryControl _console;
        KTCommandProcessor _processor;
        StorageFolder _scriptFolder = null;
        StorageFolder _userScriptFolder = null;
        double _scriptTimeout = 10;

        public async void InitializeJint(CommandEntryControl console, KTCommandProcessor processor)
        {
            try
            {
                _scriptFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("scripts", CreationCollisionOption.OpenIfExists);
                //System.Diagnostics.Debug.WriteLine(_scriptFolder.Path);

                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Containers.ContainsKey("console"))
                {
                    ApplicationDataContainer consoleSettings = localSettings.Containers["console"];
                    if (consoleSettings != null)
                    {
                        string token = (string)consoleSettings.Values["scriptFolder"];
                        _userScriptFolder = await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(token);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                // fail silently
            }

            _console = console;
            _processor = processor;
            _jint_engine = JintEngine(_scriptTimeout);
        }

        public double ScriptTimeout
        {
            get
            {
                return _scriptTimeout;
            }
            set
            {
                _scriptTimeout = value;
                _jint_engine = JintEngine(_scriptTimeout);
                _console.Error("The script environment has been reset");
            }
        }

        public Engine JintEngine(double seconds)
        {
            long ticks = (long)(seconds * 10000000);

            Engine engine = new Engine(cfg => cfg.AllowClr().TimeoutInterval(new TimeSpan(ticks)))
                .SetValue("print", new Action<object>(_console.Print))
                .SetValue("db_test", new Action<string, string, bool>(_processor.DbTest))
                .SetValue("db_clear_test", new Action(_processor.ClearTestResults))
                .SetValue("db_test_results", new Func<string>(_processor.GetTestResults))
                .SetValue("db", new Action<string>(_processor.Db));

            return engine;
        }

        public async Task<StorageFolder> SelectUserScriptFolder()
        {
            StorageFolder folder = null;
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/

            FolderPicker folderPicker = InitializeWithWindow(new FolderPicker(),App.WindowHandle);
            folderPicker.ViewMode = PickerViewMode.List;
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.CommitButtonText = "Select script folder";
            folderPicker.FileTypeFilter.Add(".js");

            folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                string token = StorageApplicationPermissions.MostRecentlyUsedList.Add(folder, DateTime.Now.ToFileTime().ToString());

                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Containers.ContainsKey("console"))
                {
                    try
                    {
                        ApplicationDataContainer container = localSettings.Containers["console"];
                        container.Values["scriptFolder"] = token;
                    }
                    catch
                    {
                    }
                }
                else
                {
                    try
                    {
                        ApplicationDataContainer container = localSettings.CreateContainer("console", ApplicationDataCreateDisposition.Always);
                        container.Values["scriptFolder"] = token;
                    }
                    catch
                    {
                    }
                }
            }

            return folder;
        }

        private static FolderPicker InitializeWithWindow(FolderPicker obj, IntPtr windowHandle)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
            return obj;
        }

        public StorageFolder UserScriptFolder
        {
            get
            {
                return _userScriptFolder;
            }
            set
            {
                _userScriptFolder = value;
            }
        }

        public JsValue Js(string input, out string error)
        {
            error = null;
            string output = "";
            JsValue result = new JsString(input);

            try
            {
#if SIBERIA
                result = _jint_engine.GetValue(_jint_engine.Execute(input).GetCompletionValue());

                if (result.Type != Types.None && result.Type != Types.Null && result.Type != Types.Undefined)
                {
                    var str = TypeConverter.ToString(_jint_engine.Json.Stringify(_jint_engine.Json, Arguments.From(result, Undefined.Instance, "  ")));
                    output = string.Format("{0}", str);
                }
#endif
            }
            catch (JavaScriptException je)
            {
                error = je.ToString();
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                   error = e.InnerException.Message;
                }
                else
                {
                    error = e.Message;
                }
            }

            return result;
        }

        public async void JsFile(string filename)
        {
            if (String.IsNullOrEmpty(filename))
            {
                _console.Error("file name is not valid");
            }
            else 
            {
                string jsfile = filename.EndsWith(".js") ? filename : filename + ".js";
                StorageFile file = null;

                try
                {
                    if (_userScriptFolder != null && await _userScriptFolder.TryGetItemAsync(jsfile) != null)
                    {
                        file = await _userScriptFolder.GetFileAsync(jsfile);
                    }
                    else if (await _scriptFolder.TryGetItemAsync(jsfile) != null)
                    {
                        file = await _scriptFolder.GetFileAsync(jsfile);
                    }
                    else if (_userScriptFolder == null)
                    {
                        _console.Error("need to select a script folder");
                    }
                    else
                    {
                        _console.Error(string.Format("file was not found: {0}", _userScriptFolder.Path + @"\" + jsfile));
                    }
                }
                catch
                {
                    _console.Error(string.Format("invalid name: {0}", jsfile));
                }

                if (file != null)
                {
                    try
                    {
#if SIBERIA
                        Stream rs = (await file.OpenReadAsync()).AsStreamForRead();
                        StreamReader reader = new StreamReader(rs);
                        var script = reader.ReadToEnd();

                        var result = _jint_engine.GetValue(_jint_engine.Execute(script).GetCompletionValue());

                        if (result.Type != Types.None && result.Type != Types.Null && result.Type != Types.Undefined)
                        {
                            var str = TypeConverter.ToString(_jint_engine.Json.Stringify(_jint_engine.Json, Arguments.From(result, Undefined.Instance, "  ")));
                            _console.PrintResult(string.Format("=> {0}", str));
                        }
#endif
                    }
                    catch (JavaScriptException je)
                    {
                        _console.Error(je.ToString());
                    }
                    catch (Exception e)
                    {
                        if (e.InnerException != null)
                        {
                            _console.Error(e.InnerException.Message);
                        }
                        else
                        {
                            _console.Error(e.Message);
                        }
                    }
                }
            }
        }

        public bool EvaluateNumericExpression(string expression, out double value)
        {
            bool isValid = false;

            try
            {
#if SIBERIA
                var result = _jint_engine.GetValue(_jint_engine.Execute(expression).GetCompletionValue());
                if (result.Type == Types.Number)
                {
                    value = TypeConverter.ToNumber(result);
                    isValid = true;
                }
                else
#endif
                {
                    value = double.NaN;
                }
            }
            catch (JavaScriptException je)
            {
                _console.Error(je.ToString());
                value = double.NaN;
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    _console.Error(e.InnerException.Message);
                }
                else
                {
                    _console.Error(e.Message);
                }
                value = double.NaN;
            }

            return isValid;
        }

        public void ThrowError(string s)
        {
#if SIBERIA
            Jint.Native.Error.ErrorConstructor errorConstructor = new Jint.Native.Error.ErrorConstructor(_jint_engine);
            throw (new JavaScriptException(errorConstructor, s));
#endif
        }
    }
}
