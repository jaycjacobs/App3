using System.Collections.Generic;

namespace Cirros.Dxf
{
    class DxfInsertEntity : DxfEntity
    {
        public DxfInsertEntity(DxfReader reader)
            : base(reader)
        {
            Type = "INSERT";
        }

        //100  Subclass marker (AcDbBlockReference) 
        //210, 220, 230  Extrusion direction (optional; default = 0, 0, 1) DXF: X value; APP: 3D vector
        // 10  Insertion point (in OCS)DXF: X value; APP: 3D point
        // 20, 30 DXF: Y and Z values of insertion point (in OCS) 

        // 66  Variable attributes-follow flag (optional; default = 0); if the value of attributes-follow flag is 1, a series of attribute entities is expected to follow the insert, terminated by a seqend entity
        //  2  Block name
        // 41  X scale factor (optional; default = 1)
        // 42  Y scale factor (optional; default = 1)
        // 43  Z scale factor (optional; default = 1)
        // 50  Rotation angle (optional; default = 0)
        // 70  Column count (optional; default = 1)
        // 71  Row count (optional; default = 1)
        // 44  Column spacing (optional; default = 0)
        // 45  Row spacing (optional; default = 0)

        public int AttribsFollow;           //66
        public string BlockName;            //2
        public float XScale = 1;            //41
        public float YScale = 1;            //42
        public float ZScale = 1;            //43
        public float Rotation = 0;          //50
        public int Columns;                 //70
        public int Rows;                    //71
        public float ColSpacing;            //44
        public float RowSpacing;            //45
        public bool IsHatch = false;

        private string App = "";

        public List<DxfAttribEntity> Attributes = new List<DxfAttribEntity>();

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 2:
                    BlockName = group.Value;
                    break;
                case 41:
                    XScale = float.Parse(group.Value);
                    break;
                case 42:
                    YScale = float.Parse(group.Value);
                    break;
                case 43:
                    ZScale = float.Parse(group.Value);
                    break;
                case 50:
                    Rotation = float.Parse(group.Value);
                    break;
                case 66:
                    AttribsFollow = int.Parse(group.Value);
                    break;
                case 70:
                    Columns = int.Parse(group.Value);
                    break;
                case 71:
                    Rows = int.Parse(group.Value);
                    break;
                case 44:
                    ColSpacing = float.Parse(group.Value);
                    break;
                case 45:
                    RowSpacing = float.Parse(group.Value);
                    break;
                case 1000:
                    IsHatch = App == "ACAD" && group.Value == "HATCH";
                    break;
                case 1001:
                    App = group.Value;
                    break;
                default:
                    base.SetValue(group);
                    break;
            }
        }
    }
}
