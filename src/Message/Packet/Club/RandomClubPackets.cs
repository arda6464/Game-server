using System.Collections.Generic;


public class RandomClubRequestPacket : IPacket
{
    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
       // No data
    }
}


public class RandomClubResponsePacket : IPacket
{
    public List<Club> Clubs { get; set; } = new List<Club>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.GetRandomClubResponse);
        buffer.WriteVarInt(Clubs.Count);
        foreach (var rclub in Clubs)
        {
            buffer.WriteVarInt(rclub.ID);
            buffer.WriteVarString(rclub.Name);
            buffer.WriteVarString(rclub.Description);
            buffer.WriteVarInt(rclub.TotalTrophy);
            buffer.WriteVarInt(rclub.Members.Count);
            buffer.WriteVarInt(rclub.MaxMembers);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
