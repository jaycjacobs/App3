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

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace CirrosUI.Symbols
{
    public sealed partial class ConfirmSymbolDeleteDialog : ContentDialog
    {
        public ConfirmSymbolDeleteDialog()
        {
            this.InitializeComponent();
        }

        private string _symbolName = "this symbol";

        public string SymbolName
        {
            set
            {
                _symbolName = value;
                _promptText.Text = $"Are you sure you want to delete '{_symbolName}' from the symbol library?";
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
