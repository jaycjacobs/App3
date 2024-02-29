using Cirros.Actions;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;

namespace Cirros.Commands
{
    public class CopyPasteCommand : SelectorBase
    {
        int _undoBase = 0;

        Rect _itemBoxUnion = Utilities.EmptyRect;

        double _cx = 0;
        double _cy = 0;

        Point _refPoint;
        //bool _isRotating = false;

        bool _isStepping = false;

        List<Primitive> _undoPrimitveSet = null;

        public CopyPasteCommand()
        {
            _type = CommandType.copypaste;
            _undoBase = Globals.CommandDispatcher.UndoCount;
            if (Globals.CommandProcessorParameter is Primitive)
            {
                AddToSelectedElements((Primitive)Globals.CommandProcessorParameter);
            }
            else if (Globals.CommandProcessorParameter is List<Primitive> list)
            {
                AddToSelectedElements(list);
            }
            else
            {
                AddToSelectedElements((Primitive)null);
            }
        }

        public override void Start()
        {
            base.Start();
            //Globals.Events.ShowContextMenu(null, "copypaste");

            Rect r = Globals.DrawingCanvas.VectorListControl.GetWindow();
            _origin.X = (r.Left + r.Width) / 2;
            _origin.Y = (r.Top + r.Height) / 2;
        }

        public override void Invoke(object o, object parameter)
        {
            //_isRotating = false;
            _isStepping = false;

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

                    case "A_SelectCopyToClipboard":
                        CopyToClipboard();
                        break;

                    case "A_SelectPaste":
                        if (parameter is bool paper)
                        {
                            Paste(paper);
                        }
                        break;
                }
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
                            //_handles.Clear();
                            //ShowHandles();
                            ClearSelection();
                            Globals.Events.ShowContextMenu(null, "copypaste");
                        }
                        break;
                }
            }
            else
            {
                ClearSelection();
                Globals.Events.ShowContextMenu(null, "copypaste");
            }
        }

        public override bool EnableCommand(object o)
        {
            bool enable = false;

            if (o is string && _selectedElements.Count > 0)
            {
                switch ((string)o)
                {
                    case "A_SelectPaste":
                        DataPackageView dataPackageView = Clipboard.GetContent();
                        enable = dataPackageView.Contains("dbsx");
                        break;

                    default:
                    case "A_SelectCopyToClipboard":
                        enable = _selectedElements.Count > 1;
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
                if (control && key == "a")
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

        private async void CopyToClipboard()
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

                StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("cdata", CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    fileStream.SetLength(0);

                    StreamWriter writer = new StreamWriter(fileStream);
                    await writer.WriteAsync(s);
                    writer.Flush();
                }


                DataPackage dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetData("dbsx", "cdata");
                Clipboard.SetContent(dataPackage);
            }
        }

        private async void Paste(bool pastePaperScale = false)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains("dbsx"))
            {
                try
                {
                    string s = "";

                    using (Stream fileStream = await ApplicationData.Current.TemporaryFolder.OpenStreamForReadAsync("cdata"))
                    {
                        System.IO.StreamReader reader = new System.IO.StreamReader(fileStream);
                        s = reader.ReadToEnd();
                        reader.Dispose();
                    }

                    if (s.Length > 0)
                    {
                        byte[] byteArray = Encoding.UTF8.GetBytes(s);
                        MemoryStream stream = new MemoryStream(byteArray);
                        Primitives.Group symbol = await FileHandling.GetSymbolFromStreamAsync(stream, pastePaperScale);
                        if (pastePaperScale)
                        {
                            symbol.CoordinateSpace = CoordinateSpace.Paper;
                            symbol.PreferredScale = 1;
                        }
                        else
                        {
                            symbol.CoordinateSpace = CoordinateSpace.Model;
                        }

                        PInstance p = new PInstance(_origin, symbol.Name);
                        p.Scale(_origin, symbol.PreferredScale, symbol.PreferredScale);
                        p.LayerId = 0;      // Groups should be created on layer 0 (unassigned) regardless of the active layer setting
                        p.AddToContainer(Globals.ActiveDrawing);

                        Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, p);

                        Deselect();
                        AddToSelectedElements(p);
                        Globals.SelectSubCommand = SelectSubCommand.Move;

                        Move(Globals.xSnap, -Globals.ySnap);
                    }
                }
                catch
                {
                    Clipboard.Clear();
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
            Globals.Events.ShowContextMenu(_selectedElements, "copypaste");
        }

        protected override void AddToSelectedElements(Primitive p)
        {
            if (p == null)
            {
                Globals.Events.ShowContextMenu(null, "copypaste");
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

                Globals.Events.ShowContextMenu(_selectedElements, "copypaste");
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

                Globals.Events.ShowContextMenu(_selectedElements, "copypaste");
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
            {
                if (_selectedElements.Count > 0)
                {
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
                if (Globals.DrawingTools.ActiveTrianglePoint(out Point trianglePoint))
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
                    EnableSelectionConstructionPoints(true);
            }

            UpdateBox();
        }

        public override Point Step(double dx, double dy, bool stillDown)
        {
            Point paper = base.Step(dx, dy, stillDown);

            if (_selectedElements.Count > 0)
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

            //_handles.Deselect();
            //_isRotating = false;
        }

        //protected void CopySelection(Point offset)
        //{
        //    if (_selectedElements.Count > 0)
        //    {
        //        List<Primitive> list = new List<Primitive>();

        //        foreach (Primitive p in _selectedElements)
        //        {
        //            list.Add(p);
        //        }

        //        ClearSelection();

        //        SetSelectionOrigin(new Point(_origin.X + offset.X, _origin.Y + offset.Y));

        //        foreach (Primitive p in list)
        //        {
        //            Primitive copy = p.Clone();
        //            copy.ZIndex = p.ZIndex + 1;
        //            copy.MoveByDelta(offset.X, offset.Y);
        //            copy.AddToContainer(Globals.ActiveDrawing);
        //            AddToSelectedElements(copy);
        //        }

        //        foreach (Primitive p in _selectedElements)
        //        {
        //            Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, p);
        //        }

        //        if (_selectedElements.Count > 1)
        //        {
        //            Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, _selectedElements.Count);
        //        }
        //    }
        //}

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
                Globals.Events.ShowContextMenu(null, "copypaste");
            }
        }

        protected override void Move(double dx, double dy)
        {
            if (dx != 0 || dy != 0)
            {
                if (_selectedHandleId > 0)
                {
                    //_handles.MoveHandle(_selectedHandleId, dx, dy, _shiftKey);
                }
                else if (Globals.SelectSubCommand != SelectSubCommand.Rotate)
                {
                    //_handles.Move(dx, dy);
                    SetSelectionOrigin(new Point(_origin.X + dx, _origin.Y + dy));

                    foreach (Primitive p in _selectedElements)
                    {
                        p.MoveByDelta(dx, dy);
                        p.Draw();
                    }
                }
            }
        }

        protected override bool PickSelection(double x, double y, double tolerance)
        {
            bool reselect = false;
            
            if (_constructHandles.SelectedHandleID >= 0 && _selectedHandleId < 0 && _itemBoxUnion.Contains(_constructHandles.SelectedHandle.Location))
            {
                // If a snap point is selected while there is a selection
                // and the snap point lies within the selection box but is not on the selection
                // make the snap point the anchor point

                reselect = true;

                // base.PickSelection sets _selectedHandleId
                //_selectedHandleId = _handles.Hit(x, y);

            }
            else if (base.PickSelection(x, y, tolerance * 2))
            {
                reselect = true;
            }

            return reselect;
        }

        public override void ShowHandles()
        {
            if (_selectedElements.Count > 0)
            {
                //_handles.Attach(this);
                //_handles.AddHandle(1000, _origin.X, _origin.Y, HandleType.Triangle);
                //_handles.Draw();
            }
        }

        public override void MoveHandle(int id, double dx, double dy, bool shift)
        {
            //double w1 = _itemBoxUnion.Width;
            //double h1 = _itemBoxUnion.Height;
            //double w2 = _itemBoxUnion.Width;
            //double h2 = _itemBoxUnion.Height;
            //double x2 = _itemBoxUnion.X;
            //double y2 = _itemBoxUnion.Y;

            //double da = 0;

            //double ax = 0;
            //double ay = 0;

            //switch (id)
            //{
            //    case 2001:  // top-left 
            //        ax = _itemBoxUnion.Right;
            //        ay = _itemBoxUnion.Bottom;
            //        x2 += dx;
            //        y2 += dy;
            //        w2 -= dx;
            //        h2 -= dy;
            //        break;

            //    case 2002:
            //        // top-right 
            //        ax = _itemBoxUnion.Left;
            //        ay = _itemBoxUnion.Bottom;
            //        //x2 -= dx;
            //        y2 += dy;
            //        w2 += dx;
            //        h2 -= dy;
            //        break;

            //    case 2003:  // bottom-right 
            //        ax = _itemBoxUnion.Left;
            //        ay = _itemBoxUnion.Top;
            //        w2 += dx;
            //        h2 += dy;
            //        break;

            //    case 2004:
            //        // bottom-left 
            //        ax = _itemBoxUnion.Right;
            //        ay = _itemBoxUnion.Top;
            //        x2 += dx;
            //        w2 -= dx;
            //        h2 += dy;
            //        break;

            //    case 2005:
            //        // left
            //        ax = _itemBoxUnion.Right;
            //        ay = _cy;
            //        x2 += dx;
            //        w2 -= dx;
            //        break;

            //    case 2006:
            //        // top
            //        ax = _cx;
            //        ay = _itemBoxUnion.Bottom;
            //        y2 += dy;
            //        h2 -= dy;
            //        break;

            //    case 2007:
            //        // right
            //        ax = _itemBoxUnion.Left;
            //        ay = _cy;
            //        w2 += dx;
            //        break;

            //    case 2008:
            //        // bottom
            //        ax = _cx;
            //        ay = _itemBoxUnion.Top;
            //        h2 += dy;
            //        break;

            //    case 1000:
            //        // origin/pivot
            //        SetSelectionOrigin(new Point(_origin.X + dx, _origin.Y + dy));
            //        break;

            //    case 3000:
            //        // Rotate ref point
            //        double a1 = Construct.Angle(_origin, _refPoint);
            //        _refPoint.X += dx;
            //        _refPoint.Y += dy;
            //        da = Construct.Angle(_origin, _refPoint) - a1;
            //        break;
            //}

            //ShowHandles();
        }
    }
}
