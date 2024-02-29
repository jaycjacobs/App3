using Cirros.Primitives;
using System;
#if UWP
#else
using CirrosCore;
#endif

namespace Cirros.Core.Primitives
{
    public class WallJoint : IComparable<WallJoint>
    {
        uint _fromId;
        uint _toId;
        uint _fromVertex;
        uint _toSegment;

        private PDoubleline _fromDb;
        private PDoubleline _toDb;

        public WallJoint(uint fromId, uint fromVertex, uint toId, uint toSegment)
        {
            _fromId = fromId;
            _fromDb = Globals.ActiveDrawing.FindObjectById(_fromId) as PDoubleline;
            _fromVertex = fromVertex;

            _toId = toId;
            _toDb = Globals.ActiveDrawing.FindObjectById(_toId) as PDoubleline;
            _toSegment = toSegment;
        }

        public PDoubleline ToDb
        {
            get { return _toDb; }
        }

        public PDoubleline FromDb
        {
            get { return _fromDb; }
        }

        public uint FromId
        {
            get { return _fromId; }
        }

        public uint ToId
        {
            get { return _toId; }
        }

        public uint FromVertex
        {
            get { return _fromVertex; }
        }

        public uint ToSegment
        {
            get { return _toSegment; }
        }

        public int CompareTo(WallJoint other)
        {
            if (other == null)
            {
                return 1;
            }
            else if (other.ToId == this.ToId)
            {
                return this.ToSegment.CompareTo(other.ToSegment);
            }
            return this.ToId.CompareTo(other.ToId);
        }
    }
}
