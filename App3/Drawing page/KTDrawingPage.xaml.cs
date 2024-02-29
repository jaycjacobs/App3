//#define debugCoordDisplay
#define HZ_SPLITTER

using App3;
using Cirros;
using Cirros.Alerts;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using Cirros8;
//using Cirros8.ModalDialogs;
using Cirros8.Popup_Panels;
using CirrosUI;
using CirrosUWP.HUIApp;
using HUI;
using KT22.Console;
using KT22.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using RedDog;
using RedDog.Console;
using RedDog.Drawing_page;
using RedDog.HUIApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.ViewManagement;

namespace KT22
{
    public sealed partial class KTDrawingPage : Page, IShareablePage, IDrawingPage
    {
#if debugCoordDisplay
        TextBlock _debugText, _debugText1;
#endif
        ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        ApplicationDataContainer _appSettings;

        public KTDrawingPage()
        {
            this.InitializeComponent();

            _hui.OnHUICommandChanged += _hui_OnHUICommandChanged;

#if debugCoordDisplay
            _debugText = new TextBlock()
            {
                FontSize = 18,
                IsHitTestVisible = false,
                Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red)
            };
            _debugText.SetValue(Grid.ColumnProperty, 1);
            _layoutRoot.Children.Add(_debugText);

            _debugText1 = new TextBlock()
            {
                FontSize = 18,
                IsHitTestVisible = false,
                Margin = new Thickness(0,20,0,0),
                Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Blue)
            };
            _debugText1.SetValue(Grid.ColumnProperty, 1);
            _layoutRoot.Children.Add(_debugText1);

            this.PointerMoved += DrawingPage_PointerMoved;
#endif
            SetUIFontSize();

            try
            {
                this.Loaded += KTDrawingPage_Loaded;
                this.Unloaded += DrawingPage_Unloaded;

                _splitter.PointerEntered += _splitter_PointerEntered;
                _splitter.PointerExited += _splitter_PointerExited;
                _splitter.PointerPressed += _splitter_PointerPressed;
                _layoutRoot.PointerMoved += _splitter_PointerMoved;
                _layoutRoot.PointerReleased += _splitter_PointerReleased;
                _layoutRoot.PointerExited += _layoutRoot_PointerExited;

                Globals.Events.OnCoordinateDisplay += Events_OnCoordinateDisplay;
                Globals.Events.OnOptionsChanged += Events_OnOptionsChanged;
                Globals.Events.OnPointerEnteredDrawingArea += Events_OnPointerEnteredDrawingArea;
                Globals.Events.OnPointerLeftDrawingArea += Events_OnPointerLeftDrawingArea;

                Globals.Events.OnShowContextMenu += Events_OnShowContextMenu;
                Globals.Events.OnShowRulers += Events_OnShowRulers;
                Globals.Events.OnAttributesListChanged += Status_OnAttributesListsChanged;
                Globals.Events.OnDrawingCanvasLoaded += Status_OnDrawingCanvasLoaded;
                Globals.Events.OnDrawingLoading += Events_OnDrawingLoading;
                Globals.Events.OnDrawingLoaded += Events_OnDrawingLoaded;
                Globals.Events.OnDrawingNameChanged += Events_OnDrawingNameChanged;
                Globals.Events.OnDrawingShouldClose += Events_OnDrawingShouldClose;
                Globals.Events.OnShowConstructionPoint += Events_OnShowConstructionPoint;
                Globals.Events.OnMeasure += Events_OnMeasure;
                Globals.Events.OnGroupInstantiated += Events_OnGroupInstantiated;
                Globals.Events.OnOriginChanged += Events_OnOriginChanged;
                Globals.Events.OnUndoStackChanged += Events_OnUndoStackChanged;
                Globals.Events.OnActiveLayerShown += Events_OnActiveLayerShown;
                Globals.Events.OnEditImage += Events_OnEditImage;
                Globals.Events.OnCommandFinished += Events_OnCommandFinished;
                Globals.Events.OnShowMenu += Events_OnShowMenu;
                Globals.Events.OnCoordinateDisplay += Events_OnCoordinateDisplay;

                DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested += dataTransferManager_DataRequested;

                this.SizeChanged += DrawingPage_SizeChanged;

                _statusBar.PointerEntered += _statusBar_PointerEntered;
                _statusBar.PointerExited += _statusBar_PointerExited;

                this.KeyDown += KTDrawingPage_KeyDown;
                this.KeyUp += KTDrawingPage_KeyUp;

                AddPrintHandlers();
            }
            catch (Exception ex)
            {
                Analytics.ReportError("Failed to initialize DrawingPage", ex, 1, 709);
                App.Navigate(typeof(KTHomePage), null);
            }
        }

        private void KTDrawingPage_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void KTDrawingPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SetUIFontSize()
        {
            _menuIconColumnWidth.Width = HGlobals.DataContext.MenuIconLength;

            double scale = HGlobals.DataContext.UIFontSizeNormal / 16;

            _absXCoord.MinWidth = 120 * scale;
            _absYCoord.MinWidth = 120 * scale;
            _layerName.MinWidth = 150 * scale;

            _statusBarRow.Height = new GridLength(HGlobals.DataContext.MenuIconSize.Height);
        }

        private void _statusBar_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _coordButton.IsEnabled = false;
        }

        private void _statusBar_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _coordButton.IsEnabled = Globals.CommandProcessor != null && _hui.SubMenuIsExpanded == false;
        }

        private void Events_OnPointerEnteredDrawingArea(object sender, EventArgs e)
        {
            _coordButton.IsEnabled = true;
        }

        private void Events_OnPointerLeftDrawingArea(object sender, EventArgs e)
        {
            _coordButton.IsEnabled = false;
        }

        private async void KTDrawingPage_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = HGlobals.DataContext;

            await ConsoleUtilities.InitializeTeachingTips();

            if (Content is FrameworkElement fe)
            {
                ConsoleUtilities.PopulateTeachingTips(this.Content as FrameworkElement);
            }
        }

        Unit _modelUnit = Unit.Millimeters;
        bool _architect = false;

        private void Events_OnOptionsChanged(object sender, EventArgs e)
        {
            if (Globals.ActiveDrawing != null)
            {
                _modelUnit = Globals.ActiveDrawing.ModelUnit;
                _architect = Globals.ActiveDrawing.IsArchitecturalScale && Globals.Input.GridSnap;
            }
        }

        private void Events_OnCoordinateDisplay(object sender, CoordinateDisplayEventArgs e)
        {
            Point model = Globals.ActiveDrawing.PaperToModel(e.Point);
            int round = _architect ? 128 : Globals.DimensionRound;

            _absXCoord.Text = string.Format("x:{0,-8}", Utilities.FormatDistance(model.X, round, _architect, true, _modelUnit, false, 4));
            _absYCoord.Text = string.Format("y:{0,-8}", Utilities.FormatDistance(model.Y, round, _architect, true, _modelUnit, false, 4));

            /*
            switch (e.CoordinateDisplayType)
            {
                case CoordinateDisplayType.coordinate:
                    _absCoord.Visibility = Visibility.Visible;
                    //_relCoord.Visibility = Visibility.Collapsed;
                    //_polCoord.Visibility = Visibility.Collapsed;
                    break;
                case CoordinateDisplayType.vector:
                    //Point delta = Globals.ActiveDrawing.PaperToModelDelta(new Point(e.Width, e.Height));
                    //_relCoord.Text = string.Format("dx:{0,-8} dy:{1,-8}",
                    //    Utilities.FormatDistance(delta.X, round, _architect, true, _modelUnit, false, 4),
                    //    Utilities.FormatDistance(delta.Y, round, _architect, true, _modelUnit, false, 4));
                    //_polCoord.Text = string.Format("d:{0,-8} a:{1,-8}",
                    //    Utilities.FormatDistance(Globals.ActiveDrawing.PaperToModel(e.Distance), round, _architect, true, _modelUnit, false, 4),
                    //    string.Format("{0}°", -Math.Round(e.Angle * Construct.cRadiansToDegrees, 3)));
                    //_absCoord.Visibility = Visibility.Visible;
                    //_relCoord.Visibility = Visibility.Visible;
                    //_polCoord.Visibility = Visibility.Visible;
                    break;
                case CoordinateDisplayType.size:
                    break;
            }
            */
        }

        private void Events_OnShowMenu(object sender, ShowMenuEventArgs e)
        {
            Globals.Events.PointAdded(Globals.workCanvas.CursorLocation, "enter");

            _hui.ShowMenu(e.Show);
        }

        private void Events_OnCommandFinished(object sender, CommandFinishedEventArgs e)
        {
            if (e.Key == "enter")
            {
                Globals.CommandDispatcher.ActiveCommand = Cirros.Commands.CommandType.none;
                Globals.Input.SelectCursor(CursorType.Arrow);

#if KT22
                _commandPanel.Activate();
#endif
            }
        }

        Rectangle _splitterBar = null;
        bool _cursorIsInSplitter = false;

        void _splitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Point location = e.GetCurrentPoint(_layoutRoot).Position;

            _splitterBar = new Rectangle();
            _splitterBar.Height = _splitter.ActualHeight;
            _splitterBar.Width = _splitter.ActualWidth;
            _splitterBar.StrokeThickness = .01;
            _splitterBar.Fill = new SolidColorBrush(Colors.Gray);
            _splitterBar.Stroke = new SolidColorBrush(Colors.Gray);
#if HZ_SPLITTER
            _splitterBar.VerticalAlignment = VerticalAlignment.Top;
            _splitterBar.SetValue(Grid.ColumnProperty, 1);
            _splitterBar.SetValue(Grid.RowProperty, 0);
            _splitterBar.SetValue(Grid.RowSpanProperty, 3);
            _splitterBar.Margin = new Thickness(0, location.Y - _splitter.ActualHeight / 2, 0, 0);
#else
            _splitterBar.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
            _splitterBar.SetValue(Grid.ColumnProperty, 0);
            _splitterBar.SetValue(Grid.ColumnSpanProperty, 4);
            _splitterBar.Margin = new Thickness(location.X - _splitter.ActualWidth / 2, 0, 0, 0);
#endif
            _layoutRoot.Children.Add(_splitterBar);
        }

        void _splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_splitterBar != null)
            {
                _layoutRoot.Children.Remove(_splitterBar);
                _splitterBar = null;

                Point location = e.GetCurrentPoint(_layoutRoot).Position;
#if HZ_SPLITTER
                double height = _layoutRoot.ActualHeight - location.Y - 30 - _splitter.ActualHeight / 2;
                _commandPaneRow.Height = new GridLength(height > 60 ? height : height > 40 ? 60 : 0);
#else
                double width = _layoutRoot.ActualWidth - location.X - _splitter.ActualWidth / 2;
                _commandPaneColumn.Width = new GridLength(width > 100 ? width : 0);
#endif
            }
            if (_cursorIsInSplitter)
            {
                _cursorIsInSplitter = false;
                _splitter.Background = new SolidColorBrush(Colors.Black);
            }
        }

        void _splitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_cursorIsInSplitter == false)
            {
                _cursorIsInSplitter = true;
                _splitter.Background = new SolidColorBrush(Colors.DarkGray);
            }
        }

        void _splitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_splitterBar == null)
            {
                _cursorIsInSplitter = false;
                _splitter.Background = new SolidColorBrush(Colors.Black);
            }
        }

        void _layoutRoot_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_splitterBar != null)
            {
                _layoutRoot.Children.Remove(_splitterBar);
                _splitterBar = null;

                Point location = e.GetCurrentPoint(_layoutRoot).Position;
#if HZ_SPLITTER
                double height = _layoutRoot.ActualHeight - location.Y - _splitter.ActualHeight / 2;
                _commandPaneRow.Height = new GridLength(height > 40 ? height : 0);
#else
                double width = _layoutRoot.ActualWidth - location.X - _splitter.ActualWidth / 2;
                _commandPaneColumn.Width = new GridLength(width > 40 ? width : 0);
#endif
            }
            if (_cursorIsInSplitter)
            {
                _cursorIsInSplitter = false;
                _splitter.Background = new SolidColorBrush(Colors.Black);
            }
        }

        void _splitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_splitterBar != null)
            {
                Point location = e.GetCurrentPoint(_layoutRoot).Position;
#if HZ_SPLITTER
                _splitterBar.Margin = new Thickness(0, location.Y - _splitter.ActualHeight / 2, 0, 0);
#else
                _splitterBar.Margin = new Thickness(location.X - _splitter.ActualWidth / 2, 0, 0, 0);
#endif
            }
        }

        async void _hui_OnHUICommandChanged(object sender, HUICommandChangedEventArgs e)
        {
            await ExecuteCommand(e.Id, e.Options);
        }

        async Task ExecuteCommand(string id, Dictionary<string, object> options)
        {
#if VERBOSE && DEBUG
            if (options != null)
            {
                string pre = "GS ";
                foreach (string key in options.Keys)
                {
                    _commandPanel.PrintResult(string.Format("{0}{1,-20}: {2}", pre, key, options[key]));
                    pre = "   ";
                }
            }
#endif

            switch (id)
            {
                case "home":
                    await GoHome();
                    break;

                default:
                    string error = await KTCommandProcessor.CommandProcessor.Execute(options);
                    if (error != null)
                    {
                        System.Diagnostics.Debug.WriteLine("hui error: {0}", error);
                    }
                    break;
            }
        }
        private async Task GoHome()
        {
            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.Finish();
            }
            if (_autoSaveChangeNumber < Globals.ActiveDrawing.ChangeNumber)
            {
                if (await FileHandling.SaveStateAsync())
                {
                    _autoSaveChangeNumber = Globals.ActiveDrawing.ChangeNumber;
                }
            }
            if (await Cirros.Alerts.StandardAlerts.ClearWarningAsync())
            {
                Globals.ActiveDrawing.IsModified = false;

                if (ApplicationData.Current.LocalSettings.Containers.Keys.Contains("drawing"))
                {
                    ApplicationDataContainer drawingSettings = ApplicationData.Current.LocalSettings.Containers["drawing"];
                    drawingSettings.Values["unsaved"] = false;
                }
                App.Navigate(typeof(KTHomePage), null);
            }
        }

        private void Events_OnDrawingNameChanged(object sender, DrawingLoadedEventArgs e)
        {
            if (e.Name != null && e.Name.EndsWith("dbfx"))
            {
                // TODO Windows.UI.ViewManagement.ApplicationView is no longer supported. Use Microsoft.UI.Windowing.AppWindow instead. For more details see https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                App.Window.Title = e.Name.Replace(".dbfx", "");
            }
            else
            {
                // TODO Windows.UI.ViewManagement.ApplicationView is no longer supported. Use Microsoft.UI.Windowing.AppWindow instead. For more details see https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                App.Window.Title = "";
            }
        }

        void Events_OnDrawingShouldClose(object sender, DrawingShouldCloseEventArgs e)
        {
            App.Navigate(typeof(KTHomePage), e.Reason);
        }

        async void Events_OnShowAlert(object sender, ShowAlertEventArgs e)
        {
            var resourceLoader = new ResourceLoader();

            if (e.AlertId == "NoImageSelected")
            {
                string message = resourceLoader.GetString("NoImageSelected");
                string title = resourceLoader.GetString("NoImageSelectedTitle");

                await StandardAlerts.SimpleAlertAsync(title, message);
            }
            else if (e.AlertId == "NoCamera")
            {
                string message = resourceLoader.GetString("NoCamera");
                string title = resourceLoader.GetString("NoCameraTitle");

                await StandardAlerts.SimpleAlertAsync(title, message);
            }
        }

        void Events_OnEditImage(object sender, EditImageEventArgs e)
        {
            if (e.PImage != null)
            {
                if (Globals.CommandProcessor != null)
                {
                    // edit existing image
                    ShowPhotoDialog(e.PImage);
                }
            }
            else
            {
                ShowPhotoDialog(e.ImageId, e.SourceName);
            }
        }

        async void Events_OnActiveLayerShown(object sender, ActiveLayerEventArgs e)
        {
            string layerName = "unknown";

            if (Globals.LayerTable.ContainsKey(e.Layer))
            {
                layerName = Globals.LayerTable[e.Layer].Name;
            }

            var resourceLoader = new ResourceLoader();
            string format = resourceLoader.GetString("AlertShowingActiveLayerFormat");
            string message = string.Format(format, layerName);
            string title = resourceLoader.GetString("AlertShowingActiveLayerProblem");

            await StandardAlerts.SimpleAlertAsync(title, message);
        }

        public void ShowExportDialog()
        {
            Cirros8.Export.ExportPage page = new Cirros8.Export.ExportPage();

            _modalDialogPopup.Child = page;
            _modalDialogPopup.IsLightDismissEnabled = true;

            _modalDialogPopup.IsOpen = true;
            _modalDialogPopup.Visibility = Visibility.Visible;
        }

        public Panel ZoomOverlaySource
        {
            get
            {
                return _drawingRoot;
            }
        }

        public Point ZoomOverlaySourceOffset
        {
            get
            {
                return new Point(_yRulerColumn.Width.Value, _xRulerRow.Height.Value);
            }
        }

        public Canvas ZoomOverlayTarget
        {
            get
            {
                return _zoomOverlayTarget;
            }
        }

        public Canvas WorkCanvasTarget
        {
            get
            {
                return _workArea;
            }
        }

        public Canvas ToolsOverlay
        {
            get
            {
                return _toolsOverlay;
            }
        }

        public Grid DrawingToolsTools
        {
            get
            {
                return _drawingToolsTools;
            }
        }

        private bool _shareHandlerInstalled = false;

        private void AddShareHandler()
        {
            if (_shareHandlerInstalled == false)
            {
                try
                {
                    DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                    dataTransferManager.DataRequested += dataTransferManager_DataRequested;
                    _shareHandlerInstalled = true;
                }
                catch
                {

                }
            }
        }

        public void RemoveShareHandler()
        {
            if (_shareHandlerInstalled)
            {
                DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested -= dataTransferManager_DataRequested;
                _shareHandlerInstalled = false;
            }
        }

        void dataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            DataPackage requestData = request.Data;

            request.Data.Properties.Title = string.IsNullOrEmpty(FileHandling.CurrentDrawingName) ? "Untitled Drawing" : FileHandling.CurrentDrawingName.Replace(".dbfx", "");

            request.Data.SetDataProvider(StandardDataFormats.Html,
                providerRequest => OnHtmlRequested(providerRequest, request.Data, request.Data.Properties.Title));

            request.Data.Properties.FileTypes.Add(StandardDataFormats.StorageItems);
            request.Data.SetDataProvider(StandardDataFormats.StorageItems, providerRequest => OnFileRequested(providerRequest, request.Data.Properties.Title));
        }

        private async static void OnHtmlRequested(DataProviderRequest request, DataPackage requestData, string title)
        {
            var dataProviderDeferral = request.GetDeferral();
            try
            {
                string filename = "drawing.png";
                string format = "<div><img src='{0}'/></div><div>{1}</div>";
                string html = String.Format(format, filename, title);

                request.SetData(HtmlFormatHelper.CreateHtmlFormat(html));
                requestData.ResourceMap[filename] = await GetImageStreamRef(600, filename);
            }
            finally
            {
                dataProviderDeferral.Complete();
            }
        }

        private async static void OnFileRequested(DataProviderRequest request, string title)
        {
            string filename = title + ".png";

            var dataProviderDeferral = request.GetDeferral();
            try
            {
                StorageFile tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                await Cirros.Core.Dx.ExportDrawingToImageFileAsync(tempFile, 1024);

                var files = new List<IStorageItem> { tempFile };
                request.SetData(files);
            }
            finally
            {
                dataProviderDeferral.Complete();
            }
        }

        private static async Task<RandomAccessStreamReference> GetImageStreamRef(int size, string filename)
        {
            StorageFile tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

            await Cirros.Core.Dx.ExportDrawingToImageFileAsync(tempFile, size);

            return RandomAccessStreamReference.CreateFromFile(tempFile);
        }

        void Events_OnUndoStackChanged(object sender, EventArgs e)
        {
            //EnableCommonButtons();
        }

        Popup _groupAttributesPopup = null;
        Popup _infoPopup = null;
        MeasureType _measurePopupType = MeasureType.none;

        void Events_OnOriginChanged(object sender, OriginChangedEventArgs e)
        {
            if (e.Finished)
            {
                if (_infoPopup != null)
                {
                    _infoPopup.IsOpen = false;
                    _infoPopup = null;
                }
            }
            else
            {
                //OriginPanel panel;

                //if (_infoPopup == null)
                //{
                //    _infoPopup = new Popup();
                //    _infoPopup.Child = panel = new OriginPanel();
                //    _infoPopup.IsOpen = true;
                //}
                //else
                //{
                //    panel = _infoPopup.Child as OriginPanel;
                //}

                //if (panel is OriginPanel)
                //{
                //    GeneralTransform tf = _workArea.TransformToVisual(null);
                //    panel.WorkCanvasOffset = tf.TransformPoint(new Point());
                //    panel.ShowOrigin(e.Origin);
                //}
            }
        }

        void Events_OnMeasure(object sender, MeasureEventArgs e)
        {
            if (e.MeasureType == MeasureType.none)
            {
                if (_infoPopup != null)
                {
                    _infoPopup.IsOpen = false;
                    _infoPopup = null;
                }
            }
            else
            {
                if (e.MeasureType != _measurePopupType)
                {
                    if (_infoPopup != null)
                    {
                        _infoPopup.IsOpen = false;
                        _infoPopup = null;
                    }

                    _measurePopupType = e.MeasureType;
                }

                switch (e.MeasureType)
                {
                    case MeasureType.distance:
                        if (e.Points.Count == 2)
                        {
                            Point from = e.Points[0];
                            Point to = e.Points[1];

                            DistancePanel panel;

                            if (_infoPopup == null)
                            {
                                _infoPopup = new Popup();
                                _infoPopup.Child = panel = new DistancePanel();
                                _infoPopup.IsOpen = true;
                            }
                            else
                            {
                                panel = _infoPopup.Child as DistancePanel;
                            }

                            if (panel is DistancePanel && e.Points.Count == 2)
                            {
                                GeneralTransform tf = _workArea.TransformToVisual(null);
                                panel.WorkCanvasOffset = tf.TransformPoint(new Point());
                                panel.ShowDistance(from, to);
                            }
                        }
                        break;

                    case MeasureType.angle:
                        if (e.Points.Count == 3)
                        {
                            AnglePanel panel;

                            if (_infoPopup == null)
                            {
                                _infoPopup = new Popup();
                                _infoPopup.Child = panel = new AnglePanel();
                                _infoPopup.IsOpen = true;
                            }
                            else
                            {
                                panel = _infoPopup.Child as AnglePanel;
                            }

                            if (panel is AnglePanel)
                            {
                                GeneralTransform tf = _workArea.TransformToVisual(null);
                                panel.WorkCanvasOffset = tf.TransformPoint(new Point());
                                panel.ShowAngle(e.Points);
                            }
                        }
                        break;

                    case MeasureType.area:
                        if (e.Points.Count > 2)
                        {
                            AreaPanel panel;

                            if (_infoPopup == null)
                            {
                                _infoPopup = new Popup();
                                _infoPopup.Child = panel = new AreaPanel();
                                _infoPopup.IsOpen = true;
                            }
                            else
                            {
                                panel = _infoPopup.Child as AreaPanel;
                            }

                            if (panel is AreaPanel)
                            {
                                GeneralTransform tf = _workArea.TransformToVisual(null);
                                panel.WorkCanvasOffset = tf.TransformPoint(new Point());
                                panel.ShowArea(e.Points);
                            }
                        }
                        break;
                }
            }
        }

        void Events_OnGroupInstantiated(object sender, GroupInstantiatedEventArgs e)
        {
            if (e.Finished)
            {
                if (_groupAttributesPopup != null)
                {
                    _groupAttributesPopup.IsOpen = false;
                    _groupAttributesPopup = null;
                }
            }
            else
            {
                GroupAttributesPanel panel;

                if (_groupAttributesPopup == null)
                {
                    _groupAttributesPopup = new Popup();
                    _groupAttributesPopup.Child = panel = new GroupAttributesPanel();
                    _groupAttributesPopup.IsOpen = true;
                }
                else
                {
                    panel = _groupAttributesPopup.Child as GroupAttributesPanel;
                }

                if (panel is GroupAttributesPanel)
                {
                    GeneralTransform tf = _workArea.TransformToVisual(null);
                    panel.WorkCanvasOffset = tf.TransformPoint(new Point());
                    panel.Show(e.Instance);
                }
            }
        }

        Hint _hint = null;

        void Events_OnShowConstructionPoint(object sender, ShowConstructionPointEventArgs e)
        {
            if (_hint == null)
            {
                _hint = new Hint();
                _workArea.Children.Add(_hint);
            }

            _hint.Show(e.Tag, Globals.View.PaperToDisplay(e.Location));
        }

        void Events_OnShowRulers(object sender, ShowRulersEventArgs e)
        {
            ShowRulers(e.Show);
        }

        public void FinalizeSettings()
        {
            if (_appSettings != null)
            {
                try
                {
                    _appSettings.Values["controlpanel_visible"] = Globals.ShowControlPanel;
                    _appSettings.Values["rulers_visible"] = Globals.ShowRulers;
                    _appSettings.Values["triangle_visible"] = Globals.ShowDrawingTools;
                    _appSettings.Values["cursor_size"] = Globals.Input.CursorSize;
                    _appSettings.Values["grid_intensity"] = Globals.GridIntensity;
                    _appSettings.Values["pinch_zoom"] = Globals.EnablePinchZoom;

                    // new as of 2.3
                    _appSettings.Values["enable_touch_magnifier"] = Globals.EnableTouchMagnifer;
                    _appSettings.Values["enable_stylus_magnifier"] = Globals.EnableStylusMagnifer;
                    _appSettings.Values["mouse_pan_button"] = Globals.MousePanButton.ToString();


                    // new as of 4.1
                    _appSettings.Values["dimround_architect"] = Globals.DimensionRoundArchitectDefault;
                    _appSettings.Values["dimround_engineer"] = Globals.DimensionRoundEngineerDefault;
                    _appSettings.Values["dimround_metric"] = Globals.DimensionRoundMetricDefault;

                    _appSettings.Values["huidialog_firstrun"] = RedDogGlobals.HUIDialogFirstRun;
                    _appSettings.Values["reddog_firstrun"] = RedDogGlobals.RedDogFirstRun;

                    _appSettings.Values["highlight_color"] = Utilities.ColorSpecFromColor(Globals.HighlightColor);

                    // new as of 4.2
                    _appSettings.Values["coordinate_mode"] = Globals.CoordinateMode.ToString();
                }
                catch
                {
                }

                _appSettings = null;
            }
        }

        void DrawingPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Globals.Reset();    // This call was moved from OnNavigatedFrom()

#if debugCoordDisplay
            this.PointerMoved -= DrawingPage_PointerMoved;
#endif
            if (_autoSaveTimer != null)
            {
                _autoSaveTimer.Stop();
                _autoSaveTimer = null;
            }

            RemovePrintHandlers();

            Globals.Events.OnCoordinateDisplay -= Events_OnCoordinateDisplay;
            Globals.Events.OnOptionsChanged -= Events_OnOptionsChanged;
            Globals.Events.OnPointerEnteredDrawingArea -= Events_OnPointerEnteredDrawingArea;
            Globals.Events.OnPointerLeftDrawingArea -= Events_OnPointerLeftDrawingArea;

            Globals.Events.OnShowContextMenu -= Events_OnShowContextMenu;
            Globals.Events.OnShowAlert -= Events_OnShowAlert;
            Globals.Events.OnShowRulers -= Events_OnShowRulers;
            Globals.Events.OnAttributesListChanged -= Status_OnAttributesListsChanged;
            Globals.Events.OnDrawingCanvasLoaded -= Status_OnDrawingCanvasLoaded;
            Globals.Events.OnDrawingLoading -= Events_OnDrawingLoading;
            Globals.Events.OnDrawingLoaded -= Events_OnDrawingLoaded;
            Globals.Events.OnDrawingShouldClose -= Events_OnDrawingShouldClose;
            Globals.Events.OnShowConstructionPoint -= Events_OnShowConstructionPoint;
            Globals.Events.OnMeasure -= Events_OnMeasure;
            Globals.Events.OnGroupInstantiated -= Events_OnGroupInstantiated;
            Globals.Events.OnOriginChanged -= Events_OnOriginChanged;
            Globals.Events.OnUndoStackChanged -= Events_OnUndoStackChanged;
            Globals.Events.OnActiveLayerShown -= Events_OnActiveLayerShown;
            Globals.Events.OnEditImage -= Events_OnEditImage;
            Globals.Events.OnCommandFinished -= Events_OnCommandFinished;
            Globals.Events.OnShowMenu -= Events_OnShowMenu;
            Globals.Events.OnCoordinateDisplay -= Events_OnCoordinateDisplay;
        }

        void workArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize == e.NewSize) return;
            if (Cirros.Utility.Utilities.__checkSizeChanged(53, sender, true)) return;

            RectangleGeometry rg = new RectangleGeometry();
            rg.Rect = new Rect(0, 0, _workArea.ActualWidth, _workArea.ActualHeight);
            _workArea.Clip = rg;
            Globals.View.ViewPortSize = new Size(_workArea.ActualWidth, _workArea.ActualHeight);
        }

#if debugCoordDisplay
        void DrawingPage_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Point p = e.GetCurrentPoint(Globals.DrawingCanvas).RawPosition;
            p = Globals.View.DisplayToPaper(p);
            _debugText.Text = string.Format("{0}, {1}", p.X, p.Y);
            //_debugText.Text = string.Format("{0}, {1}", System.Math.Round(p.X), System.Math.Round(p.Y));
            Point p1 = e.GetCurrentPoint(_layoutRoot).RawPosition;
            _debugText1.Text = string.Format("{0}, {1}", System.Math.Round(p1.X), System.Math.Round(p1.Y));
        }
#endif

        void DrawingPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"{e.NewSize.Width}, {e.NewSize.Height}");
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Analytics.CountUsageEvent("session");
            Analytics.TrackPageView("KTDrawingPage");
            Analytics.ReportEvent("drawing-page-loaded", new Dictionary<string, string>
            {
                { "window-width", App.Window.Bounds.Width.ToString() },
                { "window-height", App.Window.Bounds.Height.ToString() },
                { "font-size", HGlobals.DataContext.UIFontSizeNormal.ToString() }
            });

            //if (await License.IsValid())
            {
                AddShareHandler();

                // Create the work canvas!
                Globals.workCanvas = new WorkCanvas(e.Parameter);
                Globals.workCanvas.SetValue(Grid.ColumnSpanProperty, 2);
                _workArea.Children.Add(Globals.workCanvas);

                Globals.View = Globals.workCanvas;
                Globals.Input = Globals.workCanvas;
                Globals.DrawingCanvas = Globals.workCanvas;

                Globals.DrawingTools = new DrawingTools(Globals.DrawingCanvas);
                Globals.CommandDispatcher = new CommandDispatcher();
                Globals.ActiveDrawing = new DrawingDocument();

                _workArea.SizeChanged += workArea_SizeChanged;

                Globals.workCanvas.OnWindowChanged += workCanvas_OnWindowChanged;
                Globals.workCanvas.Loaded += workCanvas_Loaded;

                _autoSaveTimer = new DispatcherTimer();
                _autoSaveTimer.Interval = new TimeSpan(0, 0, 5, 0, 0);
                _autoSaveTimer.Tick += _autoSaveTimer_Tick;
                _autoSaveTimer.Start();
            }
        }

        void workCanvas_OnWindowChanged(object sender, EventArgs e)
        {
            if (Globals.ShowRulers)
            {
                _horizontalRuler.Render();
                _verticalRuler.Render();
            }
        }

        public async Task ReplaceDrawing(object o)
        {
            if (o is string token)
            {
                if (await Cirros.Alerts.StandardAlerts.LastChanceToSaveAsync() == false)
                {
                    await FileHandling.LoadMruDrawingAsync(token);
                    Globals.View.DisplayAll();
                }
            }
            else if (o is StorageFile file)
            {
                if (await Cirros.Alerts.StandardAlerts.LastChanceToSaveAsync() == false)
                {
                    if (file.FileType == ".svg")
                    {
                        await FileHandling.ImportSvgAsync(file);
                    }
                    else if (file.FileType == ".dxf")
                    {
                        await FileHandling.ImportDxfAsync(file);
                    }
                    else
                    {
                        await FileHandling.LoadDrawingAsync(file);
                    }
                    Globals.View.DisplayAll();
                }
            }
        }

        DispatcherTimer _autoSaveTimer = null;

        private int _autoSaveChangeNumber = 0;

        public async Task AutoSave(bool force)
        {
            try
            {
                if (Globals.ActiveDrawing != null && (force || _autoSaveChangeNumber < Globals.ActiveDrawing.ChangeNumber))
                {
                    if (await FileHandling.SaveStateAsync())
                    {
                        _autoSaveChangeNumber = Globals.ActiveDrawing.ChangeNumber;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AutoSave exception: {0}", ex.Message);
            }
        }

        async void _autoSaveTimer_Tick(object sender, object e)
        {
            try
            {
                if (Globals.ActiveDrawing != null)
                {
                    if (_autoSaveChangeNumber < Globals.ActiveDrawing.ChangeNumber)
                    {
                        if (await FileHandling.SaveStateAsync())
                        {
                            _autoSaveChangeNumber = Globals.ActiveDrawing.ChangeNumber;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("_autoSaveTimer_Tick exception: {0}", ex.Message);
            }
        }

        private async Task LoadAppSettings()
        {
            if (_appSettings == null)
            {
                if (_localSettings.Containers.ContainsKey("app"))
                {
                    _appSettings = _localSettings.Containers["app"];
                    try
                    {
                        Globals.ShowControlPanel = (bool)_appSettings.Values["controlpanel_visible"];
                        Globals.ShowRulers = (bool)_appSettings.Values["rulers_visible"];
                        Globals.ShowDrawingTools = (bool)_appSettings.Values["triangle_visible"];
                        Globals.Input.CursorSize = (double)_appSettings.Values["cursor_size"];
                        Globals.GridIntensity = (double)_appSettings.Values["grid_intensity"];
                        Globals.EnablePinchZoom = (bool)_appSettings.Values["pinch_zoom"];

                        // new as of 2.3
                        Globals.EnableTouchMagnifer = (bool)_appSettings.Values["enable_touch_magnifier"];
                        Globals.EnableStylusMagnifer = (bool)_appSettings.Values["enable_stylus_magnifier"];

                        string s = (string)_appSettings.Values["mouse_pan_button"];
                        switch (s)
                        {
                            default:
                            case "Middle":
                                Globals.MousePanButton = Globals.MouseButtonType.Middle;
                                break;
                            case "Right":
                                Globals.MousePanButton = Globals.MouseButtonType.Right;
                                break;
                            case "Button1":
                                Globals.MousePanButton = Globals.MouseButtonType.Button1;
                                break;
                            case "Button2":
                                Globals.MousePanButton = Globals.MouseButtonType.Button2;
                                break;
                        }

                        // new as of 4.1
                        Globals.DimensionRoundArchitectDefault = (int)_appSettings.Values["dimround_architect"];
                        Globals.DimensionRoundEngineerDefault = (int)_appSettings.Values["dimround_engineer"];
                        Globals.DimensionRoundMetricDefault = (int)_appSettings.Values["dimround_metric"];

                        if (_appSettings.Values.ContainsKey("highlight_color"))
                        {
                            Globals.HighlightColor = Utilities.ColorFromColorSpec((uint)_appSettings.Values["highlight_color"]);
                        }

                        // new as of 4.2
                        if (_appSettings.Values.ContainsKey("coordinate_mode"))
                        {
                            s = (string)_appSettings.Values["coordinate_mode"];
                            switch (s)
                            {
                                case "Absolute":
                                    Globals.CoordinateMode = CoordinateMode.Absolute;
                                    break;
                                default:
                                case "Delta":
                                    Globals.CoordinateMode = CoordinateMode.Delta;
                                    break;
                                case "Polar":
                                    Globals.CoordinateMode = CoordinateMode.Polar;
                                    break;
                            }
                        }
#if DEBUG
#else
                        RedDogGlobals.HUIDialogFirstRun = true;     // will be true if next line generates an exception
                        RedDogGlobals.RedDogFirstRun = true;
                        RedDogGlobals.HUIDialogFirstRun = (bool)_appSettings.Values["huidialog_firstrun"];
                        RedDogGlobals.RedDogFirstRun = (bool)_appSettings.Values["reddog_firstrun"];
#endif
                        int count = _appSettings.Values.ContainsKey("ux1_count") ? (int)_appSettings.Values["ux1_count"] : 0;
                        _appSettings.Values["ux1_count"] = count + 1;
                    }
                    catch
                    {
                    }
                }
                else
                {
                    try
                    {
                        _appSettings = _localSettings.CreateContainer("app", ApplicationDataCreateDisposition.Always);
                        _appSettings.Values["controlpanel_visible"] = Globals.ShowControlPanel;
                        _appSettings.Values["rulers_visible"] = Globals.ShowRulers;
                        _appSettings.Values["triangle_visible"] = Globals.ShowDrawingTools;
                        _appSettings.Values["cursor_size"] = Globals.Input.CursorSize;
                        _appSettings.Values["grid_intensity"] = Globals.GridIntensity;
                        _appSettings.Values["pinch_zoom"] = Globals.EnablePinchZoom;

                        // new as of 2.3
                        _appSettings.Values["enable_touch_magnifier"] = Globals.EnableTouchMagnifer;
                        _appSettings.Values["enable_stylus_magnifier"] = Globals.EnableStylusMagnifer;
                        _appSettings.Values["mouse_pan_button"] = Globals.MousePanButton.ToString();

                        // new as of 4.1
                        _appSettings.Values["dimround_architect"] = Globals.DimensionRoundArchitectDefault;
                        _appSettings.Values["dimround_engineer"] = Globals.DimensionRoundEngineerDefault;
                        _appSettings.Values["dimround_metric"] = Globals.DimensionRoundMetricDefault;

                        _appSettings.Values["huidialog_firstrun"] = RedDogGlobals.HUIDialogFirstRun;
                        _appSettings.Values["reddog_firstrun"] = RedDogGlobals.RedDogFirstRun;

                        _appSettings.Values["ux1_count"] = (int)1;

                        _appSettings.Values["highlight_color"] = Utilities.ColorSpecFromColor(Globals.HighlightColor);

                        // new as of 4.2
                        _appSettings.Values["coordinate_mode"] = Globals.CoordinateMode;

                        RedDogGlobals.HUIDialogFirstRun = true;
                        RedDogGlobals.RedDogFirstRun = true;
                    }
                    catch
                    {
                    }
                }

                //if (_appSettings != null && _appSettings.Values.ContainsKey("reddog_firstrun") == false)
                if (RedDogGlobals.RedDogFirstRun)
                {
                    await Task.Delay(800);

                    if (_hui.CurrentDialog() == null)
                    {
                        _ttMainIntro.IsOpen = true;
                        _show_ttMainIntroCloseHint = true;

                        RedDogGlobals.RedDogFirstRun = false;
                        _appSettings.Values["reddog_firstrun"] = RedDogGlobals.RedDogFirstRun;
                    }
                    else
                    {

                    }
                }
            }
        }

        void workCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            //LoadAppSettings();

            ShowRulers(Globals.ShowRulers);
        }

        async void Events_OnDrawingLoaded(object sender, DrawingLoadedEventArgs e)
        {
            await LoadAppSettings();

            _workArea.Visibility = Visibility.Visible;
            _loadingProgressBar.ShowPaused = true;
            _loadingProgressBar.Visibility = Visibility.Collapsed;

            Globals.ActiveLayerId = Globals.LayerId;

            if (Globals.LayerTable.ContainsKey(Globals.ActiveLayerId))
            {
                // TODO: Set Globals.LayerId to Globals.ActiveLayerId when serializing drawing

                Layer layer = Globals.LayerTable[Globals.ActiveLayerId];
                _layerName.Text = layer.Name;
            }

            Globals.DrawingTools.ShowTriangle(Globals.ShowDrawingTools);
        }

        void Events_OnDrawingLoading(object sender, DrawingLoadingEventArgs e)
        {
            //if (e.Size > 500000)
            //{
            //    _workArea.Visibility = Visibility.Visible;
            //    _loadingProgressBar.Visibility = Visibility.Visible;
            //    _loadingProgressBar.ShowPaused = false;
            //}
        }

        void Status_OnDrawingCanvasLoaded(object sender, DrawingCanvasLoadedEventArgs e)
        {
            _workArea.Visibility = Visibility.Visible;

            if (e.Parameter is string && (string)e.Parameter == "restore")
            {
                _autoSaveChangeNumber = Globals.ActiveDrawing.ChangeNumber;
            }
        }

        void Status_OnAttributesListsChanged(object sender, System.EventArgs e)
        {
            // update layer menus in the UI
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            try
            {
                FinalizeSettings();

                Globals.ActiveDrawing.Clear();

                // This is probably too early to call Globals.Reset
                // This may be a cause of the handled exception reports on save state
                // Try moving to DrawingPage_Unloaded()

                await FileHandling.RemoveOphanThumbnails();
                await Utilities.PurgeTemporaryImages(new TimeSpan(7, 0, 0, 0));

                RemoveShareHandler();

                base.OnNavigatedFrom(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OnNavigatedFrom exception: {0}", ex.Message);
            }
        }

        private void ShowRulers(bool show)
        {
            Globals.ShowRulers = show;

            if (show)
            {
                _xRulerRow.Height = new GridLength(20);
                _yRulerColumn.Width = new GridLength(20);

                _horizontalRuler.Visibility = Visibility.Visible;
                _horizontalRuler.Render();

                _verticalRuler.Visibility = Visibility.Visible;
                _verticalRuler.Render();
            }
            else
            {
                _horizontalRuler.Visibility = Visibility.Collapsed;
                _verticalRuler.Visibility = Visibility.Collapsed;

                _xRulerRow.Height = new GridLength(0);
                _yRulerColumn.Width = new GridLength(0);
            }
        }

        private void _undoButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.CommandDispatcher.Undo();
        }

        private void _redoButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.CommandDispatcher.Redo();
        }

        private void _triangleButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.ShowDrawingTools = !Globals.ShowDrawingTools;
            Globals.DrawingTools.ShowTriangle(Globals.ShowDrawingTools);
        }

        private void _gridButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.GridIsVisible = !Globals.GridIsVisible;
            Globals.Events.GridChanged();
        }

        private void _rulerButton_Click(object sender, RoutedEventArgs e)
        {
            ShowRulers(!Globals.ShowRulers);
        }

        private void _zoomInButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.View.Zoom(2);
        }

        private void _zoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.View.Zoom(.5);
        }

        private void _zoomAllButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.View.DisplayAll();
        }

        private void _coordButton_Click(object sender, RoutedEventArgs e)
        {
            ShowCoordinatePanel();
        }

        public void DismissPopups()
        {
            if (_contextMenuPopup.IsOpen)
            {
                _contextMenuPopup.IsOpen = false;
            }
            if (_coordPanelPopup.IsOpen)
            {
                _coordPanelPopup.IsOpen = false;
            }
        }

        private void _layerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_statusBarPopup.IsOpen)
            {
                _statusBarPopup.IsOpen = false;
            }
            else
            {
                ShowStatusBarPopup(_layerButton, new HLayerPanel());
            }
        }

        private void _statusBarPopup_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void _statusBarPopup_Closed(object sender, object e)
        {
            if (_statusBarPopup.Child is HLayerPanel)
            {
                if (Globals.CommandProcessor != null)
                {
                    Globals.CommandProcessor.Finish();
                }

                if (Globals.LayerTable.ContainsKey(Globals.ActiveLayerId))
                {
                    Globals.LayerId = Globals.ActiveLayerId;

                    Layer layer = Globals.LayerTable[Globals.ActiveLayerId];
                    _layerName.Text = layer.Name;
                }
            }
        }

        private void _visibleLayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_statusBarPopup.IsOpen)
            {
                _statusBarPopup.IsOpen = false;
            }
            else
            {
                ShowStatusBarPopup(_visibleLayerButton, new HVisibleLayersPanel());
            }
        }

        private void _snapButton_Click(object sender, RoutedEventArgs e)
        {
            if (_statusBarPopup.IsOpen)
            {
                _statusBarPopup.IsOpen = false;
            }
            else
            {
                ShowStatusBarPopup(_snapButton, new HSnapPanel());
            }
        }

        private void ShowStatusBarPopup(FrameworkElement button, FrameworkElement panel, double horizOffset = 0)
        {
            if (_statusBarPopup.IsOpen == false)
            {
                _statusBarPopup.Child = panel;
                panel.SizeChanged += Panel_SizeChanged;

                Rect cpr = XamlUtilities.GetElementRect(button, _drawingRoot);

                if (double.IsNaN(_drawingRoot.ActualWidth) == false && (cpr.Left + panel.Width) > _drawingRoot.ActualWidth)
                {
                    _statusBarPopup.HorizontalOffset = _drawingRoot.ActualWidth - panel.Width - 8;
                }
                else
                {
                    _statusBarPopup.HorizontalOffset = cpr.Left - 4;
                }

                _statusBarPopup.VerticalOffset = 0;
                _statusBarPopup.IsOpen = true;
                _statusBarPopup.Visibility = Visibility.Visible;
            }
        }

        private void Panel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        public void ShowPhotoDialog(PImage pimage)
        {
#if SIBERIA
            if (pimage != null)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add("command", "edit");
                dictionary.Add("pimage", pimage);

                PhotoEditPage ped = new PhotoEditPage(dictionary);

                _modalDialogPopup.Child = ped;

                _modalDialogPopup.IsOpen = true;
                _modalDialogPopup.Visibility = Visibility.Visible;
            }
#endif
        }

        public async void ShowPhotoDialog(StorageFile file)
        {
            if (file != null && file.FileType.ToLower() == ".pdf")
            {
                try
                {
                    PdfDocument pdfDoc = await PdfDocument.LoadFromFileAsync(file); ;
                    if (pdfDoc != null)
                    {
                        if (pdfDoc.PageCount == 1)
                        {
                            PdfPage pdfPage = pdfDoc.GetPage(0);
                            if (pdfPage != null)
                            {
                                StorageFolder tempFolder = Globals.TemporaryImageFolder;
                                StorageFile jpgFile = await tempFolder.CreateFileAsync(Guid.NewGuid().ToString() + ".jpg", CreationCollisionOption.ReplaceExisting);

                                if (jpgFile != null)
                                {
                                    using (IRandomAccessStream fileStream = await jpgFile.OpenAsync(FileAccessMode.ReadWrite))
                                    {
                                        PdfPageRenderOptions pdfPageRenderOptions = new PdfPageRenderOptions();
                                        pdfPageRenderOptions.BitmapEncoderId = BitmapEncoder.JpegEncoderId;
                                        await pdfPage.RenderToStreamAsync(fileStream, pdfPageRenderOptions);
                                        await fileStream.FlushAsync();

                                        fileStream.Dispose();
                                        pdfPage.Dispose();
                                    }

                                    file = jpgFile;
                                }
                            }
                        }
                        else if (pdfDoc.PageCount > 1)
                        {
#if SIBERIA
                            Cirros8.ModalDialogs.PdfPagePickerPage pppp = new PdfPagePickerPage(pdfDoc, file.Name);

                            _modalDialogPopup.Child = pppp;

                            _modalDialogPopup.IsOpen = true;
                            _modalDialogPopup.Visibility = Visibility.Visible;

                            file = null;
#endif
                        }
                        else
                        {
                            file = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    await StandardAlerts.PdfError();
                    System.Diagnostics.Debug.WriteLine(ex.Message);

                    file = null;
                }
            }

            if (file != null)
            {
#if SIBERIA
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add("command", "newimage");
                dictionary.Add("sourceFile", file);
                dictionary.Add("sourceName", file.Name);

                PhotoEditPage ped = new PhotoEditPage(dictionary);

                _modalDialogPopup.Child = ped;

                _modalDialogPopup.IsOpen = true;
                _modalDialogPopup.Visibility = Visibility.Visible;
#endif
            }
        }

        public void ShowPhotoDialog(string imageId, string name)
        {
            if (imageId != null)
            {
#if SIBERIA
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add("command", "editimage");

                if (imageId != null)
                {
                    dictionary.Add("imageId", imageId);
                }

                if (string.IsNullOrEmpty(name) == false)
                {
                    dictionary.Add("sourceName", name);
                }

                PhotoEditPage ped = new PhotoEditPage(dictionary);

                _modalDialogPopup.Child = ped;

                _modalDialogPopup.IsOpen = true;
                _modalDialogPopup.Visibility = Visibility.Visible;
#endif
            }
        }

        //private void _helpButton_Click(object sender, RoutedEventArgs e)
        //{
        //    _ttMainIntro.IsOpen = true;
        //}

        private bool _show_ttMainIntroCloseHint = false;

        public async void DialogFirstRun(FrameworkElement sender)
        {
            if (sender != null)
            {
                await Task.Delay(500);

                _ttDialogFirstRun.Target = sender;
                _ttDialogFirstRun.IsOpen = true;
            }
        }

        private void _ttMainIntro_CloseButtonClick(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
        {
            if (_show_ttMainIntroCloseHint)
            {
                _ttMainTour.IsOpen = true;
                _show_ttMainIntroCloseHint = false;
            }
        }

        private string _lastTeachingTip = null;

        private void ShowTeachingTip(string tag)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "drawing-page" }, { "source", tag } });

            switch (tag)
            {
                case "general":
                    _ttMainMenu.IsOpen = true;
                    break;

                case "menu":
                    _ttMainMenuHome.IsOpen = true;
                    break;

                case "home":
                    _ttMainMenuFile.IsOpen = true;
                    break;

                case "file":
                    _ttMainMenuDraw.IsOpen = true;
                    break;

                case "draw":
                    _ttMainMenuAnnotation.IsOpen = true;
                    break;

                case "annotation":
                    _ttMainMenuInsert.IsOpen = true;
                    break;

                case "insert":
                    _ttMainMenuEdit.IsOpen = true;
                    break;

                case "edit":
                    _ttMainMenuInfo.IsOpen = true;
                    break;

                case "info":
                    _ttMainMenuView.IsOpen = true;
                    break;

                case "view_menu":
                    _ttMainMenuSettings.IsOpen = true;
                    break;

                case "settings":
                    _ttMainCoordinateEntry.IsOpen = true;
                    break;

                case "coord_entry":
                    _ttMainCoordinateDisplay.IsOpen = true;
                    break;

                case "coord_display":
                    _ttMainActiveLayer.IsOpen = true;
                    break;

                case "active_layer":
                    _ttMainVisibleLayers.IsOpen = true;
                    break;

                case "visible_layers":
                    _ttMainDoneButton.IsOpen = true;
                    break;

                case "done":
                    _ttMainViewButtons.IsOpen = true;
                    break;

                case "view":
                    _ttMainSnapButton.IsOpen = true;
                    break;

                case "snap":
                    _ttMainGridButton.IsOpen = true;
                    break;

                case "grid":
                    _ttMainRulerButton.IsOpen = true;
                    break;

                case "ruler":
                    _ttMainTriangleButton.IsOpen = true;
                    break;

                case "triangle":
                    _ttMainUndoButton.IsOpen = true;
                    break;

                case "undo":
                    _ttMainHelp.IsOpen = true;
                    break;
            }

            _lastTeachingTip = tag;
            _tourFlyoutItem.Text = "Continue the UX tour";
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "drawing-page" }, { "source", "help" } });

                _show_ttMainIntroCloseHint = false;
                tip.IsOpen = false;

                ShowTeachingTip(tag);
            }
        }

        private void HelpUXTourFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (_hui.CurrentDialog() != null)
            {
                _hui.LightDismiss(true);
            }

            if (_lastTeachingTip == null)
            {
                _ttMainMenu.IsOpen = true;
                _show_ttMainIntroCloseHint = true;
            }
            else
            {
                ShowTeachingTip(_lastTeachingTip);
            }
        }

        private async void HelpBugFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem fly && fly.Tag is string tag)
            {
                if (tag == "privacy")
                {
                    await Launcher.LaunchUriAsync(new Uri("http://drawingboardapp.com/privacy/"));
                }
                else if (tag == "bug")
                {
                    await Launcher.LaunchUriAsync(new Uri("mailto:support@drawingboardapp.com?subject=Bug%20report"));
                }
                else if (tag == "feature")
                {
                    await Launcher.LaunchUriAsync(new Uri("mailto:support@drawingboardapp.com?subject=Feature%20request"));
                }
            }
        }

        private void GridSnapMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            _wholeGridFlyoutItem.IsChecked = false;
            _halfGridFlyoutItem.IsChecked = false;
            _autoGridFlyoutItem.IsChecked = false;
            _offGridFlyoutItem.IsChecked = false;

            if (sender is ToggleMenuFlyoutItem tog && tog.Tag is string tag && Globals.Input != null)
            {
                switch (tag)
                {
                    case "whole":
                        _wholeGridFlyoutItem.IsChecked = true;
                        Globals.Input.GridSnapMode = GridSnapMode.wholeGrid;
                        Globals.Input.GridSnap = true;
                        break;

                    case "half":
                        _halfGridFlyoutItem.IsChecked = true;
                        Globals.Input.GridSnapMode = GridSnapMode.halfGrid;
                        Globals.Input.GridSnap = true;
                        break;

                    case "auto":
                        _autoGridFlyoutItem.IsChecked = true;
                        Globals.Input.GridSnapMode = GridSnapMode.auto;
                        Globals.Input.GridSnap = true;
                        break;

                    case "off":
                        _offGridFlyoutItem.IsChecked = true;
                        Globals.Input.GridSnap = false;
                        break;
                }
            }
        }

        private void _tt_CloseButtonClick(TeachingTip sender, object args)
        {
            _lastTeachingTip = null;
            _tourFlyoutItem.Text = "Start the UX tour";
        }

        private void HtmlTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is HtmlTextBlock h && h.Parent is Border b)
            {
                if (b.Background is SolidColorBrush brush)
                {
                    uint cspec = Utilities.ColorSpecFromColor(brush.Color);
                    System.Diagnostics.Debug.WriteLine("color: " + Utilities.ColorNameFromColorSpec(cspec));
                }
            }
        }

        private async void _facebook_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("facebook", new Dictionary<string, string> { { "source", "reddog" } });

            await Launcher.LaunchUriAsync(new Uri("https://www.facebook.com/backtothedrawingboardapp"));
        }

        private void _doneButton_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.Invoke("A_Done", null);
            }
        }

        public void ShowCoordinatePanel()
        {
            RedDogCoordinatePanel panel;

            if (_coordPanelPopup.IsOpen)
            {
                _coordPanelPopup.IsOpen = false;
            }
            else
            {
                if (_coordPanelPopup.Child is RedDogCoordinatePanel == false)
                {
                    _coordPanelPopup.Child = panel = new RedDogCoordinatePanel();
                }

                _coordPanelPopup.IsOpen = true;
                _coordPanelPopup.Visibility = Visibility.Visible;
            }
        }

        private void ObjectSnapFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleMenuFlyoutItem tog)
            {
                Globals.Input.ObjectSnap = tog.IsChecked;
            }
        }
    }
}
