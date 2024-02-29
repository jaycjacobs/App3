using Cirros.Commands;
using Cirros.Utility;
using System;
//using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
//using Windows.UI;
using Windows.UI.Core;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Generic;

using Cirros.Core.Display;
using System.Net.NetworkInformation;
using Windows.Networking.Connectivity;
using Microsoft.Windows.System;
using Windows.System;
using Windows.UI;

namespace Cirros.Core
{
    public class WorkCanvas : DrawingCanvas
    {
        public event WindowChangedHandler OnWindowChanged;
        public delegate void WindowChangedHandler(object sender, EventArgs e);

        Win2dVectorListControl _vectorListControl;

        // Note:
        // Native canvas units are always inches (true?)
        // Metric units are always millimeters
        // Scale is always true scale (i.e., 12"=1'0' and 1000mm = 1m both mean 1:1)
        // Scale factors are applied when serializing or converting to text for display

        object _parameter;

        public WorkCanvas(object parameter)
        {
            Analytics.Trace("WorkCanvas.constructor", "enter");
            _parameter = parameter;

            this.Loaded += WorkCanvas_Loaded;
            this.Unloaded += WorkCanvas_Unloaded;

            Globals.Events.OnThemeChanged += Events_OnThemeChanged;
            Globals.Events.OnDrawingCleared += Events_OnDrawingCleared;
            Globals.Events.OnDrawingLayoutChanged += Events_OnDrawingLayoutChanged;
            Globals.Events.OnGridChanged += Events_OnGridChanged;
            Globals.Events.OnPaperSizeChanged += Events_OnPaperSizeChanged;
            Globals.Events.OnDrawingLoaded += Events_OnDrawingLoaded;

            //Globals.Events.OnPrimitiveCreated += Events_OnPrimitiveCreated;

            _vectorListControl = new Win2dVectorListControl();
            _vectorListControl.Width = _viewPortSize.Width;
            _vectorListControl.Height = _viewPortSize.Height;

            this.Children.Add(_vectorListControl);
            _vectorListControl.Redraw();

            Analytics.Trace("WorkCanvas.constructor", "exit");
        }

        public override IVectorCanvas VectorListControl
        {
            get { return _vectorListControl; }
        }

        private void Events_OnDrawingLoaded(object sender, DrawingLoadedEventArgs e)
        {
            //_vectorListControl.Regenerate();
            _vectorListControl.UpdateLineStyles();
            _vectorListControl.Redraw();
        }

        private void Events_OnDrawingLayoutChanged(object sender, EventArgs e)
        {
            if (_vectorListControl != null)
            {
                _vectorListControl.Redraw();
            }
            WindowChanged();
        }

        private void Events_OnPaperSizeChanged(object sender, EventArgs e)
        {
            if (Globals.ActiveDrawing != null)
            {
                Size paperSize = Globals.ActiveDrawing.PaperSize;

                this.Width = paperSize.Width;
                this.Height = paperSize.Height;

                _lastCursorPoint = new Point(paperSize.Width / 2, paperSize.Height / 2);

                _vectorListControl.PaperSize = Globals.ActiveDrawing.PaperSize;
            }
        }

        private void Events_OnGridChanged(object sender, EventArgs e)
        {
            switch (Globals.Input.GridSnapMode)
            {
                case GridSnapMode.wholeGrid:
                    Globals.xSnap = Globals.ySnap = (Globals.GridSpacing / Globals.GridDivisions);
                    break;

                default:
                case GridSnapMode.halfGrid:
                case GridSnapMode.auto:
                    Globals.xSnap = Globals.ySnap = (Globals.GridSpacing / Globals.GridDivisions) / 2.0;
                    break;
            }

            _vectorListControl.GridDivisions = (uint)Globals.GridDivisions;
            _vectorListControl.GridSpacing = Globals.GridSpacing;
            _vectorListControl.ShowGrid(Globals.GridIsVisible);
        }

        private void Events_OnDrawingCleared(object sender, EventArgs e)
        {
            Globals.CommandDispatcher.ActiveCommand = CommandType.none;

            Globals.CommandDispatcher.ClearActions();

            _vectorListControl.Regenerate();
            _vectorListControl.Redraw();
        }

        private void Events_OnThemeChanged(object sender, EventArgs e)
        {
            if (_vectorListControl != null)
            {
                _vectorListControl.Redraw();
            }

            SetCursorColor(Globals.ActiveDrawing.Theme.CursorColor);
        }

        private void WorkCanvas_Unloaded(object sender, RoutedEventArgs e)
        {
            UnloadInput();

            Globals.Events.OnThemeChanged -= Events_OnThemeChanged;
            Globals.Events.OnDrawingCleared -= Events_OnDrawingCleared;
            Globals.Events.OnGridChanged -= Events_OnGridChanged;
        }

        private Object thisLock = new Object();

        bool layerIsVisible(int layerId)
        {
            bool visible = true;

            if (Globals.LayerTable.ContainsKey(layerId))
            {
                visible = Globals.LayerTable[layerId].Visible;
            }

            return visible;
        }

        public override void WindowChanged()
        {
            Analytics.Trace("WorkCanvas.WindowChanged", "enter");
            if (OnWindowChanged != null)
            {
                OnWindowChanged(this, new EventArgs());
            }
            //_vectorListControl.Width = _viewPortSize.Width;
            //_vectorListControl.Height = _viewPortSize.Height;
            //_vectorListControl.PaperSize = Globals.ActiveDrawing.PaperSize;
            //_vectorListControl.SetWindow(__paperWindow);

            switch (Globals.Input.GridSnapMode)
            {
                case GridSnapMode.wholeGrid:
                    Globals.xSnap = Globals.ySnap = (Globals.GridSpacing / Globals.GridDivisions);
                    break;

                case GridSnapMode.halfGrid:
                    Globals.xSnap = Globals.ySnap = (Globals.GridSpacing / Globals.GridDivisions) / 2.0;
                    break;

                default:
                case GridSnapMode.auto:
                    {
                        double snap = Globals.GridSpacing / Globals.GridDivisions;

                        if (Globals.DrawingCanvas.ModelToDisplay(snap) < 8)
                        {
                            // default snap is good
                        }
                        else if (Globals.ActiveDrawing.IsArchitecturalScale)
                        {
                            if (Globals.GridSpacing == 1 && Globals.GridDivisions == 12)
                            {
                                snap /= 2;

                                if (Globals.DrawingCanvas.ModelToDisplay(snap) > 8)
                                {
                                    snap /= 3;
                                }
                                while (snap > 0.002604166 && Globals.DrawingCanvas.ModelToDisplay(snap) > 8)
                                {
                                    snap /= 2;
                                }
                            }
                            else if (snap <= .084)
                            {
                                if ((Math.Round(1 / snap, 4) % 12) != 0)
                                {
                                    snap = (Globals.GridSpacing / Globals.GridDivisions) / 2.0;
                                }
                                else if (Globals.GridDivisions % 4 == 0)
                                {
                                    while (snap > 0.002604166 && Globals.DrawingCanvas.ModelToDisplay(snap) > 8)
                                    {
                                        snap /= 2;
                                    }
                                }
                                else if (Globals.GridDivisions == 10)
                                {
                                    while (Globals.DrawingCanvas.ModelToDisplay(snap) > 8)
                                    {
                                        if (Globals.DrawingCanvas.ModelToDisplay(snap / 2) < 8)
                                        {
                                            snap /= 2;
                                            break;
                                        }
                                        else if (Globals.DrawingCanvas.ModelToDisplay(snap / 5) < 8)
                                        {
                                            snap /= 5;
                                            break;
                                        }
                                        else
                                        {
                                            snap /= 10;
                                        }
                                    }
                                }
                                else
                                {
                                    snap = (Globals.GridSpacing / Globals.GridDivisions) / 2.0;
                                }
                            }
                            else if (snap == 1 || snap == .5 || (Math.Round(1 / snap, 4) % 6) == 0)
                            {
                                snap = .5;          // 6"

                                if (Globals.DrawingCanvas.ModelToDisplay(snap) > 8)
                                {
                                    snap /= 3;      // 2"
                                }
                                while (snap > 0.002604166 && Globals.DrawingCanvas.ModelToDisplay(snap) > 8)
                                {
                                    snap /= 2;
                                }
                            }
                            else
                            {
                                snap = (Globals.GridSpacing / Globals.GridDivisions) / 2.0;
                            }
                            //else if (Globals.GridDivisions % 4 == 0)
                            //{
                            //    while (Globals.DrawingCanvas.ModelToDisplay(snap) > 8)
                            //    {
                            //        snap /= 2;
                            //    }
                            //}
                            //else if (Globals.GridDivisions == 10)
                            //{
                            //    while (Globals.DrawingCanvas.ModelToDisplay(snap) > 8)
                            //    {
                            //        if (Globals.DrawingCanvas.ModelToDisplay(snap / 2) < 8)
                            //        {
                            //            snap /= 2;
                            //            break;
                            //        }
                            //        else if (Globals.DrawingCanvas.ModelToDisplay(snap / 5) < 8)
                            //        {
                            //            snap /= 5;
                            //            break;
                            //        }
                            //        else
                            //        {
                            //            snap /= 10;
                            //        }
                            //    }
                            //}
                        }
                        else if (Globals.GridDivisions % 4 == 0)
                        {
                            while (Globals.DrawingCanvas.ModelToDisplay(snap) > 8)
                            {
                                snap /= 2;
                            }
                        }
                        else if (Globals.GridDivisions == 10)
                        {
                            while (Globals.DrawingCanvas.ModelToDisplay(snap) > 8)
                            {
                                if (Globals.DrawingCanvas.ModelToDisplay(snap / 2) < 8)
                                {
                                    snap /= 2;
                                    break;
                                }
                                else if (Globals.DrawingCanvas.ModelToDisplay(snap / 5) < 8)
                                {
                                    snap /= 5;
                                    break;
                                }
                                else
                                {
                                    snap /= 10;
                                }
                            }
                        }

                        Globals.xSnap = Globals.ySnap = snap;
                    }
                    break;
            }
            Globals.DrawingTools.TriangleChanged();
            Analytics.Trace("WorkCanvas.WindowChanged", "exit");
        }

        protected void Initialize()
        {
            Analytics.Trace("WorkCanvas.Initialize", "enter");
            if (Globals.workCanvas == null)
            {
                Globals.workCanvas = this;
            }

            Globals.ActiveDrawing.InitializeDrawingDocument();  // move to DrawingDocument constructor?
            Analytics.Trace("WorkCanvas.Initialize", "exit");
        }

        protected override void StartTracking()
        {
            _lastCursorPoint = CursorLocation;

            if (Globals.CommandProcessor != null)
            {
                try
                {
                    bool controlKeyIsDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                    bool shiftKeyIsDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                    Globals.CommandProcessor.StartTracking(CursorLocation.X, CursorLocation.Y, shiftKeyIsDown, controlKeyIsDown);
                }
                catch (Exception ex)
                {
                    Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "StartTracking" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 203);
                }
            }
            else
            {
                Globals.Events.ShowMenu(true);
            }
        }

        protected override void TrackCursor(bool construct)
        {
            if (Globals.CommandProcessor != null)
            {
                try
                {
                    Globals.CommandProcessor.TrackCursor(CursorLocation.X, CursorLocation.Y);
                }
                catch (Exception ex)
                {
                    Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "TrackCursor" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 204);
                }
            }
        }

        protected override void EndTracking()
        {
            _lastCursorPoint = CursorLocation;

            if (Globals.CommandProcessor != null)
            {
                try
                {
                    Globals.CommandProcessor.EndTracking(CursorLocation.X, CursorLocation.Y);
                }
                catch (Exception ex)
                {
                    Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "EndTracking" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 205);
                }
            }
        }

        protected override void PointerEnteredDrawingArea()
        {
            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.PointerEnteredDrawingArea();
                Globals.Events.PointerEnteredDrawingArea();
            }
        }

        protected override void PointerLeftDrawingArea()
        {
            if (Globals.CommandProcessor != null)
            {
                try
                {
                    Globals.CommandProcessor.PointerLeftDrawingArea();
                    Globals.DrawingCanvas.VectorListControl.ShowMagnifier = false;
                    Globals.Events.PointerLeftDrawingArea();
                }
                catch (Exception ex)
                {
                    Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "PointerLeftDrawingArea" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 206);
                }
            }
        }

#region ScrollingWorkCanvas

        protected async void WorkCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            await Analytics.TraceAsync("WorkCanvas.WorkCanvas_Loaded", "enter");
            string error = null;

            //_vectorListControl = new Win2dVectorListControl();
            //_vectorListControl.Width = _viewPortSize.Width;
            //_vectorListControl.Height = _viewPortSize.Height;

            //this.Children.Add(_vectorListControl);
            //_vectorListControl.Redraw();

            Initialize();

            await Analytics.TraceAsync("WorkCanvas.WorkCanvas_Loaded:_parameter", _parameter.ToString());
            if (_parameter is string)
            {
                string parameter = _parameter as string;

                if (parameter == "restore" || await Cirros.Alerts.StandardAlerts.LastChanceToSaveAsync())
                {
                    if (await FileHandling.RestoreStateAsync() == false)
                    {
                        error = "restore_error";
                    }
                }
                else if (parameter == null)
                {
                    FileHandling.DrawingFileIsAvailable = false;
                    Globals.ActiveDrawing.NewEmptyDrawing();
                }
                else if (parameter == "new")
                {
                    FileHandling.DrawingFileIsAvailable = false;
                    Globals.ActiveDrawing.NewEmptyDrawing();
                }
                else if (parameter == "open")
                {
                    int result = await FileHandling.LoadOrImportDrawingAsync();
                    if (result == 0)
                    {
                        error = "cancelled";
                    }
                }
                else if (parameter == "open_drawing")
                {
                    int result = await FileHandling.LoadDrawingAsync();
                    if (result == 0)
                    {
                        error = "cancelled";
                    }
                    else if (result == -1)
                    {
                        error = "";
                    }
                }
                else if (parameter == "import")
                {
                    int result = await FileHandling.ImportDrawingAsync();
                    if (result == 0)
                    {
                        error = "cancelled";
                    }
                    else if (result < 0)
                    {
                        error = "cancelled";
                    }
                }
                else if (parameter == "import-dwg")
                {
                    ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();

                    if (profile != null && profile.GetNetworkConnectivityLevel() >= NetworkConnectivityLevel.InternetAccess)
                    {
                        int result = await FileHandling.ImportDwgDocumentAsync();
                        if (result == 0)
                        {
                            error = "cancelled";
                        }
                        else if (result < 0)
                        {
                            await Cirros.Alerts.StandardAlerts.SimpleAlertAsync("Conversion failed", "The drawing could not be converted.");
                        }
                    }
                    else
                    {
                        await Cirros.Alerts.StandardAlerts.SimpleAlertAsync("No Internet connection", "Check your network settings and try again.");
                        error = "cancelled";
                    }
                }
                else
                {
                    // Assume the parameter is a MostRecentlyUsedList token
                    if (await FileHandling.LoadMruDrawingAsync(parameter) == false)
                    {
                        error = "invalid";
                        try
                        {
                            StorageFile mrufile = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(parameter);
                            if (mrufile != null)
                            {
                                StorageApplicationPermissions.MostRecentlyUsedList.Remove(parameter);
                                error = "invalid:" + mrufile.Name;
                            }
                        }
                        catch
                        {
                            // If the MRU token is bad, StorageApplicationPermissions.MostRecentlyUsedList will fail.
                            // Not a huge surprise.
                        }
                    }
                }

            }
            else if (_parameter is StorageFile)
            {
                await Analytics.TraceAsync("WorkCanvas.WorkCanvas_Loaded:_parameter", "StorageFile");
                StorageFile file = _parameter as StorageFile;
                string filetype = file.FileType.ToLower();

                if (filetype == ".dbtx")
                {
                    if (await FileHandling.LoadDrawingAsync(file) == false)
                    {
                        error = "invalid";
                    }
                }
                else if (filetype == ".dbfx")
                {
                    if (await FileHandling.LoadDrawingAsync(file) == false)
                    {
                        error = "invalid";
                    }
                }
                else if (filetype == ".svg")
                {
                    if (await FileHandling.ImportSvgAsync(file) < 1)
                    {
                        error = "invalid";
                    }
                }
                else if (filetype == ".dxf")
                {
                    if (await FileHandling.ImportDxfAsync(file) < 1)
                    {
                        error = "invalid";
                    }
                }
            }

            await Analytics.TraceAsync("WorkCanvas.WorkCanvas_Loaded:error", error == null ? "null" : error);
            if (error != null)
            {
                if (error != "cancelled")
                {
                    Analytics.ReportError(string.Format("WorkCanvas_Loaded: Failed to load ({0})", error), null, 2, 207);
                }

                Globals.Events.DrawingShouldClose(error);
            }
            else if (Globals.ActiveDrawing.PaperSize.Width == 0 || Globals.LayerTable.Count == 0)
            {
                // Saved drawing is invalid

                Analytics.ReportError("WorkCanvas_Loaded: Failed to load", null, 2, 208);
                Globals.Events.DrawingShouldClose("failed:");
            }
            else
            {
                InitializeInput();
                DisplayAll();
            }

            await Analytics.TraceAsync("WorkCanvas.WorkCanvas_Loaded", "DrawingCanvasLoaded");
            Globals.Events.DrawingCanvasLoaded(_parameter);
            await Analytics.TraceAsync("WorkCanvas.WorkCanvas_Loaded", "exit");
        }

        protected IPointerHandler _currentPointer = null;
        protected IPointerHandler _mousePointer = null;
        protected IPointerHandler _stylusPointer = null;
        protected IPointerHandler _touchPointer = null;

        private Microsoft.UI.Input.PointerDeviceType _currentPointerDeviceType = Microsoft.UI.Input.PointerDeviceType.Touch;

        protected ContentControl _focusTarget;

        protected void InitializeInput()
        {
            Analytics.Trace("WorkCanvas.InitializeInput", "enter");
            _mousePointer = new MouseHandler(this);
            _stylusPointer = new StylusPointerHandler(this);
            _touchPointer = new TouchPointerHandler(this);

            _focusTarget = new ContentControl();
            _focusTarget.Name = "focusTarget";
            _focusTarget.IsEnabled = true;
            _focusTarget.IsTabStop = true;
            _focusTarget.Visibility = Visibility.Visible;
            this.Children.Add(_focusTarget);

            this.PointerPressed += ScrollingWorkCanvas_PointerPressed;
            this.PointerReleased += ScrollingWorkCanvas_PointerReleased;
            this.PointerMoved += ScrollingWorkCanvas_PointerMoved;
            this.PointerEntered += ScrollingWorkCanvas_PointerEntered;
            this.PointerExited += ScrollingWorkCanvas_PointerExited;
            this.PointerWheelChanged += ScrollingWorkCanvas_PointerWheelChanged;

            // WINUI3 TODO
            System.Diagnostics.Debugger.Break();
            //App.Window.CoreWindow.KeyDown += CoreWindow_KeyDown;
            //App.Window.CoreWindow.KeyUp += CoreWindow_KeyUp;
            Analytics.Trace("WorkCanvas.InitializeInput", "exit");
        }

        protected void UnloadInput()
        {
            this.PointerPressed -= ScrollingWorkCanvas_PointerPressed;
            this.PointerReleased -= ScrollingWorkCanvas_PointerReleased;
            this.PointerMoved -= ScrollingWorkCanvas_PointerMoved;
            this.PointerEntered -= ScrollingWorkCanvas_PointerEntered;
            this.PointerExited -= ScrollingWorkCanvas_PointerExited;
            this.PointerWheelChanged -= ScrollingWorkCanvas_PointerWheelChanged;

            // WINUI3 TODO
            System.Diagnostics.Debugger.Break();
            //App.Window.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            //App.Window.CoreWindow.KeyUp -= CoreWindow_KeyUp;

            if (_focusTarget != null)
            {
                this.Children.Remove(_focusTarget);
                _focusTarget = null;
            }

            _currentPointer = null;

            _mousePointer = null;
            _stylusPointer = null;
            _touchPointer = null;
        }

        public override bool Focus()
        {
            bool focused = _focusTarget != null ? _focusTarget.Focus(FocusState.Keyboard) : false;
            return focused;
        }

        public override int InputDevice
        {
            get
            {
                int mode = base.InputDevice;

                switch (_currentPointerDeviceType)
                {
                    case PointerDeviceType.Mouse:
                        mode = 0;
                        break;

                    case PointerDeviceType.Touch:
                        mode = 1;
                        break;

                    case PointerDeviceType.Pen:
                        mode = 2;
                        break;
                }

                return mode;
            }
        }

        // WINUI3 TODO
        //void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs args)
        //{
        //    string k = args.VirtualKey.ToString().ToLower();
        //    HandleKeyUp(k);
        //}

        //void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        //{
        //    string k = args.VirtualKey.ToString().ToLower();
        //    if (HandleKeyDown(k))
        //    {
        //        args.Handled = true;
        //    }
        //}

        protected IPointerHandler SelectPointer(PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != _currentPointerDeviceType || _currentPointer == null)
            {
                if (_currentPointer != null)
                {
                    _currentPointer.Release(e);
                }

                switch (e.Pointer.PointerDeviceType)
                {
                    case PointerDeviceType.Mouse:
                        _currentPointer = _mousePointer;
                        _currentPointerDeviceType = PointerDeviceType.Mouse;
                        break;

                    case PointerDeviceType.Touch:
                        _currentPointer = _touchPointer;
                        _currentPointerDeviceType = PointerDeviceType.Touch;
                        break;

                    case PointerDeviceType.Pen:
                        _currentPointer = _stylusPointer;
                        _currentPointerDeviceType = PointerDeviceType.Pen;
                        break;

                    default:
                        break;
                }

                if (_currentPointer != null)
                {
                    _currentPointer.Capture(e);
                }
            }

            return _currentPointer;
        }

        void ScrollingWorkCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_currentPointer != null)
            {
                _currentPointer.PointerLeave(e.GetCurrentPoint(this).Position, e);
            }
            PointerLeftDrawingArea();
        }

        void ScrollingWorkCanvas_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (SelectPointer(e) != null)
            {
                _currentPointer.PointerEnter(e.GetCurrentPoint(this).Position, e);
            }
            PointerEnteredDrawingArea();
        }

        bool _cursorInTriangle = false;

        async void ScrollingWorkCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint pp = e.GetCurrentPoint(this);

            if (Globals.DrawingTools.PointInTriangle(pp.Position))
            {
                Globals.DrawingTools._toolsOverlay_PointerPressed(sender, e);
            }
            else
            {
                TimeSpan ts = DateTime.Now - _clickTime;
                _doubleClick = ts < _dbcTime && _clickLoc.X == pp.Position.X && _clickLoc.Y == pp.Position.Y;
                _clickTime = DateTime.Now;
                _clickLoc = pp.Position;

                if (_doubleClick)
                {
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.DoubleClick();
                    }
                }

                if (SelectPointer(e) != null)
                {
                    CapturePointer(e.Pointer);

                    _currentPointer.Capture(e);

                    try
                    {
                        bool flag = await _currentPointer.PointerPressed(e.GetCurrentPoint(this).Position, e);
                        if (!flag)
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("ScrollingWorkCanvas_PointerPressed error: {0}", ex.HResult);
                    }
                }

                if (_doubleClick == false)
                {
                    DoPointerPress(pp.Position);
                }
            }
        }

        void ScrollingWorkCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            bool shouldTrack = false;

            PointerPoint pp = e.GetCurrentPoint(this);
            Point canvasLoc = pp.Position;
            bool inTriangle = Globals.DrawingTools.PointInTriangle(canvasLoc);

            if (e.Pointer.PointerDeviceType != _currentPointerDeviceType)
            {
                SelectPointer(e);
                AcquireCursor = true;
            }

            if (_currentPointer != null)
            {
                shouldTrack = _currentPointer.PointerMoved(e.GetCurrentPoint(this).Position, e);
            }

            if (shouldTrack == false && inTriangle == false)
            {
                AcquireCursor = false;
            }
            else
            {
                if (inTriangle)
                {
                    if (_cursorInTriangle == false)
                    {
                        if (_currentPointer != null)
                        {
                            _currentPointer.PointerLeave(e.GetCurrentPoint(this).Position, e);
                        }
                        PointerLeftDrawingArea();

                        _cursorInTriangle = true;

                        Globals.DrawingTools._triangle_PointerEntered(sender, e);
                    }

                    Globals.DrawingTools._toolsOverlay_PointerMoved(sender, e);
                    Point paperLoc = _vectorListControl.DisplayToPaper(canvasLoc);
                    SetCursorPositon(paperLoc);
                }
                else
                {
                    if (_cursorInTriangle)
                    {
                        _cursorInTriangle = false;

                        Globals.DrawingTools._triangle_PointerExited(sender, e);

                        if (SelectPointer(e) != null)
                        {
                            _currentPointer.PointerEnter(e.GetCurrentPoint(this).Position, e);
                        }
                        PointerEnteredDrawingArea();
                    }

                    Point paperLoc = _vectorListControl.DisplayToPaper(canvasLoc);

                    SetCursorPositon(paperLoc);
                }
            }
        }

        void ScrollingWorkCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            bool usePoint = true;

            Globals.DrawingTools._toolsOverlay_PointerReleased(sender, e);

            if (_currentPointer != null)
            {
                usePoint = _currentPointer.PointerReleased(e.GetCurrentPoint(this).Position, e);

                if (_currentPointer.Release(e))
                {
                    _currentPointer = null;
                }
            }

            ReleasePointerCapture(e.Pointer);

            if (usePoint)
            {
                EndTracking();
            }
        }

        void ScrollingWorkCanvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            CursorVisible = false;

            PointerPoint pp = e.GetCurrentPoint(this);
            Point cc = pp.Position;

            Point pc = Globals.DrawingCanvas.DisplayToPaper(cc);

            e.Handled = true;
            if (pp.Properties.MouseWheelDelta > 0)
            {
                Zoom(pc.X, pc.Y, 1.125, false);
            }
            else
            {
                Zoom(pc.X, pc.Y, 0.88888888888888888888888888888889, false);
            }

            Point paper = Globals.DrawingCanvas.DisplayToPaper(cc);

            SetCursorPositon(paper);
            CursorVisible = true;
        }

#endregion

#region IDrawingInput

        public override bool ObjectSnap
        {
            get
            {
                return base.ObjectSnap;
            }

            set
            {
                base.ObjectSnap = value;

                if (Globals.CommandProcessor != null)
                {
                    Globals.CommandProcessor.ShowConstructHandles = _objectSnap;
                }
            }
        }

        protected override bool HandleKeyDown(string key)
        {
            object fe = FocusManager.GetFocusedElement();
            bool gmk = _gmkEnabled && !(fe is TextBox || fe is Button || fe is ComboBoxItem || fe is Popup);

            //if (!_stillDown)
            {
                ProcessKey(key, gmk);

                if (Globals.CommandProcessor != null)
                {
                    bool controlKeyIsDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                    bool shiftKeyIsDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                    Globals.CommandProcessor.KeyDown(key, shiftKeyIsDown, controlKeyIsDown, gmk);
                }

                _stillDown = true;
            }

            return gmk;
        }

        bool _stillDown = false;

        protected override void HandleKeyUp(string key)
        {
            base.HandleKeyUp(key);

            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.KeyUp(key);
            }

            _stillDown = false;
        }

        #endregion

        #region IDrawingView

        public override void DisplayWindow(double lx, double ly, double ux, double uy, bool restrict = false)
        {
#if false // constrain to paper size
            double width = Math.Abs(ux - lx);
            double height = Math.Abs(uy - ly);

            if (restrict && width >= Globals.ActiveDrawing.PaperSize.Width && height >= Globals.ActiveDrawing.PaperSize.Height)
            {
                if (width > Globals.ActiveDrawing.PaperSize.Width)
                {
                    width = Globals.ActiveDrawing.PaperSize.Width;
                    lx = 0;
                    ux = width;
                }
                if (height > Globals.ActiveDrawing.PaperSize.Height)
                {
                    height = Globals.ActiveDrawing.PaperSize.Height;
                    ly = 0;
                    uy = height;
                }
            }
#else // constrain to data extents or paper size
            if (restrict && _extents.IsEmpty == false)
            {
                lx = Math.Max(lx, _extents.Left);
                ly = Math.Max(ly, _extents.Top);
                ux = Math.Min(ux, _extents.Right);
                uy = Math.Min(uy, _extents.Bottom);
            }

            double width = Math.Abs(ux - lx);
            double height = Math.Abs(uy - ly);
#endif

            double aspect = _viewPortSize.Width / _viewPortSize.Height;

            double xscale = _viewPortSize.Width / PaperToDisplay(width);
            double yscale = _viewPortSize.Height / PaperToDisplay(height);

            if (xscale != yscale)
            {
                double cx = (lx + ux) / 2.0;
                double cy = (ly + uy) / 2.0;

                if (xscale < yscale)
                {
                    height = width / aspect;
                    CanvasToDisplayScale = xscale;
                }
                else
                {
                    width = height * aspect;
                    CanvasToDisplayScale = yscale;
                }

                lx = cx - (width / 2.0);
                ly = cy - (height / 2.0);
                ux = lx + width;
                uy = ly + height;
            }
            else
            {
                CanvasToDisplayScale = xscale;
            }

            CurrentWindow = new Rect(lx, ly, width, height);

            _vectorListControl.SetWindow(new Point(lx, ly), new Point(ux, uy));
            _vectorListControl.Redraw();

            WindowChanged();
        }

        public override void DisplayActualSize()
        {
            Rect r = _vectorListControl.GetWindow();

            _vectorListControl.ActualSizeWindow((r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2);

            CurrentWindow = _vectorListControl.GetWindow();
            WindowChanged();
        }

        public override void DisplayActualSize(double cx, double cy)
        {
            _vectorListControl.ActualSizeWindow(cx, cy);

            CurrentWindow = _vectorListControl.GetWindow();
            WindowChanged();
        }

        public override void PanToPoint(double x, double y)
        {
            Globals.DrawingCanvas.VectorListControl.PanToPoint(x, y);

            CurrentWindow = _vectorListControl.GetWindow();
            WindowChanged();
        }

        public override void Pan(Point screenDelta)
        {
            _vectorListControl.Pan(screenDelta.X, screenDelta.Y);

            CurrentWindow = _vectorListControl.GetWindow();
            WindowChanged();
        }

        public override void Pan(double horizontalFraction, double verticalFraction)
        {
            double dx = horizontalFraction * _vectorListControl.ActualWidth;
            double dy = verticalFraction * _vectorListControl.ActualHeight;

            Pan(new Point(-dx, -dy));
        }

        public override void Zoom(double zoom)
        {
            _vectorListControl.Zoom(zoom);

            Rect w = _vectorListControl.GetWindow();
            if (w.Contains(new Point(0, 0)) || w.Contains(new Point(Globals.ActiveDrawing.PaperSize.Width, Globals.ActiveDrawing.PaperSize.Height)))
            {
                DisplayAll();
            }
            else
            {
                CurrentWindow = w;
                WindowChanged();
            }
        }

        public override void Zoom(double cx, double cy, double zoom, bool center)
        {
            double width = CurrentWindow.Width / zoom;
            double height = CurrentWindow.Height / zoom;
            double left, top;

            if (center)
            {
                left = cx - width / 2;
                top = cy - height / 2;
            }
            else
            {
                left = cx - (cx - CurrentWindow.Left) / zoom;
                top = cy - (cy - CurrentWindow.Top) / zoom;
            }

            DisplayWindow(left, top, left + width, top + height, zoom < 1);
        }

        public override double CanvasToDisplayScale
        {
            get
            {
                return base.CanvasToDisplayScale;
            }

            set
            {
                base.CanvasToDisplayScale = value;

                if (Globals.CommandProcessor != null)
                {
                    Globals.CommandProcessor.CanvasScaleChanged();
                }

                SetCursorScale(1 / value);
                //Globals.DrawingTools.TriangleChanged();
            }
        }

        public override Rect CurrentWindow
        {
            get
            {
                return base.CurrentWindow;
            }

            set
            {
                base.CurrentWindow = value;

                WindowChanged();
            }
        }

        public override Size ViewPortSize
        {
            get
            {
                return base.ViewPortSize;
            }

            set
            {
                base.ViewPortSize = value;
                if (_vectorListControl != null)
                {
                    _vectorListControl.ViewPortSize = _viewPortSize;

                    if (CurrentWindow.Width > 0 & CurrentWindow.Height > 0)
                    {
                        DisplayWindow(CurrentWindow.Left, CurrentWindow.Top, CurrentWindow.Right, CurrentWindow.Bottom, true);
                        _vectorListControl.Redraw();
                    }
                }
            }
        }

        public override void Regenerate()
        {
            _vectorListControl.Regenerate();
            _vectorListControl.Redraw();
        }

#endregion

#region ICursor

        private bool _cursorAcquired = false;
        //private double _cursorSize = 0;
        private CursorType _cursorType = CursorType.Arrow;

        protected void SetCursorColor(Color color)
        {
            _vectorListControl.CursorColor = color;
        }

        public override Point CursorLocation
        {
            get
            {
                return _cursorLocation;
            }
        }

        public override bool AcquireCursor
        {
            get
            {
                return _cursorAcquired;
            }
            set
            {
                _cursorAcquired = value;

                if (_cursorAcquired && _cursorInTriangle == false)
                {
                    selectCursor();
                }
                else
                {
                    // WINUI3 TODO
                    System.Diagnostics.Debugger.Break();
                    //App.Window.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
                }
            }
        }

        public override void GhostCursor()
        {
            // for pick mode when selecting with arrow keys
            // force the draw cursor to be shown until the arrow cursor moves on the drawing canvas
        }

        public override bool CursorVisible
        {
            set
            {
                if (_cursorType == CursorType.Draw)
                {
                    _vectorListControl.ShowCursor(value);
                }
                else
                {
                    _vectorListControl.ShowCursor(false);
                }
            }
        }

        protected void SetCursorScale(double scale)
        {
        }

        public override double CursorSize
        {
            get
            {
                return _vectorListControl.CursorSize;
            }
            set
            {
                _vectorListControl.CursorSize = value;
            }
        }

        private void selectCursor()
        {
            // WINUI3 TODO
            System.Diagnostics.Debugger.Break();
            //switch (_cursorType)
            //{
            //    case CursorType.Arrow:
            //        App.Window.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            //        _vectorListControl.ShowCursor(false);
            //        _gmkEnabled = false;
            //        break;

            //    case CursorType.Wait:
            //        App.Window.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);
            //        _vectorListControl.ShowCursor(false);
            //        _gmkEnabled = false;
            //        break;

            //    case CursorType.Hand:
            //        App.Window.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
            //        _vectorListControl.ShowCursor(false);
            //        _gmkEnabled = true;
            //        break;

            //    case CursorType.Draw:
            //        App.Window.CoreWindow.PointerCursor = null;
            //        _vectorListControl.ShowCursor(true);
            //        _gmkEnabled = true;
            //        break;

            //    case CursorType.Pan:
            //        App.Window.CoreWindow.PointerCursor = null;
            //        _vectorListControl.ShowCursor(true);
            //        _gmkEnabled = true;
            //        break;
            //}
        }

        public override void SelectCursor(CursorType cursorType)
        {
            if (_cursorType != cursorType)
            {
                _cursorType = cursorType;

                if (_cursorAcquired)
                {
                    selectCursor();
                }
            }
        }


        public override void DoPointerPress(Point p)
        {
            Point paperLoc = DisplayToPaper(p);

            SetCursorPositon(paperLoc);
            StartTracking();
        }

        public override Point ResetCursorPosition(bool moveOnScreen)
        {
            if (moveOnScreen && __paperWindow.Contains(_lastCursorPoint) == false)
            {
                _lastCursorPoint = Construct.RoundXY(new Point((__paperWindow.Left + __paperWindow.Right) / 2, (__paperWindow.Top + __paperWindow.Bottom) / 2));
            }

            SetCursorPositon(_lastCursorPoint);

            return _lastCursorPoint;
        }

        protected override void SetCursorPositon(Point location)
        {
            bool moved = false;

            try
            {
                if (_gridSnap)
                {
                    Point p = Construct.RoundXY(location);
                    moved = _cursorLocation.X != p.X || _cursorLocation.Y != p.Y;
                    _cursorLocation.X = p.X;
                    _cursorLocation.Y = p.Y;
                }
                else
                {
                    moved = _cursorLocation.X != location.X || _cursorLocation.Y != location.Y;
                    _cursorLocation.X = location.X;
                    _cursorLocation.Y = location.Y;
                }

                if (moved)
                {
                    bool shiftKeyIsDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                    TrackCursor(!shiftKeyIsDown);

                    if (_vectorListControl != null)
                    {
                        _vectorListControl.CursorLocation = PaperToDisplay(_cursorLocation);
                    }
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError("SetCursorPositon", ex, 2, 209);
            }
        }

        public override void MoveCursorBy(double dx, double dy)
        {
            // dx,dy: delta in paper units
            _cursorLocation.X += dx;
            _cursorLocation.Y += dy;

            if (Globals.CommandProcessor != null)
            {
                _cursorLocation = Globals.CommandProcessor.Step(dx, dy, _stillDown);
            }

            _lastCursorPoint = _cursorLocation;

            _vectorListControl.CursorLocation = PaperToDisplay(_cursorLocation);

            CursorVisible = true;

            TrackCursor(false);
        }

        public override void MoveCursorTo(double x, double y)
        {
            _cursorLocation.X = x;
            _cursorLocation.Y = y;

            //if (Globals.CommandProcessor != null)
            //{
            //    Globals.CommandProcessor.EnterPoint(new Point(x, y));
            //}

            _lastCursorPoint = _cursorLocation;

            _vectorListControl.CursorLocation = PaperToDisplay(_cursorLocation);

            CursorVisible = true;

            TrackCursor(false);
        }

        #endregion
        public override double PaperToDisplay(double paper)
        {
            return _vectorListControl.PaperToDisplay(paper);
        }

        public override Point PaperToDisplay(Point paper)
        {
            return  _vectorListControl.PaperToDisplay(paper);
        }

        public override double DisplayToPaper(double display)
        {
            if (_vectorListControl == null)
            {
                return display;
            }
            return _vectorListControl.DisplayToPaper(display);
        }

        public override Point DisplayToPaper(Point display)
        {
            return _vectorListControl.DisplayToPaper(display);
        }
    }
}
