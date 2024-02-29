using Cirros;
using Cirros.Primitives;
using Cirros.Utility;
using Cirros.Alerts;
using Cirros.Imaging;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using CirrosUI;
using System.Collections.Generic;
using Cirros.Drawing;

namespace Cirros8.ModalDialogs
{
    public sealed partial class PhotoWarpPanel : UserControl
    {
        Unit _unit = Unit.Undefined;

        PhotoToolType _toolType = PhotoToolType.None;
        bool _isDirty = false;

        int _perspectiveCount = 0;
        int _rotateCount = 0;

        double _rotateAxisAngle = 0;

        private WriteableBitmap _bitmap = null;
        private WriteableBitmap _previewBitmap = null;
        private byte[] _previewSourceBytes = null;

        Point _previewOffset = new Point(0, 0);
        double _previewScale = 1;

        private Color _figureColor = Colors.Magenta;

        private Transform _previewTransform = new TranslateTransform();

        private List<Point> _rotateAxis;
        private Point [] _sourceQuad;
        private Rect _destQuadRect = new Rect();

        private double _refWidth = 0;
        private double _refHeight = 0;
        private double _originalRefWidth = 0;
        private double _originalRefHeight = 0;

        private bool _refWidthIsSetByUser = false;
        private bool _refHeightIsSetByUser = false;

        public event ApplyHandler OnApply;
        public delegate void ApplyHandler(object sender, ApplyEventArgs e);

        public event SelectPhotoToolHandler OnSelectPhotoTool;
        public delegate void SelectPhotoToolHandler(object sender, PhotoToolEventArgs e);

        private Dictionary<string, object> _parameterBlock;

        public PhotoWarpPanel()
        {
            this.InitializeComponent();

            _previewArea.SizeChanged += _previewArea_SizeChanged;
            this.Loaded += PhotoWarpPanel_Loaded;

            _unitPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            _unitColumn.Width = new GridLength(40);
        }

        void PhotoWarpPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (_previewArea.RenderSize.Width == 0)
            {
                _previewArea.Measure(new Size(1000, 1000));
            }

            FrameworkElement v = this;

            while (v.Parent != null && v is FrameworkElement)
            {
                v = v.Parent as FrameworkElement;

                if (v is PhotoEditPage)
                {
                    _parameterBlock = ((PhotoEditPage)v).ParameterBlock;

                    if (_parameterBlock != null)
                    {
                        if (_parameterBlock.ContainsKey("unit") && _parameterBlock.ContainsKey("command"))
                        {
                            if ((string)_parameterBlock["command"] == "newdrawing")
                            {
                                _unit = (Unit)_parameterBlock["unit"];
                            }
                        }
                    }

                    if (_unit == Unit.Undefined)
                    {
                        _unitPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        _unitColumn.Width = new GridLength(40);
                        _referenceLabel.Text = "Reference rectangle size (model units)";
                    }
                    else
                    {
                        _unitPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        _unitColumn.Width = new GridLength(80);
                        _referenceLabel.Text = "Reference rectangle size";

                        if (_unit == Unit.Millimeters)
                        {
                            _mmRBButton.IsChecked = true;
                            _inchRBButton.IsChecked = false;
                            _feetRBButton.IsChecked = false;
                        }
                        else if (_unit == Unit.Feet)
                        {
                            _feetRBButton.IsChecked = true;
                            _mmRBButton.IsChecked = false;
                            _inchRBButton.IsChecked = false;
                        }
                        else
                        {
                            _inchRBButton.IsChecked = true;
                            _mmRBButton.IsChecked = false;
                            _feetRBButton.IsChecked = false;
                        }
                    }
                    break;
                }
            }

            SelectToolType(PhotoToolType.Perspective);

            _widthBox.GotFocus += sizeBox_GotFocus;
            _widthBox.LostFocus += sizeBox_LostFocus;
            _widthBox.KeyDown += sizeBox_KeyDown;
            _widthBox.OnValueChanged += sizeBox_OnValueChanged;

            _heightBox.GotFocus += sizeBox_GotFocus;
            _heightBox.LostFocus += sizeBox_LostFocus;
            _heightBox.KeyDown += sizeBox_KeyDown;
            _heightBox.OnValueChanged += sizeBox_OnValueChanged;

            this.SizeChanged += PhotoWarpPanel_SizeChanged;
        }

        private void PhotoWarpPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ActualHeight < (_titleBlock.ActualHeight + _contentGrid.ActualHeight))
            {
                if (_previewText.Visibility != Visibility.Collapsed)
                {
                    _previewText.Visibility = Visibility.Collapsed;
                }
            }
            else if (this.ActualHeight > (_titleBlock.ActualHeight + _contentGrid.ActualHeight + 60))
            {
                if (_previewText.Visibility != Visibility.Visible)
                {
                    _previewText.Visibility = Visibility.Visible;
                }
            }
        }

        void sizeBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (sender == _widthBox)
            {
                _refWidthIsSetByUser = e.Value > 0;
                _refWidth = e.Value;
            }
            else if (sender == _heightBox)
            {
                _refHeightIsSetByUser = e.Value > 0;
                _refHeight = e.Value;
            }
        }

        public string ApplyCounts
        {
            get
            {
                string s = string.Format("P{0},R{1}", _perspectiveCount, _rotateCount);
                _perspectiveCount = _rotateCount = 0;
                return s;
            }
        }

        public bool IsDirty
        {
            get
            {
                return _isDirty;
            }
            set
            {
                _isDirty = value;
                _applyButton.IsEnabled = _isDirty;
            }
        }

        public Color FigureColor
        {
            set
            {
                _figureColor = value;

                Brush brush = new SolidColorBrush(_figureColor);

                _previewAxisLine.Stroke = brush;
                _previewQuadRectangle.Stroke = brush;
                _arrowPath.Stroke = brush;
                _arrowPath.Fill = brush;
            }
        }

        void sizeBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _destQuadRect = DestRectFromQuad(_sourceQuad);
                UpdatePreview();
            }

            if (sender == _widthBox)
            {
                _refWidthIsSetByUser = true;
            }
            else if (sender == _heightBox)
            {
                _refHeightIsSetByUser = true;
            }

            IsDirty = true;
        }

        void sizeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            HideArrow();

            if (_refHeight == 0 && _refWidth > 0 && _destQuadRect.Width > 0)
            {
                double aspect = _destQuadRect.Height / _destQuadRect.Width;
                _refHeight = Math.Round(_refWidth * aspect, 3);
                _heightBox.Value = _refHeight;
            }
            else if (_refWidth == 0 && _refHeight > 0 && _destQuadRect.Width > 0)
            {
                double aspect = _destQuadRect.Height / _destQuadRect.Width;
                _refWidth = Math.Round(_refHeight / aspect, 3);
                _widthBox.Value = _refWidth;
            }

            if (sender == _widthBox)
            {
                _destQuadRect = DestRectFromQuad(_sourceQuad);
                UpdatePreview();
            }
            else if (sender == _heightBox)
            {
                _destQuadRect = DestRectFromQuad(_sourceQuad);
                UpdatePreview();
            }
        }

        void sizeBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender == _widthBox)
            {
                DrawWidthArrow();
            }
            else
            {
                DrawHeightArrow();
            }
        }

        void _previewArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize == e.NewSize) return;
            if (Cirros.Utility.Utilities.__checkSizeChanged(18, sender)) return;

            UpdatePreviewFromSource();
        }

        public PhotoToolType ToolType
        {
            get
            {
                return _toolType;
            }
        }

        public List<Point> RotateAxis
        {
            set
            {
                _rotateAxis = value;

                _rotateAxisAngle = 0;

                if (_rotateAxis != null && _rotateAxis.Count == 2)
                {
                    Point delta = new Point(_rotateAxis[1].X - _rotateAxis[0].X, _rotateAxis[1].Y - _rotateAxis[0].Y);
                    double baseAngle = 0;
                    if (Math.Abs(delta.Y) > Math.Abs(delta.X))
                    {
                        // rotate to vertical
                        if (delta.Y < 0)
                        {
                            // swap points
                            delta.Y = -delta.Y;
                            delta.X = -delta.X;
                        }
                        baseAngle = Math.PI / 2;
                    }
                    else
                    {
                        // rotate to horizontal
                        if (delta.X < 0)
                        {
                            // swap points
                            delta.Y = -delta.Y;
                            delta.X = -delta.X;
                        }
                    }

                    if (delta.X != 0 && delta.Y != 0)
                    {
                        double angle = Construct.Angle(new Point(0, 0), delta) - baseAngle;
                        _rotateAxisAngle = -angle * Construct.cRadiansToDegrees;
                    }
                }
            }
        }

        public List<Point> PerspectiveQuad
        {
            set
            {
                _sourceQuad = new Point[4];

                for (int i = 0; i < 4; i++)
                {
                    _sourceQuad[i] = value[i];
                }

                _destQuadRect = DestRectFromQuad(_sourceQuad);
                //UpdatePreviewFromSource();
                UpdateFigures();
            }
        }

        public Size RefSize
        {
            set
            {
                if (value == Size.Empty)
                {
                    _originalRefWidth = _refWidth = 0;
                    _originalRefHeight = _refHeight = 0;
                    _widthBox.Text = "";
                    _heightBox.Text = "";
                    _refWidthIsSetByUser = false;
                    _refHeightIsSetByUser = false;
                }
                else
                {
                    _originalRefWidth = _refWidth = value.Width;
                    _originalRefHeight = _refHeight = value.Height;
                    _widthBox.Value = _refWidth;
                    _heightBox.Value = _refHeight;
                    _refWidthIsSetByUser = true;
                    _refHeightIsSetByUser = true;
                }
            }
        }

        private WriteableBitmap RotateBitmapToAxis(WriteableBitmap bitmap, List<Point> rotateAxis)
        {
            return bitmap.RotateFree(_rotateAxisAngle);
        }

        private WriteableBitmap ApplyBilinearTransform(WriteableBitmap bitmap, Point [] sourceQuad, Rect destRect)
        {
            return Effects.Warp(bitmap, sourceQuad, destRect);
        }

        Rect DestRectFromQuad(Point [] quad)
        {
            Rect rect = new Rect(0, 0, 100, 100);
            if (quad.Length > 3)
            {
                Point min = new Point((quad[0].X + quad[3].X) / 2, (quad[0].Y + quad[1].Y) / 2);
                Point max = new Point((quad[1].X + quad[2].X) / 2, (quad[2].Y + quad[3].Y) / 2);

                rect = new Rect(min, max);

                if (_refHeight > 0 && _refWidth > 0)
                {
                    double aspect = _refHeight / _refWidth;
                    double cx = (min.X + max.X) / 2;
                    double cy = (min.Y + max.Y) / 2;

                    double area = rect.Width * rect.Height;

                    double w = Math.Sqrt(area / aspect);
                    double h = w * aspect;

                    double dx = (rect.Width - w) / 2;
                    double dy = (rect.Height - h) / 2;

                    rect.X += dx;
                    rect.Y += dy;

                    rect.Width = w;
                    rect.Height = h;
                }
            }

            //System.Diagnostics.Debug.WriteLine("DestRectFromQuad: {0},{1} {2},{3}", rect.X, rect.Y, rect.Width, rect.Height);
            return rect;
        }

        public void UpdateFigures()
        {
            if (_previewBitmap != null)
            {
                if (_toolType == PhotoToolType.Rotate && _rotateAxis != null && _rotateAxis.Count == 2)
                {
                    TranslateTransform tt0 = new TranslateTransform();
                    tt0.X = (double)-_bitmap.PixelWidth / 2;
                    tt0.Y = (double)-_bitmap.PixelHeight / 2;
                    CompositeTransform ct = new CompositeTransform();
                    ct.ScaleX = _previewScale;
                    ct.ScaleY = _previewScale;
                    ct.Rotation = _rotateAxisAngle;
                    TranslateTransform tt1 = new TranslateTransform();
                    tt1.X = (double)_previewBitmap.PixelWidth / 2 + _previewOffset.X;
                    tt1.Y = (double)_previewBitmap.PixelHeight / 2 + _previewOffset.Y;
                    TransformGroup tg = new TransformGroup();
                    tg.Children.Add(tt0);
                    tg.Children.Add(ct);
                    tg.Children.Add(tt1);

                    _previewTransform = tg;

                    Point p0 = _previewTransform.TransformPoint(_rotateAxis[0]);
                    Point p1 = _previewTransform.TransformPoint(_rotateAxis[1]);

                    _previewAxisLine.X1 = p0.X;
                    _previewAxisLine.Y1 = p0.Y;
                    _previewAxisLine.X2 = p1.X;
                    _previewAxisLine.Y2 = p1.Y;
                    _previewAxisLine.Visibility = Visibility.Visible;
                }
                else if (_toolType == PhotoToolType.Perspective && _sourceQuad != null)
                {
                    ScaleTransform tf = new ScaleTransform();
                    tf.ScaleX = _previewScale;
                    tf.ScaleY = _previewScale;

                    _previewTransform = tf;

                    Point min = _previewTransform.TransformPoint(new Point(_destQuadRect.Left, _destQuadRect.Top));
                    Point max = _previewTransform.TransformPoint(new Point(_destQuadRect.Right, _destQuadRect.Bottom));

                    _previewQuadRectangle.SetValue(Canvas.LeftProperty, _previewOffset.X + min.X);
                    _previewQuadRectangle.SetValue(Canvas.TopProperty, _previewOffset.Y + min.Y);
                    _previewQuadRectangle.Width = max.X - min.X;
                    _previewQuadRectangle.Height = max.Y - min.Y;
                    _previewQuadRectangle.Visibility = Visibility.Visible;

                    if (_widthBox.FocusState != FocusState.Unfocused)
                    {
                        DrawWidthArrow();
                    }
                    else if (_heightBox.FocusState != FocusState.Unfocused)
                    {
                        DrawHeightArrow();
                    }
                }
            }
        }

        public void UpdatePreview()
        {
            if (_previewBitmap == null)
            {
                UpdatePreviewFromSource();
            }
            else if (_toolType == PhotoToolType.Rotate && _rotateAxis != null && _rotateAxis.Count == 2)
            {
                _previewImage.Source = RotateBitmapToAxis(_previewBitmap, _rotateAxis);
            }
            else if (_toolType == PhotoToolType.Perspective && _sourceQuad != null)
            {
                ScaleTransform tf = new ScaleTransform();
                tf.ScaleX = _previewScale;
                tf.ScaleY = _previewScale;

                Point [] quad = new Point[4];
                for (int i = 0; i < 4; i++)
                {
                    quad[i] = tf.TransformPoint(_sourceQuad[i]);
                }

                Point min = tf.TransformPoint(new Point(_destQuadRect.Left, _destQuadRect.Top));
                Point max = tf.TransformPoint(new Point(_destQuadRect.Right, _destQuadRect.Bottom));
                Rect destRect = new Rect(min, max);

                _previewImage.Source = ApplyBilinearTransform(_previewBitmap, quad, destRect);
            }
            else
            {
                UpdatePreviewFromSource();
            }

            UpdateFigures();

            IsDirty = true;
        }

        public async Task Apply()
        {
            if (_isDirty)
            {
                Size destSize = new Size(0, 0);

                if (_toolType == PhotoToolType.Rotate)
                {
                    _rotateCount++;

                    if (_refWidth > 0 || _refHeight > 0)
                    {
                        var resourceLoader = new ResourceLoader();
                        string message = resourceLoader.GetString("RotateScaledImageWarning");
                        string title = resourceLoader.GetString("RotateScaledImageWarningTitle");

                        string result = await StandardAlerts.SimpleAlertAsyncWithCancel(title, message);
                        if (result == "cancel")
                        {
                            return;
                        }
                        else
                        {
                            _refHeight = 0;
                            _refWidth = 0;
                            _refHeightIsSetByUser = false;
                            _refWidthIsSetByUser = false;
                            _widthBox.Text = "";
                            _heightBox.Text = "";
                        }
                    }
                }
                else
                {
                    _perspectiveCount++;

                    if (_refWidthIsSetByUser || _refHeightIsSetByUser)
                    {
                        destSize = new Size(_refWidth, _refHeight);
                    }

                    if (Globals.ActiveDrawing != null)
                    {
                        double modelWidth = Globals.ActiveDrawing.PaperToModel(Globals.ActiveDrawing.PaperSize.Width);
                        double modelHeight = Globals.ActiveDrawing.PaperToModel(Globals.ActiveDrawing.PaperSize.Height);

                        if (destSize.Width > modelWidth || destSize.Height > modelHeight)
                        {
                            var resourceLoader = new ResourceLoader();
                            string format = resourceLoader.GetString("ScaledImageTooLargeFormat");
                            string message = string.Format(format,
                                Utilities.FormatDistance(destSize.Width, Globals.DimensionRound, Globals.ActiveDrawing.IsArchitecturalScale, true, Globals.ActiveDrawing.ModelUnit, false),
                                Utilities.FormatDistance(destSize.Height, Globals.DimensionRound, Globals.ActiveDrawing.IsArchitecturalScale, true, Globals.ActiveDrawing.ModelUnit, false),
                                Utilities.FormatDistance(modelWidth, Globals.DimensionRound, Globals.ActiveDrawing.IsArchitecturalScale, true, Globals.ActiveDrawing.ModelUnit, false),
                                Utilities.FormatDistance(modelHeight, Globals.DimensionRound, Globals.ActiveDrawing.IsArchitecturalScale, true, Globals.ActiveDrawing.ModelUnit, false)
                                );
                            string title = resourceLoader.GetString("ScaledImageTooLargeTitle");

                            string result = await StandardAlerts.SimpleAlertAsyncWithCancel(title, message);
                            if (result == "cancel")
                            {
                                _refHeight = _originalRefHeight;
                                _refWidth = _originalRefWidth;
                                _refHeightIsSetByUser = false;
                                _refWidthIsSetByUser = false;

                                if (_refWidth == 0)
                                {
                                    _widthBox.Text = "";
                                }
                                else
                                {
                                    _widthBox.Value = _refWidth;
                                }

                                if (_refHeight == 0)
                                {
                                    _heightBox.Text = "";
                                }
                                else
                                {
                                    _heightBox.Value = _refHeight;
                                }
                                return;
                            }
                        }
                    }
                }

                if (OnApply != null)
                {
                    WriteableBitmap bm = null;

                    if (_toolType == PhotoToolType.Rotate && _rotateAxis != null && _rotateAxis.Count == 2)
                    {
                        bm = RotateBitmapToAxis(_bitmap, _rotateAxis);
                    }
                    else if (_toolType == PhotoToolType.Perspective && _sourceQuad != null)
                    {
                        bm = ApplyBilinearTransform(_bitmap, _sourceQuad, _destQuadRect);
                    }

                    if (bm != null)
                    {

                        OnApply(this, new ApplyEventArgs(bm, _destQuadRect, destSize));
                    }
                }

                _previewSourceBytes = null;

                _bitmap.Invalidate();

                IsDirty = false;
            }
        }

        private void UpdatePreviewFromSource()
        {
            if (_previewSourceBytes != null)
            {
                _previewBitmap.FromByteArray(_previewSourceBytes);
                _previewImage.Source = _previewBitmap;
                _previewBitmap.Invalidate();

                UpdateFigures();
            }
            else if (_bitmap != null && _previewArea.RenderSize.Width > 0 && _previewArea.RenderSize.Height > 0)
            {
                int previewWidth = (int)_previewArea.RenderSize.Width;
                int previewHeight = (int)_previewArea.RenderSize.Height;

                double xs = _previewArea.RenderSize.Width / (double)_bitmap.PixelWidth;
                double ys = _previewArea.RenderSize.Height / (double)_bitmap.PixelHeight;

                if (ys < xs)
                {
                    previewWidth = (int)(_bitmap.PixelWidth * ys);

                    _previewScale = ys;
                    _previewOffset.X = (_previewArea.RenderSize.Width - previewWidth) / 2;
                    _previewOffset.Y = 0;
                }
                else
                {
                    previewHeight = (int)(_bitmap.PixelHeight * xs);

                    _previewScale = xs;
                    _previewOffset.X = 0;
                    _previewOffset.Y = (_previewArea.RenderSize.Height - previewHeight) / 2;
                }

                _previewBitmap = _bitmap.Resize(previewWidth, previewHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
                _previewSourceBytes = _previewBitmap.ToByteArray();
                _previewImage.Source = _previewBitmap;
                _previewBitmap.Invalidate();

                UpdateFigures();
            }

            IsDirty = false;
        }

        public void SetBitmap(WriteableBitmap wbm)
        {
            _bitmap = wbm;
            _previewSourceBytes = null;

            UpdatePreviewFromSource();
        }

        void SelectToolType(PhotoToolType toolType)
        {
            _toolType = toolType;

            if (_toolType == PhotoToolType.Rotate)
            {
                _rotateButton.IsChecked = true;
                _perspectiveButton.IsChecked = false;
                _perspectiveSizePanel.Visibility = Visibility.Collapsed;
                _previewAxisLine.Visibility = Visibility.Visible;
                _previewQuadRectangle.Visibility = Visibility.Collapsed;

                if (OnSelectPhotoTool != null)
                {
                    OnSelectPhotoTool(this, new PhotoToolEventArgs(PhotoToolType.Rotate));
                }
            }
            else if (_toolType == PhotoToolType.Perspective)
            {
                _rotateButton.IsChecked = false;
                _perspectiveButton.IsChecked = true;
                _perspectiveSizePanel.Visibility = Visibility.Visible;
                _previewAxisLine.Visibility = Visibility.Collapsed;
                _previewQuadRectangle.Visibility = Visibility.Visible;

                if (OnSelectPhotoTool != null)
                {
                    OnSelectPhotoTool(this, new PhotoToolEventArgs(PhotoToolType.Perspective));
                }
            }

            if (_previewBitmap != null)
            {
                UpdatePreview();
            }

            IsDirty = false;
        }
        
        private async void _applyButton_Click(object sender, RoutedEventArgs e)
        {
            await Apply();
        }

        private void _rotateButton_Click(object sender, RoutedEventArgs e)
        {
            SelectToolType(PhotoToolType.Rotate);
        }

        private void _perspectiveButton_Click(object sender, RoutedEventArgs e)
        {
            SelectToolType(PhotoToolType.Perspective);
        }

        private void HideArrow()
        {
             _arrowPath.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void DrawWidthArrow()
        {
            double ymid = (_destQuadRect.Top + _destQuadRect.Bottom) / 2;

            Point from = new Point(_destQuadRect.Left, ymid);
            Point to = new Point(_destQuadRect.Right, ymid);

            DrawArrow(from, to);
        }

        private void DrawHeightArrow()
        {
            double xmid = (_destQuadRect.Left + _destQuadRect.Right) / 2;

            Point from = new Point(xmid, _destQuadRect.Bottom);
            Point to = new Point(xmid, _destQuadRect.Top);

            DrawArrow(from, to);
        }

        private void DrawArrow(Point from, Point to)
        {
            from = _previewTransform.TransformPoint(from);
            to = _previewTransform.TransformPoint(to);

            from.X += _previewOffset.X;
            from.Y += _previewOffset.Y;
            to.X += _previewOffset.X;
            to.Y += _previewOffset.Y;

            _arrowPath.Visibility = Windows.UI.Xaml.Visibility.Visible;

            GeometryGroup geomGroup = new GeometryGroup();

            geomGroup.Children.Add(CGeometry.ArrowGeometry(from, to, ArrowType.Filled, 12, .25));

            LineGeometry line = new LineGeometry();
            line.StartPoint = from;
            line.EndPoint = to;
            geomGroup.Children.Add(line);

            geomGroup.Children.Add(CGeometry.ArrowGeometry(to, from, ArrowType.Filled, 12, .25));

            _arrowPath.Data = geomGroup;
        }

        private void unitRBButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == _inchRBButton)
            {
                _parameterBlock["unit"] = Unit.Inches;
            }
            else if (sender == _feetRBButton)
            {
                _parameterBlock["unit"] = Unit.Feet;
            }
            else
            {
                _parameterBlock["unit"] = Unit.Millimeters;
            }
        }
    }
}
