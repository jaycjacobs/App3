using HUI;
using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI;

namespace RedDog.HUIApp
{
    public sealed partial class HSymbolLibraryItem : UserControl
    {
        FrameworkElement _itemContent = null;
        string _id = "";

        public event HSymbolLibraryItemClickHandler OnHSymbolLibraryItemClick;
        public delegate void HSymbolLibraryItemClickHandler(object sender, HSymbolLibraryItemClickEventArgs e);

        Color _borderColor = Colors.Black;

        public HSymbolLibraryItem()
        {
            this.InitializeComponent();

            this.Loaded += HSymbolLibraryItem_Loaded;
        }

        public HSymbolLibraryItem(string label, string id)
        {
            this.InitializeComponent();

            Label = label;
            Id = id;

            this.Loaded += HSymbolLibraryItem_Loaded;
        }

        void HSymbolLibraryItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (_itemContent != null)
            {
                _itemContainer.Content = _itemContent;
            }
            else
            {
                Rectangle r = new Rectangle();
                r.Fill = new SolidColorBrush(Colors.White);

                if (this.Width > 0 && this.Height > 0)
                {
                    r.Width = this.Width;
                    r.Height = this.Height;
                }
                else
                {
                    r.Width = _thumbColumn.ActualWidth;
                    r.Height = _thumbRow.ActualHeight;
                }

                _itemContainer.Content = r;
            }

            if (_itemBorder.BorderBrush is SolidColorBrush)
            {
                _borderColor = ((SolidColorBrush)_itemBorder.BorderBrush).Color;
            }

            if (OnHSymbolLibraryItemClick != null)
            {
                _borderColor.A = 0;
                _itemBorder.BorderBrush = new SolidColorBrush(_borderColor);

                this.PointerPressed += HSymbolLibraryItem_PointerPressed;
                this.PointerReleased += HSymbolLibraryItem_PointerReleased;
                this.PointerEntered += HSymbolLibraryItem_PointerEntered;
                this.PointerExited += HSymbolLibraryItem_PointerExited;
            }
            else
            {
                _itemBorder.BorderThickness = new Thickness(1);
            }
        }

        bool _isHovered = false;
        bool _isPressed = false;

        void SetBorderColor()
        {
            if (_isPressed)
            {
                _borderColor.A = 255;
            }
            else if (_isHovered)
            {
                _borderColor.A = 128;
            }
            else
            {
                _borderColor.A = 0;
            }
        
            _itemBorder.BorderBrush = new SolidColorBrush(_borderColor);
        }

        void HSymbolLibraryItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isHovered = false;

            SetBorderColor();
        }

        void HSymbolLibraryItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isHovered = true;

            SetBorderColor();
        }

        void HSymbolLibraryItem_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isPressed = false; 
            
            if (OnHSymbolLibraryItemClick != null)
            {
                OnHSymbolLibraryItemClick(this, new HSymbolLibraryItemClickEventArgs(_id));
            }

            SetBorderColor();
        }

        void HSymbolLibraryItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isPressed = true;

            SetBorderColor();
        }

        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        public string Label
        {
            get
            {
                return _label.Text;
            }
            set
            {
                _label.Text = value;
                this.FontStyle = Windows.UI.Text.FontStyle.Normal;
            }
        }

        public FrameworkElement ItemSource
        {
            get
            {
                return _itemContent;
            }
            set
            {
                _itemContent = value;
                _itemContainer.Content = _itemContent;
            }
        }
    }

    public class HSymbolLibraryItemClickEventArgs : EventArgs
    {
        public string Id { get; private set; }

        public HSymbolLibraryItemClickEventArgs(string id)
        {
            Id = id;
        }
    }
}
