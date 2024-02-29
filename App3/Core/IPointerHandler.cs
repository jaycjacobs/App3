//#define debugtracking

using Cirros.Core;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;

#if debugtracking
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
#endif

namespace Cirros
{
    public abstract class IPointerHandler
    {
        protected IDrawingInput _input = null;
#if debugtracking
        protected Rectangle _pecker = null;
#endif
        public IPointerHandler(IDrawingInput input)
        {
            _input = input;
#if debugtracking
            _pecker = new Rectangle();
            _pecker.Height = Globals.View.DisplayToCanvas(.1);
            _pecker.Width = Globals.View.DisplayToCanvas(.1);
            _pecker.StrokeThickness = Globals.View.DisplayToCanvas(.01);
            _pecker.Fill = new SolidColorBrush(Colors.Orange);
            _pecker.Stroke = new SolidColorBrush(Colors.Black);
            _pecker.Visibility = Visibility.Collapsed;
            Globals.DrawingCanvas.Children.Add(_pecker);
#endif
        }

        public async virtual Task<bool> PointerPressed(Point p, PointerRoutedEventArgs e)
        {
#if debugtracking
            double size = Globals.View.DisplayToCanvas(10);
            double hsize = size / 2;
            Point pp = e.GetCurrentPoint(Globals.DrawingCanvas).Position;
            _pecker.Height = size;
            _pecker.Width = size;
            _pecker.StrokeThickness = Globals.View.DisplayToCanvas(1);
            _pecker.SetValue(Canvas.LeftProperty, pp.X - hsize);
            _pecker.SetValue(Canvas.TopProperty, pp.Y - hsize);
            _pecker.Visibility = Visibility.Visible;
#else
            await Task.Delay(1);
#endif
            return true;
        }

        public virtual bool PointerReleased(Point p, PointerRoutedEventArgs e)
        {
#if debugtracking
            _pecker.Visibility = Visibility.Collapsed;
#endif
            return true;
        }

        public abstract bool PointerMoved(Point p, PointerRoutedEventArgs e);

        public abstract void Capture(PointerRoutedEventArgs e);

        public virtual bool Release(PointerRoutedEventArgs e)
        {
            return false;
        }

        public virtual void PointerEnter(Point p, PointerRoutedEventArgs e)
        {
            _input.AcquireCursor = true;
        }

        public virtual void PointerLeave(Point p, PointerRoutedEventArgs e)
        {
            _input.AcquireCursor = false;
            Globals.Input.CursorVisible = false;
        }
    }

    class GenericPointerHandler : IPointerHandler
    {
        public GenericPointerHandler(DrawingCanvas canvas)
            : base(canvas)
        {
        }

        public override bool PointerMoved(Windows.Foundation.Point p, PointerRoutedEventArgs e)
        {
            return true;
        }

        public override void Capture(PointerRoutedEventArgs e)
        {
        }
    }
}
