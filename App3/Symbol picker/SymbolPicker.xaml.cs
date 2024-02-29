using Cirros;
using Cirros.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml;

namespace Cirros8.Symbols
{
    public sealed partial class SymbolPicker : UserControl
    {
        private List<SymbolPickerItem> _symbolItems = new List<SymbolPickerItem>();
        List<string> _folders = new List<string>();
        SymbolPickerItem _selectedItem = null;
        string _selectedGroupName = "";
        string _selectedFolderName = "";

        public SymbolPicker()
        {
            this.InitializeComponent();

            this.Height = 600;

            this.Loaded += SymbolPicker_Loaded;
        }

        private async void SymbolPicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (_symbolItems.Count == 0)
            {
                await AddSymbolsFromLibrary();
                //CreateSymbolItems();
                if (_symbolItems.Count == 0)
                {
                    _emptyText.Visibility = Visibility.Visible;
                }
                else
                {
                    cvs2.Source = GetSymbolItemsGrouped();
                }
            }
        }

        public string SelectedSymbolName { get { return _selectedGroupName; } }

        public string SelectedFolderlName { get { return _selectedFolderName; } }

        //private void CreateSymbolItems()
        //{
        //    for (int i = 0; i < 24; i++)
        //    {
        //        string name = "Symbol number " + i.ToString();
        //        AddItem(_symbolItems, name, "XYZ", "Main");
        //    }
        //    for (int i = 0; i < 118; i++)
        //    {
        //        string name = i.ToString();
        //        AddItem(_symbolItems, name, "XYZ", "Red");
        //    }
        //    for (int i = 0; i < 32; i++)
        //    {
        //        string name = i.ToString();
        //        AddItem(_symbolItems, name, "XYZ", "Green");
        //    }
        //    for (int i = 0; i < 5; i++)
        //    {
        //        string name = i.ToString();
        //        AddItem(_symbolItems, name, "XYZ", "Blue");
        //    }
        //}

        public async Task AddFolderContents(StorageFolder folder)
        {
            IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync();
            IReadOnlyList<StorageFolder> folderList = await folder.GetFoldersAsync();

            string folderPath = folder.Path.Replace(_localFolder.Path, "").Replace("\\", "/");

            foreach (StorageFile file in fileList)
            {
                if (file.FileType == ".dbsx")
                {
                    AddItem(_symbolItems, file, folderPath);
                }
            }

            foreach (StorageFolder subfolder in folderList)
            {
                await AddFolderContents(subfolder);
            }
        }

        private void AddDrawingNamedGroups()
        {
            string folder = "Named groups in this drawing";

            foreach (Group group in Globals.ActiveDrawing.Groups.Values)
            {
                if (group.Name.StartsWith(":") == false)
                {
                    AddItem(_symbolItems, group, folder);
                }
            }
        }
               
        StorageFolder _localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

        private async Task AddSymbolsFromLibrary()
        {
            try
            {
                StorageFolder symbolsFolder = await _localFolder.CreateFolderAsync("Symbols", CreationCollisionOption.OpenIfExists);

                AddDrawingNamedGroups();
                await AddFolderContents(symbolsFolder);
            }
            catch
            {
            }
        }

        private List<GroupInfoList<object>> GetSymbolItemsGrouped()
        {
            var query = from item in _symbolItems
                        group item by ((SymbolPickerItem)item).FolderIndex into g
                        orderby g.Key
                        select new { GroupName = g.Key, Items = g };

            List<GroupInfoList<object>> groupInfoList = new List<GroupInfoList<object>>();

            foreach (var g in query)
            {
                GroupInfoList<object> info = new GroupInfoList<object>();

                if (g.GroupName is int index && index >= 0 && index < _folders.Count)
                {
                    info.Key = _folders[index] + " (" + g.Items.Count() + ")";
                }
                else
                {
                    info.Key = "???";
                }

                foreach (var item in g.Items)
                {
                    info.Add(item);
                }

                groupInfoList.Add(info);
            }

            return groupInfoList;
        }

        private void AddItem(List<SymbolPickerItem> oc, StorageFile symbolFile, string folder)
        {
            SymbolPickerItem item1 = new SymbolPickerItem(symbolFile, folder);
            oc.Add(item1);

            if (_folders.Contains(item1.Folder) == false)
            {
                _folders.Add(item1.Folder);
            }

            item1.FolderIndex = _folders.IndexOf(item1.Folder);
            item1.OnSymbolSelected += Item1_OnSymbolSelected;
        }

        private void AddItem(List<SymbolPickerItem> oc, Group g, string folder)
        {
            SymbolPickerItem item1 = new SymbolPickerItem(g, folder);
            oc.Add(item1);

            if (_folders.Contains(item1.Folder) == false)
            {
                _folders.Add(item1.Folder);
            }

            item1.FolderIndex = _folders.IndexOf(item1.Folder);
            item1.OnSymbolSelected += Item1_OnSymbolSelected;
        }

        private void AddItem(List<SymbolPickerItem> oc, string name, string description, string folder)
        {
            SymbolPickerItem item1 = new SymbolPickerItem(name, description, folder);
            oc.Add(item1);

            if (_folders.Contains(item1.Folder) == false)
            {
                _folders.Add(item1.Folder);
            }

            item1.FolderIndex = _folders.IndexOf(item1.Folder);
            item1.OnSymbolSelected += Item1_OnSymbolSelected;
        }

        private void Item1_OnSymbolSelected(object sender, SymbolSelectedEventArgs e)
        {
            if (e.SelectedItem != null)
            {
                _selectedItem = e.SelectedItem;
                //_selectedGroupName = $"{e.SelectedItem.Folder}:{e.SelectedItem.Name}";
                _selectedGroupName = e.SelectedItem.Name;
                _selectedFolderName = e.SelectedItem.Folder;

                _symbolPickerGridView.SelectedItem = e.SelectedItem;
#if KT22
                if (this.Parent is Popup popup)
                {
                    popup.IsOpen = false;
                }
#else
                if (Globals.RootVisual is DrawingPage)
                {
                    ((DrawingPage)Globals.RootVisual).DismissPopup();
                }
                else if (this.Parent is Popup popup)
                {
                    popup.IsOpen = false;
                }
#endif
            }
        }

        private class GroupInfoList<T> : List<object>
        {
            public object Key { get; set; }

            public new IEnumerator<object> GetEnumerator()
            {
                return (System.Collections.Generic.IEnumerator<object>)base.GetEnumerator();
            }
        }

        bool _expand = true;

        private void _expandCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is string folder && button.Content is string text)
                {
                    if (_expand)
                    {
                        button.Content = "";
                        _expand = false;
                    }
                    else
                    {
                        button.Content = "";
                        _expand = true;
                    }
                }
            }
        }

        private void _expandCollapseButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is string folder && button.Content is string text)
                {
                    if (_expand)
                    {
                        button.Content = "";
                        _expand = false;
                    }
                    else
                    {
                        button.Content = "";
                        _expand = true;
                    }
                }
            }
        }
    }

    public class SymbolPickerItem
    {
        public event SymbolSelectedHandler OnSymbolSelected;
        public delegate void SymbolSelectedHandler(object sender, SymbolSelectedEventArgs e);

        private bool _isSelected = false;

        public string Name { get; private set; }
        public Group Group { get; private set; }
        public StorageFile File { get; private set; }
        public string ThumbnailData { get; private set; }
        public string Folder
        {
            get;
            set;
        }
        public string Description { get; set; }
        public int FolderIndex
        {
            get;
            set;
        }


        public SymbolPickerItem(StorageFile symbolFile, string folder)
        {
            Folder = folder;
            File = symbolFile;

            InitializeFromFile(symbolFile);
        }

        private async void InitializeFromFile(StorageFile file)
        {
            Name = file.DisplayName.Replace(".dbsx", "");
            Description = "";

            try
            {
                String xml = await FileIO.ReadTextAsync(file);
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var attr = doc.SelectNodes("/SymbolFile/Header/Description");
                if (attr.Count == 1)
                {
                    Description = attr.Item(0).InnerText;
                }

                //var attr2 = doc.SelectNodes("/SymbolFile/Header/CoordinateSpace");
                //if (attr2.Count == 1)
                //{
                //    IsModelSpace = attr2.Item(0).InnerText.ToLower() == "model";
                //}

                var attr1 = doc.SelectNodes("/SymbolFile/Header/Thumbnail");
                if (attr1.Count == 1)
                {
                    ThumbnailData = attr1.Item(0).InnerText;
                }
            }
            catch
            {
                Description = "";
                ThumbnailData = "";
            }
        }

        public SymbolPickerItem(Group g, string folder)
        {
            Group = g;
            Name = g.Name;
            Description = g.Description;
            Folder = folder;
        }

        public SymbolPickerItem(string name, string description, string folder)
        {
            Name = name;
            Description = description;
            Folder = folder;
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;

                if (_isSelected && OnSymbolSelected != null)
                {
                    SymbolSelectedEventArgs ee = new SymbolSelectedEventArgs(this);
                    OnSymbolSelected(this, ee);
                }
            }
        }
    }

    public class SymbolSelectedEventArgs : EventArgs
    {
        public SymbolSelectedEventArgs(SymbolPickerItem item)
        {
            SelectedItem = item;
        }

        public SymbolPickerItem SelectedItem { get; set; }
    }
}
