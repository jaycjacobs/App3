using Cirros;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Utility;
using CirrosUI;
using RedDog.HUIApp;
using HUI;
using RedDog;
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Cirros.Actions;
using Cirros.Primitives;
using Windows.Foundation;
using Microsoft.Graphics.Display;
using RedDog.Console;
using Microsoft.UI.Xaml.Controls;

namespace RedDog.HUIApp
{
    public sealed partial class HSettingsArrowStyles : UserControl, HUIIDialog
    {
        private Dictionary<string, object> _options = new Dictionary<string, object>();
        ArrowStyle _currentStyle = null;
        Point _start;
        Point _end;

        public string Id
        {
            get { return RedDogGlobals.GS_SettingsArrowStylesCommand; }
        }

        public Dictionary<string, object> Options
        {
            get { return _options; }
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public HSettingsArrowStyles()
        {
            this.InitializeComponent();

            this.Loaded += HSettingsArrowStyles_Loaded;
        }

        private void _styleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_styleComboBox.SelectedItem is ComboBoxItem item && item.Tag is int id && Globals.ArrowStyleTable.ContainsKey(id))
            {
                SelectStyle(Globals.ArrowStyleTable[id]);
            }
        }

        private void HSettingsArrowStyles_Loaded(object sender, RoutedEventArgs e)
        {
            _arrowTypeComboBox.SelectionChanged += _arrowTypeComboBox_SelectionChanged;
            _styleComboBox.SelectionChanged += _styleComboBox_SelectionChanged;
            _styleNameBox.LostFocus += _styleNameBox_LostFocus;
            _styleNameBox.KeyDown += _styleNameBox_KeyDown;

            Populate();

            foreach (ComboBoxItem item in _styleComboBox.Items)
            {
                if (item.Tag is int styleId && styleId == RedDogGlobals.SelectedArrowStyleId)
                {
                    _styleComboBox.SelectedItem = item;
                    break;
                }
            }

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        private void _arrowTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                UpdateStyleSample();
                ArrowStyle.PropagateArrowStyleChanges(_currentStyle.Id);
            }
        }

        private void _styleNameBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                UpdateStyleName();
            }
        }

        private void _styleNameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateStyleName();
        }

        private void RefreshStyleList()
        {
            _styleComboBox.Items.Clear();

            foreach (ArrowStyle style in Globals.ArrowStyleTable.Values)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = style.Name;
                item.Tag = style.Id;
                _styleComboBox.Items.Add(item);
            }
        }

        private void SelectStyle(ArrowStyle selectedStyle)
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                Globals.Events.AttributesListChanged();
                UpdateStyleSample();
                ArrowStyle.PropagateArrowStyleChanges(_currentStyle.Id);
            }

            _styleNameBox.Visibility = Visibility.Collapsed;
            _styleComboBox.Visibility = Visibility.Visible;

            _currentStyle = null;

            if (selectedStyle != null)
            {
                RedDogGlobals.SelectedArrowStyleId = selectedStyle.Id;

                _styleNameBox.Text = selectedStyle.Name;

                int objects = ArrowStyle.GetInstanceCount(selectedStyle.Id);
                _objectCount.Text = objects.ToString();
                _deleteStyleButton.IsEnabled = selectedStyle.Id != 0 && objects == 0;

                foreach (ComboBoxItem item in _arrowTypeComboBox.Items)
                {
                    if (item.Tag is string type && type == ((ArrowType)selectedStyle.Type).ToString())
                    {
                        _arrowTypeComboBox.SelectedItem = item;
                        break;
                    }
                }

                double mfactor = Globals.ActiveDrawing.PaperUnit == Unit.Millimeters ? 25.4 : 1;

                _lengthBox.Value = selectedStyle.Size * mfactor;
                _aspectBox.Value = selectedStyle.Aspect;

                _currentStyle = selectedStyle;

                UpdateStyleSample();
            }
            else
            {
                _renameStyleButton.Visibility = Visibility.Collapsed;
                _styleNameBox.Text = "";
                _deleteStyleButton.IsEnabled = false;
            }
        }

        public void Populate()
        {
            RefreshStyleList();
        }

        private bool UpdateCurrentStyleAttributes()
        {
            bool needsUpdate = false;

            if (_currentStyle != null)
            {
                double mfactor = Globals.ActiveDrawing.PaperUnit == Unit.Millimeters ? 25.4 : 1;

                if ((_currentStyle.Size / mfactor) != _lengthBox.Value)
                {
                    _currentStyle.Size = _lengthBox.Value / mfactor;
                    needsUpdate = true;
                }
                if (_currentStyle.Aspect != _aspectBox.Value)
                {
                    _currentStyle.Aspect = _aspectBox.Value;
                    needsUpdate = true;
                }

                if (_arrowTypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string typeName)
                {
                    if (typeName != ((ArrowType)_currentStyle.Type).ToString())
                    {
                        switch (typeName)
                        {
                            case "Filled":
                                _currentStyle.Type = ArrowType.Filled;
                                break;
                            case "Open":
                                _currentStyle.Type = ArrowType.Open;
                                break;
                            case "Outline":
                                _currentStyle.Type = ArrowType.Outline;
                                break;
                            case "Ellipse":
                                _currentStyle.Type = ArrowType.Ellipse;
                                break;
                            case "Dot":
                                _currentStyle.Type = ArrowType.Dot;
                                break;

                        }
                        needsUpdate = true;
                    }
                }
            }

            return needsUpdate;
        }


        public void WillClose()
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                ArrowStyle.PropagateArrowStyleChanges(_currentStyle.Id);
                Globals.Events.AttributesListChanged();
            }
        }

        private void _renameStyleButton_Click(object sender, RoutedEventArgs e)
        {
            EnableRename();
        }

        private void EnableRename()
        {
            _styleComboBox.Visibility = Visibility.Collapsed;
            _styleNameBox.Visibility = Visibility.Visible;
            _styleNameBox.Focus(FocusState.Programmatic);
            _styleNameBox.SelectAll();
        }

        private void UpdateStyleName()
        {
            if (_currentStyle != null && _currentStyle.Id != 0)
            {
                if (_currentStyle.Name != _styleNameBox.Text)
                {
                    _currentStyle.Name = _styleNameBox.Text;
                    int index = _styleComboBox.SelectedIndex;
                    RefreshStyleList();
                    _styleComboBox.SelectedIndex = index;
                }
            }

            _styleNameBox.Visibility = Visibility.Collapsed;
            _styleComboBox.Visibility = Visibility.Visible;
        }

        private void _newStyleButton_Click(object sender, RoutedEventArgs e)
        {
            ArrowStyle style = Globals.ActiveDrawing.NewArrowStyle();

            ComboBoxItem item = new ComboBoxItem();
            item.Content = style.Name;
            item.Tag = style.Id;
            _styleComboBox.Items.Add(item);

            _styleComboBox.SelectedItem = item;

            EnableRename();
        }

        private void _deleteStyleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStyle != null && _currentStyle.Id != 0)
            {
                if (ArrowStyle.GetInstanceCount(_currentStyle.Id) == 0)
                {
                    if (Globals.ArrowStyleTable.ContainsKey(_currentStyle.Id))
                    {
                        Globals.ArrowStyleTable.Remove(_currentStyle.Id);
                        RefreshStyleList();
                        _styleComboBox.SelectedIndex = 0;
                    }
                }
            }
        }

        private void UpdateStyleSample()
        {
            if (_currentStyle != null && _start != null)
            {
                //_nameBlock.SetValue(Canvas.TopProperty, _start.Y - _nameBlock.ActualHeight - 4);
                //_nameBlock.SetValue(Canvas.LeftProperty, (_start.X + _end.X - _nameBlock.ActualWidth) / 2);

                //_nameBlock.Text = _currentStyle.Name;

                _path.Fill = (_currentStyle.Type == ArrowType.Filled || _currentStyle.Type == ArrowType.Dot) ? _path.Stroke : null;

                //var displayInformation = DisplayInformation.GetForCurrentView();
                //double size = _currentStyle.Size * displayInformation.RawDpiX;
                double size = _currentStyle.Size * Globals.DPI;

                GeometryGroup geomGroup = new GeometryGroup();

                geomGroup.Children.Add(CGeometry.ArrowGeometry(_start, _end, _currentStyle.Type, size, _currentStyle.Aspect));

                LineGeometry line = new LineGeometry();
                if (_currentStyle.Type == ArrowType.Outline)
                {
                    line.StartPoint = Construct.OffsetAlongLine(_start, _end, size);
                    line.EndPoint = Construct.OffsetAlongLine(_end, _start, size);
                }
                else if (_currentStyle.Type == ArrowType.Ellipse)
                {
                    line.StartPoint = Construct.OffsetAlongLine(_start, _end, size / 2);
                    line.EndPoint = Construct.OffsetAlongLine(_end, _start, size / 2);
                }
                else
                {
                    line.StartPoint = _start;
                    line.EndPoint = _end;
                }
                geomGroup.Children.Add(line);

                geomGroup.Children.Add(CGeometry.ArrowGeometry(_end, _start, _currentStyle.Type, size, _currentStyle.Aspect));

                _path.Data = geomGroup;
            }
        }

        private void _sampleField_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize != e.PreviousSize)
            {
                double y = _sampleField.ActualHeight / 2;
                _start = new Point(10, y);
                _end = new Point(_sampleField.ActualWidth - 10, y);
            }
        }

        private void _lengthBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                UpdateStyleSample();
                ArrowStyle.PropagateArrowStyleChanges(_currentStyle.Id);
            }
        }

        private void _aspectBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                UpdateStyleSample();
                ArrowStyle.PropagateArrowStyleChanges(_currentStyle.Id);
            }
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-arrow" }, { "source", "help" } });

            _ttSettingsArrowStylesIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-arrow" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttSettingsArrowStylesName.IsOpen = true;
                        break;

                    case "name":
                        _ttSettingsArrowStylesRename.IsOpen = true;
                        break;

                    case "rename":
                        _ttSettingsArrowStylesSample.IsOpen = true;
                        break;

                    case "sample":
                        _ttSettingsArrowStylesType.IsOpen = true;
                        break;

                    case "type":
                        _ttSettingsArrowStylesLength.IsOpen = true;
                        break;

                    case "length":
                        _ttSettingsArrowStylesAspect.IsOpen = true;
                        break;

                    case "aspect":
                        _ttSettingsArrowStylesCount.IsOpen = true;
                        break;

                    case "count":
                        _ttSettingsArrowStylesAdd.IsOpen = true;
                        break;

                    case "add":
                        _ttSettingsArrowStylesDelete.IsOpen = true;
                        break;
                }
            }
        }
    }
}
