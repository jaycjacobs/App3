using Cirros;
using Cirros.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace CirrosUI.Symbols
{
    public class SymbolLibraryUtilities
    {
        public static async Task<string> UniqueName(string name, StorageFolder folder)
        {
            var item = await folder.TryGetItemAsync(name);
            if (item != null)
            {
                string extension = "";
                string basename = name;

                int d = name.LastIndexOf(".");
                if (d >= 0)
                {
                    extension = name.Substring(d);
                    basename = basename.Replace(extension, "");
                }

                int index = 1;
                int k = basename.LastIndexOf("-");
                if (k >= 0)
                {
                    string test = basename.Substring(k + 1);
                    if (int.TryParse(test, out int i))
                    {
                        index = i;
                        basename = basename.Substring(0, k);
                    }
                }

                do
                {
                    name = String.Format("{0}-{1}{2}", basename, index++, extension);
                }
                while (await folder.TryGetItemAsync(name) != null);
            }

            return name;
        }

        public async static Task UpdateGroupFromLibraryItem(LibraryItem item)
        {
            if (item.Type == LibraryItem.LibraryItemType.File && item.FSObject is StorageFile file)
            {
                bool dirty = false;

                if (item.Group != null)
                {
                    // will Group ever be set when FSObject is StorageFile?
                    if (item.Group.Name != item.DisplayName)
                    {
                        item.Group.Name = item.DisplayName;
                        dirty = true;
                    }
                    if (item.Group.Description != item.Description)
                    {
                        item.Group.Description = item.Description;
                        dirty = true;
                    }
                    if (item.IsModelSpace != (item.Group.CoordinateSpace == CoordinateSpace.Model))
                    {
                        item.Group.CoordinateSpace = item.IsModelSpace ? CoordinateSpace.Model : CoordinateSpace.Paper;
                        dirty = true;
                    }

                    if (dirty)
                    {
                        item.Group.Id = Guid.NewGuid();
                        await FileHandling.SaveSymbolAsync(file, item.Group);
                    }
                }
                else 
                {
                    String xml = await FileIO.ReadTextAsync(file);
                    var doc = new XmlDocument();
                    doc.LoadXml(xml);

                    var attr = doc.SelectNodes("/SymbolFile/Header/SymbolName");
                    if (attr.Count == 1)
                    {
                        if (attr.Item(0).InnerText != item.DisplayName)
                        {
                            attr.Item(0).InnerText = item.DisplayName;
                            dirty = true;
                        }
                    }

                    var attr1 = doc.SelectNodes("/SymbolFile/Header/Description");
                    if (attr1.Count == 1)
                    {
                        if (attr1.Item(0).InnerText != item.Description)
                        {
                            attr1.Item(0).InnerText = item.Description;
                            dirty = true;
                        }
                    }

                    var attr2 = doc.SelectNodes("/SymbolFile/Header/CoordinateSpace");
                    if (attr2.Count == 1)
                    {
                        if (item.IsModelSpace != (attr2.Item(0).InnerText == "Model"))
                        {
                            attr2.Item(0).InnerText = item.IsModelSpace ? "Model" : "Paper";
                            dirty = true;
                        }
                    }

                    if (dirty)
                    {
                        string newId = Guid.NewGuid().ToString();
                        var attr3 = doc.SelectNodes("/SymbolFile/Header/Id");
                        if (attr3.Count == 1)
                        {
                            string oldId = attr3.Item(0).InnerText;
                            attr3.Item(0).InnerText = newId;

                            string xpath = $"/SymbolFile/Groups/Group/Id[text() = '{oldId}']";
                            var attr4 = doc.SelectNodes("/SymbolFile/Groups/Group/Id");

                            if (attr4.Count == 1)
                            {
                                attr4.Item(0).InnerText = newId;

                                string xml1 = doc.OuterXml;
                                await FileIO.WriteTextAsync(file, xml1, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                            }
                        }
                    }
                }
            }
            else if (item.Type == LibraryItem.LibraryItemType.Group)
            {
                item.Group.CoordinateSpace = item.IsModelSpace ? CoordinateSpace.Model : CoordinateSpace.Paper;
                item.Group.Description = item.Description;
            }

            Globals.ActiveDrawing.IsModified = true;
            Globals.ActiveDrawing.ChangeNumber++;
        }

        public static async Task DeleteLibraryItem(LibraryItem item)
        {
            if (item != null && item.Parent.Type == LibraryItem.LibraryItemType.Folder)
            {
                if (item.Type == LibraryItem.LibraryItemType.File || item.Type == LibraryItem.LibraryItemType.Invalid)
                {
                    if (item.FSObject is StorageFile file)
                    {
                        // Delete symbol

                        item.Parent.RemoveChild(item);

                        await file.DeleteAsync();
                    }
                }
                else if (item.Type == LibraryItem.LibraryItemType.Folder)
                {
                    item.Parent.RemoveChild(item);

                    if (item.FSObject is StorageFolder folder)
                    {
                        // delete folder and contents

                        item.Parent.RemoveChild(item);

                        await folder.DeleteAsync();
                    }
                }
            }
        }

        public static async Task<LibraryItem> MoveLibraryItem(LibraryItem sourceItem, LibraryItem destItem, string newName = null)
        {
            // If the destination item exists it will be overwritten

            if (sourceItem.FSObject is StorageFile file)
            {
                if (destItem.FSObject is StorageFolder folder)
                {
                    if (newName == null)
                    {
                        newName = sourceItem.Name;
                    }
                    else if (newName != sourceItem.DisplayName)
                    {
                        string displayname = "";

                        if (newName.EndsWith(".dbsx"))
                        {
                            displayname = newName.Substring(0, newName.Length - 5);
                        }
                        else
                        {
                            displayname = newName;
                        }
                        if (displayname.Contains("."))
                        {
                            // error - name can't contain "."
                            newName = null;
                        }
                        else
                        {
                            newName = displayname + ".dbsx";
                        }
                    }

                    if (newName != null)
                    {
                        if (newName.EndsWith(".dbsx") == false)
                        {
                            newName = newName + ".dbsx";
                        }

                        // move to a different folder - destination path is not the same as the source path

                        try
                        {
                            //bool replace = await folder.TryGetItemAsync(newName) != null;
                            if (await folder.TryGetItemAsync(newName) != null)
                            {
                                // if a libraryitem with the same name exists in destLibraryItem.Children
                                // remove it because the corresponding file will be replaced with this one

                                LibraryItem existingItem = destItem.FindChild(newName);
                                if (existingItem != null)
                                {
                                    destItem.RemoveChild(existingItem);
                                }
                            }

                            if (destItem.Path == sourceItem.Parent.Path)
                            {
                                // rename - destination path is the same as the source path

                                await RenameSymbol(file, newName);

                                sourceItem.Name = newName;
                                destItem.UpdateChild(sourceItem);
                            }
                            else
                            {
                                await file.MoveAsync(folder, newName, NameCollisionOption.ReplaceExisting);

                                sourceItem.Name = newName;
                                sourceItem.Parent.RemoveChild(sourceItem);

                                destItem.InsertChild(0, sourceItem);
                            }
                        }
                        catch
                        {
                            // dup name
                        }
                    }
                    else
                    {
                        // invalid name
                    }
                }
            }
            else if (sourceItem.FSObject is StorageFolder sourceFolder)
            {
                if (destItem.FSObject is StorageFolder destFolder)
                {
                    if (newName == null)
                    {
                        newName = sourceItem.Name;
                    }
                    else if (newName != sourceItem.Name)
                    {
                        string displayname = newName;

                        if (displayname.Contains("."))
                        {
                            // error - name can't contain "."
                            newName = null;
                        }
                    }

                    if (newName != null)
                    {
                        if (destItem.Path != sourceItem.Parent.Path)
                        {
                            if (await destFolder.TryGetItemAsync(newName) != null)
                            {
                                LibraryItem mergeItem = destItem.FindChild(newName);
                                if (mergeItem != null && mergeItem.Type == LibraryItem.LibraryItemType.Folder)
                                {
                                    destItem.RemoveChild(mergeItem);
                                }
                            }

                            // move to a different folder - destination path is not the same as the source path
                            sourceItem.Parent.RemoveChild(sourceItem);
                            sourceItem.FSObject = await MoveFolderItemToFolder(sourceItem, destFolder, newName);

                            // create new LibraryItem for folder
                            LibraryItem newFolderItem = await AddFolderContents(sourceItem.FSObject as StorageFolder, destItem);
                            sourceItem.Parent.ReplaceChild(sourceItem, newFolderItem);
                            sourceItem = newFolderItem;
                        }
                        else if (sourceItem.Name != newName)
                        {
                            // rename - destination path is the same as the source path
                            
                            await sourceFolder.RenameAsync(newName, NameCollisionOption.GenerateUniqueName);

                            LibraryItem newFolderItem = await AddFolderContents(sourceFolder);
                            sourceItem.Parent.ReplaceChild(sourceItem, newFolderItem);

                            sourceItem = newFolderItem;
                        }
                    }
                    else
                    {
                        // invalid name
                    }
                }
            }
            else if (sourceItem.Group != null && destItem.FSObject is StorageFolder folder)
            {
                if (await folder.TryGetItemAsync(sourceItem.Name) != null)
                {
                    // if a libraryitem with the same name exists in destLibraryItem.Children
                    // remove it because the corresponding file will be replaced with this one

                    LibraryItem existingItem = destItem.FindChild(sourceItem.Name);
                    if (existingItem != null)
                    {
                        destItem.RemoveChild(existingItem);
                    }
                }

                //bool replace = await folder.TryGetItemAsync(sourceItem.Name) != null;

                StorageFile symbolFile = await folder.CreateFileAsync(sourceItem.Name, CreationCollisionOption.ReplaceExisting);
                await FileHandling.SaveSymbolAsync(symbolFile, sourceItem.Group);
                sourceItem.FSObject = symbolFile;

                destItem.InsertChild(0, new LibraryItem(symbolFile));
            }
            else
            {
                // failed
            }

            return sourceItem;
        }

        protected static async Task RenameSymbol(StorageFile symbolFile, string newName)
        {
            // need to change symbol id when the name changes
            await symbolFile.RenameAsync(newName, NameCollisionOption.ReplaceExisting);

            String xml = await FileIO.ReadTextAsync(symbolFile);
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var attr0 = doc.SelectNodes("/SymbolFile/Header/SymbolName");
            if (attr0.Count == 1)
            {
                attr0.Item(0).InnerText = newName.Replace(".dbsx", "");
            }
             
            string newId = Guid.NewGuid().ToString();
            var attr = doc.SelectNodes("/SymbolFile/Header/Id");
            if (attr.Count == 1)
            {
                string oldId = attr.Item(0).InnerText;
                attr.Item(0).InnerText = newId;

                string xpath = $"/SymbolFile/Groups/Group/Id[text() = '{oldId}']";
                var attr1 = doc.SelectNodes("/SymbolFile/Groups/Group/Id");

                if (attr1.Count == 1)
                {
                    attr1.Item(0).InnerText = newId;

                    string xml1 = doc.OuterXml;
                    await FileIO.WriteTextAsync(symbolFile, xml1, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                }
            }

        }

        public static async Task<LibraryItem> AddFolderContents(StorageFolder folder, LibraryItem parent = null)
        {
            IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync();
            IReadOnlyList<StorageFolder> folderList = await folder.GetFoldersAsync();

            LibraryItem folderItem = new LibraryItem(folder);
            if (parent != null)
            {
                parent.AddChild(folderItem);
            }

            foreach (StorageFile file in fileList)
            {
                if (file.FileType == ".dbsx")
                {
                    folderItem.AddChild(new LibraryItem(file));
                }
            }

            foreach (StorageFolder subfolder in folderList)
            {
                await AddFolderContents(subfolder, folderItem);
            }

            return folderItem;
        }


        public static async Task<StorageFolder> MoveFolderItemToFolder(LibraryItem sourceLibraryItem, StorageFolder destFolder, string folderName = null)
        {
            if (folderName == null)
            {
                folderName = sourceLibraryItem.Name;
            }

            StorageFolder newDestFolder = await destFolder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);

            if (sourceLibraryItem.FSObject is StorageFolder sourceFolder)
            {
                foreach (LibraryItem child in sourceLibraryItem.Children)
                {
                    if (child.FSObject is StorageFile childFile)
                    {
                        string name = await UniqueName(child.Name, newDestFolder);
                        await childFile.MoveAsync(newDestFolder, name, NameCollisionOption.GenerateUniqueName);
                    }
                    else if (child.FSObject is StorageFolder childFolder)
                    {
                        child.FSObject = await MoveFolderItemToFolder(child, newDestFolder);
                    }
                }

                await sourceFolder.DeleteAsync();
            }

            return newDestFolder;
        }

        public static async Task<LibraryItem> RenameLibraryItem(LibraryItem item, string newName)
        {
            if (item != null && item.Parent != null)
            {
                try
                {
                    if (item.FSObject is StorageFolder folder)
                    {
                        await folder.RenameAsync(newName, NameCollisionOption.GenerateUniqueName);
                        LibraryItem newFolderItem = await AddFolderContents(folder);
                        item.Parent.ReplaceChild(item, newFolderItem);

                        item = newFolderItem;
                    }
                    else if (item.FSObject is StorageFile file && item.Parent.FSObject is StorageFolder parentFolder)
                    {
#if UNIQUE_NAME
                        if (newName.EndsWith(file.FileType) == false)
                        {
                            newName = newName + file.FileType;
                        }
                        newName = await UniqueName(newName, parentFolder);
                        await file.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
                        item.Name = file.Name;
                        item.Parent.UpdateChild(item);
#else
                        if (newName.EndsWith(file.FileType) == false)
                        {
                            newName = newName + file.FileType;
                        }
                        if (await parentFolder.TryGetItemAsync(newName) != null)
                        {
                            // if a libraryitem with the same name exists in destLibraryItem.Children
                            // remove it because the corresponding file will be replaced with this one

                            LibraryItem existingItem = item.Parent.FindChild(newName);
                            if (existingItem != null)
                            {
                                item.Parent.RemoveChild(existingItem);
                            }
                        }
                        await file.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
                        item.Name = file.Name;
                        item.Parent.UpdateChild(item);
#endif
                    }
                }
                catch
                {
                    // error
                }
            }

            return item;
        }

        public static async Task BackupSymbolLibrary(StorageFile file)
        {
            StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFolder symbolsFolder = await localFolder.CreateFolderAsync("Symbols", CreationCollisionOption.OpenIfExists);
            StorageFolder tempFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder;

            if (symbolsFolder != null)
            {
                try
                {
                    StorageFile zip = await StorageFile.GetFileFromPathAsync($"{tempFolder.Path}\\symbols.dslx");
                    await zip.DeleteAsync();
                }
                catch
                {
                }

                bool success = false;

                await Task.Run(() =>
                {
                    try
                    {
                        StorageApplicationPermissions.FutureAccessList.AddOrReplace("SymbolLibraryToken", file);
                        ZipFile.CreateFromDirectory(symbolsFolder.Path, $"{tempFolder.Path}\\symbols.dslx", CompressionLevel.Fastest, true);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                });
                      
                if (success)
                {
                    try
                    {
                        StorageFile zip = await StorageFile.GetFileFromPathAsync($"{tempFolder.Path}\\symbols.dslx");
                        await zip.CopyAndReplaceAsync(file);
                        await zip.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            }
        }

        public static async Task RestoreSymbolLibrary(StorageFile file, bool overwrite)
        {
            StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFolder symbolsFolder = await localFolder.CreateFolderAsync("Symbols", CreationCollisionOption.OpenIfExists);
            StorageFolder tempFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder;

            // copy file to app domain
            StorageFile zipSource = await file.CopyAsync(Windows.Storage.ApplicationData.Current.TemporaryFolder);

            Guid guid = Guid.NewGuid();
            //string tempPath = $"{tempFolder.Path}\\{guid.ToString()}";
            string tempPath = symbolsFolder.Path;

            if (symbolsFolder != null)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        //StorageApplicationPermissions.FutureAccessList.AddOrReplace("SymbolLibraryToken", file);
                        ZipFile.ExtractToDirectory(zipSource.Path, tempPath, overwrite);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                });
            }

            try
            {
                await zipSource.DeleteAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }

    public class FolderItem //: INotifyPropertyChanged
    {
        private bool _isExpanded = false;
        //private bool _isSelected;

        public FolderItem(LibraryItem item)
        {
            LibraryItem = item;
        }

        public LibraryItem LibraryItem { get; }

        public override string ToString()
        {
            return Path;
        }

        public string Path {
            get { return LibraryItem.Path; }
            set { LibraryItem.Path = value; }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                }
            }
        }
    }
}
