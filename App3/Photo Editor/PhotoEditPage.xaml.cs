using Cirros;
using Cirros.Alerts;
using Cirros.Primitives;
using Cirros.Utility;
using HighCampSoftware.BacktotheDrawingBoard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

namespace Cirros8.ModalDialogs
{
    public sealed partial class PhotoEditPage : Page
    {
        private enum FigureType
        {
            None,
            Line,
            Rect,
            Quad
        }

        string _cmdSource = "";

        PhotoToolType _currentTool = PhotoToolType.None;
        FigureType _figureType = FigureType.None;

        StorageFile _sourceImageFile;
        Size _sourceImageSize;
        WriteableBitmap _originalBitmap = null;
        WriteableBitmap _sourceBitmap = null;

        Matrix _inverseViewMatrix = Matrix.Identity;

        double _handleFrameStrokeThickness = 2;

        private Rect _refDestRect = Rect.Empty;
        private Size _refDestSize = Size.Empty;

        List<Point> _lineFigure = new List<Point>()
        {
            new Point(300, 100),
            new Point(300, 500)
        };

        List<Point> _rectFigure = new List<Point>()
        {
            new Point(100, 100),
            new Point(500, 100), 
            new Point(500, 500),
            new Point(100, 500)
        };

        List<Point> _quadFigure = new List<Point>()
        { 
            new Point(100, 100),
            new Point(500, 100), 
            new Point(500, 500),
            new Point(100, 500)
        };

        Polyline _handleFrame = new Polyline();
        Ellipse[] _handleMarkers = new Ellipse[4];
        Point[] _handlePoints = new Point[4];
        int _handleCount = 0;

        string _imageId;
        string _sourceImageName;

        private bool _magnifierVisible = false;

        double _magnifierDestSize = 200;
        double _magnifierSourceSize = 100;
        Point _magnifierSourceLocation = new Point();
        WriteableBitmap _magnifierSourceBitmap = null;
        Point _magnifierSourceOffset = new Point();

        Popup _magifierPopup;
        Image _magifierImage;
        Canvas _magnifierCanvas;

        Dictionary<string, object> _parameterBlock = null;

        public PhotoEditPage()
        {
            this.InitializeComponent();

            DataContext = Globals.UIDataContext;
        }

        public PhotoEditPage(Dictionary<string, object> dictionary)
        {
            this.InitializeComponent();
            Initialize(dictionary);

            DataContext = Globals.UIDataContext;
        }

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Dictionary<string, object>)
            {
                Initialize(e.Parameter as Dictionary<string, object>);
            }
            Analytics.TrackPageView("PhotoEditPage");
        }

        private void Initialize(Dictionary<string, object> dictionary = null)
        {
            if (dictionary != null)
            {
                _cmdSource = (string)dictionary["command"];
                _parameterBlock = dictionary;

                _backButton.Visibility = _cmdSource == "newdrawing" ? Visibility.Visible : Visibility.Collapsed;

                if (dictionary.ContainsKey("pimage"))
                {
                    // edit existing PImage object

                    _okButton.Content = "Update";

                    PImage pimage = (PImage)dictionary["pimage"];

                    _imageId = pimage.ImageId;
                    _sourceImageFile = null;
                    _sourceImageName = pimage.SourceName;

                    if (pimage.RefP1.X == pimage.RefP2.X || pimage.RefP1.Y == pimage.RefP2.Y)
                    {
                        _refDestRect = Rect.Empty;
                        _refDestSize = Size.Empty;
                    }
                    else
                    {
                        _refDestRect = new Rect(pimage.RefP1, pimage.RefP2);
                        _refDestSize = pimage.RefSize;
                    }
                }
                else
                {
                    // create new image
                    _okButton.Content = _cmdSource == "newimage" ? "Insert" : "Update";

                    if (dictionary.ContainsKey("sourceName"))
                    {
                        _sourceImageName = (string)dictionary["sourceName"];
                    }

                    if (dictionary.ContainsKey("imageId"))
                    {
                        // edit image with imageId reference

                        _imageId = (string)dictionary["imageId"];

                        if (dictionary.ContainsKey("refDestRect"))
                        {
                            _refDestRect = (Rect)dictionary["refDestRect"];
                        }
                        if (dictionary.ContainsKey("refDestSize"))
                        {
                            _refDestSize = (Size)dictionary["refDestSize"];
                        }
                    }

                    if (dictionary.ContainsKey("sourceFile"))
                    {
                        _sourceImageFile = (StorageFile)dictionary["sourceFile"];
                        _sourceImageName = _sourceImageFile.Name;
                    }
                    else
                    {
                        Analytics.ReportError("", null, 4, 701);
                    }
                }
            }

            var bounds = App.Window.Bounds;

            _topView.Width = bounds.Width;
            _topView.Height = bounds.Height;

            _filterPanel.OnApply += _filterPanel_OnApply;
            _warpPanel.OnApply += _warpPanel_OnApply;
            _cropPanel.OnApply += _cropPanel_OnApply;

            this.PointerPressed += _imageCanvas_PointerPressed;
            this.PointerMoved += _imageCanvas_PointerMoved;
            this.PointerReleased += _imageCanvas_PointerReleased;
            this.PointerEntered += _imageCanvas_PointerEntered;
            this.PointerExited += _imageCanvas_PointerExited;

            this.Loaded += PhotoEditPage_Loaded;
            this.LayoutUpdated += PhotoEditPage_LayoutUpdated;

            _applyPanel.Visibility = Visibility.Collapsed;

            // the following handlers are intended to keep the handles aligned to the coordinate system of the scroll view content
            _imageCanvas.LayoutUpdated += _imageCanvas_LayoutUpdated;

            _topCanvas.SizeChanged += (o, e) =>
            {
                _topCanvas.Clip = null;
                RectangleGeometry rg = new RectangleGeometry();
                rg.Rect = new Rect(0, 0, _topCanvas.ActualWidth, _topCanvas.ActualHeight);
                _topCanvas.Clip = rg;
            };

            InitializeMagnifier();
        }

        void _imageCanvas_LayoutUpdated(object sender, object e)
        {
            for (int i = 0; i < _handleCount; i++)
            {
                SetLocation(_handleMarkers[i], _handlePoints[i]);
            }
        }

        void _warpPanel_OnSelectPhotoTool(object sender, PhotoToolEventArgs e)
        {
            SelectToolType(e.Tool);
        }

        void _imageCanvas_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
        }

        void _imageCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _selectedHandle = -1;
        }

        int _selectedHandle = -1;

        void _imageCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Point p = e.GetCurrentPoint(_imageCanvas).Position;
            int distance = 50;
            _selectedHandle = -1;

            for (int i = 0; i < _handleCount; i++)
            {
                int d = (int)(Math.Abs(p.X - _handlePoints[i].X) + Math.Abs(p.Y - _handlePoints[i].Y));
                if (d < distance)
                {
                    d = distance;
                    _selectedHandle = i;
                }
            }

            if (_selectedHandle >= 0)
            {
                _handleMarkers[_selectedHandle].Opacity = .2;
                _handleFrame.StrokeThickness = _handleFrameStrokeThickness / 2;

                if (_figureType == FigureType.Line || _figureType == FigureType.Quad)
                {
                    SetMagnifierLocation(e.GetCurrentPoint(null).Position);
                    ShowMagnifier();

                    MoveMagnifier(ConstrainPointToRect(_handlePoints[_selectedHandle], _sourceBitmap.PixelWidth, _sourceBitmap.PixelHeight));
                }
            }
        }

        Point ConstrainPointToRect(Point p, double width, double height)
        {
            p.X = Math.Min(width, Math.Max(0, p.X));
            p.Y = Math.Min(height, Math.Max(0, p.Y));
            return p;
        }

        void _imageCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_selectedHandle >= 0)
            {
                Point p = ConstrainPointToRect(e.GetCurrentPoint(_imageCanvas).Position, _sourceBitmap.PixelWidth, _sourceBitmap.PixelHeight);
                if (_figureType == FigureType.Rect)
                {
                    switch (_selectedHandle)
                    {
                        case 0:
                            p.X = Math.Min(p.X, _handlePoints[2].X - 50);
                            p.Y = Math.Min(p.Y, _handlePoints[2].Y - 50);
                            _handlePoints[0] = p;
                            _handlePoints[1] = new Point(_handlePoints[2].X, p.Y);
                            _handlePoints[3] = new Point(p.X, _handlePoints[2].Y);
                            break;

                        case 1:
                            p.X = Math.Max(p.X, _handlePoints[0].X + 50);
                            p.Y = Math.Min(p.Y, _handlePoints[2].Y - 50);
                            _handlePoints[1] = p;
                            _handlePoints[0] = new Point(_handlePoints[3].X, p.Y);
                            _handlePoints[2] = new Point(p.X, _handlePoints[3].Y);
                            break;

                        case 2:
                            p.X = Math.Max(p.X, _handlePoints[0].X + 50);
                            p.Y = Math.Max(p.Y, _handlePoints[0].Y + 50);
                            _handlePoints[2] = p;
                            _handlePoints[1] = new Point(p.X, _handlePoints[1].Y);
                            _handlePoints[3] = new Point(_handlePoints[3].X, p.Y);
                            break;

                        case 3:
                            p.X = Math.Min(p.X, _handlePoints[2].X - 50);
                            p.Y = Math.Max(p.Y, _handlePoints[0].Y + 50);
                            _handlePoints[3] = p;
                            _handlePoints[0] = new Point(p.X, _handlePoints[0].Y);
                            _handlePoints[2] = new Point(_handlePoints[2].X, p.Y);
                            break;

                    }

                    for (int i = 0; i < 4; i++)
                    {
                        _rectFigure[i] = new Point(_handlePoints[i].X, _handlePoints[i].Y);
                        SetLocation(_handleMarkers[i], _handlePoints[i]);
                    }

                    if (_currentTool == PhotoToolType.Crop)
                    {
                        _cropPanel.CropRect = new Rect(_rectFigure[0].X, _rectFigure[0].Y, _rectFigure[2].X - _rectFigure[0].X, _rectFigure[2].Y - _rectFigure[0].Y);
                    }
                }
                else
                {
                    _handlePoints[_selectedHandle] = p;
                    SetLocation(_handleMarkers[_selectedHandle], _handlePoints[_selectedHandle]);

                    if (_figureType == FigureType.Quad)
                    {
                        _quadFigure[_selectedHandle] = new Point(_handlePoints[_selectedHandle].X, _handlePoints[_selectedHandle].Y);

                        if (_currentTool == PhotoToolType.Perspective)
                        {
                            _warpPanel.PerspectiveQuad = _quadFigure;
                        }
                    }
                    else if (_figureType == FigureType.Line && _selectedHandle < 2)
                    {
                        _lineFigure[_selectedHandle] = new Point(_handlePoints[_selectedHandle].X, _handlePoints[_selectedHandle].Y);

                        if (_currentTool == PhotoToolType.Rotate)
                        {
                            _warpPanel.RotateAxis = _lineFigure;
                        }
                    }
                }

                DrawFigure(_figureType);

                if (_magnifierVisible)
                {
                    SetMagnifierLocation(e.GetCurrentPoint(null).Position);
                    MoveMagnifier(p);
                }
            }
        }

        void _imageCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_magnifierVisible)
            {
                HideMagnifier();
            }

            if (_selectedHandle >= 0)
            {
                HandleMoved(_selectedHandle);

                _handleFrame.StrokeThickness = _handleFrameStrokeThickness;
                _handleMarkers[_selectedHandle].Opacity = 1;
                _selectedHandle = -1;

                if (_currentTool == PhotoToolType.Crop)
                {
                    _cropPanel.UpdatePreview();
                }
                else if (_currentTool == PhotoToolType.Rotate || _currentTool == PhotoToolType.Perspective)
                {
                    _warpPanel.UpdatePreview();
                }
            }
        }

        void PhotoEditPage_LayoutUpdated(object sender, object e)
        {
            if (_topView.Width != App.Window.Bounds.Width || _topView.Height != App.Window.Bounds.Height)
            {
                _topView.Width = App.Window.Bounds.Width;
                _topView.Height = App.Window.Bounds.Height;
            }
        }

        public Dictionary<string, object> ParameterBlock
        {
            get { return _parameterBlock; }
        }

        double _maxZoom = 1;

        void ZoomToFit()
        {
            //if (_imageScrollViewer.ViewportWidth < _sourceBitmap.PixelWidth || _imageScrollViewer.ViewportHeight < _sourceBitmap.PixelHeight)
            {
                _handleFrameStrokeThickness = 2;
                _magnifierSourceSize = 100;
                
                double sx = _imageScrollViewer.ViewportWidth / _sourceBitmap.PixelWidth;
                double sy = _imageScrollViewer.ViewportHeight / _sourceBitmap.PixelHeight;

                TransformGroup tf = new TransformGroup();
                ScaleTransform stf = new ScaleTransform();
                TranslateTransform ttf = new TranslateTransform();

#if true
                if (sx < sy)
                {
                    if (sx > 1)
                    {
                        sx = 1;
                    }
                    _maxZoom = stf.ScaleX = stf.ScaleY = sx;
                    //ttf.Y = (_imageScrollViewer.ViewportHeight - sx * _sourceBitmap.PixelHeight) / 2;
                    //_imageCanvas.Width = _imageScrollViewer.ViewportWidth;
                    _imageCanvas.Width = _sourceBitmap.PixelWidth * sx;
                    _imageCanvas.Height = _sourceBitmap.PixelHeight * sx;
                }
                else 
                {
                    if (sy > 1)
                    {
                        sy = 1;
                    }
                    _maxZoom = stf.ScaleX = stf.ScaleY = sy;
                    //ttf.X = (_imageScrollViewer.ViewportWidth - sy * _sourceBitmap.PixelWidth) / 2;
                    //_imageCanvas.Height = _imageScrollViewer.ViewportHeight;
                    _imageCanvas.Height = _sourceBitmap.PixelHeight * sy;
                    _imageCanvas.Width = _sourceBitmap.PixelWidth * sy;
                }
#else
                //if (sx < sy && sx < 1)
                if (sx < sy)
                {
                    _maxZoom = stf.ScaleX = stf.ScaleY = sx;
                    //ttf.Y = (_imageScrollViewer.ViewportHeight - sx * _sourceBitmap.PixelHeight) / 2;
                    _imageCanvas.Width = _imageScrollViewer.ViewportWidth;
                    _imageCanvas.Height = _sourceBitmap.PixelHeight * sx;
                }
                //else if (sy < 1)
                else
                {
                    _maxZoom = stf.ScaleX = stf.ScaleY = sy;
                    //ttf.X = (_imageScrollViewer.ViewportWidth - sy * _sourceBitmap.PixelWidth) / 2;
                    _imageCanvas.Height = _imageScrollViewer.ViewportHeight;
                    _imageCanvas.Width = _sourceBitmap.PixelWidth * sy;
                }
#endif

                _handleFrameStrokeThickness /= _maxZoom;
                _handleFrame.StrokeThickness = _handleFrameStrokeThickness;
                
                tf.Children.Add(stf);
                _inverseViewMatrix = CGeometry.InvertMatrix(tf.Value);
                tf.Children.Add(ttf);

                _imageCanvas.RenderTransform = tf;


                //_zoomButton.Content = ((char)0xe1d8).ToString();
            }
        }

        private void Zoom(double factor)
        {
            if (_imageCanvas.RenderTransform is TransformGroup)
            {
                TransformGroup tg = _imageCanvas.RenderTransform as TransformGroup;
                if (tg.Children.Count == 2 && tg.Children[0] is ScaleTransform && tg.Children[1] is TranslateTransform)
                {
                    ScaleTransform ts = tg.Children[0] as ScaleTransform;
                    TranslateTransform tl = tg.Children[1] as TranslateTransform;

                    double scale = ts.ScaleX * factor;
                    //System.Diagnostics.Debug.WriteLine("scale = {0}", scale);
                    if (scale >= _maxZoom)
                    {
                        ts.ScaleX = ts.ScaleY = scale;

                        _imageCanvas.Width *= factor;
                        _imageCanvas.Height *= factor;

                        _magnifierSourceSize /= factor;

                        _handleFrameStrokeThickness /= factor;
                        _handleFrame.StrokeThickness = _handleFrameStrokeThickness;

                        TransformGroup itg = new TransformGroup();
                        itg.Children.Add(ts);
                        _inverseViewMatrix = CGeometry.InvertMatrix(itg.Value);
                    }
                }
            }
        }

        async void PhotoEditPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_sourceImageFile == null && string.IsNullOrEmpty(_imageId) == false)
            {
                _sourceImageFile = await Utilities.GetImageSourceFileAsync(_imageId);
            }

            await LoadImage(_sourceImageFile);

            SelectToolType(PhotoToolType.Filter);
            ZoomToFit();

            _warpPanel.OnSelectPhotoTool += _warpPanel_OnSelectPhotoTool;
        }

        private async Task LoadImage(StorageFile imageFile)
        {
            int width = 100;
            int height = 100;

            if (_originalBitmap == null)
            {
                if (imageFile != null)
                {
                    using (IRandomAccessStream fileStream = await imageFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        bool success = false;

                        try
                        {
                            ImageProperties imageProperties = await imageFile.Properties.GetImagePropertiesAsync();
                            _sourceImageSize = new Size(imageProperties.Width, imageProperties.Height);
                            width = (int)imageProperties.Width;
                            height = (int)imageProperties.Height;


                            _originalBitmap = new WriteableBitmap((int)imageProperties.Width, (int)imageProperties.Height);

                            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                            BitmapTransform transform;

                            bool rotate = decoder.OrientedPixelWidth != decoder.PixelWidth && decoder.OrientedPixelWidth == decoder.PixelHeight;

                            if (rotate)
                            {
                                transform = new BitmapTransform()
                                {
                                    ScaledHeight = Convert.ToUInt32(_originalBitmap.PixelWidth),
                                    ScaledWidth = Convert.ToUInt32(_originalBitmap.PixelHeight)
                                };
                            }
                            else
                            {
                                transform = new BitmapTransform()
                                {
                                    ScaledWidth = Convert.ToUInt32(_originalBitmap.PixelWidth),
                                    ScaledHeight = Convert.ToUInt32(_originalBitmap.PixelHeight)
                                };
                            }

                            PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                                BitmapPixelFormat.Bgra8,    // WriteableBitmap uses BGRA format
                                BitmapAlphaMode.Straight,
                                transform,
                                ExifOrientationMode.RespectExifOrientation,
                                //ExifOrientationMode.IgnoreExifOrientation,
                                ColorManagementMode.DoNotColorManage);

                            // An array containing the decoded image data, which could be modified before being displayed
                            byte[] sourcePixels = pixelData.DetachPixelData();

                            // Open a stream to copy the image contents to the WriteableBitmap's pixel buffer
                            using (Stream stream = _originalBitmap.PixelBuffer.AsStream())
                            {
                                await stream.WriteAsync(sourcePixels, 0, sourcePixels.Length);
                            }

                            _originalBitmap.Invalidate();

                            success = true;
                        }
                        catch (Exception ex)
                        {
                            Analytics.ReportError("Error reading image file", ex, 2, 702);
                        }

                        if (success == false)
                        {
                            await Cirros.Alerts.StandardAlerts.IOError();
                        }
                    }
                }
            }

            if (_originalBitmap != null)
            {
                try
                {
                    _sourceBitmap = _originalBitmap.Clone();
                    _imagePanel.Source = _sourceBitmap;
                    _sourceBitmap.Invalidate();
                }
                catch (OutOfMemoryException ex)
                {
                    Analytics.ReportError("Out of memory cloning image file", ex, 2, 703);
                }
                catch (Exception ex)
                {
                    Analytics.ReportError("Error cloning image file", ex, 2, 704);
                }

                if (_sourceBitmap == null)
                {
                    // this is bad - we failed to load the image above
                    // load the placeholder image so we don't crash later

                    await StandardAlerts.ImageErrorAlertAsync();

                    var uri = new System.Uri("ms-appx:///Assets/missing.png");
                    StorageFile file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
                    using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
#if false
                        BitmapImage src = new BitmapImage();
                        src.SetSource(fileStream);
                        _imagePanel.Source = src;
#else
                        BitmapImage bitmapImage = new BitmapImage();
                        _sourceBitmap = await BitmapFactory.FromStream(fileStream);
                        _imagePanel.Source = _sourceBitmap;
                        _sourceBitmap.Invalidate();
#endif
                    }
                }

                InitializeFigures();
                DrawFigure(_figureType);
            }

            //_zoomButton.IsEnabled = _imageScrollViewer.ViewportWidth < _sourceBitmap.PixelWidth || _imageScrollViewer.ViewportHeight < _sourceBitmap.PixelHeight;
        }


        private void InitializeFigures()
        {
            Size rectInset = new Size(0, 0);    // no inset for default crop
            Size quadInset = new Size(_sourceBitmap.PixelWidth / 6, _sourceBitmap.PixelHeight / 6);

            _lineFigure[0] = new Point(_sourceBitmap.PixelWidth / 2, 50);
            _lineFigure[1] = new Point(_sourceBitmap.PixelWidth / 2, _sourceBitmap.PixelHeight - 50);

            _rectFigure[0] = new Point(rectInset.Width, rectInset.Height);
            _rectFigure[1] = new Point(_sourceBitmap.PixelWidth - rectInset.Width, rectInset.Height);
            _rectFigure[2] = new Point(_sourceBitmap.PixelWidth - rectInset.Width, _sourceBitmap.PixelHeight - rectInset.Height);
            _rectFigure[3] = new Point(rectInset.Width, _sourceBitmap.PixelHeight - rectInset.Height);

            if (_refDestRect == Rect.Empty || _refDestRect.Width > 1 || _refDestRect.Height > 1)
            {
                _quadFigure[0] = new Point(quadInset.Width, quadInset.Height);
                _quadFigure[1] = new Point(_sourceBitmap.PixelWidth - quadInset.Width, quadInset.Height);
                _quadFigure[2] = new Point(_sourceBitmap.PixelWidth - quadInset.Width, _sourceBitmap.PixelHeight - quadInset.Height);
                _quadFigure[3] = new Point(quadInset.Width, _sourceBitmap.PixelHeight - quadInset.Height);
            }
            else
            {
                _quadFigure[0] = new Point(_refDestRect.Left * _sourceBitmap.PixelWidth, _refDestRect.Top * _sourceBitmap.PixelHeight);
                _quadFigure[1] = new Point(_refDestRect.Right * _sourceBitmap.PixelWidth, _refDestRect.Top * _sourceBitmap.PixelHeight);
                _quadFigure[2] = new Point(_refDestRect.Right * _sourceBitmap.PixelWidth, _refDestRect.Bottom * _sourceBitmap.PixelHeight);
                _quadFigure[3] = new Point(_refDestRect.Left * _sourceBitmap.PixelWidth, _refDestRect.Bottom * _sourceBitmap.PixelHeight);
            }
            //System.Diagnostics.Debug.WriteLine("({0},{1}) ({2},{3}) ({4},{5}) ({6},{7})",
            //    _quadFigure[0].X, _quadFigure[0].Y, _quadFigure[1].X, _quadFigure[1].Y, _quadFigure[2].X, _quadFigure[2].Y, _quadFigure[3].X, _quadFigure[3].Y);
        }

        //bool _zoomToFit = true;

        private void ShowApplyAlert(PhotoToolType currentTool, PhotoToolType newTool)
        {
            PhotoToolType[] tools = new PhotoToolType[2];
            tools[0] = currentTool;
            tools[1] = newTool;

            _applyPanel.Visibility = Visibility.Visible;
            _applyPanel.Tag = tools;
        }

        void SelectToolType(PhotoToolType toolType)
        {
            if (_currentTool != toolType)
            {
                switch (_currentTool)
                {
                    case PhotoToolType.None:
                        break;

                    case PhotoToolType.Filter:
                        if (_filterPanel.IsDirty)
                        {
                            ShowApplyAlert(_currentTool, toolType);
                            //return;
                        }
                        break;

                    case PhotoToolType.Crop:
                        if (_cropPanel.IsDirty)
                        {
                            ShowApplyAlert(_currentTool, toolType);
                            //return;
                        }
                        break;

                    case PhotoToolType.Rotate:
                        if (_warpPanel.IsDirty && toolType != PhotoToolType.Perspective)
                        {
                            ShowApplyAlert(_currentTool, toolType);
                            //return;
                        }
                        break;

                    case PhotoToolType.Perspective:
                        if (_warpPanel.IsDirty && toolType != PhotoToolType.Rotate)
                        {
                            ShowApplyAlert(_currentTool, toolType);
                            //return;
                        }
                        break;
                }

                _currentTool = toolType;

                switch (_currentTool)
                {
                    case PhotoToolType.None:
                        break;

                    case PhotoToolType.Filter:
                        ShowFilterPanel();
                        break;

                    case PhotoToolType.Crop:
                        ShowCropPanel();
                        break;

                    case PhotoToolType.Rotate:
                    case PhotoToolType.Perspective:
                        ShowWarpPanel();
                        break;
                }
            }
        }

        private void SetLocation(FrameworkElement fe, Point loc)
        {
            GeneralTransform generalTransform1 = _imageCanvas.TransformToVisual(_topCanvas);
            Point p = generalTransform1.TransformPoint(loc);

            fe.SetValue(Canvas.LeftProperty, p.X);
            fe.SetValue(Canvas.TopProperty, p.Y);
        }

        private static Color _controlColor = Colors.Red;
        private static double _dotSize = 16;

        private void SelectFigureType(FigureType figureType)
        {
            _figureType = figureType;

            if (_handleMarkers[0] == null)
            {
                _handleFrame.StrokeThickness = _handleFrameStrokeThickness;
                _imageCanvas.Children.Add(_handleFrame);

                double halfDot = _dotSize / 2;

                for (int i = 0; i < 4; i++)
                {
                    _handleMarkers[i] = new Ellipse();
                    _handleMarkers[i].Fill = _controlBrush;
                    _handleMarkers[i].Stroke = _controlBrush;
                    _handleMarkers[i].Width = _dotSize;
                    _handleMarkers[i].Height = _dotSize;
                    _handleMarkers[i].Margin = new Thickness(-halfDot, -halfDot, 0, 0);

                    _topCanvas.Children.Add(_handleMarkers[i]);
                }

                SetControlColor(_controlColor);
            }

            switch (figureType)
            {
                case FigureType.None:
                    _handleFrame.Visibility = Visibility.Collapsed;
                    _handleMarkers[0].Visibility = Visibility.Collapsed;
                    _handleMarkers[1].Visibility = Visibility.Collapsed;
                    _handleMarkers[2].Visibility = Visibility.Collapsed;
                    _handleMarkers[3].Visibility = Visibility.Collapsed;
                    _handleCount = 0;
                    break;

                case FigureType.Line:
                    _handlePoints[0] = _lineFigure[0];
                    _handlePoints[1] = _lineFigure[1];
                    _handleCount = 2;

                    SetLocation(_handleMarkers[0], _handlePoints[0]);
                    SetLocation(_handleMarkers[1], _handlePoints[1]);

                    _handleFrame.Visibility = Visibility.Visible;
                    _handleMarkers[0].Visibility = Visibility.Visible;
                    _handleMarkers[1].Visibility = Visibility.Visible;
                    _handleMarkers[2].Visibility = Visibility.Collapsed;
                    _handleMarkers[3].Visibility = Visibility.Collapsed;
                    break;

                case FigureType.Rect:
                    _handlePoints[0] = _rectFigure[0];
                    _handlePoints[1] = _rectFigure[1];
                    _handlePoints[2] = _rectFigure[2];
                    _handlePoints[3] = _rectFigure[3];
                    _handleCount = 4;

                    SetLocation(_handleMarkers[0], _handlePoints[0]);
                    SetLocation(_handleMarkers[1], _handlePoints[1]);
                    SetLocation(_handleMarkers[2], _handlePoints[2]);
                    SetLocation(_handleMarkers[3], _handlePoints[3]);

                    _handleFrame.Visibility = Visibility.Visible;
                    _handleMarkers[0].Visibility = Visibility.Visible;
                    _handleMarkers[1].Visibility = Visibility.Visible;
                    _handleMarkers[2].Visibility = Visibility.Visible;
                    _handleMarkers[3].Visibility = Visibility.Visible;
                    break;

                case FigureType.Quad:
                    _handlePoints[0] = _quadFigure[0];
                    _handlePoints[1] = _quadFigure[1];
                    _handlePoints[2] = _quadFigure[2];
                    _handlePoints[3] = _quadFigure[3];
                    _handleCount = 4;

                    SetLocation(_handleMarkers[0], _handlePoints[0]);
                    SetLocation(_handleMarkers[1], _handlePoints[1]);
                    SetLocation(_handleMarkers[2], _handlePoints[2]);
                    SetLocation(_handleMarkers[3], _handlePoints[3]);

                    _handleFrame.Visibility = Visibility.Visible;
                    _handleMarkers[0].Visibility = Visibility.Visible;
                    _handleMarkers[1].Visibility = Visibility.Visible;
                    _handleMarkers[2].Visibility = Visibility.Visible;
                    _handleMarkers[3].Visibility = Visibility.Visible;
                    break;
            }

            DrawFigure(_figureType);
        }

        private void HandleMoved(int selectedHandle)
        {
            if (selectedHandle >= 0 && selectedHandle < 4)
            {
                switch (_figureType)
                {
                    case FigureType.None:
                        break;

                    case FigureType.Line:
                        if (selectedHandle < 2)
                        {
                            _lineFigure[selectedHandle] = _handlePoints[selectedHandle];
                        }
                        break;

                    case FigureType.Rect:
                        _rectFigure[selectedHandle] = _handlePoints[selectedHandle];
                        break;

                    case FigureType.Quad:
                        _quadFigure[selectedHandle] = _handlePoints[selectedHandle];
                        break;
                }
            }
        }

        private void DrawFigure(FigureType figureType)
        {
            PointCollection pc;

            switch (figureType)
            {
                case FigureType.None:
                    break;

                case FigureType.Line:
                    pc = new PointCollection();
                    pc.Add(_handlePoints[0]);
                    pc.Add(_handlePoints[1]);
                    _handleFrame.Points = pc;
                    break;

                case FigureType.Rect:
                    pc = new PointCollection();
                    pc.Add(_handlePoints[0]);
                    pc.Add(_handlePoints[1]);
                    pc.Add(_handlePoints[2]);
                    pc.Add(_handlePoints[3]);
                    pc.Add(_handlePoints[0]);
                    _handleFrame.Points = pc;
                    break;

                case FigureType.Quad:
                    pc = new PointCollection();
                    pc.Add(_handlePoints[0]);
                    pc.Add(_handlePoints[1]);
                    pc.Add(_handlePoints[2]);
                    pc.Add(_handlePoints[3]);
                    pc.Add(_handlePoints[0]);
                    _handleFrame.Points = pc;
                    break;
            }
        }

        void _filterPanel_OnApply(object sender, ApplyEventArgs e)
        {
            if (e.Bitmap is WriteableBitmap)
            {
                _sourceBitmap = e.Bitmap as WriteableBitmap;
                _imagePanel.Source = _sourceBitmap;
                _filterPanel.SetBitmap(_sourceBitmap);
                //ZoomToFit();
                Zoom(1);

                InitializeFigures();
            }
        }

        void _cropPanel_OnApply(object sender, ApplyEventArgs e)
        {
            if (e.Bitmap is WriteableBitmap)
            {
                Rect destRectInPixels = Rect.Empty;

                if (_refDestRect != Rect.Empty)
                {
                    destRectInPixels = new Rect(
                        new Point(_refDestRect.Left * _sourceBitmap.PixelWidth, _refDestRect.Top * _sourceBitmap.PixelHeight),
                        new Point(_refDestRect.Right * _sourceBitmap.PixelWidth, _refDestRect.Bottom * _sourceBitmap.PixelHeight)
                        );
                }

                _sourceBitmap = e.Bitmap as WriteableBitmap;
                _imagePanel.Source = _sourceBitmap;
                _sourceBitmap.Invalidate();
                ZoomToFit();

                if (destRectInPixels != Rect.Empty)
                {
                    destRectInPixels.X -= _rectFigure[0].X;
                    destRectInPixels.Y -= _rectFigure[0].Y;

                    _refDestRect = new Rect(
                            new Point(destRectInPixels.Left / _sourceBitmap.PixelWidth, destRectInPixels.Top / _sourceBitmap.PixelHeight),
                            new Point(destRectInPixels.Right / _sourceBitmap.PixelWidth, destRectInPixels.Bottom / _sourceBitmap.PixelHeight)
                        );

                    for (int i = 0; i < _quadFigure.Count; i++)
                    {
                        _quadFigure[i] = new Point(_quadFigure[i].X - _rectFigure[0].X, _quadFigure[i].Y - _rectFigure[0].Y);
                    }
                    _warpPanel.PerspectiveQuad = _quadFigure;
                }

                _refDestRect = Rect.Empty;
                _refDestSize = Size.Empty;

                InitializeFigures();

                SelectFigureType(_figureType);
                DrawFigure(_figureType);

                _cropPanel.CropRect = new Rect(_rectFigure[0].X, _rectFigure[0].Y, _rectFigure[2].X - _rectFigure[0].X, _rectFigure[2].Y - _rectFigure[0].Y);
                _cropPanel.SetBitmap(_sourceBitmap);
                _cropPanel.UpdatePreview();
            }
        }

        void _warpPanel_OnApply(object sender, ApplyEventArgs e)
        {
            if (e.Bitmap is WriteableBitmap)
            {
                _sourceBitmap = e.Bitmap as WriteableBitmap;
                _imagePanel.Source = _sourceBitmap;
                _warpPanel.SetBitmap(_sourceBitmap);
                ZoomToFit();

                _refDestRect = new Rect(
                    new Point(e.DestRect.Left / _sourceBitmap.PixelWidth, e.DestRect.Top / _sourceBitmap.PixelHeight),
                    new Point(e.DestRect.Right / _sourceBitmap.PixelWidth, e.DestRect.Bottom / _sourceBitmap.PixelHeight)
                    );

                _refDestSize = e.DestSize;

                InitializeFigures();

                if (e.DestRect != Rect.Empty)
                {
                    _quadFigure[0] = new Point(e.DestRect.Left, e.DestRect.Top);
                    _quadFigure[1] = new Point(e.DestRect.Right, e.DestRect.Top);
                    _quadFigure[2] = new Point(e.DestRect.Right, e.DestRect.Bottom);
                    _quadFigure[3] = new Point(e.DestRect.Left, e.DestRect.Bottom);
                }

                SelectFigureType(_figureType);
                DrawFigure(_figureType);
            }
        }

        private void ShowFilterPanel()
        {
            _filterButton.IsChecked = true;
            _cropButton.IsChecked = false;
            _warpButton.IsChecked = false;

            _cropPanel.Visibility = Visibility.Collapsed;
            _warpPanel.Visibility = Visibility.Collapsed;
            _filterPanel.Visibility = Visibility.Visible;

            _filterPanel.SetBitmap(_sourceBitmap);

            SelectFigureType(FigureType.None);
        }

        private void ShowWarpPanel()
        {
            _warpButton.IsChecked = true;
            _filterButton.IsChecked = false;
            _cropButton.IsChecked = false;

            _cropPanel.Visibility = Visibility.Collapsed;
            _filterPanel.Visibility = Visibility.Collapsed;
            _warpPanel.Visibility = Visibility.Visible;

            _warpPanel.SetBitmap(_sourceBitmap);

            SelectFigureType(_currentTool == PhotoToolType.Rotate ? FigureType.Line : FigureType.Quad);

            _warpPanel.RotateAxis = _lineFigure;
            _warpPanel.PerspectiveQuad = _quadFigure;
            _warpPanel.RefSize = _refDestSize;
            _warpPanel.IsDirty = false;
        }

        private void ShowCropPanel()
        {
            _cropButton.IsChecked = true;
            _filterButton.IsChecked = false;
            _warpButton.IsChecked = false;

            _warpPanel.Visibility = Visibility.Collapsed;
            _filterPanel.Visibility = Visibility.Collapsed;
            _cropPanel.Visibility = Visibility.Visible;

            _cropPanel.CropRect = new Rect(_rectFigure[0].X, _rectFigure[0].Y, _rectFigure[2].X - _rectFigure[0].X, _rectFigure[2].Y - _rectFigure[0].Y);

            _cropPanel.SetBitmap(_sourceBitmap);
            _cropPanel.UpdatePreview();

            SelectFigureType(FigureType.Rect);

            _cropPanel.IsDirty = false;
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            var eventData = new Dictionary<string, string> {
                { "action", "cancel" },
                { "source", _cmdSource },
                { "filter", _filterPanel.ApplyCounts },
                { "warp", _warpPanel.ApplyCounts },
                { "crop", _cropPanel.ApplyCounts } 
            };
            Analytics.ReportEvent("image", eventData);
            Analytics.ReportEvent("image-cancel");
#if KT22
            if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;
            }
#else
            if (_cmdSource == "newdrawing")
            {
                App.Navigate(typeof(HomePage2), null);
            }
            else if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;
            }
#endif
        }

        private async void DoneClick(object sender, RoutedEventArgs e)
        {
            switch (_currentTool)
            {
                case PhotoToolType.None:
                    break;

                case PhotoToolType.Filter:
                    if (_filterPanel.IsDirty)
                    {
                        ShowApplyAlert(_currentTool, _currentTool);
                        return;
                    }
                    break;

                case PhotoToolType.Crop:
                    if (_cropPanel.IsDirty)
                    {
                        ShowApplyAlert(_currentTool, _currentTool);
                        return;
                    }
                    break;

                case PhotoToolType.Rotate:
                case PhotoToolType.Perspective:
                    if (_warpPanel.IsDirty)
                    {
                        ShowApplyAlert(_currentTool, _currentTool);
                        return;
                    }
                    break;
            }

            var eventData = new Dictionary<string, string> {
                { "action", "done" },
                { "source", _cmdSource },
                { "filter", _filterPanel.ApplyCounts },
                { "warp", _warpPanel.ApplyCounts },
                { "crop", _cropPanel.ApplyCounts } 
            };

            Analytics.ReportEvent("image", eventData);
            Analytics.ReportEvent("image-save");

            if (_cmdSource != "editimage")
            {
                _imageId = Guid.NewGuid().ToString();
            }

            StorageFile file = await SaveTemporaryImage(_imageId + ".jpg");
            Globals.Events.ImageChanged(file);

            if (_parameterBlock == null)
            {
                _parameterBlock = new Dictionary<string, object>();
            }

            _parameterBlock["imageId"] = _imageId;
            _parameterBlock["sourceName"] = _sourceImageName;
            _parameterBlock["refDestRect"] = _refDestRect;
            _parameterBlock["refDestSize"] = _refDestSize;
            _parameterBlock["pixelWidth"] = _sourceBitmap.PixelWidth;
            _parameterBlock["pixelHeight"] = _sourceBitmap.PixelHeight;
            _parameterBlock["originalWidth"] = _sourceImageSize.Width;
            _parameterBlock["originalHeight"] = _sourceImageSize.Height;

            if (_cmdSource == "newdrawing")
            {
                _parameterBlock["command"] = _cmdSource;
                _parameterBlock["originalFile"] = _sourceImageFile;
                _parameterBlock["sourceFile"] = file;

                App.Navigate(typeof(NewDrawingPage), _parameterBlock);
            }
            else if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;

                if (Globals.CommandProcessor != null && _cmdSource != "editimage")
                {
                    Globals.CommandProcessor.Invoke("A_InsertImage", _parameterBlock);
                }
            }
        }

        private async Task<StorageFile> SaveTemporaryImage(string imageId)
        {
            StorageFile file = null;

            try
            {
                file = await Globals.TemporaryImageFolder.CreateFileAsync(imageId, CreationCollisionOption.ReplaceExisting);
                using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.ReadWrite), memStream = new InMemoryRandomAccessStream())
                {
                    await _sourceBitmap.ToStreamAsJpeg(fileStream);
                }
            }
            catch
            {
                // save failed
            }

            return file;
        }

        private void FilterButtonClick(object sender, RoutedEventArgs e)
        {
            SelectToolType(PhotoToolType.Filter);
        }

        private void CropButtonClick(object sender, RoutedEventArgs e)
        {
            SelectToolType(PhotoToolType.Crop);
        }

        private void WarpButtonClick(object sender, RoutedEventArgs e)
        {
            if (_warpPanel != null)
            {
                PhotoToolType toolType = _warpPanel.ToolType;
                SelectToolType(toolType);
            }
        }

        private async void RevertClick(object sender, RoutedEventArgs e)
        {
            await LoadImage(_sourceImageFile);

            ZoomToFit();

            _refDestRect = Rect.Empty;
            _refDestSize = Size.Empty;

            InitializeFigures();
            SelectFigureType(_figureType);
            DrawFigure(_figureType);

            switch (_currentTool)
            {
                case PhotoToolType.None:
                    break;

                case PhotoToolType.Filter:
                    _filterPanel.SetBitmap(_sourceBitmap);
                    break;

                case PhotoToolType.Crop:
                    _cropPanel.CropRect = new Rect(_rectFigure[0].X, _rectFigure[0].Y, _rectFigure[2].X - _rectFigure[0].X, _rectFigure[2].Y - _rectFigure[0].Y);
                    _cropPanel.SetBitmap(_sourceBitmap);
                    break;

                case PhotoToolType.Rotate:
                case PhotoToolType.Perspective:
                    _warpPanel.SetBitmap(_sourceBitmap);
                    _warpPanel.PerspectiveQuad = _quadFigure;
                    _warpPanel.RefSize = _refDestSize;
                    break;
            }
        }

        Brush _controlBrush = new SolidColorBrush(Colors.White);

        private void SetControlColor(Color color)
        {
            _controlColor = color;

            _controlBrush = new SolidColorBrush(color);
            _zoomButton.Foreground = _controlBrush;
            _handleMarkers[0].Fill = _controlBrush;
            _handleMarkers[0].Stroke = _controlBrush;
            _handleMarkers[1].Fill = _controlBrush;
            _handleMarkers[1].Stroke = _controlBrush;
            _handleMarkers[2].Fill = _controlBrush;
            _handleMarkers[2].Stroke = _controlBrush;
            _handleMarkers[3].Fill = _controlBrush;
            _handleMarkers[3].Stroke = _controlBrush;
            _handleFrame.Stroke = _controlBrush;

            _warpPanel.FigureColor = color;
        }

        private void _redButton_Click(object sender, RoutedEventArgs e)
        {
            SetControlColor(Colors.Red);
        }

        private void _greenButton_Click(object sender, RoutedEventArgs e)
        {
            SetControlColor(Colors.LimeGreen);
        }

        private void _blueButton_Click(object sender, RoutedEventArgs e)
        {
            SetControlColor(Colors.Blue);
        }

        private void _yellowButton_Click(object sender, RoutedEventArgs e)
        {
            SetControlColor(Colors.Yellow);
        }

        private void _zoomButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomToFit();
        }

        private void _zoomInButton_Click(object sender, RoutedEventArgs e)
        {
            Zoom(1.25);
        }

        private void _zoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            Zoom(.8);
        }

        private void InitializeMagnifier()
        {
            _magifierPopup = new Popup();
            _magifierPopup.HorizontalOffset = 100;
            _magifierPopup.VerticalOffset = 100;

            Border b = new Border();
            b.BorderThickness = new Thickness(3);
            b.BorderBrush = (Brush)(Application.Current.Resources["SettingsDarkForeground"]);

            Grid g = new Grid();
            g.Width = _magnifierDestSize;
            g.Height = _magnifierDestSize;
            b.Child = g;

            _magifierImage = new Image();
            _magifierImage.Width = _magnifierDestSize;
            _magifierImage.Height = _magnifierDestSize;
            g.Children.Add(_magifierImage);

            _magnifierCanvas = new Canvas();
            g.Children.Add(_magnifierCanvas);

            _magifierPopup.Child = b;
        }

        private void TakeMagnifierSnapshot()
        {
            _magnifierSourceBitmap = _sourceBitmap;

            GeneralTransform generalTransform1 = _topCanvas.TransformToVisual(_imageCanvas);
            _magnifierSourceOffset = generalTransform1.TransformPoint(new Point(0, 0));
        }

        private void ShowMagnifier()
        {
            TakeMagnifierSnapshot();
            ShowMagnifierSnapshot(true);
        }

        private void SetMagnifierLocation(Point pm)
        {
            if (pm.X < (App.Window.Bounds.Width - _magnifierDestSize - 100))
            {
                _magifierPopup.HorizontalOffset = pm.X + 100;
            }
            else
            {
                _magifierPopup.HorizontalOffset = pm.X - _magnifierDestSize - 100;
            }

            _magifierPopup.VerticalOffset = Math.Max(40, pm.Y - _magnifierDestSize);
        }

        private void MoveMagnifier(Point p0)
        {
            GeneralTransform generalTransform1 = _imagePanel.TransformToVisual(_topCanvas);
            Point pm = generalTransform1.TransformPoint(p0);

            if (_inverseViewMatrix != Matrix.Identity)
            {
                pm = _inverseViewMatrix.Transform(pm);
            }

            if (_magifierPopup.IsOpen == false)
            {
                _magifierPopup.IsOpen = true;
            }

            Point loc = new Point(pm.X - _magnifierSourceSize / 2, pm.Y - _magnifierSourceSize / 2);

            if (loc.X != _magnifierSourceLocation.X || loc.Y != _magnifierSourceLocation.Y)
            {
                _magnifierSourceLocation = loc;
                int x = (int)(_magnifierSourceLocation.X + _magnifierSourceOffset.X);
                int y = (int)(_magnifierSourceLocation.Y + _magnifierSourceOffset.Y);

                int x0 = (int)Math.Min(_magnifierSourceBitmap.PixelWidth - _magnifierSourceSize, Math.Max(x, 0));
                int y0 = (int)Math.Min(_magnifierSourceBitmap.PixelHeight - _magnifierSourceSize, Math.Max(y, 0));

                int size = (int)(_dotSize / 2);

                WriteableBitmap wbm = _magnifierSourceBitmap.Crop(x0, y0, (int)_magnifierSourceSize, (int)_magnifierSourceSize);

                _magnifierCanvas.Children.Clear();

                List<Point> points = new List<Point>();
                Rect clipRect = new Rect(0, 0, _magnifierDestSize, _magnifierDestSize);

                double mscale = _magnifierDestSize / _magnifierSourceSize;
                double mxoff = ((_magnifierSourceSize / 2) + x - x0) * mscale;
                double myoff = ((_magnifierSourceSize / 2) + y - y0) * mscale;

                for (int i = 0; i < _handleCount; i++)
                {
                    Point pp = generalTransform1.TransformPoint(_handlePoints[i]);

                    if (_inverseViewMatrix != Matrix.Identity)
                    {
                        pp = _inverseViewMatrix.Transform(pp);
                    }

                    pp.X = pp.X - pm.X + mxoff;
                    pp.Y = pp.Y - pm.Y + myoff;

                    points.Add(pp);

                    if (clipRect.Contains(pp))
                    {
                        Ellipse dot = new Ellipse();
                        dot.SetValue(Canvas.LeftProperty, pp.X);
                        dot.SetValue(Canvas.TopProperty, pp.Y);
                        dot.Fill = _controlBrush;
                        dot.Stroke = _controlBrush;
                        dot.Width = _dotSize;
                        dot.Height = _dotSize;
                        dot.Margin = new Thickness(-size, -size, 0, 0);
                        dot.Opacity = .2;
                        _magnifierCanvas.Children.Add(dot);
                    }
                }

                switch (_figureType)
                {
                    case FigureType.None:
                        break;

                    case FigureType.Line:
                        ClipLine(clipRect, points[0], points[1]);
                        break;

                    case FigureType.Rect:
                    case FigureType.Quad:
                        ClipLine(clipRect, points[0], points[1]);
                        ClipLine(clipRect, points[1], points[2]);
                        ClipLine(clipRect, points[2], points[3]);
                        ClipLine(clipRect, points[3], points[0]);
                        break;
                }

                _magifierImage.Source = wbm;
            }
        }

        private static List<Point> IntersectLinePolyline(Point A, Point B, List<Point> C)
        {
            List<Point> intersections = new List<Point>();

            if (C.Count > 1)
            {
                Point p0 = C[0];

                for (int i = 1; i < C.Count; i++)
                {
                    Point p1 = C[i];
                    Point I;

                    if (Construct.IntersectLineLine(p0, p1, A, B, out I))
                    {
                        intersections.Add(I);
                    }

                    p0 = p1;
                }
            }

            return intersections;
        }
        
        private bool ClipLine(Rect clipRect, Point point1, Point point2)
        {
            Point clip1 = new Point();
            Point clip2 = new Point();

            bool visible = false;

            bool i1 = clipRect.Contains(point1);
            bool i2 = clipRect.Contains(point2);

            if (i1 && i2)
            {
                // both points are inside - no clipping needed
                clip1 = point1;
                clip2 = point2;
                visible = true;
            }
            else if (Math.Min(point1.X, point2.X) > clipRect.Right)
            {
                // line does not interesct
            }
            else if (Math.Min(point1.Y, point2.Y) > clipRect.Bottom)
            {
                // line does not interesct
            }
            else if (Math.Max(point1.X, point2.X) < clipRect.Left)
            {
                // line does not interesct
            }
            else if (Math.Max(point1.Y, point2.Y) < clipRect.Top)
            {
                // line does not interesct
            }
            else
            {
                List<Point> pc = new List<Point>();
                pc.Add(new Point(clipRect.Left, clipRect.Top));
                pc.Add(new Point(clipRect.Right, clipRect.Top));
                pc.Add(new Point(clipRect.Right, clipRect.Bottom));
                pc.Add(new Point(clipRect.Left, clipRect.Bottom));
                pc.Add(new Point(clipRect.Left, clipRect.Top));

                List<Point> intersections = IntersectLinePolyline(point1, point2, pc);

                if (i1)
                {
                    // point1 is inside, point2 is outside
                    clip1 = point1;
                    if (intersections.Count == 1)
                    {
                        clip2 = intersections[0];
                        visible = true;
                    }
                    else
                    {

                    }
                }
                else if (i2)
                {
                    // point2 is inside, point1 is outside
                    clip2 = point2;
                    if (intersections.Count == 1)
                    {
                        clip1 = intersections[0];
                        visible = true;
                    }
                    else
                    {

                    }
                }
                else
                {
                    // line might intersect - zero or two intersection points
                    if (intersections.Count == 2)
                    {
                        clip1 = intersections[0];
                        clip2 = intersections[1];
                        visible = true;
                    }
                    else
                    {

                    }
                }
            }

            if (visible)
            {
                Line hl = new Line();
                hl.X1 = clip1.X;
                hl.Y1 = clip1.Y;
                hl.X2 = clip2.X;
                hl.Y2 = clip2.Y;
                hl.Stroke = _controlBrush;
                hl.StrokeThickness = 1;
                _magnifierCanvas.Children.Add(hl);
            }

            return visible;
        }

        private void HideMagnifier()
        {
            ShowMagnifierSnapshot(false);
        }

        private void ShowMagnifierSnapshot(bool show)
        {
            if (_magnifierVisible == false && show && _magnifierSourceBitmap != null)
            {
                _magnifierVisible = true;
                _magifierPopup.IsOpen = true;
            }
            else if (_magnifierVisible && show == false)
            {
                _magnifierVisible = false;
                _magifierPopup.IsOpen = false;
                _magnifierSourceBitmap = null;
            }
        }

        private async void _doApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _applyPanel.Visibility = Visibility.Collapsed;

            PhotoToolType[] tools = (PhotoToolType[])_applyPanel.Tag;

            switch (tools[0])
            {
                case PhotoToolType.None:
                    break;

                case PhotoToolType.Filter:
                    _filterPanel.Apply();
                    break;

                case PhotoToolType.Crop:
                    _cropPanel.Apply();
                    break;

                case PhotoToolType.Rotate:
                case PhotoToolType.Perspective:
                    await _warpPanel.Apply();
                    break;
            }

            switch (_currentTool)
            {
                case PhotoToolType.None:
                    break;

                case PhotoToolType.Filter:
                    _filterPanel.SetBitmap(_sourceBitmap);
                    break;

                case PhotoToolType.Crop:
                    _cropPanel.SetBitmap(_sourceBitmap);
                    break;

                case PhotoToolType.Rotate:
                case PhotoToolType.Perspective:
                    _warpPanel.SetBitmap(_sourceBitmap);
                    break;
            }
        }

        private void _dontApplyButton_Click(object sender, RoutedEventArgs e)
        {
            PhotoToolType[] tools = (PhotoToolType[])_applyPanel.Tag;

            switch (tools[0])
            {
                case PhotoToolType.None:
                    break;

                case PhotoToolType.Filter:
                    _filterPanel.IsDirty = false;
                    break;

                case PhotoToolType.Crop:
                    _cropPanel.IsDirty = false;
                    break;

                case PhotoToolType.Rotate:
                case PhotoToolType.Perspective:
                    _warpPanel.IsDirty = false;
                    break;
            }

            _applyPanel.Visibility = Visibility.Collapsed;
        }

        private void _backButton_Click(object sender, RoutedEventArgs e)
        {
#if KT22
#else
            if (_cmdSource == "newdrawing")
            {
                App.Navigate(typeof(HomePage2), null);
            }
#endif
        }
    }

    public enum PhotoToolType
    {
        None,
        Perspective,
        Rotate,
        Crop,
        Filter
    }

   public class ApplyEventArgs : EventArgs
    {
        public WriteableBitmap Bitmap { get; private set; }

        public Rect DestRect { get; private set; }

        public Size DestSize { get; private set; }

        public ApplyEventArgs(WriteableBitmap bitmap)
        {
            Bitmap = bitmap;
            DestRect = Rect.Empty;
            DestSize = Size.Empty;
        }

        public ApplyEventArgs(WriteableBitmap bitmap, Rect destRect, Size destSize)
        {
            Bitmap = bitmap;
            DestRect = destRect;
            DestSize = destSize;
        }
    }

    public class PhotoToolEventArgs : EventArgs
    {
        public PhotoToolType Tool { get; private set; }

        public PhotoToolEventArgs(PhotoToolType tool)
        {
            Tool = tool;
        }
    }
}
