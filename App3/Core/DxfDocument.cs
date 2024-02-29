using System.Collections.Generic;

namespace Cirros.Dxf
{
    public class DxfDocument
    {
        private Dictionary<string, object> _headerCollection = new Dictionary<string, object>();

        private Dictionary<string, DxfLtype> _ltypeList = new Dictionary<string, DxfLtype>();
        private Dictionary<string, DxfLayer> _layerList = new Dictionary<string, DxfLayer>();
        private Dictionary<string, DxfBlock> _blockList = new Dictionary<string, DxfBlock>();
        private Dictionary<string, DxfStyle> _styleList = new Dictionary<string, DxfStyle>();
        private List<DxfEntity> _entityList = new List<DxfEntity>();
        private List<DxfVport> _vportList = new List<DxfVport>();
        private Dictionary<string, DxfDimstyle> _dimstyleList = new Dictionary<string, DxfDimstyle>();
        private List<DxfView> _viewList = new List<DxfView>();
        private List<DxfUcs> _ucsList = new List<DxfUcs>();
        private Dictionary<string, DxfBlockRecord> _blockRecordList = new Dictionary<string, DxfBlockRecord>();

        private uint[] _colorSpecTable = new uint[256];

        public DxfDocument()
        {
            initializeColorTable();
        }

        public Dictionary<string, object> HEADERCollection
        {
            get
            {
                return _headerCollection;
            }
        }

        public Dictionary<string, DxfLtype> LTYPEList
        {
            get
            {
                return _ltypeList;
            }
        }

        public Dictionary<string, DxfLayer> LAYERList
        {
            get
            {
                return _layerList;
            }
        }

        public Dictionary<string, DxfBlock> BLOCKList
        {
            get
            {
                return _blockList;
            }
        }

        public List<DxfEntity> ENTITYList
        {
            get
            {
                return _entityList;
            }
        }

        public List<DxfVport> VPORTList
        {
            get
            {
                return _vportList;
            }
        }

        public Dictionary<string, DxfStyle> STYLEList
        {
            get
            {
                return _styleList;
            }
        }

        public Dictionary<string, DxfDimstyle> DIMSTYLEList
        {
            get
            {
                return _dimstyleList;
            }
        }

        public List<DxfView> VIEWList
        {
            get
            {
                return _viewList;
            }
        }

        public List<DxfUcs> UCSList
        {
            get
            {
                return _ucsList;
            }
        }

        public Dictionary<string, DxfBlockRecord> BLOCKRECORDList
        {
            get
            {
                return _blockRecordList;
            }
        }

        public uint ColorSpecFromDxfColor(int dxfColor)
        {
            return _colorSpecTable[dxfColor];
        }

        private void initializeColorTable()
        {
            _colorSpecTable[0] = 0xFFFFFFFF;
            _colorSpecTable[1] = 0xFFFF0000;
            _colorSpecTable[2] = 0xFFFFFF00;
            _colorSpecTable[3] = 0xFF00FF00;
            _colorSpecTable[4] = 0xFF00FFFF;
            _colorSpecTable[5] = 0xFF0000FF;
            _colorSpecTable[6] = 0xFFFF00FF;
            _colorSpecTable[7] = 0xFFFFFFFF;
            _colorSpecTable[8] = 0xFF414141;
            _colorSpecTable[9] = 0xFF808080;
            _colorSpecTable[10] = 0xFFFF0000;
            _colorSpecTable[11] = 0xFFFFAAAA;
            _colorSpecTable[12] = 0xFFBD0000;
            _colorSpecTable[13] = 0xFFBD7E7E;
            _colorSpecTable[14] = 0xFF810000;
            _colorSpecTable[15] = 0xFF815656;
            _colorSpecTable[16] = 0xFF680000;
            _colorSpecTable[17] = 0xFF684545;
            _colorSpecTable[18] = 0xFF4F0000;
            _colorSpecTable[19] = 0xFF4F3535;
            _colorSpecTable[20] = 0xFFFF3F00;
            _colorSpecTable[21] = 0xFFFFBFAA;
            _colorSpecTable[22] = 0xFFBD2E00;
            _colorSpecTable[23] = 0xFFBD8D7E;
            _colorSpecTable[24] = 0xFF811F00;
            _colorSpecTable[25] = 0xFF816056;
            _colorSpecTable[26] = 0xFF681900;
            _colorSpecTable[27] = 0xFF684E45;
            _colorSpecTable[28] = 0xFF4F1300;
            _colorSpecTable[29] = 0xFF4F3B35;
            _colorSpecTable[30] = 0xFFFF7F00;
            _colorSpecTable[31] = 0xFFFFD4AA;
            _colorSpecTable[32] = 0xFFBD5E00;
            _colorSpecTable[33] = 0xFFBD9D7E;
            _colorSpecTable[34] = 0xFF814000;
            _colorSpecTable[35] = 0xFF816B56;
            _colorSpecTable[36] = 0xFF683400;
            _colorSpecTable[37] = 0xFF685645;
            _colorSpecTable[38] = 0xFF4F2700;
            _colorSpecTable[39] = 0xFF4F4235;
            _colorSpecTable[40] = 0xFFFFBF00;
            _colorSpecTable[41] = 0xFFFFEAAA;
            _colorSpecTable[42] = 0xFFBD8D00;
            _colorSpecTable[43] = 0xFFBDAD7E;
            _colorSpecTable[44] = 0xFF816000;
            _colorSpecTable[45] = 0xFF817656;
            _colorSpecTable[46] = 0xFF684E00;
            _colorSpecTable[47] = 0xFF685F45;
            _colorSpecTable[48] = 0xFF4F3B00;
            _colorSpecTable[49] = 0xFF4F4935;
            _colorSpecTable[50] = 0xFFFFFF00;
            _colorSpecTable[51] = 0xFFFFFFAA;
            _colorSpecTable[52] = 0xFFBDBD00;
            _colorSpecTable[53] = 0xFFBDBD7E;
            _colorSpecTable[54] = 0xFF818100;
            _colorSpecTable[55] = 0xFF818156;
            _colorSpecTable[56] = 0xFF686800;
            _colorSpecTable[57] = 0xFF686845;
            _colorSpecTable[58] = 0xFF4F4F00;
            _colorSpecTable[59] = 0xFF4F4F35;
            _colorSpecTable[60] = 0xFFBFFF00;
            _colorSpecTable[61] = 0xFFEAFFAA;
            _colorSpecTable[62] = 0xFF8DBD00;
            _colorSpecTable[63] = 0xFFADBD7E;
            _colorSpecTable[64] = 0xFF608100;
            _colorSpecTable[65] = 0xFF768156;
            _colorSpecTable[66] = 0xFF4E6800;
            _colorSpecTable[67] = 0xFF5F6845;
            _colorSpecTable[68] = 0xFF3B4F00;
            _colorSpecTable[69] = 0xFF494F35;
            _colorSpecTable[70] = 0xFF7FFF00;
            _colorSpecTable[71] = 0xFFD4FFAA;
            _colorSpecTable[72] = 0xFF5EBD00;
            _colorSpecTable[73] = 0xFF9DBD7E;
            _colorSpecTable[74] = 0xFF408100;
            _colorSpecTable[75] = 0xFF6B8156;
            _colorSpecTable[76] = 0xFF346800;
            _colorSpecTable[77] = 0xFF566845;
            _colorSpecTable[78] = 0xFF274F00;
            _colorSpecTable[79] = 0xFF424F35;
            _colorSpecTable[80] = 0xFF3FFF00;
            _colorSpecTable[81] = 0xFFBFFFAA;
            _colorSpecTable[82] = 0xFF2EBD00;
            _colorSpecTable[83] = 0xFF8DBD7E;
            _colorSpecTable[84] = 0xFF1F8100;
            _colorSpecTable[85] = 0xFF608156;
            _colorSpecTable[86] = 0xFF196800;
            _colorSpecTable[87] = 0xFF4E6845;
            _colorSpecTable[88] = 0xFF134F00;
            _colorSpecTable[89] = 0xFF3B4F35;
            _colorSpecTable[90] = 0xFF00FF00;
            _colorSpecTable[91] = 0xFFAAFFAA;
            _colorSpecTable[92] = 0xFF00BD00;
            _colorSpecTable[93] = 0xFF7EBD7E;
            _colorSpecTable[94] = 0xFF008100;
            _colorSpecTable[95] = 0xFF568156;
            _colorSpecTable[96] = 0xFF006800;
            _colorSpecTable[97] = 0xFF456845;
            _colorSpecTable[98] = 0xFF004F00;
            _colorSpecTable[99] = 0xFF354F35;
            _colorSpecTable[100] = 0xFF00FF3F;
            _colorSpecTable[101] = 0xFFAAFFBF;
            _colorSpecTable[102] = 0xFF00BD2E;
            _colorSpecTable[103] = 0xFF7EBD8D;
            _colorSpecTable[104] = 0xFF00811F;
            _colorSpecTable[105] = 0xFF568160;
            _colorSpecTable[106] = 0xFF006819;
            _colorSpecTable[107] = 0xFF45684E;
            _colorSpecTable[108] = 0xFF004F13;
            _colorSpecTable[109] = 0xFF354F3B;
            _colorSpecTable[110] = 0xFF00FF7F;
            _colorSpecTable[111] = 0xFFAAFFD4;
            _colorSpecTable[112] = 0xFF00BD5E;
            _colorSpecTable[113] = 0xFF7EBD9D;
            _colorSpecTable[114] = 0xFF008140;
            _colorSpecTable[115] = 0xFF56816B;
            _colorSpecTable[116] = 0xFF006834;
            _colorSpecTable[117] = 0xFF456856;
            _colorSpecTable[118] = 0xFF004F27;
            _colorSpecTable[119] = 0xFF354F42;
            _colorSpecTable[120] = 0xFF00FFBF;
            _colorSpecTable[121] = 0xFFAAFFEA;
            _colorSpecTable[122] = 0xFF00BD8D;
            _colorSpecTable[123] = 0xFF7EBDAD;
            _colorSpecTable[124] = 0xFF008160;
            _colorSpecTable[125] = 0xFF568176;
            _colorSpecTable[126] = 0xFF00684E;
            _colorSpecTable[127] = 0xFF45685F;
            _colorSpecTable[128] = 0xFF004F3B;
            _colorSpecTable[129] = 0xFF354F49;
            _colorSpecTable[130] = 0xFF00FFFF;
            _colorSpecTable[131] = 0xFFAAFFFF;
            _colorSpecTable[132] = 0xFF00BDBD;
            _colorSpecTable[133] = 0xFF7EBDBD;
            _colorSpecTable[134] = 0xFF008181;
            _colorSpecTable[135] = 0xFF568181;
            _colorSpecTable[136] = 0xFF006868;
            _colorSpecTable[137] = 0xFF456868;
            _colorSpecTable[138] = 0xFF004F4F;
            _colorSpecTable[139] = 0xFF354F4F;
            _colorSpecTable[140] = 0xFF00BFFF;
            _colorSpecTable[141] = 0xFFAAEAFF;
            _colorSpecTable[142] = 0xFF008DBD;
            _colorSpecTable[143] = 0xFF7EADBD;
            _colorSpecTable[144] = 0xFF006081;
            _colorSpecTable[145] = 0xFF567681;
            _colorSpecTable[146] = 0xFF004E68;
            _colorSpecTable[147] = 0xFF455F68;
            _colorSpecTable[148] = 0xFF003B4F;
            _colorSpecTable[149] = 0xFF35494F;
            _colorSpecTable[150] = 0xFF007FFF;
            _colorSpecTable[151] = 0xFFAAD4FF;
            _colorSpecTable[152] = 0xFF005EBD;
            _colorSpecTable[153] = 0xFF7E9DBD;
            _colorSpecTable[154] = 0xFF004081;
            _colorSpecTable[155] = 0xFF566B81;
            _colorSpecTable[156] = 0xFF003468;
            _colorSpecTable[157] = 0xFF455668;
            _colorSpecTable[158] = 0xFF00274F;
            _colorSpecTable[159] = 0xFF35424F;
            _colorSpecTable[160] = 0xFF003FFF;
            _colorSpecTable[161] = 0xFFAABFFF;
            _colorSpecTable[162] = 0xFF002EBD;
            _colorSpecTable[163] = 0xFF7E8DBD;
            _colorSpecTable[164] = 0xFF001F81;
            _colorSpecTable[165] = 0xFF566081;
            _colorSpecTable[166] = 0xFF001968;
            _colorSpecTable[167] = 0xFF454E68;
            _colorSpecTable[168] = 0xFF00134F;
            _colorSpecTable[169] = 0xFF353B4F;
            _colorSpecTable[170] = 0xFF0000FF;
            _colorSpecTable[171] = 0xFFAAAAFF;
            _colorSpecTable[172] = 0xFF0000BD;
            _colorSpecTable[173] = 0xFF7E7EBD;
            _colorSpecTable[174] = 0xFF000081;
            _colorSpecTable[175] = 0xFF565681;
            _colorSpecTable[176] = 0xFF000068;
            _colorSpecTable[177] = 0xFF454568;
            _colorSpecTable[178] = 0xFF00004F;
            _colorSpecTable[179] = 0xFF35354F;
            _colorSpecTable[180] = 0xFF3F00FF;
            _colorSpecTable[181] = 0xFFBFAAFF;
            _colorSpecTable[182] = 0xFF2E00BD;
            _colorSpecTable[183] = 0xFF8D7EBD;
            _colorSpecTable[184] = 0xFF1F0081;
            _colorSpecTable[185] = 0xFF605681;
            _colorSpecTable[186] = 0xFF190068;
            _colorSpecTable[187] = 0xFF4E4568;
            _colorSpecTable[188] = 0xFF13004F;
            _colorSpecTable[189] = 0xFF3B354F;
            _colorSpecTable[190] = 0xFF7F00FF;
            _colorSpecTable[191] = 0xFFD4AAFF;
            _colorSpecTable[192] = 0xFF5E00BD;
            _colorSpecTable[193] = 0xFF9D7EBD;
            _colorSpecTable[194] = 0xFF400081;
            _colorSpecTable[195] = 0xFF6B5681;
            _colorSpecTable[196] = 0xFF340068;
            _colorSpecTable[197] = 0xFF564568;
            _colorSpecTable[198] = 0xFF27004F;
            _colorSpecTable[199] = 0xFF42354F;
            _colorSpecTable[200] = 0xFFBF00FF;
            _colorSpecTable[201] = 0xFFEAAAFF;
            _colorSpecTable[202] = 0xFF8D00BD;
            _colorSpecTable[203] = 0xFFAD7EBD;
            _colorSpecTable[204] = 0xFF600081;
            _colorSpecTable[205] = 0xFF765681;
            _colorSpecTable[206] = 0xFF4E0068;
            _colorSpecTable[207] = 0xFF5F4568;
            _colorSpecTable[208] = 0xFF3B004F;
            _colorSpecTable[209] = 0xFF49354F;
            _colorSpecTable[210] = 0xFFFF00FF;
            _colorSpecTable[211] = 0xFFFFAAFF;
            _colorSpecTable[212] = 0xFFBD00BD;
            _colorSpecTable[213] = 0xFFBD7EBD;
            _colorSpecTable[214] = 0xFF810081;
            _colorSpecTable[215] = 0xFF815681;
            _colorSpecTable[216] = 0xFF680068;
            _colorSpecTable[217] = 0xFF684568;
            _colorSpecTable[218] = 0xFF4F004F;
            _colorSpecTable[219] = 0xFF4F354F;
            _colorSpecTable[220] = 0xFFFF00BF;
            _colorSpecTable[221] = 0xFFFFAAEA;
            _colorSpecTable[222] = 0xFFBD008D;
            _colorSpecTable[223] = 0xFFBD7EAD;
            _colorSpecTable[224] = 0xFF810060;
            _colorSpecTable[225] = 0xFF815676;
            _colorSpecTable[226] = 0xFF68004E;
            _colorSpecTable[227] = 0xFF68455F;
            _colorSpecTable[228] = 0xFF4F003B;
            _colorSpecTable[229] = 0xFF4F3549;
            _colorSpecTable[230] = 0xFFFF007F;
            _colorSpecTable[231] = 0xFFFFAAD4;
            _colorSpecTable[232] = 0xFFBD005E;
            _colorSpecTable[233] = 0xFFBD7E9D;
            _colorSpecTable[234] = 0xFF810040;
            _colorSpecTable[235] = 0xFF81566B;
            _colorSpecTable[236] = 0xFF680034;
            _colorSpecTable[237] = 0xFF684556;
            _colorSpecTable[238] = 0xFF4F0027;
            _colorSpecTable[239] = 0xFF4F3542;
            _colorSpecTable[240] = 0xFFFF003F;
            _colorSpecTable[241] = 0xFFFFAABF;
            _colorSpecTable[242] = 0xFFBD002E;
            _colorSpecTable[243] = 0xFFBD7E8D;
            _colorSpecTable[244] = 0xFF81001F;
            _colorSpecTable[245] = 0xFF815660;
            _colorSpecTable[246] = 0xFF680019;
            _colorSpecTable[247] = 0xFF68454E;
            _colorSpecTable[248] = 0xFF4F0013;
            _colorSpecTable[249] = 0xFF4F353B;
            _colorSpecTable[250] = 0xFF333333;
            _colorSpecTable[251] = 0xFF505050;
            _colorSpecTable[252] = 0xFF696969;
            _colorSpecTable[253] = 0xFF828282;
            _colorSpecTable[254] = 0xFFBEBEBE;
            _colorSpecTable[255] = 0xFFFFFFFF;
        }
    }
}
