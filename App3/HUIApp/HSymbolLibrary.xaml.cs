using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RedDog.HUIApp
{
    public sealed partial class HSymbolLibrary : UserControl
    {
        int _maxItems = 0;
        int _itemsPerRow = 1;
        int _itemsPerPage = 1;
        int _itemRows = 1;
        int _pageStartItem = 0;
        int _middleSymbolMargin = 0;

        HPageControl _pageControl = null;

        List<string> _symbolList = new List<string>();

        public event HSymbolLibraryItemSelectedHandler OnHSymbolLibraryItemSelected;
        public delegate void HSymbolLibraryItemSelectedHandler(object sender, HSymbolLibraryItemSelectedEventArgs e);

        public HSymbolLibrary()
        {
            this.InitializeComponent();

            this.Loaded += HSymbolLibrary_Loaded;
            this.SizeChanged += HSymbolLibrary_SizeChanged;
        }

        bool _symbolLibraryIsInitialized = false;

        const int cPageControlHeight = 34;
        const int cSymbolWidth = 94;
        const int cSymbolHeight = 115;

        void InitializeSymbolLibrary()
        {
            if (_symbolLibraryIsInitialized == false)
            {
                _symbolLibraryIsInitialized = true;

                int symbolCount = 52;

                for (int i = 0; i < symbolCount; i++)
                {
                    _symbolList.Add(string.Format("Symbol {0}", i));
                }

                _itemsPerRow = (int)(_iconColumn.ActualWidth / cSymbolWidth);
                _itemRows = Math.Min((int)((_iconRow.ActualHeight - cPageControlHeight) / cSymbolHeight), (symbolCount / _itemsPerRow) + 1);
                _itemsPerPage = _itemsPerRow * _itemRows;
                _maxItems = _itemRows * _itemsPerRow;

                _middleSymbolMargin = (int)((_iconColumn.ActualWidth - _itemsPerRow * cSymbolWidth) / 2);

                _vstack.Children.Clear();

                for (int i = 0; i < _itemRows; i++)
                {
                    StackPanel sp = new StackPanel();
                    sp.Style = (Style)(Application.Current.Resources["HSymbolRow"]);
                    _vstack.Children.Add(sp);
                }

                _pageControl = new HPageControl(_symbolList.Count, _itemsPerRow * _itemRows);
                _pageControl.OnHPageChanged += _pageControl_OnHPageChanged;
                _vstack.Children.Add(_pageControl);

                Populate();
            }
        }
        void HSymbolLibrary_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize == e.NewSize) return;
            if (Cirros.Utility.Utilities.__checkSizeChanged(54, sender)) return;

            if (_iconColumn.ActualWidth > 0)
            {
                InitializeSymbolLibrary();
            }
        }

        void HSymbolLibrary_Loaded(object sender, RoutedEventArgs e)
        {
            if (_iconColumn.ActualWidth > 0)
            {
                InitializeSymbolLibrary();
            }
        }

        void _pageControl_OnHPageChanged(object sender, HPageChangedEventArgs e)
        {
            _pageStartItem = e.Page * _itemsPerPage;
            Populate();
        }

        void Populate()
        {
            for (int i = 0; i < _itemRows; i++)
            {
                if (_vstack.Children[i] is StackPanel)
                {
                    ((StackPanel)_vstack.Children[i]).Children.Clear();
                }
            }

            int currentItem = _pageStartItem;

            for (int row = 0; row < _itemRows && currentItem < _symbolList.Count; row++)
            {
                if (_vstack.Children[row] is StackPanel)
                {
                    StackPanel rowPanel = (StackPanel)_vstack.Children[row];
                    for (int col = 0; col < _itemsPerRow && currentItem < _symbolList.Count; col++)
                    {
                        string name = _symbolList[currentItem++];
                        string id = name;
                        HSymbolLibraryItem symbolItem = new HSymbolLibraryItem(name, id);
                        symbolItem.Height = cSymbolHeight;
                        symbolItem.Width = cSymbolWidth;
                        symbolItem.OnHSymbolLibraryItemClick += symbolItem_OnHSymbolLibraryItemClick;

                        if (col > 0 && col < (_itemsPerRow - 1))
                        {
                            symbolItem.Margin = new Thickness(_middleSymbolMargin, 0, _middleSymbolMargin, 0);
                        }

                        rowPanel.Children.Add(symbolItem);
                    }
                }
            }
        }

        void symbolItem_OnHSymbolLibraryItemClick(object sender, HSymbolLibraryItemClickEventArgs e)
        {
            if (OnHSymbolLibraryItemSelected != null && sender is HSymbolLibraryItem)
            {
                OnHSymbolLibraryItemSelected(this, new HSymbolLibraryItemSelectedEventArgs(sender as HSymbolLibraryItem));
            }
        }
    }

    public class HSymbolLibraryItemSelectedEventArgs : EventArgs
    {
        public HSymbolLibraryItem Item { get; private set; }

        public HSymbolLibraryItemSelectedEventArgs(HSymbolLibraryItem item)
        {
            Item = item;
        }
    }
}
