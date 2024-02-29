using System;
using Cirros.Primitives;
#if UWP
using Windows.Storage;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
#else
using System.Windows;
#endif
using System.Collections.Generic;

namespace Cirros
{
    public class Events
    {
        public event ShowPropertiesHandler OnShowProperties;
        public delegate void ShowPropertiesHandler(object sender, ShowPropertiesEventArgs e);

        public void ShowProperties(object selection)
        {
            if (OnShowProperties != null)
            {
                ShowPropertiesEventArgs e = new ShowPropertiesEventArgs(selection);
                OnShowProperties(this, e);
            }
        }

        public event ShowContextMenuHandler OnShowContextMenu;
        public delegate void ShowContextMenuHandler(object sender, ShowContextMenuEventArgs e);

        public void ShowContextMenu(object selection, string target, int memberIndex = -1)
        {
            if (OnShowContextMenu != null)
            {
                ShowContextMenuEventArgs e = new ShowContextMenuEventArgs(selection, target, memberIndex);
                OnShowContextMenu(this, e);
            }
        }

        public event UIScaleChangedHandler OnUIScaleChanged;
        public delegate void UIScaleChangedHandler(object sender, EventArgs e);

        public void UIScaleChanged()
        {
            if (OnUIScaleChanged != null)
            {
                OnUIScaleChanged(this, new EventArgs());
            }
        }

        public event PointerEnteredDrawingAreaHandler OnPointerEnteredDrawingArea;
        public delegate void PointerEnteredDrawingAreaHandler(object sender, EventArgs e);

        public void PointerEnteredDrawingArea()
        {
            if (OnPointerEnteredDrawingArea != null)
            {
                OnPointerEnteredDrawingArea(this, new EventArgs());
            }
        }

        public event PointerLeftDrawingAreaHandler OnPointerLeftDrawingArea;
        public delegate void PointerLeftDrawingAreaHandler(object sender, EventArgs e);

        public void PointerLeftDrawingArea()
        {
            if (OnPointerLeftDrawingArea != null)
            {
                OnPointerLeftDrawingArea(this, new EventArgs());
            }
        }

        public event ShowConstructionPointHandler OnShowConstructionPoint;
        public delegate void ShowConstructionPointHandler(object sender, ShowConstructionPointEventArgs e);

        public void ShowConstructionPoint(string tag, Point location)
        {
            if (OnShowConstructionPoint != null)
            {
                ShowConstructionPointEventArgs e = new ShowConstructionPointEventArgs(tag, location);
                OnShowConstructionPoint(this, e);
            }
        }

        public event ShowRulersHandler OnShowRulers;
        public delegate void ShowRulersHandler(object sender, ShowRulersEventArgs e);

        public void ShowRulers(bool show)
        {
            if (OnShowRulers != null)
            {
                ShowRulersEventArgs e = new ShowRulersEventArgs(show);
                OnShowRulers(this, e);
            }
        }

        public event ShowMenuHandler OnShowMenu;
        public delegate void ShowMenuHandler(object sender, ShowMenuEventArgs e);

        public void ShowMenu(bool show)
        {
            if (OnShowMenu != null)
            {
                ShowMenuEventArgs e = new ShowMenuEventArgs(show);
                OnShowMenu(this, e);
            }
        }

        public event ShowTriangleFirstRunHandler OnShowTriangleFirstRun;
        public delegate void ShowTriangleFirstRunHandler(object sender, EventArgs e);

        public void ShowTriangleFirstRun()
        {
            if (OnShowTriangleFirstRun != null)
            {
                EventArgs e = new EventArgs();
                OnShowTriangleFirstRun(this, e);
            }
        }

        public event ShowControlPanelHandler OnShowControlPanel;
        public delegate void ShowControlPanelHandler(object sender, ShowControlPanelEventArgs e);

        public void ShowControlPanel(bool show)
        {
            if (OnShowControlPanel != null)
            {
                ShowControlPanelEventArgs e = new ShowControlPanelEventArgs(show);
                OnShowControlPanel(this, e);
            }
        }

        public event PrimitiveSelectionChangedHandler OnPrimitiveSelectionChanged;
        public delegate void PrimitiveSelectionChangedHandler(object sender, PrimitiveSelectionChangedEventArgs e);

        public void PrimitiveSelectionChanged(object selection, object member = null)
        {
            if (OnPrimitiveSelectionChanged != null)
            {
                PrimitiveSelectionChangedEventArgs e = new PrimitiveSelectionChangedEventArgs(selection, member);
                OnPrimitiveSelectionChanged(this, e);
            }
        }

        public event PrimitiveCreatedHandler OnPrimitiveCreated;
        public delegate void PrimitiveCreatedHandler(object sender, PrimitiveCreatedEventArgs e);

        public void PrimitiveCreated(object primitive)
        {
            if (OnPrimitiveCreated != null)
            {
                PrimitiveCreatedEventArgs e = new PrimitiveCreatedEventArgs(primitive);
                OnPrimitiveCreated(this, e);
            }
        }

        public event PrimitiveSelectionSizeChangedHandler OnPrimitiveSelectionSizeChanged;
        public delegate void PrimitiveSelectionSizeChangedHandler(object sender, PrimitiveSelectionSizeChangedEventArgs e);

        public void PrimitiveSelectionSizeChanged(Primitive p)
        {
            if (OnPrimitiveSelectionSizeChanged != null)
            {
                OnPrimitiveSelectionSizeChanged(this, new PrimitiveSelectionSizeChangedEventArgs(p));
            }
        }

        public event PrimitiveSelectionPropertyChangedHandler OnPrimitiveSelectionPropertyChanged;
        public delegate void PrimitiveSelectionPropertyChangedHandler(object sender, PrimitiveSelectionPropertyChangedEventArgs e);

        public void PrimitiveSelectionPropertyChanged(Primitive p)
        {
            if (OnPrimitiveSelectionPropertyChanged != null)
            {
                OnPrimitiveSelectionPropertyChanged(this, new PrimitiveSelectionPropertyChangedEventArgs(p));
            }
        }

        public event PromptChangedHandler OnPromptChanged;
        public delegate void PromptChangedHandler(object sender, PromptChangedEventArgs e);

        public string PromptChanged
        {
            set
            {
                if (OnPromptChanged != null)
                {
                    PromptChangedEventArgs e = new PromptChangedEventArgs(value);
                    OnPromptChanged(this, e);
                }
            }
        }

        public event ShowAlertHandler OnShowAlert;
        public delegate void ShowAlertHandler(object sender, ShowAlertEventArgs e);

        public void ShowAlert(string alertId)
        {
            if (OnShowAlert != null)
            {
                ShowAlertEventArgs e = new ShowAlertEventArgs(alertId);
                OnShowAlert(this, e);
            }
        }

        public event ThemeChangedHandler OnThemeChanged;
        public delegate void ThemeChangedHandler(object sender, EventArgs e);

        public void ThemeChanged()
        {
            if (OnThemeChanged != null)
            {
                OnThemeChanged(this, new EventArgs());
            }
        }

        public event DrawingShouldCloseHandler OnDrawingShouldClose;
        public delegate void DrawingShouldCloseHandler(object sender, DrawingShouldCloseEventArgs e);

        public void DrawingShouldClose(string reason)
        {
            if (OnDrawingShouldClose != null)
            {
                OnDrawingShouldClose(this, new DrawingShouldCloseEventArgs(reason));
            }
        }

        public event PointAddedHandler OnPointAdded;
        public delegate void PointAddedHandler(object sender, PointAddedEventArgs e);

        public void PointAdded(Point point, string key = " ")
        {
            if (OnPointAdded != null)
            {
                PointAddedEventArgs e = new PointAddedEventArgs(point, key);
                OnPointAdded(this, e);
            }
        }

        public event CoordinateDisplayHandler OnCoordinateDisplay;
        public delegate void CoordinateDisplayHandler(object sender, CoordinateDisplayEventArgs e);

        public void CoordinateDisplay(Point point)
        {
            if (OnCoordinateDisplay != null)
            {
                CoordinateDisplayEventArgs e = new CoordinateDisplayEventArgs(point);
                OnCoordinateDisplay(this, e);
            }
        }

        public event CoordinateEntryHandler OnCoordinateEntry;
        public delegate void CoordinateEntryHandler(object sender, EventArgs e);

        public void CoordinateEntry()
        {
            if (OnCoordinateEntry != null)
            {
                OnCoordinateEntry(this, new EventArgs());
            }
        }

        public event MeasureHandler OnMeasure;
        public delegate void MeasureHandler(object sender, MeasureEventArgs e);

        public void Measure(MeasureType measureType, List<Point> points)
        {
            if (OnMeasure != null)
            {
                MeasureEventArgs e = new MeasureEventArgs(measureType, points);
                OnMeasure(this, e);
            }
        }

#if UWP
        public event EditImageHandler OnEditImage;
        public delegate void EditImageHandler(object sender, EditImageEventArgs e);

        public void EditImage(string imageId, StorageFile file, string sourceName)
        {
            if (OnEditImage != null)
            {
                OnEditImage(this, new EditImageEventArgs(imageId, file, sourceName, null));
            }
        }

        public void EditImage(PImage pimage)
        {
            if (OnEditImage != null)
            {
                OnEditImage(this, new EditImageEventArgs(null, null, null, pimage));
            }
        }

        public event ImageChangedHandler OnImageChanged;
        public delegate void ImageChangedHandler(object sender, ImageChangedEventArgs e);

        public void ImageChanged(StorageFile file)
        {
            if (OnImageChanged != null)
            {
                ImageChangedEventArgs e = new ImageChangedEventArgs(file);
                OnImageChanged(this, e);
            }
        }
#else
#endif

        public event GroupInstantiatedHandler OnGroupInstantiated;
        public delegate void GroupInstantiatedHandler(object sender, GroupInstantiatedEventArgs e);

        public void GroupInstantiated(PInstance instance, bool finished)
        {
            if (OnGroupInstantiated != null)
            {
                GroupInstantiatedEventArgs e = new GroupInstantiatedEventArgs(instance, finished);
                OnGroupInstantiated(this, e);
            }
        }


        public void CoordinateDisplay(Point point, double dx, double dy, double distance, double angle)
        {
            if (OnCoordinateDisplay != null)
            {
                CoordinateDisplayEventArgs e = new CoordinateDisplayEventArgs(point, dx, dy, distance, angle);
                OnCoordinateDisplay(this, e);
            }
        }

        public event DrawingCanvasLoadedHandler OnDrawingCanvasLoaded;
        public delegate void DrawingCanvasLoadedHandler(object sender, DrawingCanvasLoadedEventArgs e);

        public void DrawingCanvasLoaded(object parameter)
        {
            if (OnDrawingCanvasLoaded != null)
            {
                OnDrawingCanvasLoaded(this, new DrawingCanvasLoadedEventArgs(parameter));
            }
        }

        public event DrawingLoadingHandler OnDrawingLoading;
        public delegate void DrawingLoadingHandler(object sender, DrawingLoadingEventArgs e);

        public void DrawingLoading(string name, ulong size)
        {
            if (OnDrawingLoading != null)
            {
                OnDrawingLoading(this, new DrawingLoadingEventArgs(name, size));
            }
        }

        public event DrawingLoadedHandler OnDrawingLoaded;
        public delegate void DrawingLoadedHandler(object sender, DrawingLoadedEventArgs e);

        public void DrawingLoaded(string name)
        {
            if (OnDrawingLoaded != null)
            {
                OnDrawingLoaded(this, new DrawingLoadedEventArgs(name));
            }
        }

        public event DrawingNameChangedHandler OnDrawingNameChanged;
        public delegate void DrawingNameChangedHandler(object sender, DrawingLoadedEventArgs e);

        public void DrawingNameChanged(string name)
        {
            if (OnDrawingNameChanged != null)
            {
                OnDrawingNameChanged(this, new DrawingLoadedEventArgs(name));
            }
        }

        public event PrintingStartedHandler OnPrintingStarted;
        public delegate void PrintingStartedHandler(object sender, EventArgs e);

        public void PrintingStarted()
        {
            if (OnPrintingStarted != null)
            {
                OnPrintingStarted(this, new EventArgs());
            }
        }

        public event PrintingFinishedHandler OnPrintingFinished;
        public delegate void PrintingFinishedHandler(object sender, EventArgs e);

        public void PrintingFinished()
        {
            if (OnPrintingFinished != null)
            {
                OnPrintingFinished(this, new EventArgs());
            }
        }

        public event UndoStackChangedHandler OnUndoStackChanged;
        public delegate void UndoStackChangedHandler(object sender, EventArgs e);

        public void UndoStackChanged()
        {
            if (OnUndoStackChanged != null)
            {
                OnUndoStackChanged(this, new EventArgs());
            }
        }

        public event CommandInvokedHandler OnCommandInvoked;
        public delegate void CommandInvokedHandler(object sender, CommandInvokedEventArgs e);

        public void CommandInvoked(object sender, object command, object page)
        {
            if (OnCommandInvoked != null)
            {
                OnCommandInvoked(this, new CommandInvokedEventArgs(command, page));
            }
        }

        public event CommandFinishedHandler OnCommandFinished;
        public delegate void CommandFinishedHandler(object sender, CommandFinishedEventArgs e);

        public void CommandFinished(string key, bool shift, bool control, bool gmk)
        {
            if (OnCommandFinished != null)
            {
                OnCommandFinished(this, new CommandFinishedEventArgs(key, shift, control, gmk));
            }
        }

        public event OptionsChangedHandler OnOptionsChanged;
        public delegate void OptionsChangedHandler(object sender, EventArgs e);

        public void OptionsChanged()
        {
            if (OnOptionsChanged != null)
            {
                OnOptionsChanged(this, new EventArgs());
            }
        }

        public event PaperSizeChangedHandler OnPaperSizeChanged;
        public delegate void PaperSizeChangedHandler(object sender, EventArgs e);

        public void PaperSizeChanged()
        {
            if (OnPaperSizeChanged != null)
            {
                OnPaperSizeChanged(this, new EventArgs());
            }
        }

        public event DrawingClearedHandler OnDrawingCleared;
        public delegate void DrawingClearedHandler(object sender, EventArgs e);

        public void DrawingCleared()
        {
            if (OnDrawingCleared != null)
            {
                OnDrawingCleared(this, new EventArgs());
            }
        }

        public event DrawingLayoutChangedHandler OnDrawingLayoutChanged;
        public delegate void DrawingLayoutChangedHandler(object sender, EventArgs e);

        public void DrawingLayoutChanged()
        {
            if (OnDrawingLayoutChanged != null)
            {
                OnDrawingLayoutChanged(this, new EventArgs());
            }
        }

        public event GridChangedHandler OnGridChanged;
        public delegate void GridChangedHandler(object sender, EventArgs e);

        public void GridChanged()
        {
            if (OnGridChanged != null)
            {
                OnGridChanged(this, new EventArgs());
            }
        }

        public event AttributesChangedHandler OnAttributesChanged;
        public delegate void AttributesChangedHandler(object sender, EventArgs e);

        public void AttributesChanged()
        {
            if (OnAttributesChanged != null)
            {
                OnAttributesChanged(this, new EventArgs());
            }
        }

        public event AttributesListChangedHandler OnAttributesListChanged;
        public delegate void AttributesListChangedHandler(object sender, EventArgs e);

        public void AttributesListChanged()
        {
            if (OnAttributesListChanged != null)
            {
                OnAttributesListChanged(this, new EventArgs());
            }
        }

        public event LayerSelectionChangedHandler OnLayerSelectionChanged;
        public delegate void LayerSelectionChangedHandler(object sender, EventArgs e);

        public void LayerSelectionChanged()
        {
            if (OnLayerSelectionChanged != null)
            {
                OnLayerSelectionChanged(this, new EventArgs());
            }
        }

        public event ActiveLayerShownHandler OnActiveLayerShown;
        public delegate void ActiveLayerShownHandler(object sender, ActiveLayerEventArgs e);

        public void ActiveLayerShown(int layerId)
        {
            if (OnActiveLayerShown != null)
            {
                OnActiveLayerShown(this, new ActiveLayerEventArgs(layerId));
            }
        }

        public event OriginChangedHandler OnOriginChanged;
        public delegate void OriginChangedHandler(object sender, OriginChangedEventArgs e);

        public void OriginChanged(Point origin, bool finished)
        {
            if (OnOriginChanged != null)
            {
                OnOriginChanged(this, new OriginChangedEventArgs(origin, finished));
            }
        }

        public event MoveCopyOffsetChangedHandler OnMoveCopyOffsetChanged;
        public delegate void MoveCopyOffsetChangedHandler(object sender, MoveCopyOffsetChangedEventArgs e);

        public void MoveCopyOffsetChanged(Point offset)
        {
            if (OnMoveCopyOffsetChanged != null)
            {
                OnMoveCopyOffsetChanged(this, new MoveCopyOffsetChangedEventArgs(offset));
            }
        }
    }

    public class ShowPropertiesEventArgs : EventArgs
    {
        public object Selection { get; private set; }

        public ShowPropertiesEventArgs(object selection)
        {
            Selection = selection;
        }
    }

    public class PromptChangedEventArgs : EventArgs
    {
        public string Prompt { get; private set; }

        public PromptChangedEventArgs(string prompt)
        {
            Prompt = prompt;
        }
    }

    public class ShowAlertEventArgs : EventArgs
    {
        public string AlertId { get; private set; }

        public ShowAlertEventArgs(string alertId)
        {
            AlertId = alertId;
        }
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public string Prompt { get; private set; }

        public ThemeChangedEventArgs(string prompt)
        {
            Prompt = prompt;
        }
    }

    public class ShowContextMenuEventArgs : EventArgs
    {
        public object Selection { get; private set; }
        public int MemberIndex { get; private set; }
        public string Target { get; private set; }

        public ShowContextMenuEventArgs(object selection, string target, int memberIndex)
        {
            Selection = selection;
            MemberIndex = memberIndex;
            Target = target;
        }
    }

    public class ShowConstructionPointEventArgs : EventArgs
    {
        public string Tag { get; private set; }
        public Point Location { get; private set; }

        public ShowConstructionPointEventArgs(string tag, Point location)
        {
            Tag = tag;
            Location = location;
        }
    }

    public class ShowRulersEventArgs : EventArgs
    {
        public bool Show { get; private set; }

        public ShowRulersEventArgs(bool show)
        {
            Show = show;
        }
    }

    public class ShowMenuEventArgs : EventArgs
    {
        public bool Show { get; private set; }

        public ShowMenuEventArgs(bool show)
        {
            Show = show;
        }
    }

    public class ShowControlPanelEventArgs : EventArgs
    {
        public bool Show { get; private set; }

        public ShowControlPanelEventArgs(bool show)
        {
            Show = show;
        }
    }

    public class PrimitiveSelectionChangedEventArgs : EventArgs
    {
        public object Selection { get; private set; }
        public object Member { get; private set; }

        public PrimitiveSelectionChangedEventArgs(object selection, object member)
        {
            Selection = selection;
            Member = member;
        }
    }

    public class PrimitiveCreatedEventArgs : EventArgs
    {
        public object Primitive { get; private set; }

        public PrimitiveCreatedEventArgs(object primitive)
        {
            Primitive = primitive;
        }
    }

    public class ActiveLayerEventArgs : EventArgs
    {
        public int Layer { get; private set; }

        public ActiveLayerEventArgs(int layer)
        {
            Layer = layer;
        }
    }

    public class DrawingCanvasLoadedEventArgs : EventArgs
    {
        public object Parameter { get; private set; }

        public DrawingCanvasLoadedEventArgs(object parameter)
        {
            Parameter = parameter;
        }
    }

    public class DrawingLoadingEventArgs : EventArgs
    {
        public string Name { get; private set; }
        public ulong Size { get; private set; }

        public DrawingLoadingEventArgs(string name, ulong size)
        {
            Name = name;
            Size = size;
        }
    }

    public class DrawingLoadedEventArgs : EventArgs
    {
        public string Name { get; private set; }

        public DrawingLoadedEventArgs(string name)
        {
            Name = name;
        }
    }

    public class DrawingShouldCloseEventArgs : EventArgs
    {
        public string Reason { get; private set; }

        public DrawingShouldCloseEventArgs(string reason)
        {
            Reason = reason;
        }
    }

    public class PrimitiveSelectionSizeChangedEventArgs : EventArgs
    {
        public Primitive Primitive { get; private set; }

        public PrimitiveSelectionSizeChangedEventArgs(Primitive primitive)
        {
            Primitive = primitive;
        }
    }

    public class PrimitiveSelectionPropertyChangedEventArgs : EventArgs
    {
        public Primitive Primitive { get; private set; }

        public PrimitiveSelectionPropertyChangedEventArgs(Primitive primitive)
        {
            Primitive = primitive;
        }
    }

    public class CommandInvokedEventArgs : EventArgs
    {
        public object Command { get; private set; }
        public object Page { get; private set; }

        public CommandInvokedEventArgs(object command, object page)
        {
            Command = command;
            Page = page;
        }
    }

    public class CommandFinishedEventArgs : EventArgs
    {
        public string Key { get; private set; }
        public bool Shift { get; private set; }
        public bool Control { get; private set; }
        public bool Gmk { get; private set; }

        public CommandFinishedEventArgs(string key, bool shift, bool control, bool gmk)
        {
            Key = key;
            Shift = shift;
            Control = control;
            Gmk = gmk;
        }
    }

    public class OriginChangedEventArgs : EventArgs
    {
        public Point Origin { get; private set; }
        public bool Finished { get; private set; }

        public OriginChangedEventArgs(Point origin, bool finished)
        {
            Origin = origin;
            Finished = finished;
        }
    }

    public class MoveCopyOffsetChangedEventArgs : EventArgs
    {
        public Point Offset { get; private set; }

        public MoveCopyOffsetChangedEventArgs(Point offset)
        {
            Offset = offset;
        }
    }

    public class PointAddedEventArgs : EventArgs
    {
        public Point Point { get; private set; }
        public string Key { get; private set; }

        public PointAddedEventArgs(Point p, string key)
        {
            Point = p;
            Key = key;
        }
    }

    public class CoordinateDisplayEventArgs : EventArgs
    {
        public CoordinateDisplayType CoordinateDisplayType { get; private set; }
        public Point Point { get; private set; }
        public double Distance { get; private set; }
        public double Angle { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }

        public CoordinateDisplayEventArgs(Point p)
        {
            CoordinateDisplayType = CoordinateDisplayType.coordinate;
            Point = p;
        }

        public CoordinateDisplayEventArgs(Point p, Point s)
        {
            CoordinateDisplayType = CoordinateDisplayType.size;
            Point = p;
            Width = s.X;
            Height = s.Y;
        }

        public CoordinateDisplayEventArgs(Point p, double dx, double dy, double d, double a)
        {
            CoordinateDisplayType = CoordinateDisplayType.vector;
            Point = p;
            Width = dx;
            Height = dy;
            Distance = d;
            Angle = a;
        }
    }

    public enum CoordinateDisplayType
    {
        coordinate,
        size,
        vector,
        none
    }

#if UWP
    public class EditImageEventArgs : EventArgs
    {
        public string ImageId { get; private set; }
        public string SourceName { get; private set; }
        public StorageFile SourceFile { get; private set; }
        public PImage PImage { get; private set; }

        public EditImageEventArgs(string imageId, StorageFile file, string sourceName, PImage pimage)
        {
            ImageId = imageId;
            SourceName = sourceName;
            SourceFile = file;
            PImage = pimage;
        }
    }
#else
#endif

    public class MeasureEventArgs : EventArgs
    {
        public MeasureType MeasureType { get; private set; }
        public List<Point> Points { get; private set; }

        public MeasureEventArgs(MeasureType measureType, List<Point> points)
        {
            MeasureType = measureType;
            Points = points;
        }
    }

    public class GroupInstantiatedEventArgs : EventArgs
    {
        public PInstance Instance { get; private set; }
        public bool Finished { get; private set; }

        public GroupInstantiatedEventArgs(PInstance instance, bool finished)
        {
            Instance = instance;
            Finished = finished;
        }
    }

#if UWP
    public class ImageChangedEventArgs : EventArgs
    {
        public StorageFile File { get; private set; }

        public ImageChangedEventArgs(StorageFile file)
        {
            File = file;
        }
    }
#endif

    public enum MeasureType
    {
        distance,
        angle,
        area,
        none
    }
}
