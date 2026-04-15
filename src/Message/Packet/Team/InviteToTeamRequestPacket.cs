[PacketHandler(MessageType.InviteToTeamRequest)]
public class InviteToTeamRequestPacket : IPacket
{
    public int TargetId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TargetId = buffer.ReadVarInt();
    }
}
