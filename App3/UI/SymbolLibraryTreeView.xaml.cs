using Cirros;
using Cirros.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using mux = Microsoft.UI.Xaml.Controls;

namespace CirrosUI.Symbols
{
    public sealed partial class SymbolLibraryTreeView : UserControl, INotifyPropertyChanged
    {
        public event FolderListChangedHandler OnFolderListChanged;
        public delegate void FolderListChangedHandler(object sender, EventArgs e);

        public event SelectedItemChangedHandler OnSelectedItemChanged;
        public delegate void SelectedItemChangedHandler(object sender, EventArgs e);

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaiseProperty(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private List<LibraryItem> _libraryItems = new List<LibraryItem>();
        private List<FolderItem> _folderItems = new List<FolderItem>();

        LibraryItem _flyoutTargetItem = null;

        private int _changeNumber = 0;

        public List<MenuFlyoutItemBase> FolderOptionItems { get; set; }
        public List<MenuFlyoutItemBase> SymbolOptionItems { get; set; }
        public List<MenuFlyoutItemBase> GroupOptionItems { get; set; }

        public SymbolLibraryTreeView()
        {
            this.InitializeComponent();

            _treeView.AllowDrop = false;
            _treeView.CanReorderItems = false;

            _treeView.ItemInvoked += _treeView_ItemInvoked;

            _treeView.LayoutUpdated += _treeView_LayoutUpdated;

            _treeView.Collapsed += _treeView_Collapsed;
            _treeView.Expanding += _treeView_Expanding;

            this.DataContext = this;
            UpdateFlyoutItems();

            this.Loaded += SymbolLibraryTreeView_Loaded;
        }

        private void _treeView_Expanding(mux.TreeView sender, mux.TreeViewExpandingEventArgs args)
        {
            if (args.Node is SymbolTreeViewNode node)
            {
                if (node.LibraryItem.Type == LibraryItem.LibraryItemType.Folder)
                {
                    FolderItem folderItem = _folderItems.Find(folder => folder.Path == node.Path);
                    if (folderItem != null)
                    {
                        folderItem.IsExpanded = true;
                    }
                }
            }
        }

        private void _treeView_Collapsed(mux.TreeView sender, mux.TreeViewCollapsedEventArgs args)
        {
            if (args.Node is SymbolTreeViewNode node)
            {
                if (node.LibraryItem.Type == LibraryItem.LibraryItemType.Folder)
                {
                    FolderItem folderItem = _folderItems.Find(folder => folder.Path == node.Path);
                    if (folderItem != null)
                    {
                        folderItem.IsExpanded = false;
                    }
                }
            }
        }

        private void _treeView_LayoutUpdated(object sender, object e)
        {
            if (_treeView.SelectedItems.Count != _selectionCount)
            {
                _selectionCount = _treeView.SelectedItems.Count;
                if (_selectionCount == 1 && _treeView.SelectedNode is SymbolTreeViewNode node)
                {
                    _currentLibraryItem = node.LibraryItem;
                    SelectedItemChanged();
                }
                else if (_selectionCount == 0)
                {
                    Deselect();
                }

                //foreach (object o in _treeView.SelectedNodes)
                //{

                //}
            }
        }

        int _selectionCount = 0;

        private LibraryItem _currentLibraryItem = null;

        public int ChangeNumber
        {
            get { return _changeNumber; }
        }

        public void IncrementChangeNumber()
        {
            _changeNumber++;
        }

        public LibraryItem CurrentLibraryItem
        {
            get { return _currentLibraryItem; }
        }

        public List<FolderItem> Folders
        {
            get { return _folderItems; }
        }

        private async void SymbolLibraryTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            _treeView.CanReorderItems = true;
            _treeView.CanDragItems = true;

            await GetFolderTreeAsync();
            UpdateFolderList();
            UpdateFlyoutItems();

            //if (_treeView.RootNodes.Count > 0 && _treeView.RootNodes[0].Content is LibraryItem drawingItem && drawingItem.FSObject == null)
            //{
            //    _treeView.Collapse(_treeView.RootNodes[0]);
            //}
        }

        public void UpdateFlyoutItems()
        {
            List<MenuFlyoutItemBase> folderlist = new List<MenuFlyoutItemBase>();

            MenuFlyoutItem renameItem = new MenuFlyoutItem();
            renameItem.Text = "Rename folder...";
            renameItem.Click += RenameMenuItem_Click;
            folderlist.Add(renameItem);

            MenuFlyoutItem deleteItem = new MenuFlyoutItem();
            deleteItem.Text = "Delete folder";
            deleteItem.Click += DeleteMenuItem_Click;
            folderlist.Add(deleteItem);

            MenuFlyoutItem addFolderItem = new MenuFlyoutItem();
            addFolderItem.Text = "Add subfolder";
            addFolderItem.Click += AddFolderMenuItem_Click;
            folderlist.Add(addFolderItem);

            MenuFlyoutSubItem moveToFolderItem = new MenuFlyoutSubItem();
            moveToFolderItem.Text = "Move folder to";
            folderlist.Add(moveToFolderItem);

            foreach (FolderItem folderItem in Folders)
            {
                if (folderItem.Path != "")
                {
                    MenuFlyoutItem flyoutItem = new MenuFlyoutItem();
                    flyoutItem.Click += MoveToFolderMenuItem_Click;
                    flyoutItem.Text = folderItem.Path;
                    flyoutItem.Tag = folderItem.Path;
                    moveToFolderItem.Items.Add(flyoutItem);
                }
            }

            FolderOptionItems = folderlist;

            List<MenuFlyoutItemBase> symbollist = new List<MenuFlyoutItemBase>();

            MenuFlyoutItem renameSymbolItem = new MenuFlyoutItem();
            renameSymbolItem.Text = "Rename symbol";
            renameSymbolItem.Click += RenameMenuItem_Click;
            symbollist.Add(renameSymbolItem);

            MenuFlyoutItem deleteSymbolItem = new MenuFlyoutItem();
            deleteSymbolItem.Text = "Delete symbol";
            deleteSymbolItem.Click += DeleteMenuItem_Click;
            symbollist.Add(deleteSymbolItem);

            MenuFlyoutSubItem moveSymbolToFolderItem = new MenuFlyoutSubItem();
            moveSymbolToFolderItem.Text = "Move symbol to folder";
            symbollist.Add(moveSymbolToFolderItem);

            foreach (FolderItem folderItem in Folders)
            {
                if (folderItem.Path != "")
                {
                    MenuFlyoutItem flyoutItem = new MenuFlyoutItem();
                    flyoutItem.Click += MoveToFolderMenuItem_Click;
                    flyoutItem.Text = folderItem.Path;
                    flyoutItem.Tag = folderItem.Path;
                    moveSymbolToFolderItem.Items.Add(flyoutItem);
                }
            }

            SymbolOptionItems = symbollist;

            List<MenuFlyoutItemBase> grouplist = new List<MenuFlyoutItemBase>();

            MenuFlyoutSubItem addGroupToFolderItem = new MenuFlyoutSubItem();
            addGroupToFolderItem.Text = "Add to folder";
            grouplist.Add(addGroupToFolderItem);

            foreach (FolderItem folderItem in Folders)
            {
                if (folderItem.Path != "")
                {
                    MenuFlyoutItem flyoutItem = new MenuFlyoutItem();
                    flyoutItem.Click += MoveToFolderMenuItem_Click;
                    flyoutItem.Text = folderItem.Path;
                    flyoutItem.Tag = folderItem.Path;
                    addGroupToFolderItem.Items.Add(flyoutItem);
                }
            }

            GroupOptionItems = grouplist;

            RaiseProperty(nameof(FolderOptionItems));
            RaiseProperty(nameof(SymbolOptionItems));
            RaiseProperty(nameof(GroupOptionItems));
        }

        //public void FoldersChanged()
        //{
        //    RaiseProperty(nameof(Folders));
        //}

        private void AddSymbolTreeNodes(SymbolTreeViewNode parent, LibraryItem item)
        {
            parent.Content = item.DisplayName;
            parent.LibraryItem = item;

            if (parent.LibraryItem.Type == LibraryItem.LibraryItemType.Folder && parent.LibraryItem.Children.Count > 0)
            {
                foreach (LibraryItem child in parent.LibraryItem.Children)
                {
                    SymbolTreeViewNode node = new SymbolTreeViewNode();
                    node.Content = child.DisplayName;
                    node.LibraryItem = child;

                    if (node.LibraryItem.Type == LibraryItem.LibraryItemType.Folder)
                    {
                        AddSymbolTreeNodes(node, child);
                    }

                    parent.Children.Add(node);
                }
            }

            if (item.Type == LibraryItem.LibraryItemType.Folder)
            {
                FolderItem folderItem = _folderItems.Find(folder => folder.Path == item.Path);
                if (folderItem != null)
                {
                    parent.IsExpanded = folderItem.IsExpanded;
                }
            }
        }

        public void FolderTreeDataChanged(LibraryItem start = null)
        {
            if (start == null)
            {
                _treeView.RootNodes.Clear();

                _libraryItems.Sort();

                foreach (LibraryItem item in _libraryItems)
                {
                    SymbolTreeViewNode node = new SymbolTreeViewNode();
                    AddSymbolTreeNodes(node, item);
                    _treeView.RootNodes.Add(node);
                }
            }
            else if (start.Type == LibraryItem.LibraryItemType.Folder && _treeView.RootNodes.Count == 2 && _treeView.RootNodes[1] is SymbolTreeViewNode rootNode)
            {
                SymbolTreeViewNode parentNode = FindNodeByPath(rootNode, start.Parent.Path);
            }
        }

        public async Task AddSubFolder(LibraryItem folder, string newFolderName)
        {
            if (folder.FSObject is StorageFolder parent)
            {
                StorageFolder newFolder = await parent.CreateFolderAsync(newFolderName, CreationCollisionOption.GenerateUniqueName);
                IncrementChangeNumber();

                folder.InsertChild(0, new LibraryItem(newFolder));
                UpdateFolderList();
                UpdateFlyoutItems();
            }
        }

        private async void AddFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_flyoutTargetItem != null && _flyoutTargetItem.Type == LibraryItem.LibraryItemType.Folder)
            {
                string newFolderName = "New folder";

                if (_flyoutTargetItem.FSObject is StorageFolder)
                {
                    await AddSubFolder(_flyoutTargetItem, newFolderName);
                    IncrementChangeNumber();
                }
            }
        }

        private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_flyoutTargetItem != null)
            {
                if (_flyoutTargetItem.Type == LibraryItem.LibraryItemType.File)
                {
                    // Show confirm delete dialog
                    ConfirmSymbolDeleteDialog dialog = new ConfirmSymbolDeleteDialog();
                    dialog.SymbolName = _flyoutTargetItem.DisplayName;
                    ContentDialogResult dialogResult = await dialog.ShowAsync();

                    if (dialogResult == ContentDialogResult.Primary)
                    {
                        // delete the file
                        await SymbolLibraryUtilities.DeleteLibraryItem(_flyoutTargetItem);

                        Deselect();
                    }
                }
                else if (_flyoutTargetItem.Type == LibraryItem.LibraryItemType.Folder)
                {
                    // Show confirm delete dialog
                    ConfirmFolderDeleteDialog dialog = new ConfirmFolderDeleteDialog();
                    dialog.FolderName = _flyoutTargetItem.DisplayName;
                    ContentDialogResult dialogResult = await dialog.ShowAsync();

                    if (dialogResult == ContentDialogResult.Primary)
                    {
                        // delete the folder
                        await SymbolLibraryUtilities.DeleteLibraryItem(_flyoutTargetItem);

                        UpdateFolderList();
                        UpdateFlyoutItems();
                        //FolderTreeDataChanged();

                        Deselect();
                    }
                }
            }
        }

        private void RenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_flyoutTargetItem != null && e.OriginalSource is FrameworkElement fe && _flyoutTargetItem != null)
            {
                // Show rename dialog
                _renameBox.Text = _flyoutTargetItem.DisplayName;
                _renameBox.Select(_renameBox.Text.Length, 0);

                _fly.SetValue(Canvas.LeftProperty, _flyoutPoint.X);
                _fly.SetValue(Canvas.TopProperty, _flyoutPoint.Y);
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)_fly);
            }
        }

        private async Task<bool> FileExists(StorageFolder folder, string name)
        {
            var item = await folder.TryGetItemAsync(name);
            return item != null;
        }

        private async void MoveToFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_flyoutTargetItem != null && sender is MenuFlyoutItem flyoutItem && flyoutItem.Tag is string folderPath)
            {
                foreach (FolderItem folderItem in Folders)
                {
                    if (folderItem.Path == folderPath)
                    {
                        if (folderItem.LibraryItem.FSObject is StorageFolder folder)
                        {
                            bool move = true;
                            if (await folder.TryGetItemAsync(_flyoutTargetItem.Name) != null)
                            {
                                // show dup dialog
                                if (_flyoutTargetItem.Type == LibraryItem.LibraryItemType.Folder)
                                {
                                    FolderExistsMergeDialog dialog = new FolderExistsMergeDialog();
                                    dialog.FolderName = _flyoutTargetItem.DisplayName;
                                    ContentDialogResult dialogResult = await dialog.ShowAsync();
                                    move = dialogResult == ContentDialogResult.Primary;
                                }
                                else
                                {
                                    FileExistsDialog dialog = new FileExistsDialog();
                                    dialog.SymbolName = _flyoutTargetItem.DisplayName;
                                    ContentDialogResult dialogResult = await dialog.ShowAsync();

                                    move = dialogResult == ContentDialogResult.Primary;
                                }
                            }

                            if (move)
                            {
                                await Symbols.SymbolLibraryUtilities.MoveLibraryItem(_flyoutTargetItem, folderItem.LibraryItem);

                                UpdateFolderList();
                                UpdateFlyoutItems();
                            }
                        }

                        Deselect();
                        IncrementChangeNumber();

                        break;
                    }
                }
            }
        }

        Point _flyoutPoint = new Point();

        private SymbolTreeViewNode FindNodeByPath(SymbolTreeViewNode node, string path)
        {
            SymbolTreeViewNode targetNode = null;

            foreach (SymbolTreeViewNode child in node.Children)
            {
                if (child.LibraryItem.Path == path)
                {
                    return child;
                }
                else if (child.HasChildren)
                {
                    foreach (SymbolTreeViewNode childNode in child.Children)
                    {
                        if (childNode.Path == path)
                        {
                            targetNode = childNode;
                            break;
                        }
                        else
                        {
                            targetNode = FindNodeByPath(childNode, path);

                            if (targetNode != null)
                            {
                                break;
                            }
                        }
                    }

                    if (targetNode != null)
                    {
                        break;
                    }
                }
            }

            return targetNode;
        }

        private void SelectNode(SymbolTreeViewNode node)
        {
            if (_selectionCount <= 1)
            {
                _currentLibraryItem = node.LibraryItem;
                SelectedItemChanged();
            }
            else
            {

            }
            if (node.LibraryItem.Type != LibraryItem.LibraryItemType.Folder)
            {
                _treeView.SelectedNode = node;
            }
        }

        private void Select(LibraryItem libraryItem)
        {
            if (libraryItem != null)
            {
                foreach (SymbolTreeViewNode node in _treeView.RootNodes)
                {
                    SymbolTreeViewNode targetNode = FindNodeByPath(node, libraryItem.Path);

                    if (targetNode != null)
                    {
                        SelectNode(targetNode);
                        //_treeView.SelectedNodes.Clear();
                        //_treeView.SelectedNodes.Add(targetNode);
                        break;
                    }
                }
            }
        }

        private LibraryItem FindLibraryItemByPath(LibraryItem start, string path)
        {
            if (start.Path == path)
            {
                return start;
            }
            if (start.Type == LibraryItem.LibraryItemType.Folder)
            {
                foreach (LibraryItem item in start.Children)
                {
                    if (item.Path == path)
                    {
                        return item;
                    }
                    if (item.Type == LibraryItem.LibraryItemType.Folder)
                    {
                        LibraryItem item1 = FindLibraryItemByPath(item, path);
                        if (item1 != null)
                        {
                            return item1;
                        }
                    }
                }
            }
            return null;
        }

        private LibraryItem FindLibraryItemByPath(string path)
        {
            LibraryItem item = null;

            foreach (LibraryItem root in _libraryItems)
            {
                if ((item = FindLibraryItemByPath(root, path)) != null)
                {
                    break;
                }
            }

            return item;
        }

        //private LibraryItem FindLibraryItemInTreeViewItem(FrameworkElement fe)
        //{
        //    LibraryItem item = null;

        //    if (fe is Grid grid && (grid.Parent is StackPanel) == false)
        //    {
        //        foreach (FrameworkElement child in grid.Children)
        //        {
        //            if (child is StackPanel stackPanel && stackPanel.Tag is string path)
        //            {
        //                if ((item = FindLibraryItemByPath(path)) != null)
        //                {
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (fe is StackPanel stackPanel && stackPanel.Tag is string path)
        //        {
        //            item = FindLibraryItemByPath(path);
        //        }
        //        else if (fe.Parent is FrameworkElement parent)
        //        {
        //            item = FindLibraryItemInTreeViewItem(parent);
        //        }
        //    }

        //    return item;
        //}

        public void RemoveGroupFromTreeView(LibraryItem item)
        {
            if (_treeView.RootNodes.Count > 0 && _treeView.RootNodes[0].Content is LibraryItem drawingItem && drawingItem.FSObject == null)
            {
                if (item != null && item.Type == LibraryItem.LibraryItemType.Group && drawingItem.Children.Contains(item))
                {
                    drawingItem.RemoveChild(item);

                    if (item == _currentLibraryItem)
                    {
                        _currentLibraryItem = null;
                        SelectedItemChanged();
                    }
                }
            }
        }

        private void Deselect()
        {
            if (_currentLibraryItem != null)
            {
                _currentLibraryItem = null;
                SelectedItemChanged();
            }
        }

        private void _treeView_ItemInvoked(mux.TreeView sender, mux.TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is SymbolTreeViewNode node)
            {
                SelectNode(node);
            }
        }

        private LibraryItem DrawingNamedGroups()
        {
            LibraryItem drawingRoot = new LibraryItem("Named groups in this drawing");

            foreach (Group group in Globals.ActiveDrawing.Groups.Values)
            {
                if (group.Name.StartsWith(":") == false)
                {
                    drawingRoot.AddChild(new LibraryItem(group));
                }
            }

            return drawingRoot;
        }

        private async Task GetFolderTreeAsync()
        {
            try
            {
                StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFolder symbolsFolder = await localFolder.CreateFolderAsync("Symbols", CreationCollisionOption.OpenIfExists);

                LibraryItem drawingRoot = DrawingNamedGroups();
                drawingRoot.DisplayName = "Named groups in this drawing";
                LibraryItem root = await SymbolLibraryUtilities.AddFolderContents(symbolsFolder, null);

                _libraryItems.Add(drawingRoot);
                _libraryItems.Add(root);
            }
            catch
            {
            }
        }

        private void AddFolders(List<FolderItem> list, LibraryItem folder, string parent)
        {
            foreach (LibraryItem item in folder.Children)
            {
                if (item.Type == LibraryItem.LibraryItemType.Folder)
                {
                    FolderItem newItem = new FolderItem(item);
                    FolderItem oldItem = _folderItems.Find(f => f.Path == item.Path);
                    if (oldItem != null)
                    {
                        newItem.IsExpanded = oldItem.IsExpanded;
                    }
                    list.Add(newItem);
                    AddFolders(list, item, item.Path);
                }
            }
        }

        public void UpdateFolderList()
        {
            List<FolderItem> newList = new List<FolderItem>();

            foreach (LibraryItem item in _libraryItems)
            {
                if (item.Type == LibraryItem.LibraryItemType.Folder)
                {
                    FolderItem newItem = new FolderItem(item);
                    FolderItem oldItem = _folderItems.Find(folder => folder.Path == item.Path);
                    if (oldItem != null)
                    {
                        newItem.IsExpanded = oldItem.IsExpanded;
                    }
                    newList.Add(newItem);
                    AddFolders(newList, item, item.Path);
                }
            }

            _folderItems = newList;

            FolderListChanged();
            FolderTreeDataChanged();
        }

        private void FolderListChanged()
        {
            if (OnFolderListChanged != null)
            {
                OnFolderListChanged(this, new EventArgs());
            }
        }

        private void SelectedItemChanged()
        {
            if (OnSelectedItemChanged != null)
            {
                OnSelectedItemChanged(this, new EventArgs());
            }
        }

        private void _fly_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)_fly);
        }

        private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint pp = e.GetCurrentPoint(_canvas);
            _fly.SetValue(Canvas.LeftProperty, pp.Position.X);
            _fly.SetValue(Canvas.TopProperty, pp.Position.Y);
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)_fly);
        }


        private async void _renameOk_Click(object sender, RoutedEventArgs e)
        {
            if (_flyoutTargetItem != null)
            {
                if (_flyoutTargetItem.DisplayName != _renameBox.Text)
                {
                    if (_renameBox.Text.Contains(".") || _renameBox.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        // invalid name
                    }
                    else if (_flyoutTargetItem.Parent.FSObject is StorageFolder folder)
                    {
                        bool rename = true;
                        string name = _flyoutTargetItem.Type == LibraryItem.LibraryItemType.Folder ? _renameBox.Text : _renameBox.Text + ".dbsx";
                        if (await folder.TryGetItemAsync(name) != null)
                        {
                            if (_flyoutTargetItem.Type == LibraryItem.LibraryItemType.Folder)
                            {
                                FolderExistsDialog dialog = new FolderExistsDialog();
                                dialog.FolderName = name;
                                ContentDialogResult dialogResult = await dialog.ShowAsync();

                                rename = false;
                            }
                            else
                            {
                                FileExistsDialog dialog = new FileExistsDialog();
                                dialog.SymbolName = _renameBox.Text;
                                ContentDialogResult dialogResult = await dialog.ShowAsync();

                                rename = dialogResult == ContentDialogResult.Primary;
                            }
                        }
                        
                        if (rename)
                        {
                            await SymbolLibraryUtilities.RenameLibraryItem(_flyoutTargetItem, _renameBox.Text);

                            UpdateFolderList();
                            UpdateFlyoutItems();

                            Deselect();
                            IncrementChangeNumber();

                            _renameFlyout.Hide();
                        }
                    }
                }
                else
                {
                    _renameFlyout.Hide();
                }
            }
            else
            {
                _renameFlyout.Hide();
            }
        }

        private void _renameCancel_Click(object sender, RoutedEventArgs e)
        {
            _renameFlyout.Hide();
        }

        private async void _treeView_DragItemsCompleted(mux.TreeView sender, mux.TreeViewDragItemsCompletedEventArgs args)
        {
            if (args.Items.Count == 1 && args.Items[0] is SymbolTreeViewNode node)
            {
                LibraryItem item = node.LibraryItem;

                if (args.NewParentItem is SymbolTreeViewNode parentNode)
                {
                    LibraryItem parent = parentNode.LibraryItem;

                    if (parent.Type == LibraryItem.LibraryItemType.Folder && parent.FSObject is StorageFolder folder)
                    {
                        if (item.Parent == parent)
                        {
                            // node was dropped in it's own folder - nothing to do 
                        }
                        else if (item.Type == LibraryItem.LibraryItemType.File && item.FSObject is StorageFile file)
                        {
                            // a symbol was dropped into a folder
                            // move the symbol to the folder
                            // if duplicate, rename
                            // fix parent link, fsobject and path in library item

                            string name = await SymbolLibraryUtilities.UniqueName(item.Name, folder);
                            await file.MoveAsync(folder, name, NameCollisionOption.GenerateUniqueName);
                            item.Parent = parent;
                            item.Path = parent.Path + "/" + item.Name;
                        }
                        else if (item.Type == LibraryItem.LibraryItemType.Folder)
                        {
                            // a folder was dropped into a different folder
                            // recursively move the folder contents to the new folder
                            // rename duplicate items
                            // fix parent link and path in library item

                            if (parent.Children.Contains(item))
                            {
                                parent.RemoveChild(item);
                            }

                            await SymbolLibraryUtilities.MoveLibraryItem(item, parent);
                            UpdateFolderList();
                            UpdateFlyoutItems();
                        }
                        else if (item.Type == LibraryItem.LibraryItemType.Group)
                        {
                            // a group was dropped into a folder
                            // copy the group to the folder
                            // if duplicate, rename

                        }
                    }
                    else
                    {
                        // a LibraryItem was dropped onto a file - this shouldn't happen
                    }
                }
            }
        }

        private void _treeView_DragItemsStarting(mux.TreeView sender, mux.TreeViewDragItemsStartingEventArgs args)
        {
            if (args.Items.Count == 1 && args.Items[0] is SymbolTreeViewNode node)
            {
                if (node.LibraryItem.Parent == null)
                {
                    // root node - can't be moved
                    args.Cancel = true;
                }
                else if (node.LibraryItem.Parent.Path == "")
                {
                    // drawing groups can't be moved
                    args.Cancel = true;
                }
            }
        }

        private void TreeViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is mux.TreeViewItem item)
            {
                if (item.DataContext is SymbolTreeViewNode node)
                {
                    if (node.LibraryItem.Type == LibraryItem.LibraryItemType.Folder)
                    {
                        item.AllowDrop = node.LibraryItem.Path != "";
                    }
                    else if (node.LibraryItem.Type == LibraryItem.LibraryItemType.Group)
                    {
                        item.AllowDrop = false;
                    }
                    else
                    {
                        item.AllowDrop = false;
                    }

                    item.RightTapped += Item_RightTapped;
                }
            }
        }

        private void Item_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is mux.TreeViewItem item && item.DataContext is SymbolTreeViewNode node)
            {
                if (_selectionCount <= 1)
                {
                    Deselect();
                    IncrementChangeNumber();

                    _flyoutTargetItem = FindLibraryItemByPath(node.Path);
                    _flyoutPoint = e.GetPosition(_treeView);

                    if (_flyoutTargetItem != null)
                    {
                        Select(_flyoutTargetItem);

                        if (_flyoutTargetItem.Type == LibraryItem.LibraryItemType.Folder)
                        {
                            if (_flyoutTargetItem.Parent == null)
                            {
                                if (_treeView.RootNodes.Count > 1)
                                {
                                    if (_treeView.RootNodes[0] is SymbolTreeViewNode groupRoot && _flyoutTargetItem == groupRoot.LibraryItem)
                                    {
                                    }
                                    else if (_treeView.RootNodes[1] is SymbolTreeViewNode symbolRoot && _flyoutTargetItem == symbolRoot.LibraryItem)
                                    {
                                    }
                                }
                            }
                            else
                            {
                                _treeViewFolderMenuFlyout.ShowAt(_treeView, _flyoutPoint);
                            }
                        }
                        else if (_flyoutTargetItem.Type == LibraryItem.LibraryItemType.Group)
                        {
                            _treeViewGroupMenuFlyout.ShowAt(_treeView, _flyoutPoint);
                        }
                        else
                        {
                            _treeViewSymbolMenuFlyout.ShowAt(_treeView, _flyoutPoint);
                        }
                    }
                }
                else
                {

                }
            }
        }
    }

    public class SymbolTreeViewNode : mux.TreeViewNode
    {
        public LibraryItem LibraryItem;
        public string Path { get { return LibraryItem.Path; } }
    }
}
