public class NewAccountCreateResponsePacket : IPacket
{
    public string Token { get; set; }
    public string AccountId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.NewAccountCreateResponse);
        buffer.WriteString(Token);
        buffer.WriteString(AccountId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
