#if UWP
using Cirros.Core.Display;
using Windows.Foundation;
#else
using CirrosCore;
using System.Windows;
#endif

namespace Cirros.Core
{
    public interface IDrawingView
    {
        IVectorCanvas VectorListControl { get; }

        double PaperToDisplay(double paper);
        Point PaperToDisplay(Point paper);
        double DisplayToPaper(double display);
        Point DisplayToPaper(Point display);

        Point ModelToDisplay(Point model);
        Point ModelToDisplayRound(Point model);
        Point ModelToDisplayRaw(Point model);
        double ModelToDisplay(double model);
        Rect CurrentWindow { get; set; }
        void WindowChanged();
        Size ViewPortSize { get; set; }
        void DisplayAll();
        void DisplayActualSize();
        void DisplayActualSize(double cx, double cy);
        void DisplayWindow(double lx, double ly, double ux, double uy, bool restrict = true);
        void Pan(Point delta);
        void Pan(double horizontalFraction, double verticalFraction);
        void Zoom(double cx, double cy, double zoom, bool center);
        void Zoom(double zoom);
        void Regenerate();
    }
}
