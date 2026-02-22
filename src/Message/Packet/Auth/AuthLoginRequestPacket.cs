[PacketHandler(MessageType.AuthLoginRequest)]
public class AuthLoginRequestPacket : IPacket
{
    public string ClientVersion { get; set; }
    public string Token { get; set; }
    public string AccountId { get; set; }
    public string Language { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        // İstemciden geldiği için Serialize gerekmez
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // Packet ID zaten okundu
        ClientVersion = buffer.ReadString();
        Token = buffer.ReadString();
        AccountId = buffer.ReadString();
        Language = buffer.ReadString();
    }
}
