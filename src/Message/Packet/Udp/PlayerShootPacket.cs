using System.Numerics;
using DietPhysics;

public struct PlayerShootPacket : IPacket
{
    public int OwnerID { get; set; }
    public int GunID { get; set; }
    public byte aimbyte { get; set; }
    public int BulletId { get; set; } // Response için
    public Vec3 StartPos { get; set; }
    public int RemaningAmmo { get; set; }




    public void Serialize(ByteBuffer buffer)
    {


        // Payload
        buffer.WriteVarInt((int)UdpMessageType.Shoot);
        buffer.WriteVarInt(OwnerID);
        buffer.WriteVarInt(BulletId);
        buffer.WriteVarInt(GunID);
        buffer.WriteByte(aimbyte);
        buffer.WriteVarInt(RemaningAmmo);
        buffer.WriteFloat(StartPos.x);
        buffer.WriteFloat(StartPos.z);



    }


    public void Deserialize(ByteBuffer buffer)
    {

    }

}
