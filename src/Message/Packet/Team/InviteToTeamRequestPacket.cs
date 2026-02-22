[PacketHandler(MessageType.InviteToTeamRequest)]
public class InviteToTeamRequestPacket : IPacket
{
    public string TargetAccountId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TargetAccountId = buffer.ReadString();
    }
}
