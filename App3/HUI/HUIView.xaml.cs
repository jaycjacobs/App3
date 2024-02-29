using Cirros;
using CirrosUWP.HUIApp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace HUI
{
    public sealed partial class HUIView : UserControl
    {
        JObject _jsonUI = null;
        Dictionary<string, string> _submenuSelection = new Dictionary<string, string>();

        public event HUICommandChangedHandler OnHUICommandChanged;
        public delegate void HUICommandChangedHandler(object sender, HUICommandChangedEventArgs e);

        private bool _stickySelection = false;
        private string _stickySelectionId = null;
        private Dictionary<string, object> _stickySelectionOptions = null;

        private string _queuedCommand = null;

        public HUIView()
        {
            this.InitializeComponent();

            _dialogGrid.Width = 0;

            this.Loaded += HUI_Loaded;

            _canvas.PointerPressed += _canvas_PointerPressed;

            _menu.OnHUIMenuSelectionChanged += _menu_OnHUIMenuSelectionChanged;
            _subMenu.OnHUISubMenuSelectionChanged += _subMenu_OnHUISubMenuSelectionChanged;
        }

        public void ShowMenu(bool show)
        {
            //_menu.IsExpanded = show;
            _menu.OpenSelectedItem();
        }

        public bool MenuIsVisible
        {
            get { return _menu.IsExpanded; }
        }

        public bool SubMenuIsExpanded
        {
            get
            {
                return _subMenu != null && _subMenu.IsExpanded;
            }
        }

        void _subMenu_OnHUISubMenuSelectionChanged(object sender, HUISubMenuSelectionChangedEventArgs e)
        {
            if (e.Item != null)
            {
                _queuedCommand = e.Item.Id;

                if (e.Item.Dismiss == "immediate")
                {
                    LightDismiss();
                }
                else if (e.Item.Dismiss == "none")
                {
                    SendCommand(_queuedCommand, new Dictionary<string, object>() { { "command", _queuedCommand } });
                }
                else
                {
                    if (_submenuSelection.ContainsKey(e.SubMenuId))
                    {
                        _submenuSelection[e.SubMenuId] = e.Item.Id;
                    }

                    ShowDialog(e.Item.Dialog);
                }
            }
        }

        void ShowDialog(string dialog)
        {
            _dialogGrid.Children.Clear();

            if (dialog.Length == 0)
            {
                _unexpandDialogAnimation.Begin();
                //this.SetValue(Grid.ColumnSpanProperty, 1);
                _dialogColumn.Width = new GridLength(1);
            }
            else
            {
                this.SetValue(Grid.ColumnSpanProperty, 2);
                _subMenu.FastExpand();
                _expandDialogAnimation.Begin();
                _dialogGrid.Children.Add(HUIDispatcher.GetDialogById(dialog));

                //double d = (double)App.Current.Resources["HUIDialogWidth"];
                //_dialogColumn.Width = new GridLength(d);
                _dialogColumn.Width = new GridLength(HGlobals.DataContext.DialogWidth);
            }

            _topBorder.Visibility = Visibility.Visible;
            _rightBorder.Visibility = Visibility.Visible;
        }

        void _menu_OnHUIMenuSelectionChanged(object sender, HUIMenuSelectionChangedEventArgs e)
        {
            FrameworkElement fe = this;
            do
            {
#if KT22
                if (fe is KT22.KTDrawingPage dp)
                {
                    dp.DismissPopups();
                    break;
                }
#else
                if (fe is RedDog.RedDogDrawingPage dp)
                {
                    dp.DismissPopups();
                    break;
                }
#endif

                fe = fe.Parent as FrameworkElement;
            }
            while (fe != null);


            if (e.Dismiss == "immediate")
            {
                _dialogGrid.Children.Clear();
                _queuedCommand = e.Id;
                LightDismiss();
                //SendCommand(_queuedCommand);
            }
            else
            {
                try
                {
                    _subMenu.InitFromJson(_jsonUI, e.Id);

                    if (_submenuSelection.ContainsKey(e.Id))
                    {
                        _subMenu.SelectItemById(_submenuSelection[e.Id]);
                    }
                    else
                    {
                        _submenuSelection.Add(e.Id, _subMenu.SelectedItem.Id);
                    }

                    _queuedCommand = e.Sticky == "yes" ? _subMenu.SelectedItem.Id : null;

                    ShowDialog(_subMenu.SelectedItem.Dialog);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }

            _stickySelection = e.Sticky == "yes";
        }

        void _canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            LightDismiss();
        }

        async void HUI_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = HGlobals.DataContext;

            this.KeyDown += HUIView_KeyDown;

            try
            {
                var uri = new System.Uri("ms-appx:///Data/HUICommands.json");
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                string json = await FileIO.ReadTextAsync(file);
                _jsonUI = JObject.Parse(json);

                _menu.InitFromJson(_jsonUI);
                _menu.EnableAllItems(this.IsEnabled);
                _subMenu.InitFromJson(_jsonUI, "home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            _hamburgerButton.OnHUIClick += _hamburgerButton_OnHUIClick;
        }

        private void HUIView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Tab)
            {

            }
        }

        void _hamburgerButton_OnHUIClick(object sender, HUIClickEventArgs e)
        {
            _menu.IsExpanded = !_menu.IsExpanded;
        }

        public void LightDismiss(bool cancel = false)
        {
            _unexpandDialogAnimation.Begin();
            _subMenu.IsExpanded = false;
            _menu.IsExpanded = false;
            this.SetValue(Grid.ColumnSpanProperty, 1);

            _topBorder.Visibility = Visibility.Collapsed;
            _rightBorder.Visibility = Visibility.Collapsed;

            Dictionary<string, object> options = null;

            if (_dialogGrid.Children.Count == 1 && _dialogGrid.Children[0] is HUIIDialog d)
            {
                if (d.Options == null)
                {
                    options = new Dictionary<string,object>() {{"command", _queuedCommand}};
                }
                else
                {
                    options = d.Options;
                }
                d.WillClose();
            }

            if (cancel == false && _queuedCommand != null)
            {
                SendCommand(_queuedCommand, options);
            }
        }

        public HUIIDialog CurrentDialog()
        {
            if (_dialogGrid.Children.Count == 1 && _dialogGrid.Children[0] is HUIIDialog d)
            {
                return d;
            }
            return null;
        }

        void SendCommand(string id, Dictionary<string, object> options = null)
        {
            if (_queuedCommand != null)
            {
                if (options == null)
                {
                    options = new Dictionary<string, object>();
                    options.Add("command", id);
                }

                options["source"] = "ui";   // does command input go through here?

                if (OnHUICommandChanged != null)
                {
                    OnHUICommandChanged(this, new HUICommandChangedEventArgs(id, options));
                }
                _queuedCommand = null;

                if (_stickySelection)
                {
                    _stickySelectionId = id;
                    _stickySelectionOptions = options;
                }
                else if (_stickySelectionId != null)
                {
                    OnHUICommandChanged(this, new HUICommandChangedEventArgs(_stickySelectionId, _stickySelectionOptions));
                    //_menu.IsExpanded = false;
                }
            }
        }
    }

    public class HUICommandChangedEventArgs : EventArgs
    {
        public string Id { get; private set; }
        public Dictionary<string, object> Options { get; private set; }

        public HUICommandChangedEventArgs(string id, Dictionary<string, object> options)
        {
            Id = id;
            Options = options;
        }
    }
}
