using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UWP
using Windows.UI;
#else
using System.Windows.Media;
#endif

namespace Cirros.Core
{
    public class Theme
    {
        public Theme()
        {
        }

        public Theme(string name, Color background, Color foreground, Color border, Color grid, Color cursor, Color itembox, Color handle, Color handlefill, Color highlight, Color rubberband)
        {
            _name = name;

            BackgroundColor = background;
            ForegroundColor = foreground;
            BorderColor = border;
            GridColor = grid;
            CursorColor = cursor;
            HandleColor = handle;
            HandleFillColor = handlefill;
        }

        public Theme(string name, Color background, Color foreground, Color border, Color grid, Color cursor, Color handle, Color handlefill)
        {
            _name = name;

            BackgroundColor = background;
            ForegroundColor = foreground;
            BorderColor = border;
            GridColor = grid;
            CursorColor = cursor;
            HandleColor = handle;
            HandleFillColor = handlefill;
        }

        private string _name;
        public Color BackgroundColor = Colors.White;
        public Color ForegroundColor = Colors.Black;
        public Color BorderColor = Colors.Gray;
        public Color GridColor = Colors.Gray;
        public Color CursorColor = Colors.Black;
        public Color ItemBoxColor = Colors.Blue;
        public Color HandleColor = Colors.Black;
        public Color HandleFillColor = Colors.LightGray;
        public Color HighlightColor = Colors.Red;
        //public Color RubberBandColor = Colors.Black;

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
