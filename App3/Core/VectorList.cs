//#define DEBUG_RECT
using Cirros.Drawing;
using Cirros.Primitives;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace Cirros.Core.Display
{
    public class VectorList
    {
        Dictionary<uint, VectorEntity> _vectorDictionary = new Dictionary<uint, VectorEntity>();
        VectorContext _vectorContext = new VectorContext(true, true, true);
        List<VectorEntity> _vectorList = null;

        public VectorList()
        {
        }

        public List<VectorEntity> AsList
        {
            get
            {
                if (_vectorList == null)
                {
                    _vectorList = _vectorDictionary.Values.ToList<VectorEntity>();
                    _vectorList.Sort();
                }
                return _vectorList;
            }
        }

        public Rect Extents
        {
            get
            {
                Rect extents = Rect.Empty;
                if (_vectorList != null)
                {
                    foreach (VectorEntity ve in _vectorList)
                    {
                        if (ve.ItemBox.Width == double.NaN)
                        {

                        }
                        else
                        {
                            extents.Union(ve.ItemBox);
                        }
                    }
                }
                return extents;
            }
        }

        public void Regenerate()
        {
            _vectorDictionary.Clear();
            _vectorList = null;

            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                VectorEntity ve = p.Vectorize(_vectorContext);
                if (_vectorDictionary.ContainsKey(ve.SegmentId))
                {
#if DEBUG
                    System.Diagnostics.Debugger.Break();
#endif
                }
                _vectorDictionary[ve.SegmentId] = ve;
            }
        }

        public void UpdateSegment(VectorEntity ve)
        {
            _vectorDictionary[ve.SegmentId] = ve;
            _vectorList = null;
        }

        private void highlightMembers(VectorEntity ve)
        {
            ve.IsHighlighted = true;

            foreach (object o in ve.Children)
            {
                if (o is VectorEntity)
                {
                    highlightMembers(o as VectorEntity);
                }
            }
        }

        public VectorEntity UpdateSegment(Primitive p)
        {
            VectorEntity ve = p.Vectorize(_vectorContext);
            if (_vectorDictionary.ContainsKey(p.Id))
            {
                VectorEntity ve0 = _vectorDictionary[ve.SegmentId];
                //ve.IsHighlighted = ve0.IsHighlighted;
                if (ve0.IsHighlighted)
                {
                    highlightMembers(ve);
                }
                _vectorDictionary[ve.SegmentId] = ve;
            }
            else
            {
                _vectorDictionary[ve.SegmentId] = ve;
            }
            _vectorList = null;

            return ve;
        }

        public void MoveSegmentBy(uint segmentId, double dx, double dy)
        {
            if (_vectorDictionary.ContainsKey(segmentId))
            {
                ((VectorEntity)_vectorDictionary[segmentId]).Move(dx, dy);
            }
        }

        public void RemoveSegment(uint segmentId)
        {
            _vectorDictionary.Remove(segmentId);
            _vectorList = null;
        }

        public void ShowSegment(uint segmentId, bool show)
        {
            if (_vectorDictionary.ContainsKey(segmentId))
            {
                ((VectorEntity)_vectorDictionary[segmentId]).IsVisible = show;
            }
        }

        public VectorEntity GetSegment(uint segmentId)
        {
            VectorEntity ve = null;

            if (_vectorDictionary.ContainsKey(segmentId))
            {
                ve = _vectorDictionary[segmentId];
            }

            return ve;
        }

        public uint Pick(Point location)
        {
            double t = Globals.DrawingCanvas.DisplayToPaper(Globals.hitTolerance) / 2;
            //double t = 1.0 / Globals.hitTolerance;
            double ht = t / 2;

            Rect hitRect = new Rect(location.X - ht, location.Y - ht, t, t);
#if DEBUG_RECT
            VectorEntity ve0 = new VectorEntity(77776, 77776);

            ve0.Color = Windows.UI.Colors.Green;
            ve0.LineWidth = Globals.DrawingCanvas.DisplayToPaper(1);

            List<Point> pc = new List<Point>();
            pc.Add(new Point(hitRect.X, hitRect.Y));
            pc.Add(new Point(hitRect.X + hitRect.Width, hitRect.Y));
            pc.Add(new Point(hitRect.X + hitRect.Width, hitRect.Y + hitRect.Height));
            pc.Add(new Point(hitRect.X, hitRect.Y + hitRect.Height));
            pc.Add(new Point(hitRect.X, hitRect.Y));
            ve0.AddChild(pc);

            Globals.DrawingCanvas.VectorListControl.AddOverlaySegment(ve0);
            Globals.DrawingCanvas.VectorListControl.RedrawOverlay();
#endif
            VectorEntity pick = null;
            uint segmentId = 0;

            foreach (VectorEntity ve in _vectorDictionary.Values)
            {
                if (ve.IsSelectable && ve.IsVisible)
                {
                    Rect test = ve.ItemBox;
                    test.Intersect(hitRect);

                    if (test.IsEmpty == false)
                    {
                        double d = ve.Pick(location, t, ref segmentId);
                        if (d < t)
                        {
                            if (pick == null || ve.ZIndex > pick.ZIndex)
                            {
                                pick = ve;
                            }
                        }
                    }
                }
            }
            return pick == null ? 0 : pick.SegmentId;
        }

        internal void AddSegment(VectorEntity ve)
        {
            // note that if ve.SegmentId exists, the new ve will replace it
            _vectorDictionary[ve.SegmentId] = ve;
            _vectorList = null;
        }
    }
}
