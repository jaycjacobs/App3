using Cirros;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Utility;
using CirrosCore;
using CirrosUI;
using RedDog.HUIApp;
using Microsoft.UI.Xaml.Controls;
using RedDog;
using RedDog.Console;
using System;
using System.Collections.Generic;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI;

namespace RedDog.HUIApp
{
    public sealed partial class HDrawAttributesControl : UserControl
    {
        Layer _layer = null;
        private string _lineType;
        private string _fill;
        private double _thickness;
        private Dictionary<string, object> _options = new Dictionary<string, object>();
        private string _fillKey;
        private string _layerKey = RedDogGlobals.GS_Layer;

        public HDrawAttributesControl()
        {
            this.InitializeComponent();

            this.Loaded += HDrawAttributesControl_Loaded;
        }

        void HDrawAttributesControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Globals.LayerTable.ContainsKey(Globals.LayerId))
            {
                _layer = Globals.LayerTable[Globals.LayerId];
            }

            Populate();

            _layerComboBox.SelectionChanged += _layerComboBox_SelectionChanged;
            _colorComboBox.SelectionChanged += _colorComboBox_SelectionChanged;
            _colorComboBox.DropDownClosed += _colorComboBox_DropDownClosed;
            _linetypeComboBox.SelectionChanged += _linetypeComboBox_SelectionChanged;
            _fillComboBox.SelectionChanged += _fillComboBox_SelectionChanged;
            _fillComboBox.DropDownClosed += _fillComboBox_DropDownClosed;
            _thicknessComboBox.SelectionChanged += _thicknessComboBox_SelectionChanged;
            _patternComboBox.SelectionChanged += _patternComboBox_SelectionChanged;
            _patternScaleBox.OnValueChanged += numberBox_OnValueChanged;
            _patternAngleBox.OnValueChanged += numberBox_OnValueChanged;

            _swatch.BorderColor = Utilities.ColorFromColorSpec(0xff484848);

            _layerComboBox.SelectedIndex = 0;
            _colorComboBox.SelectedIndex = 0;
            _linetypeComboBox.SelectedIndex = 0;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        public double TitleColumnMinWidth
        {
            get
            {
                return _titleColumn.Width.Value;
            }
            set
            {
                if (value > _titleColumn.Width.Value)
                {
                    _titleColumn.Width = _fillTitleColumn.Width = new GridLength(value);
                }
            }
        }

        public void ShouldClose()
        {
            if (_popup is Popup && _popup.IsOpen)
            {
                _popup.IsOpen = false;
            }
        }

        public TeachingTip AttributeTeachingTip
        {
            get { return _ttDrawAttributesLine; }
        }

        public TeachingTip FillTeachingTip
        {
            get { return _ttDrawAttributesFill; }
        }

        private void _thicknessComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_thicknessComboBox.SelectedItem is ComboBoxItem item && item.Tag is string lws)
            {
                if (int.TryParse(lws, out int lwid))
                {
                    double thickness = lwid * .001;
                    SetOption(RedDogGlobals.GS_Thickness, thickness);
                }
            }
            else
            {
                SetOption(RedDogGlobals.GS_Thickness, 0.0);
            }
        }

        private void _layerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int oldLayerId = 0;
            if (_layer != null)
            {
                oldLayerId = _layer.Id;
            }

            if (_layerComboBox.SelectedItem is HLayerTile tile)
            {
                _layer = tile.Layer;
            }
            else if (_layerComboBox.SelectedItem is TextBlock tb && tb.Tag is string tag)
            {
                if (tag == "active_layer")
                {
                    if (Globals.LayerTable.ContainsKey(Globals.LayerId))
                    {
                        _layer = Globals.LayerTable[Globals.LayerId];
                    }
                }
            }

            if (_layer != null && _layer.Id != oldLayerId)
            {
                _colorComboBox.SelectedIndex = 0;
                _linetypeComboBox.SelectedIndex = 0;
                _thicknessComboBox.SelectedIndex = 0;
            }

            UpdateSwatch();
        }

        void numberBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (sender is NumberBox1 nb)
            {
                if (nb.Tag is string)
                {
                    SetOption(nb.Tag as string, nb.Value);

                    nb.Format();
                    nb.SelectionStart = nb.Text.Length;
                    nb.SelectionLength = 0;

                    if (nb == _patternScaleBox)
                    {
                        _patternScale = _patternScaleBox.Value;
                        UpdateSwatch();
                    }
                    else if (nb == _patternAngleBox)
                    {
                        _patternAngle = _patternAngleBox.Value;
                        UpdateSwatch();
                    }
                }
            }
        }

        void SetOption(string key, object value)
        {
            if (_options.ContainsKey(key))
            {
                if (value == null)
                {
                    _options.Remove(key);
                }
                else
                {
                    _options[key] = value;
                }
            }
            else
            {
                _options.Add(key, value);
            }
        }

        private void _patternComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FillOptionsChanged();

            if (_patternComboBox.SelectedValue is string patternName)
            {
                _patternName = patternName;

                SetOption(RedDogGlobals.GS_Pattern, _patternComboBox.SelectedValue);

                UpdateSwatch();
            }
        }

        private void _fillComboBox_DropDownClosed(object sender, object e)
        {
            UpdateFillList();
        }

        private void _colorComboBox_DropDownClosed(object sender, object e)
        {
            UpdateColorList();
            UpdateSwatch();
        }

        public void SetLayer(int layerId)
        {
            if (layerId < 0)
            {
                layerId = Globals.ActiveLayerId;
                _layerComboBox.SelectedIndex = 0;
            }
            else
            {
                if (Globals.LayerTable.ContainsKey(layerId))
                {
                    _layer = Globals.LayerTable[layerId];

                    for (int i = 0; i < _layerComboBox.Items.Count; i++)
                    {
                        if (_layerComboBox.Items[i] is HLayerTile tile && tile.Tag is string tilelayer)
                        {
                            if (tilelayer == _layer.Name)
                            {
                                _layerComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    _layerComboBox.SelectedIndex = 0;
                }
            }
        }

        void _fillComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object o = FocusManager.GetFocusedElement();
            if (o is ComboBoxItem)
            {
                ColorItemControl cic = _fillComboBox.SelectedItem as ColorItemControl;
                TextBlock tb = _fillComboBox.SelectedItem as TextBlock;
                uint colorspec = 0;

                if (tb != null && tb.Tag is uint)
                {
                    colorspec = (uint)tb.Tag;
                }
                else if (cic != null)
                {
                    colorspec = cic.ColorSpec;
                    Globals.PushRecentColor(colorspec);
                }

                if (colorspec == (uint)ColorCode.SetColor)
                {
                    colorspec = ((ColorItemControl)_fillComboBox.Items[4]).ColorSpec;
                    ShowColorPicker(colorspec, "fill");
                }
                else
                {
                    string colorname = Utilities.ColorNameFromColorSpec(colorspec);

                    SetOption(RedDogGlobals.GS_Fill, colorname);
                }

                FillOptionsChanged();
                UpdateSwatch();
            }
        }

        void FillOptionsChanged()
        {
            if (_fillComboBox.SelectedIndex == 0)
            {
                _patternComboBox.IsEnabled = false;
                _patternScaleBox.IsEnabled = false;
                _patternAngleBox.IsEnabled = false;
                _swatch.Visibility = _swatchTitle.Visibility = Visibility.Collapsed;
            }
            else
            {
                _patternComboBox.IsEnabled = true;
                if (_patternComboBox.SelectedIndex != 0)
                {
                    _patternScaleBox.IsEnabled = _patternAngleBox.IsEnabled = true;
                    _swatch.Visibility = _swatchTitle.Visibility = Visibility.Visible;
                }
                else
                {
                    _patternScaleBox.IsEnabled = _patternAngleBox.IsEnabled = false;
                    _swatch.Visibility = _swatchTitle.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void UpdateSwatch()
        {
            if (_swatch.Visibility == Visibility.Visible)
            {
                ColorItemControl cic = _fillComboBox.SelectedItem as ColorItemControl;
                TextBlock tb = _fillComboBox.SelectedItem as TextBlock;
                uint colorspec = 0;
                Color _fillColor = Colors.Black;

                if (tb != null && tb.Tag is uint)
                {
                    colorspec = (uint)tb.Tag;
                }
                else if (cic != null)
                {
                    colorspec = cic.ColorSpec;
                    Globals.PushRecentColor(colorspec);
                }

                if (colorspec == (uint)ColorCode.SetColor)
                {
                    // this shouldn't happen
                }
                else if (colorspec == (uint)ColorCode.ByLayer)
                {
                    _fillColor = Utilities.ColorFromColorSpec(_layer.ColorSpec);
                }
                else if (colorspec == (uint)ColorCode.SameAsOutline)
                {
                    if (_colorComboBox.SelectedIndex == 0)
                    {
                        _fillColor = Utilities.ColorFromColorSpec(_layer.ColorSpec);
                    }
                    else if (_colorComboBox.SelectedItem is ColorItemControl item)
                    {
                        _fillColor = Utilities.ColorFromColorSpec(item.ColorSpec);
                    }
                }
                else
                {
                    _fillColor = Utilities.ColorFromColorSpec(colorspec);
                }

                _swatch.SetPattern(_patternName, _fillColor, _patternScale, _patternAngle);
            }
        }

        void _linetypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_linetypeComboBox.SelectedIndex == 0)
            {
                SetOption(RedDogGlobals.GS_LineType, "by_layer");
            }
            else
            {
                if (_linetypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
                {
                    SetOption(RedDogGlobals.GS_LineType, tag);
                }
            }
        }

        void _colorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_colorComboBox.SelectedIndex == 0)
            {
                SetOption(RedDogGlobals.GS_Color, "by_layer");
            }
            else if (_colorComboBox.Items.Count > 2 && _colorComboBox.Items[2] is ColorItemControl)
            {
                uint colorspec = ((ColorItemControl)_colorComboBox.Items[2]).ColorSpec;

                if (_colorComboBox.SelectedIndex == 1)
                {
                    ShowColorPicker(colorspec, "color");
                }
                else if (_colorComboBox.SelectedItem is ColorItemControl)
                {
                    ColorItemControl item = (ColorItemControl)_colorComboBox.SelectedItem;
                    colorspec = item.ColorSpec;
                    string colorname = Utilities.ColorNameFromColorSpec(colorspec);

                    SetOption(RedDogGlobals.GS_Color, colorname);

                    Globals.PushRecentColor(colorspec);
                }
            }
        }

        private double _patternAngle = 0;
        private string _patternName = "";
        private double _patternScale = 1;

        public void SetFill(uint fill)
        {
            switch (fill)
            {
                case (uint)ColorCode.NoFill:
                    _fillComboBox.SelectedIndex = 0;
                    break;

                case (uint)ColorCode.ByLayer:
                //case (uint)ColorCode.ThemeForeground:
                    _fillComboBox.SelectedIndex = 1;
                    break;

                case (uint)ColorCode.SameAsOutline:
                    _fillComboBox.SelectedIndex = 2;
                    break;

                default:
                    //for (int i = 0; i < _fillComboBox.Items.Count; i++)
                    //{
                    //    if (_fillComboBox.Items[i] is ColorItemControl)
                    //    {
                    //        if (((ColorItemControl)_fillComboBox.Items[i]).ColorSpec == fill)
                    //        {
                    //            _fillComboBox.SelectedIndex = i;
                    //            break;
                    //        }
                    //    }
                    //}
                    if (Globals.PushRecentColor(fill))
                    {
                        UpdateFillList();
                        SelectFillColor(fill);
                    }
                    if (_fillComboBox.Items.Count > 4)
                    {
                        _fillComboBox.SelectedIndex = 4;
                    }
                    break;
            }
            FillOptionsChanged();
        }

        public void SetColorSpec(uint cspec)
        {
            if (cspec == (uint)ColorCode.ByLayer)
            {
                _colorComboBox.SelectedIndex = 0;
            }
            else
            {
                //for (int i = 0; i < _colorComboBox.Items.Count; i++)
                //{
                //    if (_colorComboBox.Items[i] is ColorItemControl cic)
                //    {
                //        if (cic.ColorSpec == cspec)
                //        {
                //            _colorComboBox.SelectedIndex = i;
                //            break;
                //        }
                //    }
                //}
                if (Globals.PushRecentColor(cspec))
                {
                    UpdateColorList();
                    SelectColor(cspec);
                }
                if (_colorComboBox.Items.Count > 2)
                {
                    _colorComboBox.SelectedIndex = 2;
                }
            }
        }

        public void SetLineTypeId(int lineTypeId)
        {
            if (lineTypeId < 0)
            {
                _linetypeComboBox.SelectedIndex = 0;
            }
            else if (Globals.LineTypeTable.ContainsKey(lineTypeId))
            {
                string ltname = Globals.LineTypeTable[lineTypeId].Name;

                foreach (object o in _linetypeComboBox.Items)
                {
                    if (o is ComboBoxItem item && (string)item.Tag == ltname)
                    {
                        _linetypeComboBox.SelectedItem = o;
                        break;
                    }
                }
            }
        }

        public void SetLineWeightId(int lineWeightId)
        {
            if (lineWeightId <= 0)
            {
                _thicknessComboBox.SelectedIndex = 0;
            }
            else
            {
                foreach (object o in _thicknessComboBox.Items)
                {
                    if (o is ComboBoxItem item && item.Tag is string ts)
                    {
                        if (int.TryParse(ts, out int t))
                        {
                            if (t == lineWeightId)
                            {
                                _thicknessComboBox.SelectedItem = o;
                            }
                        }
                    }
                }
            }
        }

        public void SetPattern(string patternName, double patternScale, double patternAngle)
        {
            _patternComboBox.SelectedValue = _patternName = patternName;
            _patternScaleBox.Value = _patternScale = patternScale;
            _patternAngleBox.Value = _patternAngle = patternAngle;

            UpdateSwatch();
        }

        public string LayerKey
        {
            get
            {
                return _layerKey;
            }
            set
            {
                _layerKey = value;
            }
        }

        public string FillKey
        {
            get
            {
                return _fillKey;
            }
            set
            {
                _fillComboBox.Visibility = _fillOptions.Visibility = value.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                _fillKey = value;
            }
        }

        public Dictionary<string, object> Options
        {
            get
            {
                if (_layerComboBox.SelectedItem is HLayerTile tile && tile.Tag is string tilelayer)
                {
                    SetOption(RedDogGlobals.GS_Layer, tilelayer);
                }
                else if (_layerComboBox.SelectedItem is TextBlock tb && tb.Tag is string tag)
                {
                    if (tag == "active_layer")
                    {
                        SetOption(RedDogGlobals.GS_Layer, "Active_Layer");
                    }
                }
                return _options;
            }
        }

        private List<string> _patternNames = null;

        public void Populate()
        {
            _layerComboBox.Items.Clear();

            TextBlock activelayer = new TextBlock();
            activelayer.Text = "Use active layer";
            activelayer.Style = (Style)(Application.Current.Resources["HDialogComboBoxContentItalic"]);
            activelayer.Tag = "active_layer";
            _layerComboBox.Items.Add(activelayer);

            foreach (Cirros.Drawing.Layer layer in Globals.LayerTable.Values)
            {
                HLayerTile item = new HLayerTile(layer);
                item.Tag = layer.Name;
                _layerComboBox.Items.Add(item);
            }

            _linetypeComboBox.Items.Clear();

            TextBlock bylayer = new TextBlock();
            bylayer.Text = "Use layer line type";
            bylayer.Style = (Style)(Application.Current.Resources["HDialogComboBoxContentItalic"]);
            bylayer.Tag = "by_layer";
            _linetypeComboBox.Items.Add(bylayer);

            foreach (Cirros.Drawing.LineType linetype in Globals.LineTypeTable.Values)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = linetype.Name;
                item.Tag = linetype.Name;
                item.FontStyle = Windows.UI.Text.FontStyle.Normal;
                _linetypeComboBox.Items.Add(item);
            }

            UpdateColorList();

            if (_fillOptions.Visibility == Visibility.Visible)
            {
                UpdateFillList();

                if (_patternComboBox != null)
                {
                    if (_patternNames == null || _patternNames.Count != Patterns.PatternDictionary.Count)
                    {
                        _patternNames = new List<string>();
                        foreach (string key in Patterns.PatternDictionary.Keys)
                        {
                            CrosshatchPattern p = Patterns.PatternDictionary[key];
                            _patternNames.Add(p.Name);
                        }

                        _patternNames.Insert(0, "Solid");
                    }

                    _patternComboBox.ItemsSource = _patternNames;
                }
            }

            FillOptionsChanged();
        }

        public string LineType
        {
            get
            {
                return _lineType;
            }
            set
            {
                _lineType = value;
            }
        }

        public string Fill
        {
            get
            {
                return _fill;
            }
            set
            {
                _fill = value;
            }
        }

        public double Thickness
        {
            get
            {
                return _thickness;
            }
            set
            {
                _thickness = value;
            }
        }

        void UpdateColorList()
        {
            if (_colorComboBox != null)
            {
                int selectedIndex = _colorComboBox.SelectedIndex;

                _colorComboBox.Items.Clear();

                TextBlock tb = new TextBlock();
                tb.Text = "Use layer color";
                tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
                tb.Style = (Style)(Application.Current.Resources["HDialogText"]);
                _colorComboBox.Items.Add(tb);

                tb = new TextBlock();
                tb.Text = "Select a new color";
                tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
                tb.Style = (Style)(Application.Current.Resources["HDialogText"]);
                tb.Tag = (uint)ColorCode.SetColor;
                _colorComboBox.Items.Add(tb);

                foreach (uint colorspec in Globals.RecentColors)
                {
                    _colorComboBox.Items.Add(new ColorItemControl(colorspec));
                }

                if (selectedIndex == 0)
                {
                    _colorComboBox.SelectedIndex = 0;
                }
                else
                {
                    _colorComboBox.SelectedIndex = 2;
                }
            }
        }

        void UpdateFillList()
        {
            if (_fillComboBox != null)
            {
                int selectedIndex = _fillComboBox.SelectedIndex;

                _fillComboBox.Items.Clear();

                TextBlock tb = new TextBlock();
                tb.Text = "No fill";
                tb.Style = (Style)(Application.Current.Resources["HDialogComboBoxContentItalic"]);
                tb.Tag = (uint)ColorCode.NoFill;
                _fillComboBox.Items.Add(tb);

                tb = new TextBlock();
                tb.Text = "Use layer color";
                tb.Style = (Style)(Application.Current.Resources["HDialogComboBoxContentItalic"]);
                tb.Tag = (uint)ColorCode.ByLayer;
                _fillComboBox.Items.Add(tb);

                tb = new TextBlock();
                tb.Text = "Use outline color";
                tb.Style = (Style)(Application.Current.Resources["HDialogComboBoxContentItalic"]);
                tb.Tag = (uint)ColorCode.SameAsOutline;
                _fillComboBox.Items.Add(tb);

                tb = new TextBlock();
                tb.Text = "Select a new color";
                tb.Style = (Style)(Application.Current.Resources["HDialogComboBoxContentItalic"]);
                tb.Tag = (uint)ColorCode.SetColor;
                _fillComboBox.Items.Add(tb);

                foreach (uint colorspec in Globals.RecentColors)
                {
                    _fillComboBox.Items.Add(new ColorItemControl(colorspec));
                }

                if (selectedIndex < 0 || selectedIndex >= 3)
                {
                    _fillComboBox.SelectedIndex = 4;
                }
                else
                {
                    _fillComboBox.SelectedIndex = selectedIndex;
                }
            }
        }

        Popup _popup = null;

        void ShowColorPicker(uint defaultColorSpec, string tag)
        {
            GeneralTransform tf = _layerComboBox.TransformToVisual(null);
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
                    if (popup.Tag is string tag)
                    {
                        if (tag == "fill")
                        {
                            UpdateFillList();
                            SelectFillColor(colorDialog.ColorSpec);
                        }
                        else
                        {
                            UpdateColorList();
                            SelectColor(colorDialog.ColorSpec);
                        }
                    }
                }

                _popup = null;
            }
        }

        private void SelectColor(uint colorspec)
        {
            string colorname = Utilities.ColorNameFromColorSpec(colorspec);
            SetOption(RedDogGlobals.GS_Color, colorname);

            UpdateColorList();
            UpdateSwatch();
        }

        private void SelectFillColor(uint colorspec)
        {
            string colorname = Utilities.ColorNameFromColorSpec(colorspec);
            SetOption(RedDogGlobals.GS_Fill, colorname);

            UpdateFillList();
        }
    }
}
