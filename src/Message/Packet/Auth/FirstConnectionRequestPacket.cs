[PacketHandler(MessageType.FirstConnectionRequest)]
public class FirstConnectionRequestPacket : IPacket
{
    public string DeviceName { get; set; }
    public string DeviceModel { get; set; }
    public string ClientKey { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        DeviceName = buffer.ReadVarString();
        DeviceModel = buffer.ReadVarString();
        ClientKey = buffer.ReadVarString();
    }
}
