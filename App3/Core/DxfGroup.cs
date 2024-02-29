
namespace Cirros.Dxf
{
    public class DxfGroup
    {
        protected int _code = 0;
        protected string _value = "";

        public DxfGroup()
        {
        }

        public DxfGroup(int code, string value)
        {
            _code = code;
            _value = value;
        }

        public int Code
        {
            get { return _code; }
            set { _code = value; }
        }

        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
