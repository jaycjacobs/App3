using Cirros.Primitives;
using Cirros.Utility;
using CirrosCore;
using System.Collections.Generic;
using Cirros.Core;
#if UWP
using Cirros.Commands;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
#else
using System.Windows;
#endif

namespace Cirros.Display
{
    public enum HandleType
    {
        Square,
        Circle,
        Diamond,
        Triangle
    }

    public interface IHandle
    {
        void Draw(bool visible);
        void Move(double dx, double dy);
        void Select(bool shouldSelect);
        bool IsVisible { get; set; }
        uint Id { get; }
        Point Location { get; }
        void Dispose();
    }

    public interface IHandleFrame
    {
        void Move(double dx, double dy);
        void Dispose();
    }

    public interface IHandleLine
    {
        void Move(double dx, double dy);
        void Dispose();
    }

    public interface IHandleArrow
    {

        void Move(double dx, double dy);
        void Dispose();
    }


    public class Handles
    {
        Dictionary<int, IHandle> _handles = new Dictionary<int, IHandle>();
        protected object _attachedObject = null;
        protected int _selectedId = -1;
        protected int _selectedId2 = -1;
        protected HandleType _type = HandleType.Square;
        protected bool _connect = false;

        IHandleFrame _frame = null;
        IHandleLine _spine = null;
        IHandleArrow _arrow = null;

        int _showArrowTo = -1;
        int _showArrowFrom = -1;

        protected bool _visible = true;

        public Handles()
        {
        }

        public int Count
        {
            get
            {
                return _handles.Count;
            }
        }

        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                _visible = value;

                foreach (IHandle h in _handles.Values)
                {
                    h.IsVisible = _visible;
                }
            }
        }

        public bool Connect
        {
            get
            {
                return _connect;
            }
            set
            {
                _connect = value;
            }
        }

        public int ArrowFrom
        {
            get
            {
                return _showArrowFrom;
            }
            set
            {
                _showArrowFrom = value;
            }
        }

        public int ArrowTo
        {
            get
            {
                return _showArrowTo;
            }
            set
            {
                _showArrowTo = value;
            }
        }

        public object AttachedObject
        {
            get
            {
                return _attachedObject;
            }
        }

        public int SelectedHandleID
        {
            get
            {
                return _visible ? _selectedId : -1;
            }
        }

        public IHandle SelectedHandle
        {
            get
            {
                return _visible && _handles.ContainsKey(_selectedId) ? _handles[_selectedId] : null;
            }
        }

        public IHandle SelectedHandle2
        {
            get
            {
                return _visible && _handles.ContainsKey(_selectedId2) ? _handles[_selectedId2] : null;
            }
        }

        public int Hit(double x, double y)
        {
            double d0 = Globals.View.DisplayToPaper(Globals.hitTolerance);
            int id = -1;

            Point p = new Point(x, y);

            foreach (IHandle h in _handles.Values)
            {
                double d = Construct.Distance(p, h.Location);
                if (d < d0)
                {
                    id = (int)h.Id;
                    d0 = d;
                }
            }

            if (id >= 0)
            {
                // Do we really need to select the handle here?  Shouldn't the caller do that?
                Select(id);
            }

            return id;
        }

        public void Attach(Primitive p)
        {
            Detach();

            _attachedObject = p;
            _type = HandleType.Circle;
        }

        public void Attach(SelectorBase selector)
        {
            Detach();

            _type = HandleType.Circle;
            _attachedObject = selector;
        }

        public void Detach()
        {
            // remind me again what the difference between Detach() and Clear() is

            Clear();

            _attachedObject = null;
            _connect = false;

            //if (_arrow != null)
            //{
            //    _arrow.Dispose();
            //    _arrow = null;
            //    _showArrowFrom = _showArrowTo = -1;
            //}
        }

        public void AddHandle(int id, double x, double y, HandleType type, double opacity)
        {
            _handles.Add(id, new VHandle((uint)id, x, y, type, opacity));
        }

        public void AddHandle(int id, double x, double y, HandleType type)
        {
            _handles.Add(id, new VHandle((uint)id, x, y, type));
        }

        public void AddHandle(int id, double x, double y)
        {
            _handles.Add(id, new VHandle((uint)id, x, y, _type));
        }

        public void AddFrame(Rect r)
        {
            if (_frame != null)
            {
                _frame.Dispose();
                _frame = null;
            }

            _frame = new VHandleFrame(r, 101000);
        }

        public void Draw()
        {
            foreach (IHandle h in _handles.Values)
            {
                h.Draw(_visible);
            }

            if (_handles.Count > 1)
            {
                if (_connect)
                {
                    if (_spine != null)
                    {
                        _spine.Dispose();
                        _spine = null;
                    }

                    List<Point> pc = new List<Point>();

                    foreach (IHandle h in _handles.Values)
                    {
                        pc.Add(h.Location);
                    }

                    _spine = new VHandleLine(pc, 101000);
                }

                if (_showArrowTo >= 0 && _showArrowFrom >= 0)
                {
                    if (_handles.ContainsKey(_showArrowFrom) && _handles.ContainsKey(_showArrowTo))
                    {
                        double dx = _handles[_showArrowTo].Location.X - _handles[_showArrowFrom].Location.X;
                        double dy = _handles[_showArrowTo].Location.Y - _handles[_showArrowFrom].Location.Y;

                        if (dx != 0 || dy != 0)
                        {
                            if (_arrow != null)
                            {
                                _arrow.Dispose();
                                _arrow = null;
                            }
                            _arrow = new VHandleArrow(_handles[_showArrowFrom].Location, _handles[_showArrowTo].Location, 101000);
                        }
                    }
                }
            }
        }

        public void Move(double dx, double dy)
        {
            if (_frame != null)
            {
                _frame.Move(dx, dy);
            }

            if (_spine != null)
            {
                _spine.Move(dx, dy);
            }

            if (_arrow != null)
            {
                _arrow.Move(dx, dy);
            }

            foreach (IHandle h in _handles.Values)
            {
                h.Move(dx, dy);
            }
        }

        public void MoveHandleTo(int id, double x, double y)
        {
            if (_handles.ContainsKey(id))
            {
                double dx = x - _handles[id].Location.X;
                double dy = y - _handles[id].Location.Y;

                this.MoveHandle(id, dx, dy);
            }
        }

        public void MoveHandle(int id, double dx, double dy, bool shift = false)
        {
            if (_handles.ContainsKey(id))
            {
                _handles[id].Move(dx, dy);

                // calling .MoveHandle could cause the primitive and handles to be redrawn
                // which would reset the selected handles
                int selectedId = _selectedId;
                int selectedId2 = _selectedId2;

                if (_attachedObject is Primitive)
                {
                    ((Primitive)_attachedObject).MoveHandleByDelta(this, id, dx, dy);
                }
                else if (_attachedObject is SelectorBase)
                {
                    ((SelectorBase)_attachedObject).MoveHandle(id, dx, dy, shift);
                }

                _selectedId = selectedId;
                _selectedId2 = selectedId2;
            }
        }

        public Point GetHandlePoint(int id)
        {
            if (_handles.ContainsKey(id))
            {
                return _handles[id].Location;
            }

            int count = _handles == null ? -1 : _handles.Count;
            string msg = string.Format("GetHandlePoint: key not found ({0})", id);
            Analytics.ReportError(msg, null, 2, 319);

            return Globals.Input.CursorLocation;
        }

        public void Select(int id)
        {
            Deselect();

            if (_handles.ContainsKey(id))
            {
                _handles[id].Select(true);
                _selectedId = id; 
            }
            else
            {
                _selectedId = -1;
            }
        }

        public int SelectNext(int id)
        {
            Deselect();

            if (_handles.Count > 0)
            {
                Dictionary<int, IHandle>.KeyCollection.Enumerator e = _handles.Keys.GetEnumerator();

                int first = -1;
                int next = -1;

                if (e.MoveNext())
                {
                    first = e.Current;

                    if (_handles.ContainsKey(id))
                    {
                        do
                        {
                            if (e.Current == id)
                            {
                                if (e.MoveNext())
                                {
                                    next = e.Current;
                                    break;
                                }
                            }
                        }
                        while (e.MoveNext());
                    }
                }

                if (_handles.ContainsKey(next))
                {
                    _handles[next].Select(true);
                    _selectedId = next;
                }
                else if (_handles.ContainsKey(first))
                {
                    _handles[first].Select(true);
                    _selectedId = first;
                }
            }

            return _selectedId;
        }

        public void SelectSegment(int id1, int id2)
        {
            Deselect();

            if (_handles.ContainsKey(id1) &&_handles.ContainsKey(id2))
            {
                _handles[id1].Select(true);
                _handles[id2].Select(true);

                _selectedId = id1;
                _selectedId2 = id2;
            }
        }

        public void Deselect()
        {
            if (_selectedId >= 0)
            {
                if (_handles.ContainsKey(_selectedId))
                {
                    _handles[_selectedId].Select(false);
                }
                _selectedId = -1;
            }

            if (_selectedId2 >= 0)
            {
                if (_handles.ContainsKey(_selectedId2))
                {
                    _handles[_selectedId2].Select(false);
                }
                _selectedId2 = -1;
            }
        }

        public void Clear()
        {
            Deselect();

            if (_frame != null)
            {
                _frame.Dispose();
                _frame = null;
            }

            if (_spine != null)
            {
                _spine.Dispose();
                _spine = null;
            }

            if (_arrow != null)
            {
                _arrow.Dispose();
                _arrow = null;
                _showArrowFrom = _showArrowTo = -1;
            }

            foreach (IHandle h in _handles.Values)
            {
                h.Dispose();
            }
            _handles.Clear();
        }
    }
}
