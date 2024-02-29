using Cirros;
using Cirros.Drawing;
using Cirros.Utility;
using System;
using Microsoft.Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;

namespace CirrosUI
{
    public class NumberBox1 : TextBox
    {
        protected string _oldString = "";
        protected bool _isInt = false;
        protected bool _isDistance = false;
        //protected bool _isPaper = false;
        protected bool _isAngle = false;
        protected bool _enforceMinimum = false;
        protected bool _enforceMaximum = false;
        protected double _minValue = 0.0;
        protected double _maxValue = 0.0;
        protected double _value = 0.0;
        protected bool _isDirty = false;
        protected int _precision = 4;
        private string _format = "{0}";
        protected bool _stripZeros = true;
        private bool _enterKeyPressed = false;
        private Unit _unit = Unit.Undefined;
        //private bool _isArchitectural = false;
        private string _zeroStringText = null;

        public event ValueChangedHandler OnValueChanged;
        public delegate void ValueChangedHandler(object sender, ValueChangedEventArgs e);

        public NumberBox1()
        {
            //TODO:
            // Why doesn't this work?
            //this.InputScope = new InputScope();
            //if (this.InputScope != null)
            //{
            //    this.InputScope.Names.Add(new InputScopeName(InputScopeNameValue.Number));
            //}
            Precision = _precision;
            _unit = Globals.ActiveDrawing == null ? Unit.Undefined : Globals.ActiveDrawing.ModelUnit;
            //_isArchitectural = Globals.ActiveDrawing == null ? false : Globals.ActiveDrawing.IsArchitecturalScale && Globals.Input.GridSnap;
        }

        public void UnitChanged()
        {
            _unit = Globals.ActiveDrawing == null ? Unit.Undefined : Globals.ActiveDrawing.ModelUnit;
            //_isArchitectural = Globals.ActiveDrawing == null ? false : Globals.ActiveDrawing.IsArchitecturalScale && Globals.Input.GridSnap;
        }

        public bool IsEmpty
        {
            get
            {
                return Text.Length == 0;
            }
        }

        public bool IsDirty
        {
            get
            {
                //if (!_isDirty)
                {
                    checkValue();
                }
                return _isDirty;
            }
        }

        public Unit Unit
        {
            get
            {
                return _unit;
            }
            set
            {
                if (Globals.ActiveDrawing != null)
                {
                    if (value == Unit.Paper)
                    {
                        _unit = Globals.ActiveDrawing.PaperUnit;
                    }
                    else if (value == Unit.Model)
                    {
                        _unit = Globals.ActiveDrawing.ModelUnit;
                    }
                    else
                    {
                        _unit = value;
                    }
                }
                else
                {
                    _unit = value;
                }
            }
        }

        public bool IsInteger
        {
            get
            {
                return _isInt;
            }
            set
            {
                _isInt = value;
                _format = "{0}";
            }
        }

        public bool IsDistance
        {
            get
            {
                return _isDistance;
            }
            set
            {
                _isDistance = value;
            }
        }

        //public bool IsPaper
        //{
        //    get
        //    {
        //        return _isPaper;
        //    }
        //    set
        //    {
        //        _isPaper = value;
        //    }
        //}

        public bool IsAngle
        {
            get
            {
                return _isAngle;
            }
            set
            {
                _isAngle = value;

                if (_isAngle)
                {
                    MinValue = -360;
                    MaxValue = 360;
                    StripTrailingZeros = true;
                }

                //Value = _value;
            }
        }

        public bool StripTrailingZeros
        {
            get
            {
                return _stripZeros;
            }
            set
            {
                _stripZeros = value;
            }
        }

        public int Precision
        {
            get
            {
                return _precision;
            }
            set
            {
                _precision = value;
                _format = _precision == 0 ? "{0}" : "{0:F" + _precision + "}";
            }
        }

        public double MinValue
        {
            get
            {
                return _minValue;
            }
            set
            {
                _minValue = value;
                _enforceMinimum = true;
            }
        }

        public double MaxValue
        {
            get
            {
                return _maxValue;
            }
            set
            {
                _maxValue = value;
                _enforceMaximum = true;
            }
        }

        public string ZeroStringText
        {
            get
            {
                return _zeroStringText;
            }
            set
            {
                _zeroStringText = value;
            }
        }

        public double Value
        {
            get
            {
                checkValue();

                return _value;
            }
            set
            {
                _value = value;
                _isDirty = false;

                Format();

                _oldString = Text;
            }
        }

        public void Format()
        {
            if (_isAngle && _value <= -360)
            {
                _value = 360;
            }

            if (_isInt)
            {
                Text = string.Format("{0}", (int)_value);
            }
            else if (_isDistance)
            {
                if (Globals.ActiveDrawing == null)
                {
                    Text = string.Format("{0}", (int)_value);
                }
                else
                {
                    Text = Utilities.FormatDistance(_value, Globals.DimensionRound, Globals.ActiveDrawing.IsArchitecturalScale && Globals.Input.GridSnap, true, _unit, false);
                }
            }
            else if (_stripZeros)
            {
                string s = string.Format(_format, Math.Round(_value, _precision));

                if (s.IndexOf('.') >= 0)
                {
                    int l = s.Length;
                    while (s[l - 1] == '0' && l > 1)
                    {
                        --l;
                    }
                    if (s[l - 1] == '.')
                    {
                        --l;
                    }
                    Text = s.Length == l ? s : s.Substring(0, l);
                }
                else
                {
                    Text = s;
                }
            }
            else
            {
                Text = string.Format("{0}", _value);
            }

            if (_value == 0 && _zeroStringText != null)
            {
#if SIBERIA
                if (FocusState == Windows.UI.Xaml.FocusState.Unfocused)
                {
                    Text = _zeroStringText;
                    FontStyle = FontStyle.Italic;
                    FontWeight = FontWeights.Light;
                }
                else
                {
                    Text = "";
                    FontStyle = FontStyle.Normal;
                    FontWeight = FontWeights.Normal;
                }
#endif
            }
            else if (_isAngle)
            {
                Text = Text + "°";
            }
        }

        protected bool checkValue()
        {
            bool isValid = true;

            // Ignoring _isDirty and keying off the string change will cause multi-numberBox dialogs
            // to light dismiss when terminating text entry with the enter key
            //if (_isDirty)
            if (_isDirty || Text != _oldString)
            {
                if (_zeroStringText != null)
                {
                    if (string.IsNullOrEmpty(Text) || Text == _zeroStringText)
                    {
                        _value = 0;
                        _isDirty = false;
                        _oldString = Text = "";

                        if (OnValueChanged != null)
                        {
                            ValueChangedEventArgs ve = new ValueChangedEventArgs(_value, _enterKeyPressed);
                            OnValueChanged(this, ve);
                        }
                        return true;
                    }
                }

                double v = 0;
                string valueString = this.Text.Trim();

                try
                {
                    if (_isDistance && _unit == Unit.Feet && Globals.ActiveDrawing.IsArchitecturalScale)
                    {
                        //v = Utilities.ParseArchitectDistance(valueString, _isPaper);
                        v = Utilities.ParseArchitectDistance(valueString);
                    }
                    else
                    {
                        double ufactor = 1;

                        if (_isDistance)
                        {
                            if (_unit == Unit.Inches || _unit == Unit.Feet)
                            {
                                int n = valueString.IndexOfAny(new char[] { '\'', '"' });
                                if (n >= 0)
                                {
                                    string unitString = valueString.Substring(n).ToLower();
                                    valueString = valueString.Substring(0, n);

                                    if (_unit == Unit.Feet)
                                    {
                                        if (unitString == "\"")
                                        {
                                            ufactor = 1.0 / 12.0;
                                        }
                                    }
                                    else if (_unit == Unit.Inches)
                                    {
                                        if (unitString == "\'")
                                        {
                                            ufactor = 12;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                int n = valueString.ToLower().IndexOfAny(new char[] { 'm', 'c' });
                                if (n >= 0)
                                {
                                    string unitString = valueString.Substring(n).ToLower();
                                    valueString = valueString.Substring(0, n);

                                    //if (_isPaper && unitString != "mm")
                                    //{
                                    //    if (unitString == "cm")
                                    //    {
                                    //        ufactor = 10;
                                    //    }
                                    //    else if (unitString == "m")
                                    //    {
                                    //        ufactor = 1000;
                                    //    }
                                    //    else
                                    //    {
                                    //        // invalid unit - should cause exception below
                                    //    }
                                    //}
                                    //else 
                                    if (unitString == "mm")
                                    {
                                        if (_unit == Unit.Centimeters)
                                        {
                                            ufactor = .1;
                                        }
                                        else if (_unit == Unit.Meters)
                                        {
                                            ufactor = 1000;
                                        }
                                    }
                                    else if (unitString == "cm")
                                    {
                                        if (_unit == Unit.Millimeters)
                                        {
                                            ufactor = 10;
                                        }
                                        else if (_unit == Unit.Meters)
                                        {
                                            ufactor = 100;
                                        }
                                    }
                                    else if (unitString == "m")
                                    {
                                        if (_unit == Unit.Centimeters)
                                        {
                                            ufactor = .01;
                                        }
                                        else if (_unit == Unit.Millimeters)
                                        {
                                            ufactor = .001;
                                        }
                                    }
                                }
                            }
                        }
                        if (_isAngle)
                        {
                            int n = valueString.IndexOfAny(new char[] { '°' });
                            if (n >= 0)
                            {
                                valueString = valueString.Substring(0, n);
                            }
                        }

                        if (_isInt)
                        {
                            int i = int.Parse(valueString);
                        }

                        if (valueString.Length == 0)
                        {
                            v = 0;
                        }
                        else if (valueString == ".")
                        {
                            v = 0;
                        }
                        else
                        {
                            v = double.Parse(valueString);
                        }

                        v *= ufactor;
                    }
                }
                catch
                {
                    Text = _oldString;
                    SelectAll();
                    isValid = false;
                }

                if (_enforceMinimum && v < _minValue)
                {
                    v = _value;
                    Text = _oldString;
                    SelectAll();
                    isValid = false;
                }

                if (_enforceMaximum && v > _maxValue)
                {
                    v = _value;
                    Text = _oldString;
                    SelectAll();
                    isValid = false;
                }

                if (_value != v)
                {
                    _value = v;

                    if (OnValueChanged != null)
                    {
                        ValueChangedEventArgs ve = new ValueChangedEventArgs(_value, _enterKeyPressed);
                        OnValueChanged(this, ve);
                    }
                }
            }

            return isValid;
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                _enterKeyPressed = true;
                checkValue();
                _isDirty = false;
            }
            else
            {
                _isDirty = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            if (_value == 0 && _zeroStringText != null)
            {
                Text = "";
#if SIBERIA
                FontStyle = FontStyle.Normal;
#endif
                FontWeight = FontWeights.Normal;
            }
            
            _oldString = Text;
            base.OnGotFocus(e);
        }
        
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            checkValue();

            if (_value == 0 && _zeroStringText != null)
            {
                Text = _zeroStringText;
#if SIBERIA
                FontStyle = FontStyle.Italic;
#endif
                FontWeight = FontWeights.Light;
            }
            //else
            //{
            //    checkValue();
            //}

            base.OnLostFocus(e);
        }

#if WINDOWS_UWP
#else
        protected override void OnApplyTemplate()
        {
            Thickness t = this.Padding;
            double pv = (t.Top + t.Bottom) / 2;
            t.Left = t.Right * .6;
            t.Top = t.Bottom;// = pv;
            this.Padding = t;

            base.OnApplyTemplate();
        }
#endif
    }

    public class ValueChangedEventArgs : EventArgs
    {
        private double _value;
        private bool _finished;

        public ValueChangedEventArgs(double value, bool finished)
        {
            _value = value;
            _finished = finished;
        }

        public bool Finished
        {
            get
            {
                return _finished;
            }
            set
            {
                _finished = value;
            }
        }

        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }
    }
}
