using System.Collections.Generic;

public class ClubShowResponsePacket : IPacket
{
    public int ClubId { get; set; }
    public string ClubName { get; set; }
    public string ClubDescription { get; set; }
    public int ClubAvatarId { get; set; }
    public int TotalTrophies { get; set; }
    public List<ClubMember> Members { get; set; } = new List<ClubMember>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.ClubShowResponse);
        buffer.WriteVarInt(ClubId);
        buffer.WriteVarString(ClubName);
        buffer.WriteVarString(ClubDescription);
        buffer.WriteVarInt(ClubAvatarId);
        buffer.WriteVarInt(TotalTrophies);
        buffer.WriteVarInt(Members.Count);
        foreach (var member in Members)
        {
            buffer.WriteVarInt(member.ID);
            buffer.WriteVarString(member.AccountName);
            buffer.WriteVarString(member.Role.ToString());
            buffer.WriteVarInt(member.NameColorID);
            buffer.WriteVarInt(member.AvatarID);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
