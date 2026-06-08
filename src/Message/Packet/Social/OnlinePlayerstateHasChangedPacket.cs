public class OnlinePlayerstateHasChangedPacket : IPacket
{
    public int PlayerId { get; set; }
    public bool LookingForTeam { get; set; }
    public bool DisturbMode { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.OnlinePlayerStateChanged);
        buffer.WriteVarInt(PlayerId);
        buffer.WriteBool(LookingForTeam);
        buffer.WriteBool(DisturbMode);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    
    }
}