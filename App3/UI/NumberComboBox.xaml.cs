using Cirros;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using App3;
using Microsoft.UI;

namespace CirrosUI
{
    public sealed partial class NumberComboBox : UserControl
    {
        public event ValueChangedHandler OnValueChanged;
        public delegate void ValueChangedHandler(object sender, ValueChangedEventArgs e);

        public event ActivateHandler OnActivate;
        public delegate void ActivateHandler(object sender, EventArgs e);

        double _currentValue = -1;

        bool _isMetric = false;

        public NumberComboBox()
        {
            this.InitializeComponent();

            DataContext = Globals.UIDataContext;

            this.Loaded += NumberComboBox_Loaded;

            _menuDropDown.SizeChanged += _menuDropDown_SizeChanged;
            _numberBox.GotFocus += _numberBox_GotFocus;
            _numberBox.OnValueChanged += _numberBox_OnValueChanged;
            _numberBox.KeyDown += _numberBox_KeyDown;
            _numberBox.PointerPressed += _numberBox_PointerPressed;

            this.PointerPressed += _comboBox_PointerPressed;
            this.PointerReleased += _comboBox_PointerReleased;

            _numberBox.StripTrailingZeros = false;
        }

        private void NumberComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            Thickness t = _textBox.Padding;
            t.Left = t.Right * .6;
            _textBox.Padding = t;
        }

        public int SelectedIndex
        {
            set
            {
                if (value >= 0 && value < _menuDropDown.Items.Count)
                {
                    _menuDropDown.SelectedIndex = value;
                }
           }
        }

        public int Precision
        {
            get { return _numberBox.Precision; }
            set { _numberBox.Precision = value; }
        }

        private void _numberBox_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            OnActivate?.Invoke(this, new EventArgs());
        }

        private void _menuDropDown_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize == e.NewSize) return;
            if (Cirros.Utility.Utilities.__checkSizeChanged(48, sender)) return;

            Thickness t = _textBox.Padding;
            t.Left = t.Right * .6;
            _textBox.Padding = t;

            GeneralTransform tf = this.TransformToVisual(null);
            Point p = tf.TransformPoint(new Point());

            double top = p.Y;
            double bottom = top + _menuDropDown.ActualHeight;

            if (bottom > App.Window.CoreWindow.Bounds.Bottom)
            {
                _menuPopup.VerticalOffset = App.Window.CoreWindow.Bounds.Bottom - bottom;
            }
        }

        public bool IsMetric
        {
            set { _isMetric = value; }
        }

        public double MinValue
        {
            set { _numberBox.MinValue = value; }
        }

        public void SetValues(List<double> values)
        {
            _menuDropDown.Items.Clear();

            foreach (double v in values)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = (_isMetric ? v * 25.4 : v).ToString();
                item.Tag = v;
                item.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                _menuDropDown.Items.Add(item);
            }
        }

        public void SetValues(Dictionary<string, double> items)
        {
            _menuDropDown.Items.Clear();

            foreach (string key in items.Keys)
            {
                double v = items[key];
                ListBoxItem item = new ListBoxItem();
                item.Content = key;
                item.Tag = v;
                item.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                _menuDropDown.Items.Add(item);
            }
        }

        public void SetValues(Dictionary<string, object> items, bool italicizeStrings)
        {
            _menuDropDown.Items.Clear();

            foreach (string key in items.Keys)
            {
                object v = items[key];
                ListBoxItem item = new ListBoxItem();
                item.Content = key;
                item.Tag = v;
                item.FontSize = Globals.UIDataContext.UIFontSizeNormal;

                item.FontStretch = Windows.UI.Text.FontStretch.Normal;  // <== hack

                if (italicizeStrings && v is string)
                {
                    item.FontStyle = Windows.UI.Text.FontStyle.Italic;
                }

                _menuDropDown.Items.Add(item);
            }
        }

        public double Value
        {
            get
            {
                return _currentValue;
            }
            set
            {
                if (_currentValue != value || _numberBox.Visibility == Visibility.Collapsed)
                {
                    _currentValue = Math.Round(value, 4);
                    _numberBox.Value = _isMetric ? _currentValue * 25.4 : _currentValue;

                    _textBox.Visibility = Visibility.Collapsed;
                    _numberBox.Visibility = Visibility.Visible;

                    if (OnValueChanged != null)
                    {
                        ValueChangedEventArgs ve = new ValueChangedEventArgs(_currentValue, true);
                        OnValueChanged(this, ve);
                    }
                }
            }
        }

        private void _numberBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                double v = _numberBox.Value;
                _numberBox.Value = v;
                _numberBox.Select(_numberBox.Text.Length, 0);

                this.Value = _isMetric ? _numberBox.Value / 25.4 : _numberBox.Value;

                _textBox.Focus(FocusState.Programmatic);
            }
            else if (_numberBox.Text.Length > 5)
            {
                e.Handled = true;
            }
        }

        private void _numberBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            double v = _numberBox.Value;
            _numberBox.Value = v;

            this.Value = _isMetric ? _numberBox.Value / 25.4 : _numberBox.Value;
        }

        private void _numberBox_GotFocus(object sender, RoutedEventArgs e)
        {
            OnActivate?.Invoke(this, new EventArgs());
        }

        private void _menuDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_menuDropDown.SelectedItem is ListBoxItem)
            {
                if (((ListBoxItem)_menuDropDown.SelectedItem).Tag is double)
                {
                    _textBox.Visibility = Visibility.Collapsed;
                    _numberBox.Visibility = Visibility.Visible;
                    this.Value = (double)((ListBoxItem)_menuDropDown.SelectedItem).Tag;
                    _numberBox.Select(_numberBox.Text.Length, 0);
                }
                else if (((ListBoxItem)_menuDropDown.SelectedItem).Tag is string)
                {
                    double v;
                    if (double.TryParse(((ListBoxItem)_menuDropDown.SelectedItem).Tag as string, out v))
                    {
                        _textBox.Visibility = Visibility.Collapsed;
                        _numberBox.Visibility = Visibility.Visible;
                        this.Value = v;
                        _numberBox.Select(_numberBox.Text.Length, 0);
                    }
                    else
                    {
                        _numberBox.Visibility = Visibility.Collapsed;
                        _textBox.Visibility = Visibility.Visible;
                        _textBox.Text = ((ListBoxItem)_menuDropDown.SelectedItem).Tag as string;

                        if (OnValueChanged != null)
                        {
                            ValueChangedEventArgs ve = new ValueChangedEventArgs(-1, true);
                            OnValueChanged(this, ve);
                        }
                    }
                }
            }
            _menuPopup.IsOpen = false;
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

            _numberBox.Focus(FocusState.Programmatic);

            _menuDropDown.MinWidth = this.ActualWidth;
            _menuPopup.IsOpen = true;
        }
    }
}
