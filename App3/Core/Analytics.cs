#if DEBUG
#define TRACEX
#endif
using System;
using System.Collections.Generic;
using Windows.Storage;
using System.Threading.Tasks;
using System.Text;
using System.Runtime.CompilerServices;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace Cirros
{
    public class Analytics
    {
//#if KT22
//        private const string _appcenter_secret = "33c0c001-5eeb-46e6-acf4-24a15fb42588";
//#else
//#if PREVIEW
//        private const string _appcenter_secret = "813b274a-eac3-44b9-afdf-ce6514f47267";
//#else
//        private const string _appcenter_secret = "b19d5cea-87f5-4da7-a241-60d8252bb703";
//#endif
//#endif

        static DateTime _initTime;
        static string _sessionId = "X0";

        static Analytics()
        {
            _initTime = DateTime.Now;
            string s = _initTime.ToFileTimeUtc().ToString("X");
            _sessionId = string.Format("X0{0}", s);
        }
   
        public static async void InitializeAnalytics(string secret)
        {
            AppCenter.Start(secret, new Type[] { typeof(Microsoft.AppCenter.Analytics.Analytics), typeof(Crashes) });
#if DEBUG
            bool has = await Crashes.HasCrashedInLastSessionAsync();
            //System.Diagnostics.Debug.WriteLine("Crashes.HasCrashedInLastSessionAsync() = " + has.ToString());
//            AppCenter.LogLevel = LogLevel.Verbose;
#endif
#if TRACEX
            StorageFolder logFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists);
            //_traceFile = await logFolder.CreateFileAsync("trace.log", CreationCollisionOption.ReplaceExisting);
            try
            {
                _traceFile = await logFolder.GetFileAsync("trace.log");
            }
            catch
            {
            }
#else
            await Task.Delay(1);
#endif
        }

#if TRACEX
        static int _traceIndex = 0;
        static StorageFile _traceFile = null;
        static StringBuilder _traceCache = new StringBuilder();
        static bool _traceBusy = false;
        static object _traceLock = new object();

        public static async void Trace(string mark, string submark = "")
        {
            if (_traceFile != null)
            {
                await TraceAsync(mark, submark);
            }
        }

        private static async Task FlushTrace()
        {
            if (_traceFile != null && _traceCache.Length > 0 && _traceBusy == false)
            {
                _traceBusy = true;
                try
                {
                    string x = "";
                    lock (_traceLock)
                    {
                        x = _traceCache.ToString();
                        _traceCache.Clear();
                    }
                    await FileIO.AppendTextAsync(_traceFile, x);
                }
                catch
                {
                }
                _traceBusy = false;
            }
        }

        public static async Task TraceAsync(string mark, string submark)
        {

            if (_traceFile != null)
            {
                string message = string.Format("{0} [{1}]: {2} {3}\n", DateTime.Now.ToString(), (++_traceIndex).ToString(), mark, submark);

                if (_traceBusy)
                {
                    _traceCache.Append(message);
                }
                else
                {
                    _traceBusy = true;
                    try
                    {
                        string x = "";
                        lock (_traceLock)
                        {
                            _traceCache.Append(message);
                            x = _traceCache.ToString();
                            _traceCache.Clear();
                        }
                        await FileIO.AppendTextAsync(_traceFile, x);
                    }
                    catch
                    {
                    }
                    _traceBusy = false;
                    await FlushTrace();
                }
            }
        }
#else   
        public static void Trace(string mark, string submark = "")
        {
        }

        public static async Task TraceAsync(string mark, string submark)
        {
            await Task.Delay(1);
        }
#endif

        private static ApplicationDataContainer _usageSettings = null;

        public static void LaunchTasks()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Containers.ContainsKey("usage"))
            {
                _usageSettings = localSettings.Containers["usage"];
            }
            else
            {
                _usageSettings = localSettings.CreateContainer("usage", ApplicationDataCreateDisposition.Always);
                _usageSettings.Values["review_threshold"] = 12;
            }

            CountUsageEvent("launch");
        }

        public static void SuspendTasks()
        {
            CountUsageEvent("suspend");
        }

        public static void CountUsageEvent(string eventname)
        {
            if (_usageSettings != null)
            {
                int count = 0;

                if (_usageSettings.Values.ContainsKey(eventname))
                {
                    count = (int)_usageSettings.Values[eventname];
                }

                _usageSettings.Values[eventname] = count + 1;
            }
        }

        public static void SetUsageCount(string eventname, int count)
        {
            if (_usageSettings != null)
            {
                _usageSettings.Values[eventname] = count;
            }
        }

        public static int GetUsageCount(string eventname)
        {
            int count = 0;

            try
            {
                if (_usageSettings != null && _usageSettings.Values.ContainsKey(eventname))
                {
                    count = (int)_usageSettings.Values[eventname];
                }
            }
            catch (Exception ex)
            {
                Dictionary<string, string> d = new Dictionary<string, string> {
                                        { "method", "GetUsageCount" },
                                        { "eventname", eventname },
                                    };
                Analytics.ReportError("GetUsageCount", ex, 4, d, 100);
            }

            return count;
        }

        //static async void FireAndForget(string url)
        //{
        //    try
        //    {
        //        HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
        //        using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
        //        {
        //            if (response.StatusCode == HttpStatusCode.OK)
        //            {
        //                Stream stream = response.GetResponseStream();
        //                StreamReader sr = new StreamReader(stream);
        //                string json = sr.ReadToEnd();
        //                //JObject job = JObject.Parse(json);
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Analytics.ReportError("Analytics.Trace: json response error", e, 4);
        //    }
        //}

        public static void ReportError(string message, Exception ex, int severity, int tag)
        {
            if (ex == null)
            {
                Microsoft.AppCenter.Analytics.Analytics.TrackEvent(message);
                Analytics.Trace($"error:{message}", "");
            }
            else
            {
                //HockeyClient.Current.TrackException(ex, new Dictionary<string, string> { { "message", message } });
                Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"exception-{tag}", 
                    new Dictionary<string, string> {
                        { "event", message },
                        { "message", ex.Message },
                        { "hresult", ex.HResult.ToString()
                        }
                    });
                Analytics.Trace($"exception-{tag}", ex.Message);
            }
        }

        public static void ReportError(string message, Exception ex, int severity, Dictionary<string, string> errorData, int tag)
        {
            if (errorData != null)
            {
                errorData["message"] = ex.Message is string ? ex.Message.ToString() : null;
                errorData["stack"] = ex.StackTrace is string ? ex.StackTrace.ToString() : null;
                errorData["hresult"] = ex.HResult.ToString();
                errorData["severity"] = severity.ToString();
            }
            if (severity < 4)
            {
                if (ex == null)
                {
                    Microsoft.AppCenter.Analytics.Analytics.TrackEvent(message);
                }
                else
                {
                    //HockeyClient.Current.TrackException(ex, errorData);
                    Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"exception-{tag}", errorData);
                }
            }
            Analytics.Trace($"error:{message}", ex.Message);
        }

        public static void ReportError(Exception ex, Dictionary<string, string> errorData, int tag)
        {
            if (errorData != null && ex != null)
            {
                errorData["message"] = ex.Message is string ? ex.Message.ToString() : null;
                errorData["stack"] = ex.StackTrace is string ? ex.StackTrace.ToString() : null;
                errorData["hresult"] = ex.HResult.ToString();
            }
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"exception-{tag}", errorData);
            Analytics.Trace($"error:{ex.HResult.ToString()}", ex.Message);
        }

        public static void ReportEvent(string eventType)
        {
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent(eventType);
            Analytics.Trace($"event:{eventType}", "");
        }

        public static void ReportEvent(string eventType, Dictionary<string, string> eventData)
        {
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent(eventType, eventData);
            Analytics.Trace($"event:{eventType}", "");
        }

        public static void ReportEvent(string eventType, Dictionary<string, string> eventData, Dictionary<string, double> eventValues)
        {
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent(eventType, eventData);
            Analytics.Trace($"event:{eventType}", "");
        }

        public static void TrackPageView(string name)
        {
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent("page-view", new Dictionary<string, string> { { "page", name } });
        }

        public static void Crash()
        {
#if DEBUG
            Crashes.GenerateTestCrash();
#endif
        }
    }
}
