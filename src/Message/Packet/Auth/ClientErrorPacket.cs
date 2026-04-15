[PacketHandler(MessageType.ClientErrorRequest)]
public class ClientErrorPacket : IPacket
{
    public string? LogMessage { get; set; }
    public string? StackTrace { get; set; }
    public int LogType { get; set; }
    public int AccountId { get; set; }
    public string? SceneName { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        // İstemciden geldiği için Serialize gerekmez
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // Packet ID zaten okundu
        LogMessage = buffer.ReadVarString();
        StackTrace = buffer.ReadVarString();
        LogType = buffer.ReadVarInt();
        AccountId = buffer.ReadVarInt();
        SceneName = buffer.ReadVarString();
    }
}
