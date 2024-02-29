using Cirros.Core;
using Cirros.Primitives;
using Cirros.Utility;

namespace Cirros.Commands
{
    public class KTPropertiesCommandProcessor : CommandProcessor
    {
        Primitive _selection = null;
        Primitive _selectedMember = null;

        public KTPropertiesCommandProcessor()
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

        public override void Finish()
        {
            Deselect();
            base.Finish();
        }

        private void Deselect()
        {
            if (_selection != null)
            {
                _selection.Highlight(false);
                _selection = null;
            }

            Globals.Events.ShowProperties(null);
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
            }

            if (_selection != null)
            {
                _selection.Highlight(true);
            }

            Globals.Events.ShowProperties(_selectedMember);
        }
    }
}
