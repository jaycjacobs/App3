using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Dialogs
{
    public sealed partial class IndeterminateProgressDialog : UserControl
    {
        public IndeterminateProgressDialog(string message)
        {
            this.InitializeComponent();
            _textBox.Text = message;
        }

        Popup _popup = new Popup();

        private static Rect GetElementRect(FrameworkElement element, UIElement visual)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(visual);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        public void Show()
        {
            if (!_popup.IsOpen)
            {
                Frame root = App.Window.Content as Frame;

                double top = root.ActualHeight / 2 - this.Height;
                double left = root.ActualWidth / 2 - this.Width / 2;

                if (Globals.DrawingCanvas != null && Globals.DrawingCanvas.Parent is Canvas canvas)
                {
                    top = canvas.ActualHeight / 2 - this.Height / 2;
                    left = canvas.ActualWidth / 2 - this.Width / 2;
                }

                _popup.Child = this as UserControl;
                _popup.VerticalOffset = top > 0 ? top : 0;
                _popup.HorizontalOffset = left > 0 ? left : 0;
                _popup.IsOpen = true;
            }
        }

        public void Hide()
        {
            if (_popup != null && _popup.IsOpen)
            {
                _popup.IsOpen = false;
            }
        }
    }
}
