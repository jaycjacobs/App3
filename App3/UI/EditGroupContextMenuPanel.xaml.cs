using Cirros;
using Cirros.Actions;
using Cirros.Commands;
using Cirros.Primitives;
using Cirros.Utility;
using System;
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
    public sealed partial class EditGroupContextMenuPanel : UserControl, IContextMenu
    {
        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;
        Popup _popup;
        Point _workCanvasOffset;

        Rect _box = Rect.Empty;

        static string _selectedOption = null;

        Primitive _selection = null;

        public EditGroupContextMenuPanel()
        {
            this.InitializeComponent();

            this.Loaded += EditGroupContextMenuPanel_Loaded;
            this.Unloaded += EditGroupContextMenuPanel_Unloaded;
            this.PointerPressed += EditGroupContextMenuPanel_PointerPressed;
            this.PointerMoved += EditGroupContextMenuPanel_PointerMoved;
            this.PointerReleased += EditGroupContextMenuPanel_PointerReleased;

            Globals.Events.OnPrimitiveSelectionPropertyChanged += Events_OnPrimitiveSelectionPropertyChanged;
            Globals.Events.OnPrimitiveSelectionSizeChanged += Events_OnPrimitiveSelectionSizeChanged;

            _jumpIconPanel.Visibility = Globals.UIVersion > 0 ? Visibility.Visible : Visibility.Collapsed;

            DataContext = Globals.UIDataContext;
        }

        private void EditGroupContextMenuPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Globals.Events.OnPrimitiveSelectionPropertyChanged -= Events_OnPrimitiveSelectionPropertyChanged;
            Globals.Events.OnPrimitiveSelectionSizeChanged -= Events_OnPrimitiveSelectionSizeChanged;
        }

        private void Events_OnPrimitiveSelectionSizeChanged(object sender, PrimitiveSelectionSizeChangedEventArgs e)
        {
            if (_selection != e.Primitive)
            {
                Select(e.Primitive as Primitive, -1);
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
                Select(e.Primitive as Primitive, -1);
            }
            if (_propertiesContentControl.Content is AttributePropertyPanel)
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
            if (_selection is PInstance instance)
            {
                if (_nameBox.Text != instance.GroupName)
                {
                    GroupNameChanged();
                }

                Group g = Globals.ActiveDrawing.GetGroup(instance.GroupName);
                if (g != null && _descriptionBox.Text != g.Description)
                {
                    g.Description = _descriptionBox.Text;
                }

                _descriptionBox.Text = "";
            }

            SelectButton(null);

            _selection = null;
        }

        public Point WorkCanvasOffset
        {
            set
            {
                _workCanvasOffset = value;
            }
        }

        void EditGroupContextMenuPanel_Loaded(object sender, RoutedEventArgs e)
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
            //SelectButton(_pointsButton);
        }

        void EditGroupContextMenuPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
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

        void EditGroupContextMenuPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _popup.HorizontalOffset = p.X - _dragXOff;
                _popup.VerticalOffset = p.Y - _dragYOff;
            }
        }

        void EditGroupContextMenuPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                ReleasePointerCapture(e.Pointer);
                _isDragging = false;
            }
        }

        ToggleButton _selectedButton = null;

        void b_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is ToggleButton)
            {
                ToggleButton b = sender as ToggleButton;
                SelectButton(b);
            }
        }

        private async void InvokeOption(string option)
        {
            bool handled = false;

            _propertiesPanel.Visibility = Visibility.Collapsed;
            _groupOptions.Visibility = Visibility.Collapsed;
            _globalChangePanel.Visibility = Visibility.Collapsed;
            _createGroupPanel.Visibility = Visibility.Collapsed;

            if (_selection is PInstance)
            {
                if (_selectedOption == "A_GroupProperties" || _selectedOption == "A_MemberProperties")
                {
                    if (_propertiesContentControl.Content is AttributePropertyPanel)
                    {
                        if (option == "A_GroupProperties")
                        {
                        }
                        else if (option == "A_MemberProperties")
                        {
                        }
                        else
                        {
                            ((AttributePropertyPanel)_propertiesContentControl.Content).Primitive = null;
                        }
                    }
                }

                switch (option)
                {
                    case "A_GlobalChange":
                        {
                            if (_selection is PInstance)
                            {
                                Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                                if (g != null)
                                {
                                    int count = Globals.ActiveDrawing.CountGroupInstances(g.Name, true);
                                    if (count > 1)
                                    {
                                        _copiesLabel.Text = string.Format("There are {0} copies of this group.", count);
                                        _globalChangePanel.Visibility = Visibility.Visible;
                                    }
                                }
                            }
                        }
                        break;

                    case "A_CreateGroup":
                        //    {
                        //        EnableAllOptions(false);
                        //        _createGroupPanel.Visibility = Visibility.Visible;
                        //        _selectedOption = option;
                        //    }
                        _selectedOption = "A_MemberProperties";
                        break;

                    case "A_GroupProperties":
                        {
                            _groupOptions.Visibility = Visibility.Visible;
                            _selectedOption = option;
                        }
                        break;

                    case "A_MemberProperties":
                        {
                            if (_selection is PInstance)
                            {
                                _selectedOption = option;
                                Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                                if (g != null && _memberIndex >= 0 && _memberIndex < g.Items.Count)
                                {
                                    Primitive member = g.Items[_memberIndex];
                                    if (member != null)
                                    {
                                        string typeName = member is PBSpline ? "Curve" : member.TypeName.ToString();

                                        _memberPropertiesHeading.Text = string.Format("{0} properties", typeName);
                                        _orderText.Text = string.Format("{0} of {1}", _memberIndex + 1, g.Items.Count);
                                        UserControl propertyPanel = new AttributePropertyPanel(member);
                                        _propertiesContentControl.Content = propertyPanel;
                                        _propertiesPanel.Visibility = Visibility.Visible;
                                    }
                                    else if (Globals.CommandProcessor is Cirros.Commands.EditGroupCommandProcessor)
                                    {
                                        ((Cirros.Commands.EditGroupCommandProcessor)Globals.CommandProcessor).SelectInstance(_selection as PInstance, 0);
                                    }
                                }
                            }
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

                    case "A_DeleteMember":
                        if (_selection is PInstance)
                        {
                            Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                            if (g.Items.Count > 1)
                            {
                                _selectedOption = option;
                            }
                            else
                            {
                                SelectButton(_memberPropertiesButton);
                            }
                        }
                        break;

                    case "A_Ungroup":
                        _selectedOption = "";
                        Select(null, -1);
                        SelectButton(null);
                        break;

                    default:
                        _selectedOption = option;
                        break;
                }

                //EnableAllOptions(true);
                EnableButtonsForObject();
            }
            else if (_selection is Primitive)
            {
                if (option == "A_CreateGroup")
                {
                    EnableAllOptions(false);
                    _createGroupPanel.Visibility = Visibility.Visible;
                    _selectedOption = option;
                }
            }

            if (handled == false && Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.Invoke(option, null);
            }
        }

        private ToggleButton FindButtonByOption(string command)
        {
            foreach (FrameworkElement e in _row0.Children)
            {
                if (e is ToggleButton && (string)e.Tag == command)
                {
                    return e as ToggleButton;
                }
            }

            foreach (FrameworkElement e in _row1.Children)
            {
                if (e is ToggleButton && (string)e.Tag == command)
                {
                    return e as ToggleButton;
                }
            }

            foreach (FrameworkElement e in _row2.Children)
            {
                if (e is ToggleButton && (string)e.Tag == command)
                {
                    return e as ToggleButton;
                }
            }

            return null;
        }

        private void SelectButton(ToggleButton button, bool invoke = true)
        {
            if (_selectedButton != null)
            {
                _selectedButton.IsChecked = false;
                _selectedButton = null;
            }

            if (button != null)
            {
                _selectedButton = button;
                _selectedButton.IsChecked = true;

                if (invoke && button.Tag is string)
                {
                    InvokeOption(button.Tag as string);
                }
            }
        }

        public void Select(Primitive p, int memberIndex)
        {
            bool newSelection = p != _selection;

            _selection = p;
            _memberIndex = memberIndex;

            if (_selection is PInstance)
            {
                EnableAllOptions(true);

                if (_selectedOption == "A_CreateGroup")
                {
                    _selectedOption = null;
                }
                Point ul = Globals.View.PaperToDisplay(new Point(_selection.Box.Left, _selection.Box.Top));
                Point lr = Globals.View.PaperToDisplay(new Point(_selection.Box.Right, _selection.Box.Bottom));
                _box = new Rect(ul, lr);

                Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                if (g != null)
                {
                    int colon = g.Name.IndexOf(":");
                    if (colon == 0)
                    {
                        _nameBox.Text = "";
                    }
                    //else if (colon > 0)
                    //{
                    //    _nameBox.Text = g.Name.Substring(0, colon);
                    //}
                    else
                    {
                        _nameBox.Text = g.Name;
                    }

                    _descriptionBox.Text = g.Description;

                    _spaceCombo.SelectedItem = g.CoordinateSpace == CoordinateSpace.Paper ? _spacePaperItem : _spaceModelItem;
                    _insertCB.IsChecked = g.InsertLocation != GroupInsertLocation.None;

                    //switch (g.InsertLocation)
                    //{
                    //    case GroupInsertLocation.None:
                    //        _insertCombo.SelectedItem = _insertNoneItem;
                    //        break;
                    //    case GroupInsertLocation.Origin:
                    //        _insertCombo.SelectedItem = _insertOriginItem;
                    //        break;
                    //    case GroupInsertLocation.Start:
                    //        _insertCombo.SelectedItem = _insertStartItem;
                    //        break;
                    //    case GroupInsertLocation.Exit:
                    //        _insertCombo.SelectedItem = _insertEndItem;
                    //        break;
                    //    case GroupInsertLocation.Center:
                    //        _insertCombo.SelectedItem = _insertMidItem;
                    //        break;
                    //}
                }

                _groupFlags.Text = String.Format("0x{0:X8}", g.Flags);

                //_libraryCB.IsChecked = g.IncludeInLibrary;

                int count = Globals.ActiveDrawing.CountGroupInstances(g.Name, true);
                InvokeOption(newSelection && count > 1 ? "A_GlobalChange" : _selectedOption);

                EnableButtonsForObject();

                _row0.Visibility = Visibility.Visible;
                _row1.Visibility = Visibility.Visible;
                _row2.Visibility = Visibility.Visible;
                _divider.Visibility = Visibility.Visible;
                _createGroupPanel.Visibility = Visibility.Collapsed;

                _noSelectionPanel.Visibility = Visibility.Collapsed;

                _title.Text = "Edit group definition";
            }
            else if (_selection is Primitive)
            {
                InvokeOption("A_CreateGroup");

                _row0.Visibility = Visibility.Collapsed;
                _row1.Visibility = Visibility.Collapsed;
                _row2.Visibility = Visibility.Collapsed;
                _divider.Visibility = Visibility.Collapsed;
                _globalChangePanel.Visibility = Visibility.Collapsed;

                _noSelectionPanel.Visibility = Visibility.Collapsed;

                _title.Text = "Create group";
            }
            else
            {
                _noSelectionPanel.Visibility = Visibility.Visible;

                _row0.Visibility = Visibility.Collapsed;
                _row1.Visibility = Visibility.Collapsed;
                _row2.Visibility = Visibility.Collapsed;
                _divider.Visibility = Visibility.Collapsed;
                _createGroupPanel.Visibility = Visibility.Collapsed;
                _globalChangePanel.Visibility = Visibility.Collapsed;
                _propertiesPanel.Visibility = Visibility.Collapsed;
                _groupOptions.Visibility = Visibility.Collapsed;

                _title.Text = "No selection";
            }
        }

        private void EnableAllOptions(bool flag)
        {
            foreach (FrameworkElement fe in _row0.Children)
            {
                if (fe is Control)
                {
                    ((Control)fe).IsEnabled = flag;
                    if (flag == false && fe is ToggleButton)
                    {
                        ((ToggleButton)fe).IsChecked = false;
                    }
                }
            }
            foreach (FrameworkElement fe in _row1.Children)
            {
                if (fe is Control)
                {
                    ((Control)fe).IsEnabled = flag;
                    if (flag == false && fe is ToggleButton)
                    {
                        ((ToggleButton)fe).IsChecked = false;
                    }
                }
            }
            foreach (FrameworkElement fe in _row2.Children)
            {
                if (fe is Control)
                {
                    ((Control)fe).IsEnabled = flag;
                    if (flag == false && fe is ToggleButton)
                    {
                        ((ToggleButton)fe).IsChecked = false;
                    }
                }
            }
        }

        private void EnableButtonsForObject()
        {
            if (_selection is PInstance)
            {
                bool untransformed = _selection.IsTransformed == false;

                _pointsButton.IsEnabled = untransformed;
                _addMemberButton.IsEnabled = untransformed;

                Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                if (g != null)
                {
                    if (_memberIndex < 0)
                    {
                        _deleteMemberButton.IsEnabled = false;
                        _moveMemberButton.IsEnabled = false;
                        _memberPropertiesButton.IsEnabled = false;

                        //if (_selectedButton.IsEnabled == false)
                        //{
                        //    SelectButton(_groupPropertiesButton);
                        //}
                    }
                    else
                    {
                        _moveMemberButton.IsEnabled = untransformed;

                        _memberPropertiesButton.IsEnabled = true;

                        if (g.Items.Count > 1)
                        {
                            _deleteMemberButton.IsEnabled = true;
                        }
                        else
                        {
                            _deleteMemberButton.IsEnabled = false;

                            if (_selectedOption == "A_DeleteMember")
                            {
                                SelectButton(_memberPropertiesButton);
                            }
                        }
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.Events.ShowContextMenu(null, null);
        }

        double _minWidth = 0;
        int _memberIndex = -1;

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

        private void _spaceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selection is PInstance)
            {
                Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                if (g != null)
                {
                    g.CoordinateSpace = _spaceCombo.SelectedItem == _spacePaperItem ? CoordinateSpace.Paper : CoordinateSpace.Model;
                    Globals.ActiveDrawing.ChangeNumber++;
                }
            }
        }

        //private void _insertCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (_selection is PInstance)
        //    {
        //        Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
        //        if (g != null)
        //        {
        //            GroupInsertLocation gil = GroupInsertLocation.None;

        //            if (_insertCombo.SelectedItem == _insertEndItem)
        //            {
        //                gil = GroupInsertLocation.Exit;
        //            }
        //            else if (_insertCombo.SelectedItem == _insertMidItem)
        //            {
        //                gil = GroupInsertLocation.Center;
        //            }
        //            else if (_insertCombo.SelectedItem == _insertOriginItem)
        //            {
        //                gil = GroupInsertLocation.Origin;
        //            }
        //            else if (_insertCombo.SelectedItem == _insertStartItem)
        //            {
        //                gil = GroupInsertLocation.Start;
        //            }

        //            if (gil != g.InsertLocation)
        //            {
        //                g.InsertLocation = gil;
        //                Globals.ActiveDrawing.ChangeNumber++;
        //            }
        //        }
        //    }
        //}

        //private void _libraryCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (_selection is PInstance)
        //    {
        //        Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
        //        if (g != null)
        //        {
        //            g.IncludeInLibrary = _libraryCB.IsChecked == true;
        //            Globals.ActiveDrawing.ChangeNumber++;
        //        }
        //    }
        //}

        private void _nameBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _nameBox.PlaceholderText = "";
        }

        private void _nameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_selection is PInstance)
            {
                _nameBox.PlaceholderText = "Anonymous";

                GroupNameChanged();
            }
        }

        private void GroupNameChanged()
        {
            Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
            if (g != null && _nameBox.Text != g.Name)
            {
                if (_nameBox.Text.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RenameGroup, g, g.Name);

                    string newName = Globals.ActiveDrawing.UniqueGroupName(_nameBox.Text);
                    Globals.ActiveDrawing.RenameGroup(g, newName);
                }
                else
                {
                    _nameBox.Text = "";
                }
            }
        }

        //private void _nameBox_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    if (e.NewSize.Height != e.PreviousSize.Height)
        //    {
        //        Thickness t = _nameBox.Padding;
        //        double pv = (t.Top + t.Bottom) / 2;
        //        t.Left = t.Right * .6;
        //        t.Top = t.Bottom;// = pv;
        //        _nameBox.Padding = t;
        //    }
        //}

        private void _globalAllButton_Click(object sender, RoutedEventArgs e)
        {
            InvokeOption(_selectedOption);
        }

        private void _globalOneButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selection is PInstance)
            {
                PInstance unique = Utilities.MakeUniqueInstance(_selection as PInstance, true);
                //Select(unique, _memberIndex);

                if (Globals.CommandProcessor is Cirros.Commands.EditGroupCommandProcessor)
                {
                    ((Cirros.Commands.EditGroupCommandProcessor)Globals.CommandProcessor).SelectInstance(unique, _memberIndex);
                }
            }
            InvokeOption(_selectedOption);
        }

        private void _moveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selection is PInstance && _memberIndex >= 0)
            {
                Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                if (g != null)
                {
                    _memberIndex = g.MoveMemberDown(_memberIndex);
                    _orderText.Text = string.Format("{0} of {1}", _memberIndex + 1, g.Items.Count);
                    Globals.ActiveDrawing.UpdateGroupInstances(g.Name);
                }
            }
        }

        private void _moveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selection is PInstance && _memberIndex >= 0)
            {
                Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                if (g != null)
                {
                    _memberIndex = g.MoveMemberUp(_memberIndex);
                    _orderText.Text = string.Format("{0} of {1}", _memberIndex + 1, g.Items.Count);
                    Globals.ActiveDrawing.UpdateGroupInstances(g.Name);
                }
            }
        }

        private void _createGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.CommandProcessor is Cirros.Commands.EditGroupCommandProcessor)
            {
                ((Cirros.Commands.EditGroupCommandProcessor)Globals.CommandProcessor).CreateGroupFromPrimitive();
                EnableAllOptions(true);
                EnableButtonsForObject();
                SelectButton(_groupPropertiesButton);
            }
        }

        private void _insertCB_Checked(object sender, RoutedEventArgs e)
        {
            if (_selection is PInstance)
            {
                Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
                if (g != null)
                {
                    g.InsertLocation = _insertCB.IsChecked == true ? GroupInsertLocation.Origin : GroupInsertLocation.None;
                    Globals.ActiveDrawing.ChangeNumber++;
                }
            }
        }

        private void EditObjectClick(object sender, RoutedEventArgs e)
        {
            if (_selection is Primitive)
            {
                Globals.CommandProcessorParameter = _selection;
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.edit;
        }

        private void SelectObjectsClick(object sender, RoutedEventArgs e)
        {
            if (_selection is Primitive)
            {
                Globals.CommandProcessorParameter = _selection;
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.select;
        }

        private void CopyPasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selection is Primitive)
            {
                Globals.CommandProcessorParameter = _selection;
            }
            Globals.CommandDispatcher.ActiveCommand = CommandType.copypaste;
        }

        private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Button b)
            {
                b.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        //private void _descriptionBox_GotFocus(object sender, RoutedEventArgs e)
        //{

        //}

        //private void _descriptionBox_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    //if (_selection is PInstance)
        //    //{
        //    //    Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_selection).GroupName);
        //    //    if (g != null && _descriptionBox.Text != g.Description)
        //    //    {
        //    //        g.Description = _descriptionBox.Text;
        //    //    }
        //    //}
        //}

        //private void _descriptionBox_SizeChanged(object sender, SizeChangedEventArgs e)
        //{

        //}
    }
}
