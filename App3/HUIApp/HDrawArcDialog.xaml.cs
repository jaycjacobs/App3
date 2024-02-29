using Cirros;
using Cirros.Commands;
using Cirros.Utility;
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
    public sealed partial class HDrawArcDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_ArcCommand } };
        HXAMLControl _selectedIcon = null;

        public HDrawArcDialog()
        {
            this.InitializeComponent();

            this.Loaded += HDrawArcDialog_Loaded;
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public void WillClose()
        {
            _attributesControl.ShouldClose();
        }

        void HDrawArcDialog_Loaded(object sender, RoutedEventArgs e)
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

            _radiusBox.OnValueChanged += numberBox_OnValueChanged;
            _startBox.OnValueChanged += numberBox_OnValueChanged;
            _includedBox.OnValueChanged += numberBox_OnValueChanged;

            _attributesControl.AttributeTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        public void Populate()
        {
            SetConstructionType(Globals.ArcCommandType);

            _radiusBox.Value = Globals.ActiveDrawing.PaperToModel(Globals.ArcRadius);
            _startBox.Value = Globals.ArcStartAngle * Construct.cRadiansToDegrees;
            _includedBox.Value = Globals.ArcIncludedAngle * Construct.cRadiansToDegrees;

            _attributesControl.SetLayer(Globals.ActiveArcLayerId);
            _attributesControl.SetColorSpec(Globals.ArcColorSpec);
            _attributesControl.SetLineTypeId(Globals.ArcLineTypeId);
            _attributesControl.SetLineWeightId(Globals.ArcLineWeightId);
            _attributesControl.SetFill(Globals.ArcFill);
            _attributesControl.SetPattern(Globals.ArcPattern, Globals.ArcPatternScale, Globals.ArcPatternAngle);
        }

        public string Id
        {
            get { return RedDogGlobals.GS_ArcCommand; }
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

        void SetConstructionType(ArcCommandType arcCommandType)
        {
            string construction;

            switch (arcCommandType)
            {
                case ArcCommandType.CenterStartEnd:
                    construction = RedDogGlobals.GS_CenterStartEnd;
                    break;

                case ArcCommandType.ThreePoint:
                    construction = RedDogGlobals.GS_3Point;
                    break;

                case ArcCommandType.SemiCircle:
                    construction = RedDogGlobals.GS_SemiCircle;
                    break;

                case ArcCommandType.Fillet:
                    construction = RedDogGlobals.GS_Fillet;
                    break;

                case ArcCommandType.FilletRadius:
                    construction = RedDogGlobals.GS_FilletRadius;
                    break;

                case ArcCommandType.CenterRadiusStartEnd:
                default:
                    construction = RedDogGlobals.GS_Radius;
                    break;

                case ArcCommandType.CenterRadiusAngles:
                    construction = RedDogGlobals.GS_RadiusAngles;
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

            if (control == null)
            {
                foreach (FrameworkElement fe in _iconRow2.Children)
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
            }

            if (control != null)
            {
                SelectConstructControl(control);
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
                case RedDogGlobals.GS_CenterStartEnd:
                case RedDogGlobals.GS_3Point:
                case RedDogGlobals.GS_SemiCircle:
                case RedDogGlobals.GS_Fillet:
                    //_radiusBox.Visibility = Visibility.Collapsed;
                    //_startBox.Visibility = Visibility.Collapsed;
                    //_includedBox.Visibility = Visibility.Collapsed;
                    _radiusBox.IsEnabled = false;
                    _startBox.IsEnabled = false;
                    _includedBox.IsEnabled = false;
                    break;

                case RedDogGlobals.GS_FilletRadius:
                case RedDogGlobals.GS_Radius:
                    //_radiusBox.Visibility = Visibility.Visible;
                    //_startBox.Visibility = Visibility.Collapsed;
                    //_includedBox.Visibility = Visibility.Collapsed;
                    _radiusBox.IsEnabled = true;
                    _startBox.IsEnabled = false;
                    _includedBox.IsEnabled = false;
                    break;

                case RedDogGlobals.GS_RadiusAngles:
                    //_radiusBox.Visibility = Visibility.Visible;
                    //_startBox.Visibility = Visibility.Visible;
                    //_includedBox.Visibility = Visibility.Visible;
                    _radiusBox.IsEnabled = true;
                    _startBox.IsEnabled = true;
                    _includedBox.IsEnabled = true;
                    break;
            }

            //_radiusLabel.Visibility = _radiusBox.Visibility;
            //_startLabel.Visibility = _startBox.Visibility;
            //_includedLabel.Visibility = _includedBox.Visibility;

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
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "arc" }, { "source", "help" } });

            _ttDrawArcIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "arc" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttDrawArcCenterStartEnd.IsOpen = true;
                        break;

                    case "centerstartend":
                        _ttDrawArcThreePoint.IsOpen = true;
                        break;

                    case "threepoint":
                        _ttDrawArcSemiCircle.IsOpen = true;
                        break;

                    case "semicircle":
                        _ttDrawArcRadiusCenterPoint.IsOpen = true;
                        break;

                    case "centerradius":
                        _ttDrawArcRadiusAngles.IsOpen = true;
                        break;

                    case "radiusangles":
                        _ttDrawArcFillet.IsOpen = true;
                        break;

                    case "fillet":
                        _ttDrawArcFilletRadius.IsOpen = true;
                        break;

                    case "filletradius":
                        _ttDrawArcRadius.IsOpen = true;
                        break;

                    case "radius":
                        _ttDrawArcStartAngle.IsOpen = true;
                        break;

                    case "start":
                        _ttDrawArcIncludedAngle.IsOpen = true;
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
