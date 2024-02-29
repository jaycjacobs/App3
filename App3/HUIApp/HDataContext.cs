using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CirrosUWP.HUIApp
{
    public class HDataContext : INotifyPropertyChanged
    {
        public HDataContext()
        {
        }

        double _sizeNormal = 14;

        public int Size
        {
            get { return (int)Math.Round(_sizeNormal); }
            set
            {
                if (_sizeNormal != value)
                {
                    _sizeNormal = value;

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

                    NotifyPropertyChanged("FontSize");
                }
            }
        }

        public double UIFontSizeExtraSmall
        {
            get { return _sizeNormal *.75; }
        }

        public double UIFontSizeSmall
        {
            get { return _sizeNormal * .85; }
        }

        public double UIFontSizeNormal
        {
            get { return _sizeNormal; }
        }

        public double UIFontSizeLarge
        {
            get { return _sizeNormal * 1.5; }
        }

        public double UIControlHeightNormal
        {
            get { return _sizeNormal * 2; }
        }

        public double UIControlHeightSmall
        {
            get { return _sizeNormal * 1.4; }
        }

        public double UIControlHeightLarge
        {
            get { return _sizeNormal * 3; }
        }

        public double DialogWidth
        {
            get { return _sizeNormal * 26; }
        }

        public GridLength DialogGridLength
        {
            get { return new GridLength(_sizeNormal * 26); }
        }

        public Size DialogTitleSize
        {
            get
            {
                return new Size(_sizeNormal * 26, _sizeNormal * 26);
            }
        }

        public Size LargeIconSize
        {
            get
            {
                Size size;
                if (_sizeNormal >= 16)
                {
                    size = new Size(80, 60);
                }
                else if (_sizeNormal >= 14)
                {
                    size = new Size(70, 52);
                }
                else
                {
                    size = new Size(60, 45);
                }
                return size;
            }
        }

        public Size MenuIconSize
        {
            get
            {
                return new Size(UIControlHeightLarge, UIControlHeightLarge);
            }
        }

        public double MenuTextWidth
        {
            get { return UIControlHeightLarge * 4; }
        }

        public double MenuPlusIconWidth
        {
            get { return UIControlHeightLarge + MenuTextWidth; }
        }

        public double MenuPlus2IconWidth
        {
            get { return UIControlHeightLarge + UIControlHeightLarge + MenuTextWidth; }
        }

        public GridLength MenuIconLength
        {
            get
            {
                return new GridLength(UIControlHeightLarge);
            }
        }

        public GridLength MenuTextLength
        {
            get { return new GridLength(MenuTextWidth); }
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
