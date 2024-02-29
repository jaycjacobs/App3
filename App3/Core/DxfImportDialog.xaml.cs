using Cirros.Drawing;
using Cirros.Dxf;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace Cirros.Dialogs
{
    public sealed partial class DxfImportDialog : ContentDialog
    {
        double _minimumScale = 1;

        DxfContext _context;

        bool _metric = false;
        Size _sheetSize = new Size();
        double _unitScale = 1;
        Unit _paperUnit = Unit.Inches;
        Unit _modelUnit = Unit.Millimeters;

        public DxfImportDialog(DxfContext context)
        {
            this.InitializeComponent();
            this.Loaded += DxfImportDialog_Loaded;

            _context = context;
        }

        private void DxfImportDialog_Loaded(object sender, RoutedEventArgs e)
        {
            _modelExtents.Text = string.Format("{0:0.####} x {1:0.####}", _context.ModelExtents.Width, _context.ModelExtents.Height);

            _sheetSizeComboBox.SelectedIndex = 0;
            _widthBox.IsEnabled = true;
            _heightBox.IsEnabled = true;
            _metric = _context.Metric;

            _themeComboBox.ItemsSource = Globals.Themes.Values;
            _themeComboBox.SelectedItem = Globals.Theme;

            string us = "in,in";

            switch (_context.ModelUnit)
            {
                case Unit.Inches:
                default:
                    us = "in,in";
                    break;
                case Unit.Feet:
                    us = "ft,in";
                    break;
                case Unit.Millimeters:
                    us = "mm,mm";
                    break;
                case Unit.Centimeters:
                    us = "cm,mm";
                    break;
                case Unit.Meters:
                    us = "m,mm";
                    break;
            }

            _unitScale = _metric ? 25.4 : 1;

            foreach (object o in _unitComboBox.Items)
            {
                if (o is ComboBoxItem c && c.Tag is string s)
                {
                    if (s == us)
                    {
                        _unitComboBox.SelectedItem = c;
                        break;
                    }
                }
            }
                
            if (_context.DrawingWidth > 0 && _context.DrawingHeight > 0)
            {
                //_sheetSize.Width = _context.DrawingWidth * _unitScale;
                //_sheetSize.Height = _context.DrawingHeight * _unitScale;
                _sheetSize.Width = _context.DrawingWidth;
                _sheetSize.Height = _context.DrawingHeight;
                _widthBox.Text = _sheetSize.Width.ToString("0.####");
                _heightBox.Text = _sheetSize.Height.ToString("0.####");
                _sheetSizeComboBox.SelectedIndex = 0;
            }
            else
            {
                _sheetSizeComboBox.SelectedIndex = 1;
            }

            if (_metric)
            {
                _paperUnit = _modelUnit = Unit.Millimeters;
            }
            else
            {
                _paperUnit = _modelUnit = Unit.Inches;
            }

            UpdateSheetSize();
            ScaleChanged();
        }

        private void addScaleListItem(List<ComboBoxItem> list, string name, int scale)
        {
            ComboBoxItem item = new ComboBoxItem();
            item.Content = name;
            item.Tag = scale;
            list.Add(item);
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (double.TryParse(_widthBox.Text, out double w) && w > 1)
            {
                if (_metric)
                {
                    w /= 25.4;
                }

                if (double.TryParse(_heightBox.Text, out double h) && h > 1)
                {
                    if (_metric)
                    {
                        h /= 25.4;
                    }

                    if (double.TryParse(_scaleBox.Text, out double s) && s > 0)
                    {
                        double ds = 1 / s;
                        int i = (int)Math.Round(ds);
                        if (Math.Round(ds, 2) == i)
                        {
                            _context.DrawingScale = i;
                            s = 1.0 / (double)i;
                        }
                        else
                        {
                            _context.DrawingScale = 1;
                        }

                        _context.Scale = _context.Metric ? s / 25.4 : s;

                        _context.DrawingWidth = w;
                        _context.DrawingHeight = h;

                        _context.Metric = _metric;
                        _context.PaperUnit = _paperUnit;
                        _context.ModelUnit = _modelUnit;

                        _context.ContextChanged();

                    }
                    else
                    {
                        _scaleBox.SelectAll();
                        _scaleBox.Focus(FocusState.Keyboard);
                        args.Cancel = true;
                    }
                }
                else
                {
                    _heightBox.SelectAll();
                    _heightBox.Focus(FocusState.Keyboard);
                    args.Cancel = true;
                }
            }
            else
            {
                _widthBox.SelectAll();
                _widthBox.Focus(FocusState.Keyboard);
                args.Cancel = true;
            }

        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void _unitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && e.AddedItems[0] is ComboBoxItem item)
            {
                if (item.Tag is string tag)
                {
                    bool metric = _metric;

                    string[] sa = tag.Split(new char[] { ',' });
                    if (sa.Length == 2)
                    {
                        switch (sa[0])
                        {
                            case "ft":
                                _modelUnit = Unit.Feet;
                                _paperUnit = Unit.Inches;
                                metric = false;
                                //imperial = true;
                                break;
                            case "in":
                                _modelUnit = Unit.Inches;
                                _paperUnit = Unit.Inches;
                                metric = false;
                                break;
                            case "mm":
                                _modelUnit = Unit.Millimeters;
                                _paperUnit = Unit.Millimeters;
                                metric = true;
                                break;
                            case "cm":
                                _modelUnit = Unit.Centimeters;
                                _paperUnit = Unit.Millimeters;
                                metric = true;
                                break;
                            case "m":
                                _modelUnit = Unit.Meters;
                                _paperUnit = Unit.Millimeters;
                                metric = true;
                                break;
                        }

                        if (metric != _metric)
                        {
                            if (double.TryParse(_widthBox.Text, out double w))
                            {
                                if (metric)
                                {
                                    _widthBox.Text = (w * 25.4).ToString("0.####");
                                }
                                else
                                {
                                    _widthBox.Text = (w / 25.4).ToString("0.####");
                                }
                            }
                            if (double.TryParse(_heightBox.Text, out double h))
                            {
                                if (metric)
                                {
                                    _heightBox.Text = (h * 25.4).ToString("0.####");
                                }
                                else
                                {
                                    _heightBox.Text = (h / 25.4).ToString("0.####");
                                }
                            }

                            _metric = metric;
                            _unitScale = _metric ? 25.4 : 1;
                        }
                    }
                }

                UpdateSheetSize();
                ScaleChanged();
            }
        }

        private void UpdateSheetSize()
        {
            if (_sheetSizeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                if (tag == "")
                {
                    _widthBox.IsEnabled = true;
                    _heightBox.IsEnabled = true;

                    if (double.TryParse(_widthBox.Text, out double w))
                    {
                        _sheetSize.Width = w;
                    }
                    if (double.TryParse(_heightBox.Text, out double h))
                    {
                        _sheetSize.Height = h;
                    }
                }
                else
                {
                    _widthBox.IsEnabled = false;
                    _heightBox.IsEnabled = false;

                    string[] sa = tag.Split(new char[] { ',' });
                    if (sa.Length == 2)
                    {
                        if (double.TryParse(sa[0], out double w))
                        {
                            _sheetSize.Width = w * _unitScale;
                            _widthBox.Text = _sheetSize.Width.ToString("0.######");
                        }
                        if (double.TryParse(sa[1], out double h))
                        {
                            _sheetSize.Height = h * _unitScale;
                            _heightBox.Text = _sheetSize.Height.ToString("0.######");
                        }
                    }
                }

                double extWidth = _context.Metric && _metric == false ? _context.ModelExtents.Width / 25.4 : _context.ModelExtents.Width;
                double extHeight = _context.Metric && _metric == false ? _context.ModelExtents.Height / 25.4 : _context.ModelExtents.Height;

                double xs = extWidth / _sheetSize.Width;
                double ys = extHeight / _sheetSize.Height;
                _minimumScale = Math.Max(xs, ys);
                _scaleBox.Text = (1 / _minimumScale).ToString("0.########");

                if (_scaleComboBox.Items.Count > 0)
                {
#if defaultIsFit
                    if (_scaleComboBox.SelectedItem == null)
                    {
                        ComboBoxItem cbi = _scaleComboBox.Items[0] as ComboBoxItem;
                        _scaleComboBox.SelectedItem = cbi;
                    }
#else
                    if (_scaleComboBox.SelectedIndex != 0)
                    {
                        _scaleComboBox.SelectedItem = null;
                    }
#endif
                    foreach (object o in _scaleComboBox.Items)
                    {
                        if (o is ComboBoxItem i && i.Tag is string s)
                        {
                            if (int.TryParse(s, out int iv))
                            {
                                if (iv < 1)
                                {
                                    i.IsEnabled = true;
                                }
                                else if ((double)iv > _minimumScale)
                                {
                                    i.IsEnabled = true;
#if defaultIsFit
#else
                                    if (_scaleComboBox.SelectedItem == null)
                                    {
                                        _scaleComboBox.SelectedItem = i;
                                    }
#endif
                                }
                                else
                                {
                                    i.IsEnabled = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void _sheetSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && e.AddedItems[0] is ComboBoxItem item)
            {
                UpdateSheetSize();
                ScaleChanged();
            }
        }

        private void _scaleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_scaleBox.Visibility == Visibility.Collapsed)
            {
                _scaleErrorText.Visibility = Visibility.Collapsed;
            }
            else if (double.TryParse(_scaleBox.Text, out double s))
            {
                double test = Math.Round(1 / _minimumScale, 5);
                _scaleErrorText.Visibility = s <= test ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                _scaleErrorText.Visibility = Visibility.Collapsed;
            }
        }

        private void _scaleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && e.AddedItems[0] is ComboBoxItem item)
            {
                ScaleChanged();
            }
        }

        private void ScaleChanged()
        {
            if (_scaleComboBox.SelectedItem is ComboBoxItem item && item.Tag is string s)
            {
                if (int.TryParse(s, out int i))
                {
                    if (i == 0)
                    {
                        _scaleBox.Visibility = Visibility.Visible;
                        _scaleValueText.Visibility = Visibility.Visible;
                        _scaleErrorText.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        _scaleBox.Visibility = Visibility.Collapsed;
                        _scaleValueText.Visibility = Visibility.Collapsed;
                        _scaleErrorText.Visibility = Visibility.Collapsed;

                        if (i > 0)
                        {
                            //_context.DrawingScale = i;
                            _scaleBox.Text = (1 / (double)i).ToString("0.########");
                        }
                        else
                        {
                            double extWidth = _context.Metric && _metric == false ? _context.ModelExtents.Width / 25.4 : _context.ModelExtents.Width;
                            double extHeight = _context.Metric && _metric == false ? _context.ModelExtents.Height / 25.4 : _context.ModelExtents.Height;

                            double xs = extWidth / _sheetSize.Width;
                            double ys = extHeight / _sheetSize.Height;
                            _minimumScale = Math.Max(xs, ys);
                            _scaleBox.Text = (1 / _minimumScale).ToString("0.########");
                        }
                    }
                }
            }
        }

        private void sizeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            bool changed = false;

            if (double.TryParse(_widthBox.Text, out double w))
            {

            }
            else
            {

            }

            if (double.TryParse(_heightBox.Text, out double h))
            {

            }
            else
            {

            }

            if (changed)
            {
                UpdateSheetSize();
                ScaleChanged();
            }
        }

        private void _symbolComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && e.AddedItems[0] is ComboBoxItem item)
            {
                if (item.Tag is string tag)
                {
                    if (_context != null)
                    {
                        _context.IncludeOnlyDesignCenterBlocksInSymbolLibrary = tag == "designcenter";
                    }
                }
            }
        }

        private void _themeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Globals.Theme = _themeComboBox.SelectedItem as Cirros.Core.Theme;
        }
    }
}
