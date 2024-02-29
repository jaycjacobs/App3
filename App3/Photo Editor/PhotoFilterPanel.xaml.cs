using Cirros.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace Cirros8.ModalDialogs
{
    public sealed partial class PhotoFilterPanel : UserControl
    {
        private enum FilterType
        {
            None,
            Grayscale,
            Contrast,
            Brightness,
            DetectEdges,
            Rotate90,
            Rotate180,
            Rotate270
        }

        private enum EDType
        {
            Laplacian3x3,
            Laplacian5x5,
            LaplacianOfGaussian,
            Laplacian3x3OfGaussian3x3Filter,
            Laplacian3x3OfGaussian5x5Filter1,
            Laplacian3x3OfGaussian5x5Filter2,
            Laplacian5x5OfGaussian3x3Filter,
            Laplacian5x5OfGaussian5x5Filter1,
            Laplacian5x5OfGaussian5x5Filter2,
            Sobel3x3,
            Prewitt3x3,
            Kirsch3x3,
        }

        int _contrastCount = 0;
        int _brightnessCount = 0;
        int _detectCount = 0;
        int _grayscaleCount = 0;
        int _rotateCount = 0;

        EDType _edType = EDType.Laplacian3x3;
        FilterType _currentFilter = FilterType.None;

        double _brightness = 0;
        double _contrast = 0;

        bool _isDirty = false;

        private WriteableBitmap _bitmap = null;
        private WriteableBitmap _previewBitmap = null;
        private byte[] _previewSourceBytes = null;

        public event ApplyHandler OnApply;
        public delegate void ApplyHandler(object sender, ApplyEventArgs e);

        public PhotoFilterPanel()
        {
            this.InitializeComponent();

            _edgePanel.Visibility = Visibility.Collapsed;
            _contrastPanel.Visibility = Visibility.Collapsed;
            _brightnessPanel.Visibility = Visibility.Collapsed;

            _previewArea.SizeChanged += _previewArea_SizeChanged;
            _edgeComboBox.SelectionChanged += _edgeComboBox_SelectionChanged;
            this.Loaded += PhotoFilterPanel_Loaded;
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

        public string ApplyCounts
        {
            get
            {
                string s = string.Format("C{0},B{1},D{2},G{3},R{4}", _contrastCount, _brightnessCount, _detectCount, _grayscaleCount, _rotateCount);
                _contrastCount = _brightnessCount = _detectCount = _grayscaleCount = _rotateCount = 0;
                return s;
            }
        }

        void PhotoFilterPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (_previewArea.RenderSize.Width == 0)
            {
                _previewArea.Measure(new Size(1000, 1000));
            }
        }

        void _previewArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize == e.NewSize) return;
            if (Cirros.Utility.Utilities.__checkSizeChanged(17, sender)) return;

            UpdatePreviewFromSource();
        }

        public void Apply()
        {
            if (_isDirty)
            {
                switch (_currentFilter)
                {
                    case FilterType.None:
                        break;

                    case FilterType.Grayscale:
                        Effects.GrayScaleFilter(_bitmap);
                        _grayscaleCount++;
                        break;

                    case FilterType.DetectEdges:
                        DetectEdges(_bitmap, _edType);
                        _detectCount++;
                        break;

                    case FilterType.Contrast:
                        Effects.ContrastFilter(_bitmap, _contrast);
                        _contrast = 0;
                        _contrastSlider.Value = _contrast;
                        _contrastCount++;
                        break;

                    case FilterType.Brightness:
                        Effects.BrightnessFilter(_bitmap, (int)(_brightness * 255));
                        _brightness = 0;
                        _brightnessSlider.Value = _brightness;
                        _brightnessCount++;
                        break;

                    case FilterType.Rotate90:
                        if (OnApply != null)
                        {
                            WriteableBitmap bm = _bitmap.Rotate(270);
                            OnApply(this, new ApplyEventArgs(bm));
                        }
                        _rotateCount++;
                        break;

                    case FilterType.Rotate180:
                        if (OnApply != null)
                        {
                            WriteableBitmap bm = _bitmap.Rotate(180);
                            OnApply(this, new ApplyEventArgs(bm));
                        }
                        _rotateCount++;
                        break;

                    case FilterType.Rotate270:
                        if (OnApply != null)
                        {
                            WriteableBitmap bm = _bitmap.Rotate(90);
                            OnApply(this, new ApplyEventArgs(bm));
                        }
                        _rotateCount++;
                        break;
                }

                _previewSourceBytes = null;

                _bitmap.Invalidate();

                IsDirty = false;
                SelectFilter(FilterType.None);
            }
        }

        private void SelectFilter(FilterType filter)
        {
            if (_currentFilter != filter)
            {
                switch (_currentFilter)
                {
                    case FilterType.None:
                        break;

                    case FilterType.Grayscale:
                        _grayScaleButton.IsChecked = false;
                        break;

                    case FilterType.Contrast:
                        _contrastButton.IsChecked = false;
                        _contrastPanel.Visibility = Visibility.Collapsed;
                        break;

                    case FilterType.Brightness:
                        _brightnessButton.IsChecked = false;
                        _brightnessPanel.Visibility = Visibility.Collapsed;
                        break;

                    case FilterType.DetectEdges:
                        if (_detectEdgesButton.IsChecked == true)
                        {
                            _detectEdgesButton.IsChecked = false;
                            _edgePanel.Visibility = Visibility.Collapsed;
                        }
                        break;

                    case FilterType.Rotate90:
                        _rotateButton.IsChecked = filter == FilterType.Rotate180;
                        break;

                    case FilterType.Rotate180:
                        _rotateButton.IsChecked = filter == FilterType.Rotate270;
                        break;

                    case FilterType.Rotate270:
                        _rotateButton.IsChecked = false;
                        break;
                }

                UpdatePreviewFromSource();

                _currentFilter = filter;

                switch (_currentFilter)
                {
                    case FilterType.None:
                        _contrastPanel.Visibility = Visibility.Collapsed;
                        _brightnessPanel.Visibility = Visibility.Collapsed;
                        _edgePanel.Visibility = Visibility.Collapsed;
                        break;

                    case FilterType.Grayscale:
                        Effects.GrayScaleFilter(_previewBitmap);
                        IsDirty = true;
                        break;

                    case FilterType.Contrast:
                        _contrastPanel.Visibility = Visibility.Visible;
                        if (_contrast != 0)
                        {
                            Effects.ContrastFilter(_previewBitmap, _contrast);
                            IsDirty = true;
                        }
                        break;

                    case FilterType.Brightness:
                        _brightnessPanel.Visibility = Visibility.Visible;
                        if (_brightness != 0)
                        {
                            Effects.BrightnessFilter(_previewBitmap, (int)(_brightness * 255));
                            IsDirty = true;
                        }
                        break;

                    case FilterType.DetectEdges:
                        _edgePanel.Visibility = Visibility.Visible;
                        DetectEdges(_previewBitmap, _edType);
                        IsDirty = true;
                        break;

                    case FilterType.Rotate90:
                        _previewSourceBytes = null;
                        _previewBitmap = _previewBitmap.Rotate(270);
                        _previewImage.Source = _previewBitmap;
                        IsDirty = true;
                        break;

                    case FilterType.Rotate180:
                        _previewSourceBytes = null;
                        _previewBitmap = _previewBitmap.Rotate(180);
                        _previewImage.Source = _previewBitmap;
                        IsDirty = true;
                        break;

                    case FilterType.Rotate270:
                        _previewSourceBytes = null;
                        _previewBitmap = _previewBitmap.Rotate(90);
                        _previewImage.Source = _previewBitmap;
                        IsDirty = true;
                        break;
                }

                _previewBitmap.Invalidate();
            }
        }

        private void UpdatePreviewFromSource()
        {
            if (_previewSourceBytes != null)
            {
                _previewBitmap.FromByteArray(_previewSourceBytes);
            }
            else if (_bitmap != null && _previewArea.RenderSize.Width > 0 && _previewArea.RenderSize.Height > 0)
            {
                int previewWidth = (int)_previewArea.RenderSize.Width;
                int previewHeight = (int)_previewArea.RenderSize.Height;

                double hs = _previewArea.RenderSize.Width / (double)_bitmap.PixelWidth;
                double vs = _previewArea.RenderSize.Height / (double)_bitmap.PixelHeight;

                if (vs < hs)
                {
                    previewWidth = (int)(_bitmap.PixelWidth * vs);
                }
                else
                {
                    previewHeight = (int)(_bitmap.PixelHeight * hs);
                }

                _previewBitmap = _bitmap.Resize(previewWidth, previewHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
                _previewSourceBytes = _previewBitmap.ToByteArray();
                _previewImage.Source = _previewBitmap;
                _previewBitmap.Invalidate();
            }

            IsDirty = false;
        }

        public void SetBitmap(WriteableBitmap wbm)
        {
            _bitmap = wbm;
            _previewSourceBytes = null;

            SelectFilter(FilterType.None);

            if (_previewArea.RenderSize.Width == 0)
            {
                _previewArea.Measure(new Size(1000, 1000));
            }
            else
            {
                UpdatePreviewFromSource();
            }
        }

        private void _applyButton_Click(object sender, RoutedEventArgs e)
        {
            Apply();
        }

        private void _grayScaleButton_Click(object sender, RoutedEventArgs e)
        {
            SelectFilter(_currentFilter == FilterType.Grayscale ? FilterType.None : FilterType.Grayscale);
        }

        private void _contrastButton_Click(object sender, RoutedEventArgs e)
        {
            SelectFilter(_currentFilter == FilterType.Contrast ? FilterType.None : FilterType.Contrast);
        }

        private void _brightnessButton_Click(object sender, RoutedEventArgs e)
        {
            SelectFilter(_currentFilter == FilterType.Brightness ? FilterType.None : FilterType.Brightness);
        }

        private void _detectEdgesButton_Click(object sender, RoutedEventArgs e)
        {
            SelectFilter(_currentFilter == FilterType.DetectEdges ? FilterType.None : FilterType.DetectEdges);
        }

        private void _rotateButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_currentFilter)
            {
                default:
                    SelectFilter(FilterType.Rotate90);
                    break;

                case FilterType.Rotate90:
                    SelectFilter(FilterType.Rotate180);
                    break;

                case FilterType.Rotate180:
                    SelectFilter(FilterType.Rotate270);
                    break;

                case FilterType.Rotate270:
                    SelectFilter(FilterType.None);
                    break;
            }
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (sender == _brightnessSlider)
            {
                if (_brightness != _brightnessSlider.Value)
                {
                    _brightness = Math.Min(1.0, Math.Max(-1.0, _brightnessSlider.Value));

                    UpdatePreviewFromSource();
                    Effects.BrightnessFilter(_previewBitmap, (int)(_brightness * 255));
                    _previewBitmap.Invalidate();

                    IsDirty = _brightness != 0;
                }
    
                _brightnessValueLabel.Text = _brightness.ToString("n2");
            }
            else if (sender == _contrastSlider)
            {
                if (_contrast != _contrastSlider.Value)
                {
                    _contrast = Math.Min(1.0, Math.Max(-1.0, _contrastSlider.Value));

                    UpdatePreviewFromSource();
                    Effects.ContrastFilter(_previewBitmap, _contrast);
                    _previewBitmap.Invalidate();

                    IsDirty = _contrast != 0;
                }
               
                _contrastValueLabel.Text = _contrast.ToString("n2");
            }
        }

        private void DetectEdges(WriteableBitmap bitmap, EDType edType)
        {
            _edType = edType;

            switch (_edType)
            {
                case EDType.Laplacian3x3:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Laplacian3x3);
                    break;

                case EDType.Laplacian5x5:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Laplacian5x5);
                    break;

                case EDType.LaplacianOfGaussian:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.LaplacianOfGaussian);
                    break;

                case EDType.Laplacian3x3OfGaussian3x3Filter:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Gaussian3x3, 1.0 / 16.0, 0);
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Laplacian3x3);
                    break;

                case EDType.Laplacian3x3OfGaussian5x5Filter1:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Gaussian5x5Type1, 1.0 / 159.0, 0);
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Laplacian3x3);
                    break;

                case EDType.Laplacian3x3OfGaussian5x5Filter2:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Gaussian5x5Type2, 1.0 / 256.0, 0);
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Laplacian3x3);
                    break;

                case EDType.Laplacian5x5OfGaussian3x3Filter:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Gaussian3x3, 1.0 / 16.0, 0);
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Laplacian5x5);
                    break;

                case EDType.Laplacian5x5OfGaussian5x5Filter1:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Gaussian5x5Type1, 1.0 / 159.0, 0);
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Laplacian5x5);
                    break;

                case EDType.Laplacian5x5OfGaussian5x5Filter2:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Gaussian5x5Type2, 1.0 / 256.0, 0);
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Laplacian5x5);
                    break;

                case EDType.Kirsch3x3:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Kirsch3x3Horizontal, IedMatrix.Kirsch3x3Vertical, 1.0, 0);
                    break;

                case EDType.Prewitt3x3:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Prewitt3x3Horizontal, IedMatrix.Prewitt3x3Vertical, 1.0, 0);
                    break;

                case EDType.Sobel3x3:
                    Effects.ConvolutionFilter(bitmap, IedMatrix.Sobel3x3Horizontal, IedMatrix.Sobel3x3Vertical, 1.0, 0);
                    break;
            }
        }

        private void _edgeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && e.AddedItems[0] is FrameworkElement)
            {
                if (((FrameworkElement)e.AddedItems[0]).Tag is string)
                {
                    UpdatePreviewFromSource();

                    switch (((FrameworkElement)e.AddedItems[0]).Tag as string)
                    {
                        default:
                        case "Laplacian3x3":
                            DetectEdges(_previewBitmap, EDType.Laplacian3x3);
                            break;

                        case "Laplacian5x5":
                            DetectEdges(_previewBitmap, EDType.Laplacian5x5);
                            break;

                        case "LaplacianOfGaussian":
                            DetectEdges(_previewBitmap, EDType.LaplacianOfGaussian);
                            break;

                        case "Sobel3x3":
                            DetectEdges(_previewBitmap, EDType.Sobel3x3);
                            break;

                        case "Laplacian3x3OfGaussian3x3Filter":
                            DetectEdges(_previewBitmap, EDType.Laplacian3x3OfGaussian3x3Filter);
                            break;

                        case "Laplacian3x3OfGaussian5x5Filter1":
                            DetectEdges(_previewBitmap, EDType.Laplacian3x3OfGaussian5x5Filter1);
                            break;

                        case "Laplacian3x3OfGaussian5x5Filter2":
                            DetectEdges(_previewBitmap, EDType.Laplacian3x3OfGaussian5x5Filter2);
                            break;

                        case "Laplacian5x5OfGaussian3x3Filter":
                            DetectEdges(_previewBitmap, EDType.Laplacian5x5OfGaussian3x3Filter);
                            break;

                        case "Laplacian5x5OfGaussian5x5Filter1":
                            DetectEdges(_previewBitmap, EDType.Laplacian5x5OfGaussian5x5Filter1);
                            break;

                        case "Laplacian5x5OfGaussian5x5Filter2":
                            DetectEdges(_previewBitmap, EDType.Laplacian5x5OfGaussian5x5Filter2);
                            break;

                        case "Prewitt3x3":
                            DetectEdges(_previewBitmap, EDType.Prewitt3x3);
                            break;

                        case "Kirsch3x3":
                            DetectEdges(_previewBitmap, EDType.Kirsch3x3);
                            break;
                    }
                }
                IsDirty = true;
            }
        }
    }
}
