using Cirros;
using EmailValidation;
using HUI;
using Microsoft.UI.Xaml.Controls;
using RedDog;
using RedDog.Console;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Windows.ApplicationModel;
using Windows.Data.Json;
using Microsoft.Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace RedDog.HUIApp
{
    public sealed partial class HSettingsPrivacySupport : UserControl, HUIIDialog
    {
        public string Id
        {
            get { return RedDogGlobals.GS_SettingsSupportCommand; }
        }

        public Dictionary<string, object> Options
        {
            get { return null; }
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public HSettingsPrivacySupport()
        {
            this.InitializeComponent();

            this.Loaded += HSettingsPrivacySupport_Loaded;
        }

        private void HSettingsPrivacySupport_Loaded(object sender, RoutedEventArgs e)
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            _versionTextBlock.Text = string.Format("{0}.{1}.{2}.{3}",
                packageId.Version.Major, packageId.Version.Minor, packageId.Version.Build, packageId.Version.Revision);

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton button && button.Tag is string s)
            {
                if (s == "privacy")
                {
                    await Launcher.LaunchUriAsync(new Uri("http://backtothedrawingboard.app/privacy/"));
                }
                else if (s == "help")
                {
                    await Launcher.LaunchUriAsync(new Uri("http://backtothedrawingboard.app/help/"));
                }
                else if (s == "tutorials")
                {
                    await Launcher.LaunchUriAsync(new Uri("http://backtothedrawingboard.app/tutorials/"));
                }
                else if (s == "rate")
                {
                    await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9wzdncrfj861"));
                }
            }
        }

        private async void _submitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_nameBox.Text.Length == 0)
            {
                //ShowAlert("name", "Please enter your name", "Ok");
                _needNameTeachingTip.IsOpen = true;
                _nameBox.Focus(FocusState.Programmatic);
            }
            else if (_emailBox.Text.Length == 0)
            {
                //ShowAlert("email", "Please enter your email address", "Ok");
                _needEmailTeachingTip.IsOpen = true;
                _emailBox.Focus(FocusState.Programmatic);
            }
            else if (ValidateEmail(_emailBox.Text) == false)
            {
                //ShowAlert("email", "Please enter a valid email address", "Ok");
                _invalidEmailTeachingTip.IsOpen = true;
                _emailBox.Focus(FocusState.Programmatic);
            }
            else if (_subjectBox.Text.Length == 0)
            {
                //ShowAlert("subject", "Please enter a subject", "Ok");
                _needSubjectTeachingTip.IsOpen = true;
                _subjectBox.Focus(FocusState.Programmatic);
            }
            else if (_reportComboBox.SelectedItem is ComboBoxItem item && item.Tag is string reportType)
            {
                if (_contentBox.Text.Length == 0)
                {
                    //ShowAlert("content", "Please enter your comments", "Ok");
                    if (reportType == "bug")
                    {
                        _needBugContentTeachingTip.IsOpen = true;
                    }
                    else
                    {
                        _needContentTeachingTip.IsOpen = true;
                    }
                    _contentBox.Focus(FocusState.Programmatic);
                }
                else
                {
                    try
                    {
                        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(_contentBox.Text);
                        string body = System.Convert.ToBase64String(plainTextBytes);

                        // Construct the HttpClient and Uri. This endpoint is for test purposes only.
                        HttpClient httpClient = new HttpClient();
                        //Uri uri = new Uri("https://backtothedrawingboard.app/json/report");
                        Uri uri = new Uri("http://localhost:16935//json/report");
                        //Uri uri = new Uri("https://webhook.site/21ffa873-2e99-4943-9b26-2b79b3b8f747");

                        string s1 = DateTime.Now.ToFileTime().ToString();
                        string s2 = string.Format("{0:0000}", body.Length);

                        JsonArray jsonArray = new JsonArray();
                        jsonArray.Add(JsonValue.CreateStringValue(_nameBox.Text));
                        jsonArray.Add(JsonValue.CreateStringValue(_emailBox.Text));
                        jsonArray.Add(JsonValue.CreateStringValue(_subjectBox.Text));
                        jsonArray.Add(JsonValue.CreateStringValue(s1 + s2));
                        jsonArray.Add(JsonValue.CreateStringValue(body));
                        jsonArray.Add(JsonValue.CreateStringValue(_versionTextBlock.Text));
                        jsonArray.Add(JsonValue.CreateStringValue(reportType));
                        string jsonString = jsonArray.Stringify();


                        // Construct the JSON to post.
                        StringContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                        // Post the JSON and wait for a response.
                        HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(uri, content);

                        // Make sure the post succeeded, and write out the response.
                        httpResponseMessage.EnsureSuccessStatusCode();
                        var httpResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private bool ValidateEmail(string email)
        {
            return EmailValidator.Validate(_emailBox.Text);
        }

        private void _emailBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                bool good = ValidateEmail(_emailBox.Text);
            }
        }

        private void _emailBox_LostFocus(object sender, RoutedEventArgs e)
        {
            bool good = ValidateEmail(_emailBox.Text);
        }

        private void _alertOkButton_Click(object sender, RoutedEventArgs e)
        {
            DismissAlert();
        }

        private void DismissAlert()
        {
            if (_alert.Visibility == Visibility.Visible)
            {
                _alert.Visibility = Visibility.Collapsed;

                if (_alertOkButton.Tag is string tag)
                {
                    if (tag == "name")
                    {
                        _nameBox.Focus(FocusState.Programmatic);
                    }
                    else if (tag == "email")
                    {
                        _emailBox.Focus(FocusState.Programmatic);
                    }
                    else if (tag == "subject")
                    {
                        _subjectBox.Focus(FocusState.Programmatic);
                    }
                    else if (tag == "content")
                    {
                        _contentBox.Focus(FocusState.Programmatic);
                    }
                }
            }
        }

        private void _alert_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            DismissAlert();
        }

        public void Populate()
        {
        }

        public void WillClose()
        {
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-privacy-support" }, { "source", "help" } });

            _ttSettingsPrivacySupport.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-privacy-support" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                    //    _ttSettingsLayersName.IsOpen = true;
                    //    break;

                    //case "name":
                    //    _ttSettingsLayersRename.IsOpen = true;
                    //    break;

                    //case "rename":
                    //    _ttSettingsLayersSample.IsOpen = true;
                    //    break;

                    //case "sample":
                    //    _ttSettingsLayersColor.IsOpen = true;
                    //    break;

                    //case "color":
                    //    _ttSettingsLayersLineType.IsOpen = true;
                    //    break;

                    //case "ltype":
                    //    _ttSettingsLayersLineThickness.IsOpen = true;
                    //    break;

                    //case "thickness":
                    //    _ttSettingsLayersCount.IsOpen = true;
                    //    break;

                    //case "count":
                    //    _ttSettingsLayersVisible.IsOpen = true;
                    //    break;

                    //case "visible":
                    //    _ttSettingsLayersAdd.IsOpen = true;
                    //    break;

                    //case "add":
                    //    _ttSettingsLayersDelete.IsOpen = true;
                        break;
                }
            }
        }
    }
}
