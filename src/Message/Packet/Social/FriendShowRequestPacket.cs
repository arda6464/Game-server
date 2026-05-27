
public class FriendShowRequestPacket : IPacket
{
    public int TargetId { get; set; }
    public AccountManager.AccountData? account { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.ShowFriendResponse);


        buffer.WriteVarInt(account.ID);
        buffer.WriteVarInt(account.Avatarid);
        buffer.WriteVarInt(account.Trophy);
        buffer.WriteVarString(account.Username);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TargetId = buffer.ReadVarInt();

    }
}

