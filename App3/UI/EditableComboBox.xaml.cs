using Cirros;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace CirrosUI
{
    public sealed partial class EditableComboBox : UserControl
    {
        public event TextChangedHandler OnTextChanged;
        public delegate void TextChangedHandler(object sender, EditableComboBoxTextChangedEventArgs e);

        private bool _textIsDirty = false;

        public event ActivateHandler OnActivate;
        public delegate void ActivateHandler(object sender, EventArgs e);

        public string Text
        {
            get { return _textBox.Text; }
            set
            {
                _textBox.Text = value == null ? "" : value;
                _menuDropDown.SelectedValue = _textBox.Text;
                _textIsDirty = false;
            }
        }

        public EditableComboBox()
        {
            this.InitializeComponent();

            DataContext = Globals.UIDataContext;

            _menuDropDown.SizeChanged += _menuDropDown_SizeChanged;

            _textBox.GotFocus += _textBox_GotFocus;
            _textBox.LostFocus += _textBox_LostFocus;
            _textBox.KeyDown += _textBox_KeyDown;

            _textBox.Loaded += _textBox_Loaded;

            this.PointerPressed += _comboBox_PointerPressed;
            this.PointerReleased += _comboBox_PointerReleased;

            this.Loaded += EditableComboBox_Loaded;
        }

        private void _textBox_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (object o in _menuDropDown.Items)
            {
                if (o is ListBoxItem item)
                {
                    if (item.Height == _textBox.ActualHeight)
                    {
                        break;
                    }
                    item.Height = _textBox.ActualHeight;
                }
            }
        }

        private void EditableComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            _textBox.FontStretch = this.FontStretch;
            _menuDropDown.FontStretch = this.FontStretch;

            _textBox.FontWeight = this.FontWeight;
            _menuDropDown.FontWeight = this.FontWeight;

            _textBox.FontSize = this.FontSize;
            _menuDropDown.FontSize = this.FontSize;

            _textBox.FontFamily = this.FontFamily;
            _menuDropDown.FontFamily = this.FontFamily;
        }

        public void Select(int start, int length)
        {
            _textBox.Select(start, length);
        }

        public void SelectAll()
        {
            _textBox.SelectAll();
        }

        public void SetValues(List<string> strings)
        {
            _menuDropDown.Items.Clear();

            foreach (string s in strings)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = s;
                item.Tag = s;
                item.FontSize = this.FontSize;
                item.FontStretch = this.FontStretch;
                item.FontFamily = this.FontFamily;
                if (_textBox.ActualHeight > 0)
                {
                    item.Height = _textBox.ActualHeight;
                }
                else if (_textBox.Height > 0)
                {
                    item.Height = _textBox.Height;
                }
                _menuDropDown.Items.Add(item);
            }
        }

        public void SetValues(Dictionary<int, string> items)
        {
            _menuDropDown.Items.Clear();

            foreach (int key in items.Keys)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = key;
                item.Tag = items[key];
                item.FontSize = this.FontSize;
                item.FontStretch = this.FontStretch;
                item.FontFamily = this.FontFamily;
                item.Height = _textBox.Height;
                _menuDropDown.Items.Add(item);
            }
        }

        public void SetValues(Dictionary<string, string> items)
        {
            _menuDropDown.Items.Clear();

            foreach (string key in items.Keys)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = key;
                item.Tag = items[key];
                item.FontSize = this.FontSize;
                item.FontStretch = this.FontStretch;
                item.FontFamily = this.FontFamily;
                item.Height = _textBox.Height;
                _menuDropDown.Items.Add(item);
            }
        }

        private void _textBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (OnTextChanged != null)
                {
                    if (_textIsDirty)
                    {
                        EditableComboBoxTextChangedEventArgs ee = new EditableComboBoxTextChangedEventArgs(_textBox.Text);
                        OnTextChanged(this, ee);
                        _textIsDirty = false;
                    }
                }
                _target.Focus(FocusState.Programmatic);
            }
            else
            {
                _textIsDirty = true;
            }
        }

        private void _textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (OnTextChanged != null)
            {
                if (_textIsDirty)
                {
                    EditableComboBoxTextChangedEventArgs ee = new EditableComboBoxTextChangedEventArgs(_textBox.Text);
                    OnTextChanged(this, ee);
                    _textIsDirty = false;
                }
            }
            //_textBox.SetValue(Grid.ColumnSpanProperty, 1);
        }

        private void _textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_textBox.FocusState == FocusState.Pointer)
            {
                OnActivate?.Invoke(this, new EventArgs());
                //_textBox.SetValue(Grid.ColumnSpanProperty, 2);
            }
        }

        private void _menuDropDown_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize == e.NewSize) return;
            if (Cirros.Utility.Utilities.__checkSizeChanged(47, sender)) return;

            GeneralTransform tf = this.TransformToVisual(null);
            Point p = tf.TransformPoint(new Point());

            double top = p.Y;
            double bottom = top + _menuDropDown.ActualHeight;

            if (bottom > App.Window.CoreWindow.Bounds.Bottom)
            {
                _menuPopup.VerticalOffset = App.Window.CoreWindow.Bounds.Bottom - bottom;
            }
        }

        private void _menuDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_menuDropDown.SelectedItem is ListBoxItem)
            {
                if (((ListBoxItem)_menuDropDown.SelectedItem).Tag is string)
                {
                    _textBox.Text = ((ListBoxItem)_menuDropDown.SelectedItem).Tag as string;
                    _textBox.Select(_textBox.Text.Length, 0);
                    _textIsDirty = true;

                    if (OnTextChanged != null)
                    {
                        EditableComboBoxTextChangedEventArgs ee = new EditableComboBoxTextChangedEventArgs(_textBox.Text);
                        OnTextChanged(this, ee);
                    }
                }
            }
            _menuPopup.IsOpen = false;
        }

        private void _comboBox_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //OnActivate?.Invoke(this, new EventArgs());

            CapturePointer(e.Pointer);
            _border.Background = new SolidColorBrush(Utilities.ColorFromColorSpec(0xFFD3D3D3));
            e.Handled = true;
        }

        private void _comboBox_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _border.Background = new SolidColorBrush(Colors.White);

            OnActivate?.Invoke(this, new EventArgs());
            _textBox.Focus(FocusState.Programmatic);

            _menuDropDown.MinWidth = this.ActualWidth;
            _menuPopup.IsOpen = true;
        }
    }

    public class EditableComboBoxTextChangedEventArgs : EventArgs
    {
        private string _text;

        public EditableComboBoxTextChangedEventArgs(string text)
        {
            _text = text;
        }

        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
            }
        }
    }
}