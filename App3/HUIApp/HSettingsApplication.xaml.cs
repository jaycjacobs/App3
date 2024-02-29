using Cirros;
using HUI;
using Microsoft.UI.Xaml.Controls;
using RedDog;
using RedDog.Console;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace RedDog.HUIApp
{
    public sealed partial class HSettingsApplication : UserControl, HUIIDialog
    {
        public string Id
        {
            get { return RedDogGlobals.GS_SettingsApplicationCommand; }
        }

        public Dictionary<string, object> Options
        {
            get { return null; }
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public HSettingsApplication()
        {
            this.InitializeComponent();

            this.Loaded += HSettingsApplication_Loaded;
        }

        private void HSettingsApplication_Loaded(object sender, RoutedEventArgs e)
        {
            Update();

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        void Update()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            _versionTextBlock.Text = string.Format("{0}.{1}.{2}.{3}", 
                packageId.Version.Major, packageId.Version.Minor, packageId.Version.Build, packageId.Version.Revision);

            switch (Globals.ActiveDrawing.LineEndCap)
            {
                case PenLineCap.Flat:
                    _eolShapeComboBox.SelectedIndex = 0;
                    break;
                case PenLineCap.Round:
                    _eolShapeComboBox.SelectedIndex = 1;
                    break;
                case PenLineCap.Square:
                    _eolShapeComboBox.SelectedIndex = 2;
                    break;
                case PenLineCap.Triangle:
                    _eolShapeComboBox.SelectedIndex = 3;
                    break;
            }

            switch (Globals.MousePanButton)
            {
                case Globals.MouseButtonType.Middle:
                    _mousePanButtonComboBox.SelectedIndex = 0;
                    break;
                case Globals.MouseButtonType.Right:
                    _mousePanButtonComboBox.SelectedIndex = 1;
                    break;
                case Globals.MouseButtonType.Button1:
                    _mousePanButtonComboBox.SelectedIndex = 2;
                    break;
                case Globals.MouseButtonType.Button2:
                    _mousePanButtonComboBox.SelectedIndex = 3;
                    break;
            }

            if (Globals.Input.CursorSize > 50)
            {
                _cursorTypeComboBox.SelectedIndex = 1;
            }
            else if (Globals.Input.CursorSize > 25)
            {
                _cursorTypeComboBox.SelectedIndex = 2;
            }
            else if (Globals.Input.CursorSize > 0)
            {
                _cursorTypeComboBox.SelectedIndex = 3;
            }
            else
            {
                _cursorTypeComboBox.SelectedIndex = 0;
            }

            _pinchZoomCB.IsChecked = Globals.EnablePinchZoom;
            _touchMagnifierCB.IsChecked = Globals.EnableTouchMagnifer;
            _stylusMagnifierCB.IsChecked = Globals.EnableStylusMagnifer;
        }

        public void Populate()
        {
        }

        public void WillClose()
        {
            bool needRedraw = false;

            PenLineCap pencap = Globals.ActiveDrawing.LineEndCap;
            switch (_eolShapeComboBox.SelectedIndex)
            {
                case 0:
                    pencap = PenLineCap.Flat;
                    break;
                case 1:
                    pencap = PenLineCap.Round;
                    break;
                case 2:
                    pencap = PenLineCap.Square;
                    break;
                case 3:
                    pencap = PenLineCap.Triangle;
                    break;
            }

            if (Globals.ActiveDrawing.LineEndCap != pencap)
            {
                Globals.ActiveDrawing.LineEndCap = pencap;
                needRedraw = true;
            }

            switch (_mousePanButtonComboBox.SelectedIndex)
            {
                case 0:
                    Globals.MousePanButton = Globals.MouseButtonType.Middle;
                    break;
                case 1:
                    Globals.MousePanButton = Globals.MouseButtonType.Right;
                    break;
                case 2:
                    Globals.MousePanButton = Globals.MouseButtonType.Button1;
                    break;
                case 3:
                    Globals.MousePanButton = Globals.MouseButtonType.Button2;
                    break;
            }

            Globals.EnablePinchZoom = (bool)_pinchZoomCB.IsChecked;
            Globals.EnableTouchMagnifer = (bool)_touchMagnifierCB.IsChecked;
            Globals.EnableStylusMagnifer = (bool)_stylusMagnifierCB.IsChecked;

            if (_cursorTypeComboBox.SelectedItem is TextBlock tb && tb.Tag is string s)
            {
                if (double.TryParse(s, out double size))
                {
                    Globals.Input.CursorSize = size;
                }
            }

            if (needRedraw)
            {
                Globals.View.Regenerate();
                Globals.ActiveDrawing.IsModified = true;
                Globals.ActiveDrawing.ChangeNumber++;
            }
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-application" }, { "source", "help" } });

            _ttSettingsApplicationIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-application" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttSettingsApplicationVersion.IsOpen = true;
                        break;

                    case "version":
                        _ttSettingsApplicationCursor.IsOpen = true;
                        break;

                    case "cursor":
                        _ttSettingsApplicationMouse.IsOpen = true;
                        break;

                    case "mouse":
                        _ttSettingsApplicationLineCap.IsOpen = true;
                        break;

                    case "cap":
                        _ttSettingsApplicationTouch.IsOpen = true;
                        break;

                    case "touch":
                        _ttSettingsApplicationStylus.IsOpen = true;
                        break;

                    case "stylus":
                        _ttSettingsApplicationPinchZoom.IsOpen = true;
                        break;
                }
            }
        }
    }
}
