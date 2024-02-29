using Cirros;
using Cirros.Core;
using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;

namespace CirrosUI
{
    public class FillComboBox : ComboBox
    {
        uint _colorspec = 0x00000000;

        public FillComboBox()
        {
            TextBlock tb = new TextBlock();
            tb.Text = "No fill";
            tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
            tb.Style = (Style)(Application.Current.Resources["SettingsTextSmallNoMargin"]);
            tb.Tag = (uint)ColorCode.NoFill;
            tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
            this.Items.Add(tb);

            tb = new TextBlock();
            tb.Text = "Use layer color";
            tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
            tb.Style = (Style)(Application.Current.Resources["SettingsTextSmallNoMargin"]);
            tb.Tag = (uint)ColorCode.ByLayer;
            tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
            this.Items.Add(tb);

            tb = new TextBlock();
            tb.Text = "Use outline color";
            tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
            tb.Style = (Style)(Application.Current.Resources["SettingsTextSmallNoMargin"]);
            tb.Tag = (uint)ColorCode.SameAsOutline;
            tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
            this.Items.Add(tb);

            tb = new TextBlock();
            tb.Text = "Select a new color";
            tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
            tb.Style = (Style)(Application.Current.Resources["SettingsTextSmallNoMargin"]);
            tb.Tag = (uint)ColorCode.SetColor;
            tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
            this.Items.Add(tb);

            if (Globals.ActiveDrawing != null)
            {
                foreach (uint colorspec in Globals.RecentColors)
                {
                    this.Items.Add(new ColorItemControl(colorspec));
                }
            }
            else
            {
                // most likely this is being instantiated by the xaml designer
                this.Items.Add(new ColorItemControl(Colors.Blue));
            }

            this.SelectionChanged += FillComboBox_SelectionChanged;
            this.DropDownClosed += FillComboBox_DropDownClosed;
        }

        void FillComboBox_DropDownClosed(object sender, object e)
        {
            UpdateColorList();
        }

        void UpdateColorList()
        {
            int max = Math.Min(this.Items.Count - 4, Globals.RecentColors.Count);

            for (int i = 0; i < max; i++)
            {
                ColorItemControl cic = (ColorItemControl)this.Items[i + 4];
                cic.ColorSpec = Globals.RecentColors[i];
            }

            if (this.SelectedIndex < 0 || this.SelectedIndex == 3 || this.SelectedIndex > 4)
            {
                this.SelectedIndex = 4;
            }
        }

        void FillComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ColorItemControl cic = this.SelectedItem as ColorItemControl;
            TextBlock tb = this.SelectedItem as TextBlock;
            
            if (tb != null)
            {
                _colorspec = (uint)tb.Tag;
            }
            else if (cic != null)
            {
                _colorspec = cic.ColorSpec;
                Globals.PushRecentColor(_colorspec);
            }
        }

        public uint DisplayedColorSpec
        {
            get
            {
                uint colorspec = 0xff0000ff;

                if (this.Items.Count > 4 && this.Items[4] is ColorItemControl)
                {
                    colorspec = ((ColorItemControl)this.Items[4]).ColorSpec;
                }

                return colorspec;
            }
        }

        public uint ColorSpec
        {
            get
            {
                return _colorspec;
            }
            set
            {
                _colorspec = value;

                if (_colorspec == (uint)ColorCode.ByLayer)
                {
                    this.SelectedIndex = 1;
                }
                else if (_colorspec == (uint)ColorCode.NoFill)
                {
                    this.SelectedIndex = 0;
                }
                else if (_colorspec == (uint)ColorCode.SameAsOutline)
                {
                    this.SelectedIndex = 2;
                }
                else if (_colorspec == (uint)ColorCode.SetColor)
                {
                    // this should never be selected
                }
                else
                {
                    Globals.PushRecentColor(_colorspec);
                    UpdateColorList();
                    this.SelectedIndex = 4;
                }
            }
        }
    }
}
