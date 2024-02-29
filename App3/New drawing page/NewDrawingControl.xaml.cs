using System;
using System.Collections.Generic;
using System.Linq;
using Cirros;
using Cirros.Utility;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CirrosUI;
using Windows.Foundation;
using KT22;
using Cirros.Drawing;
using App3;

namespace Cirros8
{
    public sealed partial class NewDrawingControl : UserControl
    {
        double _paperUnitScale = 1;     // _paperUnitScale is PaperUnit : INCHES
        double _width = 1;
        double _height = 1;
        int _scale = 1;

        private Unit _paperUnit;
        private Unit _modelUnit;

        private string _theme;

        bool _architecturalScale = false;
        int _architecturalScaleCount = 0;

        List<ComboBoxItem> _englishScales = new List<ComboBoxItem>();
        List<ComboBoxItem> _decimalScales = new List<ComboBoxItem>();

        ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        ApplicationDataContainer _drawingSettings;

        List<DrawingSizeItem> _drawingSizes = new List<DrawingSizeItem>();

        public NewDrawingControl()
        {
            this.InitializeComponent();

            this.Loaded += NewDrawingControl_Loaded;
            _startButton.Click += _start_Click;

        }

        Rect _imageFrame = Rect.Empty;

        async void NewDrawingControl_Loaded(object sender, RoutedEventArgs e)
        {
            ContentControl cc = this.Parent as ContentControl;
            if (cc != null)
            {
                this.Width = cc.ActualWidth;
                this.Height = cc.ActualHeight;
            }

            if (await Cirros.Alerts.StandardAlerts.LastChanceToSaveAsync())
            {
#if SIBERIA
                App.Navigate(typeof(KTDrawingPage), "restore");
#endif
            }

            loadSettings();

            populate();
            update();

            _sizeComboBox.SelectionChanged += new SelectionChangedEventHandler(_sizeComboBox_SelectionChanged);
            _scaleComboBox.SelectionChanged += new SelectionChangedEventHandler(_scaleComboBox_SelectionChanged);

            _rbInch.Checked += new RoutedEventHandler(_rbInch_Checked);
            _rbMm.Checked += new RoutedEventHandler(_rbMm_Checked);

            _rbAppUnit1.Checked += new RoutedEventHandler(_rbAppUnit1_Checked);
            _rbAppUnit2.Checked += new RoutedEventHandler(_rbAppUnit2_Checked);
            _rbAppUnit3.Checked += new RoutedEventHandler(_rbAppUnit3_Checked);

            _rbLandscape.Checked += _rbLandscape_Checked;
            _rbPortrait.Checked += _rbLandscape_Checked;

            _widthBox.OnValueChanged += _widthBox_OnValueChanged;
            _heightBox.OnValueChanged += _heightBox_OnValueChanged;

            _themeComboBox.SelectionChanged += _themeComboBox_SelectionChanged;
        }

        public void SetBaseImageFromDictionary(Dictionary<string, object> dictionary)
        {
            Point origin = new Point(0, 0);

            if (dictionary.ContainsKey("refDestRect") && dictionary.ContainsKey("refDestSize") && dictionary.ContainsKey("pixelWidth") && dictionary.ContainsKey("pixelHeight"))
            {
                Rect refRect = (Rect)dictionary["refDestRect"];
                Size refSize = (Size)dictionary["refDestSize"];

                if (dictionary.ContainsKey("unit"))
                {
                    Unit unit = (Unit)dictionary["unit"];
                    if (unit == Unit.Millimeters)
                    {
                        refSize.Height /= 25.4;
                        refSize.Width /= 25.4;
                    }
                    else if (unit == Unit.Feet)
                    {
                        refSize.Height *= 12;
                        refSize.Width *= 12;
                    }
                }

                Point refP1 = new Point(refRect.Left, refRect.Bottom);
                Point refP2 = new Point(refRect.Right, refRect.Top);

                int pixelWidth = (int)dictionary["pixelWidth"];
                int pixelHeight = (int)dictionary["pixelHeight"];

                // The size should be maximum of full resolution or 75% of the drawing size

                double hs = Globals.ActiveDrawing.PaperSize.Width / pixelWidth;
                double vs = Globals.ActiveDrawing.PaperSize.Height / pixelHeight;
                double s = Math.Min(1.0 / 96.0, Math.Min(hs, vs) * .75);

                Size imageSize = new Size(s * pixelWidth, s * pixelHeight);

                origin = Construct.RoundXY(new Point(origin.X - imageSize.Width / 2, origin.Y + imageSize.Height / 2));

                Point c1;
                Point c2;

                if (refP1.X != refP2.X && refP1.Y != refP2.Y)
                {
                    if (refSize == Size.Empty || refSize.Width == 0 || refSize.Height == 0)
                    {
                        // refSize is not set
                        Point p1 = new Point(refP1.X * imageSize.Width, refP1.Y * imageSize.Height);
                        Point p2 = new Point(refP2.X * imageSize.Width, refP2.Y * imageSize.Height);

                        origin = Construct.RoundXY(new Point(origin.X + p1.X, origin.Y - (imageSize.Height - p1.Y)));
                        c1 = new Point(origin.X - p1.X, origin.Y + (imageSize.Height - p1.Y));
                        c2 = new Point(c1.X + imageSize.Width, c1.Y - imageSize.Height);
                    }
                    else
                    {
                        // refSize is set, this is a scaled image
                        Size paperRefSize = Globals.ActiveDrawing.ModelToPaperSize(refSize);
                        imageSize = new Size(paperRefSize.Width / Math.Abs(refP2.X - refP1.X), paperRefSize.Height / Math.Abs(refP2.Y - refP1.Y));

                        Point p1 = new Point(refP1.X * imageSize.Width, refP1.Y * imageSize.Height);
                        Point p2 = new Point(refP2.X * imageSize.Width, refP2.Y * imageSize.Height);

                        c1 = new Point(origin.X - p1.X, origin.Y + (imageSize.Height - p1.Y));
                        c2 = new Point(c1.X + imageSize.Width, c1.Y - imageSize.Height);
                    }
                }
                else
                {
                    c1 = origin;
                    c2 = new Point(c1.X + imageSize.Width, c1.Y - imageSize.Height);
                }

                _imageFrame = new Rect(c1, c2);

                if (dictionary.ContainsKey("sourceFile"))
                {
                    StorageFile file = dictionary["sourceFile"] as StorageFile;
                }
            }
        }

        void _start_Click(object sender, RoutedEventArgs e)
        {
            Frame root = App.Window.Frame;
            System.Diagnostics.Debug.Assert(root != null, "Window.Current.Content is not a Frame");
            if (!(root.Content is KTDrawingPage))
            {
                DrawingSizeItem item = _sizeComboBox.SelectedItem as DrawingSizeItem;
                if (item == null || item.Width == 0)
                {
                    _width = _widthBox.Value / _paperUnitScale;
                    _height = _heightBox.Value / _paperUnitScale;
                }

                _drawingSettings.Values["paper_width"] = _width;
                _drawingSettings.Values["paper_height"] = _height;
                _drawingSettings.Values["scale"] = (int)_scaleBox.Value;
                _drawingSettings.Values["archscale"] = _architecturalScale;
                _drawingSettings.Values["paper_unit"] = _paperUnit.ToString();
                _drawingSettings.Values["model_unit"] = _modelUnit.ToString();
                _drawingSettings.Values["theme"] = _theme;

                root.Navigate(typeof(KTDrawingPage), "new");
            }
        }

        private void initializeFromSettings()
        {
            try
            {
                _width = (double)_drawingSettings.Values["paper_width"];
                _height = (double)_drawingSettings.Values["paper_height"];
                _scale = (int)_drawingSettings.Values["scale"];
                _architecturalScale = (bool)_drawingSettings.Values["archscale"];

                _modelUnit = Utilities.UnitFromString((string)_drawingSettings.Values["model_unit"]);
                _paperUnit = Utilities.UnitFromString((string)_drawingSettings.Values["paper_unit"]);

                _theme = (string)_drawingSettings.Values["theme"];
            }
            catch (Exception ex)
            {
                Analytics.ReportError("initializeFromSettings() failed", ex, 2, 720);
            }
        }

        private void loadSettings()
        {
            bool initialize = false;

            if (_localSettings.Containers.Keys.Contains("drawing"))
            {
                _drawingSettings = _localSettings.Containers["drawing"];

                try
                {
                    initializeFromSettings();
                }
                catch
                {
                    // this will never fail because initializeFromSettings() itself is in a try/catch block
                    initialize = true;
                }
            }
            else
            {
                _drawingSettings = _localSettings.CreateContainer("drawing", ApplicationDataCreateDisposition.Always);
                initialize = true;
            }

            if (initialize && _drawingSettings != null)
            {
                _drawingSettings.Values["paper_width"] = 17.0;
                _drawingSettings.Values["paper_height"] = 11.0;
                _drawingSettings.Values["scale"] = 1;
                _drawingSettings.Values["archscale"] = false;
                _drawingSettings.Values["paper_unit"] = Unit.Inches.ToString();
                _drawingSettings.Values["model_unit"] = Unit.Inches.ToString();
                _drawingSettings.Values["theme"] = "light";

                initializeFromSettings();
            }
        }

        private void populate()
        {
            _drawingSizes.Add(new DrawingSizeItem("Custom", 0, 0));
            _drawingSizes.Add(new DrawingSizeItem("ANSI A", 11, 8.5));
            _drawingSizes.Add(new DrawingSizeItem("ANSI B", 17, 11));
            _drawingSizes.Add(new DrawingSizeItem("ANSI C", 22, 17));
            _drawingSizes.Add(new DrawingSizeItem("ANSI D", 34, 22));
            _drawingSizes.Add(new DrawingSizeItem("ANSI E", 44, 34));
            _drawingSizes.Add(new DrawingSizeItem("ANSI F", 40, 28));
            _drawingSizes.Add(new DrawingSizeItem("ISO A0", 46.81102362, 33.11023622));
            _drawingSizes.Add(new DrawingSizeItem("ISO A1", 33.11023622, 23.38582677));
            _drawingSizes.Add(new DrawingSizeItem("ISO A2", 23.38582677, 16.53543307));
            _drawingSizes.Add(new DrawingSizeItem("ISO A3", 16.53543307, 11.69291339));
            _drawingSizes.Add(new DrawingSizeItem("ISO A4", 11.69291339, 8.267716535));
            _sizeComboBox.ItemsSource = _drawingSizes;

            addScaleListItem(_englishScales, "Custom", 0);
            addScaleListItem(_englishScales, "12\"=1'0\" (full)", 1);
            addScaleListItem(_englishScales, "3/32\"=1'0\"", 128);
            addScaleListItem(_englishScales, "1/8\"=1'0\"", 96);
            addScaleListItem(_englishScales, "3/16\"=1'0\"", 64);
            addScaleListItem(_englishScales, "1/4\"=1'0\"", 48);
            addScaleListItem(_englishScales, "3/8\"=1'0\"", 32);
            addScaleListItem(_englishScales, "1/2\"=1'0\"", 24);
            addScaleListItem(_englishScales, "3/4\"=1'0\"", 16);
            addScaleListItem(_englishScales, "1\"=1'0\"", 12);
            addScaleListItem(_englishScales, "1 1/2\"=1'0\"", 8);
            addScaleListItem(_englishScales, "3\"=1'0\"", 4);
            _architecturalScaleCount = _englishScales.Count;

            addScaleListItem(_englishScales, "1\"=1'", 12);
            addScaleListItem(_englishScales, "1\"=10'", 120);
            addScaleListItem(_englishScales, "1\"=20'", 240);
            addScaleListItem(_englishScales, "1\"=30'", 360);
            addScaleListItem(_englishScales, "1\"=40'", 480);
            addScaleListItem(_englishScales, "1\"=50'", 600);
            addScaleListItem(_englishScales, "1\"=60'", 720);
            addScaleListItem(_englishScales, "1\"=100'", 1200);

            addScaleListItem(_decimalScales, "Custom", 0);
            addScaleListItem(_decimalScales, "1:1 (full)", 1);
            addScaleListItem(_decimalScales, "1:2", 2);
            addScaleListItem(_decimalScales, "1:5", 5);
            addScaleListItem(_decimalScales, "1:10", 10);
            addScaleListItem(_decimalScales, "1:20", 20);
            addScaleListItem(_decimalScales, "1:50", 50);
            addScaleListItem(_decimalScales, "1:100", 100);
            addScaleListItem(_decimalScales, "1:200", 200);
            addScaleListItem(_decimalScales, "1:500", 500);
            addScaleListItem(_decimalScales, "1:1000", 1000);
            addScaleListItem(_decimalScales, "1:1250", 1250);

            _themeComboBox.ItemsSource = Globals.Themes.Values;
            _themeComboBox.SelectedItem = _theme == null ? Globals.Theme : Globals.Themes[_theme];

            _scaleBox.Value = _scale;

            if (_paperUnit == Unit.Inches && _modelUnit == Unit.Feet)
            {
                _scaleComboBox.ItemsSource = _englishScales;
            }
            else
            {
                _scaleComboBox.ItemsSource = _decimalScales;
            }
        }

        private void update()
        {
            updateDrawingSize();
            updateUnits();

            _scalePanel.Visibility = _scaleComboBox.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            _rbLandscape.IsChecked = _width > _height;
        }

        private void updateScaleList()
        {
            object source;

            if (_paperUnit == Unit.Inches && _modelUnit == Unit.Feet)
            {
                source = _englishScales;
            }
            else
            {
                source = _decimalScales;
            }

            if (_scaleComboBox.ItemsSource == null)
            {
                _scaleComboBox.ItemsSource = source;
            }
            else if (_scaleComboBox.ItemsSource != source)
            {
                _scaleComboBox.ItemsSource = source;
                _scaleComboBox.SelectedIndex = 1;
                _architecturalScale = true;
                return;
            }

            for (int i = 1; i < _scaleComboBox.Items.Count; i++)
            {
                if (_scale == (int)((ComboBoxItem)_scaleComboBox.Items[i]).Tag)
                {
                    _scaleComboBox.SelectedIndex = i;
                    _architecturalScale = _modelUnit == Unit.Feet ? _scaleComboBox.SelectedIndex > 0 && _scaleComboBox.SelectedIndex < _architecturalScaleCount : false;
                    return;
                }
            }

            _scaleComboBox.SelectedIndex = 0;
        }

        private void updateDrawingSize()
        {
            foreach (DrawingSizeItem item in _drawingSizes)
            {
                if (item.Width == _width && item.Height == _height)
                {
                    _sizeComboBox.SelectedItem = item;
                    _rbLandscape.IsChecked = true;
                    return;
                }
                else if (item.Width == _height && item.Height == _width)
                {
                    _sizeComboBox.SelectedItem = item;
                    _rbPortrait.IsChecked = true;
                    return;
                }
            }

            _widthBox.Value = _width;
            _heightBox.Value = _height;
            _sizeComboBox.SelectedItem = _drawingSizes[0];
            _orientationPanel.Visibility = Visibility.Collapsed;
        }

        private void updateUnits()
        {
            if (_paperUnit == Unit.Inches)
            {
                _paperUnitScale = 1;
                _rbInch.IsChecked = true;

                if (_rbAppUnit1.Tag == null || (Unit)_rbAppUnit1.Tag != Unit.Inches)
                {
                    _rbAppUnit1.Content = "Inches";
                    _rbAppUnit1.Tag = Unit.Inches;
                    _rbAppUnit2.Content = "Feet";
                    _rbAppUnit2.Tag = Unit.Feet;
                    _rbAppUnit3.Visibility = Visibility.Collapsed;

                    _widthBox.Precision = 3;
                    _heightBox.Precision = 3;

                    if (_modelUnit == Unit.Inches)
                    {
                        _rbAppUnit1.IsChecked = true;
                    }
                    else
                    {
                        _modelUnit = Unit.Feet;
                        _rbAppUnit2.IsChecked = true;
                    }
                }
            }
            else if (_paperUnit == Unit.Millimeters)
            {
                _paperUnitScale = 25.4;
                _rbMm.IsChecked = true;

                if (_rbAppUnit1.Tag == null || (Unit)_rbAppUnit1.Tag != Unit.Millimeters)
                {
                    _rbAppUnit1.Content = "Millimeters";
                    _rbAppUnit1.Tag = Unit.Millimeters;
                    _rbAppUnit2.Content = "Centimeters";
                    _rbAppUnit2.Tag = Unit.Centimeters;
                    _rbAppUnit3.Content = "Meters";
                    _rbAppUnit3.Tag = Unit.Meters;
                    _rbAppUnit3.Visibility = Visibility.Visible;

                    _widthBox.Precision = 1;
                    _heightBox.Precision = 1;

                    _rbAppUnit1.IsChecked = true;

                    if (_modelUnit == Unit.Millimeters)
                    {
                        _rbAppUnit1.IsChecked = true;
                    }
                    else if (_modelUnit == Unit.Centimeters)
                    {
                        _rbAppUnit2.IsChecked = true;
                    }
                    else
                    {
                        _rbAppUnit3.IsChecked = true;
                    }
                }
            }
            else
            {
                throw new Exception("Invalid unit: " + _paperUnit);
            }

            updateScaleList();
            sizeOrUnitChanged();
        }

        private void addScaleListItem(List<ComboBoxItem> list, string name, int scale)
        {
            ComboBoxItem item = new ComboBoxItem();
            item.Content = name;
            item.Tag = scale;
            list.Add(item);
        }

        private void selectDrawingSize(DrawingSizeItem item)
        {
            if (_sizeComboBox.SelectedItem == _drawingSizes[0])
            {
                _orientationPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                _orientationPanel.Visibility = Visibility.Visible;

                if ((bool)_rbLandscape.IsChecked)
                {
                    _width = item.Width;
                    _height = item.Height;
                }
                else
                {
                    _width = item.Height;
                    _height = item.Width;
                }
            }

            sizeOrUnitChanged();
        }

        private void sizeOrUnitChanged()
        {
            _widthBox.Value = _width * _paperUnitScale;
            _heightBox.Value = _height * _paperUnitScale;

            bool custom = _sizeComboBox.SelectedIndex == 0;
            _widthBox.IsEnabled = _heightBox.IsEnabled = custom;

            DrawPreview();
        }

        void _sizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_sizeComboBox.SelectedIndex >= 0)
            {
                selectDrawingSize(_sizeComboBox.SelectedItem as DrawingSizeItem);
            }
        }

        void _scaleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_scaleComboBox.SelectedIndex >= 0)
            {
                try
                {
                    ComboBoxItem cbi = _scaleComboBox.SelectedItem as ComboBoxItem;

                    if ((int)cbi.Tag == 0)
                    {
                        _scalePanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        _scalePanel.Visibility = Visibility.Collapsed;
                        _scaleBox.Value = (int)cbi.Tag;
                        _scale = (int)cbi.Tag;
                    }

                    _architecturalScale = _modelUnit == Unit.Feet ? _scaleComboBox.SelectedIndex > 0 && _scaleComboBox.SelectedIndex < _architecturalScaleCount : false;
                }
                catch
                {
                }
                DrawPreview();
            }
        }

        void _rbInch_Checked(object sender, RoutedEventArgs e)
        {
            if (_modelUnit == Unit.Millimeters || _modelUnit == Unit.Meters)
            {
                _modelUnit = Unit.Inches;
            }
 
            _paperUnit = Unit.Inches;
            updateUnits();
        }

        void _rbMm_Checked(object sender, RoutedEventArgs e)
        {
            if (_modelUnit == Unit.Inches || _modelUnit == Unit.Feet)
            {
                _modelUnit = Unit.Millimeters;
            }

            _paperUnit = Unit.Millimeters;
            updateUnits();
        }

        void _rbAppUnit1_Checked(object sender, RoutedEventArgs e)
        {
            _modelUnit = (Unit)_rbAppUnit1.Tag;
            updateUnits();
        }

        void _rbAppUnit2_Checked(object sender, RoutedEventArgs e)
        {
            _modelUnit = (Unit)_rbAppUnit2.Tag;
            updateUnits();
        }

        void _rbAppUnit3_Checked(object sender, RoutedEventArgs e)
        {
            _modelUnit = (Unit)_rbAppUnit3.Tag;
            updateUnits();
        }

        void _rbLandscape_Checked(object sender, RoutedEventArgs e)
        {
            selectDrawingSize(_sizeComboBox.SelectedItem as DrawingSizeItem);
        }

        void _widthBox_OnValueChanged(object o, ValueChangedEventArgs e)
        {
            _width = _widthBox.Value / _paperUnitScale;
        }

        void _heightBox_OnValueChanged(object o, ValueChangedEventArgs e)
        {
            _height = _heightBox.Value / _paperUnitScale;
        }

        void _themeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Cirros.Core.Theme theme = _themeComboBox.SelectedItem as Cirros.Core.Theme;

            foreach (string key in Globals.Themes.Keys)
            {
                if (Globals.Themes[key] == theme)
                {
                    _theme = key;
                    break;
                }
            }
        }

        private void DrawPreview()
        {
            double previewWidth = _previewRectangleGrid.ActualWidth - 10;
            double previewHeight = _previewRectangleGrid.ActualHeight - 10;

            if (previewWidth > 0 && previewHeight > 0)
            {
                double aspect = _width / _height;

                if (aspect > 1)
                {
                    _previewRectangle.Width = previewWidth;
                    _previewRectangle.Height = previewHeight / aspect;
                }
                else
                {
                    _previewRectangle.Width = previewWidth * aspect;
                    _previewRectangle.Height = previewHeight;
                }
                    //_previewRectangle.Width = previewWidth;
                    //_previewRectangle.Height = previewHeight;

                double paperScale = 1;

                if (_paperUnit == Unit.Feet)
                {
                    paperScale *= 12;
                }
                else if (_paperUnit == Unit.Millimeters)
                {
                    paperScale /= 25.4;
                }
                else if (_paperUnit == Unit.Centimeters)
                {
                    paperScale /= 2.54;
                }
                else if (_paperUnit == Unit.Meters)
                {
                    paperScale /= 0.0254;
                }

                _paperSizeText.Text = string.Format("Paper: {0} x {1}",
                        Utilities.FormatDistance(_width / paperScale, 2, _architecturalScale, true, _paperUnit, false),
                        Utilities.FormatDistance(_height / paperScale, 2, _architecturalScale, true, _paperUnit, false)
                        );

                //System.Diagnostics.Debug.WriteLine("scale: {0}  paper: {1}  model: {2}", _scale, _paperUnit, _modelUnit);

                if (_scale > 0)
                {
                    double modelScale = 1.0 / (double)_scale;

                    if (_modelUnit == Unit.Feet)
                    {
                        modelScale *= 12;
                    }
                    else if (_modelUnit == Unit.Millimeters)
                    {
                        modelScale /= 25.4;
                    }
                    else if (_modelUnit == Unit.Centimeters)
                    {
                        modelScale /= 2.54;
                    }
                    else if (_modelUnit == Unit.Meters)
                    {
                        modelScale /= 0.0254;
                    }

                    _modelSizeText.Text = string.Format("Model: {0} x {1}",
                            Utilities.FormatDistance(_width / modelScale, 2, false, true, _modelUnit, false),
                            Utilities.FormatDistance(_height / modelScale, 2, false, true, _modelUnit, false)
                            );
                }
            }
        }
    }

    public class DrawingSizeItem
    {
        public DrawingSizeItem(string name, double width, double height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        public string Name;
        public double Width;
        public double Height;

        public override string ToString()
        {
            return Name;
        }
    }
}
