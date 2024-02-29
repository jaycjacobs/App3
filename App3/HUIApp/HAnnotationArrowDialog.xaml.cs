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
    public sealed partial class HAnnotationArrowDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_ArrowCommand } };
        HXAMLControl _selectedIcon = null;

        public HAnnotationArrowDialog()
        {
            this.InitializeComponent();

            this.Loaded += HAnnotationArrowDialog_Loaded;
        }

        public void WillClose()
        {
            _attributesControl.ShouldClose();
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        void HAnnotationArrowDialog_Loaded(object sender, RoutedEventArgs e)
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

            _arrowStyleComboBox.SelectionChanged += _arrowStyleComboBox_SelectionChanged;

            _attributesControl.AttributeTeachingTip.ActionButtonContent = null;

            DataContext = HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
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
            get { return RedDogGlobals.GS_ArrowCommand; }
        }

        public void Populate()
        {
            //SetPlacement(RedDogGlobals.ArrowPlacement);
            //SetStyle(RedDogGlobals.ArrowArrowStyle);

            _arrowStyleComboBox.Items.Clear();

            foreach (Cirros.Drawing.ArrowStyle style in Globals.ArrowStyleTable.Values)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = style.Name;
                item.Tag = style.Name.ToLower();
                _arrowStyleComboBox.Items.Add(item);
            }

            SetPlacement(Globals.ArrowLocation);
            SetArrowStyle(Globals.ArrowStyleId);

            _attributesControl.SetLayer(Globals.ActiveArrowLayerId);
            _attributesControl.SetColorSpec(Globals.ArrowColorSpec);
            _attributesControl.SetLineTypeId(Globals.ArrowLineTypeId);
            _attributesControl.SetLineWeightId(Globals.ArrowLineWeightId);
        }

        void SetPlacement(ArrowLocation location)
        {
            string placement;

            switch (location)
            {
                case ArrowLocation.None:
                    placement = RedDogGlobals.GS_None;
                    break;

                case ArrowLocation.Start:
                    placement = RedDogGlobals.GS_Start;
                    break;

                default:
                case ArrowLocation.End:
                    placement = RedDogGlobals.GS_End;
                    break;

                case ArrowLocation.Both:
                    placement = RedDogGlobals.GS_Both;
                    break;
            }
            HXAMLControl control = null;

            foreach (FrameworkElement fe in _iconRow1.Children)
            {
                if (fe is HXAMLControl)
                {
                    if (((HXAMLControl)fe).Id == placement)
                    {
                        control = fe as HXAMLControl;
                        break;
                    }
                }
            }

            if (control != null)
            {
                SelectPlacementControl(control);
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

        void SelectPlacementControl(HXAMLControl control)
        {
            if (_selectedIcon != null)
            {
                _selectedIcon.IsSelected = false;
            }

            _selectedIcon = control;
            _selectedIcon.IsSelected = true;

            SetOption(RedDogGlobals.GS_Placement, control.Id);
        }

        void hxamlControl_OnHXAMLControlClick(object sender, EventArgs e)
        {
            if (sender is HXAMLControl)
            {
                SelectPlacementControl(sender as HXAMLControl);
            }
        }

        private void SetArrowStyle(int styleId)
        {
            string styleName = null;

            if (styleId >= 0 && styleId < _arrowStyleComboBox.Items.Count)
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
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "arrow" }, { "source", "help" } });

            _ttAnnotationArrowIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "arrow" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttAnnotationArrowStart.IsOpen = true;
                        break;

                    case "start":
                        _ttAnnotationArrowEnd.IsOpen = true;
                        break;

                    case "end":
                        _ttAnnotationArrowBoth.IsOpen = true;
                        break;

                    case "both":
                        _ttAnnotationArrowStyle.IsOpen = true;
                        break;

                    case "style":
                        _attributesControl.AttributeTeachingTip.IsOpen = true;
                        break;
                }
            }
        }
    }
}