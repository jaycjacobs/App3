using Cirros.Primitives;
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Cirros.Core.Display;
using System.Collections.Generic;
using Cirros.Utility;
using Cirros8;
using Microsoft.UI.Xaml;
using Microsoft.Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Input;
using Windows.System;

namespace Cirros.Core
{
    public enum Key
    {
        None = 0,
        Back = 1,
        Tab = 2,
        Enter = 3,
        Shift = 4,
        Ctrl = 5,
        Alt = 6,
        CapsLock = 7,
        Escape = 8,
        Space = 9,
        PageUp = 10,
        PageDown = 11,
        End = 12,
        Home = 13,
        Left = 14,
        Up = 15,
        Right = 16,
        Down = 17,
        Insert = 18,
        Delete = 19,
        D0 = 20,
        D1 = 21,
        D2 = 22,
        D3 = 23,
        D4 = 24,
        D5 = 25,
        D6 = 26,
        D7 = 27,
        D8 = 28,
        D9 = 29,
        A = 30,
        B = 31,
        C = 32,
        D = 33,
        E = 34,
        F = 35,
        G = 36,
        H = 37,
        I = 38,
        J = 39,
        K = 40,
        L = 41,
        M = 42,
        N = 43,
        O = 44,
        P = 45,
        Q = 46,
        R = 47,
        S = 48,
        T = 49,
        U = 50,
        V = 51,
        W = 52,
        X = 53,
        Y = 54,
        Z = 55,
        F1 = 56,
        F2 = 57,
        F3 = 58,
        F4 = 59,
        F5 = 60,
        F6 = 61,
        F7 = 62,
        F8 = 63,
        F9 = 64,
        F10 = 65,
        F11 = 66,
        F12 = 67,
        NumPad0 = 68,
        NumPad1 = 69,
        NumPad2 = 70,
        NumPad3 = 71,
        NumPad4 = 72,
        NumPad5 = 73,
        NumPad6 = 74,
        NumPad7 = 75,
        NumPad8 = 76,
        NumPad9 = 77,
        Multiply = 78,
        Add = 79,
        Subtract = 80,
        Decimal = 81,
        Divide = 82,
        Unknown = 255,
    }

    public enum CursorType
    {
        Arrow = 0,
        Hand,
        Draw,
        Select,
        Pan,
        Wait
    }

    public abstract class DrawingCanvas : Canvas, IDrawingView, IDrawingInput
    {
        protected Rect __paperWindow = new Rect();   // Window rect in paper units

        protected bool _gridSnap = true;
        protected bool _objectSnap = true;
        protected GridSnapMode _gridSnapMode = GridSnapMode.auto;

        public abstract bool Focus();
        protected abstract void StartTracking();
        protected abstract void TrackCursor(bool construct);
        protected abstract void EndTracking();
        protected abstract void PointerEnteredDrawingArea();
        protected abstract void PointerLeftDrawingArea();
        public abstract double PaperToDisplay(double paper);

        public abstract double DisplayToPaper(double display);

        public abstract Point PaperToDisplay(Point paper);

        public abstract Point DisplayToPaper(Point display);

        public Point ModelToDisplayRound(Point model)
        {
            // Return pixel-aligned (to display) canvas coordinates.

            Point paper = Globals.ActiveDrawing.ModelToPaper(model);
            Point display = PaperToDisplay(paper);
            display.X = Math.Round(display.X);
            display.Y = Math.Round(display.Y);
            return display;
        }

        public Point ModelToDisplay(Point model)
        {
            Point paper = Globals.ActiveDrawing.ModelToPaper(model);
            return PaperToDisplay(paper);
        }

        public Point ModelToDisplayRaw(Point model)
        {
            double mx = Globals.ActiveDrawing.ModelToPaper(model.X);
            double my = Globals.ActiveDrawing.ModelToPaper(model.Y);
            //Point paper = new Point(model.X * _modelToPaperScale, Globals.ActiveDrawing.PaperSize.Height - (model.Y * _modelToPaperScale));
            Point paper = new Point(mx, Globals.ActiveDrawing.PaperSize.Height - my);
            return PaperToDisplay(paper);
        }

        public double ModelToDisplay(double model)
        {
            double paper = Globals.ActiveDrawing.ModelToPaper(model);
            return PaperToDisplay(paper);
        }

#region IDrawingInput

        protected abstract void SetCursorPositon(Point location);

        public abstract void DoPointerPress(Point p);

        public bool EnableGMK
        {
            set
            {
                _gmkEnabled = value;
            }
        }

        public bool ShiftKeyIsDown
        {
            get
            {
                return InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            }
        }

        public virtual bool GridSnap
        {
            get
            {
                return _gridSnap;
            }
            set
            {
                if (_gridSnap != value)
                {
                    _gridSnap = value;
                    Globals.Events.OptionsChanged();
                }
            }
        }

        public virtual GridSnapMode GridSnapMode
        {
            get
            {
                return _gridSnapMode;
            }
            set
            {
                if (_gridSnapMode != value)
                {
                    _gridSnapMode = value;
                    Globals.Events.OptionsChanged();
                    Globals.View.WindowChanged();
                }
            }
        }

        public virtual bool ObjectSnap
        {
            get
            {
                return _objectSnap;
            }
            set
            {
                if (_objectSnap != value)
                {
                    _objectSnap = value;

                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.ShowConstructHandles = _objectSnap;
                    }
                    Globals.Events.OptionsChanged();
                }
            }
        }

        public virtual int InputDevice
        {
            get
            {
                // 0: Mouse
                // 1: Touch
                // 2: Pen
                return 0;
            }
        }

        public virtual void ProcessKey(string key, bool isGMK)
        {
            bool controlKeyIsDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shiftKeyIsDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            if (isGMK)
            {
                if (controlKeyIsDown)
                {
                    if (key == "z")
                    {
                        if (shiftKeyIsDown)
                        {
                            Globals.CommandDispatcher.Redo();
                        }
                        else
                        {
                            Globals.CommandDispatcher.Undo();
                        }
                    }
                }
                else
                { 
                    switch (key)
                    {
                        case "shift":
                        case "ctrl":
                        case "control":
                            break;

                        case "space":
                            EnterPoint();
                            break;

                        case "left":
                            Step(1);
                            break;

                        case "right":
                            Step(2);
                            break;

                        case "up":
                            Step(3);
                            break;

                        case "down":
                            Step(4);
                            break;

                        case "187":
                        case "=":
                        case "equal":
                            DisplayActualSize(CursorLocation.X, CursorLocation.Y);
                            break;

                        case "a":
                            DisplayAll();
                            break;

                        case "g":
                            Globals.GridIsVisible = !Globals.GridIsVisible;
                            Globals.Events.GridChanged();
                            break;

                        case "s":
                            GridSnap = !GridSnap;
                            break;

                        case "r":
                            Globals.Events.ShowRulers(!Globals.ShowRulers);
                            break;

                        case "c":
                            PanToPoint(CursorLocation.X, CursorLocation.Y);
                            break;

                        case "u":
                            Zoom(CursorLocation.X, CursorLocation.Y, 0.5, true);
                            break;

                        case "z":
                            Zoom(CursorLocation.X, CursorLocation.Y, 2.0, true);
                            break;

                        case "j":
                            break;

                        case "back":
                            if (ShiftKeyIsDown)
                            {
                                Globals.CommandDispatcher.Redo();
                            }
                            else
                            {
                                Globals.CommandDispatcher.Undo();
                            }
                            break;

                        case "e":
                            //if (Globals.CommandProcessor != null && Globals.CommandProcessor.EnableCommand("A_EditLast"))
                            //{
                            //    Globals.CommandProcessor.Invoke("A_EditLast", null);
                            //}
                            break;

                        case "f":
                            {
                                Primitive p = Globals.ActiveDrawing.Pick(CursorLocation.X, CursorLocation.Y, true);
                                if (p != null)
                                {
                                    p.ZIndex = Globals.ActiveDrawing.MaxZIndex;
                                }
                            }
                            break;

                        case "b":
                            {
                                Primitive p = Globals.ActiveDrawing.Pick(CursorLocation.X, CursorLocation.Y, true);
                                if (p != null)
                                {
                                    p.ZIndex = Globals.ActiveDrawing.MinZIndex;
                                }
                            }
                            break;

                        case "m":
                            {
                                Primitive p = Globals.ActiveDrawing.Pick(CursorLocation.X, CursorLocation.Y, false);
                                if (p != null)
                                {
                                    Globals.ActiveDrawing.MatchAttributes(p);
                                }
                            }
                            break;

                        case "[":
                        case "219":
                            {
                                IDrawingPage page = Globals.RootVisual as IDrawingPage;
                                if (page != null)
                                {
                                    page.ShowCoordinatePanel();
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        public void EnterPoint()
        {
            // Enter a keyboard point
            StartTracking();
            TrackCursor(true);
            EndTracking();
        }

        private void Step(int direction)
        {
            GhostCursor();

            if (_keyDirection == direction)
            {
                _keyCount++;
                if (_keyCount < 8)
                {
                    _keySteps = 1;
                }
                else if (_keyCount < 20)
                {
                    _keySteps = 3;
                }
                else
                {
                    _keySteps = 6;
                }
            }
            else
            {
                _keyDirection = direction;
                _keyCount = 1;
                _keySteps = 1;
            }

            bool shiftKeyIsDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            //double step = Globals.ActiveDrawing.ModelToPaper(shiftKeyIsDown ? .01 : Globals.xSnap);
            double step = Globals.ActiveDrawing.ModelToPaper(shiftKeyIsDown ? Globals.xSnap / 2 : Globals.xSnap);

            if (_gridSnap == false && _keyCount < 10)
            {
                if (Globals.GridDivisions == 10)
                {
                    step /= 5;
                }
                else
                {
                    step /= 4;
                }
            }

            switch (_keyDirection)
            {
                case 1:
                    MoveCursorBy(-step * _keySteps, 0.0);
                    break;
                case 2:
                    MoveCursorBy(step * _keySteps, 0.0);
                    break;
                case 3:
                    MoveCursorBy(0.0, -step * _keySteps);
                    break;
                case 4:
                    MoveCursorBy(0.0, step * _keySteps);
                    break;
            }
        }

        protected virtual bool HandleKeyDown(string key)
        {
            bool gmk = _gmkEnabled;

            ProcessKey(key, gmk);

            return gmk;
        }

        protected virtual void HandleKeyUp(string key)
        {
            _keyCount = 0;
            _keyDirection = 0;
        }

#endregion

#region IDrawingView

        protected double _canvasToDisplayScale = 1.0;       // canvas units : display units (i.e. zoom factor)
        protected Size _viewPortSize;

        public virtual double CanvasToDisplayScale
        {
            get
            {
                return _canvasToDisplayScale;
            }
            set
            {
                _canvasToDisplayScale = value;

                Globals.DrawingTools.TriangleChanged();
            }
        }

        public virtual Rect CurrentWindow
        {
            get
            {
                return __paperWindow;
            }
            set
            {
                __paperWindow = value;
            }
        }

        public virtual Size ViewPortSize
        {
            get
            {
                return _viewPortSize;
            }
            set
            {
                _viewPortSize = value;
            }
        }

        protected Rect _extents = Rect.Empty;

        public void DisplayAll()
        {
            _extents = new Rect(0.0, 0.0, Globals.ActiveDrawing.PaperSize.Width, Globals.ActiveDrawing.PaperSize.Height);
            _extents.Union(Globals.View.VectorListControl.VectorList.Extents);
            DisplayWindow(_extents.Left, _extents.Top, _extents.Right, _extents.Bottom, false);
        }

        public abstract void DisplayActualSize();
        public abstract void DisplayActualSize(double cx, double cy);

        public abstract void DisplayWindow(double lx, double ly, double ux, double uy, bool restrict = true);

        public abstract void Pan(Point delta);
        public abstract void Pan(double horizontalFraction, double verticalFraction);
        public abstract void PanToPoint(double x, double y);

        public abstract void Zoom(double cx, double cy, double zoom, bool center);
        public abstract void Zoom(double zoom);
        public abstract void Regenerate();

#endregion

#region ICursor

        protected double _stepSize = .1;
        protected Point _lastCursorPoint;
        protected Point _cursorLocation = new Point(0, 0);

        protected DateTime _clickTime = DateTime.MinValue;
        protected Point _clickLoc = new Point(0, 0);
        protected TimeSpan _dbcTime = new TimeSpan(0, 0, 0, 0, 400);
        protected bool _doubleClick = false;

        protected bool _gmkEnabled = true;

        protected int _keyDirection = 0;
        protected int _keyCount = 0;
        protected int _keySteps = 1;

        public virtual Point CursorLocation
        {
            get
            {
                return _cursorLocation;
            }
        }

        public abstract bool AcquireCursor { get; set; }
        public abstract void GhostCursor();
        public abstract bool CursorVisible { set; }
        public abstract double CursorSize { get; set; }

        public virtual IVectorCanvas VectorListControl
        {
            get { return null; }
        }

        public abstract void SelectCursor(CursorType cursorType);
        public abstract void MoveCursorBy(double dx, double dy);
        public abstract void MoveCursorTo(double x, double y);
        public abstract Point ResetCursorPosition(bool moveOnScreen);

        public abstract void WindowChanged();

        private Queue<InputPoint> _inputPoints = new Queue<InputPoint>();

        public void PushPoint(InputPoint inputPoint)
        {
            _inputPoints.Enqueue(inputPoint);
        }

        public InputPoint PopPoint()
        {
            InputPoint ip = null;
            if (_inputPoints.Count > 0)
            {
                ip = _inputPoints.Dequeue();

                //if (ip != null && ip.Mode != CoordinateMode.End)
                //{
                //    Point p = Utilities.CoordinateToPoint(ip.Mode, ip.V1, ip.V2);
                //    if (ip.Mode != CoordinateMode.Absolute)
                //    {
                //        ip.Mode = CoordinateMode.Absolute;
                //        ip.V1 = p.X;
                //        ip.V2 = p.Y;
                //    }
                //}
            }

            return ip;
        }

        #endregion
    }
}
