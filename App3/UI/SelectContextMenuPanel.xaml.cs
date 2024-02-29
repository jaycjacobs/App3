using Cirros;
using Cirros.Commands;
using Cirros.Drawing;
using Cirros.Primitives;
using CirrosUI;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public sealed partial class SelectContextMenuPanel : UserControl, IContextMenu
    {
        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;
        Popup _popup;
        Point _workCanvasOffset;

        List<Primitive> _selectedObjects = null;

        Rect _box = Rect.Empty;

        static string _selectedOption = null;
        List<string>[] _selectedOptionList = null;

        static List<string>[] _selectOptions =
        {
            new List<string> { "0", "Move",                 "A_SelectMove", "state" },
            new List<string> { "0", "Copy",                 "A_SelectCopy", "state" },
            new List<string> { "0", "Delete",               "A_SelectDelete", "state" },
            new List<string> { "1", "Resize",               "A_SelectScale", "state" },
            new List<string> { "1", "Free rotate",          "A_SelectRotate", "state" },
            new List<string> { "1", "Flip",                 "A_SelectFlip", "state" },
            new List<string> { "1", "Transform",            "A_SelectTransform", "state" },
            new List<string> { "2", "Create group",         "A_SelectGroup", "state" },
            new List<string> { "2", "Change layer",         "A_SelectLayer", "state" },
            new List<string> { "2", "Display order",        "A_DisplayOrder", "state" },
        };

        public SelectContextMenuPanel()
        {
            this.InitializeComponent();

            this.Loaded += SelectContextMenuPanel_Loaded;
            this.Unloaded += SelectContextMenuPanel_Unloaded;
            this.PointerPressed += SelectContextMenuPanel_PointerPressed;
            this.PointerMoved += SelectContextMenuPanel_PointerMoved;
            this.PointerReleased += SelectContextMenuPanel_PointerReleased;

            _jumpIconPanel.Visibility = Globals.UIVersion > 0 ? Visibility.Visible : Visibility.Collapsed;

            _layerComboBox.SelectionChanged += _layerComboBox_SelectionChanged;

            DataContext = Globals.UIDataContext;

            if (Globals.SelectMoveOffsetX == 0 && Globals.SelectMoveOffsetY == 0)
            {
                Point off = Globals.ActiveDrawing.ModelToPaperDelta(new Point(Globals.GridSpacing, 0));
                Globals.SelectMoveOffsetX = off.X;
                Globals.SelectMoveOffsetY = off.Y;
            }
        }

        private void _layerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LayerTile tile = _layerComboBox.SelectedItem as LayerTile;
            if (tile != null)
            {
                Globals.CommandProcessor.Invoke("A_SelectLayer", ((LayerTile)tile).Layer);
            }
        }

        void Events_OnMoveCopyOffsetChanged(object sender, MoveCopyOffsetChangedEventArgs e)
        {
            Globals.SelectMoveOffsetX = e.Offset.X;
            Globals.SelectMoveOffsetY = e.Offset.Y;

            Point moff = Globals.ActiveDrawing.PaperToModelDelta(e.Offset);
            _dxBox.Value = moff.X;
            _dyBox.Value = moff.Y;
        }

        public Point WorkCanvasOffset
        {
            set
            {
                _workCanvasOffset = value;
            }
        }

        void SelectOptionList(List<string>[] optionList)
        {
            if (_selectedOptionList == null || _selectedOptionList != optionList)
            {
                _row0.Children.Clear();
                _row1.Children.Clear();
                _row2.Children.Clear();
                _row3.Children.Clear();

                _selectedOptionList = optionList;
                AddContextButtons(_selectedOptionList);
            }
        }

        void SelectContextMenuPanel_Loaded(object sender, RoutedEventArgs e)
        {
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

                if (Math.Max(leftSpace, rightSpace) < (ActualWidth + 60))
                {
                    _popup.HorizontalOffset = frameWidth / 3;
                }
                else
                {
                    _popup.HorizontalOffset = (leftSpace > rightSpace ? _box.Left - ActualWidth - 40 : _box.Right + 40) + _workCanvasOffset.X;
                }

                if (Math.Max(topSpace, bottomSpace) < (ActualHeight + 60))
                {
                    _popup.VerticalOffset = frameHeight / 3;
                }
                else
                {
                    _popup.VerticalOffset = (topSpace > bottomSpace ? _box.Top - ActualHeight - 40 : _box.Bottom + 40) + _workCanvasOffset.Y;
                }
            }

            Point moff = Globals.ActiveDrawing.PaperToModelDelta(new Point(Globals.SelectMoveOffsetX, Globals.SelectMoveOffsetY));
            _dxBox.Value = moff.X;
            _dyBox.Value = moff.Y;

            Globals.Events.OnMoveCopyOffsetChanged += Events_OnMoveCopyOffsetChanged;
#if isotropicResize
            SetIsotropic(Globals.SelectResizeIsotropic);
#endif
        }

        void SelectContextMenuPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Globals.Events.OnMoveCopyOffsetChanged -= Events_OnMoveCopyOffsetChanged;
        }

        void SelectContextMenuPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
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

        void SelectContextMenuPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _popup.HorizontalOffset = p.X - _dragXOff;
                _popup.VerticalOffset = p.Y - _dragYOff;
            }
        }

        void SelectContextMenuPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _isDragging = false;
        }

        private void AddContextButtons(List<string>[] options)
        {
            if (options == null)
            {
                _noSelectionPanel.Visibility = Visibility.Visible;
            }
            else
            {
                _noSelectionPanel.Visibility = Visibility.Collapsed;

                foreach (List<string> p in options)
                {
                    StateButton b = new StateButton();
                    b.Style = (Style)(Application.Current.Resources["ContextMenuStateButtonStyle"]);
                    b.OnClick += Tb_OnClick;
                    b.Text = p[1];
                    b.Tag = p[2];
                    b.FontSize = Globals.UIDataContext.UIFontSizeNormal;

                    switch (p[0])
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

                        case "3":
                            _row3.Children.Add(b);
                            break;
                    }
                }

                FrameworkElement fe = null;

                if (_selectedOption != null)
                {
                    fe = FindButtonByOption(_selectedOption);
                    if (fe == null)
                    {
                        fe = FindButtonByOption(options[0][2]);
                    }
                }
                else
                {
                    fe = FindButtonByOption(options[0][2]);
                }

                if (fe != null)
                {
                    SelectButton(fe);
                }
            }
        }

        FrameworkElement _selectedButton = null;

        private void Tb_OnClick(object sender, EventArgs e)
        {
            if (sender is FrameworkElement b)
            {
                SelectButton(b);
            }
        }

        private void InvokeOption(string option)
        {
            //if (_selectedOption == "A_SelectProperties" && option != "A_SelectProperties")
            //{
            //    Globals.Events.ShowProperties(null);
            //}

            _moveOptions.Visibility = Visibility.Collapsed;
            _flipOptions.Visibility = Visibility.Collapsed;
            _transformOptions.Visibility = Visibility.Collapsed;
            _layerOptions.Visibility = Visibility.Collapsed;
            _propertiesPanel.Visibility = Visibility.Collapsed;
            _displayOrderOptions.Visibility = Visibility.Collapsed;
#if isotropicResize
            _resizeOptions.Visibility = Visibility.Collapsed;
#endif

            //this.Height = 120;

            switch (option)
            {
                case "A_SelectMove":
                case "A_SelectCopy":
                    _selectedOption = option;
                    _moveOptions.Visibility = Visibility.Visible;
                    object parameter = null;

                    if (Globals.SelectMoveByOffset)
                    {
                        Point delta = new Point(_dxBox.Value, _dyBox.Value);
                        parameter = Globals.ActiveDrawing.ModelToPaperDelta(delta);

                        _moveDragButton.IsChecked = false;
                        _moveOffsetButton.IsChecked = true;

                        _moveOffsetPanel.Visibility = Visibility.Visible;

                        //this.Height = 262;
                    }
                    else
                    {
                        _moveDragButton.IsChecked = true;
                        _moveOffsetButton.IsChecked = false;

                        _moveOffsetPanel.Visibility = Visibility.Collapsed;

                        //this.Height = 156;
                    }

                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Invoke(option, parameter);
                    }
                    break;

                case "A_SelectScale":
#if isotropicResize
                    _selectedOption = option;
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Invoke(option, null);
                    }
                    SetIsotropic(Globals.SelectResizeIsotropic);
                    this.Height = 156;
                    _resizeOptions.Visibility = Visibility.Visible;
                    break;
#endif
                case "A_SelectRotate":
                    _selectedOption = option;
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Invoke(option, null);
                    }
                    break;

                case "A_EditLast":
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Invoke(option, null);
                    }
                    break;

                case "A_DisplayOrder":
                    _selectedOption = option;
                    _displayOrderOptions.Visibility = Visibility.Visible;
                    if (_selectedObjects != null && _selectedObjects.Count > 0)
                    {
                        int orderMin = Globals.ActiveDrawing.MaxZIndex;
                        int orderMax = Globals.ActiveDrawing.MinZIndex;

                        foreach (Primitive p in _selectedObjects)
                        {
                            orderMin = Math.Min(orderMin, p.ZIndex);
                            orderMax = Math.Max(orderMax, p.ZIndex);
                        }

                        if (orderMin == orderMax)
                        {
                            _zIndexBox.Text = orderMax.ToString();
                        }
                        else
                        {
                            _zIndexBox.Text = "";
                        }
                    }
                    break;

                case "A_SelectDelete":
                case "A_SelectGroup":
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Invoke(option, null);
                    }
                    SelectButton(FindButtonByOption(_selectedOption));
                    break;

                case "A_SelectTransform":
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Invoke("A_SelectPivot", null);
                    }
                    _selectedOption = option;
                    _transformOptions.Visibility = Visibility.Visible;
                    //this.Height = 232;
                    break;

                case "A_SelectLayer":
                    //if (Globals.CommandProcessor != null)
                    //{
                    //    //Globals.CommandProcessor.Invoke("A_SelectPivot", null);
                    //}
                    PopulateLayers();
                    _selectedOption = option;
                    _layerOptions.Visibility = Visibility.Visible;
                    break;

                case "A_SelectFlip":
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Invoke("A_SelectPivot", null);
                    }
                    _selectedOption = option;
                    //this.Height = 156;
                    _flipOptions.Visibility = Visibility.Visible;
                    break;

                case "A_SelectProperties":
                    if (_selectedObjects != null && _selectedObjects.Count == 1)
                    {
                        UserControl propertyPanel = new AttributePropertyPanel(_selectedObjects[0]);
                        _propertiesContentControl.Content = propertyPanel;
                        _propertiesPanel.Visibility = Visibility.Visible;
                    }
                    //if (Globals.CommandProcessor != null)
                    //{
                    //    Globals.CommandProcessor.Invoke(option, null);
                    //}
                    _selectedOption = option;
                    break;

                case "A_SelectCopyToClipboard":
                case "A_SelectPaste":
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Invoke(option, null);
                    }
                    break;
            }
        }

        private void PopulateLayers()
        {
            if (_layerComboBox != null && _layerComboBox.Items.Count != Globals.LayerTable.Count)
            {
                _layerComboBox.Items.Clear();

                if (Globals.LayerTable != null && Globals.LayerTable.Count > 0)
                {
                    List<Layer> list = new List<Layer>();
                    foreach (Layer layer in Globals.LayerTable.Values)
                    {
                        if (layer.Id != 0)
                        {
                            list.Add(layer);
                        }
                    }
                    list.Sort();
                    list.Insert(0, Globals.LayerTable[0]);

                    foreach (Layer layer in list)
                    {
                        LayerTile lai = new LayerTile(layer);
                        _layerComboBox.Items.Add(lai);
                    }
                }
            }
        }

        private FrameworkElement FindButtonByOption(string command)
        {
            foreach (FrameworkElement e in _row0.Children)
            {
                if (e is FrameworkElement b && (string)e.Tag == command)
                {
                    return b;
                }
            }

            foreach (FrameworkElement e in _row1.Children)
            {
                if (e is FrameworkElement b && (string)e.Tag == command)
                {
                    return b;
                }
            }

            foreach (FrameworkElement e in _row2.Children)
            {
                if (e is FrameworkElement b && (string)e.Tag == command)
                {
                    return b;
                }
            }

            foreach (FrameworkElement e in _row3.Children)
            {
                if (e is FrameworkElement b && (string)e.Tag == command)
                {
                    return b;
                }
            }

            return null;
        }

        private void SelectButton(FrameworkElement button)
        {
            try
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
                    if (button is StateButton b)
                    {
                        b.IsSelected = true;
                    }

                    if (button.Tag is string)
                    {
                        InvokeOption(button.Tag as string);
                    }
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "command", "edit" },
                        { "method", "SelectButton" },
                    }, 500);
            }
        }

        public void Select(List<Primitive> list)
        {
            _selectedObjects = list;

            _moveOptions.Visibility = Visibility.Collapsed;
            _flipOptions.Visibility = Visibility.Collapsed;
            _transformOptions.Visibility = Visibility.Collapsed;
            _layerOptions.Visibility = Visibility.Collapsed;
            _propertiesPanel.Visibility = Visibility.Collapsed;
            _displayOrderOptions.Visibility = Visibility.Collapsed;

            _layerComboBox.SelectedItem = null;

            if (list == null || list.Count == 0)
            {
                _title.Text = "No selection";
                SelectOptionList(null);
            }
            else
            {
                if (list.Count == 1)
                {
                    _title.Text = "Selection: 1 object";
                }
                else
                {
                    _title.Text = string.Format("Selection: {0} objects", list.Count);
                }

                SelectOptionList(_selectOptions);

                Rect itemBoxUnion = RectHelper.Empty;

                int orderMin = Globals.ActiveDrawing.MaxZIndex;
                int orderMax = Globals.ActiveDrawing.MinZIndex;

                foreach (Primitive p in list)
                {
                    if (itemBoxUnion.IsEmpty)
                    {
                        itemBoxUnion = p.Box;
                    }
                    else
                    {
                        itemBoxUnion.Union(p.Box);
                    }

                    orderMin = Math.Min(orderMin, p.ZIndex);
                    orderMax = Math.Max(orderMax, p.ZIndex);
                }

                if (orderMin == orderMax)
                {
                    _zIndexBox.Text = orderMax.ToString();
                }
                else
                {
                    _zIndexBox.Text = "";
                }

                if (itemBoxUnion.IsEmpty == false)
                {
                    Point ul = Globals.View.PaperToDisplay(new Point(itemBoxUnion.Left, itemBoxUnion.Top));
                    Point lr = Globals.View.PaperToDisplay(new Point(itemBoxUnion.Right, itemBoxUnion.Bottom));
                    _box = new Rect(ul, lr);
                }

                //if (_selectedOption == "A_SelectProperties")
                //{
                //    if (Globals.CommandProcessor != null)
                //    {
                //        Globals.CommandProcessor.Invoke(_selectedOption, null);
                //    }
                //}
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.Events.ShowContextMenu(null, null);
        }

        private void MoveDrag_Click(object sender, RoutedEventArgs e)
        {
            Globals.SelectMoveByOffset = false;

            InvokeOption(_selectedOption);
        }

        private void MoveOffset_Click(object sender, RoutedEventArgs e)
        {
            Globals.SelectMoveByOffset = true;

            InvokeOption(_selectedOption);
        }

        private void MoveOffsetApply_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOption == "A_SelectCopy")
            {
                if (Globals.SelectMoveByOffset)
                {
                    Point delta = new Point(_dxBox.Value, _dyBox.Value);
                    Globals.CommandProcessor.Invoke("A_SelectCopySelection", Globals.ActiveDrawing.ModelToPaperDelta(delta));
                }
            }
            else if (_selectedOption == "A_SelectMove")
            {
                if (Globals.SelectMoveByOffset)
                {
                    Point delta = new Point(_dxBox.Value, _dyBox.Value);
                    Globals.CommandProcessor.Invoke("A_SelectMoveSelection", Globals.ActiveDrawing.ModelToPaperDelta(delta));
                }
            }
        }
        
        private void FlipHorizontal_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.Invoke("A_SelectFlipH", null);
            }
        }

        private void FlipVertical_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.Invoke("A_SelectFlipV", null);
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (_xScaleBox.Value == 0)
            {
                _xScaleBox.Value = 1;

                if (_yScaleBox.Value == 0)
                {
                    _yScaleBox.Value = 1;
                }
            }
            else if (_yScaleBox.Value == 0)
            {
                _yScaleBox.Value = 1;
            }
            else
            {
                double[] values = new double[3];
                values[0] = _xScaleBox.Value;
                values[1] = _yScaleBox.Value;
                values[2] = -_angleBox.Value;
                Globals.CommandProcessor.Invoke("A_SelectTransform", values);
            }
        }

        private void XScaleValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_aspectToggleButton != null && _xScaleBox != null && _yScaleBox != null)
            {
                if ((bool)_aspectToggleButton.IsChecked)
                {
                    _yScaleBox.Value = _xScaleBox.Value;
                }
            }
        }

        private void PreserveAspect_Checked(object sender, RoutedEventArgs e)
        {
            if (_xScaleBox != null && _yScaleBox != null)
            {
                _yScaleBox.IsEnabled = false;
                _yScaleBox.Value = _xScaleBox.Value;
                if (_tieBar != null)
                {
                    _tieBar.Stroke = new SolidColorBrush(Colors.White);
                }
            }
        }

        private void PreserveAspect_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_yScaleBox != null)
            {
                _yScaleBox.IsEnabled = true;
                _tieBar.Stroke = new SolidColorBrush(Colors.DarkGray);
            }
        }

        private void _xScaleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((bool)_aspectToggleButton.IsChecked)
            {
                _yScaleBox.Value = _xScaleBox.Value;
            }
        }

        private void _dxBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            Point delta = new Point(_dxBox.Value, _dyBox.Value);
            Globals.CommandProcessor.Invoke(_selectedOption, Globals.ActiveDrawing.ModelToPaperDelta(delta));
        }

        double minSPWidth = 0;

        private void StackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Cirros.Utility.Utilities.__checkSizeChanged(46, sender)) return;

            if (sender is StackPanel)
            {
                if (((StackPanel)sender).ActualWidth < minSPWidth)
                {
                    ((StackPanel)sender).Width = minSPWidth;
                }
                else if (minSPWidth != ((StackPanel)sender).ActualWidth)
                {
                    minSPWidth = ((StackPanel)sender).ActualWidth;
                }
            }
        }

        private void BringToFront_Click(object sender, RoutedEventArgs e)
        {
            int z = Globals.ActiveDrawing.MaxZIndex;
            foreach (Primitive p in _selectedObjects)
            {
                p.ZIndex = z;
            }

            _zIndexBox.Value = z;

            Globals.ActiveDrawing.ChangeNumber++;
        }

        private void SendToBack_Click(object sender, RoutedEventArgs e)
        {
            int z = Globals.ActiveDrawing.MinZIndex;
            foreach (Primitive p in _selectedObjects)
            {
                p.ZIndex = z;
            }

            _zIndexBox.Value = z;

            Globals.ActiveDrawing.ChangeNumber++;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Globals.CommandProcessor.Invoke("A_SelectResetTransform", null);
        }

        public void WillClose()
        {
            if (_selectedOption == "A_SelectCopy")
            {
                FrameworkElement fe = FindButtonByOption("A_SelectMove");
                if (fe != null)
                {
                    SelectButton(fe);
                }
            }

            //Select(null);
            //InvokeOption(null);
        }

        private void EditObjectClick(object sender, RoutedEventArgs e)
        {
            if (_selectedObjects != null && _selectedObjects.Count == 1)
            {
                Globals.CommandProcessorParameter = _selectedObjects[0];
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.edit;
        }

        private void EditGroupClick(object sender, RoutedEventArgs e)
        {
            if (_selectedObjects != null && _selectedObjects.Count == 1)
            {
                Globals.CommandProcessorParameter = _selectedObjects[0];
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.editgroup;
        }

        private void CopyPasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObjects != null && _selectedObjects.Count > 0)
            {
                Globals.CommandProcessorParameter = _selectedObjects.ToList<Primitive>();
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.copypaste;
        }

        private void _zIndexBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (_selectedObjects != null)
            {
                foreach (Primitive p in _selectedObjects)
                {
                    p.ZIndex = (int)args.NewValue;
                }
            }

            Globals.ActiveDrawing.ChangeNumber++;
        }

        private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Button b)
            {
                b.Foreground = new SolidColorBrush(Colors.White);
            }
        }

#if isotropicResize
        private void SetIsotropic(bool flag)
        {
            if (flag)
            {
                _isotropicButton.IsChecked = true;
                _anisotropicButton.IsChecked = false;
            }
            else
            {
                _isotropicButton.IsChecked = false;
                _anisotropicButton.IsChecked = true;
            }
        }

        private void _isotropicButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.SelectResizeIsotropic = true;
            SetIsotropic(Globals.SelectResizeIsotropic);
        }

        private void _anisotropicButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.SelectResizeIsotropic = false;
            SetIsotropic(Globals.SelectResizeIsotropic);
        }
#endif
    }
}
