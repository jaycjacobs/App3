using Cirros.Commands;
using Cirros.Core;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Cirros
{
    class StylusPointerHandler : MouseHandler
    {
        public StylusPointerHandler(IDrawingInput canvas)
            : base(canvas)
        {
        }

        public override async Task<bool> PointerPressed(Windows.Foundation.Point p, PointerRoutedEventArgs e)
        {
            bool flag = true;
            //System.Diagnostics.Debug.WriteLine("touch press InputMode: {0}", Globals.CommandProcessor.InputMode);
            if (Globals.EnableStylusMagnifer && (Globals.CommandProcessor.InputMode == InputMode.Draw || Globals.CommandProcessor.InputMode == InputMode.Pick))
            {
                flag = !(Globals.CommandProcessor != null && Globals.CommandProcessor.InputMode == InputMode.Draw);
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine("touch press PointerPressed: ({0}, {1})", p.X, p.Y);
                flag = await base.PointerPressed(p, e);
            }

            return flag;
        }

        public override bool PointerMoved(Windows.Foundation.Point p, PointerRoutedEventArgs e)
        {
            if (Globals.EnableStylusMagnifer)
            {
                //if (Globals.DrawingTools.PointInTriangle(p) == false)
                {
                    Globals.DrawingCanvas.VectorListControl.ShowMagnifier = true;
                }
            }

            return true;
        }

        public override bool PointerReleased(Windows.Foundation.Point p, PointerRoutedEventArgs e)
        {
            if (Globals.DrawingTools.PointInTriangle(p) == false)
            {
                if (Globals.CommandProcessor.InputMode != InputMode.Select)
                {
                    Globals.DrawingCanvas.DoPointerPress(p);
                }
                //System.Diagnostics.Debug.WriteLine("touch release PointerPressed: ({0}, {1})", p.X, p.Y);
                Globals.DrawingCanvas.VectorListControl.ShowMagnifier = false;
            }

            return Globals.EnableStylusMagnifer;
        }
    }
}
