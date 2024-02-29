using Cirros.Core;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Cirros.Drawing
{
    //public partial class WorkCanvas : Canvas
    //{
    //    private bool _cursorAcquired = false;

    //    private Canvas _cursor;
    //    private ScaleTransform _cursorScaleTransform = new ScaleTransform();
    //    private Line _crossCursorV;
    //    private Line _crossCursorH;
    //    private Canvas _panCursor;
    //    private CursorType _cursorType = CursorType.Arrow;

    //    protected double _stepSize = .1;
    //    protected Point _lastCursorPoint;
    //    protected Point _cursorLocation = new Point(0, 0);

    //    private double _cursorSize = 0;

    //    protected void SetCursorColor(Color color)
    //    {
    //        if (_cursor != null)
    //        {
    //            SetCanvasColor(_cursor, new SolidColorBrush(color));
    //        }
    //    }

    //    public Point CursorLocation
    //    {
    //        get
    //        {
    //            return _cursorLocation;
    //        }
    //    }

    //    public bool AcquireCursor
    //    {
    //        get
    //        {
    //            return _cursorAcquired;
    //        }
    //        set
    //        {
    //            _cursorAcquired = value;

    //            if (_cursorAcquired)
    //            {
    //                selectCursor();
    //            }
    //            else
    //            {
    //                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
    //            }
    //        }
    //    }

    //    public void GhostCursor()
    //    {
    //        // for pick mode when selecting with arrow keys
    //        // force the draw cursor to be shown until the arrow cursor moves on the drawing canvas

    //        _crossCursorV.Visibility = Visibility.Visible;
    //        _crossCursorH.Visibility = Visibility.Visible;
    //    }

    //    public bool CursorVisible
    //    {
    //        set
    //        {
    //            if (_cursorType != CursorType.Arrow)
    //            {
    //                _cursor.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    //            }
    //        }
    //    }

    //    private void SetCursorScale(double scale)
    //    {
    //        _cursorScaleTransform.ScaleX = _cursorScaleTransform.ScaleY = scale;
    //    }

    //    public double CursorSize
    //    {
    //        get
    //        {
    //            return _cursorSize;
    //        }
    //        set
    //        {
    //            if (_cursorSize != value)
    //            {
    //                _cursorSize = value;

    //                if (_crossCursorV == null)
    //                {
    //                    CreateCursors();
    //                }
    //                else
    //                {
    //                    double vsize = _cursorSize > 0 ? _cursorSize : Window.Current.Bounds.Height;
    //                    double hsize = _cursorSize > 0 ? _cursorSize : Window.Current.Bounds.Width;

    //                    _crossCursorV.Y2 = vsize * 2;
    //                    _crossCursorV.Height = vsize * 2;
    //                    TranslateTransform tfv = new TranslateTransform();
    //                    tfv.Y = -vsize;
    //                    _crossCursorV.RenderTransform = tfv;

    //                    _crossCursorH.X2 = hsize * 2;
    //                    _crossCursorH.Width = hsize * 2;
    //                    TranslateTransform tfh = new TranslateTransform();
    //                    tfh.X = -hsize;
    //                    _crossCursorH.RenderTransform = tfh;
    //                }
    //            }
    //        }
    //    }

    //    private void selectCursor()
    //    {
    //        switch (_cursorType)
    //        {
    //            case CursorType.Arrow:
    //                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
    //                _crossCursorV.Visibility = Visibility.Collapsed;
    //                _crossCursorH.Visibility = Visibility.Collapsed;
    //                _panCursor.Visibility = Visibility.Collapsed;
    //                _cursor.Visibility = Visibility.Visible;
    //                _gmkEnabled = false;
    //                break;

    //            case CursorType.Hand:
    //                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
    //                _crossCursorV.Visibility = Visibility.Collapsed;
    //                _crossCursorH.Visibility = Visibility.Collapsed;
    //                _panCursor.Visibility = Visibility.Collapsed;
    //                _cursor.Visibility = Visibility.Visible;
    //                _gmkEnabled = true;
    //                break;

    //            case CursorType.Draw:
    //                Window.Current.CoreWindow.PointerCursor = null;
    //                _crossCursorV.Visibility = Visibility.Visible;
    //                _crossCursorH.Visibility = Visibility.Visible;
    //                _panCursor.Visibility = Visibility.Collapsed;
    //                _cursor.Visibility = Visibility.Visible;
    //                _gmkEnabled = true;
    //                break;

    //            case CursorType.Pan:
    //                Window.Current.CoreWindow.PointerCursor = null;
    //                _crossCursorV.Visibility = Visibility.Collapsed;
    //                _crossCursorH.Visibility = Visibility.Collapsed;
    //                _panCursor.Visibility = Visibility.Visible;
    //                _cursor.Visibility = Visibility.Visible;
    //                _gmkEnabled = true;
    //                break;
    //        }
    //    }

    //    public void SelectCursor(CursorType cursorType)
    //    {
    //        if (_cursorType != cursorType)
    //        {
    //            _cursorType = cursorType;

    //            if (_cursorAcquired)
    //            {
    //                selectCursor();
    //            }
    //        }
    //    }

    //    protected void CreateCursors()
    //    {
    //        if (_cursor == null)
    //        {
    //            _cursor = new Canvas();
    //            _cursor.RenderTransform = _cursorScaleTransform;
    //            _cursor.SetValue(Canvas.ZIndexProperty, 110000);
    //            this.Children.Add(_cursor);
    //        }

    //        if (_crossCursorV == null)
    //        {
    //            double vsize = _cursorSize > 0 ? _cursorSize : Window.Current.Bounds.Height;
    //            double hsize = _cursorSize > 0 ? _cursorSize : Window.Current.Bounds.Width;

    //            _crossCursorV = new Line();
    //            _crossCursorV.Visibility = Visibility.Collapsed;
    //            _cursor.Children.Add(_crossCursorV);

    //            _crossCursorV.X1 = 0;
    //            _crossCursorV.Y1 = 0;
    //            _crossCursorV.X2 = 0;
    //            _crossCursorV.Y2 = vsize * 2;
    //            _crossCursorV.Stroke = new SolidColorBrush(Globals.ActiveDrawing.Theme.CursorColor);
    //            _crossCursorV.StrokeThickness = 1;
    //            _crossCursorV.Width = 1;
    //            _crossCursorV.Height = vsize * 2;
    //            TranslateTransform tfv = new TranslateTransform();
    //            tfv.Y = -vsize;
    //            _crossCursorV.RenderTransform = tfv;

    //            _crossCursorH = new Line();
    //            _crossCursorH.Visibility = Visibility.Collapsed;
    //            _cursor.Children.Add(_crossCursorH);

    //            _crossCursorH.X1 = 0;
    //            _crossCursorH.Y1 = 0;
    //            _crossCursorH.X2 = hsize * 2;
    //            _crossCursorH.Y2 = 0;
    //            _crossCursorH.Stroke = new SolidColorBrush(Globals.ActiveDrawing.Theme.CursorColor);
    //            _crossCursorH.StrokeThickness = 1;
    //            _crossCursorH.Width = hsize * 2;
    //            _crossCursorH.Height = 1;
    //            TranslateTransform tfh = new TranslateTransform();
    //            tfh.X = -hsize;
    //            _crossCursorH.RenderTransform = tfh;
    //        }

    //        if (_panCursor == null)
    //        {
    //            // Add geometry for pan cursor
    //            object o = Application.Current.Resources["PanCursor"];
    //            System.Diagnostics.Debug.Assert(o != null, "Resource not found");     // Make sure the resource is available in the UI layer
    //            DataTemplate template = o as DataTemplate;
    //            _panCursor = template.LoadContent() as Canvas;

    //            _panCursor.Visibility = Visibility.Collapsed;
    //            _cursor.Children.Add(_panCursor);

    //            Brush b = new SolidColorBrush(Globals.ActiveDrawing.Theme.CursorColor);
    //            foreach (Shape s in _panCursor.Children)
    //            {
    //                s.Stroke = b;
    //            }
    //        }
    //    }

    //    public Point ResetCursorPosition(bool moveOnScreen)
    //    {
    //        if (moveOnScreen && CurrentWindow.Contains(_lastCursorPoint) == false)
    //        {
    //            _lastCursorPoint = Construct.RoundXY(new Point((CurrentWindow.Left + CurrentWindow.Right) / 2, (CurrentWindow.Top + CurrentWindow.Bottom) / 2));
    //        }

    //        SetCursorPositon(_lastCursorPoint);

    //        return _lastCursorPoint;
    //    }

    //    protected void SetCursorPositon(Point location)
    //    {
    //        bool moved = false;

    //        if (_gridSnap)
    //        {
    //            Point p = Construct.RoundXY(location);
    //            moved = _cursorLocation.X != p.X || _cursorLocation.Y != p.Y;
    //            _cursorLocation.X = p.X;
    //            _cursorLocation.Y = p.Y;
    //        }
    //        else
    //        {
    //            moved = _cursorLocation.X != location.X || _cursorLocation.Y != location.Y;
    //            _cursorLocation.X = location.X;
    //            _cursorLocation.Y = location.Y;
    //        }
    //        //System.Diagnostics.Debug.WriteLine("SetCursorPositon({0},{1}) {2}", _cursorLocation.X, _cursorLocation.Y, moved);

    //        if (moved)
    //        {
    //            Point canvas = Globals.DrawingCanvas.PaperToCanvas(_cursorLocation);

    //            try
    //            {
    //                _cursor.SetValue(Canvas.LeftProperty, canvas.X);
    //                _cursor.SetValue(Canvas.TopProperty, canvas.Y);

    //                TrackCursor(!_shiftKeyIsDown);
    //            }
    //            catch (Exception ex)
    //            {
    //                string message = string.Format("SetValue error in SetCursorPositon: x={0}, y={0}", canvas.X, canvas.Y);
    //                Analytics.ReportError(message, ex, 2);
    //            }
    //        }
    //    }

    //    public void MoveCursorBy(double dx, double dy)
    //    {
    //        // dx,dy: delta in paper units
    //        _cursorLocation.X += dx;
    //        _cursorLocation.Y += dy;

    //        if (Globals.CommandProcessor != null)
    //        {
    //            _cursorLocation = Globals.CommandProcessor.Step(dx, dy);
    //        }

    //        _lastCursorPoint = _cursorLocation;
    //        Point canvas = Globals.DrawingCanvas.PaperToCanvas(_lastCursorPoint);
    //        try
    //        {
    //            _cursor.SetValue(Canvas.LeftProperty, canvas.X);
    //            _cursor.SetValue(Canvas.TopProperty, canvas.Y);
    //        }
    //        catch (Exception ex)
    //        {
    //            Analytics.ReportError("SetValue error in MoveCursorBy", ex, 3);
    //        }

    //        CursorVisible = true;

    //        TrackCursor(false);
    //    }
    //}
}

