using System.Collections.Generic;

[PacketHandler(MessageType.ClubShowRequest)]
public class ClubShowRequestPacket : IPacket
{
    public int ClubId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        ClubId = buffer.ReadVarInt();
    }
}


public class ClubShowResponsePacket : IPacket
{
   public Club club;
    
    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.ClubShowResponse);
        buffer.WriteVarInt(club.ID);
        buffer.WriteVarString(club.Name);
        buffer.WriteVarString(club.Description);
        buffer.WriteVarInt(club.AvatarID);
        buffer.WriteVarInt(club.TotalTrophy);
        buffer.WriteVarInt(club.Members.Count);
        buffer.WriteVarInt((int)club.State);
        buffer.WriteVarString(club.Region);        
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
