using System;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI;

namespace CirrosUI
{
    public sealed partial class StateButton : UserControl
    {
        public event ClickHandler OnClick;
        public delegate void ClickHandler(object sender, EventArgs e);

        Brush _selectedBackground = new SolidColorBrush(Colors.Gray);
        Brush _selectedHoverBackground = new SolidColorBrush(Colors.DarkGray);
        Brush _selectedForeground = new SolidColorBrush(Colors.White);
        Brush _selectedHoverForeground = new SolidColorBrush(Colors.White);
        Brush _normalBackground = new SolidColorBrush(Colors.Gainsboro);
        Brush _normalHoverBackground = new SolidColorBrush(Colors.LightGray);
        Brush _normalForeground = new SolidColorBrush(Colors.Black);
        Brush _normalHoverForeground = new SolidColorBrush(Colors.Black);

        Brush _pressedBackground = new SolidColorBrush(Colors.DimGray);
        Brush _pressedForeground = new SolidColorBrush(Colors.White);

        bool _isSelected = false;

        public StateButton()
        {
            this.InitializeComponent();

            _control.Background = _normalBackground;
            this.Foreground = _normalForeground;

            this.PointerEntered += StateButton_PointerEntered;
            this.PointerExited += StateButton_PointerExited;
            this.PointerPressed += _PointerPressed;
            this.PointerReleased += _PointerReleased;

            _content.PointerPressed += _PointerPressed;
            _content.PointerReleased += _PointerReleased;
        }

        bool _pressed = false;

        private void _PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_pressed == false)
            {
                _pressed = true;

                _control.Background = _pressedBackground;
                this.Foreground = _pressedForeground;
            }

            e.Handled = true;
        }

        private void _PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_pressed)
            {
                _pressed = false;
                if (OnClick != null)
                {
                    OnClick(this, new EventArgs());
                    _control.Background = _selectedHoverBackground;
                }
            }
        }

        private void StateButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_pressed)
            {
                _pressed = false;
            }

            if (_isSelected)
            {
                _control.Background = _selectedBackground;
                this.Foreground = _selectedForeground;
            }
            else
            {
                _control.Background = _normalBackground;
                this.Foreground = _normalForeground;
            }
        }

        private void StateButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_isSelected)
            {
                _control.Background = _selectedHoverBackground;
                this.Foreground = _selectedHoverForeground;
            }
            else
            {
                _control.Background = _normalHoverBackground;
                this.Foreground = _normalHoverForeground;
            }
        }

        public string Text
        {
            get { return _content.Text; }
            set { _content.Text = value; }
        }

        public Brush SelectedBackground
        {
            get { return _selectedBackground; }
            set { _selectedBackground = value; }
        }

        public Brush NormalBackground
        {
            get { return _normalBackground; }
            set { _normalBackground = value; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                if (_isSelected)
                {
                    _control.Background = _selectedBackground;
                    this.Foreground = _selectedForeground;
                }
                else
                {
                    _control.Background = _normalBackground;
                    this.Foreground = _normalForeground;
                }
            }
        }
    }
}
