public class FriendRemovedPacket : IPacket
{
    public int TargetId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.DeleteFriendResponse);
        buffer.WriteVarInt(TargetId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
