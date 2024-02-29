using Cirros;
using Cirros.Drawing;
using RedDog;
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
    public sealed partial class HLayerPanel : UserControl
    {
        public HLayerPanel()
        {
            this.InitializeComponent();

            _layerListView.Items.Clear();

            foreach (Layer layer in Globals.LayerTable.Values)
            {
                HLayerTile tile = new HLayerTile(layer);
                _layerListView.Items.Add(tile);
            }

            this.Loaded += HLayerPanel_Loaded;

            _layerListView.SizeChanged += _layerListView_SizeChanged;
            _layerListView.SelectionChanged += _layerListView_SelectionChanged;
        }

        private void _layerListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Parent is Popup popup && _layerListView.SelectedItem is HLayerTile tile)
            {
                Globals.ActiveLayerId = tile.Layer.Id;
                Globals.Events.LayerSelectionChanged();
                popup.IsOpen = false;
            }
        }

        private void _layerListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Parent is Popup popup)
            {
                popup.Height = e.NewSize.Height + 2;
            }
        }

        private void HLayerPanel_Loaded(object sender, RoutedEventArgs e)
        {
            //if (Parent is Popup popup && double.IsNaN(this.ActualHeight) == false)
            //{
            //    popup.Height = this.ActualHeight;
            //}
        }

        private void _layerMenuGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //if (Parent is Popup popup)
            //{
            //    popup.Height = e.NewSize.Height;
            //}
        }
    }
}
