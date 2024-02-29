using System.Collections.Generic;

namespace Cirros8
{
    public interface IHomePage
    {
        IList<object> MruSelection { set; }
        void WriteSettingsEntry(string key, string value);
        void ClearSettingsEntry(string key);
        string GetSettingsValue(string key);
    }

    public interface IShareablePage
    {
        void RemoveShareHandler();
    }
}
