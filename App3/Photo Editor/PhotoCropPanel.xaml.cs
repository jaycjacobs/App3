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
    public sealed partial class PhotoCropPanel : UserControl
    {
        public PhotoCropPanel()
        {
            this.InitializeComponent();

            _previewArea.SizeChanged += _previewArea_SizeChanged;
            this.Loaded += PhotoCropPanel_Loaded;
        }

        int _cropCount = 0;

        private Rect _cropRect;
        private bool _isDirty = false;

        private WriteableBitmap _bitmap = null;
        private WriteableBitmap _previewBitmap = null;
        private byte[] _previewSourceBytes = null;

        public event ApplyHandler OnApply;
        public delegate void ApplyHandler(object sender, ApplyEventArgs e);

        void PhotoCropPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (_previewArea.RenderSize.Width == 0)
            {
                _previewArea.Measure(new Size(1000, 1000));
            }

            this.SizeChanged += PhotoCropPanel_SizeChanged;
        }

        private void PhotoCropPanel_SizeChanged(object sender, SizeChangedEventArgs e)
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

        public string ApplyCounts
        {
            get
            {
                string s = string.Format("C{0}", _cropCount);
                _cropCount = 0;
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

        void _previewArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize == e.NewSize) return;
            if (Cirros.Utility.Utilities.__checkSizeChanged(16, sender)) return;

            UpdatePreviewFromSource();
        }

        public Rect CropRect
        {
            set
            {
                _cropRect = value;
                _croppedWidth.Text = ((int)_cropRect.Width).ToString();
                _croppedHeight.Text = ((int)_cropRect.Height).ToString();
            }
        }

        public void UpdatePreview()
        {
            if (_cropRect.IsEmpty == false)
            {
                _previewBitmap = _bitmap.Crop(_cropRect);
                _previewImage.Source = _previewBitmap;

                IsDirty = true;
            }
        }

        public void Apply()
        {
            if (_isDirty)
            {
                _cropCount++;

                if (OnApply != null)
                {
                    if (_cropRect.IsEmpty == false)
                    {
                        WriteableBitmap bm = _bitmap.Crop(_cropRect);
                        OnApply(this, new ApplyEventArgs(bm));
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

                _previewBitmap = _bitmap.Crop(_cropRect).Resize(previewWidth, previewHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
                //_previewBitmap = _bitmap.Resize(previewWidth, previewHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
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

            _originalWidth.Text = _bitmap.PixelWidth.ToString();
            _originalHeight.Text = _bitmap.PixelHeight.ToString();

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
    }
}
