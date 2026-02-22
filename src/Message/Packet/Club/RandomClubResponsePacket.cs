using System.Collections.Generic;

public class RandomClubResponsePacket : IPacket
{
    public List<Club> Clubs { get; set; } = new List<Club>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.GetRandomClubResponse);
        buffer.WriteInt(Clubs.Count);
        foreach (var rclub in Clubs)
        {
            buffer.WriteInt(rclub.ClubId);
            buffer.WriteString(rclub.ClubName);
            buffer.WriteString(rclub.Clubaciklama);
            buffer.WriteInt(rclub.TotalKupa ?? 0);
            buffer.WriteInt(rclub.Members.Count);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
