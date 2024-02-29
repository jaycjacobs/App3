using Cirros;
using Cirros.Core;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace CirrosUI
{
    public sealed partial class ColorComboBox : UserControl
    {
        public event ColorChangedHandler OnColorChanged;
        public delegate void ColorChangedHandler(object sender, ColorChangedEventArgs e);

        public event ActivateHandler OnActivate;
        public delegate void ActivateHandler(object sender, EventArgs e);
        ListBox _menuDropDown = new ListBox();

        public ColorComboBox()
        {
            this.InitializeComponent();

            DataContext = Globals.UIDataContext;

            _menuDropDown.SizeChanged += _menuDropDown_SizeChanged;
            _menuDropDown.HorizontalAlignment = HorizontalAlignment.Stretch;
            _menuDropDown.VerticalAlignment = VerticalAlignment.Stretch;
            _menuDropDown.Background = new SolidColorBrush(Colors.White);
            _menuDropDown.BorderBrush = new SolidColorBrush(Utilities.ColorFromColorSpec(0xffafafaf));
            _menuDropDown.SelectionChanged += _menuDropDown_SelectionChanged;

            this.PointerPressed += _comboBox_PointerPressed;
            this.PointerReleased += _comboBox_PointerReleased;
        }

        List<uint> _colorspecs = new List<uint>();

        public void SetValues(List<uint> colorspecs)
        {
            _colorspecs = colorspecs;
        }

        private void ShowMenu()
        {
            _menuDropDown.Items.Clear();

            foreach (uint colorspec in _colorspecs)
            {
                ListBoxItem item = new ListBoxItem();

                if (colorspec == (uint)ColorCode.SetColor)
                {
                    item.Content = "Select a new color...";
                    item.Tag = colorspec;
                    item.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                    item.FontStyle = Windows.UI.Text.FontStyle.Italic;
                    _menuDropDown.Items.Add(item);
                }
                else
                {
                    item.Content = new ColorItemControl(colorspec);
                    item.Tag = colorspec;
                    item.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                    _menuDropDown.Items.Add(item);
                }
            }

            foreach (uint colorspec in Globals.RecentColors)
            {
                ListBoxItem item = new ListBoxItem();

                item.Content = new ColorItemControl(colorspec);
                item.Tag = colorspec;
                item.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                _menuDropDown.Items.Add(item);
            }

            _menuDropDown.MinWidth = this.ActualWidth;

            _menuPopup.Child = _menuDropDown;
            _menuPopup.IsOpen = true;
        }

        private void _menuDropDown_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_menuDropDown.ActualWidth == e.NewSize.Width && _menuDropDown.ActualHeight == e.NewSize.Height)
            {
                return;
            }
            if (Cirros.Utility.Utilities.__checkSizeChanged(42, sender)) return;

            GeneralTransform tf = this.TransformToVisual(null);
            Point p = tf.TransformPoint(new Point());

            double top = p.Y;
            double bottom = top + _menuDropDown.ActualHeight;

            if (bottom > App.Window.CoreWindow.Bounds.Bottom)
            {
                _menuPopup.VerticalOffset = App.Window.CoreWindow.Bounds.Bottom - bottom;
            }
        }

        public uint ColorSpec
        {
            get { return _colorItem.ColorSpec; }
            set
            {
                if (_colorItem.ColorSpec != value)
                {
                    _colorItem.ColorSpec = value;

                    if (_colorItem.ColorSpec == (uint)ColorCode.ThemeForeground)
                    {
                        _colorItem.ColorName = "Default foreground";
                    }
                }
            }
        }

        public string ColorName
        {
            get { return _colorItem.ColorName; }
            set { _colorItem.ColorName = value; }
        }

        private void _menuDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            uint colorspec = _colorItem.ColorSpec;

            if (_menuDropDown.SelectedItem is ListBoxItem)
            {
                if (((ListBoxItem)_menuDropDown.SelectedItem).Content is ColorItemControl)
                {
                    ColorItemControl cic = ((ListBoxItem)_menuDropDown.SelectedItem).Content as ColorItemControl;
                    colorspec = cic.ColorSpec;
                }
                else if (((ListBoxItem)_menuDropDown.SelectedItem).Tag is uint)
                {
                    colorspec = (uint)((ListBoxItem)_menuDropDown.SelectedItem).Tag;
                }
            }

            _menuPopup.IsOpen = false;

            if (OnColorChanged != null)
            {
                if (colorspec == (uint)ColorCode.SetColor)
                {
                    _menuPopup.IsLightDismissEnabled = true;

                    ColorPicker colorPicker = new ColorPicker();
                    colorPicker.Width = 350;

                    Brush dark = (Brush)(Application.Current.Resources["SettingsDarkForeground"]);
                    colorPicker.ShowBorder(dark, 3);
                    colorPicker.Color = _colorItem.ColorValue;

                    colorPicker.OnColorSelected += ColorPicker_OnColorSelected;

                    if (!_menuPopup.IsOpen)
                    {
                        _menuPopup.Child = colorPicker as UserControl;
                        _menuPopup.VerticalOffset = _colorButton.ActualHeight;
                        _menuPopup.IsOpen = true;
                    }
                }
                else
                {
                    _colorItem.ColorSpec = colorspec;
                    ColorChangedEventArgs ee = new ColorChangedEventArgs(_colorItem.ColorSpec);
                    OnColorChanged(this, ee);
                }
            }
        }

        private void ColorPicker_OnColorSelected(object sender, ColorSelectedEventArgs e)
        {
            _colorItem.ColorSpec = Utilities.ColorSpecFromColor(e.Color);
            //Globals.PushRecentColor(_colorItem.ColorSpec);

            ColorChangedEventArgs ee = new ColorChangedEventArgs(_colorItem.ColorSpec);
            OnColorChanged(this, ee);
        }

        private void _comboBox_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            CapturePointer(e.Pointer);
            _border.Background = new SolidColorBrush(Utilities.ColorFromColorSpec(0xFFD3D3D3));
            e.Handled = true;
        }

        private void _comboBox_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _border.Background = new SolidColorBrush(Colors.White);

            ShowMenu();

            OnActivate?.Invoke(this, new EventArgs());

            _menuDropDown.MinWidth = this.ActualWidth;
            _menuPopup.IsOpen = true;
        }
    }

    public class ColorChangedEventArgs : EventArgs
    {
        private uint _colorSpec;

        public ColorChangedEventArgs(uint colorSpec)
        {
            _colorSpec = colorSpec;
        }

        public uint ColorSpec
        {
            get
            {
                return _colorSpec;
            }
            set
            {
                _colorSpec = value;
            }
        }
    }
}
