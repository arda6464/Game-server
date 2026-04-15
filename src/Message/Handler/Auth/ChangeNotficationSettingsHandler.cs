[PacketHandler(MessageType.ChangeNotficationRequest)]
public static class ChangeNotficationSettingsHandler
{
    public static void Handle(Session session, byte[] data)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(data);
        byte index = read.ReadByte();
        read.Dispose();
        switch (index)
        {
            case 0:
                session.Account?.SendOnlineBestFriendNotification = !session.Account.SendOnlineBestFriendNotification;
                break;
            case 1:
                session.Account?.SendNewEventNotification = !session.Account.SendNewEventNotification;
                break;
            case 2:
                session.Account?.SendInviteNotification = !session.Account.SendInviteNotification;
                break;
            case 3:
                session.Account?.SendClaimRewardNotification = !session.Account.SendClaimRewardNotification;
                break;
            default:
            Logger.errorslog($"[ChangeNotficationSettingsHandler] Geçersiz index: {index} from {session.ID}");
                break;
        }

    }
}
