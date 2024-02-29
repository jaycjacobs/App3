using App3;
using Cirros;
using Cirros.Core;
using Cirros8;
using CirrosUWP.HUIApp;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using RedDog;
using RedDog.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI;
using Windows.UI.ApplicationSettings;
using Windows.UI.Popups;

namespace KT22
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class KTHomePage : Page, IHomePage
    {
        ApplicationDataContainer _homeSettings = null;
        bool _firstRun = false;
        static bool _sessionFirstRun = true;
        static bool _uxFirstRun = false;

        private string _helpSite = "http://drawingboardapp.com/"; // "http://highcampsoftware.net/";

        Color _normalBackground = Colors.WhiteSmoke;
        Color _hoveredBackground = Colors.Gainsboro;
        Color _pressedBackground = Colors.LightGray;
        Grid _hoveredButton = null;
        private IList<object> _mruSelection;

        public KTHomePage()
        {
            Analytics.Trace("KTHomePage", "enter constructor");
            this.InitializeComponent();

            InitializeSettings();

            DataContext = Globals.UIDataContext;
            SetUIFontSize();

            // IMPORTANT: the following line should be enabled for production
            if (_homeSettings != null && _homeSettings.Values.ContainsKey("uiversion") == false)
            {
                _uxFirstRun = true;
                RedDogGlobals.HUIDialogFirstRun = true;
                RedDogGlobals.RedDogFirstRun = true;
            }

            if (_sessionFirstRun)
            {
                Globals.EnableBetaFeatures = GetSettingsValue("beta") == "enabled";
                Globals.UIVersion = GetSettingsValue("uiversion") == "1" ? (uint)1 : 0;
                _sessionFirstRun = false;
            }
            else
            {
                WriteSettingsEntry("beta", Globals.EnableBetaFeatures ? "enabled" : "disabled");
            }

            _recentDrawingsControl.Visibility = Visibility.Collapsed;

            _createButton.PointerEntered += homeButton_PointerEntered;
            _createButton.PointerExited += homeButton_PointerExited;
            _createButton.PointerPressed += homeButton_PointerPressed;
            _createButton.PointerReleased += homeButton_PointerReleased;

            _openButton.PointerEntered += homeButton_PointerEntered;
            _openButton.PointerExited += homeButton_PointerExited;
            _openButton.PointerPressed += homeButton_PointerPressed;
            _openButton.PointerReleased += homeButton_PointerReleased;

            _importButton.PointerEntered += homeButton_PointerEntered;
            _importButton.PointerExited += homeButton_PointerExited;
            _importButton.PointerPressed += homeButton_PointerPressed;
            _importButton.PointerReleased += homeButton_PointerReleased;

            if (Globals.EnableBetaFeatures)
            {
                _dwgButton.PointerEntered += homeButton_PointerEntered;
                _dwgButton.PointerExited += homeButton_PointerExited;
                _dwgButton.PointerPressed += homeButton_PointerPressed;
                _dwgButton.PointerReleased += homeButton_PointerReleased;
                _dwgButton.Visibility = Visibility.Visible;
            }
            else
            {
                _dwgButton.Visibility = Visibility.Collapsed;
            }

            _imageButton.PointerEntered += homeButton_PointerEntered;
            _imageButton.PointerExited += homeButton_PointerExited;
            _imageButton.PointerPressed += homeButton_PointerPressed;
            _imageButton.PointerReleased += homeButton_PointerReleased;

            //UpdateStoreIcons();

            if (Globals.UIDataContext.Size != 16)
            {
                ScaleTransform t = new ScaleTransform();
                t.ScaleX = t.ScaleY = (double)Globals.UIDataContext.Size / 16;
                _logoPanel.RenderTransform = t;
            }
            else
            {
                _logoPanel.RenderTransform = new MatrixTransform();
            }

            this.Loaded += KTHomePage_Loaded;
            //this.PointerExited += KTHomePage_PointerExited;
            this.SizeChanged += KTHomePage_SizeChanged;

            Analytics.TrackPageView("KTHomePage");
            Analytics.Trace("KTHomePage", "exit constructor");
        }

        private void SetUIFontSize()
        {
            if (Globals.UIFontSize == 0)
            {
                if (App.Window.Bounds.Width > 0 && App.Window.Bounds.Height > 0)
                {
                    int size = Globals.UIDataContext.Size;

                    if (App.Window.Bounds.Width > 1032 && App.Window.Bounds.Height > 740)
                    {
                        size = 16;
                    }
                    else if (App.Window.Bounds.Width > 960 && App.Window.Bounds.Height > 624)
                    {
                        size = 14;
                    }
                    else
                    {
                        size = 12;
                    }

                    if (size != Globals.UIDataContext.Size)
                    {
                        Globals.UIDataContext.Size = size;
                        HGlobals.DataContext.Size = size;
                    }
                }
            }
        }

        private void KTHomePage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double mh = _hackRect3.ActualHeight - _hackRect1.ActualHeight - 88;
            _recentDrawingsControl.UpdateRowsAndColumns(mh);
        }

        //bool _appBarOpenedByHover = false;
        //DispatcherTimer _appBarTimer = new DispatcherTimer();

        //private void KTHomePage_PointerExited(object sender, PointerRoutedEventArgs e)
        //{
        //    try
        //    {
        //        Point p = App.Window.CoreWindow.PointerPosition;
        //        Rect r = App.Window.CoreWindow.Bounds;

        //        for (int i = 0; i < 20; i++)
        //        {
        //            if (p.Y <= r.Top || p.X <= r.Left || p.X >= r.Right)
        //            {
        //                break;
        //            }
        //            else if (p.Y >= (r.Bottom - 2))
        //            {
        //                break;
        //            }

        //            p = App.Window.CoreWindow.PointerPosition;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Analytics.ReportError("KTHomePage_PointerExited menu failed", ex, 3, 522);
        //    }
        //}

        async void KTHomePage_Loaded(object sender, RoutedEventArgs e)
        {
            //CheckLicenseState();

            await ConsoleUtilities.InitializeTeachingTips();

            if (Content is FrameworkElement fe)
            {
                ConsoleUtilities.PopulateTeachingTips(this.Content as FrameworkElement);
            }
        }

        private async Task ShouldReview()
        {
            Analytics.Trace("KTHomePage.ShouldReview", "enter");
            int reviewThreshold = Analytics.GetUsageCount("review_threshold");

            if (reviewThreshold > 0)
            {
                int sessionCount = Analytics.GetUsageCount("session");

                if (sessionCount > reviewThreshold)
                {
                    await StandardAlerts8.RateRevieweMessage();
                }
            }
            Analytics.Trace("KTHomePage.ShouldReview", "exit");
        }

        private async Task ShouldOfferPreview()
        {
            Analytics.Trace("KTHomePage.ShouldOfferPreview", "enter");
            if (_uxFirstRun)
            {
                Globals.UIVersion = 1;

                await Task.Delay(500);

                _ttHomeUxPreview.IsOpen = true;
                _uxFirstRun = false;
            }
            Analytics.Trace("KTHomePage.ShouldOfferPreview", "exit");
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (Globals.UIPreview)
            {
                WriteSettingsEntry("uiversion", Globals.UIVersion == (uint)1 ? "1" : "0");
            }

            //WriteSettingsEntry("beta", Globals.EnableBetaFeatures ? "enabled" : "disabled");
            base.OnNavigatingFrom(e);
        }

        private async Task EnableRedDog()
        {
            Globals.UIPreview = await ApplicationData.Current.LocalFolder.TryGetItemAsync("reddog.txt") != null;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Analytics.Trace("KTHomePage.OnNavigatedTo", "enter");
            try
            {
                //if (await License.IsValid())
                {
                    Globals.UIPreview = true;
                    //await EnableRedDog();
#if DEBUG
                    if ((string)e.Parameter == "launch")
                    {
                        _uxFirstRun = true;
                        RedDogGlobals.HUIDialogFirstRun = true;
                        RedDogGlobals.RedDogFirstRun = true;
                    }
#endif
                    Globals.UIVersion = 1;

                    await ShouldReview();

                    Analytics.Trace("KTHomePage.e.Parameter", e.Parameter == null ? "null" : e.Parameter.ToString());
                    if (e.Parameter is string)
                    {
                        Analytics.Trace("KTHomePage.OnNavigatedTo", "e.Parameter");
                        string param = e.Parameter as string;
                        if (param.StartsWith("invalid:"))
                        {
                            await Cirros.Alerts.StandardAlerts.InvalidDrawingAsync(param.Substring(8));
                        }
                        else if (param == "new")
                        {
                            DoCreateNewDrawing();
                            return;
                        }
                        else if (param == "open_drawing")
                        {
                            await DoOpenDrawing();
                        }
                        else if (param == "cancelled")
                        {
                            // cool
                        }
                        else if (param == "launch")
                        {
                            // cool
                        }
                        else if (param == "restore_error")
                        {
                            // This condition has been hit only when stress testing Windows 8 snap mode transitions.
                            // In this case, the right behavior seems to be ignoring it and retrying.
                            await Cirros.Alerts.StandardAlerts.LoadFailedAsync(param);
                        }
                        else if (param.StartsWith("token:"))
                        {
                            // Without the delay, the app exits here when activating a file during a cold start - race condition?
                            await Task.Delay(1000);
                            string token = param.Replace("token:", "");
                            await DoOpenDrawing(token);
                        }
                        else if (string.IsNullOrEmpty(param) == false)
                        {
                            await Cirros.Alerts.StandardAlerts.LoadFailedAsync(param);
                        }
                    }
                    else if (e.Parameter is StorageFile)
                    {
                        Analytics.Trace("KTHomePage.OnNavigatedTo", "StorageFile");
                        // Without the delay, the app exits here when activating a file during a cold start - race condition?
                        await Task.Delay(500);
                        await DoOpenDrawing(e.Parameter);
                    }

                    Analytics.Trace("KTHomePage.OnNavigatedTo", "_recentDrawingsControl");
                    _recentDrawingsControl.Visibility = Visibility.Visible;

                    Package package = Package.Current;
                    PackageId packageId = package.Id;
                    string version = versionString(packageId.Version);
                    string previous = GetSettingsValue("version");

                    _versionTextBox.Text = version;

                    if (Globals.UIPreview == false)
                    {
                        if (previous != version)
                        {
                            // A new version has been installed
                            WriteSettingsEntry("version", version);

                            if (!_firstRun && !string.IsNullOrEmpty(previous))
                            {
                                // hard-coded test for updrade to version 2.x.x.x from version 3.x.x.x
                                if (previous.StartsWith("2.") && version.StartsWith("3."))
                                {
                                    if (await StandardAlerts8.FirstRunAlertAsync(previous, version) == "notes")
                                    {
                                        await Launcher.LaunchUriAsync(new Uri(_helpSite + "help/upgrade/"));
                                    }
                                }
                            }
                        }

                        Analytics.Trace("KTHomePage.OnNavigatedTo", "_firstRun");
                        if (_firstRun)
                        {
                            //_bottomAppBar.Expand();
                            //_appBar.IsOpen = true;
                            await StandardAlerts8.FirstRunAlertAsync();
                        }
                        else
                        {
                            Analytics.Trace("KTHomePage.OnNavigatedTo", "settings");
                            ApplicationDataContainer exceptionSettings = null;
                            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

                            if (localSettings.Containers.ContainsKey("exception"))
                            {
                                exceptionSettings = localSettings.Containers["exception"];
                                if (exceptionSettings.Values.ContainsKey("date"))
                                {
                                    string datestring = (string)exceptionSettings.Values["date"];
                                    exceptionSettings.Values.Remove("date");

                                    if (exceptionSettings.Values.ContainsKey("count"))
                                    {
                                        int count = (int)exceptionSettings.Values["count"];
                                        if (count == 2)
                                        {
                                            //SupportRequest sr = new SupportRequest(new Dictionary<string, string>() { { "date", datestring }, { "exceptions", count.ToString() } });
                                            //Popup popup = new Popup();
                                            //popup.IsLightDismissEnabled = false;
                                            //popup.Child = sr;
                                            //popup.VerticalOffset = (this.ActualHeight - sr.Height) / 2;
                                            //popup.HorizontalOffset = (this.ActualWidth - sr.Width) / 2;
                                            //popup.IsOpen = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //else
                //{
                //    Analytics.ReportEvent("license-error");
                //}
            }
            catch (Exception ex)
            {
                Analytics.ReportError("KTHomePage:OnNavigatedTo", ex, 2, 600);
            }
            Analytics.Trace("KTHomePage.OnNavigatedTo", "exit");
        }

        async void onSettingsCommand(IUICommand command)
        {
            SettingsCommand settingsCommand = (SettingsCommand)command;

            if ((string)settingsCommand.Id == "support")
            {
                await Launcher.LaunchUriAsync(new Uri("mailto:support@drawingboardapp.com"));
            }
            else if ((string)settingsCommand.Id == "privacy")
            {
                await Launcher.LaunchUriAsync(new Uri(_helpSite + "privacy/"));
            }
        }

        async Task DoOpenDrawing(object o = null)
        {
#if SIBERIA
            Analytics.Trace("KTHomePage.DoOpenDrawing");
            if (await Cirros.Alerts.StandardAlerts.LastChanceToSaveAsync())
            {
                App.Navigate(typeof(KTDrawingPage), "restore");
            }
            else if (o is string)
            {
                App.Navigate(typeof(KTDrawingPage), (string)o);
            }
            else if (o is StorageFile)
            {
                App.Navigate(typeof(KTDrawingPage), (StorageFile)o);
            }
            else
            {
                App.Navigate(typeof(KTDrawingPage), "open_drawing");
            }
#endif
        }

        private void DoCreateNewDrawing()
        {
            Analytics.Trace("KTHomePage.DoCreateNewDrawing");
            App.Navigate(typeof(NewDrawingPage), null);
        }

        string versionString(PackageVersion version)
        {
            return String.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        async void homeButton_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            await Analytics.TraceAsync("homeButton_PointerReleased", "enter");
            if (sender is Grid)
            {
                Grid g = sender as Grid;
                if (g == _hoveredButton)
                {
                    g.Background = new SolidColorBrush(_hoveredBackground);

                    await Analytics.TraceAsync("homeButton_PointerReleased", "LastChanceToSaveAsync");
                    if (await Cirros.Alerts.StandardAlerts.LastChanceToSaveAsync())
                    {
#if SIBERIA
                        App.Navigate(typeof(KTDrawingPage), "restore");
#endif
                    }
                    else if (g.Tag is string)
                    {
                        await Analytics.TraceAsync("homeButton_PointerReleased:switch", g.Tag as string);
                        switch ((string)g.Tag)
                        {
                            case "new":
                                App.Navigate(typeof(NewDrawingPage), "new");
                                break;

#if SIBERIA
                            case "import":
                                App.Navigate(typeof(KTDrawingPage), "import");
                                break;

                            case "dwg":
                                App.Navigate(typeof(KTDrawingPage), "import-dwg");
                                break;
                            default:
                                await Analytics.TraceAsync("homeButton_PointerReleased:default", g.Tag as string);
                                App.Navigate(typeof(KTDrawingPage), g.Tag as string);
                                break;
#endif
                        }
                    }
                }
                else
                {
                    g.Background = new SolidColorBrush(_normalBackground);
                }
                await Analytics.TraceAsync("homeButton_PointerReleased", "exit");
            }
        }

        void homeButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid)
            {
                Grid g = sender as Grid;
                g.Background = new SolidColorBrush(_pressedBackground);
            }
        }

        void homeButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid)
            {
                Grid g = sender as Grid;
                g.Background = new SolidColorBrush(_hoveredBackground);
                _hoveredButton = g;
            }
        }

        void homeButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid)
            {
                Grid g = sender as Grid;
                g.Background = new SolidColorBrush(_normalBackground);
                //_hoveredButton = null;
            }
        }

        public IList<object> MruSelection
        {
            set
            {
                _mruSelection = value;
                _selectionOptionButton.Visibility = _mruSelection != null && _mruSelection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void GetFlight()
        {
            Analytics.Trace("KTHomePage.GetFlight");
            try
            {
                string os = "4444";
                if (Dx.FontExists("Segoe MDL2 Assets"))
                {
                    os = "8444";
                }
                string url = string.Format("http://drawingboardapp.com/json/flight?x={0}&y={1}", os, DateTime.Now.ToFileTime());

                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Stream stream = response.GetResponseStream();
                        StreamReader sr = new StreamReader(stream);
                        string json = sr.ReadToEnd();
                        JObject job = JObject.Parse(json);
                        string flight = (string)job["flight"];

                        WriteSettingsEntry("flight", flight);
                    }
                    else
                    {
                        WriteSettingsEntry("flight", "");
                    }
                }
            }
            catch (Exception e)
            {
                WriteSettingsEntry("flight", "");
                Analytics.ReportError("json response error", e, 4, 601);
            }
        }

        private void InitializeSettings()
        {
            Analytics.Trace("KTHomePage.InitializeSettings");
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Containers.ContainsKey("home"))
            {
                _homeSettings = localSettings.Containers["home"];
                try
                {
                    // value = (type) _homeSettings.Values[key];
                }
                catch
                {
                }
            }
            else
            {
                try
                {
                    _homeSettings = localSettings.CreateContainer("home", ApplicationDataCreateDisposition.Always);
                    _firstRun = true;

                    GetFlight();
                }
                catch
                {
                }
            }

            if (Globals.WindowsVersion >= 10 && localSettings.Containers.ContainsKey("application"))
            {
                ApplicationDataContainer appSettings = localSettings.Containers["application"];
                if (appSettings.Values.ContainsKey("font_size"))
                {
                    object o = appSettings.Values["font_size"];
                    if (o is int)
                    {
                        if ((int)o != Globals.UIDataContext.Size)
                        {
                            Globals.UIDataContext.Size = (int)o;
                        }
                    }
                }
            }
        }

        public void WriteSettingsEntry(string key, string value)
        {
            if (_homeSettings != null)
            {
                try
                {
                    _homeSettings.Values[key] = value;
                }
                catch
                {
                }
            }
        }

        public void ClearSettingsEntry(string key)
        {
            if (_homeSettings != null)
            {
                try
                {
                    _homeSettings.Values[key] = null;
                }
                catch
                {
                }
            }
        }

        public string GetSettingsValue(string key)
        {
            string setting = null;

            if (_homeSettings != null)
            {
                try
                {
                    setting = (string)_homeSettings.Values[key];
                }
                catch
                {
                }
            }

            return setting;
        }

        public async Task<double> GetVirtualScaleFactorAsync()
        {
            Analytics.Trace("KTHomePage.GetVirtualScaleFactorAsync");
            double factor = 1;
            try
            {
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();

                await renderTargetBitmap.RenderAsync(_layoutRoot);

                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
                var width = renderTargetBitmap.PixelWidth;
                var height = renderTargetBitmap.PixelHeight;

                factor = width / _layoutRoot.ActualWidth;
            }
            catch (Exception ex)
            {
                Analytics.ReportError("KTHomePage:GetVirtualScaleFactorAsync", ex, 4, 602);
            }

            return factor;
        }

        private async void SelectionButton_Click(object sender, RoutedEventArgs e)
        {
#if SIBERIA
            switch ((string)((Button)sender).Tag)
            {
                case "delete":
                    {
                        var resourceLoader = new ResourceLoader();
                        string confirm = resourceLoader.GetString("AlertConfirmDelete");
                        string delete = resourceLoader.GetString("AlertDelete");
                        string cancel = resourceLoader.GetString("AlertCancel");
                        string title = resourceLoader.GetString("AlertConfirmDeleteTitle");

                        var messageDialog = new MessageDialog(confirm, title);

                        messageDialog.Commands.Add(new UICommand(delete, null, "delete"));
                        messageDialog.Commands.Add(new UICommand(cancel, null, "cancel"));

                        messageDialog.DefaultCommandIndex = 1;  // Default command index
                        messageDialog.CancelCommandIndex = 1;   // Cancel index

                        IUICommand command = await messageDialog.ShowAsync();

                        if ((string)command.Id == "delete")
                        {
                            foreach (RecentDrawingItem item in _mruSelection)
                            {
                                try
                                {
                                    StorageFile mrufile = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(item.Token);
                                    await FileHandling.RemoveFileFromMruAsync(item.Token);

                                    await mrufile.DeleteAsync();
                                }
                                catch (Exception ex)
                                {
                                    Analytics.ReportError("Home page: delete drawing", ex, 4, 603);
                                }
                            }

                            // Recreating the GalleryControl does a more pleasing animation
                            // than just updating the contents of the GridView
                            //Child = new GalleryControl(this);
                            await _recentDrawingsControl.UpdateMruList();

                            MruSelection = null;
                        }
                        else if ((string)command.Id == "cancel")
                        {
                        }
                    }
                    break;

                case "remove":
                    if (_mruSelection != null && _mruSelection.Count > 0)
                    {
                        foreach (RecentDrawingItem item in _mruSelection)
                        {
                            await FileHandling.RemoveFileFromMruAsync(item.Token);
                        }

                        MruSelection = null;

                        // Recreating the GalleryControl does a more pleasing animation
                        // than just updating the contents of the GridView
                        //Child = new GalleryControl(this);
                        await _recentDrawingsControl.UpdateMruList();
                    }
                    break;

                case "clearselection":
                    _recentDrawingsControl.ClearSelection();
                    break;
            }
#endif
        }

        private void _ttHomeUxPreview_ActionButtonClick(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
        {
            Analytics.ReportEvent("reddog-intro", new Dictionary<string, string> { { "choice", "modern" } });

            _ttHomeUxPreview.IsOpen = false;

            //_appBar.IsOpen = true;
            Globals.UIVersion = 1;
            WriteSettingsEntry("uiversion", "1");

            //_bottomAppBar.ShowUXTeachingTip();
        }

        private void _ttHomeUxPreview_CloseButtonClick(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
        {
            Analytics.ReportEvent("reddog-intro", new Dictionary<string, string> { { "choice", "legacy" } });

            //_appBar.IsOpen = true;
            Globals.UIVersion = 0;
            WriteSettingsEntry("uiversion", "0");

            //_bottomAppBar.ShowUXTeachingTip();
        }

        private async void _facebook_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("facebook", new Dictionary<string, string> { { "source", "homepage" } });

            await Launcher.LaunchUriAsync(new Uri("https://www.facebook.com/backtothedrawingboardapp"));
        }

        private void _ttHomeUxPreview_Closed(Microsoft.UI.Xaml.Controls.TeachingTip sender, Microsoft.UI.Xaml.Controls.TeachingTipClosedEventArgs args)
        {
        }
    }
}
