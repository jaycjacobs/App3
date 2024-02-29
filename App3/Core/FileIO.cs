//#define VERBOSE_SAVE_FILE_PICKER
using App3;
using Cirros.Core.Primitives;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using CirrosCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Search;

namespace Cirros
{
    public class FileHandling
    {
        private const string sessionStateFilename = "___sessionState.xml";
        private const string temporaryFilename = "___temporaryFile.xml";
        private static string __currentDrawingToken = "";
        private static string _currentDrawingName = "";
        private static string _currentDrawingPath = "";

        public static StorageFile _failedDrawingFile = null;

        const int _cCurrentDrawingVersion = 7;
        const int _cMaximumSupportedDrawingVersion = 7;
        const int _cCurrentSymbolVersion = 6;
        const int _cMaximumSupportedSymbolVersion = 6;

        public static async Task<StorageFile> GetCurrent()
        {
            StorageFile file = null;

            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync(sessionStateFilename);
            }
            catch
            {
                // fail silently
            }

            return file;
        }


        public static string CurrentDrawingName
        {
            get
            {
                if (_currentDrawingName == sessionStateFilename)
                {
                    return "";
                }
                if (_currentDrawingName == temporaryFilename)
                {
                    return "";
                }
                return _currentDrawingName;
            }
        }

        public static string CurrentDrawingPath
        {
            get
            {
                return _currentDrawingPath;
            }
        }

        private static string _currentDrawingToken
        {
            get
            {
                return __currentDrawingToken;
            }
        }

        private static void SetCurrentDrawing(string token, string name, string path)
        {
            if (name == null || !name.StartsWith("__"))
            {
                __currentDrawingToken = token;
                _currentDrawingName = name;
                _currentDrawingPath = path;

                Globals.Events.DrawingNameChanged(name);
            }
        }

        public static bool DrawingFileIsAvailable
        {
            get
            {
                return !string.IsNullOrEmpty(_currentDrawingToken);
            }
            set
            {
                if (value == false)
                {
                    SetCurrentDrawing(null, null, null);
                }
            }
        }

        public static async Task SaveDrawingAsync()
        {
            int trace = 0;

            if (DrawingFileIsAvailable)
            {
                bool retryWithPicker = false;
                trace = 1;

                try
                {
                    StorageFile mrufile = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(_currentDrawingToken);
                    if (mrufile != null)
                    {
#if true
                        await SaveDrawingAsAsync(mrufile);
#else
                        trace = 2;
                        // Create a temporary file to protect the target file in case of errors
                        StorageFile tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(temporaryFilename, CreationCollisionOption.ReplaceExisting);
                        await SaveDrawingAsAsync(tempFile);
                        trace = 3;

                        // If no failures, replace the target file with the temporary file
                        await tempFile.MoveAndReplaceAsync(mrufile);
#endif
                        trace = 4;

                        await UpdateMruTimestampAndThumbnailAsync(_currentDrawingToken, mrufile, true);
                        trace = 5;

                        Globals.ActiveDrawing.IsModified = false;
                        trace = 6;

                        var eventValues = new Dictionary<string, double> {
                            { "layers", Globals.LayerTable.Count },
                            { "styles", Globals.TextStyleTable.Count },
                            { "ltypes", Globals.LineTypeTable.Count }
                        };
                        Analytics.ReportEvent("save_drawing", null, eventValues);
                    }
                    else
                    {
                        Analytics.ReportEvent("SaveDrawingAsync failed to get MRU file");
                    }
                }
                catch (Exception ex)
                {
                    await Cirros.Alerts.StandardAlerts.SaveError();
                    string message = string.Format("SaveDrawingAsync failed [{0}]", trace);
                    Analytics.ReportError(message, ex, 1, 300);

                    retryWithPicker = trace <= 4;
                }

                if (retryWithPicker)
                {
                    Analytics.ReportEvent("save_retry");
                    await SaveDrawingAsAsync();
                }
            }
            else
            {
                Analytics.ReportEvent("missing MRU token in SaveDrawingAsync");
            }
        }

        static string SuggestName(string basename, int index = 1)
        {
            string name = "";
            string extension = "";

            int dot = basename.LastIndexOf('.');

            if (dot > 0)
            {
                extension = basename.Substring(dot);
            }
            else if (dot == 0)
            {
                extension = basename;
            }
            else
            {
                extension = "";
            }

            int i = basename.IndexOfAny(new char[] { ':', '|' });
            if (i > 0)
            {
                name = basename.Substring(0, i);
            }
            else if (i == 0 || dot == 0)
            {
                name = "";
            }
            else if (dot > 0)
            {
                name = basename.Substring(0, dot);
            }
            else
            {
                name = basename;
            }

            if (string.IsNullOrEmpty(name))
            {
                if (extension == ".dbsx")
                {
                    name = "Symbol";
                }
                else if (extension == ".dbtx")
                {
                    name = "Drawing Template";
                }
                else if (extension == ".dbfx")
                {
                    name = "Drawing";
                }
                else
                {
                    name = "File";
                }

                name = string.Format("{0} {1}{2}", name, index, extension);
            }

            return name;
        }

#if VERBOSE_SAVE_FILE_PICKER
        public static async Task<bool> SaveDrawingAsAsync()
        {
            bool saved = false;

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("time", DateTime.Now.ToString());
            data.Add("status", "entry");
            try
            {
                FileSavePicker savePicker = new FileSavePicker();
                data["status"] = "set start location";
                data.Add("SuggestedStartLocation0-default", savePicker.SuggestedStartLocation.ToString());
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                data.Add("SuggestedStartLocation", savePicker.SuggestedStartLocation.ToString());

                // Dropdown of file types the user can save the file as
                data["status"] = "set file type";
                savePicker.FileTypeChoices.Add("Drawing", new List<string>() { ".dbfx" });

                // Default file name if the user does not type one in or select a file to replace
                data["status"] = "suggest name";
                savePicker.SuggestedFileName = FileHandling.SuggestName(".dbfx");
                data.Add("suggestedname", savePicker.SuggestedFileName);

                data["status"] = "call PickSaveFileAsync";
                StorageFile file = await savePicker.PickSaveFileAsync();
                data["status"] = "return from PickSaveFileAsync";
                if (file != null)
                {
                    data.Add("filename", file.Name);
                    try
                    {
                        data["status"] = "call saveDrawingAsync";
                        await saveDrawingAsync(file);
                        data["status"] = "return from saveDrawingAsync";

                        Globals.ActiveDrawing.IsModified = false;
                        saved = true;

                        var eventValues = new Dictionary<string, double> {
                            { "layers", Globals.LayerTable.Count },
                            { "styles", Globals.TextStyleTable.Count },
                            { "ltypes", Globals.LineTypeTable.Count }
                        };
                        Analytics.ReportEvent("save_drawing_as", null, eventValues);
                    }
                    catch (Exception ex)
                    {
                        data.Add("exception1", ex.Message);
                        Analytics.ReportError("SaveDrawingAsAsync_failed", ex, 1, 301);
                        await StandardAlerts.SaveError();
                    }
                }
                else
                {
                    // cancelled
                    data["status"] = "cancelled";
                }
            }
            catch (Exception ex2)
            {
                data["status"] = "exception 2";
                data.Add("exception2", ex2.Message);
            }
            finally
            {
                Analytics.ReportEvent("SaveDrawingAsAsync", data);
            }

            return saved;
        }
#else
        public static async Task<bool> SaveDrawingAsAsync()
        {
            bool saved = false;
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/

            FileSavePicker savePicker = InitializeWithWindow(new FileSavePicker(),App.WindowHandle);
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Drawing", new List<string>() { ".dbfx" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = FileHandling.SuggestName(CurrentDrawingName == null ? ".dbfx" : CurrentDrawingName);

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    await SaveDrawingAsAsync(file);

                    Globals.ActiveDrawing.IsModified = false;
                    saved = true;

                    var eventValues = new Dictionary<string, double> {
                        { "layers", Globals.LayerTable.Count },
                        { "styles", Globals.TextStyleTable.Count },
                        { "ltypes", Globals.LineTypeTable.Count }
                    };
                    Analytics.ReportEvent("save_drawing_as", null, eventValues);
                }
                catch (Exception ex)
                {
                    Analytics.ReportError("SaveDrawingAsAsync_failed", ex, 1, 302);
                    //await StandardAlerts.SaveError();
                }
            }
            else
            {
                // cancelled
            }

            return saved;
        }

        private static FileSavePicker InitializeWithWindow(FileSavePicker obj, IntPtr windowHandle)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
            return obj;
        }
#endif
        public static async Task<bool> SaveDrawingAsTemplateAsync()
        {
            bool saved = false;
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/

            FileSavePicker savePicker = InitializeWithWindow(new FileSavePicker(),App.WindowHandle);
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Drawing template", new List<string>() { ".dbtx" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = FileHandling.SuggestName(".dbtx");

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await SaveDrawingAsAsync(file);

                Globals.ActiveDrawing.IsModified = false;
                saved = true;

                Analytics.ReportEvent("save_template");
            }
            else
            {
                // cancelled
            }

            return saved;
        }

        public static async Task<bool> SaveStateAsync()
        {
            int trace = 0;
            string temporarySessionStateFilename = "_" + sessionStateFilename;

            try
            {
                StorageFile tempFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(temporarySessionStateFilename, CreationCollisionOption.ReplaceExisting);
                trace = 1;

                if (tempFile != null && Globals.ActiveDrawing != null)
                {
                    await SaveDrawingAsAsync(tempFile, true);
                    trace = 2;

                    StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(temporarySessionStateFilename);
                    trace = 3;
                    await file.RenameAsync(sessionStateFilename, NameCollisionOption.ReplaceExisting);
                    trace = 4;

                    // Create drawing thumbnail
                    await CreateThumbnail(sessionStateFilename);
                    trace = 5;

                    if (ApplicationData.Current.LocalSettings.Containers.Keys.Contains("drawing"))
                    {
                        ApplicationDataContainer drawingSettings = ApplicationData.Current.LocalSettings.Containers["drawing"];
                        trace = 6;
                        drawingSettings.Values["token"] = _currentDrawingToken;
                        drawingSettings.Values["name"] = _currentDrawingName;
                        drawingSettings.Values["unsaved"] = Globals.ActiveDrawing.IsModified;
                        trace = 7;
                    }
                }
            }
            catch (Exception ex)
            {
                //  Trace >= 4 means state was saved succesfully
                string message = string.Format("SaveStateAsync failed [{0}]", trace);

                if (trace < 2)
                {
                    Analytics.ReportError(message, ex, 2, 303);
                }
            }

            return trace >= 2;
        }

        public static async Task<bool> RestoreStateAsync()
        {
            bool success = false;

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(sessionStateFilename);
                if (file != null)
                {
                    if (await LoadDrawingAsync(file))
                    {
                        success = true;

                        if (ApplicationData.Current.LocalSettings.Containers.Keys.Contains("drawing"))
                        {
                            ApplicationDataContainer drawingSettings = ApplicationData.Current.LocalSettings.Containers["drawing"];
                            string token = (string) drawingSettings.Values["token"];
                            string name = (string) drawingSettings.Values["name"];
                            string path = "";
                            try
                            {
                                StorageFile mrufile = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(token);
                                if (mrufile != null)
                                {
                                    int b = mrufile.Path.LastIndexOf('\\');
                                    path = b > 0 ? mrufile.Path.Substring(0, b) : mrufile.Path;
                                }
                            }
                            catch { }

                            SetCurrentDrawing(token, name, path);

                            if (drawingSettings.Values.ContainsKey("unsaved"))
                            {
                                Globals.ActiveDrawing.IsModified = (bool)drawingSettings.Values["unsaved"];
                            }
                            else
                            {
                                Globals.ActiveDrawing.IsModified = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (success)
                {
                    Analytics.ReportError("RestoreStateAsync failed to retrieve settings", ex, 2, 304);
                }
                else
                {
                    Analytics.ReportError("RestoreStateAsync failed", ex, 2, 305);
                }
            }

            return success;
        }

        private static Object thisLock = new Object();

        public static async Task SaveDrawingAsAsync(StorageFile file, bool isTemporary = false)
        {
            //bool isSessionState = file.Name.StartsWith("____");
            bool isSessionState = false; // set to true to prevent images from being autosaved

            if (Globals.ActiveDrawing == null)
            {
                throw new Exception("Can't save because there is not a vaild ActiveDrawing");
            }

            if (Globals.ActiveDrawing.PaperSize.Width == 0 || Globals.ActiveDrawing.PaperSize.Height == 0 || Globals.LayerTable.Count == 0)
            {
                Analytics.ReportEvent("invalid_drawing", new Dictionary<string, string> {
                    { "file", file.DisplayName },
                    { "width", Globals.ActiveDrawing.PaperSize.Width.ToString() },
                    { "height", Globals.ActiveDrawing.PaperSize.Height.ToString() },
                    { "layers", Globals.LayerTable.Count.ToString() },
                });
                throw new Exception("Can't save because the drawing is not valid");
            }

            /*
             * watch: not sure exactly why this was added originally but it is odd to have a command terminate at this point
             * 
            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.Finish();
            }
             * */

            DrawingFile drawing = new DrawingFile();

            drawing.Header.Version = _cCurrentDrawingVersion;

            Dictionary<string, int> instanceMap = Globals.ActiveDrawing.GroupInstanceMap();

            if (Globals.ActiveDrawing != null)
            {
                //lock (thisLock)
                {
                    List<string> patternNames = new List<string>();
                    List<ImageRef> imageRefs = new List<ImageRef>();

                    foreach (string key in Globals.ActiveDrawing.Groups.Keys)
                    {
                        //System.Diagnostics.Debug.WriteLine("key = {0}", key as object);
                        Group g = Globals.ActiveDrawing.Groups[key];
                        if (instanceMap.ContainsKey(key))
                        {
                            if (g.IncludeInLibrary == false && instanceMap[key] < 1)
                            {
                                continue;
                            }
                        }
                        else if (g.IncludeInLibrary == false)
                        {
                            // Don't save any anonymous groups with no instances unless they are in the symbol library
                            continue;
                        }

                        if (isSessionState == false)
                        {
                            await Utilities.AddImageRefsFromGroup(g, imageRefs);
                        }

                        foreach (string patternName in g.CrosshatchPatterns)
                        {
                            if (patternNames.Contains(patternName) == false)
                            {
                                patternNames.Add(patternName);
                            }
                        }

                        drawing.AddGroup(g);
                    }

                    foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
                    {
                        if (isSessionState == false && p is PImage)
                        {
                            await Utilities.AddImageRefFromImage(p as PImage, imageRefs);
                        }

                        if (p.FillPattern != null && p.FillPattern != "Solid")
                        {
                            Patterns.AddPatternFromEntityToList(p, patternNames);
                        }

                        drawing.AddEntity(p.Serialize());
                    }

                    drawing.AddPatterns(patternNames);
                    drawing.AddImageRefs(imageRefs);
                }
            }

            bool deferred = false;

            try
            {
                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                deferred = true;
            }
            catch
            {
                // this must be dropbox
            }

            using (Stream fileStream = await file.OpenStreamForWriteAsync())
            {
                fileStream.SetLength(0);

                StreamWriter writer = new StreamWriter(fileStream);

                XmlSerializer serializer = new XmlSerializer(typeof(DrawingFile));
                serializer.Serialize(writer, drawing);
                writer.Flush();
            }

            bool success = true;
            if (deferred)
            {
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                success = status == FileUpdateStatus.Complete;
            }

            if (success)
            {
                // success
                //if (file.Name.StartsWith("___"))
                if (isTemporary)
                {
                    // Don't add temporary drawings to the MRU list
                }
                else
                {
                    string token = AddFileToMru(file);
                    if (token != null)
                    {
                        int b = file.Path.LastIndexOf('\\');
                        string path = b > 0 ? file.Path.Substring(0, b) : file.Path;
                        SetCurrentDrawing(token, file.Name, file.Path);

                        // Create drawing thumbnail
                        await CreateThumbnail(token);
                    }
                }
            }
            else
            {
                // error
                await Cirros.Alerts.StandardAlerts.SaveError();
                throw new Exception("save failed");
            }
        }
#if SIBERIA
        private static CloudBlobClient _cloudBlobClient = null;
        static HttpClient _httpClient = null;
#endif

#if DEBUG
        const string _convertUrl = "http://localhost:16935/api/convert";
#else
        const string _convertUrl = "http://cirrosapi20240223153414.azurewebsites.net/api/convert";
#endif

        internal static async Task<int> ImportDwgDocumentAsync()
        {
            int status = 0;
#if SIBERIA

            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }

            StorageFile file = null;

            if (await Cirros.Alerts.StandardAlerts.ClearWarningAsync())
            {
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
                FileOpenPicker openPicker = InitializeWithWindow(new FileOpenPicker(),App.WindowHandle);
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".dwg");
                openPicker.FileTypeFilter.Add(".svg");

                file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    IndeterminateProgressDialog dialog = new IndeterminateProgressDialog($"Importing '{file.Name}'");

                    try
                    {
#if true
                        if (_cloudBlobClient == null)
                        {
                            // Retrieve the connection string for use with the application. The storage 
                            // connection string is stored in an environment variable on the machine 
                            // running the application called AZURE_STORAGE_CONNECTION_STRING. If the 
                            // environment variable is created after the application is launched in a 
                            // console or with Visual Studio, the shell or application needs to be closed
                            // and reloaded to take the environment variable into account.
                            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=cirros1;AccountKey=keGsh5nxtjxjsxjlEKTS7RhHO0S/VM8Zkc/F0pjAH6M94dsBT2lt7UVxMTkTh7sXf3q/vU9qsTeh7nJua1BH5g==;EndpointSuffix=core.windows.net";

                            // Check whether the connection string can be parsed.
                            CloudStorageAccount storageAccount;
                            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
                            {
                                // Create the CloudBlobClient that represents the 
                                // Blob storage endpoint for the storage account.
                                _cloudBlobClient = storageAccount.CreateCloudBlobClient();
                            }
                            else
                            {
                                // no go
                                throw new Exception("No Blob Client");
                            }
                        }
                        dialog.Show();

                        CloudBlobContainer cloudBlobContainer = _cloudBlobClient.GetContainerReference("dwg");

                        await cloudBlobContainer.CreateIfNotExistsAsync();

                        // Get a reference to the blob address, then upload the file to the blob.
                        // Use the value of localFileName for the blob name.
                        string blobName = Guid.NewGuid().ToString();

                        CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference(blobName);

                        Package package = Package.Current;
                        PackageId packageId = package.Id;
                        string version = string.Format("{0}.{1}.{2}.{3}", packageId.Version.Major, packageId.Version.Minor, packageId.Version.Build, packageId.Version.Revision);
                        blob.Metadata.Add("version", version);
                        blob.Metadata.Add("name", file.DisplayName);

                        await blob.UploadFromFileAsync(file);

                        string url = string.Format("{0}?m={1}", _convertUrl, blobName);
                        var content = await _httpClient.GetStringAsync(url);

                        if (content.Contains("exception"))
                        {
                            dialog.Hide();

                            var doc = new XmlDocument();
                            doc.LoadXml(content);
                            var tags = doc.GetElementsByTagName("exception");
                            if (tags.Count > 0)
                            {
                                if (tags[0].InnerText.StartsWith("The DWG file must be compatible with R15"))
                                {
                                    await StandardAlerts.UnsupportedDwgVersion();
                                    Analytics.ReportEvent("import-dwg", new Dictionary<string, string> { { "status", "unsupported" } });
                                }
                                else
                                {
                                    await StandardAlerts.DwgImportFailed();
                                    Analytics.ReportEvent("import-dwg", new Dictionary<string, string> { { "status", "failed" } });
                                }
                            }
                        }
                        else if (await FileHandling.DeserializeDrawingFromXML(content))
                        {
                            status = 1;
                            Globals.Events.DrawingLoaded(null);
                            Analytics.ReportEvent("import-dwg", new Dictionary<string, string> { { "status", "succeeded" } });
                        }
#else
                        using (var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString()))
                        {
                            Stream stream = await file.OpenStreamForReadAsync();
                            content.Add(new StreamContent(stream), "file", file.Name);
                            using (var message = await _httpClient.PostAsync(_convertUrl, content))
                            {
                                string s = await message.Content.ReadAsStringAsync();
                                if (await FileHandling.DeserializeDrawingFromXML(s))
                                {
                                    status = 1;
                                    Globals.Events.DrawingLoaded(null);
                                }
                            }
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        status = -1;
                    }
                    finally
                    {
                        if (dialog.IsLoaded)
                        {
                            dialog.Hide();
                        }
                    }
                }
            }
#endif
            return status;
        }

        private static FileOpenPicker InitializeWithWindow(FileOpenPicker obj, IntPtr windowHandle)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
            return obj;
        }

        public static async Task<bool> LoadMruDrawingAsync(string token)
        {
            bool succeeded = false;
            try
            {
                StorageFile mrufile = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(token);
                if (mrufile != null)
                {
                    if (await FileHandling.LoadDrawingAsync(mrufile))
                    {
                        if (mrufile.Attributes.HasFlag(Windows.Storage.FileAttributes.Temporary) == false)
                        {
                            await UpdateMruTimestampAndThumbnailAsync(token, mrufile, false);

                            int b = mrufile.Path.LastIndexOf('\\');
                            string path = b > 0 ? mrufile.Path.Substring(0, b) : mrufile.Path;
                            SetCurrentDrawing(token, mrufile.Name, path);
                        }
                        succeeded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError("Error in LoadMruDrawingAsync", ex, 2, 306);
            }

            return succeeded;
        }

#if false
        public static async Task<StorageFile> PickDrawingOrTemplate()
        {
            StorageFile file = null;

            if (await Alerts.StandardAlerts.ClearWarningAsync())
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".dbfx");
                openPicker.FileTypeFilter.Add(".dbtx");

                file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    if (file.FileType == ".dbfx")
                    {
                        FileHandling.AddFileToMru(file);
                    }
                }
            }

            return file;
        }
#endif
        public static async Task<int> LoadDrawingAsync()
        {
            int result = 0; // 1:success, 0:cancelled, -1:failed to load

            if (await Alerts.StandardAlerts.ClearWarningAsync())
            {
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
                FileOpenPicker openPicker = InitializeWithWindow(new FileOpenPicker(),App.WindowHandle);
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".dbfx");
                openPicker.FileTypeFilter.Add(".dbtx");

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    string filetype = file.FileType.ToLower();

                    if (filetype == ".dbfx")
                    {
                        string token = FileHandling.AddFileToMru(file);
                        if (token != null)
                        {
                            result = await LoadMruDrawingAsync(token) ? 1 : -1;

                            if (result == -1)
                            {
                                await Alerts.StandardAlerts.InvalidDrawingAsync(file.Name);
                                await FileHandling.RemoveFileFromMruAsync(token);
                            }
                        }
                    }
                    else if (filetype == ".dbtx")
                    {
                        // Template file - load like a drawing but don't remember name or add to MRU
                        result = await LoadDrawingAsync(file) ? 1 : -1;
                    }
                }
                else
                {
                    result = 0; // No file returned from picker, assume cancelled
                }
            }
            else
            {
                result = 0; // Cancelled from clear warning
            }

            return result;
        }

        public static async Task<int> ImportDrawingAsync()
        {
            int result = 0; // 1:success, 0:cancelled, -1:failed to load

            if (await Alerts.StandardAlerts.ClearWarningAsync())
            {
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
                FileOpenPicker openPicker = InitializeWithWindow(new FileOpenPicker(),App.WindowHandle);
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".svg");
                openPicker.FileTypeFilter.Add(".dxf");

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    string filetype = file.FileType.ToLower();

                    if (filetype == ".svg")
                    {
                        Analytics.ReportEvent("import-svg");
                        result = await ImportSvgAsync(file);
                    }
                    else if (filetype == ".dxf")
                    {
                        Analytics.ReportEvent("import-dxf");
                        result = await ImportDxfAsync(file);
                    }
                }
                else
                {
                    result = 0; // No file returned from picker, assume cancelled
                }
            }
            else
            {
                result = 0; // Cancelled from clear warning
            }

            return result;
        }

        public static async Task<int> LoadOrImportDrawingAsync()
        {
            int result = 0; // 1:success, 0:cancelled, -1:failed to load

            if (await Alerts.StandardAlerts.ClearWarningAsync())
            {
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
                FileOpenPicker openPicker = InitializeWithWindow(new FileOpenPicker(),App.WindowHandle);
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".dbfx");
                openPicker.FileTypeFilter.Add(".dbtx");
                openPicker.FileTypeFilter.Add(".svg");
                openPicker.FileTypeFilter.Add(".dxf");

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    string filetype = file.FileType.ToLower();

                    if (filetype == ".dbfx")
                    {
                        string token = FileHandling.AddFileToMru(file);
                        if (token != null)
                        {
                            result = await LoadMruDrawingAsync(token) ? 1 : -1;

                            if (result == -1)
                            {
                                //await Alerts.StandardAlerts.InvalidDrawingAsync(file.Name);
                                await FileHandling.RemoveFileFromMruAsync(token);
                            }
                        }
                    }
                    else if (filetype == ".dbtx")
                    {
                        // Template file - load like a drawing but don't remember name or add to MRU
                        result = await LoadDrawingAsync(file) ? 1 : -1;
                    }
                    else if (filetype == ".svg")
                    {
                        Analytics.ReportEvent("load-svg");
                        result = await ImportSvgAsync(file);
                    }
                    else if (filetype == ".dxf")
                    {
                        Analytics.ReportEvent("load-dxf");
                        result = await ImportDxfAsync(file);
                    }
                }
                else
                {
                    result = 0; // No file returned from picker, assume cancelled
                }
            }
            else
            {
                result = 0; // Cancelled from clear warning
            }

            return result;
        }

        public static async Task<bool> DeserializeDrawingFromXML(string xml)
        {
            bool succeeded = false;
            bool uplevel = false;

            if (Globals.ActiveDrawing == null)
            {
                throw new Exception("Can't open drawing because there is not a vaild ActiveDrawing");
            }

            bool _gridVisible = Globals.GridIsVisible;
            bool _rulersVisible = Globals.ShowRulers;

            Globals.GridIsVisible = false;
            Globals.ShowRulers = false;

            Globals.ActiveDrawing.Clear();

            int objectIndex = 0;

            try
            {
                using (StringReader stringReader = new StringReader(xml))
                {
                    //BasicProperties props = await file.GetBasicPropertiesAsync();
                    //Globals.Events.DrawingLoading(file.Name, props.Size);

                    //System.IO.StreamReader reader = new System.IO.StreamReader(fileStream);

                    XmlSerializer serializer = new XmlSerializer(typeof(DrawingFile));
                    DrawingFile drawing = serializer.Deserialize(stringReader) as DrawingFile;

                    // After reading the header, we have the drawing size and grid values
                    // so we can show the correctly sized work area - this prevents an awkward flash when loading complex drawings
                    Globals.View.DisplayAll();

                    //DateTime start = DateTime.Now;

                    uplevel = drawing.Header.Version > _cMaximumSupportedDrawingVersion;

                    foreach (ImageRef imageRef in drawing.Images)
                    {
                        StorageFile imageFile = imageFile = await Utilities.GetImageSourceFileAsync(imageRef.ImageId);
                        if (imageFile == null)
                        {
                            // if the image doesn't exist in the temporary folder, create it from the base64 stream
                            await Utilities.CreateTemporaryImageFromUriAsync(imageRef.ImageId, Globals.TemporaryImageFolder, imageRef.Contents);
                        }
                    }

                    foreach (Group g in drawing.Groups)
                    {
                        if (drawing.Header.Version < 2)
                        {
                            //g.IncludeInLibrary = g.Name.StartsWith(":") == false && g.Name.Contains("|") == false;
                        }
                        if (drawing.Header.Version < 3)
                        {
                            if (g.Exit.X != 0 || g.Exit.Y != 0)
                            {
                                // Prior to symbol version 3 the exit point was in paper units
                                if (g.ModelScale > 0)
                                {
                                    double scale = 1 / g.ModelScale;
                                    if (g.ModelUnit == Unit.Feet)
                                    {
                                        scale *= 12;
                                    }
                                    else if (g.ModelUnit == Unit.Millimeters)
                                    {
                                        scale /= 25.4;
                                    }
                                    else if (g.ModelUnit == Unit.Centimeters)
                                    {
                                        scale /= 2.54;
                                    }
                                    else if (g.ModelUnit == Unit.Meters)
                                    {
                                        scale /= 0.0254;
                                    }
                                    g.Exit = new Point(g.Exit.X / scale, -g.Exit.Y / scale);
                                }
                            }
                        }
                        Globals.ActiveDrawing.AddGroup(g);
                    }

                    Globals.ActiveDrawing.AttributeListsChanged();
                    Globals.DrawingCanvas.VectorListControl.FixupLevel = 0;

                    foreach (Entity e in drawing.Entities)
                    {
                        objectIndex++;
                        Primitive p = Primitive.DeserializeFromEntity(e, Globals.ActiveDrawing);
                        if (p != null)
                        {
                            p.AddToContainer(Globals.ActiveDrawing);
                        }
                    }

                    Globals.DrawingCanvas.VectorListControl.FixupLevel = 2;

                    //TimeSpan elapsed = DateTime.Now - start;
                    //System.Diagnostics.Debug.WriteLine("Load time: {0}", elapsed.TotalSeconds);
                }
                succeeded = true;

                _failedDrawingFile = null;

                //Analytics.ReportEvent("load", new Dictionary<string, string> { { "format", file.FileType } });

                //Globals.Events.DrawingLoaded(file.Name);
                Globals.Events.GridChanged();
            }
            catch (Exception ex)
            {
                // fail
                //_failedDrawingFile = file;
                Analytics.ReportError("LoadDrawingAsync failed", ex, 1, 307);
            }

            if (uplevel)
            {
                await Alerts.StandardAlerts.UplevelDrawing();
            }

            Globals.GridIsVisible = _gridVisible;
            Globals.ShowRulers = _rulersVisible;
            Globals.Events.ShowRulers(Globals.ShowRulers);

            return succeeded;
        }

        public static async Task<bool> LoadDrawingAsync(StorageFile file)
        {
            bool succeeded = false;
            bool uplevel = false;

            if (Globals.ActiveDrawing == null)
            {
                throw new Exception("Can't open drawing because there is not a vaild ActiveDrawing");
            }

            bool _gridVisible = Globals.GridIsVisible;
            bool _rulersVisible = Globals.ShowRulers;

            Globals.GridIsVisible = false;
            Globals.ShowRulers = false;

            Globals.ActiveDrawing.Clear();

            int objectIndex = 0;

            try
            {
                using (Stream fileStream = await file.OpenStreamForReadAsync())
                {
                    BasicProperties props = await file.GetBasicPropertiesAsync();
                    Globals.Events.DrawingLoading(file.Name, props.Size);

                    System.IO.StreamReader reader = new System.IO.StreamReader(fileStream);

                    XmlSerializer serializer = new XmlSerializer(typeof(DrawingFile));
                    DrawingFile drawing = serializer.Deserialize(fileStream) as DrawingFile;

                    // After reading the header, we have the drawing size and grid values
                    // so we can show the correctly sized work area - this prevents an awkward flash when loading complex drawings
                    Globals.View.DisplayAll();

                    //DateTime start = DateTime.Now;

                    uplevel = drawing.Header.Version > _cMaximumSupportedDrawingVersion;

                    foreach (ImageRef imageRef in drawing.Images)
                    {
                        StorageFile imageFile = imageFile = await Utilities.GetImageSourceFileAsync(imageRef.ImageId);
                        if (imageFile == null)
                        {
                            // if the image doesn't exist in the temporary folder, create it from the base64 stream
                            await Utilities.CreateTemporaryImageFromUriAsync(imageRef.ImageId, Globals.TemporaryImageFolder, imageRef.Contents);
                        }
                    }

                    foreach (Group g in drawing.Groups)
                    {
                        if (drawing.Header.Version < 2)
                        {
                            //g.IncludeInLibrary = g.Name.StartsWith(":") == false && g.Name.Contains("|") == false;
                        }
                        if (drawing.Header.Version < 3)
                        {
                            if (g.Exit.X != 0 || g.Exit.Y != 0)
                            {
                                // Prior to symbol version 3 the exit point was in paper units
                                if (g.ModelScale > 0)
                                {
                                    double scale = 1 / g.ModelScale;
                                    if (g.ModelUnit == Unit.Feet)
                                    {
                                        scale *= 12;
                                    }
                                    else if (g.ModelUnit == Unit.Millimeters)
                                    {
                                        scale /= 25.4;
                                    }
                                    else if (g.ModelUnit == Unit.Centimeters)
                                    {
                                        scale /= 2.54;
                                    }
                                    else if (g.ModelUnit == Unit.Meters)
                                    {
                                        scale /= 0.0254;
                                    }
                                    g.Exit = new Point(g.Exit.X / scale, -g.Exit.Y / scale);
                                }
                            }
                        }
                        Globals.ActiveDrawing.AddGroup(g);
                    }

                    Globals.ActiveDrawing.AttributeListsChanged();
                    Globals.DrawingCanvas.VectorListControl.FixupLevel = 0;

                    foreach (Entity e in drawing.Entities)
                    {
                        objectIndex++;
                        Primitive p = Primitive.DeserializeFromEntity(e, Globals.ActiveDrawing);
                        if (p != null)
                        {
                            p.AddToContainer(Globals.ActiveDrawing);
                        }
                    }

                    // redraw all PDoubleline objects that contain wall intersections
                    Dictionary<uint, List<WallJoint>> walls = Globals.ActiveDrawing.GetWallsWithJoints();
                    foreach (uint pid in walls.Keys)
                    {
                        Primitive p = Globals.ActiveDrawing.FindObjectById(pid);
                        Globals.DrawingCanvas.VectorListControl.VectorList.UpdateSegment(p);
                    }

                    Globals.DrawingCanvas.VectorListControl.FixupLevel = 2;

                    //TimeSpan elapsed = DateTime.Now - start;
                    //System.Diagnostics.Debug.WriteLine("Load time: {0}", elapsed.TotalSeconds);
                }
                succeeded = true;

                _failedDrawingFile = null;

                Analytics.ReportEvent("load", new Dictionary<string, string> { { "format", file.FileType } });

                Globals.Events.DrawingLoaded(file.Name);
                Globals.Events.GridChanged();
            }
            catch (Exception ex)
            {
                // fail
                _failedDrawingFile = file;
                Analytics.ReportError("LoadDrawingAsync failed", ex, 1, 308);
            }

            if (uplevel)
            {
                await Alerts.StandardAlerts.UplevelDrawing();
            }

            Globals.GridIsVisible = _gridVisible;
            Globals.ShowRulers = _rulersVisible;
            Globals.Events.ShowRulers(Globals.ShowRulers);

            return succeeded;
        }

        public static async Task<bool> SaveSymbolAsAsync(Group symbol)
        {
            bool saved = false;
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/

            FileSavePicker savePicker = InitializeWithWindow(new FileSavePicker(),App.WindowHandle);
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Symbol", new List<string>() { ".dbsx" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = FileHandling.SuggestName(symbol.Name + ".dbsx");

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await SaveSymbolAsync(file, symbol);
                saved = true;

                Analytics.ReportEvent("save_symbol");
            }
            else
            {
                // cancelled
            }

            return saved;
        }

        public static string SerializeGroup(Group group)
        {
            SymbolFile symbol = new SymbolFile();

            symbol.Header.SymbolName = DrawingDocument.GroupNameBase(group.Name);
            if (symbol.Header.SymbolName != group.Name)
            {
                group.Id = Guid.NewGuid();
            }

            symbol.Header.Version = _cCurrentSymbolVersion;
            symbol.Header.Id = group.Id;
            symbol.Header.Description = group.Description;
            symbol.Header.PaperUnit = Globals.ActiveDrawing.PaperUnit;
            symbol.Header.ModelUnit = Globals.ActiveDrawing.ModelUnit;
            symbol.Header.ModelScale = Globals.ActiveDrawing.Scale;
            symbol.Header.State = Guid.NewGuid();

            if (group.CoordinateSpace == CoordinateSpace.Drawing)
            {
                if (Globals.ActiveDrawing.ModelUnit == Globals.ActiveDrawing.PaperUnit && Globals.ActiveDrawing.Scale == 1)
                {
                    symbol.Header.CoordinateSpace = CoordinateSpace.Paper;
                }
                else
                {
                    symbol.Header.CoordinateSpace = CoordinateSpace.Model;
                }
            }
            else
            {
                symbol.Header.CoordinateSpace = group.CoordinateSpace;
            }

            foreach (int id in group.Layers)
            {
                symbol.AddLayer(Globals.LayerTable[id]);
            }

            foreach (int id in group.LineTypes)
            {
                symbol.AddLineType(Globals.LineTypeTable[id]);
            }

            foreach (int id in group.TextStyles)
            {
                symbol.AddTextStyle(Globals.TextStyleTable[id]);
            }

            foreach (int id in group.ArrowStyles)
            {
                symbol.AddArrowStyle(Globals.ArrowStyleTable[id]);
            }

            List<string> groupNames = new List<string>();
            AddGroupNamesToList(groupNames, group);
            symbol.AddGroup(group);

            foreach (string name in groupNames)
            {
                Group g = Globals.ActiveDrawing.GetGroup(name);
                if (g != null)
                {
                    symbol.AddGroup(g);
                }
            }

            symbol.AddPatterns(group.CrosshatchPatterns);
            //await symbol.AddImageRefs(group.Images);

            Utf8StringWriter writer = new Utf8StringWriter();
            XmlSerializer serializer = new XmlSerializer(typeof(SymbolFile));
            serializer.Serialize(writer, symbol);
            writer.Flush();

            return writer.ToString();
        }

        public static async Task SaveSymbolAsync(StorageFile file, Group group)
        {
            SymbolFile symbol = new SymbolFile();

            symbol.Header.SymbolName = DrawingDocument.GroupNameBase(file.DisplayName);
            if (symbol.Header.SymbolName != group.Name)
            {
                group.Id = Guid.NewGuid();
            }

            symbol.Header.Version = _cCurrentSymbolVersion;
            symbol.Header.Id = group.Id;
            symbol.Header.Description = group.Description;
            symbol.Header.PaperUnit = Globals.ActiveDrawing.PaperUnit;
            symbol.Header.ModelUnit = Globals.ActiveDrawing.ModelUnit;
            symbol.Header.ModelScale = Globals.ActiveDrawing.Scale;
            symbol.Header.State = Guid.NewGuid();

            if (group.CoordinateSpace == CoordinateSpace.Drawing)
            {
                if (Globals.ActiveDrawing.ModelUnit == Globals.ActiveDrawing.PaperUnit && Globals.ActiveDrawing.Scale == 1)
                {
                    symbol.Header.CoordinateSpace = CoordinateSpace.Paper;
                }
                else
                {
                    symbol.Header.CoordinateSpace = CoordinateSpace.Model;
                }
            }
            else
            {
                symbol.Header.CoordinateSpace = group.CoordinateSpace;
            }

            // If a thumbnail image exists, base64 encode it and save it in the header
            StorageFile thumbFile = await GetGroupThumbnail(group);
            if (thumbFile != null)
            {
                symbol.Header.Thumbnail = await Utilities.EncodeImage(thumbFile);
            }

            foreach (int id in group.Layers)
            {
                symbol.AddLayer(Globals.LayerTable[id]);
            }

            foreach (int id in group.LineTypes)
            {
                symbol.AddLineType(Globals.LineTypeTable[id]);
            }

            foreach (int id in group.TextStyles)
            {
                symbol.AddTextStyle(Globals.TextStyleTable[id]);
            }

            foreach (int id in group.ArrowStyles)
            {
                symbol.AddArrowStyle(Globals.ArrowStyleTable[id]);
            }

            List<string> groupNames = new List<string>();
            AddGroupNamesToList(groupNames, group);

            foreach (string name in groupNames)
            {
                symbol.AddGroup(Globals.ActiveDrawing.GetGroup(name));
            }

            symbol.AddPatterns(group.CrosshatchPatterns);
            await symbol.AddImageRefs(group.Images);

            bool deferred = false;

            try
            {
                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                deferred = true;
            }
            catch
            {
                // this must be dropbox
            }

            using (Stream fileStream = await file.OpenStreamForWriteAsync())
            {
                fileStream.SetLength(0);

                StreamWriter writer = new StreamWriter(fileStream);

                XmlSerializer serializer = new XmlSerializer(typeof(SymbolFile));
                serializer.Serialize(writer, symbol);
                writer.Flush();
            }

            bool success = true;
            if (deferred)
            {
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                success = status == FileUpdateStatus.Complete;

                if (success)
                {
                    // ...
                }
            }
        }

        private static void AddGroupNamesToList(List<string> groupNames, Group group)
        {
            foreach (Primitive p in group.Items)
            {
                if (p is PInstance)
                {
                    string name = ((PInstance)p).GroupName;
                    if (groupNames.Contains(name) == false)
                    {
                        Group g = Globals.ActiveDrawing.GetGroup(name);
                        AddGroupNamesToList(groupNames, g);
                    }
                }
            }

            groupNames.Add(group.Name);
        }

        public static async Task<string> GetMultipleSymbols(IList<StorageFile> files)
        {
            string groupName = null;

            if (files != null && files.Count > 0)
            {
                Group symbol = null;

                foreach (StorageFile file in files)
                {
                    if (file.FileType.ToLower() == ".dbsx")
                    {
                        symbol = await GetSymbolAsync(file);
                    }
                }

                if (symbol != null)
                {
                    groupName = symbol.Name;
                }
            }

            return groupName;
        }

        public static async Task<Group> GetSingleSymbolAsync()
        {
            Group g = null;
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/

            FileOpenPicker openPicker = InitializeWithWindow(new FileOpenPicker(),App.WindowHandle);
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".dbsx");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                g = await GetSymbolAsync(file);
            }
            return g;
        }

        public static async Task<string> GetSymbolAsync()
        {
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
            FileOpenPicker openPicker = InitializeWithWindow(new FileOpenPicker(),App.WindowHandle);
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".dbsx");
            IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();
            return await GetMultipleSymbols(files.ToList<StorageFile>());
        }

        public static async Task<Group> GetSymbolFromStreamAsync(Stream fileStream, bool usePaperSpace = false)
        {
            Group symbol = null;

            bool uplevel = false;

            System.IO.StreamReader reader = new System.IO.StreamReader(fileStream);

            XmlSerializer serializer = new XmlSerializer(typeof(SymbolFile));
            SymbolFile symbolFile = serializer.Deserialize(fileStream) as SymbolFile;

            uplevel = symbolFile.Header.Version > _cMaximumSupportedSymbolVersion;

            Dictionary<string, Group> groups = new Dictionary<string, Group>();

            foreach (Group g in symbolFile.Groups)
            {
                if (symbolFile.Header.Version < 3)
                {
                    if (g.Exit.X != 0 || g.Exit.Y != 0)
                    {
                        // Prior to symbol version 3 the exit point was in paper units
                        if (g.ModelScale >= 1)
                        {
                            if (g.ModelScale > 0)
                            {
                                double scale = 1 / g.ModelScale;
                                if (g.ModelUnit == Unit.Feet)
                                {
                                    scale *= 12;
                                }
                                else if (g.ModelUnit == Unit.Millimeters)
                                {
                                    scale /= 25.4;
                                }
                                else if (g.ModelUnit == Unit.Centimeters)
                                {
                                    scale /= 2.54;
                                }
                                else if (g.ModelUnit == Unit.Meters)
                                {
                                    scale /= 0.0254;
                                }
                                g.Exit = new Point(g.Exit.X / scale, -g.Exit.Y / scale);
                            }
                        }
                    }
                }
                groups.Add(g.Name, g);
            }

            // Proir to version 2.0 symbol files were assumed to be single symbols.
            // The symbol name was the same as the file name, enforced on load.
            // Symbol files contained all of the child groups in the symbol definition.
            // These were assumed to be anonymous.

            if (symbolFile.Header.Id != Guid.Empty && string.IsNullOrEmpty(symbolFile.Header.Thumbnail) == false)
            {
                bool needThumbnail = false;
                StorageFile thumbFile = null;

                // Create a thumbnail if one doesn't exist
                string name = symbolFile.Header.Id + ".png";
                try
                {
                    thumbFile = await Globals.SymbolThumbnailFolder.GetFileAsync(name);
                }
                catch (FileNotFoundException)
                {
                    needThumbnail = true;
                }

                if (needThumbnail)
                {
                    thumbFile = await Utilities.CreateTemporaryImageFromUriAsync(symbolFile.Header.Id.ToString(), Globals.SymbolThumbnailFolder, symbolFile.Header.Thumbnail); ;
                }
            }

            foreach (Group g in groups.Values)
            {
                if (g.Id == symbolFile.Header.Id)
                {
                    symbol = g;
                    symbol.CoordinateSpace = usePaperSpace ? CoordinateSpace.Paper : symbolFile.Header.CoordinateSpace;
                    break;
                }
            }

            if (symbol == null && groups.ContainsKey(symbolFile.Header.SymbolName))
            {
                symbol = groups[symbolFile.Header.SymbolName];
            }

            if (symbol != null)
            {
                groups.Remove(symbol.Name);

                string uniqueName = Globals.ActiveDrawing.UniqueGroupName(groups, symbol.Name);
                Group existing = Globals.ActiveDrawing.GetGroupById(symbolFile.Header.Id);

                if (existing != null)
                {
                    // This symbol already exists in the drawing.
                    symbol = existing;

                    if (DrawingDocument.GroupNameBase(existing.Name) != DrawingDocument.GroupNameBase(uniqueName))
                    {
                        // if the symbol was renamed it should have been given a new id
                        // report an error somehow
                    }
                }
                else
                {
                    symbol.Name = uniqueName;

                    //groups.Remove(symbolFile.Header.SymbolName);

                    Dictionary<string, int> layerNameMap = new Dictionary<string, int>();
                    Dictionary<string, int> lineTypeNameMap = new Dictionary<string, int>();
                    Dictionary<string, int> textStyleNameMap = new Dictionary<string, int>();
                    Dictionary<string, int> arrowStyleNameMap = new Dictionary<string, int>();

                    Dictionary<int, int> layerMap = new Dictionary<int, int>();
                    Dictionary<int, int> lineTypeMap = new Dictionary<int, int>();
                    Dictionary<int, int> textStyleMap = new Dictionary<int, int>();
                    Dictionary<int, int> arrowStyleMap = new Dictionary<int, int>();

                    lineTypeMap.Add(-1, -1);

                    if (symbolFile.LineTypes.Length > 0)
                    {
                        foreach (LineType l in Globals.LineTypeTable.Values)
                        {
                            lineTypeNameMap.Add(l.Name, l.Id);
                        }

                        foreach (LineType l in symbolFile.LineTypes)
                        {
                            if (lineTypeNameMap.ContainsKey(l.Name))
                            {
                                lineTypeMap.Add(l.Id, lineTypeNameMap[l.Name]);
                            }
                            else
                            {
                                int id = Globals.ActiveDrawing.AddLineType(l.Name, l.StrokeDashArray);
                                lineTypeMap.Add(l.Id, id);
                            }
                        }
                    }

                    if (symbolFile.TextStyles.Length > 0)
                    {
                        foreach (TextStyle ts in Globals.TextStyleTable.Values)
                        {
                            textStyleNameMap.Add(ts.Name, ts.Id);
                        }

                        foreach (TextStyle symts in symbolFile.TextStyles)
                        {
                            if (textStyleNameMap.ContainsKey(symts.Name))
                            {
                                textStyleMap.Add(symts.Id, textStyleNameMap[symts.Name]);
                            }
                            else
                            {
                                int id = Globals.ActiveDrawing.AddTextStyle(symts.Name, symts.Font, symts.Size, symts.Offset, symts.Spacing, symts.CharacterSpacing);
                                textStyleMap.Add(symts.Id, id);
                            }
                        }
                    }

                    if (symbolFile.ArrowStyles.Length > 0)
                    {
                        foreach (ArrowStyle s in Globals.ArrowStyleTable.Values)
                        {
                            arrowStyleNameMap.Add(s.Name, s.Id);
                        }

                        foreach (ArrowStyle s in symbolFile.ArrowStyles)
                        {
                            if (arrowStyleNameMap.ContainsKey(s.Name))
                            {
                                arrowStyleMap.Add(s.Id, arrowStyleNameMap[s.Name]);
                            }
                            else
                            {
                                int id = Globals.ActiveDrawing.AddArrowStyle(s.Name, s.Type, s.Size, s.Aspect);
                                arrowStyleMap.Add(s.Id, id);
                            }
                        }
                    }

                    foreach (Layer l in Globals.LayerTable.Values)
                    {
                        layerNameMap.Add(l.Name, l.Id);
                    }

                    foreach (Layer l in symbolFile.Layers)
                    {
                        if (layerNameMap.ContainsKey(l.Name))
                        {
                            layerMap.Add(l.Id, layerNameMap[l.Name]);
                        }
                        else
                        {
                            int id = Globals.ActiveDrawing.AddLayer(l.Name, l.ColorSpec, l.LineTypeId, l.LineWeightId);
                            layerMap.Add(l.Id, id);
                        }
                    }

                    Globals.Events.AttributesListChanged();

                    if (groups.Count > 0)
                    {
                        string seed = symbol.Name.Contains(":") ? symbol.Name.Replace(":", "|") : symbol.Name + "|0";

                        foreach (Group g in groups.Values)
                        {
                            string oldname = g.Name;
                            g.Name = Globals.ActiveDrawing.UniqueGroupName(groups, oldname.StartsWith(":") ? seed : oldname);
                            //g.IncludeInLibrary = false;

                            if (oldname != g.Name)
                            {
                                //System.Diagnostics.Debug.WriteLine($"ReassociateMemberInstances({oldname}, {g.Name})");
                                symbol.ReassociateMemberInstances(oldname, g.Name);

                                foreach (Group g1 in groups.Values)
                                {
                                    g1.ReassociateMemberInstances(oldname, g.Name);
                                }
                            }

                            foreach (Primitive p in g.Items)
                            {
                                if (layerMap.ContainsKey(p.SerializedLayerId))
                                {
                                    p.LayerId = layerMap[p.SerializedLayerId];
                                }
                                else
                                {
                                    p.LayerId = 0;
                                }
                                p.LineTypeId = lineTypeMap[p.LineTypeId];

                                if (p is PText)
                                {
                                    int tsid = ((PText)p).TextStyleId;
                                    tsid = textStyleMap[tsid];
                                    ((PText)p).TextStyleId = tsid;
                                }
                                else if (p is PDimension)
                                {
                                    ((PDimension)p).TextStyleId = textStyleMap[((PDimension)p).TextStyleId];
                                    ((PDimension)p).ArrowStyleId = arrowStyleMap[((PDimension)p).ArrowStyleId];
                                }
                                else if (p is PArrow)
                                {
                                    ((PArrow)p).ArrowStyleId = arrowStyleMap[((PArrow)p).ArrowStyleId];
                                }
                            }

                            Globals.ActiveDrawing.AddGroup(g);
                        }
                    }

                    foreach (Primitive p in symbol.Items)
                    {
                        if (layerMap.ContainsKey(p.SerializedLayerId))
                        {
                            p.LayerId = layerMap[p.SerializedLayerId];
                        }
                        else
                        {
                            p.LayerId = 0;
                        }
                        p.LineTypeId = lineTypeMap[p.LineTypeId];

                        if (p is PText ptext)
                        {
                            if (textStyleMap.ContainsKey(p.SerializedTextStyleId))
                            {
                                ptext.TextStyleId = textStyleMap[((PText)p).SerializedTextStyleId];
                            }
                            else
                            {
                                ptext.TextStyleId = 0;
                            }
                        }
                        else if (p is PDimension)
                        {
                            if (textStyleMap.ContainsKey(p.SerializedTextStyleId))
                            {
                                ((PDimension)p).TextStyleId = textStyleMap[((PDimension)p).SerializedTextStyleId];
                            }
                            else
                            {
                                ((PDimension)p).TextStyleId = 0;
                            }
                            if (arrowStyleMap.ContainsKey(p.SerializedArrowStyleId))
                            {
                                ((PDimension)p).ArrowStyleId = arrowStyleMap[((PDimension)p).SerializedArrowStyleId];
                            }
                            else
                            {
                                ((PDimension)p).ArrowStyleId = 0;
                            }
                        }
                        else if (p is PArrow)
                        {
                            if (arrowStyleMap.ContainsKey(p.SerializedArrowStyleId))
                            {
                                ((PArrow)p).ArrowStyleId = arrowStyleMap[((PArrow)p).SerializedArrowStyleId];
                            }
                            else
                            {
                                ((PArrow)p).ArrowStyleId = 0;
                            }
                        }
                    }

                    if (Globals.UIVersion == 0)
                    {
                        symbol.PreferredScale = OldPreferredScale(symbol);
                    }
                    else
                    {
                        if (Globals.ActiveDrawing.Scale == 1)
                        {
                            if (symbol.CoordinateSpace == CoordinateSpace.Paper)
                            {
                                symbol.PreferredScale = PreferredScale(symbol);
                            }
                            else
                            {
                                symbol.PreferredScale = symbolFile.Header.ModelScale != 0 ? symbolFile.Header.ModelScale : 1;
                                if (symbol.PaperUnit != Globals.ActiveDrawing.PaperUnit)
                                {
                                    if (symbol.PaperUnit == Unit.Inches && Globals.ActiveDrawing.PaperUnit == Unit.Millimeters)
                                    {
                                        symbol.PreferredScale /= 25.4;
                                    }
                                    else if (symbol.PaperUnit == Unit.Millimeters && Globals.ActiveDrawing.PaperUnit == Unit.Inches)
                                    {
                                        symbol.PreferredScale *= 25.4;
                                    }
                                }
                            }
                        }
                        else
                        {
                            symbol.PreferredScale = 1;
                        }
                    }

                    symbol.CoordinateSpace = symbolFile.Header.CoordinateSpace;
                    symbol.State = symbolFile.Header.State;
                    //symbol.IncludeInLibrary = true;

                    Globals.ActiveDrawing.AddGroup(symbol);
                }
            }
            else
            {
                // Format error: the symbol file doesn't contain the group referenced in the header
                throw new Exception("Symbol format error");
            }

            if (uplevel)
            {
                await Alerts.StandardAlerts.UplevelSymbol();
            }

            return symbol;
        }

        public static async Task<Group> GetSymbolAsync(StorageFile file)
        {
            Group symbol = null;

            if (Globals.ActiveDrawing == null)
            {
                throw new Exception("Can't load symbol because there is not a vaild ActiveDrawing");
            }

            try
            {
                using (Stream fileStream = await file.OpenStreamForReadAsync())
                {
                    symbol = await GetSymbolFromStreamAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                // fail
                Analytics.ReportError("GetSymbolAsync failed", ex, 1, 309);
                await Alerts.StandardAlerts.IOError();
            }

            return symbol;
        }

        public static double OldPreferredScale(Group symbol)
        {
            double preferredScale = 1;

            if (symbol.ModelUnit != Globals.ActiveDrawing.ModelUnit || symbol.PaperUnit != Globals.ActiveDrawing.PaperUnit || symbol.ModelScale != Globals.ActiveDrawing.Scale)
            {
                // the symbol coordinate system is scaled model units
                preferredScale = 1;

                if (symbol.CoordinateSpace == CoordinateSpace.Paper)
                {
                    // this symbol should be inserted at its native paper size
                    // first scale the coordinate system to full-scale model units
                    preferredScale = Globals.ActiveDrawing.Scale / symbol.ModelScale;

                    switch (Globals.ActiveDrawing.ModelUnit)
                    {
                        case Unit.Feet:
                            preferredScale /= 12;
                            break;
                        case Unit.Millimeters:
                            preferredScale *= 25.4;
                            break;
                        case Unit.Centimeters:
                            preferredScale *= 2.54;
                            break;
                        case Unit.Meters:
                            preferredScale *= 0.0254;
                            break;
                    }

                    switch (symbol.ModelUnit)
                    {
                        case Unit.Feet:
                            preferredScale *= 12;
                            break;
                        case Unit.Millimeters:
                            preferredScale /= 25.4;
                            break;
                        case Unit.Centimeters:
                            preferredScale /= 2.54;
                            break;
                        case Unit.Meters:
                            preferredScale /= 0.0254;
                            break;
                    }
                }
                else
                {
                    // this symbol should be inserted at its native model size
                    preferredScale = 1;

                    switch (Globals.ActiveDrawing.ModelUnit)
                    {
                        case Unit.Feet:
                            preferredScale /= 12;
                            break;
                        case Unit.Millimeters:
                            preferredScale *= 25.4;
                            break;
                        case Unit.Centimeters:
                            preferredScale *= 2.54;
                            break;
                        case Unit.Meters:
                            preferredScale *= 0.0254;
                            break;
                    }

                    switch (symbol.ModelUnit)
                    {
                        case Unit.Feet:
                            preferredScale *= 12;
                            break;
                        case Unit.Millimeters:
                            preferredScale /= 25.4;
                            break;
                        case Unit.Centimeters:
                            preferredScale /= 2.54;
                            break;
                        case Unit.Meters:
                            preferredScale /= 0.0254;
                            break;
                    }
                }
            }
            return preferredScale;
        }

        public static double PreferredScale(Group symbol)
        {
            double preferredScale = 1;

            if (symbol.ModelUnit != Globals.ActiveDrawing.ModelUnit || symbol.PaperUnit != Globals.ActiveDrawing.PaperUnit)
            {
                // the symbol coordinate system is scaled model units
                preferredScale = 1;

                if (symbol.CoordinateSpace == CoordinateSpace.Paper)
                {
                    switch (symbol.ModelUnit)
                    {
                        case Unit.Feet:
                            preferredScale /= 12;
                            break;
                        case Unit.Millimeters:
                            preferredScale *= 25.4;
                            break;
                        case Unit.Centimeters:
                            preferredScale *= 2.54;
                            break;
                        case Unit.Meters:
                            preferredScale *= 0.0254;
                            break;
                    }

                    //switch (symbol.ModelUnit)
                    //{
                    //    case Unit.Feet:
                    //        preferredScale *= 12;
                    //        break;
                    //    case Unit.Millimeters:
                    //        preferredScale /= 25.4;
                    //        break;
                    //    case Unit.Centimeters:
                    //        preferredScale /= 2.54;
                    //        break;
                    //    case Unit.Meters:
                    //        preferredScale /= 0.0254;
                    //        break;
                    //}
                }
                else
                {
                    //// this symbol should be inserted at its native model size
                    //preferredScale = 1;

                    //switch (Globals.ActiveDrawing.ModelUnit)
                    //{
                    //    case Unit.Feet:
                    //        preferredScale /= 12;
                    //        break;
                    //    case Unit.Millimeters:
                    //        preferredScale *= 25.4;
                    //        break;
                    //    case Unit.Centimeters:
                    //        preferredScale *= 2.54;
                    //        break;
                    //    case Unit.Meters:
                    //        preferredScale *= 0.0254;
                    //        break;
                    //}

                    //switch (symbol.ModelUnit)
                    //{
                    //    case Unit.Feet:
                    //        preferredScale *= 12;
                    //        break;
                    //    case Unit.Millimeters:
                    //        preferredScale /= 25.4;
                    //        break;
                    //    case Unit.Centimeters:
                    //        preferredScale /= 2.54;
                    //        break;
                    //    case Unit.Meters:
                    //        preferredScale /= 0.0254;
                    //        break;
                    //}
                }
            }
            return preferredScale;
        }

        public static async Task<bool> ExportDrawingAsync(string format = null, bool showFrame = true, bool showGrid = false)
        {
            bool success = false;
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/

            FileSavePicker savePicker = InitializeWithWindow(new FileSavePicker(),App.WindowHandle);
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as

            if (format == null || format == "PDF")
            {
                savePicker.FileTypeChoices.Add("PDF Document", new List<string>() { ".pdf" });
            }
            if (format == null || format == "SVG")
            {
                savePicker.FileTypeChoices.Add("SVG Document", new List<string>() { ".svg" });
            }
            if (format == null || format == "JPG")
            {
                savePicker.FileTypeChoices.Add("JPG Image", new List<string>() { ".jpg" });
                savePicker.FileTypeChoices.Add("JPEG Image", new List<string>() { ".jpeg" });
            }
            if (format == null || format == "PNG")
            {
                savePicker.FileTypeChoices.Add("PNG Image", new List<string>() { ".png" });
            }
            if (format == null || format == "DXF")
            {
                savePicker.FileTypeChoices.Add("DXF Document", new List<string>() { ".dxf" });
            }
#if SIBERIA
            // Default file name if the user does not type one in or select a file to replace
            if (string.IsNullOrEmpty(_currentDrawingName))
            {
                if (string.IsNullOrEmpty(format))
                {
                    savePicker.SuggestedFileName = "Drawing";
                }
                else
                {
                    string extension = "." + format.ToLower();
                    savePicker.SuggestedFileName = FileHandling.SuggestName(extension);
                }
            }
            else
            {
                if (_currentDrawingName.EndsWith(".dbfx"))
                {
                    savePicker.SuggestedFileName = _currentDrawingName.Replace(".dbfx", "");
                }
                else
                {
                    savePicker.SuggestedFileName = "Drawing";
                }
            }

            StorageFile file = null;
            string error = null;

            try
            {
                file = await savePicker.PickSaveFileAsync();
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            if (file != null)
            {
                Analytics.ReportEvent(string.Format("export-{0}", file.FileType.Replace(".", "")));

                try
                {
                    string filetype = file.FileType.ToLower();

                    if (filetype == ".pdf")
                    {
                        await Cirros.Pdf.PdfExport.ExportDrawingToPdfAsync(file, showFrame);
                    }
                    else if (filetype == ".svg")
                    {
                        await ExportDrawingToSvgAsync(file, showFrame);
                    }
                    else if (filetype == ".png" || filetype == ".jpg")
                    {
                        await Cirros.Core.Dx.ExportDrawingToImageFileAsync(file, Globals.ExportImageSize, Globals.ExportResolution, showFrame, showGrid);
                    }
                    else if (filetype == ".dxf")
                    {
                        await ExportDrawingToDxfAsync(file, showFrame);
                    }
                    success = true;
                }
                catch (Exception ex)
                {
                    Analytics.ReportError("ExportDrawingAsync failed", ex, 1, 310);
                }

                if (success == false)
                {
                    await Cirros.Alerts.StandardAlerts.SaveError();
                }
            }
            else if (string.IsNullOrEmpty(error) == false)
            {
                await Cirros.Alerts.StandardAlerts.SaveError();
            }
            else
            {
                // cancelled
            }
#endif
            return success;
        }
#if SIBERIA
        private async static Task ExportDrawingToSvgAsync(StorageFile file, bool showFrame)
        {
            bool deferred = false;

            try
            {
                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                deferred = true;
            }
            catch
            {
                // this must be dropbox
            }

            using (Stream fileStream = await file.OpenStreamForWriteAsync())
            {
                fileStream.SetLength(0);

                StreamWriter writer = new StreamWriter(fileStream);

                string xml = await Cirros.Core.SvgExport.ExportToSvg(showFrame);
                await writer.WriteAsync(xml);
                writer.Flush();
            }

            bool success = true;
            if (deferred)
            {
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                success = status == FileUpdateStatus.Complete;

                if (success)
                {
                    // ...
                }
            }
        }

        private async static Task ExportDrawingToDxfAsync(StorageFile file, bool showFrame)
        {
            bool deferred = false;

            try
            {
                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                deferred = true;
            }
            catch
            {
                // this must be dropbox
            }

            using (Stream fileStream = await file.OpenStreamForWriteAsync())
            {
                fileStream.SetLength(0);

                StreamWriter writer = new StreamWriter(fileStream);

                string dxf = Cirros.Dxf.DxfExport.ExportToDxf(showFrame);

                await writer.WriteAsync(dxf);
                writer.Flush();
            }

            bool success = true;
            if (deferred)
            {
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                success = status == FileUpdateStatus.Complete;

                if (success)
                {
                    // ...
                }
            }
        }
#endif
        public static async Task CreateThumbnail(string name)
        {
            DateTime then = DateTime.Now;

            if (Globals.ActiveDrawing != null)
            {
                try
                {
                    StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(name + ".png", CreationCollisionOption.ReplaceExisting);
                    if (file != null)
                    {
                        await Cirros.Core.Dx.ExportDrawingToImageFileAsync(file, 240, 72);
                    }
                    else
                    {
                        Analytics.ReportEvent("thumbnail-file-failed");
                    }
                }
                catch (Exception ex)
                {
                    Analytics.ReportEvent("thumbnail-exception");
                    Analytics.ReportError("Failed to create thumbnail", ex, 2, 311);
                }
            }

            //TimeSpan elapsed = DateTime.Now - then;
            //System.Diagnostics.Debug.WriteLine("CreateThumbnail({0}): {1}", name, elapsed);
        }

        public static async Task<StorageFile> GetGroupThumbnail(Group g)
        {
            bool needThumbnail = false;
            StorageFile file = null;

            // Create a thumbnail if one doesn't exist
            string name = g.Id + ".png";
            try
            {
                file = await Globals.SymbolThumbnailFolder.GetFileAsync(name);
            }
            catch (FileNotFoundException)
            {
                needThumbnail = true;
            }

            if (needThumbnail)
            {
                try
                {
                    Rect oldBounds = g.ModelBounds;
                    g.UpdateBounds();

                    file = await Globals.SymbolThumbnailFolder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
                    if (file != null)
                    {
                        await Cirros.Core.Dx.ExportGroupToImageFileAsync(g, file, 100, 10);
                    }
                }
                catch (Exception ex)
                {
                    Analytics.ReportError("Failed to create group thumbnail", ex, 2, 312);
                }
            }

            return file;
        }

        static bool _hasRunOnce = false;

        public static async Task RemoveOphanThumbnails()
        {
            if (_hasRunOnce == false)
            {
                _hasRunOnce = true;

                List<string> fileTypeFilter = new List<string>();
                fileTypeFilter.Add(".png");

                try
                {
                    var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);

                    // Create query and retrieve files
                    var query = ApplicationData.Current.LocalFolder.CreateFileQueryWithOptions(queryOptions);
                    IReadOnlyList<StorageFile> fileList = await query.GetFilesAsync();
                    // Process results
                    foreach (StorageFile file in fileList)
                    {
                        if (file.Name.StartsWith("__") == false)
                        {
                            if (file.Name.EndsWith(".png"))
                            {
                                string token = file.Name.Replace(".png", "");
                                if (StorageApplicationPermissions.MostRecentlyUsedList.ContainsItem(token) == false)
                                {
                                    await file.DeleteAsync();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Analytics.ReportError("Error removing orphan thumbnails", ex, 2, 313);
                }
            }
        }

        public static async Task<StorageFile> GetMruThumbnailAsync(string token)
        {
            string thumbnail = (token == null ? sessionStateFilename : token) + ".png";
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(thumbnail);
            return file;
        }

        public static string AddFileToMru(StorageFile file)
        {
            string token = null;
            try
            {
                token = StorageApplicationPermissions.MostRecentlyUsedList.Add(file, DateTime.Now.ToFileTime().ToString());
            }
            catch (Exception ex)
            {
                Analytics.ReportError("AddFileToMru", ex, 4, 314);
            }
            return token;
        }

        public static async Task UpdateMruTimestampAndThumbnailAsync(string token, StorageFile mrufile, bool forceUpdate)
        {
            try
            {
                StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(token, mrufile, DateTime.Now.ToFileTime().ToString());
            }
            catch (Exception ex)
            {
                Analytics.ReportError("UpdateMruTimestampAndThumbnailAsync", ex, 4, 315);
            }

            if (Globals.ActiveDrawing == null)
            {
                // Nothing to thumbnail
            }
            else if (Globals.ActiveDrawing.IsModified)
            {
                // Update thumbnail if the work canvas has been modified
                await CreateThumbnail(token);
            }
            else
            {
                bool needThumbnail = forceUpdate;

                // Create a thumbnail if one doesn't exist
                string thumbnail = token + ".png";
                try
                {
                    StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(thumbnail);
                }
                catch (FileNotFoundException)
                {
                    needThumbnail = true;
                }

                if (needThumbnail)
                {
                    await CreateThumbnail(token);
                }
            }
        }

        public static async Task RemoveFileFromMruAsync(string token)
        {
            try
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Remove(token);

                string thumbnail = token + ".png";

                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(thumbnail);
                if (file != null)
                {
                    await file.DeleteAsync();
                }
            }
            catch // (FileNotFoundException)
            {
            }
        }

        public static async Task<int> ImportDxfAsync(StorageFile file)
        {
            int result = 0;
#if SIBERIA
#if true
            BasicProperties props = await file.GetBasicPropertiesAsync();
            Globals.Events.DrawingLoading(file.Name, props.Size);

            DxfImporter importer = new DxfImporter();
            result = await importer.ReadDxfFile(file);
            if (result > 0)
            {
                bool doImport = true;

                if (importer.Context.ViewPortList.Count < 2)
                {
                    DxfImportDialog dialog = new DxfImportDialog(importer.Context);
                    ContentDialogResult dialogResult = await dialog.ShowAsync();

                    doImport = dialogResult == ContentDialogResult.Primary;
                }

                if (doImport)
                {
                    // continue
                    result = importer.Import();

                    switch (result)
                    {
                        case -3:
                            await Cirros.Alerts.StandardAlerts.DxfContentError();
                            break;

                        case -2:
                            await Cirros.Alerts.StandardAlerts.DxfFormatError();
                            break;

                        case -1:
                            await Cirros.Alerts.StandardAlerts.DxfIOError();
                            break;

                        case 0:
                        default:
                            break;
                    }
                }
                else 
                {
                    // cancel
                    result = -4;
                }
            }

            Globals.Events.DrawingLoaded(file.Name);
#else
            result = await Cirros.Dxf.DxfIO.ImportDxfAsync(file);
#endif
#endif
            return result;
        }

        public static async Task<int> ImportSvgAsync(StorageFile file)
        {
            int status = 0;
#if SIBERIA

            if (file != null)
            {
                SvgImport importer = new SvgImport();

                XmlReaderSettings xrs = new XmlReaderSettings() { Async = true, CloseInput = true, DtdProcessing = DtdProcessing.Ignore };

                try
                {
                    SvgDomReader reader = new SvgDomReader(xrs);
                    string error = await reader.Import(file);

                    if (error == null)
                    {
                        SvgDom dom = reader.Dom;

                        status = await importer.CreateDrawing(dom);

                        if (importer.MissingFonts.Count > 0)
                        {
                            await StandardAlerts.MissingFontsAsync(importer.MissingFonts);
                        }

                        Globals.Events.DrawingLoaded(file.Name);
                        Globals.Events.GridChanged();
                        Globals.Events.ShowRulers(Globals.ShowRulers);
                    }
                    else
                    {
                        await StandardAlerts.InvalidSvgFile(error);
                    }
                }
                catch // (Exception e)
                {
                    //System.Diagnostics.Debug.WriteLine(e.Message);
                    status = -1;
                }
            }
#endif
            return status;
        }


        public static async Task<StorageFile> PickImageFileAsync()
        {
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
            FileOpenPicker openPicker = InitializeWithWindow(new FileOpenPicker(),App.WindowHandle);
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".pdf");

            StorageFile file = await openPicker.PickSingleFileAsync();

            return file;
        }


        public static async Task<StorageFile> TakePhotoAsync()
        {
            StorageFile file = null;

#if SIBERIA
            try
            {
                DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                if (devices.Count > 0)
                {
                    // TODO Windows.Media.Capture.CameraCaptureUI is not yet supported in WindowsAppSDK. For more details see https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/what-is-supported
                    CameraCaptureUI dialog = new CameraCaptureUI();
                    Size aspectRatio = new Size(4, 3);
                    dialog.PhotoSettings.CroppedAspectRatio = aspectRatio;

                    file = await dialog.CaptureFileAsync(CameraCaptureUIMode.Photo);
                    if (file == null)
                    {
                        //System.Diagnostics.Debug.WriteLine("No photo captured.");
                    }
                }
                else
                {
                    Globals.Events.ShowAlert("NoCamera");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
#endif
            return file;
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

}
