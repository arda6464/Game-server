
public class TeamInviteResponsePacket : IPacket
{
    public int TeamId { get; set; }
    public bool Accept { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TeamId = buffer.ReadVarInt(); // Takım ID'si
        Accept = buffer.ReadBool();
    }
}
