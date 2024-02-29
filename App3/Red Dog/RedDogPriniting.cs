﻿using Cirros;
using Cirros.Alerts;
using Cirros.Core;
using Cirros.Core.Display;
using Cirros.Utility;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Printing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;

namespace RedDog
{
    public sealed partial class RedDogDrawingPage : Page
    {
        // TODO Windows.Graphics.Printing.PrintManager is not yet supported in WindowsAppSDK. For more details see https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/what-is-supported
        private PrintManager _printManager = null;
        private CanvasPrintDocument _printDocument;

        int _pageCount = 0;
        int _printRows = 1;
        int _printCols = 1;

        private VectorList _vectorList;
        Win2DVectorRenderer _renderer;
        bool _needsRegen = false;

        int _printHandlerCount = 0;

        private void AddPrintHandlers()
        {
            if (_printHandlerCount > 0)
            {
                // print handler was not removed
                Analytics.ReportEvent("print-handler-assigned", new Dictionary<string, string> {
                    { "_printHandlerCount", _printHandlerCount.ToString() },
                });
            }
            else if (_printManager != null)
            {
                // _printManager is not null
                Analytics.ReportEvent("print-handler-not-null", new Dictionary<string, string> {
                    { "_printHandlerCount", _printHandlerCount.ToString() },
                });
            }
            else
            {
                // TODO Windows.Graphics.Printing.PrintManager.GetForCurrentView is not longer supported. For more details see https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/what-is-supported
                // TODO Windows.Graphics.Printing.PrintManager is not yet supported in WindowsAppSDK. For more details see https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/what-is-supported
                                _printManager = PrintManager.GetForCurrentView();
                _printManager.PrintTaskRequested += printManager_PrintTaskRequested;
                _printHandlerCount++;
            }
        }

        private void _printDocument_PrintTaskOptionsChanged(CanvasPrintDocument sender, CanvasPrintTaskOptionsChangedEventArgs args)
        {
            var deferral = args.GetDeferral();

            try
            {
                var pageDesc = args.PrintTaskOptions.GetPageDescription(1);

                sender.InvalidatePreview();

                _renderer.DestinationRect = new Rect(new Point(0, 0), pageDesc.PageSize);
                _renderer.DestinationRect = pageDesc.ImageableRect;
                _renderer.DestinationRect = new Rect(new Point(0, 0), new Size(pageDesc.ImageableRect.Width, pageDesc.ImageableRect.Height));

                PrintTaskOptionDetails details = PrintTaskOptionDetails.GetFromPrintTaskOptions(args.PrintTaskOptions);
                if ((string)details.Options["scale"].Value == "fit")
                {
                    Size paper = Globals.ActiveDrawing.PaperSize;
                    _renderer.SetWindow(new Point(0, 0), new Point(paper.Width, paper.Height));

                    _printRows = 1;
                    _printCols = 1;
                    _pageCount = 1;
                }
                else if ((string)details.Options["scale"].Value == "clip")
                {
                    _renderer.Scale = 96;

                    _printRows = 1;
                    _printCols = 1;
                    _pageCount = 1;
                }
                else
                {
                    double pwidth = Globals.ActiveDrawing.PaperSize.Width * 96;
                    double pheight = Globals.ActiveDrawing.PaperSize.Height * 96;

                    _printCols = (int)Math.Ceiling(pwidth / pageDesc.ImageableRect.Width);
                    _printRows = (int)Math.Ceiling(pheight / pageDesc.ImageableRect.Height);
                    _pageCount = Math.Max(1, _printRows * _printCols);
                }

                _printDocument.InvalidatePreview();

                sender.SetPageCount((uint)_pageCount);
                args.NewPreviewPageNumber = 1;
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void RemovePrintHandlers()
        {
            if (_printDocument != null)
            {
                _printDocument.Dispose();
                _printDocument = null;
            }

            if (_printManager == null)
            {
                // TODO Windows.Graphics.Printing.PrintManager.GetForCurrentView is not longer supported. For more details see https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/what-is-supported
                // TODO Windows.Graphics.Printing.PrintManager is not yet supported in WindowsAppSDK. For more details see https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/what-is-supported
                                _printManager = PrintManager.GetForCurrentView();
            }

            if (_printManager != null)
            {
                _printManager.PrintTaskRequested -= printManager_PrintTaskRequested;
                _printManager = null;
                --_printHandlerCount;
            }
        }

        async void printManager_PrintTaskRequested(// TODO Windows.Graphics.Printing.PrintManager is not yet supported in WindowsAppSDK. For more details see https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/what-is-supported
PrintManager sender, PrintTaskRequestedEventArgs e)
        {
            var deferral = e.Request.GetDeferral();

            try
            {
                if (_printDocument == null)
                {
                    // printer is not ready
                    // return;
                    await Utilities.ExecuteOnUIThread(() =>
                    {
                        _printDocument = new CanvasPrintDocument();
                        _printDocument.PrintTaskOptionsChanged += _printDocument_PrintTaskOptionsChanged;
                        _printDocument.Print += _printDocument_Print;
                        _printDocument.Preview += _printDocument_Preview;
                    });
                }

                PrintTask printTask = null;

                if (_vectorList == null)
                {
                    _vectorList = new VectorList();
                }
                if (_renderer == null)
                {
                    _renderer = new Win2DVectorRenderer(_printDocument.Device, new Rect(0, 0, 1000, 1000));
                    _needsRegen = true;
                }

                printTask = e.Request.CreatePrintTask("Back to the Drawing Board", sourceRequestedArgs =>
                {
                    PrintTaskOptionDetails printDetailedOptions = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);

                    printDetailedOptions.DisplayedOptions.Clear();

                    printDetailedOptions.DisplayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.MediaSize);
                    printDetailedOptions.DisplayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.Orientation);
                    printDetailedOptions.DisplayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.ColorMode);
                    printDetailedOptions.DisplayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.Copies);

                    PrintCustomItemListOptionDetails pageFormat =
                        printDetailedOptions.CreateItemListOption("scale", "Scale");

                    pageFormat.AddItem("clip", "Print full size (clipped)");
                    pageFormat.AddItem("tile", "Tile drawing at full size");
                    pageFormat.AddItem("fit", "Fit drawing to page");

                    printDetailedOptions.DisplayedOptions.Add("scale");

                    printDetailedOptions.OptionChanged += printDetailedOptions_OptionChanged;

                    sourceRequestedArgs.SetSource(_printDocument);
                });

                printTask.Completed += PrintTask_Completed;
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void _printDocument_Preview(CanvasPrintDocument sender, CanvasPreviewEventArgs args)
        {
            var deferral = args.GetDeferral();

            try
            {
                var ds = args.DrawingSession;
                await PrintPage(ds, args.PageNumber, args.PrintTaskOptions.GetPageDescription(args.PageNumber));
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void _printDocument_Print(CanvasPrintDocument sender, CanvasPrintEventArgs args)
        {
            var deferral = args.GetDeferral();

            try
            {
                for (uint page = 1; page <= _pageCount; ++page)
                {
                    using (CanvasDrawingSession ds = args.CreateDrawingSession())
                    {
                        Rect imageableRect = args.PrintTaskOptions.GetPageDescription(page).ImageableRect;
                        await PrintPage(ds, page, args.PrintTaskOptions.GetPageDescription(page));
                    }
                }
            }
            catch
            {

            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void PrintTask_Completed(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            if (_printDocument != null)
            {
                _printDocument.Dispose();
                _printDocument = null;
            }

            if (_renderer != null)
            {
                _renderer = null;
            }

            switch (args.Completion)
            {
                case PrintTaskCompletion.Abandoned:
                    break;
                case PrintTaskCompletion.Canceled:
                    break;
                case PrintTaskCompletion.Failed:
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await StandardAlerts.SimpleAlertAsync("Print task failed", "The drawing couldn't be printed on the selected printer");
                    });
                    break;
                case PrintTaskCompletion.Submitted:
                    break;
                default:
                    break;
            }
        }

        async void printDetailedOptions_OptionChanged(PrintTaskOptionDetails sender, PrintTaskOptionChangedEventArgs args)
        {
            if ((string)args.OptionId == "scale")
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    _printDocument.InvalidatePreview();
                });
            }
        }

        async Task PrintPage(CanvasDrawingSession ds, uint pageNumber, PrintPageDescription desc)
        {
            int page = Math.Max(1, (int)pageNumber);
            double left = ((page - 1) % _printCols) * desc.ImageableRect.Width - desc.ImageableRect.Left;
            double top = ((page - 1) / _printCols) * desc.ImageableRect.Height - desc.ImageableRect.Top;

            _renderer.XOffset = -left;
            _renderer.YOffset = -top;

            if (_needsRegen)
            {
                Theme _saveTheme = Globals.ActiveDrawing.Theme;
                if (Globals.Themes.ContainsKey("light"))
                {
                    Globals.ActiveDrawing.Theme = Globals.Themes["light"];
                }

                await _renderer.Regenerate(_vectorList);

                Globals.ActiveDrawing.Theme = _saveTheme;

                _needsRegen = false;
            }

            await _renderer.RenderVectorList(ds, _vectorList.AsList);
        }
    }
}
