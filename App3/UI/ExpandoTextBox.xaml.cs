using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace CirrosUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExpandoTextBox : UserControl
    {
        public event ExpandoTextChangedHandler OnExpandoTextChanged;
        public delegate void ExpandoTextChangedHandler(object sender, ExpandoTextChangedEventArgs e);

        bool _textIsDirty = false;

        public ExpandoTextBox()
        {
            this.InitializeComponent();

            _textBox.Width = this.ActualWidth;

            this.Loaded += ExpandoTextBox_Loaded;
            this.SizeChanged += ExpandoTextBox_SizeChanged;
            this.PointerPressed += ExpandoTextBox_PointerPressed;
        }

        private void ExpandoTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void ExpandoTextBox_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_textPopup.IsOpen == false)
            {
                _textPopup.IsOpen = true;
                _popupTextBox.Focus(FocusState.Pointer);
                _popupTextBox.Select(_popupTextBox.Text.Length, 0);
            }
        }

        private void ExpandoTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            _textBox.FontSize = _popupTextBox.FontSize = this.FontSize;
            _textBox.FontFamily = _popupTextBox.FontFamily = this.FontFamily;
            _textBox.FontStyle = _popupTextBox.FontStyle = this.FontStyle;
            _textBox.FontWeight = _popupTextBox.FontWeight = this.FontWeight;
            _textBox.BorderBrush = _popupTextBox.BorderBrush = this.BorderBrush;
            _textBox.BorderThickness = _popupTextBox.BorderThickness = this.BorderThickness;
            _textBox.MinHeight = this.MinHeight;
            _textBox.Width = this.ActualWidth;
            _popupTextBox.MinHeight = this.ActualHeight;
            _popupTextBox.MinWidth = this.ActualWidth;

            Thickness t = _textBox.Padding;
            double pv = (t.Top + t.Bottom) / 2;
            t.Left = t.Right * .6;
            t.Top = t.Bottom;// = pv;
            _textBox.Padding = _popupTextBox.Padding = t;
        }

        public string Text
        {
            get
            {
                return _popupTextBox.Text.Trim();
            }
            set
            {
                _popupTextBox.Text = _textBox.Text = value.Trim();
                _textIsDirty = true;
            }
        }

        private void _textPopup_Closed(object sender, object e)
        {
            if (OnExpandoTextChanged != null)
            {
                _popupTextBox.Text = _popupTextBox.Text.Trim();
                UpdateTextBoxFromPopup();

                ExpandoTextChangedEventArgs ea = new ExpandoTextChangedEventArgs(_popupTextBox.Text);
                OnExpandoTextChanged(this, ea);
            }
        }

        private void _popupTextBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void _popupTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_popupTextBox.Text.EndsWith("\n\n"))
            {
                _textPopup.IsOpen = false;
            }
            else if (_popupTextBox.Text.EndsWith("\r\r"))
            {
                _textPopup.IsOpen = false;
            }
            else if (_popupTextBox.Text.EndsWith("\r\n\r\n"))
            {
                _textPopup.IsOpen = false;
            }
        }

        private void UpdateTextBoxFromPopup()
        {
            bool truncate = false;

            if (_popupTextBox.Text.IndexOf('\n') >= 0)
            {
                string[] sa = _popupTextBox.Text.Split(new char[] { '\n' });
                truncate = sa.Length > 1;
                _textBox.Text = sa[0].Trim();
            }
            else if (_popupTextBox.Text.IndexOf('\r') >= 0)
            {
                string[] sa = _popupTextBox.Text.Split(new char[] { '\r' });
                truncate = sa.Length > 1;
                _textBox.Text = sa[0].Trim();
            }
            else
            {
                _textBox.Text = _popupTextBox.Text;
            }

            double w = _textBox.ActualWidth - (_textBox.Padding.Left + _textBox.Padding.Right) - 10;
            double sw = Cirros.TextUtilities.FontInfo.StringWidth(_textBox.Text, _textBox.FontFamily, _textBox.FontSize);
            if (sw > w)
            {
                int b = (int)((w / sw) * _textBox.Text.Length);
#if true
                while (sw > w && b > 0)
                {
                    _textBox.Text = _textBox.Text.Substring(0, b);
                    sw = Cirros.TextUtilities.FontInfo.StringWidth(_textBox.Text, _textBox.FontFamily, _textBox.FontSize);
                    --b;
                }
#else
                // possible crash if b <= 0
                do
                {
                    _textBox.Text = _textBox.Text.Substring(0, b);
                    sw = Cirros.TextUtilities.FontInfo.StringWidth(_textBox.Text, _textBox.FontFamily, _textBox.FontSize);
                    --b;
                }
                while (sw > w && b > 0);
#endif

                truncate = true;
            }

            if (truncate)
            {
                _textBox.Text = _textBox.Text + "…";
            }
        }

        private void _textBox_LayoutUpdated(object sender, object e)
        {
            if (this.Visibility == Visibility.Visible && _textBox.ActualWidth > 0)
            {
                if (_textIsDirty)
                {
                    UpdateTextBoxFromPopup();

                    _textIsDirty = false;
                }
            }
            else
            {

            }
        }

        private void _textBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _textIsDirty = true;
        }
    }

    public class ExpandoTextChangedEventArgs : EventArgs
    {
        private string _text;

        public ExpandoTextChangedEventArgs(string text)
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
