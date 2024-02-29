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
    public sealed partial class HDrawRectangleDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_RectangleCommand } };
        HXAMLControl _selectedIcon = null;

        public HDrawRectangleDialog()
        {
            this.InitializeComponent();

            this.Loaded += HDrawRectangleDialog_Loaded;
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public void WillClose()
        {
            _attributesControl.ShouldClose();
        }

        void HDrawRectangleDialog_Loaded(object sender, RoutedEventArgs e)
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

            _widthBox.OnValueChanged += numberBox_OnValueChanged;
            _heightBox.OnValueChanged += numberBox_OnValueChanged;

            _attributesControl.AttributeTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        public string Id
        {
            get { return RedDogGlobals.GS_RectangleCommand; }
        }

        public void Populate()
        {
            SetConstructionType(Globals.RectangleType);
            _widthBox.Value = Globals.ActiveDrawing.PaperToModel(Globals.RectangleWidth);
            _heightBox.Value = Globals.ActiveDrawing.PaperToModel(Globals.RectangleHeight);

            _attributesControl.SetLayer(Globals.ActiveRectangleLayerId);
            _attributesControl.SetColorSpec(Globals.RectangleColorSpec);
            _attributesControl.SetLineTypeId(Globals.RectangleLineTypeId);
            _attributesControl.SetLineWeightId(Globals.RectangleLineWeightId);
            _attributesControl.SetFill(Globals.RectangleFill);
            _attributesControl.SetPattern(Globals.RectanglePattern, Globals.RectanglePatternScale, Globals.RectanglePatternAngle);
        }

        void SetConstructionType(RectangleCommandType rectangleCommandType)
        {
            string type;

            switch (rectangleCommandType)
            {
                default:
                case RectangleCommandType.Corners:
                    type = RedDogGlobals.GS_Corners;
                    break;


                case RectangleCommandType.Size:
                    type = RedDogGlobals.GS_Size;
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
                case RedDogGlobals.GS_Corners:
                    //_widthBox.Visibility = Visibility.Collapsed;
                    //_heightBox.Visibility = Visibility.Collapsed;
                    _widthBox.IsEnabled = false;
                    _heightBox.IsEnabled = false;
                    break;

                case RedDogGlobals.GS_Size:
                    //_widthBox.Visibility = Visibility.Visible;
                    //_heightBox.Visibility = Visibility.Visible;
                    _widthBox.IsEnabled = true;
                    _heightBox.IsEnabled = true;
                    break;
            }

            //_widthLabel.Visibility = _widthBox.Visibility;
            //_heightLabel.Visibility = _heightBox.Visibility;

            SetOption(RedDogGlobals.GS_Construction, control.Id);
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
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "rectangle" }, { "source", "help" } });

            _ttDrawRectangleIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "rectangle" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttDrawRectangleCorners.IsOpen = true;
                        break;

                    case "corners":
                        _ttDrawRectangleSize.IsOpen = true;
                        break;

                    case "size":
                        _ttDrawRectangleWidth.IsOpen = true;
                        break;

                    case "width":
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
