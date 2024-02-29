using System;
using Cirros.Utility;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Cirros.Display
{
    public abstract class ARubberBand
    {
        protected const double cDotSize = 6;

        protected double _thickness = 0.75;
        protected double _opacity = 0.75;
        protected bool _scaleWithWindow = true;

        protected Point _anchor = new Point(0, 0);
        protected int _state = 0;

        protected Point _start;
        protected Point _mid;
        protected Point _end;
        protected Point _center;
        protected double _radius;

        protected Color _fillColor;
        protected bool _shouldFill = false;
        protected string _fillPattern;
        protected double _patternScale;
        protected double _patternAngle;

        protected ARubberBand()
        {
        }

        protected ARubberBand(Color color)
        {
            _opacity = 1;
            _scaleWithWindow = true;
        }

        public abstract void Update();
        public abstract double Opacity { set; }
        public abstract void Hide();
        public abstract void Show();
        public abstract void StartTracking(double x, double y);
        public abstract void TrackCursor(double x, double y);
        public abstract void EndTracking();

        protected Color _color = Colors.Black;

        public virtual Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                _color.A = (byte)(_opacity * 255);
            }
        }

        public virtual Point Anchor
        {
            get
            {
                return _anchor;
            }
        }

        public virtual int State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }

        public virtual double Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
            }
        }

        public virtual Point Start
        {
            get
            {
                return _start;
            }
            set
            {
                _start = value;
            }
        }

        public virtual Point Mid
        {
            get
            {
                return _mid;
            }
            set
            {
                _mid = value;
            }
        }

        public virtual Point End
        {
            get
            {
                return _end;
            }
            set
            {
                _end = value;
            }
        }

        public virtual Point Center
        {
            get
            {
                return _center;
            }
            set
            {
                _center = value;
            }
        }

        public virtual Color FillColor
        {
            get
            {
                return _fillColor;
            }
            set
            {
                _fillColor = value;
                _shouldFill = true;
            }
        }

        public virtual string FillPattern
        {
            get
            {
                return _fillPattern;
            }
            set
            {
                _fillPattern = value;
            }
        }

        public virtual double PatternScale
        {
            get
            {
                return _patternScale;
            }
            set
            {
                _patternScale = value;
            }
        }

        public virtual double PatternAngle
        {
            get
            {
                return _patternAngle;
            }
            set
            {
                _patternAngle = value;
            }
        }

        public abstract void Reset();

        protected void CoordinateDisplayVector(Point p)
        {
            double d = Construct.Distance(_anchor, p);
            double a = Construct.Angle(_anchor, p);
            Globals.Events.CoordinateDisplay(p, p.X - _anchor.X, p.Y - _anchor.Y, d, a);
        }
    }
}
