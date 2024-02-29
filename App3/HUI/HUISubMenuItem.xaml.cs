using Cirros;
using CirrosUWP.HUIApp;
using Microsoft.UI.Xaml.Controls;
using RedDog.Console;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI;
using KT22.UI;

namespace HUI
{
    public sealed partial class HUISubMenuItem : UserControl
    {
        private Color _backgroundColor = Colors.White;
        private Color _activeColor = Colors.WhiteSmoke;
        private Color _foregroundColor = Colors.Black;
        private Color _highlightColor = Colors.Red;

        private string _glyph = "?";
        private string _id = "?";
        private string _title = "?";
        private string _dismiss = "light";
        private string _dialog = "";
        private string _font = "";

        private bool _isActive = false;
        private bool _isHovered = false;

        public event HUISubMenuClickHandler OnHUISubMenuClick;
        public delegate void HUISubMenuClickHandler(object sender, HUISubMenuClickEventArgs e);

        public HUISubMenuItem()
        {
            this.InitializeComponent();
            this.Loaded += HUISubMenuItem_Loaded;
        }

        public HUISubMenuItem(Color foreground, Color background, Color highlight, string id, string glyph, string font, string title, string dismiss, string dialog)
        {
            this.InitializeComponent();
            this.Loaded += HUISubMenuItem_Loaded;

            _backgroundColor = background;
            _foregroundColor = foreground;
            _highlightColor = highlight;

            _activeColor.A = 255;
            _activeColor.R = (byte)(Math.Min((int)_backgroundColor.R - 32, 255));
            _activeColor.G = (byte)(Math.Min((int)_backgroundColor.G - 32, 255));
            _activeColor.B = (byte)(Math.Min((int)_backgroundColor.B - 32, 255));

            _dismiss = dismiss;
            _dialog = dialog;
            _font = font;

            Id = id;
            Glyph = glyph;
            Title = title;
        }

        async void HUISubMenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = HGlobals.DataContext;

            this.PointerEntered += HUISubMenuItem_PointerEntered;
            this.PointerExited += HUISubMenuItem_PointerExited;
            this.PointerPressed += HUISubMenuItem_PointerPressed;
            this.PointerReleased += HUISubMenuItem_PointerReleased;

            if (_glyph.EndsWith(".xaml"))
            {
                _icon.Content = await GetIconContent(_glyph);
            }
            else
            {
                if (_glyph.StartsWith("0x"))
                {
                    string hs = _glyph.Substring(2);
                    int h = int.Parse(hs, System.Globalization.NumberStyles.HexNumber);
                    char u = (char)h;
                    _glyphBlock.Text = u.ToString();
                }
                else
                {
                    _glyphBlock.Text = _glyph;
                }

                if (string.IsNullOrEmpty(_font) == false)
                {
                    _glyphBlock.FontFamily = new FontFamily(_font);
                }
            }

            Draw();
        }

        public void HUISubMenuClick()
        {
            if (OnHUISubMenuClick != null)
            {
                OnHUISubMenuClick(this, new HUISubMenuClickEventArgs(this.Id));
            }
        }

        private void Draw()
        {
            Color color = _isActive || _isHovered ? _highlightColor : _foregroundColor;
            Color background = _isActive ? _activeColor : _backgroundColor;

            _field.Fill = new SolidColorBrush(background);

            Brush brush = new SolidColorBrush(color);
            double thickness = _isActive ? 3 : 2;

            _titleBlock.Foreground = brush;

            if (_icon.Content is TextBlock)
            {
                _glyphBlock.Foreground = brush;
            }
            else if (_icon.Content is Canvas)
            {
                HUIUtilities.HSetXamlBrush(_icon.Content as Canvas, brush, thickness);
            }
        }

        void HUISubMenuItem_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Draw();
            HUISubMenuClick();
        }

        void HUISubMenuItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Draw();
        }

        void HUISubMenuItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isHovered = false;
            Draw();
        }

        void HUISubMenuItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isHovered = true;
            Draw();
        }

        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                _isActive = _dismiss == "immediate" ? false : value;
                Draw();
            }
        }

        public static async Task<UIElement> GetIconContent(string item)
        {
            UIElement u = null;

            try
            {
                Uri uri = new Uri("ms-appx:///Icons/" + item);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);

                if (file != null)
                {
                    string xaml = await Windows.Storage.FileIO.ReadTextAsync(file);
                    object o = XamlReader.Load(xaml);
                    u = (UIElement)o;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return u;
        }

        public string Glyph
        {
            get { return _glyph; }
            set
            {
                _glyph = value;

                if (_glyph == null || _glyph.Length == 0)
                {
                    _iconColumn.Width = new GridLength(20);
                }
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

        public string Dialog
        {
            get
            {
                return _dialog;
            }
            set
            {
                _dialog = value;
            }
        }

        TeachingTip _ttSubMenu = null;

        public void ShowTeachingTip(string subMenuId, bool showNext)
        {
            if (_ttSubMenu == null)
            {
                _ttSubMenu = new TeachingTip();
                _ttSubMenu.Name = "_ttSubMenu_" + subMenuId + "_" + Id;
                _ttSubMenu.Target = _field;
                _ttSubMenu.PreferredPlacement = TeachingTipPlacementMode.RightBottom;
                _ttSubMenu.PlacementMargin = new Thickness(0);
                _ttSubMenu.IsLightDismissEnabled = true;
                _ttSubMenu.CloseButtonContent = "Close";
                _ttSubMenu.Title = Title;

                if (showNext)
                {
                    _ttSubMenu.ActionButtonContent = "Next";
                    _ttSubMenu.ActionButtonClick += _ttSubMenu_ActionButtonClick;
                }

                Microsoft.UI.Xaml.Controls.FontIconSource fis = new Microsoft.UI.Xaml.Controls.FontIconSource();
                fis.FontFamily = _glyphBlock.FontFamily;
                fis.Glyph = _glyphBlock.Text;
                _ttSubMenu.IconSource = fis;

                HtmlTextBlock htmlBlock = new HtmlTextBlock();
                htmlBlock.FontFamily = new FontFamily("Segoe UI");
                htmlBlock.Margin = new Thickness(0, 6, 0, 0);
                htmlBlock.Html = ConsoleUtilities.GetTeachingTipContent(_ttSubMenu.Name);
                _ttSubMenu.Content = htmlBlock;
#if DEBUG
                htmlBlock.AccessKey = "A";
                htmlBlock.AccessKeyInvoked += ConsoleUtilities.Bh_AccessKeyInvoked;
#endif
                _contentGrid.Children.Add(_ttSubMenu);
            }

            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", subMenuId }, { "source", Id } });

            _ttSubMenu.IsOpen = true;
        }

        private void _ttSubMenu_ActionButtonClick(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
        {
            _ttSubMenu.IsOpen = false;

            FrameworkElement fe = this.Parent as FrameworkElement;
            while (fe != null)
            {
                if (fe is HUISubMenu sub)
                {
                    sub.ShowNextTeachingTip();
                    break;
                }
                fe = fe.Parent as FrameworkElement;
            }
        }

        private void _ttSubMenu_CloseButtonClick(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
        {

        }
    }

    public class HUISubMenuClickEventArgs : EventArgs
    {
        public string Id { get; private set; }

        public HUISubMenuClickEventArgs(string id)
        {
            Id = id;
        }
    }
}
