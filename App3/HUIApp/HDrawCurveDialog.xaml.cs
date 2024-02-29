using Cirros;
using HUI;
using Microsoft.UI.Xaml.Controls;
using RedDog;
using RedDog.Console;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace RedDog.HUIApp
{
    public sealed partial class HDrawCurveDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_CurveCommand } };

        public HDrawCurveDialog()
        {
            this.InitializeComponent();

            this.Loaded += HDrawDialog_Loaded;
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public void WillClose()
        {
            _attributesControl.ShouldClose();
        }

        void HDrawDialog_Loaded(object sender, RoutedEventArgs e)
        {
            _attributesControl.AttributeTeachingTip.ActionButtonContent = null;

            Populate();

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        public string Id
        {
            get { return RedDogGlobals.GS_CurveCommand; }
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

        public void Populate()
        {
            _attributesControl.SetLayer(Globals.ActiveCurveLayerId);
            _attributesControl.SetColorSpec(Globals.CurveColorSpec);
            _attributesControl.SetLineTypeId(Globals.CurveLineTypeId);
            _attributesControl.SetLineWeightId(Globals.CurveLineWeightId);
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "curve" }, { "source", "help" } });

            _ttDrawCurveIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "curve" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _attributesControl.AttributeTeachingTip.IsOpen = true;
                        break;
                }
            }
        }
    }
}
