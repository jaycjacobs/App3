using Cirros;
using Cirros.Primitives;
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
    public sealed partial class HDrawDoublelineDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_DoublelineCommand } };

        public HDrawDoublelineDialog()
        {
            this.InitializeComponent();

            this.Loaded += HDrawDoublelineDialog_Loaded;
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public void WillClose()
        {
            _attributesControl.ShouldClose();
        }

        void HDrawDoublelineDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Populate();

            _widthBox.OnValueChanged += numberBox_OnValueChanged;
            _capComboBox.SelectionChanged += _capComboBox_SelectionChanged;

            _attributesControl.AttributeTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        void _capComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_capComboBox.SelectedItem is ComboBoxItem && ((ComboBoxItem)_capComboBox.SelectedItem).Tag is string)
            {
                SetOption(RedDogGlobals.GS_Cap, (string)((ComboBoxItem)_capComboBox.SelectedItem).Tag);
            }
        }

        public string Id
        {
            get { return RedDogGlobals.GS_DoublelineCommand; }
        }

        public void Populate()
        {
            _widthBox.Value = Globals.ActiveDrawing.PaperToModel(Globals.DoubleLineWidth);
            SetCap(Globals.DoublelineEndStyle);

            _attributesControl.SetLayer(Globals.ActiveDoubleLineLayerId);
            _attributesControl.SetColorSpec(Globals.DoubleLineColorSpec);
            _attributesControl.SetLineTypeId(Globals.DoubleLineLineTypeId);
            _attributesControl.SetLineWeightId(Globals.DoubleLineLineWeightId);
            _attributesControl.SetFill(Globals.DoublelineFill);
            _attributesControl.SetPattern(Globals.DoublelinePattern, Globals.DoublelinePatternScale, Globals.DoublelinePatternAngle);
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

        private void SetCap(DbEndStyle doublelineEndStyle)
        {
            string cap;

            switch(doublelineEndStyle)
            {
                case DbEndStyle.Both:
                    cap = RedDogGlobals.GS_Both;
                    break;

                case DbEndStyle.Start:
                    cap = RedDogGlobals.GS_Start;
                    break;

                case DbEndStyle.End:
                    cap = RedDogGlobals.GS_End;
                    break;

                default:
                case DbEndStyle.None:
                    cap = RedDogGlobals.GS_None;
                    break;
            }

            for (int i = 0; i < _capComboBox.Items.Count; i++)
            {
                if (_capComboBox.Items[i] is ComboBoxItem && ((ComboBoxItem)_capComboBox.Items[i]).Tag is string)
                {
                    if ((string)((ComboBoxItem)_capComboBox.Items[i]).Tag == cap)
                    {
                        _capComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "double-line" }, { "source", "help" } });

            _ttDrawDoublelineIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "double-line" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttDrawDoublelineWidth.IsOpen = true;
                        break;

                    case "width":
                        _ttDrawDoublelineEnds.IsOpen = true;
                        break;

                    case "ends":
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
