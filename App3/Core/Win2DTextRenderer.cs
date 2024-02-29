using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Cirros.Core.Display
{
    internal class Win2DTextRenderer : ICanvasTextRenderer
    {
        CanvasDrawingSession _ds = null;
        CanvasSolidColorBrush _brush;
        float _space = 1;

        public Win2DTextRenderer(CanvasDrawingSession ds, CanvasSolidColorBrush brush, float space)
        {
            _ds = ds;
            _brush = brush;
            _space = space;
        }

        public void DrawGlyphRun(Vector2 point, CanvasFontFace fontFace, float fontSize, CanvasGlyph[] glyphs, 
            bool isSideways, uint bidiLevel, object brush, CanvasTextMeasuringMode measuringMode, string localeName, 
            string textString, int[] clusterMapIndices, uint characterIndex, CanvasGlyphOrientation glyphOrientation)
        {
            if (glyphs != null)
            {
                int count = glyphs.Count<CanvasGlyph>();

                if (count > 0)
                {
#if false
                    // more accurate method
                    float tw = 0;
                    for (int i = 1; i < count; i++)
                    {
                        tw += glyphs[i].Advance;
                    }

                    float nominalWidth = tw / count;
#else
                    // less accurate method that matches print and export results
                    float nominalWidth = fontSize * .8f;
#endif
                    float space = nominalWidth * (_space - 1);

                    for (int i = 1; i < count; i++)
                    {
                        glyphs[i].AdvanceOffset = space * i;
                    }
                }

                _ds.DrawGlyphRun(
                    point,
                    fontFace,
                    fontSize,
                    glyphs,
                    isSideways,
                    bidiLevel,
                    _brush);
            }
        }

        public void DrawStrikethrough(Vector2 point, float strikethroughWidth, float strikethroughThickness, float strikethroughOffset, CanvasTextDirection textDirection, object brush, CanvasTextMeasuringMode textMeasuringMode, string localeName, CanvasGlyphOrientation glyphOrientation)
        {
        }

        public void DrawUnderline(Vector2 point, float underlineWidth, float underlineThickness, float underlineOffset, float runHeight, CanvasTextDirection textDirection, object brush, CanvasTextMeasuringMode textMeasuringMode, string localeName, CanvasGlyphOrientation glyphOrientation)
        {
        }

        public void DrawInlineObject(Vector2 point, ICanvasTextInlineObject inlineObject, bool isSideways, bool isRightToLeft, object brush, CanvasGlyphOrientation glyphOrientation)
        {
        }

        public float Dpi { get { return 96; } }

        public bool PixelSnappingDisabled { get { return true; } }

        public Matrix3x2 Transform { get { return System.Numerics.Matrix3x2.Identity; } }
    }
}
