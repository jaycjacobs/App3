using Cirros.Primitives;
using Cirros.Display;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Cirros.Utility;
using System;
using System.Collections.Generic;

namespace Cirros.Commands
{
    public abstract class DistanceBaseCommandProcessor : CommandProcessor
    {
        protected Handles _handles = new Handles();
        protected int _selectedHandleId = -1;
        protected List<Point> _points = new List<Point>();
        protected Primitive _line = null;
        protected bool _isTracking = false;

        protected double _lastTrackX;
        protected double _lastTrackY;

        public override InputMode InputMode
        {
            get
            {
                return _handles.Count > 0 ? InputMode.Pick : InputMode.Draw;
            }
        }

        public override void CanvasScaleChanged()
        {
            _handles.Draw();
            base.CanvasScaleChanged();
        }

        protected abstract void Report();

        public override void TrackCursor(double x, double y)
        {
            base.TrackCursor(x, y);

            if (_isTracking)
            {
                double dx = x - _lastTrackX;
                double dy = y - _lastTrackY;

                if (_selectedHandleId > 0)
                {
                    // A handle is selected.  Drag it.
                    if (dx != 0 || dy != 0)
                    {
                        _handles.MoveHandle(_selectedHandleId, dx, dy);

                        // [primitive].MoveHandle may have changed the handle location and selection to conform to the primitive geometry
                        // so _lastTrack[XY] and the selected index should be adjusted accordingly

                        Point h2 = _handles.GetHandlePoint(_selectedHandleId);
                        _lastTrackX = h2.X;
                        _lastTrackY = h2.Y;

                        _handles.Select(_selectedHandleId);
                    }

                    Report();

                    Globals.Events.CoordinateDisplay(new Point(x, y));
                }
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_isTracking)
            {
                if (Globals.Input.ObjectSnap && _shiftKey == false && _selectedHandleId >= 0)
                {
                    // If object snap is enabled, adjust the final point accordingly
                    //Point trianglePoint;

                    if (_constructHandles.SelectedHandleID >= 0)
                    {
                        Point c = _constructHandles.SelectedHandle.Location;
                        Point h = _handles.GetHandlePoint(_selectedHandleId);
                        double dx = c.X - h.X;
                        double dy = c.Y - h.Y;

                        _handles.MoveHandle(_selectedHandleId, dx, dy);

                        Report();
                    }
                    else if (Globals.DrawingTools.ActiveTrianglePoint(out Point trianglePoint))
                    {
                        Point c = trianglePoint;
                        Point h = _handles.GetHandlePoint(_selectedHandleId);
                        double dx = c.X - h.X;
                        double dy = c.Y - h.Y;

                        _handles.MoveHandle(_selectedHandleId, dx, dy);

                        Report();
                    }
                }

                _isTracking = false;
            }
        }

        public override Point Step(double dx, double dy, bool stillDown)
        {
            Point paper = base.Step(dx, dy, stillDown);

            if (_selectedHandleId > 0)
            {
                _handles.MoveHandle(_selectedHandleId, dx, dy);

                Globals.Events.PrimitiveSelectionSizeChanged(null);

                _handles.Select(_selectedHandleId);

                paper = _handles.SelectedHandle.Location;

                Report();
            }

            return paper;
        }

        //protected override void Hover(Point from, Point current, Point through)
        //{
        //    if (_rubberBand.State == 1)
        //    {
        //        _constructNodes.Clear();

        //        // Check for workCanvas not null - this could be hit after workCanvas is closed
        //        if (Globals.ActiveDrawing != null)
        //        {
        //            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
        //            {
        //                if (Globals.LayerTable[p.LayerId].Visible && p.IsNear(current, _hoverDistance))
        //                {
        //                    _constructNodes.AddRange(p.ConstructNodes);
        //                    _constructNodes.AddRange(p.DynamicConstructNodes(from, through));
        //                }
        //            }
        //            _constructNodes.AddRange(Globals.DrawingTools.DynamicTriangleConstructNodes(from, through));
        //        }
        //    }
        //    else
        //    {
        //        base.Hover(from, current, through);
        //    }
        //}

        protected void DisposeLine()
        {
            if (_line != null)
            {
                _handles.Detach();
                Globals.ActiveDrawing.DeletePrimitive(_line);
                _line = null;

                Globals.Events.Measure(MeasureType.none, null);
            }
        }

        public override void Finish()
        {
            DisposeLine();
            base.Finish();
        }
    }

    public class AreaCommandProcessor : DistanceBaseCommandProcessor
    {
        double _strokeThickness = 1;

        public AreaCommandProcessor()
        {
            _type = CommandType.area;
            _rubberBand = new RubberBandBasic();

            _strokeThickness = Globals.DrawingCanvas.DisplayToPaper(1);
        }

        public override bool EnableCommand(object o)
        {
            bool enable = base.EnableCommand(o);

            if (enable == false)
            {
                if (o is string && (string)o == "A_Done")
                {
                    enable = _rubberBand.State == 2;
                }
            }

            return enable;
        }

        public override void DoubleClick()
        {
            Close();
        }

        public override void Invoke(object o, object parameter)
        {
            if (o is string)
            {
                switch ((string)o)
                {
                    case "A_Done":
                        Close();
                        //_rubberBand.State = 0;
                        break;
                }
            }
        }

        public override void CanvasScaleChanged()
        {
            base.CanvasScaleChanged();

            _strokeThickness = Globals.DrawingCanvas.DisplayToPaper(1);
            if (_line != null)
            {
                _line.LineWeightId =(int)Math.Ceiling(_strokeThickness * 1000);
            }
        }

        protected override void Report()
        {
            PPolygon pg = _line as PPolygon;
            if (pg != null && pg.Points.Count > 1)
            {
                Point o = pg.Origin;

                _points.Clear();
                _points.Add(o);

                foreach (Point p in pg.Points)
                {
                    _points.Add(new Point(p.X + o.X, p.Y + o.Y));
                }

                Globals.Events.Measure(MeasureType.area, _points);
            }
        }


        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            _selectedHandleId = _handles.Hit(x, y);

            if (_selectedHandleId >= 0)
            {
                _isTracking = true;
            }
            else if (_rubberBand.State == 0)
            {
                DisposeLine();

                _first = _start;

                _rubberBand = new RubberBandLine(Globals.Theme.ForegroundColor);
                _rubberBand.State = 1;
                _rubberBand.StartTracking(_start.X, _start.Y);
            }
            else if (_rubberBand.State == 1)
            {
                if (_first.X != _start.X || _first.Y != _start.Y)
                {
                    //_rubberBand = new RubberBandBasic();
                    _rubberBand.Reset();

                    if (_line == null)
                    {
                        PPolygon pg = new PPolygon(_first);
                        pg.ConstructEnabled = false;
                        pg.LayerId = 0;
                        pg.ColorSpec = Utilities.ColorSpecFromColor(Globals.ActiveDrawing.Theme.ForegroundColor);
                        pg.LineWeightId = (int)Math.Ceiling(_strokeThickness * 1000);
                        pg.AddToContainer(Globals.ActiveDrawing);
                        pg.AddPoint(_start.X, _start.Y, false);
                        pg.AddPoint(_start.X, _start.Y, true);

                        _line = pg;

                        uint colorSpec = _line.Fill;
                        _line.Fill = (colorSpec & 0x00ffffff) | 0x4d000000;

                        _rubberBand.State = 2;
                    }
                }
            }
            else if (_rubberBand.State == 2)
            {
                ((PPolygon)_line).AddPoint(_start.X, _start.Y, true);

                Report();
            }
            else if (_rubberBand.State == 3)
            {
                DisposeLine();

                _rubberBand.State = 0;
            }

            _lastTrackX = x;
            _lastTrackY = y;
        }

        public override void TrackCursor(double x, double y)
        {
            base.TrackCursor(x, y);

            if (_rubberBand.State == 2)
            {
                if (_line != null)
                {
                    _line.MoveHandlePoint(((PPolygon)_line).Points.Count + 1, new Point(x, y));

                    Report();
                }
            }
        }

        void Close()
        {
            if (_line == null)
            {
                //_rubberBand = new RubberBandBasic();
                if (_rubberBand != null)
                {
                    _rubberBand.Reset();
                }
                _rubberBand.State = 0;
            }
            else
            {
                if (_rubberBand.State == 3)
                {
                    // If we're already in handle mode, start over
                    DisposeLine();

                    //_rubberBand = new RubberBandBasic();
                    _rubberBand.Reset();
                    _rubberBand.State = 0;
                }
                else
                {
                    if (((PPolygon)_line).Points.Count <= 2)
                    {
                        // If we have less than three nodes, start over
                        DisposeLine();

                        //_rubberBand = new RubberBandBasic();
                        _rubberBand.Reset();
                        _rubberBand.State = 0;
                    }
                    else
                    {
                        // If we have at least three nodes, terminate the definition and show the handles
                        ((PPolygon)_line).RemovePoint();

                        _rubberBand.State = 3;

                        _line.Highlight(true);
                        _line.ShowHandles(_handles);
                    }
                }
            }
        }

        public override void KeyDown(string key, bool shift, bool control, bool gmk)
        {
            if (gmk)
            {
                if (key == "enter" || key == "escape")
                {
                    Close();
                    Globals.Events.Measure(MeasureType.area, _points);
                    Report();
                }
                else if (key == "x")
                {
                    Point p = new Point(_current.X, _start.Y);

                    StartTracking(p.X, p.Y, false, false);
                    EndTracking(p.X, p.Y);
                }
                else if (key == "y")
                {
                    Point p = new Point(_start.X, _current.Y);

                    StartTracking(p.X, p.Y, false, false);
                    EndTracking(p.X, p.Y);
                }
            }

            if (key == "shift")
            {
                _shiftKey = true;
                _constructHandles.Visible = false;
            }
        }
    }

    public class AngleCommandProcessor : DistanceBaseCommandProcessor
    {
        public AngleCommandProcessor()
        {
            _type = CommandType.angle;

            _rubberBand = new RubberBandLine(Globals.Theme.ForegroundColor);
        }

        protected override void Report()
        {
            if (_handles.Count == 3)
            {
                _points.Clear();
                _points.Add(_handles.GetHandlePoint(1));
                _points.Add(_handles.GetHandlePoint(2));
                _points.Add(_handles.GetHandlePoint(3));

                Globals.Events.Measure(MeasureType.angle, _points);
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            _selectedHandleId = _handles.Hit(x, y);

            if (_selectedHandleId >= 0)
            {
                _isTracking = true;
            }
            else if (_rubberBand.State == 0)
            {
                _first = _start;

                DisposeLine();

                _rubberBand.State = 1;
                _rubberBand.StartTracking(_start.X, _start.Y);
            }
            else if (_rubberBand.State == 1)
            {
                if (_first.X != _start.X || _first.Y != _start.Y)
                {
                    if (_line == null)
                    {
                        _line = new PLine(_first, _start);
                        _line.ConstructEnabled = false;
                        _line.LayerId = 0;
                        _line.AddToContainer(Globals.ActiveDrawing);
                    }
                    _rubberBand.State = 2;
                    _rubberBand.StartTracking(_start.X, _start.Y);
                }
            }
            else if (_rubberBand.State == 2)
            {
                ((PLine)_line).AddPoint(_start.X, _start.Y, true);
                _points.Add(_start);
                _line.Highlight(true);
                _line.ShowHandles(_handles);

                Report();

                _rubberBand.EndTracking();
                _rubberBand.State = 3;

                Start();
            }
            else if (_rubberBand.State == 3)
            {
                DisposeLine();

                _rubberBand.State = 0;
            }

            _lastTrackX = x;
            _lastTrackY = y;
        }
    }

    public class DistanceCommandProcessor : DistanceBaseCommandProcessor
    {
        public DistanceCommandProcessor()
        {
            _type = CommandType.distance;
            _rubberBand = new RubberBandLine(Globals.Theme.ForegroundColor);

            _points.Clear();
            _points.Add(new Point());
            _points.Add(new Point());
        }

        protected override void Report()
        {
            if (_points.Count == 2)
            {
                _points[0] = _handles.GetHandlePoint(1);
                _points[1] = _handles.GetHandlePoint(2);
                Globals.Events.Measure(MeasureType.distance, _points);
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            _selectedHandleId = _handles.Hit(x, y);

            if (_selectedHandleId >= 0)
            {
                _isTracking = true;
            }
            else if (_rubberBand.State == 0)
            {
                _first = _start;

                DisposeLine();

                _rubberBand.State = 1;
                _rubberBand.StartTracking(_start.X, _start.Y);
            }
            else if (_rubberBand.State == 1)
            {
                if (_first.X != _start.X || _first.Y != _start.Y)
                {
                    if (_line == null)
                    {
                        _line = new PLine(_first, _start);
                        _line.ConstructEnabled = false;
                        _line.LayerId = 0;
                        _line.AddToContainer(Globals.ActiveDrawing);
                        _line.Highlight(true);
                        _line.ShowHandles(_handles);

                        Report();
                    }
                }

                _rubberBand.EndTracking();
                _rubberBand.State = 3;

                Start();
            }
            else// if (_rubberBand.State == 3)
            {
                DisposeLine();

                _rubberBand.State = 0;
            }

            _lastTrackX = x;
            _lastTrackY = y;
        }
    }

    public class OriginCommandProcessor : CommandProcessor
    {
        protected Handles _handles = new Handles();
        Point _newPaperOrigin;
        private bool _isTracking;

        public OriginCommandProcessor()
        {
            _type = CommandType.origin;
            _rubberBand = new RubberBandBasic();
            _newPaperOrigin = Globals.ActiveDrawing.ModelToPaperRaw(Globals.ActiveDrawing.Origin);

            Globals.Events.OriginChanged(Globals.ActiveDrawing.Origin, false);

            _handles.AddHandle(1, _newPaperOrigin.X, _newPaperOrigin.Y, HandleType.Triangle);
            _handles.Draw();
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            if (_handles.Count == 0)
            {
                _handles.AddHandle(1, Globals.ActiveDrawing.Origin.X, Globals.ActiveDrawing.Origin.Y, HandleType.Triangle);
                _handles.Draw();
            }

            base.StartTracking(x, y, shift, control);

            _isTracking = true;
        }

        public override void TrackCursor(double x, double y)
        {
            base.TrackCursor(x, y);

            if (_isTracking && _handles.Count > 0)
            {
                _newPaperOrigin = new Point(x, y);

                Point p = _handles.GetHandlePoint(1);
                double dx = x - p.X;
                double dy = y - p.Y;

                    if (dx != 0 || dy != 0)
                    {
                        _handles.MoveHandle(1, dx, dy);

                        // [primitive].MoveHandle may have changed the handle location and selection to conform to the primitive geometry
                        // so _lastTrack[XY] and the selected index should be adjusted accordingly

                        Point h2 = _handles.GetHandlePoint(1);

                        _handles.Select(1);
                        _handles.Draw();
                    }

                    //Globals.Events.CoordinateDisplay(new Point(x, y));

                    Globals.Events.OriginChanged(Globals.ActiveDrawing.PaperToModelRaw(_newPaperOrigin), false);
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_isTracking)
            {
                if (Globals.Input.ObjectSnap && _shiftKey == false)
                {
                    // If object snap is enabled, adjust the final point accordingly

                    if (_constructHandles.SelectedHandleID >= 0)
                    {
                        _newPaperOrigin = _constructHandles.SelectedHandle.Location;
                    }
                    else if (Globals.DrawingTools.ActiveTrianglePoint(out Point trianglePoint))
                    {
                        _newPaperOrigin = trianglePoint;
                    }
                    else
                    {
                        _newPaperOrigin = new Point(x, y);
                    }
                }
                else
                {
                    _newPaperOrigin = new Point(x, y);
                }

                if (_handles.Count > 0)
                {
                    Point h = _handles.GetHandlePoint(1);
                    double dx = _newPaperOrigin.X - h.X;
                    double dy = _newPaperOrigin.Y - h.Y;

                    _handles.MoveHandle(1, dx, dy);
                    _handles.Draw();
                }

                Globals.ActiveDrawing.Origin = Globals.ActiveDrawing.PaperToModelRaw(_newPaperOrigin);
                Globals.Events.OriginChanged(Globals.ActiveDrawing.Origin, false);
            
                _isTracking = false;
            }
        }

        public override void Finish()
        {
            _handles.Detach();

            Globals.Events.OriginChanged(Globals.ActiveDrawing.Origin, true);

            base.Finish();
        }
    }
}
