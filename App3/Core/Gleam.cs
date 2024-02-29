using Cirros;
using Cirros.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Cirros8
{
    public class Gleam
    {
        DispatcherTimer _gleamTimer = null;
        double _opacityIncrement = -.2;
        double _opacity = 1;

        List<Primitive> _list;

        public Gleam(List<Primitive> list)
        {
            _list = list;
        }

        public void Start()
        {
            if (_gleamTimer == null)
            {
                _gleamTimer = new DispatcherTimer();
                _gleamTimer.Interval = new TimeSpan(0, 0, 0, 0, 15);
                _gleamTimer.Tick += _gleamTimer_Tick;
            }

            _opacityIncrement = -.3;
            _opacity += _opacityIncrement;

            //System.Diagnostics.Debug.WriteLine("Gleam.Start: {0:f3}", _opacity);
            foreach (Primitive p in _list)
            {
                p.Opacity = _opacity;
                p.Draw();
            }

            _gleamTimer.Start();
        }

        private void _gleamTimer_Tick(object sender, object e)
        {
            if (_opacityIncrement < 0)
            {
                if (_opacity > .2)
                {
                    _opacity += _opacityIncrement;
                }
                else
                {
                    _opacityIncrement = .3;
                }
            }
            else
            {
                if (_opacity < (1 - _opacityIncrement))
                {
                    _opacity += _opacityIncrement;
                }
                else
                {
                    _gleamTimer.Stop();
                    _gleamTimer = null;

                    _opacity = 1;
                }
            }

            //System.Diagnostics.Debug.WriteLine("_gleamTimer_Tick: {0:f3}", _opacity);
            foreach (Primitive p in _list)
            {
                Globals.DrawingCanvas.VectorListControl.SetSegmentOpacity(p.Id, _opacity);
                //p.Opacity = _opacity;
                //p.Draw();
            }
        }
    }
}
