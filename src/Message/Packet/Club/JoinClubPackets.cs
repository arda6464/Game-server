using System.Collections.Generic;

[PacketHandler(MessageType.JoinClubRequest)]
public class JoinClubRequestPacket : IPacket
{
    public int ClubId { get; set; }
    public string jointext;

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        ClubId = buffer.ReadVarInt();
        jointext = buffer.ReadVarString();
    }
}


public class JoinClubResponsePacket : IPacket
{
    public Club Club;

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.JoinClubResponse);
        buffer.WriteVarInt(Club.ID);
        buffer.WriteVarInt(Club.AvatarID);
        buffer.WriteVarString(Club.Name);
        buffer.WriteVarString(Club.Description);
        buffer.WriteVarInt(Club.TotalTrophy);
        buffer.WriteVarInt((int)Club.State);
        buffer.WriteVarString(Club.Region);
        buffer.WriteVarInt(Club.Members.Count);
        buffer.WriteVarInt(Club.Messages.Count);

        foreach (var message in Club.Messages)
        {
            buffer.WriteVarInt((int)message.messageFlags);
            switch ((ClubMessageFlags)message.messageFlags)
            {
                case ClubMessageFlags.None:
                    buffer.WriteVarInt((int)message.MessageId);
                    buffer.WriteVarInt(message.SenderId);
                    buffer.WriteVarString(message.SenderName);
                    buffer.WriteVarInt(message.SenderAvatarID);
                    buffer.WriteVarInt((int)message.Role);
                    buffer.WriteVarString(message.Content);
                    break;
                case ClubMessageFlags.HasSystem:
                    buffer.WriteVarInt((int)message.eventType);
                    buffer.WriteVarString(message.ActorName);
                    buffer.WriteVarInt(message.ActorID);
                    break;
                case ClubMessageFlags.HasTarget:
                    buffer.WriteVarInt((int)message.eventType);
                    buffer.WriteVarString(message.ActorName);
                    buffer.WriteVarString(message.TargetName);
                    buffer.WriteVarInt(message.TargetID);
                    break;
                case ClubMessageFlags.Request:
                    buffer.WriteVarInt(message.MessageId);
                    buffer.WriteVarInt(message.ActorID);
                    buffer.WriteVarString(message.ActorName);
                    buffer.WriteVarString(message.Content);
                    buffer.WriteVarInt(message.SenderAvatarID);
                    buffer.WriteVarInt((int)message.RequestState);
                    break;
            }
        }
        foreach (var member in (Club?.Members ?? new List<ClubMember>()))
        {
            buffer.WriteVarInt(member.ID);
            buffer.WriteVarString(member.AccountName);
            buffer.WriteVarInt((int)member.Role);
            buffer.WriteVarInt(member.NameColorID);
            buffer.WriteVarInt(member.AvatarID);
            buffer.WriteBool(SessionManager.IsOnline(member.ID));
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
