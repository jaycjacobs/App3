using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using KT22.UI;

namespace CirrosUWP.RedDog
{
    public sealed partial class RedDogTeachingTipEditor : ContentDialog
    {
        public RedDogTeachingTipEditor()
        {
            this.InitializeComponent();

            this.Loaded += RedDogTeachingTipEditor_Loaded;
            this.KeyDown += RedDogTeachingTipEditor_KeyDown;

            _textBox.KeyDown += _textBox_KeyDown;
        }

        private void _textBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            IsModified = true;
            _htmlBlock.Html = _textBox.Text;
        }

        private void RedDogTeachingTipEditor_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            IsModified = true;
            _htmlBlock.Html = _textBox.Text;
        }

        private void RedDogTeachingTipEditor_Loaded(object sender, RoutedEventArgs e)
        {
            if (TeachingTip is TeachingTip && HtmlTextBlock is HtmlTextBlock)
            {
                _htmlBlock.Html = HtmlTextBlock.Html;
                _textBox.Text = HtmlTextBlock.Html;
                _spellCheckCB.IsChecked = _textBox.IsSpellCheckEnabled;
            }
        }

        public TeachingTip TeachingTip { set; get; }

        public HtmlTextBlock HtmlTextBlock { set; get; }

        public bool IsModified { set; get; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string tag)
            {
                if (tag == "ok")
                {
                    HtmlTextBlock.Html = _htmlBlock.Html = _textBox.Text;
                    this.Hide();
                }
                else if (tag == "cancel")
                {
                    IsModified = false;
                    this.Hide();
                }
                else if (tag == "revert")
                {
                    if (TeachingTip != null && string.IsNullOrEmpty(TeachingTip.Name) != true)
                    {
                        var resourceLoader = new ResourceLoader();
                        _textBox.Text = resourceLoader.GetString(TeachingTip.Name);
                        _htmlBlock.Html = _textBox.Text;
                        IsModified = true;
                    }
                }
                else if (tag == "update")
                {
                    IsModified = true;
                    _htmlBlock.Html = _textBox.Text;
                }
            }
        }

        private void _spellCheckCB_Checked(object sender, RoutedEventArgs e)
        {
            _textBox.IsSpellCheckEnabled = true;
        }

        private void _spellCheckCB_Unchecked(object sender, RoutedEventArgs e)
        {
            _textBox.IsSpellCheckEnabled = false;
        }
    }
}
