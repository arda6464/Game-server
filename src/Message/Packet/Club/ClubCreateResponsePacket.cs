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
        buffer.WriteVarInt((int)MessageType.ClubCreateResponse);
        buffer.WriteVarInt(ClubId);
        buffer.WriteVarString(ClubName);
        buffer.WriteVarString(ClubDescription);
        buffer.WriteVarInt(TotalTrophies);
        
        buffer.WriteVarInt(Messages.Count);
        foreach (var message in Messages)
        {
            buffer.WriteVarInt(message.SenderId);
            buffer.WriteVarString(message.SenderName);
            buffer.WriteVarInt(message.SenderAvatarID);
            buffer.WriteVarString("Üye"); // todo enum send
            buffer.WriteVarString(message.Content);
        }

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
