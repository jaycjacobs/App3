using RedDog.Drawing_page;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace CirrosUWP.RedDog
{
    public enum RedDogArrowButtonDirection
    {
        up,
        down,
        left,
        right
    }

    // up:      xE74A
    // down:    xE1FD
    // left:    xE72B
    // right:   xE72A

    public sealed partial class RedDogArrowButton : UserControl
    {
        RedDogCoordinatePanel _coordinatePanel = null;
        //DispatcherTimer _repeatTimer = null;
        RedDogArrowButtonDirection _direction;
        //bool _stillDown = false;

        public RedDogArrowButton()
        {
            this.InitializeComponent();

            this.Loaded += RedDogArrowButton_Loaded;
            this.Unloaded += RedDogArrowButton_Unloaded;
            //this.PointerPressed += RedDogArrowButton_PointerPressed;
            //this.PointerReleased += RedDogArrowButton_PointerReleased;
        }

        private void RedDogArrowButton_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void RedDogArrowButton_Unloaded(object sender, RoutedEventArgs e)
        {
            //if (_repeatTimer != null)
            //{
            //    _repeatTimer.Stop();
            //    _repeatTimer = null;
            //    _stillDown = false;
            //}
        }

        //private void RedDogArrowButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        //{
        //    CapturePointer(e.Pointer);

        //    //_rectangle.Fill = new SolidColorBrush(Colors.LightGray);

        //    if (_coordinatePanel != null && _direction > 0)
        //    {
        //        _coordinatePanel.Step(_direction);

        //        if (_repeatTimer == null)
        //        {
        //            _repeatTimer = new DispatcherTimer();
        //            _repeatTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
        //            _repeatTimer.Tick += _repeatTimer_Tick;
        //        }
        //        _repeatTimer.Start();
        //    }
        //}

        //private void RedDogArrowButton_PointerReleased(object sender, PointerRoutedEventArgs e)
        //{
        //    ReleasePointerCapture(e.Pointer);

        //    if (_repeatTimer != null)
        //    {
        //        _repeatTimer.Stop();
        //        _repeatTimer = null;
        //    }
        //    //_rectangle.Fill = new SolidColorBrush(Colors.Transparent);
        //}

        //void _repeatTimer_Tick(object sender, object e)
        //{
        //    if (_coordinatePanel != null && _direction > 0)
        //    {
        //        _coordinatePanel.Step(_direction, _stillDown);
        //    }

        //    if (_repeatTimer == null)
        //    {
        //        _stillDown = true;
        //    }
        //    else
        //    {
        //        _repeatTimer.Interval = new TimeSpan(0, 0, 0, 0, 70);
        //    }
        //}

        public RedDogCoordinatePanel CoordinatePanel
        {
            get
            {
                return _coordinatePanel;
            }
            set
            {
                _coordinatePanel = value;
            }
        }

        public RedDogArrowButtonDirection Direction
        {
            get
            {
                return _direction;
            }
            set
            {
                _direction = value;

                SetDirection(_direction);
            }
        }

        private void SetDirection(RedDogArrowButtonDirection direction)
        {
            string symbol = "X";
            string tt = "";

            switch (_direction)
            {
                case RedDogArrowButtonDirection.up:
                    symbol = "0xe74a";
                    tt = "Step up";
                    break;
                case RedDogArrowButtonDirection.down:
                    symbol = "0xe1fd";
                    tt = "Step down";
                    break;
                case RedDogArrowButtonDirection.left:
                    symbol = "0xe72b";
                    tt = "Step left";
                    break;
                case RedDogArrowButtonDirection.right:
                    symbol = "0xe72a";
                    tt = "Step right";
                    break;
            }

            string hs = symbol.Substring(2);
            int i = int.Parse(hs, System.Globalization.NumberStyles.HexNumber);
            char u = (char)i;
            _arrowButton.Content = u.ToString();
            _arrowButton.SetValue(ToolTipService.ToolTipProperty, tt);
        }

        private void _arrowButton_Click(object sender, RoutedEventArgs e)
        {
            if (_coordinatePanel != null)
            {
                _coordinatePanel.Step(_direction);
            }
        }

        private void _arrowButton_Holding(object sender, HoldingRoutedEventArgs e)
        {

        }
    }
}
