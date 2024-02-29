using Cirros;
using Cirros.Commands;
using Cirros.Primitives;
using CirrosUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using App3;
using Microsoft.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace CirrosUWP.CirrosUI.Context_Menu
{
    public sealed partial class CopyPasteContextMenuPanel : UserControl
    {
        public CopyPasteContextMenuPanel()
        {
            this.InitializeComponent();

            this.Loaded += CopyPasteContextMenuPanel_Loaded;
            this.Unloaded += CopyPasteContextMenuPanel_Unloaded;
            this.PointerPressed += CopyPasteContextMenuPanel_PointerPressed;
            this.PointerMoved += CopyPasteContextMenuPanel_PointerMoved;
            this.PointerReleased += CopyPasteContextMenuPanel_PointerReleased;

            Clipboard.ContentChanged += Clipboard_ContentChanged;

            _jumpIconPanel.Visibility = Globals.UIVersion > 0 ? Visibility.Visible : Visibility.Collapsed;

            _copyButton.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Colors.Gray);
            _copyButton.Resources["ButtonForegroundPressed"] = new SolidColorBrush(Colors.White);
            _copyButton.Resources["ButtonBackgroundDisabled"] = new SolidColorBrush(Colors.Gainsboro);
            _copyButton.Resources["ButtonForegroundDisabled"] = new SolidColorBrush(Colors.Gray);
            _copyButton.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Colors.LightGray);
            _copyButton.IsEnabled = false;

            _pasteButton.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Colors.DarkGray);
            _pasteButton.Resources["ButtonForegroundPressed"] = new SolidColorBrush(Colors.White);
            _pasteButton.Resources["ButtonBackgroundDisabled"] = new SolidColorBrush(Colors.Gainsboro);
            _pasteButton.Resources["ButtonForegroundDisabled"] = new SolidColorBrush(Colors.Gray);
            _pasteButton.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Colors.LightGray);

            try
            {
                DataPackageView dataPackageView = Clipboard.GetContent();
                _clipboardDataAvailable = _pasteButton.IsEnabled = dataPackageView.Contains("dbsx");
            }
            catch
            {

            }

            DataContext = Globals.UIDataContext;
        }

        bool _clipboardDataAvailable = false;

        private async void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                DataPackageView dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains("dbsx"))
                {
                    _pasteButton.IsEnabled = true;
                    _clipboardDataAvailable = true;

                    string s = "";

                    using (Stream fileStream = await ApplicationData.Current.TemporaryFolder.OpenStreamForReadAsync("cdata"))
                    {
                        System.IO.StreamReader reader = new System.IO.StreamReader(fileStream);
                        s = reader.ReadToEnd();
                        reader.Dispose();
                    }
                    if (s.Length > 0)
                    {
                        byte[] byteArray = Encoding.UTF8.GetBytes(s);
                        MemoryStream stream = new MemoryStream(byteArray);
                        Cirros.Primitives.Group g = await FileHandling.GetSymbolFromStreamAsync(stream);
                        if (g != null && 
                            g.PaperUnit == Globals.ActiveDrawing.PaperUnit && 
                            g.ModelUnit == Globals.ActiveDrawing.ModelUnit &&
                            g.ModelScale == Globals.ActiveDrawing.Scale)
                        {
                            _spaceButtons.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            _spaceButtons.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        _spaceButtons.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    _pasteButton.IsEnabled = false;
                    _clipboardDataAvailable = false;
                }
            }
            catch { }
        }

        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;
        Popup _popup;
        Point _workCanvasOffset;

        List<Primitive> _selectedObjects = null;

        Rect _box = Rect.Empty;

        static string _selectedOption = null;
        List<string>[] _selectedOptionList = null;


        void CopyPasteContextMenuPanel_Loaded(object sender, RoutedEventArgs e)
        {
            _popup = this.Parent as Popup;

            double frameWidth = App.Window.Bounds.Width;
            double frameHeight = App.Window.Bounds.Height;

            if (Globals.View != null)
            {
                frameWidth = Globals.View.ViewPortSize.Width;
                frameHeight = Globals.View.ViewPortSize.Height;
            }

            if (_popup != null && _box != Rect.Empty && ActualHeight > 0 && ActualWidth > 0 && _popup.HorizontalOffset == 0)
            {
                double leftSpace = _box.Left + _workCanvasOffset.X;
                double rightSpace = frameWidth - (_box.Right + _workCanvasOffset.X);
                double topSpace = _box.Top + _workCanvasOffset.Y;
                double bottomSpace = frameHeight - (_box.Bottom + _workCanvasOffset.Y);

                if (Math.Max(leftSpace, rightSpace) < (ActualWidth + 60))
                {
                    _popup.HorizontalOffset = frameWidth / 3;
                }
                else
                {
                    _popup.HorizontalOffset = (leftSpace > rightSpace ? _box.Left - ActualWidth - 40 : _box.Right + 40) + _workCanvasOffset.X;
                }

                if (Math.Max(topSpace, bottomSpace) < (ActualHeight + 60))
                {
                    _popup.VerticalOffset = frameHeight / 3;
                }
                else
                {
                    _popup.VerticalOffset = (topSpace > bottomSpace ? _box.Top - ActualHeight - 40 : _box.Bottom + 40) + _workCanvasOffset.Y;
                }
            }
        }

        void CopyPasteContextMenuPanel_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        void CopyPasteContextMenuPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _popup = this.Parent as Popup;
            if (_popup != null)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _dragXOff = p.X - _popup.HorizontalOffset;
                _dragYOff = p.Y - _popup.VerticalOffset;
                _isDragging = true;

                CapturePointer(e.Pointer);
            }
        }

        void CopyPasteContextMenuPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _popup.HorizontalOffset = p.X - _dragXOff;
                _popup.VerticalOffset = p.Y - _dragYOff;
            }
        }

        void CopyPasteContextMenuPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _isDragging = false;
        }

        FrameworkElement _selectedButton = null;
        bool _pastePaperScale = false;

        private void Tb_OnClick(object sender, EventArgs e)
        {
            if (sender is FrameworkElement b)
            {
                SelectButton(b);
            }
        }

        private void InvokeOption(string option)
        {
            switch (option)
            {
                case "A_SelectCopyToClipboard":
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Invoke(option, null);
                    }
                    break;
                case "A_SelectPaste":
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Invoke(option, _pastePaperScale);
                    }
                    break;
            }
        }

        private void SelectButton(FrameworkElement button)
        {
            try
            {
                if (_selectedButton != null)
                {
                    if (_selectedButton is StateButton b)
                    {
                        b.IsSelected = false;
                    }
                    _selectedButton = null;
                }

                if (button != null)
                {
                    _selectedButton = button;
                    if (button is StateButton b)
                    {
                        b.IsSelected = true;
                    }

                    if (button.Tag is string)
                    {
                        InvokeOption(button.Tag as string);
                    }
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "command", "edit" },
                        { "method", "SelectButton" },
                    }, 500);
            }
        }

        public void Select(List<Primitive> list)
        {
            _selectedObjects = list;

            if (list == null || list.Count == 0)
            {
                _title.Text = "No selection";

                if (_clipboardDataAvailable)
                {
                    _copyButton.Visibility = Visibility.Visible;
                    _pasteButton.Visibility = Visibility.Visible;
                    _spaceButtons.Visibility = Visibility.Collapsed;
                    _noSelectionPanel.Visibility = Visibility.Collapsed;
                    _pasteButton.IsEnabled = true;
                    _copyButton.IsEnabled = false;
                }
                else
                {
                    _copyButton.Visibility = Visibility.Collapsed;
                    _pasteButton.Visibility = Visibility.Collapsed;
                    _spaceButtons.Visibility = Visibility.Collapsed;
                    _noSelectionPanel.Visibility = Visibility.Visible;
                }
            }
            else
            {
                _copyButton.Visibility = Visibility.Visible;
                _pasteButton.Visibility = Visibility.Visible;
                _spaceButtons.Visibility = Visibility.Collapsed;
                _noSelectionPanel.Visibility = Visibility.Collapsed;
                _pasteButton.IsEnabled = _clipboardDataAvailable;
                _copyButton.IsEnabled = true;

                _copyButton.IsEnabled = true;

                if (list.Count == 1)
                {
                    _title.Text = "Selection: 1 object";
                }
                else
                {
                    _title.Text = string.Format("Selection: {0} objects", list.Count);
                }

                Rect itemBoxUnion = RectHelper.Empty;

                foreach (Primitive p in list)
                {
                    if (itemBoxUnion.IsEmpty)
                    {
                        itemBoxUnion = p.Box;
                    }
                    else
                    {
                        itemBoxUnion.Union(p.Box);
                    }
                }

                if (itemBoxUnion.IsEmpty == false)
                {
                    Point ul = Globals.View.PaperToDisplay(new Point(itemBoxUnion.Left, itemBoxUnion.Top));
                    Point lr = Globals.View.PaperToDisplay(new Point(itemBoxUnion.Right, itemBoxUnion.Bottom));
                    _box = new Rect(ul, lr);
                }
            }
        }

        private void SelectObjectsClick(object sender, RoutedEventArgs e)
        {
            if (_selectedObjects != null && _selectedObjects.Count > 0)
            {
                Globals.CommandProcessorParameter = _selectedObjects.ToList<Primitive>();
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.select;
        }

        private void EditObjectClick(object sender, RoutedEventArgs e)
        {
            if (_selectedObjects != null && _selectedObjects.Count == 1)
            {
                Globals.CommandProcessorParameter = _selectedObjects[0];
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.edit;
        }

        private void EditGroupClick(object sender, RoutedEventArgs e)
        {
            if (_selectedObjects != null && _selectedObjects.Count == 1)
            {
                Globals.CommandProcessorParameter = _selectedObjects[0];
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.editgroup;
        }

        private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Button b)
            {
                b.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.Events.ShowContextMenu(null, null);
        }

        public void WillClose()
        {
        }

        private void _button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb && tb.Tag is string tag)
            {
                if (tag == "paper")
                {
                    _paperButton.IsChecked = true;
                    _modelButton.IsChecked = false;
                    _pastePaperScale = true;
                }
                else if (tag == "model")
                {
                    _paperButton.IsChecked = false;
                    _modelButton.IsChecked = true;
                    _pastePaperScale = false;
                }
            }
            else if (sender is FrameworkElement fe)
            {
                SelectButton(fe);
            }
        }
    }
}
