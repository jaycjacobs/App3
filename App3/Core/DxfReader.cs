using System;
using System.Collections.Generic;
using System.IO;

namespace Cirros.Dxf
{
    public class DxfReader : StringReader
    {
        protected DxfDocument _dxfDoc = new DxfDocument();

        public DxfReader(string s)
            : base(s)
        {
        }

        bool EndOfStream = false;

        public DxfDocument Document
        {
            get
            {
                return _dxfDoc;
            }
        }

        public void ReadDocument()
        {
            do
            {
                if (ReadSection() == "EOF")
                {
                    break;
                }
            }
            while (EndOfStream == false);
        }

        protected string ReadSection()
        {
            string section = "UNKNOWN";

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "SECTION")
                    {
                        g = ReadGroup();
                        if (g.Code == 2)
                        {
                            section = g.Value;
                            //System.Diagnostics.Debug.WriteLine("DXF Section: {0}", g.Value);

                            if (section == "HEADER")
                            {
                                ReadHeader();
                            }
                            else if (section == "TABLES")
                            {
                                ReadTables();
                            }
                            else if (section == "BLOCKS")
                            {
                                ReadBlocks();
                            }
                            else if (section == "ENTITIES")
                            {
                                ReadEntities();
                            }
                            else if (section == "CLASSES")
                            {
                                ReadSection(section);
                            }
                            else if (section == "OBJECTS")
                            {
                                ReadSection(section);
                            }
                            else
                            {
#if DEBUG
                                string msg = string.Format("DXF parse: Unexpected SECTION: {0}", section);
                                System.Diagnostics.Debug.WriteLine(msg);
#endif
                                ReadSection(section);
                            }
                        }
                        else
                        {
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected group in section definition");
#endif
                        }
                    }
                    else if (g.Value == "ENDSEC")
                    {
                        break;
                    }
                    else if (g.Value == "EOF")
                    {
                        section = "EOF";
                        break;
                    }
                }
                else
                {
#if DEBUG
                    string msg = string.Format("DXF parse: Unexpected top-level group: {0}, {1}", g.Code, g.Value);
                    System.Diagnostics.Debug.WriteLine(msg);
#endif
                }
            }
            while (EndOfStream == false);

            return section;
        }

        protected void ReadHeader()
        {
            string headerKey = "unassigned";

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "ENDSEC")
                    {
                        break;
                    }
                    else
                    {
#if DEBUG
                        string msg = string.Format("DXF parse: Unexpected top-level group in HEADER: {0}, {1}", g.Code, g.Value);
                        System.Diagnostics.Debug.WriteLine(msg);
#endif
                    }
                }
                else if (g.Code == 9)
                {
                    headerKey = g.Value;
                }
                else if (g.Code >= 0 && g.Code <= 9)
                {
                    //_dxfDoc.HEADERCollection.Add(headerKey, g.Value);
                    _dxfDoc.HEADERCollection[headerKey] = g.Value;
                }
                else if (g.Code == 10 || g.Code == 20 || g.Code == 30)
                {
                    DxfPoint3 p = new DxfPoint3();
                    do
                    {
                        if (g.Code == 10)
                        {
                            p.X = float.Parse(g.Value);
                        }
                        else if (g.Code == 20)
                        {
                            p.Y = float.Parse(g.Value);
                        }
                        else if (g.Code == 30)
                        {
                            p.Z = float.Parse(g.Value);
                        }
                        else
                        {
                            PushGroup(g);
                            break;
                        }

                        g = ReadGroup();
                    }
                    while (EndOfStream == false);

                    _dxfDoc.HEADERCollection.Add(headerKey, p);
                }
                else if (g.Code == 40 || g.Code == 50)
                {
                    float floatValue = float.Parse(g.Value);
                    _dxfDoc.HEADERCollection.Add(headerKey, floatValue);
                }
                else if (g.Code == 62 || g.Code == 70)
                {
                    int intValue = int.Parse(g.Value);
                    _dxfDoc.HEADERCollection.Add(headerKey, intValue);
                }
                else if (g.Code >= 280 && g.Code <= 290)
                {
                    int intValue = int.Parse(g.Value);
                    _dxfDoc.HEADERCollection.Add(headerKey, intValue);
                }
                else if (g.Code == 370 || g.Code == 380)
                {
                    int intValue = int.Parse(g.Value);
                    _dxfDoc.HEADERCollection.Add(headerKey, intValue);
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("DXF parse: Unknown g.Code=" + g.Code);
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadTables()
        {
            string table = "UNKNOWN";

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "TABLE")
                    {
                        g = ReadGroup();
                        if (g.Code == 2)
                        {
                            table = g.Value;
                            //System.Diagnostics.Debug.WriteLine("DXF Table: {0}", g.Value);

                            if (table == "LTYPE")
                            {
                                ReadLtypeTable();
                            }
                            else if (table == "LAYER")
                            {
                                ReadLayerTable();
                            }
                            else if (table == "STYLE")
                            {
                                ReadStyleTable();
                            }
                            else if (table == "VIEW")
                            {
                                ReadViewTable();
                            }
                            else if (table == "VPORT")
                            {
                                ReadVPortTable();
                            }
                            else if (table == "UCS")
                            {
                                ReadUcsTable();
                            }
                            else if (table == "APPID")
                            {
                                ReadTable();
                            }
                            else if (table == "DIMSTYLE")
                            {
                                ReadDimStyleTable();
                            }
                            else if (table == "BLOCK_RECORD")
                            {
                                ReadBlockRecordTable();
                            }
                            else
                            {
                                //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected table: " + g.Value);
                            }
                        }
                    }
                    else if (g.Value == "ENDSEC")
                    {
                        break;
                    }
                    else if (g.Value == "EOF")
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected EOF in TABLE section");
                    }
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadTable()
        {
            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "LTYPE")
                    {
                        //cool
                    }
                    else if (g.Value == "LAYER")
                    {
                        //cool
                    }
                    else if (g.Value == "STYLE")
                    {
                        //cool
                    }
                    else if (g.Value == "VPORT")
                    {
                        //cool
                    }
                    else if (g.Value == "APPID")
                    {
                        //cool
                    }
                    else if (g.Value == "DIMSTYLE")
                    {
                        //cool
                    }
                    else if (g.Value == "BLOCK_RECORD")
                    {
                        //cool
                    }
                    else if (g.Value == "ENDTAB")
                    {
                        break;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected section in TABLE: " + g.Value);
                    }
                }
                else if (g.Code == 102)
                {
                    PushGroup(g);
                    List<DxfGroup> list = Read102Group();
                }
                else if (g.Code == 1002)
                {
                    PushGroup(g);
                    List<DxfGroup> list = Read1002Group();
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadSection(string section)
        {
            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "ENDSEC")
                    {
                        break;
                    }
                    else
                    {
                        // Handle top-level groups
                    }
                }
                else if (g.Code == 102)
                {
                    PushGroup(g);
                    List<DxfGroup> list = Read102Group();
                }
                else if (g.Code == 1002)
                {
                    PushGroup(g);
                    List<DxfGroup> list = Read1002Group();
                }
                else
                {
                    // Handle groups
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadVPortTable()
        {
            DxfVport vport = new DxfVport(this);

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "VPORT")
                    {
                        vport = new DxfVport(this);
                        _dxfDoc.VPORTList.Add(vport);
                    }
                    else if (g.Value == "ENDTAB")
                    {
                        break;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected section in VPORT: " + g.Value);
                    }
                }
                else
                {
                    vport.SetGroup(g);
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadBlockRecordTable()
        {
            DxfBlockRecord record = new DxfBlockRecord(this);

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "BLOCK_RECORD")
                    {
                        if (string.IsNullOrEmpty(record.Name) == false)
                        {
                            _dxfDoc.BLOCKRECORDList.Add(DxfExport.FixAcName(record.Name), record);
                        }
                        record = new DxfBlockRecord(this);
                    }
                    else if (g.Value == "ENDTAB")
                    {
                        if (string.IsNullOrEmpty(record.Name) == false)
                        {
                            _dxfDoc.BLOCKRECORDList.Add(DxfExport.FixAcName(record.Name), record);
                        }
                        break;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected section in BLOCK_RECORD table: " + g.Value);
                    }
                }
                else
                {
                    record.SetGroup(g);
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadUcsTable()
        {
            DxfUcs ucs = new DxfUcs(this);

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "UCS")
                    {
                        ucs = new DxfUcs(this);
                        _dxfDoc.UCSList.Add(ucs);
                    }
                    else if (g.Value == "ENDTAB")
                    {
                        break;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected section in UCS table: " + g.Value);
                    }
                }
                else
                {
                    ucs.SetGroup(g);
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadViewTable()
        {
            DxfView view = new DxfView(this);

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "VIEW")
                    {
                        view = new DxfView(this);
                        _dxfDoc.VIEWList.Add(view);
                    }
                    else if (g.Value == "ENDTAB")
                    {
                        break;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected section in VIEW table: " + g.Value);
                    }
                }
                else
                {
                    view.SetGroup(g);
                }
            }
            while (EndOfStream == false);
        }

        private void AddLTYPE(DxfLtype ltype)
        {
            if (string.IsNullOrEmpty(ltype.Name) == false)
            {
                string name = DxfExport.FixAcName(ltype.Name);
                if (_dxfDoc.LTYPEList.ContainsKey(name))
                {
                    _dxfDoc.LTYPEList[name] = ltype;
                }
                else
                {
                    _dxfDoc.LTYPEList.Add(name, ltype);
                }
            }
        }

        private void AddLAYER(DxfLayer layer)
        {
            if (string.IsNullOrEmpty(layer.Name) == false)
            {
                string name = DxfExport.FixAcName(layer.Name);
                if (_dxfDoc.LTYPEList.ContainsKey(name))
                {
                    _dxfDoc.LAYERList[name] = layer;
                }
                else
                {
                    _dxfDoc.LAYERList.Add(name, layer);
                }
            }
        }

        private void AddSTYLE(DxfStyle style)
        {
            if (string.IsNullOrEmpty(style.Name) == false)
            {
                string name = DxfExport.FixAcName(style.Name);
                if (_dxfDoc.STYLEList.ContainsKey(name))
                {
                    _dxfDoc.STYLEList[name] = style;
                }
                else
                {
                    _dxfDoc.STYLEList.Add(name, style);
                }
            }
        }

        private void AddDIMSTYLE(DxfDimstyle style)
        {
            if (string.IsNullOrEmpty(style.Name) == false)
            {
                string name = DxfExport.FixAcName(style.Name);
                if (_dxfDoc.DIMSTYLEList.ContainsKey(name))
                {
                    _dxfDoc.DIMSTYLEList[name] = style;
                }
                else
                {
                    _dxfDoc.DIMSTYLEList.Add(name, style);
                }
            }
        }

        protected void ReadLtypeTable()
        {
            DxfLtype ltype = new DxfLtype(this);

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "LTYPE")
                    {
                        AddLTYPE(ltype);
                        ltype = new DxfLtype(this);
                    }
                    else if (g.Value == "ENDTAB")
                    {
                        AddLTYPE(ltype);
                        break;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected section in LTYPE table: " + g.Value);
                    }
                }
                else
                {
                    ltype.SetGroup(g);
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadLayerTable()
        {
            DxfLayer layer = new DxfLayer(this);

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "LAYER")
                    {
                        AddLAYER(layer);
                        //if (string.IsNullOrEmpty(layer.Name) == false)
                        //{
                        //    _dxfDoc.LAYERList.Add(layer.Name.ToUpper(), layer);
                        //}
                        layer = new DxfLayer(this);
                    }
                    else if (g.Value == "ENDTAB")
                    {
                        AddLAYER(layer);
                        //if (string.IsNullOrEmpty(layer.Name) == false)
                        //{
                        //    _dxfDoc.LAYERList.Add(layer.Name.ToUpper(), layer);
                        //}
                        break;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected section in LAYER table: " + g.Value);
                    }
                }
                else if (g.Code == 102)
                {
                    PushGroup(g);
                    List<DxfGroup> list = Read102Group();
                }
                else if (g.Code == 1002)
                {
                    PushGroup(g);
                    List<DxfGroup> list = Read1002Group();
                }
                else
                {
                    layer.SetGroup(g);
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadStyleTable()
        {
            DxfStyle style = new DxfStyle(this);

            do
            {
                DxfGroup g = ReadGroup();
                if (g.Code == 0)
                {
                    if (g.Value == "STYLE")
                    {
                        AddSTYLE(style);
                        //if (string.IsNullOrEmpty(style.Name) == false)
                        //{
                        //    _dxfDoc.STYLEList.Add(style.Name.ToUpper(), style);
                        //}
                        style = new DxfStyle(this);
                    }
                    else if (g.Value == "ENDTAB")
                    {
                        AddSTYLE(style);
                        //if (string.IsNullOrEmpty(style.Name) == false)
                        //{
                        //    _dxfDoc.STYLEList.Add(style.Name.ToUpper(), style);
                        //}
                        break;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected section in STYLE table: " + g.Value);
                    }
                }
                else
                {
                    style.SetGroup(g);
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadDimStyleTable()
        {
            DxfDimstyle style = new DxfDimstyle(this);

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "DIMSTYLE")
                    {
                        AddDIMSTYLE(style);
                        //if (string.IsNullOrEmpty(style.Name) == false)
                        //{
                        //    _dxfDoc.DIMSTYLEList.Add(style.Name.ToUpper(), style);
                        //}
                        style = new DxfDimstyle(this);
                    }
                    else if (g.Value == "ENDTAB")
                    {
                        AddDIMSTYLE(style);
                        //if (string.IsNullOrEmpty(style.Name) == false)
                        //{
                        //    _dxfDoc.DIMSTYLEList.Add(style.Name.ToUpper(), style);
                        //}
                        break;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected section in DIMSTYLE table: " + g.Value);
                    }
                }
                else
                {
                    style.SetGroup(g);
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadBlocks()
        {
            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "BLOCK")
                    {
                        DxfBlock block = ReadBlock();
                        _dxfDoc.BLOCKList.Add(block.Name.ToUpper(), block);
                    }
                    else if (g.Value == "ENDSEC")
                    {
                        break;
                    }
                    else if (g.Value == "EOF")
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected EOF in BLOCKS section");
                    }
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadEntities()
        {
            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "ENDSEC")
                    {
                        break;
                    }
                    else if (g.Value == "EOF")
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected EOF in ENTITIES section");
                    }
                    else
                    {
                        PushGroup(g);
                        ReadEntities(_dxfDoc.ENTITYList);
                    }
                }
            }
            while (EndOfStream == false);
        }

        protected DxfBlock ReadBlock()
        {
            DxfBlock block = new DxfBlock(this);

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "ENDBLK")
                    {
                        break;
                    }
                    else if (g.Value == "EOF")
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected EOF in BLOCK section");
                    }
                    else
                    {
                        PushGroup(g);
                        ReadEntities(block.Entities);
                    }
                }
                else if (g.Code == 102)
                {
                    PushGroup(g);
                    List<DxfGroup> list = Read102Group();
                }
                else if (g.Code == 1002)
                {
                    PushGroup(g);
                    List<DxfGroup> list = Read1002Group();
                }
                else
                {
                    block.SetGroup(g);
                }
            }
            while (EndOfStream == false);

            return block;
        }

        protected void ReadEntities(List<DxfEntity> entities)
        {
            DxfEntity entity = new DxfEntity(this);

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "ENDBLK")
                    {
                        PushGroup(g);
                        break;
                    }
                    else if (g.Value == "ENDSEC")
                    {
                        PushGroup(g);
                        break;
                    }
                    else if (g.Value == "EOF")
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected EOF in entity section");
                    }

                    switch (g.Value)
                    {
                        case "LINE":
                            entity = new DxfLineEntity(this);
                            break;
                        case "CIRCLE":
                            entity = new DxfCircleEntity(this);
                            break;
                        case "ARC":
                            entity = new DxfArcEntity(this);
                            break;
                        case "TEXT":
                            entity = new DxfTextEntity(this);
                            break;
                        case "MTEXT":
                            entity = new DxfMTextEntity(this);
                            break;
                        case "POINT":
                            entity = new DxfPointEntity(this);
                            break;
                        case "POLYLINE":
                            entity = new DxfPolylineEntity(this);
                            break;
                        case "DIMENSION":
                            entity = new DxfDimensionEntity(this);
                            break;
                        case "ATTDEF":
                            entity = new DxfAttdefEntity(this);
                            break;
                        case "ATTRIB":
                            if (entity is DxfInsertEntity)
                            {
                                PushGroup(g);
                                ReadAttribList(((DxfInsertEntity)entity).Attributes);
                            }
                            entity = new DxfEntity(this);
                            continue;
                        case "VERTEX":
                            if (entity is DxfPolylineEntity)
                            {
                                PushGroup(g);
                                ReadVertexList(((DxfPolylineEntity)entity).VertexList);
                            }
                            // In the test files, the SEQEND entity has a layer and ltype value
                            // the documentation implies it should have no attributes
                            // To prevent these attributes from overwriting the current entity (polyline) attributes
                            // I'm creating a dummy entity that is not added to the entity list
                            entity = new DxfEntity(this);
                            continue;
                        case "INSERT":
                            entity = new DxfInsertEntity(this);
                            break;
                        case "SOLID":
                            entity = new DxfSolidEntity(this);
                            break;
                        case "VIEWPORT":
                            entity = new DxfViewportEntity(this);
                            break;
                        case "LWPOLYLINE":
                            entity = new DxfLwpolylineEntity(this);
                            break;
                        case "SPLINE":
                            entity = new DxfSplineEntity(this);
                            break;
                        case "LEADER":
                            entity = new DxfLeaderEntity(this);
                            break;
                        case "ELLIPSE":
                            entity = new DxfEllipseEntity(this);
                            break;
                        case "HATCH":
                            entity = new DxfHatchEntity(this);
                            break;
                        case "TRACE":
                            entity = new DxfTraceEntity(this);
                            break;
                        case "3DFACE":
                            entity = new Dxf3DFaceEntity(this);
                            break;
                        default:
                            entity = new DxfEntity(this);
                            entity.Type = g.Value;
                            break;
                    }
                    entities.Add(entity);
                }
                else if (g.Code == 102)
                {
                    PushGroup(g);
                    List<DxfGroup> list = Read102Group();
                }
                //else if (g.Code == 1002)
                //{
                //    PushGroup(g);
                //    List<DxfGroup> list = Read1002Group();
                //}
                else
                {
                    entity.SetValue(g);
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadVertexList(List<DxfVertexEntity> vertexList)
        {
            DxfVertexEntity vertex = new DxfVertexEntity(this);

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "SEQEND")
                    {
                        break;
                    }
                    else if (g.Value == "VERTEX")
                    {
                        vertex = new DxfVertexEntity(this);
                        vertexList.Add(vertex);
                    }
                    else if (g.Value == "EOF")
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected EOF in VERTEX list");
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected top level group in VERTEX list");
                    }
                }
                else
                {
                    vertex.SetValue(g);
                }
            }
            while (EndOfStream == false);
        }

        protected void ReadAttribList(List<DxfAttribEntity> attribList)
        {
            DxfAttribEntity attrib = new DxfAttribEntity(this);

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 0)
                {
                    if (g.Value == "SEQEND")
                    {
                        break;
                    }
                    else if (g.Value == "ATTRIB")
                    {
                        attrib = new DxfAttribEntity(this);
                        attribList.Add(attrib);
                    }
                    else if (g.Value == "EOF")
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected EOF in ATTRIB list");
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected top level group in ATTRIB list");
                    }
                }
                else
                {
                    attrib.SetValue(g);
                }
            }
            while (EndOfStream == false);
        }

        public List<DxfGroup> Read102Group()
        {
            bool open = false;

            List<DxfGroup> list = new List<DxfGroup>();

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 102)
                {
                    if (open)
                    {
                        if (g.Value.Trim() == "}")
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (g.Value.StartsWith("{"))
                        {
                            open = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    list.Add(g);
                }
            }
            while (EndOfStream == false);

            return list;
        }

        public List<DxfGroup> Read1002Group()
        {
            //bool open = false;
            int nest = 0;

            List<DxfGroup> list = new List<DxfGroup>();

            do
            {
                DxfGroup g = ReadGroup();

                if (g.Code == 1002)
                {
                    string v = g.Value.Trim();
                    if (v == "{")
                    {
                        nest++;
                    }
                    else if (v == "}")
                    {
                        if (--nest == 0)
                        {
                            break;
                        }
                    }
                    //if (open)
                    //{
                    //    if (g.Value.Trim() == "}")
                    //    {
                    //        break;
                    //    }
                    //}
                    //else
                    //{
                    //    if (g.Value.StartsWith("{"))
                    //    {
                    //        open = true;
                    //    }
                    //    else
                    //    {
                    //        break;
                    //    }
                    //}
                }
                else
                {
                    list.Add(g);
                }
            }
            while (EndOfStream == false);

            return list;
        }

        private bool _pushGroup = false;
        private int _pushGroupCode;
        private string _pushGroupValue;

        public void PushGroup(DxfGroup group)
        {
            _pushGroupCode = group.Code;
            _pushGroupValue = group.Value;
            _pushGroup = true;
        }

        int _line = 0;

        public DxfGroup ReadGroup()
        {
            DxfGroup g = new DxfGroup();

            if (_pushGroup)
            {
                g.Code = _pushGroupCode;
                g.Value = _pushGroupValue;
                _pushGroup = false;
            }
            else
            {
                string groupCodeString = ReadLine();
                _line++;

                EndOfStream = string.IsNullOrEmpty(groupCodeString);

                if (EndOfStream)
                {
                    //System.Diagnostics.Debug.WriteLine("DXF parse: Unexpected EndOfStream while reading group");
                    g.Code = 0;
                    g.Value = "EOF";
                }
                else
                {
                    g.Code = int.Parse(groupCodeString);
                    g.Value = ReadLine();
                    _line++;
                }
            }

            return g;
        }
    }
}
