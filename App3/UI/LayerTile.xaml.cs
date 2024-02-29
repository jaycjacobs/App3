using Cirros;
using Cirros.Drawing;
using Cirros.Utility;
using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace CirrosUI
{
    public sealed partial class LayerTile : UserControl
    {
        Layer _layer;

        public LayerTile()
        {
            this.InitializeComponent();
        }

        public LayerTile(Layer layer)
        {
            this.InitializeComponent();
            this.SizeChanged += LayerTile_SizeChanged;
            _layer = layer;

            //_lineSampleBackground.Background = new SolidColorBrush(Globals.ActiveDrawing.Theme.BackgroundColor);
            _lineSampleBackground.Background = new SolidColorBrush(Colors.Transparent);
            _lineSample.Stroke = new SolidColorBrush(Utilities.ColorFromColorSpec(_layer.ColorSpec));
            //_lineSample.StrokeThickness = Globals.DrawingCanvas.PaperToDisplay(_layer.LineWeightId == 0 ? .01 : (double)_layer.LineWeightId / 1000);
            //_lineSample.StrokeThickness = _layer.LineWeightId == 0 ? .01 : (double)_layer.LineWeightId / 10;
            _lineSample.StrokeThickness = _layer.LineWeightId < 5 ? .5 : (double)_layer.LineWeightId / 10;
            _lineSample.Height = _lineSample.StrokeThickness * 2;
            _lineSample.Y1 = _lineSample.Y2 = Math.Round(_lineSample.Height / 2);

            if (Globals.LineTypeTable[_layer.LineTypeId].StrokeDashArray == null)
            {
                _lineSample.StrokeDashArray = null;
            }
            else if (Globals.LineTypeTable[_layer.LineTypeId].StrokeDashArray.Count == 0)
            {
                _lineSample.StrokeDashArray = null;
            }
            else
            {
                DoubleCollection dc = new DoubleCollection();
                foreach (double d in Globals.LineTypeTable[_layer.LineTypeId].StrokeDashArray)
                {
                    dc.Add(d * 72 / _lineSample.StrokeThickness);
                }
                _lineSample.StrokeDashArray = dc;
                _lineSample.StrokeDashOffset = dc[0] > 20 ? dc[0] - 10 : 0;
            }

            _layerName.Text = _layer.Name;
        }

        void LayerTile_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ActualWidth == e.NewSize.Width && this.ActualHeight == e.NewSize.Height)
            {
                return;
            }
            if (Cirros.Utility.Utilities.__checkSizeChanged(37, sender)) return;

            double top = (this.ActualHeight - _lineSample.StrokeThickness / 2) / 2;
            _lineSample.SetValue(Canvas.TopProperty, top);
            _lineSampleBackground.Height = this.Height - 2;
        }

        public Layer Layer
        {
            get
            {
                return _layer;
            }
        }
    }
}
