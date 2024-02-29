using Cirros;
using Cirros.Primitives;
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using App3;

namespace Cirros8.Popup_Panels
{
    public sealed partial class GroupAttributesPanel : UserControl
    {
        double _dragXOff = 0;
        double _dragYOff = 0;
        bool _isDragging = false;
        Popup _popup;
        Point _workCanvasOffset;

        Rect _box = Rect.Empty;
        private PInstance _instance;
        private int _attributeIndex = -1;
        private int _maxLines = 1;
        private bool _isDirty = false;

        private double _valuePanel1BaseHeight = 0;
        private double _gaPanelBaseHeight = 0;

        public GroupAttributesPanel()
        {
            this.InitializeComponent();

            this.Loaded += GroupAttributesPanel_Loaded;
            this.LayoutUpdated += GroupAttributesPanel_LayoutUpdated;
            this.Unloaded += GroupAttributesPanel_Unloaded;
            this.PointerPressed += GroupAttributesPanel_PointerPressed;
            this.PointerMoved += GroupAttributesPanel_PointerMoved;
            this.PointerReleased += GroupAttributesPanel_PointerReleased;

            _valuePanel1.TextChanged += _valuePanel1_TextChanged;
            _valuePanel1.SizeChanged += _valuePanel1_SizeChanged;

            _valuePanel1.KeyDown += _valuePanel1_KeyDown;
            _valuePanel1.GotFocus += _valuePanel1_GotFocus;
            _doneButton.Click += _doneButton_Click;

            DataContext = Globals.UIDataContext;
        }

        void GroupAttributesPanel_LayoutUpdated(object sender, object e)
        {
            if (_valuePanel1BaseHeight == 0 || _gaPanelBaseHeight == 0)
            {
                _valuePanel1BaseHeight = _valuePanel1.ActualHeight;
                _gaPanelBaseHeight = this.ActualHeight - _valuePanel1BaseHeight;
            }
        }

        void _valuePanel1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Cirros.Utility.Utilities.__checkSizeChanged(18, sender)) return;

            if (_maxLines > 0 && _gaPanelBaseHeight > 0 && _valuePanel1BaseHeight > 0)
            {
                this.Height = _gaPanelBaseHeight + _valuePanel1.ActualHeight;
            }
        }

        void _valuePanel1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_valuePanel1.Text.EndsWith("\r\r") || _valuePanel1.Text.EndsWith("\r\n\r\n"))
            {
                doNext();
            }
            else if (_valuePanel1.Text.EndsWith("\r") || _valuePanel1.Text.EndsWith("\r\n"))
            {
                if (_maxLines < 2)
                {
                    doNext();
                }
                else
                {
#if WINDOWS_UWP
                    string newline = "\r";
#else
                    string newline = "\n";
#endif
                    int lines = 1;
                    int nli = _valuePanel1.Text.IndexOf(newline);
                    while (nli >= 0)
                    {
                        if (lines++ >= _maxLines)
                        {
                            doNext();
                            break;
                        }
                        nli = _valuePanel1.Text.IndexOf(newline, nli + 1);
                    }
                }
            }
            _isDirty = true;
        }

        void _valuePanel1_GotFocus(object sender, RoutedEventArgs e)
        {
            _valuePanel1.SelectAll();
        }

        void _valuePanel1_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                doNext();
                e.Handled = true;
            }
        }

        void GroupAttributesPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            saveCurrent();

            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.Invoke("A_Done", null);
            }
        }

        void _doneButton_Click(object sender, RoutedEventArgs e)
        {
            doNext();
        }

        void doNext()
        {
            if (_instance == null)
            {
                _popup.IsOpen = false;
            }
            else
            {
                saveCurrent();

                if (_attributeIndex >= 0)
                {
                    _attributeIndex++;

                    _doneButton.Content = _attributeIndex < _instance.AttributeList.Count - 1 ? "Next" : "Done";

                    if (_instance.AttributeList.Count > _attributeIndex)
                    {
                        _attribute1.Text = _instance.AttributeList[_attributeIndex].Prompt;
                        _valuePanel1.Text = _instance.AttributeList[_attributeIndex].Value;
                        _maxLines = _instance.AttributeList[_attributeIndex].MaxLines;
                        _valuePanel1.AcceptsReturn = _maxLines > 1;

                        if (_maxLines < 2)
                        {
                            _linesMessage.Text = "";
                        }
                        else
                        {
                            string format = "Enter up to {0} lines";
                            _linesMessage.Text = string.Format(format, _maxLines);
                        }
                        _valuePanel1.SelectAll();
                    }
                    else
                    {
                        _popup.IsOpen = false;
                    }
                }
                else
                {
                    _popup.IsOpen = false;
                }
            }
        }

        void saveCurrent()
        {
            if (_isDirty && _instance != null && _attributeIndex >= 0 &&  _instance.AttributeList.Count > _attributeIndex)
            {
                _instance.SetAttributeValue(_instance.AttributeList[_attributeIndex].Prompt, _valuePanel1.Text.Trim().Replace("\r", "\n"));
                _instance.Draw();
            }
        }

        public Point WorkCanvasOffset
        {
            set
            {
                _workCanvasOffset = value;
            }
        }

        void GroupAttributesPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // Assume that Show() has already been called

            _popup = this.Parent as Popup;

            if (_popup != null && _box != Rect.Empty && ActualHeight > 0 && ActualWidth > 0)
            {
                double leftSpace = _box.Left + _workCanvasOffset.X;
                double rightSpace = App.Window.Bounds.Width - (_box.Right + _workCanvasOffset.X);
                double topSpace = _box.Top + _workCanvasOffset.Y;
                double bottomSpace = App.Window.Bounds.Height - (_box.Bottom + _workCanvasOffset.Y);

                if (Math.Max(leftSpace, rightSpace) < (ActualWidth + 60))
                {
                    _popup.HorizontalOffset = App.Window.Bounds.Width / 3;
                }
                else
                {
                    _popup.HorizontalOffset = (leftSpace > rightSpace ? _box.Left - ActualWidth - 40 : _box.Right + 40) + _workCanvasOffset.X;
                }

                if (Math.Max(topSpace, bottomSpace) < (ActualHeight + 60))
                {
                    _popup.VerticalOffset = App.Window.Bounds.Height / 3;
                }
                else
                {
                    _popup.VerticalOffset = (topSpace > bottomSpace ? _box.Top - ActualHeight - 40 : _box.Bottom + 40) + _workCanvasOffset.Y;
                }

                _doneButton.Content = _instance.AttributeList.Count > 1 ? "Next" : "Done";
            }

            _valuePanel1.Focus(FocusState.Programmatic);
        }

        void GroupAttributesPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _popup = this.Parent as Popup;
            if (_popup != null)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _dragXOff = p.X - _popup.HorizontalOffset;
                _dragYOff = p.Y - _popup.VerticalOffset;
                _isDragging = true;

                CapturePointer(e.Pointer);
            }
        }

        void GroupAttributesPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                Point p = e.GetCurrentPoint(null).Position;
                _popup.HorizontalOffset = p.X - _dragXOff;
                _popup.VerticalOffset = p.Y - _dragYOff;
            }
        }

        void GroupAttributesPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCapture(e.Pointer);
            _isDragging = false;
        }

        public void Show(PInstance p)
        {
            if (_box == Rect.Empty)
            {
                Point ll = Globals.View.PaperToDisplay(new Point(p.Box.Left, p.Box.Bottom));
                Point ur = Globals.View.PaperToDisplay(new Point(p.Box.Right, p.Box.Top));
                _box = new Rect(ll, ur);
            }
            if (p.AttributeList.Count > 0)
            {
                _instance = p;
                _attributeIndex = 0;
                _attribute1.Text = _instance.AttributeList[_attributeIndex].Prompt;
                _valuePanel1.Text = _instance.AttributeList[_attributeIndex].Value;
                _maxLines = _instance.AttributeList[_attributeIndex].MaxLines;
                _valuePanel1.AcceptsReturn = _maxLines > 1;

                if (_maxLines < 2)
                {
                    _linesMessage.Text = "";
                }
                else
                {
                    string format = "Enter up to {0} lines";
                    _linesMessage.Text = string.Format(format, _maxLines);
                }

                _isDirty = false;
            }
        }
    }
}
