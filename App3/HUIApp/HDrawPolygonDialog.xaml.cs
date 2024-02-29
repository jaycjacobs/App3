using Cirros;
using Cirros.Commands;
using CirrosUI;
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
    public sealed partial class HDrawPolygonDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_PolygonCommand } };
        HXAMLControl _selectedIcon = null;

        public HDrawPolygonDialog()
        {
            this.InitializeComponent();

            this.Loaded += HDrawPolygonDialog_Loaded;
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public void WillClose()
        {
            _attributesControl.ShouldClose();
        }

        void HDrawPolygonDialog_Loaded(object sender, RoutedEventArgs e)
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

            _radiusBox.OnValueChanged += numberBox_OnValueChanged;
            _sidesBox.OnValueChanged += numberBox_OnValueChanged;

            _attributesControl.AttributeTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        public string Id
        {
            get { return RedDogGlobals.GS_PolygonCommand; }
        }

        public void Populate()
        {
            SetConstructionType(Globals.PolygonCommandType);
            _radiusBox.Value = Globals.ActiveDrawing.PaperToModel(Globals.PolygonFilletRadius);
            _sidesBox.Value = Globals.PolygonSides;

            _attributesControl.SetLayer(Globals.ActivePolygonLayerId);
            _attributesControl.SetColorSpec(Globals.PolygonColorSpec);
            _attributesControl.SetLineTypeId(Globals.PolygonLineTypeId);
            _attributesControl.SetLineWeightId(Globals.PolygonLineWeightId);
            _attributesControl.SetFill(Globals.PolygonFill);
            _attributesControl.SetPattern(Globals.PolygonPattern, Globals.PolygonPatternScale, Globals.PolygonPatternAngle);

            _ruleComboBox.SelectedItem = Globals.PolygonFillEvenOdd ? 0 : 1;
        }

        void SetConstructionType(PolygonCommandType polygonCommandType)
        {
            string type;

            switch (polygonCommandType)
            {
                case PolygonCommandType.Regular:
                    type = RedDogGlobals.GS_Regular;
                    break;

                default:
                case PolygonCommandType.Irregular:
                    type = RedDogGlobals.GS_Irregular;
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
                SelectConstructControl(control);
            }
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
                case RedDogGlobals.GS_Regular:
                    //_radiusBox.Visibility = Visibility.Collapsed;
                    //_sidesBox.Visibility = Visibility.Visible;
                    _radiusBox.IsEnabled = false;
                    _sidesBox.IsEnabled = true;
                    break;

                case RedDogGlobals.GS_Irregular:
                    //_radiusBox.Visibility = Visibility.Visible;
                    //_sidesBox.Visibility = Visibility.Collapsed;
                    _radiusBox.IsEnabled = true;
                    _sidesBox.IsEnabled = false;
                    break;
            }

            //_radiusLabel.Visibility = _radiusBox.Visibility;
            //_sidesLabel.Visibility = _sidesBox.Visibility;

            SetOption(RedDogGlobals.GS_Type, control.Id);
        }

        void hxamlControl_OnHXAMLControlClick(object sender, EventArgs e)
        {
            if (sender is HXAMLControl)
            {
                SelectConstructControl(sender as HXAMLControl);
            }
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "polygon" }, { "source", "help" } });

            _ttDrawPolygonIntro.IsOpen = true;
        }

        private void _ruleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && e.AddedItems[0] is ComboBoxItem item && item.Tag is string tag)
            { 
                Globals.PolygonFillEvenOdd = tag == "even-odd";
            }
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "polygon" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttDrawPolygonRegular.IsOpen = true;
                        break;

                    case "regular":
                        _ttDrawPolygonIrregular.IsOpen = true;
                        break;

                    case "irregular":
                        _ttDrawPolygonFilletRadius.IsOpen = true;
                        break;

                    case "radius":
                        _ttDrawPolygonSides.IsOpen = true;
                        break;

                    case "sides":
                        _ttDrawPolygonFillRule.IsOpen = true;
                        break;

                    case "rule":
                        _attributesControl.AttributeTeachingTip.IsOpen = true;
                        break;

                    case "attributes":
                        _attributesControl.FillTeachingTip.IsOpen = true;
                        break;
                }
            }
        }
    }
}
