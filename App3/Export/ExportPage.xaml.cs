using Cirros;
using Cirros.Drawing;
using CirrosUI;
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
using Microsoft.UI.Xaml.Navigation;
using App3;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Cirros8.Export
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExportPage : Page
    {
        bool _isMetric = false;
        bool _userUpdate = false;

        double _width = 1000;
        double _aspect = 1;
        
        public ExportPage()
        {
            this.InitializeComponent();

            var bounds = App.Window.Bounds;

            _isMetric = Globals.ActiveDrawing.PaperUnit == Unit.Millimeters;

            _unitComboBox.SelectedIndex = _isMetric ? 1 : 0;

            _topView.Width = bounds.Width;
            _topView.Height = bounds.Height;

            SelectFormat(Globals.ExportFormat);

            _width = Globals.ActiveDrawing.PaperSize.Width * Globals.ExportResolution;
            _aspect = Globals.ActiveDrawing.PaperSize.Height / Globals.ActiveDrawing.PaperSize.Width;

            UpdateValues();

            _userUpdate = true;

            Analytics.TrackPageView("ExportPage");
        }

        private void SelectFormat(string format)
        {
            Globals.ExportFormat = format;

            _svgButton.IsChecked = Globals.ExportFormat == "SVG";
            _jpgButton.IsChecked = Globals.ExportFormat == "JPG";
            _pngButton.IsChecked = Globals.ExportFormat == "PNG";
            _pdfButton.IsChecked = Globals.ExportFormat == "PDF";
            _dxfButton.IsChecked = Globals.ExportFormat == "DXF";

            if (Globals.ExportFormat == "JPG" || Globals.ExportFormat == "PNG")
            {
                _imageOptions.Visibility = Visibility.Visible;
                _baseOptions.Visibility = Visibility.Collapsed;
                _imageShowFrame.IsChecked = Globals.ExportShowFrame;
                _imageShowGrid.IsChecked = Globals.ExportShowGrid;
            }
            else
            {
                _imageOptions.Visibility = Visibility.Collapsed;
                _baseOptions.Visibility = Visibility.Visible;
                _baseShowFrame.IsChecked = Globals.ExportShowFrame;
            }
        }

        private async void _okButton_Click(object sender, RoutedEventArgs e)
        {
            if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;
            }

            Globals.ExportImageSize = (int)Math.Max(_width, _width * _aspect);

            await FileHandling.ExportDrawingAsync(Globals.ExportFormat, Globals.ExportShowFrame, Globals.ExportShowGrid);
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;
            }
        }

        private void _formatButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton)
            {
                SelectFormat(((ToggleButton)sender).Tag as string);
            }
        }

        private void showFrame_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox)
            {
                if (Globals.ExportShowFrame != (bool)((CheckBox)sender).IsChecked)
                {
                    Globals.ExportShowFrame = (bool)((CheckBox)sender).IsChecked;

                    _baseShowFrame.IsChecked = Globals.ExportShowFrame;
                    _imageShowFrame.IsChecked = Globals.ExportShowFrame;
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == _unitComboBox)
            {
                _isMetric = _unitComboBox.SelectedIndex == 1;
                _resolutionLabel.Text = _isMetric ? "Pixels/centimeter" : "Pixels/inch";
                _docResoulution.Value = _isMetric ? Globals.ExportResolution / 2.54 : Globals.ExportResolution;

                UpdateValues();
            }
        }

        private void numberBox_OnValueChanged(object sender, CirrosUI.ValueChangedEventArgs e)
        {
            if (_userUpdate)
            {
                if (sender == _pixelWidth)
                {
                    _width = _pixelWidth.Value;
                }
                else if (sender == _pixelHeight)
                {
                    _width = Math.Round((_pixelHeight.Value + .5) / _aspect);
                }
                else if (sender == _docWidth)
                {
                    if (_isMetric)
                    {
                        _width = (_docWidth.Value / 25.4) * Globals.ExportResolution;
                    }
                    else
                    {
                        _width = _docWidth.Value * Globals.ExportResolution;
                    }
                }
                else if (sender == _docHeight)
                {
                    if (_isMetric)
                    {
                        _width = ((_docHeight.Value / 25.4) / _aspect) * Globals.ExportResolution;
                    }
                    else
                    {
                        _width = (_docHeight.Value / _aspect) * Globals.ExportResolution;
                    }
                }
                else if (sender == _docResoulution)
                {
                    if (_isMetric)
                    {
                        Globals.ExportResolution = ((NumberBox1)sender).Value * 2.54;
                    }
                    else
                    {
                        Globals.ExportResolution = ((NumberBox1)sender).Value;
                    }
                }

                UpdateValues();
            }
        }

        private void UpdateValues()
        {
            _userUpdate = false;

            _pixelWidth.Value = _width;
            _pixelHeight.Value = _width * _aspect;
            _docWidth.Value = _width / Globals.ExportResolution;
            _docHeight.Value = _width * _aspect / Globals.ExportResolution;

            if (_isMetric)
            {
                _docWidth.Value *= 25.4;
                _docHeight.Value *= 25.4;
            }

            _userUpdate = true;
        }

        private void _showGrid_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox)
            {
                if (Globals.ExportShowGrid != (bool)((CheckBox)sender).IsChecked)
                {
                    Globals.ExportShowGrid = (bool)((CheckBox)sender).IsChecked;

                    _imageShowGrid.IsChecked = Globals.ExportShowGrid;
                }
            }
        }
    }
}
