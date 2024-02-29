using Cirros.Alerts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CirrosUI
{
    public class XamlAlertPlatform : AlertPlatform
    {
        static XamlAlertPlatform _alertPlatform = new XamlAlertPlatform();
            
        private XamlAlertPlatform()
        {
            StandardAlerts.SetAlertPlatform(this);
        }

        public override Task<string> AlertOk(string title, string content, string ok)
        {
            return base.AlertOk(title, content, ok);
        }

        public override Task<string> AlertYNC(string title, string content, string yes, string no, string cancel = null)
        {
            return base.AlertYNC(title, content, yes, no, cancel);
        }
    }
}
