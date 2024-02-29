using Cirros;
using System;
using System.IO;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI;

namespace Cirros8.Symbols
{
    public sealed partial class SymbolPickerGridCell : UserControl
    {
        private SymbolPickerItem _symbol;

        public SymbolPickerGridCell()
        {
            this.InitializeComponent();
            this.DataContextChanged += SymbolGridCell_DataContextChanged;
        }

        private async void SymbolGridCell_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue is SymbolPickerItem si)
            {
                _symbol = si;
                _symbolName.Text = si.Name;

                ToolTip toolTip = new ToolTip();
                toolTip.Content = string.IsNullOrEmpty(si.Description) ? si.Name : $"{si.Name}\n{si.Description}";
                ToolTipService.SetToolTip(this, toolTip);

                if (string.IsNullOrEmpty(si.ThumbnailData) == false)
                {
                    if (si.ThumbnailData.StartsWith("data:image/png;base64,"))
                    {
                        string b64 = si.ThumbnailData.Substring(22);
                        using (var stream = new MemoryStream(Convert.FromBase64String(b64)))
                        {
                            BitmapImage src = new BitmapImage();
                            await src.SetSourceAsync(stream.AsRandomAccessStream());
                            _thumbnail.Source = src;
                        }
                    }
                }
                else if (si.Group != null)
                {
                    try
                    {
                        StorageFile thumbfile = await FileHandling.GetGroupThumbnail(si.Group);
                        if (thumbfile != null)
                        {
                            BitmapImage src = new BitmapImage();
                            src.SetSource(await thumbfile.OpenAsync(FileAccessMode.Read));
                            _thumbnail.Source = src;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        bool _inside = false;

        private void StackPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is StackPanel sp)
            {
                sp.Background = new SolidColorBrush(Color.FromArgb(255, 0xee, 0xee, 0xee));
                _inside = true;
            }
        }

        private void StackPanel_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is StackPanel sp)
            {
                sp.Background = new SolidColorBrush(Colors.Transparent);
                _inside = false;
            }
        }

        private void StackPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is StackPanel sp)
            {
                sp.Background = new SolidColorBrush(Color.FromArgb(255, 0xdd, 0xdd, 0xdd));
            }
        }

        private void StackPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (sender is StackPanel sp)
            {
                if (_inside)
                {
                    sp.Background = new SolidColorBrush(Color.FromArgb(255, 0xee, 0xee, 0xee));
                    _symbol.IsSelected = true;
                }
                else
                {
                    sp.Background = new SolidColorBrush(Colors.Transparent);
                }
            }
        }
    }
}
