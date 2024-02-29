using Cirros;
using CirrosUWP.HUIApp;
using System;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace HUI
{
    public sealed partial class HUIButton : UserControl
    {
        private Color _inactiveColor = Colors.White;
        private Color _activeColor = Colors.White;
        private Color _inactiveHoverColor = Colors.White;
        private Color _activeHoverColor = Colors.White;
        private Color _inactivePressedColor = Colors.White;
        private Color _activePressedColor = Colors.White;
        private Color _foreground = Colors.Black;

        private string _glyph = "?";
        private string _id = "?";
        private string _title = "?";
        private string _dismiss = "light";
        private string _sticky = "yes";
        private string _font = "";
        private string _toolTip = null;

        private bool _isActive = false;
        private bool _isPressed = false;
        private bool _isHovered = false;

        public event HUIClickHandler OnHUIClick;
        public delegate void HUIClickHandler(object sender, HUIClickEventArgs e);

        public HUIButton()
        {
            this.InitializeComponent();
            this.Loaded += HUIButton_Loaded;
        }

        public HUIButton(Color active, Color inactive, Color foreground, 
            string id, string glyph, string title, string dismiss, string 
            sticky, string font, string tip)
        {
            this.InitializeComponent();
            this.Loaded += HUIButton_Loaded;

            InactiveColor = inactive;
            ActiveColor = active;
            ForegroundColor = foreground;

            Id = id;
            Glyph = glyph;
            Title = title;
            Dismiss = dismiss;
            Sticky = sticky;
            _font = font;
            _toolTip = tip;
        }

        public FrameworkElement Icon
        {
            get { return _iconFrame; }
        }

        void HUIButton_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = HGlobals.DataContext;

            this.PointerEntered += HUIButton_PointerEntered;
            this.PointerExited += HUIButton_PointerExited;
            this.PointerPressed += HUIButton_PointerPressed;
            this.PointerReleased += HUIButton_PointerReleased;

            _grid.SetValue(ToolTipService.ToolTipProperty, _toolTip);

            Draw();
        }

        public void HUIClick()
        {
            if (OnHUIClick != null)
            {
                OnHUIClick(this, new HUIClickEventArgs(this.Id));
            }
        }

        private async void Draw()
        {
            Color color;

            _foreground.A = (byte)(IsEnabled ? 255 : 128);

            if (_isPressed)
            {
                if (_isActive)
                {
                    color = _activePressedColor;
                }
                else
                {
                    color = _inactivePressedColor;
                }
            }
            else if (_isHovered)
            {
                if (_isActive)
                {
                    color = _activeHoverColor;
                }
                else
                {
                    color = _inactiveHoverColor;
                }
            }
            else if (_isActive)
            {
                color = _activeColor;
            }
            else
            {
                color = _inactiveColor;
            }

            if (_glyph.EndsWith(".xaml"))
            {
                if (_iconArea.Content is TextBlock)
                {
                    try
                    {
                        Uri uri = new Uri("ms-appx:///Icons/" + _glyph);
                        StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);

                        if (file != null)
                        {
                            string xaml = await Windows.Storage.FileIO.ReadTextAsync(file);
                            object o = XamlReader.Load(xaml);
                            _iconArea.Content = (UIElement)o;

                            Brush brush = new SolidColorBrush(_foreground);

                            if (_iconArea.Content is Canvas)
                            {
                                HUIUtilities.HSetXamlBrush(_iconArea.Content as Canvas, brush, 2);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            }
            else if (_iconArea.Content is TextBlock)
            {
                _glyphBlock.Text = _glyph;
                _glyphBlock.Foreground = new SolidColorBrush(_foreground);
                if (string.IsNullOrEmpty(_font) == false)
                {
                    _glyphBlock.FontFamily = new FontFamily(_font);
                }
            }

            _field.Fill = new SolidColorBrush(color);
        }

        void HUIButton_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isPressed = false;
            Draw();
            HUIClick();
        }

        void HUIButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isPressed = true;
            Draw();
        }

        void HUIButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isHovered = false;
            Draw();
        }

        void HUIButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isHovered = true;
            Draw();
        }

        public Color InactiveColor
        {
            get
            {
                this.Background = new SolidColorBrush(_inactiveColor);
                return _inactiveColor;
            }
            set 
            {
                _inactiveHoverColor = _inactivePressedColor = _inactiveColor = value;

                _inactiveHoverColor.R = (byte)(Math.Min((int)_inactiveHoverColor.R + 20, 255));
                _inactiveHoverColor.G = (byte)(Math.Min((int)_inactiveHoverColor.G + 20, 255));
                _inactiveHoverColor.B = (byte)(Math.Min((int)_inactiveHoverColor.B + 20, 255));

                _inactivePressedColor.R = (byte)(Math.Min((int)_inactiveHoverColor.R + 40, 255));
                _inactivePressedColor.G = (byte)(Math.Min((int)_inactiveHoverColor.G + 40, 255));
                _inactivePressedColor.B = (byte)(Math.Min((int)_inactiveHoverColor.B + 40, 255));
            }
        }

        public Color ActiveColor
        {
            get
            {
                return _activeColor;
            }
            set
            {
                _activeHoverColor = _activePressedColor = _activeColor = value;

                _activeHoverColor.R = (byte)(Math.Min((int)_activeHoverColor.R + 20, 255));
                _activeHoverColor.G = (byte)(Math.Min((int)_activeHoverColor.G + 20, 255));
                _activeHoverColor.B = (byte)(Math.Min((int)_activeHoverColor.B + 20, 255));

                _activePressedColor.R = (byte)(Math.Min((int)_activeHoverColor.R + 40, 255));
                _activePressedColor.G = (byte)(Math.Min((int)_activeHoverColor.G + 40, 255));
                _activePressedColor.B = (byte)(Math.Min((int)_activeHoverColor.B + 40, 255));
            }
        }

        public Color ForegroundColor
        {
            get
            {
                return _foreground;
            }
            set
            {
                _foreground = value;
                _glyphBlock.Foreground = new SolidColorBrush(_foreground);
                _titleBlock.Foreground = _glyphBlock.Foreground;
            }
        }

        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                _isActive = value;
                Draw();
            }
        }
        public string Glyph
        {
            get
            {
                return _glyph;
            }
            set
            {
                _glyph = value;
            }
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

        public string Dismiss
        {
            get
            {
                return _dismiss;
            }
            set
            {
                _dismiss = value;
            }
        }

        public string Sticky
        {
            get
            {
                return _sticky;
            }
            set
            {
                _sticky = value;
            }
        }

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                _titleBlock.Text = _title;
            }
        }
    }

    public class HUIClickEventArgs : EventArgs
    {
        public string Id { get; private set; }

        public HUIClickEventArgs(string id)
        {
            Id = id;
        }
    }
}
