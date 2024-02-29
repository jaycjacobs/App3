#define sharpdx3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using SharpDX.Direct2D1;
#if sharpdx3
using SharpDX.Mathematics.Interop;
#else
using RawVector2 = SharpDX.Vector2;
#endif

namespace Cirros.Utility
{
    public class Tesselator : SharpDX.Direct2D1.TessellationSink
    {
        SharpDX.Direct2D1.Factory _factory2D = new SharpDX.Direct2D1.Factory();
        List<Windows.Foundation.Point[]> _triangles = new List<Windows.Foundation.Point[]>();

        public Tesselator()
        {
        }

        public List<Windows.Foundation.Point[]> TesselateEllipse(Windows.Foundation.Point center, double major, double minor)
        {
            _triangles.Clear();

            RawVector2 c = new RawVector2((float)center.X, (float)center.Y);
            float radiusx = (float)major / 2;
            float radiusy = (float)minor / 2;
            SharpDX.Direct2D1.Ellipse ellipse = new SharpDX.Direct2D1.Ellipse(c, radiusx, radiusy);
            SharpDX.Direct2D1.EllipseGeometry Ellipse = new SharpDX.Direct2D1.EllipseGeometry(_factory2D, ellipse);

            Ellipse.Tessellate(1, this);

            return _triangles;
        }

        public List<Windows.Foundation.Point[]> TesselatePolygon(List<Point> points, bool fillEvenOdd)
        {
            _triangles.Clear();

            SharpDX.Direct2D1.PathGeometry pathGeometry = new SharpDX.Direct2D1.PathGeometry(_factory2D);
            var pathSink = pathGeometry.Open();
            pathSink.SetFillMode(fillEvenOdd ? FillMode.Alternate : FillMode.Winding);

            RawVector2 startingPoint = new RawVector2((float)points[0].X, (float)points[0].Y);

            pathSink.BeginFigure(startingPoint, FigureBegin.Filled);

            foreach (Windows.Foundation.Point p in points)
            {
                pathSink.AddLine(new RawVector2((float)p.X, (float)p.Y));
            }

            pathSink.EndFigure(FigureEnd.Closed);
            pathSink.Close();
            pathSink.Dispose();

            pathGeometry.Tessellate(1, this);

            pathGeometry.Dispose();

            return _triangles;
        }

        public void AddTriangles(SharpDX.Direct2D1.Triangle[] triangles)
        {
            foreach (SharpDX.Direct2D1.Triangle t in triangles)
            {
                Windows.Foundation.Point[] pa = new Windows.Foundation.Point[3];

                pa[0].X = t.Point1.X;
                pa[0].Y = t.Point1.Y;
                pa[1].X = t.Point2.X;
                pa[1].Y = t.Point2.Y;
                pa[2].X = t.Point3.X;
                pa[2].Y = t.Point3.Y;

                _triangles.Add(pa);
            }
        }

        public void Close()
        {
        }

        public IDisposable Shadow
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public void Dispose()
        {
        }

        public Result QueryInterface(ref Guid guid, out IntPtr comObject)
        {
            throw new NotImplementedException();
        }

        public int AddReference()
        {
            throw new NotImplementedException();
        }

        public int Release()
        {
            throw new NotImplementedException();
        }
    }
}
