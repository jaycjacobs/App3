using System;
using System.Collections.Generic;
using Cirros.Primitives;
using Cirros.Actions;
using Cirros.Utility;
using Cirros.Display;
using Cirros.Drawing;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Cirros.Core;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;

namespace Cirros.Commands
{
    public enum CommandType
    {
        none = 0,
        select,
        copyalongline,
        copyalongarc,
        edit,
        editgroup,
        line,
        freehand,
        fillet,
        polyline,
        polygon,
        doubleline,
        bezier,
        bspline,
        rectangle,
        circle,
        circle3,
        arc,
        arc2,
        arc3,
        arcf,
        arcfr,
        ellipse,
        text,
        arrow,
        dimension,
        image,
        insertimage,
        window,
        pan,
        insert,
        insertsymbol,
        properties,
        distance,
        angle,
        area,
        origin,
        ktproperties,
        copypaste,
        managesymbols
    };

    public enum SelectSubCommand
    {
        Move,
        MoveOffset,
        Copy,
        CopyOffset,
        Scale,
        Rotate,
        Pivot,
        Properties
    }

    public enum EditSubCommand
    {
        None,
        MovePoint,
        InsertPoint,
        DeletePoint,
        ExtendTrim,
        MoveSegment,
        OffsetMove,
        OffsetCopy,
        Gap,
        Properties,
        MemberProperties,
        AddMember,
        MoveMember,
        DeleteMember,
        Stroke
    }

    public enum LineCommandType
    {
        Single,
        Multi,
        Fillet,
        Freehand
    }

    public enum PolygonCommandType
    {
        Regular,
        Irregular
    }

    public enum RectangleCommandType
    {
        Corners,
        Size
    }

    public enum EllipseCommandType
    {
        Box,
        Axis,
        Center
    }

    public enum ArcCommandType
    {
        CenterStartEnd,
        ThreePoint,
        SemiCircle,
        CenterRadiusStartEnd,
        CenterRadiusAngles,
        Fillet,
        FilletRadius
    }

    public enum CopyRepeatType
    {
        Distribute,
        Space
    }


    public enum InputMode
    {
        Draw,       // Free input, construct enabled
        Pick,       // Pick single object, construct disabled
        Select,     // Pick single object or drag rectangle, construct disabled
        Reselect,   // Pick point on selection, construct enabled
        Drag,       // Track while mouse is down
    }

    public abstract class CommandProcessor
    {
        protected CommandType _type = CommandType.none;

        protected ARubberBand _rubberBand = new RubberBandBasic();

        protected bool _shiftKey = false;
        protected bool _showConstructHandles = true;
        protected string _string = "";

        protected Point _start;
        protected Point _first;
        protected Point _current;
        protected Point _through;

        protected double _hoverDistance;
        protected Point _hoverLoc = new Point(0, 0);
        protected DispatcherTimer _hoverTimer;

        protected List<ConstructNode> _constructNodes = new List<ConstructNode>();
        protected Handles _constructHandles = new Handles();
        protected Primitive _lastObject = null;

        protected Color _color = Globals.Theme.ForegroundColor;

        public CommandProcessor()
        {
            ShowConstructHandles = true;

            if (Globals.LayerTable.ContainsKey(Globals.LayerId))
            {
                Layer layer = Globals.LayerTable[Globals.LayerId];
                if (layer != null)
                {
                    _color = Utilities.ColorFromColorSpec(layer.ColorSpec);
                }
            }
        }

        public void Initialize()
        {
            CanvasScaleChanged();
            Start();

            _hoverTimer = new DispatcherTimer();
            _hoverTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _hoverTimer.Tick += _timer_Tick;
        }

        void _timer_Tick(object sender, object e)
        {
            Hover(_start, _current, _through);

            _hoverTimer.Stop();
        }

        public virtual void Invoke(object o, object parameter)
        {
            if (o is string)
            {
                switch ((string)o)
                {
                    case "A_Done":
                        Finish();
                        break;

                    case "A_EditLast":
                        if (_lastObject != null)
                        {
                            Globals.CommandProcessorParameter = _lastObject;
                            Globals.EditSubCommand = EditSubCommand.MovePoint;
                            Globals.CommandDispatcher.ActiveCommand = CommandType.edit;
                            Globals.ReturnToCommand = _type;
                        }
                        break;
                }
            }
        }

        public virtual bool EnableCommand(object o)
        {
            bool enable = false;

            if (o is string && (string)o == "A_EditLast")
            {
                enable = _lastObject != null;
            }

            return enable;
        }

        public virtual InputMode InputMode
        {
            get
            {
                return InputMode.Draw;
            }
        }

        protected virtual CursorType cursorType
        {
            get
            {
                return CursorType.Draw;
            }
        }

        public CommandType Type
        {
            get
            {
                return _type;
            }
        }

        public bool ShowConstructHandles
        {
            set
            {
                _showConstructHandles = value;
                _constructHandles.Visible = _showConstructHandles && Globals.Input.ObjectSnap && _shiftKey == false;

                if (!_showConstructHandles)
                {
                    ResetConstructHandles();
                }
            }
        }

        public void ResetConstructHandles()
        {
            _constructNodes.Clear();

            _constructHandles.Deselect();
            _constructHandles.Clear();
            _constructHandles.Draw();
        }

        public void SetString(string s)
        {
            _string = s;
        }

        public virtual Point Anchor
        {
            get
            {
                return _rubberBand != null ? _rubberBand.Anchor : _start;
            }
        }

        public virtual void StartTracking(double x, double y, bool shift, bool control)
        {
            //System.Diagnostics.Debug.Assert(_constructHandles != null, "_constructHandles is not defined");

            if (Globals.Input.ObjectSnap && shift == false)
            {
                if (_constructHandles.SelectedHandleID >= 0)
                {
                   if (_rubberBand.State == 0 && _constructHandles.SelectedHandle2 != null)
                    {
                        _first = _constructHandles.SelectedHandle.Location;
                        _start = _constructHandles.SelectedHandle2.Location;

                        _rubberBand.State = 2;
                    }
                    else
                    {
                        _start = _constructHandles.SelectedHandle.Location;
                        TrackCursor(_start.X, _start.Y);
                    }
                }
                else if (Globals.DrawingTools.ActiveTrianglePoint(out Point trianglePoint))
                {
                    _start = trianglePoint;
                    TrackCursor(_start.X, _start.Y);
                }
                else
                {
                    _start = new Point(x, y);
                    _lastObject = null;
                }
            }
            else
            {
                _start = new Point(x, y);
                _lastObject = null;
            }
             
            _through = _start;
        }

        public virtual void TrackCursor(double x, double y)
        {
            if (_showConstructHandles)
            {
                Point hoverLoc = new Point(x, y);

                if (hoverLoc != _hoverLoc)
                {
                    double d = _hoverDistance / 4;
                    int nodeIndex = -1;

                    _constructHandles.Deselect();
                    _constructHandles.Clear();

                    for (int i = 0; i < _constructNodes.Count; i++)
                    {
                        Point n = _constructNodes[i].Location;
                        double ds = Math.Abs(n.X - x) + Math.Abs(n.Y - y);

                        if (ds < _hoverDistance)
                        {
                            double opc = (_hoverDistance - ds) / _hoverDistance;
                            _constructHandles.AddHandle(i, n.X, n.Y, HandleType.Diamond, opc);

                            if (ds < d)
                            {
                                d = ds;
                                nodeIndex = i;
                            }
                        }
                    }

                    if (nodeIndex >= 0)
                    {
                        ConstructNode node = _constructNodes[nodeIndex];

                        _constructHandles.Draw();
                        _constructHandles.Select(nodeIndex);

                        Globals.Events.ShowConstructionPoint(node.Name, node.Location);
                    }
                    else
                    {
                        Globals.Events.ShowConstructionPoint(null, hoverLoc);
                        _constructHandles.Draw();
                    }

                    if (_hoverTimer != null)
                    {
                        _hoverTimer.Start();
                        _hoverLoc = hoverLoc;
                    }
                }
            }

            _current = new Point(x, y);

            _rubberBand.TrackCursor(x, y);
        }

        public virtual void EndTracking(double x, double y)
        {
            Globals.Events.PointAdded(new Point(x, y));
        }

        public virtual Point Step(double dx, double dy, bool stillDown)
        {
            return Globals.Input.CursorLocation;
        }

        public virtual void PointerEnteredDrawingArea()
        {
            _rubberBand.Show();
        }

        public virtual void PointerLeftDrawingArea()
        {
            _rubberBand.Hide();
        }

        public virtual void EnterPoint(Point p)
        {
            StartTracking(p.X, p.Y, false, false);
            TrackCursor(p.X, p.Y);
            EndTracking(p.X, p.Y);
        }

        public virtual void KeyDown(string key, bool shift, bool control, bool gmk)
        {
            if (gmk)
            {
                if (key == "enter" || key == "escape")
                {
#if KT22
                    var f = FocusManager.GetFocusedElement();
                    if (f is ContentControl c && c.Name == "focusTarget")
                    {
                        // command entry control
                    }
                    else
                    {
                        Finish();

                        Globals.Events.PointAdded(Globals.workCanvas.CursorLocation, "enter");
                        Globals.Events.CommandFinished(key, shift, control, gmk);
                    }
#else
                    Finish();

                    Globals.Events.PointAdded(Globals.workCanvas.CursorLocation, "enter");
                    Globals.Events.CommandFinished(key, shift, control, gmk);
#endif
                }
                else if (key == "x")
                {
                    EnterPoint(new Point(_current.X, _start.Y));
                }
                else if (key == "y")
                {
                    EnterPoint(new Point(_start.X, _current.Y));
                }
                else if (key == "i")
                {
                    if (_through != _start)
                    {
                        EnterPoint(Construct.NormalPointToLine(_current, _start, _through));
                    }
                }
                else if (key == "t")
                {
                    if (Globals.Input.ObjectSnap && shift == false && _constructHandles.SelectedHandleID >= 0)
                    {
                        _through = _constructHandles.SelectedHandle.Location;
                    }
                    else
                    {
                        _through = _current;
                    }
                }
            }

            if (key == "shift")
            {
                _shiftKey = true;
                _constructHandles.Visible = false;
            }
        }

        public virtual void KeyUp(string key)
        {
            if (key == "shift")
            {
                _shiftKey = false;
                _constructHandles.Visible = _showConstructHandles;
            }
        }

        public virtual void UndoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            if (actionId == ActionID.CommandInternal)
            {
                UndoInternalCommand(subject, predicate, predicate2);
            }
        }

        public virtual void RedoNotification(ActionID actionId, object subject, object predicate, object predicate2)
        {
            if (actionId == ActionID.CommandInternal)
            {
                RedoInternalCommand(subject, predicate, predicate2);
            }
        }

        protected virtual void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            _through = _start;
        }

        protected virtual void RedoInternalCommand(object subject, object predicate, object predicate2)
        {
        }

        public virtual void Start()
        {
            int layerId = -1;

            if (Globals.UIVersion == 0)
            {
                switch (_type)
                {
                    case CommandType.line:
                    case CommandType.polyline:
                    case CommandType.polygon:
                    case CommandType.doubleline:
                    case CommandType.bezier:
                    case CommandType.bspline:
                    case CommandType.rectangle:
                    case CommandType.circle:
                    case CommandType.circle3:
                    case CommandType.arc:
                    case CommandType.arc2:
                    case CommandType.arc3:
                    case CommandType.arcf:
                    case CommandType.arcfr:
                    case CommandType.ellipse:
                        layerId = Globals.LayerId;
                        break;

                    case CommandType.text:
                        layerId = Globals.TextLayerId;
                        break;

                    case CommandType.arrow:
                    case CommandType.dimension:
                        layerId = Globals.DimensionLayerId;
                        break;

                    case CommandType.select:
                    case CommandType.copyalongline:
                    case CommandType.copyalongarc:
                    case CommandType.edit:
                    case CommandType.window:
                    case CommandType.pan:
                    case CommandType.insert:
                    case CommandType.insertsymbol:
                    case CommandType.properties:
                    case CommandType.ktproperties:
                    case CommandType.distance:
                    case CommandType.angle:
                    case CommandType.area:
                    case CommandType.origin:
                    case CommandType.none:
                    default:
                        layerId = Globals.LayerId;
                        break;
                }
            }
            else
            {
                switch (_type)
                {
                    case CommandType.line:
                    case CommandType.polyline:
                        layerId = Globals.ActiveLineLayerId;
                        break;

                    case CommandType.polygon:
                        layerId = Globals.ActivePolygonLayerId;
                        break;

                    case CommandType.doubleline:
                        layerId = Globals.ActiveDoubleLineLayerId;
                        break;

                    case CommandType.bezier:
                    case CommandType.bspline:
                        layerId = Globals.ActiveCurveLayerId;
                        break;

                    case CommandType.rectangle:
                        layerId = Globals.ActiveRectangleLayerId;
                        break;

                    case CommandType.circle:
                    case CommandType.circle3:
                        layerId = Globals.ActiveCircleLayerId;
                        break;

                    case CommandType.arc:
                    case CommandType.arc2:
                    case CommandType.arc3:
                    case CommandType.arcf:
                    case CommandType.arcfr:
                        layerId = Globals.ActiveArcLayerId;
                        break;

                    case CommandType.ellipse:
                        layerId = Globals.ActiveEllipseLayerId;
                        break;

                    case CommandType.text:
                        layerId = Globals.ActiveTextLayerId;
                        break;

                    case CommandType.arrow:
                        layerId = Globals.ActiveArrowLayerId;
                        break;

                    case CommandType.dimension:
                        layerId = Globals.ActiveDimensionLayerId;
                        break;

                    case CommandType.copyalongline:
                    case CommandType.copyalongarc:
                    case CommandType.insert:
                    case CommandType.insertsymbol:
                        layerId = Globals.ActiveInstanceLayerId;
                        break;

                    case CommandType.select:
                    case CommandType.copypaste:
                    case CommandType.edit:
                    case CommandType.window:
                    case CommandType.pan:
                    case CommandType.properties:
                    case CommandType.ktproperties:
                    case CommandType.distance:
                    case CommandType.angle:
                    case CommandType.area:
                    case CommandType.origin:
                    case CommandType.none:
                    default:
                        layerId = Globals.ActiveLayerId;
                        break;
                }
            }

            if (Globals.LayerTable.ContainsKey(layerId) == false)
            {
                layerId = Globals.ActiveLayerId;
            }

            if (Globals.LayerTable.ContainsKey(layerId))
            {
                Layer currentLayer = Globals.LayerTable[layerId];
                if (currentLayer.Visible == false)
                {
                    currentLayer.Visible = true;
                    Layer.PropagateLayerChanges(layerId);

                    Globals.Events.ActiveLayerShown(layerId);
                }

                if (Globals.ColorSpec == (uint)ColorCode.ByLayer)
                {
                    _color = Utilities.ColorFromColorSpec(currentLayer.ColorSpec);
                }
                else
                {
                    _color = Utilities.ColorFromColorSpec(Globals.ColorSpec);
                }

                if (_rubberBand != null)
                {
                    _rubberBand.Color = _color;
                }
            }

            Globals.Input.SelectCursor(cursorType);
            Globals.DrawingCanvas.Focus();

            InputPoint ip;
            while ((ip = Globals.Input.PopPoint()) != null)
            {
                if (ip.Mode == CoordinateMode.End)
                {
                    Finish();
                    Globals.Events.PointAdded(Globals.workCanvas.CursorLocation, "enter");
                    Globals.Events.CommandFinished("enter", false, false, true);
                }
                else
                { 
                    Point p = Utilities.CoordinateToPoint(ip.Mode, ip.V1, ip.V2);
                    Globals.Input.MoveCursorTo(p.X, p.Y);
                    Globals.Input.EnterPoint();
                }
            }
        }

        public virtual void DoubleClick()
        {
            Finish();
        }

        public virtual void Finish()
        {
            if (_hoverTimer != null)
            {
                _hoverTimer.Stop();
            }

            _constructHandles.Deselect(); 
            _constructHandles.Clear();
            _constructNodes.Clear();

            Start();

            if (_rubberBand != null)
            {
                _rubberBand.State = 0;
                _rubberBand.EndTracking();
                _rubberBand.Color = _color;
            }
        }

        protected virtual Primitive addPrimitive(Primitive p)
        {
            p.AddToContainer(Globals.ActiveDrawing);
            Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, p);
            return p;
        }

        public virtual void CanvasScaleChanged()
        {
            if (Globals.ActiveDrawing != null)
            {
                if (_rubberBand != null)
                {
                    _rubberBand.Update();
                }

                _hoverDistance = Globals.DrawingCanvas.DisplayToPaper(Globals.hitTolerance * 2);
                _constructHandles.Draw();
            }
        }

        public virtual void CanvasScrolled()
        {
        }

        public Color ColorFromFillColorSpec(uint colorspec)
        {
            Color fillColor = Colors.Transparent;

            if (colorspec == (uint)ColorCode.NoFill)
            {
                fillColor = Colors.Transparent;
            }
            else if (colorspec == (uint)ColorCode.SameAsOutline)
            {
                fillColor = _color;
            }
            else if (colorspec == (uint)ColorCode.ByLayer)
            {
                Layer layer = Globals.LayerTable[Globals.LayerId];
                fillColor = Utilities.ColorFromColorSpec(layer.ColorSpec);
            }
            else
            {
                fillColor = Utilities.ColorFromColorSpec(colorspec);
            }

            return fillColor;
        }

        protected virtual void Hover(Point from, Point to, Point through)
        {
            List<List<Point>> sl = new List<List<Point>>();

            _constructNodes.Clear();

            // Check for workCanvas not null - this could be hit after workCanvas is closed
            if (Globals.ActiveDrawing != null)
            {
                foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
                {
                    if (p.ConstructEnabled && Globals.LayerTable[p.LayerId].Visible && p.IsNear(to, _hoverDistance))
                    {
                        _constructNodes.AddRange(p.ConstructNodes);

                        if (p is PLine)
                        {
                            List<Point> pc = ((PLine)p).GetTangent(to);
                            if (pc != null)
                            {
                                sl.Add(pc);
                            }
                        }
                    }
                }
            }

            if (sl.Count == 1)
            {
                Point pd = to;
                if (Globals.DrawingTools.IntersectWithEdge(sl[0][0], sl[0][1], ref pd, _hoverDistance))
                {
                    _constructNodes.Add(new ConstructNode(pd, "intersection"));
                }
                else
                {
                    _constructNodes.AddRange(Globals.DrawingTools.DynamicTriangleConstructNodes(from, through));
                }
            }
            else
            {
                if (sl.Count == 2)
                {
                    Point pi = Construct.IntersectLineLine(sl[0][0], sl[0][1], sl[1][0], sl[1][1]);
                    if (pi.X != sl[0][0].X || pi.Y != sl[0][0].Y)
                    {
                        _constructNodes.Add(new ConstructNode(pi, "intersection"));
                    }
                }

                _constructNodes.AddRange(Globals.DrawingTools.DynamicTriangleConstructNodes(from, through));
            }
        }
    }

    public abstract class RectCommandProcessorBase : CommandProcessor
    {
        protected double _trackLeft = 0;
        protected double _trackTop = 0;
        protected double _trackWidth = 0;
        protected double _trackHeight = 0;

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            if (_rubberBand.State == 0)
            {
                _first = _start;
            }
        }

        public override void TrackCursor(double x, double y)
        {
            base.TrackCursor(x, y);

            if (_rubberBand.State > 0)
            {
                _trackLeft = _first.X;
                _trackTop = _first.Y;
                _trackWidth = x -_first.X;
                _trackHeight = y - _first.Y;
            }
        }

        protected virtual Primitive instantiate()
        {
            return null;
        }
    }

    public class PolylineCommandProcessor : CommandProcessor
    {
        protected PLine _wkPolyline = null;
        bool _alternate = false;
        bool _gap = false;
        int _pointCount = 0;

        public PolylineCommandProcessor(bool alternate)
        {
            _type = CommandType.polyline;
            _alternate = alternate;
            _rubberBand = new RubberBandLine(_color);
        }

        protected virtual PLine instantiate(Point s, Point e)
        {
            return new PLine(s, e);
        }

        public override bool EnableCommand(object o)
        {
            bool enable = base.EnableCommand(o);

            if (enable == false)
            {
                if (o is string && (string)o == "A_Done")
                {
                    enable = _pointCount > 0;
                }
            }

            return enable;
        }

        public override void KeyDown(string key, bool shift, bool control, bool gmk)
        {
            if (key == "186")
            {
                _gap = true;
                _rubberBand.State = 0;
                _rubberBand.StartTracking(_through.X, _through.Y);
            }
            base.KeyDown(key, shift, control, gmk);
        }

        // State 0
        //    No RB
        //    Start tracking
        //      Save anchor point
        //      State = 1
        // State = 1
        //    RB from anchor
        //    Start tracking
        //       Create line from anchor to current
        //       Save anchor
        //       State = 2
        // State = 2
        //    RB from anchor
        //    Start tracking
        //       Add current point to line
        //       Save anchor
        //       State = 2

        protected virtual void addPoint(Point p)
        {
            if (_pointCount == 0)
            {
                _first = p;

                _rubberBand.State = 1;
            }
            else if (_pointCount == 1)
            {
                if (_first.X != p.X || _first.Y != p.Y)
                {
                    _wkPolyline = instantiate(_first, p);
                    _wkPolyline.ConstructEnabled = true;

                    _wkPolyline.AddToContainer(Globals.ActiveDrawing);

                    _rubberBand.State = _alternate ? 0 : 2;
                }
                else
                {
                    _pointCount = 0;
                }
                _through = _first;
            }
            else if (_wkPolyline != null)
            {
                Point e = _wkPolyline.EndPoint;
                if ((p.X != e.X || p.Y != e.Y) || _rubberBand.State == 0)
                {
                    if (_alternate)
                    {
                        if ((_wkPolyline.Points.Count % 2) == 0)
                        {
                            _wkPolyline.AddPoint(p.X, p.Y, true);
                            _rubberBand.StartTracking(p.X, p.Y);
                            _rubberBand.State = 0;
                        }
                        else
                        {
                            _wkPolyline.AddPoint(new CPoint(p, 0), true);
                            _rubberBand.StartTracking(p.X, p.Y);
                            _rubberBand.State = 1;
                        }
                    }
                    else if (_gap)
                    {
                        _gap = false;
                        _wkPolyline.AddPoint(new CPoint(p, 0), true);
                        _rubberBand.StartTracking(p.X, p.Y);
                        _rubberBand.State = 1;
                    }
                    else
                    {
                        _wkPolyline.AddPoint(p.X, p.Y, true);
                    }
                }

                _through = e;
            }

            if (_wkPolyline != null)
            {
                _wkPolyline.ClearStaticConstructNodes();
            }

            ++_pointCount;

            _rubberBand.StartTracking(p.X, p.Y);
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {

            base.StartTracking(x, y, shift, control);
            addPoint(_start);
            Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal, CommandProcessorActionID.EnterPoint, _start);
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

            if (_wkPolyline != null)
            {
                _lastObject = _wkPolyline;
                _wkPolyline.ClearStaticConstructNodes();
                _wkPolyline.ConstructEnabled = true;

                Globals.Events.PrimitiveCreated(_lastObject);
                Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, _wkPolyline);
                _wkPolyline = null;
            }
          
            _rubberBand.EndTracking();
            _rubberBand.State = 0;

            _pointCount = 0;

            base.Finish();
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            if ((CommandProcessorActionID)subject == CommandProcessorActionID.EnterPoint)
            {
                if (_wkPolyline != null)
                {
                    _wkPolyline.RemovePoint();

                    _first = _start = _wkPolyline.EndPoint;

                    if (_wkPolyline.Points.Count < 1)
                    {
                        Globals.ActiveDrawing.RemovePrimitive(_wkPolyline);

                        _wkPolyline = null;
                        _pointCount = 1;
                        _rubberBand.State = 1;
                    }
                    else
                    {
                        _pointCount = _wkPolyline.Points.Count + 1;

                        if (_alternate == false || (_wkPolyline.Points.Count % 2) == 0)
                        {
                            _rubberBand.State = 1;
                        }
                        else
                        {
                            _rubberBand.State = 0;
                        }
                        
                        if (_wkPolyline.Points.Count <= 1)
                        {
                            _through = _wkPolyline.Origin;
                        }
                        else
                        {
                            Point t = _wkPolyline.Points[_wkPolyline.Points.Count - 2];
                            _through.X = t.X + _wkPolyline.Origin.X;
                            _through.Y = t.Y + _wkPolyline.Origin.Y;
                        }
           
                        _wkPolyline.ClearStaticConstructNodes();
                    }

                    _rubberBand.StartTracking(_start.X, _start.Y);
                }
                else
                {
                    _rubberBand.Reset();
                    _rubberBand.State = 0;
                    _pointCount = 0;
                    _through = _start;
                }
            }
        }

        protected override void RedoInternalCommand(object subject, object predicate, object predicate2)
        {
            if (subject is CommandProcessorActionID && (CommandProcessorActionID)subject == CommandProcessorActionID.EnterPoint && predicate is Point)
            {
                if (_wkPolyline != null)
                {
                    _start = (Point)predicate;
                }
                addPoint((Point)predicate);
            }

            base.RedoInternalCommand(subject, predicate, predicate2);
        }

        protected override void Hover(Point from, Point current, Point through)
        {
            if (_rubberBand.State >= 1)
            {
                List<List<Point>> sl = new List<List<Point>>();

                _constructNodes.Clear();

                // Check for workCanvas not null - this could be hit after workCanvas is closed
                if (Globals.ActiveDrawing != null)
                {
                    foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
                    {
                        if (Globals.LayerTable[p.LayerId].Visible && p.IsNear(current, _hoverDistance))
                        {
                            _constructNodes.AddRange(p.ConstructNodes);
                            _constructNodes.AddRange(p.DynamicConstructNodes(from, through));

                            if (p is PLine)
                            {
                                List<Point> pc = ((PLine)p).GetTangent(current);
                                if (pc != null)
                                {
                                    sl.Add(pc);
                                }
                            }
                        }
                    }
                }

                if (sl.Count == 1)
                {
                    Point pd = current;
                    if (Globals.DrawingTools.IntersectWithEdge(sl[0][0], sl[0][1], ref pd, _hoverDistance))
                    {
                        _constructNodes.Add(new ConstructNode(pd, "intersection"));
                    }
                    else
                    {
                        _constructNodes.AddRange(Globals.DrawingTools.DynamicTriangleConstructNodes(from, through));
                    }
                }
                else
                {
                    if (sl.Count == 2)
                    {
                        Point pi = Construct.IntersectLineLine(sl[0][0], sl[0][1], sl[1][0], sl[1][1]);
                        if (pi.X != sl[0][0].X || pi.Y != sl[0][0].Y)
                        {
                            _constructNodes.Add(new ConstructNode(pi, "intersection"));
                        }
                    }

                    _constructNodes.AddRange(Globals.DrawingTools.DynamicTriangleConstructNodes(from, through));
                }
            }
            else
            {
                base.Hover(from, current, through);
            }

#if true
            if (_wkPolyline != null)
            {
                _constructNodes.AddRange(_wkPolyline.ConstructNodes);
            }
#else
            if (_wkPolyline != null && Math.Abs(current.X - _wkPolyline.Origin.X) < _hoverDistance && Math.Abs(current.Y - _wkPolyline.Origin.Y) < _hoverDistance)
            {
                _constructNodes.Add(new ConstructNode(_wkPolyline.Origin, "origin"));
            }
#endif
        }
    }

    public class FilletCommandProcessor : PolylineCommandProcessor
    {
        public FilletCommandProcessor() : base(false)
        {
            _type = CommandType.fillet;
            _rubberBand = new RubberBandLine(_color);
        }

        protected override PLine instantiate(Point s, Point e)
        {
            PLine line = new PLine(s);
            if (line != null)
            {
                line.Radius = Globals.FilletRadius;
            }
            line.AddPoint(e.X, e.Y, true);

            return line;
        }
    }

    public class FreehandCommandProcessor : CommandProcessor
    {
        protected PLine _wkPolyline = null;

        public FreehandCommandProcessor()
        {
            _type = CommandType.freehand;
            _rubberBand = new RubberBandBasic();
        }

        protected bool _tracking = false;
        protected bool _saveSnap;

        Point _cursorStart;

        protected override CursorType cursorType
        {
            get
            {
                return CursorType.Draw;
            }
        }

        public override InputMode InputMode
        {
            get
            {
                return InputMode.Drag;
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            _tracking = true;
            _saveSnap = Globals.Input.GridSnap;
            Globals.Input.GridSnap = false;

            if (Globals.DrawingTools.ActiveTrianglePoint(out Point p))
            {
                x = p.X;
                y = p.Y;
            }

            _rubberBand.StartTracking(x, y);
            _rubberBand.State = 1;

            // Start point
            _first = new Point(x, y);

            _isTrackingTriangle = false;
            _triangleStartIndex = -1;
        }

        bool _isTrackingTriangle = false;
        int _triangleStartIndex = -1;
        Point _triangleStartPoint;
        Point _triangleAlignemntPoint;

        public override void TrackCursor(double x, double y)
        {
            if (_tracking)
            {
                bool trackingTriangle = Globals.DrawingTools.ActiveTrianglePoint(out Point p);

                if (_isTrackingTriangle)
                {
                    p = Construct.NormalPointToLine(p, _triangleStartPoint, _triangleAlignemntPoint);
                    x = p.X;
                    y = p.Y;
                }
                else if (trackingTriangle)
                {
                    x = p.X;
                    y = p.Y;

                    if (_wkPolyline != null)
                    {
                        if (_triangleStartIndex < 0 && _wkPolyline.Points.Count > 0)
                        {
                            // this is the first tracking point
                            _triangleStartIndex = _wkPolyline.Points.Count - 1;
                            _triangleStartPoint = p;
                        }
                        else if (!_isTrackingTriangle && (Math.Abs(_triangleStartPoint.X - p.X) > .2 || Math.Abs(_triangleStartPoint.X - p.Y) > .2))
                        {
                            _isTrackingTriangle = true;
                            _triangleAlignemntPoint = p;

                            if (_wkPolyline.Points.Count > 0 && _triangleStartIndex >= 0)
                            {
                                while (_wkPolyline.Points.Count > (_triangleStartIndex + 1))
                                {
                                    _wkPolyline.RemovePoint(false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    _triangleStartIndex = -1;
                    _isTrackingTriangle = false;
                }

                double dx = x - _cursorStart.X;
                double dy = y - _cursorStart.Y;

                _rubberBand.TrackCursor(x, y);

                if (dx != 0 || dy != 0)
                {
                    // Add point
                    _cursorStart = new Point(x, y);

                    if (_wkPolyline == null)
                    {
                        _wkPolyline = new PLine(_first, _cursorStart);
                        _wkPolyline.ConstructEnabled = false;

                        _wkPolyline.AddToContainer(Globals.ActiveDrawing);
                    }
                    else if (_isTrackingTriangle && _triangleStartIndex <= 0)
                    {
                        _wkPolyline.ReplaceEndPoint(x, y, true);
                    }
                    else
                    {
                        _wkPolyline.AddPoint(x, y, true);
                    }
                }
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_tracking)
            {
                Globals.Input.GridSnap = _saveSnap;
                _tracking = false;
            }

            if (_wkPolyline != null)
            {
                _wkPolyline.RemoveColinearPoints();

                _lastObject = _wkPolyline;
                _wkPolyline.ConstructEnabled = true;
                Globals.Events.PrimitiveCreated(_lastObject);
                Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, _wkPolyline);
                _wkPolyline = null;
            }
        }
    }

    public class PolygonCommandProcessor : PolylineCommandProcessor
    {
        bool _gap = false;

        public PolygonCommandProcessor() : base(false)
        {
            switch (Globals.PolygonFill)
            {
                case (uint)ColorCode.ByLayer:
                case (uint)ColorCode.SameAsOutline:
                    Layer layer = Globals.LayerTable[Globals.LayerId];
                    break;

                case (uint)ColorCode.NoFill:
                    break;

                default:
                    break;
            }

            _type = CommandType.polygon;

            _rubberBand = new RubberBandLine();
        }

        protected override PLine instantiate(Point s, Point e)
        {
            PPolygon p = new PPolygon(s, e);
            p.ConstructEnabled = false;
            p.ZIndex = Globals.ActiveDrawing.MaxZIndex;
            p.Opacity = .5;
            p.IsDynamic = true;

            //_rubberBand = new RubberBandBasic();

            return p;
        }

        private void TrimNullPolygonFigures()
        {
            if (_wkPolyline != null)
            {
                int startIndex = -1;
                int trimCount = 0;

                List<CPoint> cpa = _wkPolyline.CPoints;
                List<CPoint> cpb = new List<CPoint>();

                for (int i = 0; i < cpa.Count; i++)
                {
                    if (cpa[i].M == 0)
                    {
                        if ((i - startIndex) > 2)
                        {
                            for (int j = Math.Max(startIndex, 0); j < i; j++)
                            {
                                cpb.Add(cpa[j]);
                            }
                        }
                        else if (i > startIndex)
                        {
                            trimCount++;
                        }

                        startIndex = i;
                    }
                }

                if (cpb.Count > 0)
                {
                    if ((cpa.Count - startIndex) > 2)
                    {
                        for (int j = Math.Max(startIndex, 0); j < cpa.Count; j++)
                        {
                            cpb.Add(cpa[j]);
                        }
                    }

                    _wkPolyline.CPoints = cpb;
                }
            }
        }

        public override void KeyDown(string key, bool shift, bool control, bool gmk)
        {
            if (key == "186")   // ";"
            {
                if (_wkPolyline != null)
                {
                    if (_wkPolyline.Points.Count > 2)
                    {
                        _gap = true;

                        _wkPolyline.RemovePoint();

                        _wkPolyline.CPoints[_wkPolyline.Points.Count - 1] = new CPoint(_through.X - _wkPolyline.Origin.X, _through.Y - _wkPolyline.Origin.Y, 0);
                        _wkPolyline.Draw();
                    }
                    else
                    {
                        Globals.ActiveDrawing.RemovePrimitive(_wkPolyline);
                        _wkPolyline = null;
                    }
                }
            }
            base.KeyDown(key, shift, control, gmk);
        }

        protected override void addPoint(Point p)
        {
            if (_wkPolyline == null)
            {
                _wkPolyline = instantiate(p, p);
                _wkPolyline.AddToContainer(Globals.ActiveDrawing);
                _through = p;
                _rubberBand.State = 2;
            }
            else
            {
                if (_wkPolyline.Points.Count == 1)
                {
                    _rubberBand.EndTracking();

                    RubberBandBasic rbb = new RubberBandBasic();
                    rbb.ShowCoordinateVector = true;
                    _rubberBand = rbb;
                    _rubberBand.State = 2;
                }

                if (_wkPolyline.Points.Count > 1)
                {
                    Point t = _wkPolyline.Points[_wkPolyline.Points.Count - 2];
                    _through = new Point(t.X + _wkPolyline.Origin.X, t.Y + _wkPolyline.Origin.Y);
                    _start = p;

                }
                if (_gap)
                {
                    _gap = false;
                    _wkPolyline.AddPoint(new CPoint(p, 0), true);
                    _wkPolyline.AddPoint(p.X, p.Y, true);
                }
                else
                {
                    _wkPolyline.AddPoint(p.X, p.Y, true);
                }
            }

            _rubberBand.StartTracking(p.X, p.Y);
        }

        private void AddVectorEntity(VectorEntity ve)
        {
            int undoCount = 0;

            if (ve.Children != null)
            {
                foreach (object o in ve.Children)
                {
                    if (o is List<Point>)
                    {
                        List<Point> pc = o as List<Point>;

                        if (pc.Count > 1)
                        {
                            if (Construct.Distance(pc[0], pc[pc.Count - 1]) < .01)
                            {
                                // add a closed figure
                                if (_wkPolyline == null)
                                {
                                    // this is the start of a new polygon
                                    addPoint(pc[0]);
                                    undoCount++;
                                }
                                else if (_wkPolyline.CPoints.Count > 1)
                                {
                                    if (_gap)
                                    {
                                        addPoint(pc[0]);
                                        undoCount++;
                                    }
                                    else
                                    {
                                        _wkPolyline.CPoints[_wkPolyline.Points.Count - 1] = new CPoint(pc[0].X - _wkPolyline.Origin.X, pc[0].Y - _wkPolyline.Origin.Y, 0);
                                        _gap = true;
                                    }
                                }
                                else
                                {
                                    Globals.ActiveDrawing.RemovePrimitive(_wkPolyline);
                                    _wkPolyline = null;

                                    addPoint(pc[0]);
                                    undoCount++;
                                }

                                undoCount += pc.Count;

                                for (int i = 1; i < pc.Count; i++)
                                {
                                    addPoint(pc[i]);
                                }

                                addPoint(pc[0]);
                                undoCount++;

                                _gap = true;
                            }
                            else
                            {
                                Point cs = _through;
                                Point ce = _through;
                                Point ns = pc[0];
                                Point ne = pc[pc.Count - 1];

                                if (_wkPolyline != null && _wkPolyline.Points.Count > 0)
                                {
                                    cs = _wkPolyline.Origin;
                                    ce = _wkPolyline.EndPoint;
                                }

                                double csns = Construct.Distance(cs, ns);
                                double csne = Construct.Distance(cs, ne);
                                double cens = Construct.Distance(ce, ns);
                                double cene = Construct.Distance(ce, ne);

                                double tol = Globals.View.DisplayToPaper(Globals.hitTolerance);

                                if (_wkPolyline != null && csns < tol && csne > tol)
                                {
                                    _wkPolyline.Reverse();

                                    cs = _wkPolyline.Origin;
                                    ce = _wkPolyline.EndPoint;

                                    csns = Construct.Distance(cs, ns);
                                    csne = Construct.Distance(cs, ne);
                                    cens = Construct.Distance(ce, ns);
                                    cene = Construct.Distance(ce, ne);
                                }

                                if (csne > tol && cens > cene)
                                {
                                    // reverse the points and add the open figure
                                    undoCount += pc.Count;

                                    for (int i = pc.Count - 1; i >= 0; --i)
                                    {
                                        addPoint(pc[i]);
                                    }

                                    addPoint(pc[0]);
                                    undoCount++;
                                }
                                else
                                {
                                    // add an open figure
                                    undoCount += pc.Count;

                                    foreach (Point p in pc)
                                    {
                                        addPoint(p);
                                    }

                                    addPoint(pc[pc.Count - 1]);
                                    undoCount++;
                                }
                            }
                        }
                    }
                    else if (o is VectorEntity)
                    {
                        AddVectorEntity(o as VectorEntity);
                    }
                }
            }

            if (undoCount > 1)
            {
                Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal, CommandProcessorActionID.EnterPoint, _start, undoCount);
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            if (shift)
            {
                Primitive p = null;

                VectorContext vc = new VectorContext(false, false, false);
                if (_wkPolyline == null)
                {
                    p = Globals.ActiveDrawing.Pick(x, y, true);
                }
                else
                {
                    VectorEntity vep = Globals.DrawingCanvas.VectorListControl.VectorList.GetSegment(_wkPolyline.Id);
                    if (vep != null)
                    {
                        bool wasSelectable = vep.IsSelectable;
                        vep.IsSelectable = false;
                    
                        p = Globals.ActiveDrawing.Pick(x, y, true);
                    
                        vep.IsSelectable = wasSelectable;
                    }
                    else
                    {
                        p = Globals.ActiveDrawing.Pick(x, y, true);
                    }
                }
                if (p != null && p != _wkPolyline)
                {
                    switch (p.TypeName)
                    {
                        case PrimitiveType.Text:
                        case PrimitiveType.Dimension:
                        case PrimitiveType.Arrow:
                            // don't add annotation primitiives
                            break;

                        default:
                            {
                                if (_wkPolyline == null)
                                {
                                    _through = new Point(x, y);
                                }
                                else
                                {
                                    _wkPolyline.RemovePoint();
                                }

                                VectorEntity ve = p.Vectorize(vc);
                                AddVectorEntity(ve);

                                if (_wkPolyline != null)
                                {
                                    _wkPolyline.Draw();
                                }
                            }
                            break;
                    }
                }
            }
            else
            {
                base.StartTracking(x, y, shift, control);
            }
        }

        public override bool EnableCommand(object o)
        {
            bool enable = base.EnableCommand(o);

            if (enable == false)
            {
                if (o is string && (string)o == "A_Done")
                {
                    enable = _wkPolyline != null;
                }
            }

            return enable;
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            if ((CommandProcessorActionID)subject == CommandProcessorActionID.EnterPoint)
            {
                int count = 1;

                if (predicate2 is int)
                {
                    count = (int)predicate2;
                }

                if (_wkPolyline != null)
                {
                    for (int i = 0; i < count && _wkPolyline.Points.Count > 0; i++)
                    {
                        _wkPolyline.RemovePoint();
                    }

                    _first = _start = _wkPolyline.EndPoint;

                    if (_wkPolyline.Points.Count < 1)
                    {
                        Globals.ActiveDrawing.RemovePrimitive(_wkPolyline);
                        _wkPolyline = null;
                    }
                    else
                    {
                        if (_wkPolyline.Points.Count <= 1)
                        {
                            _through = _wkPolyline.Origin;
                        }
                        else
                        {
                            Point t = _wkPolyline.Points[_wkPolyline.Points.Count - 2];
                            _through.X = t.X + _wkPolyline.Origin.X;
                            _through.Y = t.Y + _wkPolyline.Origin.Y;
                        }
                    }

                    _rubberBand.StartTracking(_start.X, _start.Y);
                }
                else
                {
                    _rubberBand.Reset();
                    _rubberBand.State = 0;
                    _through = _start;
                }
            }
        }

        public override void EnterPoint(Point p)
        {
            if (_wkPolyline != null)
            {
                _wkPolyline.Points[_wkPolyline.Points.Count - 1] = new Point(p.X - _wkPolyline.Origin.X, p.Y - _wkPolyline.Origin.Y);
            }
            base.EnterPoint(p);
        }

        public override void TrackCursor(double x, double y)
        {
            base.TrackCursor(x, y);

            if (_wkPolyline != null)
            {
                if (_gap)
                {
                    _wkPolyline.CPoints[_wkPolyline.Points.Count - 1] = new CPoint(x - _wkPolyline.Origin.X, y - _wkPolyline.Origin.Y, 0);
                }
                else
                {
                    _wkPolyline.Points[_wkPolyline.Points.Count - 1] = new Point(x - _wkPolyline.Origin.X, y - _wkPolyline.Origin.Y);
                }
                _wkPolyline.Draw();
            }
        }

        public override void Finish()
        {
            if (_wkPolyline != null)
            {
                if (_wkPolyline.Points.Count > 1)
                {
                    if (_gap == false)
                    {
                        _wkPolyline.Points.RemoveAt(_wkPolyline.Points.Count - 1);
                    }
                }

                TrimNullPolygonFigures();

                if (_wkPolyline.Points.Count > 1)
                {
                    PPolygon polygon = new PPolygon(_wkPolyline.Origin, _wkPolyline.CPoints);
                    polygon.LayerId = _wkPolyline.LayerId;

                    Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

                    addPrimitive(polygon);

                    _lastObject = polygon;
                    Globals.Events.PrimitiveCreated(_lastObject);
                }

                Globals.ActiveDrawing.RemovePrimitive(_wkPolyline);
                _wkPolyline = null;
            }

            base.Finish();

            _gap = false;
        }
    }

    public class DimensionCommandProcessor : CommandProcessor
    {
        PDimension _wkDimension = null;

        public DimensionCommandProcessor()
        {
            _type = CommandType.dimension;

            RubberBandArrow rba = new RubberBandArrow(_color);
            rba.ArrowStyle = Globals.ArrowStyleTable[0];
            _rubberBand = rba;
        }

        public override bool EnableCommand(object o)
        {
            bool enable = base.EnableCommand(o);

            if (enable == false)
            {
                if (o is string && (string)o == "A_Done")
                {
                    enable = _wkDimension == null ? false : _wkDimension.Points.Count > 0;
                }
            }

            return enable;
        }

        protected void addPoint(Point p)
        {
            _rubberBand.StartTracking(p.X, p.Y);

            if (_wkDimension == null)
            {
                _first = p;
            }

            if (_isTrackingDimension)
            {
                _isTrackingDimension = false;
            }
            else
            {
                RubberBandArrow rba = _rubberBand as RubberBandArrow;

                if (_wkDimension == null)
                {
                    _wkDimension = new PDimension(_first, p);
                    _wkDimension.ConstructEnabled = false;
                    _wkDimension.ZIndex = Globals.ActiveDrawing.MaxZIndex;

                    _wkDimension.AddToContainer(Globals.ActiveDrawing);

                    rba.ArrowLocation = ArrowLocation.End;
                    rba.StartTracking(p.X, p.Y);
                }
                else
                {
                    _wkDimension.AddPoint(p.X, p.Y, true);
                }

                if (_wkDimension.Points.Count == 0)
                {
                    rba.State = 1;
                }
                else
                {
                    rba.State = 0;
                }
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            addPoint(_start);

            Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal, CommandProcessorActionID.EnterPoint, _start);
        }

        bool _isTrackingDimension = false;

        public override void TrackCursor(double x, double y)
        {
            base.TrackCursor(x, y);

            if (_wkDimension != null)
            {
                if (_wkDimension.DimensionType == PDimension.DimType.PointToPoint && _wkDimension.Points.Count == 0)
                {
                    if (_isTrackingDimension == false)
                    {
                        if (Math.Abs(_start.X - x) > .1 || Math.Abs(_start.Y - y) > .1)
                        {
                            addPoint(_start);
                            _isTrackingDimension = true;
                        }
                    }
                    else
                    {
                        _wkDimension.Points[_wkDimension.Points.Count - 1] = new Point(x - _wkDimension.Origin.X, y - _wkDimension.Origin.Y);
                    }
                    _wkDimension.Draw();
                }
                else if (_wkDimension.Points.Count >= 1)
                {
                    if (_isTrackingDimension == false)
                    {
                        if (Math.Abs(_start.X - x) > .1 || Math.Abs(_start.Y - y) > .1)
                        {
                            addPoint(_start);
                            _isTrackingDimension = true;
                        }
                    }
                    else
                    {
                        _wkDimension.Points[_wkDimension.Points.Count - 1] = new Point(x - _wkDimension.Origin.X, y - _wkDimension.Origin.Y);
                    }
                    _wkDimension.Draw();
                }
            }
        }

        public override void CanvasScaleChanged()
        {
            // Resize handles after canvas scale change
            Point p = Globals.Input.CursorLocation;
            _rubberBand.TrackCursor(p.X, p.Y);

            base.CanvasScaleChanged();
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

            if (_wkDimension != null)
            {
                _wkDimension.ZIndex = Globals.ActiveDrawing.MaxZIndex;

                if (_isTrackingDimension)
                {
                    // _wkDimension includes an extra point for cursor tracking that hasn't been saved yet
                    _wkDimension.RemovePoint();
                    _isTrackingDimension = false;
                }

                if (_wkDimension.DimensionType == PDimension.DimType.PointToPoint && _wkDimension.Points.Count < 1)
                {
                    Globals.ActiveDrawing.RemovePrimitive(_wkDimension);
                }
                if (_wkDimension.DimensionType != PDimension.DimType.PointToPoint && _wkDimension.Points.Count <= 2)
                {
                    Globals.ActiveDrawing.RemovePrimitive(_wkDimension);
                }
                else
                {
                    _lastObject = _wkDimension;
                    _wkDimension.ConstructEnabled = true;
                    Globals.Events.PrimitiveCreated(_lastObject);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, _wkDimension);
                }
                _wkDimension = null;
            }

            _rubberBand.EndTracking();
            _rubberBand.State = 0;

            _isTrackingDimension = false;

            base.Finish();
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            if ((CommandProcessorActionID)subject == CommandProcessorActionID.EnterPoint)
            {
                _rubberBand.EndTracking();
                _rubberBand.Reset();

                if (_wkDimension != null)
                {
                    int count = _wkDimension.RemovePoint();

                    if (_isTrackingDimension)
                    {
                        // _wkDimension includes an extra point for cursor tracking that hasn't been saved yet
                        _wkDimension.RemovePoint();
                        _isTrackingDimension = false;
                    }

                    if (count == 1)
                    {
                        _start = _wkDimension.Origin;

                        RubberBandArrow rba = new RubberBandArrow(_color);
                        rba.ArrowStyle = Globals.ArrowStyleTable[0];
                        _rubberBand = rba;

                        rba.ArrowLocation = ArrowLocation.End;
                        rba.StartTracking(_start.X, _start.Y);
                        rba.State = 1;
                    }
                    else if (count == 0)
                    {
                        _start = _wkDimension.Origin;
                        Globals.ActiveDrawing.RemovePrimitive(_wkDimension);
                        _wkDimension = null;

                        _rubberBand.EndTracking();
                        _rubberBand.Reset();

                        _isTrackingDimension = false;
                    }
                }
                else
                {
                    _rubberBand.EndTracking();
                }
            }

            base.UndoInternalCommand(subject, predicate, predicate2);
        }

        protected override void RedoInternalCommand(object subject, object predicate, object predicate2)
        {
            if (subject is CommandProcessorActionID && (CommandProcessorActionID)subject == CommandProcessorActionID.EnterPoint && predicate is Point)
            {
                if (_isTrackingDimension)
                {
                    // _wkDimension includes an extra point for cursor tracking that hasn't been saved yet
                    _wkDimension.RemovePoint();
                    _isTrackingDimension = false;
                }

                addPoint((Point)predicate);
            }

            base.RedoInternalCommand(subject, predicate, predicate2);
        }
    }

    public class ArrowCommandProcessor : PolylineCommandProcessor
    {
        public ArrowCommandProcessor() : base(false)
        {
            _type = CommandType.arrow;

            _rubberBand = new RubberBandArrow(_color);
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            RubberBandArrow rba = _rubberBand as RubberBandArrow;

            if (_wkPolyline == null)
            {
                if (Globals.ArrowLocation == ArrowLocation.End)
                {
                    rba.ArrowLocation = ArrowLocation.End;
                }
                else
                {
                    rba.ArrowLocation = Globals.ArrowLocation;
                }
            }
            else if (Globals.ArrowLocation == ArrowLocation.Both || Globals.ArrowLocation == ArrowLocation.End)
            {
                rba.ArrowLocation = ArrowLocation.End;
            }
            else
            {
                rba.ArrowLocation = ArrowLocation.None;
            }
        }

        public override void Start()
        {
            _rubberBand = new RubberBandArrow(_color);
            base.Start();
        }

        protected override PLine instantiate(Point s, Point e)
        {
            return new PArrow(s, e);
        }
    }

    public class DoublelineCommandProcessor : CommandProcessor
    {
        protected PDoubleline _wkDBline = null;

        public DoublelineCommandProcessor()
        {
            _type = CommandType.doubleline;

            _rubberBand = new RubberBandDoubleline(_color);
        }

        // State 0
        //    No RB
        //    Start tracking
        //      Save anchor point
        //      State = 1
        // State = 1
        //    RB from anchor
        //    Start tracking
        //       Create line from anchor to current
        //       Save anchor
        //       State = 2
        // State = 2
        //    RB from anchor
        //    Start tracking
        //       Add current point to line
        //       Save anchor
        //       State = 2

        protected void addPoint(Point p)
        {
            if (_rubberBand.State == 0)
            {
                _first = p;

                _rubberBand.State = 1;
            }
            else if (_rubberBand.State == 1)
            {
                if (_first.X != p.X || _first.Y != p.Y)
                {
                    _wkDBline = new PDoubleline(new CPoint(_first, 0), new CPoint(p, 3));
                    _wkDBline.ConstructEnabled = false;
                
                    if (_joinToAtStart != null)
                    {
                        CGeometry.JoinWalls(_joinToAtStart, 0, _wkDBline, 0);
                        _joinToAtStart = null;
                    }

                    _wkDBline.AddToContainer(Globals.ActiveDrawing);

                    _rubberBand.State = 2;
                }
                _through = _first;
            }
            else if (_wkDBline != null)
            {
                Point e = _wkDBline.EndPoint;
                if (p.X != e.X || p.Y != e.Y)
                {
                    _wkDBline.AddPoint(p.X, p.Y, true);
                }
                _through = e;
            }

            _rubberBand.StartTracking(p.X, p.Y);
            _rubberBand.TrackCursor(p.X, p.Y);
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);
            addPoint(_start);
            Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal, CommandProcessorActionID.EnterPoint, _start);
        }

        public override bool EnableCommand(object o)
        {
            bool enable = base.EnableCommand(o);

            if (enable == false)
            {
                if (o is string && (string)o == "A_Done")
                {
                    enable = _rubberBand.State != 0;
                }
            }

            return enable;
        }

        PDoubleline _joinToAtStart = null;

        public override void KeyDown(string key, bool shift, bool control, bool gmk)
        {
            if (key == "j")
            {
                Point loc = Globals.Input.CursorLocation;
                Primitive p = Globals.ActiveDrawing.Pick(loc.X, loc.Y, true);
                if (p is PDoubleline)
                {
                    double pv;
                    int segment = ((PDoubleline)p).PickSegment(ref loc, out pv);
                    if (pv > 0 && pv < 1)
                    {
                        addPoint(loc);
                        if (_wkDBline == null)
                        {
                            // join the start
                            _joinToAtStart = p as PDoubleline;
                        }
                        else if (_wkDBline.Points.Count > 0)
                        {
                            // join the end
                            Finish();

                            PDoubleline pd = _lastObject as PDoubleline;
                            CGeometry.JoinWalls(p as PDoubleline, (uint)segment, pd, (uint)pd.Points.Count);
                            _lastObject.Draw();
                            p.Draw();
                        }
                    }
                }
            }
            base.KeyDown(key, shift, control, gmk);
        }

        protected override void Hover(Point from, Point current, Point through)
        {
            if (_rubberBand.State >= 1)
            {
                _constructNodes.Clear();

                // Check for ActiveDrawing not null - this could be hit after ActiveDrawing is closed
                if (Globals.ActiveDrawing != null)
                {
                    foreach (Primitive p in Globals.ActiveDrawing.PrimitiveList)
                    {
                        if (Globals.LayerTable[p.LayerId].Visible && p.IsNear(current, _hoverDistance))
                        {
                            _constructNodes.AddRange(p.ConstructNodes);
                            _constructNodes.AddRange(p.DynamicConstructNodes(from, through));
                        }
                    }
                    _constructNodes.AddRange(Globals.DrawingTools.DynamicTriangleConstructNodes(from, through));
                }
            }
            else
            {
                base.Hover(from, current, through);
            }

            if (_wkDBline != null && Math.Abs(current.X - _wkDBline.Origin.X) < _hoverDistance && Math.Abs(current.Y - _wkDBline.Origin.Y) < _hoverDistance)
            {
                _constructNodes.Add(new ConstructNode(_wkDBline.Origin, "origin"));
            }
        }

        public override void Start()
        {
            _rubberBand = new RubberBandDoubleline(_color);
            base.Start();
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

            if (_wkDBline != null)
            {
                _lastObject = _wkDBline;
                _wkDBline.ConstructEnabled = true;
                Globals.Events.PrimitiveCreated(_lastObject);
                Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, _wkDBline);

                if (_wkDBline.JoinStart != 0)
                {
                    // TODO
                    // this is a (hopefully) temporary hack - need to fixup the doubleline joined at the start
                    // the end is taken care of elsewhere
                    uint tid = _wkDBline.JoinStart;
                    PDoubleline toDb = Globals.ActiveDrawing.FindObjectById(tid) as PDoubleline;
                    toDb.Draw();
                }
                _wkDBline = null;
            }

            _rubberBand.EndTracking();
            _rubberBand.State = 0;

            base.Finish();
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            if ((CommandProcessorActionID)subject == CommandProcessorActionID.EnterPoint)
            {
                if (_wkDBline != null)
                {
                    int remaining = _wkDBline.RemovePoint();

                    if (remaining > 0)
                    {
                        _rubberBand.EndTracking();

                        _first = _start = _wkDBline.EndPoint;
                        _rubberBand.StartTracking(_start.X, _start.Y);
                        _rubberBand.TrackCursor(_start.X, _start.Y);

                        if (_wkDBline.Points.Count == 1)
                        {
                            _through = _wkDBline.Origin;
                        }
                        else
                        {
                            Point t = _wkDBline.Points[_wkDBline.Points.Count - 2];
                            _through.X = t.X + _wkDBline.Origin.X;
                            _through.Y = t.Y + _wkDBline.Origin.Y;
                        }
                    }
                    else
                    {
                        _start = _wkDBline.Origin;
                        _through = _start;

                        Globals.ActiveDrawing.RemovePrimitive(_wkDBline);

                        _wkDBline = null;
                        _rubberBand.EndTracking();

                        if (_rubberBand.State == 2)
                        {
                            _first = _start;

                            _rubberBand.State = 1;

                            _rubberBand.StartTracking(_start.X, _start.Y);
                            _rubberBand.TrackCursor(_start.X, _start.Y);
                        }
                        else
                        {
                            _rubberBand.State = 0;
                        }
                    }
                }
                else
                {
                    _rubberBand.Reset();
                    _rubberBand.State = 0;
                    _through = _start;
                }
            }
        }

        protected override void RedoInternalCommand(object subject, object predicate, object predicate2)
        {
         if (subject is CommandProcessorActionID && (CommandProcessorActionID)subject == CommandProcessorActionID.EnterPoint && predicate is Point)
            {
                _start = (Point)predicate;
                addPoint((Point)predicate);
            }

            base.RedoInternalCommand(subject, predicate, predicate2);
        }
    }

    public class CircleCommandProcessor : CommandProcessor
    {
        public CircleCommandProcessor()
        {
            _type = CommandType.circle;


            if (Globals.CircleCommandType == ArcCommandType.CenterRadiusStartEnd)
            {
                _rubberBand = new RubberBandBasic();
            }
            else
            {
                _rubberBand = new RubberBandCircle(_color);

                _rubberBand.FillColor = ColorFromFillColorSpec(Globals.CircleFill);

                if (string.IsNullOrEmpty(Globals.CirclePattern) == false && Globals.CirclePattern != "Solid")
                {
                    _rubberBand.FillPattern = Globals.CirclePattern;
                    _rubberBand.PatternScale = Globals.CirclePatternScale;
                    _rubberBand.PatternAngle = Globals.CirclePatternAngle;
                }
            }
        }

        // State 0: None
        // State 1: Have center point
        // State 2: Have radius point

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            if (Globals.CircleCommandType == ArcCommandType.CenterRadiusStartEnd)
            {
                Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

                _lastObject = addPrimitive(new PArc(_start, Globals.CircleRadius));
                Globals.Events.PrimitiveCreated(_lastObject);
            }
            else if (_rubberBand.State == 0)
            {
                _first = _start;

                _rubberBand.State = 1;
                _rubberBand.StartTracking(_start.X, _start.Y);
                Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal);
            }
            else
            {
                Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);
                _rubberBand.EndTracking();

                double r = Construct.Distance(_first, _start);
                if (r > 0)
                {
                    double a = Construct.Angle(_first, _start);
                    _lastObject = addPrimitive(new PArc(_first, r));
                    Globals.Events.PrimitiveCreated(_lastObject);
                }

                Start();

                _rubberBand.State = 0;
            }
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);
            base.Finish();
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            // TODO: Update internal command logic
            _rubberBand.EndTracking();

            if (_rubberBand.State == 1)
            {
                _rubberBand.State = 0;
            }
            else if (_rubberBand.State == 0)
            {
                /*
                 * This undo state (undo first point after previous object is created) 
                 * no longer exists for this command                 * 
                 */
            }
        }
    }

    public class RegularPolygonCommandProcessor : CommandProcessor
    {
        uint _sides = 3;

        public RegularPolygonCommandProcessor(uint sides)
        {
            _sides = sides;

            _type = CommandType.polygon;
            _rubberBand = new RubberBandBasic();
        }

        PPolygon _wkPoly = null;

        private List<Point> RegularPolygonPoints(Point center, Point start, uint sides)
        {
            List<Point> pc = new List<Point>();

            if (sides > 2 && center != start)
            {
                double r = Construct.Distance(center, start);
                double ainc = Math.PI * 2 / sides;
                double angle = Construct.Angle(center, start);

                for (int i = 0; i < sides; i++)
                {
                    Point p = Construct.PolarOffset(center, r, angle);
                    pc.Add(p);
                    angle += ainc;
                }
                pc.Add(pc[0]);
            }

            return pc;
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            if (_rubberBand.State == 0)
            {
                _first = _start;

                _rubberBand.State = 1;
                _rubberBand.StartTracking(_start.X, _start.Y);
            }
            else
            {
                _rubberBand.EndTracking();

                if (_wkPoly != null)
                {
                    List<Point> pc = RegularPolygonPoints(_first, new Point(x, y), _sides);
                    if (pc.Count > 3)
                    {
                        _wkPoly.InitWithPointCollection(pc);
                    }
                    _wkPoly.ConstructEnabled = true;
                }

                Start();

                _rubberBand.State = 0;
                _wkPoly = null;
            }
        }

        public override void TrackCursor(double x, double y)
        {
            base.TrackCursor(x, y);

            if (_rubberBand.State == 1)
            {
                List<Point> pc = RegularPolygonPoints(_first, new Point(x, y), _sides);
                if (pc.Count > 3)
                {
                    if (_wkPoly == null)
                    {
                        _wkPoly = new PPolygon(pc);
                        _wkPoly.ConstructEnabled = false;
                        addPrimitive(_wkPoly);

                        _lastObject = _wkPoly;
                        Globals.Events.PrimitiveCreated(_lastObject);
                    }
                    else
                    {
                        _wkPoly.InitWithPointCollection(pc);
                    }
                }
            }
        }

        public override void Finish()
        {
            _wkPoly = null;

            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);
            base.Finish();
        }
    }

    public class Circle3CommandProcessor : Arc3CommandProcessor
    {
        public Circle3CommandProcessor()
        {
            _type = CommandType.circle3;

            _rubberBand = new RubberBandCircle3(_color);
            _isCircle = true;

            _rubberBand.FillColor = ColorFromFillColorSpec(Globals.CircleFill);

            if (string.IsNullOrEmpty(Globals.CirclePattern) == false && Globals.CirclePattern != "Solid")
            {
                _rubberBand.FillPattern = Globals.CirclePattern;
                _rubberBand.PatternScale = Globals.CirclePatternScale;
                _rubberBand.PatternAngle = Globals.CirclePatternAngle;
            }
        }
    }

    public class BSplineCommandProcessor : CommandProcessor
    {
        protected PBSpline _wkBSpline = null;

        public BSplineCommandProcessor()
        {
            _type = CommandType.bspline;
            _rubberBand = new RubberBandBSpline(_color);
        }

        public override bool EnableCommand(object o)
        {
            bool enable = base.EnableCommand(o);

            if (enable == false)
            {
                if (o is string && (string)o == "A_Done")
                {
                    enable = _rubberBand.State != 0;
                }
            }

            return enable;
        }

        // State 0
        //    No RB
        //    Start tracking
        //      Save anchor point
        //      State = 1
        // State = 1
        //    RB from anchor
        //    Start tracking
        //       Create line from anchor to current
        //       Save anchor
        //       State = 2
        // State = 2
        //    RB from anchor
        //    Start tracking
        //       Add current point to line
        //       Save anchor
        //       State = 2

        protected void addPoint(Point p)
        {
            if (_rubberBand.State == 0)
            {
                Start();

                _first = p;

                _rubberBand.State = 1;
            }
            else
            {
                if (_rubberBand.State == 1)
                {
                    if (_first.X != p.X || _first.Y != p.Y)
                    {
                        _wkBSpline = new PBSpline(_first, p);
                        _wkBSpline.ConstructEnabled = false;
                        _wkBSpline.DrawFinalSegment = false;
                        _wkBSpline.IsVisible = false;

                        _wkBSpline.AddToContainer(Globals.ActiveDrawing);

                        _rubberBand.State = 2;
                    }
                }
                else if (_wkBSpline != null)
                {
                    Point e = _wkBSpline.EndPoint;
                    if (p.X != e.X || p.Y != e.Y)
                    {
                        _wkBSpline.AddPoint(p.X, p.Y, true);
                    }
                    _wkBSpline.IsVisible = true;
                }
            }

            _rubberBand.StartTracking(p.X, p.Y);
            _rubberBand.TrackCursor(p.X, p.Y);
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);
            addPoint(_start);
            Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal, CommandProcessorActionID.EnterPoint, _start);
        }

        protected override void Hover(Point from, Point to, Point through)
        {
            base.Hover(from, to, through);

            if (_wkBSpline != null && Math.Abs(to.X - _wkBSpline.Origin.X) < _hoverDistance && Math.Abs(to.Y - _wkBSpline.Origin.Y) < _hoverDistance)
            {
                _constructNodes.Add(new ConstructNode(_wkBSpline.Origin, "origin"));
            }
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

            if (_wkBSpline != null)
            {
                _lastObject = _wkBSpline;
                _wkBSpline.ConstructEnabled = true;
                _wkBSpline.DrawFinalSegment = true;
                Globals.Events.PrimitiveCreated(_lastObject);
                Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, _wkBSpline);
                _wkBSpline = null;
            }

            _rubberBand.EndTracking();
            _rubberBand.Reset();

            base.Finish();
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            if ((CommandProcessorActionID)subject == CommandProcessorActionID.EnterPoint)
            {
                RubberBandBSpline rbb = _rubberBand as RubberBandBSpline;
                if (rbb != null)
                {
                    rbb.EndTracking();
                    rbb.RemovePoint(2);
                }

                if (_wkBSpline != null)
                {
                    int remaining = _wkBSpline.RemovePoint();

                    if (remaining < 0)
                    {
                        Globals.ActiveDrawing.RemovePrimitive(_wkBSpline);
                        _wkBSpline = null;

                        _rubberBand.EndTracking();
                        _rubberBand.Reset();
                        _rubberBand.State = 0;
                        //return;
                    }

                    Point p = Globals.Input.CursorLocation;
                    _rubberBand.StartTracking(p.X, p.Y);
                    _rubberBand.TrackCursor(p.X, p.Y);
                }
                else
                {
                    _rubberBand.EndTracking();
                }
            }

            base.UndoInternalCommand(subject, predicate, predicate2);
        }

        protected override void RedoInternalCommand(object subject, object predicate, object predicate2)
        {
            if (subject is CommandProcessorActionID && (CommandProcessorActionID)subject == CommandProcessorActionID.EnterPoint && predicate is Point)
            {
                _rubberBand.TrackCursor(_start.X, _start.Y);
                addPoint(_start);
                _rubberBand.TrackCursor(_start.X, _start.Y);
            }

            base.RedoInternalCommand(subject, predicate, predicate2);
        }
    }

    public class ArcCommandProcessor : CommandProcessor
    {
        public ArcCommandProcessor()
        {
            _type = CommandType.arc;
            _rubberBand = new RubberBandArc(_color);

            _rubberBand.FillColor = ColorFromFillColorSpec(Globals.ArcFill);

            if (string.IsNullOrEmpty(Globals.ArcPattern) == false && Globals.ArcPattern != "Solid")
            {
                _rubberBand.FillPattern = Globals.ArcPattern;
                _rubberBand.PatternScale = Globals.ArcPatternScale;
                _rubberBand.PatternAngle = Globals.ArcPatternAngle;
            }

            if (Globals.ArcCommandType == ArcCommandType.CenterRadiusStartEnd)
            {
                ((RubberBandArc)_rubberBand).Radius = Globals.ArcRadius;
            }
        }

        // State 0: None
        // State 1: Have center point
        // State 2: Have start point
        // State 3: Have end point

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            RubberBandArc rba = _rubberBand as RubberBandArc;
            if (rba != null)
            {
                switch (rba.State)
                {
                    case 0:
                    case 3:
                        rba.Center = new Point(_start.X, _start.Y);
                        rba.State = 1;
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal);
                        break;

                    case 1:
                        rba.Start = new Point(_start.X, _start.Y);
                        rba.State = 2;
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal);
                        break;

                    case 2:
                        rba.End = new Point(_start.X, _start.Y);
                        rba.State = 3;
 
                        Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

                        double r = Globals.ArcCommandType == ArcCommandType.CenterRadiusStartEnd ? Globals.ArcRadius : rba.Radius;
                        if (r > 0)
                        {
                            double a =  rba.StartAngle;
                            _lastObject = addPrimitive(new PArc(rba.Center, r, rba.StartAngle, rba.IncludedAngle));
                            Globals.Events.PrimitiveCreated(_lastObject);
                        }

                        _rubberBand.Reset();
                        break;
                }
            }
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            // TODO: Update internal command logic
            RubberBandArc rba = _rubberBand as RubberBandArc;
            if (rba != null)
            {
                Point p = Globals.Input.CursorLocation;

                if (rba.State == 3)
                {
                    /*
                     * This undo state (undo first point after previous object is created) 
                     * no longer exists for this command
                     */
                }
                else if (rba.State > 0)
                {
                    --rba.State;
                }
                _rubberBand.TrackCursor(p.X, p.Y);
            }
        }

        public override void Start()
        {
            base.Start();

            if (Globals.ArcCommandType == ArcCommandType.CenterRadiusStartEnd)
            {
                ((RubberBandArc)_rubberBand).Radius = Globals.ArcRadius;
            }
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

            _rubberBand.EndTracking();
            _rubberBand.Reset();

            base.Finish();

            // this used to happen in the base class - need to investigate...
            Start();
        }
    }

    public class Arc3CommandProcessor : CommandProcessor
    {
        public Arc3CommandProcessor()
        {
            _type = CommandType.arc3;
            _rubberBand = new RubberBandArc3(_color);
        }

        // State 0: None
        // State 1: Have first point
        // State 2: Have second point
        // State 3: Have end point

        protected bool _isCircle = false;

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            RubberBandArc3 rba = _rubberBand as RubberBandArc3;
            if (rba != null)
            {
                _rubberBand.StartTracking(_start.X, _start.Y);

                switch (rba.State)
                {
                    case 0:
                    case 3:
                        rba.Start = new Point(_start.X, _start.Y);
                        rba.State = 1;
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal);
                        break;

                    case 1:
                        if (_start.X != rba.Start.X || _start.Y != rba.Start.Y)
                        {
                            rba.Mid = new Point(_start.X, _start.Y);
                            rba.State = 2;
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal);
                        }
                        break;

                    case 2:
                        if (_start.X != rba.Mid.X || _start.Y != rba.Mid.Y)
                        {
                            rba.End = new Point(_start.X, _start.Y);
                            rba.State = 3;

                            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

                            _lastObject = addPrimitive(new PArc3(rba.Start, rba.Mid, rba.End, _isCircle));
                            Globals.Events.PrimitiveCreated(_lastObject);

                            _rubberBand.Reset();

                            Start();
                        }
                        break;
                }
            }
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            RubberBandArc3 rba = _rubberBand as RubberBandArc3;
            if (rba != null)
            {
                Point p = Globals.Input.CursorLocation;

                if (rba.State == 3)
                {
                    /*
                     * This undo state (undo first point after previous object is created) 
                     * no longer exists for this command
                     */
                }
                else if (rba.State > 0)
                {
                    --rba.State;
                }
                _rubberBand.TrackCursor(p.X, p.Y);
            }
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

            _rubberBand.EndTracking();
            _rubberBand.Reset();

            base.Finish();
        }
    }

    public class Arc2CommandProcessor : CommandProcessor
    {
        public Arc2CommandProcessor()
        {
            _type = CommandType.arc2;
            _rubberBand = new RubberBandArc2(_color);

            _rubberBand.FillColor = ColorFromFillColorSpec(Globals.ArcFill);

            if (string.IsNullOrEmpty(Globals.ArcPattern) == false && Globals.ArcPattern != "Solid")
            {
                _rubberBand.FillPattern = Globals.ArcPattern;
                _rubberBand.PatternScale = Globals.ArcPatternScale;
                _rubberBand.PatternAngle = Globals.ArcPatternAngle;
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            RubberBandArc2 rba = _rubberBand as RubberBandArc2;
            if (rba != null)
            {
                _rubberBand.StartTracking(_start.X, _start.Y);

                switch (rba.State)
                {
                    case 0:
                        _first = _start;
                        rba.Start = new Point(_start.X, _start.Y);
                        rba.State = 1;
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal, CommandProcessorActionID.EnterPoint, _start);
                        break;

                    case 1:
                        if (_start.X != _first.X || _start.Y != _first.Y)
                        {
                            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

                            Point c = new Point((_first.X + _start.X) / 2, (_first.Y + _start.Y) / 2);
                            double r = Construct.Distance(c, _first);
                            double a = Construct.Angle(c, _first);

                            _lastObject = addPrimitive(new PArc(c, r, a, -Math.PI));
                            Globals.Events.PrimitiveCreated(_lastObject);

                            _rubberBand.Reset();

                            Start();
                        }
                        break;
                }
            }
        }

        protected override void RedoInternalCommand(object subject, object predicate, object predicate2)
        {
            if ((CommandProcessorActionID)subject == CommandProcessorActionID.EnterPoint)
            {
                _first = _start;
                RubberBandArc2 rba = _rubberBand as RubberBandArc2;
                if (rba != null)
                {
                    rba.Start = new Point(_start.X, _start.Y);
                    rba.State = 1;
                }
            }
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            Finish();
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

            _rubberBand.EndTracking();
            _rubberBand.Reset();

            base.Finish();
        }
    }

    public class ArcFilletCommandProcessor : CommandProcessor
    {
        public ArcFilletCommandProcessor()
        {
            if (Globals.ArcCommandType == ArcCommandType.FilletRadius)
            {
                _type = CommandType.arcfr;
                _rubberBand = new RubberBandArcFillet(_color);
                ((RubberBandArcFillet)_rubberBand).Radius = Globals.ArcRadius;
            }
            else
            {
                _type = CommandType.arcf;
                _rubberBand = new RubberBandArcFillet(_color);
                ((RubberBandArcFillet)_rubberBand).Radius = 0;
            }
        }

        // State 0: None
        // State 1: Have first point
        // State 2: Have second point
        // State 3: Have end point

        protected bool _isCircle = false;

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            RubberBandArcFillet rba = _rubberBand as RubberBandArcFillet;
            if (rba != null)
            {
                _rubberBand.StartTracking(_start.X, _start.Y);

                switch (rba.State)
                {
                    case 0:
                    case 3:
                        rba.Start = new Point(_start.X, _start.Y);
                        rba.State = 1;
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal);
                        break;

                    case 1:
                        if (_start.X != rba.Start.X || _start.Y != rba.Start.Y)
                        {
                            rba.Mid = new Point(_start.X, _start.Y);
                            rba.State = 2;
                            Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal);
                        }
                        break;

                    case 2:
                        if (_start.X == rba.Mid.X && _start.Y == rba.Mid.Y)
                        {
                            // Same as mid-point - ignore
                        }
                        else if (_start.X == rba.Start.X && _start.Y == rba.Start.Y)
                        {
                            // Same as start-point - ignore
                        }
                        else
                        {
                            rba.End = new Point(_start.X, _start.Y);
                            rba.State = 3;

                            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

                            Point center;
                            double startAngle, includedAngle;

                            if (_type == CommandType.arcf)
                            {
                                double radius;
                                Construct.FilletPoints(rba.Start, rba.Mid, rba.End, out radius, out center, out startAngle, out includedAngle);
                                _lastObject = addPrimitive(new PArc(center, radius, startAngle, includedAngle));
                            }
                            else
                            {
                                Construct.FilletPoints(rba.Start, rba.Mid, rba.End, Globals.ArcRadius, out center, out startAngle, out includedAngle);
                                _lastObject = addPrimitive(new PArc(center, Globals.ArcRadius, startAngle, includedAngle));
                            }

                            Globals.Events.PrimitiveCreated(_lastObject);

                            _rubberBand.Reset();

                            Start();
                        }
                        break;
                }
            }
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            RubberBandArcFillet rba = _rubberBand as RubberBandArcFillet;
            if (rba != null)
            {
                Point p = Globals.Input.CursorLocation;

                if (rba.State == 3)
                {
                    /*
                     * This undo state (undo first point after previous object is created) 
                     * no longer exists for this command
                     */
                }
                else if (rba.State > 0)
                {
                    --rba.State;
                }
                _rubberBand.TrackCursor(p.X, p.Y);
            }
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

            _rubberBand.EndTracking();
            _rubberBand.Reset();

            RubberBandArcFillet rbaf = _rubberBand as RubberBandArcFillet;
            if (rbaf != null && rbaf.Radius > 0)
            {
                ((RubberBandArcFillet)_rubberBand).Radius = Globals.ArcRadius;
            }

            base.Finish();
        }
    }

    public class Arc1CommandProcessor : CommandProcessor
    {
        public Arc1CommandProcessor()
        {
            _type = CommandType.arc;
            _rubberBand = new RubberBandBasic();
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            _lastObject = addPrimitive(new PArc(_start, Globals.ArcRadius, Globals.ArcStartAngle, Globals.ArcIncludedAngle));

            Globals.Events.PrimitiveCreated(_lastObject);
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);
            base.Finish();
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            _rubberBand.EndTracking();

            if (_rubberBand.State == 1)
            {
                _rubberBand.State = 0;
            }
        }
    }

    public class RectangleCommandProcessor : RectCommandProcessorBase
    {
        public RectangleCommandProcessor()
        {
            _type = CommandType.rectangle;
        }

        protected override Primitive instantiate()
        {
            return new PRectangle(new Point(_trackLeft, _trackTop), _trackWidth, _trackHeight);
        }

        // State 0
        //    No RB
        //    Start tracking
        //      Save anchor point
        //      State = 1
        // State = 1
        //    RB from anchor
        //    Start tracking
        //       Create rect from anchor to current
        //       State = 0

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            if (_rubberBand.State == 0)
            {
                _first = _start;

                if (Globals.RectangleType == RectangleCommandType.Corners)
                {
                    _rubberBand.State = 1;
                    _rubberBand.StartTracking(_start.X, _start.Y);
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal);
                }
                else
                {
                    _lastObject = addPrimitive(new PRectangle(_first, Globals.RectangleWidth, -Globals.RectangleHeight));
                    Globals.Events.PrimitiveCreated(_lastObject);
                }
            }
            else
            {
                if (_trackWidth != 0 && _trackHeight != 0)
                {
                    Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

                    _lastObject = addPrimitive(instantiate());
                    Globals.Events.PrimitiveCreated(_lastObject);

                    _rubberBand.State = 0;
                    _rubberBand.EndTracking();

                    Start();
                }
            }
        }

        public override void Start()
        {
            base.Start();

            if (Globals.RectangleType == RectangleCommandType.Corners)
            {
                if (Globals.UIVersion > 0 && Globals.RectangleColorSpec != (uint)ColorCode.ByLayer)
                {
                    _color = Utilities.ColorFromColorSpec(Globals.RectangleColorSpec);
                }
                    
                _rubberBand = new RubberBandRectangle(_color);

                if (Globals.RectangleFill == (uint)ColorCode.NoFill)
                {
                    _rubberBand.FillColor = Colors.Transparent;
                }
                else if (Globals.RectangleFill == (uint)ColorCode.SameAsOutline)
                {
                    _rubberBand.FillColor = _color;
                }
                else if (Globals.RectangleFill == (uint)ColorCode.ByLayer)
                {
                    Layer layer;
                    if (Globals.UIVersion == 0)
                    {
                        layer = Globals.LayerTable[Globals.LayerId];
                    }
                    else
                    {
                        if (Globals.LayerTable.ContainsKey(Globals.ActiveRectangleLayerId))
                        {
                            layer = Globals.LayerTable[Globals.ActiveRectangleLayerId];
                        }
                        else
                        {
                            layer = Globals.LayerTable[Globals.ActiveLayerId];
                        }
                    }
                    Color fillColor = Utilities.ColorFromColorSpec(layer.ColorSpec);
                    _rubberBand.FillColor = fillColor;
                }
                else
                {
                    Color fillColor = Utilities.ColorFromColorSpec(Globals.RectangleFillSpec);
                    _rubberBand.FillColor = fillColor;
                }

                if (string.IsNullOrEmpty(Globals.RectanglePattern) == false && Globals.RectanglePattern != "Solid")
                {
                    _rubberBand.FillPattern = Globals.RectanglePattern;
                    _rubberBand.PatternScale = Globals.RectanglePatternScale;
                    _rubberBand.PatternAngle = Globals.RectanglePatternAngle;
                }
            }
            else
            {
                _rubberBand = new RubberBandBasic();
            }

            //base.Start();
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

            if (_rubberBand.State != 0)
            {
                _rubberBand.State = 0;
                _rubberBand.EndTracking();
            }

            base.Finish();
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            // TODO: Update internal command logic
            _rubberBand.EndTracking();

            if (_rubberBand.State == 1)
            {
                _rubberBand.State = 0;
            }
        }
    }

    public class EllipseBoxCommandProcessor : RectCommandProcessorBase
    {
        public EllipseBoxCommandProcessor()
        {
            _type = CommandType.ellipse;
            _rubberBand = new RubberBandEllipse(_color);

            _rubberBand.FillColor = ColorFromFillColorSpec(Globals.EllipseFill);
            if (string.IsNullOrEmpty(Globals.EllipsePattern) == false && Globals.EllipsePattern != "Solid")
            {
                _rubberBand.FillPattern = Globals.EllipsePattern;
                _rubberBand.PatternScale = Globals.EllipsePatternScale;
                _rubberBand.PatternAngle = Globals.EllipsePatternAngle;
            }
        }

        protected override Primitive instantiate()
        {
            double major = _trackWidth / 2;
            double minor = _trackHeight / 2;
            double cx = _trackLeft + major;
            double cy = _trackTop + minor;
            return new PEllipse(new Point(cx, cy), major, minor, 0, Globals.EllipseStartAngle, Globals.EllipseIncludedAngle);
        }

        // State 0
        //    No RB
        //    Start tracking
        //      Save anchor point
        //      State = 1
        // State = 1
        //    RB from anchor
        //    Start tracking
        //       Create rect from anchor to current
        //       State = 0

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            if (_rubberBand.State == 0)
            {
                _first = _start;

                _rubberBand.State = 1;
                _rubberBand.StartTracking(_start.X, _start.Y);
                Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal);
            }
            else
            {
                if (_trackWidth != 0 && _trackHeight != 0)
                {
                    Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

                    _lastObject = addPrimitive(instantiate());
                    Globals.Events.PrimitiveCreated(_lastObject);

                    _rubberBand.State = 0;
                    _rubberBand.EndTracking();

                    Start();
                }
            }
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);
            base.Finish();
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            _rubberBand.EndTracking();

            if (_rubberBand.State == 1)
            {
                _rubberBand.State = 0;
            }
        }
    }

    public class EllipseAxisCommandProcessor : CommandProcessor
    {
        public EllipseAxisCommandProcessor()
        {
            _type = CommandType.ellipse;
            _rubberBand = new RubberBandEllipseAxis(_color);

            _rubberBand.FillColor = ColorFromFillColorSpec(Globals.EllipseFill);

            if (string.IsNullOrEmpty(Globals.EllipsePattern) == false && Globals.EllipsePattern != "Solid")
            {
                _rubberBand.FillPattern = Globals.EllipsePattern;
                _rubberBand.PatternScale = Globals.EllipsePatternScale;
                _rubberBand.PatternAngle = Globals.EllipsePatternAngle;
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            RubberBandEllipseAxis rba = _rubberBand as RubberBandEllipseAxis;
            if (rba != null)
            {
                _rubberBand.StartTracking(_start.X, _start.Y);

                switch (rba.State)
                {
                    case 0:
                        _first = _start;
                        rba.Start = new Point(_start.X, _start.Y);
                        rba.State = 1;
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.CommandInternal, CommandProcessorActionID.EnterPoint, _start);
                        break;

                    case 1:
                        if (_start.X != _first.X || _start.Y != _first.Y)
                        {
                            double major = Construct.Distance(_first, _start) / 2;

                            if (major > 0)
                            {
                                Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

                                double minor = major / Globals.EllipseMajorMinorRatio;
                                double cx = (_first.X + _start.X) / 2;
                                double cy = (_first.Y + _start.Y) / 2;
                                double angle = Construct.Angle(_first, _start);

                                _lastObject = addPrimitive(new PEllipse(new Point(cx, cy), major, minor, angle, Globals.EllipseStartAngle, Globals.EllipseIncludedAngle));

                                Globals.Events.PrimitiveCreated(_lastObject);

                                _rubberBand.State = 0;
                                _rubberBand.EndTracking();

                                Start();
                            }
                        }
                        break;
                }
            }
        }

        protected override void RedoInternalCommand(object subject, object predicate, object predicate2)
        {
            if ((CommandProcessorActionID)subject == CommandProcessorActionID.EnterPoint)
            {
                _first = _start;
                RubberBandEllipseAxis rba = _rubberBand as RubberBandEllipseAxis;
                if (rba != null)
                {
                    rba.Start = new Point(_start.X, _start.Y);
                    rba.State = 1;
                }
            }
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            Finish();
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);

            _rubberBand.EndTracking();
            _rubberBand.Reset();

            base.Finish();
        }
    }

    public class EllipseCenterCommandProcessor : CommandProcessor
    {
        public EllipseCenterCommandProcessor()
        {
            _type = CommandType.ellipse;
            _rubberBand = new RubberBandBasic();
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            double major = Globals.EllipseMajorLength / 2; 
            double minor = major / Globals.EllipseMajorMinorRatio;

            _lastObject = addPrimitive(new PEllipse(_start, major, minor, Globals.EllipseAxisAngle, Globals.EllipseStartAngle, Globals.EllipseIncludedAngle));

            Globals.Events.PrimitiveCreated(_lastObject);
        }

        public override void Finish()
        {
            Globals.CommandDispatcher.RemoveActions(ActionID.CommandInternal);
            base.Finish();
        }

        protected override void UndoInternalCommand(object subject, object predicate, object predicate2)
        {
            _rubberBand.EndTracking();

            if (_rubberBand.State == 1)
            {
                _rubberBand.State = 0;
            }
        }
    }

    public class WindowCommandProcessor : RectCommandProcessorBase
    {
        public WindowCommandProcessor()
        {
            Color color = Globals.Theme.CursorColor;

            _type = CommandType.window;
            _rubberBand = new RubberBandRectangle(color);

            Color fillColor = color;
            fillColor.A = 25;
            _rubberBand.FillColor = fillColor;

            ShowConstructHandles = false;
        }

        public override InputMode InputMode
        {
            get
            {
                return InputMode.Drag;
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);

            if (_rubberBand.State == 0)
            {
                _rubberBand.State = 1;
                _rubberBand.StartTracking(_start.X, _start.Y);
            }
            else
            {
                _rubberBand.State = 0;
                _rubberBand.EndTracking();
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            double d = Construct.Distance(_start, new Point(x, y));
            double tolerance = Globals.DrawingCanvas.DisplayToPaper(Globals.hitTolerance / 2);
            if (d < tolerance)
            {
                Globals.View.Zoom(_start.X, _start.Y, 1.0, true);
            }
            else
            {
                Globals.View.DisplayWindow(_trackLeft, _trackTop, _trackLeft + _trackWidth, _trackTop + _trackHeight);
            }

            _rubberBand.State = 0;
            _rubberBand.EndTracking();

            Start();
        }
    }

    public class PanCommandProcessor : CommandProcessor
    {
        public PanCommandProcessor()
        {
            _type = CommandType.pan;
        }

        protected bool _tracking = false;
        protected bool _saveSnap;

        Point _cursorStart;

        protected override CursorType cursorType
        {
            get
            {
                return CursorType.Arrow;
            }
        }

        public override InputMode InputMode
        {
            get
            {
                return InputMode.Drag;
            }
        }

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            _cursorStart = Globals.DrawingCanvas.VectorListControl.PaperToDisplay(new Point(x, y));

             _tracking = true;
            _saveSnap = Globals.Input.GridSnap;
            Globals.Input.GridSnap = false;
            Globals.Input.SelectCursor(CursorType.Pan);
        }

        public override void TrackCursor(double x, double y)
        {
            if (_tracking)
            {
                Point s = Globals.DrawingCanvas.VectorListControl.PaperToDisplay(new Point(x, y));
                double dx = s.X - _cursorStart.X;
                double dy = s.Y - _cursorStart.Y;
                Globals.View.Pan(new Point(dx, dy));

                _cursorStart = s;
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_tracking)
            {
                Globals.Input.GridSnap = _saveSnap;
                Globals.Input.SelectCursor(cursorType);
                _tracking = false;
            }
        }
    }

    public class TextCommandProcessor : CommandProcessor
    {
        public TextCommandProcessor()
        {
            _type = CommandType.text;
        }

        protected PText _ptext = null;
        protected PTextEdit _editTextBox = null;

        public override void StartTracking(double x, double y, bool shift, bool control)
        {
            base.StartTracking(x, y, shift, control);
            if (Globals.TextSinglePoint == false)
            {
                if (_rubberBand.State == 0)
                {
                    Finish();

                    _first = _start;
                    _rubberBand.State = 1;
                    _rubberBand.StartTracking(_start.X, _start.Y);
                }
                else
                {
                    if (_rubberBand.State == 1)
                    {
                        // Two points selected
                        _rubberBand.State = 2;
                        _rubberBand.EndTracking();

                        double d = Construct.Distance(_first, _start);
                        double tolerance = Globals.View.DisplayToPaper(Globals.hitTolerance / 2);
 
                        if (d < tolerance)
                        {
                            _ptext = new PText(_first, Globals.TextAngle, Globals.TextStyleId, Globals.TextAlign, Globals.TextPosition, _string);
                        }
                        else
                        {
                            _ptext = new PText(_first, _start, Globals.TextStyleId, Globals.TextAlign, Globals.TextPosition, _string);
                        }
                        _ptext.AddToContainer(Globals.ActiveDrawing);
                        _lastObject = _ptext;

                        Globals.Events.PrimitiveCreated(_lastObject);
                    }
                    else
                    {
                        // Segment selected (DISABLED FOR NOW)
                        Finish();

                        _rubberBand.EndTracking();

                        _ptext = new PText(_first, _start, Globals.TextStyleId, Globals.TextAlign, Globals.TextPosition, _string);
                        _ptext.AddToContainer(Globals.ActiveDrawing);
                        _lastObject = _ptext;

                        Globals.Events.PrimitiveCreated(_lastObject);
                    }
                }
            }
            else
            {
                if (_rubberBand.State == 0)
                {
                    _first = _start;
                }

                Finish();

                double fontsize = Globals.TextStyleTable[Globals.TextStyleId].Size;
                _ptext = new PText(_first, _start, Globals.TextStyleId, Globals.TextAlign, Globals.TextPosition, _string);
                _ptext.AddToContainer(Globals.ActiveDrawing);
                _lastObject = _ptext;

                Globals.Events.PrimitiveCreated(_lastObject);

                _rubberBand.State = 2;
            }
        }

        public override void TrackCursor(double x, double y)
        {
            if (_editTextBox == null)
            {
                base.TrackCursor(x, y);
            }
            else if (_editTextBox.Finished)
            {
                _editTextBox = null;

                if (_ptext != null)
                {
                    if (_ptext.Text.Length == 0)
                    {
                        Globals.ActiveDrawing.RemovePrimitive(_ptext);
                    }
                    else 
                    {
                        Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, _ptext);
                    }
                    _ptext = null;
                }
            }
        }

        public override void EndTracking(double x, double y)
        {
            base.EndTracking(x, y);

            if (_rubberBand.State == 2)
            {
                if (_string == "" && _ptext is PText)
                {
                    if (_editTextBox != null)
                    {
                        _editTextBox.Finish();
                        _editTextBox = null;
                    }

                    _editTextBox = new PTextEdit(_ptext);
                    _editTextBox.Loaded += _editTextBox_Loaded;
                }

                _rubberBand.State = 0;
            }
        }

        void _editTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            _editTextBox.LostFocus += _editTextBox_LostFocus;
        }

        void _editTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_ptext != null)
            {
                if (_ptext.Text.Length == 0)
                {
                    Globals.ActiveDrawing.RemovePrimitive(_ptext);
                }
                else 
                {
                    Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, _ptext);
                }
                _ptext = null;
            }
        }

        public override void Start()
        {
            if (Globals.TextSinglePoint == false)
            {
                RubberBandTextBox rbtb = new RubberBandTextBox(_color);
                rbtb.Position = Globals.TextPosition;
                rbtb.Alignment = Globals.TextAlign;

                double fontsize = Globals.TextStyleTable[Globals.TextStyleId].Size;
                rbtb.Height = fontsize;
                rbtb.Offset = fontsize / 2;

                _rubberBand = rbtb;
            }

            base.Start();
        }

        public override void Finish()
        {
            _rubberBand.EndTracking();

            if (_editTextBox != null)
            {
                _editTextBox.Finish();
                _editTextBox = null;
            }

            if (_ptext != null)
            {
                // This should get hit if the user enters a point or switches commands without terminating text input.
                if (_ptext.Text.Length == 0)
                {
                    Globals.ActiveDrawing.RemovePrimitive(_ptext);
                }
                else 
                {
                     Globals.CommandDispatcher.AddUndoableAction(ActionID.DeletePrimitive, _ptext);
                }
                _ptext = null;
            }

            base.Finish();
        }

        public override void CanvasScrolled()
        {
            Finish();
            base.CanvasScrolled();
        }

        public override void CanvasScaleChanged()
        {
            Finish();
            base.CanvasScaleChanged();
        }
    }

    public enum CommandProcessorActionID
    {
        EnterFirstLinePoint,
        EnterPoint
    }
}
