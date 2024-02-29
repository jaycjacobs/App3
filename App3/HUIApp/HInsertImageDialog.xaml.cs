using Cirros;
using Cirros.Drawing;
using Cirros.Utility;
using CirrosUI;
using RedDog.HUIApp;
using HUI;
using RedDog;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using RedDog.Console;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace RedDog.HUIApp
{
    public sealed partial class HInsertImageDialog : UserControl, HUIIDialog
    {
        Dictionary<string, object> _options = new Dictionary<string, object>() { { "command", RedDogGlobals.GS_InsertImageCommand } };
        HXAMLControl _selectedIcon = null;

        public HInsertImageDialog()
        {
            this.InitializeComponent();

            this.Loaded += HInsertImageDialog_Loaded;
        }

        public FrameworkElement HelpButton
        {
            get { return null; }
        }

        public void WillClose()
        {
            Globals.Events.OnImageChanged -= Events_OnImageChanged;
        }

        void HInsertImageDialog_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (FrameworkElement fe in _iconRow1.Children)
            {
                if (fe is HXAMLControl)
                {
                    HXAMLControl hxamlControl = fe as HXAMLControl;
                    hxamlControl.OnHXAMLControlClick += hxamlControl_OnHXAMLControlClick;
                    hxamlControl.IsSelected = false;
                }
            }

            Populate();

            _layerComboBox.SelectionChanged += _layerComboBox_SelectionChanged;
            _opacitySlider.ValueChanged += _opacitySlider_ValueChanged;

            Globals.Events.OnImageChanged += Events_OnImageChanged;

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        private async void Events_OnImageChanged(object sender, ImageChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_imageId) == false && e.File.Name.StartsWith(_imageId))
            {
                IRandomAccessStream fileStream = await e.File.OpenReadAsync();
                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(fileStream);
                Image image = new Image();
                image.Source = bitmapImage;
                _selectedImage.ItemSource = image;

                if (Globals.ImageDictionary.ContainsKey("pixelWidth"))
                {
                    Globals.ImageDictionary["pixelWidth"] = bitmapImage.PixelWidth;
                }
                if (Globals.ImageDictionary.ContainsKey("pixelHeight"))
                {
                    Globals.ImageDictionary["pixelHeight"] = bitmapImage.PixelHeight;
                }
            }
        }

        void _opacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            SetOption(RedDogGlobals.GS_Opacity, _opacitySlider.Value);
        }

        void _layerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_layerComboBox.SelectedItem is ComboBoxItem && ((ComboBoxItem)_layerComboBox.SelectedItem).Tag is string layer)
            {
                SetOption(RedDogGlobals.GS_Layer, layer);
            }
            else if (_layerComboBox.SelectedItem is HLayerTile tile && tile.Tag is string tilelayer)
            {
                SetOption(RedDogGlobals.GS_Layer, tilelayer);
            }
        }

        public string Id
        {
            get { return RedDogGlobals.GS_InsertImageCommand; }
        }

        public void Populate()
        {
            if (_layerComboBox != null)
            {
                _layerComboBox.Items.Clear();

                TextBlock activelayer = new TextBlock();
                activelayer.Text = "Use active layer";
                activelayer.Style = (Style)(Application.Current.Resources["HDialogComboBoxContentItalic"]);
                activelayer.Tag = "active_layer";
                _layerComboBox.Items.Add(activelayer);

                foreach (Cirros.Drawing.Layer layer in Globals.LayerTable.Values)
                {
                    HLayerTile item = new HLayerTile(layer);
                    item.Tag = layer.Name;
                    _layerComboBox.Items.Add(item);
                }
            }

            SetLayer(Globals.ActiveImageLayerId);

            // get image file from Globals.InsertImageSource
            SetSelectedImage(null);

            _opacitySlider.Value = RedDogGlobals.InsertImageOpacity;
        }

        void SetInputType(string type)
        {
            HXAMLControl control = null;

            foreach (FrameworkElement fe in _iconRow1.Children)
            {
                if (fe is HXAMLControl)
                {
                    if (((HXAMLControl)fe).Id == type)
                    {
                        control = fe as HXAMLControl;
                        break;
                    }
                }
            }

            if (control != null)
            {
                SelectInputControl(control);
            }
        }

        public Dictionary<string, object> Options
        {
            get
            {
                if (FocusManager.GetFocusedElement() is NumberBox1)
                {
                    NumberBox1 nb = FocusManager.GetFocusedElement() as NumberBox1;
                    if (nb.Tag is string)
                    {
                        SetOption(nb.Tag as string, nb.Value);
                    }
                }

                return _options;
            }
        }

        void SetOption(string key, object value)
        {
            if (_options.ContainsKey(key))
            {
                if (value == null)
                {
                    _options.Remove(key);
                }
                else
                {
                    _options[key] = value;
                }
            }
            else
            {
                _options.Add(key, value);
            }
        }

        StorageFile _imageFile = null;
        string _imageName = null;
        string _imageId = null;

        private async void SetSelectedImage(StorageFile file)
        {
            if (file == null)
            {
                _selectedImage.Id = "";
                _selectedImage.Label = "No image is selected";
                _selectedImage.ItemSource = null;
                _selectedImage.FontStyle = Windows.UI.Text.FontStyle.Italic;
                _editImageButton.IsEnabled = false;

                _imageFile = null;
                _imageName = null;
                _imageId = null;
            }
            else
            {
                IRandomAccessStream fileStream = await file.OpenReadAsync();
                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(fileStream);
                Image image = new Image();
                image.Source = bitmapImage;

                _imageId = Guid.NewGuid().ToString();
#if true
                _imageFile = await Utilities.TemporaryJpegFromFile(file, _imageId);
#else
                _imageFile = await file.CopyAsync(Globals.TemporaryImageFolder, _imageId + file.FileType);
#endif
                _imageName = file.Name;

                _selectedImage.Id = file.DisplayName;
                _selectedImage.Label = file.DisplayName;
                _selectedImage.ItemSource = image;
                _selectedImage.FontStyle = Windows.UI.Text.FontStyle.Normal;
                _editImageButton.IsEnabled = true;

                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add("command", "newimage");
                dictionary.Add("imageId", _imageId);
                dictionary.Add("sourceFile", _imageFile);
                dictionary.Add("sourceName", _imageName);
                dictionary.Add("pixelWidth", bitmapImage.PixelWidth);
                dictionary.Add("pixelHeight", bitmapImage.PixelHeight);
                Globals.ImageDictionary = dictionary;
            }
        }

        async void SelectInputControl(HXAMLControl control)
        {
            if (_selectedIcon != null)
            {
                _selectedIcon.IsSelected = false;
            }

            _selectedIcon = control;
            _selectedIcon.IsSelected = true;

            switch (control.Id)
            {
                case RedDogGlobals.GS_InsertImageFromFile:
                    break;

                case RedDogGlobals.GS_InsertImageFromCamera:
                    break;
            }

            if (control.Id == RedDogGlobals.GS_InsertImageFromFile)
            {
                _fileIcon.ProgressRingActive = true;

                List<string> types = new List<string>();
                types.Add(".jpg");
                types.Add(".jpeg");
                types.Add(".png");
                //types.Add(".pdf");
                StorageFile file = await HUIUtilities.HGetSingleFileAsync(types);

                _fileIcon.ProgressRingActive = false;

                if (file != null)
                {
                    SetSelectedImage(file);
                    SetOption(RedDogGlobals.GS_InsertImageSource, _selectedImage.Id);
                }
            }
            else if (control.Id == RedDogGlobals.GS_InsertImageFromCamera)
            {
                StorageFile file = await FileHandling.TakePhotoAsync();

                if (file != null)
                {
                    SetSelectedImage(file);
                    SetOption(RedDogGlobals.GS_InsertImageSource, _selectedImage.Id);
                }
            }
        }

        void hxamlControl_OnHXAMLControlClick(object sender, EventArgs e)
        {
            if (sender is HXAMLControl)
            {
                SelectInputControl(sender as HXAMLControl);
            }
        }

        public void SetLayer(int layerId)
        {
            if (layerId < 0)
            {
                layerId = Globals.ActiveLayerId;
                _layerComboBox.SelectedIndex = 0;
            }
            else
            {
                if (Globals.LayerTable.ContainsKey(layerId))
                {
                    Layer layer = Globals.LayerTable[layerId];

                    for (int i = 0; i < _layerComboBox.Items.Count; i++)
                    {
                        if (_layerComboBox.Items[i] is HLayerTile tile && tile.Tag is string tilelayer)
                        {
                            if (tilelayer == layer.Name)
                            {
                                _layerComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    _layerComboBox.SelectedIndex = 0;
                }
            }
        }

        private void _editImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_imageFile != null)
            {
                Globals.Events.EditImage(_imageId, _imageFile, _imageName);
            }
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "insert-image" }, { "source", "help" } });

            _ttInsertImageIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "insert-image" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttInsertImageFile.IsOpen = true;
                        break;

                    case "file":
                        _ttInsertImageCamera.IsOpen = true;
                        break;

                    case "camera":
                        _ttInsertImageLayer.IsOpen = true;
                        break;

                    case "layer":
                        _ttInsertImageOpacity.IsOpen = true;
                        break;

                    case "opacity":
                        _ttInsertImageThumbnail.IsOpen = true;
                        break;

                    case "thumbnail":
                        _ttInsertImageEdit.IsOpen = true;
                        break;

                    case "edit":
                        break;
                }
            }
        }
    }
}
