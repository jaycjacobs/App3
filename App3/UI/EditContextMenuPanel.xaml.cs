using Cirros;
using Cirros.Actions;
using Cirros.Commands;
using Cirros.Primitives;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using App3;
using Microsoft.UI;

namespace CirrosUI
{
    public sealed partial class EditContextMenuPanel : UserControl, IContextMenu
    {
        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;
        Popup _popup;
        Point _workCanvasOffset;

        Rect _box = Rect.Empty;

        double _baseHeight = 0;

        static string _selectedOption = null;

        Primitive _selection = null;

        static List<string>[] _lineOptions =
        {
            new List<string> { "0", "Move vertex",      "A_EditPoints" },
            new List<string> { "0", "Insert vertex",    "A_EditInsertVertex" },
            new List<string> { "0", "Delete vertex",    "A_EditDeleteVertex" },
            new List<string> { "1", "Move segment",     "A_EditMoveSegment" },
            new List<string> { "1", "Offset move",      "A_EditOffsetMove" },
            new List<string> { "1", "Offset copy",      "A_EditOffsetCopy" },
            new List<string> { "2", "Gap",              "A_EditGap" },
            new List<string> { "2", "Extend/trim",      "A_EditExtendTrim" },
            new List<string> { "2", "Stroke",           "A_EditStroke" },
            new List<string> { "2", "Properties",       "A_SelectProperties" },
        };

        static List<string>[] _lineNoGapOptions =
        {
            new List<string> { "0", "Move vertex",      "A_EditPoints" },
            new List<string> { "0", "Insert vertex",    "A_EditInsertVertex" },
            new List<string> { "0", "Delete vertex",    "A_EditDeleteVertex" },
            new List<string> { "1", "Move segment",     "A_EditMoveSegment" },
            new List<string> { "1", "Offset move",      "A_EditOffsetMove" },
            new List<string> { "1", "Offset copy",      "A_EditOffsetCopy" },
            new List<string> { "2", "Stroke",           "A_EditStroke" },
            new List<string> { "2", "Extend/trim",      "A_EditExtendTrim" },
            new List<string> { "2", "Properties",       "A_SelectProperties" },
        };

        static List<string>[] _curveOptions =
        {
            new List<string> { "0", "Move vertex",      "A_EditPoints" },
            new List<string> { "0", "Insert vertex",    "A_EditInsertVertex" },
            new List<string> { "0", "Delete vertex",    "A_EditDeleteVertex" },
            new List<string> { "1", "Offset move",      "A_EditOffsetMove" },
            new List<string> { "1", "Offset copy",      "A_EditOffsetCopy" },
            new List<string> { "2", "Stroke",           "A_EditStroke" },
            new List<string> { "2", "Properties",       "A_SelectProperties" },
        };

        static List<string>[] _arcOptions =
        {
            new List<string> { "0", "Move point",       "A_EditPoints" },
            new List<string> { "0", "Gap",              "A_EditGap" },
            new List<string> { "0", "Stroke",           "A_EditStroke" },
            new List<string> { "1", "Properties",       "A_SelectProperties" },
        };

        static List<string>[] _transformedObjectOptions =
        {
            new List<string> { "0", "Properties",       "A_SelectProperties" },
            new List<string> { "0", "Stroke",           "A_EditStroke" },
        };

        static List<string>[] _dimensionOptions =
        {
            new List<string> { "0", "Move point",       "A_EditPoints" },
            new List<string> { "0", "Delete point",    "A_EditDeleteVertex" },
            new List<string> { "1", "Properties",       "A_SelectProperties" },
        };

        static List<string>[] _defaultOptions =
        {
            new List<string> { "0", "Move point",       "A_EditPoints" },
            new List<string> { "0", "Stroke",           "A_EditStroke" },
            new List<string> { "0", "Properties",       "A_SelectProperties" },
        };

        static List<string>[] _textOptions =
        {
            new List<string> { "0", "Change alignment points",  "A_EditPoints" },
            new List<string> { "1", "Edit text",                "A_EditText" },
            new List<string> { "1", "Show properties",          "A_SelectProperties" },
        };

        static List<string>[] _groupOptions =
        {
            //new List<string> { "0", "Change alignment points",  "A_EditPoints" },
            new List<string> { "0", "Ungroup",                  "A_SelectUngroup" }, 
            new List<string> { "0", "Save as symbol",           "A_SaveSymbol" },
            new List<string> { "1", "Show properties",          "A_SelectProperties" },
            new List<string> { "1", "Flip along axis",          "A_EditFlip" },
            new List<string> { "2", "Fill in text attributes",  "A_EditTextAttribute" },
        };

        static List<string>[] _imageOptions =
        {   
            new List<string> { "0", "Move corner",          "A_EditPoints" },
            new List<string> { "0", "Modify image",         "A_UpdateImage" },
            new List<string> { "1", "Show properties",      "A_SelectProperties" },
        };

        public EditContextMenuPanel()
        {
            this.InitializeComponent();

            this.Loaded += EditContextMenuPanel_Loaded;
            this.Unloaded += EditContextMenuPanel_Unloaded;
            this.PointerPressed += EditContextMenuPanel_PointerPressed;
            this.PointerMoved += EditContextMenuPanel_PointerMoved;
            this.PointerReleased += EditContextMenuPanel_PointerReleased;

            _jumpIconPanel.Visibility = Globals.UIVersion > 0 ? Visibility.Visible : Visibility.Collapsed;

            Globals.Events.OnPrimitiveSelectionPropertyChanged += Events_OnPrimitiveSelectionPropertyChanged;
            Globals.Events.OnPrimitiveSelectionSizeChanged += Events_OnPrimitiveSelectionSizeChanged;

            DataContext = Globals.UIDataContext;
        }

        private void EditContextMenuPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Globals.Events.OnPrimitiveSelectionPropertyChanged -= Events_OnPrimitiveSelectionPropertyChanged;
            Globals.Events.OnPrimitiveSelectionSizeChanged -= Events_OnPrimitiveSelectionSizeChanged;
        }

        private void Events_OnPrimitiveSelectionSizeChanged(object sender, PrimitiveSelectionSizeChangedEventArgs e)
        {
           
            if (_selection != e.Primitive)
            {
                Select(e.Primitive);
            }
            else if (_propertiesContentControl.Content is AttributePropertyPanel)
            {
                ((AttributePropertyPanel)_propertiesContentControl.Content).Update();
            }
        }

        private void Events_OnPrimitiveSelectionPropertyChanged(object sender, PrimitiveSelectionPropertyChangedEventArgs e)
        {
            if (_selection != e.Primitive)
            {
                Select(e.Primitive);
            }
            else if (_propertiesContentControl.Content is AttributePropertyPanel)
            {
                ((AttributePropertyPanel)_propertiesContentControl.Content).Update();
            }
        }

        public void WillClose()
        {
            if (_propertiesContentControl.Content is AttributePropertyPanel)
            {
                ((AttributePropertyPanel)_propertiesContentControl.Content).Update();
            }
        }

        private void SelectObjectsClick(object sender, RoutedEventArgs e)
        {
            if (_selection is Primitive)
            {
                Globals.CommandProcessorParameter = _selection;
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.select;
        }

        private void EditGroupClick(object sender, RoutedEventArgs e)
        {
            if (_selection is Primitive)
            {
                Globals.CommandProcessorParameter = _selection;
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.editgroup;
        }

        private void CopyPasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selection is Primitive)
            {
                Globals.CommandProcessorParameter = _selection;
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.copypaste;
        }

        public Point WorkCanvasOffset
        {
            set
            {
                _workCanvasOffset = value;
            }
        }

        void EditContextMenuPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // Assume that Show() has already been called

            _popup = this.Parent as Popup;

            double frameWidth = App.Window.Bounds.Width;
            double frameHeight = App.Window.Bounds.Height;

            if (Globals.View != null)
            {
                frameWidth = Globals.View.ViewPortSize.Width;
                frameHeight = Globals.View.ViewPortSize.Height;
            }

            if (_popup != null && _box != Rect.Empty && ActualHeight > 0 && ActualWidth > 0 && _popup.HorizontalOffset == 0)
            {
                double leftSpace = _box.Left + _workCanvasOffset.X;
                double rightSpace = frameWidth - (_box.Right + _workCanvasOffset.X);
                double topSpace = _box.Top + _workCanvasOffset.Y;
                double bottomSpace = frameHeight - (_box.Bottom + _workCanvasOffset.Y);
                bool horizontallyImpeded = false;

                if (Math.Max(leftSpace, rightSpace) < (ActualWidth + 60))
                {
                    _popup.HorizontalOffset = frameWidth / 3;
                    horizontallyImpeded = true;
                }
                else
                {
                    _popup.HorizontalOffset = (leftSpace > rightSpace ? _box.Left - ActualWidth - 40 : _box.Right + 40) + _workCanvasOffset.X;
                }

                if (Math.Max(topSpace, bottomSpace) < (ActualHeight + 60))
                {
                    _popup.VerticalOffset = frameHeight / 3;
                }
                else if (horizontallyImpeded)
                {
                    _popup.VerticalOffset = (topSpace > bottomSpace ? _box.Top - ActualHeight - 40 : _box.Bottom + 40) + _workCanvasOffset.Y;
                }
                else
                {
                    _popup.VerticalOffset = _box.Top;
                }
            }

            _offsetDistanceBox.Value = Globals.ActiveDrawing.PaperToModel(Globals.EditOffsetLineDistance);
        }

        void EditContextMenuPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _popup = this.Parent as Popup;
            if (_popup != null)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _dragXOff = p.X - _popup.HorizontalOffset;
                _dragYOff = p.Y - _popup.VerticalOffset;
                _isDragging = true;

                CapturePointer(e.Pointer);
            }
        }

        void EditContextMenuPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _popup.HorizontalOffset = p.X - _dragXOff;
                _popup.VerticalOffset = p.Y - _dragYOff;
            }
        }

        void EditContextMenuPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                ReleasePointerCapture(e.Pointer);
                _isDragging = false;
            }
        }

        private void AddContextButtons(List<string>[] options)
        {
            if (options != null)
            {
                foreach (List<string> p in options)
                {
                    string row = p[0];
                    string content =  p[1];
                    string tag = p[2];

                    // really ugly hack:
                    if (tag == "A_EditTextAttribute" && _selection is PInstance)
                    {
                        if (((PInstance)_selection).AttributeList.Count == 0)
                        {
                            continue;
                        }
                    }

                    StateButton b = new StateButton();
                    b.Style = (Style)(Application.Current.Resources["ContextMenuStateButtonStyle"]);
                    b.OnClick += B_OnClick;
                    b.Text = content;
                    b.Tag = tag;
                    b.FontSize = Globals.UIDataContext.UIFontSizeNormal;

                    if (p.Count > 3)
                    {
                        b.IsEnabled = p[3] == "true";
                    }

                    switch (row)
                    {
                        case "0":
                            _row0.Children.Add(b);
                            break;

                        case "1":
                            _row1.Children.Add(b);
                            break;

                        case "2":
                            _row2.Children.Add(b);
                            break;
                    }
                }

                int rowHeight = 32;

                if (_row2.Children.Count > 0)
                {
                    _baseHeight = 3 * rowHeight + 30;
                }
                else if (_row1.Children.Count > 0)
                {
                    _baseHeight = 2 * rowHeight + 30;
                }
                else
                {
                    _baseHeight = rowHeight + 30;
                }

                FrameworkElement button = null;

                if (_selectedOption != null)
                {
                    button = FindButtonByOption(_selectedOption);
                    if (button == null)
                    {
                        button = FindButtonByOption(options[0][2]);
                    }
                }
                else
                {
                    button = FindButtonByOption(options[0][2]);
                }

                SelectButton(button);
            }
        }

        FrameworkElement _selectedButton = null;

        private void B_OnClick(object sender, EventArgs e)
        {
            if (sender is FrameworkElement b)
            {
                SelectButton(b);
            }
        }

        private async void InvokeOption(string option)
        {
            bool handled = false;

            if (_selectedOption == "A_SelectProperties" && option != "A_SelectProperties")
            {
                if (_propertiesContentControl.Content is AttributePropertyPanel && option != "A_SelectProperties")
                {
                    ((AttributePropertyPanel)_propertiesContentControl.Content).Primitive = null;
                }
            }

            _offsetOptions.Visibility = Visibility.Collapsed;
            _textAttributeOptions.Visibility = Visibility.Collapsed;
            _propertiesPanel.Visibility = Visibility.Collapsed;

            switch (option)
            {
                case "A_SelectProperties":
                    UserControl propertyPanel = new AttributePropertyPanel(_selection);
                    _propertiesContentControl.Content = propertyPanel;
                    _propertiesPanel.Visibility = Visibility.Visible;
                    _selectedOption = option;
                    break;

                case "A_SelectUngroup":
                    break;

                case "A_EditFlip":
                    SelectButton(null);
                    break;

                case "A_EditStroke":
                    SelectButton(null);
                    break;

                case "A_EditTextAttribute":
                    {
                        _currentTextAttribute = 0;
                        ShowTextAttribute();
                        handled = true;
                    }
                    break;

                case "A_SaveSymbol":
                    if (_selection is PInstance)
                    {
                        Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                        if (g != null)
                        {
                            await FileHandling.SaveSymbolAsAsync(g);
                            handled = true;
                            SelectButton(null);
                        }
                    }
                    break;

                case "A_EditMoveSegment":
                    _selectedOption = option;
                    break;

                case "A_EditOffsetMove":
                case "A_EditOffsetCopy":
                    //this.Height = 197;
                    _offsetOptions.Visibility = Visibility.Visible;
                    _selectedOption = option;
                    break;

                case "A_UpdateImage":
                    if (_selection is PImage)
                    {
                        PImage image = _selection as PImage;
                        if (image != null)
                        {
                            Globals.Events.EditImage(image);

                            handled = true;

                            SelectButton(FindButtonByOption("A_EditPoints"));
                        }
                    }
                    break;

                default:
                    _selectedOption = option;
                    break;
            }

            if (handled == false && Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.Invoke(option, null);
            }
        }

        private FrameworkElement FindButtonByOption(string command)
        {
            foreach (FrameworkElement e in _row0.Children)
            {
                if (e.Tag is string s && s == command)
                {
                    return e;
                }
            }

            foreach (FrameworkElement e in _row1.Children)
            {
                if (e.Tag is string s && s == command)
                {
                    return e;
                }
            }

            foreach (FrameworkElement e in _row2.Children)
            {
                if (e.Tag is string s && s == command)
                {
                    return e;
                }
            }

            return null; 
        }

        private void SelectButton(FrameworkElement button, bool invoke = true)
        {
            if (_selectedButton != null)
            {
                if (_selectedButton is StateButton b)
                {
                    b.IsSelected = false;
                }
                _selectedButton = null;
            }

            if (button != null)
            {
                _selectedButton = button;
                if (_selectedButton is StateButton b)
                {
                    b.IsSelected = true;
                }

                if (invoke && _selectedButton.Tag is string s)
                {
                    InvokeOption(s);
                }
            }
        }

        public void Select(Primitive p)
        {
            _selection = p;

            _row0.Children.Clear();
            _row1.Children.Clear();
            _row2.Children.Clear();

            if (_selection != null)
            {
                string title = _selection.TypeName.ToString();

                switch (_selection.TypeName)
                {
                    case PrimitiveType.Line:
                    case PrimitiveType.Doubleline:
                        AddContextButtons(_lineOptions);
                        break;

                    case PrimitiveType.Polygon:
                    case PrimitiveType.Arrow:
                        AddContextButtons(_lineNoGapOptions);
                        break;

                    case PrimitiveType.Arc:
                    case PrimitiveType.Arc3:
                        if (_selection.IsTransformed)
                        {
                            title = "Transformed " + _selection.TypeName.ToString();
                            AddContextButtons(_transformedObjectOptions);
                        }
                        else
                        {
                            AddContextButtons(_arcOptions);
                        }
                        break;

                    case PrimitiveType.BSpline:
                        title = "Curve";
                        if (_selection.IsTransformed)
                        {
                            title = "Transformed " + _selection.TypeName.ToString();
                            AddContextButtons(_transformedObjectOptions);
                        }
                        else
                        {
                            AddContextButtons(_curveOptions);
                        }
                        break;

                    case PrimitiveType.Text:
                        AddContextButtons(_textOptions);
                        break;

                    case PrimitiveType.Instance:
                        PInstance pi = _selection as PInstance;
                        Group g = Globals.ActiveDrawing.GetGroup(pi.GroupName);
                        if (g != null)
                        {
                                title = "Group instance";
                                _selectedOption = "A_SelectProperties";
                                AddContextButtons(_groupOptions);
                        }
                        break;

                    case PrimitiveType.Image:
                        AddContextButtons(_imageOptions);
                        break;

                    case PrimitiveType.Dimension:
                        if (_selection.IsTransformed)
                        {
                            title = "Transformed " + _selection.TypeName.ToString();
                            AddContextButtons(_transformedObjectOptions);
                        }
                        else
                        {
                            AddContextButtons(_dimensionOptions);
                        }
                        break;

                    case PrimitiveType.Rectangle:
                        if (_selection.IsTransformed)
                        {
                            _selection.Normalize(false);
                        }
                        if (_selection.IsTransformed)
                        {
                            title = "Transformed " + _selection.TypeName.ToString();
                            AddContextButtons(_transformedObjectOptions);
                        }
                        else
                        {
                            AddContextButtons(_defaultOptions);
                        }
                        break;

                    case PrimitiveType.Ellipse:
                        if (_selection.IsTransformed)
                        {
                            _selection.Normalize(false);
                        }
                        if (_selection.IsTransformed)
                        {
                            title = "Transformed " + _selection.TypeName.ToString();
                            AddContextButtons(_transformedObjectOptions);
                        }
                        else
                        {
                            AddContextButtons(_defaultOptions);
                        }
                        break;

                    default:
                        break;
                }

                _title.Text = string.Format("Edit {0}", title.ToLower());

                _noSelectionPanel.Visibility = Visibility.Collapsed;

                Point ul = Globals.View.PaperToDisplay(new Point(_selection.Box.Left, _selection.Box.Top));
                Point lr = Globals.View.PaperToDisplay(new Point(_selection.Box.Right, _selection.Box.Bottom));
                _box = new Rect(ul, lr);
            }
            else
            {
                _title.Text = "No selection";
                _noSelectionPanel.Visibility = Visibility.Visible;
                _box = Rect.Empty;
                InvokeOption(null);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.Events.ShowContextMenu(null, null);
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (_offsetDistanceBox.Value != 0)
            {
                if (_selectedOption == "A_EditOffsetMove" || _selectedOption == "A_EditOffsetCopy")
                {
                    if (_selection is PLine)
                    {
                        double distance = Globals.ActiveDrawing.ModelToPaper(_offsetDistanceBox.Value);
                        Globals.CommandProcessor.Invoke(_selectedOption, distance);
                        Globals.EditOffsetLineDistance = distance;
                    }
                }
            }
        }

        private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Button b)
            {
                b.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        #region Edit instance text attributes

        int _currentTextAttribute = 0;
        bool _attributeValueIsDirty = false;
        bool _attributeUndoHandled = false;
        int _attributeMaxLines = 1;

        void SaveAttributeValue()
        {
            if (_attributeValueIsDirty)
            {
                PInstance pi = _selection as PInstance;
                if (pi != null && pi.AttributeList.Count > _currentTextAttribute)
                {
                    if (_attributeUndoHandled == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.EditAttribute, pi, pi.CloneAttributeList());
                        _attributeUndoHandled = true;
                    }

                    GroupAttribute ga = pi.AttributeList[_currentTextAttribute];
                    ga.Value = _attributeBox.Text.Trim().Replace("\r", "\n");
                    pi.Draw();
                }

                _attributeValueIsDirty = false;
            }
        }

        private void _nextAttributeButton_Click(object sender, RoutedEventArgs e)
        {
            ShowNextTextAttribute();
        }

        void ShowNextTextAttribute()
        {
            SaveAttributeValue();

            _currentTextAttribute++;

            ShowTextAttribute();
        }

        void ShowTextAttribute()
        {
            PInstance pi = _selection as PInstance;
            if (pi != null && pi.AttributeList.Count > 0)
            {
                if (_currentTextAttribute >= pi.AttributeList.Count)
                {
                    SelectButton(FindButtonByOption("A_SelectProperties"));
                }
                else
                {
                    _nextAttributeButton.Content = _currentTextAttribute == pi.AttributeList.Count - 1 ? "Done" : "Next";

                    GroupAttribute ga = pi.AttributeList[_currentTextAttribute];
                    _promptLabel.Text = ga.Prompt;
                    _attributeBox.AcceptsReturn = ga.MaxLines > 1;
                    _attributeBox.Text = ga.Value.TrimEnd();
                    _attributeMaxLines = pi.AttributeList[_currentTextAttribute].MaxLines;
                    _attributeBox.Select(_attributeBox.Text.Length, 0);

                    _ofLabel.Text = string.Format("{0} of {1}", _currentTextAttribute + 1, pi.AttributeList.Count);

                    if (_currentTextAttribute == 0)
                    {
                        _attributeUndoHandled = false;
                        _textAttributeOptions.Visibility = Visibility.Visible;
                        //this.Height = 149 + _attributeStackPanel.RenderSize.Height;
                    }

                    _attributeValueIsDirty = false;
                }
            }
        }

        private void _attributeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Cirros.Utility.Utilities.__checkSizeChanged(44, sender)) return;

            if (_attributeBox.Text.EndsWith("\r\r") || _attributeBox.Text.EndsWith("\r\n\r\n"))
            {
                ShowNextTextAttribute();
            }
            else if (_attributeBox.Text.Contains("\r\n\r\n"))
            {
                _attributeBox.Text = _attributeBox.Text.Replace("\r\n\r\n", "\r\n");
                ShowNextTextAttribute();
            }
            else if (_attributeBox.Text.Contains("\r\r"))
            {
                _attributeBox.Text = _attributeBox.Text.Replace("\r\r", "\r");
                ShowNextTextAttribute();
            }
            else if (_attributeBox.Text.EndsWith("\r") || _attributeBox.Text.EndsWith("\r\n"))
            {
                if (_attributeMaxLines < 2)
                {
                    ShowNextTextAttribute();
                }
                else
                {
#if WINDOWS_UWP
                    string newline = "\r";
#else
                    string newline = "\n";
#endif
                    int lines = 1;
                    int nli = _attributeBox.Text.IndexOf(newline);
                    while (nli >= 0)
                    {
                        if (lines++ >= _attributeMaxLines)
                        {
                            ShowNextTextAttribute();
                            break;
                        }
                        nli = _attributeBox.Text.IndexOf(newline, nli + 1);
                    }
                }
            }
        }

        private void _attributeBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ShowNextTextAttribute();
                e.Handled = true;
            }
            else
            {
                _attributeValueIsDirty = true;
            }
        }

#endregion

        double _minWidth = 0;

        private void _menuPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Cirros.Utility.Utilities.__checkSizeChanged(45, sender)) return;

            if (sender is StackPanel)
            {
                StackPanel sp = sender as StackPanel;

                if (sp.ActualWidth > _minWidth)
                {
                    _minWidth = sp.ActualWidth;
                }
                else if (sp.MinWidth != _minWidth)
                {
                    sp.MinWidth = _minWidth;
                }
            }
        }
    }
}
