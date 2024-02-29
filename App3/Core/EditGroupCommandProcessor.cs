using Cirros.Actions;
using Cirros.Core;
using Cirros.Display;
using Cirros.Primitives;
using Cirros.Utility;
using Cirros8;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Commands
{
    public class EditGroupCommandProcessor : CommandProcessor
    {
        double _lastTrackX = 0;
        double _lastTrackY = 0;

        bool _isTracking = false;
        bool _isDragging = false;

        bool _controlKey = false;

        Handles _handles = new Handles();

        Primitive _selection = null;
        Primitive _member = null;
        int _memberIndex = -1;
        int _highlightedMemberIndex = -1;

        //Primitive _primitive = null;

        int _selectedHandleId = -1;

        private Point _lineAnchorPoint;
        private Point _lineReferencePoint;

        const int cOriginMarker = 800;
        const int cStartMarker = 820;
        const int cEndMarker = 840;

        public EditGroupCommandProcessor()
        {
            _type = CommandType.edit;

            Globals.EditSubCommand = EditSubCommand.None;

            if (Globals.CommandProcessorParameter is Primitive)
            {
                SelectPrimitive((Primitive)Globals.CommandProcessorParameter);
                Invoke("A_EditPoints", null);
                UpdateHandles();
            }
            else
            {
                SelectPrimitive(null);
                ShowConstructHandles = false;
            }
        }

        public override void Invoke(object o, object parameter)
        {
            if (o is string)
            {
                if (_selection is PInstance)
                {
                    switch ((string)o)
                    {
                        case "A_EditPoints":
                            _selection.ConstructEnabled = true;
                            Globals.EditSubCommand = Commands.EditSubCommand.MovePoint;
                            UpdateHandles();
                            break;

                        case "A_GroupProperties":
                            _handles.Detach();
                            Globals.EditSubCommand = Commands.EditSubCommand.Properties;
                            break;

                        case "A_MemberProperties":
                            _handles.Detach();
                            Globals.EditSubCommand = Commands.EditSubCommand.MemberProperties;
                            break;

                        case "A_AddMember":
                            _handles.Detach();
                            Globals.EditSubCommand = Commands.EditSubCommand.AddMember;
                            break;

                        case "A_MoveMember":
                            _handles.Detach();
                            Globals.EditSubCommand = Commands.EditSubCommand.MoveMember;
                            break;

                        case "A_DeleteMember":
                            {
                                Globals.EditSubCommand = Commands.EditSubCommand.MemberProperties;
                                if (_selection is PInstance && _highlightedMemberIndex >= 0 && _highlightedMemberIndex == _memberIndex)
                                {
                                    Group g = Globals.ActiveDrawing.Groups[((PInstance)_selection).GroupName];
                                    Globals.CommandDispatcher.AddUndoableAction(ActionID.AddGroupMember, g, _member, _highlightedMemberIndex);
                                    g.RemoveMemberAt(_highlightedMemberIndex);
                                    Globals.ActiveDrawing.UpdateGroupInstances(g.Name);
                                    _memberIndex = -1;
                                    _highlightedMemberIndex = -1;
                                    _member = null;
                                    SelectInstance(_selection as PInstance, -1);
                                }
                            }
                            break;

                        case "A_CreateGroup":
                            CreateGroupFromPrimitive();
                            break;

                        case "A_Ungroup":
                            Utilities.UnGroup(_selection as PInstance, true);
                            Deselect();
                            break;
                    }
                }
                else if (_selection is Primitive)
                {

                }
                HighlightSelection(true);
            }
        }

        public override bool EnableCommand(object o)
        {
            bool enable = false;

            if (o is string)
            {
                if (_selection is PInstance)
                {
                    switch ((string)o)
                    {
                        case "A_EditPoints":
                            enable = _selection.IsTransformed == false;
                            break;

                        case "A_GroupProperties":
                            enable = _selection != null;
                            break;

                        case "A_MemberProperties":
                            enable = _member != null;
                            break;
                    }
                }
                else if (_selection is Primitive)
                {

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

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            _shiftKey = shift;
            _controlKey = control;

            if (_selection != null)
            {
                if (Globals.EditSubCommand == EditSubCommand.MovePoint)
                {
                    _selectedHandleId = _handles.Hit(x, y);
                }
                else
                {
                    _selectedHandleId = -1;
                }

                if (_selectedHandleId > 0)
                {
                    ShowConstructHandles = true;

                    if (_selection is PInstance && Globals.EditSubCommand == EditSubCommand.MovePoint)
                    {
                        // nothing to do here for move origin or exit
                    }
                    else
                    {
                        Point handlePoint = _handles.GetHandlePoint(_selectedHandleId);
                        Point xy = new Point(handlePoint.X, handlePoint.Y);
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveVertex, _selection, _selectedHandleId, xy);
                        Globals.Input.SelectCursor(CursorType.Draw);
                    }
                }
                else if (_selection is PInstance)
                {
                    if (Globals.EditSubCommand == EditSubCommand.MoveMember)
                    {
                        int m = Globals.DrawingCanvas.VectorListControl.GetMemberIndex(_selection.Id, new Point(x, y));
                        if (m == _memberIndex)
                        {
                            HighlightSelection(true);
                        }
                        else
                        {
                            Deselect();
                        }
                    }
                    else if (Globals.EditSubCommand == EditSubCommand.AddMember)
                    {
                        if (Globals.ActiveDrawing.Pick(x, y, true) == null)
                        {
                            Deselect();
                        }
                        else
                        {
                            // keep the selection
                            // pick the member in EndTracking()
                        }
                    }
                    else if (_selection != Globals.ActiveDrawing.Pick(x, y, true))
                    {
                        Deselect();
                    }
                }
                else if (_selection is Primitive)
                {
                    if (_selection != Globals.ActiveDrawing.Pick(x, y, true))
                    {
                        Deselect();
                    }
                }
            }
            else if (_isDragging)
            {
                // Keep selection - does this ever happen?

                ShowConstructHandles = true;

                _selection.ConstructEnabled = false;

                if (Globals.Input.ObjectSnap && shift == false)
                {
                    if (_constructHandles.SelectedHandleID >= 0)
                    {
                        x = _constructHandles.SelectedHandle.Location.X;
                        y = _constructHandles.SelectedHandle.Location.Y;
                        Globals.Input.SelectCursor(CursorType.Draw);
                    }
                    else if (Globals.DrawingTools.ActiveTrianglePoint(out Point trianglePoint))
                    {
                        x = trianglePoint.X;
                        y = trianglePoint.Y;
                        Globals.Input.SelectCursor(CursorType.Draw);
                    }
                }
            }
            else
            {
                Deselect();
            }

            if (_selection is PInstance && Globals.EditSubCommand == EditSubCommand.MovePoint)
            {
                // _start, _through and _current will be used for line construct in TrackCursor
                _start = _lineAnchorPoint;
                _current = _lineReferencePoint;
                _through = _lineReferencePoint;
            }
            else
            {
                // _start will be used to pick a new selection in EndTracking
                _start = new Point(x, y);
                _lineAnchorPoint = _lineReferencePoint = _start;
            }

            _lastTrackX = x;
            _lastTrackY = y;

            _isTracking = true;
        }

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
                    if (_selection is PInstance)
                    {
                        if (_selectedHandleId == cStartMarker)
                        {
                            _handles.MoveHandle(cStartMarker, dx, dy);
                        }
                        else if (_selectedHandleId == cEndMarker)
                        {
                            _handles.MoveHandle(cEndMarker, dx, dy);
                        }
                        else if (_selectedHandleId == cOriginMarker)
                        {
                            _handles.MoveHandle(cOriginMarker, dx, dy);
                        }
                        _handles.Draw();

                        Point h2 = _handles.GetHandlePoint(_selectedHandleId);
                        _lastTrackX = h2.X;
                        _lastTrackY = h2.Y;
                    }
                    else if (Globals.EditSubCommand == EditSubCommand.MovePoint && (dx != 0 || dy != 0))
                    {
                        //_handles.MoveHandle(_selectedHandleId, dx, dy);

                        //// [primitive].MoveHandle may have changed the handle location and selection to conform to the primitive geometry
                        //// so _lastTrack[XY] and the selected index should be adjusted accordingly

                        //Point h2 = _handles.GetHandlePoint(_selectedHandleId);
                        //_lastTrackX = h2.X;
                        //_lastTrackY = h2.Y;

                        //Globals.Events.PrimitiveSelectionSizeChanged(_selection);

                        //_handles.Select(_selectedHandleId);
                    }

                    Globals.Events.CoordinateDisplay(new Point(x, y));
                }
                else if (_selection is PInstance && _member != null)
                {
                    Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                    if (g != null)
                    {
                        if (Globals.EditSubCommand == EditSubCommand.MoveMember && (dx != 0 || dy != 0))
                        {
                            // A handle is not selected.  Drag the selection.
                            if (!_isDragging)
                            {
                                // If this is a new drag, save the current position
                                Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveGroupMember, g, _memberIndex, _member.Origin);
                                _isDragging = true;
                            }

                            MoveMemberByDelta(dx, dy);
                            _handles.Detach();
                        }

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
                if (_selection is PInstance)
                {
                    // Finish up the selection move.
                    if (Globals.Input.ObjectSnap && _shiftKey == false)
                    {
                        if (_constructHandles.SelectedHandleID >= 0)
                        {
                            double dx = _constructHandles.SelectedHandle.Location.X - _lastTrackX;
                            double dy = _constructHandles.SelectedHandle.Location.Y - _lastTrackY;
                            MoveMemberByDelta(dx, dy);
                        }
                        else if (Globals.DrawingTools.ActiveTrianglePoint(out Point trianglePoint))
                        {
                            double dx = trianglePoint.X - _lastTrackX;
                            double dy = trianglePoint.Y - _lastTrackY;
                            MoveMemberByDelta(dx, dy);
                        }
                    }

                    _isDragging = false;
                    Globals.ActiveDrawing.UpdateGroupInstances(((PInstance)_selection).GroupName);
                    _selection.ConstructEnabled = false;

                    UpdateHandles();
                }
            }
            else if (_selectedHandleId > 0)
            {
                // Finish up the handle move.

                if (Globals.Input.ObjectSnap && _shiftKey == false)
                {
                    // If object snap is enabled, adjust the final point accordingly

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

                if (_selection is PInstance)
                {
                    Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                    Matrix ivm = CGeometry.InvertMatrix(_selection.Matrix);
                    Point final = _handles.GetHandlePoint(_selectedHandleId);

                    if (g != null)
                    {
                        if (_selectedHandleId == cOriginMarker)
                        {
                            // move origin
                            Point d = new Point(final.X - _selection.Origin.X, final.Y - _selection.Origin.Y);
                            Point dd = ivm.Transform(d);
                            g.MoveOriginBy(-dd.X, -dd.Y);

                            Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveGroupOrigin, _selection, dd);
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveGroupEntry, g, g.Entry);
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveGroupExit, g, g.Exit);
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, 3);

                            Point entry = _handles.GetHandlePoint(cStartMarker);
                            entry = new Point(entry.X - final.X, entry.Y - final.Y);
                            g.Entry = Globals.ActiveDrawing.PaperToModelDelta(ivm.Transform(entry));

                            Point exit = _handles.GetHandlePoint(cEndMarker);
                            exit = new Point(exit.X - final.X, exit.Y - final.Y);
                            g.Exit = Globals.ActiveDrawing.PaperToModelDelta(ivm.Transform(exit));

                            _selection.MoveByDelta(d.X, d.Y);
                            //_selection.Regenerate();
                            _selection.Draw();
                            HighlightSelection(true);
                        }
                        else if (_selectedHandleId == cEndMarker)
                        {
                            // move exit
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveGroupExit, g, g.Exit);

                            Point e = new Point(final.X - _selection.Origin.X, final.Y - _selection.Origin.Y);
                            g.Exit = Globals.ActiveDrawing.PaperToModelDelta(ivm.Transform(e));
                        }
                        else if (_selectedHandleId == cStartMarker)
                        {
                            // move entry
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveGroupEntry, g, g.Entry);

                            Point e = new Point(final.X - _selection.Origin.X, final.Y - _selection.Origin.Y);
                            g.Entry = Globals.ActiveDrawing.PaperToModelDelta(ivm.Transform(e));
                        }

                        g.InsertLocation = GroupInsertLocation.Origin;
                    }

                    Globals.ActiveDrawing.UpdateGroupInstances(g.Name);
                }
            }
            else if (_selection is PInstance && Globals.EditSubCommand == EditSubCommand.AddMember)
            {
                Primitive p = Globals.ActiveDrawing.Pick(_start.X, _start.Y, false);
                Group g = Globals.ActiveDrawing.Groups[((PInstance)_selection).GroupName];
                if (p != null && g != null && p != _selection)
                {
                    if (p is PInstance && ((PInstance)p).GroupName == g.Name)
                    {
                        // don't add an instance of the same group
                        // that would be bad
                    }
                    else
                    {
                        double dx = p.Origin.X - _selection.Origin.X;
                        double dy = p.Origin.Y - _selection.Origin.Y;
                        Primitive m = p.Clone();
                        m.MoveTo(dx, dy);
                        int index = g.AddMember(m);
                        SelectInstance(_selection as PInstance, index);

                        Globals.CommandDispatcher.AddUndoableAction(ActionID.DeleteGroupMember, _selection, _member, _memberIndex);
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, p);
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, 2);

                        Globals.ActiveDrawing.DeletePrimitive(p);
                        Globals.ActiveDrawing.UpdateGroupInstances(g.Name);
                    }
                }
            }
            else
            {
                if (_selection == null)
                {
                    Primitive p = Globals.ActiveDrawing.Pick(_start.X, _start.Y, false);
                    if (p is PInstance)
                    {
                        int memberIndex = Globals.DrawingCanvas.VectorListControl.GetMemberIndex(p.Id, _start);
                        SelectInstance(p as PInstance, memberIndex);
                    }
                    else if (p != null)
                    {
                        // select a primitive
                        SelectPrimitive(p);
                    }
                    else
                    {
                        // no selection
                        ClearSelection();
                    }
                }
                else if (_selection is PInstance)
                {
                    int memberIndex = Globals.DrawingCanvas.VectorListControl.GetMemberIndex(_selection.Id, new Point(x, y));
                    SelectInstance(_selection as PInstance, memberIndex);
                }
                else if (_selection is Primitive)
                {
                    SelectPrimitive(_selection);
                }

                //if (_selection != null)
                //{
                //    if (_memberIndex >= 0)
                //    {
                //        if (Globals.EditSubCommand == EditSubCommand.DeleteMember)
                //        {
                //            Group g = Globals.ActiveDrawing.Groups[_selection.GroupName];
                //            Globals.CommandDispatcher.AddUndoableAction(ActionID.AddGroupMember, g, _member, _memberIndex);
                //            g.RemoveMemberAt(_memberIndex);
                //            Globals.ActiveDrawing.UpdateGroupInstances(g.Name);
                //            _memberIndex = -1;
                //            _member = null;
                //        }
                //    }
                //    else
                //    {
                //        // no member selected
                //    }
                //}
            }

            Globals.Input.SelectCursor(CursorType.Hand);

            _isTracking = false;
        }

        private Primitive GetMemberByIndex(int index)
        {
            Primitive member = null;

            if (_selection is PInstance && index >= 0)
            {
                Group g = Globals.ActiveDrawing.Groups[((PInstance)_selection).GroupName];
                if (index < g.Items.Count)
                {
                    member = g.Items[index];
                }
                else
                {

                }
            }

            return member;
        }

        public override Point Step(double dx, double dy, bool stillDown)
        {
            Point paper = base.Step(dx, dy, stillDown);

            if (_selectedHandleId > 0)
            {
                ShowConstructHandles = true;

                _handles.MoveHandle(_selectedHandleId, dx, dy);

                Globals.Events.PrimitiveSelectionSizeChanged(null);

                _handles.Select(_selectedHandleId);

                paper = _handles.SelectedHandle.Location;
            }
            else if (_selection is PInstance && _member != null && Globals.EditSubCommand == EditSubCommand.MoveMember)
            {
                Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                if (g != null)
                {
                    if (stillDown == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.MoveGroupMember, g, _memberIndex, _member.Origin);
                    }
                    MoveMemberByDelta(dx, dy);
                    Globals.ActiveDrawing.UpdateGroupInstances(g.Name);
                    _handles.Detach();
                }
            }

            return paper;
        }

        private void MoveMemberByDelta(double dx, double dy)
        {
            if (_selection is PInstance && _memberIndex >= 0)
            {
                Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                if (g != null)
                {
                    g.MoveMemberAt(_memberIndex, dx, dy);
                    _selection.Draw();
                }
            }
        }

        public override void UndoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            switch (actionId)
            {
                case ActionID.MultiUndo:
                    break;

                case ActionID.DeleteGroupMember:
                    if (subject is Group)
                    {
                        Group g = subject as Group;
                        Globals.ActiveDrawing.UpdateGroupInstances(g.Name);
                        Deselect();
                    }
                    else if (subject is PInstance)
                    {
                        Group g = Globals.ActiveDrawing.GetGroup(((PInstance)subject).GroupName);
                        Globals.ActiveDrawing.UpdateGroupInstances(g.Name);
                        SelectInstance(subject as PInstance, -1);
                    }
                    break;

                case ActionID.AddGroupMember:
                    if (subject is Group)
                    {
                        Group g = subject as Group;
                        Globals.ActiveDrawing.UpdateGroupInstances(g.Name);
                        if (_selection is PInstance && ((PInstance)_selection).GroupName == g.Name && predicate2 is int)
                        {
                            SelectInstance(_selection as PInstance, (int)predicate2);
                        }
                        else
                        {
                            Deselect();
                        }
                    }
                    break;

                case ActionID.MoveGroupMember:
                    if (subject is Group)
                    {
                        Group g = subject as Group;
                        Globals.ActiveDrawing.UpdateGroupInstances(g.Name);
                    }
                    break;

                case ActionID.MoveVertex:
                    if (subject == _selection)
                    {
                        UpdateHandles();
                        _selection.Draw();
                    }
                    else if (subject is PInstance)
                    {
                        SelectInstance(subject as PInstance, -1);
                        UpdateHandles();
                    }
                    else if (subject is Group)
                    {
                        UpdateHandles();
                    }
                    break;

                case ActionID.DeletePrimitive:
                    break;

                default:
                    if (subject == _selection)
                    {
                        UpdateHandles();
                        _selection.Draw();
                    }
                    else if (subject is PInstance)
                    {
                        Globals.ActiveDrawing.UpdateGroupInstances(((PInstance)subject).GroupName);
                        SelectInstance(subject as PInstance, -1);
                        UpdateHandles();
                    }
                    else if (subject is Primitive)
                    {
                        object o = ((Primitive)subject).Parent;
                        if (o is PInstance)
                        {
                            Globals.ActiveDrawing.UpdateGroupInstances(((PInstance)o).GroupName);
                            SelectInstance(o as PInstance, -1);
                            UpdateHandles();
                        }
                        else if (o is Group)
                        {
                            Globals.ActiveDrawing.UpdateGroupInstances(((Group)o).Name);
                            UpdateHandles();
                        }
                    }
                    else if (subject is Group)
                    {
                        Globals.ActiveDrawing.UpdateGroupInstances(((Group)subject).Name);
                        UpdateHandles();
                    }
                    break;
            }
        }

        public override void RedoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            UndoNotification(actionId, subject, predicate, predicate2);
        }

        public void UpdateHandles()
        {
            if (_selection is PInstance)
            {
                if (Globals.EditSubCommand == EditSubCommand.Properties)
                {
                    _handles.Detach();
                }
                else if (Globals.EditSubCommand == EditSubCommand.MemberProperties)
                {
                    _handles.Detach();
                }
                else
                {
                    _handles.Detach();
                    if (Globals.EditSubCommand == EditSubCommand.MovePoint)
                    {
                        if (Globals.ActiveDrawing.Groups.ContainsKey(((PInstance)_selection).GroupName))
                        {
                            Group g = Globals.ActiveDrawing.Groups[((PInstance)_selection).GroupName];

                            //_handles.Attach(pi);
                            _handles.AddHandle(cOriginMarker, _selection.Origin.X, _selection.Origin.Y, HandleType.Triangle);

                            Point entry = Globals.ActiveDrawing.ModelToPaperDelta(g.Entry);
                            entry = _selection.Matrix.Transform(entry);
                            entry.X += _selection.Origin.X;
                            entry.Y += _selection.Origin.Y;                   

                            Point exit = Globals.ActiveDrawing.ModelToPaperDelta(g.Exit);
                            exit = _selection.Matrix.Transform(exit);
                            exit.X += _selection.Origin.X;
                            exit.Y += _selection.Origin.Y;

                            _handles.AddHandle(cEndMarker, exit.X, exit.Y, HandleType.Diamond);
                            _handles.AddHandle(cStartMarker, entry.X, entry.Y, HandleType.Diamond);

                            _handles.ArrowFrom = cStartMarker;
                            _handles.ArrowTo = cEndMarker;

                            _handles.Draw();
                        }
                    }
                }

                HighlightSelection(true);
            }
        }


        private void HighlightSelection(bool flag)
        {
            _highlightedMemberIndex = -1;

            if (_selection is PInstance)
            {
                if (flag == false)
                {
                    _selection.Highlight(false);
                }
                else if (Globals.EditSubCommand == Commands.EditSubCommand.MemberProperties)
                {
                    _highlightedMemberIndex = _memberIndex;
                    ((PInstance)_selection).HighlightMember(_highlightedMemberIndex);
                }
                else if (Globals.EditSubCommand == Commands.EditSubCommand.MoveMember)
                {
                    _highlightedMemberIndex = _memberIndex;
                    ((PInstance)_selection).HighlightMember(_highlightedMemberIndex);
                }
                else if (Globals.EditSubCommand == Commands.EditSubCommand.DeleteMember)
                {
                    _highlightedMemberIndex = _memberIndex;
                    ((PInstance)_selection).HighlightMember(_highlightedMemberIndex);
                }
                else
                {
                    _selection.Highlight(true);
                }
            }
            else if (_selection is Primitive)
            {
                _selection.Highlight(flag);
            }
        }

        public override void CanvasScrolled()
        {
            base.CanvasScrolled();
        }

        public override void CanvasScaleChanged()
        {
            _handles.Draw();

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

            _isTracking = false;
            _isDragging = false;

            //_primitive = null;

            if (_selection != null)
            {
                _selection.ConstructEnabled = true;
                HighlightSelection(false);
                _selection = null;

                Globals.Events.PrimitiveSelectionChanged(null);
            }

            ShowConstructHandles = false;
        }

        public void SelectPrimitive(Primitive p)
        {
            Deselect();

            _selection = p;

            Globals.Events.PrimitiveSelectionChanged(_selection);
            Globals.Events.ShowContextMenu(p, "editgroup", -1);
        }

        public void ClearSelection()
        {
            Globals.Events.PrimitiveSelectionChanged(null);
            Globals.Events.ShowContextMenu(null, "editgroup");
        }

        public void SelectInstance(PInstance p, int memberIndex)
        {
            if (p != _selection)
            {
                Deselect();
            }

            if (p != null && memberIndex >= 0)
            {
                _selection = p;
                _memberIndex = memberIndex;
                _member = GetMemberByIndex(_memberIndex);

                ShowConstructHandles = _selection != null;

                p.ConstructEnabled = false;
            }

            Globals.Events.PrimitiveSelectionChanged(_selection);
            Globals.Events.ShowContextMenu(_selection, "editgroup", _memberIndex);

            UpdateHandles();
        }

        public void CreateGroupFromPrimitive()
        {
            if (_selection is Primitive)
            {
                Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, _selection);

                Group group = new Group(Globals.ActiveDrawing.UniqueGroupName(null));
                group.PaperUnit = Globals.ActiveDrawing.PaperUnit;
                group.ModelUnit = Globals.ActiveDrawing.ModelUnit;
                group.ModelScale = Globals.ActiveDrawing.Scale;
                group.MovePrimitivesFromDrawing(_selection.Origin.X, _selection.Origin.Y, new List<Primitive>() { _selection });
                group.InsertLocation = GroupInsertLocation.None;
                //group.IncludeInLibrary = group.Name.StartsWith(":") == false;
                group.Entry = new Point(0, 0);
                group.Exit = new Point(0, 0);

                Globals.ActiveDrawing.AddGroup(group);

                PInstance p = new PInstance(_selection.Origin, group.Name);
                p.LayerId = 0;      // Groups should be created on layer 0 (unassigned) regardless of the active layer setting
                p.AddToContainer(Globals.ActiveDrawing);
                p.Draw();

                Gleam gleam = new Gleam(new List<Primitive>() { p });
                gleam.Start();

                Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, p);
                Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, 2);

                _selection = null;

                SelectInstance(p, 0);
            }
        }
    }
}
