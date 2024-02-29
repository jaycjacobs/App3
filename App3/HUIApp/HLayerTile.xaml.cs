using Cirros;
using Cirros.Drawing;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace RedDog.HUIApp
{
    public sealed partial class HLayerTile : UserControl
    {
        Layer _layer;
        private uint _colorSpec;
        private int _lineWeightId;
        private int _lineTypeId;
        Brush _layerBrush = new SolidColorBrush(Colors.Black);

        public HLayerTile()
        {
            this.InitializeComponent();
        }

        public HLayerTile(Layer layer)
        {
            Layer = layer;

            this.InitializeComponent();

            this.Loaded += HLayerTile_Loaded;
        }

        private void HLayerTile_Loaded(object sender, RoutedEventArgs e)
        {
            SetLayer(_layer);
        }

        private void SetLayer(Layer layer)
        {
            if (_layerName != null && _layer != null)
            {
                _layerName.Text = _layer.Name;

                _layer = layer;
                _sampleGrid.Background = new SolidColorBrush(Colors.Transparent); // new SolidColorBrush(Globals.ActiveDrawing.Theme.BackgroundColor);
                _sampleGrid.Background.Opacity = 1;

                _layerName.Text = _layer.Name;

                _colorSpec = _layer.ColorSpec;
                _lineWeightId = _layer.LineWeightId;
                _lineTypeId = _layer.LineTypeId;

                _layerBrush = new SolidColorBrush(Utilities.ColorFromColorSpec(_colorSpec));

                _line.Stroke = _layerBrush;
                //_layerName.Foreground = _foregroundBrush;

                //_line.StrokeThickness = Globals.DrawingCanvas.PaperToDisplay(_lineWeightId < 2 ? .002 : (double)_lineWeightId / 1000);
                _line.StrokeThickness = _lineWeightId < 5 ? .5 : (double)_lineWeightId / 10;
                _line.Height = _line.StrokeThickness * 2;
                _line.Y1 = _line.Y2 = Math.Round(_line.Height / 2);

                if (Globals.LineTypeTable[_lineTypeId].StrokeDashArray == null)
                {
                    _line.StrokeDashArray = null;
                }
                else if (Globals.LineTypeTable[_lineTypeId].StrokeDashArray.Count == 0)
                {
                    _line.StrokeDashArray = null;
                }
                else
                {
                    DoubleCollection dc = new DoubleCollection();
                    foreach (double d in Globals.LineTypeTable[_lineTypeId].StrokeDashArray)
                    {
                        dc.Add(d * 72 / _line.StrokeThickness);
                    }
                    _line.StrokeDashArray = dc;
                    _line.StrokeDashOffset = dc[0] > 20 ? dc[0] - 20 : 0;
                }
            }
        }

        public Layer Layer
        {
            get { return _layer; }
            set
            {
                _layer = value;
                SetLayer(_layer);
            }
        }
    }
}
