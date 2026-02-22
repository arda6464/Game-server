public class UdpConnectionPacket : IPacket
{
    public int ConnectionToken { get; set; }
    public string Username { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.UdpConnect);
       
    }

    public void Deserialize(ByteBuffer buffer)
    {
        ConnectionToken = buffer.ReadInt();
        Username = buffer.ReadString();
    }
}
