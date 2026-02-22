public class SendVerifyCodePacket : IPacket
{
    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.SendVerifyCode);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
