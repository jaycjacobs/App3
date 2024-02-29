using Cirros.Drawing;
using Cirros.Primitives;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.UI;

namespace Cirros.Core.Display
{
    public interface IVectorCanvas
    {
        ICanvasResourceCreator ResourceCreator { get; }
        VectorList VectorList { get; set; }
        int FixupLevel { get; set; }
        void Regenerate();
        void Redraw();
        void RedrawOverlay();
        void UpdateLineStyles();
        void Zoom(double factor);
        void ActualSizeWindow();
        void Pan(double dx, double dy);
        void PanToPoint(double cx, double cy);
        void SetWindow(Point p1, Point p2);
        void SetWindow(Rect r);
        Rect GetWindow();
        void AddSegment(VectorEntity ve);
        void AddOverlaySegment(VectorEntity ve);
        void RemoveSegment(uint segmentId);
        void RemoveOverlaySegment(uint segmentId);
        uint PickSegment(Point paper);
        int GetMemberIndex(uint segmentId, Point paper);
        void HighlightSegment(uint segmentId, bool flag);
        void HighlightMember(uint segmentId, int index);
        void HideSegment(uint segmentId, bool flag);
        void SetSegmentZIndex(uint _objectId, int _zIndex);
        void MoveSegmentBy(uint segmentId, double dx, double dy);
        VectorEntity UpdateSegment(Primitive p);
        System.Threading.Tasks.Task LoadImage(PImage pi);
        void LoadImage(string imageId);
        Point DisplayToPaper(Point display);
        Point PaperToDisplay(Point paper);
        void ShowGrid(bool show);
        //void DrawOverlayLine(Point from, Point to, Color stroke);
        //void DrawOverlayRectangle(Rect rect, Color stroke, Color fill);
        //void RandomLinesTest(int count);
        void ShowItemBoxes(bool show);
        bool ShowMagnifier { get; set; }
        Size PaperSize { get; set; }
        double GridSpacing { get; set; }
        uint GridDivisions { get; set; }
        double DisplayGridSpacing { get; }
        uint DisplayGridDivisions { get; }

        void SetSegmentOpacity(uint _objectId, double v);
    }
}
