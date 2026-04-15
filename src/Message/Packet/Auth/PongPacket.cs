public class PongPacket : IPacket
{
       public float ClientSentTime { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.Pong);
        buffer.WriteFloat(ClientSentTime);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // Sunucudan istemciye giden bir paket olduğu için Deserialize gerekmez
        throw new NotImplementedException();
    }
}
