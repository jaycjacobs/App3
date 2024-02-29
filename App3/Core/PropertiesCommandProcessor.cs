using Cirros.Primitives;
using Cirros.Drawing;
using Microsoft.UI.Xaml;
using Cirros.Utility;
using Cirros.Actions;
using Windows.Foundation;
using Cirros.Core;

namespace Cirros.Commands
{
    public class PropertiesCommandProcessor : CommandProcessor
    {
        Primitive _selection = null;
        Primitive _selectedMember = null;

        public PropertiesCommandProcessor()
        {
            _type = CommandType.properties;

            ShowConstructHandles = false;
        }

        public override InputMode InputMode
        {
            get
            {
                return InputMode.Pick;
            }
        }

        protected override CursorType cursorType
        {
            get
            {
                return CursorType.Hand;
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            Deselect();

            Primitive p = Globals.ActiveDrawing.Pick(_start.X, _start.Y, false);
            if (p != null)
            {
                SelectSingleObject(p);
            }
        }

        public override void Invoke(object o, object parameter)
        {
            if (o is string && _selection != null)
            {
                switch ((string)o)
                {
                    case "A_SelectCopy":
                        CopySelection();
                        break;

                    case "A_SelectDelete":
                        DeleteSelection();
                        break;
                }
            }
        }

        private void DeleteSelection()
        {
            if (_selection != null && _selection.IsInstanceMember == false)
            {
                Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, _selection);
                Globals.ActiveDrawing.DeletePrimitive(_selection);

                Deselect();
            }
        }

        private void CopySelection()
        {
            Point moffset = Globals.ActiveDrawing.ModelToPaperDelta(new Point(Globals.xSnap, Globals.ySnap * 2));

            if (_selection != null && _selection.IsInstanceMember == false)
            {
                Primitive copy = _selection.Clone();
                copy.ZIndex = Globals.ActiveDrawing.MaxZIndex;
                copy.MoveByDelta(moffset.X, moffset.Y);
                copy.AddToContainer(Globals.ActiveDrawing);

                Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, copy);

                SelectSingleObject(copy);
            }
        }

        public override bool EnableCommand(object o)
        {
            bool enable = false;

            if (o is string && _selection != null)
            {
                switch ((string)o)
                {
                    case "A_SelectDelete":
                    case "A_SelectCopy":
                        enable = _selection != null;
                        break;
                }
            }
            return enable;
        }

        public override void Finish()
        {
            Deselect();
            base.Finish();
        }

        private void Deselect()
        {
            if (_selection != null)
            {
                _selection.IsDynamic = false;
                _selection.Highlight(false);
                _selection = null;
            }

            //Globals.Events.ShowProperties(null);
            Globals.Events.ShowContextMenu(null, "properties");
        }

        public void SelectSingleObject(Primitive p)
        {
            Deselect();

            _selectedMember = p;

            if (p.IsInstanceMember)
            {
                _selection = Utilities.GetTopLevelPrimitive(p);
            }
            else
            {
                _selection = p;
                if (p.Matrix.IsIdentity == false)
                {
                    p.Normalize(false);
                }
            }

            if (_selection != null)
            {
                _selection.IsDynamic = true;
                _selection.Highlight(true);
            }

            //Globals.Events.ShowProperties(_selectedMember);
            Globals.Events.ShowContextMenu(_selectedMember, "properties");
        }
    }
}
