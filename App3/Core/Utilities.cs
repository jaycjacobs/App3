using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using RedDog;
using Microsoft.UI;


#if UWP
using Cirros.Actions;
using Cirros8;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
#else
using CirrosCore;
using System.Windows;
using System.Windows.Media;
using static CirrosCore.WpfStubs;
#endif
using System.Runtime.InteropServices.WindowsRuntime;

namespace Cirros.Utility
{
    public class Utilities
    {
        public static void DecomposeMatrix(Matrix m, out double xscale, out double yscale, out double xskew, out double yskew, out double rotation)
        {
            var delta = m.M11 * m.M22 - m.M21 * m.M12;

            if (m.M11 != 0 || m.M21 != 0)
            {
                var r = Math.Sqrt(m.M11 * m.M11 + m.M21 * m.M21);
                rotation = m.M21 > 0 ? Math.Acos(m.M11 / r) : -Math.Acos(m.M11 / r);
                xscale = r;
                yscale = delta / r;
                xskew = Math.Atan((m.M11 * m.M12 + m.M21 * m.M22) / (r * r));
                yskew = 0;
            }
            else if (m.M12 != 0 || m.M22 != 0)
            {
                var s = Math.Sqrt(m.M12 * m.M12 + m.M22 * m.M22);
                rotation = Math.PI / 2 - (m.M22 > 0 ? Math.Acos(-m.M12 / s) : -Math.Acos(m.M12 / s));
                xscale = delta / s;
                yscale = s;
                xskew = 0;
                yskew = Math.Atan((m.M11 * m.M12 + m.M21 * m.M22) / (s * s));
            }
            else
            {
                // a = b = c = d = 0
                rotation = 0;
                xscale = 1;
                yscale = 1;
                xskew = 0;
                yskew = 0;
            }
        }

        public static double GetScaleFromMatrix(Matrix m)
        {
            double scale = 1;

            Utilities.DecomposeMatrix(m, out double xscale, out double yscale, out double xskew, out double yskew, out double rotation);

            double asx = Math.Round(Math.Abs(xscale), 7);
            double asy = Math.Round(Math.Abs(yscale), 7);

            if (asx == asy)
            {
                scale = asx;
            }
            else
            {
                scale = (asx + asy) / 2;
            }
            return scale;
        }

        public static string DoubleAsRatio(double d)
        {
            string scaleString = "1:1";

            if (d != 1)
            {
                long numerator;
                long denominator;

                if (Utilities.DoubleAsFraction(d, out numerator, out denominator))
                {
                    scaleString = string.Format("{0}:{1}", numerator, denominator);
                }
                else
                {
                    scaleString = d.ToString();
                }
            }

            return scaleString;
        }

        public static bool ParseRatio(string ratio, out double value)
        {
            bool isratio = false;
            value = 1;

            long numerator = 1;
            long denominator = 1;

            string[] sa = ratio.Split(new char[] { ':' });

            if (sa.Length == 2)
            {
                if (long.TryParse(sa[0], out numerator))
                {
                    if (long.TryParse(sa[1], out denominator))
                    {
                        isratio = numerator > 0 && denominator > 0;

                        if (isratio)
                        {
                            value = (double)numerator / (double)denominator;
                        }
                    }
                }
            }

            return isratio;
        }

        public static bool DoubleAsFraction(double d, out long numerator, out long denominator)
        {
            long[] tda = new long[] {
                1024, 2310, 2370, 1287, 3570, 3390, 1482, 4380, 3390,
                6210, 3600, 4872, 6696, 9102, 11352, 13818, 16536, 21594,
                24570, 27336, 32376, 40338, 49128, 54720, 58782 };

            bool valid = false;
            numerator = 1;
            denominator = 1;

            double whole = Math.Floor(d);
            double frac = d - whole;

            if (frac == 0)
            {
                numerator = (long)whole;
                denominator = 1;
                valid = true;
            }
            else
            {
                try
                {
                    foreach (long td in tda)
                    {
                        if (TryFraction(frac, td, out numerator, out denominator))
                        {
                            numerator += (long)whole * denominator;
                            valid = true;
                            break;
                        }
                    }

                    if (valid == false)
                    {
                        double inverse = 1 / d;
                        foreach (long td in tda)
                        {
                            if (TryFraction(inverse, td, out numerator, out denominator))
                            {
                                long temp = numerator;
                                numerator = denominator;
                                denominator = temp;
                                valid = true;
                                break;
                            }
                        }
                    }

                    if (valid == false)
                    {
                        // we didn't find an exact fraction
                        // we return frac / 1000 as an approximation

                        TryFraction(frac, 1000, out numerator, out denominator);
                        numerator += (long)whole * denominator;
                        Reduce(ref numerator, ref denominator);
                    }
                }
                catch
                {
                    valid = false;
                }
            }

            return valid;
        }

        private static bool TryFraction(double d, long tryDenominator, out long numerator, out long denominator)
        {
            bool isFrac = false;

            numerator = 1;
            denominator = 1;

            double whole = Math.Floor(d);
            double frac = d - whole;

            double tryfrac = frac * tryDenominator;
            double diff = Math.Abs(tryfrac - Math.Round(tryfrac));
            if (diff < .001)
            {
                numerator = (long)Math.Round(tryfrac) + (long)whole * tryDenominator;
                denominator = tryDenominator;
                Reduce(ref numerator, ref denominator);
                isFrac = true;
            }
            else
            {
                numerator = (long)Math.Round(tryfrac) + (long)whole * tryDenominator;
                denominator = tryDenominator;
                isFrac = false;
            }

            return isFrac;
        }

        public static void Reduce(ref long numerator, ref long denominator)
        {
            if (numerator == 0)
            {
                denominator = 1;
            }
            else
            {
                long iGCD = GCD(numerator, denominator);
                numerator /= iGCD;
                denominator /= iGCD;

                if (denominator < 0)   // if -ve sign in denominator
                {
                    //pass -ve sign to numerator
                    numerator *= -1;
                    denominator *= -1;
                }
            }
        }

        private static long GCD(long iNo1, long iNo2)
        {
            if (iNo1 < 0)
            {
                iNo1 = -iNo1;
            }
            if (iNo2 < 0)
            {
                iNo2 = -iNo2;
            }

            do
            {
                if (iNo1 < iNo2)
                {
                    long tmp = iNo1;  // swap the two operands
                    iNo1 = iNo2;
                    iNo2 = tmp;
                }

                iNo1 = iNo1 % iNo2;

            }
            while (iNo1 != 0);

            return iNo2;
        }

#if UWP
        public static IAsyncAction ExecuteOnUIThread(Windows.UI.Core.DispatchedHandler action)
        {
            return Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, action);
        }
#endif
        /*
        public static T FindParent<T>(UIElement control) where T : UIElement
        {
            UIElement p = VisualTreeHelper.GetParent(control) as UIElement;
            if (p != null)
            {
                if (p is T)
                {
                    return p as T;
                }
                else
                {
                    return FindParent<T>(p);
                }
            }
            return null;
        }

        public static T FindTopParent<T>(UIElement control) where T : UIElement
        {
            UIElement e = null;

            e = FindParent<T>(control);
            while (e != null)
            {
                UIElement e0 = FindParent<T>(e);

                if (e0 == null)
                {
                    return e as T;
                }
                e = e0;
            }
            
            return null;
        }
         * */

        public static Rect EmptyRect
        {
            get
            {
#if UWP
                return RectHelper.Empty;
#else
                return Rect.Empty;
#endif
            }
        }

        public static string FormatDistance(double distance, int round, bool architect, bool showUnit, Unit unit, bool compact, int zfill = 0)
        {
            string s;

            //if (Double.IsNaN(distance) || Double.IsInfinity(distance))
            if (Double.IsNaN(distance))
            {
                distance = 0;
                Analytics.ReportEvent("Null distance in FormatDistance");
            }
            if (architect && unit == Unit.Feet)
            {
                int sign = Math.Sign(distance);
                distance = Math.Abs(distance);

                double dround = 1.0 / (double)(round * 12);
                float feet = (float)Math.Floor(distance + dround / 2);
                double fInches = (distance - feet) * 12;

                if (fInches < 0)
                {
                    fInches = 0;
                }

                float inches = (float)Math.Floor(fInches);

                if (inches == 12)
                {
                    inches = 0;
                    feet++;
                }

                double fraction = fInches - Math.Floor(inches);
                string fs = "";

                //int denominator = 64;
                int denominator = round > 0 ? round : 64;

                if (denominator > 1)
                {
                    //int numerator = (int)(fraction * denominator + (1.0 / (denominator * 2.0)));
                    int numerator = (int)Math.Round(fraction * denominator);
                    if (numerator > 0)
                    {
                        while (numerator % 2 == 0)
                        {
                            numerator /= 2;
                            denominator /= 2;
                        }

                        if (denominator == 1)
                        {
                            inches += numerator;
                            numerator = 0;
                        }

                        if (inches == 12)
                        {
                            feet += 1;
                            inches = 0;
                        }

                        if (numerator > 0)
                        {
                            fs = string.Format(" {0}/{1}", numerator, denominator);
                            //if (compact)
                            //{
                            //    fs = string.Format(" {0}/{1}", numerator, denominator);
                            //}
                            //else
                            //{
                            //    fs = string.Format("+{0}/{1}", numerator, denominator);
                            //}
                        }
                    }
                    else if (feet == 0 && inches == 0 && distance != 0)
                    {

                    }
                }

                if (feet == 0)
                {
                    if (inches == 0 && fs == "" && distance != 0)
                    {
                        string unitString = "";
                        double d = unit == Unit.Feet ? distance * 12 : distance;
                        if (showUnit)
                        {
                            unitString = UnitString(Unit.Inches);
                        }

                        d = Math.Round(d, 4);
                        s = string.Format("{0}{1}", d, unitString);
                    }
                    else if (inches == 0 && fs != "")
                    {
                        s = string.Format("{0}{1}\"", sign >= 0 ? "" : "-", fs).Trim();
                    }
                    else
                    {
                        s = string.Format("{0}0'{1}{2}\"", sign >= 0 ? "" : "-", inches, fs);
                    }
                }
                else if (compact && fs.Length == 0)
                {
                    if (inches == 0)
                    {
                        s = string.Format("{0}{1}'", sign >= 0 ? "" : "-", feet);
                    }
                    else
                    {
                        s = string.Format("{3}{0}'{1}{2}\"", feet, inches, fs, sign >= 0 ? "" : "-");
                    }
                }
                else
                {
                    s = string.Format("{0}{1}'{2}{3}\"", sign >= 0 ? "" : "-", feet, inches, fs);
                }
            }
            else
            {
                string unitString = "";

                if (showUnit)
                {
                    unitString = UnitString(unit);
                }

                if (round > 5)
                {
                    distance = Math.Round(distance, 5);
                }
                else if (round >= 0)
                {
                    distance = Math.Round(distance, round);
                }

                string[] fmt =
                {
                    "{0}{1}",
                    "{0:0.0}{1}",
                    "{0:0.00}{1}",
                    "{0:0.000}{1}",
                    "{0:0.0000}{1}",
                    "{0:0.00000}{1}"
                };
                s = string.Format(fmt[zfill % 6], distance, unitString);
            }

            return s;
        }

        public static string UnitString(Unit unit)
        {
            string unitString = "";

            switch (unit)
            {
                case Unit.Centimeters:
                    unitString = " cm";
                    break;

                case Unit.Millimeters:
                    unitString = " mm";
                    break;

                case Unit.Meters:
                    unitString = " m";
                    break;

                case Unit.Inches:
                    unitString = "\"";
                    break;

                case Unit.Feet:
                    unitString = "'";
                    break;
            }

            return unitString;
        }

#if true
        public static double ParseArchitectDistance(string input, bool isPaper = false)
        {
            string astring = input.Trim();

            string expr = @"(?<number>(\-?\d+\.\d*)|(\-?\.\d+))\""?|(?<fraction1>(?<numerator1>\d+)\/(?<denominator1>\d+)\""?)|(?<feetdinches>((?<feet>\-?\d+)'\-?)?(?<dinches>(\-?\d+\.\d*)))|(?<feetinches>((?<feet>\-?\d+)'\-?)?(?<inchesfraction>(?<inches>\d+)([\ \+](?<fraction>(?<numerator>\d+)\/(?<denominator>\d+)))?)?\""?)|(?<delimiter>[=:])|(?<integer>\-?\d+)";
            double value = 0;

            if (double.TryParse(astring, out value) == false)
            {
                MatchCollection mc = Regex.Matches(astring, expr);
                foreach (Match match in mc)
                {
                    if (match.Length > 0)
                    {
                        if (match.Groups["feetdinches"].Success)
                        {
                            string feetstring = match.Groups["feet"].Value;
                            string inchstring = match.Groups["dinches"].Value;

                            double feet = feetstring == "" ? 0.0 : double.Parse(feetstring);

                            if (double.TryParse(inchstring, out double d))
                            {
                                value = feet + d / 12;
                                break;
                            }
                        }
                        else if (match.Groups["feetinches"].Success)
                        {
                            string feetstring = match.Groups["feet"].Value;
                            string inchstring = match.Groups["inches"].Value;
                            string numeratorstring = match.Groups["numerator"].Value;
                            string denominatorstring = match.Groups["denominator"].Value;

                            int feet = feetstring == "" ? 0 : int.Parse(feetstring);
                            int inches = inchstring == "" ? 0 : int.Parse(inchstring);
                            int numerator = numeratorstring == "" ? 0 : int.Parse(numeratorstring);
                            int denominator = denominatorstring == "" ? 0 : int.Parse(denominatorstring);

                            if ((numeratorstring != "" && numerator == 0) || (denominatorstring != "" && denominator == 0))
                            {
                                // invalid fraction
                                value = double.NaN;
                                throw new Exception("Invalid syntax");
                            }
                            else
                            {
                                double fraction = denominator == 0 ? 0.0 : (double)numerator / (double)denominator;
                                double sign = astring.StartsWith("-") ? -1 : 1;
                                value = (double)feet + (((double)inches + fraction) / 12.0) * sign;
                                if (isPaper && Globals.ActiveDrawing.PaperUnit == Unit.Inches && Globals.ActiveDrawing.ModelUnit == Unit.Feet)
                                {
                                    value *= 12;
                                }
                                break;
                            }
                        }
                        if (match.Groups["fraction1"].Success)
                        {
                            string numeratorstring = match.Groups["numerator1"].Value;
                            string denominatorstring = match.Groups["denominator1"].Value;

                            int numerator = numeratorstring == "" ? 0 : int.Parse(numeratorstring);
                            int denominator = denominatorstring == "" ? 0 : int.Parse(denominatorstring);

                            if ((numeratorstring != "" && numerator == 0) || (denominatorstring != "" && denominator == 0))
                            {
                                // invalid fraction
                                value = double.NaN;
                                throw new Exception("Invalid syntax");
                            }
                            else
                            {
                                double fraction = denominator == 0 ? 0.0 : (double)numerator / (double)denominator;
                                double sign = astring.StartsWith("-") ? -1 : 1;
                                value = (fraction / 12.0) * sign;
                                if (isPaper && Globals.ActiveDrawing.PaperUnit == Unit.Inches && Globals.ActiveDrawing.ModelUnit == Unit.Feet)
                                {
                                    value *= 12;
                                }
                                break;
                            }
                        }
                        if (match.Groups["number"].Success)
                        {
                            string number = match.Groups["number"].Value;
                            if (double.TryParse(number, out value))
                            {
                                if (astring.TrimEnd().EndsWith("\"") && isPaper == false)
                                {
                                    value /= 12;
                                }
                                break;
                            }
                            //System.Diagnostics.Debug.WriteLine(value.ToString());
                        }
                        if (match.Groups["integer"].Success)
                        {
                            string number = match.Groups["integer"].Value;
                            int i;
                            if (int.TryParse(number, out i))
                            {
                                value = astring.TrimEnd().EndsWith("\"") && isPaper == false ? (double)i / 12 : (double)i;
                                break;
                            }
                            //System.Diagnostics.Debug.WriteLine(value.ToString());
                        }
                    }
                }
            }

            return value;
        }
#else
        public static double ParseArchitectDistance(string s)
        {
            int sign = 0;
            int feet = -1;
            int inches = -1;
            int numerator = -1;
            int denominator = -1;
            double value = 0;

            bool valid = false;

            try
            {
                value = double.Parse(s);
                valid = true;
            }
            catch
            {
            }

            if (!valid)
            {
                valid = true; // straw man

                s = s.Trim();

                StringBuilder sb = new StringBuilder();

                foreach (char c in s)
                {
                    if (char.IsDigit(c))
                    {
                        sb.Append(c);
                    }
                    else if (c == '\'' && feet < 0 && inches < 0)
                    {
                        feet = int.Parse(sb.ToString());
                        sb.Clear();
                    }
                    else if (c == '"')
                    {
                        if (feet < 0)
                        {
                            feet = 0;
                        }
                        if (inches < 0)
                        {
                            numerator = 0;
                            denominator = 1;
                            inches = int.Parse(sb.ToString());
                            sb.Clear();
                        }
                    }
                    else if (c == '/')
                    {
                        if (feet < 0)
                        {
                            feet = 0;
                        }
                        if (inches < 0)
                        {
                            inches = 0;
                        }

                        if (denominator >= 0 || numerator >= 0)
                        {
                            valid = false;
                            break;
                        }

                        numerator = int.Parse(sb.ToString());
                        sb.Clear();
                    }
                    else if (c == '-')
                    {
                        if (feet < 0)
                        {
                            if (sb.Length == 0)
                            {
                                // Dash before feet means negative value
                                sign = -1;
                            }
                            else
                            {
                                // Missing ' sign
                                valid = false;
                                break;
                            }
                        }
                        else if (inches < 0)
                        {
                            // Dash after feet and before inches is noise - ignore (could be more than one - oh well)
                        }
                        else
                        {
                            // Inappropriate dash
                            valid = false;
                            break;
                        }
                    }
                    else if (c == ' ' && inches < 0)
                    {
                        // Space is only ok between inches and fraction
                        inches = int.Parse(sb.ToString());
                        sb.Clear();
                    }
                    else if (c == '.' && feet < 0 && inches < 0)
                    {
                        // If we have a simple decimal value with followed by " or ' or nothing, we'll accept that
                        if (s.EndsWith("\""))
                        {
                            feet = 0;
                            s = s.Remove(s.Length - 1);
                        }
                        else if (s.EndsWith("'"))
                        {
                            s = s.Remove(s.Length - 1);
                        }

                        try
                        {
                            double f = double.Parse(s);

                            if (feet == 0)
                            {
                                value = f / 12;
                            }
                            else
                            {
                                value = f;
                            }
                        }
                        catch
                        {
                            valid = false;
                            break;
                        }
                        return value;
                    }
                    else
                    {
                        // Invalid character
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    if (sb.Length > 0)
                    {
                        int v = int.Parse(sb.ToString());

                        if (feet < 0)
                        {
                            feet = v;
                            inches = 0;
                            numerator = 0;
                            denominator = 1;
                        }
                        else if (inches < 0)
                        {
                            inches = v;
                            numerator = 0;
                            denominator = 1;
                        }
                        else if (numerator < 0)
                        {
                            // No denominator
                            valid = false;
                        }
                        else if (denominator < 0)
                        {
                            denominator = v;
                        }
                        else
                        {
                            // Extra digits after the " sign
                            valid = false;
                        }
                    }
                    else if (inches < 0)
                    {
                        inches = 0;
                        numerator = 0;
                        denominator = 1;
                    }
                    else if (numerator < 0)
                    {
                        numerator = 0;
                        denominator = 1;
                    }

                    if (feet >= 0 && inches >= 0 && numerator >= 0 && denominator > 0)
                    {
                        //inches += (double)numerator / (double)denominator;
                        value = feet + (double)(inches + (double)numerator / (double)denominator) / 12;

                        if (sign < 0)
                        {
                            value = -value;
                        }
                    }
                    else
                    {
                        valid = false;
                    }
                }
            }

            if (!valid)
            {
                throw new Exception("Invalid syntax");
            }

            return value;
        }
#endif
        public static float Round(double value, double round)
        {
            return round > 0 ? (float)Math.Floor(value / round + round / 2) : (float)value;
        }

        public static uint ColorSpecFromARGB(int a, int r, int g, int b)
        {
            return (uint)(a * 16777216 + r * 65536 + g * 256 + b);
        }

        public static uint ColorSpecFromColor(Color c)
        {
            return (uint)(c.A * 16777216 + c.R * 65536 + c.G * 256 + c.B);
        }

        public static uint ColorSpecFromColorName(string colorName)
        {
            uint cspec = 1;

            if (colorName.StartsWith("#"))
            {
                if (colorName.Length > 9)
                {
                    // error - too long
                    // this should be caught in _nameBox_KeyDown
                    colorName = colorName.Substring(0, 9);
                }
                else
                {
                    string s = colorName.TrimEnd().Substring(1);
                    string h = "00000000";

                    if (s.Length == 8)
                    {
                        h = s;
                    }
                    else if (s.Length > 0)
                    {
                        h = string.Concat(s, "00000000".Substring(0, 8 - s.Length));
                    }

                    int result;

                    if (int.TryParse(h, NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo, out result))
                    {
                        var color = new Color();
                        color.A = byte.Parse(h.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                        color.R = byte.Parse(h.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                        color.G = byte.Parse(h.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                        color.B = byte.Parse(h.Substring(6, 2), NumberStyles.AllowHexSpecifier);

                        cspec = Utilities.ColorSpecFromColor(color);
                    }
                }
            }
            else if (colorName.Length > 0 && char.IsDigit(colorName[0]))
            {
                int value;
                int p = colorName.IndexOf(' ');
                if (p > 0)
                {
                    colorName = colorName.Substring(0, p);
                }
                if (int.TryParse(colorName, out value) && value <= 255)
                {
                    cspec = Utilities.ColorSpecFromAutoCadColor(value);
                }
            }
            else
            {
                string s = colorName.ToLower();
                if (StandardColors.ColorNames.ContainsKey(s))
                {
                    cspec = StandardColors.ColorNames[s];
                }
            }

            return cspec;
        }

        public static Color ColorFromColorSpec(uint colorSpec)
        {
            if (colorSpec == (uint)ColorCode.ThemeForeground)
            {
                return Globals.ActiveDrawing.Theme.ForegroundColor;
            }

            uint uspec = (uint)colorSpec;
            byte a = (byte)(uspec / 16777216);
            byte r = (byte)(uspec / 65536);
            byte g = (byte)(uspec / 256);
            byte b = (byte)uspec;
#if UWP
            return ColorHelper.FromArgb(a, r, g, b);
#else
            return Color.FromArgb(a, r, g, b);
#endif
        }

        public static string ColorNameFromColorSpec(uint colorspec)
        {
            string colorName = "";
#if KT22
            switch (colorspec)
            {
                case (uint)ColorCode.ByLayer:
                    colorName = RedDogGlobals.GS_Layer;
                    break;

                case (uint)ColorCode.NoFill:
                    colorName = RedDogGlobals.GS_None;
                    break;

                case (uint)ColorCode.SameAsOutline:
                    colorName = RedDogGlobals.GS_Outline;
                    break;

                default:
                    if (StandardColors.Colors.ContainsKey(colorspec))
                    {
                        colorName = StandardColors.Colors[colorspec];
                    }
                    else if (StandardColors.AcadColorSpecTable.Contains(colorspec))
                    {
                        int index = StandardColors.AcadColorSpecTable.FindIndex(new Predicate<uint>((uint x) => x == colorspec));
                        colorName = string.Format("{0} (acad)", index);
                    }
                    else
                    {
                        colorName = string.Format("#{0:X8}", colorspec);
                    }
                    break;
            }
#else
#if UWP
            if (StandardColors.Colors.ContainsKey(colorspec))
            {
                colorName = StandardColors.Colors[colorspec];
            }
            else if (StandardColors.AcadColorSpecTable.Contains(colorspec))
            {
                int index = StandardColors.AcadColorSpecTable.FindIndex(new Predicate<uint>((uint x) => x == colorspec));
                colorName = string.Format("{0} (acad)", index);
            }
            else
            {
                colorName = string.Format("#{0:X8}", colorspec);
            }
#endif
#endif

            return colorName;
        }

        public static uint ColorSpecFromAutoCadColor(int acadColor)
        {
            uint colorSpec;

            if (acadColor < 0)
            {
                colorSpec = (uint)ColorCode.ByLayer;
            }
            else if (acadColor >= 256)
            {
                colorSpec = (uint)ColorCode.ByLayer;
            }
            else if (acadColor == 0)
            {
                colorSpec = StandardColors.AcadColorSpecTable[acadColor];
            }
            else
            {
                colorSpec = StandardColors.AcadColorSpecTable[acadColor];
            }

            return colorSpec;
        }

        public static Color ColorFromARGB(byte a, byte r, byte g, byte b)
        {
#if UWP
            return ColorHelper.FromArgb(a, r, g, b);
#else
            return Color.FromArgb(a, r, g, b);
#endif
        }

        public static Point TransformPoint(GeneralTransform t, Point p)
        {
#if UWP
            return t.TransformPoint(p);
#else
            return t.Transform(p);
#endif
        }

        public static Unit UnitFromString(string s)
        {
            Unit unit;

            if (s == Unit.Inches.ToString())
            {
                unit = Unit.Inches;
            }
            else if (s == Unit.Feet.ToString())
            {
                unit = Unit.Feet;
            }
            else if (s == Unit.Millimeters.ToString())
            {
                unit = Unit.Millimeters;
            }
            else if (s == Unit.Meters.ToString())
            {
                unit = Unit.Meters;
            }
            else
            {
                unit = Unit.Centimeters;
            }
            return unit;
        }

        public static Primitive GetTopLevelPrimitive(Primitive p)
        {
            Primitive top = p;

            if (top.IsInstanceMember)
            {
                while (top.Parent is PInstance)
                {
                    top = top.Parent as PInstance;
                }
            }

            return top;
        }

        public static List<Primitive> UnGroup(PInstance p, bool undoable)
        {
            List<Primitive> list = new List<Primitive>();

            Primitive top = Utilities.GetTopLevelPrimitive(p);
            PInstance instance = top as PInstance;
            List<Primitive> gleamList = new List<Primitive>();

            if (instance != null && instance.IsInstanceMember == false)
            {
                Cirros.Primitives.Group group = Globals.ActiveDrawing.GetGroup(instance.GroupName);
                if (group != null)
                {
                    foreach (Primitive member in group.Items)
                    {
                        Primitive copy = member.Clone();
                        if (top.Matrix.IsIdentity)
                        {
                            copy.MoveByDelta(top.Origin.X, top.Origin.Y);
                        }
                        else
                        {
                            Point t = top.Matrix.Transform(member.Origin);
#if UWP
                            Matrix m = CGeometry.MultiplyMatrix(copy.Matrix, top.Matrix);
#else
                            Matrix m = Matrix.Multiply(copy.Matrix, top.Matrix);
#endif
                            copy.SetTransform(t.X, t.Y, m);
                            copy.MoveByDelta(top.Origin.X, top.Origin.Y);
                        }

                        if (copy.LayerId == 0)
                        {
                            copy.LayerId = instance.LayerId;
                        }

                        if (copy.ColorSpec == (uint)ColorCode.ThemeForeground)
                        {
                            copy.ColorSpec = instance.ColorSpec;
                        }

                        if (copy is PText)
                        {
                            PText pt = copy as PText;
                            GroupAttribute ga0 = PText.ResolveAttributeString(pt.Text);

                            if (pt.AttributeName != null)
                            {
                                foreach (GroupAttribute ga in instance.AttributeList)
                                {
                                    if (ga.Prompt == pt.AttributeName && ga.Value != ga0.Value)
                                    {
                                        if (ga.Value.Length > 0)
                                        {
                                            pt.Text = ga.Value;
                                        }
                                        break;
                                    }
                                }
                            }
                        }

                        copy.ZIndex = Globals.ActiveDrawing.MaxZIndex;

                        copy.AddToContainer(Globals.ActiveDrawing);
                        list.Add(copy);

                        if (undoable)
                        {
                            gleamList.Add(copy);
#if UWP
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, copy);
#else
#endif
                        }
                    }
                }

#if UWP
                if (undoable)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, top);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, group.Items.Count + 1);

                    if (gleamList.Count > 0)
                    {
                        Gleam gleam = new Gleam(gleamList);
                        gleam.Start();
                    }
                }
#else
#endif

                Globals.ActiveDrawing.DeletePrimitive(top);
            }

            return list;
        }

        public static PInstance MakeUniqueInstance(PInstance original, bool undoable)
        {
#if UWP
            if (undoable)
            {
                Globals.CommandDispatcher.AddUndoableAction(ActionID.RestorePrimitive, original);
            }
#else
#endif

            Cirros.Primitives.Group parent = Globals.ActiveDrawing.GetGroup(original.GroupName);

            List<Primitive> members = UnGroup(original, false);

            Cirros.Primitives.Group group = new Cirros.Primitives.Group(Globals.ActiveDrawing.UniqueGroupName(original.GroupName));
            group.PaperUnit = parent.PaperUnit;
            group.ModelUnit = parent.ModelUnit;
            group.ModelScale = parent.PreferredScale;
            group.PreferPaperSpace = parent.PreferPaperSpace;
            group.MovePrimitivesFromDrawing(original.Origin.X, original.Origin.Y, members);
            group.InsertLocation = parent.InsertLocation;
            group.Description = parent.Description;
            group.CoordinateSpace = parent.CoordinateSpace;

            //group.IncludeInLibrary = false;

            Matrix ivm = CGeometry.InvertMatrix(original.Matrix);
            group.Entry = ivm.Transform(parent.Entry);
            group.Exit = ivm.Transform(parent.Exit);

            Globals.ActiveDrawing.AddGroup(group);

            PInstance unique = new PInstance(original.Origin, group.Name);
            unique.LayerId = original.LayerId;
            unique.ColorSpec = original.ColorSpec;
            unique.AddToContainer(Globals.ActiveDrawing);

#if UWP
            Gleam gleam = new Gleam(new List<Primitive>() { unique });
            gleam.Start();

            if (undoable)
            {
                Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, unique);
                //Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, group.Items.Count + 1);
                Globals.CommandDispatcher.AddUndoableAction(ActionID.MultiUndo, 2);
            }
#else
#endif

            return unique;
        }

        public static DoubleCollection NormalizeStrokeDashArray(Point a, Point b, DoubleCollection dc)
        {
            double distance = Construct.Distance(a, b);
            return NormalizeStrokeDashArray(distance, dc);
        }

        public static DoubleCollection NormalizeStrokeDashArray(double distance, DoubleCollection dc)
        {
            if (dc == null)
            {
                return null;
            }

            DoubleCollection normalized = new DoubleCollection();

            double patternSize = 0;

            foreach (double d in dc)
            {
                patternSize += d;
            }

            if (distance < patternSize)
            {
                double scale = distance / patternSize;
                foreach (double d in dc)
                {
                    normalized.Add(d * scale);
                }
            }
            else
            {
                double nd = Math.Floor(distance / patternSize) * patternSize;
                double scale = distance / nd;
                foreach (double d in dc)
                {
                    normalized.Add(d * scale);
                }
            }

            return normalized;
        }

        public static Rect AdjustRectForAspect(Rect source, Rect destination, Stretch stretch)
        {
            Rect result = Rect.Empty;

            if (source != Rect.Empty)
            {
                if (stretch == Stretch.Fill)
                {
                    result = destination;
                }
                else if (stretch == Stretch.None)
                {
                    result = source;
                }
                else
                {
                    result = destination;

                    double sourceAspect = source.Width / source.Height;
                    double xs = destination.Width / source.Width;
                    double ys = destination.Height / source.Height;

                    if (xs != ys)
                    {
                        if (stretch == Stretch.Uniform)
                        {
                            if (xs > ys)
                            {
                                result.Width = source.Width * ys;
                                result.X += (destination.Width - result.Width) / 2;
                            }
                            else
                            {
                                result.Height = source.Height * xs;
                                result.Y += (destination.Height - result.Height) / 2;
                            }
                        }
                        else if (stretch == Stretch.UniformToFill)
                        {
                            if (xs < ys)
                            {
                                result.Width = source.Width * ys;
                                result.X += (destination.Width - result.Width) / 2;
                            }
                            else
                            {
                                result.Height = source.Height * xs;
                                result.Y += (destination.Height - result.Height) / 2;
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static string FixBase64ForImage(string Image)
        {
            System.Text.StringBuilder sbText = new System.Text.StringBuilder(Image, Image.Length);

            sbText.Replace("\r", String.Empty);
            sbText.Replace("\n", String.Empty);
            sbText.Replace(" ", String.Empty);

            return sbText.ToString();
        }

#if UWP
        public static async Task<string> EncodeImage(StorageFile imageFile)
        {
            StringBuilder sb = new StringBuilder();
            string contentType = null;

            try
            {
                string filetype = imageFile.FileType.ToLower();

                if (filetype == ".png")
                {
                    contentType = "image/png";
                }
                else if (filetype == ".jpg" || filetype == ".jpeg")
                {
                    contentType = "image/jpg";
                }

                sb.AppendFormat("data:{0};base64,", contentType);

                if (contentType != null)
                {
                    using (IRandomAccessStreamWithContentType stream = await imageFile.OpenReadAsync())
                    {
                        byte[] fileBytes = new byte[stream.Size];
                        using (DataReader reader = new DataReader(stream))
                        {
                            byte[] bytes = new byte[1000];

                            await reader.LoadAsync((uint)stream.Size);
                            reader.ReadBytes(fileBytes);

                            sb.Append(Convert.ToBase64String(fileBytes));
                        }
                    }
                }
            }
            catch
            {

            }

            return sb.ToString();
        }

        public static string GetImageSourcePath(string imageId)
        {
            string path = null;

            Guid guid;

            if (Guid.TryParse(imageId, out guid))
            {
                // ImageId is a guid. Add extension and get file from temporary folder
                path = Globals.TemporaryImageFolder.Path + @"\" + imageId + ".jpg";
            }
            else if (imageId.EndsWith(".png") || imageId.EndsWith(".jpg") || imageId.EndsWith(".jpeg"))
            {
                // imageId is a filename in the temporary folder
                path = Globals.TemporaryImageFolder.Path + @"\" + imageId;
            }
            else
            {
                // invalid
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
            }

            return path;
        }

        public static async Task<StorageFile> GetImageSourceFileAsync(string imageId)
        {
            StorageFile file = null;

            try
            {
                Guid guid;

                if (Guid.TryParse(imageId, out guid))
                {
                    // ImageId is a guid. Add extension and get file from temporary folder
                    file = await Globals.TemporaryImageFolder.GetFileAsync(imageId + ".jpg");
                }
                else if (imageId.EndsWith(".png") || imageId.EndsWith(".jpg") || imageId.EndsWith(".jpeg"))
                {
                    // imageId is a filename in the temporary folder
                    file = await Globals.TemporaryImageFolder.GetFileAsync(imageId);
                }
                else 
                {
                    // invalid
#if DEBUG
                    System.Diagnostics.Debugger.Break();
#endif
                }
            }
            catch
            {
                // this is normal - the cached image in the temporary folder has probably aged out
            }

            return file;
        }

        public static async Task<StorageFile> CreateTemporaryImageFromUriAsync(string imageId, StorageFolder folder, string imageUri)
        {
            StorageFile file = null;
            string tempFileName = null;

            try
            {
                if (imageUri.StartsWith("http://"))
                {
                    // path is a URL
                    if (imageUri.EndsWith(".png"))
                    {
                        tempFileName = imageId + ".png";
                    }
                    else if (imageUri.EndsWith(".jpg") || imageUri.EndsWith(".jpeg"))
                    {
                        tempFileName = imageId + ".jpg";
                    }
                    else
                    {
                        throw new Exception("web image file type is not supported");
                    }

                    var httpClient = new HttpClient();
                    var contentBytes = await httpClient.GetByteArrayAsync(imageUri);

                    file = await folder.CreateFileAsync(tempFileName, CreationCollisionOption.ReplaceExisting);
                    using (Stream stream = await file.OpenStreamForWriteAsync())
                    {
                        await stream.WriteAsync(contentBytes, 0, contentBytes.Length);
                        await stream.FlushAsync();
                        stream.Dispose();
                    }
                }
                else if (imageUri.StartsWith("data:"))
                {
                    if ((imageUri.Contains("image/jpg") || imageUri.Contains("image/jpeg") || imageUri.Contains("image/png")) && imageUri.Contains("base64"))
                    {
                        int sep = imageUri.IndexOf(",");
                        if (sep > 0)
                        {
                            // path is a base64 stream
                            Byte[] contentBytes = Convert.FromBase64String(FixBase64ForImage(imageUri.Substring(sep + 1)));

                            if (imageUri.Contains("image/png"))
                            {
                                tempFileName = imageId + ".png";
                            }
                            else
                            {
                                tempFileName = imageId + ".jpg";
                            }

                            file = await folder.CreateFileAsync(tempFileName, CreationCollisionOption.ReplaceExisting);
                            using (Stream stream = await file.OpenStreamForWriteAsync())
                            {
                                await stream.WriteAsync(contentBytes, 0, contentBytes.Length);
                                await stream.FlushAsync();
                                stream.Dispose();
                            }
                        }
                    }
                }

                if (tempFileName.EndsWith(".png"))
                {
                    file = await TemporaryJpegFromPng(file);
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError("CreateTemporaryImageFromUri: ", ex, 1, 410);
            }

            return file;
        }

        public static async Task AddImageRefFromImage(PImage image, List<ImageRef> imageRefs)
        {
            if (image != null)
            {
                foreach (ImageRef imageRef in imageRefs)
                {
                    if (imageRef.ImageId == image.ImageId)
                    {
                        return;
                    }
                }

                try
                {
                    StorageFile file = await Utilities.GetImageSourceFileAsync(image.ImageId);

                    if (file != null)
                    {
                        ImageRef ir = new ImageRef();
                        ir.ImageId = image.ImageId;
                        ir.Name = image.SourceName;
                        ir.Contents = await Utilities.EncodeImage(file);

                        imageRefs.Add(ir);
                    }
                }
                catch
                {
                    // image is missing - don't crash
                }
            }
        }

        public static async Task AddImageRefsFromGroup(Primitives.Group g, List<ImageRef> imageRefs)
        {
            foreach (Primitive p in g.Items)
            {
                if (p is PImage)
                {
                    await Utilities.AddImageRefFromImage(p as PImage, imageRefs);
                }
                else if (p is PInstance instance)
                {
                    //TODO
                    Primitives.Group gm = Globals.ActiveDrawing.GetGroup(instance.GroupName);
                    if (gm != null)
                    {
                        await Utilities.AddImageRefsFromGroup(gm, imageRefs);
                    }
                }
            }
        }

        public static async Task<StorageFile> ResizeJpegFile(StorageFile jpegFile, uint maxSize)
        {
            if (jpegFile.Name.EndsWith(".jpg"))
            {
                using (IRandomAccessStream fileStream = await jpegFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                    PixelDataProvider pdp = await decoder.GetPixelDataAsync();
                    fileStream.Dispose();

                    if (decoder.PixelWidth > maxSize || decoder.PixelHeight > maxSize)
                    {
                        using (IRandomAccessStream outStream = await jpegFile.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outStream);
                            encoder.SetPixelData(decoder.BitmapPixelFormat, BitmapAlphaMode.Ignore,
                                decoder.PixelWidth, decoder.PixelHeight,
                                decoder.DpiX, decoder.DpiY,
                                pdp.DetachPixelData());

                            if (decoder.PixelWidth > decoder.PixelHeight)
                            {
                                encoder.BitmapTransform.ScaledWidth = maxSize;
                                encoder.BitmapTransform.ScaledHeight = (maxSize * decoder.PixelHeight) / decoder.PixelWidth;
                            }
                            else
                            {
                                encoder.BitmapTransform.ScaledHeight = maxSize;
                                encoder.BitmapTransform.ScaledHeight = (maxSize * decoder.PixelWidth) / decoder.PixelHeight;
                            }

                            await encoder.FlushAsync();
                            outStream.Dispose();
                        }
                    }
                }
            }

            return jpegFile;
        }

        public static async Task<StorageFile> TemporaryJpegFromPng(StorageFile pngFile)
        {
            StorageFile jpegFile = null;

            if (pngFile.Name.EndsWith(".png"))
            {
                string jpegName = pngFile.Name.Replace(".png", ".jpg");
                jpegFile = await Globals.TemporaryImageFolder.CreateFileAsync(jpegName, CreationCollisionOption.ReplaceExisting);
                using (IRandomAccessStream outStream = await jpegFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    ImageProperties imageProperties = await pngFile.Properties.GetImagePropertiesAsync();

                    using (IRandomAccessStream fileStream = await pngFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                        PixelDataProvider pdf = await decoder.GetPixelDataAsync();

                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outStream);
                        encoder.SetPixelData(decoder.BitmapPixelFormat, BitmapAlphaMode.Ignore,
                            decoder.PixelWidth, decoder.PixelHeight,
                            decoder.DpiX, decoder.DpiY,
                            pdf.DetachPixelData());

                        await encoder.FlushAsync();
                        outStream.Dispose();
                    }
                }
            }

            return jpegFile;
        }

        public static async Task<StorageFile> TemporaryJpegFromFile(StorageFile file, string imageId)
        {
            StorageFile jpegFile = null;

            string jpegName = imageId + ".jpg";

            using (IRandomAccessStream inputStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                bool success = false;

                try
                {
                    ImageProperties imageProperties = await file.Properties.GetImagePropertiesAsync();

                    WriteableBitmap _originalBitmap = new WriteableBitmap((int)imageProperties.Width, (int)imageProperties.Height);

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(inputStream);
                    BitmapTransform transform;

                    bool rotate = decoder.OrientedPixelWidth != decoder.PixelWidth && decoder.OrientedPixelWidth == decoder.PixelHeight;

                    if (rotate)
                    {
                        transform = new BitmapTransform()
                        {
                            ScaledHeight = Convert.ToUInt32(_originalBitmap.PixelWidth),
                            ScaledWidth = Convert.ToUInt32(_originalBitmap.PixelHeight)
                        };
                    }
                    else
                    {
                        transform = new BitmapTransform()
                        {
                            ScaledWidth = Convert.ToUInt32(_originalBitmap.PixelWidth),
                            ScaledHeight = Convert.ToUInt32(_originalBitmap.PixelHeight)
                        };
                    }

                    PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,    // WriteableBitmap uses BGRA format
                        BitmapAlphaMode.Straight,
                        transform,
                        ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.DoNotColorManage);

                    // An array containing the decoded image data, which could be modified before being displayed
                    byte[] sourcePixels = pixelData.DetachPixelData();

                    // Open a stream to copy the image contents to the WriteableBitmap's pixel buffer
                    using (Stream stream = _originalBitmap.PixelBuffer.AsStream())
                    {
                        await stream.WriteAsync(sourcePixels, 0, sourcePixels.Length);
                    }

                    _originalBitmap.Invalidate();

                    file = await Globals.TemporaryImageFolder.CreateFileAsync(jpegName, CreationCollisionOption.ReplaceExisting);
#if SIBERIA
                    using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.ReadWrite), memStream = new InMemoryRandomAccessStream())
                    {
                        await _originalBitmap.ToStreamAsJpeg(fileStream);
                    }
#endif
                    success = true;
                    jpegFile = file;
                }
                catch (Exception ex)
                {
                    Analytics.ReportError("Error reading image file", ex, 2, 702);
                }

                if (success == false)
                {
                    await Cirros.Alerts.StandardAlerts.IOError();
                }
            }

            return jpegFile;
        }

        public static async Task<BitmapImage> GetImageFromImageIdAsync(string imageId, Size size)
        {
            BitmapImage bitmapImage = null;
            StorageFile file = null;

            try
            {
                Guid guid;

                if (Guid.TryParse(imageId, out guid))
                {
                    // ImageId is a guid. Add extension and get file from temporary folder
                    file = await Globals.TemporaryImageFolder.GetFileAsync(imageId + ".jpg");
                }
                else if (imageId.EndsWith(".png") || imageId.EndsWith(".jpg") || imageId.EndsWith(".jpeg"))
                {
                    // imageId is a filename in the temporary folder
                    file = await Globals.TemporaryImageFolder.GetFileAsync(imageId);
                }
                else
                {
                    // invalid
#if DEBUG
                    System.Diagnostics.Debugger.Break();
#endif
                    // imageId is an MRU token
                    file = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(imageId);
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError(string.Format("Image is missing: {0}", imageId), ex, 1, 411);
            }

            if (bitmapImage == null)
            {
                try
                {
                    if (file == null)
                    {
                        var uri = new System.Uri("ms-appx:///Assets/missing.png");
                        file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
                    }

                    if (file != null)
                    {
                        using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                        {
                            bitmapImage = new BitmapImage();
                            if (!size.IsEmpty)
                            {
                                bitmapImage.DecodePixelHeight = (int)size.Height;
                                bitmapImage.DecodePixelWidth = (int)size.Width;
                            }

                            await bitmapImage.SetSourceAsync(fileStream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Analytics.ReportError(string.Format("Image failed to load: {0}", imageId), ex, 1, 412);
                }
            }

            return bitmapImage;
        }

        public static async Task<Size> GetImageSizeAsync(string imageId)
        {
            Size size = Size.Empty;
            StorageFile file = null;

            Guid guid;

            if (Guid.TryParse(imageId, out guid))
            {
                // ImageId is a guid. Add extension and get file from temporary folder
                file = await Globals.TemporaryImageFolder.GetFileAsync(imageId + ".jpg");
            }
            else if (imageId.EndsWith(".png") || imageId.EndsWith(".jpg") || imageId.EndsWith(".jpeg"))
            {
                // imageId is a filename in the temporary folder
                file = await Globals.TemporaryImageFolder.GetFileAsync(imageId);
            }

            if (file != null)
            {
                ImageProperties imageProperties = await file.Properties.GetImagePropertiesAsync();

                size.Width = imageProperties.Width;
                size.Height = imageProperties.Height;
            }

            return size;
        }

        public static async Task PurgeTemporaryImages(TimeSpan age)
        {
            DateTime purgeDate = DateTime.Now - age;

            try
            {
                var queryOptions = new QueryOptions();

                var query = Globals.TemporaryImageFolder.CreateFileQueryWithOptions(queryOptions);
                IReadOnlyList<StorageFile> fileList = await query.GetFilesAsync();
         
                foreach (StorageFile file in fileList)
                {
                    if (file.DateCreated < purgeDate)
                    {
                        await file.DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Analytics.ReportError("Error purging temporary images", ex, 2, 413);
            }
        }
#else
#endif

        private static Dictionary<long, Dictionary<string, long>> __sizeChangedTracker = new Dictionary<long, Dictionary<string, long>>();

        public static Dictionary<long, Dictionary<string, long>> sizeChangedTracker
        {
            get
            {
                return __sizeChangedTracker;
            }
        }

#if UWP
        public static bool __checkSizeChanged(long identifier, object sender, bool abort = false)
        {
            if (__sizeChangedTracker.ContainsKey(identifier))
            {
                Dictionary<string, long> d = __sizeChangedTracker[identifier];
                long count = ++d["count"];
                //System.Diagnostics.Debug.WriteLine("__checkSizeChanged: identifier: {0}; count: {1};", identifier, count);
                if (count > 20)
                {
                    DateTime then = DateTime.FromBinary(d["time"]);
                    if ((DateTime.Now - then) < new TimeSpan(0, 0, 2))
                    {
                        Analytics.ReportEvent(string.Format("Possible_layout_cycle_{0}", identifier));
                        return abort; // return true to break out of the possible cycle
                    }

                    d["time"] = DateTime.Now.ToBinary();
                    d["count"] = 0;
                }
                else if (count == 1)
                {
                    d["time"] = DateTime.Now.ToBinary();
                }
            }
            else
            {
                Dictionary<string, long> d = new Dictionary<string, long>
                {
                    { "time", DateTime.Now.ToBinary() },
                    { "count", 0 }
                };
                __sizeChangedTracker.Add(identifier, d);
                //System.Diagnostics.Debug.WriteLine("__checkSizeChanged: identifier: {0}; count: {1};", identifier, 0);
            }

            return false;
        }
#else
#endif

        public static Point CoordinateToPoint(CoordinateMode mode, double d1, double d2)
        {
            Point p = new Point();

            if (mode == CoordinateMode.Absolute)
            {
                p = Globals.ActiveDrawing.ModelToPaper(new Point(d1, d2));
            }
            else if (mode == CoordinateMode.Delta)
            {
                Point delta = Globals.ActiveDrawing.ModelToPaperDelta(new Point(d1, d2));

#if UWP
                if (Globals.CommandProcessor != null)
                {
                    Point current = Globals.Input.CursorLocation;
                    Point f = Globals.CommandProcessor.Anchor;
                    p = new Point(f.X + delta.X, f.Y + delta.Y);
                }
                else
                {
                    Point current = Globals.Input.CursorLocation;
                    p = new Point(current.X + delta.X, current.Y + delta.Y);
                }
#else
                throw new Exception("Unimplemented");
#endif
            }
            else if (mode == CoordinateMode.Polar)
            {
                double d = Globals.ActiveDrawing.ModelToPaper(d1);
                Point delta = Construct.PolarOffset(new Point(0, 0), d, -d2 / Construct.cRadiansToDegrees);

#if UWP
                if (Globals.CommandProcessor != null)
                {
                    Point f = Globals.CommandProcessor.Anchor;
                    p = new Point(f.X + delta.X, f.Y + delta.Y);
                }
                else
                {
                    Point current = Globals.Input.CursorLocation;
                    p = new Point(current.X + delta.X, current.Y + delta.Y);
                }
#else
                throw new Exception("Unimplemented");
#endif
            }

            return p;
        }
    }
}
