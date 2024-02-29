using Cirros.Display;
using Cirros.Drawing;
using Cirros.Utility;
using System;
using System.Collections.Generic;
#if UWP
using Cirros.Core;
using Cirros.Actions;
using Cirros.Core.Display;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
#else
using CirrosCore;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
#endif

namespace Cirros.Primitives
{
    public class PImage : Primitive
    {
        protected Point _c1 = new Point(0, 0);
        protected Point _c2 = new Point(0, 0);
        protected string _imageId;
        protected string _sourceName;

        protected Point _refP1 = new Point();
        protected Point _refP2 = new Point();
        protected Size _refSize = new Size(0, 0);

        BitmapImage _bitmapImage = null;
        private bool _preserveAspect;
        private double _aspect = 1;

#if UWP
        public override async void Draw()
        {
            if (_objectId > 0)
            {
                try
                {
                    await Globals.DrawingCanvas.VectorListControl.LoadImage(this);

                    VectorList vlist = Globals.DrawingCanvas.VectorListControl.VectorList;
                    if (vlist != null)
                    {
                        VectorEntity ve = vlist.GetSegment(this.Id);
                        if (ve != null)
                        {
                            _bbox = ve.ItemBox;
                        }
                    }
                }
                catch (Exception ex)
                {
                    uint v = 0x8007000e;
                    if (ex.HResult == (int)v)
                    {
                        //out of memory
                    }
                    else
                    {

                    }
                }
            }
        }
#else
#endif

        public PImage(Point origin)
            : base(origin)
        {
        }

        public PImage(Point origin, Point c1, Point c2, double opacity, Point refP1, Point refP2, Size refSize, string imageId, string sourceName)
            : base(origin)
        {
            _origin = origin;
            _c1 = new Point(c1.X - _origin.X, c1.Y - _origin.Y);
            _c2 = new Point(c2.X - _origin.X, c2.Y - _origin.Y);
            _imageId = imageId;
            _sourceName = sourceName;
            _opacity = opacity;
            _refP1 = refP1;
            _refP2 = refP2;

            if (_refP1.X < 0 || _refP1.X > 1 || _refP2.X < 0 || _refP2.X > 1 || _refP1.Y < 0 || _refP1.Y > 1 || _refP2.Y < 0 || _refP2.Y > 1)
            {
                Analytics.ReportEvent("pimage_bounds_error_0");
            }
            _refSize = refSize;
        }

        public PImage(PImage original)
            : base(original)
        {
            _c1 = original._c1;
            _c2 = original._c2;
            _imageId = original._imageId;
            _sourceName = original._sourceName;
            _opacity = original._opacity;
            _refP1 = original._refP1;
            _refP2 = original._refP2;
            if (_refP1.X < 0 || _refP1.X > 1 || _refP2.X < 0 || _refP2.X > 1 || _refP1.Y < 0 || _refP1.Y > 1 || _refP2.Y < 0 || _refP2.Y > 1)
            {
                Analytics.ReportEvent("pimage_bounds_error_1");
            }
            _refSize = original._refSize;
            _bitmapImage = original._bitmapImage;
        }

        public PImage(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            _c1 = e.P1.ToPoint();
            _c2 = e.P2.ToPoint();
            _imageId = e.ImageId;
            _sourceName = e.Name;
            _opacity = e.Opacity;
            _refP1 = e.RefP1.ToPoint();
            _refP2 = e.RefP2.ToPoint();
            if (_refP1.X < 0 || _refP1.X > 1 || _refP2.X < 0 || _refP2.X > 1 || _refP1.Y < 0 || _refP1.Y > 1 || _refP2.Y < 0 || _refP2.Y > 1)
            {
                Analytics.ReportEvent("pimage_bounds_error_2");
            }
            _refSize = e.RefSize.ToSize();

#if UWP
            Globals.DrawingCanvas.VectorListControl.LoadImage(_imageId);
#else
#endif
        }

        public override Primitive Clone()
        {
            return new PImage(this);
        }

        public override Entity Serialize()
        {
            Entity e = base.Serialize();

            e.P1 = new FPoint(_c1);
            e.P2 = new FPoint(_c2);
            e.ImageId = _imageId;
            e.Name = _sourceName;
            e.Opacity = PrimitiveUtilities.SerializeDoubleAsFloat(_opacity);

            e.RefP1 = new FPoint(_refP1);
            e.RefP2 = new FPoint(_refP2);
            if (_refP1.X < 0 || _refP1.X > 1 || _refP2.X < 0 || _refP2.X > 1 || _refP1.Y < 0 || _refP1.Y > 1 || _refP2.Y < 0 || _refP2.Y > 1)
            {
                Analytics.ReportEvent("pimage_bounds_error_3");
            }
            e.RefSize = new FSize(_refSize);

            return e;
        }

        public Point C1
        {
            get
            {
                return new Point(_c1.X + _origin.X, _c1.Y + _origin.Y);
            }
            set
            {
                _c1 = new Point(value.X - _origin.X, value.Y - _origin.Y);
            }
        }

        public Point C2
        {
            get
            {
                return new Point(_c2.X + _origin.X, _c2.Y + _origin.Y);
            }
            set
            {
                _c2 = new Point(value.X - _origin.X, value.Y - _origin.Y);
            }
        }

        public Point RefP1
        {
            get
            {
                return new Point(_refP1.X, _refP1.Y);
            }
            set
            {
                _refP1 = value;
                if (_refP1.X < 0 || _refP1.X > 1 || _refP2.X < 0 || _refP2.X > 1 || _refP1.Y < 0 || _refP1.Y > 1 || _refP2.Y < 0 || _refP2.Y > 1)
                {
                    Analytics.ReportEvent("pimage_bounds_error_3");
                }
            }
        }

        public Point RefP2
        {
            get
            {
                return new Point(_refP2.X, _refP2.Y);
            }
            set
            {
                _refP2 = value;
                if (_refP1.X < 0 || _refP1.X > 1 || _refP2.X < 0 || _refP2.X > 1 || _refP1.Y < 0 || _refP1.Y > 1 || _refP2.Y < 0 || _refP2.Y > 1)
                {
                    Analytics.ReportEvent("pimage_bounds_error_5");
                }
            }
        }

        public Size RefSize
        {
            get { return _refSize; }
            set { _refSize = value; }
        }

#if UWP
        public async Task LoadImageIdAsync()
        {
            if (_imageId != null)
            {
                await GetImageSourceAsync();
            }
        }
#else
#endif

        public string ImageId
        {
            get
            { 
                return _imageId;
            }
            set
            {
                if (_imageId != value)
                {
                    _imageId = value;
                    _bitmapImage = null;
                }
            }
        }

        public string SourceName
        {
            get { return _sourceName; }
            set { _sourceName = value; }
        }

        public override bool Normalize(bool undoable = true)
        {
            bool undid = false;

            if (_matrix.IsIdentity == false)
            {
                if (_matrix.M12 == 0 && _matrix.M21 == 0)
                {
#if UWP
                    if (undoable)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.UnNormalize, this, _matrix);
                        undid = true;
                    }
#else
#endif

                    //Point size = _matrix.Transform(new Point(_width, _height));
                    //_width = size.X;
                    //_height = size.Y;
                    _c1 = _matrix.Transform(_c1);
                    _c2 = _matrix.Transform(_c2);

                    _matrix = CGeometry.IdentityMatrix();
                }
            }
            return undid;
        }

        public override bool UnNormalize(Matrix m)
        {
            if (m.IsIdentity == false)
            {
                Matrix inverse = CGeometry.InvertMatrix(m);

                //Point size = inverse.Transform(new Point(_width, _height));
                //_width = size.X;
                //_height = size.Y;
                _c1 = inverse.Transform(_c1);
                _c2 = inverse.Transform(_c2);

                _matrix = m;
            }
            return true;
        }

#if UWP
        private async Task GetImageSourceAsync()
        {
            if (_bitmapImage == null)
            {
                _bitmapImage = await Utilities.GetImageFromImageIdAsync(_imageId, Size.Empty);
            }
        }

        private async void SetImageSource(Image image)
        {
            if (_bitmapImage == null)
            {
                _bitmapImage = await Utilities.GetImageFromImageIdAsync(_imageId, Size.Empty);
            }
            image.Source = _bitmapImage;
        }

        public override void Highlight(bool flag)
        {
            Globals.DrawingCanvas.VectorListControl.SetSegmentOpacity(_objectId, flag ? _opacity * .5 : _opacity);
        }
#else
#endif

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Image;
            }
        }

        public override int ActiveLayer
        {
            get
            {
                if (Globals.UIVersion == 0)
                {
                    return Globals.LayerId;
                }
                else
                {
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveImageLayerId))
                    {
                        return Globals.ActiveImageLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        public override List<ConstructNode> ConstructNodes
        {
            get
            {
                List<ConstructNode> nodes = _staticConstructNodes;

                if (_constructEnabled && nodes.Count == 0)
                {
                    if (IsTransformed)
                    {
                        Point p0 = _matrix.Transform(_c1);
                        Point p1 = _matrix.Transform(new Point(_c2.X, _c1.Y));
                        Point p2 = _matrix.Transform(_c2);
                        Point p3 = _matrix.Transform(new Point(_c1.X, _c2.Y));

                        nodes.Add(new ConstructNode(new Point(_origin.X, _origin.Y), "origin"));
                        nodes.Add(new ConstructNode(new Point(_origin.X + p0.X, _origin.Y + p0.Y), "corner"));
                        nodes.Add(new ConstructNode(new Point(_origin.X + p1.X, _origin.Y + p1.Y), "corner"));
                        nodes.Add(new ConstructNode(new Point(_origin.X + p2.X, _origin.Y + p2.Y), "corner"));
                        nodes.Add(new ConstructNode(new Point(_origin.X + p3.X, _origin.Y + p3.Y), "corner"));
                    }
                    else
                    {
                        nodes.Add(new ConstructNode(new Point(_origin.X, _origin.Y), "origin"));
                        nodes.Add(new ConstructNode(new Point(_origin.X + _c1.X, _origin.Y + _c1.Y), "corner"));
                        nodes.Add(new ConstructNode(new Point(_origin.X + _c2.X, _origin.Y + _c1.Y), "corner"));
                        nodes.Add(new ConstructNode(new Point(_origin.X + _c2.X, _origin.Y + _c2.Y), "corner"));
                        nodes.Add(new ConstructNode(new Point(_origin.X + _c1.X, _origin.Y + _c2.Y), "corner"));

                        double width = Math.Abs(_c2.X - _c1.X);
                        double height = Math.Abs(_c2.Y - _c1.Y);
                        Point r1 = new Point(_refP1.X * width, _refP1.Y * height);
                        Point r2 = new Point(_refP2.X * width, _refP2.Y * height);
                        nodes.Add(new ConstructNode(new Point(_origin.X + _c1.X + r2.X, _origin.Y + _c2.Y + r1.Y), "reference"));
                        nodes.Add(new ConstructNode(new Point(_origin.X + _c1.X + r1.X, _origin.Y + _c2.Y + r2.Y), "reference"));
                        nodes.Add(new ConstructNode(new Point(_origin.X + _c1.X + r2.X, _origin.Y + _c2.Y + r2.Y), "reference"));
                    }
                }

                return nodes;
            }
        }

        //public override void MoveTo(double x, double y)
        //{
        //    ClearStaticConstructNodes();

        //    _origin = new Point(x, y);
        //}

        protected override void _drawHandles(Handles handles)
        {
            if (IsTransformed)
            {
                base._drawHandles(handles);
            }
            else
            {
                handles.Attach(this);
                handles.AddHandle(1, _origin.X + _c1.X, _origin.Y + _c1.Y);
                handles.AddHandle(2, _origin.X + _c2.X, _origin.Y + _c1.Y);
                handles.AddHandle(3, _origin.X + _c2.X, _origin.Y + _c2.Y);
                handles.AddHandle(4, _origin.X + _c1.X, _origin.Y + _c2.Y);
                handles.Draw();
            }
        }

        public bool PreserveAspect
        {
            set
            {
                _preserveAspect = value;

                if (_preserveAspect && Box.Width > 0 && Box.Height > 0)
                {
                    _aspect = Box.Height / Box.Width;
                }
                else
                {
                    _aspect = 1;
                }
            }
        }

        protected override void _moveHandle(Handles handles, int id, double dx, double dy)
        {
            ClearStaticConstructNodes();

            if (_refSize == Size.Empty || _refSize.Width == 0 || _refSize.Height == 0)
            {
                double d = Math.Abs(_c1.X - _c2.X);
                int s = Math.Sign(_c1.Y - _c2.Y);
                if (id == 1)
                {
                    _c1.X += dx;
                    if (_preserveAspect)
                    {
                        _c1.Y = _c2.Y + d * s * _aspect;
                    }
                    else
                    {
                        _c1.Y += dy;
                    }
                }
                else if (id == 2)
                {
                    _c2.X += dx;
                    if (_preserveAspect)
                    {
                        _c1.Y = _c2.Y + d * s * _aspect;
                    }
                    else
                    {
                        _c1.Y += dy;
                    }
                }
                else if (id == 3)
                {
                    _c2.X += dx;
                    if (_preserveAspect)
                    {
                        _c2.Y = _c1.Y - d * s * _aspect;
                    }
                    else
                    {
                        _c2.Y += dy;
                    }
                }
                else if (id == 4)
                {
                    _c1.X += dx;
                    if (_preserveAspect)
                    {
                        _c2.Y = _c1.Y - d * s * _aspect;
                    }
                    else
                    {
                        _c2.Y += dy;
                    }
                }
            }
            else
            {
                MoveByDelta(dx, dy);
            }
        }

        public override CPoint GetHandlePoint(int handleId)
        {
            CPoint cp = null;

            switch (handleId)
            {
                case 1:
                default:
                    cp = new CPoint(_origin.X + _c1.X, _origin.Y + _c1.Y);
                    break;

                case 2:
                    cp = new CPoint(_origin.X + _c2.X, _origin.Y + _c1.Y);
                    break;

                case 3:
                    cp = new CPoint(_origin.X + _c2.X, _origin.Y + _c2.Y);
                    break;

                case 4:
                    cp = new CPoint(_origin.X + _c1.X, _origin.Y + _c2.Y);
                    break;
            }

            return cp;
        }

        public override void MoveHandlePoint(int handleId, Point p)
        {
            ClearStaticConstructNodes();

            p.X -= _origin.X;
            p.Y -= _origin.Y;

            if (_refSize == Size.Empty || _refSize.Width == 0 || _refSize.Height == 0)
            {
                if (handleId == 1)
                {
                    _c1 = p;
                }
                else if (handleId == 2)
                {
                    _c2.X = p.X;
                    _c1.Y = p.Y;
                }
                else if (handleId == 3)
                {
                    _c2 = p;
                }
                else if (handleId == 4)
                {
                    _c1.X = p.X;
                    _c2.Y = p.Y;
                }
            }
            else
            {
                double dx = 0;
                double dy = 0;

                if (handleId == 1)
                {
                    //_c1 = p;
                    dx = _c1.X - p.X;
                    dy = _c1.Y - p.Y;
                }
                else if (handleId == 2)
                {
                    //_c2.X = p.X;
                    //_c1.Y = p.Y;
                    dx = _c2.X - p.X;
                    dy = _c1.Y - p.Y;
                }
                else if (handleId == 3)
                {
                    //_c2 = p;
                    dx = _c2.X - p.X;
                    dy = _c2.Y - p.Y;
                }
                else if (handleId == 4)
                {
                    //_c1.X = p.X;
                    //_c2.Y = p.Y;
                    dx = _c1.X - p.X;
                    dy = _c2.Y - p.Y;
                }

                MoveByDelta(-dx, -dy);
            }

            Draw();
        }

        public override VectorEntity Vectorize(VectorContext context)
        {
#if true
            VectorEntity ve = new VectorEntity(_objectId, _zIndex);
            SetVEAttributes(ve);
#else
            Layer layer = Globals.LayerTable[_layerId];
            int lineWeight = _lineWeightId < 0 ? layer.LineWeightId : _lineWeightId;
            int lineType = _lineTypeId < 0 ? layer.LineTypeId : _lineTypeId;

            VectorEntity ve = new VectorEntity(_objectId, _zIndex);

            ve.Color = _colorSpec == (uint)ColorCode.ByLayer ? Utilities.ColorFromColorSpec(layer.ColorSpec) : Utilities.ColorFromColorSpec(_colorSpec);
            ve.DashList = Globals.LineTypeTable[lineType].StrokeDashArray;
            ve.LineWidth = lineWeight == 0 ? .01 : (double)lineWeight / 1000;
            ve.IsVisible = _isVisible && layer.Visible;
            ve.LineType = lineType;
#endif
            Point tl = new Point(Math.Min(_c1.X, _c2.X), Math.Min(_c1.Y, _c2.Y));
            tl = _matrix.Transform(tl);

            VectorImageEntity va = new VectorImageEntity();
            va.Origin = new Point(_origin.X + tl.X, _origin.Y + tl.Y);
            va.Width = Math.Abs(_c1.X - _c2.X);
            va.Height = Math.Abs(_c1.Y - _c2.Y);
            va.Opacity = _opacity;
            va.Matrix = _matrix;
            va.ImageId = _imageId;
            ve.AddChild(va);

            _ve = ve;

            return ve;
        }
    }
}
