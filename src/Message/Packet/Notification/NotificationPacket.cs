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
    public string Sender { get; set; }
    public bool IsViewed { get; set; }
    public long UnixTime { get; set; }
    public int RewardType { get; set; }
    public int DonationCount { get; set; }
    public bool IsClaimed { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.Notification);
        buffer.WriteByte((byte)Type);

        switch (Type)
        {
            case NotficationTypes.NotficationType.toast:
                buffer.WriteString(Title);
                buffer.WriteString(Message);
                buffer.WriteInt(IconId);
                break;
            case NotficationTypes.NotficationType.banner:
                buffer.WriteString(Title ?? "");
                buffer.WriteString(Message ?? "");
                buffer.WriteString(ButtonText ?? " ");
                buffer.WriteString(Url ?? " ");
                break;
            case NotficationTypes.NotficationType.Inbox:
                buffer.WriteString(Sender);
                buffer.WriteString(Message);
                buffer.WriteBool(IsViewed);
                buffer.WriteLong(UnixTime);
                buffer.WriteInt(RewardType);
                buffer.WriteInt(DonationCount);
                buffer.WriteBool(IsClaimed);
                break;
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
