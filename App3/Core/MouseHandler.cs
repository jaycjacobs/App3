using Cirros.Core;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;

namespace Cirros
{
    public class MouseHandler : IPointerHandler
    {
        protected bool _leftButton = false;
        protected bool _panButton = false;
        protected Point _startPoint;

        public MouseHandler(IDrawingInput canvas) : base(canvas)
        {
        }

        public override void Capture(PointerRoutedEventArgs e)
        {
        }

        public override bool Release(PointerRoutedEventArgs e)
        {
            return base.Release(e);
        }

        bool _dragHappened = false;

        public async override Task<bool> PointerPressed(Windows.Foundation.Point p, PointerRoutedEventArgs e)
        {
            PointerPointProperties ppp = e.GetCurrentPoint(Globals.DrawingCanvas).Properties;
            _leftButton = ppp.IsLeftButtonPressed;

            if (Globals.MousePanButton == Cirros.Globals.MouseButtonType.Middle && ppp.IsMiddleButtonPressed)
            {
                _panButton = true;
            }
            else if (Globals.MousePanButton == Cirros.Globals.MouseButtonType.Right && ppp.IsRightButtonPressed)
            {
                _panButton = true;
            }
            else if (Globals.MousePanButton == Cirros.Globals.MouseButtonType.Button1 && ppp.IsXButton1Pressed)
            {
                _panButton = true;
            }
            else if (Globals.MousePanButton == Cirros.Globals.MouseButtonType.Button2 && ppp.IsXButton1Pressed)
            {
                _panButton = true;
            }

            if (_leftButton)
            {
                await base.PointerPressed(p, e);
            }
            else if (_panButton)
            {
                _startPoint = p;
                //System.Diagnostics.Debug.WriteLine("pan: _startPoint.X = {0}, _startPoint.Y = {1}", _startPoint.X, _startPoint.Y);
            }

            _dragHappened = false;

            return _leftButton;
        }

        public override bool PointerReleased(Windows.Foundation.Point p, PointerRoutedEventArgs e)
        {
            bool usePoint = false;

            if (_leftButton)
            {
                base.PointerReleased(p, e);
                _leftButton = false;
                usePoint = true;
            }
            else if (_panButton)
            {
                if (_dragHappened == false)
                {
                    Globals.Events.ShowMenu(true);
                }
                _panButton = false;
            }

            return usePoint;
        }

        public override bool PointerMoved(Windows.Foundation.Point p, PointerRoutedEventArgs e)
        {
            if (_leftButton)
            {
            }
            else if (_panButton)
            {
                double dx = p.X - _startPoint.X;
                double dy = p.Y - _startPoint.Y;

                _startPoint = p;

                Globals.View.Pan(new Point(dx, dy));

                _dragHappened = true;
            }
            return true;
        }
    }
}
