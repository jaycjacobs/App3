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
    class CopyAlongArcCommandProcessor : CommandProcessor
    {
        protected Group _group = null;
        //protected Point _insertOffset = new Point(0, 0);
        protected Primitive _pickedPrimitive = null;

        public CopyAlongArcCommandProcessor(string groupName = null)
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

            _rubberBand = new RubberBandArc();
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
                ShowConstructHandles = false;

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
                    Globals.Events.ShowContextMenu(_pickedPrimitive, "copyalongarc");
                }
            }
            else
            {
                ShowConstructHandles = true;

                RubberBandArc rba = _rubberBand as RubberBandArc;
                if (rba != null)
                {
                    switch (rba.State)
                    {
                        case 0:
                        case 3:
                            rba.Center = new Point(_start.X, _start.Y);
                            rba.State = 1;
                            break;

                        case 1:
                            rba.Start = new Point(_start.X, _start.Y);
                            rba.State = 2;
                            break;

                        case 2:
                            rba.End = new Point(_start.X, _start.Y);
                            rba.State = 3;

                            double va = _group.Exit == _group.Entry ? 0 : Construct.Angle(_group.Entry, _group.Exit);

                            double startAngle;
                            double angleIncrement;
                            int repeatCount;

                            if (Globals.RadialCopyRepeatType == CopyRepeatType.Distribute)
                            {
                                repeatCount = Globals.RadialCopyRepeatCount;

                                if (Globals.RadialCopyRepeatAtEnd)
                                {
                                    startAngle = rba.StartAngle;
                                    angleIncrement = rba.IncludedAngle / Math.Max(1, repeatCount - 1);
                                }
                                else
                                {
                                    angleIncrement = rba.IncludedAngle / repeatCount;
                                    startAngle = rba.StartAngle + angleIncrement / 2;
                                }
                            }
                            else
                            {
                                repeatCount = (int)(Math.Abs(rba.IncludedAngle) / Math.Abs(Globals.RadialCopyRepeatAngle));
                                angleIncrement = Math.Abs(Globals.RadialCopyRepeatAngle) * Math.Sign(rba.IncludedAngle);

                                if (Globals.RadialCopyRepeatAtEnd)
                                {
                                    startAngle = rba.StartAngle;
                                    repeatCount = (int)(rba.IncludedAngle / angleIncrement) + 1;
                                }
                                else
                                {
                                    startAngle = rba.StartAngle + angleIncrement / 2;
                                    repeatCount = (int)((rba.IncludedAngle + (angleIncrement / 2)) / angleIncrement);
                                }
                            }

                            double angle = startAngle;
                            int undoCount = 0;
                            double connectStartAngle = rba.StartAngle;
                            double connectIncludedAngle = rba.IncludedAngle;
                            double endAngle = rba.StartAngle + rba.IncludedAngle;

                            bool cw = angleIncrement > 0;

                            for (int i = 0; i < repeatCount; i++)
                            {
                                double dx = rba.Radius * Math.Cos(angle);
                                double dy = rba.Radius * Math.Sin(angle);
                                Point p = new Point(rba.Center.X + dx, rba.Center.Y + dy);

                                PInstance newInstance;

                                // WARNING
                                // CreateInstance() will return null if _group is null
                                if (_group.InsertLocation == GroupInsertLocation.None)
                                {
                                    newInstance = CreateInstance(new Point(p.X, p.Y));
                                }
                                else
                                {
                                    double degrees = (angle + Math.PI / 2 + va) * Construct.cRadiansToDegrees;

                                    Matrix mtx = CGeometry.ScaleMatrix(_group.PreferredScale, _group.PreferredScale);
                                    mtx = CGeometry.RotateMatrixAboutZ(mtx, angle + Math.PI / 2 + va);

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

                                    newInstance = CreateInstance(new Point(p.X - vOff.X, p.Y - vOff.Y));
                                    newInstance.Rotate(newInstance.Origin, degrees);

                                    if (Globals.RadialCopyRepeatConnect)
                                    {
                                        Point sp = new Point(p.X - vGS.X, p.Y - vGS.Y);
                                        Point ep = new Point(sp.X + vGap.X, sp.Y + vGap.Y);

                                        if (cw == false)
                                        {
                                            Point tmp = sp;
                                            sp = ep;
                                            ep = tmp;
                                        }

                                        double ag = Construct.Angle(rba.Center, sp);
                                        connectIncludedAngle = Construct.IncludedAngle(connectStartAngle, ag, cw);
                                        //System.Diagnostics.Debug.WriteLine("Construct.IncludedAngle({0:#.###}, {1:#.###}, {2}) = {3:#.###}", connectStartAngle, ag, cw, connectIncludedAngle);

                                        if (Math.Abs(connectIncludedAngle) < Math.Abs(angleIncrement) && connectIncludedAngle != 0)
                                        {
                                            PArc arc = new PArc(rba.Center, rba.Radius, connectStartAngle, connectIncludedAngle, (uint)ColorCode.NoFill);
                                            addPrimitive(arc);
                                            undoCount++;
                                        }

                                        connectStartAngle = Construct.Angle(rba.Center, ep);
                                    }
                                }

                                addPrimitive(newInstance);
                                undoCount++;

                                angle += angleIncrement;
                            }

                            if (Globals.RadialCopyRepeatConnect)
                            {
                                connectIncludedAngle = Construct.IncludedAngle(connectStartAngle, endAngle, cw);

                                if (Math.Abs(connectIncludedAngle) <= Math.Abs(rba.IncludedAngle))
                                {
                                    PArc arc = new PArc(rba.Center, rba.Radius, connectStartAngle, connectIncludedAngle, (uint)ColorCode.NoFill);
                                    addPrimitive(arc);
                                    undoCount++;
                                }
                            }

                            if (undoCount > 1)
                            {
                                Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, undoCount);
                                undoCount = 0;
                            }

                            _rubberBand.Reset();
                            break;
                    }
                }
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
                Globals.Events.ShowContextMenu(_pickedPrimitive, "copyalongarc");
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
