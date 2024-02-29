using Cirros.Primitives;
using System;

namespace Cirros.Drawing
{
    public class SymbolHeader
    {
        int _version = 3;

        private Unit _paperUnit;
        private Unit _modelUnit;
        private double _scale;
        private string _name;
        private Guid _state;
        //private Guid _libraryId;
        private Guid _id;

        public SymbolHeader()
        {
        }

        public Guid Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                IdSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool IdSpecified = false;

        //public string LibraryName
        //{
        //    get
        //    {
        //        return _name;
        //    }
        //    set
        //    {
        //        _name = value;
        //        LibraryNameSpecified = true;
        //    }
        //}

        //[System.Xml.Serialization.XmlIgnoreAttribute]
        //public bool LibraryNameSpecified = false;

        //public Guid LibraryId
        //{
        //    get
        //    {
        //        return _libraryId;
        //    }
        //    set
        //    {
        //        _libraryId = value;
        //        LibraryIdSpecified = true;
        //    }
        //}

        //[System.Xml.Serialization.XmlIgnoreAttribute]
        //public bool LibraryIdSpecified = false;

        public string SymbolName
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                SymbolNameSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool SymbolNameSpecified = false;

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
                DescriptionSpecified = true;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool DescriptionSpecified = false;

        public int Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
            }
        }

        public Unit PaperUnit
        {
            get
            {
                return _paperUnit;
            }
            set
            {
                _paperUnit = value;
            }
        }

        public Unit ModelUnit
        {
            get
            {
                return _modelUnit;
            }
            set
            {
                _modelUnit = value;
            }
        }

        public double ModelScale
        {
            get
            {
                return _scale;
            }
            set
            {
                _scale = value;
            }
        }

        public Guid State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }

        public CoordinateSpace CoordinateSpace
        {
            get { return _coordinateSpace; }
            set { _coordinateSpace = value; }
        }

        private CoordinateSpace _coordinateSpace = CoordinateSpace.Drawing;

        private string _thumbnail = "";
        private string _description = "";

        public string Thumbnail
        {
            get
            {
                // on save
                // if the thumbnail exists, base64 encode it and return
                // otherwise, return an empty string
                return _thumbnail;
            }
            set
            {
                // on load
                // if the thumbnail exists in the thumbnailfolder, do nothing
                // if the serialized exists, base64 decoode and save
                // otherwise do nothing
                _thumbnail = value;
            }
        }
    }
}
