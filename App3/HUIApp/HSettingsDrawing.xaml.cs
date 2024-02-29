using Cirros;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Utility;
using CirrosUI;
using HUI;
using Microsoft.UI.Xaml.Controls;
using RedDog;
using RedDog.Console;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace RedDog.HUIApp
{
    public sealed partial class HSettingsDrawing : UserControl, HUIIDialog
    {
        public string Id
        {
            get { return RedDogGlobals.GS_SettingsDrawingCommand; }
        }

        public Dictionary<string, object> Options
        {
            get { return null; }
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        private double _newDrawingScale = 1;

        public HSettingsDrawing()
        {
            this.InitializeComponent();

            this.Loaded += HSettingsDrawing_Loaded;
        }

        private void HSettingsDrawing_Loaded(object sender, RoutedEventArgs e)
        {
            Populate();
            Update();

            _colorComboBox.SelectionChanged += _colorComboBox_SelectionChanged;

            _paperWidthBox.OnValueChanged += numberBox_OnValueChanged;
            _paperHeightBox.OnValueChanged += numberBox_OnValueChanged;
            _drawingScaleBox.LostFocus += _drawingScaleBox_LostFocus;
            _drawingScaleBox.KeyDown += _drawingScaleBox_KeyDown;
            _gridIntensitySlider.ValueChanged += _gridIntensitySlider_ValueChanged;
            _paperUnitComboBox.SelectionChanged += _paperUnitComboBox_SelectionChanged;
            _englishModelUnitComboBox.SelectionChanged += _englishModelUnitComboBox_SelectionChanged;
            _metricModelUnitComboBox.SelectionChanged += _metricModelUnitComboBox_SelectionChanged;
            _themeComboBox.SelectionChanged += _themeComboBox_SelectionChanged;
            _gridDivisionsBox.OnValueChanged += numberBox_OnValueChanged;
            _majorGridSpacingBox.OnValueChanged += numberBox_OnValueChanged;

            _showDimUnitCB.Checked += _showDimUnitCB_Checked;
            _showDimUnitCB.Unchecked += _showDimUnitCB_Checked;

            _dimPrecisionAComboBox.SelectionChanged += _dimPrecisionComboBox_SelectionChanged;
            _dimPrecisionEComboBox.SelectionChanged += _dimPrecisionComboBox_SelectionChanged;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        void UpdateColorList()
        {
            if (_colorComboBox != null)
            {
                _colorComboBox.Items.Clear();

                TextBlock tb = new TextBlock();
                tb = new TextBlock();
                tb.Text = "Default experience";
                tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
                tb.Style = (Style)(Application.Current.Resources["HDialogText"]);
                tb.Tag = (uint)ColorCode.SetColor;
                tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                _colorComboBox.Items.Add(tb);

                tb = new TextBlock();
                tb.Text = "Select a new color";
                tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
                tb.Style = (Style)(Application.Current.Resources["HDialogText"]);
                tb.Tag = (uint)ColorCode.SetColor;
                tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                _colorComboBox.Items.Add(tb);

                _colorComboBox.SelectedIndex = 0;

                foreach (uint colorspec in Globals.RecentColors)
                {
                    ColorItemControl cic = new ColorItemControl(colorspec);
                    _colorComboBox.Items.Add(cic);

                    if (cic.ColorValue == Globals.HighlightColor)
                    {
                        _colorComboBox.SelectedItem = cic;
                    }
                }
            }
        }

        void _colorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object o = FocusManager.GetFocusedElement();
            if (o is ComboBoxItem)
            {
                if (_colorComboBox.Items.Count > 2 && _colorComboBox.Items[2] is ColorItemControl)
                {
                    if (_colorComboBox.SelectedIndex == 0)
                    {
                        Globals.HighlightColor = Colors.Transparent;
                    }
                    else if (_colorComboBox.SelectedIndex == 1)
                    {
                        uint colorspec = ((ColorItemControl)_colorComboBox.Items[2]).ColorSpec;
                        ShowColorPicker(colorspec, "color");
                    }
                    else if (_colorComboBox.SelectedItem is ColorItemControl item)
                    {
                        Globals.HighlightColor = item.ColorValue;

                        if (Globals.PushRecentColor(item.ColorSpec))
                        {
                            UpdateColorList();
                        }
                    }

                    //if (_currentLayer != null && UpdateCurrentLayerAttributes())
                    //{
                    //    RefreshLayerList();
                    //    UpdateLineSample();
                    //    Layer.PropagateLayerChanges(_currentLayer.Id);
                    //}
                }
            }
        }

        Popup _popup = null;

        void ShowColorPicker(uint defaultColorSpec, string tag)
        {
            GeneralTransform tf = _colorComboBox.TransformToVisual(null);
            Point t = tf.TransformPoint(new Point());
            if (t.X != 0 && t.Y != 0)
            {
                HColorDialog panel = new HColorDialog();
                panel.ColorSpec = defaultColorSpec;

                _popup = new Popup();
                _popup.Tag = tag;
                _popup.IsLightDismissEnabled = false;
                _popup.Child = panel;
                _popup.HorizontalOffset = t.X;
                _popup.VerticalOffset = t.Y;
                _popup.IsOpen = true;
                _popup.Closed += Popup_Closed;
            }
        }

        private void Popup_Closed(object sender, object e)
        {
            if (sender is Popup popup && popup.Child is HColorDialog colorDialog)
            {
                if (Globals.PushRecentColor(colorDialog.ColorSpec))
                {
                    UpdateColorList();
                }

                Globals.HighlightColor = Utilities.ColorFromColorSpec(colorDialog.ColorSpec);

                //if (_currentLayer != null && UpdateCurrentLayerAttributes())
                //{
                //    RefreshLayerList();
                //    UpdateLineSample();
                //    Layer.PropagateLayerChanges(_currentLayer.Id);
                //}
            }

            _popup = null;
        }

        public void WillClose()
        {
            if (_popup != null)
            {
                _popup.IsOpen = false;
            }

            //if (_currentLayer != null && UpdateCurrentLayerAttributes())
            //{
            //    Layer.PropagateLayerChanges(_currentLayer.Id);
            //    Globals.Events.AttributesListChanged();
            //}
        }

        private void _dimPrecisionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool needRedraw = false;

            object o = Globals.ActiveDrawing.IsArchitecturalScale ? _dimPrecisionAComboBox.SelectedItem : _dimPrecisionEComboBox.SelectedItem;
            if (o is TextBlock)
            {
                try
                {
                    int dimRound = int.Parse((string)((TextBlock)o).Tag);
                    if (dimRound != Globals.DimensionRound)
                    {
                        Globals.DimensionRound = dimRound;

                        if (Globals.ActiveDrawing.PaperUnit == Unit.Millimeters)
                        {
                            Globals.DimensionRoundMetricDefault = dimRound;
                        }
                        else if (Globals.ActiveDrawing.IsArchitecturalScale)
                        {
                            Globals.DimensionRoundArchitectDefault = dimRound;
                        }
                        else
                        {
                            Globals.DimensionRoundEngineerDefault = dimRound;
                        }

                        needRedraw = true;
                    }
                }
                catch
                {
                }
            }

            if (needRedraw)
            {
                Regenerate();
            }
        }

        private void _showDimUnitCB_Checked(object sender, RoutedEventArgs e)
        {
            bool needRedraw = false;

            if (Globals.ShowDimensionUnit != (bool)_showDimUnitCB.IsChecked)
            {
                Globals.ShowDimensionUnit = (bool)_showDimUnitCB.IsChecked;
                needRedraw = true;
            }

            if (needRedraw)
            {
                Regenerate();
            }
        }

        private void _drawingScaleBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                UpdateDrawingScale();
                _drawingScaleBox.Select(_drawingScaleBox.Text.Length, 0);
            }
        }

        private void _drawingScaleBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateDrawingScale();
        }

        void numberBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (sender is NumberBox1 nb)
            {
                if (nb.Tag is string tag)
                {
                    nb.Format();
                    nb.SelectionStart = nb.Text.Length;
                    nb.SelectionLength = 0;

                    if (tag.Contains("grid"))
                    {
                        UpdateGrid();
                    }
                    else if (tag == RedDogGlobals.GS_PaperWidth)
                    {
                        UpdateDrawingSize();
                    }
                    else if (tag == RedDogGlobals.GS_PaperHeight)
                    {
                        UpdateDrawingSize();
                    }
                }
            }
        }

        private void _paperUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_paperUnitComboBox.SelectedIndex == 0)
            {
                if (Globals.ActiveDrawing.PaperUnit != Unit.Inches)
                {
                    Globals.ActiveDrawing.PaperUnit = Unit.Inches;
                    Globals.ActiveDrawing.ModelUnit = Unit.Inches;

                    _englishModelUnitComboBox.SelectedIndex = 0;
                    _englishModelUnitComboBox.Visibility = Visibility.Visible;
                    _metricModelUnitComboBox.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (Globals.ActiveDrawing.PaperUnit != Unit.Millimeters)
                {
                    Globals.ActiveDrawing.PaperUnit = Unit.Millimeters;
                    Globals.ActiveDrawing.ModelUnit = Unit.Millimeters;

                    _metricModelUnitComboBox.SelectedIndex = 0;
                    _englishModelUnitComboBox.Visibility = Visibility.Collapsed;
                    _metricModelUnitComboBox.Visibility = Visibility.Visible;
                }
            }

            if (_newDrawingScale > 0)
            {
                bool isArchitects = ShowScaleText(_newDrawingScale);
                Globals.ActiveDrawing.SetDrawingScaleAndUnits(_newDrawingScale, Globals.ActiveDrawing.PaperUnit, Globals.ActiveDrawing.ModelUnit, isArchitects);

                _majorGridSpacingBox.UnitChanged();

                Update();
            }

            Regenerate();
        }


        private void _metricModelUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (_metricModelUnitComboBox.SelectedIndex)
            {
                case 0:
                default:
                    if (Globals.ActiveDrawing.ModelUnit != Unit.Millimeters)
                    {
                        Globals.ActiveDrawing.ModelUnit = Unit.Millimeters;
                        Regenerate();
                        _majorGridSpacingBox.UnitChanged();
                    }
                    break;

                case 1:
                    if (Globals.ActiveDrawing.ModelUnit != Unit.Centimeters)
                    {
                        Globals.ActiveDrawing.ModelUnit = Unit.Centimeters;
                        Regenerate();
                        _majorGridSpacingBox.UnitChanged();
                    }
                    break;

                case 2:
                    if (Globals.ActiveDrawing.ModelUnit != Unit.Meters)
                    {
                        Globals.ActiveDrawing.ModelUnit = Unit.Meters;
                        Regenerate();
                        _majorGridSpacingBox.UnitChanged();
                    }
                    break;
            }
        }

        void _themeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Globals.ActiveDrawing.Theme = _themeComboBox.SelectedItem as Cirros.Core.Theme;

            Regenerate();
            Globals.Events.ThemeChanged();
        }

        private void Regenerate()
        {
            Globals.View.Regenerate();
            Globals.ActiveDrawing.IsModified = true;
            Globals.ActiveDrawing.ChangeNumber++;
            Globals.Events.ShowRulers(Globals.ShowRulers);
        }

        private void _englishModelUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_englishModelUnitComboBox.SelectedIndex == 0)
            {
                if (Globals.ActiveDrawing.PaperUnit != Unit.Inches)
                {
                    Globals.ActiveDrawing.ModelUnit = Unit.Inches;
                    Regenerate();
                    _majorGridSpacingBox.UnitChanged();
                }
            }
            else if (Globals.ActiveDrawing.PaperUnit != Unit.Feet)
            {
                Globals.ActiveDrawing.ModelUnit = Unit.Feet;
                Regenerate();
                _majorGridSpacingBox.UnitChanged();
            }
        }

        private void _gridIntensitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Globals.GridIntensity = _gridIntensitySlider.Value / 100;
            Globals.View.VectorListControl.Redraw();
        }

        public void Populate()
        {
            if (Globals.ActiveDrawing.IsArchitecturalScale)
            {
                _dimPrecisionAComboBox.Visibility = Visibility.Visible;
                _dimPrecisionEComboBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                _dimPrecisionAComboBox.Visibility = Visibility.Collapsed;
                _dimPrecisionEComboBox.Visibility = Visibility.Visible;
            }

            _paperWidthBox.Value = Globals.ActiveDrawing.PaperSize.Width;
            _paperHeightBox.Value = Globals.ActiveDrawing.PaperSize.Height;

            _themeComboBox.ItemsSource = Globals.Themes.Values;

            UpdateColorList();
        }

        void Update()
        {
            if (Globals.HighlightColor == Colors.Transparent)
            {
                _colorComboBox.SelectedIndex = 0;
            }
            else
            {
                foreach (object o in _colorComboBox.Items)
                {
                    if (o is ColorItemControl cic && cic.ColorValue == Globals.HighlightColor)
                    {
                        _colorComboBox.SelectedItem = cic;
                        break;
                    }
                }
            }

            _majorGridSpacingBox.Unit = Globals.ActiveDrawing.ModelUnit;
            _majorGridSpacingBox.Value = Globals.GridSpacing;
            _gridDivisionsBox.Value = Globals.GridDivisions;

            if (Globals.ActiveDrawing.PaperUnit == Unit.Inches)
            {
                _paperUnitComboBox.SelectedIndex = 0;

                _englishModelUnitComboBox.Visibility = Visibility.Visible;
                _metricModelUnitComboBox.Visibility = Visibility.Collapsed;

                if (Globals.ActiveDrawing.ModelUnit == Unit.Inches)
                {
                    _englishModelUnitComboBox.SelectedIndex = 0;
                }
                else
                {
                    _englishModelUnitComboBox.SelectedIndex = 1;
                }
            }
            else
            {
                _paperUnitComboBox.SelectedIndex = 1;

                _englishModelUnitComboBox.Visibility = Visibility.Collapsed;
                _metricModelUnitComboBox.Visibility = Visibility.Visible;

                if (Globals.ActiveDrawing.ModelUnit == Unit.Millimeters)
                {
                    _metricModelUnitComboBox.SelectedIndex = 0;
                }
                else if (Globals.ActiveDrawing.ModelUnit == Unit.Centimeters)
                {
                    _metricModelUnitComboBox.SelectedIndex = 1;
                }
                else
                {
                    _metricModelUnitComboBox.SelectedIndex = 2;
                }
            }

            if (Globals.ActiveDrawing.PaperUnit == Unit.Millimeters)
            {
                _paperWidthBox.MinValue = 25;
                _paperHeightBox.MinValue = 25;
                _paperWidthBox.MaxValue = 1200;
                _paperHeightBox.MaxValue = 1200;
            }
            else
            {
                _paperWidthBox.MinValue = 1;
                _paperHeightBox.MinValue = 1;
                _paperWidthBox.MaxValue = 44;
                _paperHeightBox.MaxValue = 44;
            }

            _showDimUnitCB.IsEnabled = Globals.ActiveDrawing.IsArchitecturalScale == false;

            ComboBox cb = Globals.ActiveDrawing.IsArchitecturalScale ? _dimPrecisionAComboBox : _dimPrecisionEComboBox;
            if (cb != null)
            {
                string s = Globals.DimensionRound.ToString();

                foreach (TextBlock tb in cb.Items)
                {
                    if ((string)tb.Tag == s)
                    {
                        cb.SelectedItem = tb;
                        break;
                    }
                }
            }

            //string fs = Globals.UIDataContext.Size.ToString();

            //foreach (TextBlock tb in _fontSizeBox.Items)
            //{
            //    if ((string)tb.Tag == fs)
            //    {
            //        _fontSizeBox.SelectedItem = tb;
            //        break;
            //    }
            //}

            _gridIntensitySlider.Value = Globals.GridIntensity * 100;

            //switch (Globals.ActiveDrawing.LineEndCap)
            //{
            //    case PenLineCap.Flat:
            //        _eolShapeComboBox.SelectedIndex = 0;
            //        break;
            //    case PenLineCap.Round:
            //        _eolShapeComboBox.SelectedIndex = 1;
            //        break;
            //    case PenLineCap.Square:
            //        _eolShapeComboBox.SelectedIndex = 2;
            //        break;
            //    case PenLineCap.Triangle:
            //        _eolShapeComboBox.SelectedIndex = 3;
            //        break;
            //}

            //switch (Globals.MousePanButton)
            //{
            //    case Globals.MouseButtonType.Middle:
            //        _mousePanButtonComboBox.SelectedIndex = 0;
            //        break;
            //    case Globals.MouseButtonType.Right:
            //        _mousePanButtonComboBox.SelectedIndex = 1;
            //        break;
            //    case Globals.MouseButtonType.Button1:
            //        _mousePanButtonComboBox.SelectedIndex = 2;
            //        break;
            //    case Globals.MouseButtonType.Button2:
            //        _mousePanButtonComboBox.SelectedIndex = 3;
            //        break;
            //}

            _showDimUnitCB.IsChecked = Globals.ShowDimensionUnit;
            //_cursorTypeComboBox.SelectedIndex = Globals.Input.CursorSize == 0 ? 0 : 1;
            //_betaComboBox.SelectedIndex = Globals.EnableBetaFeatures ? 1 : 0;

            //_pinchZoomCB.IsChecked = Globals.EnablePinchZoom;
            //_touchMagnifierCB.IsChecked = Globals.EnableTouchMagnifer;
            //_stylusMagnifierCB.IsChecked = Globals.EnableStylusMagnifer;

            _themeComboBox.SelectedItem = Globals.ActiveDrawing.Theme;

            _newDrawingScale = Globals.ActiveDrawing.Scale;
            //_drawingScale.Value = Globals.ActiveDrawing.Scale;
            _drawingScaleBox.Text = Utilities.DoubleAsRatio(1 / Globals.ActiveDrawing.Scale);

            _paperWidthBox.Unit = Globals.ActiveDrawing.PaperUnit;
            _paperHeightBox.Unit = Globals.ActiveDrawing.PaperUnit;
            _paperWidthBox.Value = Globals.ActiveDrawing.PaperToUser(Globals.ActiveDrawing.PaperSize.Width);
            _paperHeightBox.Value = Globals.ActiveDrawing.PaperToUser(Globals.ActiveDrawing.PaperSize.Height);

            Globals.ActiveDrawing.IsArchitecturalScale = ShowScaleText(Globals.ActiveDrawing.Scale);

            if (_drawingScaleBox.FocusState == FocusState.Keyboard)
            {
                _drawingScaleBox.SelectAll();
            }

            _activeTimeBox.Text = Globals.ActiveDrawing.ActiveTime.ToString(@"hh\:mm\:ss");
        }

        private void UpdateDrawingSize()
        {
            Globals.ActiveDrawing.PaperSize = new Size(Globals.ActiveDrawing.UserToPaper(_paperWidthBox.Value), Globals.ActiveDrawing.UserToPaper(_paperHeightBox.Value));
            Globals.View.DisplayAll();
            Globals.ActiveDrawing.IsModified = true;
            Globals.ActiveDrawing.ChangeNumber++;
        }

        private void UpdateGrid()
        {
            if (Globals.GridSpacing != _majorGridSpacingBox.Value || Globals.GridDivisions != (int)_gridDivisionsBox.Value)
            {
                Globals.GridSpacing = _majorGridSpacingBox.Value;
                Globals.GridDivisions = (int)_gridDivisionsBox.Value;
                Globals.Events.GridChanged();
                Globals.Events.ShowRulers(Globals.ShowRulers);
            }
        }

        private void UpdateDrawingScale()
        {
            long numerator = 1;
            long denominator = 1;

            bool valid = false;

            string[] sa = _drawingScaleBox.Text.Split(new char[] { ':', ' ' });

            if (sa.Length >= 2)
            {
                if (long.TryParse(sa[0], out numerator))
                {
                    if (long.TryParse(sa[1], out denominator))
                    {
                        valid = numerator > 0 && denominator > 0;
                    }
                }
            }
            else
            {
                try
                {
                    double r;

                    if (double.TryParse(_drawingScaleBox.Text, out r))
                    {
                        valid = Utilities.DoubleAsFraction(r, out numerator, out denominator);
                    }
                }
                catch
                {
                    // not a fraction
                }
            }

            if (valid)
            {
                double inverse = (double)numerator / (double)denominator;
                if (inverse > 0)
                {
                    _newDrawingScale = 1 / inverse;
                    _drawingScaleBox.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
            else
            {
                _drawingScaleBox.Foreground = new SolidColorBrush(Colors.Red);
            }

            if (Globals.ActiveDrawing.Scale != _newDrawingScale)
            {
                bool isArchitects = ShowScaleText(_newDrawingScale);

                Globals.ActiveDrawing.SetDrawingScaleAndUnits(_newDrawingScale,
                    Globals.ActiveDrawing.PaperUnit, Globals.ActiveDrawing.ModelUnit,
                    isArchitects);

                //Globals.View.DisplayAll();

                Update();

                Globals.ActiveDrawing.IsModified = true;
                Globals.ActiveDrawing.ChangeNumber++;

                _newDrawingScale = Globals.ActiveDrawing.Scale;
            }
        }

        private bool ShowScaleText(double scale)
        {
            //bool isArchitects = Globals.ActiveDrawing.IsArchitecturalScale;
            bool isArchitects = false;
            string ratio = Utilities.DoubleAsRatio(1 / scale);

            if (Globals.ActiveDrawing.PaperUnit == Unit.Inches && Globals.ActiveDrawing.ModelUnit == Unit.Feet)
            {
                isArchitects = true;

                switch (ratio)
                {
                    case "1:1":
                        _drawingScaleBox.Text = "1:1 (12\"=1'0\")";
                        break;
                    case "1:2":
                        _drawingScaleBox.Text = "1:2 (6\"=1'0\")";
                        break;
                    case "1:4":
                        _drawingScaleBox.Text = "1:4 (3\"=1'0\")";
                        break;
                    case "1:8":
                        _drawingScaleBox.Text = "1:8 (1 1/2\"=1'0\")";
                        break;
                    case "1:12":
                        _drawingScaleBox.Text = "1:12 (1\"=1'0\")";
                        break;
                    case "1:16":
                        _drawingScaleBox.Text = "1:16 (3/4\"=1'0\")";
                        break;
                    case "1:24":
                        _drawingScaleBox.Text = "1:24 (1/2\"=1'0\")";
                        break;
                    case "1:32":
                        _drawingScaleBox.Text = "1:32 (3/8\"=1'0\")";
                        break;
                    case "1:48":
                        _drawingScaleBox.Text = "1:48 (1/4\"=1'0\")";
                        break;
                    case "1:64":
                        _drawingScaleBox.Text = "1:64 (3/16\"=1'0\")";
                        break;
                    case "1:96":
                        _drawingScaleBox.Text = "1:96 (1/8\"=1'0\")";
                        break;
                    case "1:128":
                        _drawingScaleBox.Text = "1:128 (3/32\"=1'0\")";
                        break;
                    default:
                        _drawingScaleBox.Text = ratio;
                        isArchitects = false;
                        break;
                }
            }
            else if (_newDrawingScale == 1)
            {
                _drawingScaleBox.Text = "1:1 (Full)";
            }
            else
            {
                _drawingScaleBox.Text = ratio;
            }

            Regenerate();

            return isArchitects;
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-drawing" }, { "source", "help" } });

            _ttSettingsDrawingIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-drawing" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttSettingsDrawingSize.IsOpen = true;
                        break;

                    case "size":
                        _ttSettingsDrawingPaperUnit.IsOpen = true;
                        break;

                    case "paper":
                        _ttSettingsDrawingModelUnit.IsOpen = true;
                        break;

                    case "model":
                        _ttSettingsDrawingScale.IsOpen = true;
                        break;

                    case "scale":
                        _ttSettingsDrawingTheme.IsOpen = true;
                        break;

                    case "theme":
                        _ttSettingsDrawingHighlight.IsOpen = true;
                        break;

                    case "highlight":
                        _ttSettingsDrawingTime.IsOpen = true;
                        break;

                    case "time":
                        _ttSettingsDrawingGridSpacing.IsOpen = true;
                        break;

                    case "grid-spacing":
                        _ttSettingsDrawingGridDivisions.IsOpen = true;
                        break;

                    case "grid-divisions":
                        _ttSettingsDrawingGridIntensity.IsOpen = true;
                        break;

                    case "grid-intensity":
                        _ttSettingsDrawingDimensionPrecision.IsOpen = true;
                        break;

                    case "dim-precision":
                        _ttSettingsDrawingDimensionShowUnits.IsOpen = true;
                        break;
                }
            }
        }
    }
}
