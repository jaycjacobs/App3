using Cirros.Actions;
using Cirros.Core;
using Cirros.Primitives;
using Cirros.Utility;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Commands
{
    public class InsertCommandProcessor : CommandProcessor
    {
        protected PInstance _dragInstance = null;
        protected PInstance _currentInstance = null;
        protected PInstance _attributePanelInstance = null;

        string _defaultGroupName = null;
        bool _pickGroup = false;

        Group _group = null;
        double _groupScale = 1;

        //Point _insertOffset = new Point(0, 0);

        public InsertCommandProcessor(string groupName = null)
        {
            _pickGroup = groupName == null;

            _defaultGroupName = groupName;
            _groupScale = Globals.GroupScale;

            _type = CommandType.insertsymbol;
        }

        public override void Start()
        {
            base.Start();

            if (Globals.UIVersion > 0)
            {
                if (Globals.LayerTable.ContainsKey(Globals.ActiveInstanceLayerId))
                {
                    Globals.LayerId = Globals.ActiveInstanceLayerId;
                }
                else
                {
                    Globals.LayerId = Globals.ActiveLayerId;
                }
            }

            if (string.IsNullOrEmpty(_defaultGroupName))
            {
                // Pick an object
                _group = null;
            }
            else
            {
                SelectGroupByName(_defaultGroupName);

                Point p = Globals.Input.CursorLocation;
                CreateDragInstance(p.X, p.Y);
            }
        }

        protected void SelectGroupByName(string groupName)
        {
            _group = Globals.ActiveDrawing.GetGroup(groupName);
            //_insertOffset = new Point(0, 0);

            //if (_group != null && _group.InsertLocation != GroupInsertLocation.None)
            //{
            //    Point exit = Globals.ActiveDrawing.ModelToPaperDelta(_group.Exit);

            //    if (_group.Entry.X == 0 && _group.Entry.Y == 0)
            //    {
            //        if (_group.InsertLocation == GroupInsertLocation.Center)
            //        {
            //            _insertOffset.X = -exit.X / 2;
            //            _insertOffset.Y = -exit.Y / 2;
            //        }
            //        else if (_group.InsertLocation == GroupInsertLocation.Exit)
            //        {
            //            _insertOffset.X = -exit.X;
            //            _insertOffset.Y = -exit.Y;
            //        }
            //    }
            //    else
            //    {
            //        _insertOffset = new Point();
            //    }
            //}

            Globals.Input.SelectCursor(cursorType);
        }

        protected override CursorType cursorType
        {
            get
            {
                return _group == null ? CursorType.Hand : CursorType.Draw;
            }
        }

        public override bool EnableCommand(object o)
        {
            bool enable = false;
            if (o is string)
            {
                switch (o as string)
                {
                    case "A_Flip":
                        enable = _currentInstance != null;
                        break;

                    case "A_Done":
                        enable = _dragInstance != null;
                        break;
                }
            }

            return enable;
        }

        public override void Invoke(object o, object parameter)
        {
            if (o is string)
            {
                switch ((string)o)
                {
                    case "A_Flip":
                        Flip();
                        break;

                    case "A_Done":
                        Finish();
                        break;
                }
            }
        }


        public override void UndoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            base.UndoNotification(actionId, subject, predicate, predicate2);
            _currentInstance = null;
        }

        Primitive _pickedObject = null;

        protected void CreateDragInstance(double x, double y)
        {
            if (_dragInstance == null && _attributePanelInstance == null)
            {
                _dragInstance = CreateInstance(new Point(x, y));
                if (_dragInstance != null)
                {
                    _dragInstance.ZIndex = Globals.ActiveDrawing.MinZIndex;
                    _dragInstance.AddToContainer(Globals.ActiveDrawing);
                    _dragInstance.Opacity = .4;
                    _dragInstance.ConstructEnabled = false;

                    //_dragInstance.MoveTo(x + _insertOffset.X * _group.PreferredScale, y + _insertOffset.Y * _group.PreferredScale);
                }
            }
        }

        public override void PointerEnteredDrawingArea()
        {
            if (_dragInstance != null)
            {
                _dragInstance.IsVisible = true;
            }
            base.PointerEnteredDrawingArea();
        }

        public override void PointerLeftDrawingArea()
        {
            base.PointerLeftDrawingArea();
            if (_dragInstance != null)
            {
                _dragInstance.IsVisible = false;
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            if (_group == null)
            {
                _pickedObject = Globals.ActiveDrawing.Pick(x, y, true);
            }
            else if (_dragInstance == null && _attributePanelInstance == null)
            {
                CreateDragInstance(x, y);
            }
            else
            {
                base.StartTracking(x, y, shift, control);
            }
        }

        public override void TrackCursor(double x, double y)
        {
            if (_group == null)
            {
                // Pick
            }
            else
            {
                base.TrackCursor(x, y);

                if (_rubberBand.State == 0)
                {
                    _rubberBand.State = 1;
                }
                else if (_dragInstance != null)
                {
                    //_dragInstance.MoveTo(x + _insertOffset.X * _group.PreferredScale, y + _insertOffset.Y * _group.PreferredScale);
                    _dragInstance.MoveTo(x, y);
                }
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_group != null)
            {
                PInstance newInstance = null;

                if (Globals.Input.ObjectSnap && _shiftKey == false)
                {
                    if (_constructHandles.SelectedHandleID >= 0)
                    {
                        x = _constructHandles.SelectedHandle.Location.X;
                        y = _constructHandles.SelectedHandle.Location.Y;
                    }
                    else if (Globals.DrawingTools.ActiveTrianglePoint(out Point trianglePoint))
                    {
                        x = trianglePoint.X;
                        y = trianglePoint.Y;
                    }
                }

                if (_group.InsertLocation != GroupInsertLocation.None)
                {
                    Primitive p = Globals.ActiveDrawing.Pick(x, y, true);

                    //newInstance = CreateInstance(new Point(x + _insertOffset.X * _group.PreferredScale, y + _insertOffset.Y * _group.PreferredScale));
                    newInstance = CreateInstance(new Point(x, y));
                    if (newInstance != null)
                    {
                        addPrimitive(newInstance);
                        _currentInstance = newInstance;

                        Globals.Events.PrimitiveCreated(newInstance);

                        if (p is PLine)
                        {
                            Point loc = new Point(x, y);
                            Point porigin = p.Origin;
                            double pv = 0;

                            int segment = ((PLine)p).PickSegment(ref loc, out pv);
                            if (segment >= 0)
                            {
                                Point s, e;
                                ((PLine)p).GetSegment(segment, out s, out e);

                                double va = _group.Exit == _group.Entry ? 0 : Construct.Angle(_group.Entry, _group.Exit);
                                double angle = Construct.Angle(s, e) + va;
                                double degrees = angle * Construct.cRadiansToDegrees;
                                newInstance.Rotate(newInstance.Origin, degrees);

                                double scale = 1;
                                if (Globals.UIVersion == 0)
                                {
                                    scale = _group.PreferredScale;
                                }
                                else
                                {
                                    scale = _groupScale;
                                }

                                Matrix mtx = CGeometry.ScaleMatrix(scale, scale);
                                mtx = CGeometry.RotateMatrixAboutZ(mtx, angle);

                                Point sp, ep;

                                //if (_group.Entry.X == 0 && _group.Entry.Y == 0)
                                //{
                                //    // old method using InsertLocation, Origin & Exit
                                //    Point exit = Globals.ActiveDrawing.ModelToPaperDelta(_group.Exit);
                                //    Point vGap = mtx.Transform(exit);
                                //    Point vOff = mtx.Transform(new Point(-_insertOffset.X, -_insertOffset.Y));

                                //    sp = new Point(loc.X - vOff.X, loc.Y - vOff.Y);
                                //    ep = new Point(sp.X + vGap.X, sp.Y + vGap.Y);

                                //    newInstance.MoveTo(sp.X, sp.Y);
                                //}
                                //else
                                {
                                    // new method using Origin, Entry & Exit
                                    Point entry = Globals.ActiveDrawing.ModelToPaperDelta(_group.Entry);
                                    Point exit = Globals.ActiveDrawing.ModelToPaperDelta(_group.Exit);
                                    Point vGap = mtx.Transform(new Point(exit.X - entry.X, exit.Y - entry.Y));
                                    Point vGS = mtx.Transform(new Point(-entry.X, -entry.Y));

                                    sp = new Point(loc.X - vGS.X, loc.Y - vGS.Y);
                                    ep = new Point(sp.X + vGap.X, sp.Y + vGap.Y);

                                    newInstance.MoveTo(loc.X, loc.Y);
                                }

                                double pvs = 0;
                                double pve = 0;

                                if (_group.Exit == _group.Entry)
                                {
                                    // no gap
                                }
                                else if (((PLine)p).PickSegment(ref sp, out pvs) == segment && ((PLine)p).PickSegment(ref ep, out pve) == segment)
                                {
                                    List<CPoint> cpoints = new List<CPoint>();
                                    cpoints.AddRange(((PLine)p).CPoints);
                                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePoints, p, cpoints);

                                    if (pvs < pve)
                                    {
                                        ((PLine)p).Gap(segment, new CPoint(sp, 1), segment, new CPoint(ep, 0));
                                    }
                                    else
                                    {
                                        ((PLine)p).Gap(segment, new CPoint(ep, 1), segment, new CPoint(sp, 0));
                                    }

                                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, 2);
                                }
                            }

                            if (p is PDoubleline)
                            {
                                _currentInstance.WallSize = ((PDoubleline)p).Width;
                                _currentInstance.Draw();
                            }
                        }
                    }
                }
                else
                {
                    // Don't insert, just create an instance
                    newInstance = CreateInstance(new Point(x, y));
                    if (newInstance != null)
                    {
                        addPrimitive(newInstance);

                        Globals.Events.PrimitiveCreated(newInstance);
                    }
                }

                if (newInstance != null)
                {
                    ShowAttributePanel(newInstance);
                }
            }
            else if (_pickedObject != null)
            {
                if (_pickedObject is PInstance)
                {
                    PInstance pi = _pickedObject as PInstance;
                    _defaultGroupName = pi.GroupName;
                    SelectGroupByName(_defaultGroupName);

                    _groupScale = Utilities.GetScaleFromMatrix(pi.Matrix);
                }
                else
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, _pickedObject);

                    PInstance pi = Globals.ActiveDrawing.CreateGroupFromSinglePrimitive(_pickedObject);

                    Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, pi);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, 2);

                    _defaultGroupName = pi.GroupName;
                    SelectGroupByName(_defaultGroupName);
                }

                Start();

                _pickedObject = null;

                CreateDragInstance(x, y);
            }
        }

        protected void HideDragInstance()
        {
            if (_dragInstance != null)
            {
                Globals.ActiveDrawing.DeletePrimitive(_dragInstance);
                _dragInstance = null;
            }
        }

        protected void HideAttributePanel()
        {
            if (_attributePanelInstance != null)
            {
                Globals.Events.GroupInstantiated(_attributePanelInstance, true);
                _attributePanelInstance = null;
            }
        }

        protected void ShowAttributePanel(PInstance instance)
        {
            HideAttributePanel();

            if (_group.AttributeList.Count > 0)
            {
                HideDragInstance();

                Globals.Events.GroupInstantiated(instance, false);
                _attributePanelInstance = instance;
            }
        }

        protected PInstance CreateInstance(Point location)
        {
            PInstance instance = null;

            if (_group != null)
            {
                instance = new PInstance(location, _group.Name);

                if (Globals.UIVersion == 0)
                {
                    instance.Scale(location, _group.PreferredScale, _group.PreferredScale);
                }
                else
                {
                    instance.Scale(location, _group.PreferredScale, _group.PreferredScale);
                }
            }

            return instance;
        }

        private void Flip()
        {
            if (_currentInstance != null)
            {
                _currentInstance.Flip = (_currentInstance.Flip + 1) % 4;
            }
        }

        public override void Finish()
        {
            HideAttributePanel();
            HideDragInstance();

            if (_pickGroup)
            {
                _defaultGroupName = null;
            }

            Globals.Input.SelectCursor(cursorType);

            if (Globals.UIVersion == 0)
            {
                //base.Finish();

                // begin from base class
                if (_hoverTimer != null)
                {
                    _hoverTimer.Stop();
                }

                _constructHandles.Deselect();
                _constructHandles.Clear();
                _constructNodes.Clear();

                //Start();

                if (_rubberBand != null)
                {
                    _rubberBand.State = 0;
                    _rubberBand.EndTracking();
                    _rubberBand.Color = _color;
                }
                // end from base class
            }
            else
            {
                if (_hoverTimer != null)
                {
                    _hoverTimer.Stop();
                }

                _constructHandles.Deselect();
                _constructHandles.Clear();
                _constructNodes.Clear();

                //Start();

                if (_rubberBand != null)
                {
                    _rubberBand.State = 0;
                    _rubberBand.EndTracking();
                    _rubberBand.Color = _color;
                }
            }
        }
    }

    //public enum InsertRepeatMode
    //{
    //    Single,
    //    Linear,
    //    LinearDistribute,
    //    Radial,
    //    RadialDistribute
    //}
}
