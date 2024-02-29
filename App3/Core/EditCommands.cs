using Cirros;
using Cirros.Actions;
using Cirros.Commands;
using Cirros.Display;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Cirros.Core
{
    public abstract class EditCommand
    {
        protected Handles _handles = null;
        protected Primitive _selection = null;
        protected int _selectedHandleId = -1;

        protected double _lastTrackX = 0;
        protected double _lastTrackY = 0;
        protected double _lastTrackDX = 0;
        protected double _lastTrackDY = 0;

        protected Point _lineAnchorPoint;
        protected Point _lineReferencePoint;

        protected int _undoCount = 0;

        public EditCommand(Primitive selection, Handles handles)
        {
            Selection = selection;
            _handles = handles;
        }

        public Point AnchorPoint
        {
            get { return _lineAnchorPoint; }
        }

        public Point ReferencePoint
        {
            get { return _lineReferencePoint; }
        }

        public virtual Primitive Selection
        {
            get { return _selection; }
            set
            {
                if (_selection != value)
                {
                    _selectedHandleId = -1;

                    if (this.CanHandlePrimitive(value))
                    {
                        _selection = value;
                    }
                    else
                    {
                        _selection = null;
                    }
                }
                _undoCount = 0;
            }
        }

        public virtual void StartTracking(double x, double y, bool shift, bool control)
        {
            _lastTrackX = x;
            _lastTrackY = y;

            _lineAnchorPoint = _lineReferencePoint = new Point(x, y);
        }

        public virtual void TrackCursor(ref double x, ref double y)
        {
            _lastTrackDX = x - _lastTrackX;
            _lastTrackDY = y - _lastTrackY;

            _lastTrackX = x;
            _lastTrackY = y;
        }

        public virtual void EndTracking(double x, double y)
        {
        }

        public virtual void Step(double dx, double dy)
        {
        }

        public virtual void MoveHandle(int handleId, double dx, double dy)
        {
        }

        public virtual void DrawHandles()
        {
        }

        public virtual void SelectHandle(int handleId)
        {
            _selectedHandleId = handleId;
        }

        public virtual void FinishUndo()
        {
            if (_undoCount > 1)
            {
                Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _undoCount);
            }
            _undoCount = 0;
        }

        public virtual void UndoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
        }

        public virtual void RedoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
        }

        public abstract bool CanHandlePrimitive(Primitive p);

        public abstract EditSubCommand EditSubCommand { get; }

        public virtual bool HasSelection
        {
            get
            {
                return false; // _selection != null;
            }
        }
    }

    public class EditDeleteVertexCommand : EditCommand
    {
        public EditDeleteVertexCommand(Primitive selection, Handles handles) : base(selection, handles)
        {
        }

        public override EditSubCommand EditSubCommand
        {
            get { return EditSubCommand.DeletePoint; }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_selection is PDimension dimension)
            {
                if (_selectedHandleId >= 4)
                {
                    List<CPoint> cpoints = new List<CPoint>();
                    cpoints.AddRange(dimension.CPoints);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePoints, dimension, cpoints);
                    _undoCount++;

                    dimension.RemoveHandlePoint(_selectedHandleId);
                    dimension.ShowHandles(_handles);
                    _selectedHandleId = -1;
                    FinishUndo();
                }
            }
            else if (_selection is PLine line && line.Points.Count > 1 && _selectedHandleId >= 0)
            {
                List<CPoint> cpoints = new List<CPoint>();
                cpoints.AddRange(line.CPoints);
                Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePoints, line, cpoints);
                _undoCount++;

                Point oldOrigin = line.Origin;

                line.RemoveHandlePoint(_selectedHandleId);
                line.ShowHandles(_handles);
                _selectedHandleId = -1;

                if (oldOrigin.X != line.Origin.X || oldOrigin.Y != line.Origin.Y)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.Move, line, oldOrigin);
                    _undoCount++; ;
                }
                FinishUndo();
            }
        }

        public override Primitive Selection
        {
            get => base.Selection;
            set
            {
                if (_selection != value)
                {
                    _selectedHandleId = -1;

                    if (this.CanHandlePrimitive(value))
                    {
                        _selection = value;

                        if (_selection is PDimension pdim)
                        {
                            pdim.InsertHandleMode = true;
                        }
                    }
                    else
                    {
                        _selection = null;
                    }
                }
                _undoCount = 0;
            }
        }

        public override void DrawHandles()
        {
            if (_selection != null)
            {
                _selection.ShowHandles(_handles);
            }
        }

        public override bool CanHandlePrimitive(Primitive p)
        {
            return p is PLine || (p is PDimension && p.IsTransformed == false);
        }
    }

    public class EditMoveVertexCommand : EditCommand
    {
        protected bool _allowDragObject = true;
        protected bool _shiftKey = false;
        bool _isDragging = false;

        public EditMoveVertexCommand(Primitive selection, Handles handles) : base(selection, handles)
        {
        }

        public override EditSubCommand EditSubCommand
        {
            get { return EditSubCommand.MovePoint; }
        }

        public override bool HasSelection
        {
            get
            {
                return _selection != null && _selectedHandleId != -1;
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            _shiftKey = shift;
            base.StartTracking(x, y, shift, control);

            if (_selectedHandleId != _handles.SelectedHandleID)
            {
                _selectedHandleId = _handles.SelectedHandleID;
            }

            if (_selectedHandleId > 0)
            {
                if (_selection is PImage image)
                {
                    image.PreserveAspect = shift;
                }
                if (_selection is PLine line)
                {
                    if (line.Points.Count > 0)
                    {
                        Point p = new Point(x, y);
                        int segment = line.PickSegment(ref p, out double pv);
                        if (segment < 0)
                        {
                            if (_selectedHandleId == 1)
                            {
                                segment = 0;
                                pv = 0;
                            }
                            else if (_selectedHandleId >= line.Points.Count)
                            {
                                segment = line.Points.Count - 1;
                                pv = 1;
                            }
                            else
                            {
                                segment = _selectedHandleId - 1;
                                pv = 1;
                            }
                        }

                        Point pA = new Point();
                        Point pB = new Point();

                        if (segment < line.Points.Count)
                        {
                            pA = segment == 0 ? new Point() : line.Points[segment - 1];
                            pB = line.Points[segment];
                        }
                        else if (_selection.TypeName == PrimitiveType.Polygon)
                        {
                            pA = new Point();
                            pB = line.Points[line.Points.Count - 1];
                        }

                        pA.X += line.Origin.X;
                        pA.Y += line.Origin.Y;
                        pB.X += line.Origin.X;
                        pB.Y += line.Origin.Y;

                        if (pv > .5)
                        {
                            _lineAnchorPoint = pA;
                            _lineReferencePoint = pB;
                        }
                        else
                        {
                            _lineAnchorPoint = pB;
                            _lineReferencePoint = pA;
                        }
                    }
                }
            }

            _isDragging = false;
        }

        protected void MoveVertexBy(double dx, double dy)
        {
            if (_selection is Primitive p && _selectedHandleId > 0)
            {
                if (p.IsTransformed)
                {
                    if (p.Normalize())
                    {
                        _undoCount++;
                    }
                }
                if (_isDragging == false)
                {
                    Point handlePoint = _handles.GetHandlePoint(_selectedHandleId);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveVertex, _selection, _selectedHandleId, handlePoint);
                    Globals.Input.SelectCursor(Cirros.Core.CursorType.Draw);
                    _undoCount++;

                    _isDragging = true;
                }

                FinishUndo();

                _handles.MoveHandle(_selectedHandleId, dx, dy);
                p.Draw();

                if (_selection is PDoubleline)
                {
                    PDoubleline pd = _selection as PDoubleline;
                    if (pd.JoinStart != 0)
                    {
                        uint id = pd.JoinStart;
                        PDoubleline pj = Globals.ActiveDrawing.FindObjectById(id) as PDoubleline;
                        pj.Draw();
                    }
                    else if (pd.JoinEnd != 0)
                    {
                        uint id = pd.JoinEnd;
                        PDoubleline pj = Globals.ActiveDrawing.FindObjectById(id) as PDoubleline;
                        pj.Draw();
                    }
                }

                DrawHandles();
            }
        }

        public override void TrackCursor(ref double x, ref double y)
        {
            base.TrackCursor(ref x, ref y);

            if (_selection == null)
            {
                // this isn't supposed to happen
            }
            else if (_lastTrackDX != 0 || _lastTrackDY != 0)
            {
                if (_selection.IsTransformed)
                {
                    if (_selection.Normalize())
                    {
                        _undoCount++;
                    }
                }
                if (_selectedHandleId > 0)
                {
                    MoveVertexBy(_lastTrackDX, _lastTrackDY);

                    // [primitive].MoveHandle may have changed the handle location and selection to conform to the primitive geometry
                    // so _lastTrack[XY] and the selected index should be adjusted accordingly

                    Point h2 = _handles.GetHandlePoint(_selectedHandleId);
                    _lastTrackX = h2.X;
                    _lastTrackY = h2.Y;

                    Globals.Events.PrimitiveSelectionSizeChanged(_selection);

                    _handles.Select(_selectedHandleId);
                }
                else if (_allowDragObject && _selection != null)
                {
                    // A handle is not selected.  Drag the selection.
                    if (!_isDragging)
                    {
                        // If this is a new drag, save the current position
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.Move, _selection, _selection.Origin);
                        _undoCount++;
                        _isDragging = true;
                    }

                    _selection.MoveByDelta(_lastTrackDX, _lastTrackDY);
                    _selection.Draw();
                    _handles.Detach();
                }

                FinishUndo();
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_selectedHandleId > 0)
            {
                if (Globals.Input.ObjectSnap && _shiftKey == false)
                {
                    if (Globals.CommandProcessor is EditCommandProcessor editCommandProcessor)
                    {
                        Point h = _handles.GetHandlePoint(_selectedHandleId);
                        Point c = editCommandProcessor.SnapTo(h);
                        double dx = c.X - h.X;
                        double dy = c.Y - h.Y;

                        if (dx != 0 || dy != 0)
                        {
                            MoveHandle(_selectedHandleId, dx, dy);
                        }
                    }
                }
            }
        }

        public override void Step(double dx, double dy)
        {
            MoveVertexBy(dx, dy);
        }

        public override void MoveHandle(int handleId, double dx, double dy)
        {
            MoveVertexBy(dx, dy);
        }

        public override Primitive Selection
        {
            get => base.Selection;
            set
            {
                if (_selection != value)
                {
                    _selectedHandleId = -1;

                    if (this.CanHandlePrimitive(value))
                    {
                        _selection = value;

                        if (_selection is PDimension pdim)
                        {
                            pdim.InsertHandleMode = false;
                        }
                    }
                    else
                    {
                        _selection = null;
                    }
                    _undoCount = 0;
                }
            }
        }

        public override void DrawHandles()
        {
            _selection.ShowHandles(_handles);
        }

        public override void UndoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            base.UndoNotification(actionId, subject, predicate, predicate2);

            _isDragging = false;
        }

        public override void RedoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            base.RedoNotification(actionId, subject, predicate, predicate2);

            _isDragging = false;
        }

        public override bool CanHandlePrimitive(Primitive p)
        {
            bool can = false;

            if (p != null)
            {
                switch (p.TypeName)
                {
                    case PrimitiveType.Arc:
                    case PrimitiveType.Arc3:
                    case PrimitiveType.BSpline:
                    case PrimitiveType.Dimension:
                    case PrimitiveType.Ellipse:
                    case PrimitiveType.Image:
                    case PrimitiveType.Instance:
                    case PrimitiveType.Rectangle:
                        can = p.IsTransformed == false;
                        break;

                    case PrimitiveType.Arrow:
                    case PrimitiveType.Doubleline:
                    case PrimitiveType.Line:
                    case PrimitiveType.Polygon:
                    case PrimitiveType.Text:
                        can = true;
                        break;

                    default:
                        can = false;
                        break;
                }
            }
            return can;
        }
    }

    public class EditInsertVertexCommand : EditMoveVertexCommand
    {
        public EditInsertVertexCommand(Primitive selection, Handles handles) : base(selection, handles)
        {
            _allowDragObject = false;
        }

        public override EditSubCommand EditSubCommand
        {
            get { return EditSubCommand.InsertPoint; }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            if (_selection is PLine line && _handles.SelectedHandleID < 0)
            {
                if (line.IsTransformed)
                {
                    if (line.Normalize(true))
                    {
                        _undoCount++;
                    }
                }
                Point p = new Point(x, y);
                int segment = line.PickSegment(ref p, out double pv);
                if (segment >= 0)
                {
                    List<CPoint> cpoints = new List<CPoint>();
                    cpoints.AddRange(line.CPoints);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePoints, line, cpoints);
                    _undoCount++;

                    int handleId = segment + 2;
                    if (line is PDoubleline)
                    {
                        line.InsertHandlePoint(handleId, new CPoint(p, 3));
                    }
                    else
                    {
                        line.InsertHandlePoint(handleId, new CPoint(p, 1));
                    }
                    line.ShowHandles(_handles);
                    _handles.Deselect();
                    _selectedHandleId = -1;
                }
            }
            FinishUndo();
        }

        public override Primitive Selection
        {
            get => base.Selection;
            set
            {
                if (_selection != value)
                {
                    _selectedHandleId = -1;

                    if (this.CanHandlePrimitive(value))
                    {
                        _selection = value;

                        if (_selection is PDimension pdim)
                        {
                            pdim.InsertHandleMode = true;
                        }
                    }
                    else
                    {
                        _selection = null;
                    }
                    _undoCount = 0;
                }
            }
        }
    }

    public class EditExtendTrimCommand : EditMoveVertexCommand
    {
        public EditExtendTrimCommand(Primitive selection, Handles handles) : base(selection, handles)
        {
            _allowDragObject = false;
        }

        public override EditSubCommand EditSubCommand
        {
            get { return EditSubCommand.ExtendTrim; }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            if (_selection is PLine line && line.IsTransformed)
            {
                if (line.Normalize(true))
                {
                    _undoCount++;
                }
            }
            base.StartTracking(x, y, shift, control);

            if (_lineAnchorPoint != _lineReferencePoint)
            {
                Point constrained;
                Construct.DistancePointToLine(new Point(x, y), _lineAnchorPoint, _lineReferencePoint, out constrained);
                _lastTrackX = constrained.X;
                _lastTrackY = constrained.Y;
            }
        }

        public override void TrackCursor(ref double x, ref double y)
        {
            if (_selection != null)
            {
                if (_selection.TypeName == PrimitiveType.Line || _selection.TypeName == PrimitiveType.Polygon ||
                    _selection.TypeName == PrimitiveType.Doubleline || _selection.TypeName == PrimitiveType.Arrow)
                {
                    Point constrained;
                    Construct.DistancePointToLine(new Point(x, y), _lineAnchorPoint, _lineReferencePoint, out constrained);
                    x = constrained.X;
                    y = constrained.Y;
                }
            }

            base.TrackCursor(ref x, ref y);
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_selectedHandleId > 0)
            {
                if (Globals.Input.ObjectSnap && _shiftKey == false)
                {
                    if (Globals.CommandProcessor is EditCommandProcessor editCommandProcessor)
                    {
                        Point h = _handles.GetHandlePoint(_selectedHandleId);
                        Point c = editCommandProcessor.SnapTo(h);
                        Point constrained;
                        Construct.DistancePointToLine(c, _lineAnchorPoint, _lineReferencePoint, out constrained);

                        double dx = constrained.X - h.X;
                        double dy = constrained.Y - h.Y;

                        if (dx != 0 || dy != 0)
                        {
                            MoveHandle(_selectedHandleId, dx, dy);
                        }
                    }
                }
            }
        }

        public override bool CanHandlePrimitive(Primitive p)
        {
            bool can = false;

            if (p != null)
            {
                switch (p.TypeName)
                {
                    case PrimitiveType.Doubleline:
                    case PrimitiveType.Line:
                    case PrimitiveType.Polygon:
                    case PrimitiveType.Arrow:
                        can = true;
                        break;

                    default:
                        can = false;
                        break;
                }
            }
            return can;
        }
    }

    public class EditPropertiesCommand : EditCommand
    {
        public EditPropertiesCommand(Primitive selection, Handles handles) : base(selection, handles)
        {
        }

        public override EditSubCommand EditSubCommand
        {
            get { return EditSubCommand.Properties; }
        }

        public override void DrawHandles()
        {
            _handles.Detach();
        }

        public override bool CanHandlePrimitive(Primitive p)
        {
            return p != null;
        }
    }

    public class EditMoveSegmentCommand : EditCommand
    {
        int _selectedSegment = -1;
        bool _isDragging = false;

        public EditMoveSegmentCommand(Primitive selection, Handles handles) : base(selection, handles)
        {
        }

        public override EditSubCommand EditSubCommand
        {
            get { return EditSubCommand.MoveSegment; }
        }

        public override bool HasSelection
        {
            get
            {
                return _selection != null && _selectedSegment != -1;
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            if (_selection is PLine line)
            {
                if (line.IsTransformed)
                {
                    if (line.Normalize(true))
                    {
                        _undoCount++;
                    }
                }
                Point p = new Point(x, y);
                double pv = 0;
                _selectedSegment = line.PickSegment(ref p, out pv);

                DrawHandles();
            }

            _isDragging = false;
        }

        private void MoveSelectedSegmentBy(double dx, double dy)
        {
            if (_selection is PLine line && _selectedSegment >= 0)
            {
                if (_isDragging == false)
                {
                    line.GetSegment(_selectedSegment, out Point start, out Point end);

                    if (line.IsTransformed)
                    {
                        if (line.Normalize(true))
                        {
                            _undoCount++;
                        }
                    }
                    if (_selectedSegment == line.Points.Count)
                    {
                        if (line.TypeName == PrimitiveType.Polygon)
                        {
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveVertex, _selection, _selectedSegment + 1, start);
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveVertex, _selection, 1, end);
                            _undoCount += 2;
                        }
                    }
                    else
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveVertex, _selection, _selectedSegment + 1, start);
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveVertex, _selection, _selectedSegment + 2, end);
                        _undoCount += 2;
                    }

                    _isDragging = true;
                }

                line.MoveSegmentBy(_selectedSegment, dx, dy);
                line.Draw();

                DrawHandles();

                FinishUndo();
            }
        }

        public override void TrackCursor(ref double x, ref double y)
        {
            base.TrackCursor(ref x, ref y);

            if (_lastTrackDX != 0 || _lastTrackDY != 0)
            {
                MoveSelectedSegmentBy(_lastTrackDX, _lastTrackDY);
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_selection is PLine line)
            {
                if (line.IsTransformed)
                {
                    if (line.Normalize(true))
                    {
                        _undoCount++;
                    }
                }
                if (_selectedHandleId < 0)
                {
                    Point p = new Point(x, y);
                    double pv = 0;
                    _selectedSegment = line.PickSegment(ref p, out pv);
                }
            }

            DrawHandles();

            _isDragging = false;
        }

        public override void Step(double dx, double dy)
        {
            MoveSelectedSegmentBy(dx, dy);
        }

        public override void MoveHandle(int handleId, double dx, double dy)
        {
            MoveSelectedSegmentBy(dx, dy);
        }

        public override void DrawHandles()
        {
            if (_selection != null)
            {
                if (_handles.Count > 0 && _selectedHandleId == 4000 || _selectedHandleId == 4001)
                {
                    if (_selectedSegment >= 0)
                    {
                        if (_selection is PLine line)
                        {
                            line.GetSegment(_selectedSegment, out Point start, out Point end);

                            _handles.MoveHandleTo(4000, start.X, start.Y);
                            _handles.MoveHandleTo(4001, end.X, end.Y);
                        }
                    }
                    else
                    {
                        _handles.Detach();
                    }
                }
                else
                {
                    _handles.Detach();

                    if (_selectedSegment >= 0)
                    {
                        if (_selection is PLine line)
                        {
                            line.GetSegment(_selectedSegment, out Point start, out Point end);

                            _handles.AddHandle(4000, start.X, start.Y, HandleType.Circle);
                            _handles.AddHandle(4001, end.X, end.Y, HandleType.Circle);
                            _handles.Draw();
                        }
                    }
                }
            }
        }

        public override void UndoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            base.UndoNotification(actionId, subject, predicate, predicate2);

            _isDragging = false;
        }

        public override void RedoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            base.RedoNotification(actionId, subject, predicate, predicate2);

            _isDragging = false;
        }

        public override bool CanHandlePrimitive(Primitive p)
        {
            return (p is PRectangle == false) && p is PLine;
        }
    }

    public class EditGapCommand : EditCommand
    {
        private Point _gapStart;
        private Point _gapEnd;
        private int _gapStartSegment = -1;
        private int _gapEndSegment = -1;
        private double _gapStartValue = 0;
        private double _gapEndValue = 0;

        public EditGapCommand(Primitive selection, Handles handles) : base(selection, handles)
        {
        }

        public override EditSubCommand EditSubCommand
        {
            get { return EditSubCommand.Gap; }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            if (_selection is PArc arc)
            {
                Point p = new Point(x, y);

                if (arc.Box.Contains(p))
                {
                    double tol = Globals.DrawingCanvas.DisplayToPaper(Globals.hitTolerance);

                    if ((Math.Abs(Construct.Distance(p, arc.Origin) - arc.Radius)) < tol)
                    {
                        if (_gapStartSegment < 0)
                        {
                            _gapStart = Construct.PointAlongLine(arc.Origin, p, arc.Radius);
                            _gapStartSegment = 0;

                            _handles.AddHandle(3000, _gapStart.X, _gapStart.Y, HandleType.Diamond);
                            _handles.Draw();
                        }
                        else if (_gapEndSegment < 0)
                        {
                            _gapEnd = Construct.PointAlongLine(arc.Origin, p, arc.Radius);
                            _gapEndSegment = 0;

                            _handles.AddHandle(3001, _gapEnd.X, _gapEnd.Y, HandleType.Diamond);
                            _handles.Draw();
                        }
                    }
                }
            }
            else if (_selection is PArc3 arc3)
            {
                Point c = arc3.Center;
                double radius = Construct.Distance(c, arc3.Origin);

                Point p = new Point(x, y);

                if (arc3.Box.Contains(p))
                {
                    double tol = Globals.DrawingCanvas.DisplayToPaper(Globals.hitTolerance);

                    if ((Math.Abs(Construct.Distance(p, c) - radius)) < tol)
                    {
                        if (_gapStartSegment < 0)
                        {
                            _gapStart = Construct.PointAlongLine(c, p, radius);
                            _gapStartSegment = 0;

                            _handles.AddHandle(3000, _gapStart.X, _gapStart.Y, HandleType.Diamond);
                            _handles.Draw();
                        }
                        else if (_gapEndSegment < 0)
                        {
                            _gapEnd = Construct.PointAlongLine(c, p, radius);
                            _gapEndSegment = 0;

                            _handles.AddHandle(3001, _gapEnd.X, _gapEnd.Y, HandleType.Diamond);
                            _handles.Draw();
                        }
                    }
                    //else
                    //{
                    //    Deselect();
                    //}
                }
                //else
                //{
                //    Deselect();
                //}
            }
            else if (_selection is PLine line)
            {
                Point p = new Point(x, y);
                double pv = 0;
                int segment = line.PickSegment(ref p, out pv);
                if (segment >= 0)
                {
                    if (_gapStartSegment < 0)
                    {
                        _gapStart = p;
                        _gapStartSegment = segment;
                        _gapStartValue = pv;

                        _handles.AddHandle(3000, p.X, p.Y, HandleType.Diamond);
                        _handles.Draw();
                    }
                    else if (_gapEndSegment < 0)
                    {
                        _gapEnd = p;
                        _gapEndSegment = segment;
                        _gapEndValue = pv;

                        _handles.AddHandle(3001, p.X, p.Y, HandleType.Diamond);
                        _handles.Draw();
                    }
                }
                //else
                //{
                //    Deselect();
                //}
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_selection != null && _gapEndSegment >= 0)
            {
                if (_selection.IsTransformed)
                {
                    if (_selection.Normalize())
                    {
                        _undoCount++;
                    }
                }
                if (_selection is PDoubleline dbline)
                {
                    List<CPoint> cpoints = new List<CPoint>();
                    cpoints.AddRange(dbline.CPoints);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePoints, dbline, cpoints);
                    _undoCount++;

                    if (_gapStartSegment > _gapEndSegment || (_gapStartSegment == _gapEndSegment && _gapStartValue > _gapEndValue))
                    {
                        dbline.Gap(_gapEndSegment, new CPoint(_gapEnd, 3), _gapStartSegment, new CPoint(_gapStart, 0));
                    }
                    else
                    {
                        dbline.Gap(_gapStartSegment, new CPoint(_gapStart, 3), _gapEndSegment, new CPoint(_gapEnd, 0));
                    }
                }
                else if (_selection is PArc || _selection is PArc3)
                {
                    if (_selection is PArc3 a3)
                    {
                        // If the selection is a 3-point arc, convert it to a conventional arc
                        Selection = new PArc(a3);
                        _selection.AddToContainer(Globals.ActiveDrawing);

                        Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, a3);
                        Globals.ActiveDrawing.DeletePrimitive(a3);
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, _selection);
                        _undoCount += 2;
                    }

                    PArc arc = _selection as PArc;
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetStartAngle, arc, arc.StartAngle);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetIncludedAngle, arc, arc.IncludedAngle);
                    _undoCount += 2;

                    PArc newArc = PrimitiveUtilities.GapArc(arc, _gapStart, _gapEnd);
                    if (newArc == null)
                    {
                    }
                    else
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, newArc);
                        ++_undoCount;
                    }
                }
                else if (_selection is PLine line)
                {
                    List<CPoint> cpoints = new List<CPoint>();
                    cpoints.AddRange(line.CPoints);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePoints, line, cpoints);
                    ++_undoCount;

                    if (_gapStartSegment > _gapEndSegment || (_gapStartSegment == _gapEndSegment && _gapStartValue > _gapEndValue))
                    {
                        line.Gap(_gapEndSegment, new CPoint(_gapEnd, 1), _gapStartSegment, new CPoint(_gapStart, 0));
                    }
                    else
                    {
                        line.Gap(_gapStartSegment, new CPoint(_gapStart, 1), _gapEndSegment, new CPoint(_gapEnd, 0));
                    }
                }

                if (_selection is PLine)
                {
                    // Don't deselect line now that line gaps are available
                    _handles.Detach();
                    _selectedHandleId = -1;

                    _gapStartSegment = -1;
                    _gapEndSegment = -1;
                }
            }
            FinishUndo();
        }

        public override void DrawHandles()
        {
            _handles.Detach();
        }

        public override bool CanHandlePrimitive(Primitive p)
        {
            bool can = false;

            if (p != null)
            {
                switch (p.TypeName)
                {
                    case PrimitiveType.Arc:
                    case PrimitiveType.Arc3:
                        can = p.IsTransformed == false;
                        break;

                    case PrimitiveType.Doubleline:
                    case PrimitiveType.Line:
                        can = true;
                        break;

                    default:
                        can = false;
                        break;
                }
            }
            return can;
        }
    }

    public class EditOffsetMoveCommand : EditCommand
    {
        int _offsetSegment = -1;
        protected bool _isCopy = false;

        //Point _currentPoint = new Point();

        public EditOffsetMoveCommand(Primitive selection, Handles handles) : base(selection, handles)
        {
        }

        public override EditSubCommand EditSubCommand
        {
            get { return EditSubCommand.OffsetMove; }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            //_currentPoint = new Point(x, y);

            if (_selection is PLine line)
            {
                Point p = new Point(x, y);
                double pv = 0;
                _offsetSegment = line.PickSegment(ref p, out pv);
                if (_offsetSegment >= 0)
                {
                    if (control || _isCopy)
                    {
                        Selection = CopySelection();
                    }
                    else
                    {
                        List<CPoint> cpoints = new List<CPoint>();
                        cpoints.AddRange(line.CPoints);

                        Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePoints, line, cpoints);
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.Move, _selection, _selection.Origin);
                        _undoCount += 2;
                    }
                }
            }
            FinishUndo();
        }

        public override void TrackCursor(ref double x, ref double y)
        {
            base.TrackCursor(ref x, ref y);

            if (_lastTrackDX != 0 || _lastTrackDY != 0)
            {
                if (_selection is PLine line)
                {
                    Point p = new Point(x, y);
                    Point pA = new Point();
                    Point pB = new Point();
                    if (_offsetSegment < 0)
                    {
                        _offsetSegment = line.PickSegment(ref p, out double pv);
                    }

                    if (_offsetSegment >= 0)
                    {
                        if (_offsetSegment < line.Points.Count)
                        {
                            pA = _offsetSegment == 0 ? new Point() : line.Points[_offsetSegment - 1];
                            pB = line.Points[_offsetSegment];
                        }
                        else if (_selection.TypeName == PrimitiveType.Polygon)
                        {
                            pA = new Point();
                            pB = line.Points[line.Points.Count - 1];
                        }

                        pA.X += line.Origin.X;
                        pA.Y += line.Origin.Y;
                        pB.X += line.Origin.X;
                        pB.Y += line.Origin.Y;

                        double distance = Construct.DistancePointToLine(p, pA, pB);

                        if (Construct.WhichSide(pA, pB, p) < 0)
                        {
                            distance = -distance;
                        }

                        line.MoveParallel(distance);
                    }
                }
            }
        }

        public override void DrawHandles()
        {
            _handles.Detach();
        }

        public override bool CanHandlePrimitive(Primitive p)
        {
            bool can = false;

            if (p != null)
            {
                switch (p.TypeName)
                {
                    case PrimitiveType.Doubleline:
                    case PrimitiveType.Line:
                    case PrimitiveType.Polygon:
                        can = true;
                        break;

                    default:
                        can = false;
                        break;
                }
            }
            return can;
        }

        private Primitive CopySelection()
        {
            Primitive copy = null;

            if (_selection != null)
            {
                copy = _selection.Clone();
                copy.ZIndex = Globals.ActiveDrawing.MaxZIndex;
                copy.AddToContainer(Globals.ActiveDrawing);

                Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, copy);
                _undoCount++;

                if (Globals.CommandProcessor is EditCommandProcessor editCommandProcessor)
                {
                    editCommandProcessor.SelectSingleObject(copy);
                }
            }

            FinishUndo();

            return copy;
        }
    }

    public class EditOffsetCopyCommand : EditOffsetMoveCommand
    {
        public EditOffsetCopyCommand(Primitive selection, Handles handles) : base(selection, handles)
        {
            _isCopy = true;
        }

        public override EditSubCommand EditSubCommand
        {
            get { return EditSubCommand.OffsetCopy; }
        }
    }
}
