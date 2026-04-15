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
        buffer.WriteVarInt((int)MessageType.JoinClubResponse);
        buffer.WriteVarInt(ClubId);
        buffer.WriteVarInt(ClubAvatarId);
        buffer.WriteVarString(ClubName);
        buffer.WriteVarString(ClubDescription);
        
        buffer.WriteVarInt(Members.Count);
        foreach (var member in Members)
        {
            buffer.WriteVarInt(member.ID);
            buffer.WriteVarString(member.AccountName);
            buffer.WriteVarString(member.Role.ToString());
            buffer.WriteVarInt(member.NameColorID);
            buffer.WriteVarInt(member.AvatarID);
        }

        buffer.WriteVarInt(Messages.Count);
        foreach (var clubmessage in Messages)
        {
            buffer.WriteByte((byte)clubmessage.messageFlags);
            switch((ClubMessageFlags)clubmessage.messageFlags)
            {
                case ClubMessageFlags.None:
                    buffer.WriteVarInt(clubmessage.SenderId);
                    buffer.WriteVarString(clubmessage.SenderName);
                    buffer.WriteVarInt(clubmessage.SenderAvatarID);
                    buffer.WriteVarString("Üye");
                    buffer.WriteVarString(clubmessage.Content);
                    break;
                case ClubMessageFlags.HasSystem:
                    buffer.WriteVarInt((int)clubmessage.eventType);
                    buffer.WriteVarString(clubmessage.ActorName);
                    buffer.WriteVarInt(clubmessage.ActorID);
                    break;
                case ClubMessageFlags.HasTarget:
                    buffer.WriteVarInt((int)clubmessage.eventType);
                    buffer.WriteVarString(clubmessage.ActorName);
                    buffer.WriteVarInt(clubmessage.ActorID);
                    buffer.WriteVarString(clubmessage.TargetName);
                    break;
            }
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
