[PacketHandler(MessageType.AuthLoginRequest)]
public class AuthLoginRequestPacket : IPacket
{
    public string ClientVersion { get; set; }
    public string Token { get; set; }
    public int ID { get; set; }
    public string Language { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        // İstemciden geldiği için Serialize gerekmez
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // Packet ID zaten okundu
        ClientVersion = buffer.ReadVarString();
        Token = buffer.ReadVarString();
        ID = buffer.ReadVarInt();
        Language = buffer.ReadVarString();
    }
}
