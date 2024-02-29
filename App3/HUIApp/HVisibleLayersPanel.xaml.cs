using Cirros;
using Cirros.Drawing;
using Cirros.Primitives;
//using SharpDX.Direct2D1;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace RedDog.HUIApp
{
    public sealed partial class HVisibleLayersPanel : UserControl
    {
        public HVisibleLayersPanel()
        {
            this.InitializeComponent();

            this.Loaded += HVisibleLayersPanel_Loaded;
            this.SizeChanged += HVisibleLayersPanel_SizeChanged;
        }

        private void HVisibleLayersPanel_Loaded(object sender, RoutedEventArgs e)
        {
            this.MaxHeight = 600;
            UpdateMenu();
        }

        CheckBox _allCheckBox = new CheckBox();

        private void UpdateMenu()
        {
            _layerListView.Items.Clear();

            _allCheckBox.Content = "All layers";
            _allCheckBox.IsChecked = null;
            _allCheckBox.FontStyle = Windows.UI.Text.FontStyle.Italic;
            _allCheckBox.Tag = "all";
            _allCheckBox.Checked += Cb_Checked;
            _allCheckBox.Unchecked += Cb_Checked;
            _layerListView.Items.Add(_allCheckBox);

            foreach (Layer layer in Globals.LayerTable.Values)
            {
                CheckBox cb = new CheckBox();
                cb.Content = layer.Name;
                cb.IsChecked = layer.Visible;
                cb.Tag = layer;
                cb.Checked += Cb_Checked;
                cb.Unchecked += Cb_Checked;
                _layerListView.Items.Add(cb);
            }
        }

        static bool _cbHandlerIsBusy = false;

        private void Cb_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                if (cb.Tag is Layer layer)
                {
                    layer.Visible = (bool)cb.IsChecked;
                    Layer.PropagateLayerChanges(layer.Id);

                    if (cb.IsChecked == false && _cbHandlerIsBusy == false)
                    {
                        _allCheckBox.IsChecked = null;
                    }
                }
                else if (cb.Tag is string s && s == "all" && cb.IsChecked != null)
                {
                    _cbHandlerIsBusy = true;

                    //foreach (Layer l in Globals.LayerTable.Values)
                    //{
                    //    l.Visible = (bool)cb.IsChecked;
                    //}
                    bool flag = (bool)cb.IsChecked;

                    foreach (object o in _layerListView.Items)
                    {
                        if (o is CheckBox cb1 && cb1.IsChecked != flag && cb1.Tag is Layer l1)
                        {
                            cb1.IsChecked = flag;
                        }
                    }

                    //foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
                    //{
                    //    p.Draw();
                    //}
                    _cbHandlerIsBusy = false;
                }

            }
        }

        private void HVisibleLayersPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Parent is Popup popup)
            {
                popup.Height = e.NewSize.Height;
            }
        }
    }
}
