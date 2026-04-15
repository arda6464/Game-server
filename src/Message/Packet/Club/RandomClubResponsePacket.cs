using System.Collections.Generic;

public class RandomClubResponsePacket : IPacket
{
    public List<Club> Clubs { get; set; } = new List<Club>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.GetRandomClubResponse);
        buffer.WriteVarInt(Clubs.Count);
        foreach (var rclub in Clubs)
        {
            buffer.WriteVarInt(rclub.ClubId);
            buffer.WriteVarString(rclub.ClubName);
            buffer.WriteVarString(rclub.Clubaciklama);
            buffer.WriteVarInt(rclub.TotalKupa ?? 0);
            buffer.WriteVarInt(rclub.Members.Count);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
