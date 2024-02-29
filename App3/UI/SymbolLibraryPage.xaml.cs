using Cirros;
using Cirros.Alerts;
using Cirros.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CirrosUI.Symbols
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SymbolLibraryPage : Page
    {
        LibraryItem _selectedItem = null;

        public SymbolLibraryPage()
        {
            this.InitializeComponent();

            var bounds = App.Window.Bounds;

            _topView.Width = bounds.Width;
            _topView.Height = bounds.Height;

            _symbolProperties.Visibility = Visibility.Collapsed;
            _folderProperties.Visibility = Visibility.Collapsed;

            _treeView.OnSelectedItemChanged += _treeView_OnSelectedItemChanged;
            _treeView.OnFolderListChanged += _treeView_OnFolderListChanged;

            _treeViewGrid.PointerPressed += _treeViewGrid_PointerPressed;
        }

        private async void _treeViewGrid_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            await Deselect();
        }

        private void _treeView_OnFolderListChanged(object sender, EventArgs e)
        {
            _folderParentComboBox.Items.Clear();
            _symbolFolderComboBox.Items.Clear();

            foreach (FolderItem folderItem in _treeView.Folders)
            {
                if (folderItem.LibraryItem.Path != "")
                {
                    _folderParentComboBox.Items.Add(folderItem);
                    _symbolFolderComboBox.Items.Add(folderItem);
                }
            }
        }

        public bool IsDirty
        {
            get
            {
                return _updateButton.IsEnabled || _updateFolderButton.IsEnabled;
            }
            set
            {
                _updateFolderButton.IsEnabled = value;
                _updateButton.IsEnabled = value;

                _treeView.IsEnabled = value == false;
            }
        }

        public bool FolderIsDirty
        {
            get
            {
                return _updateFolderButton.IsEnabled;
            }
            set
            {
                if (value)
                {
                    _updateFolderButton.IsEnabled = true;
                    _treeView.IsEnabled = false;
                }
                else
                {
                    IsDirty = false;
                }
            }
        }

        public bool SymbolIsDirty
        {
            get
            {
                return _updateButton.IsEnabled;
            }
            set
            {
                if (value)
                {
                    _updateButton.IsEnabled = true;
                    _treeView.IsEnabled = false;
                }
                else
                {
                    IsDirty = false;
                }
            }
        }

        private async Task Deselect()
        {
            if (SymbolIsDirty)
            {
                ApplyChangesDialog dialog = new ApplyChangesDialog("symbol");
                ContentDialogResult dialogResult = await dialog.ShowAsync();

                if (dialogResult == ContentDialogResult.Primary)
                {
                    await ApplySymbolChanges();
                }
            }
            else if (FolderIsDirty)
            {
                ApplyChangesDialog dialog = new ApplyChangesDialog("folder");
                ContentDialogResult dialogResult = await dialog.ShowAsync();

                if (dialogResult == ContentDialogResult.Primary)
                {
                    await ApplyFolderChanges();
                }
            }

            IsDirty = false;

            _selectedItem = null;

            _symbolProperties.Visibility = Visibility.Collapsed;
            _folderProperties.Visibility = Visibility.Collapsed;
        }

        private double ScaleFromMatrix(Matrix m)
        {
            double scale = 1;

            if (Math.Abs(m.M11) == Math.Abs(m.M22) && m.M12 == 0 && m.M21 == 0)
            {
                scale = Math.Abs(m.M11);
            }
            return scale;
        }

        private void UpdateSymbolSize()
        {
            if (_selectedItem.Type == LibraryItem.LibraryItemType.Group && _selectedItem.Group != null)
            {
                double width = _selectedItem.Group.ModelBounds.Width;
                double height = _selectedItem.Group.ModelBounds.Height;

            }
        }

        private async Task SelectLibraryItem(LibraryItem item)
        {
            await Deselect();

            if (item != null)
            {
                _selectedItem = item;
                
                // Initialize the Library (if necessary)
                await _selectedItem.Initialize();

                if (_selectedItem.Parent == null)
                {
                    // top level folder

                    if (_selectedItem.Type == LibraryItem.LibraryItemType.Folder)
                    {
                        _addSubfolderButton.Visibility = _selectedItem.FSObject is StorageFolder ? Visibility.Visible : Visibility.Collapsed;

                        _symbolProperties.Visibility = Visibility.Collapsed;
                        _folderProperties.Visibility = Visibility.Visible;
                        _folderNameBox.Text = _selectedItem.Name;

                        _folderParentComboBox.SelectedItem = null;
                        _folderParentComboBox.IsEnabled = false;
                        _folderNameBox.IsEnabled = false;

                        _removeOrphansButton.Visibility = Visibility.Visible;
                        _deleteFolderButton.Visibility = Visibility.Collapsed;
                        _updateFolderButton.Visibility = Visibility.Collapsed;

                        _symbolThumbnail.Source = null;
                    }
                }
                else
                {
                    foreach (FolderItem folder in _treeView.Folders)
                    {
                        if (folder.Path == _selectedItem.Parent.Path)
                        {
                            if (_selectedItem.Type == LibraryItem.LibraryItemType.Folder)
                            {
                                if (_selectedItem.FSObject is StorageFolder)
                                {
                                    _addSubfolderButton.Visibility =  Visibility.Visible;
                                    _removeOrphansButton.Visibility = Visibility.Collapsed;
                                }
                                else
                                {
                                    _addSubfolderButton.Visibility = Visibility.Collapsed;
                                    _removeOrphansButton.Visibility = Visibility.Visible;
                                }

                                _symbolProperties.Visibility = Visibility.Collapsed;
                                _folderProperties.Visibility = Visibility.Visible;
                                _folderNameBox.Text = _selectedItem.Name;

                                _folderParentComboBox.SelectedItem = folder;
                                _folderParentComboBox.IsEnabled = true;
                                _folderNameBox.IsEnabled = true;

                                _deleteFolderButton.IsEnabled = true;
                                _removeOrphansButton.Visibility = Visibility.Collapsed;
                                _deleteFolderButton.Visibility = Visibility.Visible;
                                _updateFolderButton.Visibility = Visibility.Visible;

                                _symbolThumbnail.Source = null;

                                FolderIsDirty = false;
                            }
                            else
                            {
                                if (_selectedItem.Type == LibraryItem.LibraryItemType.Group)
                                {
                                    // Group from drawing
                                    _groupPrescaleLabel.Visibility = Visibility.Visible;
                                    _groupPrescaleComboBox.Visibility = Visibility.Visible;
                                    _groupSizeLabel.Visibility = Visibility.Visible;
                                    _groupSizeTextBlock.Visibility = Visibility.Visible;
                                    _symbolTitlePanel.Visibility = Visibility.Collapsed;
                                    _symbolFolderLabel.Visibility = Visibility.Collapsed;
                                    _groupTitlePanel.Visibility = Visibility.Visible;
                                    _symbolAdded.Visibility = Visibility.Collapsed;
                                    _groupFolderLabel.Visibility = Visibility.Visible;
                                    _folderProperties.Visibility = Visibility.Collapsed;
                                    _symbolProperties.Visibility = Visibility.Visible;
                                    _updateButton.Visibility = Visibility.Collapsed;
                                    _deleteSymbolButton.Visibility = Visibility.Collapsed;
                                    _addSymbolButton.Visibility = Visibility.Visible;
                                    _removeSymbolButton.Visibility = Visibility.Visible;
                                    _addSymbolButton.IsEnabled = false;
                                    _symbolFolderComboBox.SelectedItem = null;

                                    _symbolDescriptionBox.IsEnabled = false;
                                    _symbolNameBox.IsEnabled = false;
                                    _symbolSpaceFolderComboBox.IsEnabled = false;

                                    List<double> prescales = new List<double>()
                                    {
                                        1
                                    };

                                    int instanceCount = Globals.ActiveDrawing.CountGroupInstances(_selectedItem.Group.Name, false);
                                    _removeSymbolButton.IsEnabled = instanceCount == 0;
                                    _groupInstanceCountBox.Text = instanceCount.ToString();

                                    if (instanceCount > 0)
                                    {
                                        List<PInstance> instances = Globals.ActiveDrawing.GetGroupInstances(_selectedItem.Group.Name);

                                        foreach (PInstance instance in instances)
                                        {
                                            double scale = ScaleFromMatrix(instance.Matrix);
                                            if (prescales.Contains(scale) == false)
                                            {
                                                prescales.Add(scale);
                                            }
                                        }
                                    }

                                    _groupPrescaleComboBox.SetValues(prescales);
                                    if (prescales.Count > 0)
                                    {
                                        _groupPrescaleComboBox.SelectedIndex = prescales.Count - 1;
                                    }
                                }
                                else
                                {
                                    // Symbol file from library

                                    _groupPrescaleLabel.Visibility = Visibility.Collapsed;
                                    _groupPrescaleComboBox.Visibility = Visibility.Collapsed;
                                    _groupSizeLabel.Visibility = Visibility.Collapsed;
                                    _groupSizeTextBlock.Visibility = Visibility.Collapsed;
                                    _groupTitlePanel.Visibility = Visibility.Collapsed;
                                    _groupFolderLabel.Visibility = Visibility.Collapsed;
                                    _symbolTitlePanel.Visibility = Visibility.Visible;
                                    _symbolFormatError.Visibility = _selectedItem.Type == LibraryItem.LibraryItemType.Invalid ? Visibility.Visible : Visibility.Collapsed;
                                    _symbolFolderLabel.Visibility = Visibility.Visible;
                                    _folderProperties.Visibility = Visibility.Collapsed;
                                    _symbolProperties.Visibility = Visibility.Visible;
                                    _symbolFolderComboBox.SelectedItem = folder;
                                    _updateButton.Visibility = Visibility.Visible;
                                    _deleteSymbolButton.IsEnabled = true;
                                    _deleteSymbolButton.Visibility = Visibility.Visible;
                                    _addSymbolButton.Visibility = Visibility.Collapsed;
                                    _removeSymbolButton.Visibility = Visibility.Collapsed;

                                    _symbolDescriptionBox.IsEnabled = true;
                                    _symbolNameBox.IsEnabled = true;
                                    _symbolSpaceFolderComboBox.IsEnabled = true;

                                    SymbolIsDirty = false;
                                }

                                _symbolDescriptionBox.Text = _selectedItem.Description;
                                _symbolNameBox.Text = _selectedItem.DisplayName;
                                _symbolThumbnail.Source = _selectedItem.Thumbnail;
                                _symbolSpaceFolderComboBox.SelectedIndex = _selectedItem.IsModelSpace ? 1 : 0;
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                _selectedItem = null;

                // no item is selected
                _symbolProperties.Visibility = Visibility.Collapsed;
                _folderProperties.Visibility = Visibility.Collapsed;
            }
        }

        private async void _treeView_OnSelectedItemChanged(object sender, EventArgs e)
        {
            await SelectLibraryItem(_treeView.CurrentLibraryItem);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Parent is Popup popup)
            {
                popup.IsOpen = false;
            }
        }

        private async void _deleteSymbolButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem != null)
            {
                if (_selectedItem.Type == LibraryItem.LibraryItemType.File || _selectedItem.Type == LibraryItem.LibraryItemType.Invalid)
                {
                    ConfirmSymbolDeleteDialog dialog = new ConfirmSymbolDeleteDialog();
                    dialog.SymbolName = _selectedItem.DisplayName;
                    ContentDialogResult dialogResult = await dialog.ShowAsync();

                    if (dialogResult == ContentDialogResult.Primary)
                    {
                        // delete the symbol
                        await SymbolLibraryUtilities.DeleteLibraryItem(_selectedItem);
                        _treeView.IncrementChangeNumber();

                        await Deselect();
                    }
                }
            }
        }

        private async Task ApplySymbolChanges()
        {
            if (_selectedItem != null &&
                (_selectedItem.Type == LibraryItem.LibraryItemType.File || _selectedItem.Type == LibraryItem.LibraryItemType.Group) &&
                _selectedItem.Parent is LibraryItem parent)
            {
                if (_symbolNameBox.Text.Contains(".") || _symbolNameBox.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    AlertDialog dialog = new AlertDialog("Invalid Name", "The name contains invalid characters");
                    ContentDialogResult dialogResult = await dialog.ShowAsync();

                    _symbolNameBox.Text = _selectedItem.DisplayName;
                }
                else
                {
                    if (_selectedItem.Description != _symbolDescriptionBox.Text)
                    {
                        // change symbol description
                        _selectedItem.Description = _symbolDescriptionBox.Text;
                    }

                    if (_selectedItem.Type == LibraryItem.LibraryItemType.File && _symbolFolderComboBox.SelectedValue is FolderItem folderItem)
                    {
                        string filename = _symbolNameBox.Text + ".dbsx";
                        bool move = _selectedItem.Parent.Path != folderItem.Path || filename != _selectedItem.Name;

                        if (move && folderItem.LibraryItem.FSObject is StorageFolder folder)
                        {
                            if (await folder.TryGetItemAsync(filename) != null)
                            {
                                FileExistsDialog dialog = new FileExistsDialog();
                                dialog.SymbolName = _symbolNameBox.Text;
                                ContentDialogResult dialogResult = await dialog.ShowAsync();

                                move = dialogResult == ContentDialogResult.Primary;
                            }
                        }

                        if (move)
                        {
                            // rename and/or move
                            await Symbols.SymbolLibraryUtilities.MoveLibraryItem(_selectedItem, folderItem.LibraryItem, _symbolNameBox.Text);
                        }
                        else
                        {
                            await SelectLibraryItem(_selectedItem);
                        }
                    }
                    await SymbolLibraryUtilities.UpdateGroupFromLibraryItem(_selectedItem);

                    _treeView.FolderTreeDataChanged();
                    _treeView.IncrementChangeNumber();
                }
            }

            SymbolIsDirty = false;
        }

        private async void _updateButton_Click(object sender, RoutedEventArgs e)
        {
            await ApplySymbolChanges();
        }

        private async void _deleteFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem != null &&
                _selectedItem.Type == LibraryItem.LibraryItemType.Folder)
            {
                ConfirmFolderDeleteDialog dialog = new ConfirmFolderDeleteDialog();
                dialog.FolderName = _selectedItem.DisplayName;
                ContentDialogResult dialogResult = await dialog.ShowAsync();

                if (dialogResult == ContentDialogResult.Primary)
                {
                    // delete the folder
                    await SymbolLibraryUtilities.DeleteLibraryItem(_selectedItem);
                    await Deselect();
                    _treeView.IncrementChangeNumber();
                }
            }
        }

        private async Task ApplyFolderChanges()
        {
            if (FolderIsDirty)
            {
                FolderIsDirty = false;

                // check for invalid name
                if (_folderNameBox.Text.Contains(".") || _folderNameBox.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    AlertDialog dialog = new AlertDialog("Invalid Name", "The name contains invalid characters");
                    ContentDialogResult dialogResult = await dialog.ShowAsync();

                    _folderNameBox.Text = _selectedItem.DisplayName;
                }
                else if (_selectedItem != null &&
                    _selectedItem.Type == LibraryItem.LibraryItemType.Folder &&
                    _selectedItem.Parent is LibraryItem parent)
                {
                    if (_folderParentComboBox.SelectedValue is FolderItem folderItem && folderItem.LibraryItem.FSObject is StorageFolder folder)
                    {
                        string foldername = _folderNameBox.Text;
                        bool exists = await folder.TryGetItemAsync(foldername) != null;
                        LibraryItem item = _selectedItem;

                        if (folderItem.Path == parent.Path)
                        {
                            if (exists)
                            {
                                FolderExistsDialog dialog = new FolderExistsDialog();
                                dialog.FolderName = _folderNameBox.Text;
                                ContentDialogResult dialogResult = await dialog.ShowAsync();
                            }
                            else
                            {
                                // rename folder - new parent path is the same as the existing path
                                item = await Symbols.SymbolLibraryUtilities.RenameLibraryItem(_selectedItem, _folderNameBox.Text);
                            }
                        }
                        else
                        {
                            bool move = true;

                            if (exists)
                            {
                                FolderExistsMergeDialog dialog = new FolderExistsMergeDialog();
                                dialog.FolderName = _folderNameBox.Text;
                                ContentDialogResult dialogResult = await dialog.ShowAsync();
                                move = dialogResult == ContentDialogResult.Primary;
                            }

                            if (move)
                            {
                                // move folder - new parent path is not the same as the existing path
                                item = await Symbols.SymbolLibraryUtilities.MoveLibraryItem(_selectedItem, folderItem.LibraryItem, _folderNameBox.Text);
                            }
                        }

                        _treeView.UpdateFolderList();
                        _treeView.UpdateFlyoutItems();

                        await SelectLibraryItem(item);
                        _treeView.IncrementChangeNumber();
                    }
                    else if (_folderParentComboBox.SelectedValue == null)
                    {
                        // no selected folder in control
                    }
                }
            }
        }

        private async void _updateFolderButton_Click(object sender, RoutedEventArgs e)
        {
            await ApplyFolderChanges();
        }

        private void _symbol_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedItem != null &&
                (_selectedItem.Type == LibraryItem.LibraryItemType.File || _selectedItem.Type == LibraryItem.LibraryItemType.Group) &&
                SymbolIsDirty == false)
            {
                bool dirty = false;

                if (sender == _symbolNameBox)
                {
                    dirty = _selectedItem.DisplayName != _symbolNameBox.Text;
                }
                if (dirty == false && sender == _symbolDescriptionBox)
                {
                    dirty = _selectedItem.Description != _symbolDescriptionBox.Text;
                }

                SymbolIsDirty = dirty;
            }
        }

        private void _symbolFolderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedItem != null &&
                _selectedItem.Parent is LibraryItem parent &&
                SymbolIsDirty == false)
            {
                if (_symbolFolderComboBox.SelectedValue is FolderItem folderItem)
                {
                    if (_selectedItem.Type == LibraryItem.LibraryItemType.Group)
                    {
                        _addSymbolButton.IsEnabled = true;
                    }
                    else if (_selectedItem.Type == LibraryItem.LibraryItemType.File)
                    {
                        SymbolIsDirty = folderItem.Path != parent.Path;
                    }
                }
            }
        }

        private void _symbolSpaceFolderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedItem != null &&
               (_selectedItem.Type == LibraryItem.LibraryItemType.File || _selectedItem.Type == LibraryItem.LibraryItemType.Group)
               )
            {
                if (_symbolSpaceFolderComboBox.SelectedValue is ComboBoxItem item && item.Tag is string text)
                {
                    if (_selectedItem.IsModelSpace != (text == "model"))
                    {
                        _selectedItem.IsModelSpace = text == "model";
                        SymbolIsDirty = true;
                    }
                }
            }
        }

        private void _folderParentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedItem != null &&
                _selectedItem.Type == LibraryItem.LibraryItemType.Folder &&
                _selectedItem.Parent is LibraryItem parent &&
                FolderIsDirty == false)
            {
                if (_folderParentComboBox.SelectedValue is FolderItem folderItem)
                {
                    FolderIsDirty = folderItem.Path != parent.Path;
                }
            }
        }

        private void _folderNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedItem != null &&
                _selectedItem.Type == LibraryItem.LibraryItemType.Folder &&
                FolderIsDirty == false)
            {
                if (sender == _folderNameBox)
                {
                    FolderIsDirty = _selectedItem.DisplayName != _folderNameBox.Text;
                }
            }
        }

        private async void _addSymbolButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem != null &&
                _selectedItem.Type == LibraryItem.LibraryItemType.Group &&
                _selectedItem.Group != null &&
                _symbolFolderComboBox.SelectedValue is FolderItem folderItem
                )
            {
                string filename = _selectedItem.Group.Name + ".dbsx";
                if (folderItem.LibraryItem.FSObject is StorageFolder folder)
                {
                    bool add = true;

                    if (await folder.TryGetItemAsync(filename) != null)
                    {
                        FileExistsDialog dialog = new FileExistsDialog();
                        dialog.SymbolName = _selectedItem.DisplayName;
                        ContentDialogResult dialogResult = await dialog.ShowAsync();

                        add = dialogResult == ContentDialogResult.Primary;
                    }

                    if (add)
                    {
#if true
                        await Symbols.SymbolLibraryUtilities.MoveLibraryItem(_selectedItem, folderItem.LibraryItem);

                        _treeView.IncrementChangeNumber();

                        _symbolFolderComboBox.SelectedItem = null;
                        _addSymbolButton.IsEnabled = false;
                        _symbolAddedFolderName.Text = folderItem.LibraryItem.DisplayName;
                        _symbolAdded.Visibility = Visibility.Visible;

                        if (_selectedItem.FSObject is StorageFile file && _groupPrescaleComboBox.Value != 1)
                        {
                            await WrapAndScaleSymbol(file, _groupPrescaleComboBox.Value);
                        }
#else
                        if (_groupPrescaleComboBox.Value != 1)
                        {
                            Group wrap = WrapAndScaleSymbol(_selectedItem.Group, _groupPrescaleComboBox.Value);
                            LibraryItem newItem = new LibraryItem(wrap);
                            await Symbols.SymbolLibraryUtilities.MoveLibraryItem(newItem, folderItem.LibraryItem);
                        }
                        else
                        {
                            await Symbols.SymbolLibraryUtilities.MoveLibraryItem(_selectedItem, folderItem.LibraryItem);
                        }

                        _treeView.IncrementChangeNumber();

                        _symbolFolderComboBox.SelectedItem = null;
                        _addSymbolButton.IsEnabled = false;
                        _symbolAddedFolderName.Text = folderItem.LibraryItem.DisplayName;
                        _symbolAdded.Visibility = Visibility.Visible;
#endif
                    }
                }
            }
        }

        public Group WrapAndScaleSymbol(Group g, double prescale)
        {
            PInstance p = new PInstance(new Point(), g.Name);
            p.LayerId = 0;      // Groups should be created on layer 0 (unassigned) regardless of the active layer setting

            string name = Globals.ActiveDrawing.UniqueGroupName(g.Name);

            List<Primitive> list = new List<Primitive>();
            list.Add(p);

            Group group = new Group(name);
            group.PaperUnit = Globals.ActiveDrawing.PaperUnit;
            group.ModelUnit = Globals.ActiveDrawing.ModelUnit;
            group.ModelScale = Globals.ActiveDrawing.Scale;
            group.MovePrimitivesFromDrawing(p.Origin.X, p.Origin.Y, list);
            group.InsertLocation = GroupInsertLocation.None;
            //group.IncludeInLibrary = false;
            group.Entry = new Point(0, 0);
            group.Exit = new Point(0, 0);

            return group;
        }

        private async Task WrapAndScaleSymbol(StorageFile file, double prescale)
        {
            if (file.Name.EndsWith(".dbsx"))
            {
                try
                {
                    String xml = await FileIO.ReadTextAsync(file);
                    var doc = new XmlDocument();
                    doc.LoadXml(xml);

                    var idAttr = doc.SelectNodes("/SymbolFile/Header/Id");
                    if (idAttr.Count == 1)
                    {
                        string symbolId = idAttr.Item(0).InnerText;
                        string mainGroupXPath = $"//SymbolFile/Groups/Group/Id[text() = '{symbolId}']/..";
                        XmlNodeList mainGroupNodeList = doc.SelectNodes(mainGroupXPath);
                        if (mainGroupNodeList.Count == 1)
                        {
                            uint objectId = 0;
                            string objectIdXPath = "//Group/Entities/Entity/ObjectId";
                            XmlNodeList objectIdNodeList = doc.SelectNodes(objectIdXPath);
                            foreach (XmlNode node in objectIdNodeList)
                            {
                                if (uint.TryParse(node.InnerText, out uint oid))
                                {
                                    objectId = Math.Max(objectId, oid);
                                }
                            }

                            int zIndex = 0;
                            string zIndexXPath = "//Group/Entities/Entity/ZIndex";
                            XmlNodeList zIndexNodeList = doc.SelectNodes(zIndexXPath);
                            foreach (XmlNode node in zIndexNodeList)
                            {
                                if (int.TryParse(node.InnerText, out int zid))
                                {
                                    zIndex = Math.Max(zIndex, zid);
                                }
                            }

                            objectId++;
                            zIndex++;

                            string groupNameXPath = "//SymbolFile/Groups/Group[1]/Name";
                            XmlNodeList groupNameList = doc.SelectNodes(groupNameXPath);

                            if (groupNameList.Count == 1)
                            {
                                string groupName = groupNameList.Item(0).InnerText;

                                string entityXml = $"<Entity Type=\"Instance\" X=\"0\" Y=\"-0\" LayerId=\"0\" ColorSpec=\"0\" LineTypeId=\"-1\" LineWeightId=\"-1\" Name=\"{groupName}\"><Matrix><M11>{prescale}</M11><M12>0</M12><M21>0</M21><M22>{prescale}</M22><OffsetX>0</OffsetX><OffsetY>0</OffsetY></Matrix><ObjectId>{objectId}</ObjectId><ZIndex>{zIndex}</ZIndex><Flip>0</Flip></Entity>";
                                System.Diagnostics.Debug.WriteLine(entityXml);

                                string newGroupId = Guid.NewGuid().ToString();
                                idAttr.Item(0).InnerText = newGroupId;

                                XmlNode newGroupNode = doc.ImportNode(mainGroupNodeList.Item(0), true);
                                System.Diagnostics.Debug.WriteLine(newGroupNode.OuterXml);

                                XmlNode newGroupNameNode = newGroupNode.SelectSingleNode("/Name");
                                newGroupNameNode.InnerText = ":0";

                                XmlNode newGroupIdNode = newGroupNode.SelectSingleNode("/Id");
                                newGroupIdNode.InnerText = newGroupId;

                                XmlNode newGroupBoundsXNode = newGroupNode.SelectSingleNode("/ModelBounds/X");
                                XmlNode newGroupBoundsYNode = newGroupNode.SelectSingleNode("/ModelBounds/Y");
                                XmlNode newGroupBoundsWidthNode = newGroupNode.SelectSingleNode("/ModelBounds/Width");
                                XmlNode newGroupBoundsHeightNode = newGroupNode.SelectSingleNode("/ModelBounds/Height");

                                if (float.TryParse(newGroupBoundsXNode.InnerText, out float boundsX) &&
                                    float.TryParse(newGroupBoundsYNode.InnerText, out float boundsY) &&
                                    float.TryParse(newGroupBoundsWidthNode.InnerText, out float boundsWidth) &&
                                    float.TryParse(newGroupBoundsHeightNode.InnerText, out float boundsHeight)
                                    )
                                {
                                    float ps = (float)prescale;

                                    boundsX *= ps;
                                    boundsY *= ps;
                                    boundsWidth *= ps;
                                    boundsHeight *= ps;

                                    newGroupBoundsXNode.InnerText = boundsX.ToString();
                                    newGroupBoundsYNode.InnerText = boundsY.ToString();
                                    newGroupBoundsWidthNode.InnerText = boundsWidth.ToString();
                                    newGroupBoundsHeightNode.InnerText = boundsHeight.ToString();
                                }

                                XmlNode newGroupEntitiesNode = newGroupNode.SelectSingleNode("/Entities");
                                newGroupEntitiesNode.InnerXml = entityXml;

                                System.Diagnostics.Debug.WriteLine(newGroupNode.OuterXml);

                                XmlNode groupsNode = doc.SelectSingleNode("//SymbolFile/Groups");
                                groupsNode.AppendChild(newGroupNode);

                                doc.Save(file.Path);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        private async void _addSubfolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem != null &&
                _selectedItem.Type == LibraryItem.LibraryItemType.Folder)
            {
                string newFolderName = "New folder";
                await _treeView.AddSubFolder(_selectedItem, newFolderName);
            }
        }

        private void _icOkButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _removeSymbolButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem != null && _selectedItem.Group != null)
            {
                Globals.ActiveDrawing.RemoveGroup(_selectedItem.Group.Name);
                _treeView.RemoveGroupFromTreeView(_selectedItem);
            }
        }

        private async void _backupButton_Click(object sender, RoutedEventArgs e)
        {
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
            FileSavePicker savePicker = InitializeWithWindow(new FileSavePicker(),App.WindowHandle);
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Symbol Library", new List<string>() { ".dslx" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "SymbolLibraryBackup.dslx";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await SymbolLibraryUtilities.BackupSymbolLibrary(file);
            }
        }

        private static FileSavePicker InitializeWithWindow(FileSavePicker obj, IntPtr windowHandle)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
            return obj;
        }

        private async void _restoreButton_Click(object sender, RoutedEventArgs e)
        {
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
            FileOpenPicker openPicker = InitializeWithWindow(new FileOpenPicker(),App.WindowHandle);
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".dslx");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                if (file.FileType == ".dslx")
                {
                    await SymbolLibraryUtilities.RestoreSymbolLibrary(file, false);
                }
            }
        }

        private static FileOpenPicker InitializeWithWindow(FileOpenPicker obj, IntPtr windowHandle)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
            return obj;
        }

        private void _removeOrphansButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
