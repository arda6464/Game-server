[PacketHandler(MessageType.Notification)]
public class NotificationPacket : IPacket
{
    public NotficationTypes.NotficationType Type { get; set; }

    // Toast Fields
    public string Title { get; set; }
    public string Message { get; set; }
    public int IconId { get; set; }

    // Banner Fields (Shared: Title, Message)
    public string ButtonText { get; set; }
    public string Url { get; set; }

    // Inbox Fields (Shared: Message)
    public int IndexID { get; set; }
    public string Sender { get; set; }
    public bool IsViewed { get; set; }
    public long UnixTime { get; set; }
    public bool IsClaimed { get; set; }
    public List<RewardItem> Rewards { get; set; } = new List<RewardItem>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.Notification);
        buffer.WriteByte((byte)Type);

        switch (Type)
        {
            case NotficationTypes.NotficationType.toast:

                buffer.WriteVarString(Message);
                buffer.WriteVarInt(IconId);
                break;
            case NotficationTypes.NotficationType.banner:
                buffer.WriteVarString(Title ?? "");
                buffer.WriteVarString(Message ?? "");
                buffer.WriteVarString(ButtonText ?? " ");
                buffer.WriteVarString(Url ?? " ");
                break;
            case NotficationTypes.NotficationType.Inbox:
                buffer.WriteVarInt(IndexID);
                buffer.WriteVarString(Sender);
                buffer.WriteVarString(Message);
                buffer.WriteBool(IsViewed);
                buffer.WriteVarLong(UnixTime);
                 buffer.WriteBool(Rewards != null && Rewards.Count > 0); // hasBonus: Ödül var mı?
                buffer.WriteBool(IsClaimed);
               
                break;
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
