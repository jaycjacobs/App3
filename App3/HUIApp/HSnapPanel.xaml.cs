using Cirros;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace RedDog.HUIApp
{
    public sealed partial class HSnapPanel : UserControl
    {
        public HSnapPanel()
        {
            this.InitializeComponent();

            this.Loaded += HSnapPanel_Loaded;
            this.Unloaded += HSnapPanel_Unloaded;
            this.SizeChanged += HSnapPanel_SizeChanged;
        }

        private void HSnapPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Globals.Input.GridSnap = (bool)_gridSnapCheckBox.IsChecked;
            Globals.Input.ObjectSnap = (bool)_objectSnapCheckBox.IsChecked;

            Globals.Events.GridChanged();

            if ((bool)_halfGrid.IsChecked)
            {
                Globals.Input.GridSnapMode = Cirros.Core.GridSnapMode.halfGrid;
            }
            else if ((bool)_wholeGrid.IsChecked)
            {
                Globals.Input.GridSnapMode = Cirros.Core.GridSnapMode.wholeGrid;
            }
            else
            {
                Globals.Input.GridSnapMode = Cirros.Core.GridSnapMode.auto;
            }
        }

        private void HSnapPanel_Loaded(object sender, RoutedEventArgs e)
        {
            _gridSnapCheckBox.IsChecked = Globals.Input.GridSnap;
            _objectSnapCheckBox.IsChecked = Globals.Input.ObjectSnap;

            _halfGrid.IsChecked = false;
            _wholeGrid.IsChecked = false;
            _autoGrid.IsChecked = true;

            switch (Globals.Input.GridSnapMode)
            {
                case Cirros.Core.GridSnapMode.wholeGrid:
                    _halfGrid.IsChecked = false;
                    _wholeGrid.IsChecked = true;
                    _autoGrid.IsChecked = false;
                    break;

                case Cirros.Core.GridSnapMode.halfGrid:
                    _halfGrid.IsChecked = true;
                    _wholeGrid.IsChecked = false;
                    _autoGrid.IsChecked = false;
                    break;

                default:
                case Cirros.Core.GridSnapMode.auto:
                    _halfGrid.IsChecked = false;
                    _wholeGrid.IsChecked = false;
                    _autoGrid.IsChecked = true;
                    break;
            }

            EnableGridSnapOptions();
        }

        private void HSnapPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Parent is Popup popup)
            {
                popup.Height = e.NewSize.Height;
            }
        }

        private void _gridSnapCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Globals.Input.GridSnap = (bool)_gridSnapCheckBox.IsChecked;
            EnableGridSnapOptions();
        }

        private void _objectSnapCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Globals.Input.ObjectSnap = (bool)_objectSnapCheckBox.IsChecked;
        }

        private void _wholeGrid_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void _halfGrid_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void _autoGrid_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void EnableGridSnapOptions()
        {
            _autoGrid.IsEnabled = Globals.Input.GridSnap;
            _halfGrid.IsEnabled = Globals.Input.GridSnap;
            _wholeGrid.IsEnabled = Globals.Input.GridSnap;
        }
    }
}
