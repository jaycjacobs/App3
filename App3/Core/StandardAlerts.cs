using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Popups;

namespace Cirros.Alerts
{
    public class AlertPlatform
    {
        public async virtual Task<string> AlertYNC(string title, string content, string yes, string no, string cancel = null)
        {
            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(content, title);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(
                yes,
                new UICommandInvokedHandler(this.CommandInvokedHandler),
                "yes"));
            messageDialog.Commands.Add(new UICommand(
                no,
                new UICommandInvokedHandler(this.CommandInvokedHandler),
                "no"));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 1;

            // Show the message dialog
            IUICommand result = await messageDialog.ShowAsync();
            
            //await Task.Delay(1);
            //System.Diagnostics.Debugger.Break();
            return (string)result.Id;
        }

        private void CommandInvokedHandler(IUICommand command)
        {
        }

        public async virtual Task<string> AlertOk(string title, string content, string ok)
        {
            await Task.Delay(1);
            System.Diagnostics.Debugger.Break();
            return "ok";
        }
    }

    public class StandardAlerts
    {
        public static AlertPlatform _alertPlatform = new AlertPlatform();

        public static void SetAlertPlatform(AlertPlatform alertPlatform)
        {
            _alertPlatform = alertPlatform;
        }

        public static async Task<bool> ClearWarningAsync()
        {
            bool okToClear = false;

            try
            {
                if (Globals.ActiveDrawing != null)
                {
                    if (Globals.ActiveDrawing.IsModified)
                    {
                        string name = null;

                        if (!string.IsNullOrEmpty(FileHandling.CurrentDrawingName) && FileHandling.CurrentDrawingName.Length > 5)
                        {
                            name = FileHandling.CurrentDrawingName;
                            int d = name.LastIndexOf(".");
                            if (d > 0)
                            {
                                name = FileHandling.CurrentDrawingName.Substring(0, d);
                            }
                        }

                        var resourceLoader = new ResourceLoader();
                        string title;
                        string yes = resourceLoader.GetString("AlertYes");
                        string no = resourceLoader.GetString("AlertNo");
                        string cancel = resourceLoader.GetString("AlertCancel");
                        string confirm;
                        bool saveAs = false;

                        if (string.IsNullOrEmpty(name))
                        {
                            title = resourceLoader.GetString("AlertUnsavedTitle");
                            confirm = resourceLoader.GetString("AlertConfirmSaveAs");
                            saveAs = true;
                        }
                        else
                        {
                            title = resourceLoader.GetString("AlertModifiedTitle");
                            string format = resourceLoader.GetString("AlertConfirmSaveFormat");
                            confirm = string.Format(format, name);
                        }

                        string result = await _alertPlatform.AlertYNC(title, confirm, yes, no, cancel);

                        if (result == "yes")
                        {
                            if (saveAs)
                            {
                                if (await FileHandling.SaveDrawingAsAsync())
                                {
                                    okToClear = true;
                                }
                            }
                            else
                            {
                                await FileHandling.SaveDrawingAsync();
                                okToClear = true;
                            }
                        }
                        else if (result == "no")
                        {
                            okToClear = true;
                        }
                    }
                    else
                    {
                        okToClear = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "ClearWarningAsync" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 101);
            }

            return okToClear;
        }

        public static async Task<bool> LastChanceToSaveAsync()
        {
            bool save = false;

            try
            {
                bool isModified = false;
                ApplicationDataContainer drawingSettings = null;

                if (ApplicationData.Current.LocalSettings.Containers.Keys.Contains("drawing"))
                {
                    drawingSettings = ApplicationData.Current.LocalSettings.Containers["drawing"];
                    isModified = drawingSettings.Values.Keys.Contains("unsaved") ? (bool)drawingSettings.Values["unsaved"] : false;
                }

                if (isModified == false && Globals.CommandDispatcher != null && Globals.CommandDispatcher.CanUndo)
                {
                    isModified = true;
                }

                if (isModified)
                {
                    var resourceLoader = new ResourceLoader();
                    string title = resourceLoader.GetString("AlertUnsavedTitle");
                    string confirm = resourceLoader.GetString("AlertUnsavedConfirm");
                    string discard = resourceLoader.GetString("AlertDiscard");
                    string show = resourceLoader.GetString("AlertShowMe");

                    string result = await _alertPlatform.AlertYNC(title, confirm, discard, show);

                    if (result == "yes")
                    {
                        if (drawingSettings != null)
                        {
                            drawingSettings.Values["unsaved"] = false;
                        }
                    }
                    else if (result == "no")
                    {
                        save = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "LastChanceToSaveAsync" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 102);
            }

            return save;
        }

        public static async Task<string> InvalidDrawingAsync(string name)
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("AlertInvalidDrawingTitle");
                string problemF = resourceLoader.GetString("AlertInvalidDrawingConfirmFormat");
                string continueString = resourceLoader.GetString("AlertContinue");
                string supportString = resourceLoader.GetString("SendToSupport");

                string problem = string.Format(problemF, name);

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "InvalidDrawingAsync" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 103);
            }

            return "ok";
        }

        public static async Task<string> InvalidSvgFile(string errorMessage)
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("AlertInvalidSvgTitle");
                string problemF = resourceLoader.GetString("AlertInvalidSvgFormat");
                string continueString = resourceLoader.GetString("AlertContinue");

                string problem = string.Format(problemF, errorMessage);
                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "InvalidSvgFile" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 104);
            }

            return "ok";
        }

        public static async Task<string> MissingFontsAsync(List<string> fonts)
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("AlertMissingFontsTitle");
                string problemF = resourceLoader.GetString("AlertMissingFontsFormat");
                string continueString = resourceLoader.GetString("AlertContinue");

                StringBuilder sb = new StringBuilder();
                foreach (string font in fonts)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(font);
                }

                string problem = string.Format(problemF, sb.ToString());

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "MissingFontsAsync" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 105);
            }

            return "ok";
        }

        public static async Task UnsupportedDwgVersion()
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("AlertInvalidDwgVersionTitle");
                string problem = resourceLoader.GetString("AlertInvalidDwgVersion");
                string continueString = resourceLoader.GetString("AlertContinue");

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "InvalidDwgVersion" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 106);
            }
        }

        public static async Task DwgImportFailed()
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("AlertDwgImportFailedTitle");
                string problem = resourceLoader.GetString("AlertDwgImportFailed");
                string continueString = resourceLoader.GetString("AlertContinue");

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "DwgImportFailed" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 107);
            }
        }

        public static async Task ImageErrorAlertAsync()
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("AlertSystemError");
                string problem = resourceLoader.GetString("AlertSystemImageError");
                string continueString = resourceLoader.GetString("AlertContinue");

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "ImageErrorAlertAsync" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 108);
            }
        }

        public static async Task SimpleAlertAsync(string title, string problem)
        {
            try
            {
                string continueString = "Continue";

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "SimpleAlertAsync" },
                        { "title", title },
                        { "problem", problem },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 109);
            }
        }

        public static async Task<string> SimpleAlertAsyncWithCancel(string title, string problem)
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string continueString = resourceLoader.GetString("AlertContinue");
                string cancelString = resourceLoader.GetString("AlertCancel");

                await _alertPlatform.AlertYNC(title, problem, continueString, cancelString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "SimpleAlertAsyncWithCancel" },
                        { "title", title },
                        { "problem", problem },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 110);
            }

            return "ok";
        }

        public static async Task<string> LoadFailedAsync(string param)
        {
            try
            {
                string problemKey = param == "restore_error" ? "AlertRestoreErrorConfirm" : "AlertInvalidDrawingConfirm";
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("AlertInvalidDrawingTitle");
                string problem = resourceLoader.GetString(problemKey);
                string continueString = resourceLoader.GetString("AlertContinue");
                string supportString = resourceLoader.GetString("SendToSupport");

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "LoadFailedAsync" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 111);
            }

            Analytics.ReportError(string.Format("LoadFailed ({0})", param), null, 1, 112);

            return "ok";
        }

        public static async Task UplevelDrawing()
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("AlertUplevelDrawingTitle");
                string problem = resourceLoader.GetString("AlertUplevelConfirm");
                string continueString = resourceLoader.GetString("AlertContinue");

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "UplevelDrawing" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 113);
            }
        }

        public static async Task DxfIOError()
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("DxfIOErrorTitle");
                string problem = resourceLoader.GetString("DxfIOErrorConfirm");
                string continueString = resourceLoader.GetString("AlertContinue");

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "DxfIOError" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 114);
            }
        }

        public static async Task IOError()
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("IOErrorTitle");
                string problem = resourceLoader.GetString("IOErrorConfirm");
                string continueString = resourceLoader.GetString("AlertContinue");

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "IOError" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 115);
            }
        }

        public static async Task SaveError()
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("SaveErrorTitle");
                string problem = resourceLoader.GetString("SaveErrorConfirm");
                string continueString = resourceLoader.GetString("AlertContinue");

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "SaveError" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 116);
            }
        }

        public static async Task DxfFormatError()
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("DxfFormatErrorTitle");
                string problem = resourceLoader.GetString("DxfErrorConfirm");
                string continueString = resourceLoader.GetString("AlertContinue");

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "DxfFormatError" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 117);
            }
        }

        public static async Task DxfContentError()
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("DxfContentErrorTitle");
                string problem = resourceLoader.GetString("DxfErrorConfirm");
                string continueString = resourceLoader.GetString("AlertContinue");

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "DxfContentError" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 118);
            }
        }

        public static async Task UplevelSymbol()
        {
            try
            {
                var resourceLoader = new ResourceLoader();
                string title = resourceLoader.GetString("AlertUplevelSymbolTitle");
                string problem = resourceLoader.GetString("AlertUplevelConfirm");
                string continueString = resourceLoader.GetString("AlertContinue");

                await _alertPlatform.AlertOk(title, problem, continueString);
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "UplevelSymbol" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 119);
            }
        }

        public static async Task PdfError()
        {
            var resourceLoader = new ResourceLoader();

            string continueString = resourceLoader.GetString("AlertContinue");
            string title = resourceLoader.GetString("PdfErrorTitle");
            string message = resourceLoader.GetString("PdfErrorMessage");

            var messageDialog = new MessageDialog(message, title);

            messageDialog.Commands.Add(new UICommand(continueString, null, "continue"));

            messageDialog.DefaultCommandIndex = 0;  // Default command index
            messageDialog.CancelCommandIndex = 0;   // Cancel index

            UICommand command = (UICommand)await messageDialog.ShowAsync();
        }
    }
}
