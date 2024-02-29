using Cirros;
using Cirros.Commands;
using Cirros.Primitives;
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using App3;

namespace CirrosUI
{
    public sealed partial class CopyAlongLineContextMenuPanel : UserControl
    {
        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;
        Popup _popup;
        Point _workCanvasOffset;

        Rect _box = Rect.Empty;

        object _selection = null;

        public CopyAlongLineContextMenuPanel()
        {
            this.InitializeComponent();

            this.Loaded += SelectContextMenuPanel_Loaded;
            this.PointerPressed += SelectContextMenuPanel_PointerPressed;
            this.PointerMoved += SelectContextMenuPanel_PointerMoved;
            this.PointerReleased += SelectContextMenuPanel_PointerReleased;

            DataContext = Globals.UIDataContext;
        }

        public Point WorkCanvasOffset
        {
            set
            {
                _workCanvasOffset = value;
            }
        }

        void SelectContextMenuPanel_Loaded(object sender, RoutedEventArgs e)
        {
            SetCopyRepeatType(Globals.LinearCopyRepeatType);

            _connectCB.IsChecked = Globals.LinearCopyRepeatConnect;
            _endCB.IsChecked = Globals.LinearCopyRepeatAtEnd;

            _spacingBox.Value = Globals.ActiveDrawing.PaperToModel(Globals.LinearCopyRepeatDistance);
            _repeatBox.Value = Globals.LinearCopyRepeatCount;

            //Select(null);

            _popup = this.Parent as Popup;

            if (_popup != null && _box != Rect.Empty && ActualHeight > 0 && ActualWidth > 0)
            {
                double leftSpace = _box.Left + _workCanvasOffset.X;
                double rightSpace = App.Window.Bounds.Width - (_box.Right + _workCanvasOffset.X);
                double topSpace = _box.Top + _workCanvasOffset.Y;
                double bottomSpace = App.Window.Bounds.Height - (_box.Bottom + _workCanvasOffset.Y);

                if (Math.Max(leftSpace, rightSpace) < (ActualWidth + 60))
                {
                    _popup.HorizontalOffset = App.Window.Bounds.Width / 3;
                }
                else
                {
                    _popup.HorizontalOffset = (leftSpace > rightSpace ? _box.Left - ActualWidth - 40 : _box.Right + 40) + _workCanvasOffset.X;
                }

                if (Math.Max(topSpace, bottomSpace) < (ActualHeight + 60))
                {
                    _popup.VerticalOffset = App.Window.Bounds.Height / 3;
                }
                else
                {
                    _popup.VerticalOffset = (topSpace > bottomSpace ? _box.Top - ActualHeight - 40 : _box.Bottom + 40) + _workCanvasOffset.Y;
                }
            }
        }

        void SelectContextMenuPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
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

        void SelectContextMenuPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _popup.HorizontalOffset = p.X - _dragXOff;
                _popup.VerticalOffset = p.Y - _dragYOff;
            }
        }

        void SelectContextMenuPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _isDragging = false;
        }

        public void Select(object o)
        {
            _selection = o;

            if (o != null)
            {
                if (o is Primitive)
                {
                    Primitive p = o as Primitive;
                    Point ul = Globals.View.PaperToDisplay(new Point(p.Box.Left, p.Box.Top));
                    Point lr = Globals.View.PaperToDisplay(new Point(p.Box.Right, p.Box.Bottom));
                    _box = new Rect(ul, lr);
                }

                _selectNewButton.IsChecked = false;
                _getSymbolButton.IsChecked = false;
                _drawLineButton.IsChecked = true;

                _options.Visibility = Visibility.Visible;
            }
            else
            {
                _selectNewButton.IsChecked = true;
                _getSymbolButton.IsChecked = false;
                _drawLineButton.IsChecked = false;

                _options.Visibility = Visibility.Collapsed;
            }
        }

        private void SetCopyRepeatType(CopyRepeatType type)
        {
            if (type == CopyRepeatType.Distribute)
            {
                _spacingBox.IsEnabled = false;
                _repeatBox.IsEnabled = true;
                _spaceButton.IsChecked = false;
                _distributeButton.IsChecked = true;
            }
            else
            {
                _repeatBox.IsEnabled = false;
                _spacingBox.IsEnabled = true;
                _spaceButton.IsChecked = true;
                _distributeButton.IsChecked = false;
            }

            Globals.LinearCopyRepeatType = type;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.Events.ShowContextMenu(null, null);
        }

        private void _spaceButton_Click(object sender, RoutedEventArgs e)
        {
            SetCopyRepeatType(CopyRepeatType.Space);
        }

        private void _distributeButton_Click(object sender, RoutedEventArgs e)
        {
            SetCopyRepeatType(CopyRepeatType.Distribute);
        }

        private void _selectNewButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.CommandProcessor.Invoke("A_SelectNew", null);

            Select(null);
        }

        private async void _getSymbolButton_Click(object sender, RoutedEventArgs e)
        {
            Select(null);

            Group group = await FileHandling.GetSingleSymbolAsync();

            if (group != null)
            {
                Globals.CommandProcessor.Invoke("A_GetSymbol", group.Name);
                Select(group);

                // Loading a symbol could change attributes,
                // which in turn could cause the Done action to be invoked,
                // which in turn would close the context menu.
                // We don't want that.

                _popup.IsOpen = true;
            }
            else
            {
                Select(_selection);
            }
        }

        private void _drawLineButton_Click(object sender, RoutedEventArgs e)
        {
            Select(_selection);
        }

        private void _connectCB_Checked(object sender, RoutedEventArgs e)
        {
            Globals.LinearCopyRepeatConnect = (bool)_connectCB.IsChecked;
        }

        private void _endCB_Checked(object sender, RoutedEventArgs e)
        {
            Globals.LinearCopyRepeatAtEnd = (bool)_endCB.IsChecked;
        }

        private void _repeatBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_repeatBox.Value >= 1)
            {
                Globals.LinearCopyRepeatCount = (int)_repeatBox.Value;
            }
            else
            {
                _repeatBox.Value = Globals.LinearCopyRepeatCount;
            }
        }

        private void _spacingBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_spacingBox.Value > 0)
            {
                Globals.LinearCopyRepeatDistance = Globals.ActiveDrawing.ModelToPaper(_spacingBox.Value);
            }
            else
            {
                _spacingBox.Value = Globals.ActiveDrawing.PaperToModel(Globals.LinearCopyRepeatDistance);
            }
        }
    }
}
