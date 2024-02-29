using Cirros;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace Cirros8.ModalDialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PdfPagePickerPage : Page
    {
        uint _pageCount = 0;
        uint _pageNumber = 0;

        PdfDocument _pdfDocument = null;
        string _name = null;

        string[] _pageMap = null;

        public PdfPagePickerPage(PdfDocument pdfDocument, string name)
        {
            this.InitializeComponent();

            var bounds = App.Window.Bounds;

            _topView.Width = bounds.Width;
            _topView.Height = bounds.Height;

            if (pdfDocument != null && pdfDocument.PageCount > 0 && string.IsNullOrEmpty(name) == false)
            {
                _pageCount = pdfDocument.PageCount;

                _pdfDocument = pdfDocument;
                _name = name;

                _pageMap = new string[_pageCount];

                SetPage(0);
            }
            Analytics.TrackPageView("PdfPagePickerPage");
        }

        private async void SetPage(uint page)
        {
            if (page >= 0 && page < _pageCount)
            {
                _pageNumber = page;
                _pageNumberBox.Text = (_pageNumber + 1).ToString();

                string filename;

                if (_pageMap[_pageNumber] != null)
                {
                    filename = _pageMap[_pageNumber];
                }
                else
                {
                    filename = string.Format("{0}.{1}.jpg", _name, page);
                    _pageMap[_pageNumber] = filename;
                }

                StorageFile file = null;

                try
                {
                    StorageFolder tempFolder = Globals.TemporaryImageFolder;
                    file = await tempFolder.GetFileAsync(filename);
                }
                catch
                {
                }

                if (file == null)
                {
                    file = await JpegFromPdfPage(filename, _pdfDocument, _pageNumber);
                }

                if (file != null)
                {
                    await DisplayImageFileAsync(file);
                }
            }
        }

        public static async Task<StorageFile> JpegFromPdfPage(string filename, PdfDocument pdfDoc, uint page)
        {
            StorageFile jpgFile = null;

            if (pdfDoc != null)
            {
                if (page < pdfDoc.PageCount)
                {
                    PdfPage pdfPage = pdfDoc.GetPage(page);
                    if (pdfPage != null)
                    {
                        StorageFolder tempFolder = Globals.TemporaryImageFolder;
                        jpgFile = await tempFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                        if (jpgFile != null)
                        {
                            using (IRandomAccessStream fileStream = await jpgFile.OpenAsync(FileAccessMode.ReadWrite))
                            {
                                PdfPageRenderOptions pdfPageRenderOptions = new PdfPageRenderOptions();
                                pdfPageRenderOptions.BitmapEncoderId = BitmapEncoder.JpegEncoderId;
                                await pdfPage.RenderToStreamAsync(fileStream, pdfPageRenderOptions);
                                await fileStream.FlushAsync();

                                fileStream.Dispose();
                                pdfPage.Dispose();
                            }
                        }
                    }
                }
            }
            return jpgFile;
        }

        public async Task DisplayImageFileAsync(StorageFile file)
        {
            BitmapImage src = new BitmapImage();
            src.SetSource(await file.OpenAsync(FileAccessMode.Read));
            _pdfPageImage.Source = src;
        }
        
        private void _prevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pageNumber > 0)
            {
                SetPage(--_pageNumber);
            }
        }

        private void _nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pageNumber < (_pageCount - 1))
            {
                SetPage(++_pageNumber);
            }
        }

        private async void _selectButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFile selectedPage = null;

            if (_pageNumber < _pageMap.Length && string.IsNullOrEmpty(_pageMap[_pageNumber]) == false)
            {
                StorageFolder tempFolder = Globals.TemporaryImageFolder;

                try
                {
                    selectedPage = await tempFolder.GetFileAsync(_pageMap[_pageNumber]);
                    _pageMap[_pageNumber] = null;
                }
                catch
                {
                }
            }

            await DeleteTemporaryImages();

            if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;
            }

            if (selectedPage != null)
            {
                Globals.Events.EditImage(null, selectedPage, null);
            }
        }

        private async Task DeleteTemporaryImages()
        {
            StorageFolder tempFolder = Globals.TemporaryImageFolder;

            foreach (string name in _pageMap)
            {
                if (name != null)
                {
                    try
                    {
                        StorageFile file = await tempFolder.GetFileAsync(name);
                        await file.DeleteAsync();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private async void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            await DeleteTemporaryImages();

            if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;
            }
        }
    }
}
