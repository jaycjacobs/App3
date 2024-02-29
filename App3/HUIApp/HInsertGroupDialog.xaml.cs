using Cirros;
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
    public sealed partial class HInsertGroupDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_InsertGroupCommand } };

        public HInsertGroupDialog()
        {
            this.InitializeComponent();

            this.Loaded += HInsertGroupDialog_Loaded;
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public void WillClose()
        {
            _groupPanel.ShouldClose();
        }

        private void HInsertGroupDialog_Loaded(object sender, RoutedEventArgs e)
        {
            _groupPanel.PredefinedIcon.Visibility = Visibility.Collapsed;

            _groupPanel.OnHGroupPanelOptionChanged += _groupPanel_OnHGroupPanelOptionChanged;

            Populate();

            if (RedDogGlobals.InsertGroupFrom == RedDogGlobals.GS_InsertGroupFromDrawing)
            {
                _groupPanel.SelectGroup(null);
                Globals.GroupName = null;
                Globals.GroupScale = 1;
            }
            else  if (string.IsNullOrEmpty(Globals.GroupName) == false)
            {
                Group g = Globals.ActiveDrawing.GetGroup(Globals.GroupName);
                _groupPanel.SelectGroup(g);
                _groupPanel.SetGroupScale(Globals.GroupScale);
            }

            _groupPanel.FromFileTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;
            _groupPanel.FromLibraryTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;
            _groupPanel.FromDrawingTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;
            _groupPanel.ThumbnailTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;
            _groupPanel.LayerTeachingTip.ActionButtonClick += _teachingTip_ActionButtonClick;

            _groupPanel.ScaleTeachingTip.ActionButtonContent = null;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        private void _groupPanel_OnHGroupPanelOptionChanged(object sender, HGroupPanelOptionChangedEventArgs e)
        {
            SetOption(e.OptionName, e.OptionValue);
        }

        public string Id
        {
            get { return RedDogGlobals.GS_InsertGroupCommand; }
        }

        public void Populate()
        {
            _groupPanel.SetInputType(RedDogGlobals.InsertGroupFrom);

            _groupPanel.SetLayer(Globals.ActiveInstanceLayerId);
            _groupPanel.SetColor(ColorCode.ByLayer);
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

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "insert-group" }, { "source", "help" } });

            _ttInsertGroupIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "insert-group" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
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
                }
            }
        }
    }
}
