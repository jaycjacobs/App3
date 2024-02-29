using System.Collections.Generic;
using Cirros.Actions;
using Cirros.Display;
using Cirros.Primitives;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Cirros.Utility;
using System;
using Cirros.Core;
using Cirros8;
using Cirros.Drawing;
using Windows.ApplicationModel.DataTransfer;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using System.IO.Pipes;
using System.Text;

namespace Cirros.Commands
{
    public class SelectCommand : SelectorBase
    {
        public SelectCommand()
        {
            _type = CommandType.select;
            _undoBase = Globals.CommandDispatcher.UndoCount;
            if (Globals.CommandProcessorParameter is Primitive p)
            {
                AddToSelectedElements(p);
                _origin = p.Origin;
            }
            else if (Globals.CommandProcessorParameter is List<Primitive> list)
            {
                AddToSelectedElements(list);
                _origin = new Point((_itemBoxUnion.Left + _itemBoxUnion.Right) / 2, (_itemBoxUnion.Top + _itemBoxUnion.Bottom) / 2);
            }
            else
            {
                AddToSelectedElements((Primitive)null);
            }
        }

        int _undoBase = 0;

        Rect _itemBoxUnion = Utilities.EmptyRect;

        double _cx = 0;
        double _cy = 0;

        Point _refPoint;
        bool _isRotating = false;

        bool _isStepping = false;

        List<Primitive> _undoPrimitveSet = null;

        public override void Invoke(object o, object parameter)
        {
            _isRotating = false;
            _isStepping = false;
            //_undoPrimitveSet = null;

            //if (o is string && _selectedElements.Count > 0)
            if (o is string)
            {
                if (_selectedElements.Count == 0)
                {
                    // watch for bad behavior
                }

                string option = o as string;

                switch (option)
                {
                    case "A_ClearSelection":
                        Deselect();
                        break;

                    case "A_SelectMove":
                        if (parameter is Point)
                        {
                            //Globals.SelectMoveOffsetX = ((Point)parameter).X;
                            //Globals.SelectMoveOffsetY = ((Point)parameter).Y;
                            Globals.SelectSubCommand = SelectSubCommand.MoveOffset;
                        }
                        else
                        {
                            Globals.SelectSubCommand = SelectSubCommand.Move;
                        }
                        EnableSelectionConstructionPoints(true);
                        _handles.Clear();
                        SetSelectionOrigin(_origin);
                        ShowHandles();
                        break;

                    case "A_SelectMoveSelection":
                        if (parameter is Point)
                        {
                            Globals.SelectMoveOffsetX = ((Point)parameter).X;
                            Globals.SelectMoveOffsetY = ((Point)parameter).Y;
                        }
                        _selectedHandleId = -1;

                        MoveSelection((Point)parameter);

                        _handles.Clear();
                        ShowHandles();
                        break;

                    case "A_SelectCopySelection":
                        if (parameter is Point)
                        {
                            Globals.SelectMoveOffsetX = ((Point)parameter).X;
                            Globals.SelectMoveOffsetY = ((Point)parameter).Y;
                        }

                        CopySelection((Point)parameter);

                        _handles.Clear();
                        ShowHandles();
                        break;

                    case "A_SelectCopy":
                        if (parameter is Point)
                        {
                            Globals.SelectMoveOffsetX = ((Point)parameter).X;
                            Globals.SelectMoveOffsetY = ((Point)parameter).Y;
                            Globals.SelectSubCommand = SelectSubCommand.CopyOffset;
                        }
                        else
                        {
                            Globals.SelectSubCommand = SelectSubCommand.Copy;
                        }
                        _selectedHandleId = -1;

                        EnableSelectionConstructionPoints(true);
                        _handles.Clear();
                        SetSelectionOrigin(_origin);
                        ShowHandles();
                        break;

                    case "A_SelectDelete":
                        DeleteSelection();
                        break;

                    case "A_SelectLayer":
                        if (_selectedElements.Count > 0)
                        {
                            Layer layer = parameter as Layer;
                            foreach (Primitive p in _selectedElements)
                            {
                                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetLayer, p, p.LayerId);
                                p.LayerId = layer.Id;
                                p.Draw();
                            }
                            if (_selectedElements.Count > 1)
                            {
                                Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count);
                            }
                        }
                        break;

                    case "A_SelectRotate":
                        Globals.SelectSubCommand = SelectSubCommand.Rotate;
                        EnableSelectionConstructionPoints(true);
                        _handles.Clear();
                        ShowHandles();
                        break;

                    case "A_SelectScale":
                        Globals.SelectSubCommand = SelectSubCommand.Scale;
                        _handles.Clear();
                        ShowHandles();
                        break;

                    case "A_SelectGroup":
                        CreateGroupFromSelection();
                        break;

                    case "A_SelectPivot":
                        Globals.SelectSubCommand = SelectSubCommand.Pivot;
                        _handles.Clear();
                        ShowHandles();
                        break;

                    case "A_SelectFlipH":
                        ScaleSelection(-1, 1);
                        UpdateBox();
                        break;

                    case "A_SelectFlipV":
                        ScaleSelection(1, -1);
                        UpdateBox();
                        break;

                    case "A_SelectTransform":
                        if (parameter is double[])
                        {
                            double[] values = parameter as double[];
                            TransformSelection(values[0], values[1], values[2]);
                            UpdateBox();
                        }
                        break;

                    case "A_SelectResetTransform":
                        UntransformSelection();
                        UpdateBox();
                        break;

                    case "A_SelectProperties":
                        Globals.SelectSubCommand = SelectSubCommand.Properties;
                        //Globals.Events.ShowProperties(_selectedElements);

                        _handles.Clear();
                        ShowHandles();
                        break;

                    case "A_SelectUngroup":
                        if (_selectedElements.Count == 1 && _selectedElements[0] is PInstance)
                        {
                            Utilities.UnGroup(_selectedElements[0] as PInstance, true);
                            Deselect();
                        }
                        break;

                    case "A_EditLast":
                        if (_selectedElements.Count == 1)
                        {
                            _lastObject = _selectedElements[0];
                            Deselect();

                            Globals.CommandProcessorParameter = _lastObject;
                            Globals.CommandDispatcher.ActiveCommand = CommandType.edit;
                            Globals.ReturnToCommand = _type;
                        }
                        break;

                    case "A_SelectCopyToClipboard":
                        CopyToClipboard();
                        break;

                    case "A_SelectPaste":
                        Paste();
                        break;
                }

                ShowConstructHandles = option == "A_SelectMove" || option == "A_SelectCopy" || option == "A_SelectRotate";
            }
        }

        public override void Finish()
        {
            Globals.Events.ShowContextMenu(null, null);

            base.Finish();
        }

        public override void RedoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            UndoNotification(actionId, subject, predicate, predicate2);

            base.RedoNotification(actionId, subject, predicate, predicate2);
        }

        public override void UndoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            if (Globals.CommandDispatcher.UndoCount >= _undoBase)
            {
                switch (actionId)
                {
                    case ActionID.MultiUndo:
                        if (_undoPrimitveSet != null)
                        {
                            foreach (Primitive p in _undoPrimitveSet)
                            {
                                AddToSelectedElements(p);
                            }
                            UpdateBox();
                            SetSelectionOrigin(new Point((_itemBoxUnion.Left + _itemBoxUnion.Right) / 2, (_itemBoxUnion.Top + _itemBoxUnion.Bottom) / 2));
                            ShowHandles();

                            _undoPrimitveSet.Clear();
                            _undoPrimitveSet = null;
                        }
                        else
                        {
                            _handles.Clear();
                            ShowHandles();
                            ClearSelection();
                            Globals.Events.ShowContextMenu(null, "select");
                        }
                        break;

                    case ActionID.DeletePrimitive:
                        break;

                    case ActionID.RestorePrimitive:
                        break;

                    case ActionID.RestoreMatrix:
                        if (subject is Primitive)
                        {
                            if (_undoPrimitveSet == null)
                            {
                                _undoPrimitveSet = new List<Primitive>();
                            }
                            _undoPrimitveSet.Add(subject as Primitive);
                            UpdateBox();
                        }
                        break;

                    case ActionID.Move:
                    case ActionID.UnNormalize:
                    case ActionID.MoveVertex:
                        if (subject is Primitive)
                        {
                            if (_undoPrimitveSet == null)
                            {
                                //Deselect();
                                //ClearSelection();

                                _undoPrimitveSet = new List<Primitive>();
                            }
                            _undoPrimitveSet.Add(subject as Primitive);
                            //AddToSelectedElements(subject as Primitive);
                        }
                        break;
                }
            }
            else
            {
                ClearSelection();
                Globals.Events.ShowContextMenu(null, "select");
            }
        }

        public override bool EnableCommand(object o)
        {
            bool enable = false;

            if (o is string && _selectedElements.Count > 0)
            {
                switch ((string)o)
                {
                    case "A_SelectScale":
                    case "A_SelectMove":
                    case "A_SelectRotate":
                    case "A_SelectFlipH":
                    case "A_SelectFlipV":
                    case "A_ClearSelection":
                    case "A_SelectDelete":
                    case "A_SelectCopy":
                    case "A_SelectFlip":
                    case "A_SelectTransform":
                    case "A_SelectResetTransform":
                    case "A_SelectProperties":
                        enable = true;
                        break;

                    case "A_SelectUngroup":
                        enable = _selectedElements.Count == 1 && _selectedElements[0] is PInstance;
                        break;
                }
            }

            return enable;
        }

        public override void KeyDown(string key, bool shift, bool control, bool gmk)
        {
            base.KeyDown(key, shift, control, gmk);

            if (gmk)
            {
                if (key == "delete")
                {
                    this.DeleteSelection();
                }
                else if (key == "n" && _handles.Count > 0)
                {
                    _selectedHandleId = _handles.SelectNext(_selectedHandleId);
                }
                else if (control && key == "a")
                {
                    SelectAll();
                }
                else if (control && key == "c")
                {
                    CopyToClipboard();
                }
                else if (control && key == "v")
                {
                    Paste();
                }
            }
        }

        private void CopyToClipboard()
        {
            if (_selectedElements.Count > 0)
            {
                Guid guid = Guid.NewGuid();
                Group group = new Group(guid.ToString());
                group.PaperUnit = Globals.ActiveDrawing.PaperUnit;
                group.ModelUnit = Globals.ActiveDrawing.ModelUnit;
                group.ModelScale = Globals.ActiveDrawing.Scale;
                group.MovePrimitivesFromDrawing(_origin.X, _origin.Y, _selectedElements, false);
                group.InsertLocation = GroupInsertLocation.None;
                group.Entry = new Point(0, 0);
                group.Exit = new Point(0, 0);

                string s = FileHandling.SerializeGroup(group);

                DataPackage dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetData("dbsx", s);
                Clipboard.SetContent(dataPackage);
            }
        }

        private async void Paste()
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains("dbsx"))
            {
                object o = await dataPackageView.GetDataAsync("dbsx");
                if (o is string s)
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(s);
                    MemoryStream stream = new MemoryStream(byteArray);
                    Group symbol = await FileHandling.GetSymbolFromStreamAsync(stream);
                    //Globals.ActiveDrawing.AddGroup(symbol);

                    PInstance p = new PInstance(_origin, symbol.Name);
                    p.LayerId = 0;      // Groups should be created on layer 0 (unassigned) regardless of the active layer setting
                    p.AddToContainer(Globals.ActiveDrawing);

                    Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, p);

                    Deselect();
                    AddToSelectedElements(p);
                    Globals.SelectSubCommand = SelectSubCommand.Move;

                    Move(Globals.xSnap, Globals.ySnap);
                }
            }
        }

        protected override void SelectAll()
        {
            ClearSelection();

            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                if (Globals.LayerTable.ContainsKey(p.LayerId))
                {
                    Layer layer = Globals.LayerTable[p.LayerId];
                    if (layer.Visible)
                    {
                        _selectedElements.Add(p);
                        p.ConstructEnabled = false;
                        p.IsDynamic = true;
                        p.Highlight(true);

                        if (_itemBoxUnion.IsEmpty)
                        {
                            _itemBoxUnion = p.Box;
                        }
                        else
                        {
                            _itemBoxUnion.Union(p.Box);
                        }
                    }
                }
            }

            Globals.Events.PrimitiveSelectionChanged(_selectedElements);
            Globals.Events.ShowContextMenu(_selectedElements, "select");
        }

        protected override void AddToSelectedElements(Primitive p)
        {
            if (p == null)
            {
                Globals.Events.ShowContextMenu(null, "select");
            }
            else if (_selectedElements.Contains(p))
            {

            }
            else
            {
                base.AddToSelectedElements(p);

                if (_itemBoxUnion.IsEmpty)
                {
                    _itemBoxUnion = p.Box;
                }
                else
                {
                    _itemBoxUnion.Union(p.Box);
                }

                Globals.Events.ShowContextMenu(_selectedElements, "select");
            }
        }

        protected void AddToSelectedElements(List<Primitive> list)
        {
            if (list == null)
            {
                Globals.Events.ShowContextMenu(null, "select");
            }
            else
            {
                foreach (Primitive p in list)
                {
                    if (_selectedElements.Contains(p) == false)
                    {
                        base.AddToSelectedElements(p);

                        if (_itemBoxUnion.IsEmpty)
                        {
                            _itemBoxUnion = p.Box;
                        }
                        else
                        {
                            _itemBoxUnion.Union(p.Box);
                        }
                    }
                }

                Globals.Events.ShowContextMenu(_selectedElements, "select");
            }
        }

        protected override void Pick(Rect r)
        {
            base.Pick(r);

            // sort the list by z-index - important for creating groups
            _selectedElements.Sort();

            SetSelectionOrigin(new Point((_itemBoxUnion.Left + _itemBoxUnion.Right) / 2, (_itemBoxUnion.Top + _itemBoxUnion.Bottom) / 2));
        }

        protected override void startTrackingSelection(bool control)
        {
            if (control || (Globals.SelectSubCommand == SelectSubCommand.Copy && _selectedHandleId < 0))
            {
                // If the control key is down, copy the current selection
                _handles.Deselect();

                CopySelection(new Point(0, 0));
               
                Globals.Input.SelectCursor(CursorType.Hand);
            }
            else
            {
                // If the control key is not down, move the selection

                if (_selectedHandleId == 1000)
                {
                    // drag the pivot point
                    ShowConstructHandles = true;
                }
                else if (_selectedHandleId > 0)
                {
                    //_selectedHandle = _handles.SelectedHandleID;

                    // If a handle (other than the pivot) is selected, operation is transform
                    AddUndoableTransformation();
                    Globals.Input.SelectCursor(CursorType.Draw);
                }
                else if (_selectedElements.Count > 0)
                {
                    // If no handle is selected, move the object
                    foreach (Primitive p in _selectedElements)
                    {
                        p.ConstructEnabled = false;
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.Move, p, p.Origin);
                    }

                    if (_selectedElements.Count > 1)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count);
                    }
                    Globals.Input.SelectCursor(CursorType.Draw);
                }
                else
                {
                    Globals.Input.SelectCursor(CursorType.Hand);
                }
            }
        }

        bool _operationIsTransform = false;

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            _isStepping = false;
            _selectedHandleId = -1;

            if (Globals.Input.ObjectSnap && shift == false)
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

            if (Globals.SelectSubCommand == SelectSubCommand.Rotate)
            {
                // This shouldn't require two cases - need to fix for 1.1
                // but this way rotate and move allow objects to be dragged by handle points
                if (_selectedElements.Count > 0)
                {
                    EnableSelectionConstructionPoints(false);
                }

                base.StartTracking(x, y, shift, control);
            }
            else if (Globals.SelectSubCommand == SelectSubCommand.Pivot)
            {
                // self snapping is a good thing when dragging the pivot
                base.StartTracking(x, y, shift, control);
            }
            else if (Globals.SelectSubCommand == SelectSubCommand.MoveOffset || Globals.SelectSubCommand == SelectSubCommand.CopyOffset)
            {
                // self snapping is a good thing when moving or copying by offset
                base.StartTracking(x, y, shift, control);
            }
            else
            {
                base.StartTracking(x, y, shift, control);

                if (_selectedElements.Count > 0)
                {
                    EnableSelectionConstructionPoints(false);
                }
            }
        }

        public override void EndTracking(double x, double y)
        {
            if (Globals.Input.ObjectSnap && _shiftKey == false)
            {
                //Point trianglePoint;

                if (_constructHandles.SelectedHandleID >= 0)
                {
                    double dx = _constructHandles.SelectedHandle.Location.X - _lastTrackX;
                    double dy = _constructHandles.SelectedHandle.Location.Y - _lastTrackY;
                    Move(dx, dy);
                }
                else if (Globals.DrawingTools.ActiveTrianglePoint(out Point trianglePoint))
                {
                    double dx = trianglePoint.X - _lastTrackX;
                    double dy = trianglePoint.Y - _lastTrackY;
                    Move(dx, dy);
                }
            }

            Globals.Input.SelectCursor(CursorType.Hand);

            base.EndTracking(x, y);

            if (_selectedElements.Count > 0)
            {
                if (Globals.SelectSubCommand == SelectSubCommand.Move || Globals.SelectSubCommand == SelectSubCommand.Copy || Globals.SelectSubCommand == SelectSubCommand.Rotate)
                {
                    ShowConstructHandles = true;
                }

                if (!_isRotating)
                {
                    EnableSelectionConstructionPoints(true);
                }

                if (_operationIsTransform)
                {
                    if (Globals.EnableBetaFeatures)
                    {
                        // undo is flakey in this scenaro
                        // if the object previously had a non-identity matrix
                        int count = 0;
                        foreach (Primitive p in _selectedElements)
                        {
                            if (p.Normalize(true))
                            {
                                count++;
                            }
                        }
                        if (count > 1)
                        {
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, count);
                        }
                    }

                    _operationIsTransform = false;
                }
            }

            UpdateBox();
        }

        public override Point Step(double dx, double dy, bool stillDown)
        {
            Point paper = base.Step(dx, dy, stillDown);

            if (_selectedHandleId >= 0)
            {
                if (_isStepping == false)
                {
                    _isStepping = true;
                    AddUndoableTransformation();
                }

                _handles.Select(_selectedHandleId);

                if (_handles.SelectedHandle == null)
                {
                    _selectedHandleId = -1;
                }
                else
                {
                    if (_isRotating)
                    {
                        // This is a bit of a hack, but produces the expected behavior.
                        // When rotating, coordinate display shows the angle of the reference line, so if a distance/angle is entered
                        // the expected behavior would be to move the reference line to that angle.
                        // To accomplish this, we need to move the corresponding handle to the origin before applying the delta

                        _handles.MoveHandle(_selectedHandleId, _origin.X - _handles.SelectedHandle.Location.X, _origin.Y - _handles.SelectedHandle.Location.Y);
                    }

                    this.Move(dx, dy);

                    paper = _handles.SelectedHandle.Location;
                }

            }
            else if (_selectedElements.Count > 0)
            {
                if (_isStepping == false)
                {
                    _isStepping = true;

                    foreach (Primitive p in _selectedElements)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.Move, p, p.Origin);
                    }

                    if (_selectedElements.Count > 1)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count);
                    }
                }

                this.Move(dx, dy);
            }

            return paper;
        }

        public override void SetSelectionOrigin(Point p)
        {
            base.SetSelectionOrigin(p);

            if (Globals.SelectSubCommand == SelectSubCommand.MoveOffset || Globals.SelectSubCommand == SelectSubCommand.CopyOffset)
            {
                _refPoint.X = _origin.X + Globals.SelectMoveOffsetX;
                _refPoint.Y = _origin.Y + Globals.SelectMoveOffsetY;
            }
        }
        
        public override void ClearSelection()
        {
            base.ClearSelection();
            _itemBoxUnion = Utilities.EmptyRect;
            _undoPrimitveSet = null;
        }

        protected override void Deselect()
        {
            EnableSelectionConstructionPoints(true);

            base.Deselect();

            _handles.Deselect();
            _isRotating = false;
        }

        protected void ScaleSelection(double xscale, double yscale)
        {
            if (_selectedElements.Count > 0)
            {
                foreach (Primitive p in _selectedElements)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestoreMatrix, p, p.Origin, p.Matrix);
                    p.Scale(_origin, xscale, yscale);
                }

                if (_selectedElements.Count > 1)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count);
                }
            }
        }

        protected void TransformSelection(double xscale, double yscale, double angle)
        {
            if (_selectedElements.Count > 0)
            {
                if (angle == 0)
                {
                    int count = 0;
                    foreach (Primitive p in _selectedElements)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.RestoreMatrix, p, p.Origin, p.Matrix);
                        p.Scale(_origin, xscale, yscale);
                        count++;

                        if (p.Normalize(true))
                        {
                            count++;
                        }
                    }
                    if (count > 1)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, count);
                    }
                }
                else
                {
                    foreach (Primitive p in _selectedElements)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.RestoreMatrix, p, p.Origin, p.Matrix);
                        p.Scale(_origin, xscale, yscale);
                        p.Rotate(_origin, angle);
                    }

                    if (_selectedElements.Count > 1)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count);
                    }
                }
            }
        }

        protected void UntransformSelection()
        {
            int count = 0;

            if (_selectedElements.Count > 0)
            {
                foreach (Primitive p in _selectedElements)
                {
                    if (p.Matrix.IsIdentity == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.RestoreMatrix, p, p.Origin, p.Matrix);
                        p.Matrix = CGeometry.IdentityMatrix();
                        p.Draw();
                        count++;
                    }
                }

                if (count > 1)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count);
                }
            }
        }

        protected void MoveSelection(Point offset)
        {
            if (_selectedElements.Count > 0)
            {
                foreach (Primitive p in _selectedElements)
                {
                    //p.ConstructEnabled = false;
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.Move, p, p.Origin);
                }

                if (_selectedElements.Count > 1)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count);
                }

                this.Move(offset.X, offset.Y);
            }
        }

        protected void CopySelection(Point offset)
        {
            if (_selectedElements.Count > 0)
            {
                List<Primitive> list = new List<Primitive>();

                foreach (Primitive p in _selectedElements)
                {
                    list.Add(p);
                }

                ClearSelection();

                SetSelectionOrigin(new Point(_origin.X + offset.X, _origin.Y + offset.Y));

                foreach (Primitive p in list)
                {
                    Primitive copy = p.Clone();
                    copy.ZIndex = p.ZIndex + 1;
                    copy.MoveByDelta(offset.X, offset.Y);
                    copy.AddToContainer(Globals.ActiveDrawing);
                    AddToSelectedElements(copy);
                }

                foreach (Primitive p in _selectedElements)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, p);
                }

                if (_selectedElements.Count > 1)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count);
                }
            }
        }

        protected void DeleteSelection()
        {
            if (_selectedElements.Count > 0)
            {
                _handles.Clear();

                foreach (Primitive p in _selectedElements)
                {
                    p.IsDynamic = false;
                    p.Highlight(false);

                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, p);
                    Globals.ActiveDrawing.DeletePrimitive(p);
                }

                if (_selectedElements.Count > 1)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count);
                }

                // clear _selectedElements before calling ClearSelection()
                // the objects should not be unhighlighted -- they're already gone.
                _selectedElements.Clear();

                ClearSelection();

                Deselect();

                GC.Collect();
            }
        }

        protected void CreateGroupFromSelection()
        {
            if (_selectedElements.Count == 1 && _selectedElements[0] is PInstance)
            {
                // this is already a group
            }
            else if (_selectedElements.Count > 0)
            {
                foreach (Primitive p1 in _selectedElements)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, p1);
                }

                Group group = new Group(Globals.ActiveDrawing.UniqueGroupName(null));
                group.PaperUnit = Globals.ActiveDrawing.PaperUnit;
                group.ModelUnit = Globals.ActiveDrawing.ModelUnit;
                group.ModelScale = Globals.ActiveDrawing.Scale;
                group.MovePrimitivesFromDrawing(_origin.X, _origin.Y, _selectedElements);
                group.InsertLocation = GroupInsertLocation.None;
                //group.IncludeInLibrary = group.Name.StartsWith(":") == false;
                group.Entry = new Point(0, 0);
                group.Exit = new Point(0, 0);

                Globals.ActiveDrawing.AddGroup(group);

                PInstance p = new PInstance(_origin, group.Name);
                p.LayerId = 0;      // Groups should be created on layer 0 (unassigned) regardless of the active layer setting
                p.AddToContainer(Globals.ActiveDrawing);

                Gleam gleam = new Gleam(new List<Primitive>() { p });
                gleam.Start();
                
                Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, p);
                Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count + 1);

                ClearSelection();

                AddToSelectedElements(p);
            }
        }

        public void UpdateBox()
        {
            if (_selectedElements.Count > 0)
            {
                _itemBoxUnion = Utilities.EmptyRect;

                foreach (Primitive p in _selectedElements)
                {
                    if (_itemBoxUnion.IsEmpty)
                    {
                        _itemBoxUnion = p.Box;
                    }
                    else
                    {
                        _itemBoxUnion.Union(p.Box);
                    }
                }

                ShowHandles();
            }
            else
            {
                Globals.Events.ShowContextMenu(null, "select");
            }
        }

        protected override void Move(double dx, double dy)
        {
            if (dx != 0 || dy != 0)
            {
                if (_selectedHandleId > 0)
                {
                    _handles.MoveHandle(_selectedHandleId, dx, dy, _shiftKey);
                }
                else if (Globals.SelectSubCommand != SelectSubCommand.Rotate)
                {
                    _handles.Move(dx, dy);
                    SetSelectionOrigin(new Point(_origin.X + dx, _origin.Y + dy));

                    foreach (Primitive p in _selectedElements)
                    {
                        p.MoveByDelta(dx, dy);
                        p.Draw();
                    }
                }
            }
        }

        public void AddUndoableTransformation()
        {
            if (_selectedElements.Count > 0)
            {
                foreach (Primitive p in _selectedElements)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestoreMatrix, p, p.Origin, p.Matrix);
                }
                if (_selectedElements.Count > 1)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count);
                }
                _operationIsTransform = true;
            }
        }

        protected override bool PickSelection(double x, double y, double tolerance)
        {
            bool reselect = false;
            
            //double tol = Globals.View.DisplayToPaper(Globals.hitTolerance);

            if (_constructHandles.SelectedHandleID >= 0 && _selectedHandleId < 0 && _itemBoxUnion.Contains(_constructHandles.SelectedHandle.Location))
            {
                // If a snap point is selected while there is a selection
                // and the snap point lies within the selection box but is not on the selection
                // make the snap point the anchor point

                reselect = true;

                // base.PickSelection sets _selectedHandleId
                _selectedHandleId = _handles.Hit(x, y);

            }
            else if (base.PickSelection(x, y, tolerance * 2))
            {
                reselect = true;
            }

            if (reselect)
            {
                switch (Globals.SelectSubCommand)
                {
                    case SelectSubCommand.Rotate:
                        if (_selectedHandleId == 1000)
                        {
                            // Move the pivot
                            EnableSelectionConstructionPoints(true);
                            _isRotating = false;
                        }
                        else if (_shiftKey == false)
                        {
                            reselect = true;

                            _refPoint = new Point(x, y);
                            _isRotating = true;
                            EnableSelectionConstructionPoints(false);
                            ShowHandles();
                            _selectedHandleId = _handles.Hit(x, y);

                            _rubberBand = new RubberBandNone();
                        }
                        break;

                    case SelectSubCommand.Pivot:
                    case SelectSubCommand.MoveOffset:
                    case SelectSubCommand.CopyOffset:
                        if (_selectedHandleId == 1000)
                        {
                            // Move the pivot
                            EnableSelectionConstructionPoints(true);
                        }
                        break;
                }
            }

            return reselect;
        }

        public override void ShowHandles()
        {
            if (Globals.SelectSubCommand == SelectSubCommand.Scale)
            {
                if (!_itemBoxUnion.IsEmpty)
                {
                    _cx = (_itemBoxUnion.Left + _itemBoxUnion.Right) / 2;
                    _cy = (_itemBoxUnion.Top + _itemBoxUnion.Bottom) / 2;

                    _handles.Attach(this);
                    _handles.AddFrame(_itemBoxUnion);
                    _handles.AddHandle(2001, _itemBoxUnion.Left, _itemBoxUnion.Top);
                    _handles.AddHandle(2002, _itemBoxUnion.Right, _itemBoxUnion.Top);
                    _handles.AddHandle(2003, _itemBoxUnion.Right, _itemBoxUnion.Bottom);
                    _handles.AddHandle(2004, _itemBoxUnion.Left, _itemBoxUnion.Bottom);
                    _handles.AddHandle(2005, _itemBoxUnion.Left, _cy, HandleType.Square);
                    _handles.AddHandle(2006, _cx, _itemBoxUnion.Top, HandleType.Square);
                    _handles.AddHandle(2007, _itemBoxUnion.Right, _cy, HandleType.Square);
                    _handles.AddHandle(2008, _cx, _itemBoxUnion.Bottom, HandleType.Square);
                    _handles.Draw();
                }
            }
            else if (Globals.SelectSubCommand == SelectSubCommand.Rotate)
            {
                if (_selectedElements.Count > 0)
                {
                    _handles.Attach(this);
                    _handles.AddHandle(1000, _origin.X, _origin.Y, HandleType.Triangle);

                    if (_isRotating)
                    {
                        _handles.AddHandle(3000, _refPoint.X, _refPoint.Y, HandleType.Diamond);
                        _handles.ArrowTo = 3000;
                        _handles.ArrowFrom = 1000;
                    }
                    _handles.Draw();
                }
            }
            else if (Globals.SelectSubCommand == SelectSubCommand.Pivot)
            {
                if (_selectedElements.Count > 0)
                {
                    _handles.Attach(this);
                    _handles.AddHandle(1000, _origin.X, _origin.Y, HandleType.Triangle);
                    _handles.Draw();
                }
            }
            else if (Globals.SelectSubCommand == SelectSubCommand.MoveOffset || Globals.SelectSubCommand == SelectSubCommand.CopyOffset)
            {
                if (_selectedElements.Count > 0)
                {
                    _handles.Attach(this);
                    _handles.AddHandle(3000, _refPoint.X, _refPoint.Y, HandleType.Diamond);
                    _handles.AddHandle(1000, _origin.X, _origin.Y, HandleType.Triangle);
                    _handles.ArrowFrom = 1000;
                    _handles.ArrowTo = 3000;
                    _handles.Draw();
                }
            }
            else if (Globals.SelectSubCommand == SelectSubCommand.Move || Globals.SelectSubCommand == SelectSubCommand.Copy)
            {
                _handles.Deselect();
                _handles.Draw();
            }
            else if (Globals.SelectSubCommand == SelectSubCommand.CopyOffset)
            {
                if (_selectedElements.Count > 0)
                {
                    _handles.Attach(this);
                    _handles.AddHandle(1000, _origin.X, _origin.Y, HandleType.Triangle);
                    _handles.Draw();
                }
            }
            else if (Globals.SelectSubCommand == SelectSubCommand.Properties)
            {
                // no handles for properties
            }
            else
            {
                if (_selectedElements.Count > 0)
                {
                    _handles.Attach(this);
                    _handles.AddHandle(1000, _origin.X, _origin.Y, HandleType.Triangle);
                    _handles.Draw();
                }
            }
        }

        public override void MoveHandle(int id, double dx, double dy, bool shift)
        {
            double w1 = _itemBoxUnion.Width;
            double h1 = _itemBoxUnion.Height;
            double w2 = _itemBoxUnion.Width;
            double h2 = _itemBoxUnion.Height;
            double x2 = _itemBoxUnion.X;
            double y2 = _itemBoxUnion.Y;

            double da = 0;

            double ax = 0;
            double ay = 0;

            switch (id)
            {
                case 2001:  // top-left 
                    ax = _itemBoxUnion.Right;
                    ay = _itemBoxUnion.Bottom;
                    x2 += dx;
                    y2 += dy;
                    w2 -= dx;
                    h2 -= dy;
                    break;

                case 2002:
                    // top-right 
                    ax = _itemBoxUnion.Left;
                    ay = _itemBoxUnion.Bottom;
                    //x2 -= dx;
                    y2 += dy;
                    w2 += dx;
                    h2 -= dy;
                    break;

                case 2003:  // bottom-right 
                    ax = _itemBoxUnion.Left;
                    ay = _itemBoxUnion.Top;
                    w2 += dx;
                    h2 += dy;
                    break;

                case 2004:
                    // bottom-left 
                    ax = _itemBoxUnion.Right;
                    ay = _itemBoxUnion.Top;
                    x2 += dx;
                    w2 -= dx;
                    h2 += dy;
                    break;

                case 2005:
                    // left
                    ax = _itemBoxUnion.Right;
                    ay = _cy;
                    x2 += dx;
                    w2 -= dx;
                    break;

                case 2006:
                    // top
                    ax = _cx;
                    ay = _itemBoxUnion.Bottom;
                    y2 += dy;
                    h2 -= dy;
                    break;

                case 2007:
                    // right
                    ax = _itemBoxUnion.Left;
                    ay = _cy;
                    w2 += dx;
                    break;

                case 2008:
                    // bottom
                    ax = _cx;
                    ay = _itemBoxUnion.Top;
                    h2 += dy;
                    break;

                case 1000:
                    // origin/pivot
                    SetSelectionOrigin(new Point(_origin.X + dx, _origin.Y + dy));
                    break;

                case 3000:
                    // Rotate ref point
                    double a1 = Construct.Angle(_origin, _refPoint);
                    _refPoint.X += dx;
                    _refPoint.Y += dy;
                    da = Construct.Angle(_origin, _refPoint) - a1;
                    break;
            }


            if (Globals.SelectSubCommand == SelectSubCommand.Scale)
            {
                if (w2 > 0.01 && h2 > 0.01)
                {
                    if (_itemBoxUnion.X != x2 || _itemBoxUnion.Y != y2 ||_itemBoxUnion.Width != w2 || _itemBoxUnion.Height != h2)
                    {
                        _itemBoxUnion.X = x2;
                        _itemBoxUnion.Y = y2;
                        _itemBoxUnion.Width = w2;
                        _itemBoxUnion.Height = h2;

                        double xscale = w2 / w1;
                        double yscale = h1 == 0 ? 1 : h2 / h1;
                        //double yscale = shift ? xscale : h1 == 0 ? 1 : h2 / h1;

                        if (shift)
                        {
                            if (Math.Abs(dx) > Math.Abs(dy))
                            {
                                yscale = xscale;
                            }
                            else
                            {
                                xscale = yscale;
                            }
                        }

                        foreach (Primitive p in _selectedElements)
                        {
                            p.Scale(new Point(ax, ay), xscale, yscale);
                        }
                    }
                }
            }
            else if (Globals.SelectSubCommand == SelectSubCommand.Rotate)
            {
                if (da != 0)
                {
                    double d = Construct.Distance(_origin, _refPoint);
                    double a = Construct.Angle(_origin, _refPoint);
                    Globals.Events.CoordinateDisplay(_refPoint, _refPoint.X - _origin.X, _refPoint.Y - _origin.Y, d, a);

                    double degrees = da * Construct.cRadiansToDegrees;

                    foreach (Primitive p in _selectedElements)
                    {
                        p.Rotate(_origin, degrees);
                    }
                }
            }
            else if (Globals.SelectSubCommand == SelectSubCommand.MoveOffset || Globals.SelectSubCommand == SelectSubCommand.CopyOffset)
            {
                if (id == 3000)
                {
                    double xoff = _refPoint.X - _origin.X;
                    double yoff = _refPoint.Y - _origin.Y;
                    double d = Construct.Distance(_origin, _refPoint);
                    double a = Construct.Angle(_origin, _refPoint);
                    Globals.Events.CoordinateDisplay(_refPoint, xoff, yoff, d, a);
                    Globals.Events.MoveCopyOffsetChanged(new Point(xoff, yoff));
                }
            }

            ShowHandles();
        }
    }
}
