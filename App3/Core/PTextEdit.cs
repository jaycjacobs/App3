using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using System;
using Windows.Foundation;
using Microsoft.Windows.System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.System;

namespace Cirros.Utility
{
    public class PTextEdit : Canvas
    {
        protected PText _ptext;
        protected TextBox _textBox;
        protected double _scale;

        Point _location;
        Rect _itemBox = Rect.Empty;

        Canvas _rootCanvas;

        bool _finished = false;

        public PTextEdit(PText ptext)
        {
            this.Unloaded += PTextEdit_Unloaded;
            _ptext = ptext;

            _rootCanvas = Globals.DrawingCanvas.Parent as Canvas;
            _rootCanvas.Children.Add(this);

            _location = Globals.View.PaperToDisplay(ptext.Origin);

            int styleId = ptext.TextStyleId < Globals.TextStyleTable.Count ? ptext.TextStyleId : 0;
            TextStyle style = Globals.TextStyleTable[styleId];
            double size = Globals.View.PaperToDisplay((ptext.Size > 0 ? ptext.Size : style.Size) * 1.35);

            _textBox = new TextBox();
            _textBox.FontFamily = new FontFamily(style.Font);
            _textBox.FontSize = Math.Max(size, 12);

            _textBox.TextChanged += _textBox_TextChanged;
            _textBox.LayoutUpdated += _textBox_LayoutUpdated;
            _textBox.TextWrapping = TextWrapping.NoWrap;
            _textBox.AcceptsReturn = true;
            _textBox.KeyDown += _textBox_KeyDown;
            _textBox.BorderBrush = new SolidColorBrush(Colors.Red);
            _textBox.Text = ptext.Text;

            switch (_ptext.Alignment)
            {
                case TextAlignment.Left:
                    _textBox.HorizontalAlignment = HorizontalAlignment.Left;
                    _textBox.TextAlignment = TextAlignment.Left;
                    break;
                case TextAlignment.Center:
                    _textBox.HorizontalAlignment = HorizontalAlignment.Center;
                    _textBox.TextAlignment = TextAlignment.Center;
                    break;
                case TextAlignment.Right:
                    _textBox.HorizontalAlignment = HorizontalAlignment.Right;
                    _textBox.TextAlignment = TextAlignment.Right;
                    break;
            }

            _textBox.SetValue(Canvas.LeftProperty, _location.X);
            _textBox.SetValue(Canvas.TopProperty, _location.Y);

            this.Children.Add(_textBox);

            _ptext.IsVisible = false;

            _textBox.Focus(FocusState.Keyboard);
            Select();

            Globals.Input.SelectCursor(CursorType.Arrow);
        }

        void _textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_textBox.Text.EndsWith("\r\r"))
            {
                Finish();
            }
            else if (_textBox.Text.EndsWith("\r\n\r\n"))
            {
                Finish();
            }
            else
            {
                _ptext.Text = _textBox.Text;
                //_ptext.TextStyleChanged();
                _ptext.Draw();
            }
        }

        void _textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Finish();
        }

        void PTextEdit_Unloaded(object sender, RoutedEventArgs e)
        {
            Finish();
        }

        void _textBox_LayoutUpdated(object sender, object e)
        {
            if (_textBox != null)
            {
                if (_itemBox.IsEmpty && _textBox.ActualWidth > 0 && _textBox.ActualHeight > 0)
                {
                    VectorContext vc = new VectorContext(true, true, true);
                    VectorEntity ve = _ptext.Vectorize(vc);
                    if (ve.Children.Count == 1 && ve.Children[0] is VectorTextEntity)
                    {
                        VectorTextEntity vt = ve.Children[0] as VectorTextEntity;
                        double lh = vt.TextHeight * vt.LineSpacing;

                        string[] lines = _ptext.Text.Split(new[] { '\n' });

                        double fs = Dx.GetFontSizeFromHeight(vt.FontFamily, vt.TextHeight);
                        double height, descent;
                        TextUtilities.FontInfo.FontHeight(vt.FontFamily, fs, out height, out descent);

                        double sw = 0;

                        foreach (string s in lines)
                        {
                            double w = 0;
                            try
                            {
                                w = Cirros.TextUtilities.FontInfo.StringWidth(s, vt.FontFamily, fs);
                            }
                            catch
                            {
                                w = vt.TextHeight * .6 * s.Length;
                            }

                            if (w > sw)
                            {
                                sw = w;
                            }
                        }

                        sw *= vt.CharacterSpacing;

                        double dx = 0;
                        double dy = 0;

                        if (vt.TextAlignment == TextAlignment.Center)
                        {
                            dx = -sw / 2;
                        }
                        else if (vt.TextAlignment == TextAlignment.Right)
                        {
                            dx = -sw;
                        }

                        if (vt.TextPosition == TextPosition.Above)
                        {
                            dy = -(lines.Length - 1) * lh;
                        }
                        else if (vt.TextPosition == TextPosition.On)
                        {
                            dy = -(lines.Length - 1) * lh / 2;
                        }

                        _itemBox = new Rect(vt.Location.X + dx, vt.Location.Y + dy - vt.TextHeight, sw, vt.TextHeight + (lh * (lines.Length - 1)) + descent);
                        Point c = Globals.View.PaperToDisplay(new Point(_itemBox.X, _itemBox.Y));

                        double bw = Globals.View.PaperToDisplay(_itemBox.Width);
                        double bh = Globals.View.PaperToDisplay(_itemBox.Height);

                        _location.X = c.X - (_textBox.ActualWidth - bw) / 2;
                        _location.Y = c.Y - (_textBox.ActualHeight - bh) / 2;

                        _textBox.SetValue(Canvas.LeftProperty, _location.X);
                        _textBox.SetValue(Canvas.TopProperty, _location.Y);
                    }
                    else
                    {
                        // something bad happened
                    }
                }

                if (FocusManager.GetFocusedElement() != this)
                {
                    if (Globals.Input.InputDevice == 0)
                    {
                        _textBox.Focus(FocusState.Pointer);
                    }
                }
            }
        }

        public void Select()
        {
            if (Globals.Input.InputDevice == 0)
            {
                _textBox.Focus(FocusState.Pointer);
            }
            _textBox.SelectAll();
        }

        public bool Finished
        {
            get
            {
                return _finished;
            }
        }

        void _textBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                Finish();
            }
        }

        public void Finish()
        {
            _ptext.IsVisible = true;

            if (_textBox != null)
            {
                //_ptext.TextStyleChanged();
                _ptext.Draw();

                this.Children.Remove(_textBox);
                _textBox = null;

                if (_ptext.Text.Length == 0)
                {
                    Globals.ActiveDrawing.DeletePrimitive(_ptext);
                }
            }

            if (!_finished)
            {
                Canvas g = Globals.DrawingCanvas.Parent as Canvas;
                g.Children.Remove(this);

                Globals.Input.SelectCursor(CursorType.Draw);

                _finished = true;
            }
        }
    }
}
