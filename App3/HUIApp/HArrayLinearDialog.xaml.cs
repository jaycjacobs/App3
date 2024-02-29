using Cirros;
using Cirros.Commands;
using Cirros.Core;
using Cirros.Primitives;
using CirrosUI;
using HUI;
using Microsoft.UI.Xaml.Controls;
using RedDog.Console;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace RedDog.HUIApp
{
    public sealed partial class HArrayLinearDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_InsertGroupLinearCommand } };

        public HArrayLinearDialog()
        {
            this.InitializeComponent();

            this.Loaded += HArrayLinearDialog_Loaded;
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public void WillClose()
        {
            _groupPanel.ShouldClose();
        }

        void HArrayLinearDialog_Loaded(object sender, RoutedEventArgs e)
        {
            _groupPanel.OnHGroupPanelOptionChanged += _groupPanel_OnHGroupPanelOptionChanged;

            Populate();

            _spaceButton.Checked += _spaceButton_Checked;
            _distributeButton.Checked += _distributeButton_Checked;

            _spacingBox.OnValueChanged += numberBox_OnValueChanged;
            _copiesBox.OnValueChanged += numberBox_OnValueChanged;

            _connectCB.Checked += checkBox_Checked;
            _connectCB.Unchecked += checkBox_Checked;
            _endsCB.Checked += checkBox_Checked;
            _endsCB.Unchecked += checkBox_Checked;

            _groupPanel.PredefinedTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;
            _groupPanel.FromFileTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;
            _groupPanel.FromLibraryTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;
            _groupPanel.FromDrawingTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;
            _groupPanel.ThumbnailTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;
            _groupPanel.LayerTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;
            _groupPanel.ScaleTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        private void _groupPanel_OnHGroupPanelOptionChanged(object sender, HGroupPanelOptionChangedEventArgs e)
        {
            SetOption(e.OptionName, e.OptionValue);
        }

        void _distributeButton_Checked(object sender, RoutedEventArgs e)
        {
            _spaceButton.IsChecked = false;
            _spacingBox.IsEnabled = false;
            _copiesBox.IsEnabled = true;

            SetOption(RedDogGlobals.GS_InsertGroupLinearMode, RedDogGlobals.GS_InsertGroupLinearModeDistribute);
        }

        void _spaceButton_Checked(object sender, RoutedEventArgs e)
        {
            _distributeButton.IsChecked = false;
            _copiesBox.IsEnabled = false;
            _spacingBox.IsEnabled = true;

            SetOption(RedDogGlobals.GS_InsertGroupLinearMode, RedDogGlobals.GS_InsertGroupLinearModeSpace);
        }

        public string Id
        {
            get { return RedDogGlobals.GS_InsertGroupLinearCommand; }
        }

        public void Populate()
        {
            _groupPanel.SetInputType(RedDogGlobals.InsertGroupLinearFrom);

            if (RedDogGlobals.InsertGroupLinearFrom == RedDogGlobals.GS_InsertGroupFromDrawing)
            {
                _groupPanel.SelectGroup(null);
                Globals.LinearCopyGroupName = null;
                Globals.LinearCopyGroupScale = 1;
            }
            else if (string.IsNullOrEmpty(Globals.LinearCopyGroupName) == false)
            {
                Group g = Globals.ActiveDrawing.GetGroup(Globals.LinearCopyGroupName);
                _groupPanel.SelectGroup(g);
                _groupPanel.SetGroupScale(Globals.LinearCopyGroupScale);
            }

            _groupPanel.SetLayer(Globals.ActiveInstanceLayerId);
            _groupPanel.SetColor(ColorCode.ByLayer);

            //if (RedDogGlobals.InsertGroupLinearMode == RedDogGlobals.GS_InsertGroupLinearModeSpace)
            if (Globals.LinearCopyRepeatType == CopyRepeatType.Space)
            {
                _spaceButton.IsChecked = true;
                _distributeButton.IsChecked = false;
                _copiesBox.IsEnabled = false;
                _spacingBox.IsEnabled = true;
            }
            //else if (RedDogGlobals.InsertGroupLinearMode == RedDogGlobals.GS_InsertGroupLinearModeDistribute)
            else if (Globals.LinearCopyRepeatType == CopyRepeatType.Distribute)
            {
                _spaceButton.IsChecked = false;
                _distributeButton.IsChecked = true;
                _spacingBox.IsEnabled = false;
                _copiesBox.IsEnabled = true;
            }

            _spacingBox.Value = Globals.LinearCopyRepeatDistance; // RedDogGlobals.InsertGroupLinearSpacing;
            _copiesBox.Value = Globals.LinearCopyRepeatCount; // RedDogGlobals.InsertGroupLinearCount;

            _connectCB.IsChecked = Globals.LinearCopyRepeatConnect; // RedDogGlobals.InsertGroupLinearConnect;
            _endsCB.IsChecked = Globals.LinearCopyRepeatAtEnd; // RedDogGlobals.InsertGroupLinearEndCopy;

            UpdatePlacementIcons();

            if (string.IsNullOrEmpty(Globals.LinearCopyGroupName) == false)
            {
                Group g = Globals.ActiveDrawing.GetGroup(Globals.LinearCopyGroupName);
                _groupPanel.SelectGroup(g);
            }

            _groupPanel.SetLayer(Globals.ActiveInstanceLayerId);
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

        void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox && ((CheckBox)sender).Tag is string)
            {
                CheckBox cb = sender as CheckBox;
                SetOption((string)cb.Tag, cb.IsChecked);
            }
        }

        public Dictionary<string, object> Options
        {
            get
            {
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

        private void UpdatePlacementIcons()
        {
            _connectIcon0.Visibility = Visibility.Collapsed;
            _connectIcon1.Visibility = Visibility.Collapsed;
            _connectIcon2.Visibility = Visibility.Collapsed;
            _connectIcon3.Visibility = Visibility.Collapsed;

            if (_connectCB.IsChecked == true)
            {
                if (_endsCB.IsChecked == true)
                {
                    _connectIcon3.Visibility = Visibility.Visible;
                }
                else
                {
                    _connectIcon2.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (_endsCB.IsChecked == true)
                {
                    _connectIcon1.Visibility = Visibility.Visible;
                }
                else
                {
                    _connectIcon0.Visibility = Visibility.Visible;
                }
            }
        }

        private void _connectCB_Checked(object sender, RoutedEventArgs e)
        {
            UpdatePlacementIcons();
        }

        private void _connectCB_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdatePlacementIcons();
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "insert-along-line" }, { "source", "help" } });

            _ttInsertAlongLineIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "insert-along-line" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _groupPanel.PredefinedTeachingTip.IsOpen = true;
                        break;

                    case "predefined":
                        _groupPanel.FromFileTeachingTip.IsOpen = true;
                        break;

                    case "file":
                        _groupPanel.FromLibraryTeachingTip.IsOpen = true;
                        break;

                    case "library":
                        _groupPanel.FromDrawingTeachingTip.IsOpen = true;
                        break;

                    case "drawing":
                        _groupPanel.ThumbnailTeachingTip.IsOpen = true;
                        break;

                    case "thumbnail":
                        _groupPanel.LayerTeachingTip.IsOpen = true;
                        break;

                    case "layer":
                        _groupPanel.ScaleTeachingTip.IsOpen = true;
                        break;

                    case "scale":
                        _ttInsertAlongLineSpace.IsOpen = true;
                        break;

                    case "space":
                        _ttInsertAlongLineDistribute.IsOpen = true;
                        break;

                    case "distribute":
                        _ttInsertAlongLineCopies.IsOpen = true;
                        break;

                    case "copies":
                        _ttInsertAlongLineSpacing.IsOpen = true;
                        break;

                    case "spacing":
                        _ttInsertAlongLineConnect.IsOpen = true;
                        break;

                    case "connect":
                        _ttInsertAlongLineEnds.IsOpen = true;
                        break;

                    case "ends":
                        break;
                }
            }
        }
    }
}
