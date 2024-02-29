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
    public sealed partial class HDrawCircleDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_CircleCommand } };
        HXAMLControl _selectedIcon = null;

        public HDrawCircleDialog()
        {
            this.InitializeComponent();

            this.Loaded += HDrawCircleDialog_Loaded;
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public void WillClose()
        {
            _attributesControl.ShouldClose();
        }

        void HDrawCircleDialog_Loaded(object sender, RoutedEventArgs e)
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

            _attributesControl.AttributeTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        public string Id
        {
            get { return RedDogGlobals.GS_CircleCommand; }
        }

        public void Populate()
        {
            SetConstructionType(Globals.CircleCommandType);
            _radiusBox.Value = Globals.ActiveDrawing.PaperToModel(Globals.CircleRadius);

            _attributesControl.SetLayer(Globals.ActiveCircleLayerId);
            _attributesControl.SetColorSpec(Globals.CircleColorSpec);
            _attributesControl.SetLineTypeId(Globals.CircleLineTypeId);
            _attributesControl.SetLineWeightId(Globals.CircleLineWeightId);
            _attributesControl.SetFill(Globals.CircleFill);
            _attributesControl.SetPattern(Globals.CirclePattern, Globals.CirclePatternScale, Globals.CirclePatternAngle);
        }

        void SetConstructionType(ArcCommandType arcCommandType)
        {
            string construction;

            switch (arcCommandType)
            {
                case ArcCommandType.CenterStartEnd:
                    construction = RedDogGlobals.GS_Center;
                    break;

                case ArcCommandType.ThreePoint:
                    construction = RedDogGlobals.GS_3Point;
                    break;

                case ArcCommandType.CenterRadiusStartEnd:
                default:
                    construction = RedDogGlobals.GS_Radius;
                    break;
            }

            HXAMLControl control = null;

            foreach (FrameworkElement fe in _iconRow1.Children)
            {
                if (fe is HXAMLControl)
                {
                    if (((HXAMLControl)fe).Id == construction)
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
                case RedDogGlobals.GS_Center:
                case RedDogGlobals.GS_3Point:
                    _radiusBox.IsEnabled = false;
                    break;

                case RedDogGlobals.GS_Radius:
                    _radiusBox.IsEnabled = true;
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
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "circle" }, { "source", "help" } });

            _ttDrawCircleIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "circle" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttDrawCircleCenterPoint.IsOpen = true;
                        break;

                    case "centerpoint":
                        _ttDrawCircleThreePoint.IsOpen = true;
                        break;

                    case "threepoint":
                        _ttDrawCircleCenterRadius.IsOpen = true;
                        break;

                    case "centerradius":
                        _ttDrawCircleRadius.IsOpen = true;
                        break;

                    case "radius":
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
