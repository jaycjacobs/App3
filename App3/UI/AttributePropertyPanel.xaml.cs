using Cirros;
using Cirros.Actions;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using CirrosCore;
using RedDog.HUIApp;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace CirrosUI
{
    public sealed partial class AttributePropertyPanel : UserControl
    {
        private Primitive _primitive;

        double _minValueColumnWidth = 0;

        public AttributePropertyPanel()
        {
            this.InitializeComponent();

            Initialize();
        }

        private void Initialize()
        {
            this.SizeChanged += AttributePropertyPanel_SizeChanged;

            _layerComboBox.SelectionChanged += _layerComboBox_SelectionChanged;
            _weightNumberComboBox.OnValueChanged += _weightNumberComboBox_OnValueChanged;
            _lineTypeComboBox.SelectionChanged += _lineTypeComboBox_SelectionChanged;
            _colorComboBox.SelectionChanged += _colorComboBox_SelectionChanged;
            _colorComboBox.DropDownClosed += _colorComboBox_DropDownClosed;
            _fillComboBox.SelectionChanged += _fillComboBox_SelectionChanged;
            _fillComboBox.DropDownClosed += _fillComboBox_DropDownClosed;
            _fillRuleCombo.SelectionChanged += _fillRuleCombo_SelectionChanged;
            _endStyleCombo.SelectionChanged += _endStyleCombo_SelectionChanged;

            _fillPatternCombo.SelectionChanged += _fillPatternCombo_SelectionChanged;
            _fillPatternCombo.DropDownClosed += _fillPatternCombo_DropDownClosed;
            _fillPatternScale.OnValueChanged += _fillPatternScale_OnValueChanged;
            _fillPatternAngle.OnValueChanged += _fillPatternAngle_OnValueChanged;

            _radiusBox.OnValueChanged += _radiusBox_OnValueChanged;
            _startBox.OnValueChanged += _startBox_OnValueChanged;
            _includedBox.OnValueChanged += _includedBox_OnValueChanged;

            _axisAngleBox.OnValueChanged += _axisAngleBox_OnValueChanged;

            _majorBox.OnValueChanged += _majorBox_OnValueChanged;
            _minorBox.OnValueChanged += _minorBox_OnValueChanged;
            _widthBox.OnValueChanged += _widthBox_OnValueChanged;
            _heightBox.OnValueChanged += _heightBox_OnValueChanged;

            _textStyleCombo.SelectionChanged += _textStyleCombo_SelectionChanged;
            _alignmentCombo.SelectionChanged += _alignmentCombo_SelectionChanged;
            _positionCombo.SelectionChanged += _positionCombo_SelectionChanged;
            _fontHeightBox.OnValueChanged += _fontHeightBox_OnValueChanged;
            _lineSpacingBox.OnValueChanged += _lineSpacingBox_OnValueChanged;
            _spacingBox.OnValueChanged += _spacingBox_OnValueChanged;
            _rotationBox.OnValueChanged += _rotationBox_OnValueChanged;

            _textBox.OnExpandoTextChanged += _textBox_OnExpandoTextChanged;

            _arrowStyleCombo.SelectionChanged += _arrowStyleCombo_SelectionChanged;
            _placementCombo.SelectionChanged += _placementCombo_SelectionChanged;

            _dimstyleCombo.SelectionChanged += _dimstyleCombo_SelectionChanged;

            _showDimExtensionCB.Checked += dimCheckAction;
            _showDimExtensionCB.Unchecked += dimCheckAction;
            _showDimTextCB.Checked += dimCheckAction;
            _showDimTextCB.Unchecked += dimCheckAction;

            _opacitySlider.ValueChanged += _opacitySlider_ValueChanged;
        }

        private void _lineSpacingBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PText)
            {
                PText pt = _primitive as PText;

                if (_lineSpacingBox.Text.Length == 0)
                {
                    _lineSpacingBox.Value = 0;
                }

                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetLineSpacing, pt, pt.LineSpacing);

                    pt.LineSpacing = _lineSpacingBox.Value;
                    UpdatePrimitive(_primitive);
                }
            }
        }

        private void _fillPatternCombo_DropDownClosed(object sender, object e)
        {

        }

        private void _fillPatternAngle_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            //if (_primitive is PPolygon poly)
            {
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetPatternAngle, _primitive, _primitive.PatternAngle);
                _primitive.PatternAngle = _fillPatternAngle.Value;
                UpdatePrimitive(_primitive);
            }
        }

        private void _fillPatternScale_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            //if (_primitive is PPolygon poly)
            {
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetPatternScale, _primitive, _primitive.PatternScale);
                _primitive.PatternScale = _fillPatternScale.Value;
                UpdatePrimitive(_primitive);
            }
        }

        private void _fillPatternCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (_primitive is PPolygon poly)
            {
                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetPattern, _primitive, _primitive.FillPattern);
                    if (_fillPatternCombo.SelectedIndex == 0)
                    {
                        _primitive.FillPattern = "Solid";
                    }
                    else if (_fillPatternCombo.SelectedItem is string name)
                    {
                        _primitive.FillPattern = name;
                    }
                    UpdatePrimitive(_primitive);
                }
            }
        }

        public AttributePropertyPanel(Primitive primitive)
        {
            this.InitializeComponent();
            Initialize();

            Primitive = primitive;
        }

        DateTime _opacitySliderTime = DateTime.MinValue;
        TimeSpan _opacitySliderTimeInterval = new TimeSpan(0, 0, 0, 0, 500);

        private void _opacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_primitive is PImage)
            {
                if (_programaticUpdate == false)
                {
                    if ((DateTime.Now - _opacitySliderTime) > _opacitySliderTimeInterval)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.SetOpacity, _primitive, ((PImage)_primitive).Opacity);
                    }
                    _opacitySliderTime = DateTime.Now;
                }
                ((PImage)_primitive).Opacity = _opacitySlider.Value;
            }
        }

        private void dimCheckAction(object sender, RoutedEventArgs e)
        {
            if (_primitive is PDimension)
            {
                PDimension pd = _primitive as PDimension;

                if (sender == _showDimTextCB)
                {
                    if (_programaticUpdate == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.ShowDimensionText, pd, pd.ShowText);
                        pd.ShowText = (bool)_showDimTextCB.IsChecked;
                    }
                }
                else if (sender == _showDimExtensionCB)
                {
                    if (_programaticUpdate == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.ShowDimensionExtension, pd, pd.ShowExtension);
                        pd.ShowExtension = (bool)_showDimExtensionCB.IsChecked;
                    }
                }
                UpdatePrimitive(_primitive);
            }
        }

        private void _textBox_OnExpandoTextChanged(object sender, ExpandoTextChangedEventArgs e)
        {
            if (_primitive is PText)
            {
                PText pt = _primitive as PText;
                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.EditText, pt, pt.Text);
                    pt.Text = e.Text;
                }
                UpdatePrimitive(_primitive);
            }
        }

        private void _textStyleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextStyle style = _textStyleCombo.SelectedItem as TextStyle;
            if (style != null)
            {
                if (_primitive is PText)
                {
                    PText pt = _primitive as PText;
                    if (_programaticUpdate == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.SetTextStyle, pt, pt.TextStyleId);
                        pt.TextStyleId = style.Id;
                        UpdatePrimitive(_primitive);
                    }
                }
                else if (_primitive is PDimension)
                {
                    PDimension pd = _primitive as PDimension;
                    if (_programaticUpdate == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.SetTextStyle, pd, pd.TextStyleId);
                        pd.TextStyleId = style.Id;
                        UpdatePrimitive(_primitive);
                    }
                }
            }
        }

        private void _arrowStyleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ArrowStyle style = _arrowStyleCombo.SelectedItem as ArrowStyle;
            if (style != null)
            {
                if (_primitive is PArrow)
                {
                    PArrow pa = _primitive as PArrow;
                    if (_programaticUpdate == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.SetArrowStyle, pa, pa.ArrowStyleId);
                        pa.ArrowStyleId = style.Id;
                        UpdatePrimitive(_primitive);
                    }
                }
                else if (_primitive is PDimension)
                {
                    PDimension pd = _primitive as PDimension;
                    if (_programaticUpdate == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.SetArrowStyle, pd, pd.ArrowStyleId);
                        pd.ArrowStyleId = style.Id;
                        UpdatePrimitive(_primitive);
                    }
                }
            }
        }

        private void _placementCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_primitive is PArrow)
            {
                PArrow pa = _primitive as PArrow;
                ArrowLocation placement;
                switch (_placementCombo.SelectedIndex)
                {
                    case 0:
                    default:
                        placement = ArrowLocation.Start;
                        break;

                    case 1:
                        placement = ArrowLocation.End;
                        break;

                    case 2:
                        placement = ArrowLocation.Both;
                        break;
                }

                if (pa.ArrowLocation != placement)
                {
                    if (_programaticUpdate == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.SetArrowLocation, pa, pa.ArrowLocation);
                        pa.ArrowLocation = placement;
                        UpdatePrimitive(_primitive);
                    }
                }
            }
        }

        private void _dimstyleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_primitive is PDimension)
            {
                ComboBoxItem item = _dimstyleCombo.SelectedItem as ComboBoxItem;
                if (item != null && item.Tag is PDimension.DimType)
                {
                    PDimension pd = _primitive as PDimension;
                    if (_programaticUpdate == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.SetDimensionType, pd, pd.DimensionType);
                        pd.DimensionType = (PDimension.DimType)item.Tag;
                        UpdatePrimitive(_primitive);
                    }
                }
            }
        }

        private void _alignmentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_primitive is PText)
            {
                PText pt = _primitive as PText;
                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetAlignment, pt, pt.Alignment);
                    if (_alignmentCombo.SelectedIndex == 0)
                    {
                        pt.Alignment = TextAlignment.Left;
                    }
                    else if (_alignmentCombo.SelectedIndex == 1)
                    {
                        pt.Alignment = TextAlignment.Center;
                    }
                    else
                    {
                        pt.Alignment = TextAlignment.Right;
                    }
                    UpdatePrimitive(_primitive);
                }
            }
        }

        private void _positionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_primitive is PText)
            {
                PText pt = _primitive as PText;
                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetPosition, pt, pt.Position);
                    if (_positionCombo.SelectedIndex == 0)
                    {
                        pt.Position = TextPosition.Above;
                    }
                    else if (_positionCombo.SelectedIndex == 1)
                    {
                        pt.Position = TextPosition.On;
                    }
                    else
                    {
                        pt.Position = TextPosition.Below;
                    }
                    UpdatePrimitive(_primitive);
                }
            }
        }

        private void _fontHeightBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PText)
            {
                PText pt = _primitive as PText;

                if (_fontHeightBox.Text.Length == 0)
                {
                    _fontHeightBox.Value = 0;
                }

                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetHeight, pt, pt.Size);

                    pt.Size = Globals.ActiveDrawing.UserToPaper(_fontHeightBox.Value);
                    UpdatePrimitive(_primitive);
                }
            }
        }

        private void _spacingBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PText)
            {
                PText pt = _primitive as PText;

                if (_spacingBox.Text.Length == 0)
                {
                    _spacingBox.Value = 0;
                }

                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetSpacing, pt, pt.CharacterSpacing);

                    pt.CharacterSpacing = _spacingBox.Value;
                    UpdatePrimitive(_primitive);
                }
            }
        }

        private void _rotationBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PText)
            {
                PText pt = _primitive as PText;

                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetAngle, pt, pt.Angle);

                    pt.Angle = -_rotationBox.Value;
                    UpdatePrimitive(_primitive);
                }
            }
        }

        private void _axisAngleBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PEllipse)
            {
                PEllipse pel = _primitive as PEllipse;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetAngle, pel, pel.AxisAngle);
                pel.AxisAngle = _axisAngleBox.Value / Construct.cRadiansToDegrees;
                UpdatePrimitive(_primitive);
            }
        }

        private void _includedBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PArc)
            {
                PArc parc = _primitive as PArc;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetIncludedAngle, parc, parc.IncludedAngle);
                parc.IncludedAngle = _includedBox.Value / Construct.cRadiansToDegrees;
                UpdatePrimitive(_primitive);

                if (parc.IsCircle)
                {
                    _startLabel.Visibility = Visibility.Collapsed;
                    _startBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _startLabel.Visibility = Visibility.Visible;
                    _startBox.Visibility = Visibility.Visible;
                }
            }
            else if (_primitive is PEllipse)
            {
                PEllipse pel = _primitive as PEllipse;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetIncludedAngle, pel, pel.IncludedAngle);
                pel.IncludedAngle = _includedBox.Value / Construct.cRadiansToDegrees;
                UpdatePrimitive(_primitive);

                double included = Math.Abs(pel.IncludedAngle);

                if (included < .001 || Math.Abs(included - Math.PI * 2) < .001)
                {
                    _startLabel.Visibility = Visibility.Collapsed;
                    _startBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _startLabel.Visibility = Visibility.Visible;
                    _startBox.Visibility = Visibility.Visible;
                }
            }
        }

        private void _startBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PArc)
            {
                PArc parc = _primitive as PArc;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetStartAngle, parc, parc.StartAngle);
                parc.StartAngle = _startBox.Value / Construct.cRadiansToDegrees;
                UpdatePrimitive(_primitive);
            }
            else if (_primitive is PEllipse)
            {
                PEllipse pel = _primitive as PEllipse;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetStartAngle, pel, pel.StartAngle);
                pel.StartAngle = _startBox.Value / Construct.cRadiansToDegrees;
                UpdatePrimitive(_primitive);
            }
        }

        private void _radiusBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PArc)
            {
                PArc parc = _primitive as PArc;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetRadius, parc, parc.Radius);
                parc.Radius = Globals.ActiveDrawing.ModelToPaper(_radiusBox.Value); // model unit
                UpdatePrimitive(_primitive);
            }
            else if (_primitive is PLine)
            {
                PLine pline = _primitive as PLine;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetRadius, pline, pline.Radius);
                pline.Radius = Globals.ActiveDrawing.ModelToPaper(_radiusBox.Value); // model unit
                UpdatePrimitive(_primitive);
            }
        }


        private void _heightBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PRectangle)
            {
                PRectangle prect = _primitive as PRectangle;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetHeight, prect, prect.Height);
                Point d = Globals.ActiveDrawing.ModelToPaperDelta(new Point(_widthBox.Value, _heightBox.Value));
                prect.Height = d.Y;
                UpdatePrimitive(_primitive);
            }
        }

        private void _widthBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PRectangle)
            {
                PRectangle prect = _primitive as PRectangle;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetWidth, prect, prect.Width);
                prect.Width = Globals.ActiveDrawing.ModelToPaper(_widthBox.Value);
                UpdatePrimitive(_primitive);
            }
            else if (_primitive is PDoubleline)
            {
                PDoubleline pdo = _primitive as PDoubleline;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetWidth, pdo, pdo.Width);
                pdo.Width = Globals.ActiveDrawing.ModelToPaper(_widthBox.Value);
                UpdatePrimitive(_primitive);
            }
        }

        private void _minorBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PEllipse)
            {
                PEllipse pel = _primitive as PEllipse;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetHeight, pel, pel.Minor);
                pel.Minor = Globals.ActiveDrawing.ModelToPaper(_minorBox.Value / 2); // model unit
                UpdatePrimitive(_primitive);
            }
        }

        private void _majorBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_primitive is PEllipse)
            {
                PEllipse pel = _primitive as PEllipse;
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetWidth, pel, pel.Major);
                pel.Major = Globals.ActiveDrawing.ModelToPaper(_majorBox.Value / 2); // model unit
                UpdatePrimitive(_primitive);
            }
        }
        void _fillRuleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_primitive is PPolygon)
            {
                bool fillEvenOdd = _fillRuleCombo.SelectedIndex != 0;

                PPolygon poly = _primitive as PPolygon;
                if (_primitive.FillEvenOdd != fillEvenOdd)
                {
                    if (_programaticUpdate == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.SetFillRule, poly, poly.FillEvenOdd);
                        poly.FillEvenOdd = fillEvenOdd;
                        UpdatePrimitive(_primitive);
                    }
                }
            }
        }

        private void _endStyleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_primitive is PDoubleline)
            {
                PDoubleline pdo = _primitive as PDoubleline;
                DbEndStyle es;
                switch (_endStyleCombo.SelectedIndex)
                {
                    case 0:
                    default:
                        es = DbEndStyle.None;
                        break;

                    case 1:
                        es = DbEndStyle.Start;
                        break;

                    case 2:
                        es = DbEndStyle.End;
                        break;

                    case 3:
                        es = DbEndStyle.Both;
                        break;
                }

                if (pdo.EndStyle != es)
                {
                    if (_programaticUpdate == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.SetEndStyle, pdo, pdo.EndStyle);
                        pdo.EndStyle = es;
                        UpdatePrimitive(_primitive);
                    }
                }
            }
        }

        private void _weightNumberComboBox_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            int newLineWeight = -2; 

            if (e.Value <= .001)
            {
                newLineWeight = -1;
            }
            else
            {
                newLineWeight = (int)(e.Value * 1000);
            }

            if (newLineWeight < 0)
            {
                _weightNumberComboBox.SelectedIndex = 0;
            }
            else
            {
                _weightNumberComboBox.Value = (double)newLineWeight / 1000;
            }

            if (_programaticUpdate == false)
            {
                Globals.CommandDispatcher.AddUndoableAction(ActionID.SetLineWeight, _primitive, _primitive.LineWeightId);

                _primitive.LineWeightId = newLineWeight;    // user units
                UpdatePrimitive(_primitive);
            }
        }

        private void PopulateLineWeightControl()
        {
            Dictionary<string, object> weights = new Dictionary<string, object>() {
                { "Use layer thickness", "Use layer thickness" },
                { ".003", .003 },
                { ".005", .005 },
                { ".007", .007 },
                { ".010", .010 },
                { ".012", .012 },
                { ".014", .014 },
                { ".024", .024 },
                { ".031", .031 },
                { ".035", .035 },
                { ".039", .039 },
                { ".042", .042 },
                { ".047", .047 },
                { ".056", .056 },
                { ".062", .062 },
                { ".078", .078 },
                { ".083", .083 },
                { ".096", .096 },
                { ".112", .112 }
            };

            _weightNumberComboBox.SetValues(weights, true);
            _weightNumberComboBox.MinValue = .001;
        }

        public Primitive Primitive
        {
            get
            {
                return _primitive;
            }
            set
            {
                _primitive = value;

                if (_primitive == null)
                {

                }
                else
                {

                    Populate();
                    Update();

                    int ptype = (int)_primitive.TypeName;
                    int bit = 1 << ptype;

                    foreach (FrameworkElement fe in _grid.Children)
                    {
                        if (fe.Tag is string)
                        {
                            try
                            {
                                string hs = fe.Tag as string;
                                if (hs.StartsWith("0x"))
                                {
                                    int mask = int.Parse(hs.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
                                    if ((bit & mask) == 0)
                                    {
                                        fe.Visibility = Visibility.Collapsed;
                                    }
                                    else
                                    {
                                        fe.Visibility = Visibility.Visible;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
        }

        private void AttributePropertyPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Cirros.Utility.Utilities.__checkSizeChanged(23, sender)) return;

            if (Parent is ContentControl)
            {
                ContentControl cc = Parent as ContentControl;
                if (cc.ActualWidth != 0 && (double.IsNaN(Width) || Width < cc.ActualWidth))
                {
                    Width = cc.ActualWidth;
                }
            }

            if (_weightNumberComboBox.ActualHeight != 0 && _lineTypeComboBox.Height != _weightNumberComboBox.ActualHeight)
            {
                _weightNumberComboBox.Height = _lineTypeComboBox.ActualHeight;
            }

            if (_valueColumn.ActualWidth > _minValueColumnWidth)
            {
                _minValueColumnWidth = _valueColumn.ActualWidth;
            }

            if (_valueColumn.MinWidth < _minValueColumnWidth)
            {
                _valueColumn.MinWidth = _minValueColumnWidth;
            }
        }

        public double ValueColumnWidth
        {
            get { return _minValueColumnWidth; }
            set
            {
                if (value > _minValueColumnWidth)
                {
                    _minValueColumnWidth = value;
                    _valueColumn.MinWidth = _minValueColumnWidth;
                }
            }
        }

        void _colorComboBox_DropDownClosed(object sender, object e)
        {
            UpdateColorList();
        }

        private void _fillComboBox_DropDownClosed(object sender, object e)
        {
            UpdateFillList();
        }

        void Populate()
        {
            if (_primitive != null)
            {
                _zIndexBox.Text = _primitive.ZIndex.ToString();

                if (_layerComboBox != null)
                {
                    _layerComboBox.Items.Clear();

                    if (Globals.LayerTable != null && Globals.LayerTable.Count > 0)
                    {
                        List<Layer> list = new List<Layer>();
                        foreach (Layer layer in Globals.LayerTable.Values)
                        {
                            if (layer.Id != 0)
                            {
                                list.Add(layer);
                            }
                        }
                        list.Sort();
                        list.Insert(0, Globals.LayerTable[0]);

                        foreach (Layer layer in list)
                        {
                            LayerTile lai = new LayerTile(layer);
                            _layerComboBox.Items.Add(lai);
                        }
                    }
                }

                if (_lineTypeComboBox != null)
                {
                    _lineTypeComboBox.Items.Clear();

                    TextBlock tb = new TextBlock();
                    tb.Text = "Use layer line type";
                    tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
                    tb.Style = (Style)(Application.Current.Resources["SettingsTextSmallNoMargin"]);
                    tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                    _lineTypeComboBox.Items.Add(tb);

                    if (Globals.LineTypeTable != null)
                    {
                        for (int i = 0; i < Globals.LineTypeTable.Count; i++)
                        {
                            _lineTypeComboBox.Items.Add(Globals.LineTypeTable[i]);
                        }
                    }
                }

                if (_colorComboBox != null)
                {
                    _colorComboBox.Items.Clear();

                    TextBlock tb = new TextBlock();
                    tb.Text = "Use layer color";
                    tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
                    tb.Style = (Style)(Application.Current.Resources["SettingsTextSmallNoMargin"]);
                    tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                    _colorComboBox.Items.Add(tb);

                    tb = new TextBlock();
                    tb.Text = "Select a new color";
                    tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
                    tb.Style = (Style)(Application.Current.Resources["SettingsTextSmallNoMargin"]);
                    tb.Tag = (uint)ColorCode.SetColor;
                    tb.FontSize = Globals.UIDataContext.UIFontSizeNormal;
                    _colorComboBox.Items.Add(tb);

                    Globals.PushRecentColor(_primitive.ColorSpec);

                    foreach (uint colorspec in Globals.RecentColors)
                    {
                        _colorComboBox.Items.Add(new ColorItemControl(colorspec));
                    }
                }

                if (_fillPatternCombo != null)
                {
                    List<string> list = new List<string>();
                    foreach (CrosshatchPattern pattern in Patterns.PatternDictionary.Values)
                    {
                        list.Add(pattern.Name);
                    }

                    //list.Sort();
                    list.Insert(0, "Solid");

                    _programaticUpdate = true;
                    _fillPatternCombo.ItemsSource = list;
                    _programaticUpdate = false;
                }

                PopulateLineWeightControl();

                if (_primitive is PText || _primitive is PDimension)
                {
                    _textStyleCombo.Items.Clear();

                    foreach (TextStyle style in Globals.TextStyleTable.Values)
                    {
                        _textStyleCombo.Items.Add(style);
                    }
                }

                _fontHeightBox.Unit = Globals.ActiveDrawing.PaperUnit;

                if (_primitive is PArrow || _primitive is PDimension)
                {
                    _arrowStyleCombo.Items.Clear();

                    foreach (ArrowStyle style in Globals.ArrowStyleTable.Values)
                    {
                        _arrowStyleCombo.Items.Add(style);
                    }
                }

                if (_primitive is PInstance)
                {
                    PInstance pi = _primitive as PInstance;
                }
            }
        }

        bool _programaticUpdate = false;

        public void Update()
        {
            if (_primitive == null)
            {
                return;
            }
            _programaticUpdate = true;

            foreach (LayerTile tile in _layerComboBox.Items)
            {
                if (tile.Layer.Id == _primitive.LayerId)
                {
                    _layerComboBox.SelectedItem = tile;
                    break;
                }
            }

            if (_primitive.LineTypeId == -1)
            {
                _lineTypeComboBox.SelectedIndex = 0;
            }
            else
            {
                _lineTypeComboBox.SelectedItem = Globals.LineTypeTable[_primitive.LineTypeId];
            }

            if (_primitive.LineWeightId < 1)
            {
                _weightNumberComboBox.SelectedIndex = 0;
            }
            else
            {
                _weightNumberComboBox.Value = (double)_primitive.LineWeightId / 1000;
            }

            if (_primitive.ColorSpec == (uint)ColorCode.ByLayer)
            {
                _colorComboBox.SelectedIndex = 0;
            }
            else if (_primitive.ColorSpec == (uint)ColorCode.ThemeForeground)
            {
                // Hack #1: if the object color is theme foreground, set the color to bylayer
                // proper fix is to support theme foreground in the control
                // overrides the block below
                _colorComboBox.SelectedIndex = 0;
            }
            else
            {
                if (_primitive.ColorSpec == (uint)ColorCode.ThemeForeground)
                {
                    // Hack #2 (overridden by Hack #1): if the object color is theme foreground, use the color
                    // this case is currently handled in the block above
                    _primitive.ColorSpec = Utilities.ColorSpecFromColor(Globals.ActiveDrawing.Theme.ForegroundColor);
                }

                int index = GetColorIndex(_primitive.ColorSpec);
                if (index < 0)
                {
                    // The selected color is not in the recent colors list
                    // push the color and set the index to the top color
                    Globals.PushRecentColor(_primitive.ColorSpec);
                    UpdateColorList();
                    _colorComboBox.SelectedIndex = 2;
                }
                else
                {
                    _colorComboBox.SelectedIndex = index;
                }
            }

            _fillComboBox.ColorSpec = _primitive.Fill;

            //if (_primitive is PPolygon poly)
            {
                _fillRuleCombo.SelectedIndex = _primitive.FillEvenOdd ? 1 : 0;
                _fillPatternScale.Value = _primitive.PatternScale;
                _fillPatternAngle.Value = _primitive.PatternAngle;
                _fillPatternCombo.SelectedItem = _primitive.FillPattern == null ? "Solid" : _primitive.FillPattern;
            }

            if (_primitive is PLine)
            {
                PLine pline = _primitive as PLine;
                _radiusBox.Value = Globals.ActiveDrawing.PaperToModel(pline.Radius);
            }

            if (_primitive is PArc)
            {
                PArc parc = _primitive as PArc;
                _radiusBox.Value = Globals.ActiveDrawing.PaperToModel(parc.Radius);
                _startBox.Value = parc.StartAngle * Construct.cRadiansToDegrees;
                if (parc.IncludedAngle == 0)
                {
                    _includedBox.Value = 360;
                }
                else
                {
                    _includedBox.Value = parc.IncludedAngle * Construct.cRadiansToDegrees;
                }
            }

            if (_primitive is PRectangle)
            {
                PRectangle prect = _primitive as PRectangle;
                Point d = Globals.ActiveDrawing.PaperToModelDelta(new Point(prect.Width, prect.Height));
                _widthBox.Value = d.X;
                _heightBox.Value = d.Y;
            }

            if (_primitive is PDoubleline)
            {
                PDoubleline pdo = _primitive as PDoubleline;
                _widthBox.Value = Globals.ActiveDrawing.PaperToModel(pdo.Width);
                switch (pdo.EndStyle)
                {
                    case DbEndStyle.None:
                        _endStyleCombo.SelectedIndex = 0;
                        break;

                    case DbEndStyle.Start:
                        _endStyleCombo.SelectedIndex = 1;
                        break;

                    case DbEndStyle.End:
                        _endStyleCombo.SelectedIndex = 2;
                        break;

                    case DbEndStyle.Both:
                        _endStyleCombo.SelectedIndex = 3;
                        break;
                }
            }

            if (_primitive is PEllipse)
            {
                PEllipse pel = _primitive as PEllipse;
                _startBox.Value = pel.StartAngle * Construct.cRadiansToDegrees;
                _axisAngleBox.Value = pel.AxisAngle * Construct.cRadiansToDegrees;

                if (pel.IncludedAngle == 0)
                {
                    _includedBox.Value = 360;
                }
                else
                {
                    _includedBox.Value = pel.IncludedAngle * Construct.cRadiansToDegrees;
                }
                _majorBox.Value = Globals.ActiveDrawing.PaperToModel(pel.Major * 2);
                _minorBox.Value = Globals.ActiveDrawing.PaperToModel(pel.Minor * 2);
            }

            if (_primitive is PText)
            {
                PText pt = _primitive as PText;
                for (int i = 0; i < _textStyleCombo.Items.Count; i++)
                {
                    TextStyle ts = _textStyleCombo.Items[i] as TextStyle;
                    if (ts != null && ts.Id == pt.TextStyleId)
                    {
                        _textStyleCombo.SelectedIndex = i;
                        break;
                    }
                }

                switch (pt.Alignment)
                {
                    default:
                    case TextAlignment.Left:
                        _alignmentCombo.SelectedIndex = 0;
                        break;
                    case TextAlignment.Center:
                        _alignmentCombo.SelectedIndex = 1;
                        break;
                    case TextAlignment.Right:
                        _alignmentCombo.SelectedIndex = 2;
                        break;
                }

                switch (pt.Position)
                {
                    default:
                    case TextPosition.Above:
                        _positionCombo.SelectedIndex = 0;
                        break;
                    case TextPosition.On:
                        _positionCombo.SelectedIndex = 1;
                        break;
                    case TextPosition.Below:
                        _positionCombo.SelectedIndex = 2;
                        break;
                }

                if (pt.P1.X == pt.P2.X && pt.P1.Y == pt.P2.Y)
                {
                    _rotationBox.Value = -pt.Angle;// *Construct.cRadiansToDegrees;
                    _rotationBox.IsEnabled = !pt.IsInstanceMember;
                    _rotationBox.FontStyle = Windows.UI.Text.FontStyle.Normal;
                }
                else
                {
                    //_rotationBox.Value = -Construct.Angle(pt.P1, pt.P2) * Construct.cRadiansToDegrees;
                    _rotationBox.Text = "By vector";
                    _rotationBox.IsEnabled = false;
                    _rotationBox.FontStyle = Windows.UI.Text.FontStyle.Italic;
                }

                _fontHeightBox.Value = Globals.ActiveDrawing.PaperToUser(pt.Size);
                _lineSpacingBox.Value = pt.LineSpacing;
                _spacingBox.Value = pt.CharacterSpacing;

                _textBox.Text = pt.Text;
            }

            if (_primitive is PArrow)
            {
                PArrow pa = _primitive as PArrow;
                for (int i = 0; i < _arrowStyleCombo.Items.Count; i++)
                {
                    ArrowStyle style = _arrowStyleCombo.Items[i] as ArrowStyle;
                    if (style != null && style.Id == pa.ArrowStyleId)
                    {
                        _arrowStyleCombo.SelectedIndex = i;
                        break;
                    }
                }

                switch (pa.ArrowLocation)
                {
                    default:
                    case ArrowLocation.Start:
                        _placementCombo.SelectedIndex = 0;
                        break;

                    case ArrowLocation.End:
                        _placementCombo.SelectedIndex = 1;
                        break;

                    case ArrowLocation.Both:
                        _placementCombo.SelectedIndex = 2;
                        break;
                }
            }

            if (_primitive is PDimension)
            {
                PDimension pd = _primitive as PDimension;

                for (int i = 0; i < _textStyleCombo.Items.Count; i++)
                {
                    TextStyle ts = _textStyleCombo.Items[i] as TextStyle;
                    if (ts != null && ts.Id == pd.TextStyleId)
                    {
                        _textStyleCombo.SelectedIndex = i;
                        break;
                    }
                }

                for (int i = 0; i < _arrowStyleCombo.Items.Count; i++)
                {
                    ArrowStyle style = _arrowStyleCombo.Items[i] as ArrowStyle;
                    if (style != null && style.Id == pd.ArrowStyleId)
                    {
                        _arrowStyleCombo.SelectedIndex = i;
                        break;
                    }
                }

                _dimstyleCombo.Items.Clear();

                switch (pd.DimensionType)
                {
                    case PDimension.DimType.PointToPoint:
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Point to point", PDimension.DimType.PointToPoint));
                        _dimstyleCombo.SelectedIndex = 0;
                        break;
                    case PDimension.DimType.Baseline:
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Baseline", PDimension.DimType.Baseline));
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Incremental", PDimension.DimType.Incremental));
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Outside", PDimension.DimType.Outside));
                        _dimstyleCombo.SelectedIndex = 0;
                        break;
                    case PDimension.DimType.Incremental:
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Baseline", PDimension.DimType.Baseline));
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Incremental", PDimension.DimType.Incremental));
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Outside", PDimension.DimType.Outside));
                        _dimstyleCombo.SelectedIndex = 1;
                        break;
                    case PDimension.DimType.Outside:
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Baseline", PDimension.DimType.Baseline));
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Incremental", PDimension.DimType.Incremental));
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Outside", PDimension.DimType.Outside));
                        _dimstyleCombo.SelectedIndex = 2;
                        break;
                    case PDimension.DimType.BaselineAngular:
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Baseline angular", PDimension.DimType.BaselineAngular));
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Incremental angular", PDimension.DimType.IncrementalAngular));
                        _dimstyleCombo.SelectedIndex = 0;
                        break;
                    case PDimension.DimType.IncrementalAngular:
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Baseline angular", PDimension.DimType.BaselineAngular));
                        _dimstyleCombo.Items.Add(NewComboBoxItem("Incremental angular", PDimension.DimType.IncrementalAngular));
                        _dimstyleCombo.SelectedIndex = 1;
                        break;
                }

                _showDimTextCB.IsChecked = pd.ShowText;
                _showDimExtensionCB.IsChecked = pd.ShowExtension;
            }

            if (_primitive is PImage)
            {
                PImage pi = _primitive as PImage;
                _opacitySlider.Value = pi.Opacity;
                _imageName.Text = pi.SourceName;
            }

            _programaticUpdate = false;
        }

        private ComboBoxItem NewComboBoxItem(string text, object tag)
        {
            ComboBoxItem item = new ComboBoxItem();
            item.Content = text;
            item.Tag = tag;
            return item;
        }

        private int GetColorIndex(uint colorspec)
        {
            int index = -1;

            int max = Math.Min(_colorComboBox.Items.Count - 2, Globals.RecentColors.Count);

            for (int i = 0; i < max; i++)
            {
                ColorItemControl cic = (ColorItemControl)_colorComboBox.Items[i + 2];
                if (cic.ColorSpec == colorspec)
                {
                    index = i + 2;
                }
            }
            return index;
        }

        void UpdateColorList()
        {
            _programaticUpdate = true;

            int max = Math.Min(_colorComboBox.Items.Count - 2, Globals.RecentColors.Count);

            for (int i = 0; i < max; i++)
            {
                ColorItemControl cic = (ColorItemControl)_colorComboBox.Items[i + 2];
                cic.ColorSpec = Globals.RecentColors[i];
            }

            if (_colorComboBox.SelectedIndex == 1)
            {
                _colorComboBox.SelectedIndex = 2;
            }
            else if (_colorComboBox.SelectedIndex > 2)
            {
                _colorComboBox.SelectedIndex = 2;
            }

            _programaticUpdate = false;
        }

        void UpdateFillList()
        {
            _programaticUpdate = true;

            int max = Math.Min(_fillComboBox.Items.Count - 4, Globals.RecentColors.Count);

            for (int i = 0; i < max; i++)
            {
                ColorItemControl cic = (ColorItemControl)_fillComboBox.Items[i + 4];
                cic.ColorSpec = Globals.RecentColors[i];
            }

            if (_fillComboBox.SelectedIndex < 0 || _fillComboBox.SelectedIndex == 3 || _fillComboBox.SelectedIndex > 4)
            {
                _fillComboBox.SelectedIndex = 4;
            }

            _programaticUpdate = false;
        }

        //void ShowTextPopup()
        //{
        //    _textPopup.IsLightDismissEnabled = true;

        //    TextBox box = new TextBox();
        //    box.MinWidth = _textBox.ActualWidth;
        //    box.MinHeight = _textBox.ActualHeight;

        //    box.Text = "XXXXXXXXXXXXXXX";

        //    _textPopup.HorizontalOffset = (double)_textBox.GetValue(Canvas.LeftProperty);
        //    _textPopup.VerticalOffset = (double)_textBox.GetValue(Canvas.TopProperty);

        //    _textPopup.Child = box;
        //    _textPopup.IsOpen = true;
        //}

        void ShowColorPicker(uint defaultColorSpec, string tag)
        {
            if (Globals.UIVersion == 0)
            {
                _colorPickerPopup.IsLightDismissEnabled = true;

                ColorPicker colorPicker = new ColorPicker();
                colorPicker.Width = 350;
                colorPicker.Tag = tag;

                Brush dark = (Brush)(Application.Current.Resources["SettingsDarkForeground"]);
                colorPicker.ShowBorder(dark, 2);
                colorPicker.Color = Utilities.ColorFromColorSpec(defaultColorSpec);

                colorPicker.OnColorSelected += colorPicker_OnColorSelected;

                if (!_colorPickerPopup.IsOpen)
                {
                    double left = (double)_colorComboBox.GetValue(Canvas.LeftProperty);
                    _colorPickerPopup.HorizontalOffset = left + _colorComboBox.ActualWidth - colorPicker.Width;

                    if (_colorPickerPopup.HorizontalOffset < 10)
                    {
                        _colorPickerPopup.HorizontalOffset = 10;
                    }
                    _colorPickerPopup.Child = colorPicker as UserControl;
                    _colorPickerPopup.IsOpen = true;
                }
            }
            else
            {
                GeneralTransform tf = _layerComboBox.TransformToVisual(null);
                Point t = tf.TransformPoint(new Point());
                if (t.X != 0 && t.Y != 0)
                {
                    HColorDialog panel = new HColorDialog();
                    panel.ColorSpec = defaultColorSpec;

                    Popup popup = new Popup();
                    popup.Tag = tag;
                    popup.IsLightDismissEnabled = false;
                    popup.Child = panel;
                    popup.HorizontalOffset = t.X;
                    popup.VerticalOffset = t.Y;
                    popup.IsOpen = true;
                    popup.Closed += Popup_Closed; ;
                }

            }
        }

        private void Popup_Closed(object sender, object e)
        {
            if (sender is Popup popup && popup.Child is HColorDialog colorDialog)
            {
                Globals.PushRecentColor(colorDialog.ColorSpec);

                if ((string)popup.Tag == "fill")
                {
                    SelectFillColor(colorDialog.ColorSpec);
                }
                else
                {
                    SelectColor(colorDialog.ColorSpec);
                }
            }
        }

        void _colorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_colorComboBox.SelectedIndex == 0)
            {
                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetColor, _primitive, _primitive.ColorSpec);
                    _primitive.ColorSpec = (uint)ColorCode.ByLayer;
                    UpdatePrimitive(_primitive);
                }
            }
            else if (_colorComboBox.Items.Count > 2 && _colorComboBox.Items[2] is ColorItemControl)
            {
                uint colorspec = ((ColorItemControl)_colorComboBox.Items[2]).ColorSpec;

                if (_colorComboBox.SelectedIndex == 1)
                {
                    ShowColorPicker(colorspec, "color");
                }
                else if (_colorComboBox.SelectedItem is ColorItemControl)
                {
                    colorspec = ((ColorItemControl)_colorComboBox.SelectedItem).ColorSpec;

                    if (_primitive.ColorSpec != colorspec)
                    {
                        if (_programaticUpdate == false)
                        {
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.SetColor, _primitive, _primitive.ColorSpec);
                            _primitive.ColorSpec = colorspec;
                            UpdatePrimitive(_primitive);
                        }
                        Globals.PushRecentColor(colorspec);
                    }
                }
            }
        }

        private void UpdatePrimitive(Primitive p)
        {
            if (p.Parent is Primitive)
            {
                UpdatePrimitive(p.Parent as Primitive);
            }
            else if (p.Parent is Group g)
            {
                List<GroupAttribute> newList = new List<GroupAttribute>();

                foreach (Primitive member in g.Items)
                {
                    if (member is PText)
                    {
                        PText pt = member as PText;
                        if (pt.AttributeName != null)
                        {
                            newList.Add(new GroupAttribute(pt.AttributeName, pt.AttributeValue, pt.AttributeLines));
                        }
                    }
                }

                if (newList.Count > 0 || newList.Count != g.AttributeList.Count)
                {
                    g.AttributeList = newList;
                }

                Globals.ActiveDrawing.UpdateGroupInstances(((Group)p.Parent).Name);
            }
            else if (p is Primitive)
            {
                p.Draw();
            }
            _primitive.ClearStaticConstructNodes();
        }

        private void _fillComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_fillComboBox.ColorSpec == (int)ColorCode.SetColor)
            {
                uint colorspec = _fillComboBox.DisplayedColorSpec > (uint)ColorCode.SetColor ?
                    _fillComboBox.DisplayedColorSpec : Utilities.ColorSpecFromColor(Globals.ActiveDrawing.Theme.ForegroundColor);

                ShowColorPicker(colorspec, "fill");
            }
            else if (_primitive.Fill != _fillComboBox.ColorSpec)
            {
                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetFill, _primitive, _primitive.Fill);
                    _primitive.Fill = _fillComboBox.ColorSpec;
                    UpdatePrimitive(_primitive);
                }
            }
        }

        void colorPicker_OnColorSelected(object sender, ColorSelectedEventArgs e)
        {
            if (e.FinalSelection && _colorPickerPopup.IsOpen)
            {
                _colorPickerPopup.IsOpen = false;
            }
        }

        private void _colorPickerPopup_Closed(object sender, object e)
        {
            if (_colorPickerPopup.Child is ColorPicker)
            {
                ColorPicker colorPicker = _colorPickerPopup.Child as ColorPicker;
                if ((string)colorPicker.Tag == "fill")
                {
                    SelectFillColor(colorPicker.Color);
                }
                else
                {
                    SelectColor(colorPicker.Color);
                }
                colorPicker.OnColorSelected -= colorPicker_OnColorSelected;
                _colorPickerPopup.Child = null;
            }
        }

        private void SelectColor(Color color)
        {
            SelectColor(Utilities.ColorSpecFromColor(color));
        }

        private void SelectFillColor(Color color)
        {
            SelectFillColor(Utilities.ColorSpecFromColor(color));
        }

        private void SelectColor(uint colorspec)
        {
            if (_primitive.ColorSpec != colorspec)
            {
                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetColor, _primitive, _primitive.ColorSpec);
                    _primitive.ColorSpec = colorspec;
                    UpdatePrimitive(_primitive);
                }
                Globals.PushRecentColor(colorspec);
            }

            UpdateColorList();
        }

        private void SelectFillColor(uint colorspec)
        {
            if (_primitive.Fill != colorspec)
            {
                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetFill, _primitive, _primitive.Fill);
                    _primitive.Fill = colorspec;
                    UpdatePrimitive(_primitive);
                }
                Globals.PushRecentColor(colorspec);
            }

            UpdateFillList();
        }

        void _lineTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_lineTypeComboBox.Items.Count > 0)
            {
                LineType style = _lineTypeComboBox.SelectedItem as LineType;
                if (style == null)
                {
                    _primitive.LineTypeId = -1;
                }
                else
                {
                    if (_programaticUpdate == false)
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.SetLineType, _primitive, _primitive.LineTypeId);
                        _primitive.LineTypeId = style.Id;
                    }
                }
                UpdatePrimitive(_primitive);
            }
        }

        void _layerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LayerTile tile = _layerComboBox.SelectedItem as LayerTile;
            if (tile != null)
            {
                if (_programaticUpdate == false)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.SetLayer, _primitive, _primitive.LayerId);
                    Layer layer = ((LayerTile)tile).Layer;
                    _primitive.LayerId = layer.Id;
                    UpdatePrimitive(_primitive);
                }
            }
        }

        private void _orderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_primitive != null)
            {
                if (sender == _orderFrontButton)
                {
                    _primitive.ZIndex = Globals.ActiveDrawing.MaxZIndex;
                }
                else if (sender == _orderBackButton)
                {
                    _primitive.ZIndex = Globals.ActiveDrawing.MinZIndex;
                }

                _zIndexBox.Text = _primitive.ZIndex.ToString();
            }

            Globals.ActiveDrawing.ChangeNumber++;
        }

        private void _zIndexBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (_primitive != null)
            {
                _primitive.ZIndex = (int)args.NewValue;
            }
        }
    }
}
