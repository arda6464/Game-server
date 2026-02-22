using System.Collections.Generic;

public class JoinTeamResponsePacket : IPacket
{
    public int TeamId { get; set; }
    public List<TeamMessage> Messages { get; set; } = new List<TeamMessage>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.JoinTeamResponse);
        buffer.WriteInt(TeamId);
        
        buffer.WriteInt(Messages.Count);
        foreach(var teammessage in Messages)
        {
            buffer.WriteString(teammessage.SenderId);
            buffer.WriteString(teammessage.SenderName);
            buffer.WriteInt(teammessage.SenderAvatarID);
            buffer.WriteString("");
            buffer.WriteString(teammessage.Content);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
