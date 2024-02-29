using Cirros;
using Cirros.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace CirrosUI.Context_Menu
{
    public sealed partial class PropertiesContextPanel : UserControl
    {
        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;

        private Primitive _primitive;

        public PropertiesContextPanel()
        {
            this.InitializeComponent();
        }

        public PropertiesContextPanel(Primitive p)
        {
            this.InitializeComponent();

            this.PointerPressed += PropertiesContextPanel_PointerPressed;
            this.PointerMoved += PropertiesContextPanel_PointerMoved;
            this.PointerReleased += PropertiesContextPanel_PointerReleased;  

            Primitive = p;

            DataContext = Globals.UIDataContext;
        }

        private void PropertiesContextPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Popup popup = this.Parent as Popup;
            if (popup != null)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _dragXOff = p.X - popup.HorizontalOffset;
                _dragYOff = p.Y - popup.VerticalOffset;
                _isDragging = true;

                CapturePointer(e.Pointer);
            }
        }

        private void PropertiesContextPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _isDragging = false;
        }

        private void PropertiesContextPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                Popup popup = this.Parent as Popup;
                if (popup != null)
                {
                    Point p = e.GetCurrentPoint(null).Position;
                    popup.HorizontalOffset = p.X - _dragXOff;
                    popup.VerticalOffset = p.Y - _dragYOff;
                }
            }
        }

        public Primitive Primitive
        {
            get { return _primitive; }
            set
            {
                _primitive = value;
                _propertyPanel.Primitive = _primitive;

                //_zIndexBox.Value = _primitive.ZIndex;

                var resourceLoader = new ResourceLoader();
                string format = resourceLoader.GetString("PropertiesTitle");
                string type = resourceLoader.GetString("PrimitiveType" + _primitive.TypeName.ToString());
                _titleBlock.Text = string.Format(format, type);
#if DEBUG
                _titleBlock.Text = _titleBlock.Text + " [" + _primitive.Id.ToString() + "]";
#endif
            }
        }
    }
}
