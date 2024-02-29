using Cirros;
using Cirros.Drawing;
using Cirros.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UWP
using Windows.Storage;
using Microsoft.UI.Xaml.Media;
#else
using System.Windows;
using System.Windows.Media;
using System.Windows.Resources;
#endif

namespace CirrosCore
{
    public class CrosshatchPatternItem
    {
        public FPoint Origin { get; set; }
        public FPoint Offset { get; set; }
        public float Angle { get; set; }
        public List<float> DashArray { get; set; }

        public CrosshatchPatternItem()
        {
        }

        public CrosshatchPatternItem(CrosshatchPatternItem item, double scale, double rotation)
        {
            Angle = (float)(item.Angle + rotation);

            CompositeTransform tf = new CompositeTransform();
            tf.ScaleX = tf.ScaleY = scale;

            Offset = new FPoint(tf.TransformPoint(item.Offset.ToPoint()));

            tf.Rotation = rotation;
            Origin = new FPoint(tf.TransformPoint(item.Origin.ToPoint()));

            DashArray = new List<float>();
            if (item.DashArray != null)
            {
                foreach (float d in item.DashArray)
                {
                    DashArray.Add(d * (float)scale);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Origin = ({0}, {1})\n", Origin.X, Origin.Y);
            sb.AppendFormat("Offset = ({0}, {1})\n", Offset.X, Offset.Y);
            sb.AppendFormat("Angle = {0}\n", Angle);

            if (DashArray == null)
            {
                sb.AppendLine("DashArray: null");
            }
            else
            {
                sb.Append("DashArray: ");
                foreach (float d in DashArray)
                {
                    sb.AppendFormat("{0} ", d);
                }
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public class CrosshatchPattern : IComparable
    {
        List<CrosshatchPatternItem> _items = new List<CrosshatchPatternItem>();

        public string Name { get; set; }
        public string Description { get; set; }

        public List<CrosshatchPatternItem> Items
        {
            get { return _items; }
        }

        public int CompareTo(object obj)
        {
            return ((IComparable)Name).CompareTo(((CrosshatchPattern)obj).Name);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrEmpty(Description))
            {
                sb.AppendFormat("*{0}", Name);
            }
            else
            {
                sb.AppendFormat("*{0},{1}", Name, Description);
            }
            sb.AppendLine();

            foreach (CrosshatchPatternItem item in _items)
            {
                sb.AppendFormat("{0},{1},{2},{3},{4}", Math.Round(item.Angle, 5), 
                    Math.Round(item.Origin.X, 5), Math.Round(item.Origin.Y, 5),
                    Math.Round(item.Offset.X, 5), Math.Round(item.Offset.Y, 5));

                if (item.DashArray != null && (item.DashArray.Count % 2) == 0)
                {
                    for (int i = 0; i < item.DashArray.Count; i += 2)
                    {
                        sb.AppendFormat(",{0},{1}", Math.Round(item.DashArray[i], 5), Math.Round(item.DashArray[i + 1], 5));
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    public class Patterns
    {
#if UWP
        private static string _defaultPath = "ms-appx:///Data/patterns.pat";
#endif

        static List<CrosshatchPattern> _defaultPatterns = new List<CrosshatchPattern>();
        static SortedDictionary<string, CrosshatchPattern> _patternDictionary = new SortedDictionary<string, CrosshatchPattern>();
        static bool _isDirty = false;

        public async static void Save()
        {
            if (_isDirty)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CrosshatchPattern pattern in _defaultPatterns)
                {
                    sb.Append(pattern.ToString());
                }

                //var uri = new System.Uri(_defaultPath);

#if UWP
                StorageFolder patternFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("data", CreationCollisionOption.OpenIfExists);
                StorageFile file = await patternFolder.CreateFileAsync("patterns.pat", CreationCollisionOption.OpenIfExists);
                if (file != null)
                {
                    await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
                }
#else
                await Task.Delay(1);
#endif

                _isDirty = false;
            }
        }

        public static void AddPatternFromEntityToList(Primitive p, List<string> patternNames)
        {
            if (string.IsNullOrEmpty(p.FillPattern) == false)
            {
                string key = p.FillPattern.ToLower();

                if (CirrosCore.Patterns.PatternDictionary.ContainsKey(key))
                {
                    if (patternNames.Contains(key) == false)
                    {
                        patternNames.Add(key);
                    }
                }
            }
        }

        public static void AddDefaultPattern(CrosshatchPattern pattern)
        {
            string key = pattern.Name.ToLower();
            foreach (CrosshatchPattern defaultPattern in _defaultPatterns)
            {
                if (defaultPattern.Name.ToLower() == key)
                {
                    _defaultPatterns.Remove(defaultPattern);
                    break;
                }
            }

            _defaultPatterns.Add(pattern);
            AddPattern(pattern);

            _isDirty = true;
        }

        public static void DeletePattern(string name)
        {
            string key = name.ToLower();

            if (PatternDictionary.ContainsKey(key))
            {
                Patterns.PatternDictionary.Remove(key);

                foreach (CrosshatchPattern defaultPattern in _defaultPatterns)
                {
                    if (defaultPattern.Name.ToLower() == key)
                    {
                        _defaultPatterns.Remove(defaultPattern);
                        break;
                    }
                }

                _isDirty = true;
            }
        }

        public static void AddPattern(CrosshatchPattern pattern)
        {
            string key = pattern.Name.ToLower();
            if (_patternDictionary.ContainsKey(key))
            {
                _patternDictionary[key] = pattern;
            }
            else
            {
                _patternDictionary.Add(key, pattern);
            }
        }

        public static bool IsDefaultPattern(CrosshatchPattern pattern)
        {
            bool isDefault = _defaultPatterns.Contains(pattern);
            return isDefault;
        }

#if UWP
        private static async Task<List<CrosshatchPattern>> ParseInternalPatternFile()
        {
            StorageFile file = null;

            StorageFolder patternFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("data", CreationCollisionOption.OpenIfExists);
            file = await patternFolder.TryGetItemAsync("patterns.pat") as StorageFile;
            if (file == null)
            {
                var uri = new System.Uri(_defaultPath);
                file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            }

            return await ParsePatternFile(file);
#else
        private static List<CrosshatchPattern> ParseInternalPatternFile()
        {
            List<CrosshatchPattern> list = new List<CrosshatchPattern>();

            try
            {
                string appdata = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
                string path = appdata + "/paper-patterns.pat";
                var fileContents = System.IO.File.ReadAllText(path);
                list = ParsePattern(fileContents);
            }
            catch
            {
            }
            return list;
#endif
        }

        public static List<CrosshatchPattern> ParsePattern(string patternString)
        {
            List<CrosshatchPattern> patternList = new List<CrosshatchPattern>();
            CrosshatchPattern pattern = null;

            string line;

            StringReader sr = new StringReader(patternString);
            while ((line = sr.ReadLine()) != null)
            {
                int c = line.IndexOf(";");
                if (c >= 0)
                {
                    line = line.Substring(0, c).Trim();
                }
                else
                {
                    line = line.Trim();
                }

                if (line == "")
                {
                    // ignore blank lines
                }
                else if (line.StartsWith("*"))
                {
                    if (pattern != null)
                    {
                        patternList.Add(pattern);

                        pattern = null;
                    }

                    pattern = new CrosshatchPattern();
                    int d = line.IndexOf(",");
                    if (d > 1)
                    {
                        pattern.Name = line.Substring(1, d - 1).Trim();
                        pattern.Description = line.Substring(d + 1).Trim();
                    }
                    else
                    {
                        pattern.Name = line.Substring(1).Trim();
                        pattern.Description = "";
                    }
                }
                else
                {
                    List<float> dashArray = null;

                    string[] sa = line.Split(new char[] { ',' });
                    if (sa.Length >= 5)
                    {
                        try
                        {
                            FPoint origin = new FPoint();
                            FPoint offset = new FPoint();

                            float angle = float.Parse(sa[0].Trim());
                            origin.X = float.Parse(sa[1].Trim());
                            origin.Y = float.Parse(sa[2].Trim());
                            offset.X = float.Parse(sa[3].Trim());
                            offset.Y = float.Parse(sa[4].Trim());

                            if (sa.Length > 5)
                            {
                                int dashes = sa.Length - 5;
                                dashArray = new List<float>(dashes);
                                for (int i = 0; i < dashes; i++)
                                {
                                    string dash = sa[i + 5].Trim();
                                    float dashLength = float.Parse(dash);
                                    dashArray.Add(dashLength);
                                }
                            }

                            pattern.Items.Add(new CrosshatchPatternItem() { Angle = angle, Origin = origin, Offset = offset, DashArray = dashArray });
                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine("invalid pattern ({0}): {1}", pattern.Name, line);
                        }
                    }
                }
            }

            if (pattern != null)
            {
                patternList.Add(pattern);
                pattern = null;
            }

            return patternList;
        }

#if UWP
        public static async Task<List<CrosshatchPattern>> ParsePatternFile(StorageFile file)
        {
            List<CrosshatchPattern> patternList = new List<CrosshatchPattern>();

            if (file.IsAvailable)
            {
                string data = await FileIO.ReadTextAsync(file);
                patternList = ParsePattern(data);
            }

            return patternList;
        }

        public static async Task InitializeAsync()
        {
            _defaultPatterns = await ParseInternalPatternFile();
            _defaultPatterns.Sort();

            ResetPatternList();
        }
#else
        public static async Task<List<CrosshatchPattern>> ParsePatternStream(Stream stream)
        {
            List<CrosshatchPattern> patternList = new List<CrosshatchPattern>();

            try
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    String data = await sr.ReadToEndAsync();
                    patternList = ParsePattern(data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return patternList;
        }

        public static void Initialize()
        {
            _defaultPatterns = ParseInternalPatternFile();
            _defaultPatterns.Sort();

            ResetPatternList();
        }
#endif

        public static void ResetPatternList()
        {
            _patternDictionary.Clear();

            foreach (CrosshatchPattern pattern in _defaultPatterns)
            {
                AddPattern(pattern);
            }
        }

        public static IDictionary<string, CrosshatchPattern> PatternDictionary
        {
            get
            {
                return _patternDictionary;
            }
        }

        public static void PropagatePatternChange(string name)
        {
            foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
            {
                if (p.FillPattern == name)
                {
                    p.Draw();
                }
            }
        }
    }
}
