[PacketHandler(MessageType.VerifyCodeResponse)]
public class VerifyCodeRequestPacket : IPacket
{
    public int Code { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        Code = buffer.ReadVarInt();
    }
}
