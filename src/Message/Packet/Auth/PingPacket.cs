public class PingPacket : IPacket
{
    public double ClientSentTime { get; set; }
    public int LastPing { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        // İstemciden sunucuya gönderilen bir paket olduğu için Serialize gerekmez (şimdilik)
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // İlk short (Packet ID) zaten okundu
        ClientSentTime = buffer.ReadDouble();
        LastPing = buffer.ReadInt();
    }
}
