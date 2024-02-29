using Cirros.Actions;
using Cirros.Core;
using Cirros.Display;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Commands
{
    public abstract class SelectorBase : CommandProcessor
    {
        protected InputMode _inputMode = InputMode.Select;

        protected double _trackLeft = 0;
        protected double _trackTop = 0;
        protected double _trackWidth = 0;
        protected double _trackHeight = 0;

        protected double _lastTrackX = 0;
        protected double _lastTrackY = 0;

        protected Handles _handles = new Handles();

        protected bool _shiftSelectToAddToSelection = true;

        bool _ignoreSelection = false;

        protected Point _origin = new Point(0, 0);

        protected List<Primitive> _selectedElements = new List<Primitive>();

        protected int _selectedHandleId = -1;

        public SelectorBase()
        {
            ShowConstructHandles = false;
        }

        public override InputMode InputMode
        {
            get
            {
                return _inputMode;
            }
        }

        public Point Origin
        {
            get
            {
                return _origin;
            }
        }

        public override Point Anchor
        {
            get
            {
                return Globals.Input.CursorLocation;
            }
        }

        protected override CursorType cursorType
        {
            get
            {
                return CursorType.Hand;
            }
        }

        public List<Primitive> Selection
        {
            get
            {
                return _selectedElements;
            }
        }

        public override void CanvasScaleChanged()
        {
            _handles.Draw();
            base.CanvasScaleChanged();
        }

        abstract protected void SelectAll();

        protected virtual void AddToSelectedElements(Primitive p)
        {
            bool fire = _selectedElements.Count <= 1;

            _selectedElements.Add(p);
            //p.ConstructEnabled = false;
            p.IsDynamic = true;
            p.Highlight(true);

            if (fire && _selectedElements.Count > 0)
            {
                Globals.Events.PrimitiveSelectionChanged(_selectedElements);
            }
        }

        protected virtual void RemoveFromSelectedElements(Primitive p)
        {
            _selectedElements.Remove(p);
            p.ConstructEnabled = true;
            p.IsDynamic = false;
            p.Highlight(false);
        }

        public virtual void SetSelectionOrigin(Point p)
        {
            _origin = p;
        }

        public virtual void ClearSelection()
        {
            foreach (Primitive p in _selectedElements)
            {
                p.IsDynamic = false;
                p.Highlight(false);
                p.ConstructEnabled = true;
            }

            _selectedElements.Clear();

            _handles.Detach();

            _trackLeft = 0;
            _trackTop = 0;
            _trackWidth = 0;
            _trackHeight = 0;
        }

        protected virtual void Deselect()
        {
            if (_selectedElements.Count > 0)
            {
                _handles.Detach();

                ClearSelection();

                Globals.Events.PrimitiveSelectionChanged(null);
            }

            ShowConstructHandles = false;
        }

        protected void EnableSelectionConstructionPoints(bool enable)
        {
            foreach (Primitive p in _selectedElements)
            {
                p.ConstructEnabled = enable;
            }
            ResetConstructHandles();
        }

        protected virtual void startTrackingSelection(bool control)
        {
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            // When in select mode and a selection is active, 
            //   if the new click reselects the current selection, move or copy it
            //   if the shift key is down, add to the current selection
            //   otherwise, mouse down clears the selection

            _handles.Deselect();

            base.StartTracking(x, y, shift, control);

            _first = _start;
            _start = new Point(x, y);

            if (_selectedElements.Count > 0)
            {
                // Selection exists

                if (PickSelection(x, y, Globals.hitTolerance))
                {
                    if (shift && _selectedHandleId < 0)
                    {
                        // Shift-selecting a selected object deselects it
                        Primitive p = Globals.ActiveDrawing.Pick(x, y, true);
                        if (p != null)
                        {
                            RemoveFromSelectedElements(p);
                        }

                        if (_selectedElements.Count == 0)
                        {
                            _ignoreSelection = true;
                            _handles.Detach();
                        }
                    }

                    startTrackingSelection(control);

                    if (_selectedHandleId >= 0)
                    {
                        Point p = _handles.GetHandlePoint(_selectedHandleId);
                        _handles.MoveHandle(_selectedHandleId, x - p.X, y - p.Y, shift);
                    }

                    _rubberBand.State = 2;
                }
                else if (shift || _shiftSelectToAddToSelection == false)
                {
                    Primitive p = Globals.ActiveDrawing.Pick(x, y, true);
                    if (p != null)
                    {
                        _handles.Detach();
                        AddToSelectedElements(p);

                        startTrackingSelection(control);
                        _rubberBand.State = 2;
                    }
                }
                else
                {
                    Deselect();
                }
            }

            if (_selectedElements.Count > 0)
            {
                //_rubberBand = new RubberBandNone();
                _lastTrackX = x;
                _lastTrackY = y;
                
                _rubberBand.State = 2;
            }
            else
            {
                if ((_rubberBand is RubberBandRectangle) == false)
                {
                    _rubberBand = new RubberBandRectangle();
                }

                Color fillColor = _rubberBand.Color;
                fillColor.A = 25;
                ((RubberBandRectangle)_rubberBand).FillColor = fillColor;

                _rubberBand.StartTracking(x, y);
                _rubberBand.State = 1;

                ShowConstructHandles = false;
            }
        }

        public override void TrackCursor(double x, double y)
        {
            base.TrackCursor(x, y);

            if (_rubberBand.State > 0)
            {
                _trackLeft = Math.Min(_first.X, x);
                _trackTop = Math.Min(_first.Y, y);
                _trackWidth = Math.Abs(_first.X - x);
                _trackHeight = Math.Abs(_first.Y - y);
            }

            if (_rubberBand.State == 2)
            {
                if (_selectedElements.Count > 0)
                {
                    this.Move(x - _lastTrackX, y - _lastTrackY);

                    _lastTrackX = x;
                    _lastTrackY = y;

                    //Globals.Events.CoordinateDisplay(new Point(x, y)); 
                }
            }
        }

        public override void EndTracking(double x, double y)
        {
            // Selection occurs on mouse up if there is no current selection;
            //   if single click, select a single item;
            //   if drag select, select objects contained in the rectangle

            base.EndTracking(x, y);

            if (_ignoreSelection)
            {
                _ignoreSelection = false;
            }
            else if (_selectedElements.Count == 0)
            {
                if (Construct.Distance(new Point(x, y), _start) < 0.01)
                {
                    // Single click - single select
                    Primitive p = Globals.ActiveDrawing.Pick(x, y, true);
                    if (p != null)
                    {
                        AddToSelectedElements(p);
                        SetSelectionOrigin(p.Origin);
                    }
                }
                else
                {
                    // Drag select - select rectangle
                    Rect r = new Rect(_trackLeft, _trackTop, _trackWidth, _trackHeight);

                    Pick(r);
                }
            }

            _rubberBand.EndTracking();

            if (_selectedElements.Count > 0)
            {
                _rubberBand = new RubberBandBasic();
                _rubberBand.State = 3;

                //ShowConstructHandles = true;
            }
            else
            {
                _rubberBand.State = 0;
            }

            ShowHandles();
        }

        protected virtual void Move(double dx, double dy)
        {
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);
            Deselect();
            base.Finish();
        }

        protected virtual void Pick(Rect r)
        {
            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                if (Globals.LayerTable[p.LayerId].Visible && p.ContainedBy(r))
                {
                    if (_selectedElements.Count == 0)
                    {
                        SetSelectionOrigin(p.Origin);
                    }

                    AddToSelectedElements(p);
                }
            }
        }

        protected virtual bool PickSelection(double x, double y, double tolerance)
        {
            if (_selectedElements.Count > 0)
            {
                _selectedHandleId = _handles.Hit(x, y);

                if (_selectedHandleId > 0)
                {
                    return true;
                }
                else
                {
                    Point paper = new Point(x, y);
#if true
                    uint sid = Globals.DrawingCanvas.VectorListControl.PickSegment(paper);
                    if (sid > 0)
                    {
                        foreach (Primitive p in _selectedElements)
                        {
                            if (p.Id == sid)
                            {
                                return true;
                            }
                        }
                    }
#else
                    foreach (Primitive p in _selectedElements)
                    {
                        double pd;

                        if (p.Pick(paper, out pd))
                        {
                            return true;
                        }
                    }
#endif
                }
            }

            return false;
        }

        public abstract void ShowHandles();
        public abstract void MoveHandle(int id, double dx, double dy, bool shift);
    }

    public enum SelectorActionID
    {
        AddToSelection,
        RemoveFromSelection
    }
}
