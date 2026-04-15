[PacketHandler(MessageType.ChangeNameColorRequest)]
public class SetNameColorRequestPacket : IPacket
{
    public int ColorId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        ColorId = buffer.ReadVarInt();
    }
}
