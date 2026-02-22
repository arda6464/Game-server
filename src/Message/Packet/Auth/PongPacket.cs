public class PongPacket : IPacket
{
       public double ClientSentTime { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.Pong);
        buffer.WriteDouble(ClientSentTime);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // Sunucudan istemciye giden bir paket olduğu için Deserialize gerekmez
        throw new NotImplementedException();
    }
}
