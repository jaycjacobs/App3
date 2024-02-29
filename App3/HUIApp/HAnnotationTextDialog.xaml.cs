using Cirros;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using CirrosUI;
using RedDog.HUIApp;
using HUI;
using Microsoft.UI.Xaml.Controls;
using RedDog;
using RedDog.Console;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using TextAlignment = Microsoft.UI.Xaml.TextAlignment;

namespace RedDog.HUIApp
{
    public sealed partial class HAnnotationTextDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_TextCommand } };
        HXAMLControl _selectedIcon = null;

        public HAnnotationTextDialog()
        {
            this.InitializeComponent();

            this.Loaded += HAnnotationTextDialog_Loaded;
        }

        public void WillClose()
        {
            if (_popup != null)
            {
                _popup.IsOpen = false;
            }
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        void HAnnotationTextDialog_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (FrameworkElement fe in _iconRow1.Children)
            {
                if (fe is HXAMLControl)
                {
                    HXAMLControl hxamlControl = fe as HXAMLControl;
                    hxamlControl.OnHXAMLControlClick += hxamlControl_OnHXAMLControlClick;
                    hxamlControl.IsSelected = false;
                }
            }

            Populate();

            _heightBox.OnValueChanged += numberBox_OnValueChanged;
            _spacingBox.OnValueChanged += numberBox_OnValueChanged;
            _lineSpacingBox.OnValueChanged += numberBox_OnValueChanged;
            _angleBox.OnValueChanged += numberBox_OnValueChanged;

            _styleComboBox.SelectionChanged += _styleComboBox_SelectionChanged;
            _alignmentComboBox.SelectionChanged += _alignmentComboBox_SelectionChanged;
            _positionComboBox.SelectionChanged += _positionComboBox_SelectionChanged;
            _fontComboBox.SelectionChanged += _fontComboBox_SelectionChanged;
            _layerComboBox.SelectionChanged += _layerComboBox_SelectionChanged;
            _colorComboBox.SelectionChanged += _colorComboBox_SelectionChanged;
            _colorComboBox.DropDownClosed += _colorComboBox_DropDownClosed;

            List<string> fontNames = new List<string>();
            try
            {
                FontCollection fc = Cirros.Core.Dx.SystemFontCollection;
                for (int i = 0; i < fc.FontFamilyCount; i++)
                {
                    SharpDX.DirectWrite.FontFamily writeFontFamily = fc.GetFontFamily(i);

                    if (writeFontFamily.FamilyNames.FindLocaleName("en-us", out int index))
                    {
                        fontNames.Add(writeFontFamily.FamilyNames.GetString(index));
                    }
                }
            }
            catch
            {

            }

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        private void _fontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_fontComboBox.SelectedIndex == 0)
            {
                if (_options.ContainsKey(RedDogGlobals.GS_Font))
                {
                    _options.Remove(RedDogGlobals.GS_Font);
                }
            }
            else if (_fontComboBox.SelectedItem is ComboBoxItem tb && tb.Tag is string fontName)
            {
                if (_options.ContainsKey(RedDogGlobals.GS_Font))
                {
                    _options[RedDogGlobals.GS_Font] = fontName;
                }
                else
                {
                    _options.Add(RedDogGlobals.GS_Font, fontName);
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

                    if (_options.ContainsKey(RedDogGlobals.GS_Color))
                    {
                        _options[RedDogGlobals.GS_Color] = colorname;
                    }
                    else
                    {
                        _options.Add(RedDogGlobals.GS_Color, colorname);
                    }

                    Globals.PushRecentColor(colorspec);
                }
            }
        }

        private void Popup_Closed(object sender, object e)
        {
            if (sender is Popup popup && popup.Child is HColorDialog colorDialog)
            {
                Globals.PushRecentColor(colorDialog.ColorSpec);
                UpdateColorList();

                string colorname = Utilities.ColorNameFromColorSpec(colorDialog.ColorSpec);
                if (_options.ContainsKey(RedDogGlobals.GS_Color))
                {
                    _options[RedDogGlobals.GS_Color] = colorname;
                }
                else
                {
                    _options.Add(RedDogGlobals.GS_Color, colorname);
                }
            }
        }

        Popup _popup = null;

        void ShowColorPicker(uint defaultColorSpec, string tag)
        {
            GeneralTransform tf = _alignmentComboBox.TransformToVisual(null);
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

        private void _colorComboBox_DropDownClosed(object sender, object e)
        {
            UpdateColorList();
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
                //tb.FontWeight = Windows.UI.Text.FontWeights.Light;
                tb.Style = (Style)(Application.Current.Resources["HDialogText"]);
                tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                _colorComboBox.Items.Add(tb);

                tb = new TextBlock();
                tb.Text = "Select a new color";
                tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
                tb.Style = (Style)(Application.Current.Resources["HDialogText"]);
                tb.Tag = (uint)ColorCode.SetColor;
                tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
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

        void _layerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_layerComboBox.SelectedItem is HLayerTile tile && tile.Tag is string tilelayer)
            {
                SetOption(RedDogGlobals.GS_Layer, tilelayer);
            }
            else if (_layerComboBox.SelectedItem is TextBlock tb && tb.Tag is string tag && tag == "active_layer")
            {
                SetOption(RedDogGlobals.GS_Layer, "Active_Layer");
            }
        }

        void _positionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_positionComboBox.SelectedItem is ComboBoxItem && ((ComboBoxItem)_positionComboBox.SelectedItem).Tag is string)
            {
                SetOption(RedDogGlobals.GS_Position, (string)((ComboBoxItem)_positionComboBox.SelectedItem).Tag);
            }
        }

        void _alignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_alignmentComboBox.SelectedItem is ComboBoxItem && ((ComboBoxItem)_alignmentComboBox.SelectedItem).Tag is string)
            {
                SetOption(RedDogGlobals.GS_Alignment, (string)((ComboBoxItem)_alignmentComboBox.SelectedItem).Tag);
            }
        }

        void _styleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_styleComboBox.SelectedItem is ComboBoxItem && ((ComboBoxItem)_styleComboBox.SelectedItem).Tag is string name)
            {
                foreach (TextStyle style in Globals.TextStyleTable.Values)
                {
                    if (style.Name.ToLower() == name)
                    {
                        string newStyle = (string)((ComboBoxItem)_styleComboBox.SelectedItem).Tag;
                        if (style.Id != Globals.TextStyleId)
                        {
                            SetOption(RedDogGlobals.GS_TextStyle, newStyle);

                            _fontComboBox.SelectedIndex = 0;
                            _heightBox.Value = 0;
                            _spacingBox.Value = 0;
                            _lineSpacingBox.Value = 0;
                        }
                        break;
                    }
                }
            }
        }

        public string Id
        {
            get { return RedDogGlobals.GS_CircleCommand; }
        }

        public void Populate()
        {
            _styleComboBox.Items.Clear();

            foreach (Cirros.Drawing.TextStyle style in Globals.TextStyleTable.Values)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = style.Name;
                item.Tag = style.Name.ToLower();
                _styleComboBox.Items.Add(item);
            }

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

            if (_colorComboBox != null)
            {
                _colorComboBox.Items.Clear();

                TextBlock tb = new TextBlock();
                tb.Text = "Use layer color";
                tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
                //tb.FontWeight = Windows.UI.Text.FontWeights.Light;
                tb.Style = (Style)(Application.Current.Resources["HDialogText"]);
                tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                _colorComboBox.Items.Add(tb);

                tb = new TextBlock();
                tb.Text = "Select a new color";
                tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
                //tb.FontWeight = Windows.UI.Text.FontWeights.Light;
                tb.Style = (Style)(Application.Current.Resources["HDialogText"]);
                tb.Tag = (uint)ColorCode.SetColor;
                tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                _colorComboBox.Items.Add(tb);

                foreach (uint colorspec in Globals.RecentColors)
                {
                    _colorComboBox.Items.Add(new ColorItemControl(colorspec));
                }
            }

            SetConstructionType(Globals.TextSinglePoint);

            double mfactor = Globals.ActiveDrawing.PaperUnit == Unit.Millimeters ? 25.4 : 1;

            _heightBox.Value = Globals.TextHeight * mfactor;               // 0 means use style value
            _lineSpacingBox.Value = Globals.TextLineSpacing;              // 0 means use style value
            _spacingBox.Value = Globals.TextSpacing;              // 0 means use style value

            _fontComboBox.SelectedIndex = 0;    // 0 means use style value

            foreach (object o in _fontComboBox.Items)
            {
                if (o is ComboBoxItem item && item.Tag is string fontName)
                {
                    if (fontName.ToLower() == Globals.TextFont.ToLower())
                    {
                        _fontComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            _angleBox.Value = Globals.TextAngle;
            _colorComboBox.SelectedIndex = 0;

            SetTextStyle(Globals.TextStyleId);
            SetAlignment(Globals.TextAlign);
            SetPosition(Globals.TextPosition);
            SetLayer(Globals.ActiveTextLayerId);
            SetColor(Utilities.ColorNameFromColorSpec(Globals.TextColorSpec));
        }

        void SetConstructionType(bool isSinglePoint)
        {
            string type = isSinglePoint ? RedDogGlobals.GS_1Point : RedDogGlobals.GS_2Point;

            HXAMLControl control = null;

            foreach (FrameworkElement fe in _iconRow1.Children)
            {
                if (fe is HXAMLControl)
                {
                    if (((HXAMLControl)fe).Id == type)
                    {
                        control = fe as HXAMLControl;
                        break;
                    }
                }
            }

            if (control != null)
            {
                SelectConstructControl(control);
            }
        }

        void numberBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (sender is NumberBox1 nb)
            {
                if (nb.Tag is string)
                {
                    if (nb.IsDistance && Globals.ActiveDrawing.PaperUnit == Unit.Millimeters)
                    {
                        SetOption(nb.Tag as string, nb.Value / 25.4);
                    }
                    else
                    {
                        SetOption(nb.Tag as string, nb.Value);
                    }

                    nb.Format();
                    nb.SelectionStart = nb.Text.Length;
                    nb.SelectionLength = 0;
                }
            }
        }

        public Dictionary<string, object> Options
        {
            get
            {
                if (FocusManager.GetFocusedElement() is NumberBox1)
                {
                    NumberBox1 nb = FocusManager.GetFocusedElement() as NumberBox1;
                    if (nb.Tag is string)
                    {
                        if (nb.IsDistance && Globals.ActiveDrawing.PaperUnit == Unit.Millimeters)
                        {
                            SetOption(nb.Tag as string, nb.Value / 25.4);
                        }
                        else
                        {
                            SetOption(nb.Tag as string, nb.Value);
                        }
                    }
                }

                return _options;
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

        void SelectConstructControl(HXAMLControl control)
        {
            if (_selectedIcon != null)
            {
                _selectedIcon.IsSelected = false;
            }

            _selectedIcon = control;
            _selectedIcon.IsSelected = true;

            switch (control.Id)
            {
                case RedDogGlobals.GS_1Point:
                    //_angleBox.Visibility = Visibility.Visible;
                    _angleBox.IsEnabled = true;
                    _angleBox.Value = Globals.TextAngle;
                    _angleBox.FontStyle = Windows.UI.Text.FontStyle.Normal;
                    break;

                case RedDogGlobals.GS_2Point:
                    //_angleBox.Visibility = Visibility.Collapsed;
                    _angleBox.Text = "Points";
                    _angleBox.FontStyle = Windows.UI.Text.FontStyle.Italic;
                    _angleBox.IsEnabled = false;
                    break;
            }

            //_angleLabel.Visibility = _angleBox.Visibility;

            SetOption(RedDogGlobals.GS_Construction, control.Id);
        }

        void hxamlControl_OnHXAMLControlClick(object sender, EventArgs e)
        {
            if (sender is HXAMLControl)
            {
                SelectConstructControl(sender as HXAMLControl);
            }
        }

        private void SetTextStyle(int styleId)
        {
            string styleName = null;

            if (Globals.TextStyleTable.ContainsKey(styleId))
            {
                TextStyle textStyle = Globals.TextStyleTable[styleId];
                styleName = textStyle.Name.ToLower();
            }

            for (int i = 0; i < _styleComboBox.Items.Count; i++)
            {
                if (_styleComboBox.Items[i] is ComboBoxItem && ((ComboBoxItem)_styleComboBox.Items[i]).Tag is string)
                {
                    if ((string)((ComboBoxItem)_styleComboBox.Items[i]).Tag == styleName)
                    {
                        _styleComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void SetAlignment(TextAlignment textAlignment)
        {
            string alignment;

            switch (textAlignment)
            {
                default:
                case TextAlignment.Left:
                    alignment = RedDogGlobals.GS_Left;
                    break;

                case TextAlignment.Center:
                    alignment = RedDogGlobals.GS_Center;
                    break;

                case TextAlignment.Right:
                    alignment = RedDogGlobals.GS_Right;
                    break;
            }

            for (int i = 0; i < _alignmentComboBox.Items.Count; i++)
            {
                if (_alignmentComboBox.Items[i] is ComboBoxItem && ((ComboBoxItem)_alignmentComboBox.Items[i]).Tag is string)
                {
                    if ((string)((ComboBoxItem)_alignmentComboBox.Items[i]).Tag == alignment)
                    {
                        _alignmentComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void SetPosition(TextPosition textPosition)
        {
            string position;

            switch (textPosition)
            {
                default:
                case TextPosition.Above:
                    position = RedDogGlobals.GS_Above;
                    break;

                case TextPosition.On:
                    position = RedDogGlobals.GS_On;
                    break;

                case TextPosition.Below:
                    position = RedDogGlobals.GS_Below;
                    break;
            }

            for (int i = 0; i < _positionComboBox.Items.Count; i++)
            {
                if (_positionComboBox.Items[i] is ComboBoxItem && ((ComboBoxItem)_positionComboBox.Items[i]).Tag is string)
                {
                    if ((string)((ComboBoxItem)_positionComboBox.Items[i]).Tag == position)
                    {
                        _positionComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        void SetLayer(int layerId)
        {
            if (layerId < 0)
            {
                layerId = Globals.ActiveLayerId;
                _layerComboBox.SelectedIndex = 0;
            }
            else if (Globals.LayerTable.ContainsKey(layerId))
            {
                Cirros.Drawing.Layer layer = Globals.LayerTable[layerId];

                for (int i = 0; i < _layerComboBox.Items.Count; i++)
                {
                    if (_layerComboBox.Items[i] is HLayerTile tile && tile.Tag is string tilelayer && tilelayer == layer.Name)
                    {
                        _layerComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void SetColor(string color)
        {
            for (int i = 0; i < _colorComboBox.Items.Count; i++)
            {
                if (_colorComboBox.Items[i] is ComboBoxItem && ((ComboBoxItem)_colorComboBox.Items[i]).Tag is string)
                {
                    if ((string)((ComboBoxItem)_colorComboBox.Items[i]).Tag == color)
                    {
                        _colorComboBox.SelectedIndex = i;
                        break;
                    }
                }
                else if (_colorComboBox.Items[i] is ColorItemControl cic)
                {
                    if (cic.ColorName == color)
                    {
                        _colorComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "text" }, { "source", "help" } });

            _ttAnnotationTextIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "text" }, { "source", tag } });

                tip.IsOpen = false;

                switch (tag)
                {
                    case "intro":
                        _ttAnnotationTextOnePoint.IsOpen = true;
                        break;

                    case "onepoint":
                        _ttAnnotationTextTwoPoint.IsOpen = true;
                        break;

                    case "twopoint":
                        _ttAnnotationTextAlignment.IsOpen = true;
                        break;

                    case "alignment":
                        _ttAnnotationTextPosition.IsOpen = true;
                        break;

                    case "position":
                        _ttAnnotationTextStyle.IsOpen = true;
                        break;

                    case "style":
                        _ttAnnotationTextHeight.IsOpen = true;
                        break;

                    case "height":
                         _ttAnnotationTextLineSpacing.IsOpen = true;
                       break;

                    case "line-spacing":
                        _ttAnnotationTextSpacing.IsOpen = true;
                        break;

                    case "spacing":
                        _ttAnnotationTextFont.IsOpen = true;
                        break;

                    case "font":
                        _ttAnnotationTextAngle.IsOpen = true;
                        break;

                    case "angle":
                        _ttAnnotationTextLayer.IsOpen = true;
                        break;

                    case "layer":
                        _ttAnnotationTextColor.IsOpen = true;
                        break;
                }
            }
        }
    }
}
