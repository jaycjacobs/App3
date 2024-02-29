using Cirros;
using Cirros.Primitives;
using CirrosCore;
using HUI;
using Microsoft.UI.Xaml.Controls;
using RedDog;
using RedDog.Console;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI;

namespace RedDog.HUIApp
{
    public sealed partial class HSettingsPatterns : UserControl, HUIIDialog
    {
        public string Id
        {
            get { return RedDogGlobals.GS_SettingsPatternsCommand; }
        }

        public Dictionary<string, object> Options
        {
            get { return null; }
        }

        public FrameworkElement HelpButton
        {
            get { return _helpButton; }
        }

        private bool _isDirty = false;
        private List<string> _patternsInUse = null;
        ComboBoxItem _newPatternItem = null;
        CrosshatchPattern _currentPattern = null;

        public HSettingsPatterns()
        {
            this.InitializeComponent();

            this.Loaded += HSettingsPatterns_Loaded;
        }

        private void HSettingsPatterns_Loaded(object sender, RoutedEventArgs e)
        {
            Populate();

            foreach (ComboBoxItem item in _patternComboBox.Items)
            {
                if (item.Tag is int lineTypeId && lineTypeId == RedDogGlobals.SelectedLineTypeId)
                {
                    _patternComboBox.SelectedItem = item;
                    break;
                }
            }

            if (_patternComboBox.SelectedItem == null)
            {
                _patternComboBox.SelectedIndex = 0;
            }

            DataContext = CirrosUWP.HUIApp.HGlobals.DataContext;
            ConsoleUtilities.PopulateTeachingTips(this as FrameworkElement);
        }

        public void Populate()
        {
            RefreshPatternList();
        }

        private void RefreshPatternList()
        {
            if (_newPatternItem != null)
            {
                _newPatternItem = null;
            }

            _patternComboBox.Items.Clear();

            foreach (string key in Patterns.PatternDictionary.Keys)
            {
                CrosshatchPattern p = Patterns.PatternDictionary[key];

                ComboBoxItem item = new ComboBoxItem();
                item.Content = p.Name;
                item.Tag = p.Name.ToLower(); ;
                _patternComboBox.Items.Add(item);
            }
        }

        private void _renamePatternButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private List<string> PatternsInUse
        {
            get
            {
                if (_patternsInUse == null)
                {
                    _patternsInUse = new List<string>();

                    foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
                    {
                        if (p.FillPattern != null && p.FillPattern != "Solid")
                        {
                            Patterns.AddPatternFromEntityToList(p, _patternsInUse);
                        }
                    }

                    foreach (string key in Globals.ActiveDrawing.Groups.Keys)
                    {
                        Group g = Globals.ActiveDrawing.Groups[key];
                        foreach (string patternName in g.CrosshatchPatterns)
                        {
                            if (_patternsInUse.Contains(patternName) == false)
                            {
                                _patternsInUse.Add(key);
                            }
                        }
                    }
                }

                return _patternsInUse;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
#if true
            await System.Threading.Tasks.Task.Delay(1);
#else
            if (sender == _fileButton)
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".pat");

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    int dups = 0;

                    List<CrosshatchPattern> patterns = await Patterns.ParsePatternFile(file);

                    if (patterns != null && patterns.Count > 0)
                    {
                        foreach (CrosshatchPattern pattern in patterns)
                        {
                            if (Patterns.PatternDictionary.ContainsKey(pattern.Name.ToLower()))
                            {
                                dups++;
                            }
                            else
                            {
                                Patterns.AddDefaultPattern(pattern);
                            }
                        }

                        _patternNames = null;
                        Populate();

                        string lcname = patterns[0].Name.ToLower();

                        foreach (object item in _patternListBox.Items)
                        {
                            if (item is string name)
                            {

                                if (name.ToLower() == lcname)
                                {
                                    _patternListBox.SelectedItem = item;
                                    _patternListBox.ScrollIntoView(item);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else 
#endif
            if (sender == _editButton)
            {
                if (_textBox.IsEnabled == false)
                {
                    SaveAlert("edit");

                    _textBox.IsEnabled = true;
                    _saveButton.IsEnabled = false;
                    _isDirty = false;
                }
            }
            else if (sender == _saveButton)
            {
                if (_textBox.IsEnabled)
                {
                    if (_isDirty)
                    {
                        if (SavePattern())
                        {
                            _textBox.IsEnabled = false;
                            _saveButton.IsEnabled = false;
                        }
                    }
                    else
                    {
                        _textBox.IsEnabled = false;
                        _saveButton.IsEnabled = false;
                    }
                }
            }
            else if (sender == _deleteButton)
            {
                List<string> patternsInUse = PatternsInUse;
                if (_patternComboBox.SelectedItem is ComboBoxItem item && item.Tag is string name)
                {
                    if (patternsInUse.Contains(name.ToLower()))
                    {
                        string s = string.Format("\"{0}\" can't be deleted because it is used in the current drawing.", name);
                        ShowAlert("inuse", s, "Ok");
                    }
                    else
                    {
                        DeleteAlert();
                    }
                }

            }
            else if (sender == _newButton)
            {
                if (_isDirty)
                {
                    SaveAlert("new");
                }
                else
                {
                    NewPattern();
                }
            }
        }

        private void DeleteAlert()
        {
            if (_patternComboBox.SelectedItem is ComboBoxItem item && item.Tag is string name)
            {
                string s = string.Format("Are you sure you want to delete the selected pattern \"{0}\"", name);
                ShowAlert("delete", s, "Delete", "Don't delete");
            }
        }

        private void SaveAlert(string tag)
        {
            if (_isDirty)
            {
                List<CrosshatchPattern> patternList = Patterns.ParsePattern(_textBox.Text);
                if (patternList.Count > 0)
                {
                    _swatch.SetPattern(patternList[0], Colors.Black);

                    if (string.IsNullOrEmpty(tag))
                    {
                        tag = "save";
                    }
                    else
                    {
                        tag = "save|" + tag;
                    }

                    string s = string.Format("Do you want to save your changes to \"{0}\"", patternList[0].Name);
                    ShowAlert(tag, s, "Save", "Don't save");
                }
            }
        }

        private void ShowAlert(string tag, string content, string ok, string cancel = null)
        {
            _alertContent.Text = content;
            _alertOkButton.Content = ok;
            _alertOkButton.Tag = tag;

            if (string.IsNullOrEmpty(cancel))
            {
                _alertCancelButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                _alertCancelButton.Content = cancel;
                _alertCancelButton.Visibility = Visibility.Visible;
            }

            _alert.Visibility = Visibility.Visible;
        }

        private void _alertButton_Click(object sender, RoutedEventArgs e)
        {
            if (_alert.Visibility == Visibility.Visible && _alertOkButton.Tag is string tag)
            {
                if (tag == "delete")
                {
                    if (sender == _alertOkButton)
                    {
                        if (_patternComboBox.SelectedItem is ComboBoxItem item && item.Tag is string name)
                        {
                            string key = name.ToLower();
                            if (Patterns.PatternDictionary.ContainsKey(key))
                            {
                                CrosshatchPattern pattern = Patterns.PatternDictionary[key];
                                DeleteSelectedPattern();
                            }
                        }
                    }

                    _isDirty = false;
                }
                else if (tag.StartsWith("save"))
                {
                    if (sender == _alertOkButton)
                    {
                        SavePattern();
                    }

                    _isDirty = false;

                    if (tag.EndsWith("new"))
                    {
                        NewPattern();
                    }
                }
            }

            _alert.Visibility = Visibility.Collapsed;
        }

        private void NewPattern()
        {
            for (int i = 1; i < 20; i++)
            {
                string name = string.Format("Pattern{0}", i);
                if (Patterns.PatternDictionary.ContainsKey(name.ToLower()) == false)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("*{0}, New pattern definition\n", name);
                    sb.AppendLine("45,0,0,0,.125");
                    _textBox.Text = sb.ToString();
                    break;
                }
            }

            List<CrosshatchPattern> patternList = Patterns.ParsePattern(_textBox.Text);
            if (patternList.Count > 0)
            {
                _currentPattern = patternList[0];

                _newPatternItem = new ComboBoxItem();
                _newPatternItem.Content = patternList[0].Name;
                _newPatternItem.Tag = patternList[0].Name.ToLower(); ;
                _patternComboBox.Items.Add(_newPatternItem);

                _swatch.SetPattern(patternList[0], Colors.Black);
                SelectComboItem(patternList[0].Name);
            }

            _textBox.IsEnabled = true;
            _saveButton.IsEnabled = true;
            _isDirty = true;
        }

        private void PatternDefinitionChanged()
        {

        }

        private void _patternComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isDirty && _currentPattern != null)
            {
                SaveAlert(_currentPattern.Name);
            }

            if (_patternComboBox.SelectedItem is ComboBoxItem item && item.Tag is string name)
            {
                _patternLoading = true;

                if (_patternComboBox.SelectedItem == _newPatternItem)
                {
                    _textBox.IsEnabled = true;
                    _saveButton.IsEnabled = true;
                }
                else
                {
                    _textBox.IsEnabled = false;
                    _saveButton.IsEnabled = false;
                }
                _newPatternItem = null;

                string key = name.ToLower();
                if (Patterns.PatternDictionary.ContainsKey(key))
                {
                    _currentPattern = Patterns.PatternDictionary[key];
                    _swatch.SetPattern(name, Colors.Black, 1, 0);
                    _textBox.Text = _currentPattern.ToString();

                    _saveButton.IsEnabled = _isDirty || Patterns.IsDefaultPattern(_currentPattern) == false;
                    _editButton.IsEnabled = true;
                    _deleteButton.IsEnabled = true;
                }
            }
            else
            {
                _swatch.SetPattern("", Colors.Transparent, 1, 0);

                _saveButton.IsEnabled = false;
                _editButton.IsEnabled = false;
                _deleteButton.IsEnabled = false;
            }

            _isDirty = false;
        }

        private void DuplicateNameAlert(string name)
        {
            string s = string.Format("There is already a pattern named \"{0}\".  Choose another name or delete the existing pattern.", name);
            ShowAlert("dupname", s, "Ok");
        }

        private bool SavePattern()
        {
            bool success = false;

            if (_currentPattern != null)
            {
                List<CrosshatchPattern> patternList = Patterns.ParsePattern(_textBox.Text);
                CrosshatchPattern pattern = null;

                // existing pattern is being updated (patternList[0].Name == _currentPattern.Name)
                // existing pattern is being renamed

                if (patternList.Count > 0)
                {
                    string key = patternList[0].Name.ToLower();
                    if (Patterns.PatternDictionary.ContainsKey(key))
                    {
                        // the new name already exists in the pattern list
                        if (_currentPattern.Name.ToLower() == key)
                        {
                            // the pattern being edited is the selected pattern (the names match) - save
                            pattern = patternList[0];
                        }
                        else
                        {
                            // the pattern's new name exists
                            DuplicateNameAlert(patternList[0].Name);
                        }
                    }
                    else
                    {
                        // the new name is available - rename
                        pattern = patternList[0];

                        Patterns.DeletePattern(_currentPattern.Name);
                    }

                    if (pattern != null)
                    {
                        _isDirty = false;

                        Patterns.AddDefaultPattern(pattern);

                        RefreshPatternList();

                        SelectComboItem(pattern.Name);

                        Patterns.PropagatePatternChange(pattern.Name);

                        success = true;
                    }
                }
                else
                {
                    // parse failed
                    InvalidPatternAlert();
                }
            }

            return success;
        }

        private void InvalidPatternAlert()
        {
            throw new NotImplementedException();
        }

        private void SelectComboItem(string name)
        {
            string lc = name.ToLower();

            foreach (ComboBoxItem item in _patternComboBox.Items)
            {
                if (item.Tag is string tag && tag == lc)
                {
                    _patternComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void DeleteSelectedPattern()
        {
            if (_patternComboBox.SelectedItem is ComboBoxItem item && item.Tag is string name)
            {
                Patterns.DeletePattern(name);

                RefreshPatternList();

                _patternComboBox.SelectedIndex = 0;
            }
        }

        bool _patternLoading = false;

        private void _textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isDirty == false && _patternLoading == false)
            {
                _isDirty = true;
                _saveButton.IsEnabled = true;
            }
        }

        private void _textBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void _textBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_patternLoading)
            {
                _patternLoading = false;
            }
        }

        public void WillClose()
        {
        }

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-patterns" }, { "source", "help" } });

            _ttSettingsPatternsIntro.IsOpen = true;
        }

        private void _teachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (sender is TeachingTip tip && tip.Tag is string tag)
            {
                tip.IsOpen = false;

                Analytics.ReportEvent("help-tip", new Dictionary<string, string> { { "page", "settings-patterns" }, { "source", tag } });

                switch (tag)
                {
                    case "intro":
                        _ttSettingsPatternsName.IsOpen = true;
                        break;

                    case "name":
                        _ttSettingsPatternsSample.IsOpen = true;
                        break;

                    case "sample":
                        _ttSettingsPatternsDefinition.IsOpen = true;
                        break;

                    case "definition":
                        _ttSettingsPatternsNew.IsOpen = true;
                        break;

                    case "new":
                        _ttSettingsPatternsEdit.IsOpen = true;
                        break;

                    case "edit":
                        _ttSettingsPatternsSave.IsOpen = true;
                        break;

                    case "save":
                        _ttSettingsPatternsDelete.IsOpen = true;
                        break;
                }
            }
        }
    }
}
