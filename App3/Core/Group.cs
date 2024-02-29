using Cirros.Core;
using Cirros.Drawing;
using Cirros.Utility;
using CirrosCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
#if UWP
using Windows.Foundation;
#else
using System.Windows;
#endif

namespace Cirros.Primitives
{
    public class Group
    {
        protected List<Primitive> _items = new List<Primitive>();
        protected string _name;
        protected Rect _bounds = Rect.Empty;
        protected Guid _state = Guid.Empty;
        protected uint _flags = 0;

        protected List<int> _layers = new List<int>();
        protected List<int> _linetypes = new List<int>();
        protected List<int> _textstyles = new List<int>();
        protected List<int> _arrowstyles = new List<int>();
        protected List<string> _patternNames = new List<string>();
        protected List<string> _imageIds = new List<string>();

        private Unit _modelUnit = Unit.Inches;
        private Unit _paperUnit = Unit.Inches;
        private double _modelScale = 1;
        private bool _preferPaperSpace = false;
        private double _preferredScale = 1;
        private Point _entry = new Point(0, 0);
        private Point _exit = new Point(0, 0);
        private GroupInsertLocation _insertLocation;
        private List<GroupAttribute> _attributeList = new List<GroupAttribute>();
        //private bool _includeInLibrary;
        private Guid _id = Guid.Empty;

        private string _description = "";
        private CoordinateSpace _coordinateSpace = CoordinateSpace.Drawing;
        private string _folder;

        public Group()
        {
        }

        public Group(string name)
        {
            _name = name;
        }

        public void MoveOriginBy(double dx, double dy)
        {
            foreach (Primitive member in _items)
            {
                member.MoveTo(member.Origin.X + dx, member.Origin.Y + dy);
            }

            _bounds.X += dx;
            _bounds.Y += dy;
        }

        public void UpdateBounds()
        {
            List<PInstance> instances = Globals.ActiveDrawing.GetGroupInstances(this);

            foreach (PInstance instance in instances)
            {
                if (instance.Matrix == null || instance.Matrix.IsIdentity)
                {
                    Rect bounds = instance.Box;
                    bounds.X -= instance.Origin.X;
                    bounds.Y -= instance.Origin.Y;
                    _bounds = bounds;
                    break;
                }
                else if (instance.Matrix.M12 == 0 && instance.Matrix.M21 == 0)
                {
                    Rect bounds = instance.Box;
                    bounds.Width /= instance.Matrix.M11;
                    bounds.Height /= instance.Matrix.M22;

                    double dx = (instance.Origin.X - bounds.X) / instance.Matrix.M11;
                    double dy = (instance.Origin.Y - bounds.Y) / instance.Matrix.M22;
                    bounds.X = -dx;
                    bounds.Y = -dy;
                    _bounds = bounds;
                    break;
                }
            }
        }

        public void MovePrimitivesFromDrawing(double ox, double oy, List<Primitive> list, bool deleteOriginals = true)
        {
            _id = Guid.Empty;

            Point ll = new Point(ox, oy);
            Point ur = new Point(ox, oy);

            foreach (Primitive p in list)
            {
                ll.X = Math.Min(p.Box.Left, ll.X);
                ll.Y = Math.Min(p.Box.Top, ll.Y);
                ur.X = Math.Max(p.Box.Right, ur.X);
                ur.Y = Math.Max(p.Box.Bottom, ur.Y);

                if (deleteOriginals && p is PInstance && (((PInstance)p).GroupName.StartsWith(":")) && ((PInstance)p).ColorSpec == (uint)ColorCode.ByLayer)
                {
                    List<Primitive> members = Utilities.UnGroup(p as PInstance, false);

                    foreach (Primitive member in members)
                    {
#if true
                        AddMember(member);
#else
                        member.IsGroupMember = true;
                        _items.Add(member);
#endif
                        member.MoveTo(member.Origin.X - ox, member.Origin.Y - oy);

                        if (deleteOriginals)
                        {
                            Globals.ActiveDrawing.DeletePrimitive(member);
                        }
                    }
                }
                else
                {
                    Primitive copy = p.Clone();
                    copy.IsGroupMember = true;

                    if (copy is PText)
                    {
                        PText pt = copy as PText;
                        if (pt.AttributeName != null)
                        {
                            _attributeList.Add(new GroupAttribute(pt.AttributeName, pt.AttributeValue, pt.AttributeLines));
                        }
                    }

#if true
                    AddMember(copy);
#else
                    _items.Add(copy);  // add p, or copy of p?
#endif
                    copy.MoveTo(p.Origin.X - ox, p.Origin.Y - oy);

                    if (deleteOriginals)
                    {
                        Globals.ActiveDrawing.DeletePrimitive(p);
                    }
                }
            }

            ll.X -= ox;
            ll.Y -= oy;
            ur.X -= ox;
            ur.Y -= oy;

            _bounds = new Rect(ll, ur);
            System.Diagnostics.Debug.Assert(_bounds.Width != 0 || _bounds.Height != 0, "Null width or height in group");

            UpdateStyleLists();
        }

        public void ReassociateMemberInstances(string oldname, string newname)
        {
            foreach (Primitive m in _items)
            {
                if (m is PInstance)
                {
                    PInstance instance = m as PInstance;
                    if (instance.GroupName == oldname)
                    {
                        instance.Reassociate(newname);
                    }
                }
            }
        }

        private bool AddUniqueEntry(List<int> list, int entry)
        {
            if (entry != -1 && !list.Contains(entry))
            {
                list.Add(entry);
                return true;
            }
            return false;
        }

        protected async Task updateStyleLists(Primitive p)
        {
            if (AddUniqueEntry(_layers, p.LayerId))
            {
                Layer layer = Globals.LayerTable[p.LayerId];
                AddUniqueEntry(_linetypes, layer.LineTypeId);
            }

            if (p is PText)
            {
                AddUniqueEntry(_textstyles, ((PText)p).TextStyleId);
            }
            else
            {
                AddUniqueEntry(_linetypes, p.LineTypeId);

                if (p is PArrow)
                {
                    AddUniqueEntry(_arrowstyles, ((PArrow)p).ArrowStyleId);
                }
                else if (p is PDimension)
                {
                    PDimension pd = p as PDimension;
                    AddUniqueEntry(_arrowstyles, pd.ArrowStyleId);
                    AddUniqueEntry(_textstyles, pd.TextStyleId);
                }
                else if (p is PInstance)
                {
                    string name = ((PInstance)p).GroupName;
                    Group group = Globals.ActiveDrawing.GetGroup(name);

                    if (group != null)
                    {
                        foreach (Primitive m in group.Items)
                        {
                            await updateStyleLists(m);
                        }
                    }
                }
                else if (p is PImage image)
                {
                    if (_imageIds.Contains(image.ImageId) == false)
                    {
                        _imageIds.Add(image.ImageId);
                    }
                }
            }

            Patterns.AddPatternFromEntityToList(p, _patternNames);

        }

        public async void UpdateStyleLists()
        {
            _layers.Clear();
            _linetypes.Clear();
            _textstyles.Clear();
            _arrowstyles.Clear();
            _patternNames.Clear();
            _imageIds.Clear();

            foreach (Primitive p in _items)
            {
                await updateStyleLists(p);
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
            }
        }

        public CoordinateSpace CoordinateSpace
        {
            get
            {
                return _coordinateSpace;
            }
            set
            {
                _coordinateSpace = value;
            }
        }

        public string Folder
        {
            get
            {
                return _folder;
            }
            set
            {
                _folder = value;
            }
        }

        [XmlIgnore]
        public Rect ModelBounds
        {
            get
            {
                Point tl = Globals.ActiveDrawing.PaperToModelDelta(new Point(_bounds.Left, _bounds.Top));
                Point br = Globals.ActiveDrawing.PaperToModelDelta(new Point(_bounds.Right, _bounds.Bottom));
                return new Rect(tl, br);
            }
            set
            {
                Point tl = Globals.ActiveDrawing.ModelToPaperDelta(new FPoint(value.Left, value.Top));
                Point br = Globals.ActiveDrawing.ModelToPaperDelta(new FPoint(value.Right, value.Bottom));
                _bounds = new Rect(tl, br);
            }
        }

        [XmlElement("ModelBounds")]
        public FloatRect SerializeModelBounds
        {
            get
            {
                Point tl = Globals.ActiveDrawing.PaperToModelDelta(new Point(_bounds.Left, _bounds.Top));
                Point br = Globals.ActiveDrawing.PaperToModelDelta(new Point(_bounds.Right, _bounds.Bottom));
                return new FloatRect(tl, br);
            }
            set
            {
                Point tl = Globals.ActiveDrawing.ModelToPaperDelta(new FPoint(value.Left, value.Top));
                Point br = Globals.ActiveDrawing.ModelToPaperDelta(new FPoint(value.Right, value.Bottom));
                _bounds = new Rect(tl, br);
            }
        }

        public GroupInsertLocation InsertLocation
        {
            get
            {
                return _insertLocation;
            }
            set
            {
                _insertLocation = value;
            }
        }

        public Point Entry
        {
            get
            {
                return _entry;
            }
            set
            {
                _entry = value;
            }
        }

        public Point Exit
        {
            get
            {
                return _exit;
            }
            set
            {
                _exit = value;
            }
        }

        public List<int> Layers
        {
            get { return _layers; }
            set { _layers = value; }
        }

        public List<int> LineTypes
        {
            get { return _linetypes; }
            set { _linetypes = value; }
        }

        public List<int> TextStyles
        {
            get { return _textstyles; }
            set { _textstyles = value; }
        }

        public List<int> ArrowStyles
        {
            get { return _arrowstyles; }
            set { _arrowstyles = value; }
        }

        [System.Xml.Serialization.XmlArrayItem("ImageId")]
        public List<string> Images
        {
            get { return _imageIds; }
            set { _imageIds = value; }
        }

        public List<string> CrosshatchPatterns
        {
            get { return _patternNames; }
            set { _patternNames = value; }
        }

        public int AddMember(Primitive p)
        {
            _id = Guid.Empty;
            
            p.IsGroupMember = true;
            p.Parent = this;

            int index = _items.Count;
            _items.Add(p);

            var t = Task.Run(() => updateStyleLists(p));
            t.Wait();

            return index;
        }

        public void AddMemberAt(Primitive p, int index)
        {
            _id = Guid.Empty;

            p.IsGroupMember = true;
            p.Parent = this;

            var t = Task.Run(() => updateStyleLists(p));
            t.Wait();

            _items.Insert(index, p);
        }

        public void RemoveMemberAt(int index)
        {
            _id = Guid.Empty;

            _items.RemoveAt(index);
        }

        public void MoveMemberAt(int index, double dx, double dy)
        {
            _id = Guid.Empty;

            dx = Math.Round(dx, 5, MidpointRounding.AwayFromZero);
            dy = Math.Round(dy, 5, MidpointRounding.AwayFromZero);
            //System.Diagnostics.Debug.WriteLine("Move member: group {0}; index {1}; dx = {2}, dy = {3}", _name, index, dx, dy);

            if (index >= 0 && index < _items.Count)
            {
                Primitive member = _items[index];
                if (member != null)
                {
                    member.MoveByDelta(dx, dy);
                }

            }
        }

        public int MoveMemberUp(int index)
        {
            if (index > 0)
            {
                _id = Guid.Empty;

                Primitive member = _items[index];
                _items.RemoveAt(index);
                _items.Insert(--index, member);

            }
            return index;
        }

        public int MoveMemberDown(int index)
        {
            if (index < (_items.Count - 1))
            {
                _id = Guid.Empty;

                Primitive member = _items[index];
                _items.RemoveAt(index);
                _items.Insert(++index, member);

            }
            return index;
        }

        public bool ContainsGroup(string name)
        {
            if (name == _name)
            {
                return true;
            }

            foreach (Primitive member in _items)
            {
                if (member is PInstance)
                {
                    Group g = Globals.ActiveDrawing.GetGroup(((PInstance)member).GroupName);
                    if (g.ContainsGroup(name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool ContainsLayer(int id)
        {
            return _layers.Contains(id);
        }

        public bool ContainsLineType(int id)
        {
            return _linetypes.Contains(id);
        }

        public bool ContainsTextStyle(int id)
        {
            return _textstyles.Contains(id);
        }

        public bool ContainsArrowStyle(int id)
        {
            return _arrowstyles.Contains(id);
        }

        [System.Xml.Serialization.XmlIgnore]
        public Rect PaperBounds
        {
            get
            {
                return _bounds;
            }
            set
            {
                _bounds = value;
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public Guid State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }

        public uint Flags
        {
            get
            {
                return _flags;
            }
            set
            {
                _flags = value;
            }
        }


        [XmlIgnore]
        public double ModelScale
        {
            get
            {
                return _modelScale;
            }
            set
            {
                _modelScale = value;
            }
        }

        [XmlElement("ModelScale")]
        public float SerialzeModelScale
        {
            get
            {
                return (float)_modelScale;
            }
            set
            {
                _modelScale = value;
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

        public bool PreferPaperSpace
        {
            // PreferPaperSpace is a hint that the symbol should be loaded into the drawing in paper units instead of model units
            // this flag is used only when the symbol is loaded and causes the PreferredScale value to be set
            // it doesn't make sense to access this value after the symbol has been loaded
            get
            {
                return _preferPaperSpace;
            }
            set
            {
                _preferPaperSpace = value;
            }
        }

        public double PreferredScale
        {
            // PreferredScale is the default scale factor that should be used when inserting symbols into the drawing
            // It converts the symbol's coordinate system (paper unit, model unit, model scale) inte the drawings coordinate system
            // This value is calculated when the symbol is loaded - the symbol's saved value is irrelevant
            get
            {
                return _preferredScale;
            }
            set
            {
                _preferredScale = value;
            }
        }

        public List<GroupAttribute> AttributeList
        {
            get { return _attributeList; }
            set { _attributeList = value; }
        }

        public List<GroupAttribute> CloneAttributeList()
        {
            List<GroupAttribute> newList = new List<GroupAttribute>(_attributeList.Count);
            foreach (GroupAttribute a in _attributeList)
            {
                newList.Add(new GroupAttribute(a.Prompt, a.Value, a.MaxLines));
            }
            return newList;
        }

        [System.Xml.Serialization.XmlIgnore]
        public List<Primitive> Items
        {
            get
            {
                return _items;
            }
        }

        [System.Xml.Serialization.XmlArrayItem("Entity")]
        public Cirros.Drawing.Entity[] Entities
        {
            get
            {
                Cirros.Drawing.Entity[] entities = new Cirros.Drawing.Entity[_items.Count];

                for (int i = 0; i < _items.Count; i++)
                {
                    entities[i] = _items[i].Serialize();
                }

                return entities;
            }
            set
            {
                _items.Clear();

                foreach (Cirros.Drawing.Entity e in value)
                {
                    e.IsGroupMember = true;
                    try
                    {
                        Primitive p = Primitive.DeserializeFromEntity(e, Globals.ActiveDrawing);
                        AddMember(p);
                    }
                    catch
                    {

                    }
                }

                //if (_layers.Count == 0)
                {
                    UpdateStyleLists();
                }
            }
        }

        public bool IncludeInLibrary
        {
            get
            {
                return string.IsNullOrWhiteSpace(_name) == false && _name.StartsWith(":") == false;
            }
            //set
            //{
            //    _includeInLibrary = value;
            //}
        }

        public Guid Id
        {
            get
            {
                if (_id == Guid.Empty)
                {
                    _id = Guid.NewGuid();
                }
                return _id;
            }
            set
            {
                _id = value;
            }
        }
    }

    public class GroupAttribute
    {
        public GroupAttribute()
        {
        }

        public GroupAttribute(string name, string val, int maxlines)
        {
            Prompt = name;
            Value = val;
            MaxLines = maxlines;
        }

        public string Prompt = "";
        public string Value = "";
        public int MaxLines = 1;
    }

    public enum GroupInsertLocation
    {
        None = 0,
        Origin,
        Start,
        Center,
        Exit
    }

    public enum CoordinateSpace
    {
        Drawing = 0,
        Paper,
        Model
    }

    public struct FloatRect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public FloatRect(Point topLeft, Point bottomRight)
        {
            X = (float)Math.Min(topLeft.X, bottomRight.X);
            Y = (float)Math.Min(topLeft.Y, bottomRight.Y);
            Width = (float)Math.Abs(topLeft.X - bottomRight.X);
            Height = (float)Math.Abs(topLeft.Y - bottomRight.Y);
        }

        public FloatRect(Rect rect)
        {
            X = (float)rect.X;
            Y = (float)rect.Y;
            Width = (float)rect.Width;
            Height = (float)rect.Height;
        }

        public float Left { get { return X; } }
        public float Bottom { get { return Y; } }
        public float Right { get { return X + Width; } }
        public float Top { get { return Y + Height; } }
    }
}
