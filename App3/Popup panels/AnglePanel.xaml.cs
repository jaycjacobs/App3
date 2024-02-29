using Cirros;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using App3;

namespace Cirros8
{
    public sealed partial class AnglePanel : UserControl
    {
        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;
        Popup _popup;
        Point _workCanvasOffset;

        Rect _box = Rect.Empty;

        public AnglePanel()
        {
            this.InitializeComponent();

            this.Loaded += AnglePanel_Loaded;
            this.PointerPressed += AnglePanel_PointerPressed;
            this.PointerMoved += AnglePanel_PointerMoved;
            this.PointerReleased += AnglePanel_PointerReleased;

            DataContext = Globals.UIDataContext;
        }

        public Point WorkCanvasOffset
        {
            set
            {
                _workCanvasOffset = value;
            }
        }

        void AnglePanel_Loaded(object sender, RoutedEventArgs e)
        {
            // Assume that Show() has already been called

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

        void AnglePanel_PointerPressed(object sender, PointerRoutedEventArgs e)
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

        void AnglePanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _popup.HorizontalOffset = p.X - _dragXOff;
                _popup.VerticalOffset = p.Y - _dragYOff;
            }
        }

        void AnglePanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _isDragging = false;
        }

        public void ShowAngle(List<Point> pc)
        {
            List<Point> mpc = new List<Point>();

            if (_box == Rect.Empty)
            {
                foreach (Point p in pc)
                {
                    _box.Union(Globals.View.PaperToDisplay(p));
                }
            }

            double startAngle = -Construct.Angle(pc[1], pc[0]) * Construct.cRadiansToDegrees;
            double endAngle = -Construct.Angle(pc[1], pc[2]) * Construct.cRadiansToDegrees;
            double includedAngle = -Construct.IncludedAngle(pc[0], pc[1], pc[2]) * Construct.cRadiansToDegrees;

            _startBlock.Text = string.Format("{0:0.00}°", startAngle);
            _endBlock.Text = string.Format("{0:0.00}°", endAngle);
            _includedBlock.Text = string.Format("{0:0.00}°", includedAngle);
        }
    }
}
