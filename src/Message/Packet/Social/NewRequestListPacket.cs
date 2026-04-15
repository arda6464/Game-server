using System.Collections.Generic;

public class NewRequestListPacket : IPacket
{
    public List<FriendInfo> Requests { get; set; } = new List<FriendInfo>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.NewRequestList);
        buffer.WriteVarInt(Requests.Count);
        foreach (var request in Requests)
        {
            buffer.WriteVarInt(request.ID); // Sayısal I
            buffer.WriteVarInt(request.AvatarId);
            buffer.WriteVarString(request.Username);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
