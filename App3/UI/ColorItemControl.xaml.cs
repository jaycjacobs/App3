using Cirros;
using Cirros.Core;
using Cirros.Utility;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace CirrosUI
{
    public sealed partial class ColorItemControl : UserControl
    {
        private Color _color = Colors.AliceBlue;
        private string _colorName = "AliceBlue";
        private uint _colorSpec = 0xff00cc00;

        public ColorItemControl()
        {
            this.InitializeComponent();

            DataContext = Globals.UIDataContext;
        }

        public ColorItemControl(Color color)
        {
            this.InitializeComponent();

            DataContext = Globals.UIDataContext;

            ColorValue = color;
        }

        public ColorItemControl(uint colorspec)
        {
            this.InitializeComponent();

            DataContext = Globals.UIDataContext;

            ColorSpec = colorspec;
        }

        public ColorItemControl(string colorName, uint colorspec)
        {
            this.InitializeComponent();

            DataContext = Globals.UIDataContext;

            ColorName = colorName;
            _colorSpec = colorspec;

            ColorValue = Utilities.ColorFromColorSpec(_colorSpec);
        }

        public string ColorName
        {
            get
            {
                return _colorName;
            }
            set
            {
                _colorText.Text = _colorName = value;
                Tag = _colorName;
            }
        }

        public Color ColorValue
        {
            get
            {
                return _color;
            }
            set
            {
                _color = value;
                _colorRect.Fill = new SolidColorBrush(_color);
                _colorSpec = Utilities.ColorSpecFromColor(_color);
                _colorText.Text = _colorName; // = Utilities.ColorNameFromColorSpec(_colorSpec);
                Tag = _colorName;
            }
        }

        public uint ColorSpec
        {
            get
            {
                return _colorSpec;
            }
            set
            {
                _colorSpec = value;

                if (_colorSpec == (uint)ColorCode.ThemeForeground)
                {
                    _color = Utilities.ColorFromColorSpec(Utilities.ColorSpecFromColor(Globals.ActiveDrawing.Theme.ForegroundColor));
                    _colorRect.Fill = new SolidColorBrush(_color);
                    _colorText.Text = _colorName = "Theme foreground";
                }
                else if (_colorSpec == (uint)ColorCode.SetColor)
                {
                    _color = Utilities.ColorFromColorSpec(_colorSpec);
                    _colorRect.Fill = new SolidColorBrush(_color);
                    _colorText.Text = _colorName = "Select a new color";
                }
                else if (_colorSpec > 10)
                {
                    _color = Utilities.ColorFromColorSpec(_colorSpec);
                    _colorRect.Fill = new SolidColorBrush(_color);
                    _colorText.Text = _colorName = Utilities.ColorNameFromColorSpec(_colorSpec);
                }
                //Tag = _colorName;
                Tag = _colorSpec;
            }
        }

        public uint ColorType { get; private set; }
    }
}
