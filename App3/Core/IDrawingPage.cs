using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Cirros.Core
{
    public interface IDrawingPage
    {
        Task ReplaceDrawing(object o);
        Panel ZoomOverlaySource { get; }
        Point ZoomOverlaySourceOffset { get; }
        Canvas ZoomOverlayTarget { get; }
        Canvas WorkCanvasTarget { get; }
        Canvas ToolsOverlay { get; }
        Grid DrawingToolsTools { get; }
        void FinalizeSettings();
        Task AutoSave(bool force);
        void DialogFirstRun(FrameworkElement sender);
        void ShowCoordinatePanel();
    }
}
