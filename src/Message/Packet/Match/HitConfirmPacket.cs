public class HitConfirmPacket : IPacket
{

    public int TargetID { get; set; }
    public int Damage { get; set; }
    public bool Shield { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.HitConfirm);
        buffer.WriteVarInt(TargetID);
        buffer.WriteVarInt(Damage);
        buffer.WriteBool(Shield);

    }

    public void Deserialize(ByteBuffer buffer)
    {

    }
}
