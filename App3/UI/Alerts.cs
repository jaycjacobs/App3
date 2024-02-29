using Cirros.Alerts;
using Cirros.Drawing;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Cirros.UI.Alerts
{
    public class XamlAlertPlatform : AlertPlatform
    {
        public override async Task<string> AlertOk(string title, string content, string ok)
        {
            var messageDialog = new MessageDialog(content, title);

            messageDialog.Commands.Add(new UICommand(ok, null, "ok"));

            messageDialog.DefaultCommandIndex = 0;  // Default command index
            messageDialog.CancelCommandIndex = 1;   // Cancel index

            UICommand command = (UICommand)await messageDialog.ShowAsync();

            return (string)command.Id;
        }

        public override async Task<string> AlertYNC(string title, string content, string yes, string no, string cancel = null)
        {
            var messageDialog = new MessageDialog(content, title);

            messageDialog.Commands.Add(new UICommand(yes, null, "yes"));
            messageDialog.Commands.Add(new UICommand(no, null, "no"));

            if (cancel != null)
            {
                messageDialog.Commands.Add(new UICommand(cancel, null, "cancel"));
            }

            messageDialog.DefaultCommandIndex = 0;  // Default command index
            messageDialog.CancelCommandIndex = 1;   // Cancel index

            UICommand command = (UICommand)await messageDialog.ShowAsync();

            return (string)command.Id;
        }
    }

    public class SettingsPanelAlerts
    {
        public static void Alert(FrameworkElement sender, string alertString)
        {
            Rect senderRect = XamlUtilities.GetElementRect(sender, null);
            AlertF(senderRect, alertString, null);
        }

        public static void AlertF(FrameworkElement sender, string alertString, string s)
        {
            Rect senderRect = XamlUtilities.GetElementRect(sender, null);
            AlertF(senderRect, alertString, s);
        }

        private static void Alert(Rect senderRect, string alertString)
        {
            AlertF(senderRect, alertString, null);
        }

        private static void AlertF(Rect senderRect, string alertString, string s)
        {
            Popup _popup = new Popup();
            _popup.IsLightDismissEnabled = true;

            AlertBox content = new AlertBox();

            var resourceLoader = new ResourceLoader();

            if (s == null)
            {
                content.AlertText = resourceLoader.GetString(alertString);
            }
            else
            {
                s = s.Replace("\"", "\\\"");
                string f = resourceLoader.GetString(alertString);
                content.AlertText = string.Format(f, s);
            }

            if (!_popup.IsOpen)
            {
                Frame root = App.Window.Content as Frame;
                double top = senderRect.Top - content.Height - 20;
                double left = senderRect.Left + 50;

                if (root != null)
                {
                    if (left < root.ActualWidth / 2)
                    {
                        left = (root.ActualWidth - content.Width) / 2;
                        top = root.ActualHeight / 2 - content.Height;
                    }
                    else if (left + content.Width > root.ActualWidth)
                    {
                        left = root.ActualWidth - content.Width - 10;
                    }
                }

                _popup.Child = content as UserControl;
                _popup.VerticalOffset = top > 20 ? top : 20;
                _popup.HorizontalOffset = left > 20 ? left : 20;
                _popup.IsOpen = true;
            }
        }
    }

    public class ConfirmBoxes
    {
        public static void InvisibleLayerWarning(FrameworkElement sender, Layer layer)
        {
            Popup _popup = new Popup();
            _popup.IsLightDismissEnabled = true;

            ConfirmBox content = new ConfirmBox();
            content.Tag = layer;
            content.Unloaded += content_Unloaded;

            var resourceLoader = new ResourceLoader();

            content.AlertText = resourceLoader.GetString("AlertInvisibleLayer");
            content.ContinueText = resourceLoader.GetString("AlertInvisibleLayerContinue");

            if (!_popup.IsOpen)
            {
                Rect senderRect = XamlUtilities.GetElementRect(sender, null);

                double top = senderRect.Top - content.Height - 20;
                double left = senderRect.Left + 50;

                Frame root = App.Window.Content as Frame;
                if (root != null)
                {
                    if (left > root.ActualWidth / 2)
                    {
                        left = root.ActualWidth - content.Width - 10;
                        top = senderRect.Bottom + 10;
                    }
                    else if (senderRect.Bottom == 0 || top <= 0)
                    {
                        top = root.ActualHeight - content.Height - 100;
                        left = 100;
                    }
                }

                _popup.Child = content as UserControl;
                _popup.VerticalOffset = top > 0 ? top : 0;
                _popup.HorizontalOffset = left > 0 ? left : 0;
                _popup.IsOpen = true;
            }
        }

        static void content_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is ConfirmBox && ((ConfirmBox)sender).ShouldContinue)
            {
                Layer layer = ((Layer)((ConfirmBox)sender).Tag);

                if (layer != null)
                {
                    layer.Visible = true;
                    Layer.PropagateLayerChanges(layer.Id);
                }
            }
        }
    }
}
