using Cirros.Display;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Cirros.Core.ConstructHandles
{
    class ConstructHandles
    {
        protected Point _hoverLoc;
        protected double _hoverDistance = 0;
        protected List<ConstructNode> _constructNodes = new List<ConstructNode>();
        protected Handles _constructHandles = new Handles();
        protected DispatcherTimer _hoverTimer;

        public ConstructHandles()
        {
            _hoverTimer = new DispatcherTimer();
            _hoverTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _hoverTimer.Tick += _timer_Tick;

            CanvasScaleChanged();
        }

        void _timer_Tick(object sender, object e)
        {
            Hover(_hoverLoc);

            _hoverTimer.Stop();
        }

        protected virtual void Hover(Point to)
        {
            _constructNodes.Clear();

            if (Globals.ActiveDrawing != null)
            {
                foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
                {
                    if (Globals.LayerTable[p.LayerId].Visible && p.IsNear(to, _hoverDistance))
                    {
                        _constructNodes.AddRange(p.ConstructNodes);
                    }
                }
            }
        }

        public void CanvasScaleChanged()
        {
            if (Globals.ActiveDrawing != null)
            {
                //double d = Globals.View.DisplayToCanvas(Globals.hitTolerance * 3);
                //_hoverDistance = Globals.DrawingCanvas.CanvasToPaper(d);
                _hoverDistance = Globals.DrawingCanvas.DisplayToPaper(Globals.hitTolerance * 2);
            }
        }

        public void ShowHandlesNear(Point hoverLoc)
        {
            if (hoverLoc != _hoverLoc)
            {
                double d = _hoverDistance / 4;
                int nodeIndex = -1;

                _constructHandles.Deselect();
                _constructHandles.Clear();

                for (int i = 0; i < _constructNodes.Count; i++)
                {
                    Point n = _constructNodes[i].Location;
                    double ds = Math.Abs(n.X - hoverLoc.X) + Math.Abs(n.Y - hoverLoc.Y);

                    if (ds < _hoverDistance)
                    {
                        double opc = (_hoverDistance - ds) / _hoverDistance;
                        _constructHandles.AddHandle(i, n.X, n.Y, HandleType.Diamond, opc);

                        if (ds < d)
                        {
                            d = ds;
                            nodeIndex = i;
                        }
                    }
                }

                if (nodeIndex >= 0)
                {
                    ConstructNode node = _constructNodes[nodeIndex];

                    _constructHandles.Draw();
                    _constructHandles.Select(nodeIndex);

                    Globals.Events.ShowConstructionPoint(node.Name, node.Location);
                }
                else
                {
                    Globals.Events.ShowConstructionPoint(null, hoverLoc);
                    _constructHandles.Draw();
                }

                _hoverTimer.Start();
                _hoverLoc = hoverLoc;
            }
        }

        public void HideHandles()
        {
            _constructHandles.Deselect();
            _constructHandles.Clear();
        }

        public bool ActivePoint(ref Point p)
        {
            bool isActive = false;

            if (_constructHandles.SelectedHandle != null)
            {
                p = _constructHandles.SelectedHandle.Location;
            }

            return isActive;
        }
    }
}
