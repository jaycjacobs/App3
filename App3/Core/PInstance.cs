using Cirros.Core;
using Cirros.Display;
using Cirros.Drawing;
using Cirros.Utility;
using System;
using System.Collections.Generic;
#if UWP
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
#else
using CirrosCore;
using System.Windows;
using System.Windows.Media;
#endif

namespace Cirros.Primitives
{
    public class PInstance : Primitive
    {
        // wallflags
        // 0x1 - draw top
        // 0x2 - draw bottom
        // 0x4 - draw left
        // 0x8 - draw right
        // 0x10 - offset half wall size
       
        protected string _groupName;
        protected uint _flip = 0;
        protected double _wallSize = 0;
        protected Matrix _localMatrix = Matrix.Identity;
        protected Matrix _translateMatrix = Matrix.Identity;
        protected List<CPoint> _wallFramePoints = new List<CPoint>();
        protected List<Primitive> _members = new List<Primitive>();

        public double WallSize
        {
            get
            {
                return _wallSize;
            }
            set
            {
                _wallSize = value;
            }
        }

        private List<GroupAttribute> _attributeList = new List<GroupAttribute>();

        public PInstance(Point o, string name)
            : base(o)
        {
            _groupName = name;
            _flip = 0;
            _wallSize = 0;

            Group group = Globals.ActiveDrawing.GetGroup(_groupName);
            if (group != null)
            {
                _attributeList = group.CloneAttributeList();
            }
        }

        public PInstance(PInstance original)
            : base(original)
        {
            _groupName = original._groupName;
            _flip = original._flip;
            _wallSize = original._wallSize;
            _attributeList = original.CloneAttributeList();
        }

        public PInstance(Entity e, IDrawingContainer drawingCanvas)
            : base(e, drawingCanvas)
        {
            _lineTypeId = e.LineTypeId;
            _lineWeightId = e.LineWeightId;
            _flip = e.Flip;
            _wallSize = e.WallSize;

            _groupName = e.Name;

            _attributeList = new List<GroupAttribute>(e.Attributes);
        }

        public override void Draw()
        {
            base.Draw();
        }

        public override Entity Serialize()
        {
            Entity e = base.Serialize();

            e.LineTypeId = LineTypeId;
            e.LineWeightId = LineWeightId;
            e.Flip = _flip;

            if (e.WallSize != 0)
            {
                e.WallSize = (float)_wallSize;
            }

            e.Name = _groupName;

            if (_attributeList.Count > 0)
            {
                e.Attributes = _attributeList;
            }

            return e;
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

        public void SetAttributeValue(string prompt, string value)
        {
            foreach (GroupAttribute ga in _attributeList)
            {
                if (ga.Prompt == prompt)
                {
                    ga.Value = value;
                    //break;
                }
            }
        }

        public override Primitive Clone()
        {
            return new PInstance(this);
        }

        private Primitive cloneMember(Primitive p)
        {
            Primitive copy = null;
            try
            {
                Matrix matrix = _matrix;

                if (_flip != 0)
                {
                    Matrix fm = Matrix.Identity;
                    if ((_flip & 0x1) != 0)
                    {
                        fm = CGeometry.ScaleMatrix(fm, -1, 1);
                    }
                    if ((_flip & 0x2) != 0)
                    {
                        fm = CGeometry.ScaleMatrix(fm, 1, -1);
                    }
                    matrix = CGeometry.MultiplyMatrix(fm, _matrix);
                }

                copy = p.Clone();
                copy.IsInstanceMember = true;
                copy.ZIndex = this.ZIndex;
                copy.Parent = this;

                if (copy.LayerId == 0)
                {
                    // If the member's layer is unassigned, it goes on the instance layer
                    copy.LayerId = _layerId;
                }

                if (_colorSpec != (uint)ColorCode.ByLayer)
                {
                    // If the instance color is explicitly set, all of the members will be that color
                    if (copy.ColorSpec == (uint)ColorCode.ByLayer)
                    {
                        copy.ColorSpec = _colorSpec;
                    }
                    if (copy.Fill == (uint)ColorCode.ByLayer)
                    {
                        copy.Fill = _colorSpec;
                    }
                    else if (copy.Fill == (uint)ColorCode.SameAsOutline)
                    {
                        copy.Fill = copy.ColorSpec;
                    }
                }

                if (_translateMatrix.IsIdentity == false)
                {
                    Point o = _translateMatrix.Transform(copy.Origin);
                    copy.MoveTo(o.X, o.Y);
                }
                if (matrix.IsIdentity == false)
                {
                    copy.Transform(0, 0, matrix);
                }

                if (copy is PText)
                {
                    PText pt = copy as PText;
                    if (pt.AttributeName != null)
                    {
                        foreach (GroupAttribute ga in _attributeList)
                        {
                            if (ga.Prompt == pt.AttributeName)
                            {
                                pt.AttributeValue = ga.Value;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Analytics.ReportError("cloneMember: ", e, 2, 403);
            }

            return copy;
        }

        private void UpdateLocalMatrix(Group g)
        {
        }

        public override PrimitiveType TypeName
        {
            get
            {
                return PrimitiveType.Instance;
            }
        }

        public override int ActiveLayer
        {
            get
            {
                if (Globals.UIVersion == 0)
                {
                    return Globals.LayerId;
                }
                else
                {
                    if (Globals.LayerTable.ContainsKey(Globals.ActiveInstanceLayerId))
                    {
                        return Globals.ActiveInstanceLayerId;
                    }
                    else
                    {
                        return Globals.ActiveLayerId;
                    }
                }
            }
        }

        public override int LineWeightId
        {
            get
            {
                return base.LineWeightId;
            }
            set
            {
                // Instances do not have line weights
                _lineWeightId = value;
            }
        }

        public override int LineTypeId
        {
            get
            {
                return base.LineTypeId;
            }
            set
            {
                // Instances do not have line types
                _lineTypeId = value;
            }
        }

        public uint Flip
        {
            get
            {
                return _flip;
            }
            set
            {
                _flip = value % 4;
                this.Draw();
            }
        }

        public List<GroupAttribute> AttributeList
        {
            get
            {
                return _attributeList;
            }
            set
            {
                _attributeList = value;
            }
        }

        public bool ContainsLayer(int layerid)
        {
            bool contains = false;

            if (this.LayerId == layerid)
            {
                contains = true;
            }
            else
            {
                Group group = Globals.ActiveDrawing.GetGroup(_groupName);
                contains = group.ContainsLayer(layerid);
            }
            return contains;
        }

        public bool ContainsLineType(int lineTypeId)
        {
            Group group = Globals.ActiveDrawing.GetGroup(_groupName);
            return group.ContainsLineType(lineTypeId);
        }

        public bool ContainsTextStyle(int textstyleid)
        {
            Group group = Globals.ActiveDrawing.GetGroup(_groupName);
            return group.ContainsTextStyle(textstyleid);
        }

        public bool ContainsArrowStyle(int arrowstyleid)
        {
            Group group = Globals.ActiveDrawing.GetGroup(_groupName);
            return group.ContainsArrowStyle(arrowstyleid);
        }

        public string GroupName
        {
            get
            {
                return _groupName;
            }
        }

        public void Reassociate(string groupName)
        {
            _groupName = groupName;
        }

        public override void ClearStaticConstructNodes()
        {
            // disable base clase functionality
            // this is handled in vectorize
        }

        public override bool Pick(Point paper, out double distance)
        {
            bool hit = false;

            if (base.Pick(paper, out distance))
            {
                double d = distance;

                paper.X -= _origin.X;
                paper.Y -= _origin.Y;

                foreach (Primitive p in _members)
                {
                    if (p.Pick(paper, out distance))
                    {
                        if (distance < d)
                        {
                            hit = true;
                            d = distance;
                        }
                    }
                }
            }

            return hit;
        }

        protected override void _drawHandles(Handles handles)
        {
            // Handles are not useful on an instance, are they?
        }

        protected override void _moveHandle(Handles handles, int id, double dx, double dy)
        {
            // Handles are not useful on an instance, are they?
        }

        public override VectorEntity Vectorize(VectorContext context)
        {
            VectorEntity ve = base.Vectorize(context);
            ve.RemoveChildren();

            Group group = Globals.ActiveDrawing.GetGroup(_groupName);

            if (group != null)
            {
                uint flags = group.Flags;

                _translateMatrix = Matrix.Identity;
                _wallFramePoints.Clear();

                if (_wallSize > 0)
                {
                    double offset = _wallSize / 2;

                    if ((flags & 0x10) != 0)
                    {
                        // need to offset
                        _translateMatrix = CGeometry.TranslateMatrix(_translateMatrix, 0, -offset);
                    }
                    if ((flags & 0xf) != 0)
                    {
                        // need to frame wall opening

                        Point entry = Globals.ActiveDrawing.ModelToPaperDelta(group.Entry);
                        Point exit = Globals.ActiveDrawing.ModelToPaperDelta(group.Exit);
                        double angle = Construct.Angle(entry, exit);
                        Point d = Construct.PolarOffset(new Point(0, 0), offset, angle + Math.PI / 2);
                        Point p0 = new Point(entry.X + d.X, entry.Y + d.Y);
                        Point p1 = new Point(exit.X + d.X, exit.Y + d.Y);
                        Point p2 = new Point(exit.X - d.X, exit.Y - d.Y);
                        Point p3 = new Point(entry.X - d.X, entry.Y - d.Y);

                        //List<Point> pc = new List<Point>();

                        if ((flags & 0x1) != 0)
                        {
                            _wallFramePoints.Add(new CPoint(p0, 0));
                            _wallFramePoints.Add(new CPoint(p1, 1));
                        }
                        if ((flags & 0x2) != 0)
                        {
                            _wallFramePoints.Add(new CPoint(p1, 0));
                            _wallFramePoints.Add(new CPoint(p2, 1));
                        }
                        if ((flags & 0x4) != 0)
                        {
                            _wallFramePoints.Add(new CPoint(p2, 0));
                            _wallFramePoints.Add(new CPoint(p3, 1));
                        }
                        if ((flags & 0x8) != 0)
                        {
                            _wallFramePoints.Add(new CPoint(p3, 0));
                            _wallFramePoints.Add(new CPoint(p0, 1));
                        }
                    }
                }

                if (_staticConstructNodes.Count == 0)
                {
                    _staticConstructNodes.Clear();
                }

                for (int i = 0; i < group.Items.Count; i++)
                {
                    Primitive copy = cloneMember(group.Items[i]);
                    copy.Id = (uint)i;

                    _staticConstructNodes.AddRange(copy.ConstructNodes);

                    VectorEntity v = copy.Vectorize(context);

                    moveVE(v, _origin);
                    ve.AddChild(v);
                }

                if (_wallFramePoints.Count > 1)
                {
                    for (int i = 0; i < _wallFramePoints.Count; i += 2)
                    {
                        List<Point> pc = new List<Point>();
                        Point p0 = _matrix.Transform(_wallFramePoints[i].Point);
                        Point p1 = _matrix.Transform(_wallFramePoints[i + 1].Point);
                        p0.X += _origin.X;
                        p0.Y += _origin.Y;
                        p1.X += _origin.X;
                        p1.Y += _origin.Y;
                        pc.Add(p0);
                        pc.Add(p1);
                        ve.AddChild(pc);
                    }
                }

                foreach (ConstructNode node in _staticConstructNodes)
                {
                    node.Location.X += _origin.X;
                    node.Location.Y += _origin.Y;
                }

                _staticConstructNodes.Add(new ConstructNode(_origin, "origin"));
            }

            return ve;
        }

        private void moveVE(VectorEntity v, Point offset)
        {
            foreach (object o in v.Children)
            {
                if (o is List<Point>)
                {
                    List<Point> pc = o as List<Point>;
                    for (int i = 0; i < pc.Count; i++)
                    {
                        pc[i] = new Point(pc[i].X + offset.X, pc[i].Y + offset.Y);
                    }
                }
                else if (o is VectorEntity)
                {
                    moveVE(o as VectorEntity, offset);
                }
                else if (o is VectorMarkerEntity)
                {
                    VectorMarkerEntity vm = o as VectorMarkerEntity;
                    vm.Move(offset.X, offset.Y);
                }
                else if (o is VectorArcEntity)
                {
                    VectorArcEntity va = o as VectorArcEntity;
                    va.Move(offset.X, offset.Y);
                    //va.Center = new Point(va.Center.X + offset.X, va.Center.Y + offset.Y);
                }
                else if (o is VectorTextEntity)
                {
                    VectorTextEntity vt = o as VectorTextEntity;
                    vt.Move(offset.X, offset.Y);
                    //vt.Location = new Point(vt.Location.X + offset.X, vt.Location.Y + offset.Y);
                    //vt.Origin = new Point(vt.Origin.X + offset.X, vt.Origin.Y + offset.Y);
                }
                else if (o is VectorImageEntity)
                {
                    VectorImageEntity vi = o as VectorImageEntity;
                    vi.Move(offset.X, offset.Y);
                    //vi.Origin = new Point(vi.Origin.X + offset.X, vi.Origin.Y + offset.Y);
                }
            }
        }

        public void HighlightMember(int index)
        {
            this.Highlight(false);
#if UWP
            Globals.DrawingCanvas.VectorListControl.HighlightMember(_objectId, index);
#else
#endif
        }
    }
}
