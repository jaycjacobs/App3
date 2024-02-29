using Cirros.Drawing;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Cirros.Dxf
{
    public class DxfContext
    {
        Point _paperMin;
        Point _paperMax;
        Point _modelMin;
        Point _modelMax;

        Size _viewportSize = new Size();
        Point _viewportCenter = new Point();

        Point _paperOrigin;

        Size _dwgSize;
        Size _modelSize;

        int _dwgScale = 1;

        Point _modelOrigin = new Point(0, 0);

        bool _metric = false;
        float _paperScale = 1;

        Unit _paperUnit = Unit.Inches;
        Unit _modelUnit = Unit.Inches;

        bool _isArchitects = false;
        bool _centerModel = true;

        bool _includeOnlyDesignCenterBlocksInSymbolLibrary = true;

        public Size DwgSize
        {
            get
            {
                return _dwgSize;
            }
        }

        public double GroupScale
        {
            get { return _groupScale; }
        }

        DxfViewportEntity _activeViewport = null;

        public bool IncludeOnlyDesignCenterBlocksInSymbolLibrary
        {
            get { return _includeOnlyDesignCenterBlocksInSymbolLibrary; }
            set { _includeOnlyDesignCenterBlocksInSymbolLibrary = value; }
        }

        public DxfViewportEntity Viewport
        {
            get
            {
                return _activeViewport;
            }
            set
            {
                _activeViewport = value;
                ContextChanged();
            }
        }

        public bool CenterModel
        {
            get { return _centerModel; }
        }

        //public Size ViewportSize
        //{
        //    get { return _viewportSize; }
        //}

        //public Point ViewportCenter
        //{
        //    get { return _viewportCenter; }
        //}

        public double DrawingWidth
        {
            get { return _dwgSize.Width; }
            set { _dwgSize.Width = value; }
        }

        public double DrawingHeight
        {
            get { return _dwgSize.Height; }
            set { _dwgSize.Height = value; }
        }

        public int DrawingScale
        {
            get { return _dwgScale; }
            set { _dwgScale = value; }
        }

        public bool IsArchitects
        {
            get { return _isArchitects; }
            set { _isArchitects = value; }
        }

        public Unit PaperUnit
        {
            get { return _paperUnit; }
            set { _paperUnit = value; }
        }

        public Unit ModelUnit
        {
            get { return _modelUnit; }
            set { _modelUnit = value; }
        }

        public bool Metric
        {
            get { return _metric; }
            set { _metric = value; }
        }

        public void ContextChanged()
        {
            if (_activeViewport == null)
            {
                if (_viewportSize.IsEmpty == false)
                {
                    if (_dwgSize.IsEmpty == false)
                    {
                        if (_dwgScale == 1)
                        {
                            _modelOrigin = new Point();
                        }
                        else
                        {
                            Point paperCenter = new Point(_dwgSize.Width / 2, _dwgSize.Height / 2);
                            _modelOrigin = new Point(_viewportCenter.X * _scale - paperCenter.X, _viewportCenter.Y * _scale - paperCenter.Y);
                        }
                    }
                }
            }
            else
            {
                if (_activeViewport.ParseExtendedEntityData())
                {
                    //Point plotTarget = new Point(_activeViewport.ExViewCenterX - _activeViewport.Width / 2, _activeViewport.ExViewCenterY - _activeViewport.Height / 2);
                    ////_modelOrigin = new Point(plotTarget.X - _activeViewport.ExViewTargetX * _scale, plotTarget.Y - _activeViewport.ExViewTargetY * _scale);
                    //_modelOrigin = new Point(_activeViewport.ExViewTargetX * _scale - plotTarget.X, _activeViewport.ExViewTargetY * _scale - plotTarget.Y);
                    _scale = _activeViewport.ExViewScale;
                    _modelOrigin = new Point(_activeViewport.ExViewCenterX - _activeViewport.X0 / _scale, _activeViewport.ExViewCenterY - _activeViewport.Y0 / _scale);
                    //_modelOrigin = new Point(_activeViewport.X0 - _activeViewport.ExViewCenterX, _activeViewport.Y0 - _activeViewport.ExViewCenterY);
                }
                else
                {
                    _activeViewport.ExViewCenterX = _activeViewport.X2;
                    _activeViewport.ExViewCenterY = _activeViewport.Y2;
                    _activeViewport.ExViewHeight = _activeViewport.ModelHeight;

                    _activeViewport.ExViewScale = _activeViewport.Height / _activeViewport.ExViewHeight;
                    if (Math.Round(_activeViewport.ExViewScale) == 1)
                    {
                        _activeViewport.ExViewScale = 1;
                    }
                    _activeViewport.ExViewRect = new Rect(_activeViewport.ExViewCenterX - (_activeViewport.Width / 2) / _activeViewport.ExViewScale, _activeViewport.ExViewCenterY - (_activeViewport.ExViewHeight / 2), _activeViewport.Width / _activeViewport.ExViewScale, _activeViewport.ExViewHeight);
                    _modelOrigin = new Point(_activeViewport.ExViewCenterX - _activeViewport.X0 / _scale, _activeViewport.ExViewCenterY - _activeViewport.Y0 / _scale);

                    //_modelOrigin = new Point(_activeViewport.X0 * _scale - _activeViewport.X2, _activeViewport.Y0 * _scale - _activeViewport.Y2);
                }
            }
        }

        public Rect ModelExtents
        {
            get
            {
                if (_modelMin.X < _modelMax.X && _modelMin.Y < _modelMax.Y)
                {
                    return new Rect(_modelMin, _modelMax);
                }
                return Rect.Empty;
            }
            set
            {
                _modelMin = new Point(value.Left, value.Top);
                _modelMax = new Point(value.Right, value.Bottom);
            }
        }

        public DxfContext(DxfContext context)
        {
            _dxfDocument = context.Document;

            _paperMin = new Point();
            _paperMax = new Point();

            _modelMin = new Point();
            _modelMax = new Point();

            _dwgSize = new Size(0, 0);

            _scale = context._scale;
        }

        DxfDocument _dxfDocument = null;

        public DxfDocument Document
        {
            get
            {
                return _dxfDocument;
            }
        }

        double _scale = 1;
        double _groupScale = 1;

        public DxfContext(DxfDocument doc)
        {
            _dxfDocument = doc;

            _scale = 1;

            DxfPoint3 _limitMin = new DxfPoint3();
            DxfPoint3 _limitMax = new DxfPoint3();
            DxfPoint3 _extentMin = new DxfPoint3();
            DxfPoint3 _extentMax = new DxfPoint3();
            DxfPoint3 _plimitMin = new DxfPoint3();
            DxfPoint3 _plimitMax = new DxfPoint3();
            DxfPoint3 _pextentMin = new DxfPoint3();
            DxfPoint3 _pextentMax = new DxfPoint3();

            _paperOrigin = new Point();

            try
            {
                _limitMin = (DxfPoint3)doc.HEADERCollection["$LIMMIN"];
                _limitMax = (DxfPoint3)doc.HEADERCollection["$LIMMAX"];
            }
            catch
            {
            }

            try
            {
                _extentMin = (DxfPoint3)doc.HEADERCollection["$EXTMIN"];
                _extentMax = (DxfPoint3)doc.HEADERCollection["$EXTMAX"];
            }
            catch
            {
            }

            try
            {
                _pextentMin = (DxfPoint3)doc.HEADERCollection["$PEXTMIN"];
                _pextentMax = (DxfPoint3)doc.HEADERCollection["$PEXTMAX"];
            }
            catch
            {
                _pextentMin = _extentMin;
                _pextentMax = _extentMax;
            }

            try
            {
                if (_pextentMax.X > _pextentMin.X)
                {
                    _plimitMin = (DxfPoint3)doc.HEADERCollection["$PLIMMIN"];
                    _plimitMax = (DxfPoint3)doc.HEADERCollection["$PLIMMAX"];
                }
                else
                {
                    _plimitMin = _limitMin;
                    _plimitMax = _limitMax;
                }
                _paperOrigin = new Point(_plimitMin.X, _plimitMin.Y);
            }
            catch
            {
                _plimitMin = _limitMin;
                _plimitMax = _limitMax;
            }

            _paperMin = new Point(
                _plimitMin.X < _pextentMin.X ? _plimitMin.X : _pextentMin.X,
                _plimitMin.Y < _pextentMin.Y ? _plimitMin.Y : _pextentMin.Y);

            _paperMax = new Point(
                _plimitMax.X > _pextentMax.X ? _plimitMax.X : _pextentMax.X,
                _plimitMax.Y > _pextentMax.Y ? _plimitMax.Y : _pextentMax.Y);

            _modelMin = new Point(_extentMin.X, _extentMin.Y);
            _modelMax = new Point(_extentMax.X, _extentMax.Y);

            if (doc.HEADERCollection.ContainsKey("$MEASUREMENT"))  
            {
                int measurement = (int)doc.HEADERCollection["$MEASUREMENT"];
                _metric = measurement == 1;
                if (_metric)
                {
                    _paperUnit = Unit.Millimeters;
                    _modelUnit = Unit.Millimeters;
                }
                else
                {
                    _paperUnit = Unit.Inches;
                    _modelUnit = Unit.Inches;
                }
            }
            else if (doc.HEADERCollection.ContainsKey("$INSUNITS"))
            {
                int insunit = (int)doc.HEADERCollection["$INSUNITS"];
                _metric = insunit >= 4 && insunit <= 7;
                switch (insunit)
                {
                    case 0:     // unitless
                    case 1:     // inches
                    case 3:     // miles
                    default:    // other
                        _paperUnit = Unit.Inches;
                        _modelUnit = Unit.Inches;
                        break;
                    case 2:     // feet
                        _paperUnit = Unit.Inches;
                        _modelUnit = Unit.Feet;
                        break;
                    case 4:     // millimeters
                        _paperUnit = Unit.Millimeters;
                        _modelUnit = Unit.Millimeters;
                        break;
                    case 5:     // centimeters
                        _paperUnit = Unit.Millimeters;
                        _modelUnit = Unit.Centimeters;
                        break;
                    case 6:     // meters
                    case 7:     // kilometers
                        _paperUnit = Unit.Millimeters;
                        _modelUnit = Unit.Meters;
                        break;
                }
            }

            _paperOrigin.X *= _paperScale;
            _paperOrigin.Y *= _paperScale;

            foreach (DxfVport vport in doc.VPORTList)
            {
                if (vport.Name.ToUpper() == "*ACTIVE")
                {
                    _viewportSize = new Size(vport.ViewHeight * vport.ViewAspect, vport.ViewHeight);
                    _viewportCenter = new Point(vport.X2, vport.Y2);
                }
            }

            if (doc.HEADERCollection.ContainsKey("$DIMSCALE"))
            {
                try
                {
                    float iscale = (float)doc.HEADERCollection["$DIMSCALE"];
                    if (iscale >= 1)
                    {
                        _dwgScale = (int)Math.Round(iscale);
                        _scale = 1.0 / iscale;
                    }
                    else
                    {
                        _scale = 1.0;
                        _dwgScale = 1;
                    }
                }
                catch
                {
                    _scale = 1.0;
                    _dwgScale = 1;
                }
            }
            else
            {
                _scale = 1.0;
                _dwgScale = 1;
            }

            float paperWidth = (float)(_paperMax.X - _paperMin.X);
            float paperHeight = (float)(_paperMax.Y - _paperMin.Y);

            if (paperWidth > 0 && paperHeight > 0)
            {
                _dwgSize = new Size((double)PaperValue(paperWidth), (double)PaperValue(paperHeight));
            }
            else
            {
                _dwgSize.Width = 11;
                _dwgSize.Height = 8.5;
            }

            double modelWidth = _modelMax.X - _modelMin.X;
            double modelHeight = _modelMax.Y - _modelMin.Y;

            if (modelWidth > 0 && modelHeight > 0)
            {
                _modelSize = new Size(modelWidth, modelHeight);

                double xs = _dwgSize.Width / _modelSize.Width;
                double ys = _dwgSize.Height / _modelSize.Height;
                double s = xs < ys ? xs : ys;

                if (s < _scale)
                {
                    _scale = s;
                }
            }
            else
            {
                _modelSize = Size.Empty;
            }

            foreach (DxfEntity e in doc.ENTITYList)
            {
                if (e is DxfViewportEntity vp)
                {
                    _viewPortList.Add(vp);
                }
            }

            _centerModel = _viewPortList.Count == 0;
            _groupScale = _scale;

            ContextChanged();
        }

        List<DxfViewportEntity> _viewPortList = new List<DxfViewportEntity>();

        public List<DxfViewportEntity> ViewPortList
        {
            get { return _viewPortList; }
        }

        public Point ModelOrigin
        {
            get
            {
                return _modelOrigin;
            }
            set
            {
                _modelOrigin = value;
            }
        }

        public double Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                _scale = value;
            }
        }

        public float PaperValue(float v)
        {
            return v * _paperScale;
        }

        public float WcsToModel(float f, int space)
        {
            return space == 0 ? (float)(f * _scale) : (float)(f * _paperScale);
        }

        public Point WcsToModelVector(float wx, float wy)
        {
            return new Point(wx * _scale, -wy * _scale);
        }

        public Point WcsToModel(float wx, float wy, int space)
        {
            Point model;
            
            if (space == 0)
            {
                // Model space
                model = new Point((wx - _modelOrigin.X) * _scale, _dwgSize.Height - ((wy - _modelOrigin.Y) * _scale));
            }
            else
            {
                // Paper space
                model = new Point((wx * _paperScale) - _paperOrigin.X, _dwgSize.Height - ((wy * _paperScale) - _paperOrigin.Y));
            }

            return model;
        }
    }
}
