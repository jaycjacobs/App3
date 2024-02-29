using Cirros.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UWP
using Windows.Foundation;
#else
using System.Windows;
#endif

namespace Cirros.Drawing
{
    public interface IDrawingContainer
    {
        double ModelToPaper(double model);
        Point ModelToPaper(Point model);
        Point ModelToPaperDelta(FPoint model);
        Point ModelToPaperDelta(Point model);
        FPoint PaperToModelDeltaF(Point paper);
        double PaperToModel(double paper);
        Point PaperToModel(Point paper);
        FPoint PaperToModelF(Point paper);
        Point PaperToModelDelta(Point paper);
        void AddPrimitive(Primitive p);
        uint NewObjectId();
    }
}
