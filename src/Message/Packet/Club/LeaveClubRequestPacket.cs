[PacketHandler(MessageType.LeaveClubRequest)]
public class LeaveClubRequestPacket : IPacket
{
    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // No data to read
    }
}
