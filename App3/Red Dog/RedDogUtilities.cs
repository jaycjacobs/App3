using Cirros;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using CirrosUWP.RedDog;
using HUI;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using KT22.UI;
using Microsoft.UI.Xaml.Input;

namespace RedDog.Console
{
    class ConsoleUtilities
    {
        public static int LayerIdFromName(string layerName)
        {
            if (layerName != "Active_Layer")
            {
                string lcname = layerName.ToLower();

                foreach (int id in Globals.LayerTable.Keys)
                {
                    Layer layer = Globals.LayerTable[id];

                    if (layer.Name.ToLower() == lcname)
                    {
                        return id;
                    }
                }
            }

            return -1;
        }

        public static int LineTypeIdFromThickness(double thickness)
        {
            return (int)Math.Ceiling(thickness * 1000);
        }

        public static int LineTypeIdFromName(string lineTypeName)
        {
            string lcname = lineTypeName.ToLower();

            foreach (int id in Globals.LineTypeTable.Keys)
            {
                LineType lineType = Globals.LineTypeTable[id];

                if (lineType.Name.ToLower() == lcname)
                {
                    return id;
                }
            }

            return -1;
        }

        public static uint ColorSpecFromString(string value)
        {
            uint colorSpec = 0x1;

            if (value.StartsWith("#"))
            {
                colorSpec = uint.Parse(value.Substring(1), NumberStyles.AllowHexSpecifier);
            }
            else if (value == "outline")
            {
                colorSpec = (uint)ColorCode.SameAsOutline;
            }
            else if (value == "layer" || value == "by_layer" || value == "by-layer")
            {
                colorSpec = (uint)ColorCode.ByLayer;
            }
            else if (value == "none")
            {
                colorSpec = (uint)ColorCode.NoFill;
            }
            else if (int.TryParse(value.Replace(" (acad)", ""), out int acolor) && acolor <= 255)
            {
                colorSpec = Utilities.ColorSpecFromAutoCadColor(acolor);
            }
            else
            {
                colorSpec = StandardColors.Colors.FirstOrDefault(x => x.Value.ToLower() == value.ToLower()).Key;
                if (colorSpec == 0)
                {
                    colorSpec = 0x1;
                }
            }

            return colorSpec;
        }

        public static ArrowType ArrowStyleTypeFromName(string name)
        {
            ArrowType type;

            switch (name)
            {
                case "dot":
                    type = ArrowType.Dot;
                    break;

                case "ellipse":
                    type = ArrowType.Ellipse;
                    break;

                default:
                case "filled":
                    type = ArrowType.Filled;
                    break;

                case "open":
                    type = ArrowType.Open;
                    break;

                case "outline":
                    type = ArrowType.Outline;
                    break;

                case "wide":
                    type = ArrowType.Wide;
                    break;
            }

            return type;
        }

        internal static int ArrowStyleIdFromName(string styleName)
        {
            string lcname = styleName.ToLower();

            foreach (int id in Globals.ArrowStyleTable.Keys)
            {
                ArrowStyle style = Globals.ArrowStyleTable[id];

                if (style.Name.ToLower() == lcname)
                {
                    return id;
                }
            }

            return -1;
        }

        internal static int TextStyleIdFromName(string styleName)
        {
            string lcname = styleName.ToLower();

            foreach (int id in Globals.TextStyleTable.Keys)
            {
                TextStyle style = Globals.TextStyleTable[id];

                if (style.Name.ToLower() == lcname)
                {
                    return id;
                }
            }

            return -1;
        }

        internal static int LinetypeIdFromName(string typeName)
        {
            string lcname = typeName.ToLower();

            foreach (int id in Globals.LineTypeTable.Keys)
            {
                LineType type = Globals.LineTypeTable[id];

                if (type.Name.ToLower() == lcname)
                {
                    return id;
                }
            }

            return -1;
        }

        internal static DoubleCollection LengthCollectionFromString(string p)
        {
            DoubleCollection dc = new DoubleCollection();

            string[] sa = p.Split(new char[] { ',' });

            foreach (string s in sa)
            {
                try
                {
                    double d = double.Parse(s.Trim());
                    if (d > 0)
                    {
                        dc.Add(d);
                    }
                    else
                    {
                        // lengths must be positive
                        dc = new DoubleCollection();
                        break;
                    }
                }
                catch
                {
                    dc = new DoubleCollection();
                }
            }

            return dc;
        }

        static RedDogTeachingTips _teachingTips = null;

        public static async Task InitializeTeachingTips()
        {
            if (_teachingTips == null)
            {
                _teachingTips = new RedDogTeachingTips();
                await _teachingTips.Populate();
            }
        }

        public static void PopulateTeachingTips(FrameworkElement baseElement)
        {
            if (baseElement is Border beb && beb.Child is FrameworkElement fe)
            {
                baseElement = fe;
            }

            foreach (Object o in baseElement.Resources.Values)
            {
                if (o is TeachingTip tip && string.IsNullOrEmpty(tip.Name) == false)
                {
                    KT22.UI.HtmlTextBlock htmlBlock = null;

                    if (tip.Content is HtmlTextBlock h)
                    {
                        htmlBlock = h;
                    }
                    else if (tip.Content is Border b && b.Child is HtmlTextBlock bh)
                    {
                        htmlBlock = bh;
                    }

                    if (_teachingTips != null && htmlBlock != null && string.IsNullOrEmpty(htmlBlock.Html))
                    {
                        string s = _teachingTips.GetTip(tip.Name);
                        if (s != null)
                        {
                            htmlBlock.Html = s;
#if DEBUG
                            htmlBlock.AccessKey = "A";
                            htmlBlock.AccessKeyInvoked += Bh_AccessKeyInvoked;
#endif
                        }
                    }
                }
            }

            if (RedDogGlobals.HUIDialogFirstRun)
            {
                if (baseElement is HUIIDialog hd)
                {
                    if (Globals.RootVisual is IDrawingPage page)
                    {
                        page.DialogFirstRun(hd.HelpButton);
                        RedDogGlobals.HUIDialogFirstRun = false;
                    }
                }
            }
        }

        public static string GetTeachingTipContent(string tip)
        {
            return _teachingTips.GetTip(tip);
        }

#if DEBUG
        public static async void Bh_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
        {
            if (sender is HtmlTextBlock htb)
            {
                FrameworkElement parent = htb.Parent as FrameworkElement;
                while (parent != null)
                {
                    if (parent is TeachingTip tip)
                    {
                        RedDogTeachingTipEditor dialog = new RedDogTeachingTipEditor();
                        dialog.TeachingTip = tip;
                        dialog.HtmlTextBlock = htb;

                        ContentDialogResult dialogResult = await dialog.ShowAsync();

                        if (dialog.IsModified && string.IsNullOrEmpty(tip.Name) == false)
                        {
                            _teachingTips.SetTip(tip.Name, htb.Html);
                            await _teachingTips.Save();
                            break;
                        }
                    }
                    parent = parent.Parent as FrameworkElement;
                }
            }
        }
#endif

        public class RedDogTeachingTips
        {
            private Dictionary<string, string> _tips = null;

            public RedDogTeachingTips()
            {
            }

            private void PopulateFromJSON(string json)
            {
                if (_tips != null && string.IsNullOrEmpty(json) == false)
                {
                    JObject jobj = JObject.Parse(json);

                    if (jobj.ContainsKey("Values"))
                    {
                        foreach (JProperty jp in jobj["Values"])
                        {
                            if (jp.Name is string name && jp.Value.Type == JTokenType.String)
                            {
                                SetTip(name, jp.Value.ToString());
                            }
                        }
                    }
                    else if (jobj.Count > 0)
                    {
                        foreach (JProperty jp in jobj.Children())
                        {
                            if (jp.Name is string name && jp.Value.Type == JTokenType.String)
                            {
                                SetTip(name, jp.Value.ToString());
                            }
                        }
                    }
                }
            }

            public async Task Populate()
            {
                if (_tips == null)
                {
                    _tips = new Dictionary<string, string>();

                    StorageFile local = await ApplicationData.Current.LocalFolder.TryGetItemAsync("teachingtips.json") as StorageFile;
                    if (local != null)
                    {
                        string json = await FileIO.ReadTextAsync(local);
                        PopulateFromJSON(json);
                    }

                    if (_tips.Count == 0)
                    {
                        var uri = new System.Uri("ms-appx:///Data/teachingtips.json");
                        StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                        if (file != null)
                        {
                            string json = await FileIO.ReadTextAsync(file);
                            PopulateFromJSON(json);
                        }
                    }
                }
            }

            public string GetTip(string key)
            {
                string tip = "";

                if (_tips.ContainsKey(key))
                {
                    tip = _tips[key];
                }
                else
                {
                    var resourceLoader = new ResourceLoader();
                    tip = resourceLoader.GetString(key);
                }

                return tip;
            }

            public void SetTip(string key, string value)
            {
                if (_tips.ContainsKey(key))
                {
                    _tips[key] = value;
                }
                else
                {
                    _tips.Add(key, value);
                }
            }

            public async Task Save()
            {
                string json = JsonConvert.SerializeObject(_tips);
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("teachingtips.json", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
        }
    }
}
