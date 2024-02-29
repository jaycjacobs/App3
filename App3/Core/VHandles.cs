using Cirros.Display;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Core
{
    public class VHandle : IHandle
    {
        public static uint cHandleBaseId = 10000000;
        public static uint _handleIdSequence = 0;
        public static uint _handleIdPool = 1024;
        public static uint cHandleFrameId = cHandleBaseId - 1;

        const double cMarkerSize = 8;

        double _x;
        double _y;

        double _opacity = 1;

        bool _isVisible = true;

        uint _id;
        uint _objectId;

        protected HandleType _type = HandleType.Triangle;

        public VHandle(uint id, double x, double y, HandleType type, double opacity = 1)
        {
            _id = id;
            _objectId = cHandleBaseId + _handleIdSequence;
            _handleIdSequence = ++_handleIdSequence % _handleIdPool;

            _x = x;
            _y = y;
            _type = type;
            _opacity = opacity;
        }

        public uint Id
        {
            get
            {
                return _id;
            }
        }

        public Point Location
        {
            get
            {
                return new Point(_x, _y);
            }
        }

        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                _isVisible = value;
            }
        }

        VectorEntity _ve;
        VectorMarkerEntity _vm;

        public void Draw(bool visible)
        {
            _ve = new VectorEntity(_objectId, (int)(100000 + _id));

            _ve.Color = Globals.ActiveDrawing.Theme.HandleColor;
            _ve.FillColor = Globals.ActiveDrawing.Theme.HandleFillColor;
            _ve.Fill = true;
            _ve.LineWidth = 1;
            _ve.IsVisible = visible;

            _vm = new VectorMarkerEntity();
            _vm.Type = _type;
            _vm.Size = cMarkerSize;
            _vm.Opacity = _opacity;
            _vm.Location = new Point(_x, _y);
            _ve.AddChild(_vm);

            Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
        }

        public void Dispose()
        {
            Globals.DrawingCanvas.VectorListControl.RemoveOverlaySegment(_ve.SegmentId);
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
        }

        public void Move(double dx, double dy)
        {
            _x += dx;
            _y += dy;

            _vm.Location = new Point(_x, _y);
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
        }

        public void Select(bool flag)
        {
            _ve.FillColor = flag ? Globals.ActiveDrawing.Theme.HandleColor : Globals.ActiveDrawing.Theme.HandleFillColor;
            _ve.ZIndex = (int)(flag ? VHandle.cHandleBaseId + _id + 500 : VHandle.cHandleBaseId + _id);
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
        }
    }

    public class VHandleFrame : IHandleFrame
    {
        protected VectorEntity _ve;

        public VHandleFrame()
        {
        }

        public VHandleFrame(Rect r, int zindex)
        {
            Initialize();

            List<Point> pc = new List<Point>();
            pc.Add(new Point(r.Left, r.Top));
            pc.Add(new Point(r.Left + r.Width, r.Top));
            pc.Add(new Point(r.Left + r.Width, r.Top + r.Height));
            pc.Add(new Point(r.Left, r.Top + r.Height));
            pc.Add(new Point(r.Left, r.Top));

            _ve.AddChild(pc);

            Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
        }

        protected void Initialize()
        {
            _ve = new VectorEntity(VHandle.cHandleFrameId, 99999);

            _ve.Color = Globals.ActiveDrawing.Theme.HandleColor;
            _ve.FillColor = Globals.ActiveDrawing.Theme.HandleFillColor;
            //_ve.LineWidth = Globals.DrawingCanvas.DisplayToPaper(.1);
            _ve.LineWidth = .5;
            _ve.ScaleLineWidth = false;
        }

        public void Move(double dx, double dy)
        {
            _ve.Move(dx, dy);
        }

        public void Dispose()
        {
            Globals.DrawingCanvas.VectorListControl.RemoveOverlaySegment(_ve.SegmentId);
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();

            _ve = null;
        }
    }

    public class VHandleLine : VHandleFrame, IHandleLine
    {
        public VHandleLine(List<Point> pc, int zindex) : base()
        {
            Initialize();

            _ve.AddChild(pc);

            Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
        }
    }

    public class VHandleArrow : VHandleFrame, IHandleArrow
    {
        public VHandleArrow(Point from, Point to, int zindex)
        {
            Initialize();

            List<Point> pcs = new List<Point>();
            pcs.Add(from);
            pcs.Add(to);
            _ve.AddChild(pcs);

            double size = Globals.DrawingCanvas.DisplayToPaper(Globals.hitTolerance * .8);
            _ve.AddChild(CGeometry.ArrowPointCollection(to, from, ArrowType.Filled, size, .25));

            Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(_ve);
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
        }
    }
}
