[PacketHandler(MessageType.InviteToTeamResponse)]
public class TeamInviteResponsePacket : IPacket
{
    public int InviterId { get; set; }
    public bool Accept { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        InviterId = buffer.ReadVarInt();
        Accept = buffer.ReadBool();
    }
}
