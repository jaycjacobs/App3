using Cirros;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Microsoft.Windows.System;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Windows.System;
using App3;

namespace KT22
{
    class StandardAlerts8
    {
        public static async Task<string> FirstRunAlertAsync(string fromVersion = null, string toVersion = null)
        {
            //bool upgradeFromVersion2 = fromVersion != null && fromVersion.StartsWith("2.");
            bool upgradeToVersion31 = toVersion != null && toVersion.StartsWith("3.1");

            var resourceLoader = new ResourceLoader();

            string title;
            string message;

            if (upgradeToVersion31)
            {
                title = resourceLoader.GetString("FirstRunUpgradeTitle");
                message = resourceLoader.GetString("FirstRunUpgradeMessage");
            }
            else
            {
                title = resourceLoader.GetString("FirstRunTitle");
                message = resourceLoader.GetString("FirstRunMessage");
            }

            string continueString = resourceLoader.GetString("AlertContinue");

            var messageDialog = new MessageDialog(message, title);

            if (upgradeToVersion31)
            {
                string upgradeNotes = resourceLoader.GetString("FirstRunUpgradeNotes");
                messageDialog.Commands.Add(new UICommand(upgradeNotes, null, "notes"));
                messageDialog.Commands.Add(new UICommand(continueString, null, "continue"));
                messageDialog.DefaultCommandIndex = 0;  // Default command index
                messageDialog.CancelCommandIndex = 1;   // View upgrade notes index
            }
            else
            {
                messageDialog.Commands.Add(new UICommand(continueString, null, "continue"));
                messageDialog.DefaultCommandIndex = 0;  // Default command index
                messageDialog.CancelCommandIndex = 0;   // Cancel index
            }

            UICommand command = (UICommand)await messageDialog.ShowAsync();

            return command.Id as string;
        }

        public static async Task ComingSoonAlertAsync()
        {
            var resourceLoader = new ResourceLoader();

            string continueString = resourceLoader.GetString("AlertContinue");
            string title = resourceLoader.GetString("ComingSoonTitle");
            string message = resourceLoader.GetString("ComingSoonMessage");

            var messageDialog = new MessageDialog(message, title);

            messageDialog.Commands.Add(new UICommand(continueString, null, "continue"));

            messageDialog.DefaultCommandIndex = 0;  // Default command index
            messageDialog.CancelCommandIndex = 0;   // Cancel index

            UICommand command = (UICommand)await messageDialog.ShowAsync();
        }

        public static async Task TrialModePurchaseMessage()
        {
            var resourceLoader = new ResourceLoader();

            string title = resourceLoader.GetString("AlertTrialModeThanks");
            string problem = resourceLoader.GetString("AlertTrialModeFull");

            string continueString = resourceLoader.GetString("AlertContinue");

            var messageDialog = new MessageDialog(problem, title);

            messageDialog.Commands.Add(new UICommand(continueString, null, "continue"));

            messageDialog.DefaultCommandIndex = 0;  // Default command index
            messageDialog.CancelCommandIndex = 0;   // Cancel index

            UICommand command = (UICommand)await messageDialog.ShowAsync();
        }

//        public static async Task TrialModeMessage(TimeSpan timeRemaining, Uri storeListing)
//        {
//            var resourceLoader = new ResourceLoader();

//            double days = Math.Ceiling(timeRemaining.TotalDays);

//            string contentF = resourceLoader.GetString("AlertTrialModeMessageFormat");
//            string title = resourceLoader.GetString("AlertTrialModeMessageTitle");
//            string buy = resourceLoader.GetString("AlertTrialModeBuy");
//            string close = resourceLoader.GetString("AlertTrialModeClose");
//            string continueTrial = resourceLoader.GetString("AlertTrialModeContinue");

//            var messageDialog = new MessageDialog("", title);

//            messageDialog.Commands.Add(new UICommand(buy, null, "buy"));

//            if (days <= 0)
//            {
//                messageDialog.Content = resourceLoader.GetString("AlertTrailModeExpired");
//                messageDialog.Commands.Add(new UICommand(close, null, "close"));
//            }
//            else
//            {
//                messageDialog.Commands.Add(new UICommand(continueTrial, null, "continue"));

//                if (days <= 1)
//                {
//                    messageDialog.Content = resourceLoader.GetString("AlertTrailModeExpiresToday");
//                }
//                else
//                {
//                    messageDialog.Content = string.Format(contentF, days);
//                }
//            }

//            messageDialog.DefaultCommandIndex = 0;  // Default command index
//            messageDialog.CancelCommandIndex = 1;   // Cancel index

//            UICommand command = (UICommand)await messageDialog.ShowAsync();

//            if ((string)command.Id == "buy")
//            {
//#if DEBUG
//                await Windows.ApplicationModel.Store.CurrentAppSimulator.RequestAppPurchaseAsync(false);
//#else
//                await Launcher.LaunchUriAsync(storeListing);
//#endif
//            }
//            else if ((string)command.Id == "continue")
//            {
//            }
//            else if ((string)command.Id == "close")
//            {
//                App.Window.Exit();
//            }
//        }

        internal static async Task RateRevieweMessage()
        {
            try
            {
                // Create the message dialog and set its content and title
                var messageDialog = new MessageDialog("If you find Back to the Drawing Board useful, we would like to see your rating in the Windows Store.  A 5-star rating will help us continue to enhance the app in the future.  Thanks!", "Rate Back to the Drawing Board");

                messageDialog.Commands.Add(new UICommand("Remind me later", (command) =>
                {
                    int reviewThreshold = Analytics.GetUsageCount("review_threshold");
                    Analytics.SetUsageCount("review_threshold", reviewThreshold + 50);
                    Analytics.ReportEvent("review", new Dictionary<string, string> { { "action", "later" }, { "threshold", reviewThreshold.ToString() } });
                }));

                messageDialog.Commands.Add(new UICommand("Don't add my rating", (command) =>
                {
                    Analytics.SetUsageCount("review_threshold", 0);
                    Analytics.ReportEvent("review", new Dictionary<string, string> { { "action", "no" } });
                }));

                // Add commands and set their callbacks
                messageDialog.Commands.Add(new UICommand("Add my rating", async (command) =>
                {
                    await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9wzdncrfj861"));
                    Analytics.SetUsageCount("review_threshold", 0);
                    Analytics.ReportEvent("review", new Dictionary<string, string> { { "action", "yes" } });
                }));

                // Set the command that will be invoked by default
                messageDialog.DefaultCommandIndex = 2;

                // Show the message dialog
                await messageDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "RateRevieweMessage" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 625);
            }
        }

        public static async Task InvalidLicense(Uri storeListing)
        {
            var resourceLoader = new ResourceLoader();

            string title = resourceLoader.GetString("AlertInvalidLicense");
            string message = resourceLoader.GetString("AlertInvalidLicenseMessage");

            var messageDialog = new MessageDialog(message, title);

            string buy = resourceLoader.GetString("AlertTrialModeBuy");
            string close = resourceLoader.GetString("AlertTrialModeClose");

            messageDialog.Commands.Add(new UICommand(buy, null, "buy"));
            messageDialog.Commands.Add(new UICommand(close, null, "close"));

            messageDialog.DefaultCommandIndex = 0;  // Default command index
            messageDialog.CancelCommandIndex = 0;   // Cancel index

            UICommand command = (UICommand)await messageDialog.ShowAsync();
            if ((string)command.Id == "buy")
            {
                try
                {
#if DEBUG
                    await Windows.ApplicationModel.Store.CurrentAppSimulator.RequestAppPurchaseAsync(false);
#else
                    await Windows.ApplicationModel.Store.CurrentApp.RequestAppPurchaseAsync(false);
#endif
                }
                catch
                {

                }
            }
            else if ((string)command.Id == "continue")
            {
            }
            else if ((string)command.Id == "close")
            {
                Application.Current.Exit();
            }
        }
    }
}
