using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Cirros;
using Microsoft.UI.Xaml.Controls.Primitives;
using Cirros.Alerts;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml.Controls;

namespace KT22
{
    public class KTAlerts
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
                double top = senderRect.Top - content.Height - 20;
                double left = senderRect.Left + 50;

                Frame root = App.Window.Content as Frame;
                if (root != null)
                {
                    if (senderRect.Bottom == 0 || top <= 0)
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
    }
}
