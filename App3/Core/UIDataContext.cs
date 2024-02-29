using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Cirros.Core
{
    public class UIDataContext : INotifyPropertyChanged
    {
        double _fontSizeExtraSmall = 12;
        double _fontSizeSmall = 14;
        double _fontSizeNormal = 16;
        double _fontSizeLarge = 18;
        double _fontSizeTitle = 24;

        double _ff = .9375;
        private double _uiScale;

        public UIDataContext()
        {
        }

        DispatcherTimer _uiScaleTimer = new DispatcherTimer();

        public double UIScale
        {
            get { return _uiScale; }
            set
            {
                try
                {
                    if (_uiScale != value && value > .5)
                    {
                        _uiScale = value;

                        _uiScaleTimer.Stop();
                        _uiScaleTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
                        _uiScaleTimer.Tick += (tsender, te) => {

                            _uiScaleTimer.Stop();

                            NotifyPropertyChanged();
                            Globals.Events.UIScaleChanged();
                        };
                        _uiScaleTimer.Start();
                    }
                }
                catch (Exception ex)
                {
                    Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "command", "edit" },
                        { "method", "set UIScale" },
                        { "scale", value.ToString() },
                    }, 409);
                }
            }
        }

        public int MinFontSize
        {
            get { return 9; }
        }

        public int MaxFontSize
        {
            get { return 20; }
        }

        public int Size
        {
            get { return (int)Math.Round(_fontSizeNormal); }
            set
            {
                try
                {
                    var tb = new TextBlock { Text = "Text", FontSize = value };
                    tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    double ff = value / tb.DesiredSize.Height;
                    if (ff != .8)
                    {
                        _ff = ff / .8;
                    }
                }
                catch
                {
                    _ff = 1;
                }

                if (_fontSizeNormal != value)
                {
                    _fontSizeNormal = value;

                    switch (_fontSizeNormal)
                    {
                        case 9:
                            _fontSizeExtraSmall = 8;
                            _fontSizeSmall = 8;
                            _fontSizeLarge = 11;
                            _fontSizeTitle = 16;
                            break;

                        case 10:
                            _fontSizeExtraSmall = 8;
                            _fontSizeSmall = 9;
                            _fontSizeLarge = 12;
                            _fontSizeTitle = 14;
                            break;

                        case 11:
                            _fontSizeExtraSmall = 8;
                            _fontSizeSmall = 10;
                            _fontSizeLarge = 13;
                            _fontSizeTitle = 16;
                            break;

                        case 12:
                            _fontSizeExtraSmall = 8;
                            _fontSizeSmall = 10;
                            _fontSizeLarge = 14;
                            _fontSizeTitle = 16;
                            break;

                        case 13:
                            _fontSizeExtraSmall = 9;
                            _fontSizeSmall = 11;
                            _fontSizeLarge = 15;
                            _fontSizeTitle = 18;
                            break;

                        case 14:
                            _fontSizeExtraSmall = 10;
                            _fontSizeSmall = 12;
                            _fontSizeLarge = 16;
                            _fontSizeTitle = 20;
                            break;

                        case 15:
                            _fontSizeExtraSmall = 11;
                            _fontSizeSmall = 12;
                            _fontSizeLarge = 17;
                            _fontSizeTitle = 20;
                            break;

                        case 16:
                            _fontSizeExtraSmall = 12;
                            _fontSizeSmall = 14;
                            _fontSizeLarge = 18;
                            _fontSizeTitle = 22;
                            break;

                        case 17:
                            _fontSizeExtraSmall = 13;
                            _fontSizeSmall = 15;
                            _fontSizeLarge = 19;
                            _fontSizeTitle = 22;
                            break;

                        case 18:
                            _fontSizeExtraSmall = 14;
                            _fontSizeSmall = 16;
                            _fontSizeLarge = 20;
                            _fontSizeTitle = 24;
                            break;

                        case 20:
                            _fontSizeExtraSmall = 16;
                            _fontSizeSmall = 18;
                            _fontSizeLarge = 22;
                            _fontSizeTitle = 26;
                            break;

                        default:
                            return;
                    }

                    try
                    {
                        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                        ApplicationDataContainer appSettings;

                        if (localSettings.Containers.ContainsKey("application"))
                        {
                            appSettings = localSettings.Containers["application"];
                        }
                        else
                        {
                            appSettings = localSettings.CreateContainer("application", ApplicationDataCreateDisposition.Always);
                        }
                        appSettings.Values["font_size"] = value;
                    }
                    catch
                    {
                    }

                    NotifyPropertyChanged();
                }
            }
        }

        public double UIFontSizeExtraSmall
        {
            get { return _fontSizeExtraSmall * _ff; }
            set
            {
                _fontSizeExtraSmall = value; //(int)Math.Round(value); ;
                NotifyPropertyChanged();
            }
        }

        public double UIFontSizeSmall
        {
            get { return _fontSizeSmall * _ff; }
            set
            {
                _fontSizeSmall = value; //(int)Math.Round(value); ;
                NotifyPropertyChanged();
            }
        }

        public double UIFontSizeNormal
        {
            get { return _fontSizeNormal * _ff; }
            set
            {
                _fontSizeNormal = value; //(int)Math.Round(value); ;
                NotifyPropertyChanged();
            }
        }

        public double UIFontSizeLarge
        {
            get { return _fontSizeLarge * _ff; }
            set
            {
                _fontSizeLarge = value; //(int)Math.Round(value); ;
                NotifyPropertyChanged();
            }
        }

        public double UIFontSizeTitle
        {
            get { return _fontSizeTitle * _ff; }
            set
            {
                _fontSizeTitle = value;  //(int)Math.Round(value);
                NotifyPropertyChanged();
            }
        }

        public double UIControlHeightNormal
        {
            //get { return _fontSizeNormal < 12 ? _fontSizeNormal * 2.5 : _fontSizeNormal * 2; }
            //get { return (_fontSizeNormal * 2 + 4) * _ff; }
            get { return _fontSizeNormal * 2; }
        }

        public double UIControlHeightSmall
        {
            get { return _fontSizeNormal * 1.5; }
        }

        public double UIControlHeightLarge
        {
            get { return _fontSizeNormal * 3; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
