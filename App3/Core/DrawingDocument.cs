using Cirros.Core.Primitives;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Cirros.Core;
using Microsoft.UI;

#if UWP
using Cirros.Commands;
using Cirros8;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
#else
using System.Windows;
using System.Windows.Media;
using CirrosCore;
#endif

namespace Cirros.Drawing
{
    public enum Unit
    {
        Undefined,
        Inches,
        Feet,
        Millimeters,
        Centimeters,
        Meters,
        Paper,
        Model
    };

    public class DrawingDocument : IDrawingContainer
    {
#region IDrawingDocument

        protected Size _paperSize;
        protected Unit _paperUnit = Unit.Inches;
        protected Unit _modelUnit = Unit.Inches;
        private bool _architecturalScale = false;

        protected bool _containsSymbols = false;

        protected Dictionary<string, Group> _groups = new Dictionary<string, Group>();

        protected double _modelToPaperScale = 1;
        //protected double _paperToCanvasScale = 100;            // display resolution in units per inch
        protected Point _modelOrigin = new Point(0, 0);     // Model origin in model units

        private uint _objectIdSeed = 1024;
        protected int _changeNumber = 1;
        protected bool _isModified = false;
        private List<Primitive> _primitiveList = new List<Primitive>();
        private Dictionary<uint, List<WallJoint>> _wallJointList = new Dictionary<uint, List<WallJoint>>();
        protected Theme _theme;
        protected PenLineCap _lineEndCap = PenLineCap.Round;

        protected int _minZIndex = 0;
        protected int _maxZIndex = 0;

        private int _anonGroupCount = 0;

        Dictionary<int, int> _layerCountMap = null;
        int _layerCountMapChangeNumber = 0;
        private bool _showItemBoxes;

        private TimeSpan _activeTime = new TimeSpan();
        private DateTime _startTime = DateTime.Now;

        public TimeSpan ActiveTime
        {
            get 
            {
                return _activeTime + (DateTime.Now - _startTime);
            } 
            set 
            {
                _startTime = DateTime.Now;
                _activeTime = value;
            }
        }

        public int ChangeNumber
        {
            get
            {
                return _changeNumber;
            }
            set
            {
                _changeNumber = value;
            }
        }

        public Unit PaperUnit
        {
            get
            {
                return _paperUnit;
            }
            set
            {
                _paperUnit = value;
            }
        }

        public Unit ModelUnit
        {
            get
            {
                return _modelUnit;
            }
            set
            {
                _modelUnit = value;
            }
        }

        public bool IsModified
        {
            get
            {
                return _isModified;
            }
            set
            {
                _isModified = value;
            }
        }

        public int MinZIndex
        {
            get
            {
                return --_minZIndex;
            }
            set
            {
                _minZIndex = value;
            }
        }

        public int MaxZIndex
        {
            get
            {
                return ++_maxZIndex;
            }
            set
            {
                _maxZIndex = value;
            }
        }

        public virtual Size PaperSize
        {
            get
            {
                return _paperSize;
            }
            set
            {
                _paperSize = value;

                Globals.Events.PaperSizeChanged();
            }
        }


        // Scale is the number of model units per paper unit (the inverse of the actual scale)

        public double Scale
        {
            get
            {
                double scale = 1 / _modelToPaperScale;

                if (_modelUnit == Unit.Feet)
                {
                    scale *= 12;
                }
                else if (_modelUnit == Unit.Millimeters)
                {
                    scale /= 25.4;
                }
                else if (_modelUnit == Unit.Centimeters)
                {
                    scale /= 2.54;
                }
                else if (_modelUnit == Unit.Meters)
                {
                    scale /= 0.0254;
                }
                //return Math.Round(scale);
                return scale;
            }
            set
            {
                double scale = 1 / value;

                if (_modelUnit == Unit.Feet)
                {
                    _modelToPaperScale = scale * 12;
                }
                else if (_modelUnit == Unit.Millimeters)
                {
                    _modelToPaperScale = scale / 25.4;
                }
                else if (_modelUnit == Unit.Centimeters)
                {
                    _modelToPaperScale = scale / 2.54;
                }
                else if (_modelUnit == Unit.Meters)
                {
                    _modelToPaperScale = scale / 0.0254;
                }
                else
                {
                    _modelToPaperScale = scale;
                    _architecturalScale = false;
                }
            }
        }

        public Point Origin
        {
            get
            {
                return _modelOrigin;
            }
            set
            {
                if (_modelOrigin != value)
                {
                    _modelOrigin = value;

                    Globals.Events.DrawingLayoutChanged();
                }
            }
        }

        public Theme Theme
        {
            get
            {
                return _theme;
            }
            set
            {
                _theme = value;

                Globals.Theme = _theme;
                Globals.Events.ThemeChanged();
            }
        }

        public bool IsArchitecturalScale
        {
            get
            {
                return _architecturalScale;
            }
            set
            {
                _architecturalScale = value;
            }
        }

        public void InitializeDrawingDocument()
        {
            Theme = Globals.Theme;

            _primitiveList = new List<Primitive>();
        }

        public void NewEmptyDrawing()
        {
#if UWP
            if (ApplicationData.Current.LocalSettings.Containers.Keys.Contains("drawing"))
            {
                ApplicationDataContainer drawingSettings = ApplicationData.Current.LocalSettings.Containers["drawing"];

                try
                {
                    object o = drawingSettings.Values["scale"];
                    double scale = o is int ? (double)(int)o : (double)o;
                    double width = (double)drawingSettings.Values["paper_width"];
                    double height = (double)drawingSettings.Values["paper_height"];
                    bool isArchitecturalScale = (bool)drawingSettings.Values["archscale"];

                    Unit modelUnit = Utilities.UnitFromString((string)drawingSettings.Values["model_unit"]);
                    Unit paperUnit = Utilities.UnitFromString((string)drawingSettings.Values["paper_unit"]);

                    string themeName = (string)drawingSettings.Values["theme"];
                    Theme theme = Globals.Themes[themeName == null ? "light" : themeName];
                    NewEmptyDrawing(width, height, scale, paperUnit, modelUnit, isArchitecturalScale, theme);

                    Analytics.ReportEvent("new_drawing", new Dictionary<string, string> { { "paper", paperUnit.ToString() }, { "model", modelUnit.ToString() } });
                }
                catch (Exception ex)
                {
                    // failed to get valid settings

                    Analytics.ReportError("NewEmptyDrawing: Failed to get valid settings", ex, 2, 202);
                    NewEmptyDrawing(17, 11, 1, Unit.Inches, Unit.Inches, false, Globals.Theme);
                }
            }
            else
            {
                NewEmptyDrawing(17, 11, 1, Unit.Inches, Unit.Inches, false, Globals.Theme);
            }
#else
            throw new Exception("Unimplemented");
#endif
        }

        public void NewEmptyDrawing(double width, double height, double scale, Unit paperUnit, Unit modelUnit, bool architecturalScale, Theme theme)
        {
            Clear();

            Globals.ColorSpec = (uint)ColorCode.ByLayer;
            Globals.LineWeightId = -1;
            Globals.LineTypeId = -1;
            Globals.LayerId = 0;
            Globals.TextStyleId = 0;

            Theme = theme;

            SetDefaultAttributes(paperUnit == Unit.Millimeters);

            PaperSize = new Size(width, height);

            SetDrawingScaleAndUnits(scale, paperUnit, modelUnit, architecturalScale);

            Globals.Events.DrawingLoaded(null);
        }

        public void SetDrawingScaleAndUnits(double scale, Unit paperUnit, Unit modelUnit, bool architecturalScale)
        {
            Point origin = Globals.ActiveDrawing.ModelToPaperRaw(Globals.ActiveDrawing.Origin);

            PaperUnit = paperUnit;
            ModelUnit = modelUnit;

            if (IsArchitecturalScale != architecturalScale)
            {
                IsArchitecturalScale = architecturalScale;
            }

            double majorGridSpacing = 1;
            int divisions = 10;

            if (paperUnit == Unit.Inches)
            {
                if (modelUnit == Unit.Feet)
                {
                    if (IsArchitecturalScale)
                    {
                        if (scale == 1)
                        {
                            majorGridSpacing = 1.0 / 12.0;
                            divisions = 8;
                        }
                        else if (scale < 5) //else if (scale > 0.2)
                        {
                            majorGridSpacing = 1;
                            divisions = 24;
                        }
                        else if (scale < 12) //else if (scale > 0.08)
                        {
                            majorGridSpacing = 1;
                            divisions = 12;
                        }
                        else if (scale < 25) //else if (scale > 0.04)
                        {
                            majorGridSpacing = 1;
                            divisions = 6;
                        }
                        else if (scale < 48) //else if (scale > 0.021)
                        {
                            majorGridSpacing = 1;
                            divisions = 6;
                        }
                        else if (scale < 96)
                        {
                            majorGridSpacing = 5;
                            divisions = 10;
                        }
                        else
                        {
                            majorGridSpacing = 10;
                            divisions = 10;
                        }

                        if (Globals.DimensionRoundArchitectDefault == 0)
                        {
                            Globals.DimensionRound = scale < 48 ? 16 : 8;
                        }
                        else
                        {
                            Globals.DimensionRound = Globals.DimensionRoundArchitectDefault;
                        }
                    }
                    else
                    {
                        // modelUnit == Unit.Feet, Engineering

                        if (scale < 2)
                        {
                            majorGridSpacing = .1;
                        }
                        else if (scale == 12)
                        {
                            majorGridSpacing = 1;
                        }
                        else if (scale <= 5)
                        {
                            majorGridSpacing = 5;
                        }
                        else if (scale <= 120)
                        {
                            majorGridSpacing = 10;
                        }
                        else if (scale <= 480)
                        {
                            majorGridSpacing = 50;
                        }
                        else if (scale <= 1200)
                        {
                            majorGridSpacing = 100;
                        }
                        else if (scale <= 4800)
                        {
                            majorGridSpacing = 500;
                        }
                        else if (scale <= 12000)
                        {
                            majorGridSpacing = 1000;
                        }
                        else
                        {
                            majorGridSpacing = 2000;
                        }

                        if (Globals.DimensionRoundEngineerDefault == 0)
                        {
                            Globals.DimensionRound = 3;
                        }
                        else
                        {
                            Globals.DimensionRound = Globals.DimensionRoundEngineerDefault;
                        }
                    }
                }
                else
                {
                    // modelUnit == Unit.Inches

                    if (scale < 2)
                    {
                        majorGridSpacing = 1;
                    }
                    else if (scale == 2)
                    {
                        majorGridSpacing = 5;
                    }
                    else if (scale <= 10)
                    {
                        majorGridSpacing = 10;
                    }
                    else if (scale <= 20)
                    {
                        majorGridSpacing = 50;
                    }
                    else if (scale <= 100)
                    {
                        majorGridSpacing = 100;
                    }
                    else if (scale <= 200)
                    {
                        majorGridSpacing = 500;
                    }
                    else if (scale <= 1000)
                    {
                        majorGridSpacing = 1000;
                    }
                    else
                    {
                        majorGridSpacing = 2000;
                    }

                    divisions = 10;

                    if (Globals.DimensionRoundEngineerDefault == 0)
                    {
                        Globals.DimensionRound = 3;
                    }
                    else
                    {
                        Globals.DimensionRound = Globals.DimensionRoundEngineerDefault;
                    }
                }
            }
            else if (paperUnit == Unit.Millimeters)
            {
                if (scale < 2)
                {
                    majorGridSpacing = 20;
                }
                else if (scale == 2)
                {
                    majorGridSpacing = 100;
                }
                else if (scale <= 5)
                {
                    majorGridSpacing = 200;
                }
                else if (scale <= 10)
                {
                    majorGridSpacing = 500;
                }
                else if (scale <= 20)
                {
                    majorGridSpacing = 1000;
                }
                else if (scale <= 50)
                {
                    majorGridSpacing = 2000;
                }
                else if (scale <= 100)
                {
                    majorGridSpacing = 5000;
                }
                else if (scale <= 200)
                {
                    majorGridSpacing = 10000;
                }
                else if (scale <= 500)
                {
                    majorGridSpacing = 20000;
                }
                else
                {
                    majorGridSpacing = 50000;
                }

                if (Globals.DimensionRoundMetricDefault > 0)
                {
                    Globals.DimensionRound = Globals.DimensionRoundMetricDefault;
                }
                else if (modelUnit == Unit.Millimeters)
                {
                    Globals.DimensionRound = 1;
                }
                else if (modelUnit == Unit.Centimeters)
                {
                    Globals.DimensionRound = 2;
                    majorGridSpacing /= 10;
                }
                else if (modelUnit == Unit.Meters)
                {
                    Globals.DimensionRound = 4;
                    majorGridSpacing /= 1000;
                }

                divisions = 10;
            }
            else
            {
                throw new Exception("Invalid paper unit");
            }

            this.Scale = scale;

            Globals.GridSpacing = majorGridSpacing;
            Globals.GridDivisions = divisions;
            Globals.Events.GridChanged();

            Globals.FilletRadius = majorGridSpacing / divisions;
            Globals.EditOffsetLineDistance = majorGridSpacing / divisions;

            Globals.ActiveDrawing.Origin = Globals.ActiveDrawing.PaperToModelRaw(origin);

            Globals.Events.OptionsChanged();
        }

        public void SetDefaultAttributes(bool metric)
        {
            Globals.LayerTable.Clear();
            Globals.LineTypeTable.Clear();
            Globals.TextStyleTable.Clear();
            Globals.ArrowStyleTable.Clear();

            AddLayer("Unassigned", (uint)ColorCode.ThemeForeground, 0, 10);
            AddLayer("Layer 001", (uint)ColorCode.ThemeForeground, 0, 7);
            AddLayer("Layer 002", (uint)ColorCode.ThemeForeground, 0, 20);
            AddLayer("Layer 003", (uint)ColorCode.ThemeForeground, 0, 35);
            AddLayer("Layer 004", (uint)ColorCode.ThemeForeground, 0, 56);
            AddLayer("Layer 005", (uint)ColorCode.ThemeForeground, 1, 10);
            AddLayer("Layer 006", (uint)ColorCode.ThemeForeground, 2, 10);
            AddLayer("Layer 007", (uint)ColorCode.ThemeForeground, 3, 10);
            AddLayer("Layer 008", Utilities.ColorSpecFromColor(Colors.Red), 0, 10);
            AddLayer("Layer 009", Utilities.ColorSpecFromColor(Colors.Green), 0, 10);
            AddLayer("Layer 010", Utilities.ColorSpecFromColor(Colors.Blue), 0, 10);
            AddLayer("Layer 011", Utilities.ColorSpecFromColor(Colors.Cyan), 0, 10);
            AddLayer("Layer 012", Utilities.ColorSpecFromColor(Colors.Magenta), 0, 10);
            AddLayer("Layer 013", Utilities.ColorSpecFromColor(Colors.Yellow), 0, 10);

            if (metric)
            {
                AddLineType("Solid", null);
                AddLineType("Dash", new DoubleCollection() { 0.0984251946, 0.0492125973 });
                AddLineType("Long dash", new DoubleCollection() { 0.157480314, 0.0492125973 });
                AddLineType("Centerline", new DoubleCollection() { 0.984252, 0.0492125973, 0.0984251946, 0.0492125973 });
                AddLineType("Phantom", new DoubleCollection() { 0.984252, 0.0492125973, 0.0984251946, 0.0492125973, 0.0984251946, 0.0492125973 });

                AddTextStyle("Normal", "Segoe UI", 3 / 25.4, .5, 1.8, 1);
                AddTextStyle("Small", "Segoe UI", 2 / 25.4, .5, 1.8, 1);
                AddTextStyle("Large", "Segoe UI", 5 / 25.4, .5, 1.8, 1);
                AddTextStyle("Title", "Segoe UI", 6 / 25.4, .5, 1.8, 1);

                AddArrowStyle("Filled", ArrowType.Filled, 3 / 25.4, .25);
                AddArrowStyle("Small filled", ArrowType.Filled, 2 / 25.4, .25);
                AddArrowStyle("Open", ArrowType.Open, 3 / 25.4, 0.25);
                AddArrowStyle("Small open", ArrowType.Open, 2 / 25.4, 0.25);
                AddArrowStyle("Outline", ArrowType.Outline, 3 / 25.4, 0.25);
                AddArrowStyle("Small outline", ArrowType.Outline, 2 / 25.4, 0.25);
                AddArrowStyle("Wide", ArrowType.Wide, 3 / 25.4, 0.5);
                AddArrowStyle("Small wide", ArrowType.Wide, 2 / 25.4, 0.5);
                AddArrowStyle("Circle", ArrowType.Ellipse, 2 / 25.4, 1);
                AddArrowStyle("Ellipse", ArrowType.Ellipse, 3 / 25.4, .5);
                AddArrowStyle("Dot", ArrowType.Dot, 3 / 25.4, 1);
                AddArrowStyle("Small dot", ArrowType.Dot, 2 / 25.4, 1);
            }
            else
            {
                AddLineType("Solid", null);
                AddLineType("Dash", new DoubleCollection() { 0.1, 0.05 });
                AddLineType("Long dash", new DoubleCollection() { 0.15, 0.05 });
                AddLineType("Centerline", new DoubleCollection() { 1.0, 0.05, 0.1, 0.05 });
                AddLineType("Phantom", new DoubleCollection() { 1.0, 0.05, 0.1, 0.05, 0.1, 0.05 });

                AddTextStyle("Normal", "Segoe UI", 0.1, .5, 1.8, 1);
                AddTextStyle("Small", "Segoe UI", 0.075, .5, 1.8, 1);
                AddTextStyle("Large", "Segoe UI", 0.2, .5, 1.8, 1);
                AddTextStyle("Title", "Segoe UI", 0.25, .5, 1.8, 1);

                AddArrowStyle("Filled", ArrowType.Filled, 0.125, .25);
                AddArrowStyle("Small filled", ArrowType.Filled, 0.075, .25);
                AddArrowStyle("Open", ArrowType.Open, 0.125, 0.25);
                AddArrowStyle("Small open", ArrowType.Open, 0.075, 0.25);
                AddArrowStyle("Outline", ArrowType.Outline, 0.125, 0.25);
                AddArrowStyle("Small outline", ArrowType.Outline, 0.075, 0.25);
                AddArrowStyle("Wide", ArrowType.Wide, 0.125, 0.5);
                AddArrowStyle("Small wide", ArrowType.Wide, 0.075, 0.5);
                AddArrowStyle("Circle", ArrowType.Ellipse, 0.075, 1);
                AddArrowStyle("Ellipse", ArrowType.Ellipse, 0.125, .5);
                AddArrowStyle("Dot", ArrowType.Dot, 0.125, 1);
                AddArrowStyle("Small dot", ArrowType.Dot, 0.075, 1);
            }

            AttributeListsChanged();
        }

        public void Clear()
        {
            if (Globals.ActiveDrawing.PaperSize.Width == 0 || Globals.ActiveDrawing.PaperSize.Height == 0 || Globals.LayerTable.Count == 0)
            {
                // If the work canvas hasn't been initialized, there's no need to clear it
            }
            else
            {
                FileHandling.DrawingFileIsAvailable = false;

                _primitiveList.Clear();
                _groups.Clear();
                _containsSymbols = false;

                _isModified = false;
                _changeNumber++;

                Globals.Events.DrawingCleared();
            }
        }

        public void AddPrimitive(Primitive p)
        {
            if (p != null)
            {
                if (p.Id >= _objectIdSeed)
                {
                    _objectIdSeed = p.Id + 1;
                }
                _primitiveList.Add(p);
            }
        }

        public void RemovePrimitive(Primitive p)
        {
            if (_primitiveList.Contains(p))
            {
                _primitiveList.Remove(p);

#if UWP
                Globals.DrawingCanvas.VectorListControl.RemoveSegment(p.Id);
                Globals.DrawingCanvas.VectorListControl.Redraw();
#else
#endif

                p.Dispose();
            }
        }

        public void DeletePrimitive(Primitive p)
        {
            RemovePrimitive(p);

#if UWP
            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.ResetConstructHandles();
            }
#else
#endif
        }

        public void RestoreDeletedPrimitive(Primitive p)
        {
            if (p != null)
            {
                p.AddToContainer(this);
            }
        }

        public List<Primitive> PrimitiveList
        {
            get
            {
                return _primitiveList;
            }
        }

        public Dictionary<string, Group> Groups
        {
            get
            {
                return _groups;
            }
        }

        public Primitive Pick(double x, double y, bool returnTopLevel)
        {
            Primitive pick = null;

            Point paper = new Point(x, y);

#if UWP
            uint sid = Globals.DrawingCanvas.VectorListControl.PickSegment(paper);
            if (sid > 0)
            {
                pick = FindObjectById(sid);
            }
#else
#endif

            return pick;
        }

        public uint NewObjectId()
        {
            return _objectIdSeed++;
        }

        public Primitive FindObjectById(uint id)
        {
            Primitive found = null;

            foreach (Primitive p in _primitiveList)
            {
                if (p.Id == id)
                {
                    found = p;
                    break;
                }
            }

            return found;
        }

        public void DrawItemBoxes()
        {
            _showItemBoxes = !_showItemBoxes;
#if UWP
            Globals.DrawingCanvas.VectorListControl.ShowItemBoxes(_showItemBoxes);
#else
#endif
        }

        public string AddGroup(Group group)
        {
            group.Name = UniqueGroupName(group.Name);

            _groups.Add(group.Name, group);

            return group.Name;
        }

        public Group GetGroup(string name)
        {
            string key = name;

            if (string.IsNullOrEmpty(name) == false && _groups.ContainsKey(key))
            {
                return _groups[key];
            }
            return null;
        }

        public Group GetGroupById(Guid groupId)
        {
            Group group = null;

            if (groupId != Guid.Empty)
            {
                foreach (Group g in _groups.Values)
                {
                    if (g.Id == groupId)
                    {
                        group = g;
                        break;
                    }
                }
            }

            return group;
        }

        public virtual async void SaveGroup(Group group)
        {
            bool success = await FileHandling.SaveSymbolAsAsync(group);
        }

        public PInstance CreateGroupFromSinglePrimitive(Primitive p)
        {
            string name = Globals.ActiveDrawing.UniqueGroupName(null);

            List<Primitive> list = new List<Primitive>();
            list.Add(p);

            Group group = new Group(name);
            group.PaperUnit = Globals.ActiveDrawing.PaperUnit;
            group.ModelUnit = Globals.ActiveDrawing.ModelUnit;
            group.ModelScale = Globals.ActiveDrawing.Scale;
            group.MovePrimitivesFromDrawing(p.Origin.X, p.Origin.Y, list);
            group.InsertLocation = GroupInsertLocation.None;
            //group.IncludeInLibrary = false;
            group.Entry = new Point(0, 0);
            group.Exit = new Point(0, 0);

            Globals.ActiveDrawing.AddGroup(group);

            PInstance newInstance = new PInstance(p.Origin, name);
            newInstance.LayerId = 0;      // Groups should be created on layer 0 (unassigned) regardless of the active layer setting
            newInstance.AddToContainer(Globals.ActiveDrawing);

            return newInstance;
        }

        protected int countGroupInstancesInGroup(Group g, string name)
        {
            int count = 0;

            foreach (Primitive p in g.Items)
            {
                if (p is PInstance instance)
                {
                    if (instance.GroupName == name)
                    {
                        count++;
                    }
                    else
                    {
                        Group gm = GetGroup(instance.GroupName);
                        if (gm != null)
                        {
                            count += countGroupInstancesInGroup(gm, name);
                        }
                    }
                }
            }

            return count;
        }

        public Dictionary<string, int> GroupInstanceMap()
        {
            Dictionary<string, int> map = new Dictionary<string, int>();
#if false
            foreach (Primitive p in _primitiveList)
            {
                if (p is PInstance)
                {
                    string name = ((PInstance)p).GroupName;
                    if (map.ContainsKey(name))
                    {
                        map[name]++;
                    }
                    else
                    {
                        map.Add(name, 0);
                        AddChildGroupsToGroupInstanceMap(name, map);
                    }
                }
                foreach (Group g in _groups.Values)
                {
                    if (g.IncludeInLibrary)
                    {
                        if (map.ContainsKey(g.Name))
                        {
                            map[g.Name]++;
                        }
                        else
                        {
                            map.Add(g.Name, 0);
                            AddChildGroupsToGroupInstanceMap(g.Name, map);
                        }
                    }
                }
            }
#else
            foreach (Group g in _groups.Values)
            {
                if (map.ContainsKey(g.Name))
                {
                    map[g.Name]++;
                }
                else
                {
                    map.Add(g.Name, 0);
                }

                foreach (Primitive member in g.Items)
                {
                    if (member is PInstance)
                    {
                        string name = ((PInstance)member).GroupName;
                        if (map.ContainsKey(name))
                        {
                            map[name]++;
                        }
                        else
                        {
                            map.Add(name, 0);
                        }
                    }
                }
            }

            foreach (Primitive p in _primitiveList)
            {
                if (p is PInstance)
                {
                    string name = ((PInstance)p).GroupName;
                    if (map.ContainsKey(name))
                    {
                        map[name]++;
                    }
                    else
                    {
                        map.Add(name, 0);
                    }
                }
            }
#endif
            return map;
        }

#if false
        private void AddChildGroupsToGroupInstanceMap(string name, Dictionary<string, int> map)
        {
            Group g = GetGroup(name);

            foreach (Primitive member in g.Items)
            {
                if (member is PInstance)
                {
                    string iname = ((PInstance)member).GroupName;
                    if (map.ContainsKey(iname))
                    {
                        map[iname]++;
                    }
                    else
                    {
                        map.Add(iname, 0);
                        AddChildGroupsToGroupInstanceMap(name, map);
                    }
                }
            }
        }
#endif

        public List<PInstance> GetGroupInstances(Group g)
        {
            List<PInstance> list = new List<PInstance>();

            foreach (Primitive p in _primitiveList)
            {
                if (p is PInstance pi && pi.GroupName == g.Name)
                {
                    list.Add(pi);
                }
            }

            return list;
        }

        public int CountGroupInstances(string name, bool topLevelOnly)
        {
            int count = 0;

            foreach (Primitive p in _primitiveList)
            {
                if (p is PInstance instance && instance.GroupName == name)
                {
                    count++;
                }
            }

            if (topLevelOnly == false)
            {
                foreach (Group g in _groups.Values)
                {
#if true
                    if (g.Name != name && g.ContainsGroup(name))
                    {
                        count++;
                    }
#else
                    if (g.Name != name)
                    {
                        int contained = countGroupInstancesInGroup(g, name);

                        if (contained > 0)
                        {
                            foreach (Primitive p in _primitiveList)
                            {
                                if (p is PInstance && ((PInstance)p).GroupName == g.Name)
                                {
                                    count += contained;
                                }
                            }
                        }
                    }
#endif
                }
            }

            return count;
        }

        public List<PInstance> GetGroupInstances(string name)
        {
            List<PInstance> instances = new List<PInstance>();

            foreach (Primitive p in _primitiveList)
            {
                if (p is PInstance pi && pi.GroupName == name)
                {
                    instances.Add(pi);
                }
            }

            return instances;
        }

        public void RemoveGroup(string name)
        {
            if (Globals.ActiveDrawing.CountGroupInstances(name, false) == 0)
            {
                if (_groups.ContainsKey(name))
                {
                    _groups.Remove(name);

                    _isModified = true;
                    _changeNumber++;
                }
            }
        }

        public string UniqueGroupName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                do
                {
                    name = string.Format(":{0}", _anonGroupCount++);
                }
                while (_groups.ContainsKey(name));
            }
            else if (GroupExists(name))
            {
                string baseName = GroupNameBase(name);

                for (int i = 1; i <= _groups.Count; i++)
                {
                    name = string.Format("{0}:{1}", baseName, i);

                    if (GroupExists(name) == false)
                    {
                        break;
                    }
                }
            }

            return name;
        }

        public string UniqueGroupName(Dictionary<string, Group> groups, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                do
                {
                    name = string.Format(":{0}", _anonGroupCount++);
                }
                while (_groups.ContainsKey(name));
            }
            else if (GroupExists(name))
            {
                string baseName = GroupNameBase(name);

                for (int i = 1; i <= _groups.Count; i++)
                {
                    name = string.Format("{0}:{1}", baseName, i);

                    if (GroupExists(name) == false && groups.ContainsKey(name) == false)
                    {
                        break;
                    }
                }
            }

            return name;
        }

        public static string GroupNameBase(string name)
        {
            name = name.Replace(".dbsx", "");

            int i = name.IndexOf(":");
            if (i > 0)
            {
                name = name.Substring(0, i);
            }
            return name;
        }

        public void RenameGroup(string oldname, string newname)
        {
            newname = UniqueGroupName(newname);

            Group g = GetGroup(oldname);
            if (g != null && GroupNameBase(oldname) != GroupNameBase(newname))
            {
                RenameGroup(g, newname);
            }
        }

        public void RenameGroup(Group group, string name)
        {
            if (!GroupExists(name))
            {
                string oldname = group.Name;
                group.Name = name;

                _groups.Remove(oldname);
                _groups[name] = group;

                foreach (Group g in _groups.Values)
                {
                    g.ReassociateMemberInstances(oldname, name);
                }

                foreach (Primitive p in _primitiveList)
                {
                    if (p is PInstance)
                    {
                        if (((PInstance)p).GroupName == oldname)
                        {
                            ((PInstance)p).Reassociate(name);
                        }
                    }
                }
            }
        }

        public void ChangeGroupFlags(string groupName, uint flags)
        {
            Group g = GetGroup(groupName);
            if (g != null)
            {
                g.Flags = flags;
            }
        }

        private bool GroupExists(string name)
        {
            return string.IsNullOrEmpty(name) ? false : _groups.ContainsKey(name);
        }

        public void HighlightGroupInstances(string name, bool hilight)
        {
            foreach (Primitive p in _primitiveList)
            {
                if (p is PInstance && ((PInstance)p).GroupName == name)
                {
                    p.Highlight(hilight);
                }
            }
        }

        public void UpdateGroupInstances(string name)
        {
#if true // recursive
            foreach (Primitive p in _primitiveList)
            {
                if (p is PInstance)
                {
                    Group g = Globals.ActiveDrawing.GetGroup(((PInstance)p).GroupName);
                    if (g.ContainsGroup(name))
                    {
                        p.Draw();
                    }
                }
            }
#else
            foreach (Primitive p in _primitiveList)
            {
                if (p is PInstance)
                {
                    if (p is PInstance && ((PInstance)p).GroupName == name)
                    {
                        p.Draw();
                    }
                }
            }
#endif
        }

        public bool ContainsSymbols
        {
            get
            {
                if (_containsSymbols == false)
                {
                    foreach (Group g in _groups.Values)
                    {
                        if (g.IncludeInLibrary)
                        {
                            _containsSymbols = true;
                            break;
                        }
                    }
                }
                return _containsSymbols;
            }
        }

        public void AddLayer(Layer layer, bool assignId = false)
        {
            if (assignId)
            {
                layer.Id = Globals.LayerTable.Count;

                while (Globals.LayerTable.ContainsKey(layer.Id))
                {
                    layer.Id++;
                }
            }

            if (Globals.LayerTable.ContainsKey(layer.Id) == false)
            {
                Globals.LayerTable.Add(layer.Id, layer);
            }
        }

        public int AddLayer(string name, uint colorspec, int lineTypeId, int lineWeightId)
        {
            int id = Globals.LayerTable.Count;

            while (Globals.LayerTable.ContainsKey(id))
            {
                id++;
            }

            Globals.LayerTable.Add(id, new Layer(id, name, colorspec, lineTypeId, lineWeightId));

            return id;
        }

        public int AddLineType(string name, DoubleCollection sda)
        {
            int id = Globals.LineTypeTable.Count;

            while (Globals.LineTypeTable.ContainsKey(id))
            {
                id++;
            }

            Globals.LineTypeTable.Add(id, new LineType(id, name, sda));

            return id;
        }

        public int AddTextStyle(string name, string font, double size, double offset, double lineSpacing, double characterSpacing)
        {
            int id = Globals.TextStyleTable.Count;

            while (Globals.TextStyleTable.ContainsKey(id))
            {
                id++;
            }

            Globals.TextStyleTable.Add(id, new TextStyle(id, name, font, size, offset, lineSpacing, characterSpacing));

            return id;
        }

        public int AddArrowStyle(string name, ArrowType type, double size, double aspect)
        {
            int id = Globals.ArrowStyleTable.Count;

            while (Globals.ArrowStyleTable.ContainsKey(id))
            {
                id++;
            }

            Globals.ArrowStyleTable.Add(id, new ArrowStyle(id, name, type, size, aspect));

            return id;
        }

        public Layer NewLayer()
        {
            int id = AddLayer("", (uint)ColorCode.ThemeForeground, 0, 10);
            Globals.LayerTable[id].Name = string.Format("Layer {0:000}", id);

            Globals.Events.AttributesListChanged();

            return Globals.LayerTable[id];
        }

        public void DeleteLayer(int layerId)
        {
            if (layerId == 0)
            {
                // Can't delete layer 0 (Unassigned)
            }
            else if (ObjectsInLayer(layerId) != 0)
            {
                // Can't delete non-empty layer
            }
            else if (layerId == Globals.LayerId || layerId == Globals.TextLayerId || layerId == Globals.DimensionLayerId)
            {
                // Can't delete an active layer
            }
            else
            {
                Globals.LayerTable.Remove(layerId);
            }
        }

        protected Dictionary<int, int> LayerCountMap
        {
            get
            {
                if (_layerCountMap == null || _layerCountMapChangeNumber != _changeNumber)
                {
                    _layerCountMap = new Dictionary<int, int>();

                    foreach (Primitive p in _primitiveList)
                    {
                        if (_layerCountMap.ContainsKey(p.LayerId))
                        {
                            _layerCountMap[p.LayerId]++;
                        }
                        else
                        {
                            _layerCountMap.Add(p.LayerId, 1);
                        }
                    }

                    _layerCountMapChangeNumber = _changeNumber;
                }
                return _layerCountMap;
            }
        }

        public int ObjectsInLayer(int layerId)
        {
            int count = 0;

            if (LayerCountMap.ContainsKey(layerId))
            {
                count = LayerCountMap[layerId];
            }

            return count;
        }

        public LineType NewLineType()
        {
            int id = AddLineType("", null);
            Globals.LineTypeTable[id].Name = string.Format("Line Type {0:000}", id);

            Globals.Events.AttributesListChanged();

            return Globals.LineTypeTable[id];
        }

        public TextStyle NewTextStyle()
        {
            int id = AddTextStyle("", "Segoe UI", .1, .5, 1.8, 1);
            Globals.TextStyleTable[id].Name = string.Format("Text Style {0:000}", id);

            Globals.Events.AttributesListChanged();

            return Globals.TextStyleTable[id];
        }

        public ArrowStyle NewArrowStyle()
        {
            int id = AddArrowStyle("", ArrowType.Filled, 0.125, .25);
            Globals.ArrowStyleTable[id].Name = string.Format("Arrow Style {0:000}", id);

            Globals.Events.AttributesListChanged();

            return Globals.ArrowStyleTable[id];
        }

        public void AddWallJoint(WallJoint joint)
        {
            List<WallJoint> joints;

            if (_wallJointList.ContainsKey(joint.ToId))
            {
                joints = _wallJointList[joint.ToId];
                foreach (WallJoint j in joints)
                {
                    if (j.FromId == joint.FromId && j.FromVertex == joint.FromVertex)
                    {
                        joints.Remove(j);
                        break;
                    }
                }
            }
            else
            {
                joints = new List<WallJoint>();
                _wallJointList.Add(joint.ToId, joints);
            }

            joints.Add(joint);
        }

        public Dictionary<uint, List<WallJoint>> GetWallsWithJoints()
        {
            return _wallJointList;
        }

        public List<WallJoint> GetWallJoints(uint targetDb)
        {
            List<WallJoint> joints = null;

            if (_wallJointList.ContainsKey(targetDb))
            {
                joints = _wallJointList[targetDb];
            }

            return joints;
        }

        public void AttributeListsChanged()
        {
            Globals.Events.AttributesListChanged();
            Globals.Events.OptionsChanged();
        }

        public PenLineCap LineEndCap
        {
            get
            {
                return _lineEndCap;
            }
            set
            {
                _lineEndCap = value;
            }
        }

#if UWP
        public void MatchAttributes(Primitive p)
        {
            int layerId = Globals.TextLayerId;
            uint fill = Globals.ArcFill;
            TextAlignment textAlign = Globals.TextAlign;
            TextPosition textPosition = Globals.TextPosition;
            int textStyleId = Globals.TextStyleId;
            int arrowStyleId = Globals.DimArrowStyleId;
            PDimension.DimType dimType = Globals.DimensionType;
            ArrowLocation arrowLocation = Globals.ArrowLocation;
            DbEndStyle dbEndStyle = Globals.DoublelineEndStyle;
            double dbWidth = Globals.DoubleLineWidth;

            double radius = 1;
            double width = 1;
            double height = 1;
            double startAngle = 0;
            double includedAngle = 360;
            double angle = 0;
            double ratio = 1;

            switch (Globals.CommandProcessor.Type)
            {
                case CommandType.text:
                    layerId = Globals.TextLayerId;
                    textStyleId = Globals.TextStyleId;
                    break;

                case CommandType.dimension:
                    layerId = Globals.DimensionLayerId;
                    textStyleId = Globals.DimTextStyleId;
                    break;

                case CommandType.arrow:
                    layerId = Globals.DimensionLayerId;
                    arrowStyleId = Globals.ArrowStyleId;
                    break;

                case CommandType.doubleline:
                    fill = Globals.DoublelineFill;
                    width = Globals.DoubleLineWidth;
                    break;

                case CommandType.circle:
                case CommandType.circle3:
                    radius = Globals.CircleRadius;
                    fill = Globals.CircleFill;
                    //fill = Globals.ArcFill;
                    break;

                case CommandType.arc:
                case CommandType.arc3:
                    radius = Globals.ArcRadius;
                    fill = Globals.ArcFill;
                    startAngle = Globals.ArcStartAngle;
                    includedAngle = Globals.ArcIncludedAngle;
                    break;

                case CommandType.ellipse:
                    fill = Globals.EllipseFill;
                    startAngle = Globals.EllipseStartAngle;
                    includedAngle = Globals.EllipseIncludedAngle;
                    angle = Globals.EllipseAxisAngle;
                    width = Globals.EllipseMajorLength;
                    ratio = Globals.EllipseMajorMinorRatio;
                    break;

                case CommandType.rectangle:
                    fill = Globals.RectangleFill;
                    width = Globals.RectangleWidth;
                    height = Globals.RectangleHeight;
                    break;

                case CommandType.polygon:
                    fill = Globals.PolygonFill;
                    break;

                case CommandType.bspline:
                case CommandType.insert:
                case CommandType.insertsymbol:
                case CommandType.line:
                case CommandType.polyline:
                case CommandType.freehand:
                    break;

                default:
                    return;
            }

            if (p is PText)
            {
                PText pt = p as PText;
                textAlign = pt.Alignment;
                textPosition = pt.Position;
                layerId = pt.LayerId;
                textStyleId = pt.TextStyleId;
            }
            else if (p is PDimension)
            {
                PDimension pd = p as PDimension;
                arrowStyleId = pd.ArrowStyleId;
                layerId = pd.LayerId;
                dimType = pd.DimensionType;
                textStyleId = pd.TextStyleId;
            }
            else if (p is PArrow)
            {
                PArrow pa = p as PArrow;
                arrowStyleId = pa.ArrowStyleId;
                arrowLocation = pa.ArrowLocation;
                layerId = pa.LayerId;
            }
            else if (p is Primitive)
            {
                layerId = p.LayerId;
                fill = p.Fill;

                if (p is PDoubleline)
                {
                    PDoubleline pd = p as PDoubleline;
                    dbEndStyle = pd.EndStyle;
                    dbWidth = pd.Width;
                }
                else if (p is PRectangle)
                {
                    width = ((PRectangle)p).Width;
                    height = ((PRectangle)p).Height;
                }
                else if (p is PArc)
                {
                    PArc pa = p as PArc;
                    startAngle = pa.StartAngle;
                    includedAngle = pa.IncludedAngle;
                    radius = pa.Radius;
                }
                else if (p is PEllipse)
                {
                    PEllipse pe = p as PEllipse;
                    startAngle = pe.StartAngle;
                    includedAngle = pe.IncludedAngle;
                    angle = pe.AxisAngle;
                    width = pe.Major;
                    if (pe.Minor != 0)
                    {
                        ratio = pe.Major / pe.Minor;
                    }
                }
            }

            // We're going to assume that the user selected a visible (selectable) object to match
            // so there's no need to ensure that the layer is visible

            switch (Globals.CommandProcessor.Type)
            {
                case CommandType.text:
                    Globals.TextAlign = textAlign;
                    Globals.TextPosition = textPosition;
                    Globals.TextLayerId = layerId;
                    Globals.TextStyleId = textStyleId;
                    break;

                case CommandType.dimension:
                    Globals.DimArrowStyleId = arrowStyleId;
                    Globals.DimensionLayerId = layerId;
                    Globals.DimensionType = dimType;
                    Globals.DimTextStyleId = textStyleId;
                    break;

                case CommandType.arrow:
                    Globals.DimensionLayerId = layerId;
                    Globals.ArrowLocation = arrowLocation;
                    Globals.ArrowStyleId = arrowStyleId;
                    break;

                case CommandType.doubleline:
                    Globals.LayerId = layerId;
                    Globals.DoublelineFill = fill;
                    Globals.DoublelineEndStyle = dbEndStyle;
                    Globals.DoubleLineWidth = dbWidth;
                    break;

                case CommandType.arc:
                case CommandType.arc3:
                    Globals.LayerId = layerId;
                    Globals.ArcFill = fill;
                    Globals.ArcRadius = radius;
                    Globals.ArcStartAngle = startAngle;
                    Globals.ArcIncludedAngle = includedAngle;
                    break;

                case CommandType.circle:
                case CommandType.circle3:
                    Globals.LayerId = layerId;
                    Globals.CircleFill = fill;
                    //Globals.ArcFill = fill;
                    Globals.CircleRadius = radius;
                    break;

                case CommandType.ellipse:
                    Globals.LayerId = layerId;
                    Globals.EllipseFill = fill;
                    Globals.EllipseMajorLength = width;
                    Globals.EllipseMajorMinorRatio = ratio;
                    Globals.EllipseStartAngle = startAngle;
                    Globals.EllipseIncludedAngle = includedAngle;
                    Globals.EllipseAxisAngle = angle;
                    break;

                case CommandType.rectangle:
                    Globals.LayerId = layerId;
                    Globals.RectangleFill = fill;
                    Globals.RectangleHeight = height;
                    Globals.RectangleWidth = width;
                    break;

                case CommandType.polygon:
                    Globals.LayerId = layerId;
                    Globals.PolygonFill = fill;
                    break;

                case CommandType.bspline:
                case CommandType.insert:
                case CommandType.insertsymbol:
                case CommandType.line:
                case CommandType.polyline:
                case CommandType.freehand:
                    Globals.LayerId = layerId;
                    break;
            }

            Gleam gleam = new Gleam(new List<Primitive>() { p });
            gleam.Start();

            Globals.Events.LayerSelectionChanged();

            Globals.CommandProcessor.Finish();
        }
#else
#endif

#endregion

#region IDrawingContainer

        public Point ModelToPaper(Point model)
        {
            Point paper = new Point((model.X + _modelOrigin.X) * _modelToPaperScale, Globals.ActiveDrawing.PaperSize.Height - ((model.Y + _modelOrigin.Y) * _modelToPaperScale));
            return paper;
        }

        public Point ModelToPaperRaw(Point model)
        {
            Point paper = new Point(model.X * _modelToPaperScale, Globals.ActiveDrawing.PaperSize.Height - model.Y * _modelToPaperScale);
            return paper;
        }

        public double ModelToPaper(double model)
        {
            double paper = model * _modelToPaperScale;

            return paper;
        }

        public Point PaperToModel(Point paper)
        {
            Point model = new Point((paper.X / _modelToPaperScale) - _modelOrigin.X, ((Globals.ActiveDrawing.PaperSize.Height - paper.Y) / _modelToPaperScale) - _modelOrigin.Y);

            return model;
        }

        public Point PaperToModelRaw(Point paper)
        {
            Point model = new Point(paper.X / _modelToPaperScale, (Globals.ActiveDrawing.PaperSize.Height - paper.Y) / _modelToPaperScale);

            return model;
        }

        public Point PaperToModelDelta(Point paper)
        {
            Point model = new Point(paper.X / _modelToPaperScale, -paper.Y / _modelToPaperScale);

            return model;
        }

        public FPoint PaperToModelF(Point paper)
        {
            FPoint model = new FPoint((paper.X / _modelToPaperScale) - _modelOrigin.X, ((Globals.ActiveDrawing.PaperSize.Height - paper.Y) / _modelToPaperScale) - _modelOrigin.Y);

            return model;
        }

        public Point ModelToPaperDelta(Point model)
        {
            Point paper = new Point(model.X * _modelToPaperScale, -model.Y * _modelToPaperScale);
            return paper;
        }

        public Size ModelToPaperSize(Size model)
        {
            Size paper = new Size(model.Width * _modelToPaperScale, model.Height * _modelToPaperScale);
            return paper;
        }

        public Point ModelToPaperDelta(FPoint model)
        {
            Point paper = new Point(model.X * _modelToPaperScale, -model.Y * _modelToPaperScale);
            return paper;
        }

        public double PaperToUser(double paper)
        {

            // Paper units are always in inches, so when displaying paper unit distances in a metric drawing
            // we need to convert the units to millimetres
            return _paperUnit == Unit.Millimeters ? paper * 25.4 : paper;
        }

        public double UserToPaper(double user)
        {
            // See PaperToUser() comment
            return _paperUnit == Unit.Millimeters ? user / 25.4 : user;
        }

        public FPoint PaperToModelDeltaF(Point paper)
        {
            FPoint model = new FPoint(paper.X / _modelToPaperScale, -paper.Y / _modelToPaperScale);
            if (paper.Y == 0)
            {
                paper.Y = +0F;    // This to avoid serialization of -0
            }
            return model;
        }

        public double PaperToModel(double paper)
        {
            double model = paper / _modelToPaperScale;

            return model;
        }
#endregion
    }
}
