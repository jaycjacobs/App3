using Cirros;
using Cirros.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls;

namespace CirrosUI.Symbols
{
    public class LibraryItem : INotifyPropertyChanged, IComparable
    {
        private bool _isExpanded;
        private bool _isSelected;
        private string _name;

        private ObservableCollection<LibraryItem> _children;

        public event PropertyChangedEventHandler PropertyChanged;
        public enum LibraryItemType { Folder, File, Group, Invalid };
        public string Path { get; set; }
        public LibraryItem Parent { get; set; }
        public object FSObject { get; set; }
        public Group Group { get; set; }
        public bool IsModelSpace { get; set; }
        public string FolderCount { get; set; }
        public string Description { get; set; }
        public BitmapImage Thumbnail { get; set; }
        public LibraryItemType Type { get; set; }

        public LibraryItem(StorageFolder storageFolder)
        {
            Type = LibraryItemType.Folder;
            Name = storageFolder.DisplayName;
            Path = storageFolder.DisplayName;

            FSObject = storageFolder;
            Group = null;

            _children = new ObservableCollection<LibraryItem>();
        }

        public LibraryItem(string name)
        {
            Type = LibraryItemType.Folder;
            Name = name;
            Path = "";

            FSObject = null;

            _children = new ObservableCollection<LibraryItem>();
        }

        public LibraryItem(Group group)
        {
            Type = LibraryItemType.Group;
            Name = group.Name + ".dbsx";
            Path = "";

            FSObject = null;
            Group = group;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        private bool _initialized = false;

        public async Task Initialize()
        {
            if (_initialized == false)
            {
                if (FSObject is StorageFolder folder)
                {
                    DisplayName = folder.DisplayName;
                    Thumbnail = null;
                }
                else if (FSObject is StorageFile file && file.Name.EndsWith(".dbsx"))
                {
                    _displayName = file.DisplayName;

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

                        var attr2 = doc.SelectNodes("/SymbolFile/Header/CoordinateSpace");
                        if (attr2.Count == 1)
                        {
                            IsModelSpace = attr2.Item(0).InnerText.ToLower() == "model";
                        }

                        var attr1 = doc.SelectNodes("/SymbolFile/Header/Thumbnail");
                        if (attr1.Count == 1)
                        {
                            string b64 = attr1.Item(0).InnerText;
                            if (b64.StartsWith("data:image/png;base64,"))
                            {
                                b64 = b64.Substring(22);
                                using (var stream = new MemoryStream(Convert.FromBase64String(b64)))
                                {
                                    BitmapImage src = new BitmapImage();
                                    await src.SetSourceAsync(stream.AsRandomAccessStream());
                                    Thumbnail = src;
                                }
                            }
                        }
                    }
                    catch 
                    {
                        Description = "";
                        Thumbnail = null;
                        Type = LibraryItemType.Invalid;
                    }
                }
                else if (Group != null)
                {
                    _displayName = Group.Name;

                    Description = Group.Description;
                    IsModelSpace = Group.CoordinateSpace == CoordinateSpace.Model;

                    StorageFile thumbfile = await FileHandling.GetGroupThumbnail(Group);
                    if (thumbfile != null)
                    {
                        using (IRandomAccessStream stream = await thumbfile.OpenAsync(FileAccessMode.Read))
                        {
                            BitmapImage src = new BitmapImage();
                            src.SetSource(stream);
                            Thumbnail = src;
                        }
                    }
                }

                _initialized = true;
            }
        }

        public LibraryItem(StorageFile storageFile)
        {
            Type = LibraryItemType.File;
            Name = storageFile.Name;
            Path = storageFile.Name;

            FSObject = storageFile;
            Group = null;
        }

        public void AddChild(LibraryItem item)
        {
            item.Parent = this;
            item.Path = $"{this.Path}/{item.Name}";

            if (Type == LibraryItemType.Folder && _children != null)
            {
                _children.Add(item);
            }
        }

        public void InsertChild(int index, LibraryItem item)
        {
            item.Parent = this;
            item.Path = $"{this.Path}/{item.Name}";

            if (Type == LibraryItemType.Folder && _children != null)
            {
                if (_children.Count > index)
                {
                    _children.Insert(index, item);
                }
                else
                {
                    _children.Add(item);
                }

                NotifyPropertyChanged("Children");
            }
        }

        public void RemoveChild(LibraryItem item)
        {
            if (_children.Contains(item))
            {
                _children.Remove(item);

                NotifyPropertyChanged("DisplayName");
                NotifyPropertyChanged("Name");
                NotifyPropertyChanged("Children");
            }
        }

        public void UpdateChild(LibraryItem item)
        {
            if (_children.Contains(item))
            {
                int index = _children.IndexOf(item);
                _children.Remove(item);
                _children.Insert(index, item);

                NotifyPropertyChanged("DisplayName");
                NotifyPropertyChanged("Name");
                NotifyPropertyChanged("Children");
            }
        }

        public void ReplaceChild(LibraryItem olditem, LibraryItem newitem)
        {
            if (_children.Contains(olditem))
            {
                int index = _children.IndexOf(olditem);
                _children.Remove(olditem);

                newitem.Parent = this;
                newitem.Path = $"{this.Path}/{newitem.Name}";
                _children.Insert(index, newitem);
                //AddChild(newitem);

                NotifyPropertyChanged("DisplayName");
                NotifyPropertyChanged("Name");
                NotifyPropertyChanged("Children");
            }
        }

        public LibraryItem FindChild(string name)
        {
            foreach (LibraryItem item in _children)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }

            return null;
        }

        string _displayName = null;

        public string DisplayName
        {
            get
            {
                if (_displayName == null)
                {
                    _displayName = _name.EndsWith(".dbsx") ? _name.Substring(0, _name.Length - 5) : _name;
                }
                return _displayName;
            }
            set
            {
                _displayName = value;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                _displayName = null;

                if (Type == LibraryItemType.Folder && Parent is LibraryItem)
                {
                    Path = $"{Parent.Path}/{_name}";
                }
            }
        }

        public ObservableCollection<LibraryItem> Children
        {
            get
            {
                if (Type == LibraryItemType.Folder && _children == null)
                {
                    _children = new ObservableCollection<LibraryItem>();
                }

                return _children;
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }

            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }

        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is LibraryItem that)
            {
                return this.Path.CompareTo(that.Path);
            }
            return 0;
        }
    }

    public class LibraryItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object o)
        {
            DataTemplate t = null;

            if (o is SymbolTreeViewNode node)
            {
                if (node.LibraryItem.Type == LibraryItem.LibraryItemType.Folder)
                {
                    t = FolderTemplate;
                    //System.Diagnostics.Debug.WriteLine($"{node.LibraryItem.DisplayName}: Folder");
                }
                else
                {
                    t = FileTemplate;
                    //System.Diagnostics.Debug.WriteLine($"{node.LibraryItem.DisplayName}: File");
                }

                // Without this delay the template/icon selection is not reliable
                Task.Delay(2);
            }
            return  t;
        }
    }
}
