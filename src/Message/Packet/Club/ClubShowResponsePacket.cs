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
        buffer.WriteShort((short)MessageType.ClubShowResponse);
        buffer.WriteInt(ClubId);
        buffer.WriteString(ClubName);
        buffer.WriteString(ClubDescription);
        buffer.WriteInt(ClubAvatarId);
        buffer.WriteInt(TotalTrophies);
        buffer.WriteInt(Members.Count);
        foreach (var member in Members)
        {
            buffer.WriteString(member.Accountid);
            buffer.WriteString(member.AccountName);
            buffer.WriteString(member.Role.ToString());
            buffer.WriteInt(member.NameColorID);
            buffer.WriteInt(member.AvatarID);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
