public class PingPacket : IPacket
{
    public float ClientSentTime { get; set; }
    public int LastPing { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // İlk short (Packet ID) zaten okundu
        ClientSentTime = buffer.ReadFloat();
        
    }
}
