using Cirros;
using Cirros.Utility;
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Cirros8
{
    public sealed partial class OriginPanel : UserControl
    {
        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;
        Popup _popup;
        Point _workCanvasOffset;
        Point _origin;

        public OriginPanel()
        {
            this.InitializeComponent();

            this.Loaded += OriginPanel_Loaded;
            this.PointerPressed += OriginPanel_PointerPressed;
            this.PointerReleased += OriginPanel_PointerReleased;
            this.PointerMoved += OriginPanel_PointerMoved;

            DataContext = Globals.UIDataContext;
        }

        void OriginPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
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

        void OriginPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _popup.HorizontalOffset = p.X - _dragXOff;
                _popup.VerticalOffset = p.Y - _dragYOff;
            }
        }

        void OriginPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _isDragging = false;
        }

        void OriginPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // Assume that Show() has already been called

            _popup = this.Parent as Popup;

            if (_popup != null)
            {
                Point dorigin = Globals.View.ModelToDisplayRaw(_origin);

                double leftSpace = dorigin.X + _workCanvasOffset.X;
                double rightSpace = App.Window.Bounds.Width - (dorigin.X + _workCanvasOffset.X);
                double topSpace = dorigin.Y + _workCanvasOffset.Y;
                double bottomSpace = App.Window.Bounds.Height - (dorigin.Y + _workCanvasOffset.Y);

                if (Math.Max(leftSpace, rightSpace) < (ActualWidth + 60))
                {
                    _popup.HorizontalOffset = App.Window.Bounds.Width / 3;
                }
                else
                {
                    _popup.HorizontalOffset = (leftSpace > rightSpace ? dorigin.X - ActualWidth - 40 : dorigin.X + 40) + _workCanvasOffset.X;
                }

                if (Math.Max(topSpace, bottomSpace) < (ActualHeight + 60))
                {
                    _popup.VerticalOffset = App.Window.Bounds.Height / 3;
                }
                else
                {
                    _popup.VerticalOffset = (topSpace > bottomSpace ? dorigin.Y - ActualWidth - 40 : dorigin.Y + 40) + _workCanvasOffset.Y;
                }
            }
        }

        internal Point WorkCanvasOffset
        {
            set
            {
                _workCanvasOffset = value;
            }
        }

        internal void ShowOrigin(Point point)
        {
            _origin = point;

            Point paper = Globals.ActiveDrawing.ModelToPaperRaw(_origin);

            string format = "({0:0.0000}{2}, {1:0.0000}{2})";

            _paperOffsetBlock.Text = string.Format(format, paper.X, paper.Y, Utilities.UnitString(Globals.ActiveDrawing.PaperUnit));
            _modelOffsetBlock.Text = string.Format(format, _origin.X, _origin.Y, Utilities.UnitString(Globals.ActiveDrawing.ModelUnit));
        }
    }
}
