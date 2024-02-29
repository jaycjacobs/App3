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
    public sealed partial class HDrawLineDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_LineCommand } };
        HXAMLControl _selectedIcon = null;

        public HDrawLineDialog()
        {
            this.InitializeComponent();

            this.Loaded += HDrawLineDialog_Loaded;
        }

        void HDrawLineDialog_Loaded(object sender, RoutedEventArgs e)
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

            _attributesControl.AttributeTeachingTip.ActionButtonContent = null;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public string Id
        {
            get { return RedDogGlobals.GS_LineCommand; }
        }

        public void Populate()
        {
            SetConstructionType(Globals.LineCommandType);
            _radiusBox.Value = Globals.ActiveDrawing.PaperToModel(Globals.FilletRadius);

            _attributesControl.SetLayer(Globals.ActiveLineLayerId);
            _attributesControl.SetColorSpec(Globals.LineColorSpec);
            _attributesControl.SetLineTypeId(Globals.LineLineTypeId);
            _attributesControl.SetLineWeightId(Globals.LineLineWeightId);
        }

        void SetConstructionType(LineCommandType lineCommandType)
        {
            string construction;

            switch (lineCommandType)
            {
                case LineCommandType.Single:
                    construction = RedDogGlobals.GS_SingleSegment;
                    break;

                default:
                case LineCommandType.Multi:
                    construction = RedDogGlobals.GS_MultiSegment;
                    break;

                case LineCommandType.Fillet:
                    construction = RedDogGlobals.GS_Fillet;
                    break;

                case LineCommandType.Freehand:
                    construction = RedDogGlobals.GS_Freehand;
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
                case RedDogGlobals.GS_Freehand:
                case RedDogGlobals.GS_MultiSegment:
                case RedDogGlobals.GS_SingleSegment:
                    _radiusBox.IsEnabled = false;
                    break;

                case RedDogGlobals.GS_Fillet:
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

        public void WillClose()
        {
            _attributesControl.ShouldClose();
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "line" }, { "source", "help" } });

            _ttDrawLineIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "line" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttDrawLineSingleSegment.IsOpen = true;
                        break;

                    case "single":
                        _ttDrawLineMultiSegment.IsOpen = true;
                        break;

                    case "multi":
                        _ttDrawLineFillet.IsOpen = true;
                        break;

                    case "fillet":
                        _ttDrawLineFreehand.IsOpen = true;
                        break;

                    case "freehand":
                        _ttDrawLineFilletRadius.IsOpen = true;
                        break;

                    case "radius":
                        _attributesControl.AttributeTeachingTip.IsOpen = true;
                        break;
                }
            }
        }
    }
}