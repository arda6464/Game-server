public class SendVerifyCodePacket : IPacket
{
    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.SendVerifyCode);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
