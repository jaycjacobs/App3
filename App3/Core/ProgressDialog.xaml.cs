using System;
using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Cirros.Dialogs
{
    public sealed partial class ProgressDialog : ContentDialog
    {
        double _progressMax = 0;
        ICollection _collection = null;

        public ICollection Collection
        {
            set
            {
                _collection = value;
                if (_collection != null)
                {
                    _progressMax = _progressBar.Maximum = (double)_collection.Count;
                }
            }
        }

        public ProgressDialog(string message)
        {
            this.InitializeComponent();
            this.Title = message;
            this.Loaded += ProgressDialog_Loaded;
        }

        DispatcherTimer _timer;

        private void ProgressDialog_Loaded(object sender, RoutedEventArgs e)
        {

            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _timer.Tick += _timer_Tick;

            if (_timer != null)
            {
                _timer.Start();
            }
        }

        void _timer_Tick(object sender, object e)
        {
            if (_collection.Count > 0)
            {
                _progressBar.Value = _progressMax - _collection.Count;
            }
            else
            {
                _timer.Stop();
                Hide();
            }
        }
    }
}
