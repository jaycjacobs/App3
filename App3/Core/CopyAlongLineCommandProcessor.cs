using Cirros.Actions;
using Cirros.Core;
using Cirros.Display;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Commands
{
    class CopyAlongLineCommandProcessor : CommandProcessor
    {
        protected Group _group = null;
        //protected Point _insertOffset = new Point(0, 0);
        protected Primitive _pickedPrimitive = null;

        public CopyAlongLineCommandProcessor(string groupName = null)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                ShowConstructHandles = false;
            }
            else
            {
                SelectGroupByName(groupName);
            }

            int layerId = Globals.UIVersion == 0 ? Globals.LayerId : Globals.ActiveInstanceLayerId;

            if (Globals.LayerTable.ContainsKey(layerId))
            {
                Layer layer = Globals.LayerTable[layerId];
                if (layer != null)
                {
                    _color = Utilities.ColorFromColorSpec(layer.ColorSpec);
                }
            }

            _rubberBand = new RubberBandLine(_color);
            _rubberBand.State = 0;
        }

        protected override CursorType cursorType
        {
            get
            {
                return _group == null ? CursorType.Hand : CursorType.Draw;
            }
        }

        public override void CanvasScaleChanged()
        {
            _handles.Draw();
            base.CanvasScaleChanged();
        }

        public override bool EnableCommand(object o)
        {
            bool enable = false;

            if (o is string && (string)o == "A_Done")
            {
                enable = _pickedPrimitive != null;
            }

            return enable;
        }

        public override void Invoke(object o, object parameter)
        {
            if (o is string)
            {
                string option = o as string;

                switch (option)
                {
                    case "A_Done":
                        Finish();
                        break;

                    case "A_SelectNew":
                        ClearSelection();

                        _rubberBand.State = 0;
                        _rubberBand.EndTracking();
                        break;

                    case "A_GetSymbol":
                        if (parameter is string)
                        {
                            ClearSelection();
                            SelectGroupByName(parameter as string);
                        }
                        break;

                    case "A_ClearSelection":
                        break;
                }
            }
        }

        public override void UndoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            base.UndoNotification(actionId, subject, predicate, predicate2);
            ClearSelection();
        }

        Handles _handles = new Handles();

        void HighlightSelection(Primitive p, bool flag)
        {
            if (p is PInstance)
            {
                p.Highlight(flag);

                if (flag)
                {
                    PInstance pi = p as PInstance;
                    if (Globals.ActiveDrawing.Groups.ContainsKey(pi.GroupName))
                    {
                        Group g = Globals.ActiveDrawing.Groups[pi.GroupName];

                        if (g.InsertLocation != GroupInsertLocation.None)
                        {
                            Point entry = Globals.ActiveDrawing.ModelToPaperDelta(g.Entry);
                            entry = pi.Matrix.Transform(entry);
                            entry.X += pi.Origin.X;
                            entry.Y += pi.Origin.Y;

                            Point exit = Globals.ActiveDrawing.ModelToPaperDelta(g.Exit);
                            exit = pi.Matrix.Transform(exit);
                            exit.X += pi.Origin.X;
                            exit.Y += pi.Origin.Y;

                            _handles.AddHandle(1000, entry.X, entry.Y, HandleType.Diamond);
                            _handles.AddHandle(2000, exit.X, exit.Y, HandleType.Diamond);
                            _handles.ArrowFrom = 1000;
                            _handles.ArrowTo = 2000;
                        }
                        else
                        {
                            _handles.AddHandle(1000, pi.Origin.X, pi.Origin.Y, HandleType.Triangle);
                        }

                        _handles.Draw();
                    }
                }
                else
                {
                    _handles.Detach();
                }
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            if (_group == null)
            {
                _pickedPrimitive = Globals.ActiveDrawing.Pick(x, y, true);
                if (_pickedPrimitive != null)
                {
                    if (_pickedPrimitive is PInstance == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, _pickedPrimitive);

                        _pickedPrimitive = Globals.ActiveDrawing.CreateGroupFromSinglePrimitive(_pickedPrimitive);

                        Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, _pickedPrimitive);
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, 2);
                    }

                    SelectGroupByName(((PInstance)_pickedPrimitive).GroupName);
                    HighlightSelection(_pickedPrimitive, true);
                    ShowConstructHandles = true;
                }

                if (Globals.UIVersion == 0)
                {
                    Globals.Events.ShowContextMenu(_pickedPrimitive, "copyalongline");
                }
            }
            else
            {
                if (_rubberBand is RubberBandLine)
                {
                    _rubberBand.StartTracking(_start.X, _start.Y);

                    switch (_rubberBand.State)
                    {
                        case 0:
                            _first = _start;
                            _rubberBand.State = 1;
                            break;

                        case 1:
                            if (_start.X != _first.X || _start.Y != _first.Y)
                            {
                                Copy(_first, _start);

                                _rubberBand.State = 0;
                                _rubberBand.EndTracking();
                            }
                            break;
                    }
                }
            }
        }

        protected void Copy(Point from, Point to)
        {
            double distance = Construct.Distance(from, to);
            double lineAngle = Construct.Angle(from, to);
            Point p = from;
            int count;
            double incr;

            double va = _group.Exit == _group.Entry ? 0 : Construct.Angle(_group.Entry, _group.Exit);
            double angle = Construct.Angle(from, to) + va;
            double degrees = angle * Construct.cRadiansToDegrees;

            double scale = Globals.GroupScale * _group.PreferredScale;
            Matrix mtx = CGeometry.ScaleMatrix(scale, scale);
            mtx = CGeometry.RotateMatrixAboutZ(mtx, angle);

            Point vGap = new Point();
            Point vOff = new Point();
            Point vGS = new Point();

            if (_group.Entry.X == 0 && _group.Entry.Y == 0)
            {
                Point exit = Globals.ActiveDrawing.ModelToPaperDelta(_group.Exit);
                vGap = mtx.Transform(exit);
                //vGS = vOff = mtx.Transform(new Point(-_insertOffset.X, -_insertOffset.Y));
            }
            else
            {
                Point entry = Globals.ActiveDrawing.ModelToPaperDelta(_group.Entry);
                Point exit = Globals.ActiveDrawing.ModelToPaperDelta(_group.Exit);
                vGap = mtx.Transform(new Point(exit.X - entry.X, exit.Y - entry.Y));
                //vOff = mtx.Transform(new Point(-_insertOffset.X, -_insertOffset.Y));
                vGS = mtx.Transform(new Point(-entry.X, -entry.Y));
            }

            List<Primitive> _instances = new List<Primitive>();

            if (distance > 0)
            {
                double gap = Construct.Distance(new Point(0, 0), vGap);

                if (Globals.LinearCopyRepeatType == CopyRepeatType.Distribute)
                {
                    count = Globals.LinearCopyRepeatCount;
                    incr = distance / Math.Max(Globals.LinearCopyRepeatAtEnd ? Globals.LinearCopyRepeatCount - 1 : Globals.LinearCopyRepeatCount, 1);
                }
                else
                {
                    if (Globals.LinearCopyRepeatAtEnd)
                    {
                        count = (int)(distance / Globals.LinearCopyRepeatDistance) + 1;
                    }
                    else
                    {
                        count = Math.Max((int)(distance / Globals.LinearCopyRepeatDistance), 1);
                    }

                    incr = Math.Min(Globals.LinearCopyRepeatDistance, distance);
                }

                bool connect = Globals.LinearCopyRepeatConnect && gap < incr;

                List<CPoint> _linePoints = null;

                if (connect)
                {
                    _linePoints = new List<CPoint>();
                    _linePoints.Add(new CPoint(from, 0));
                }

                Point delta = Construct.PolarOffset(new Point(), incr, lineAngle);

                if (Globals.LinearCopyRepeatAtEnd == false)
                {
                    if (Globals.LinearCopyRepeatType == CopyRepeatType.Space)
                    {
                        double initialOffset = (distance - ((count - 1) * Globals.LinearCopyRepeatDistance)) / 2;
                        Point initialDelta = Construct.PolarOffset(new Point(), initialOffset, lineAngle);
                        p.X += initialDelta.X;
                        p.Y += initialDelta.Y;
                    }
                    else
                    {
                        p.X += delta.X / 2;
                        p.Y += delta.Y / 2;
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    if (Construct.PointValue(from, to, p) > 1.001)
                    {
                        break;
                    }

                    PInstance newInstance;

                    // WARNING
                    // CreateInstance() will return null if _group is null
                    if (_group.InsertLocation == GroupInsertLocation.None)
                    {
                        newInstance = CreateInstance(new Point(p.X, p.Y));
                    }
                    else
                    {
                        newInstance = CreateInstance(new Point(p.X - vOff.X, p.Y - vOff.Y));
                        newInstance.Rotate(newInstance.Origin, degrees);

                        if (connect)
                        {
                            Point sp = new Point(p.X - vGS.X, p.Y - vGS.Y);
                            Point ep = new Point(sp.X + vGap.X, sp.Y + vGap.Y);
                            double pvs = Construct.PointValue(from, to, sp);

                            if (i == 0 && pvs < 0)
                            {
                                // Gap start point is before the beginning of the line - remove the first segment
                                _linePoints[0] = new CPoint(ep, 0);
                            }
                            else if (pvs < 1)
                            {
                                _linePoints.Add(new CPoint(sp, 1));
                                if (Construct.PointValue(from, to, ep) < 1)
                                {
                                    _linePoints.Add(new CPoint(ep, 0));
                                }
                            }
                        }
                    }

                    addPrimitive(newInstance);
                    _instances.Add(newInstance);

                    p.X += delta.X;
                    p.Y += delta.Y;
                }

                if (connect && _linePoints != null && _linePoints.Count > 0)
                {
                    if (_linePoints[_linePoints.Count - 1].M == 0)
                    {
                        _linePoints.Add(new CPoint(to, 1));
                    }

                    PLine line;

                    if (Globals.UIVersion == 0)
                    {
                        line = new PLine(_linePoints[0].Point, _linePoints[1].Point);
                    }
                    else
                    {
                        int saveLayerId = Globals.ActiveLineLayerId;
                        Globals.ActiveLineLayerId = Globals.ActiveInstanceLayerId;

                        line = new PLine(_linePoints[0].Point, _linePoints[1].Point);

                        Globals.ActiveLineLayerId = saveLayerId;
                    }

                    for (int i = 2; i < _linePoints.Count; i++)
                    {
                        line.AddPoint(_linePoints[i], false);
                    }

                    addPrimitive(line);
                    _instances.Add(line);
                }

                if (_instances.Count > 0)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _instances.Count);
                }

                _rubberBand.State = 0;
                _rubberBand.EndTracking();
            }
        }

        protected void ClearSelection()
        {
            if (_pickedPrimitive != null)
            {
                HighlightSelection(_pickedPrimitive, false);
                _pickedPrimitive = null;
            }
          
            _group = null;

            ShowConstructHandles = false;
            Globals.Input.SelectCursor(cursorType);
        }

        protected void SelectGroupByName(string groupName)
        {
            _group = Globals.ActiveDrawing.GetGroup(groupName);

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

        protected PInstance CreateInstance(Point location)
        {
            PInstance instance = null;

            if (_group != null)
            {
                double scale = Globals.GroupScale * _group.PreferredScale;
                instance = new PInstance(location, _group.Name);
                instance.Scale(location, scale, scale);
            }

            return instance;
        }

        public override void Start()
        {
            base.Start();
            if (Globals.UIVersion == 0)
            {
                Globals.Events.ShowContextMenu(_pickedPrimitive, "copyalongline");
            }
        }

        public override void Finish()
        {
            ClearSelection();

            _rubberBand.EndTracking();
            _rubberBand.Reset();

            base.Finish();

            if (Globals.UIVersion == 0)
            {
                Globals.Events.ShowContextMenu(null, null);
            }
        }
    }
}
