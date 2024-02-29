using Cirros;
using Cirros.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI;

namespace HUI
{
    class HUIUtilities
    {
        public static void HSetXamlBrush(Canvas canvas, Brush brush, double thickness)
        {
            foreach (UIElement e in canvas.Children)
            {
                if (e is Shape)
                {
                    Shape s = e as Shape;

                    s.Stroke = brush;
                    s.StrokeThickness = thickness;

                    if (s.Fill != null)
                    {
                        if (s.Fill is SolidColorBrush && (((SolidColorBrush)s.Fill).Color).A == 255)
                        {
                            s.Fill = brush;
                        }
                    }
                }
                else if (e is TextBlock)
                {
                    ((TextBlock)e).Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (e is Canvas)
                {
                    HSetXamlBrush(e as Canvas, brush, thickness);
                }
            }
        }

        public static async Task<StorageFile> HGetSingleFileAsync(List<string> types)
        {
/*
    TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
    Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
            FileOpenPicker openPicker = InitializeWithWindow(new FileOpenPicker(),App3.App.WindowHandle);
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            foreach (string type in types)
            {
                openPicker.FileTypeFilter.Add(type);
            }

            StorageFile file = await openPicker.PickSingleFileAsync();

            return file;
        }

        private static FileOpenPicker InitializeWithWindow(FileOpenPicker obj, IntPtr windowHandle)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
            return obj;
        }
    }
}
