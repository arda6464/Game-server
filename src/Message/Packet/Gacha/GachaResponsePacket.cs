using GachaSystem;
using System.Collections.Generic;

[PacketHandler(MessageType.GachaResponse)]
public class GachaResponsePacket : IPacket
{
    public List<GachaReward> Drops { get; set; } = new List<GachaReward>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.GachaResponse);
        buffer.WriteVarInt(Drops.Count);
        foreach (var drop in Drops)
        {
             buffer.WriteVarInt(drop.Type);
            buffer.WriteVarInt(drop.DataId);
             buffer.WriteVarInt(drop.Count);
           
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        
    }
}
