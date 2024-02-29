using Cirros;
using Cirros.Drawing;
using Cirros.Primitives;
using CirrosCore;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;

namespace CirrosUI
{
    public sealed partial class PatternSwatch : UserControl
    {
        CanvasControl _canvasControl = null;

        double _patternScale = 1;
        double _patternAngle = 0;
        Color _patternColor = Colors.Blue;
        Color _borderColor = Colors.DarkGray;

        CrosshatchPattern _pattern = null;

        List<List<Point>> _hatches = null;

        public PatternSwatch()
        {
            this.InitializeComponent();

            if (_canvasControl == null)
            {
                _canvas.Children.Clear();
                _canvasControl = new CanvasControl();
                _canvasControl.Draw += _canvasControl_Draw;
                _canvas.Children.Add(_canvasControl);
            }

            this.SizeChanged += PatternSwatch_SizeChanged;

            SetPattern("Solid", Colors.WhiteSmoke);
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; }
        }

        private void PatternSwatch_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _canvasControl.Width = e.NewSize.Width;
            _canvasControl.Height = e.NewSize.Height;

            _hatches = null;
        }

        public void SetPattern(string name, Color color, double scale = 1, double angle = 0)
        {
            _patternColor = color;
            _patternScale = scale;
            _patternAngle = angle;

            if (string.IsNullOrEmpty(name) || name == "Solid")
            {
                _pattern = null;
            }
            else
            {
                string key = name.ToLower();
                if (Patterns.PatternDictionary.ContainsKey(key))
                {
                    _pattern = Patterns.PatternDictionary[key];
                }
                else
                {
                    _pattern = null;
                }
            }

            _hatches = null;

            Render();
        }

        public void SetPattern(CrosshatchPattern pattern, Color color, double scale = 1, double angle = 0)
        {
            _pattern = pattern;
            _patternColor = color;
            _patternScale = scale;
            _patternAngle = angle;

            _hatches = null;

            Render();
        }

        private void _canvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_patternColor == Colors.Transparent)
            {
                // no fill
            }
            else if (_pattern == null)
            {
                Rect r = new Rect(0, 0, _canvasControl.Size.Width, _canvasControl.Size.Height);
                args.DrawingSession.FillRectangle(r, _patternColor);
                args.DrawingSession.DrawRectangle(r, _borderColor);
            }
            else 
            {
                //double scale = 1 / args.DrawingSession.Dpi;
                double scale = 1 / Globals.DPI;

                if (_hatches == null)
                {
                    double w = _canvasControl.Size.Width * scale;
                    double h = _canvasControl.Size.Height * scale;

                    List<Point> boundary = new List<Point>
                    {
                        new Point(0, 0),
                        new Point(0, h),
                        new Point(w, h),
                        new Point(w, 0),
                        new Point(0, 0)
                    };

                    args.DrawingSession.DrawRectangle(new Rect(0, 0, _canvasControl.Size.Width, _canvasControl.Size.Height), _borderColor);

                    VectorEntity ve = new VectorEntity(1000, 1000);
                    ve.FillColor = _patternColor;
                    ve.LineWidth = 1;
                    ve.AddChild(boundary);

                    if (_pattern != null && _pattern.Items.Count > 0)
                    {
                        _hatches = PrimitiveUtilities.Crosshatch(ve, _pattern, _patternScale, _patternAngle);
                    }
                }
                if (_hatches == null)
                {
                    //args.DrawingSession.DrawLine(0, 0, (float)_canvasControl.Size.Width, (float)_canvasControl.Size.Height, _patternColor);
                    //args.DrawingSession.DrawLine(0, (float)_canvasControl.Size.Height, (float)_canvasControl.Size.Width, 0, _patternColor);
                }
                else
                {
                    foreach (List<Point> points in _hatches)
                    {
                        for (int i = 1; i < points.Count; i++)
                        {
                            args.DrawingSession.DrawLine((float)(points[i - 1].X / scale), (float)(points[i - 1].Y / scale),
                                (float)(points[i].X / scale), (float)(points[i].Y / scale), _patternColor);
                        }
                    }
                }
            }
        }

        private void Render()
        {
            //System.Diagnostics.Debug.WriteLine("_canvasControl.ActualWidth = {0}, _canvasControl.ActualHeight = {1}", _canvasControl.ActualWidth, _canvasControl.ActualHeight);
            _canvasControl.Invalidate();
        }
    }
}
