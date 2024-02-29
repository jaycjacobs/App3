using Cirros;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
using App3;

namespace Cirros8
{
    public sealed partial class RecentDrawingsControl : UserControl
    {
        public RecentDrawingsControl()
        {
            this.InitializeComponent();

            this.Loaded += RecentDrawingsControl_Loaded;

            _mruGrid.Items.Clear();
            _mruGrid.SelectionChanged += _mruGrid_SelectionChanged;

            _rows = 0;
        }

        int _rows = 0;
        int _cols = 0;

        public void UpdateRowsAndColumns(double maxHeight)
        {
            this.MaxHeight = maxHeight;

            if (_mruItems.Count > 0)
            {
                int rows = Math.Max((int)Math.Floor((maxHeight - 40) / (_mruItems[0].MruTileSize.Height + 10)), 1);
                int cols = (int)Math.Floor(this.ActualWidth / (_mruItems[0].MruTileSize.Width + 10));

                if (rows > 0 && cols > 0)
                {
                    if (rows != _rows)
                    {
                        _rows = rows;
                        this.Height = rows * (_mruItems[0].MruTileSize.Height + 0) + 40;

                        if (_mruGrid.ItemsPanelRoot is WrapGrid wrapGrid)
                        {
                            if (_rows == 1)
                            {
                                wrapGrid.Orientation = Orientation.Vertical;
                                wrapGrid.MaximumRowsOrColumns = 1;
                            }
                            else
                            {
                                wrapGrid.Orientation = Orientation.Horizontal;
                                wrapGrid.MaximumRowsOrColumns = _cols;
                            }
                        }
                    }
                }
            }
        }

        IHomePage _homePage = null;

        async void RecentDrawingsControl_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = this;

#if UWP
            _mruGrid.IsMultiSelectCheckBoxEnabled = false;
#endif
            while (fe.Parent is FrameworkElement && (fe is IHomePage) == false)
            {
                fe = (FrameworkElement)fe.Parent;
            }

            if (fe is IHomePage)
            {
                _homePage = fe as IHomePage;
            }

            if (_homePage != null && _homePage.GetSettingsValue("mrutoken") != null)
            {
                // If mrutoken was set and not cleared, Windows threw an uncatchable exception in a StorageApplicationPermissions call
                // the last time the app was run (NOT OUR FAULT).  Apparently the MRU list contains a corrupt entry that crashes the app on launch.
                // We'll try to remove it here.

                Analytics.ReportError("Cleared corrupt MRU list", null, 2, 605);

                try
                {
                    StorageApplicationPermissions.MostRecentlyUsedList.Clear();
                }
                catch (Exception ex)
                {
                    Analytics.ReportError("RecentDrawingsControl_Loaded", ex, 4, 606);
                }
                _homePage.ClearSettingsEntry("mrutoken");
            }

            await UpdateMruList();
        }

        private List<RecentDrawingItem> _mruItems = new List<RecentDrawingItem>();

        public void ClearSelection()
        {
            _mruGrid.SelectedItems.Clear();
        }

        void _mruGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (RecentDrawingItem item in _mruGrid.SelectedItems)
            {
                if ((item is MruRecentDrawingItem) == false)
                {
                    _mruGrid.SelectedItems.Remove(item);
                }
            }

            _homePage.MruSelection = _mruGrid.SelectedItems;
        }

        public async Task UpdateMruList()
        {
            _mruItems.Clear();

            StorageFile current = await FileHandling.GetCurrent();
            if (current != null)
            {
                RecentDrawingItem currentItem = new RecentDrawingItem("restore");
                await currentItem.InitializeFromFile(current, DateTime.Now.ToFileTime().ToString());

                if (currentItem.AccessTime != "")
                {
                    _mruItems.Add(currentItem);
                }
            }

            try
            {
                AccessListEntryView entries = StorageApplicationPermissions.MostRecentlyUsedList.Entries;

                foreach (AccessListEntry entry in entries)
                {
                    MruRecentDrawingItem item = new MruRecentDrawingItem();

                    _homePage.WriteSettingsEntry("mrutoken", entry.Token.ToString());

                    //System.Diagnostics.Debug.WriteLine("calling InitializeFromMruEntry");
                    await item.InitializeFromMruEntry(entry);
                    //System.Diagnostics.Debug.WriteLine("back from InitializeFromMruEntry");

                    _homePage.ClearSettingsEntry("mrutoken");

                    if (item.DrawingName == "")
                    {
                        // Failed to get metadata for MRU entry - ignore
                    }
                    else
                    {
                        long.TryParse(item.AccessTime, out long t);

                        for (int i = 0; i < _mruItems.Count; i++)
                        {
                            if (_mruItems[i].File != null && _mruItems[i].File.IsEqual(item.File))
                            {
                                // this drawing is already in the list
                                long.TryParse(_mruItems[i].AccessTime, out long t0);
                                if (t0 < t)
                                {
                                    // keep only the newest entry
                                    _mruItems[i] = item;
                                }
                                item = null;
                                break;
                            }
                        }

                        if (item != null)
                        {
                            _mruItems.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError("UpdateMruList", ex, 4, 607);
            }

            _mruItems.Sort();

            if (this.MaxHeight > 0)
            {
                UpdateRowsAndColumns(this.MaxHeight);
            }

            _mruGrid.ItemsSource = null;
            _mruGrid.ItemsSource = _mruItems;
        }

        private void _mruGridItemClick(object sender, ItemClickEventArgs e)
        {
            RecentDrawingItem item = e.ClickedItem as RecentDrawingItem;
            if (item != null)
            {
                App.NavigateToDrawingPage(item.Token);
            }
        }

        public IHomePage HostPage
        {
            set
            {
                _homePage = value;
            }
        }

        private void _mruGrid_Holding(object sender, HoldingRoutedEventArgs e)
        {
            e.Handled = true;

            if (e.HoldingState == HoldingState.Completed)
            {
                var item = (e.OriginalSource as FrameworkElement).DataContext as RecentDrawingItem;
                if (item is MruRecentDrawingItem)
                {
                    if (_mruGrid.SelectedItems.Contains(item))
                    {
                        _mruGrid.SelectedItems.Remove(item);
                    }
                    else
                    {
                        _mruGrid.SelectedItems.Add(item);
                    }
                }
            }
        }

        private void _mruGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var item = (e.OriginalSource as FrameworkElement).DataContext as RecentDrawingItem;
            if (item is MruRecentDrawingItem)
            {
                if (_mruGrid.SelectedItems.Contains(item))
                {
                    _mruGrid.SelectedItems.Remove(item);
                }
                else
                {
                    _mruGrid.SelectedItems.Add(item);
                }
            }
        }
    }

    public class RecentDrawingItem : INotifyPropertyChanged, IComparable
    {
        protected string _drawingName = "";
        protected string _path = "";
        protected string _actionName = "";
        protected string _token = "";
        protected string _accessTime = "";
        protected string _date = "";
        private string _icon = "";
        protected ImageSource _thumbSource = null;
        protected StorageFile _file = null;

        public RecentDrawingItem()
        {
        }

        public RecentDrawingItem(string content)
        {
            if (content == "restore")
            {
                _actionName = "My current drawing";
                _accessTime = DateTime.Now.ToFileTime().ToString();
            }

            _date = "";
            _token = content;
        }

        public async Task UpdateThmbnail()
        {
            string thumbName = _token == "restore" ? null : _token;

            try
            {
                StorageFile thumbfile = await FileHandling.GetMruThumbnailAsync(thumbName);
                if (thumbfile != null)
                {
                    BitmapImage src = new BitmapImage();
                    src.SetSource(await thumbfile.OpenAsync(FileAccessMode.Read));
                    _thumbSource = src;
                }
            }
            catch
            {
            }
        }

        public async Task InitializeFromFile(StorageFile file, string filetime)
        {
            if (file.Name.StartsWith("__"))
            {
                _accessTime = filetime;
            }
            else
            {
                int epos = file.Name.IndexOf(".dbfx");
                if (epos > 0)
                {
                    _drawingName = file.Name.Substring(0, epos);
                    _path = file.Path;
                    AccessTime = filetime;

                    try
                    {
                        long fileTime = long.Parse(filetime);
                        _date = DateTime.FromFileTime(fileTime).ToString();
                    }
                    catch
                    {
                        _date = "";
                    }

                    _file = file;
                }
            }

            await UpdateThmbnail();
        }

        public double UIFontSizeSmall
        {
            get { return Globals.UIDataContext.UIFontSizeExtraSmall; }
        }

        public double UIFontSizeNormal
        {
            get { return Globals.UIDataContext.UIFontSizeNormal; }
        }

        public Size MaxLargeThumbnailSize
        {
            get
            {
                Size size;
                if (Globals.UIDataContext.Size >= 18)
                {
                    size = new Size(240, 240);
                }
                else if (Globals.UIDataContext.Size >= 14)
                {
                    size = new Size(180, 180);
                }
                else
                {
                    size = new Size(140, 140);
                }

                return size;
            }
        }

        public Size MruTileSize
        {
            get
            {
                Size size;
                if (Globals.UIDataContext.Size >= 18)
                {
                    size = new Size(300, 300);
                }
                else if (Globals.UIDataContext.Size >= 16)
                {
                    size = new Size(220, 220);
                }
                else
                {
                    size = new Size(160, 160);
                }
                return size;
            }
        }

        public string ActionName
        {
            get
            {
                return _actionName;
            }
            set
            {
                _actionName = value;
            }
        }

        public string DrawingName
        {
            get
            {
                return _drawingName;
            }
            set
            {
                _drawingName = value;
            }
        }

        public string Path
        {
            get
            {
                if (string.IsNullOrEmpty(_path))
                {
                    return "Current drawing";
                }
                return _path;
            }
            //set
            //{
            //    _path = value;
            //}
        }

        public string Date
        {
            get
            {
                return _date;
            }
        }

        public string Icon
        {
            get
            {
                return _icon;
            }
        }

        public string Token
        {
            get
            {
                return _token;
            }
            set
            {
                _token = value;
            }
        }

        public string AccessTime
        {
            get
            {
                return _accessTime;
            }
            set
            {
                _accessTime = value;
            }
        }

        public StorageFile File
        {
            get
            {
                return _file;
            }
            set
            {
                _file = value;
            }
        }

        public ImageSource Thumbnail
        {
            get
            {
                return _thumbSource;
            }
            set
            {
                _thumbSource = value;
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        public int CompareTo(object obj)
        {
            return ((RecentDrawingItem)obj).AccessTime.CompareTo(AccessTime);
        }
    }

    public class MruRecentDrawingItem : RecentDrawingItem
    {
        protected AccessListEntry _entry;

        public MruRecentDrawingItem()
        {
        }

        public async Task InitializeFromMruEntry(AccessListEntry entry)
        {
            bool entryIsValid = true;

            _entry = entry;

            try
            {
                _token = _entry.Token;
                StorageFile file = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(_entry.Token);
                if (file.Attributes.HasFlag(FileAttributes.Temporary))
                {
                    entryIsValid = false;
                }
                else
                {
                    await InitializeFromFile(file, _entry.Metadata);
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                entryIsValid = false;
            }
            catch (Exception ex)
            {
                entryIsValid = false;
                Analytics.ReportError("InitializeFromMruEntry 1", ex, 2, 608);
            }

            if (!entryIsValid)
            {
                try
                {
                    await FileHandling.RemoveFileFromMruAsync(_entry.Token);
                }
                catch (Exception ex)
                {
                    Analytics.ReportError("InitializeFromMruEntry 2", ex, 2, 609);
                }
            }
        }
    }
}
