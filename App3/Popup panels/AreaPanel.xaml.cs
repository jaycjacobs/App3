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
    public sealed partial class AreaPanel : UserControl
    {
        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;
        Popup _popup;
        Point _workCanvasOffset;

        Rect _box = Rect.Empty;

        public AreaPanel()
        {
            this.InitializeComponent();

            this.Loaded += AreaPanel_Loaded;
            this.PointerPressed += AreaPanel_PointerPressed;
            this.PointerMoved += AreaPanel_PointerMoved;
            this.PointerReleased += AreaPanel_PointerReleased;

            DataContext = Globals.UIDataContext;
        }

        public Point WorkCanvasOffset
        {
            set
            {
                _workCanvasOffset = value;
            }
        }

        void AreaPanel_Loaded(object sender, RoutedEventArgs e)
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

        void AreaPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
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

        void AreaPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _popup.HorizontalOffset = p.X - _dragXOff;
                _popup.VerticalOffset = p.Y - _dragYOff;
            }
        }

        void AreaPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _isDragging = false;
        }

        public void ShowArea(List<Point> pc)
        {
            bool architect = Globals.ActiveDrawing.IsArchitecturalScale && Globals.Input.GridSnap;

            double perimeter;
            double area;

            List<Point> mpc = new List<Point>();

            if (_box == Rect.Empty)
            {
                foreach (Point p in pc)
                {
                    _box.Union(Globals.View.PaperToDisplay(p));
                }
            }

            foreach (Point p in pc)
            {
                mpc.Add(Globals.ActiveDrawing.PaperToModel(p));
            }

            Construct.AreaPerimeter(mpc, out area, out perimeter);

            _perimeterBlock.Text = Utilities.FormatDistance(perimeter, Globals.DimensionRound, architect, true, Globals.ActiveDrawing.ModelUnit, false);
            _areaBlock.Text = string.Format("{0:0.0000}", area);
#if KT22
#else
            DrawingPage page = Globals.RootVisual as DrawingPage;

            if (page != null)
            {
                page.EnableCommonButtons();
            }
#endif
        }
    }
}
