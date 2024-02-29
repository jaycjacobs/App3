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
using Windows.Foundation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using Microsoft.Graphics.Display;
using RedDog.Console;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;

namespace RedDog.HUIApp
{
    public sealed partial class HSettingsTextStyles : UserControl, HUIIDialog
    {
        private Dictionary<string, object> _options = new Dictionary<string, object>();
        TextStyle _currentStyle = null;

        public string Id
        {
            get { return RedDogGlobals.GS_SettingsTextStylesCommand; }
        }

        public Dictionary<string, object> Options
        {
            get { return _options; }
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public HSettingsTextStyles()
        {
            this.InitializeComponent();

            this.Loaded += HSettingsTextStyles_Loaded;
        }

        private void _styleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_styleComboBox.SelectedItem is ComboBoxItem item && item.Tag is int id && Globals.TextStyleTable.ContainsKey(id))
            {
                SelectStyle(Globals.TextStyleTable[id]);
            }
        }

        private void HSettingsTextStyles_Loaded(object sender, RoutedEventArgs e)
        {
            _heightBox.OnValueChanged += _heightBox_OnValueChanged;
            _offsetBox.OnValueChanged += _offsetBox_OnValueChanged;
            _lineSpacingBox.OnValueChanged += _lineSpacingBox_OnValueChanged;
            _charSpacingBox.OnValueChanged += _charSpacingBox_OnValueChanged;
            _styleComboBox.SelectionChanged += _styleComboBox_SelectionChanged;
            _styleNameBox.LostFocus += _styleNameBox_LostFocus;
            _styleNameBox.KeyDown += _styleNameBox_KeyDown;
            _fontComboBox.SelectionChanged += _fontComboBox_SelectionChanged;

            Populate();

            foreach (ComboBoxItem item in _styleComboBox.Items)
            {
                if (item.Tag is int styleId && styleId == RedDogGlobals.SelectedTextStyleId)
                {
                    _styleComboBox.SelectedItem = item;
                    break;
                }
            }

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        private void _fontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                UpdateStyleSample();
                TextStyle.PropagateTextStyleChanges(_currentStyle.Id);
            }
        }

        private void _charSpacingBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                UpdateStyleSample();
                TextStyle.PropagateTextStyleChanges(_currentStyle.Id);
            }
        }

        private void _lineSpacingBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                UpdateStyleSample();
                TextStyle.PropagateTextStyleChanges(_currentStyle.Id);
            }
        }

        private void _offsetBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                UpdateStyleSample();
                TextStyle.PropagateTextStyleChanges(_currentStyle.Id);
            }
        }

        private void _heightBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                UpdateStyleSample();
                TextStyle.PropagateTextStyleChanges(_currentStyle.Id);
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

            foreach (TextStyle style in Globals.TextStyleTable.Values)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = style.Name;
                item.Tag = style.Id;
                _styleComboBox.Items.Add(item);
            }
        }

        private void SelectStyle(TextStyle selectedStyle)
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                Globals.Events.AttributesListChanged();
                UpdateStyleSample();
                TextStyle.PropagateTextStyleChanges(_currentStyle.Id);
            }

            _styleNameBox.Visibility = Visibility.Collapsed;
            _styleComboBox.Visibility = Visibility.Visible;

            _currentStyle = null;

            if (selectedStyle != null)
            {
                RedDogGlobals.SelectedTextStyleId = selectedStyle.Id;

                int objects = TextStyle.GetInstanceCount(selectedStyle.Id);
                _objectCount.Text = objects.ToString();
                _deleteStyleButton.IsEnabled = selectedStyle.Id != 0 && objects == 0;

                _styleNameBox.Text = selectedStyle.Name;

                foreach (object o in _fontComboBox.Items)
                {
                    if (o is ComboBoxItem item && item.Tag is string ff && ff == selectedStyle.Font)
                    {
                        _fontComboBox.SelectedItem = o;
                        break;
                    }
                    else if (o is string font && font == selectedStyle.Font)
                    {
                        _fontComboBox.SelectedItem = o;
                        break;
                    }
                }

                double mfactor = Globals.ActiveDrawing.PaperUnit == Unit.Millimeters ? 25.4 : 1;

                _heightBox.Value = selectedStyle.Size * mfactor;
                _offsetBox.Value = selectedStyle.Offset * mfactor;
                _lineSpacingBox.Value = selectedStyle.Spacing;
                _charSpacingBox.Value = selectedStyle.CharacterSpacing;

                _currentStyle = selectedStyle;

                UpdateStyleSample();
            }
            else
            {
                _styleNameBox.Text = "";
                _deleteStyleButton.IsEnabled = false;
            }
        }

        public void Populate()
        {
            _fontComboBox.Items.Clear();

            List<string> fonts = Dx.FontNames;

            foreach (string font in fonts)
            {
                _fontComboBox.Items.Add(font);
            }

            RefreshStyleList();
        }

        private bool UpdateCurrentStyleAttributes()
        {
            bool needsUpdate = false;

            if (_currentStyle != null)
            {
                if (_fontComboBox.SelectedItem is ComboBoxItem cbi && cbi.Content is string ff && _currentStyle.Font != ff)
                {
                    _currentStyle.Font = ff;
                    needsUpdate = true;
                }
                else if (_fontComboBox.SelectedItem is string font && _currentStyle.Font != font)
                {
                    _currentStyle.Font = font;
                    needsUpdate = true;
                }

                double mfactor = Globals.ActiveDrawing.PaperUnit == Unit.Millimeters ? 25.4 : 1;

                if ((_heightBox.Value / mfactor) != _currentStyle.Size)
                {
                    _currentStyle.Size = _heightBox.Value / mfactor;
                    needsUpdate = true;
                }

                if ((_offsetBox.Value / mfactor) != _currentStyle.Offset)
                {
                    _currentStyle.Offset = _offsetBox.Value / mfactor;
                    needsUpdate = true;
                }

                if (_lineSpacingBox.Value != _currentStyle.Spacing)
                {
                    _currentStyle.Spacing = _lineSpacingBox.Value;
                    needsUpdate = true;
                }

                if (_charSpacingBox.Value != _currentStyle.CharacterSpacing)
                {
                    _currentStyle.CharacterSpacing = _charSpacingBox.Value;
                    needsUpdate = true;
                }
            }

            return needsUpdate;
        }

        public void WillClose()
        {
            if (_currentStyle != null && UpdateCurrentStyleAttributes())
            {
                TextStyle.PropagateTextStyleChanges(_currentStyle.Id);
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
            TextStyle style = Globals.ActiveDrawing.NewTextStyle();

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
                if (TextStyle.GetInstanceCount(_currentStyle.Id) == 0)
                {
                    if (Globals.TextStyleTable.ContainsKey(_currentStyle.Id))
                    {
                        Globals.TextStyleTable.Remove(_currentStyle.Id);
                        RefreshStyleList();
                        _styleComboBox.SelectedIndex = 0;
                    }
                }
            }
        }

        private void UpdateStyleSample()
        {
            Size size = UpdateStyleSample(_sampleField);

            _expandSampleButton.Visibility = size.Height > _sampleField.ActualHeight ? Visibility.Visible : Visibility.Collapsed;
        }

        private Size UpdateStyleSample(Canvas canvas)
        {
            Size size = new Size(canvas.Width, canvas.Height);

            if (_currentStyle != null)
            {
                TextBlock tb1 = null;
                TextBlock tb2 = null;
                TextBlock tb3 = null;
                Ellipse e = null;

                //var displayInformation = DisplayInformation.GetForCurrentView();
                double dotsize = 2;
                double margin = 10;
                double textHeight = _currentStyle.Size * Globals.DPI;
                double fontHeight = Dx.GetFontSizeFromHeight(_currentStyle.Font, _currentStyle.Size) * Globals.DPI;
                double lineHeight = textHeight * _currentStyle.Spacing;
                double blockHeight = lineHeight * 2 + textHeight;
                double offset = _currentStyle.Offset * textHeight;
                Point anchor = new Point(margin + margin, blockHeight + offset + margin);
                size.Height = anchor.Y + margin;

                foreach (FrameworkElement fe in canvas.Children)
                {
                    if (fe is TextBlock tb)
                    {
                        if ((string)tb.Tag == "tb1")
                        {
                            tb1 = tb;
                        }
                        else if ((string)tb.Tag == "tb2")
                        {
                            tb2 = tb;
                        }
                        else if ((string)tb.Tag == "tb3")
                        {
                            tb3 = tb;
                        }
                    }
                    else if (fe is Ellipse)
                    {
                        e = fe as Ellipse;
                    }
                }

                if (tb1 == null)
                {
                    tb1 = new TextBlock();
                    tb1.Tag = "tb1";
                    tb1.Text = "ABCDEFG abcdefg";
                    canvas.Children.Add(tb1);
                }
                if (tb2 == null)
                {
                    tb2 = new TextBlock();
                    tb2.Tag = "tb2";
                    tb2.Text = "ABCDEFG abcdefg";
                    canvas.Children.Add(tb2);
                }
                if (tb3 == null)
                {
                    tb3 = new TextBlock();
                    tb3.Tag = "tb3";
                    tb3.Text = "1234567890";
                    canvas.Children.Add(tb3);
                }
                if (e == null)
                {
                    e = new Ellipse();
                    e.Width = dotsize;
                    e.Height = dotsize;
                    e.Fill = new SolidColorBrush(Colors.Green);
                    e.Stroke = new SolidColorBrush(Colors.Green);
                    canvas.Children.Add(e);
                }

                e.SetValue(Canvas.LeftProperty, anchor.X - dotsize / 2);
                e.SetValue(Canvas.TopProperty, anchor.Y - dotsize / 2);

                tb1.FontFamily = new FontFamily(_currentStyle.Font);
                tb2.FontFamily = new FontFamily(_currentStyle.Font);
                tb3.FontFamily = new FontFamily(_currentStyle.Font);

                tb1.FontSize = fontHeight;
                tb2.FontSize = fontHeight;
                tb3.FontSize = fontHeight;

                tb1.CharacterSpacing = (int)Math.Round((_currentStyle.CharacterSpacing - 1) * 800);
                tb2.CharacterSpacing = (int)Math.Round((_currentStyle.CharacterSpacing - 1) * 800);
                tb3.CharacterSpacing = (int)Math.Round((_currentStyle.CharacterSpacing - 1) * 800);

                Size test = new Size(10000, 10000);
                tb1.Measure(test);
                tb2.Measure(test);
                tb3.Measure(test);

                double maxWidth = Math.Max(tb1.ActualWidth, Math.Max(tb2.ActualWidth, tb3.ActualWidth));
                if (tb1.ActualHeight > 0)
                {
                    tb1.SetValue(Canvas.LeftProperty, anchor.X + offset);
                    tb2.SetValue(Canvas.LeftProperty, anchor.X + offset);
                    tb3.SetValue(Canvas.LeftProperty, anchor.X + offset);

                    double tb3y = anchor.Y - tb1.BaselineOffset - offset;
                    tb1.SetValue(Canvas.TopProperty, tb3y - lineHeight - lineHeight);
                    tb2.SetValue(Canvas.TopProperty, tb3y - lineHeight);
                    tb3.SetValue(Canvas.TopProperty, tb3y);

                    size.Width = maxWidth + margin * 3;
                }
            }

            return size;
        }

        private void _sampleField_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize != e.PreviousSize)
            {
                RectangleGeometry rectangle = new RectangleGeometry();
                rectangle.Rect = new Rect(0, 0, _sampleField.ActualWidth - 0, _sampleField.ActualHeight - 0);
                _sampleField.Clip = rectangle;
            }
        }

        private void _expandSampleButton_Click(object sender, RoutedEventArgs e)
        {
            Size size = UpdateStyleSample(_expandedSampleCanvas);

            _expandedSampleCanvas.Width = Math.Max(size.Width + 40, _sampleField.ActualWidth);
            _expandedSampleCanvas.Height = Math.Max(size.Height + 10, _sampleField.ActualHeight);

            _expandedSamplePopup.IsOpen = true;
        }

        private void _expandedSamplePopup_Closed(object sender, object e)
        {

        }

        private void _unexpandSampleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_expandedSamplePopup.IsOpen == true)
            {
                _expandedSamplePopup.IsOpen = false;
            }
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-text-styles" }, { "source", "help" } });

            _ttSettingsTextStylesIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-text-styles" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttSettingsTextStylesName.IsOpen = true;
                        break;

                    case "name":
                        _ttSettingsTextStylesRename.IsOpen = true;
                        break;

                    case "rename":
                        _ttSettingsTextStylesSample.IsOpen = true;
                        break;

                    case "sample":
                        _ttSettingsTextStylesFont.IsOpen = true;
                        break;

                    case "font":
                        _ttSettingsTextStylesHeight.IsOpen = true;
                        break;

                    case "height":
                        _ttSettingsTextStylesOffset.IsOpen = true;
                        break;

                    case "offset":
                        _ttSettingsTextStylesLineSpacing.IsOpen = true;
                        break;

                    case "lspacing":
                        _ttSettingsTextStylesCharSpacing.IsOpen = true;
                        break;

                    case "cspacing":
                        _ttSettingsTextStylesCount.IsOpen = true;
                        break;

                    case "count":
                        _ttSettingsTextStylesAdd.IsOpen = true;
                        break;

                    case "add":
                        _ttSettingsTextStylesDelete.IsOpen = true;
                        break;
                }
            }
        }
    }
}
