using Network;

public class UdpConnectionPacket : IPacket
{
   public int seqNo {get;set;}


    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((byte)UdpPacketFlags.Reliable);
        buffer.WriteVarInt(seqNo);
        buffer.WriteVarInt((byte)UdpMessageType.ConnectResponse);

    }

    public void Deserialize(ByteBuffer buffer)
    {
       
    }
}
