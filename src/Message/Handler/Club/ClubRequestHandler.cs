using System;
using System.Linq;

[PacketHandler(MessageType.ClubJoinRespondRequest)]
public static class ClubRequestHandler
{
    public static void Handle(Session session, byte[] data)
    {
        ByteBuffer buffer = ByteBufferPool.Get();
        buffer.WriteBytes(data);

        var request = new ClubRequestPacket();
        request.Deserialize(buffer);
        buffer.Dispose();

        if (session.Account == null || session.Account.Clubid == 0) return;

        AccountManager.AccountData account = session.Account;
        var club = ClubManager.LoadClub(account.Clubid);
        if (club == null)
        {
            Console.WriteLine("[ClubRequestHandler] club bulunamadı....");
            return;
        }

        /*  // Sadece Lider ve Yardımcı Lider istekleri kabul/red edebilir
          if (account.clubRole != ClubRole.Leader && account.clubRole != ClubRole.CoLeader)
          {
              return; 
          }*/

        lock (club.SyncLock)
        {
            var message = club.GetCLubMessage(request.MessageID);

            // Mesaj bulunamadıysa veya bir istek mesajı değilse, ya da zaten cevaplandıysa çık
            if (message == null || message.messageFlags != ClubMessageFlags.Request || message.RequestState != ClubRequestState.Waiting)
            {
                Console.WriteLine("[ClubRequestHandler] mesaj bulunamadı.... messageId: " + request.MessageID);
                return;
            }

            club.PendingInvites.Remove(message.ActorID);

            if (request.Isjoined)
            {
                if (club.Members.Count >= club.MaxMembers)
                {
                    MessageCodeManager.Send(session, MessageCodeManager.Message.ClubFull);

                    return;

                }

                bool added = club.AddMember(message.ActorID);
                if (added)
                {
                    message.RequestState = ClubRequestState.Accepted;

                    // Katılma mesajı oluştur ve kulübe gönder (isteğe bağlı ama güzel olur)
                    ClubMessage joinMessage = new ClubMessage
                    {
                        messageFlags = ClubMessageFlags.HasSystem,
                        eventType = ClubEventType.JoinMessage,
                        ActorName = message.ActorName,
                        ActorID = message.ActorID
                    };
                    club.SendMessageToClubMembers(joinMessage);
                }
                else
                {
                    // Eklenemediyse (örn: zaten başka kulüpteyse)
                    message.RequestState = ClubRequestState.Rejected;
                }
            }
            else
            {
                Notfication notfication = new Notfication
                {
                    type = NotficationTypes.NotficationType.Inbox,
                    Sender = "Sistem",
                    Message = $"{club.Name} kulübüne gönderdiğin istek reddedildi.",
                    Timespam = DateTime.Now
                };
                var acccount = AccountCache.Load(message.ActorID);
                if (acccount != null)
                {
                    NotificationManager.Add(acccount, notfication);
                    if (SessionManager.IsOnline(acccount.ID))
                    {
                        var tsession = SessionManager.GetSession(acccount.ID);
                        NotficationSender.Send(tsession, notfication);
                    }
                }


                message.RequestState = ClubRequestState.Rejected;
            }

            // Client UI'ını güncellemesi için paketi broadcast et
            var updatePacket = new ClubRequestStateUpdatePacket
            {
                MessageId = message.MessageId,
                NewState = (int)message.RequestState,
                ResponderName = account.Username
            };

            foreach (var member in club.Members)
            {
                if (SessionManager.IsOnline(member.ID))
                {
                    var targetSession = SessionManager.GetSession(member.ID);
                    targetSession?.Send(updatePacket);
                }
            }
        }

        ClubManager.Save();
    }
}