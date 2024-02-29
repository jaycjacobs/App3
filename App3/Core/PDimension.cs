using Cirros.Display;
using Cirros.Drawing;
using Cirros.Utility;
using System;
using System.Collections.Generic;
#if UWP
using Cirros.Actions;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
#else
using System.Windows;
using CirrosCore;
using System.Windows.Media;
#endif

namespace Cirros.Primitives
{
    public class PDimension : PLine
    {
        public enum DimType
        {
            Baseline,
            Incremental,
            PointToPoint,
            BaselineAngular,
            IncrementalAngular,
            Outside
        }

        private int _textstyle = 0;
        private int _arrowStyleId = 0;
        private bool _showText = true;
        private bool _showExtension = true;
 
        private DimType _dimType = DimType.Baseline;

        private VectorEntity _rendered_ve = null;

        // Point definition
        // 0: Anchor point
        // 1: Direction
        // 2: Location of first dimension line
        // 3-n: Dimension node

        public PDimension(PDimension original)
            : base(original)
        {
            _textstyle = original._textstyle;
            _dimType = original._dimType;
            _arrowStyleId = original._arrowStyleId;
            _showText = original._showText;
            _showExtension = original._showExtension;
        }

        public PDimension(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            _textstyle = e.TextStyleId;
            _dimType = e.DimensionType;
            _arrowStyleId = e.ArrowStyleId;
            _showText = e.ShowText;
            _showExtension = e.ShowExtension;
        }

        public PDimension(Point s, Point e)
            : base(s)
        {
            Point p = new Point(e.X - s.X, e.Y - s.Y);
            if (p.X != 0 || p.Y != 0)
            {
                _points.Add(p);
            }

            //_layerId = Globals.DimensionLayerId;
            _layerId = ActiveLayer;

            _textstyle = Globals.DimTextStyleId;
            _dimType = Globals.DimensionType;
            _arrowStyleId = Globals.DimArrowStyleId;
            _showText = Globals.ShowDimensionText;
            _showExtension = Globals.ShowDimensionExtension;

            if (Globals.UIVersion > 0)
            {
                _lineWeightId = Globals.DimensionLineWeightId;
                _colorSpec = Globals.DimensionColorSpec;
                _lineTypeId = Globals.DimensionLineTypeId;
            }
        }

        public override Entity Serialize()
        {
            Entity e = base.Serialize();

            e.TextStyleId = _textstyle;
            e.DimensionType = _dimType;
            e.ArrowStyleId = _arrowStyleId;
            e.ShowText = _showText;
            e.ShowExtension = _showExtension;

            return e;
        }

        public override Primitive Clone()
        {
            return new PDimension(this);
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Dimension;
            }
        }

        public override int ActiveLayer
        {
            get
            {
                if (Globals.UIVersion == 0)
                {
                    return Globals.DimensionLayerId;
                }
                else
                {
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveDimensionLayerId))
                    {
                        return Globals.ActiveDimensionLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        public bool ShowText
        {
            get
            {
                return _showText;
            }
            set
            {
                _showText = value;
            }
        }

        public bool ShowExtension
        {
            get
            {
                return _showExtension;
            }
            set
            {
                _showExtension = value;
            }
        }

        public int TextStyleId
        {
            get
            {
                return _textstyle;
            }
            set
            {
                _textstyle = value;
            }
        }


        public DimType DimensionType
        {
            get
            {
                return _dimType;
            }
            set
            {
                _dimType = value;
            }
        }

        public int ArrowStyleId
        {
            get
            {
                return _arrowStyleId;
            }
            set
            {
                _arrowStyleId = value;
            }
        }

        public override List<ConstructNode> DynamicConstructNodes(Point from, Point through)
        {
            System.Diagnostics.Debug.Assert(_dynamicConstructNodes.Count == 0, "DynamicConstructNodes is not zero");
            return _dynamicConstructNodes;
        }

        bool _insertHandleMode = false;

        public bool InsertHandleMode
        {
            get
            {
                return _insertHandleMode;
            }
            set
            {
                _insertHandleMode = value;
            }
        }

        protected override void _drawHandles(Handles handles)
        {
            handles.Attach(this);

            if (_dimType == DimType.PointToPoint)
            {
                int index = 1;

                handles.AddHandle(index++, _origin.X, _origin.Y);

                foreach (Point pt in _points)
                {
                    handles.AddHandle(index++, _origin.X + pt.X, _origin.Y + pt.Y);
                }
            }
            else if (_dimType == DimType.BaselineAngular || _dimType == DimType.IncrementalAngular)
            {
                int index = 1;

                handles.AddHandle(index++, _origin.X, _origin.Y, HandleType.Triangle);

                foreach (Point pt in _points)
                {
                    HandleType ht = index > 3 ? HandleType.Circle : HandleType.Diamond;
                    handles.AddHandle(index++, _origin.X + pt.X, _origin.Y + pt.Y, ht);
                }

                handles.ArrowFrom = 1;
                handles.ArrowTo = 2;
            }
            else
            {
                handles.AddHandle(1, _origin.X, _origin.Y);

                if (_insertHandleMode)
                {
                    for (int i = 2; i < _points.Count; i++)
                    {
                        handles.AddHandle(i + 2, _origin.X + _points[i].X, _origin.Y + _points[i].Y);
                    }
                    handles.Connect = true;
                }
                else
                {
                    int index = 2;

                    foreach (Point pt in _points)
                    {
                        HandleType ht = index > 3 ? HandleType.Circle : HandleType.Diamond;
                        handles.AddHandle(index++, _origin.X + pt.X, _origin.Y + pt.Y, ht);
                    }

                    handles.ArrowFrom = 1;
                    handles.ArrowTo = 2;
                }
            }

            handles.Draw();
        }

        public override void InsertHandlePoint(int beforeHandleId, CPoint cp)
        {
            if (beforeHandleId < 4)
            {
                beforeHandleId = 4;
            }

            base.InsertHandlePoint(beforeHandleId, cp);
        }

        public override List<ConstructNode> ConstructNodes
        {
            get
            {
#if true//UWP
                if (_constructEnabled && _staticConstructNodes.Count == 0)
                {
                    if (_ve == null)
                    {
                        Vectorize(new VectorContext(false, false, false));
                    }

                    if (_ve.Children != null)
                    {
                        foreach (object o in _ve.Children)
                        {
                            if (o is List<Point>)
                            {
                                List<Point> pc = o as List<Point>;

                                if (pc.Count > 2)
                                {
                                    _staticConstructNodes.Add(new ConstructNode(pc[0], "anchor"));
                                    _staticConstructNodes.Add(new ConstructNode(pc[1], "direction point"));

                                    for (int i = 2; i < pc.Count; i++)
                                    {
                                        _staticConstructNodes.Add(new ConstructNode(pc[i], "node"));
                                    }
                                }
                            }
                        }
                    }
                }
#endif
                return _constructEnabled ? _staticConstructNodes : new List<ConstructNode>();
            }
        }

        public override bool Normalize(bool undoable)
        {
            bool undid = false;

            if (_matrix.IsIdentity == false)
            {
#if UWP
                if (undoable)
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.UnNormalize, this, _matrix);
                    undid = true;
                }
#else
#endif

                for (int i = 0; i < _points.Count; i++)
                {
                    _points[i] = _matrix.Transform(_points[i]);
                }

                _matrix = CGeometry.IdentityMatrix();
            }
            return undid;
        }

        List<Point> LineToPointCollection(Point s, Point e)
        {
            List<Point> pc = new List<Point>();
            pc.Add(s);
            pc.Add(e);
            return pc;
        }

#if UWP
        Size _actualSize = new Size();
        double _baselineOffset = 0;

        private Size MeasureText(string text)
        {
            Size test = new Size(10000, 10000);

            TextBlock tb;

            TextStyle style = Globals.TextStyleTable[_textstyle];
            double fontsize = style.Size;   // style.Size is in (paper) pixels
            double cfontsize = fontsize;

            tb = new TextBlock();
            tb.FontSize = fontsize * 1.35;  // tb.FontSize property is in (canvas) points, not pixels
            tb.LineHeight = style.Spacing * fontsize;
            tb.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;

            string ff = style.Font;

            if (ff != null && ff.Length > 0)
            {
                try
                {
                    tb.FontFamily = new FontFamily(ff);
                }
                catch
                {
                }
            }
            tb.Text = text;

            if (tb.ActualHeight == 0)
            {
                tb.Measure(test);
            }

            if (tb.ActualHeight > 0)
            {
                _actualSize = new Size(tb.ActualWidth, tb.ActualHeight);
            }

            if (tb.BaselineOffset != 0)
            {
                _baselineOffset = tb.BaselineOffset;
            }

            return _actualSize;
        }
#endif
        private VectorTextEntity VectorizeText(VectorContext context, Point msStartPoint, Point msEndPoint, TextPosition position, string text, TextAlignment hAlign = TextAlignment.Center)
        {
            TextStyle style = Globals.TextStyleTable[_textstyle];
            double fontsize = style.Size;
            double charSpacing = style.CharacterSpacing;
            double psOffset = fontsize * style.Offset;
            double width = text.Length * fontsize * .6;
            double angle = 0;

            if (msStartPoint.X != msEndPoint.X || msStartPoint.Y != msEndPoint.Y)
            {
                double a = Construct.Angle(msStartPoint, msEndPoint);
                angle = a * Construct.cRadiansToDegrees;
            }

            double textHeight = 0;
            double textWidth = 0;
#if UWP
            if (context.CanAlignText == false)
            {
                Size sz = MeasureText(text);
                textHeight = Globals.DrawingCanvas.DisplayToPaper(sz.Height);
                textWidth = Globals.DrawingCanvas.DisplayToPaper(sz.Width);
            }
#else
#endif

            string ff = style.Font;

            RotateTransform rotateTransform = new RotateTransform();

            try
            {
                if (double.IsNaN(angle))
                {
                    angle = 0;
                }
                rotateTransform.Angle = angle;
            }
            catch (Exception ex)
            {
                // reported in analytics - how can this happen?
                Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "PDimension:VectorizeText" },
                        { "angle", angle.ToString() },
                        { "msStartPoint", string.Format("({0},{1})", msStartPoint.X, msStartPoint.Y) },
                        { "msEndPoint", string.Format("({0},{1})", msEndPoint.X, msEndPoint.Y) },
                        { "type", this.DimensionType.ToString() },
                        { "count", _points == null ? "null" : _points.Count.ToString() }
                    }, 402);
                rotateTransform.Angle = angle = 0;
            }

            Point psAlignmentOffset = new Point();
            Point pt = new Point();
            TextAlignment _hAlign = hAlign;

            if (context.CanAlignText)
            {
                switch (_hAlign)
                {
                    case TextAlignment.Left:
                        pt = msStartPoint;
                        psAlignmentOffset.X = psOffset;
                        break;
                    case TextAlignment.Center:
                    default:
                        pt.X = (msStartPoint.X + msEndPoint.X) / 2;
                        pt.Y = (msStartPoint.Y + msEndPoint.Y) / 2;
                        break;
                    case TextAlignment.Right:
                        pt = msEndPoint;
                        psAlignmentOffset.X = -psOffset;
                        break;
                }
            }
            else
            {
                switch (_hAlign)
                {
                    case TextAlignment.Left:
                        pt = msStartPoint;
                        psAlignmentOffset.X = psOffset;
                        break;
                    case TextAlignment.Center:
                    default:
                        pt.X = (msStartPoint.X + msEndPoint.X) / 2;
                        pt.Y = (msStartPoint.Y + msEndPoint.Y) / 2;
                        psAlignmentOffset.X = -(textWidth / 2);
                        break;
                    case TextAlignment.Right:
                        pt = msEndPoint;
                        psAlignmentOffset.X = -(textWidth + psOffset);
                        break;
                }
            }

            if (context.TextOriginLowerLeft)
            {
                switch (position)
                {
                    case TextPosition.Below:
                        psAlignmentOffset.Y = fontsize + psOffset;
                        break;
                    case TextPosition.On:
                        psAlignmentOffset.Y = fontsize / 2;
                        break;
                    default:
                    case TextPosition.Above:
                        psAlignmentOffset.Y = -psOffset;
                        break;
                }
            }
            else
            {
                int lines = 0;
                int nli = text.IndexOf('\n');
                while (nli >= 0)
                {
                    lines++;
                    nli = text.IndexOf('\n', nli + 1);
                }

                switch (position)
                {
                    case TextPosition.Below:
                        psAlignmentOffset.Y = -(textHeight / 2) + (fontsize * style.Spacing * lines / 2) + psOffset;
                        break;
                    case TextPosition.On:
                    default:
                        psAlignmentOffset.Y = -(textHeight / 2);
                        break;
                    case TextPosition.Above:
                        psAlignmentOffset.Y = -(textHeight / 2) - (fontsize * style.Spacing * lines / 2) - psOffset;
                        break;
                }
            }

            Point psRotatedAlignmentOffset = Utilities.TransformPoint(rotateTransform, psAlignmentOffset);

            VectorTextEntity vt = new VectorTextEntity();
            vt.Text = text;
            vt.TextHeight = fontsize;
            vt.Angle = angle;
            vt.FontFamily = ff;
            vt.TextAlignment = _hAlign;
            vt.TextPosition = position;
            vt.LineSpacing = style.Spacing;
            vt.CharacterSpacing = charSpacing;
            vt.Location = new Point(pt.X + psRotatedAlignmentOffset.X, pt.Y + psRotatedAlignmentOffset.Y);
            vt.Origin = new Point(pt.X, pt.Y);
            vt.PsOffset = psRotatedAlignmentOffset;

            return vt;
        }

        private void VectorizeDimensionLine(VectorEntity ve, Point arrowstart, Point arrowend)
        {
            ArrowType arrowType = Globals.ArrowStyleTable[_arrowStyleId].Type;
            double csize = Globals.ArrowStyleTable[_arrowStyleId].Size;
            double aspect = Globals.ArrowStyleTable[_arrowStyleId].Aspect;

            if (arrowType == ArrowType.Outline)
            {
                Point s = Construct.OffsetAlongLine(arrowstart, arrowend, csize);
                Point e = Construct.OffsetAlongLine(arrowend, arrowstart, csize);
                ve.AddChild(LineToPointCollection(s, e));
            }
            else if (arrowType == ArrowType.Ellipse)
            {
                Point s = Construct.OffsetAlongLine(arrowstart, arrowend, csize / 2);
                Point e = Construct.OffsetAlongLine(arrowend, arrowstart, csize / 2);
                ve.AddChild(LineToPointCollection(s, e));
            }
            else
            {
                ve.AddChild(LineToPointCollection(arrowstart, arrowend));
            }

            List<Point> asc = CGeometry.ArrowPointCollection(arrowstart, arrowend, arrowType, csize, aspect);
            List<Point> aec = CGeometry.ArrowPointCollection(arrowend, arrowstart, arrowType, csize, aspect);

            if (arrowType == ArrowType.Filled || arrowType == ArrowType.Dot)
            {
                VectorEntity veStart = new VectorEntity(_objectId, _zIndex);
                veStart.Fill = true;
                veStart.FillColor = veStart.Color = ve.Color;
                veStart.AddChild(asc);
                ve.AddChild(veStart);

                VectorEntity veEnd = new VectorEntity(_objectId, _zIndex);
                veEnd.Fill = true;
                veEnd.FillColor = veEnd.Color = ve.Color;
                veEnd.AddChild(aec);
                ve.AddChild(veEnd);
            }
            else
            {
                ve.AddChild(asc);
                ve.AddChild(aec);
            }
        }

        private void VectorizeOutsideDimensionLine(VectorEntity ve, Point arrowstart, Point arrowend)
        {
            ArrowType arrowType = Globals.ArrowStyleTable[_arrowStyleId].Type;
            double csize = Globals.ArrowStyleTable[_arrowStyleId].Size;
            double aspect = Globals.ArrowStyleTable[_arrowStyleId].Aspect;
            double leader = csize * 2;

            if (arrowType == ArrowType.Outline)
            {
                Point s = Construct.OffsetAlongLine(arrowstart, arrowend, -leader);
                Point e = Construct.OffsetAlongLine(arrowstart, arrowend, -csize);
                ve.AddChild(LineToPointCollection(s, e));

                s = Construct.OffsetAlongLine(arrowend, arrowstart, -leader);
                e = Construct.OffsetAlongLine(arrowend, arrowstart, -csize);
                ve.AddChild(LineToPointCollection(s, e));

                csize = -csize;
            }
            else if (arrowType == ArrowType.Ellipse)
            {
                Point s = Construct.OffsetAlongLine(arrowstart, arrowend, -leader);
                Point e = Construct.OffsetAlongLine(arrowstart, arrowend, -csize / 2);
                ve.AddChild(LineToPointCollection(s, e));

                s = Construct.OffsetAlongLine(arrowend, arrowstart, -leader);
                e = Construct.OffsetAlongLine(arrowend, arrowstart, -csize / 2);
                ve.AddChild(LineToPointCollection(s, e));
            }
            else
            {
                Point s = Construct.OffsetAlongLine(arrowstart, arrowend, -leader);
                Point e = arrowstart;
                ve.AddChild(LineToPointCollection(s, e));

                s = Construct.OffsetAlongLine(arrowend, arrowstart, -leader);
                e = arrowend;
                ve.AddChild(LineToPointCollection(s, e));

                if (arrowType != ArrowType.Dot)
                {
                    csize = -csize;
                }
            }

            List<Point> asc = CGeometry.ArrowPointCollection(arrowstart, arrowend, arrowType, csize, aspect);
            List<Point> aec = CGeometry.ArrowPointCollection(arrowend, arrowstart, arrowType, csize, aspect);

            if (arrowType == ArrowType.Filled || arrowType == ArrowType.Dot)
            {
                VectorEntity veStart = new VectorEntity(_objectId, _zIndex);
                veStart.Fill = true;
                veStart.FillColor = veStart.Color = ve.Color;
                veStart.AddChild(asc);
                ve.AddChild(veStart);

                VectorEntity veEnd = new VectorEntity(_objectId, _zIndex);
                veEnd.Fill = true;
                veEnd.FillColor = veEnd.Color = ve.Color;
                veEnd.AddChild(aec);
                ve.AddChild(veEnd);
            }
            else
            {
                ve.AddChild(asc);
                ve.AddChild(aec);
            }
        }

        public override VectorEntity Vectorize(VectorContext context)
        {
            _rendered_ve = base.Vectorize(context);

            if (_rendered_ve != null && _rendered_ve.Children.Count > 0)
            {
                List<Point> raw = _rendered_ve.Children[0] as List<Point>;
                _rendered_ve.RemoveChildren();

                ArrowType arrowType = Globals.ArrowStyleTable[_arrowStyleId].Type;
                double csize = Globals.ArrowStyleTable[_arrowStyleId].Size;
                double aspect = Globals.ArrowStyleTable[_arrowStyleId].Aspect;

                if (_dimType == DimType.PointToPoint)
                {
                    for (int i = 1; i < raw.Count; i++)
                    {
                        // Draw dimension line
                        Point arrowstart = new Point(raw[i - 1].X, raw[i - 1].Y);
                        Point arrowend = new Point(raw[i].X, raw[i].Y);

                        VectorizeDimensionLine(_rendered_ve, arrowstart, arrowend);

                        if (_showText)
                        {
                            double ds = Construct.Distance(arrowstart, arrowend);

                            VectorTextEntity vt;
                            string dstr = Utilities.FormatDistance(Globals.ActiveDrawing.PaperToModel(ds), Globals.DimensionRound,
                                Globals.ActiveDrawing.IsArchitecturalScale, Globals.ShowDimensionUnit, Globals.ActiveDrawing.ModelUnit, true);
                            if (arrowstart.X > arrowend.X || (arrowstart.X == arrowend.X && arrowstart.Y < arrowend.Y))
                            {
                                vt = VectorizeText(context, arrowend, arrowstart, TextPosition.Above, dstr);
                            }
                            else
                            {
                                vt = VectorizeText(context, arrowstart, arrowend, TextPosition.Above, dstr);
                            }
                            _rendered_ve.AddChild(vt);
                        }
                    }
                }
                else if (raw.Count > 2)
                {
                    double angle = Cirros.Utility.Construct.Angle(raw[0], raw[1]);
                    double fontsize = Globals.TextStyleTable[_textstyle].Size;
                    double dmCount = raw.Count > 3 ? raw.Count - 4 : 0;

                    if (_dimType == DimType.BaselineAngular || _dimType == DimType.IncrementalAngular)
                    {
                        // 0: center
                        // 1: first node
                        // 2: dimension arc location
                        // 3+: additional nodes

                        double offset = fontsize * .75;
                        double distanceToDimension = Construct.Distance(raw[0], raw[2]);
                        double space = _dimType == DimType.BaselineAngular && dmCount > 0 ? fontsize * 3 : 0;

                        Point p0 = Construct.PolarOffset(raw[1], offset, angle);
                        Point p1 = Construct.PolarOffset(raw[0], distanceToDimension + space * dmCount + offset, angle);

                        if (_showExtension)
                        {
                            _rendered_ve.AddChild(LineToPointCollection(p0, p1));
                        }

                        for (int i = 3; i < raw.Count; i++)
                        {
                            double a = Cirros.Utility.Construct.Angle(raw[0], raw[i]);

                            p0 = Construct.PolarOffset(raw[i], offset, a);
                            p1 = Construct.PolarOffset(raw[0], distanceToDimension + offset, a);

                            double included = Construct.MinorIncludedAngle(angle, a);

                            if (_showExtension)
                            {
                                _rendered_ve.AddChild(LineToPointCollection(p0, p1));
                            }
                            _rendered_ve.AddChild(CGeometry.ArcPointCollection(raw[0], distanceToDimension, angle, included, false, CGeometry.IdentityMatrix()));

                            List<Point> asc = CGeometry.ArrowPointCollection(raw[0], distanceToDimension, angle, included, arrowType, csize, aspect);
                            List<Point> aec = CGeometry.ArrowPointCollection(raw[0], distanceToDimension, a, -included, arrowType, csize, aspect);

                            if (arrowType == ArrowType.Filled || arrowType == ArrowType.Dot)
                            {
                                VectorEntity veStart = new VectorEntity(_objectId, _zIndex);
                                veStart.Fill = true;
                                veStart.FillColor = veStart.Color = _rendered_ve.Color;
                                veStart.AddChild(asc);
                                _rendered_ve.AddChild(veStart);

                                VectorEntity veEnd = new VectorEntity(_objectId, _zIndex);
                                veEnd.Fill = true;
                                veEnd.FillColor = veEnd.Color = _rendered_ve.Color;
                                veEnd.AddChild(aec);
                                _rendered_ve.AddChild(veEnd);
                            }
                            else
                            {
                                _rendered_ve.AddChild(asc);
                                _rendered_ve.AddChild(aec);
                            }

                            if (_showText && distanceToDimension > 0)
                            {
                                double degrees = Math.Round(Math.Abs(included) * Construct.cRadiansToDegrees, 1);

                                VectorTextEntity vt;
                                string dstr = string.Format("{0}°", degrees);
                                double ta = angle + included / 2;
                                double ainc = fontsize / distanceToDimension;
                                if (ta < -Math.PI || (ta > 0 && ta < Math.PI))
                                {
                                    ainc = -ainc;
                                }
                                Point v0 = Construct.PolarOffset(raw[0], distanceToDimension, ta - ainc);
                                Point v1 = Construct.PolarOffset(raw[0], distanceToDimension, ta + ainc);
                                vt = VectorizeText(context, v0, v1, TextPosition.Above, dstr);
                                _rendered_ve.AddChild(vt);
                            }

                            if (_dimType == DimType.IncrementalAngular)
                            {
                                angle = a;
                            }
                            else
                            {
                                distanceToDimension += space;
                            }
                        }
                    }
                    else
                    {
                        Point v = new Point(Math.Cos(angle), Math.Sin(angle));

                        // First extension line requires distance from direction vector to dimension line location
                        double sign = 1;
                        double ia = Construct.IncludedAngle(raw[2], raw[0], raw[1]);
                        if (!double.IsNaN(ia))
                        {
                            sign = Math.Sign(ia);
                        }
                        double distanceToDimension = sign * Construct.DistancePointToLine(raw[2], raw[0], raw[1]);
                        double offset = sign * fontsize * .75;
                        double space = (_dimType == DimType.Baseline || _dimType == DimType.Outside) && dmCount > 0 ? sign * fontsize * 3 : 0;

                        double dsx = distanceToDimension * v.Y;
                        double dsy = -distanceToDimension * v.X;
                        double sx = space * v.Y;
                        double sy = -space * v.X;
                        double ox = offset * v.Y;
                        double oy = -offset * v.X;

                        Point ex1 = new Point(raw[0].X + ox, raw[0].Y + oy);
                        Point ex2 = new Point(raw[0].X + ox + dsx + sx * dmCount, raw[0].Y + oy + dsy + sy * dmCount);

                        if (_showExtension)
                        {
                            _rendered_ve.AddChild(LineToPointCollection(ex1, ex2));
                        }

                        int direction = Construct.WhichSide(ex1, ex2, raw[1]);

                        for (int i = 3; i < raw.Count; i++)
                        {
                            // Draw dimension line
                            Point arrowstart = new Point(raw[0].X + dsx, raw[0].Y + dsy);
                            Point p1 = new Point(raw[0].X + dsx + v.Y, raw[0].Y + dsy - v.X);
                            double ds = Construct.DistancePointToLine(raw[i], arrowstart, p1);
                            double sign0 = 1;
                            if (Construct.WhichSide(ex1, ex2, raw[i]) != direction)
                            {
                                ds = -ds;
                                sign0 = -1;
                            }

                            Point arrowend = new Point(raw[0].X + dsx + ds * v.X, raw[0].Y + dsy + ds * v.Y);

                            if (_dimType == DimType.Outside)
                            {
                                VectorizeOutsideDimensionLine(_rendered_ve, arrowstart, arrowend);
                            }
                            else
                            {
                                VectorizeDimensionLine(_rendered_ve, arrowstart, arrowend);
                            }

                            if (_showText)
                            {
                                VectorTextEntity vt;
                                string dstr = Utilities.FormatDistance(Globals.ActiveDrawing.PaperToModel(ds), Globals.DimensionRound,
                                    Globals.ActiveDrawing.IsArchitecturalScale, Globals.ShowDimensionUnit, Globals.ActiveDrawing.ModelUnit, true);
                                if (_dimType == DimType.Outside)
                                {
                                    Point s, e;
                                    TextAlignment align;

                                    if (raw[0].X > raw[1].X)
                                    {
                                        if (arrowstart.X > arrowend.X || (arrowstart.X == arrowend.X && arrowstart.Y < arrowend.Y))
                                        {
                                            s = Construct.PolarOffset(arrowstart, -2 * csize, angle);
                                            e = Construct.PolarOffset(arrowstart, -3 * csize, angle);
                                            align = TextAlignment.Left;
                                        }
                                        else
                                        {
                                            s = Construct.PolarOffset(arrowstart, 3 * csize, angle);
                                            e = Construct.PolarOffset(arrowstart, 2 * csize, angle);
                                            align = TextAlignment.Right;
                                        }
                                    }
                                    else if (arrowstart.X > arrowend.X || (arrowstart.X == arrowend.X && angle > 0))
                                    {
                                        s = Construct.PolarOffset(arrowstart, 2 * csize, angle);
                                        e = Construct.PolarOffset(arrowstart, 3 * csize, angle);
                                        align = TextAlignment.Left;
                                    }
                                    else
                                    {
                                        s = Construct.PolarOffset(arrowstart, -3 * csize, angle);
                                        e = Construct.PolarOffset(arrowstart, -2 * csize, angle);
                                        align = TextAlignment.Right;
                                    }

                                    vt = VectorizeText(context, s, e, TextPosition.On, dstr, align);
                                }
                                else
                                {
                                    if (arrowstart.X > arrowend.X || (arrowstart.X == arrowend.X && arrowstart.Y < arrowend.Y))
                                    {
                                        vt = VectorizeText(context, arrowend, arrowstart, TextPosition.Above, dstr);
                                    }
                                    else
                                    {
                                        vt = VectorizeText(context, arrowstart, arrowend, TextPosition.Above, dstr);
                                    }
                                }
                                _rendered_ve.AddChild(vt);
                            }

                            sign = Construct.WhichSide(arrowstart, arrowend, raw[i]);

                            // Subsequent extensions require distance from dimension node to dimension line
                            distanceToDimension = sign * sign0 * Construct.DistancePointToLine(raw[i], arrowstart, arrowend);

                            double dx = distanceToDimension * v.Y;
                            double dy = -distanceToDimension * v.X;
                            //ox = raw[i].X + sign * offset * v.Y;
                            //oy = raw[i].Y - sign * offset * v.X;
                            ox = raw[i].X + offset * v.Y;
                            oy = raw[i].Y - offset * v.X;

                            if (_showExtension)
                            {
                                _rendered_ve.AddChild(LineToPointCollection(new Point(ox, oy), new Point(ox + dx, oy + dy)));
                            }

                            if (_dimType == DimType.Incremental)
                            {
                                dsx += ds * v.X;
                                dsy += ds * v.Y;
                            }
                            else if (_dimType == DimType.Baseline || _dimType == DimType.Outside)
                            {
                                dsx += sx;
                                dsy += sy;
                            }
                        }
                    }
                }
            }

            // Call base.Vectorize() again to set the _ve global (for construction points) to the dimension control points 
            base.Vectorize(context, true);

            return _rendered_ve;
        }

        private void _pickVE(VectorEntity ve, Point paper, ref double distance)
        {
            if (ve.Children != null)
            {
                foreach (object o in ve.Children)
                {
                    if (o is List<Point>)
                    {
                        List<Point> pc = o as List<Point>;
                        Point v0 = pc[0];

                        for (int i = 1; i < pc.Count; i++)
                        {
                            Point v1 = pc[i];

                            Point n0 = Construct.NormalPointToLine(paper, v0, v1);
                            double npv = Construct.PointValue(v0, v1, n0);
                            if (npv == 0 || npv == 1)
                            {
                                distance = 0;
                                return;
                            }
                            else if (npv >= 0 && npv <= 1)
                            {
                                double d = Construct.Distance(paper, n0);
                                if (d == 0)
                                {
                                    distance = 0;
                                    return;
                                }
                                else if (d < distance)
                                {
                                    distance = d;
                                }
                            }

                            v0 = v1;
                        }
                    }
                    else if (o is VectorEntity)
                    {
                        _pickVE(o as VectorEntity, paper, ref distance);
                    }
                }
            }
        }
    }
}
