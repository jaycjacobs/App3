using Cirros;
using Cirros.Core;
using Cirros.Utility;
using System.Collections.ObjectModel;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace CirrosUI
{
    public class ColorNameItem
    {
        string _name;
        string _lcName;
        Color _color;
        uint _colorSpec;

        public Brush Brush
        {
            get
            {
                return new SolidColorBrush(_color);
            }
        }

        public string Name { get { return _name; } }
        public string LCName { get { return _lcName; } }
        public Color Color { get { return _color; } }
        public uint ColorSpec { get { return _colorSpec; } }
        public double FontSize { get { return Globals.UIDataContext.UIFontSizeNormal;  } }

        public ColorNameItem(string name, Color color, uint colorSpec)
        {
            _name = name;
            _lcName = name.ToLower();
            _color = color;
            _colorSpec = colorSpec;
        }

        public ColorNameItem(string name, Color color)
        {
            _name = name;
            _lcName = name.ToLower();
            _color = color;
            _colorSpec = Utilities.ColorSpecFromColor(color);
        }

        public ColorNameItem(string name)
        {
            _name = name;
            _lcName = name.ToLower();

            if (StandardColors.ColorNames.ContainsKey(_lcName))
            {
                _colorSpec = StandardColors.ColorNames[_lcName];
                _color = Utilities.ColorFromColorSpec(_colorSpec);
            }
            else
            {
                _color = Colors.Black;
                _colorSpec = 0xff000000;
            }
        }
    }

    public class ColorNameItemCollection : ObservableCollection<ColorNameItem>
    {
        public ColorNameItemCollection()
        {
            Add(new ColorNameItem("Red", Colors.Red));
            Add(new ColorNameItem("Green", Colors.Green));
            Add(new ColorNameItem("Blue", Colors.Blue));
            Add(new ColorNameItem("Light Goldenrod Yellow", Colors.LightGoldenrodYellow));
            Add(new ColorNameItem("Light Steel Blue", Colors.LightSteelBlue));
            Add(new ColorNameItem("Midnight Blue", Colors.MidnightBlue));
        }
    }
}
