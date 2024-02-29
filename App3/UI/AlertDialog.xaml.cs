using Microsoft.UI.Xaml.Controls;

namespace CirrosUI.Symbols
{
    public sealed partial class AlertDialog : ContentDialog
    {
        public AlertDialog(string title, string line1, string line2 = "")
        {
            this.InitializeComponent();

            Title = title;
            _line1Text.Text = line1;
            _line2Text.Text = line2;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
