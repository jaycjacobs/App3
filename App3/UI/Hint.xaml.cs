using Cirros;
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CirrosUI
{
    public sealed partial class Hint : UserControl
    {
        public Hint()
        {
            this.InitializeComponent();

            this.Unloaded += Hint_Unloaded;

            _border.Background = new SolidColorBrush(Globals.ActiveDrawing.Theme.BackgroundColor);
            _border.BorderBrush = _text.Foreground = new SolidColorBrush(Globals.ActiveDrawing.Theme.ForegroundColor);

            Globals.Events.OnThemeChanged += Events_OnThemeChanged;

            DataContext = Globals.UIDataContext;
        }

        void Events_OnThemeChanged(object sender, EventArgs e)
        {
            _border.Background = new SolidColorBrush(Globals.ActiveDrawing.Theme.BackgroundColor);
            _border.BorderBrush = _text.Foreground = new SolidColorBrush(Globals.ActiveDrawing.Theme.ForegroundColor);
        }

        void Hint_Unloaded(object sender, RoutedEventArgs e)
        {
            Globals.Events.OnThemeChanged -= Events_OnThemeChanged;
        }

        Point _location = new Point(-1, -1);
        int _phase = 0;

        DispatcherTimer _timer = null;

        public void Show(string tag, Point location)
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Tick +=_hoverTimer_Tick;
            }

            if (tag == null)
            {
                Cancel();
            }
            else if (location != _location)
            {
                if (_phase > 0)
                {
                    _timer.Stop();
                    _phase = 0;
                }

                _location = location;

                _text.Text = tag;
                SetValue(Canvas.LeftProperty, location.X);
                SetValue(Canvas.TopProperty, location.Y);

                Opacity = 0;
                Visibility = Visibility.Visible;

                _timer.Interval = new TimeSpan(0, 0, 0, 0, 500);


                _timer.Start();
            }
        }

        public void Cancel()
        {
            if (_phase > 0)
            {
                _timer.Stop();
                _phase = 0;
            }

            Opacity = 0;
            //Visibility = Visibility.Collapsed;
            _location = new Point(-1, -1);
        }

        void _hoverTimer_Tick(object sender, object e)
        {
            if (_phase == 0)
            {
                // Start
                _phase = 1;
                _timer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            }
            else if (_phase == 1)
            {
                // Fade in
                if (Opacity < .85)
                {
                    Opacity += .1;
                }
                else
                {
                    _phase = 2;
                _timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
                }
            }
            else if (_phase == 2)
            {
                // Hold
                _phase = 3;
                _timer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            }
            else if (_phase == 3)
            {
                // Fade out
                if (Opacity > 0)
                {
                    Opacity -= .1;
                }
                else
                {
                    _timer.Interval = new TimeSpan(0, 0, 0, 3, 0);
                    _phase = 4;

                    Opacity = 0;
                    Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Wait a while before showing the same tag

                _timer.Stop();
                _phase = 0;
                _location = new Point(-1, -1);

                Opacity = 0;
                Visibility = Visibility.Collapsed;
            }
        }
    }
}
