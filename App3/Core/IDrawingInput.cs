using Cirros.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace Cirros.Core
{
    public interface IDrawingInput
    {
        bool EnableGMK { set; }
        bool ShiftKeyIsDown { get; }
        bool GridSnap { get; set; }
        GridSnapMode GridSnapMode { get; set; }
        bool ObjectSnap { get; set; }
        int InputDevice { get; }
        void ProcessKey(string key, bool isGMK);
        void EnterPoint();
        Point CursorLocation { get; }
        bool AcquireCursor { get; set; }
        void GhostCursor();
        bool CursorVisible { set; }
        double CursorSize { get; set; }
        void SelectCursor(CursorType cursorType);
        Point ResetCursorPosition(bool moveOnScreen);
        void MoveCursorBy(double dx, double dy);
        void MoveCursorTo(double x, double y);
        void PushPoint(InputPoint inputPoint);
        InputPoint PopPoint();
    }

    public enum GridSnapMode
    {
        wholeGrid,
        halfGrid,
        auto
    }

    public enum CoordinateMode
    {
        Absolute,
        Delta,
        Polar,
        End
    }

    public class InputPoint
    {
        CoordinateMode _mode;
        double _v1;
        double _v2;
        string _key;

        public InputPoint(CoordinateMode mode, double v1, double v2, string key)
        {
            _mode = mode;
            _v1 = v1;
            _v2 = v2;
            _key = key;
        }

        public static InputPoint NewAbsolutePoint(double x, double y, string key = "")
        {
            return new InputPoint(CoordinateMode.Absolute, x, y, key);
        }

        public static InputPoint NewDeltaPoint(double dx, double dy, string key = "")
        {
            return new InputPoint(CoordinateMode.Absolute, dx, dy, key);
        }

        public static InputPoint NewPolarPoint(double distance, double angle, string key = "")
        {
            return new InputPoint(CoordinateMode.Absolute, distance, angle, key);
        }

        public static InputPoint NewTerminatePoint()
        {
            return new InputPoint(CoordinateMode.End, 0, 0, "");
        }

        public CoordinateMode Mode { get { return _mode; } set { _mode = value; } }
        public double V1 { get { return _v1; } set { _v1 = value; } }
        public double X { get { return _v1; } }
        public double Dx { get { return _v1; } }
        public double Distance { get { return _v1; } }
        public double V2 { get { return _v2; } set { _v2 = value; } }
        public double Y { get { return _v2; } }
        public double Dy { get { return _v2; } }
        public double Angle { get { return _v2; } }
        public string Key { get { return _key; } set { _key = value; } }
    }
}
