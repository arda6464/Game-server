using System;

public static class MessageManager
{
    public static void HandleMessage(Session session, byte[] data)
    {
        int value = BitConverter.ToInt32(data, 0);
        switch ((MessageType)value)
        {
            case MessageType.FirstConnectionRequest:
                FirstConnectionHandler.Handle(session, data);
                break;
            case MessageType.AuthLoginRequest:
                AuthLoginHandler.Handle(session, data);
                break;
            case MessageType.Disconnect:
                session.Close();
                break;
            case MessageType.ChangeNameColorRequest:
                SetNameColor.Handle(session, data);
                break;
            case MessageType.SetAvatarRequest:
                SetAvatar.Handle(session, data);
                break;
            case MessageType.ShowProfileRequest:
                ShowProfileHandler.Handle(session, data);
                break;
            case MessageType.SendFriendRequest:
                SendFriendRequestHandler.Handle(session, data);
                break;
            case MessageType.AcceptFriendRequest:
                FriendRequestAccept.Handle(session, data);
                break;
            case MessageType.DeclineFriendRequest:
                FriendRequestDecline.Handle(session, data);
                break;
            case MessageType.SendClubMessage:
                ClubMessageHandler.Handle(session, data);
                break;
            case MessageType.JoinClubRequest:
                JoinedClubHandler.Handle(session, data);
                break;
            case MessageType.LeaveClubRequest:
                LeaveClubHandler.Handle(session, data);
                break;
            case MessageType.ClubEditRequest:
                ClubEditHandler.Handle(session, data);
                break;
            case MessageType.ChangeNameRequest:
                ChangeNameHandler.Handle(session,data);
                break;
            case MessageType.GetRandomClubRequest:
                RandomClubHandler.Handle(session);
                break;
            case MessageType.Ping:
                PingHandler.Handle(session,data);
                break;
            default:
                Logger.errorslog("[MESSAGE MANAGER] gelen paket id bulunamadı: " + value);
                break;
        }
    }


    
}