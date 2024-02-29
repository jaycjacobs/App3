using KT22;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using App3;
using Microsoft.UI.Xaml.Navigation;

namespace Cirros8
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewDrawingPage : Page
    {
        Dictionary<string, object> _imageParameters = null;

        public NewDrawingPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Dictionary<string, object>)
            {
                _imageParameters = e.Parameter as Dictionary<string, object>;
                _newDrawingControl.SetBaseImageFromDictionary(_imageParameters);
            }
        }

        private void _backButton_Click(object sender, RoutedEventArgs e)
        {
            if (_imageParameters == null)
            {
                App.Navigate(typeof(KTHomePage), null);
            }
        }
    }
}
