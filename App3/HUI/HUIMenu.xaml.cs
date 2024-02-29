using Cirros;
using CirrosUWP.HUIApp;
using Newtonsoft.Json.Linq;
using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;

namespace HUI
{
    public sealed partial class HUIMenu : UserControl
    {
        Color _backgroundColor = Colors.Black;
        Color _activeColor = Colors.SteelBlue;
        Color _foregroundColor = Colors.White;

        public event HUIMenuSelectionChangedHandler OnHUIMenuSelectionChanged;
        public delegate void HUIMenuSelectionChangedHandler(object sender, HUIMenuSelectionChangedEventArgs e);

        public HUIMenu()
        {
            this.InitializeComponent();

            DataContext = HGlobals.DataContext;
        }

        HUIButton _selectedHUIButton = null;
        private bool _isExpanded = false;

        public void InitFromJson(JObject jobj)
        {
            _menu.Children.Clear();
#if true
            _activeColor = (Color)(Application.Current.Resources["MenuActiveColor"]);
            _foregroundColor = (Color)(Application.Current.Resources["MenuForegroundColor"]);
            _backgroundColor = (Color)(Application.Current.Resources["MenuInactiveColor"]);
#else
            string activeColor = (string)jobj["mainMenu"]["activeColor"];
            string inactiveColor = (string)jobj["mainMenu"]["inactiveColor"];
            string foregroundColor = (string)jobj["mainMenu"]["foregroundColor"];

            activeColor = activeColor.ToLower();
            inactiveColor = inactiveColor.ToLower();
            foregroundColor = foregroundColor.ToLower();

            if (StandardColors.ColorNames.ContainsKey(activeColor))
            {
                uint c = StandardColors.ColorNames[activeColor];
                _activeColor = StandardColors.ColorFromColorSpec(c);
            }

            if (StandardColors.ColorNames.ContainsKey(inactiveColor))
            {
                uint c = StandardColors.ColorNames[inactiveColor];
                _backgroundColor = StandardColors.ColorFromColorSpec(c);
            }

            if (StandardColors.ColorNames.ContainsKey(foregroundColor))
            {
                uint c = StandardColors.ColorNames[foregroundColor];
                _foregroundColor = StandardColors.ColorFromColorSpec(c);
            }
#endif
            JArray items = (JArray)jobj["mainMenu"]["items"];

            for (int i = 0; i < items.Count; i++)
            {
                string id = (string)items[i]["id"];
                string glyph = (string)items[i]["glyph"];
                string title = (string)items[i]["title"];

                string dismiss = items[i]["dismiss"] == null ? "light" : (string)items[i]["dismiss"];
                string sticky = items[i]["sticky"] == null ? "yes" : (string)items[i]["sticky"];
                string font = items[i]["font"] == null ? "" : (string)items[i]["font"];
                string tip = items[i]["tip"] == null ? "" : (string)items[i]["tip"];

                if (glyph.StartsWith("0x"))
                {
                    string hs = glyph.Substring(2);
                    int h = int.Parse(hs, System.Globalization.NumberStyles.HexNumber);
                    char u = (char)h;
                    glyph = u.ToString();
                }

                AddMenuItem(_activeColor, _backgroundColor, _foregroundColor, id, glyph, title, dismiss, sticky, font, tip);
            }
        }

        void AddMenuItem(Color active, Color inactive, Color foreground, string id, string glyph, string title, string dismiss, string sticky, string font, string tip)
        {
            HUIButton hb = new HUIButton(active, inactive, foreground, id, glyph, title, dismiss, sticky, font, tip);
            hb.OnHUIClick += hb_OnHUIClick;
            _menu.Children.Add(hb);
        }

        public void EnableAllItems(bool enable)
        {
            foreach (object o in _menu.Children)
            {
                if (o is HUIButton)
                {
                    ((HUIButton)o).IsEnabled = enable;
                }
            }
        }

        public void OpenSelectedItem()
        {
            if (_selectedHUIButton == null)
            {
                SelectItemById("draw");
            }
            else if (_selectedHUIButton != null)
            {
                _selectedHUIButton.IsActive = true;
                SelectionChanged(_selectedHUIButton.Id, _selectedHUIButton.Dismiss, _selectedHUIButton.Sticky);
            }
        }

        void hb_OnHUIClick(object sender, HUIClickEventArgs e)
        {
            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.Finish();
            }

            if (_selectedHUIButton != null)
            {
                _selectedHUIButton.IsActive = false;
                _selectedHUIButton = null;
            }

            if (sender is HUIButton)
            {
                _selectedHUIButton = sender as HUIButton;
                _selectedHUIButton.IsActive = true;
                SelectionChanged(_selectedHUIButton.Id, _selectedHUIButton.Dismiss, _selectedHUIButton.Sticky);
            }

            this.IsExpanded = false;
        }

        public void SelectItemById(string id)
        {
            if (_selectedHUIButton == null || _selectedHUIButton.Id != id)
            {
                foreach (HUIButton hb in _menu.Children)
                {
                    if (hb.Id == id)
                    {
                        if (_selectedHUIButton != null)
                        {
                            _selectedHUIButton.IsActive = false;
                            _selectedHUIButton = null;
                        }

                        _selectedHUIButton = hb;
                        _selectedHUIButton.IsActive = true;
                        SelectionChanged(_selectedHUIButton.Id, _selectedHUIButton.Dismiss, _selectedHUIButton.Sticky);
                        IsExpanded = false;
                        break;
                    }
                }
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
                        object o = huiView.GetValue(Grid.ColumnSpanProperty);
                        //int i = (int)o;
                        //System.Diagnostics.Debug.WriteLine("column span = {0}", i);
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
                        if (huiView.SubMenuIsExpanded == false)
                        {
                            huiView.SetValue(Grid.ColumnSpanProperty, 1);
                        }
                    }
                }
            }
        }

        private void SelectionChanged(string command, string dismiss, string sticky)
        {
            if (OnHUIMenuSelectionChanged != null)
            {
                OnHUIMenuSelectionChanged(this, new HUIMenuSelectionChangedEventArgs(command, dismiss, sticky));
            }
        }
    }

    public class HUIMenuSelectionChangedEventArgs : EventArgs
    {
        public string Id { get; private set; }
        public string Dismiss { get; private set; }
        public string Sticky { get; private set; }

        public HUIMenuSelectionChangedEventArgs(string id, string dismiss, string sticky)
        {
            Id = id;
            Dismiss = dismiss;
            Sticky = sticky;
        }
    }
}
