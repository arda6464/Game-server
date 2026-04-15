[PacketHandler(MessageType.ClubShowRequest)]
public class ClubShowRequestPacket : IPacket
{
    public int ClubId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        ClubId = buffer.ReadVarInt();
    }
}
