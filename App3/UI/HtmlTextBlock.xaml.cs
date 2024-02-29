using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
#if WINDOWS_UWP
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
#endif


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace KT22.UI
{
    public sealed partial class HtmlTextBlock : UserControl
    {
        public HtmlTextBlock()
        {
            this.InitializeComponent();

            this.Loaded += HtmlTextBlock_Loaded;
        }

        private void HtmlTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            _tb.FontFamily = this.FontFamily;
        }

        public string Html 
        {
            get
            {
                return _tb.Text;
            }
            set
            {
                _tb.Text = Format(value);
            }
        }


        private string Format(string s)
        {
            s = s.Replace("\r", "");
            s = s.Replace("</p><p/><p>", "\r\r");
            s = s.Replace("<p>", "");
            s = s.Replace("</p>", "");
            s = s.Replace("<p />", "");

            return s;
        }
    }
}
