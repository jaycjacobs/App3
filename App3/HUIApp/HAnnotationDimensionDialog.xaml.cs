using Cirros;
using Cirros.Drawing;
using Cirros.Primitives;
using CirrosUI;
using CirrosUWP.HUIApp;
using HUI;
using Microsoft.UI.Xaml.Controls;
using RedDog;
using RedDog.Console;
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace RedDog.HUIApp
{
    public sealed partial class HAnnotationDimensionDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_DimensionCommand } };
        HXAMLControl _selectedIcon = null;

        public HAnnotationDimensionDialog()
        {
            this.InitializeComponent();

            this.Loaded += HAnnotationDimensionDialog_Loaded;
        }

        public void WillClose()
        {
            _attributesControl.ShouldClose();
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        void HAnnotationDimensionDialog_Loaded(object sender, RoutedEventArgs e)
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

            foreach (FrameworkElement fe in _iconRow2.Children)
            {
                if (fe is HXAMLControl)
                {
                    HXAMLControl hxamlControl = fe as HXAMLControl;
                    hxamlControl.OnHXAMLControlClick += hxamlControl_OnHXAMLControlClick;
                    hxamlControl.IsSelected = false;
                }
            }

            Populate();

            _styleComboBox.SelectionChanged += _styleComboBox_SelectionChanged;
            _arrowStyleComboBox.SelectionChanged += _arrowStyleComboBox_SelectionChanged;

            _showTextCB.Checked += checkBox_Checked;
            _showTextCB.Unchecked += checkBox_Checked;

            _showExtensionCB.Checked += checkBox_Checked;
            _showExtensionCB.Unchecked += checkBox_Checked;

            _attributesControl.AttributeTeachingTip.ActionButtonContent = null;

            DataContext = HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox && ((CheckBox)sender).Tag is string)
            {
                CheckBox cb = sender as CheckBox;
                SetOption((string)cb.Tag, cb.IsChecked);
            }
        }

        void _styleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_styleComboBox.SelectedItem is ComboBoxItem && ((ComboBoxItem)_styleComboBox.SelectedItem).Tag is string)
            {
                SetOption(RedDogGlobals.GS_TextStyle, (string)((ComboBoxItem)_styleComboBox.SelectedItem).Tag);
            }
        }

        void _arrowStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_arrowStyleComboBox.SelectedItem is ComboBoxItem && ((ComboBoxItem)_arrowStyleComboBox.SelectedItem).Tag is string)
            {
                SetOption(RedDogGlobals.GS_ArrowStyle, (string)((ComboBoxItem)_arrowStyleComboBox.SelectedItem).Tag);
            }
        }

        public string Id
        {
            get { return RedDogGlobals.GS_DimensionCommand; }
        }

        public void Populate()
        {
            //SetType(RedDogGlobals.DimensionType);

            //SetStyle(RedDogGlobals.DimensionTextStyle);
            //SetArrowStyle(RedDogGlobals.DimensionArrowStyle);

            //_showExtensionCB.IsChecked = RedDogGlobals.DimensionShowExtension;
            //_showTextCB.IsChecked = RedDogGlobals.DimensionShowText;

            _styleComboBox.Items.Clear();

            foreach (Cirros.Drawing.TextStyle style in Globals.TextStyleTable.Values)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = style.Name;
                item.Tag = style.Name.ToLower();
                _styleComboBox.Items.Add(item);
            }

            _arrowStyleComboBox.Items.Clear();

            foreach (Cirros.Drawing.ArrowStyle style in Globals.ArrowStyleTable.Values)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = style.Name;
                item.Tag = style.Name.ToLower();
                _arrowStyleComboBox.Items.Add(item);
            }

            SetType(Globals.DimensionType);

            SetTextStyle(Globals.DimTextStyleId);
            SetArrowStyle(Globals.DimArrowStyleId);

            _showExtensionCB.IsChecked = Globals.ShowDimensionExtension;
            _showTextCB.IsChecked = Globals.ShowDimensionText;

            _attributesControl.SetLayer(Globals.ActiveDimensionLayerId);
            _attributesControl.SetColorSpec(Globals.DimensionColorSpec);
            _attributesControl.SetLineTypeId(Globals.DimensionLineTypeId);
            _attributesControl.SetLineWeightId(Globals.DimensionLineWeightId);
        }

        void SetType(PDimension.DimType dimensionType)
        {
            string type;

            switch (dimensionType)
            {
                case PDimension.DimType.Baseline:
                    type = RedDogGlobals.GS_Baseline;
                    break;

                case PDimension.DimType.Outside:
                    type = RedDogGlobals.GS_Outside;
                    break;

                default:
                case PDimension.DimType.Incremental:
                    type = RedDogGlobals.GS_Incremental;
                    break;

                case PDimension.DimType.PointToPoint:
                    type = RedDogGlobals.GS_PointToPoint;
                    break;

                case PDimension.DimType.IncrementalAngular:
                    type = RedDogGlobals.GS_Angular;
                    break;

                case PDimension.DimType.BaselineAngular:
                    type = RedDogGlobals.GS_AngularBaseline;
                    break;
            }

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
                SelectTypeControl(control);
            }
        }

        void numberBox_OnValueChanged(object sender, CirrosUI.ValueChangedEventArgs e)
        {
            if (sender is NumberBox1)
            {
                NumberBox1 nb = sender as NumberBox1;
                if (nb.Tag is string)
                {
                    SetOption(nb.Tag as string, nb.Value);
                }
            }
        }

        public Dictionary<string, object> Options
        {
            get
            {
                Dictionary<string, object> attributes = _attributesControl.Options;

                foreach (string key in attributes.Keys)
                {
                    SetOption(key, attributes[key]);
                }

                if (FocusManager.GetFocusedElement() is NumberBox1)
                {
                    NumberBox1 nb = FocusManager.GetFocusedElement() as NumberBox1;
                    if (nb.Tag is string)
                    {
                        SetOption(nb.Tag as string, nb.Value);
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

        void SelectTypeControl(HXAMLControl control)
        {
            if (_selectedIcon != null)
            {
                _selectedIcon.IsSelected = false;
            }

            _selectedIcon = control;
            _selectedIcon.IsSelected = true;

            switch (control.Id)
            {
                case RedDogGlobals.GS_Angular:
                case RedDogGlobals.GS_AngularBaseline:
                case RedDogGlobals.GS_Baseline:
                case RedDogGlobals.GS_Incremental:
                    //_showExtensionCB.Visibility = Visibility.Visible;
                    _showExtensionCB.IsEnabled = true;
                    break;

                case RedDogGlobals.GS_PointToPoint:
                    //_showExtensionCB.Visibility = Visibility.Collapsed;
                    _showExtensionCB.IsEnabled = false;
                    break;
            }

            SetOption(RedDogGlobals.GS_Type, control.Id);
        }

        void hxamlControl_OnHXAMLControlClick(object sender, EventArgs e)
        {
            if (sender is HXAMLControl)
            {
                SelectTypeControl(sender as HXAMLControl);
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

        private void SetArrowStyle(int styleId)
        {
            string styleName = null;

            if (Globals.ArrowStyleTable.ContainsKey(styleId))
            {
                ArrowStyle textStyle = Globals.ArrowStyleTable[styleId];
                styleName = textStyle.Name.ToLower();
            }

            for (int i = 0; i < _arrowStyleComboBox.Items.Count; i++)
            {
                if (_arrowStyleComboBox.Items[i] is ComboBoxItem && ((ComboBoxItem)_arrowStyleComboBox.Items[i]).Tag is string)
                {
                    if ((string)((ComboBoxItem)_arrowStyleComboBox.Items[i]).Tag == styleName)
                    {
                        _arrowStyleComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        //private void SetStyle(string style)
        //{
        //    for (int i = 0; i < _styleComboBox.Items.Count; i++)
        //    {
        //        if (_styleComboBox.Items[i] is ComboBoxItem && ((ComboBoxItem)_styleComboBox.Items[i]).Tag is string)
        //        {
        //            if ((string)((ComboBoxItem)_styleComboBox.Items[i]).Tag == style)
        //            {
        //                _styleComboBox.SelectedIndex = i;
        //                break;
        //            }
        //        }
        //    }
        //}

        //private void SetArrowStyle(string style)
        //{
        //    for (int i = 0; i < _arrowStyleComboBox.Items.Count; i++)
        //    {
        //        if (_arrowStyleComboBox.Items[i] is ComboBoxItem && ((ComboBoxItem)_arrowStyleComboBox.Items[i]).Tag is string)
        //        {
        //            if ((string)((ComboBoxItem)_arrowStyleComboBox.Items[i]).Tag == style)
        //            {
        //                _arrowStyleComboBox.SelectedIndex = i;
        //                break;
        //            }
        //        }
        //    }
        //}

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "dimension" }, { "source", "help" } });

            _ttAnnotationDimensionIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "dimension" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttAnnotationDimensionIncremental.IsOpen = true;
                        break;

                    case "incremental":
                        _ttAnnotationDimensionBaseline.IsOpen = true;
                        break;

                    case "baseline":
                        _ttAnnotationDimensionPoint2Point.IsOpen = true;
                        break;

                    case "point2point":
                        _ttAnnotationDimensionAngular.IsOpen = true;
                        break;

                    case "angular":
                        _ttAnnotationDimensionBaselineAngular.IsOpen = true;
                        break;

                    case "baselineangular":
                        _ttAnnotationDimensionTextStyle.IsOpen = true;
                        break;

                    case "style":
                        _ttAnnotationDimensionArrowStyle.IsOpen = true;
                        break;

                    case "arrowstyle":
                        _ttAnnotationDimensionShowText.IsOpen = true;
                        break;

                    case "showtext":
                        _ttAnnotationDimensionShowExt.IsOpen = true;
                        break;

                    case "showext":
                        _attributesControl.AttributeTeachingTip.IsOpen = true;
                        break;
                }
            }
        }
    }
}
