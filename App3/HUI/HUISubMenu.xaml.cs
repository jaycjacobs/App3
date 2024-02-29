using CirrosUWP.HUIApp;
using Newtonsoft.Json.Linq;
using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace HUI
{
    public sealed partial class HUISubMenu : UserControl
    {
        private string _submenuId = "";

        Color _backgroundColor = Colors.Black;
        Color _highlightColor = Colors.Green;
        Color _foregroundColor = Colors.White;

        private bool _isExpanded = false;

        public event HUISubMenuSelectionChangedHandler OnHUISubMenuSelectionChanged;
        public delegate void HUISubMenuSelectionChangedHandler(object sender, HUISubMenuSelectionChangedEventArgs e);

        public HUISubMenu()
        {
            this.InitializeComponent();

            this.Loaded += HUISubMenu_Loaded;
        }

        void HUISubMenu_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = HGlobals.DataContext;

            _isExpanded = false;
            _submenu.Width = 0;
        }

        public void InitFromJson(JObject jobj, string submenuid)
        {
            _submenuId = submenuid;
            _submenu.Children.Clear();

            JArray submenus = (JArray)jobj["subMenus"];

            foreach (JObject submenu in submenus)
            {
                if (submenu[submenuid] != null)
                {
                    _highlightColor = (Color)(Application.Current.Resources["SubMenuHighlightColor"]);
                    _foregroundColor = (Color)(Application.Current.Resources["SubMenuForegroundColor"]);
                    _backgroundColor = (Color)(Application.Current.Resources["SubMenuBackgroundColor"]);

                    _background.Background = new SolidColorBrush(_backgroundColor);

                    JArray items = (JArray)submenu[submenuid]["items"];

                    for (int i = 0; i < items.Count; i++)
                    {
                        string id = (string)items[i]["id"];
                        string glyph = (string)items[i]["glyph"];
                        string title = (string)items[i]["title"];
                        string font = items[i]["font"] == null ? "" : (string)items[i]["font"];

                        string dismiss = items[i]["dismiss"] == null ? "light" : (string)items[i]["dismiss"];
                        string dialog = items[i]["dialog"] == null ? "" : (string)items[i]["dialog"];

                        AddSubmenuItem(_foregroundColor, _backgroundColor, _highlightColor, id, glyph, font, title, dismiss, dialog);
                    }

                    if (items.Count > 0 && items[0]["dialog"] == null)
                    {
                        _helpButton.Visibility = Visibility.Visible;
                    }  
                    else
                    {
                        _helpButton.Visibility = Visibility.Collapsed;
                    }
                }
            }
            if (_submenu.Children.Count > 0)
            {
                _selectedItem = _submenu.Children[0] as HUISubMenuItem;
                _selectedItem.IsActive = true;

                this.IsExpanded = true;
            }
            else
            {
                this.IsExpanded = false;
            }
        }

        HUISubMenuItem _selectedItem = null;

        void AddSubmenuItem(Color foreground, Color background, Color highlight, string id, string glyph, string font, string title, string dismiss, string dialog)
        {
            HUISubMenuItem item = new HUISubMenuItem(foreground, background, highlight, id, glyph, font, title, dismiss, dialog);
            item.OnHUISubMenuClick += item_OnHUISubMenuClick;
            _submenu.Children.Add(item);
        }

        void item_OnHUISubMenuClick(object sender, HUISubMenuClickEventArgs e)
        {
            if (_selectedItem != null)
            {
                _selectedItem.IsActive = false;
                _selectedItem = null;
            }

            if (sender is HUISubMenuItem)
            {
                _selectedItem = sender as HUISubMenuItem;
                _selectedItem.IsActive = true;

                if (OnHUISubMenuSelectionChanged != null)
                {
                    OnHUISubMenuSelectionChanged(this, new HUISubMenuSelectionChangedEventArgs(_submenuId, _selectedItem.Id, _selectedItem));
                }
            }
        }

        public void SelectItemById(string id)
        {
            foreach (HUISubMenuItem item in _submenu.Children)
            {
                if (item.Id == id)
                {
                    SelectedItem = item;
                    break;
                }
            }
        }

        public HUISubMenuItem SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                if (_selectedItem != null)
                {
                    _selectedItem.IsActive = false;
                    _selectedItem = null;
                }

                _selectedItem = value;
                _selectedItem.IsActive = true;
            }
        }

        public void FastExpand()
        {
            HUIView huiView = null;
            FrameworkElement fe = this.Parent as FrameworkElement;
            while (fe != null)
            {
                if (fe is HUIView)
                {
                    huiView = fe as HUIView;
                    huiView.SetValue(Grid.ColumnSpanProperty, 2);
                    break;
                }
                fe = fe.Parent as FrameworkElement;
            }
            _expandNoAnimation.Begin();
        }

        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                HUIView huiView = null;
                FrameworkElement fe = this.Parent as FrameworkElement;
                while (fe != null)
                {
                    if (fe is HUIView)
                    {
                        huiView = fe as HUIView;
                        break;
                    }
                    fe = fe.Parent as FrameworkElement;
                }

                _isExpanded = value;
                if (_isExpanded)
                {
                    if (huiView != null)
                    {
                        huiView.SetValue(Grid.ColumnSpanProperty, 2);
                    }
                    _expandAnimation.Begin();
                }
                else
                {
                    _unexpandAnimation.Begin();
                    if (huiView != null)
                    {
                        huiView.SetValue(Grid.ColumnSpanProperty, 1);
                    }
                }
            }
        }

        private int _tipIndex = 0;

        private void _helpButton_Click(object sender, RoutedEventArgs e)
        {
            _tipIndex = 0;

            if (_submenu.Children.Count > 0 && _submenu.Children[0] is HUISubMenuItem item)
            {
                item.ShowTeachingTip(_submenuId, _submenu.Children.Count > 1);
            }
        }

        public void ShowNextTeachingTip()
        {
            ++_tipIndex;

            if (_submenu.Children.Count > _tipIndex && _submenu.Children[_tipIndex] is HUISubMenuItem item)
            {
                item.ShowTeachingTip(_submenuId, _submenu.Children.Count > (_tipIndex + 1));
            }
        }
    }

    public class HUISubMenuSelectionChangedEventArgs : EventArgs
    {
        public string SubMenuId { get; private set; }
        public string ItemId { get; private set; }
        public HUISubMenuItem Item { get; private set; }

        public HUISubMenuSelectionChangedEventArgs(string submenuId, string itemId, HUISubMenuItem item)
        {
            SubMenuId = submenuId;
            ItemId = itemId;
            Item = item;
        }
    }
}
