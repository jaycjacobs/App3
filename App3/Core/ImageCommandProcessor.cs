using Cirros.Actions;
using Cirros.Commands;
using Cirros.Display;
using Cirros.Primitives;
using System.Collections.Generic;
using Windows.Foundation;

namespace Cirros.Core
{
    class ImageCommandProcessor : CommandProcessor
    {
        PImage _image = null;

        Handles _handles = new Handles();
        int _selectedHandleId = -1;

        double _lastTrackX = 0;
        double _lastTrackY = 0;

        bool _isTracking = false;
        bool _isDragging = false;

        public ImageCommandProcessor()
        {
        }

        protected override CursorType cursorType
        {
            get
            {
                return _image == null ? CursorType.Arrow : CursorType.Hand;
            }
        }

        public override void Start()
        {
            base.Start();

            //if (_image == null)
            //{
            //    // get a new image
            //    Globals.Events.EditImage(null, null, null);
            //}
        }

        const uint cMaxSize = 800;

        public override bool EnableCommand(object o)
        {
            if (o is string s && s == "C_GetImage")
            {
                // The Get Image button shouldn't be enabled until this command is fully running
                return true;
            }
            return base.EnableCommand(o);
        }

        public override void Invoke(object o, object parameter)
        {
            if (o is string)

            {
                switch ((string)o)
                {
                    case "A_InsertImage":
                        if (parameter is Dictionary<string, object>)
                        {
                            Dictionary<string, object> dictionary = parameter as Dictionary<string, object>;

                            Point center = new Point(
                                (Globals.View.CurrentWindow.Left + Globals.View.CurrentWindow.Right) / 2,
                                (Globals.View.CurrentWindow.Top + Globals.View.CurrentWindow.Bottom) / 2
                                );

                            PImage pi = new PImage(new Point());
                            pi.Opacity = Globals.ImageOpacity;

                            PrimitiveUtilities.UpdatePImageFromDictionary(pi, dictionary, center);

                            _image = pi;
                            _image.AddToContainer(Globals.ActiveDrawing);

                            Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, _image);

                            UpdateHandles();

                            _image.ConstructEnabled = false;
                            ShowConstructHandles = true;
                            
                            Globals.Input.SelectCursor(cursorType);
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public override void UndoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            switch (actionId)
            {
                case ActionID.Move:
                case ActionID.MoveVertex:
                    if (subject == _image)
                    {
                        UpdateHandles();
                    }
                    break;

                case ActionID.DeletePrimitive:
                    Deselect();
                    break;
            }
        }

        public override void RedoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            switch (actionId)
            {
                case ActionID.Move:
                case ActionID.MoveVertex:
                    if (subject == _image)
                    {
                        UpdateHandles();
                    }
                    break;

                case ActionID.RestorePrimitive:
                    if (subject is PImage)
                    {
                        _image = subject as PImage;
                        _image.ConstructEnabled = false;
                        ShowConstructHandles = true;
                        UpdateHandles();
                        Globals.Input.SelectCursor(cursorType);
                    }
                    break;
            }
        }

        private void UpdateHandles()
        {
            if (_image != null)
            {
                _image.ShowHandles(_handles);
                _handles.Draw();
            }
        }

        private void Deselect()
        {
            _handles.Clear();

            if (_image != null)
            {
                _image.ConstructEnabled = true;
                _image = null;
            }

            ShowConstructHandles = false;
            Globals.Input.SelectCursor(cursorType);
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            if (_image == null)
            {
#if true
                Globals.Events.ShowAlert("NoImageSelected");
#else
                // get a new image
                Globals.Events.EditImage(null, null, null);
#endif
            }
            else
            {
                _image.PreserveAspect = shift;

                _selectedHandleId = _handles.Hit(x, y);

                if (_selectedHandleId >= 0)
                {
                    // move the handle
                    Point handlePoint = _handles.GetHandlePoint(_selectedHandleId);
                    Point xy = new Point(handlePoint.X, handlePoint.Y);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveVertex, _image, _selectedHandleId, xy);
                }
                else if (_image.Box.Contains(new Point(x, y)))
                {
                    // move the image
                    _handles.Deselect();
                }
                else
                {
                    Deselect();
                }

                _lastTrackX = x;
                _lastTrackY = y;

                _isTracking = true;
            }
        }

        public override void TrackCursor(double x, double y)
        {
            base.TrackCursor(x, y);

            if (_isTracking)
            {
                double dx = x - _lastTrackX;
                double dy = y - _lastTrackY;

                if (dx != 0 || dy != 0)
                {
                    if (_selectedHandleId > 0)
                    {
                        _handles.MoveHandle(_selectedHandleId, dx, dy);

                        // [primitive].MoveHandle may have changed the handle location and selection to conform to the primitive geometry
                        // so _lastTrack[XY] and the selected index should be adjusted accordingly

                        Point h2 = _handles.GetHandlePoint(_selectedHandleId);
                        _lastTrackX = h2.X;
                        _lastTrackY = h2.Y;

                        Globals.Events.PrimitiveSelectionSizeChanged(null);

                        _handles.Select(_selectedHandleId);
                    }
                    else if (_image != null)
                    {
                        // A handle is not selected.  Drag the selection.
                        if (!_isDragging)
                        {
                            // If this is a new drag, save the current position
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.Move, _image, _image.Origin);
                            _isDragging = true;
                        }

                        _image.MoveByDelta(dx, dy);
                        _image.Draw();
                        _handles.Detach();

                        _lastTrackX = x;
                        _lastTrackY = y;
                    }
                }
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_isDragging)
            {
                // Finish up the selection move.
                if (Globals.Input.ObjectSnap && _shiftKey == false)
                {
                    if (_constructHandles.SelectedHandleID >= 0)
                    {
                        double dx = _constructHandles.SelectedHandle.Location.X - _lastTrackX;
                        double dy = _constructHandles.SelectedHandle.Location.Y - _lastTrackY;
                        _image.MoveByDelta(dx, dy);
                        _image.Draw();
                    }
                    else if (Globals.DrawingTools.ActiveTrianglePoint(out Point trianglePoint))
                    {
                        double dx = trianglePoint.X - _lastTrackX;
                        double dy = trianglePoint.Y - _lastTrackY;
                        _image.MoveByDelta(dx, dy);
                        _image.Draw();
                    }
                }

                _isDragging = false;

                UpdateHandles();
            }
            else if (_selectedHandleId > 0)
            {
                // Finish up the handle move.

                if (Globals.Input.ObjectSnap && _shiftKey == false)
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
                        _handles.Draw();
                    }
                    else if (Globals.DrawingTools.ActiveTrianglePoint(out Point trianglePoint))
                    {
                        Point c = trianglePoint;
                        Point h = _handles.GetHandlePoint(_selectedHandleId);
                        double dx = c.X - h.X;
                        double dy = c.Y - h.Y;

                        _handles.MoveHandle(_selectedHandleId, dx, dy);
                        _handles.Draw();
                    }
                }
            }

            _isTracking = false;
        }

        public override void Finish()
        {
            Deselect();

            _handles.Clear();

            base.Finish();
        }
    }
}
