
namespace Cirros.Dxf
{
    public class DxfPoint3
    {
        public DxfPoint3()
        {
        }

        public DxfPoint3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X = 0;
        public float Y = 0;
        public float Z = 0;

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", X, Y, Z);
        }
    }
}
