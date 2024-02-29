using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HUI
{
    public interface HUIIDialog
    {
        string Id { get; }
        Dictionary<string, object> Options { get; }
        void Populate();
        void WillClose();
        FrameworkElement HelpButton { get; }
    }

    public sealed partial class HUIDialog : UserControl, HUIIDialog
    {
        public HUIDialog(string title)
        {
            this.InitializeComponent();

            _titleTextBlock.Text = title;

            this.Loaded += HUIDialog_Loaded;
        }

        private void HUIDialog_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
        }

        public string Id
        {
            get { return null; }
        }

        public Dictionary<string, object> Options
        {
            get { return null; }
        }

        public FrameworkElement HelpButton
        {
            get { return null; }
        }

        public void Populate()
        {
        }

        public void WillClose()
        {
        }
    }
}
