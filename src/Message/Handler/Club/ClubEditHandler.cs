[PacketHandler(MessageType.ClubEditRequest)]
public static class ClubEditHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = ByteBufferPool.Get();
        read.WriteBytes(message, true);

        var request = new ClubEditRequestPacket();
        request.Deserialize(read);

        string? ClubName = request.ClubName;
        string? ClubAciklama = request.ClubDescription;
        int Avatarıd = request.AvatarId;
        int State = request.State;
        string? Region = request.Region;

        if (session.Account == null) return;
        AccountManager.AccountData account = session.Account;

        if (account.Clubid == 0)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.NotAClub);
            return;
        }
        else
        {
            var club = ClubManager.LoadClub(account.Clubid);
            if (club == null) return;
            {
                bool change = club.ChangeClubSettings(account.ID, ClubName, ClubAciklama, Avatarıd, State, Region);

                if (change)
                {
                    var response = new ClubEditResponsePacket
                    {
                        ClubName = club.Name,
                        ClubDescription = club.Description,
                        ClubAvatarId = club.AvatarID,
                        State = (int)club.State,
                        Region = club.Region,
                        AccountId = account.ID
                    };


                    foreach (var clubmember in club.Members)
                    {
                        if (SessionManager.IsOnline(clubmember.ID))
                        {
                            Session? membersession = SessionManager.GetSession(clubmember.ID);
                            membersession?.Send(response);
                        }

                    }
                    ClubMessage clubMessage = new ClubMessage
                    {
                       ActorID = account.ID,
                       ActorName = account.Username,
                        eventType = ClubEventType.EditMessage,
                        messageFlags = ClubMessageFlags.HasSystem,
                    };
                    club.SendMessageToClubMembers(clubMessage);
                }
                         

                }



            }
        }
    }


