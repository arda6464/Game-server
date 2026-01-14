using System;

public static class MessageManager
{
    public static void HandleMessage(Session session, byte[] data)
    {
        int value = BitConverter.ToInt32(data, 0);
        if ((MessageType)value != MessageType.Ping)
         Console.WriteLine($"[MessageManager] {session.AccountId} kullanıcısından {((MessageType)value).ToString()} mesajı alındı.");
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
            case MessageType.DeleteFriendRequest:
                DeleteFriendHandler.Handle(session, data);
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
            case MessageType.ClubShowRequest:
                ClubShowHandler.Handle(session,data);
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
            case MessageType.MatchMakingRequest:
                MatchMaking.JoinQueue(session);
                break;
            case MessageType.MatchMakingCancelRequest:
                break;
            case MessageType.Move:
                PlayerMoveHandler.Handle(session, data);
                break;
            case MessageType.ShootRequest:
                PlayerShotRequestHandler.Handle(session, data);
                break;
            case MessageType.HitRequest:
                PlayerHitRequest.Handle(session, data);
                break;
            case MessageType.KickMemberinClubRequest:
                KickMemberHandler.Handle(session, data);
                break;
            case MessageType.MemberToLowerRequest:
                ClubMemberChangeHandler.Handle(session, data);
                break;
            case MessageType.MemberToUpperRequest:
             ClubMemberChangeHandler.Handle(session, data);
                break;
            case MessageType.AllNotficationViewed:
                AllNotficationViewedHandler.Handle(session);
                break;
            case MessageType.CreateTeamRequest:
                CreateTeamHandler.Handle(session);
                break;
            case MessageType.LeaveTeamRequest:
                LeaveTeamHandler.Handle(session, data);
                break;
            case MessageType.JoinTeamRequest:
                JoinTeamHandler.Handle(session, data);
                break;
            case MessageType.SendTeamMessageRequest:
                TeamMessageHandler.Handle(session, data);
                break;
            case MessageType.GetAllMarketItemsRequest:
                ShopItemsHandler.Handle(session);
                break;
            case MessageType.LeaderboardRequest:
            GetLeaderboard.Handle(session);
                break;
            case MessageType.AccountLogin:
                LoginAccountHandler.Handle(session, data);
                break;
            case MessageType.VerifyCodeResponse:
                CodeVerify.Handle(session, data);
                break;
            case MessageType.SignAccount:
                CreateAccountHandler.Handle(session, data);
                break;
            case MessageType.SupporCreateTicketRequest:
                CreateTicket.Handle(session,data);
                break;
            case MessageType.SupportMessageSend:
                GetChatMessage.Handle(session, data);
                break;
            case MessageType.ClubCreateRequest:
                ClubCreateHandler.Handle(session, data);
                break;
            case MessageType.Alive:
                session.LastAlive = DateTime.Now;
                break;
            case MessageType.InviteToTeamRequest:
                TeamInviteHandler.Handle(session, data);
                break;
            case MessageType.InviteToTeamResponse:
                TeamInviteHandler.ResponseHandle(session, data);
                break;
            case MessageType.SupportGetAllTicketRequest:
                GetAllTickets.Handle(session);
                break;
        
            default:
                Logger.errorslog("[MESSAGE MANAGER] gelen paket id bulunamadı: " + value);
                break;

        }
       
    }


    
}