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
using RedDog.Console;
using Microsoft.UI.Xaml.Controls;

namespace RedDog.HUIApp
{
    public sealed partial class HSettingsLineTypes : UserControl, HUIIDialog
    {
        private Dictionary<string, object> _options = new Dictionary<string, object>();
        LineType _currentLineType = null;

        public string Id
        {
            get { return RedDogGlobals.GS_SettingsLineTypesCommand; }
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        public Dictionary<string, object> Options
        {
            get { return _options; }
        }

        public HSettingsLineTypes()
        {
            this.InitializeComponent();

            this.Loaded += HSettingsLineTypes_Loaded;
        }

        private void _lineTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_lineTypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is int id && Globals.LineTypeTable.ContainsKey(id))
            {
                SelectLineType(Globals.LineTypeTable[id]);
            }
        }

        private void HSettingsLineTypes_Loaded(object sender, RoutedEventArgs e)
        {
            _lineTypeComboBox.SelectionChanged += _lineTypeComboBox_SelectionChanged;
            _lineTypeNameBox.LostFocus += _lineTypeNameBox_LostFocus;
            _lineTypeNameBox.KeyDown += _lineTypeNameBox_KeyDown;

            Populate();

            foreach (ComboBoxItem item in _lineTypeComboBox.Items)
            {
                if (item.Tag is int lineTypeId && lineTypeId == RedDogGlobals.SelectedLineTypeId)
                {
                    _lineTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            if (_lineTypeComboBox.SelectedItem == null)
            {
                _lineTypeComboBox.SelectedIndex = 0;
            }

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        private void _lineTypeNameBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                UpdateLineTypeName();
            }
        }

        private void _lineTypeNameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateLineTypeName();
        }

        private void RefreshLineTypeList()
        {
            _lineTypeComboBox.Items.Clear();

            foreach (LineType lineType in Globals.LineTypeTable.Values)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = lineType.Name;
                item.Tag = lineType.Id;
                _lineTypeComboBox.Items.Add(item);
            }
        }

        private void SelectLineType(LineType selectedLineType)
        {
            if (_currentLineType != null && UpdateCurrentLineTypettributes())
            {
                Globals.Events.AttributesListChanged();
                UpdateLineTypeSample();
                LineType.PropagateLineTypeChanges(_currentLineType.Id);
            }

            _lineTypeNameBox.Visibility = Visibility.Collapsed;
            _lineTypeComboBox.Visibility = Visibility.Visible;

            _currentLineType = null;

            if (selectedLineType != null)
            {
                RedDogGlobals.SelectedLineTypeId = selectedLineType.Id;

                _lineTypeNameBox.Text = selectedLineType.Name;
                _lineTypeNameBox.IsEnabled = selectedLineType.Id != 0;

                int objects = LineType.GetInstanceCount(selectedLineType.Id);
                int layers = LineType.GetContainingLayerCount(selectedLineType.Id);

                _objectCount.Text = objects.ToString();
                _layerCount.Text = layers.ToString();

                _deleteLineTypeButton.IsEnabled = selectedLineType.Id != 0 && objects == 0 && layers == 0;

                _currentLineType = selectedLineType;
                _currentLineTypeIsDirty = false;

                if (selectedLineType.StrokeDashArray == null || selectedLineType.StrokeDashArray.Count < 2)
                {
                    // solid line
                    _dash1Box.Text = "";
                    _gap1Box.Text = "";
                    _dash4Box.Visibility = Visibility.Collapsed;
                    _gap4Box.Visibility = Visibility.Collapsed;
                    _dash3Box.Visibility = Visibility.Collapsed;
                    _gap3Box.Visibility = Visibility.Collapsed;
                    _dash2Box.Visibility = Visibility.Collapsed;
                    _gap2Box.Visibility = Visibility.Collapsed;
                    _dash1Box.Visibility = Visibility.Visible;
                    _gap1Box.Visibility = Visibility.Collapsed;
                }
                else
                {
                    double mfactor = Globals.ActiveDrawing.PaperUnit == Unit.Millimeters ? 25.4 : 1;

                    _dash1Box.Value = selectedLineType.StrokeDashArray[0] * mfactor;
                    _gap1Box.Value = selectedLineType.StrokeDashArray[1] * mfactor;
                    _dash1Box.Visibility = Visibility.Visible;
                    _gap1Box.Visibility = Visibility.Visible;

                    if (selectedLineType.StrokeDashArray.Count > 3)
                    {
                        _dash2Box.Value = selectedLineType.StrokeDashArray[2] * mfactor;
                        _gap2Box.Value = selectedLineType.StrokeDashArray[3] * mfactor;
                        _dash2Box.Visibility = Visibility.Visible;
                        _gap2Box.Visibility = Visibility.Visible;

                        if (selectedLineType.StrokeDashArray.Count > 5)
                        {
                            _dash3Box.Value = selectedLineType.StrokeDashArray[4] * mfactor;
                            _gap3Box.Value = selectedLineType.StrokeDashArray[5] * mfactor;
                            _dash3Box.Visibility = Visibility.Visible;
                            _gap3Box.Visibility = Visibility.Visible;

                            if (selectedLineType.StrokeDashArray.Count > 7)
                            {
                                _dash4Box.Value = selectedLineType.StrokeDashArray[6] * mfactor;
                                _gap4Box.Value = selectedLineType.StrokeDashArray[7] * mfactor;
                                _dash4Box.Visibility = Visibility.Visible;
                                _gap4Box.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                _dash4Box.Text = "";
                                _gap4Box.Text = "";
                                _dash4Box.Visibility = Visibility.Visible;
                                _gap4Box.Visibility = Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            _dash3Box.Text = "";
                            _gap3Box.Text = "";
                            _dash4Box.Visibility = Visibility.Collapsed;
                            _gap4Box.Visibility = Visibility.Collapsed;
                            _dash3Box.Visibility = Visibility.Visible;
                            _gap3Box.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        _dash2Box.Text = "";
                        _gap2Box.Text = "";
                        _dash4Box.Visibility = Visibility.Collapsed;
                        _gap4Box.Visibility = Visibility.Collapsed;
                        _dash3Box.Visibility = Visibility.Collapsed;
                        _gap3Box.Visibility = Visibility.Collapsed;
                        _dash2Box.Visibility = Visibility.Visible;
                        _gap2Box.Visibility = Visibility.Collapsed;
                    }
                }

                UpdateLineTypeSample();
                LineType.PropagateLineTypeChanges(_currentLineType.Id);
            }
            else
            {
                _lineTypeNameBox.Text = "";
                _deleteLineTypeButton.IsEnabled = false;
            }
        }

        public void Populate()
        {
            RefreshLineTypeList();
        }

        private bool UpdateCurrentLineTypettributes()
        {
            if (_currentLineType != null && _currentLineType.StrokeDashArray != null)
            {
                for (int i = 0; i < _currentLineType.StrokeDashArray.Count; i += 2)
                {
                    if (_currentLineType.StrokeDashArray[i] == 0)
                    {
                        _currentLineType.StrokeDashArray.RemoveAt(i);
                        if (i < _currentLineType.StrokeDashArray.Count)
                        {
                            _currentLineType.StrokeDashArray.RemoveAt(i);
                        }
                        _currentLineTypeIsDirty = true;
                        break;
                    }
                }
            }

            bool needsUpdate = _currentLineTypeIsDirty == true;
            _currentLineTypeIsDirty = false;

            return needsUpdate;
        }


        public void WillClose()
        {
            if (_currentLineType != null && UpdateCurrentLineTypettributes())
            {
                LineType.PropagateLineTypeChanges(_currentLineType.Id);
                Globals.Events.AttributesListChanged();
            }
        }

        private void _renameLineTypeButton_Click(object sender, RoutedEventArgs e)
        {
            EnableRename();
        }

        private void EnableRename()
        {
            _lineTypeComboBox.Visibility = Visibility.Collapsed;
            _lineTypeNameBox.Visibility = Visibility.Visible;
            _lineTypeNameBox.Focus(FocusState.Programmatic);
            _lineTypeNameBox.SelectAll();
        }

        private void UpdateLineTypeName()
        {
            if (_currentLineType != null && _currentLineType.Id != 0)
            {
                if (_currentLineType.Name != _lineTypeNameBox.Text)
                {
                    _currentLineType.Name = _lineTypeNameBox.Text;
                    int index = _lineTypeComboBox.SelectedIndex;
                    RefreshLineTypeList();
                    _lineTypeComboBox.SelectedIndex = index;
                }
            }

            _lineTypeNameBox.Visibility = Visibility.Collapsed;
            _lineTypeComboBox.Visibility = Visibility.Visible;
        }

        private void _newLineTypeButton_Click(object sender, RoutedEventArgs e)
        {
            LineType lineType = Globals.ActiveDrawing.NewLineType();

            ComboBoxItem item = new ComboBoxItem();
            item.Content = lineType.Name;
            item.Tag = lineType.Id;
            _lineTypeComboBox.Items.Add(item);

            _lineTypeComboBox.SelectedItem = item;

            EnableRename();
        }

        private void _deleteLineTypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLineType != null && _currentLineType.Id != 0)
            {
                if (LineType.GetContainingLayerCount(_currentLineType.Id) == 0 && LineType.GetInstanceCount(_currentLineType.Id) == 0)
                {
                    if (Globals.LineTypeTable.ContainsKey(_currentLineType.Id))
                    {
                        Globals.LineTypeTable.Remove(_currentLineType.Id);
                        RefreshLineTypeList();
                        _lineTypeComboBox.SelectedIndex = 0;
                    }
                }
            }
        }

        private void UpdateLineTypeSample()
        {
            if (_currentLineType != null && _currentLineType.StrokeDashArray != null && _currentLineType.StrokeDashArray.Count > 1)
            {
                DoubleCollection dc = new DoubleCollection();
                foreach (double d in _currentLineType.StrokeDashArray)
                {
                    dc.Add(d * 72 / _line.StrokeThickness);
                }
                _line.StrokeDashArray = dc;
            }
            else
            {
                _line.StrokeDashArray = null;
            }
        }

        private void _sampleField_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize != e.PreviousSize)
            {
                _line.X2 = e.NewSize.Width;
            }
        }

        bool _currentLineTypeIsDirty = false;

        private void _dashBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (sender is NumberBox1 nb && nb.Tag is string s)
            {
                if (int.TryParse(s, out int index))
                {
                    double mfactor = Globals.ActiveDrawing.PaperUnit == Unit.Millimeters ? 25.4 : 1;

                    if (_currentLineType != null)
                    {
                        List<double> dl = new List<double>();

                        if (_currentLineType.StrokeDashArray != null)
                        {
                            foreach (double d in _currentLineType.StrokeDashArray)
                            {
                                dl.Add(d);
                            }
                        }

                        if (dl.Count > index)
                        {
                            if ((index % 2) == 0)
                            {
                                if (nb.Value > 0)
                                {
                                    dl[index] = nb.Value / mfactor;
                                }
                                else
                                {
                                    dl.RemoveAt(index);
                                    if (dl.Count > index)
                                    {
                                        dl.RemoveAt(index);
                                    }
                                }
                            }
                            else if (nb.Value > 0)
                            {
                                dl[index] = nb.Value / mfactor;
                            }
                        }
                        else if (dl.Count == index && nb.Value > 0)
                        {
                            // add a new dash pair
                            dl.Add(nb.Value / mfactor);
                            if (index > 1)
                            {
                                dl.Add(dl[1]);
                            }
                            else
                            {
                                dl.Add((nb.Value / mfactor) / 2);
                            }
                        }
                        else
                        {

                        }

                        DoubleCollection dc = new DoubleCollection();
                        foreach (double d in dl)
                        {
                            dc.Add(d);
                        }
                        _currentLineType.StrokeDashArray = dc;

                        _currentLineTypeIsDirty = true;
                    }

                    if (Globals.LineTypeTable.ContainsKey(_currentLineType.Id))
                    {
                        SelectLineType(Globals.LineTypeTable[_currentLineType.Id]);
                    }
                }
            }
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-line-types" }, { "source", "help" } });

            _ttSettingsLineTypesIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-line-types" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttSettingsLineTypesName.IsOpen = true;
                        break;

                    case "name":
                        _ttSettingsLineTypesRename.IsOpen = true;
                        break;

                    case "rename":
                        _ttSettingsLineTypesSample.IsOpen = true;
                        break;

                    case "sample":
                        _ttSettingsLineTypesLengths.IsOpen = true;
                        break;

                    case "lengths":
                        _ttSettingsLineTypesCount.IsOpen = true;
                        break;

                    case "count":
                        _ttSettingsLineTypesLayerCount.IsOpen = true;
                        break;

                    case "layer-count":
                        _ttSettingsLineTypesAdd.IsOpen = true;
                        break;

                    case "add":
                        _ttSettingsLineTypesDelete.IsOpen = true;
                        break;
                }
            }
        }
    }
}
