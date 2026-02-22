public static class PlayerSetPresence
{
    public enum PresenceScope : byte
    {
        Friend,
        Club,
        Chat
    }
    public enum PresenceState : byte
    {
        Offline,
        Online
    }
    public static void Handle(Session sessions, PresenceState presence)
    {

        var acccount = sessions.Account;
         if (acccount == null) 
        {
            Logger.errorslog($"Account not found: {sessions.AccountId}");
            return;
        }



        if (acccount.Friends.Count != 0)
        {
            ByteBuffer bufer = new ByteBuffer();
            bufer.WriteShort((short)MessageType.Presence);
            bufer.WriteString(acccount.AccountId);
            bufer.WriteByte((byte)presence);
            bufer.WriteByte((byte)PresenceScope.Friend);
            byte[] friendresponse = bufer.ToArray();
            bufer.Dispose();

            foreach (var friend in acccount.Friends)
            {
                if (SessionManager.IsOnline(friend.Id))
                {
                    Session? session = SessionManager.GetSession(friend.Id);
                    if (session != null)
                    {
                        session.Send(friendresponse);

                        // --- En İyi Arkadaş Bildirimi ---
                        if (presence == PresenceState.Online)
                        {
                            var targetAccount = session.Account;
                            if (targetAccount != null)
                            {
                                // Arkadaşın listesinde biz "En İyi Arkadaş" mıyız?
                                var relation = targetAccount.Friends.Find(f => f.Id == acccount.AccountId);
                                if (relation != null && relation.IsBestFriend)
                                {
                                    // Cooldown Kontrolü (Oyun içi Toast)
                                    if (NotificationPolicyManager.CanSendNotification(targetAccount, NotificationPolicyManager.NotificationType.OnlineBest))
                                    {
                                        Notfication toast = new Notfication
                                        {
                                            type = NotficationTypes.NotficationType.toast,
                                            Title = "En İyi Arkadaşın Çevrimiçi!",
                                            Message = $"{acccount.Username} oyuna girdi.",
                                            iconid = acccount.Avatarid
                                        };
                                        NotficationSender.Send(session, toast);
                                        NotificationPolicyManager.UpdateCooldown(targetAccount, NotificationPolicyManager.NotificationType.OnlineBest);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (presence == PresenceState.Online)
                {
                    // Oyuncu çevrimdışı -> Push Bildirimi Gönder (Sadece Best Friend ise)
                    var friendAccount = AccountCache.Load(friend.Id);
                    if (friendAccount != null)
                    {
                        var relation = friendAccount.Friends.Find(f => f.Id == acccount.AccountId);
                        if (relation != null && relation.IsBestFriend)
                        {
                            // Cooldown Kontrolü (Push Bildirimi)
                            if (NotificationPolicyManager.CanSendNotification(friendAccount, NotificationPolicyManager.NotificationType.OnlineBest))
                            {
                                if (!string.IsNullOrEmpty(friendAccount.FBNToken))
                                {
                                    AndroidNotficationManager.SendNotification(
                                        "En İyi Arkadaş!",
                                        $"{acccount.Username} şu an oyunda, gel beraber kapışın!",
                                        friendAccount.FBNToken
                                    );
                                    NotificationPolicyManager.UpdateCooldown(friendAccount, NotificationPolicyManager.NotificationType.OnlineBest);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        if(acccount.Clubid != -1)
        {
            ByteBuffer bufer = new ByteBuffer();
            bufer.WriteShort((short)MessageType.Presence);
            bufer.WriteString(acccount.AccountId);
            bufer.WriteByte((byte)presence);
            bufer.WriteByte((byte)PresenceScope.Club);
            byte[] Clubresponse = bufer.ToArray();
            bufer.Dispose();
            var club = ClubCache.Load(acccount.Clubid);
            if(club == null)
            {
                Logger.errorslog($"[Presence]{acccount.Username}({acccount.AccountId}) adlı hesabın clubune erişilmedi");
                return;
            }
            foreach (var clubmember in club.Members)
            {
                if (clubmember.Accountid == acccount.AccountId) continue;
                if (SessionManager.IsOnline(clubmember.Accountid))
                {
                    Session? session = SessionManager.GetSession(clubmember.Accountid);
                       session.Send(Clubresponse);
                    
                }
            }

        }
     
    }
}