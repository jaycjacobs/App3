using Cirros;
using Cirros.Core;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace CirrosUI
{
    public sealed partial class ColorPicker : UserControl
    {
        public event ColorSelectedHandler OnColorSelected;
        public delegate void ColorSelectedHandler(object sender, ColorSelectedEventArgs e);

        private Color _color;
        private string _colorName;

        ColorNameItemCollection _filteredColorNameList = new ColorNameItemCollection();

        public ColorPicker()
        {
            this.InitializeComponent();
            this.Loaded += ColorPicker_Loaded;
            this.Unloaded += ColorPicker_Unloaded;

            this.PointerPressed += ColorPicker_PointerPressed;
            DataContext = Globals.UIDataContext;

            SizeChanged += ColorPicker_SizeChanged;
        }

        private void ColorPicker_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ShowColorNameDropDown(false);
        }

        private void ColorPicker_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Cirros.Utility.Utilities.__checkSizeChanged(44, sender)) return;

            GridLength gl = new GridLength(Globals.UIDataContext.UIControlHeightNormal + 2);
            _rowR.Height = gl;
            _rowG.Height = gl;
            _rowB.Height = gl;
            _rowA.Height = gl;

            try
            {
                double sw = Cirros.XamlUtilities.StringWidth("Light Goldenrod Yellow", "Segoe UI", Globals.UIDataContext.UIFontSizeNormal);
                sw += 50;

                if (sw != _colorNameColumn.Width.Value)
                {
                    _colorNameColumn.Width = new GridLength(sw/* + 50*/);
                }

                double w = 0;

                foreach (ColumnDefinition cd in _grid.ColumnDefinitions)
                {
                    if (cd.Width.IsAbsolute)
                    {
                        w += cd.Width.Value;
                    }
                    else
                    {
                        w += cd.ActualWidth;
                    }
                }

                this.Width = w;

                double h = 0;

                foreach (RowDefinition rd in _grid.RowDefinitions)
                {
                    if (rd.Height.IsAbsolute)
                    {
                        h += rd.Height.Value;
                    }
                    else
                    {
                        h += rd.ActualHeight;
                    }
                }

                this.Height = h + 10;
            }
            catch
            {
            }
        }

        void ColorPicker_Unloaded(object sender, RoutedEventArgs e)
        {
            _filteredColorNameList.Clear();
           
            Globals.PushRecentColor(Utilities.ColorSpecFromColor(_color));
        }

        void ColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateColorList();
            _colorDropDown.ItemsSource = _filteredColorNameList;
            _colorDropDownScrollToSelection = true;
            _colorDropDown.UpdateLayout();

            _colorDropDown.LayoutUpdated += _colorDropDown_LayoutUpdated;
            _nameBox.GotFocus += _nameBox_GotFocus;
        }

        void _nameBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ShowColorNameDropDown(true);
        }

        public void ShowBorder(Brush brush, double thickness)
        {
            _border.BorderBrush = brush;
            _border.BorderThickness = new Thickness(thickness);
        }

        private void ShowColorNameDropDown(bool show)
        {
            if (show == false && _colorDropDown.Visibility == Visibility.Visible)
            {
                _colorDropDown.Visibility = Visibility.Collapsed;
            }
            else if (show && _colorDropDown.Visibility == Visibility.Collapsed)
            {
                string lcname = _nameBox.Text.ToLower();

                if (lcname.Length > 0 && char.IsDigit(lcname[0]))
                {
                    PopulateAutocadColorList();
                    int value;

                    
                    if (int.TryParse(lcname.Replace(" (acad)", ""), out value) && value < 256)
                    {
                        if (_filteredColorNameList.Count > value)
                        {
                            _colorDropDown.SelectedIndex = value;
                        }
                    }
                }
                else if (StandardColors.ColorNames.ContainsKey(lcname))
                {
                    PopulateColorList();

                    foreach (ColorNameItem item in _filteredColorNameList)
                    {
                        if (item.LCName == lcname)
                        {
                            _colorDropDown.SelectedItem = item;
                            break;
                        }
                    }
                }

                _colorDropDownScrollToSelection = true;
                _colorDropDown.UpdateLayout();
                _colorDropDown.Visibility = Visibility.Visible;
            }
        }

        void _colorDropDown_LayoutUpdated(object sender, object e)
        {
            if (_colorDropDownScrollToSelection && _colorDropDown.SelectedItem != null)
            {
                _colorDropDown.ScrollIntoView(_colorDropDown.SelectedItem);
                _colorDropDownScrollToSelection = false;
            }
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (sender is Slider && ((Slider)sender).FocusState != FocusState.Unfocused)
            {
                Color color = new Color();

                color.A = Convert.ToByte(_alphaSlider.Value);
                color.R = Convert.ToByte(_redSlider.Value);
                color.G = Convert.ToByte(_greenSlider.Value);
                color.B = Convert.ToByte(_blueSlider.Value);

                SetColor(color, false, true, true);
            }
        }

        public Color Color
        {
            get { return _color; }
            set { SetColor(value, true, true, true);  }
        }

        public void ColorSelected(Color color, string colorName, bool finalSelection)
        {
            if (OnColorSelected != null)
            {
                OnColorSelected(this, new ColorSelectedEventArgs(color, colorName, finalSelection));
            }
        }

        List<string> _sortedColorNameList = null;

        void PopulateColorList(string filter = null)
        {
            _filteredColorNameList.Clear();

            if (filter != null)
            {
                filter = filter.ToLower();
            }

            if (_sortedColorNameList == null)
            {
                _sortedColorNameList = new List<string>();

                foreach (string name in StandardColors.Colors.Values)
                {
                    if (filter == null || name.ToLower().Contains(filter))
                    {
                        _sortedColorNameList.Add(name);
                    }
                }

                // Hack: here are two duplicate colors in the CSS3 color name set that are not in the StandardColors.Colors dictionary
                // yes, we could use StandardColors.ColorNames.Keys but they are lower case

                _sortedColorNameList.Add("Aqua");
                _sortedColorNameList.Add("Fuchsia");

                _sortedColorNameList.Sort();
            }

            foreach (string name in _sortedColorNameList)
            {
                if (filter == null)
                {
                    _filteredColorNameList.Add(new ColorNameItem(name));
                }
                else if (name.ToLower().Contains(filter))
                {
                    _filteredColorNameList.Add(new ColorNameItem(name));
                }
            }
        }

        void PopulateAutocadColorList()
        {
            _filteredColorNameList.Clear();

            for (int i = 0; i < 256; i++)
            {
                string name = string.Format("{0} (acad)", i);
                uint colorspec = Utilities.ColorSpecFromAutoCadColor(i);
                Color color = Utilities.ColorFromColorSpec(colorspec);

                _filteredColorNameList.Add(new ColorNameItem(name, color, colorspec));
            }
        }

        private void _nameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox && ((TextBox)sender).FocusState != FocusState.Unfocused)
            {
                if (_nameBox.Text.StartsWith("#"))
                {
                    if (_nameBox.Text.Length > 9)
                    {
                        // error - too long
                        // this should be caught in _nameBox_KeyDown
                        _nameBox.Text = _nameBox.Text.Substring(0, 9);
                    }
                    else
                    {
                        string s = _nameBox.Text.TrimEnd().Substring(1);
                        string h = "00000000";

                        if (s.Length == 8)
                        {
                            h = s;
                        }
                        else if (s.Length > 0)
                        {
                            h = string.Concat(s, "00000000".Substring(0, 8 - s.Length));
                        }

                        int result;

                        if (int.TryParse(h, NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo, out result))
                        {
                            var color = new Color();
                            color.A = byte.Parse(h.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                            color.R = byte.Parse(h.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                            color.G = byte.Parse(h.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                            color.B = byte.Parse(h.Substring(6, 2), NumberStyles.AllowHexSpecifier);

                            SetColor(color, true, true, false);
                        }
                    }
                }
                else if (_nameBox.Text.Length > 0 && char.IsDigit(_nameBox.Text[0]))
                {
                    int value;

                    if (_filteredColorNameList.Count == 0 || char.IsDigit(_filteredColorNameList[0].LCName[0]) == false)
                    {
                        PopulateAutocadColorList();
                        _colorDropDown.UpdateLayout();
                    }

                    if (int.TryParse(_nameBox.Text, out value) && value <= 255)
                    {
                        uint colorspec = Utilities.ColorSpecFromAutoCadColor(value);
                        SetColor(Utilities.ColorFromColorSpec(colorspec), true, true, false);

                        if (_colorDropDown.Items.Count > value)
                        {
                            _colorDropDownScrollToSelection = true;
                            _colorDropDownExitOnSelection = false;

                            _colorDropDown.SelectedIndex = value;

                            _colorDropDownExitOnSelection = true;
                        }
                    }
                }
                else
                {
                    string s = _nameBox.Text.ToLower();
                    if (StandardColors.ColorNames.ContainsKey(s))
                    {
                        SetColor(Utilities.ColorFromColorSpec(StandardColors.ColorNames[s]), true, true, false);
                    }

                    if (_colorDropDown.Visibility == Visibility.Visible)
                    {
                        // if the dropdown is visible while the user is typing in _nameBox
                        // filter the contents of the dropdown
                        PopulateColorList(_nameBox.Text);
                    }
                }
            }
        }

        private void SetColor(Color color, bool setSliders, bool setComponents, bool setName)
        {
            if (color != _color)
            {
                _color = color;

                if (setComponents)
                {
                    _alphaValue.Value = _color.A;
                    _redValue.Value = _color.R;
                    _greenValue.Value = _color.G;
                    _blueValue.Value = _color.B;
                }

                if (setSliders)
                {
                    _alphaSlider.Value = _color.A;
                    _redSlider.Value = _color.R;
                    _greenSlider.Value = _color.G;
                    _blueSlider.Value = _color.B;
                }

                uint colorspec = Utilities.ColorSpecFromColor(color);
                _colorName = Utilities.ColorNameFromColorSpec(colorspec);

                if (setName && _nameBox.Text != _colorName)
                {
                    _nameBox.Text = _colorName;
                }

                _colorSample.Fill = new SolidColorBrush(_color);

                if (_color.R < 200 && _color.G < 200 && _color.A > 200)
                {
                    _colorSampleCheck.Foreground = new SolidColorBrush(Colors.White);
                }
                else
                {
                    _colorSampleCheck.Foreground = new SolidColorBrush(Colors.Black);
                }

                ColorSelected(_color, _colorName, false);
            }
        }

        private void _nameBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ShowColorNameDropDown(false);
            }
            else if (_nameBox.Text.StartsWith("#"))
            {
                if (_nameBox.Text.Length >= 9 && _nameBox.SelectionLength == 0)
                {
                    e.Handled = true;
                }
                else
                {
                    switch (e.Key)
                    {
                        case Windows.System.VirtualKey.Number0:
                        case Windows.System.VirtualKey.Number1:
                        case Windows.System.VirtualKey.Number2:
                        case Windows.System.VirtualKey.Number3:
                        case Windows.System.VirtualKey.Number4:
                        case Windows.System.VirtualKey.Number5:
                        case Windows.System.VirtualKey.Number6:
                        case Windows.System.VirtualKey.Number7:
                        case Windows.System.VirtualKey.Number8:
                        case Windows.System.VirtualKey.Number9:
                        case Windows.System.VirtualKey.A:
                        case Windows.System.VirtualKey.B:
                        case Windows.System.VirtualKey.C:
                        case Windows.System.VirtualKey.D:
                        case Windows.System.VirtualKey.E:
                        case Windows.System.VirtualKey.F:
                            break;

                        default:
                            e.Handled = true;
                            break;
                    }
                }
            }
        }

        private void HighlightColorBox(bool flag)
        {
            _colorSample.Fill.Opacity = flag ? .5 : 1;
        }

        private void _colorSample_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _pointerIsPressedInColorBox = true;
            HighlightColorBox(_pointerIsInColorBox && _pointerIsPressedInColorBox);
        }

        private void _colorSample_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _pointerIsPressedInColorBox = false;
            HighlightColorBox(false);

            ColorSelected(_color, _colorName, true);
        }

        bool _pointerIsInColorBox = false;
        bool _pointerIsPressedInColorBox = false;

        private void _colorSample_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _pointerIsInColorBox = true;
            HighlightColorBox(_pointerIsInColorBox && _pointerIsPressedInColorBox);
        }

        private void _colorSample_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _pointerIsInColorBox = false;
            HighlightColorBox(_pointerIsInColorBox && _pointerIsPressedInColorBox);
        }

        private void _colorDropDownButton_Click(object sender, RoutedEventArgs e)
        {
            ShowColorNameDropDown(_colorDropDown.Visibility == Visibility.Collapsed);
        }

        bool _colorDropDownExitOnSelection = true;
        bool _colorDropDownScrollToSelection = false;

        private void _colorDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_colorDropDownExitOnSelection && _colorDropDown.Visibility == Visibility.Visible)
            {
                if (_colorDropDown.SelectedItem is ColorNameItem)
                {
                    ColorNameItem item = _colorDropDown.SelectedItem as ColorNameItem;
                    SetColor(item.Color, true, true, true);

                    _colorDropDown.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void _colorComponent_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (sender is NumberBox1)
            {
                Color color = _color;

                NumberBox1 n = sender as NumberBox1;
                switch ((string)n.Tag)
                {
                    case "R":
                        color.R = (byte)n.Value;
                        break;

                    case "G":
                        color.G = (byte)n.Value;
                        break;

                    case "B":
                        color.B = (byte)n.Value;
                        break;

                    case "A":
                        color.A = (byte)n.Value;
                        break;

                    default:
                        break;
                }

                SetColor(color, true, false, true);
            }
        }
    }

    public class ColorSelectedEventArgs : EventArgs
    {
        public Color Color { get; private set; }
        public string ColorName { get; private set; }
        public bool FinalSelection { get; private set; }

        public ColorSelectedEventArgs(Color color, string colorName, bool finalSelection)
        {
            Color = color;
            ColorName = colorName;
            FinalSelection = finalSelection;
        }
    }
}
