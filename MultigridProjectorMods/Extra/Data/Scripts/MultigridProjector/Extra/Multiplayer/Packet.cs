using ProtoBuf;

namespace MultigridProjector.Extra
{
    [ProtoContract]
    public struct Packet
    {
        [ProtoMember(4)]
        public int magic;

        [ProtoMember(5)]
        public byte[] payload;
    }
}