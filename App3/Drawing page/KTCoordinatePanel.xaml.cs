using Cirros;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Windows.System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace KT22.Drawing_page
{
    public sealed partial class KTCoordinatePanel : UserControl
    {
        //int _keyDirection = 0;
        //int _keyCount = 0;
        //int _keySteps = 1;

        CoordinateMode _coordinateMode = CoordinateMode.Absolute;

        Unit _modelUnit = Unit.Millimeters;
        bool _architect = false;

        public KTCoordinatePanel()
        {
            this.InitializeComponent();

            this.Loaded += KTCoordinatePanel_Loaded;
            this.Unloaded += KTCoordinatePanel_Unloaded;
            this.PointerPressed += KTCoordinatePanel_PointerPressed;
            this.PointerMoved += KTCoordinatePanel_PointerMoved;
            this.PointerReleased += KTCoordinatePanel_PointerReleased;

            Globals.Events.OnCoordinateDisplay += Events_OnCoordinateDisplay;
            Globals.Events.OnOptionsChanged += Events_OnOptionsChanged;
            Globals.Events.OnCoordinateEntry += Events_OnCoordinateEntry;

            DataContext = Globals.UIDataContext;
        }

        private void KTCoordinatePanel_Loaded(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void KTCoordinatePanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Globals.Events.OnCoordinateDisplay -= Events_OnCoordinateDisplay;
            Globals.Events.OnOptionsChanged -= Events_OnOptionsChanged;
            Globals.Events.OnCoordinateEntry -= Events_OnCoordinateEntry;
        }

        private void Events_OnCoordinateEntry(object sender, EventArgs e)
        {
            _coordinateEntryBox.Focus(FocusState.Programmatic);

            Globals.Input.ResetCursorPosition(true);
        }

        void Events_OnOptionsChanged(object sender, EventArgs e)
        {
            Update();
        }

        void Events_OnCoordinateDisplay(object sender, CoordinateDisplayEventArgs e)
        {
            Point model = Globals.ActiveDrawing.PaperToModel(e.Point);
            int round = _architect ? 128 : Globals.DimensionRound;

            _xValue.Text = Utilities.FormatDistance(model.X, round, _architect, true, _modelUnit, false);
            _yValue.Text = Utilities.FormatDistance(model.Y, round, _architect, true, _modelUnit, false);

            switch (e.CoordinateDisplayType)
            {
                case CoordinateDisplayType.coordinate:
                    //string z = Utilities.FormatDistance(0, round, _architect, true, _modelUnit, false);
                    _dxValue.Text = " ";
                    _dyValue.Text = " ";
                    _distanceValue.Text = " ";
                    _angleValue.Text = " ";
                    break;
                case CoordinateDisplayType.vector:
                    Point delta = Globals.ActiveDrawing.PaperToModelDelta(new Point(e.Width, e.Height));
                    _dxValue.Text = Utilities.FormatDistance(delta.X, round, _architect, true, _modelUnit, false);
                    _dyValue.Text = Utilities.FormatDistance(delta.Y, round, _architect, true, _modelUnit, false);
                    _distanceValue.Text = string.Format("{0:#.####}", Globals.ActiveDrawing.PaperToModel(e.Distance));
                    _angleValue.Text = string.Format("{0}°", -Math.Round(e.Angle * Construct.cRadiansToDegrees, 3));
                    break;
                case CoordinateDisplayType.size:
                    break;
            }

            if ((bool)_coordEntryDisplayToggle.IsChecked)
            {
                switch (_coordinateMode)
                {
                    case CoordinateMode.Absolute:
                        _coordinateEntryBox.PlaceholderText = string.Format("{0} {1}", _xValue.Text, _yValue.Text);
                        break;
                    case CoordinateMode.Delta:
                        _coordinateEntryBox.PlaceholderText = string.Format("{0} {1}", _dxValue.Text, _dyValue.Text);
                        break;
                    case CoordinateMode.Polar:
                        _coordinateEntryBox.PlaceholderText = string.Format("{0} {1}", _distanceValue.Text, _angleValue.Text);
                        break;
                }
            }
            else
            {
                _coordinateEntryBox.PlaceholderText = "";
            }
        }

        void Update()
        {
            if (Globals.ActiveDrawing != null)
            {
                _modelUnit = Globals.ActiveDrawing.ModelUnit;
                _architect = Globals.ActiveDrawing.IsArchitecturalScale && Globals.Input.GridSnap;
            }
        }

        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;

        private void KTCoordinatePanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (Parent is Popup popup)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _dragXOff = p.X - popup.HorizontalOffset;
                _dragYOff = p.Y - popup.VerticalOffset;
                _isDragging = true;

                CapturePointer(e.Pointer);
            }
        }

        private void KTCoordinatePanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging && Parent is Popup popup)
            {
                Point p = e.GetCurrentPoint(null).Position;
                popup.HorizontalOffset = p.X - _dragXOff;
                popup.VerticalOffset = p.Y - _dragYOff;
            }
        }

        private void KTCoordinatePanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _isDragging = false;
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Parent is Popup popup)
            {
                popup.IsOpen = false;
            }
        }

        private void SetCoordinateMode(CoordinateMode mode)
        {
            _coordinateMode = mode;

            switch (_coordinateMode)
            {
                case CoordinateMode.Absolute:
                    _promptText.Text = "Enter absolute coordinates: X, Y";
                    _absoluteButton.IsChecked = true;
                    _relativeButton.IsChecked = false;
                    _polarButton.IsChecked = false;
                    break;

                case CoordinateMode.Delta:
                    _promptText.Text = "Enter relative coordinates: DX, DY";
                    _absoluteButton.IsChecked = false;
                    _relativeButton.IsChecked = true;
                    _polarButton.IsChecked = false;
                    break;

                case CoordinateMode.Polar:
                    _promptText.Text = "Enter polar coordinates: distance, angle";
                    _absoluteButton.IsChecked = false;
                    _relativeButton.IsChecked = false;
                    _polarButton.IsChecked = true;
                    break;
            }
        }

        private void _absoluteButton_Click(object sender, RoutedEventArgs e)
        {
            SetCoordinateMode(CoordinateMode.Absolute);
        }

        private void _relativeButton_Click(object sender, RoutedEventArgs e)
        {
            SetCoordinateMode(CoordinateMode.Delta);
        }

        private void _polarButton_Click(object sender, RoutedEventArgs e)
        {
            SetCoordinateMode(CoordinateMode.Polar);
        }

        private void _leftButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _upButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _downButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _rightButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _enterButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _coordinateEntryBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CoordinateMode mode;
            double d1, d2;

            if (_coordinateEntryBox.Text == "[")
            {
                _coordinateEntryBox.Text = "";
            }
            else
            {
                string s = _coordinateEntryBox.Text.Trim();

                if ("undo".Equals(s) || "redo".Equals(s) || "done".Equals(s))
                {
                    _coordinateEntryBox.Foreground = new SolidColorBrush(Colors.Black);
                }
                else if (ParseCoordinate(s, out mode, out d1, out d2))
                {
                    _coordinateEntryBox.Foreground = new SolidColorBrush(Colors.Black);

                    if (mode != _coordinateMode)
                    {
                        SetCoordinateMode(mode);
                    }
                }
                else
                {
                    _coordinateEntryBox.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
        }

        private void _coordinateEntryBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                EnterKeyboardCoordinate();
            }
        }

        private void _coordinateEntryBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Globals.CommandProcessor != null && Globals.Input != null)
            {
                _coordinateEntryBox.PlaceholderText = "";

                //Globals.Input.ResetCursorPosition(true);
                Globals.CommandProcessor.PointerEnteredDrawingArea();
                Globals.Input.CursorVisible = true;

                Globals.Input.GhostCursor();
            }
        }

        private bool ParseCoordinate(string s, out CoordinateMode cmode, out double d1, out double d2)
        {
            string c1 = "";
            string c2 = "";
            bool good = true;
            string mode = "";
            cmode = _coordinateMode;

            d1 = 0;
            d2 = 0;

            int n = s.IndexOfAny(new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '+' });
            if (n > 0)
            {
                mode = s.Substring(0, n - 1).ToLower();
                s = s.Substring(n);
            }

            int comma = s.IndexOf(',');
            int dq = Globals.ActiveDrawing.IsArchitecturalScale ? s.IndexOf('"') : -1;

            if (comma >= 0)
            {
                string[] pa = s.Split(new[] { ',' });
                c1 = pa[0];
                c2 = pa[1];
            }
            else if (dq >= 0)
            {
                c1 = s.Substring(0, dq + 1);
                c2 = s.Substring(dq + 1);
            }
            else
            {
                string regex = @"\s*(?<mode>([a-zA-Z]+))?\s*,?\s*(?<coord1>([\d.\-\+'\/" + Regex.Escape("\"") + @"]+))?\s*,?\s*(?<coord2>([\d.\-\+'\/" + Regex.Escape("\"") + @"]+))?\s?(?<extra>(.)?)";
                string extra = "";

                Match m = Regex.Match(s.Trim(), regex);

                if (m.Groups["mode"].Success)
                {
                    mode = m.Groups["mode"].Value.ToLower();
                }
                if (m.Groups["coord1"].Success)
                {
                    c1 = m.Groups["coord1"].Value;
                }
                if (m.Groups["coord2"].Success)
                {
                    c2 = m.Groups["coord2"].Value;
                }
                if (m.Groups["extra"].Success)
                {
                    extra = m.Groups["extra"].Value;
                    good = extra.Length == 0;
                }
            }


            if (good == false)
            {
                // parse error
            }
            else if (mode.Length == 0)
            {
                // no change to mode
            }
            else if ("absolute".StartsWith(mode))
            {
                cmode = CoordinateMode.Absolute;
            }
            else if ("delta".StartsWith(mode))
            {
                cmode = CoordinateMode.Delta;
            }
            else if ("relative".StartsWith(mode))
            {
                cmode = CoordinateMode.Delta;
            }
            else if ("polar".StartsWith(mode))
            {
                cmode = CoordinateMode.Polar;
            }
            else
            {
                // invalid coordinate mode
                good = false;
            }

            if (good)
            {
                if (cmode == CoordinateMode.Absolute)
                {
                    // default x and y to cursor location
                    Point model = Globals.ActiveDrawing.PaperToModel(Globals.Input.CursorLocation);
                    d1 = model.X;
                    d2 = model.Y;
                }

                try
                {
                    if (good && c1.Length > 0 && c1 != "-")
                    {
                        if (_architect)
                        {
                            d1 = Utilities.ParseArchitectDistance(c1);
                        }
                        else
                        {
                            d1 = double.Parse(c1);
                        }
                    }

                    if (good && c2.Length > 0 && c2 != "-")
                    {
                        if (_coordinateMode == CoordinateMode.Polar)
                        {
                            d2 = double.Parse(c2);
                        }
                        else if (_architect)
                        {
                            d2 = Utilities.ParseArchitectDistance(c2);
                        }
                        else
                        {
                            d2 = double.Parse(c2);
                        }
                    }
                }
                catch
                {
                    // invalid numeric value
                    good = false;
                }
            }

            return good;
        }

        private void EnterPoint(double d1, double d2)
        {
            if (_coordinateMode == CoordinateMode.Absolute)
            {
                Point p = Globals.ActiveDrawing.ModelToPaper(new Point(d1, d2));
                Point current = Globals.Input.CursorLocation;
                double dx = p.X - current.X;
                double dy = p.Y - current.Y;
                Globals.Input.MoveCursorBy(dx, dy);
                Globals.Input.EnterPoint();
            }
            else if (_coordinateMode == CoordinateMode.Delta)
            {
                Point p = Globals.ActiveDrawing.ModelToPaperDelta(new Point(d1, d2));

                if (Globals.CommandProcessor != null)
                {
                    Point current = Globals.Input.CursorLocation;
                    Point f = Globals.CommandProcessor.Anchor;
                    Point t = new Point(f.X + p.X, f.Y + p.Y);

                    double dx = t.X - current.X;
                    double dy = t.Y - current.Y;

                    Globals.Input.MoveCursorBy(dx, dy);
                    Globals.Input.EnterPoint();
                }
                else
                {
                    Globals.Input.ResetCursorPosition(false);
                    Globals.Input.MoveCursorBy(p.X, p.Y);
                    Globals.Input.EnterPoint();
                }
            }
            else if (_coordinateMode == CoordinateMode.Polar)
            {
                double d = Globals.ActiveDrawing.ModelToPaper(d1);
                Point p = Construct.PolarOffset(new Point(0, 0), d, -d2 / Construct.cRadiansToDegrees);

                if (Globals.CommandProcessor != null)
                {
                    Point current = Globals.Input.CursorLocation;
                    Point f = Globals.CommandProcessor.Anchor;
                    Point t = new Point(f.X + p.X, f.Y + p.Y);

                    double dx = t.X - current.X;
                    double dy = t.Y - current.Y;

                    Globals.Input.MoveCursorBy(dx, dy);
                    Globals.Input.EnterPoint();
                }
                else
                {
                    Globals.Input.ResetCursorPosition(false);
                    Globals.Input.MoveCursorBy(p.X, p.Y);
                    Globals.Input.EnterPoint();
                }
            }

            Globals.Input.GhostCursor();
        }

        private void EnterKeyboardCoordinate()
        {
            if (_coordinateEntryBox.Text.Length > 0)
            {
                CoordinateMode mode;
                double d1, d2;
                string s = _coordinateEntryBox.Text.Trim().ToLower();

                if ("undo".Equals(s))
                {
                    Globals.CommandDispatcher.Undo();
                    _coordinateEntryBox.Text = "";
                }
                else if ("redo".Equals(s))
                {
                    Globals.CommandDispatcher.Redo();
                    _coordinateEntryBox.Text = "";
                }
                else if ("done".Equals(s))
                {
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Finish();
                    }
                    _coordinateEntryBox.Text = "";
                    _coordinateEntryBox.Focus(FocusState.Keyboard);
                }
                else if (ParseCoordinate(s, out mode, out d1, out d2))
                {
                    if (mode != _coordinateMode)
                    {
                        SetCoordinateMode(mode);
                    }

                    // enter the point
                    EnterPoint(d1, d2);

                    _coordinateEntryBox.Text = "";
                }
            }
        }
    }
}
