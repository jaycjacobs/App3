//#define TEMP_SYMBOL_STUFF
using Cirros;
using Cirros.Dialogs;
using Cirros.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;

#if TEMP_SYMBOL_STUFF

namespace CirrosCore.SymbolUtilities
{
    public class SymbolUtilities
    {
        private static ConcurrentQueue<SymbolItem> _updateQueue = new ConcurrentQueue<SymbolItem>();

        private static Task task;

        private static void StartTask()
        {
            if ((task != null) && (task.IsCompleted == false ||
                                   task.Status == TaskStatus.Running ||
                                   task.Status == TaskStatus.WaitingToRun ||
                                   task.Status == TaskStatus.WaitingForActivation))
            {
                // Task is already running
            }
            else
            {
                task = Task.Run(async delegate
                {
                    while (_updateQueue.TryDequeue(out SymbolItem item))
                    {
                        await item.UpdateThumbnail();
                    }
                });
            }
        }

        public static async Task EnsureSymbolThumbnails()
        {
            List<string> thumbnails = new List<string>();

            var files = await Globals.SymbolThumbnailFolder.GetFilesAsync();

            foreach (StorageFile file in files)
            {
                string name = file.Name;
                int i = name.LastIndexOf(".");
                if (i > 0)
                {
                    name = name.Substring(0, i);
                }
                thumbnails.Add(name);
            }

            foreach (string key in Globals.ActiveDrawing.Groups.Keys)
            {
                Cirros.Primitives.Group g = Globals.ActiveDrawing.Groups[key];
                if (g.IncludeInLibrary)
                {
                    if (thumbnails.Contains(g.Id.ToString()) == false)
                    {
                        _updateQueue.Enqueue(new SymbolItem(g));
                    }
                }
            }

            StartTask();
        }

        public static async Task ShowProgress()
        {
            if (_updateQueue.Count > 12)
            {
                ProgressDialog dialog = new ProgressDialog("Creating symbol thumbnails");
                dialog.Collection = _updateQueue;
                await dialog.ShowAsync();
            }
        }

        public static async Task<List<string>> MissingSymbolThumbnails()
        {
            List<string> missing = new List<string>();
            List<string> thumbnails = new List<string>();

            var files = await Globals.SymbolThumbnailFolder.GetFilesAsync();

            foreach (StorageFile file in files)
            {
                string name = file.Name;
                int i = name.LastIndexOf(".");
                if (i > 0)
                {
                    name = name.Substring(0, i);
                }
                thumbnails.Add(name);
            }

            foreach (string key in Globals.ActiveDrawing.Groups.Keys)
            {
                Cirros.Primitives.Group g = Globals.ActiveDrawing.Groups[key];
                if (g.IncludeInLibrary)
                {
                    if (thumbnails.Contains(g.Id.ToString()) == false)
                    {
                        missing.Add(g.Name);
                    }
                }
            }

            return missing;
        }
    }

    public class SymbolItem : INotifyPropertyChanged
    {
        public string Name { get; private set; }
        public string Folder { get; set; }
        public int FolderIndex { get; set; }
        public Group Group { get; private set; }
        public SymbolItem SymbolItemObject { get { return this; } }
        public bool NeedsUpdate { get; set; }

        public string ImageLocation
        {
            get
            {
                return $"ms-appdata:///temp/symbols/{Group.Id}.png";
            }
        }

        public SymbolItem(Group group)
        {
            Group = group;
            Name = group.Name;
            Description = group.Description;
            Folder = group.Folder;
        }

        public string Description
        {
            get
            {
                return Group.Description;
            }
            set
            {
                Group.Description = value;
                NotifyPropertyChanged("Description");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public async Task<bool> UpdateThumbnail()
        {
            bool updated = false;

            try
            {
                StorageFile thumbfile = await FileHandling.GetGroupThumbnail(Group);
                if (thumbfile != null)
                {
                    NeedsUpdate = false;
                }
            }
            catch (FileNotFoundException)
            {
                // no thumbnail
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return updated;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
#endif