public class GenerateInviteLinkResponsePacket : IPacket
{
    public string Token { get; set; } = string.Empty;
    public string FullUrl { get; set; } = string.Empty;

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarString(Token);
        buffer.WriteVarString(FullUrl);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
