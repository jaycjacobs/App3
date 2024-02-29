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
    public sealed partial class HPredefinedSymbolPanel : UserControl
    {
        public HPredefinedSymbolPanel()
        {
            this.InitializeComponent();
        }

        public string SelectedSymbolName
        {
            get
            {
                string symbolName = null;

                if (_gridView.SelectedItem is Image image && image.Tag is string s)
                {
                    symbolName = s;
                }

                return symbolName;
            }
        }
        private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                while (fe.Parent is FrameworkElement parent)
                {
                    if (parent is Popup popup)
                    {
                        popup.IsOpen = false;
                        break;
                    }
                    fe = parent;
                }
            }
        }
    }
}
