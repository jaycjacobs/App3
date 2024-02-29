using Cirros;
using Cirros.Commands;
using Cirros.Utility;
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
    public sealed partial class HDrawEllipseDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_EllipseCommand } };
        HXAMLControl _selectedIcon = null;

        public HDrawEllipseDialog()
        {
            this.InitializeComponent();

            this.Loaded += HDrawEllipseDialog_Loaded;
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public void WillClose()
        {
            _attributesControl.ShouldClose();
        }

        void HDrawEllipseDialog_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (FrameworkElement fe in _iconRow1.Children)
            {
                if (fe is HXAMLControl)
                {
                    HXAMLControl hxamlControl = fe as HXAMLControl;
                    hxamlControl.OnHXAMLControlClick += hxamlControl_OnHXAMLControlClick;
                    hxamlControl.IsSelected = false;
                }

                _attributesControl.TitleColumnMinWidth = _titleColumn.ActualWidth;
            }

            Populate();

            _majorBox.OnValueChanged += numberBox_OnValueChanged;
            _ratioBox.OnValueChanged += numberBox_OnValueChanged;
            _axisAngleBox.OnValueChanged += numberBox_OnValueChanged;
            _startBox.OnValueChanged += numberBox_OnValueChanged;
            _includedBox.OnValueChanged += numberBox_OnValueChanged;

            _attributesControl.AttributeTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;

            DataContext = HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
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

        public string Id
        {
            get { return RedDogGlobals.GS_EllipseCommand; }
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

        public void Populate()
        {
            SetConstructionType(Globals.EllipseCommandType);

            _majorBox.Value = Globals.ActiveDrawing.PaperToModel(Globals.EllipseMajorLength);
            _ratioBox.Value = Globals.EllipseMajorMinorRatio;
            _axisAngleBox.Value = Globals.EllipseAxisAngle * Construct.cRadiansToDegrees;
            _startBox.Value = Globals.EllipseStartAngle * Construct.cRadiansToDegrees;
            _includedBox.Value = Globals.EllipseIncludedAngle == 0 ? 360 : Globals.EllipseIncludedAngle * Construct.cRadiansToDegrees;

            _attributesControl.SetLayer(Globals.ActiveEllipseLayerId);
            _attributesControl.SetColorSpec(Globals.EllipseColorSpec);
            _attributesControl.SetLineTypeId(Globals.EllipseLineTypeId);
            _attributesControl.SetLineWeightId(Globals.EllipseLineWeightId);
            _attributesControl.SetFill(Globals.EllipseFill);
            _attributesControl.SetPattern(Globals.EllipsePattern, Globals.EllipsePatternScale, Globals.EllipsePatternAngle);
        }

        void SetConstructionType(EllipseCommandType ellipseCommandType)
        {
            string type;

            switch (ellipseCommandType)
            {
                case EllipseCommandType.Axis:
                    type = RedDogGlobals.GS_Axis;
                    break;

                default:
                case EllipseCommandType.Box:
                    type = RedDogGlobals.GS_Box;
                    break;

                case EllipseCommandType.Center:
                    type = RedDogGlobals.GS_CenterSize;
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
                case RedDogGlobals.GS_Box:
                    _majorBox.IsEnabled = false;
                    _ratioBox.IsEnabled = false;
                    _axisAngleBox.IsEnabled = false;
                    _startBox.IsEnabled = true;
                    _includedBox.IsEnabled = true;
                    break;

                case RedDogGlobals.GS_Axis:
                    _majorBox.IsEnabled = false;
                    _ratioBox.IsEnabled = true;
                    _axisAngleBox.IsEnabled = false;
                    _startBox.IsEnabled = true;
                    _includedBox.IsEnabled = true;
                    break;

                case RedDogGlobals.GS_CenterSize:
                    _majorBox.IsEnabled = true;
                    _ratioBox.IsEnabled = true;
                    _axisAngleBox.IsEnabled = true;
                    _startBox.IsEnabled = true;
                    _includedBox.IsEnabled = true;
                    break;
            }

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
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "ellipse" }, { "source", "help" } });

            _ttDrawEllipseIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "ellipse" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttDrawEllipseBox.IsOpen = true;
                        break;

                    case "box":
                        _ttDrawEllipseAxis.IsOpen = true;
                        break;

                    case "axis":
                        _ttDrawEllipseCenterSize.IsOpen = true;
                        break;

                    case "centersize":
                        _ttDrawEllipseMajorAxis.IsOpen = true;
                        break;

                    case "major":
                        _ttDrawEllipseRatio.IsOpen = true;
                        break;

                    case "ratio":
                        _ttDrawEllipseAxisAngle.IsOpen = true;
                        break;

                    case "axisangle":
                        _ttDrawEllipseStartAngle.IsOpen = true;
                        break;

                    case "start":
                        _ttDrawEllipseIncludedAngle.IsOpen = true;
                        break;

                    case "included":
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
