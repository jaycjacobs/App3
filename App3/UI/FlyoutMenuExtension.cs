using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CirrosUI.Symbols
{
    public static class FlyoutMenuExtension
    {
        public static List<MenuFlyoutItemBase> GetFlyoutItems(DependencyObject obj)
        {
            return (List<MenuFlyoutItemBase>)obj.GetValue(FlyoutItemsProperty);
        }

        public static void SetFlyoutItems(DependencyObject obj, List<MenuFlyoutItemBase> value)
        {
            obj.SetValue(FlyoutItemsProperty, value);
        }

        public static readonly DependencyProperty FlyoutItemsProperty =
            DependencyProperty.Register("FlyoutItems",
                typeof(List<MenuFlyoutItemBase>),
                typeof(FlyoutMenuExtension),
                new PropertyMetadata(new List<MenuFlyoutItemBase>(), (sender, e) => {
                    var menu = sender as MenuFlyout;
                    menu.Items.Clear();
                    foreach (var item in e.NewValue as List<MenuFlyoutItemBase>)
                    {
                        menu.Items.Add(item);
                    }
                }));
    }
}
