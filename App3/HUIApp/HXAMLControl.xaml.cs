using CirrosUWP.HUIApp;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace RedDog.HUIApp
{
    public sealed partial class HXAMLControl : UserControl
    {
        private bool _isSelected;

        public event HXAMLControlClickHandler OnHXAMLControlClick;
        private string _id;
        private string _xamlSource;
        private Brush _highlightBrush;
        public delegate void HXAMLControlClickHandler(object sender, EventArgs e);

        public HXAMLControl()
        {
            this.InitializeComponent();

            this.Loaded += HXAMLControl_Loaded;
        }

        public bool ProgressRingActive
        {
            set { _progressRing.IsActive = value; }
        }

        async void HXAMLControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = HGlobals.DataContext;

            _border.PointerPressed += HXAMLControl_PointerPressed;
            _border.PointerReleased += HXAMLControl_PointerReleased;

            if (this.Background is SolidColorBrush)
            {
                SolidColorBrush b = this.Background as SolidColorBrush;
                Color c = new Color();
                c.A = 255;
                c.R = (byte)(Math.Min((int)b.Color.R - 32, 255));
                c.G = (byte)(Math.Min((int)b.Color.G - 32, 255));
                c.B = (byte)(Math.Min((int)b.Color.B - 32, 255));

                _highlightBrush = new SolidColorBrush(c);
            }
            else
            {
                _highlightBrush = this.Background;
            }

            _border.Background = _isSelected ? _highlightBrush : this.Background;
            _border.BorderBrush = this.Foreground;
            _title.Foreground = this.Foreground;

            _border.Width = HGlobals.DataContext.LargeIconSize.Width; ;
            _border.Height = HGlobals.DataContext.LargeIconSize.Height;
            _title.FontSize = HGlobals.DataContext.UIFontSizeSmall;

            if (HGlobals.DataContext.UIFontSizeNormal == 16)
            {
                _iconArea.RenderTransform = new MatrixTransform();
            }
            else
            {
                ScaleTransform t = new ScaleTransform();
                t.ScaleX = t.ScaleY = HGlobals.DataContext.UIFontSizeNormal / 16;
                _iconArea.RenderTransform = t;
            }

            await SetIconSource(_xamlSource);
        }

        public async Task SetIconSource(string name)
        {
            if (name.EndsWith(".xaml"))
            {
                try
                {
                    _iconArea.Visibility = Visibility.Collapsed;

                    Uri uri = new Uri("ms-appx:///Icons/" + name);
                    StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);

                    if (file != null)
                    {
                        string xaml = await Windows.Storage.FileIO.ReadTextAsync(file);
#if false
                        object o = XamlReader.Load(xaml);
                        if (o is Canvas canvas)
                        {
                            Brush brush = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80));

                            HUIUtilities.HSetXamlBrush(canvas, brush, 1);

                            _iconArea.Content = canvas;
                        }
#else
                        xaml = xaml.Replace("=\"#FFFFFFFF\"", "=\"{StaticResource DarkDarkGray}\"");
                        object o = XamlReader.Load(xaml);
                        if (o is Canvas canvas)
                        {
                            _iconArea.Content = (UIElement)o;
                            _iconArea.Visibility = Visibility.Visible;
                        }
#endif
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            else
            {
                string glyph = "";

                if (name.Length == 1)
                {
                    glyph = name;
                }
                else if (name.StartsWith("0x"))
                {
                    string hs = name.Substring(2);
                    int h = int.Parse(hs, System.Globalization.NumberStyles.HexNumber);
                    char u = (char)h;
                    glyph = u.ToString();
                }

                if (glyph.Length == 1)
                {
                    TextBlock tb = new TextBlock();
                    tb.Text = glyph;
                    tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    tb.VerticalAlignment = VerticalAlignment.Stretch;

                    _iconArea.Content = tb;
                }
            }
        }

        void HXAMLControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (OnHXAMLControlClick != null)
            {
                OnHXAMLControlClick(this, new EventArgs());
            }
        }

        void HXAMLControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
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
                return _title.Text;
            }
            set
            {
                _title.Text = value;
            }
        }

        public string IconSource
        {
            get
            {
                return _xamlSource;
            }
            set
            {
                _xamlSource = value;
            }
        }

        public Thickness IconBorderThickness
        {
            get { return _border.BorderThickness; }
            set { _border.BorderThickness = value; }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                _border.Background = _isSelected ? _highlightBrush : this.Background;
            }
        }
    }
}
