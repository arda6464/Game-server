using System.Collections.Generic;

public class NewRequestListPacket : IPacket
{
    public List<FriendInfo> Requests { get; set; } = new List<FriendInfo>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.NewRequestList);
        buffer.WriteInt(Requests.Count);
        foreach (var request in Requests)
        {
            buffer.WriteString(request.Id);
            buffer.WriteInt(request.AvatarId);
            buffer.WriteString(request.Username);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
