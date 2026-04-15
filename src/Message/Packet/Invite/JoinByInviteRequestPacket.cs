[PacketHandler(MessageType.JoinByInviteRequest)]
public class JoinByInviteRequestPacket : IPacket
{
    public string Token { get; set; } = string.Empty;

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        Token = buffer.ReadVarString();
    }
}
