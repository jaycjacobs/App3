using Cirros;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Utility;
using CirrosUI;
using RedDog.HUIApp;
using HUI;
using RedDog;
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Cirros.Actions;
using Microsoft.Graphics.Display;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using RedDog.Console;
using Microsoft.UI.Xaml.Controls;

namespace RedDog.HUIApp
{
    public sealed partial class HSettingsLayers : UserControl, HUIIDialog
    {
        private Dictionary<string, object> _options = new Dictionary<string, object>();
        Layer _currentLayer = null;

        public string Id
        {
            get { return RedDogGlobals.GS_SettingsLayersCommand; }
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public Dictionary<string, object> Options
        {
            get { return _options; }
        }

        public HSettingsLayers()
        {
            this.InitializeComponent();

            this.Loaded += HSettingsLayers_Loaded;
        }

        private void _layerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_layerComboBox.SelectedItem is ComboBoxItem item && item.Tag is int id && Globals.LayerTable.ContainsKey(id))
            {
                SelectLayer(Globals.LayerTable[id]);
            }
            else if (_layerComboBox.SelectedItem is HLayerTile tile && tile.Tag is int tid && Globals.LayerTable.ContainsKey(tid))
            {
                SelectLayer(Globals.LayerTable[tid]);
            }
        }

        private void _layerVisibleCB_Checked(object sender, RoutedEventArgs e)
        {
            if (_currentLayer != null && _currentLayer.Visible != _layerVisibleCB.IsChecked)
            {
                _currentLayer.Visible = _layerVisibleCB.IsChecked == true;
                Layer.PropagateLayerChanges(_currentLayer.Id);
            }
        }

        private void HSettingsLayers_Loaded(object sender, RoutedEventArgs e)
        {
            _colorComboBox.SelectionChanged += _colorComboBox_SelectionChanged;
            _linetypeComboBox.SelectionChanged += _linetypeComboBox_SelectionChanged;
            _thicknessBox.OnValueChanged += _thicknessBox_OnValueChanged;
            _layerVisibleCB.Checked += _layerVisibleCB_Checked;
            _layerVisibleCB.Unchecked += _layerVisibleCB_Checked;
            _layerComboBox.SelectionChanged += _layerComboBox_SelectionChanged;
            _layerNameBox.LostFocus += _layerNameBox_LostFocus;
            _layerNameBox.KeyDown += _layerNameBox_KeyDown;

            Populate();

            foreach (object o in _layerComboBox.Items)
            {
                if (o is ComboBoxItem item)
                {
                    if (item.Tag is int layerId && layerId == RedDogGlobals.SelectedLayerId)
                    {
                        _layerComboBox.SelectedItem = item;
                        break;
                    }
                }
                else if (o is HLayerTile tile)
                {
                    if (tile.Tag is int layerId && layerId == RedDogGlobals.SelectedLayerId)
                    {
                        _layerComboBox.SelectedItem = tile;
                        break;
                    }
                }
            }

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        private void _thicknessBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_currentLayer != null && UpdateCurrentLayerAttributes())
            {
                RefreshLayerList();
                UpdateLineSample();
                Layer.PropagateLayerChanges(_currentLayer.Id);
            }
        }

        private void _linetypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentLayer != null && UpdateCurrentLayerAttributes())
            {
                RefreshLayerList();
                UpdateLineSample();
                Layer.PropagateLayerChanges(_currentLayer.Id);
            }
        }

        private void _layerNameBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                UpdateLayerName();
            }
        }

        private void _layerNameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateLayerName();
        }

        private void SelectActiveLayer(ComboBox box, int layerId)
        {
            foreach (object o in box.Items)
            {
                if (o is HLayerTile tile && (int)tile.Tag == layerId)
                {
                    box.SelectedItem = tile;
                    break;
                }
            }
        }

        private void RefreshLayerList()
        {
            int selectedIndex = _layerComboBox.SelectedIndex;

            _layerComboBox.Items.Clear();

            foreach (Layer layer in Globals.LayerTable.Values)
            {
                HLayerTile item = new HLayerTile(layer);
                item.Tag = layer.Id;
                _layerComboBox.Items.Add(item);
            }

            if (selectedIndex < _layerComboBox.Items.Count)
            {
                _layerComboBox.SelectedIndex = selectedIndex;
            }
            else
            {
                _layerComboBox.SelectedIndex = 0;
            }
        }

        private void SelectLayer(Layer selectedLayer)
        {
            if (_currentLayer != null && UpdateCurrentLayerAttributes())
            {
                Globals.Events.AttributesListChanged();
                UpdateLineSample();
                Layer.PropagateLayerChanges(_currentLayer.Id);
            }

            _layerNameBox.Visibility = Visibility.Collapsed;
            _layerComboBox.Visibility = Visibility.Visible;

            _currentLayer = null;

            if (selectedLayer != null)
            {
                RedDogGlobals.SelectedLayerId = selectedLayer.Id;

                int objectCount = Globals.ActiveDrawing.ObjectsInLayer(selectedLayer.Id);
                _objectCountBox.Text = objectCount == 0 ? "None" : objectCount.ToString(); ;

                _layerNameBox.Text = selectedLayer.Name;
                _layerNameBox.IsEnabled = selectedLayer.Id != 0;

                foreach (ComboBoxItem ltitem in _linetypeComboBox.Items)
                {
                    if (ltitem.Tag is int ltid && ltid == selectedLayer.LineTypeId)
                    {
                        _linetypeComboBox.SelectedItem = ltitem;
                        break;
                    }
                }

                _thicknessBox.Value = selectedLayer.LineWeightId == 0 ? .01 : (double)selectedLayer.LineWeightId / 1000;

                if (Globals.ActiveDrawing.PaperUnit == Unit.Millimeters)
                {
                    _thicknessBox.Value *= 25.4;
                }

                if (Globals.PushRecentColor(selectedLayer.ColorSpec))
                {
                    UpdateColorList();
                }
               
                _colorComboBox.SelectedIndex = 1;

                //_renameLayerButton.Visibility = selectedLayer.Id == 0 ? Visibility.Collapsed : Visibility.Visible;
                _renameLayerButton.IsEnabled = selectedLayer.Id != 0;
                _deleteLayerButton.IsEnabled = selectedLayer.Id != 0 && objectCount == 0;
                _layerVisibleCB.IsChecked = selectedLayer.Visible;

                _currentLayer = selectedLayer;

                UpdateLineSample();
            }
            else
            {
                _layerVisibleCB.IsChecked = false;
                //_renameLayerButton.Visibility = Visibility.Collapsed;
                _renameLayerButton.IsEnabled = false;
                _layerNameBox.Text = "";
                _deleteLayerButton.IsEnabled = false;
            }
        }

        public void Populate()
        {
            RefreshLayerList();

            _linetypeComboBox.Items.Clear();

            foreach (Cirros.Drawing.LineType linetype in Globals.LineTypeTable.Values)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = linetype.Name;
                item.Tag = linetype.Id;
                item.FontStyle = Windows.UI.Text.FontStyle.Normal;
                _linetypeComboBox.Items.Add(item);
            }

            UpdateColorList();

            //_layerComboBox.SelectedIndex = 0;
        }

        private void UpdateColorComboBoxItems()
        {
            if (_colorComboBox != null)
            {
                _colorComboBox.Items.Clear();

                TextBlock tb = new TextBlock();
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
            }
        }

        void UpdateColorList()
        {
            if (_colorComboBox != null)
            {
                _colorComboBox.Items.Clear();

                TextBlock tb = new TextBlock();
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

                if (_colorComboBox.Items.Count > 1)
                {
                    _colorComboBox.SelectedIndex = 1;
                }
            }
        }

        private bool UpdateCurrentLayerAttributes()
        {
            bool needsUpdate = false;

            if (_currentLayer != null)
            {
                if (_colorComboBox.SelectedItem is ColorItemControl cic && _currentLayer.ColorSpec != cic.ColorSpec)
                {
                    _currentLayer.ColorSpec = cic.ColorSpec;
                    needsUpdate = true;
                }

                if (_linetypeComboBox.SelectedItem is ComboBoxItem cbi && cbi.Tag is int ltid && _currentLayer.LineTypeId != ltid)
                {
                    _currentLayer.LineTypeId = ltid;
                    needsUpdate = true;
                }

                int lwid = (int)Math.Round((Globals.ActiveDrawing.PaperUnit == Unit.Millimeters ? _thicknessBox.Value / 25.4 : _thicknessBox.Value) * 1000);

                if (_currentLayer.LineWeightId != lwid)
                {
                    _currentLayer.LineWeightId = lwid;
                    needsUpdate = true;
                }

                if (_currentLayer.Visible != _layerVisibleCB.IsChecked)
                {
                    _currentLayer.Visible = _layerVisibleCB.IsChecked == true;
                    needsUpdate = true;
                }
            }

            return needsUpdate;
        }

        void _colorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object o = FocusManager.GetFocusedElement();
            if (o is ComboBoxItem)
            {
                if (_colorComboBox.Items.Count > 1 && _colorComboBox.Items[1] is ColorItemControl)
                {
                    if (_colorComboBox.SelectedIndex == 0)
                    {
                        uint colorspec = ((ColorItemControl)_colorComboBox.Items[1]).ColorSpec;
                        ShowColorPicker(colorspec, "color");
                    }
                    else if (_colorComboBox.SelectedItem is ColorItemControl item)
                    {
                        if (Globals.PushRecentColor(item.ColorSpec))
                        {
                            UpdateColorList();
                        }
                    }

                    if (_currentLayer != null && UpdateCurrentLayerAttributes())
                    {
                        RefreshLayerList();
                        UpdateLineSample();
                        Layer.PropagateLayerChanges(_currentLayer.Id);
                    }
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

                if (_currentLayer != null && UpdateCurrentLayerAttributes())
                {
                    RefreshLayerList();
                    UpdateLineSample();
                    Layer.PropagateLayerChanges(_currentLayer.Id);
                }
            }

            _popup = null;
        }

        public void WillClose()
        {
            if (_popup != null)
            {
                _popup.IsOpen = false;
            }

            if (_currentLayer != null && UpdateCurrentLayerAttributes())
            {
                Layer.PropagateLayerChanges(_currentLayer.Id);
                Globals.Events.AttributesListChanged();
            }
        }

        private void _renameLayerButton_Click(object sender, RoutedEventArgs e)
        {
            EnableRename();
        }

        private void EnableRename()
        {
            _layerComboBox.Visibility = Visibility.Collapsed;
            _layerNameBox.Visibility = Visibility.Visible;
            _layerNameBox.Focus(FocusState.Programmatic);
            //_layerNameBox.Select(_layerNameBox.Text.Length, 0);
            _layerNameBox.SelectAll();
        }

        private void UpdateLayerName()
        {
            if (_currentLayer != null && _currentLayer.Id != 0)
            {
                if (_currentLayer.Name != _layerNameBox.Text)
                {
                    _currentLayer.Name = _layerNameBox.Text;
                    int index = _layerComboBox.SelectedIndex;
                    RefreshLayerList();
                    _layerComboBox.SelectedIndex = index;
                }
            }

            _layerNameBox.Visibility = Visibility.Collapsed;
            _layerComboBox.Visibility = Visibility.Visible;
        }

        private void _newLayerButton_Click(object sender, RoutedEventArgs e)
        {
            Layer layer = Globals.ActiveDrawing.NewLayer();

            HLayerTile item = new HLayerTile(layer);
            item.Tag = layer.Id;
            _layerComboBox.Items.Add(item);

            _layerComboBox.SelectedItem = item;

            EnableRename();
        }

        private void _deleteLayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLayer != null && Globals.LayerTable.ContainsKey(_currentLayer.Id))
            {
                Globals.CommandDispatcher.AddUndoableAction(ActionID.RestoreLayer, _currentLayer);

                // The Delete button should not be enabled for active layers, non-empty layers or layer 0.
                // DeleteLayer() will also check this.
                Globals.ActiveDrawing.DeleteLayer(_currentLayer.Id);

                RefreshLayerList();
                _layerComboBox.SelectedIndex = 0;

                _currentLayer = null;
            }
        }

        private void UpdateLineSample()
        {
            if (_currentLayer != null)
            {
                Brush brush = new SolidColorBrush(Utilities.ColorFromColorSpec(_currentLayer.ColorSpec));
                _line.Stroke = brush;

                //_line.StrokeThickness = _currentLayer.LineWeightId < 5 ? .5 : (double)_currentLayer.LineWeightId / 10;
                //var displayInformation = DisplayInformation.GetForCurrentView();
                //_line.StrokeThickness = Math.Max(.4, ((double)_currentLayer.LineWeightId / 1000) * displayInformation.RawDpiX);
                _line.StrokeThickness = Math.Max(.4, ((double)_currentLayer.LineWeightId / 1000) * Globals.DPI);

                if (Globals.LineTypeTable.ContainsKey(_currentLayer.LineTypeId))
                {
                    if (Globals.LineTypeTable[_currentLayer.LineTypeId].StrokeDashArray == null)
                    {
                        _line.StrokeDashArray = null;
                    }
                    else
                    {
                        DoubleCollection dc = new DoubleCollection();
                        foreach (double d in Globals.LineTypeTable[_currentLayer.LineTypeId].StrokeDashArray)
                        {
                            dc.Add(d * 72 / _line.StrokeThickness);
                        }
                        _line.StrokeDashArray = dc;
                    }
                }
            }
        }

        private void _sampleField_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize != e.PreviousSize)
            {
                double h = _line.StrokeThickness * 2 + _line.Margin.Top + _line.Margin.Bottom;
                if (_canvasHeight.MinHeight < h)
                {
                    _canvasHeight.MinHeight = h;
                }

                if (_sampleField.Height != h || _sampleField.Width != _grid.ActualWidth)
                {
                    _sampleField.Height = h;
                    _sampleField.Width = _grid.ActualWidth;
                    _line.Y1 = _line.Y2 = (h - 0) / 2;
                    _line.X2 = _sampleField.Width;
                }
            }
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-layers" }, { "source", "help" } });

            _ttSettingsLayersIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-layers" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttSettingsLayersName.IsOpen = true;
                        break;

                    case "name":
                        _ttSettingsLayersRename.IsOpen = true;
                        break;

                    case "rename":
                        _ttSettingsLayersSample.IsOpen = true;
                        break;

                    case "sample":
                        _ttSettingsLayersColor.IsOpen = true;
                        break;

                    case "color":
                        _ttSettingsLayersLineType.IsOpen = true;
                        break;

                    case "ltype":
                        _ttSettingsLayersLineThickness.IsOpen = true;
                        break;

                    case "thickness":
                        _ttSettingsLayersCount.IsOpen = true;
                        break;

                    case "count":
                        _ttSettingsLayersVisible.IsOpen = true;
                        break;

                    case "visible":
                        _ttSettingsLayersAdd.IsOpen = true;
                        break;

                    case "add":
                        _ttSettingsLayersDelete.IsOpen = true;
                        break;
                }
            }
        }
    }
}
