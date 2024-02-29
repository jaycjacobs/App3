using Cirros.Commands;
using Cirros.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using App3;

namespace Cirros
{
    class TouchPointerHandler : IPointerHandler
    {
        List<uint> _activePointers = new List<uint>();

        GestureRecognizer _gestureRecognizer = null;
        UIElement _grSurface = null;

        CompositeTransform _deltaTransform;

        bool _gestureManipulating = false;
        bool _pinchZoomIsEnabled = false;

        public TouchPointerHandler(DrawingCanvas canvas)
            : base(canvas)
        {
            if (App.Window.Frame is IDrawingPage dp)
            {
                InitializeManipulationInputProcessor(new GestureRecognizer(), dp.ZoomOverlayTarget);
            }
        }

        public async override Task<bool> PointerPressed(Windows.Foundation.Point p, PointerRoutedEventArgs e)
        {
            if (_pinchZoomIsEnabled != Globals.EnablePinchZoom)
            {
                EnablePinchZoom(Globals.EnablePinchZoom);
            }

            _input.AcquireCursor = true;

            if (_activePointers.Contains(e.Pointer.PointerId) == false)
            {
                _activePointers.Add(e.Pointer.PointerId);
            }

            try
            {
                _gestureRecognizer.ProcessDownEvent(e.GetCurrentPoint(_grSurface));
            }
            catch (Exception ex)
            {
                Analytics.ReportError("_gestureRecognizer.ProcessDownEvent", ex, 3, 320);
            }

            if (_activePointers.Count > 1)
            {
                if (Globals.EnableTouchMagnifer)
                {
                    Globals.DrawingCanvas.VectorListControl.ShowMagnifier = false;
                }

                if (Globals.CommandProcessor != null && Globals.CommandProcessor.InputMode == InputMode.Draw)
                {
                    Globals.CommandProcessor.PointerLeftDrawingArea();
                }

                Globals.Input.CursorVisible = false;
            }

            if (Globals.CommandProcessor != null && Globals.CommandProcessor.InputMode == InputMode.Draw)
            {
                return false;
            }
            if (_activePointers.Count > 1)
            {
                return false;
            }

            await base.PointerPressed(p, e);

            return true;
        }

        public override bool PointerReleased(Windows.Foundation.Point p, PointerRoutedEventArgs e)
        {
            if (_gestureManipulating == false && _activePointers.Count == 1 && Globals.CommandProcessor != null)
            {
                if (Globals.CommandProcessor.InputMode == InputMode.Draw)
                {
                    // In single touch draw mode, commit on pointer released
                    Globals.DrawingCanvas.DoPointerPress(p);
                }
                else if (Globals.CommandProcessor.InputMode == InputMode.Pick)
                {
                    // In single touch pick mode, commit on pointer released
                    Globals.DrawingCanvas.DoPointerPress(p);
                }
            }

            try
            {
                _gestureRecognizer.ProcessUpEvent(e.GetCurrentPoint(_grSurface));
            }
            catch (Exception ex)
            {
                Analytics.ReportError("_gestureRecognizer.ProcessUpEvent", ex, 3, 400);
            }

            if (_activePointers.Contains(e.Pointer.PointerId))
            {
                _activePointers.Remove(e.Pointer.PointerId);
            }

            if (Globals.EnableTouchMagnifer)
            {
                Globals.DrawingCanvas.VectorListControl.ShowMagnifier = false;
            }

            return base.PointerReleased(p, e);
        }

        public override bool PointerMoved(Windows.Foundation.Point p, PointerRoutedEventArgs e)
        {
            try
            {
                _gestureRecognizer.ProcessMoveEvents(e.GetIntermediatePoints(_grSurface));
            }
            catch (Exception ex)
            {
                Analytics.ReportError("_gestureRecognizer.ProcessMoveEvents", ex, 3, 401);
            }

            return _activePointers.Count == 1;
        }

        public override void PointerEnter(Windows.Foundation.Point p, PointerRoutedEventArgs e)
        {
        }

        public override void Capture(PointerRoutedEventArgs e)
        {
        }

        public override bool Release(PointerRoutedEventArgs e)
        {
            return false;
        }

        public override void PointerLeave(Point p, PointerRoutedEventArgs e)
        {
        }

        protected void EnablePinchZoom(bool enable)
        {
            _gestureRecognizer.CompleteGesture();

            if (enable)
            {
                // Pinch zoom

                _gestureRecognizer.GestureSettings =
                    GestureSettings.ManipulationScale |
                    GestureSettings.ManipulationScaleInertia |
                    GestureSettings.ManipulationMultipleFingerPanning |
                    GestureSettings.ManipulationTranslateX |
                    GestureSettings.ManipulationTranslateY |
                    GestureSettings.ManipulationTranslateInertia;
            }
            else
            {
                // Pan only

                _gestureRecognizer.GestureSettings =
                    GestureSettings.ManipulationTranslateX |
                    GestureSettings.ManipulationTranslateY |
                    GestureSettings.ManipulationTranslateInertia;

            }

            _pinchZoomIsEnabled = enable;
        }

        public void InitializeManipulationInputProcessor(GestureRecognizer gr, UIElement referenceframe)
        {
            _gestureRecognizer = gr;
            _grSurface = referenceframe;

            EnablePinchZoom(Globals.EnablePinchZoom);

            _gestureRecognizer.ManipulationStarted += gestureRecognizer_ManipulationStarted;
            _gestureRecognizer.ManipulationUpdated += gestureRecognizer_ManipulationUpdated;
            _gestureRecognizer.ManipulationCompleted += gestureRecognizer_ManipulationCompleted;
        }

        void gestureRecognizer_ManipulationStarted(GestureRecognizer sender, ManipulationStartedEventArgs args)
        {
            if (_activePointers.Count > 1)
            {
                _deltaTransform = new CompositeTransform();

                _gestureManipulating = true;
            }
        }

        void gestureRecognizer_ManipulationUpdated(GestureRecognizer sender, ManipulationUpdatedEventArgs args)
        {
            if (_gestureManipulating)
            {
                Point center = new Point(args.Position.X, args.Position.Y);

                _deltaTransform.CenterX = center.X;
                _deltaTransform.CenterY = center.Y;

                _deltaTransform.ScaleX = _deltaTransform.ScaleY = args.Delta.Scale;
                _deltaTransform.TranslateX = args.Delta.Translation.X;
                _deltaTransform.TranslateY = args.Delta.Translation.Y;

                GeneralTransform gt = _deltaTransform.Inverse;

                Point tp0 = gt.TransformPoint(new Point(0, 0));
                Point tp1 = gt.TransformPoint(new Point(Globals.DrawingCanvas.ViewPortSize.Width, Globals.DrawingCanvas.ViewPortSize.Height));

                tp0 = Globals.DrawingCanvas.DisplayToPaper(tp0);
                tp1 = Globals.DrawingCanvas.DisplayToPaper(tp1);
                Globals.View.DisplayWindow(tp0.X, tp0.Y, tp1.X, tp1.Y);
            }
            else
            {
                if (Globals.EnableTouchMagnifer)
                {
                    Globals.DrawingCanvas.VectorListControl.ShowMagnifier = true;
                }
            }
        }

        void gestureRecognizer_ManipulationCompleted(GestureRecognizer sender, ManipulationCompletedEventArgs args)
        {
            if (_gestureManipulating)
            {
                GeneralTransform gt = _deltaTransform.Inverse;

                Point tp0 = gt.TransformPoint(new Point(0, 0));
                Point tp1 = gt.TransformPoint(new Point(Globals.DrawingCanvas.ViewPortSize.Width, Globals.DrawingCanvas.ViewPortSize.Height));

                tp0 = Globals.DrawingCanvas.DisplayToPaper(tp0);
                tp1 = Globals.DrawingCanvas.DisplayToPaper(tp1);
                Globals.View.DisplayWindow(tp0.X, tp0.Y, tp1.X, tp1.Y);

                _gestureManipulating = false;
            }
        }
    }
}
