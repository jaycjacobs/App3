using Cirros.Actions;
using Cirros.Core;
using Cirros.Core.Display;
using Cirros.Display;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Cirros.Commands
{
    public class EditCommandProcessor : CommandProcessor
    {
        EditCommand _editCommand = null;
        static string _editCommandstring = "A_EditPoints";

        private Primitive _selection = null;

        bool _isTracking = false;
        int _selectedHandleId = -1;

        Handles _handles = new Handles();

        PTextEdit _editTextBox = null;
        private PInstance _currentInstance;

        public EditCommandProcessor()
        {
            _type = CommandType.edit;

            ShowConstructHandles = false;

            if (Globals.CommandProcessorParameter is Primitive)
            {
                SelectSingleObject((Primitive)Globals.CommandProcessorParameter);
                Invoke(_editCommandstring, null);
                UpdateHandles();
            }
            else
            {
                Globals.Events.ShowContextMenu(null, "edit");
            }
        }

        protected override void Hover(Point from, Point to, Point through)
        {
            if (_start == _current)
            {
                base.Hover(from, to, through);
            }
            else
            {
                _constructNodes.Clear();

                // Check for workCanvas not null - this could be hit after workCanvas is closed
                if (Globals.ActiveDrawing != null)
                {
                    foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
                    {
                        if (Globals.LayerTable[p.LayerId].Visible && p.IsNear(to, _hoverDistance))
                        {
                            _constructNodes.AddRange(p.ConstructNodes);
                            _constructNodes.AddRange(p.DynamicConstructNodes(from, through));
                        }
                    }
                    _constructNodes.AddRange(Globals.DrawingTools.DynamicTriangleConstructNodes(from, through));
                    //System.Diagnostics.Debug.WriteLine("Hover(({0:F3},{1:F3}) ,({2:F3},{3:F3}), ({4:F3},{5:F3})", from.X, from.Y, to.X, to.Y, through.X, through.Y);
                }
            }
        }

        void DeleteSelection()
        {
            if (_selection != null)
            {
                Primitive p = _selection;
                Deselect();
                Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, p);
                Globals.ActiveDrawing.DeletePrimitive(p);
            }
        }

        void CopySelection()
        {
            if (_selection != null)
            {
                Primitive copy = _selection.Clone();
                copy.ZIndex = Globals.ActiveDrawing.MaxZIndex;
                copy.AddToContainer(Globals.ActiveDrawing);

                Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, copy);
                SelectSingleObject(copy);
            }
        }

        void StrokeSelection()
        {
            if (_selection != null)
            {
                switch (_selection.TypeName)
                {
                    case PrimitiveType.Text:
                    case PrimitiveType.Dimension:
                    case PrimitiveType.Instance:
                        break;

                    default:
                        {
                            List<Primitive> primitives = _selection.Stroke();

                            if (primitives != null && primitives.Count > 0)
                            {
                                int undocount = 1;
                                Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, _selection);
                                Globals.ActiveDrawing.DeletePrimitive(_selection);

                                foreach (Primitive p in primitives)
                                {
                                    p.AddToContainer(Globals.ActiveDrawing);
                                    Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, p);
                                    undocount++;
                                }
                                Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, 2);

                                SelectSingleObject(primitives[0]);
                            }
                        }
                        break;
                }
            }
        }

        public override void Invoke(object o, object parameter)
        {
            if (o is string && _selection != null)
            {
                closeTextEdit();

                _editCommandstring = (string)o;

                switch (_editCommandstring)
                {
                    case "A_EditMoveSegment":
                        if (_editCommand is EditMoveSegmentCommand == false)
                        {
                            _selection.ConstructEnabled = false;
                            _editCommand = new EditMoveSegmentCommand(_selection, _handles);
                            Globals.EditSubCommand = _editCommand.EditSubCommand;
                            UpdateHandles();
                        }
                        break;

                    case "A_EditPoints":
                        _selection.ConstructEnabled = _selection is PInstance ? true : false;
                        _editCommand = new EditMoveVertexCommand(_selection, _handles);
                        Globals.EditSubCommand = _editCommand.EditSubCommand;
                        UpdateHandles();
                        break;

                    case "A_EditInsertVertex":
                        _selection.ConstructEnabled = _selection is PInstance ? true : false;
                        _editCommand = new EditInsertVertexCommand(_selection, _handles);
                        Globals.EditSubCommand = _editCommand.EditSubCommand;
                        UpdateHandles();
                        break;

                    case "A_EditExtendTrim":
                        _selection.ConstructEnabled = _selection is PInstance ? true : false;
                        _editCommand = new EditExtendTrimCommand(_selection, _handles);
                        Globals.EditSubCommand = _editCommand.EditSubCommand;
                        UpdateHandles();
                        break;

                    case "A_EditDeleteVertex":
                        if (_editCommand is EditDeleteVertexCommand == false)
                        {
                            _selection.ConstructEnabled = false;
                            Globals.EditSubCommand = Commands.EditSubCommand.DeletePoint;
                            _editCommand = new EditDeleteVertexCommand(_selection, _handles);
                            UpdateHandles();
                        }
                        break;

                    case "A_EditGap":
                        _selection.ConstructEnabled = false;
                        _editCommand = new EditGapCommand(_selection, _handles);
                        Globals.EditSubCommand = _editCommand.EditSubCommand;
                        UpdateHandles();
                        break;

                    case "A_EditOffsetMove":
                        {
                            _selection.ConstructEnabled = false;
                            if (_editCommand == null || Globals.EditSubCommand != EditSubCommand.OffsetMove)
                            {
                                _editCommand = new EditOffsetMoveCommand(_selection, _handles);
                                Globals.EditSubCommand = _editCommand.EditSubCommand;
                            }
                            if (parameter is double)
                            {
                                MoveParallel((double)parameter);
                            }
                            UpdateHandles();
                        }
                        break;

                    case "A_EditOffsetCopy":
                        {
                            _selection.ConstructEnabled = false;
                            if (_editCommand == null || Globals.EditSubCommand != EditSubCommand.OffsetCopy)
                            {
                                _editCommand = new EditOffsetCopyCommand(_selection, _handles);
                                Globals.EditSubCommand = _editCommand.EditSubCommand;
                            }
                            if (parameter is double)
                            {
                                MoveParallel((double)parameter, true);
                            }
                            UpdateHandles();
                        }
                        break;

                    case "A_SelectProperties":
                        _handles.Detach();
                        _editCommand = new EditPropertiesCommand(_selection, _handles);
                        Globals.EditSubCommand = _editCommand.EditSubCommand;
                        //closeTextEdit();
                        break;

                    default:
                        _editCommand = null;
                        switch ((string)o)
                        {
                            case "A_InsertImage":
                                if (_selection is PImage pi && parameter is Dictionary<string, object>)
                                {
                                    PImage clone = new PImage(pi);
                                    Dictionary<string, object> dictionary = parameter as Dictionary<string, object>;

                                    PrimitiveUtilities.UpdatePImageFromDictionary(pi, dictionary, pi.Origin);
                                    Globals.CommandDispatcher.AddUndoableAction(ActionID.ReplaceImage, pi, clone);
                                }
                                break;

                            case "A_EditText":
                                if (_selection is PText)
                                {
                                    //closeTextEdit();

                                    PText text = _selection as PText;
                                    _handles.Detach();
                                    Globals.CommandDispatcher.AddUndoableAction(ActionID.EditText, text, text.Text);
                                    _editTextBox = new PTextEdit(_selection as PText);
                                }
                                break;

                            case "A_SelectUngroup":
                                if (_selection is PInstance)
                                {
                                    Utilities.UnGroup(_selection as PInstance, true);
                                    Deselect();
                                }
                                break;

                            case "A_EditStroke":
                                if (_selection != null)
                                {
                                    switch (_selection.TypeName)
                                    {
                                        case PrimitiveType.Text:
                                        case PrimitiveType.Dimension:
                                        case PrimitiveType.Instance:
                                            break;

                                        default:
                                            StrokeSelection();
                                            break;
                                    }
                                }
                                break;

                            case "A_EditFlip":
                                if (_selection is PInstance)
                                {
                                    Globals.CommandDispatcher.AddUndoableAction(ActionID.Flip, _selection, ((PInstance)_selection).Flip);
                                    ((PInstance)_selection).Flip = (((PInstance)_selection).Flip + 1) % 4;
                                }
                                break;
                        }
                        break;
                }
            }
        }

        public override bool EnableCommand(object o)
        {
            bool enable = false;

            if (o is string s && _selection != null)
            {
                if (s == "A_EditText")
                {
                    enable = _selection is PText || (_selection is PInstance && ((PInstance)_selection).AttributeList.Count > 0);
                }
                else if (_editCommand != null)
                {
                    enable = _editCommand.CanHandlePrimitive(_selection);
                }
            }
            return enable;
        }

        public override InputMode InputMode
        {
            get
            {
                return InputMode.Pick;
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

        public override void KeyDown(string key, bool shift, bool control, bool gmk)
        {
            base.KeyDown(key, shift, control, gmk);

            if (gmk)
            {
                if (key == "delete")
                {
                    DeleteSelection();
                }
                else if (key == "n" && _handles.Count > 0)
                {
                    _selectedHandleId = _handles.SelectNext(_selectedHandleId);
                    Point handlePoint = _handles.GetHandlePoint(_selectedHandleId);
                    Point xy = new Point(handlePoint.X, handlePoint.Y);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveVertex, _handles.AttachedObject, _selectedHandleId, xy);
                }
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            _shiftKey = shift;

            if (_selection != null)
            {
                if (_editCommand != null)
                {
                    _handles.Deselect();

                    _selectedHandleId = _handles.Hit(x, y);
                    _editCommand.SelectHandle(_selectedHandleId);

                    if (_selectedHandleId > 0)
                    {
                        ShowConstructHandles = true;
                    }
                    else if (PickSelection(x, y) == false)
                    {
                        Deselect();
                    }

                    _editCommand.StartTracking(x, y, shift, control);

                    _start = _editCommand.AnchorPoint;
                    _current = _through = _editCommand.ReferencePoint;
                }
                else
                {
                    Deselect();
                    _start = _current = _through = new Point(x, y);
                }
            }
            else
            {
                if (_editCommand != null)
                {
                    _editCommand.StartTracking(x, y, shift, control);
                }

                _start = _current = _through = new Point(x, y);
            }

            if (_selection == null && Globals.ReturnToCommand != CommandType.none)
            {
                // This is a one-time edit on behalf of another command
                // Once the edit if finished, control shoule be returned to the previous command
                Globals.CommandDispatcher.ActiveCommand = Globals.ReturnToCommand;
                Globals.ReturnToCommand = CommandType.none;
            }

            _isTracking = true;
        }

        public override void TrackCursor(double x, double y)
        {
            base.TrackCursor(x, y);

            if (_isTracking)
            {
                if (_editCommand != null)
                {
                    _editCommand.TrackCursor(ref x, ref y);
                }
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_editCommand != null)
            {
                if (_editCommand.HasSelection == false)
                {
                    Primitive pick = Globals.ActiveDrawing.Pick(_start.X, _start.Y, false);
                    SelectSingleObject(pick);
                }
                _editCommand.EndTracking(x, y);
            }
            else
            {
                if (_selectedHandleId == -1)
                {
                    Primitive pick = Globals.ActiveDrawing.Pick(_start.X, _start.Y, false);
                    SelectSingleObject(pick);
                }

            }

            Globals.Input.SelectCursor(CursorType.Hand);

            _isTracking = false;
        }

        public Point SnapTo(Point p)
        {
            Point snap = p;

            if (_constructHandles.SelectedHandleID >= 0)
            {
                snap = _constructHandles.SelectedHandle.Location;
            }
            else if (Globals.DrawingTools.ActiveTrianglePoint(out Point trianglePoint))
            {
                snap = trianglePoint;
            }

            return snap;
        }

        public override Point Step(double dx, double dy, bool stillDown)
        {
            Point paper = base.Step(dx, dy, stillDown);

            if (_selection != null)
            {
                if (_editCommand != null)
                {
                    _editCommand.Step(dx, dy);
                }
            }

            return paper;
        }

        public override void UndoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            switch (actionId)
            {
                case ActionID.MultiUndo:
                    break;

                case ActionID.UnNormalize:
                    break;

                case ActionID.MoveGroupOrigin:
                case ActionID.MoveGroupEntry:
                case ActionID.MoveGroupExit:
                case ActionID.ReplaceImage:
                    UpdateHandles();
                    break;

                case ActionID.Move:
                case ActionID.RestoreMatrix:
                case ActionID.RestorePoints:
                case ActionID.MoveVertex:
                case ActionID.SetAngle:
                case ActionID.SetHeight:
                case ActionID.SetRadius:
                case ActionID.SetWidth:
                default:
                    if (subject == _selection)
                    {
                        UpdateHandles();
                    }
                    else if (subject is Primitive)
                    {
                        SelectSingleObject(subject as Primitive);
                        UpdateHandles();
                    }
                    break;

                case ActionID.DeletePrimitive:
                    Deselect();
                    break;
            }

            if (_editCommand != null)
            {
                _editCommand.UndoNotification(actionId, subject, predicate, predicate2);
            }
        }

        public override void RedoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            switch (actionId)
            {
                case ActionID.MultiUndo:
                    break;

                case ActionID.UnNormalize:
                    break;

                case ActionID.MoveGroupOrigin:
                case ActionID.MoveGroupEntry:
                case ActionID.MoveGroupExit:
                case ActionID.ReplaceImage:
                    UpdateHandles();
                    break;

                case ActionID.Move:
                case ActionID.RestoreMatrix:
                case ActionID.RestorePoints:
                case ActionID.MoveVertex:
                case ActionID.SetAngle:
                case ActionID.SetHeight:
                case ActionID.SetRadius:
                case ActionID.SetWidth:
                default:
                    if (subject == _selection)
                    {
                        UpdateHandles();
                    }
                    else if (subject is Primitive)
                    {
                        SelectSingleObject(subject as Primitive);
                        UpdateHandles();
                    }
                    break;

                case ActionID.DeletePrimitive:
                    Deselect();
                    break;
            }

            if (_editCommand != null)
            {
                _editCommand.UndoNotification(actionId, subject, predicate, predicate2);
            }
        }

        public void UpdateHandles()
        {
            if (_editCommand != null && _editCommand.Selection != null)
            {
                if (_selection != null)
                {
                    try
                    {
                        _editCommand.DrawHandles();
                        _selection.Highlight(true);
                    }
                    catch (Exception ex)
                    {
                        Analytics.ReportError(ex, new Dictionary<string, string> {
                            { "method", "EditCommandProcessor.UpdateHandles" },
                            { "command", _editCommand.EditSubCommand.ToString() }
                        }, 201);
                    }
                }
            }
        }

        private void closeTextEdit()
        {
            if (_editTextBox != null)
            {
                _editTextBox.Finish();
                _editTextBox = null;
            }

            if (_currentInstance != null)
            {
                Globals.Events.GroupInstantiated(_currentInstance, true);
                _currentInstance = null;
            }
        }

        public override void CanvasScrolled()
        {
            closeTextEdit();
            base.CanvasScrolled();
        }

        public override void CanvasScaleChanged()
        {
            _handles.Draw();

            closeTextEdit();
            base.CanvasScaleChanged();
        }

        public override void Finish()
        {
            Deselect();
            base.Finish();

            Globals.Events.ShowContextMenu(null, null);
        }

        private void Deselect()
        {
            _handles.Detach();
            _selectedHandleId = -1;

            closeTextEdit();

            _isTracking = false;

            if (_selection != null)
            {
                _selection.IsDynamic = false;
                _selection.ConstructEnabled = true;
                _selection.Highlight(false);
                _selection = null;

                Globals.Events.PrimitiveSelectionChanged(null);
                Globals.Events.ShowContextMenu(null, "edit");
            }

            ShowConstructHandles = false;

            if (_editCommand != null)
            {
                _editCommand.Selection = null;
                _editCommand.SelectHandle(-1);
            }
        }

        public void SelectSingleObject(Primitive p)
        {
            if (p != _selection)
            {
                if (_selection != null)
                {
                    Deselect();
                }
                if (p != null)
                {
                    _selection = Utilities.GetTopLevelPrimitive(p);
                    ShowConstructHandles = _selection != null;
                    _selection.IsDynamic = true;

                    p.ConstructEnabled = false;

                    if (_editCommand != null)
                    {
                        if (_editCommand.CanHandlePrimitive(_selection))
                        {
                            _editCommand.Selection = _selection;
                        }
                        else
                        {
                            _editCommand = null;
                        }
                    }

                    Globals.Events.PrimitiveSelectionChanged(_selection);
                    Globals.Events.ShowContextMenu(_selection, "edit");

                    UpdateHandles();
                }

            }
        }

        void MoveParallel(double distance, bool makeCopy = false)
        {
            if (_selection is PLine)
            {
                if (makeCopy)
                {
                    CopySelection();
                }
                else
                {
                    PLine line = _selection as PLine;
                    List<CPoint> cpoints = new List<CPoint>();
                    cpoints.AddRange(line.CPoints);

                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePoints, line, cpoints);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.Move, _selection, _selection.Origin);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, 2);
                }

                ((PLine)_selection).MoveParallel(distance);
            }
        }

        protected virtual bool PickSelection(double x, double y)
        {
            uint segmentId = uint.MaxValue;

            if (_selection != null)
            {
                VectorList vlist = Globals.DrawingCanvas.VectorListControl.VectorList;
                if (vlist != null)
                {
                    VectorEntity ve = vlist.GetSegment(_selection.Id);
                    if (ve != null)
                    {
                        double tol = Globals.DrawingCanvas.DisplayToPaper(Globals.hitTolerance);
                        ve.Pick(new Point(x, y), tol, ref segmentId);
                    }
                }
            }

            return segmentId != uint.MaxValue;
        }
    }
}
