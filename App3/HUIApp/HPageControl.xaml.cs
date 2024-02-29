using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace RedDog.HUIApp
{
    public sealed partial class HPageControl : UserControl
    {
        int _startItem = 0;
        int _totalItems = 0;
        int _pageSize = 0;
        int _pageCount = 0;
        int _page;

        public event HPageChangedHandler OnHPageChanged;
        public delegate void HPageChangedHandler(object sender, HPageChangedEventArgs e);

        public HPageControl()
        {
            this.InitializeComponent();
        }

        public HPageControl(int total, int pageSize)
        {
            this.InitializeComponent();

            _page = 0;
            _startItem = 0;
            _totalItems = total;
            _pageSize = pageSize;
            _pageCount = ((_totalItems - 1) / _pageSize) + 1;

            Page = _startItem / _pageSize;

            _nextButton.Click += _nextButton_Click;
            _prevButton.Click += _prevButton_Click;
        }

        void _prevButton_Click(object sender, RoutedEventArgs e)
        {
            if (OnHPageChanged != null)
            {
                if (_page > 0)
                {
                    --Page;
                    OnHPageChanged(this, new HPageChangedEventArgs(_page));
                }
            }
        }

        void _nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (OnHPageChanged != null)
            {
                if (_page < (_totalItems - 1))
                {
                    ++Page;
                    OnHPageChanged(this, new HPageChangedEventArgs(_page));
                }
            }
        }

        public int Page
        {
            get
            {
                return _page;
            }
            set
            {
                _page = value;

                _startItem = _page * _pageSize;
                int end = Math.Min(_startItem + _pageSize, _totalItems) - 1;

                _label.Text = string.Format("{0} - {1} of {2}", _startItem + 1, end + 1, _totalItems);

                _prevButton.IsEnabled = _page > 0;
                _nextButton.IsEnabled = _page < (_pageCount - 1);
            }
        }
    }

    public class HPageChangedEventArgs : EventArgs
    {
        public int Page { get; private set; }

        public HPageChangedEventArgs(int page)
        {
            Page = page;
        }
    }
}
