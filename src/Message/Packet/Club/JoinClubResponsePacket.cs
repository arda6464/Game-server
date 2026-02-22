using System.Collections.Generic;

public class JoinClubResponsePacket : IPacket
{
    public int ClubId { get; set; }
    public int ClubAvatarId { get; set; }
    public string ClubName { get; set; }
    public string ClubDescription { get; set; }
    public List<ClubMember> Members { get; set; } = new List<ClubMember>();
    public List<ClubMessage> Messages { get; set; } = new List<ClubMessage>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.JoinClubResponse);
        buffer.WriteInt(ClubId);
        buffer.WriteInt(ClubAvatarId);
        buffer.WriteString(ClubName);
        buffer.WriteString(ClubDescription);
        
        buffer.WriteInt(Members.Count);
        foreach (var member in Members)
        {
            buffer.WriteString(member.Accountid);
            buffer.WriteString(member.AccountName);
            buffer.WriteString(member.Role.ToString());
            buffer.WriteInt(member.NameColorID);
            buffer.WriteInt(member.AvatarID);
        }

        buffer.WriteInt(Messages.Count);
        foreach (var clubmessage in Messages)
        {
            buffer.WriteByte((byte)clubmessage.messageFlags);
            switch((ClubMessageFlags)clubmessage.messageFlags)
            {
                case ClubMessageFlags.None:
                    buffer.WriteString(clubmessage.SenderId);
                    buffer.WriteString(clubmessage.SenderName);
                    buffer.WriteInt(clubmessage.SenderAvatarID);
                    buffer.WriteString("Üye");
                    buffer.WriteString(clubmessage.Content);
                    break;
                case ClubMessageFlags.HasSystem:
                    buffer.WriteInt((int)clubmessage.eventType);
                    buffer.WriteString(clubmessage.ActorName);
                    buffer.WriteString(clubmessage.ActorID ??"");
                    break;
                case ClubMessageFlags.HasTarget:
                    buffer.WriteInt((int)clubmessage.eventType);
                    buffer.WriteString(clubmessage.ActorName);
                    buffer.WriteString(clubmessage.ActorID);
                    buffer.WriteString(clubmessage.TargetName);
                    break;
            }
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
