using System.Numerics;

public class PlayerShootPacket : IPacket
{
    public ushort SequenceNumber { get; set; }
    public string OwnerId { get; set; }
    public float DirectionX { get; set; }
    public float DirectionY { get; set; }
    public int BulletId { get; set; } // Response için

    public int ConnectionToken { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        // 1. UDP Header (Flags + Sequence)
        buffer.WriteByte((byte)Network.UdpPacketFlags.Reliable); // Reliable
        buffer.WriteUShort(SequenceNumber);
        
        // Sadece Client -> Server (Token varsa) gönderiyoruz. 
        // Sunucu yayınlarında (Broadcast) Token gönderilmez (Optimizasyon).
        if (ConnectionToken != 0)
        {
            buffer.WriteInt(ConnectionToken);
        }

        // 2. Payload
        buffer.WriteShort((short)MessageType.UdpShoot);
        buffer.WriteString(OwnerId);
        buffer.WriteFloat(DirectionX);
        buffer.WriteFloat(DirectionY);
        buffer.WriteInt(BulletId);
    }


    public void Deserialize(ByteBuffer buffer)
    {
        // Header UdpServer tarafında ayıklandı.
        OwnerId = buffer.ReadString();
        DirectionX = buffer.ReadFloat();
        DirectionY = buffer.ReadFloat();
        BulletId = buffer.ReadInt();
    }

}
