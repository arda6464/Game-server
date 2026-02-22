using System.Collections.Generic;

public class ClubCreateResponsePacket : IPacket
{
    public int ClubId { get; set; }
    public string ClubName { get; set; }
    public string ClubDescription { get; set; }
    public int TotalTrophies { get; set; }
    public List<ClubMessage> Messages { get; set; } = new List<ClubMessage>();
    public List<ClubMember> Members { get; set; } = new List<ClubMember>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.ClubCreateResponse);
        buffer.WriteInt(ClubId);
        buffer.WriteString(ClubName);
        buffer.WriteString(ClubDescription);
        buffer.WriteInt(TotalTrophies);
        
        buffer.WriteInt(Messages.Count);
        foreach (var message in Messages)
        {
            buffer.WriteString(message.SenderId);
            buffer.WriteString(message.SenderName);
            buffer.WriteInt(message.SenderAvatarID);
            buffer.WriteString("Üye"); // todo enum send
            buffer.WriteString(message.Content);
        }

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
