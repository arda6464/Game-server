
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

public class FirstConnectionResponsePacket : IPacket
{
    public bool Success { get; set; }
    public string Message { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.FirstConnectionResponse);
        buffer.WriteBool(Success);
        buffer.WriteVarString(Message);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
