using Cirros;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using Cirros8.Symbols;
using CirrosUI;
using Microsoft.UI.Xaml.Controls;
using RedDog.Console;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace RedDog.HUIApp
{
    public sealed partial class HGroupPanel : UserControl
    {
        public event HGroupPanelOptionChangedHandler OnHGroupPanelOptionChanged;
        public delegate void HGroupPanelOptionChangedHandler(object sender, HGroupPanelOptionChangedEventArgs e);
        HXAMLControl _selectedIcon = null;
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_InsertGroupLinearCommand } };

        Group _selectedGroup = null;

        public HGroupPanel()
        {
            this.InitializeComponent();

            this.Loaded += HGroupPanel_Loaded;
        }

        private void HGroupPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Populate();

            foreach (FrameworkElement fe in _iconRow1.Children)
            {
                if (fe is HXAMLControl)
                {
                    HXAMLControl hxamlControl = fe as HXAMLControl;
                    hxamlControl.OnHXAMLControlClick += hxamlControl_OnHXAMLControlClick;
                    hxamlControl.IsSelected = false;
                }
            }

            _scaleBox.Value = Globals.GroupScale;

            _layerComboBox.SelectionChanged += _layerComboBox_SelectionChanged;
            //_colorComboBox.SelectionChanged += _colorComboBox_SelectionChanged;
            //_colorComboBox.DropDownClosed += _colorComboBox_DropDownClosed;
            _scaleBox.OnValueChanged += _scaleBox_OnValueChanged;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        public void SetInputType(string type)
        {
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
                if (_selectedIcon != null)
                {
                    _selectedIcon.IsSelected = false;
                }

                _selectedIcon = control;
                _selectedIcon.IsSelected = true;
            }
        }

        void hxamlControl_OnHXAMLControlClick(object sender, EventArgs e)
        {
            if (sender is HXAMLControl)
            {
                InputControlChanged(sender as HXAMLControl);
            }
        }

        async void InputControlChanged(HXAMLControl control)
        {
            if (_selectedIcon != null)
            {
                _selectedIcon.IsSelected = false;
            }

            _selectedIcon = control;
            _selectedIcon.IsSelected = true;

            OptionChanged(RedDogGlobals.GS_InsertGroupFrom, control.Id);

            if (control.Id == RedDogGlobals.GS_InsertGroupPredefined)
            {
                GeneralTransform tf = _selectedIcon.TransformToVisual(null);
                Point t = tf.TransformPoint(new Point());
                if (t.X != 0 && t.Y != 0)
                {
                    HPredefinedSymbolPanel panel = new HPredefinedSymbolPanel();
                    Popup popup = new Popup();
                    popup.IsLightDismissEnabled = true;
                    popup.Child = panel;
                    popup.HorizontalOffset = t.X; // - symbolPicker.Width;
                    popup.VerticalOffset = t.Y; // - symbolPicker.Height;
                    popup.IsOpen = true;
                    popup.Closed += Popup_Closed;
                }
            }
            else if (control.Id == RedDogGlobals.GS_InsertGroupFromFile)
            {
                _fromFileIcon.ProgressRingActive = true;

                Group g = await FileHandling.GetSingleSymbolAsync();
                _fromFileIcon.ProgressRingActive = false;

                if (g != null)
                {
                    OptionChanged(RedDogGlobals.GS_InsertGroupName, g.Name);
                    this.SelectGroup(g);
                }
            }
            else if (control.Id == RedDogGlobals.GS_InsertGroupFromLibrary)
            {
                GeneralTransform tf = _selectedIcon.TransformToVisual(null);
                Point t = tf.TransformPoint(new Point());
                if (t.X != 0 && t.Y != 0)
                {
                    SymbolPicker symbolPicker = new SymbolPicker();
                    Popup popup = new Popup();
                    popup.IsLightDismissEnabled = true;
                    popup.Child = symbolPicker;
                    popup.HorizontalOffset = t.X; // - symbolPicker.Width;
                    popup.VerticalOffset = t.Y; // - symbolPicker.Height;
                    popup.IsOpen = true;
                    popup.Closed += Popup_Closed;
                }
            }
            else if (RedDogGlobals.InsertGroupLinearFrom == RedDogGlobals.GS_InsertGroupFromDrawing)
            {
                this.SelectGroup(null);

                Globals.LinearCopyGroupName = null;
                Globals.LinearCopyGroupScale = 1;
            }
        }

        private async void Popup_Closed(object sender, object e)
        {
            if (sender is Popup popup)
            {
                if (popup.Child is SymbolPicker picker)
                {
                    if (string.IsNullOrEmpty(picker.SelectedSymbolName) == false)
                    {
                        if (picker.SelectedFolderlName.StartsWith("/"))
                        {
                            // library symbol
                        }
                        else
                        {
                            // drawing group
                            OptionChanged(RedDogGlobals.GS_InsertGroupName, picker.SelectedSymbolName);
                            Group g = await this.SelectGroupByName(picker.SelectedSymbolName);
                            this.SetGroupDefaultScale(g);
                        }
                    }
                }
                else if (popup.Child is HPredefinedSymbolPanel panel)
                {
                    if (panel != null && panel.SelectedSymbolName != null)
                    {
                        string uriName = "ms-appx:///Data/" + panel.SelectedSymbolName;
                        var uri = new System.Uri(uriName);
                        StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                        Group g = await FileHandling.GetSymbolAsync(file);
                        await this.SelectGroupByName(g.Name);
                        OptionChanged(RedDogGlobals.GS_InsertGroupName, g.Name);
                        this.SetGroupDefaultScale(g);
                    }
                }
            }
        }

        private void HXAMLControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public FrameworkElement PredefinedIcon
        {
            get { return _predefinedIcon; }
        }

        public TeachingTip PredefinedTeachingTip
        {
            get { return _ttInsertGroupPredefined; }
        }

        public TeachingTip FromFileTeachingTip
        {
            get { return _ttInsertGroupFromFile; }
        }

        public TeachingTip FromLibraryTeachingTip
        {
            get { return _ttInsertGroupFromLibrary; }
        }

        public TeachingTip FromDrawingTeachingTip
        {
            get { return _ttInsertGroupFromDrawing; }
        }

        public TeachingTip ThumbnailTeachingTip
        {
            get { return _ttInsertGroupSelectionInfo; }
        }

        public TeachingTip LayerTeachingTip
        {
            get { return _ttInsertGroupLayer; }
        }

        public TeachingTip ScaleTeachingTip
        {
            get { return _ttInsertGroupScale; }
        }

        private void _colorComboBox_DropDownClosed(object sender, object e)
        {
            UpdateColorList();
        }

        private void Populate()
        {
            if (_layerComboBox != null)
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
                //                _layerComboBox.Items.Clear();

                //                foreach (Cirros.Drawing.Layer layer in Globals.LayerTable.Values)
                //                {
                //#if true
                //                    HLayerTile item = new HLayerTile(layer);
                //                    item.Tag = layer.Name;
                //                    _layerComboBox.Items.Add(item);
                //#else
                //                    ComboBoxItem item = new ComboBoxItem();
                //                    item.Content = layer.Name;
                //                    item.Tag = layer.Name;
                //                    _layerComboBox.Items.Add(item);
                //#endif
                //                }
            }

            //if (_colorComboBox != null)
            //{
            //    _colorComboBox.Items.Clear();

            //    TextBlock tb = new TextBlock();
            //    tb.Text = "Use layer color";
            //    tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
            //    tb.Tag = (uint)ColorCode.ByLayer;
            //    tb.Style = (Style)(Application.Current.Resources["HDialogText"]);
            //    tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
            //    _colorComboBox.Items.Add(tb);

            //    tb = new TextBlock();
            //    tb.Text = "Select a new color";
            //    tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
            //    tb.Style = (Style)(Application.Current.Resources["HDialogText"]);
            //    tb.Tag = (uint)ColorCode.SetColor;
            //    tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
            //    _colorComboBox.Items.Add(tb);

            //    foreach (uint colorspec in Globals.RecentColors)
            //    {
            //        _colorComboBox.Items.Add(new ColorItemControl(colorspec));
            //    }
            //}
        }

        public void ShouldClose()
        {
        }

        private void OptionChanged(string key, object value)
        {
            if (OnHGroupPanelOptionChanged != null)
            {
                HGroupPanelOptionChangedEventArgs e = new HGroupPanelOptionChangedEventArgs(key, value);
                OnHGroupPanelOptionChanged(this, e);
            }
        }

        private void _scaleBox_OnValueChanged(object sender, CirrosUI.ValueChangedEventArgs e)
        {
            OptionChanged(RedDogGlobals.GS_Scale, _scaleBox.Value);

            if (_selectedGroup != null)
            {
                _groupSize.Text = GroupSizeString(_selectedGroup);
            }
            else
            {
                _groupSize.Text = "";
            }
        }

        void UpdateColorList()
        {
            //int max = Math.Min(_colorComboBox.Items.Count - 2, Globals.RecentColors.Count);

            //for (int i = 0; i < max; i++)
            //{
            //    ColorItemControl cic = (ColorItemControl)_colorComboBox.Items[i + 2];
            //    cic.ColorSpec = Globals.RecentColors[i];
            //}

            //if (_colorComboBox.SelectedIndex == 1)
            //{
            //    _colorComboBox.SelectedIndex = 2;
            //}
            //else if (_colorComboBox.SelectedIndex > 2)
            //{
            //    _colorComboBox.SelectedIndex = 2;
            //}
        }

        void _colorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (_colorComboBox.SelectedIndex == 0)
            //{
            //    OptionChanged(RedDogGlobals.GS_Color, RedDogGlobals.GS_ByLayer);
            //}
            //else if (_colorComboBox.Items.Count > 2 && _colorComboBox.Items[2] is ColorItemControl)
            //{
            //    uint colorspec = ((ColorItemControl)_colorComboBox.Items[2]).ColorSpec;

            //    if (_colorComboBox.SelectedIndex == 1)
            //    {
            //        ShowColorPicker(colorspec, "color");
            //    }
            //    else if (_colorComboBox.SelectedItem is ColorItemControl cic)
            //    {
            //        colorspec = cic.ColorSpec;
            //        string colorname = Utilities.ColorNameFromColorSpec(colorspec);

            //        OptionChanged(RedDogGlobals.GS_Color, colorname);

            //        Globals.PushRecentColor(colorspec);
            //    }
            //}
        }

        void ShowColorPicker(uint defaultColorSpec, string field)
        {
            _colorPickerPopup.IsLightDismissEnabled = true;

            CirrosUI.ColorPicker colorPicker = new CirrosUI.ColorPicker();
            colorPicker.Width = 350;
            colorPicker.Tag = field;

            Brush dark = (Brush)(Application.Current.Resources["SettingsDarkForeground"]);
            colorPicker.ShowBorder(dark, 2);
            colorPicker.Color = Cirros.Utility.Utilities.ColorFromColorSpec(defaultColorSpec);

            colorPicker.OnColorSelected += colorPicker_OnColorSelected;

            //if (!_colorPickerPopup.IsOpen)
            //{
            //    double left = (double)_colorComboBox.GetValue(Canvas.LeftProperty);
            //    _colorPickerPopup.HorizontalOffset = left + _colorComboBox.ActualWidth - colorPicker.Width;

            //    if (_colorPickerPopup.HorizontalOffset < 10)
            //    {
            //        _colorPickerPopup.HorizontalOffset = 10;
            //    }
            //    _colorPickerPopup.Child = colorPicker as UserControl;
            //    _colorPickerPopup.IsOpen = true;
            //}
        }

        private void colorPicker_OnColorSelected(object sender, ColorSelectedEventArgs e)
        {
            //if (e.FinalSelection && _colorPickerPopup.IsOpen)
            //{
            //    _colorPickerPopup.IsOpen = false;
            //}
        }

        private void _colorPickerPopup_Closed(object sender, object e)
        {
            //if (_colorPickerPopup.Child is CirrosUI.ColorPicker)
            //{
            //    CirrosUI.ColorPicker colorPicker = _colorPickerPopup.Child as CirrosUI.ColorPicker;
            //    SelectColor(colorPicker.Color);

            //    colorPicker.OnColorSelected -= colorPicker_OnColorSelected;
            //    _colorPickerPopup.Child = null;
            //}
        }

        private void SelectColor(Color color)
        {
            uint colorspec = Utilities.ColorSpecFromColor(color);
            string colorname = Utilities.ColorNameFromColorSpec(colorspec);

            UpdateColorList();
        }

        void _layerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_layerComboBox.SelectedItem is ComboBoxItem && ((ComboBoxItem)_layerComboBox.SelectedItem).Tag is string layer)
            {
                OptionChanged(RedDogGlobals.GS_Layer, layer);
            }
            else if (_layerComboBox.SelectedItem is HLayerTile tile && tile.Tag is string tilelayer)
            {
                OptionChanged(RedDogGlobals.GS_Layer, tilelayer);
            }
            else if (_layerComboBox.SelectedItem is TextBlock tb && tb.Tag is string tag)
            {
                if (tag == "active_layer")
                {
                    OptionChanged(RedDogGlobals.GS_Layer, "Active_Layer");
                }
            }
        }

        public void SetLayer(int layerId)
        {
            if (Globals.LayerTable.ContainsKey(layerId))
            {
                Cirros.Drawing.Layer layer = Globals.LayerTable[layerId];

                for (int i = 0; i < _layerComboBox.Items.Count; i++)
                {
                    if (_layerComboBox.Items[i] is ComboBoxItem && ((ComboBoxItem)_layerComboBox.Items[i]).Tag is string)
                    {
                        if ((string)((ComboBoxItem)_layerComboBox.Items[i]).Tag == layer.Name)
                        {
                            _layerComboBox.SelectedIndex = i;
                            break;
                        }
                    }
                    else if (_layerComboBox.Items[i] is HLayerTile tile && tile.Tag is string tilelayer)
                    {
                        if (tilelayer == layer.Name)
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

        public void SetColor(ColorCode colorCode)
        {
            //for (int i = 0; i < _colorComboBox.Items.Count; i++)
            //{
            //    if (_colorComboBox.Items[i] is ColorItemControl cic)
            //    {
            //        if (cic.ColorSpec == (uint)colorCode)
            //        {
            //            _colorComboBox.SelectedIndex = i;
            //            break;
            //        }
            //    }
            //    else if (_colorComboBox.Items[i] is TextBlock tb && tb.Tag is uint cspec)
            //    {
            //        if (cspec == (uint)colorCode)
            //        {
            //            _colorComboBox.SelectedIndex = i;
            //            break;
            //        }
            //    }
            //}
        }

        public async void SelectGroup(Group g)
        {
            await SetSelectedGroup(g);
        }

        public async Task<Group> SelectGroupByName(string name)
        {
            Group g = null;

            if (string.IsNullOrEmpty(name) == false)
            {
                g = Globals.ActiveDrawing.GetGroup(name);
            }

            await SetSelectedGroup(g);

            return g;
        }

        public void SetGroupDefaultScale(Group g)
        {
            if (g != null)
            {
                _scaleBox.Value = 1 / g.PreferredScale;
                OptionChanged(RedDogGlobals.GS_Scale, _scaleBox.Value);
            }
        }

        public void SetGroupScale(double scale)
        {
            _scaleBox.Value = scale;
            OptionChanged(RedDogGlobals.GS_Scale, _scaleBox.Value);
        }

        public string GroupSizeString(Group g)
        {
            string s = "";
            string us = "";

            if (g != null)
            {
                Unit unit = g.CoordinateSpace == CoordinateSpace.Model ? g.ModelUnit : g.PaperUnit;

                switch (unit)
                {
                    case Unit.Centimeters:
                        us = " cm";
                        break;
                    case Unit.Millimeters:
                        us = " mm";
                        break;
                    case Unit.Meters:
                        us = " m";
                        break;
                    case Unit.Feet:
                        us = "'";
                        break;
                    case Unit.Inches:
                        us = "\"";
                        break;
                }
                s = $"{Math.Round(g.ModelBounds.Width * _scaleBox.Value, 2)}{us} x {Math.Round(g.ModelBounds.Height * _scaleBox.Value, 2)}{us}";
            }

            return s;
        }

        private async Task SetSelectedGroup(Group g)
        {
            _selectedGroup = null;

            if (g == null)
            {
                _groupName.Text = "Pick from drawing";
                _groupName.FontStyle = Windows.UI.Text.FontStyle.Italic;
                _groupSize.Text = "";

                var uri = new System.Uri("ms-appx:///Assets/pick-icon-thumbnail.png");
                StorageFile file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
                if (file != null)
                {
                    BitmapImage src = new BitmapImage();
                    src.SetSource(await file.OpenAsync(FileAccessMode.Read));
                    _thumbnail.Source = src;
                }
            }
            else
            {
                try
                {
                    StorageFile thumbfile = await FileHandling.GetGroupThumbnail(g);
                    if (thumbfile != null)
                    {
                        BitmapImage src = new BitmapImage();
                        src.SetSource(await thumbfile.OpenAsync(FileAccessMode.Read));
                        _thumbnail.Source = src;
                        _groupName.Text = g.Name;
                        _groupName.FontStyle = Windows.UI.Text.FontStyle.Normal;
                        _thumbnail.Visibility = Visibility.Visible;
                        _groupSize.Text = GroupSizeString(g);
                    }

                    _selectedGroup = g;

                }
                catch
                {
                    _groupName.Text = "Can't load group";
                    _groupName.FontStyle = Windows.UI.Text.FontStyle.Italic;
                    _groupSize.Text = "";
                    _thumbnail.Source = null;
                }
            }
        }
    }

    public class HGroupPanelOptionChangedEventArgs : EventArgs
    {
        public string OptionName { get; private set; }
        public object OptionValue { get; private set; }

        public HGroupPanelOptionChangedEventArgs(string optionName, object optionValue)
        {
            OptionName = optionName;
            OptionValue = optionValue;
        }
    }
}
