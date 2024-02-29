using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;


namespace Cirros.Alerts
{
    public sealed partial class AlertBox : UserControl
    {
        public AlertBox()
        {
            this.InitializeComponent();

            this.PointerPressed += AlertBox_PointerPressed;
            this.KeyDown += AlertBox_KeyDown;
        }

        void AlertBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;
            }
        }

        void AlertBox_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;
            }
        }

        public string AlertText
        {
            get
            {
                return _alertBox.Text;
            }
            set
            {
                _alertBox.Text = value;
            }
        }
    }
}
