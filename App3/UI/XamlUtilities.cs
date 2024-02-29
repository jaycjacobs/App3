using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Cirros
{
    public class XamlUtilities
    {
        public static Rect GetElementRect(FrameworkElement element, UIElement visual)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(visual);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        public static double StringWidth(string s, string font, double size)
        {
            double sw = 0;

            char[] cArray = s.ToCharArray();
            string name = font.ToLower();

            double scale = size > 10 ? size : 18 / size;

            Size test = new Size(10000, 10000);

            TextBlock tb = new TextBlock();
            tb.FontSize = size * scale;

            try
            {
                tb.FontFamily = new FontFamily(font);

                tb.Text = s;

                if (tb.ActualHeight == 0)
                {
                    tb.Measure(test);
                }

                if (tb.ActualHeight > 0)
                {
                    sw = tb.ActualWidth / scale;
                }
            }
            catch
            {
                sw = cArray.Length * 600 * size / 1000;
            }

            return sw;
        }
    }
}
