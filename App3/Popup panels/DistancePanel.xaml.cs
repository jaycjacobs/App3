using Cirros;
using Cirros.Drawing;
using Cirros.Utility;
using System;
using System.Globalization;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using App3;

namespace Cirros8
{
    public sealed partial class DistancePanel : UserControl
    {
        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;
        Popup _popup;
        Point _from;
        Point _to;
        Point _workCanvasOffset;

        public DistancePanel()
        {
            this.InitializeComponent();

            this.Loaded += DistancePanel_Loaded;
            this.PointerPressed += DistancePanel_PointerPressed;
            this.PointerMoved += DistancePanel_PointerMoved;
            this.PointerReleased += DistancePanel_PointerReleased;

            DataContext = Globals.UIDataContext;
        }

        public Point WorkCanvasOffset
        {
            set
            {
                _workCanvasOffset = value;
            }
        }

        void DistancePanel_Loaded(object sender, RoutedEventArgs e)
        {
            // Assume that Show() has already been called 
            _popup = this.Parent as Popup;
           if (_popup != null && ActualHeight > 0 && ActualWidth > 0)
            {
                Point pf = Globals.View.PaperToDisplay(_from);
                Point pt = Globals.View.PaperToDisplay(_to);
                Point mp = new Point((pf.X + pt.X) / 2, (pf.Y + pt.Y) / 2);

                mp.X += _workCanvasOffset.X;
                mp.Y += _workCanvasOffset.Y;

                _popup.HorizontalOffset = mp.X;
                _popup.VerticalOffset = mp.Y;

                if (_from.X != _to.X)
                {
                    double m = (_to.Y - _from.Y) / (_to.X - _from.X);

                    if (m > 0)
                    {
                        if (_popup.VerticalOffset < ActualHeight)
                        {
                            _popup.HorizontalOffset -= ActualWidth;
                        }
                        else if (_popup.HorizontalOffset > (App.Window.Bounds.Width - ActualWidth))
                        {
                           _popup.HorizontalOffset -= ActualWidth;
                            
                            if ((_popup.VerticalOffset + ActualHeight) > App.Window.Bounds.Height)
                            {
                                _popup.VerticalOffset = Math.Min(pf.Y, pt.Y) - ActualHeight - 10;
                            }
                        }
                        else
                        {
                            _popup.VerticalOffset -= ActualHeight;
                        }
                    }
                    else
                    {
                        if (_popup.VerticalOffset > (App.Window.Bounds.Height - ActualHeight))
                        {
                            _popup.VerticalOffset -= ActualHeight;
                            _popup.HorizontalOffset -= ActualWidth;
                        }
                        else if (_popup.HorizontalOffset > (App.Window.Bounds.Width - ActualWidth))
                        {
                            if (_popup.VerticalOffset < ActualHeight)
                            {
                                _popup.HorizontalOffset = Math.Min(pf.X, pt.X) - ActualWidth - 10;
                            }
                            else
                            {
                                _popup.VerticalOffset -= ActualHeight;
                                _popup.HorizontalOffset -= ActualWidth;
                            }
                        }
                    }
                }
            }

            if (_unit == Unit.Model)
            {
                _modelRB.IsChecked = true;
            }
            else
            {
                _paperRB.IsChecked = true;
            }
        }

        void DistancePanel_PointerPressed(object sender, PointerRoutedEventArgs e)
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

        void DistancePanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _popup.HorizontalOffset = p.X - _dragXOff;
                _popup.VerticalOffset = p.Y - _dragYOff;
            }
        }

        void DistancePanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _isDragging = false;
        }

        private Unit _unit = Unit.Model;

        public void ShowDistance(Point from, Point to)
        {
            _from = from;
            _to = to;

            if (_unit == Unit.Model)
            {
                Point mf = Globals.ActiveDrawing.PaperToModel(from);
                Point mt = Globals.ActiveDrawing.PaperToModel(to);

                double dx = mt.X - mf.X;
                double dy = mt.Y - mf.Y;
                double distance = Construct.Distance(mf, mt);

                //bool architect = Globals.ActiveDrawing.IsArchitecturalScale && Globals.Input.GridSnap;
                bool architect = false;
                if (Globals.Input.GridSnap && Globals.xSnap > .0001)
                {
                    long n = (int)Math.Round(distance / Globals.xSnap);
                    double test = n * Globals.xSnap;
                    double t = Math.Abs(distance - test);
                    architect = t < Globals.xSnap / 32;
                }

                _dxBlock.Text = Utilities.FormatDistance(dx, Globals.DimensionRound, architect, true, Globals.ActiveDrawing.ModelUnit, false);
                _dyBlock.Text = Utilities.FormatDistance(dy, Globals.DimensionRound, architect, true, Globals.ActiveDrawing.ModelUnit, false);
                _distanceBlock.Text = Utilities.FormatDistance(distance, Globals.DimensionRound, architect, true, Globals.ActiveDrawing.ModelUnit, false);
            }
            else
            {
                Point mf = Globals.ActiveDrawing.PaperToModel(from);
                Point mt = Globals.ActiveDrawing.PaperToModel(to);

                double dx = to.X - from.X;
                double dy = to.Y - from.Y;
                double distance = Construct.Distance(from, to);

                _dxBlock.Text = Utilities.FormatDistance(dx, Globals.DimensionRound, false, true, Globals.ActiveDrawing.PaperUnit, false);
                _dyBlock.Text = Utilities.FormatDistance(dy, Globals.DimensionRound, false, true, Globals.ActiveDrawing.PaperUnit, false);
                _distanceBlock.Text = Utilities.FormatDistance(distance, Globals.DimensionRound, false, true, Globals.ActiveDrawing.ModelUnit, false);

            }

            double angle = -Construct.Angle(from, to) * Construct.cRadiansToDegrees;
            _angleBlock.Text = string.Format("{0:0.00}°", angle);
        }

        private void _unitRB_Checked(object sender, RoutedEventArgs e)
        {
            _unit = _modelRB.IsChecked == true ? Unit.Model : Unit.Paper;

            ShowDistance(_from, _to);
        }
    }
}
