public class PlayerHealthUpdatePacket : IPacket
{
    public string PlayerId { get; set; }
    public int Health { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.PlayerHealthUpdate);
        buffer.WriteString(PlayerId);
        buffer.WriteInt(Health);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
