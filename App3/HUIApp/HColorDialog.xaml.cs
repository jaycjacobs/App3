using Cirros.Core;
using Cirros.Utility;
using CirrosUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace RedDog.HUIApp
{
    public sealed partial class HColorDialog : UserControl
    {
        uint _colorSpec = 0;

        public HColorDialog()
        {
            this.InitializeComponent();

            this.Loaded += HColorDialog_Loaded;
        }

        public uint ColorSpec
        {
            get { return _colorSpec; }
            set {
                _colorSpec = value;

                if (_colorSpec == 1)
                {
                    _colorIsValid = false;
                    _colorSample.Fill = new SolidColorBrush(Colors.Transparent);
                }
                else
                {
                    _colorIsValid = true;
                    _colorSample.Fill = new SolidColorBrush(Utilities.ColorFromColorSpec(_colorSpec));

                    if (_colorNameBox.Text == "")
                    {
                        _colorNameBox.Text = Utilities.ColorNameFromColorSpec(_colorSpec);
                    }
                }
            }
        }

        private void HColorDialog_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (uint colorspec in Cirros.Globals.RecentColors)
            {
                string colorname = Utilities.ColorNameFromColorSpec(colorspec);
                //_recentColorsListBox.Items.Add(new ColorItemControl(colorspec));
                _recentColorsGridView.Items.Add(new GridViewColorItem(colorname, colorspec, false));
            }

            List<string> list = StandardColors.ColorNames.Keys.ToList();
            list.Sort();

            foreach (string key in list)
            {
                //_namedColorsListBox.Items.Add(new ColorItemControl(Utilities.ColorSpecFromColorName(key)));
                _namedColorsGridView.Items.Add(new GridViewColorItem(key, Utilities.ColorSpecFromColorName(key), false));
            }

            for (int i = 0; i < 256; i++)
            {
                string name = i.ToString();
                uint cspec = Utilities.ColorSpecFromColorName(name);
                //_acadColorsListBox.Items.Add(new ColorItemControl(name, cspec));
                _acadColorsGridView.Items.Add(new GridViewColorItem(name, cspec, true));
            }
        }

        //private void _listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    uint cspec = 1;

        //    if (sender == _recentColorsListBox)
        //    {
        //        if (_recentColorsListBox.SelectedItem is ColorItemControl recent)
        //        {
        //            cspec = recent.ColorSpec;
        //        }
        //    }
        //    //else if (sender == _acadColorsListBox)
        //    //{
        //    //    if (_acadColorsListBox.SelectedItem is ColorItemControl acad)
        //    //    {
        //    //        cspec = acad.ColorSpec;
        //    //    }
        //    //}
        //    //else if (sender == _namedColorsListBox)
        //    //{
        //    //    if (_namedColorsListBox.SelectedItem is ColorItemControl named)
        //    //    {
        //    //        cspec = named.ColorSpec;
        //    //    }
        //    //}

        //    ColorSpec = cspec;
        //    _colorNameBox.Text = Utilities.ColorNameFromColorSpec(_colorSpec);

        //}

        private void ColorPicker_ColorChanged(Microsoft.UI.Xaml.Controls.ColorPicker sender, Microsoft.UI.Xaml.Controls.ColorChangedEventArgs args)
        {
            if (sender == _colorPicker)
            {
                ColorSpec = Utilities.ColorSpecFromColor(_colorPicker.Color);
                _colorNameBox.Text = Utilities.ColorNameFromColorSpec(_colorSpec);
            }
        }

        bool _colorIsValid = true;

        private void buttonClick(object sender, RoutedEventArgs e)
        {
            if (Parent is Popup popup)
            {
                if (sender is Button b && b.Tag is string tag)
                {
                    if (tag == "ok" && _colorIsValid == false)
                    {
                        _invalidNameTeachingTip.Title = $"{_colorNameBox.Text} is not a valid color name";
                        _invalidNameTeachingTip.IsOpen = true;
                    }
                    else
                    {
                        popup.IsOpen = false;
                    }
                }
            }
        }

        private void _colorNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FocusManager.GetFocusedElement() == _colorNameBox)
            {
                ColorSpec = Utilities.ColorSpecFromColorName(_colorNameBox.Text);
            }
        }

        private void TabViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string s && s == "picker")
            {
                _colorPicker.Color = Utilities.ColorFromColorSpec(_colorSpec);
            }
        }

        private void _colorsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is GridViewColorItem colorItem)
            {
                ColorSpec = colorItem.ColorSpec;
                _colorNameBox.Text = Utilities.ColorNameFromColorSpec(_colorSpec);
            }
        }
    }
    public class GridViewColorItem
    {
        public Color Color { get; set; }
        public Brush Brush { get; set; }
        public Brush TextBrush { get; set; }
        public string ColorName { get; set; }
        public uint ColorSpec { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public GridViewColorItem(string name, uint cspec, bool compact)
        {
            ColorName = name;
            ColorSpec = cspec;
            Color = Utilities.ColorFromColorSpec(cspec);
            Brush = new SolidColorBrush(Color);

            double brightness = Color.R * 0.3 + Color.G * 0.59 + Color.B * 0.11;
            TextBrush = new SolidColorBrush(brightness > 128 ? Colors.Black : Colors.White);

            if (compact)
            {
                Width = 59;
                Height = 28;
            }
            else
            {
                Width = 150;
                Height = 28;
            }
        }
    }
}
