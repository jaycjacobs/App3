using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls;
using Cirros;
using Cirros.Primitives;
using System.Text;
using Cirros.Utility;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace CirrosCore.Entity_Explorer
{
    public sealed partial class EntityExplorer : ContentDialog
    {
        //private StringBuilder _stringBuilder = new StringBuilder();
        private string _indent = "";
        private int _indentCount = 0;

        public EntityExplorer()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ShowEntity(string identifier)
        {
            _listView.Items.Clear();

            if (uint.TryParse(identifier, out uint eId))
            {
                Primitive p = Globals.ActiveDrawing.FindObjectById(eId);
                if (p != null)
                {
                    _listView.Items.Add($"{_indent}Entity {eId}");
                    AddEntityNodes(p);
                }
                else
                {
                    _listView.Items.Add($"{_indent}Entity not found");
                }
            }
            else
            {
                Group g = Globals.ActiveDrawing.GetGroup(identifier);
                if (g != null)
                {
                    _listView.Items.Add($"{_indent}Group");
                    AddGroupNodes(g);
                }
                else
                {
                    _listView.Items.Add($"{_indent}Group not found");
                }
            }
        }

        private void ShowEntity()
        {
            ShowEntity(_entityIdBox.Text);
        }

        private void _entityIdButton_Click(object sender, RoutedEventArgs e)
        {
            ShowEntity();
        }

        private void AddIndent(int delta)
        {
            _indentCount += delta;
            if (_indentCount >= 0)
            {
                _indent = "               ".Substring(0, _indentCount);
            }
            else
            {
                _indentCount = 0;
                _indent = "";
            }
        }

        void AddGroupNodes(Group g)
        {
            AddIndent(2);

            _listView.Items.Add(KeyStringField("Name", g.Name));
            _listView.Items.Add(KeyStringField("Folder", g.Folder));
            _listView.Items.Add(KeyStringField("PaperUnit", g.PaperUnit.ToString()));
            _listView.Items.Add(KeyStringField("ModelUnit", g.ModelUnit.ToString()));
            _listView.Items.Add(KeyUIntHexField("Flags", g.Flags));
            _listView.Items.Add(KeyStringField("PreferPaperSpace", g.PreferPaperSpace.ToString()));
            _listView.Items.Add(KeyDoubleField("PreferredScale", g.PreferredScale));
            _listView.Items.Add(KeyStringField("CoordinateSpace", g.CoordinateSpace.ToString()));
            _listView.Items.Add(KeyStringField("Description", g.Description));
            _listView.Items.Add(KeyStringField("Id", g.Id.ToString()));
            _listView.Items.Add(KeyStringField("InsertLocation", g.InsertLocation.ToString()));
            _listView.Items.Add(KeyPointField("Entry", g.Entry));
            _listView.Items.Add(KeyPointField("Exit", g.Exit));
            _listView.Items.Add(KeyRectField("PaperBounds", g.PaperBounds));
            _listView.Items.Add(KeyRectField("ModelBounds", g.ModelBounds));

            int i = 1;

            foreach (Primitive p in g.Items)
            {
                _listView.Items.Add($"{_indent}Member {i++}");

                AddEntityNodes(p);
            }

            AddIndent(-2);
        }

        void AddEntityNodes(Primitive p)
        {
            AddIndent(2);

            _listView.Items.Add(KeyStringField("TypeName", p.TypeName.ToString()));
            _listView.Items.Add(KeyPointField("Origin", p.Origin));
            _listView.Items.Add(KeyIntField("LayerId", p.LayerId));
            _listView.Items.Add(KeyIntField("LineTypeId", p.LineTypeId));
            _listView.Items.Add(KeyIntField("LineWeightId", p.LineWeightId));
            _listView.Items.Add(KeyUIntHexField("ColorSpec", p.ColorSpec));
            _listView.Items.Add(KeyUIntHexField("Fill", p.Fill));
            _listView.Items.Add(KeyDoubleField("Opacity", p.Opacity));
            _listView.Items.Add(KeyStringField("FillEvenOdd", p.FillEvenOdd.ToString()));

            if (string.IsNullOrEmpty(p.FillPattern) == false)
            {
                _listView.Items.Add(KeyStringField("FillPattern", p.FillPattern));
                _listView.Items.Add(KeyDoubleField("PatternScale", p.PatternScale));
                _listView.Items.Add(KeyDoubleField("PatternAngle", p.PatternAngle));
            }

            _listView.Items.Add(KeyRectField("Box", p.Box));
            _listView.Items.Add(KeyMatrixField("Matrix", p.Matrix));

            switch (p.TypeName)
            {
                case PrimitiveType.Arc:
                    AddArcFields(p as PArc);
                    break;
                case PrimitiveType.Arc3:
                    AddArc3Fields(p as PArc3);
                    break;
                case PrimitiveType.Arrow:
                    AddArrowFields(p as PArrow);
                    break;
                case PrimitiveType.BSpline:
                    AddBSplineFields(p as PBSpline);
                    break;
                case PrimitiveType.Dimension:
                    AddDimensionField(p as PDimension);
                    break;
                case PrimitiveType.Doubleline:
                    AddDoublelineField(p as PDoubleline);
                    break;
                case PrimitiveType.Ellipse:
                    AddEllipseField(p as PEllipse);
                    break;
                case PrimitiveType.Image:
                    AddImageField(p as PImage);
                    break;
                case PrimitiveType.Instance:
                    AddInstanceFields(p as PInstance);
                    break;
                case PrimitiveType.Line:
                case PrimitiveType.Polygon:
                    AddLineFields(p as PLine);
                    break;
                //case PrimitiveType.Polygon:
                //    AddPolygonField(p as PPolygon);
                //    break;
                case PrimitiveType.Rectangle:
                    AddRectangleField(p as PRectangle);
                    break;
                case PrimitiveType.Text:
                    AddTextField(p as PText);
                    break;
                default:
                    break;
            }

            AddIndent(-2);
        }

        void AddLineFields(PLine pline)
        {
            _listView.Items.Add($"{_indent}Points");
            int index = 0;

            AddIndent(2);

            foreach (CPoint p in pline.CPoints)
            {
                _listView.Items.Add(KeyCPointField($"[{index++}]", p));
            }

            AddIndent(-2);
        }

        void AddArcFields(PArc parc)
        {
            _listView.Items.Add(KeyDoubleField("Radius", parc.Radius));
            _listView.Items.Add(KeyDoubleField("StartAngle", parc.StartAngle * Construct.cRadiansToDegrees));
            _listView.Items.Add(KeyDoubleField("IncludedAngle", parc.IncludedAngle * Construct.cRadiansToDegrees));
        }

        void AddArc3Fields(PArc3 parc3)
        {
            _listView.Items.Add(KeyPointField("P1", parc3.Points[0]));
            _listView.Items.Add(KeyPointField("P2", parc3.Points[1]));
        }

        private void AddTextField(PText pText)
        {
            _listView.Items.Add(KeyIntField("TextStyleId", pText.TextStyleId));
            _listView.Items.Add(KeyStringField("Alignment", pText.Alignment.ToString()));
            _listView.Items.Add(KeyStringField("Position", pText.Position.ToString()));
            _listView.Items.Add(KeyPointField("P1", pText.P1));
            _listView.Items.Add(KeyPointField("P2", pText.P2));
            _listView.Items.Add(KeyDoubleField("Size", pText.Size));
            _listView.Items.Add(KeyDoubleField("CharacterSpacing", pText.CharacterSpacing));
            _listView.Items.Add(KeyDoubleField("LineSpacing", pText.LineSpacing));
            _listView.Items.Add(KeyDoubleField("Angle", pText.Angle));

            string[] lines = pText.Text.Split(new[] { '\n' });
            _listView.Items.Add(KeyStringField("Text", lines[0]));

            if (lines.Length > 0)
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    _listView.Items.Add(KeyStringField("", lines[i]));
                }
            }
        }

        private void AddRectangleField(PRectangle pRectangle)
        {
            _listView.Items.Add(KeyDoubleField("Width", pRectangle.Width));
            _listView.Items.Add(KeyDoubleField("Height", pRectangle.Height));
        }

        private void AddPolygonField(PPolygon pPolygon)
        {
        }

        private void AddImageField(PImage pImage)
        {
        }

        private void AddEllipseField(PEllipse pEllipse)
        {
        }

        private void AddDoublelineField(PDoubleline pDoubleline)
        {
        }

        private void AddDimensionField(PDimension pDimension)
        {
        }

        private void AddBSplineFields(PBSpline pBSpline)
        {
        }

        private void AddArrowFields(PArrow pArrow)
        {
        }

        private void AddInstanceFields(PInstance pInstance)
        {
            _listView.Items.Add(KeyStringField("GroupName", pInstance.GroupName));
        }

        string KeyStringField(string key, string s)
        {
            return $"{_indent}{key,-18} {s}";
        }

        string KeyDoubleField(string key, double d)
        {
            return $"{_indent}{key,-18} {d:F4}";
        }

        string KeyPointField(string key, Point p)
        {
            return $"{_indent}{key,-18} ({p.X:F4}, {p.Y:F4})";
        }

        string KeyCPointField(string key, CPoint p)
        {
            return $"{_indent}{key,-18} ({p.X:F4}, {p.Y:F4}, {p.M})";
        }

        string KeyRectField(string key, Rect r)
        {
            return $"{_indent}{key,-18} ({r.X:F4}, {r.Y:F4}), Width={r.Width:F4}, Height={r.Height:F4}";
        }

        string KeyMatrixField(string key, Matrix m)
        {
            string s = string.Format("{0:F8},{1:F8},{2:F8},{3:F8},{4:F8},{5:F8}", m.M11, m.M12, m.M21, m.M22, m.OffsetX, m.OffsetY);
            return $"{_indent}{key,-18} {s}";
            //return $"{_indent}{key,-18} {m.ToString()}";
        }

        string KeyUIntHexField(string key, uint u)
        {
            return $"{_indent}{key,-18} {u:x}";
        }

        string KeyIntField(string key, int i)
        {
            return $"{_indent}{key,-18} {i}";
        }

        private void _entityIdBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ShowEntity();
            }
        }

        private void _listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                if (e.AddedItems[0] is string s)
                {
                    if (s.Length >= 20 && s.Substring(0,20).Trim() == "GroupName")
                    {
                        string name = s.Substring(20).Trim();
                        ShowEntity(name);
                    }
                }
            }
        }
    }
}
