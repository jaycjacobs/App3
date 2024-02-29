using Cirros.Drawing;
using Cirros.Primitives;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Actions
{
    public class CAction
    {
        ActionID _actionId;
        object _subject;
        object _predicate;
        object _predicate2;

        public CAction(ActionID actionId, object subject = null, object predicate = null, object predicate2 = null)
        {
            _actionId = actionId;
            _subject = subject;
            _predicate = predicate;
            _predicate2 = predicate2;
        }

        public ActionID ID
        {
            get
            {
                return _actionId;
            }
        }

        public object Subject
        {
            get
            {
                return _subject;
            }
        }

        public CAction UndoExecute()
        {
            CAction redoAction = null;

            //System.Diagnostics.Debug.WriteLine("Action.Execute(): {0}", _actionId);
            switch (_actionId)
            {
                case ActionID.MultiUndo:
                    {
                        int count = (int)_subject;
                        for (int i = 0; i < count; i++)
                        {
                            Globals.CommandDispatcher.Undo();
                        }
                    }
                    redoAction = this;
                    break;

                case ActionID.Move:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.Move, p, p.Origin);

                            Point xy = (Point)_predicate;
                            p.MoveTo(xy.X, xy.Y);
                        }
                    }
                    break;

                case ActionID.DeleteGroupMember:
                    {
                        if (_subject is Group && _predicate is Primitive && _predicate2 is int)
                        {
                            Group g = _subject as Group;
                            Primitive member = _predicate as Primitive;
                            int memberIndex = (int)_predicate2;
                            g.RemoveMemberAt(memberIndex);
                            redoAction = new CAction(ActionID.AddGroupMember, g, member, memberIndex);
                        }
                        else if (_subject is PInstance && _predicate is Primitive && _predicate2 is int)
                        {
                            Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_subject).GroupName);
                            Primitive member = _predicate as Primitive;
                            int memberIndex = (int)_predicate2;
                            g.RemoveMemberAt(memberIndex);
                            redoAction = new CAction(ActionID.AddGroupMember, g, member, memberIndex);
                        }
                    }
                    break;

                case ActionID.AddGroupMember:
                    {
                        if (_subject is Group && _predicate is Primitive && _predicate2 is int)
                        {
                            Group g = _subject as Group;
                            Primitive member = _predicate as Primitive;
                            int memberIndex = (int)_predicate2;
                            g.AddMemberAt(member, memberIndex);
                            redoAction = new CAction(ActionID.DeleteGroupMember, g, member, memberIndex);
                        }
                    }
                    break;

                case ActionID.MoveGroupMember:
                    {
                        Group g = _subject as Group;
                        if (g != null && _predicate is int && _predicate2 is Point)
                        {
                            int index = (int)_predicate;
                            if (index >= 0 && index < g.Items.Count)
                            {
                                Primitive member = g.Items[index];
                                redoAction = new CAction(ActionID.MoveGroupMember, g, index, member.Origin);

                                Point xy = (Point)_predicate2;
                                g.MoveMemberAt(index, xy.X - member.Origin.X, xy.Y - member.Origin.Y);
                            }
                        }
                    }
                    break;

                case ActionID.DeletePrimitive:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            Globals.ActiveDrawing.DeletePrimitive(p);
                            redoAction = new CAction(ActionID.RestorePrimitive, p);
                        }
                    }
                    break;

                case ActionID.RestorePrimitive:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                           Globals.ActiveDrawing.RestoreDeletedPrimitive(p);
                            redoAction = new CAction(ActionID.DeletePrimitive, p);
                        }
                    }
                    break;

                case ActionID.RestoreMatrix:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.RestoreMatrix, p, p.Origin, p.Matrix);

                            Point xy = (Point)_predicate;
                            p.SetTransform(xy.X, xy.Y, (Matrix)_predicate2);
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.UnNormalize:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.Normalize, p);

                            p.UnNormalize((Matrix)_predicate);
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.Normalize:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.UnNormalize, p, p.Matrix);

                            p.Normalize(false);
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.RestorePoints:
                    {
                        if (_subject is PLine)
                        {
                            PLine p = _subject as PLine;

                            redoAction = new CAction(ActionID.RestorePoints, p, p.CPoints);

                            p.CPoints = (List<CPoint>)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.MoveVertex:
                    {
                        if (_subject is Primitive)
                        {
                            Primitive p = _subject as Primitive;
                            int handleId = (int)_predicate;

                            CPoint cp = p.GetHandlePoint(handleId);
                            redoAction = new CAction(ActionID.MoveVertex, p, handleId, cp.Point);

                            Point p0 = (Point)_predicate2;
                            p.MoveHandlePoint(handleId, p0);
                        }
                    }
                    break;

                case ActionID.SetLayer:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetLayer, p, p.LayerId);
                            p.LayerId = (int)_predicate;
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetColor:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetColor, p, p.ColorSpec);
                            p.ColorSpec = (uint)_predicate;
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetFill:
                    {
                        if (_subject is Primitive)
                        {
                            redoAction = new CAction(ActionID.SetFill, _subject, ((Primitive)_subject).Fill);
                            ((Primitive)_subject).Fill = (uint)_predicate;
                            ((Primitive)_subject).Draw();

                            Globals.Events.PrimitiveSelectionPropertyChanged(_subject as Primitive);
                        }
                    }
                    break;

                case ActionID.SetLineWeight:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetLineWeight, p, p.LineWeightId);
                            p.LineWeightId = (int)_predicate;
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetLineType:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetLineType, p, p.LineTypeId);
                            p.LineTypeId = (int)_predicate;
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetOpacity:
                    {
                        if (_subject is PImage)
                        {
                            PImage pi = _subject as PImage;
                            redoAction = new CAction(ActionID.SetOpacity, pi, pi.Opacity);
                            pi.Opacity = (double)_predicate;
                            Globals.Events.PrimitiveSelectionPropertyChanged(pi);
                        }
                    }
                    break;

                case ActionID.SetWidth:
                    {
                        if (_subject is PRectangle)
                        {
                            PRectangle p = _subject as PRectangle;
                            if (p != null)
                            {
                                redoAction = new CAction(ActionID.SetWidth, p, p.Width);
                                p.Width = (double)_predicate;
                                p.Draw();
                            }
                        }
                        else if (_subject is PDoubleline)
                        {
                            PDoubleline p = _subject as PDoubleline;
                            if (p != null)
                            {
                                redoAction = new CAction(ActionID.SetWidth, p, p.Width);
                                p.Width = (double)_predicate;
                                p.Draw();
                            }
                        }
                        else if (_subject is PEllipse)
                        {
                            PEllipse p = _subject as PEllipse;
                            if (p != null)
                            {
                                redoAction = new CAction(ActionID.SetWidth, p, p.Major);
                                p.Major = (double)_predicate;
                                p.Draw();
                            }
                        }
                        //else if (_subject is PImage)
                        //{
                        //    PImage p = _subject as PImage;
                        //    if (p != null)
                        //    {
                        //        redoAction = new CAction(ActionID.SetWidth, p, p.Width);
                        //        p.Width = (double)_predicate;
                        //        p.Draw();
                        //    }
                        //}
                        else
                        {
                            break;
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetHeight:
                    {
                        if (_subject is PRectangle)
                        {
                            PRectangle p = _subject as PRectangle;
                            if (p != null)
                            {
                                redoAction = new CAction(ActionID.SetHeight, p, p.Height);
                                p.Height = (double)_predicate;
                                p.Draw();
                            }
                        }
                        else if (_subject is PEllipse)
                        {
                            PEllipse p = _subject as PEllipse;
                            if (p != null)
                            {
                                redoAction = new CAction(ActionID.SetHeight, p, p.Minor);
                                p.Minor = (double)_predicate;
                                p.Draw();
                            }
                        }
                        else if (_subject is PText)
                        {
                            PText p = _subject as PText;
                            if (p != null)
                            {
                                redoAction = new CAction(ActionID.SetHeight, p, p.Size);
                                p.Size = (double)_predicate;
                                p.Draw();
                            }
                        }
                        //else if (_subject is PImage)
                        //{
                        //    PImage p = _subject as PImage;
                        //    if (p != null)
                        //    {
                        //        redoAction = new CAction(ActionID.SetHeight, p, p.Height);
                        //        p.Height = (double)_predicate;
                        //        p.Draw();
                        //    }
                        //}
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetRadius:
                    {
                        if (_subject is PArc)
                        {
                            PArc p = _subject as PArc;
                            redoAction = new CAction(ActionID.SetRadius, p, p.Radius);
                            p.Radius = (double)_predicate;
                            p.Draw();
                        }
                        else if (_subject is PLine)
                        {
                            PLine p = _subject as PLine;
                            redoAction = new CAction(ActionID.SetRadius, p, p.Radius);
                            p.Radius = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetAngle:
                    {
                        if (_subject is PEllipse)
                        {
                            PEllipse p = _subject as PEllipse;
                            redoAction = new CAction(ActionID.SetAngle, p, p.AxisAngle);
                            p.AxisAngle = (double)_predicate;
                            p.Draw();
                        }
                        else if (_subject is PText)
                        {
                            PText p = _subject as PText;
                            redoAction = new CAction(ActionID.SetAngle, p, p.Angle);
                            p.Angle = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetSpacing:
                    {
                        if (_subject is PText)
                        {
                            PText p = _subject as PText;
                            redoAction = new CAction(ActionID.SetSpacing, p, p.CharacterSpacing);
                            p.CharacterSpacing = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetLineSpacing:
                    {
                        if (_subject is PText)
                        {
                            PText p = _subject as PText;
                            redoAction = new CAction(ActionID.SetLineSpacing, p, p.LineSpacing);
                            p.LineSpacing = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetStartAngle:
                    {
                        if (_subject is PArc)
                        {
                            PArc p = _subject as PArc;
                            redoAction = new CAction(ActionID.SetStartAngle, p, p.StartAngle);
                            p.StartAngle = (double)_predicate;
                            p.Draw();
                        }
                        else if (_subject is PEllipse)
                        {
                            PEllipse p = _subject as PEllipse;
                            redoAction = new CAction(ActionID.SetStartAngle, p, p.StartAngle);
                            p.StartAngle = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetIncludedAngle:
                    {
                        if (_subject is PArc)
                        {
                            PArc p = _subject as PArc;
                            redoAction = new CAction(ActionID.SetIncludedAngle, p, p.IncludedAngle);
                            p.IncludedAngle = (double)_predicate;
                            p.Draw();
                        }
                        else if (_subject is PEllipse)
                        {
                            PEllipse p = _subject as PEllipse;
                            redoAction = new CAction(ActionID.SetIncludedAngle, p, p.IncludedAngle);
                            p.IncludedAngle = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetIsCircle:
                    {
                        PArc3 p = _subject as PArc3;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetIsCircle, p, p.IsCircle);
                            p.IsCircle = (bool)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetFillRule:
                    {
                        PPolygon p = _subject as PPolygon;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetFillRule, p, p.FillEvenOdd);
                            p.FillEvenOdd = (bool)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.SetPattern:
                    {
                        PPolygon p = _subject as PPolygon;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetPattern, p, p.FillPattern);
                            p.FillPattern = (string)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.SetPatternScale:
                    {
                        PPolygon p = _subject as PPolygon;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetPatternScale, p, p.PatternScale);
                            p.PatternScale = (double)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.SetPatternAngle:
                    {
                        PPolygon p = _subject as PPolygon;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetPatternAngle, p, p.PatternAngle);
                            p.PatternAngle = (double)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.SetTextStyle:
                    if (_subject is PText)
                    {
                        PText p = _subject as PText;
                        redoAction = new CAction(ActionID.SetTextStyle, p, p.TextStyleId);
                        p.TextStyleId = (int)_predicate;
                        p.Draw();
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    else if (_subject is PDimension)
                    {
                        PDimension p = _subject as PDimension;
                        redoAction = new CAction(ActionID.SetTextStyle, p, p.TextStyleId);
                        p.TextStyleId = (int)_predicate;
                        p.Draw();
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetAlignment:
                    {
                        PText p = _subject as PText;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetAlignment, p, p.Alignment);
                            p.Alignment = (TextAlignment)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetPosition:
                    {
                        PText p = _subject as PText;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetPosition, p, p.Position);
                            p.Position = (TextPosition)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.EditText:
                    {
                        PText p = _subject as PText;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.EditText, p, p.Text);
                            p.Text = (string)_predicate;
                            //p.TextStyleChanged();
                            p.Draw();
                            Globals.Events.PrimitiveSelectionPropertyChanged(p);
                        }
                    }
                    break;

                case ActionID.EditAttribute:
                    {
                        PInstance p = _subject as PInstance;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.EditAttribute, p, p.CloneAttributeList());
                            p.AttributeList = (List<GroupAttribute>)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.Flip:
                    {
                        PInstance p = _subject as PInstance;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.Flip, p, p.Flip);
                            p.Flip = (uint)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.SetEndStyle:
                    {
                        PDoubleline p = _subject as PDoubleline;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetEndStyle, p, p.EndStyle);
                            p.EndStyle = (DbEndStyle)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetDimensionType:
                    {
                        PDimension p = _subject as PDimension;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetDimensionType, p, p.DimensionType);
                            p.DimensionType = (PDimension.DimType)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.ShowDimensionText:
                    {
                        PDimension p = _subject as PDimension;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.ShowDimensionText, p, p.ShowText);
                            p.ShowText = (bool)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.ShowDimensionExtension:
                    {
                        PDimension p = _subject as PDimension;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.ShowDimensionExtension, p, p.ShowExtension);
                            p.ShowExtension = (bool)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetArrowStyle:
                    {
                        if (_subject is PArrow)
                        {
                            PArrow p = _subject as PArrow;
                            if (p != null)
                            {
                                redoAction = new CAction(ActionID.SetArrowStyle, p, p.ArrowStyleId);
                                p.ArrowStyleId = (int)_predicate;
                                p.Draw();
                            }
                        }
                        else if (_subject is PDimension)
                        {
                            PDimension p = _subject as PDimension;
                            if (p != null)
                            {
                                redoAction = new CAction(ActionID.SetArrowStyle, p, p.ArrowStyleId);
                                p.ArrowStyleId = (int)_predicate;
                                p.Draw();
                            }
                        }

                        Globals.Events.PrimitiveSelectionPropertyChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetArrowLocation:
                    {
                        PArrow p = _subject as PArrow;
                        if (p != null)
                        {
                            redoAction = new CAction(ActionID.SetArrowLocation, p, p.ArrowLocation);
                            p.ArrowLocation = (ArrowLocation)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetLineTypeDefinition:
                    {
                        int id = (int)_subject;
                        if (Globals.LineTypeTable.ContainsKey(id))
                        {
                            redoAction = new CAction(ActionID.SetLineTypeDefinition, id, Globals.LineTypeTable[id]);
                            Globals.LineTypeTable[id] = (LineType)_predicate;
                            LineType.PropagateLineTypeChanges(id);
                        }
                    }
                    break;

                case ActionID.SetLayerDefinition:
                    {
                        int id = (int)_subject;
                        if (Globals.LayerTable.ContainsKey(id))
                        {
                            redoAction = new CAction(ActionID.SetLayerDefinition, id, Globals.LayerTable[id]);
                            Globals.LayerTable[id] = (Layer)_predicate;
                            Layer.PropagateLayerChanges(id);
                        }
                    }
                    break;

                case ActionID.SetTextStyleDefinition:
                    {
                        int id = (int)_subject;
                        if (Globals.TextStyleTable.ContainsKey(id))
                        {
                            redoAction = new CAction(ActionID.SetTextStyleDefinition, id, Globals.TextStyleTable[id]);
                            Globals.TextStyleTable[id] = (TextStyle)_predicate;
                            TextStyle.PropagateTextStyleChanges(id);
                        }
                    }
                    break;

                case ActionID.SetArrowStyleDefinition:
                    {
                        int id = (int)_subject;
                        if (Globals.ArrowStyleTable.ContainsKey(id))
                        {
                            redoAction = new CAction(ActionID.SetArrowStyleDefinition, id, Globals.ArrowStyleTable[id]);
                            Globals.ArrowStyleTable[id] = (ArrowStyle)_predicate;
                            ArrowStyle.PropagateArrowStyleChanges(id);
                        }
                    }
                    break;

                case ActionID.DeleteLayer:
                    {
                        Layer layer = _subject as Layer;
                        if (layer != null)
                        {
                            Globals.ActiveDrawing.DeleteLayer(layer.Id);
                            redoAction = new CAction(ActionID.RestoreLayer, layer);
                        }
                    }
                    break;

                case ActionID.RestoreLayer:
                    {
                        Layer layer = _subject as Layer;
                        if (layer != null)
                        {
                            Globals.ActiveDrawing.AddLayer(layer);
                            redoAction = new CAction(ActionID.DeleteLayer, layer);
                        }
                    }
                    break;

                case ActionID.RenameGroup:
                    {
                        if (_subject is Group)
                        {
                            Group g = _subject as Group;
                            string name = _predicate as string;
                            if (name != null)
                            {
                                redoAction = new CAction(ActionID.RenameGroup, g, g.Name);
                                string newName = Globals.ActiveDrawing.UniqueGroupName(name);
                                Globals.ActiveDrawing.RenameGroup(g, newName);
                            }
                        }
                    }
                    break;

                case ActionID.MoveGroupOrigin:
                    {
                        PInstance pi = _subject as PInstance;
                        if (pi != null)
                        {
                            Group g = Globals.ActiveDrawing.GetGroup(pi.GroupName);
                            if (g != null)
                            {
                                Point delta = (Point)_predicate;
                                redoAction = new CAction(ActionID.MoveGroupOrigin, pi, new Point(-delta.X, -delta.Y));
                                g.MoveOriginBy(delta.X, delta.Y);

                                Point d = pi.Matrix.Transform(delta);
                                pi.MoveByDelta(-d.X, -d.Y);
                                pi.Draw();
                            }
                        }
                    }
                    break;

                case ActionID.MoveGroupEntry:
                    {
                        Group g = _subject as Group;
                        if (g != null)
                        {
                            redoAction = new CAction(ActionID.MoveGroupEntry, g, g.Entry);
                            g.Entry = (Point)_predicate;
                        }
                    }
                    break;

                case ActionID.MoveGroupExit:
                    {
                        Group g = _subject as Group;
                        if (g != null)
                        {
                            redoAction = new CAction(ActionID.MoveGroupExit, g, g.Exit);
                            g.Exit = (Point)_predicate;
                        }
                    }
                    break;

                case ActionID.ReplaceImage:
                    {
                        PImage image = _subject as PImage;
                        if (image != null)
                        {
                            PImage clone = new PImage(image);
                            redoAction = new CAction(ActionID.ReplaceImage, image, clone);
                            clone = (PImage)_predicate;
                            if (clone != null)
                            {
                                image.C1 = clone.C1;
                                image.C2 = clone.C2;
                                image.ImageId = clone.ImageId;
                                image.SourceName = clone.SourceName;
                                image.Opacity = clone.Opacity;
                                image.RefP1 = clone.RefP1;
                                image.RefP2 = clone.RefP2;
                                image.Draw();
                                image.ClearStaticConstructNodes();
                            }
                        }
                    }
                    break;

                case ActionID.CommandInternal:
                    redoAction = this;
                    break;
            }

            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.UndoNotification(_actionId, _subject, _predicate, _predicate2);
            }

            return redoAction;
        }

        public CAction RedoExecute()
        {
            CAction undoAction = null;

            //System.Diagnostics.Debug.WriteLine("Action.Execute(): {0}", _actionId);
            switch (_actionId)
            {
                case ActionID.MultiUndo:
                    {
                        int count = (int)_subject;
                        for (int i = 0; i < count; i++)
                        {
                            Globals.CommandDispatcher.Redo();
                        }
                    }
                    undoAction = this;
                    break;

                case ActionID.Move:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.Move, p, p.Origin);

                            Point xy = (Point)_predicate;
                            p.MoveTo(xy.X, xy.Y);
                        }
                    }
                    break;


                case ActionID.DeleteGroupMember:
                    {
                        if (_subject is Group && _predicate is Primitive && _predicate2 is int)
                        {
                            Group g = _subject as Group;
                            Primitive member = _predicate as Primitive;
                            int memberIndex = (int)_predicate2;
                            g.RemoveMemberAt(memberIndex);
                            undoAction = new CAction(ActionID.AddGroupMember, g, member, memberIndex);
                        }
                        else if (_subject is PInstance && _predicate is Primitive && _predicate2 is int)
                        {
                            Group g = Globals.ActiveDrawing.GetGroup(((PInstance)_subject).GroupName);
                            Primitive member = _predicate as Primitive;
                            int memberIndex = (int)_predicate2;
                            g.RemoveMemberAt(memberIndex);
                            undoAction = new CAction(ActionID.AddGroupMember, g, member, memberIndex);
                        }
                    }
                    break;

                case ActionID.AddGroupMember:
                    {
                        if (_subject is Group && _predicate is Primitive && _predicate2 is int)
                        {
                            Group g = _subject as Group;
                            Primitive member = _predicate as Primitive;
                            int memberIndex = (int)_predicate2;
                            g.AddMemberAt(member, memberIndex);
                            undoAction = new CAction(ActionID.DeleteGroupMember, g, member, memberIndex);
                        }
                    }
                    break;

                case ActionID.MoveGroupMember:
                    {
                        Group g = _subject as Group;
                        if (g != null && _predicate is int && _predicate2 is Point)
                        {
                            int index = (int)_predicate;
                            if (index >= 0 && index < g.Items.Count)
                            {
                                Primitive member = g.Items[index];
                                undoAction = new CAction(ActionID.MoveGroupMember, g, index, member.Origin);

                                Point xy = (Point)_predicate2;
                                g.MoveMemberAt(index, xy.X - member.Origin.X, xy.Y - member.Origin.Y);
                            }
                        }
                    }
                    break;

                case ActionID.DeletePrimitive:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            Globals.ActiveDrawing.DeletePrimitive(p);
                            undoAction = new CAction(ActionID.RestorePrimitive, p);
                        }
                    }
                    break;

                case ActionID.RestorePrimitive:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            Globals.ActiveDrawing.RestoreDeletedPrimitive(p);
                            undoAction = new CAction(ActionID.DeletePrimitive, p);
                        }
                    }
                    break;

                case ActionID.RestoreMatrix:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.RestoreMatrix, p, p.Origin, p.Matrix);

                            Point xy = (Point)_predicate;
                            p.SetTransform(xy.X, xy.Y, (Matrix)_predicate2);
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.UnNormalize:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.Normalize, p);

                            p.UnNormalize((Matrix)_predicate);
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.Normalize:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.UnNormalize, p, p.Matrix);

                            p.Normalize(false);
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.RestorePoints:
                    {
                        if (_subject is PLine)
                        {
                            PLine p = _subject as PLine;

                            undoAction = new CAction(ActionID.RestorePoints, p, p.CPoints);

                            p.CPoints = (List<CPoint>)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.MoveVertex:
                    {
                        if (_subject is Primitive)
                        {
                            Primitive p = _subject as Primitive;
                            int handleId = (int)_predicate;

                            CPoint cp = p.GetHandlePoint(handleId);
                            undoAction = new CAction(ActionID.MoveVertex, p, handleId, cp.Point);

                            Point p0 = (Point)_predicate2;
                            p.MoveHandlePoint(handleId, p0);
                        }
                    }
                    break;

                case ActionID.SetLayer:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetLayer, p, p.LayerId);
                            p.LayerId = (int)_predicate;
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetColor:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetColor, p, p.ColorSpec);
                            p.ColorSpec = (uint)_predicate;
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetFill:
                    {
                        if (_subject is Primitive)
                        {
                            undoAction = new CAction(ActionID.SetFill, _subject, ((Primitive)_subject).Fill);
                            ((Primitive)_subject).Fill = (uint)_predicate;
                            ((Primitive)_subject).Draw();

                            Globals.Events.PrimitiveSelectionPropertyChanged(_subject as Primitive);
                        }
                    }
                    break;

                case ActionID.SetLineWeight:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetLineWeight, p, p.LineWeightId);
                            p.LineWeightId = (int)_predicate;
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetLineType:
                    {
                        Primitive p = _subject as Primitive;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetLineType, p, p.LineTypeId);
                            p.LineTypeId = (int)_predicate;
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetOpacity:
                    {
                        if (_subject is PImage)
                        {
                            PImage pi = _subject as PImage;
                            undoAction = new CAction(ActionID.SetOpacity, pi, pi.Opacity);
                            pi.Opacity = (double)_predicate;
                            Globals.Events.PrimitiveSelectionPropertyChanged(pi);
                        }
                    }
                    break;

                case ActionID.SetWidth:
                    {
                        if (_subject is PRectangle)
                        {
                            PRectangle p = _subject as PRectangle;
                            if (p != null)
                            {
                                undoAction = new CAction(ActionID.SetWidth, p, p.Width);
                                p.Width = (double)_predicate;
                                p.Draw();
                            }
                        }
                        else if (_subject is PDoubleline)
                        {
                            PDoubleline p = _subject as PDoubleline;
                            if (p != null)
                            {
                                undoAction = new CAction(ActionID.SetWidth, p, p.Width);
                                p.Width = (double)_predicate;
                                p.Draw();
                            }
                        }
                        else if (_subject is PEllipse)
                        {
                            PEllipse p = _subject as PEllipse;
                            if (p != null)
                            {
                                undoAction = new CAction(ActionID.SetWidth, p, p.Major);
                                p.Major = (double)_predicate;
                                p.Draw();
                            }
                        }
                        //else if (_subject is PImage)
                        //{
                        //    PImage p = _subject as PImage;
                        //    if (p != null)
                        //    {
                        //        undoAction = new CAction(ActionID.SetWidth, p, p.Width);
                        //        p.Width = (double)_predicate;
                        //        p.Draw();
                        //    }
                        //}
                        else
                        {
                            break;
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetHeight:
                    {
                        if (_subject is PRectangle)
                        {
                            PRectangle p = _subject as PRectangle;
                            if (p != null)
                            {
                                undoAction = new CAction(ActionID.SetHeight, p, p.Height);
                                p.Height = (double)_predicate;
                                p.Draw();
                            }
                        }
                        else if (_subject is PEllipse)
                        {
                            PEllipse p = _subject as PEllipse;
                            if (p != null)
                            {
                                undoAction = new CAction(ActionID.SetHeight, p, p.Minor);
                                p.Minor = (double)_predicate;
                                p.Draw();
                            }
                        }
                        else if (_subject is PText)
                        {
                            PText p = _subject as PText;
                            if (p != null)
                            {
                                undoAction = new CAction(ActionID.SetHeight, p, p.Size);
                                p.Size = (double)_predicate;
                                p.Draw();
                            }
                        }
                        //else if (_subject is PImage)
                        //{
                        //    PImage p = _subject as PImage;
                        //    if (p != null)
                        //    {
                        //        undoAction = new CAction(ActionID.SetHeight, p, p.Height);
                        //        p.Height = (double)_predicate;
                        //        p.Draw();
                        //    }
                        //}
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetRadius:
                    {
                        if (_subject is PArc)
                        {
                            PArc p = _subject as PArc;
                            undoAction = new CAction(ActionID.SetRadius, p, p.Radius);
                            p.Radius = (double)_predicate;
                            p.Draw();
                        }
                        else if (_subject is PLine)
                        {
                            PLine p = _subject as PLine;
                            undoAction = new CAction(ActionID.SetRadius, p, p.Radius);
                            p.Radius = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetAngle:
                    {
                        if (_subject is PEllipse)
                        {
                            PEllipse p = _subject as PEllipse;
                            undoAction = new CAction(ActionID.SetAngle, p, p.AxisAngle);
                            p.AxisAngle = (double)_predicate;
                            p.Draw();
                        }
                        else if (_subject is PText)
                        {
                            PText p = _subject as PText;
                            undoAction = new CAction(ActionID.SetAngle, p, p.Angle);
                            p.Angle = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetSpacing:
                    {
                        if (_subject is PText)
                        {
                            PText p = _subject as PText;
                            undoAction = new CAction(ActionID.SetSpacing, p, p.CharacterSpacing);
                            p.CharacterSpacing = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetLineSpacing:
                    {
                        if (_subject is PText)
                        {
                            PText p = _subject as PText;
                            undoAction = new CAction(ActionID.SetLineSpacing, p, p.LineSpacing);
                            p.LineSpacing = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetStartAngle:
                    {
                        if (_subject is PArc)
                        {
                            PArc p = _subject as PArc;
                            undoAction = new CAction(ActionID.SetStartAngle, p, p.StartAngle);
                            p.StartAngle = (double)_predicate;
                            p.Draw();
                        }
                        else if (_subject is PEllipse)
                        {
                            PEllipse p = _subject as PEllipse;
                            undoAction = new CAction(ActionID.SetStartAngle, p, p.StartAngle);
                            p.StartAngle = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetIncludedAngle:
                    {
                        if (_subject is PArc)
                        {
                            PArc p = _subject as PArc;
                            undoAction = new CAction(ActionID.SetIncludedAngle, p, p.IncludedAngle);
                            p.IncludedAngle = (double)_predicate;
                            p.Draw();
                        }
                        else if (_subject is PEllipse)
                        {
                            PEllipse p = _subject as PEllipse;
                            undoAction = new CAction(ActionID.SetIncludedAngle, p, p.IncludedAngle);
                            p.IncludedAngle = (double)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionSizeChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetIsCircle:
                    {
                        PArc3 p = _subject as PArc3;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetIsCircle, p, p.IsCircle);
                            p.IsCircle = (bool)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetFillRule:
                    {
                        PPolygon p = _subject as PPolygon;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetFillRule, p, p.FillEvenOdd);
                            p.FillEvenOdd = (bool)_predicate;
                            p.Draw();
                        }
                    }
                    break;


                case ActionID.SetPattern:
                    {
                        PPolygon p = _subject as PPolygon;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetPattern, p, p.FillPattern);
                            p.FillPattern = (string)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.SetPatternScale:
                    {
                        PPolygon p = _subject as PPolygon;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetPatternScale, p, p.PatternScale);
                            p.PatternScale = (double)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.SetPatternAngle:
                    {
                        PPolygon p = _subject as PPolygon;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetPatternAngle, p, p.PatternAngle);
                            p.PatternAngle = (double)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.SetTextStyle:
                    {
                        if (_subject is PText)
                        {
                            PText p = _subject as PText;
                            undoAction = new CAction(ActionID.SetTextStyle, p, p.TextStyleId);
                            p.TextStyleId = (int)_predicate;
                            p.Draw();
                            Globals.Events.PrimitiveSelectionPropertyChanged(p);
                        }
                        else if (_subject is PDimension)
                        {
                            PDimension p = _subject as PDimension;
                            undoAction = new CAction(ActionID.SetTextStyle, p, p.TextStyleId);
                            p.TextStyleId = (int)_predicate;
                            p.Draw();
                            Globals.Events.PrimitiveSelectionPropertyChanged(p);
                        }
                    }
                    break;

                case ActionID.SetAlignment:
                    {
                        PText p = _subject as PText;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetAlignment, p, p.Alignment);
                            p.Alignment = (TextAlignment)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetPosition:
                    {
                        PText p = _subject as PText;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetPosition, p, p.Position);
                            p.Position = (TextPosition)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.EditText:
                    {
                        PText p = _subject as PText;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.EditText, p, p.Text);
                            p.Text = (string)_predicate;
                            //p.TextStyleChanged();
                            p.Draw();
                            Globals.Events.PrimitiveSelectionPropertyChanged(p);
                        }
                    }
                    break;

                case ActionID.EditAttribute:
                    {
                        PInstance p = _subject as PInstance;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.EditAttribute, p, p.CloneAttributeList());
                            p.AttributeList = (List<GroupAttribute>)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.Flip:
                    {
                        PInstance p = _subject as PInstance;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.Flip, p, p.Flip);
                            p.Flip = (uint)_predicate;
                            p.Draw();
                        }
                    }
                    break;

                case ActionID.SetEndStyle:
                    {
                        PDoubleline p = _subject as PDoubleline;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetEndStyle, p, p.EndStyle);
                            p.EndStyle = (DbEndStyle)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetDimensionType:
                    {
                        PDimension p = _subject as PDimension;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetDimensionType, p, p.DimensionType);
                            p.DimensionType = (PDimension.DimType)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.ShowDimensionText:
                    {
                        PDimension p = _subject as PDimension;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.ShowDimensionText, p, p.ShowText);
                            p.ShowText = (bool)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.ShowDimensionExtension:
                    {
                        PDimension p = _subject as PDimension;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.ShowDimensionExtension, p, p.ShowExtension);
                            p.ShowExtension = (bool)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetArrowStyle:
                    {
                        if (_subject is PArrow)
                        {
                            PArrow p = _subject as PArrow;
                            if (p != null)
                            {
                                undoAction = new CAction(ActionID.SetArrowStyle, p, p.ArrowStyleId);
                                p.ArrowStyleId = (int)_predicate;
                                p.Draw();
                            }
                        }
                        else if (_subject is PDimension)
                        {
                            PDimension p = _subject as PDimension;
                            if (p != null)
                            {
                                undoAction = new CAction(ActionID.SetArrowStyle, p, p.ArrowStyleId);
                                p.ArrowStyleId = (int)_predicate;
                                p.Draw();
                            }
                        }

                        Globals.Events.PrimitiveSelectionPropertyChanged(_subject as Primitive);
                    }
                    break;

                case ActionID.SetArrowLocation:
                    {
                        PArrow p = _subject as PArrow;
                        if (p != null)
                        {
                            undoAction = new CAction(ActionID.SetArrowLocation, p, p.ArrowLocation);
                            p.ArrowLocation = (ArrowLocation)_predicate;
                            p.Draw();
                        }
                        Globals.Events.PrimitiveSelectionPropertyChanged(p);
                    }
                    break;

                case ActionID.SetLineTypeDefinition:
                    {
                        int id = (int)_subject;
                        if (Globals.LineTypeTable.ContainsKey(id))
                        {
                            undoAction = new CAction(ActionID.SetLineTypeDefinition, id, Globals.LineTypeTable[id]);
                            Globals.LineTypeTable[id] = (LineType)_predicate;
                            LineType.PropagateLineTypeChanges(id);
                        }
                    }
                    break;

                case ActionID.SetLayerDefinition:
                    {
                        int id = (int)_subject;
                        if (Globals.LayerTable.ContainsKey(id))
                        {
                            undoAction = new CAction(ActionID.SetLayerDefinition, id, Globals.LayerTable[id]);
                            Globals.LayerTable[id] = (Layer)_predicate;
                            Layer.PropagateLayerChanges(id);
                        }
                    }
                    break;

                case ActionID.SetTextStyleDefinition:
                    {
                        int id = (int)_subject;
                        if (Globals.TextStyleTable.ContainsKey(id))
                        {
                            undoAction = new CAction(ActionID.SetTextStyleDefinition, id, Globals.TextStyleTable[id]);
                            Globals.TextStyleTable[id] = (TextStyle)_predicate;
                            TextStyle.PropagateTextStyleChanges(id);
                        }
                    }
                    break;

                case ActionID.SetArrowStyleDefinition:
                    {
                        int id = (int)_subject;
                        if (Globals.ArrowStyleTable.ContainsKey(id))
                        {
                            undoAction = new CAction(ActionID.SetArrowStyleDefinition, id, Globals.ArrowStyleTable[id]);
                            Globals.ArrowStyleTable[id] = (ArrowStyle)_predicate;
                            ArrowStyle.PropagateArrowStyleChanges(id);
                        }
                    }
                    break;

                case ActionID.DeleteLayer:
                    {
                        Layer layer = _subject as Layer;
                        if (layer != null)
                        {
                            Globals.ActiveDrawing.DeleteLayer(layer.Id);
                            undoAction = new CAction(ActionID.RestoreLayer, layer);
                        }
                    }
                    break;

                case ActionID.RestoreLayer:
                    {
                        Layer layer = _subject as Layer;
                        if (layer != null)
                        {
                            Globals.ActiveDrawing.AddLayer(layer);
                            undoAction = new CAction(ActionID.DeleteLayer, layer);
                        }
                    }
                    break;

                case ActionID.RenameGroup:
                    {
                        if (_subject is Group)
                        {
                            Group g = _subject as Group;
                            string name = _predicate as string;
                            if (name != null)
                            {
                                undoAction = new CAction(ActionID.RenameGroup, g, g.Name);
                                string newName = Globals.ActiveDrawing.UniqueGroupName(name);
                                Globals.ActiveDrawing.RenameGroup(g, newName);
                            }
                        }
                    }
                    break;

                case ActionID.MoveGroupOrigin:
                    {
                        PInstance pi = _subject as PInstance;
                        if (pi != null)
                        {
                            Group g = Globals.ActiveDrawing.GetGroup(pi.GroupName);
                            if (g != null)
                            {
                                Point delta = (Point)_predicate;
                                undoAction = new CAction(ActionID.MoveGroupOrigin, pi, new Point(-delta.X, -delta.Y));
                                g.MoveOriginBy(delta.X, delta.Y);

                                Point d = pi.Matrix.Transform(delta);
                                pi.MoveByDelta(-d.X, -d.Y);
                                pi.Draw();
                            }
                        }
                    }
                    break;

                case ActionID.MoveGroupEntry:
                    {
                        Group g = _subject as Group;
                        if (g != null)
                        {
                            undoAction = new CAction(ActionID.MoveGroupEntry, g, g.Entry);
                            g.Entry = (Point)_predicate;
                        }
                    }
                    break;

                case ActionID.MoveGroupExit:
                    {
                        Group g = _subject as Group;
                        if (g != null)
                        {
                            undoAction = new CAction(ActionID.MoveGroupExit, g, g.Exit);
                            g.Exit = (Point)_predicate;
                        }
                    }
                    break;

                case ActionID.ReplaceImage:
                    {
                        PImage image = _subject as PImage;
                        if (image != null)
                        {
                            PImage clone = new PImage(image);
                            undoAction = new CAction(ActionID.ReplaceImage, image, clone);
                            clone = (PImage)_predicate;
                            if (clone != null)
                            {
                                image.C1 = clone.C1;
                                image.C2 = clone.C2;
                                image.ImageId = clone.ImageId;
                                image.SourceName = clone.SourceName;
                                image.Opacity = clone.Opacity;
                                image.RefP1 = clone.RefP1;
                                image.RefP2 = clone.RefP2;
                                image.Draw();
                                image.ClearStaticConstructNodes();
                            }
                        }
                    }
                    break;

                case ActionID.CommandInternal:
                    undoAction = this;
                    break;
            }

            if (Globals.CommandProcessor != null)
            {
                Globals.CommandProcessor.RedoNotification(_actionId, _subject, _predicate, _predicate2);
            }

            return undoAction;
        }
    }

    public enum ActionID
    {
        MultiUndo,
        SetLayer,
        SetColor,
        SetFill,
        SetLineWeight,
        SetLineType,
        SetOpacity,
        SetWidth,
        SetHeight,
        SetRadius,
        SetAngle,
        SetStartAngle,
        SetIncludedAngle,
        SetTextStyle,
        SetPosition,
        SetAlignment,
        SetSpacing,
        SetLineSpacing,
        SetArrowStyle,
        SetArrowLocation,
        SetEndStyle,
        SetDimensionType,
        SetIsCircle,
        ShowDimensionText,
        ShowDimensionExtension,
        Move,
        MoveVertex,
        DeletePrimitive,
        RestorePrimitive,
        RestorePoints,
        RestoreMatrix,
        UnNormalize,
        Normalize,
        Flip,
        EditText,
        EditAttribute,
        SetLayerDefinition,
        SetLineTypeDefinition,
        SetTextStyleDefinition,
        SetArrowStyleDefinition,
        DeleteLayer,
        RestoreLayer,
        RenameGroup,
        MoveGroupOrigin,
        MoveGroupEntry,
        MoveGroupExit,
        AddGroupMember,
        DeleteGroupMember,
        MoveGroupMember,
        ReplaceImage,
        CommandInternal,
        SetFillRule,
        SetPattern,
        SetPatternScale,
        SetPatternAngle
    }
}
