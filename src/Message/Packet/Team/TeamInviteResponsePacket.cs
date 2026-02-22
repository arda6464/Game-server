[PacketHandler(MessageType.InviteToTeamResponse)]
public class TeamInviteResponsePacket : IPacket
{
    public string InviterAccountId { get; set; }
    public bool Accept { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        InviterAccountId = buffer.ReadString();
        Accept = buffer.ReadBool();
    }
}
