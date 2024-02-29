using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cirros.Core.Alerts
{
    public sealed partial class SupportRequest : UserControl
    {
        public SupportRequest(Dictionary<string, string> data)
        {
            this.InitializeComponent();

            _data = data;
        }

        Dictionary<string, string> _data;

        private void SaveSharedEmailState(bool shared)
        {
            ApplicationDataContainer exceptionSettings = null;
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Containers.ContainsKey("exception"))
            {
                exceptionSettings = localSettings.Containers["exception"];
                exceptionSettings.Values["shared-email"] = shared ? "yes" : "no";
            }
        }

        private void _sendButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSharedEmailState(true);

            _data.Add("sharedemail", "yes");
            _data.Add("email", _emailBox.Text);

            if (_nameBox.Text.Length > 0)
            {
                _data.Add("name", _nameBox.Text);
            }

            Analytics.ReportEvent("SupportRequest", _data);

            if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;
            }
        }

        private void _ignoreButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSharedEmailState(true);

            _data.Add("sharedemail", "no");

            Analytics.ReportEvent("SupportRequest", _data);

            if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;
            }
        }

        private void _emailBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_emailBox.Text.Length < 5)
            {
                _sendButton.IsEnabled = false;
            }
            else
            {
                bool valid = false;

                string expr = @"(?<email>\w+@\w+\.\w+)";
                MatchCollection mc = Regex.Matches(_emailBox.Text, expr);
                if (mc.Count > 0)
                {
                    if (mc[0].Groups["email"].Success)
                    {
                        valid = true;
                    }
                }
                _sendButton.IsEnabled = valid;
            }
        }
    }
}
