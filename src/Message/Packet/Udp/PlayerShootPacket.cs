using System.Numerics;

public class PlayerShootPacket : IPacket
{
    public int SequenceNumber { get; set; }
    public int OwnerID { get; set; }
    public float DirectionX { get; set; }
    public float DirectionY { get; set; }
    public int BulletId { get; set; } // Response için

    public int ConnectionToken { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        // Sadece Client -> Server (Token varsa) gönderiyoruz. 
        if (ConnectionToken != 0)
        {
            buffer.WriteVarInt(ConnectionToken);
        }

        // Payload
        buffer.WriteByte((byte)UdpMessageType.Shoot);
        buffer.WriteVarInt(OwnerID);
        buffer.WriteFloat(DirectionX);
        buffer.WriteFloat(DirectionY);
        buffer.WriteVarInt(BulletId);
    }


    public void Deserialize(ByteBuffer buffer)
    {
        // Header UdpServer tarafında ayıklandı.
        OwnerID = buffer.ReadVarInt();
        DirectionX = buffer.ReadFloat();
        DirectionY = buffer.ReadFloat();
        BulletId = buffer.ReadVarInt();
    }

}
