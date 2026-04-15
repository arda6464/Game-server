[PacketHandler(MessageType.LeaveTeamRequest)]
public class LeaveTeamRequestPacket : IPacket
{
    public int Type { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        Type = buffer.ReadVarInt(); 
    }
}
