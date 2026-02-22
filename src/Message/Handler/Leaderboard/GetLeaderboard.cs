[PacketHandler(MessageType.LeaderboardRequest)]
public static class GetLeaderboard
{
    public static void Handle(Session session)
    {
        try

        {
            var topPlayers = AccountManager.GetTop100Players();
            var playerRank = AccountManager.GetPlayerRank(session.AccountId);
            var acccount = session.Account;
            if (acccount == null) return;

            var response = new LeaderboardResponsePacket
            {
                PlayerRankIndex = playerRank - 1,
                PlayerTrophy = acccount.Trophy
            };
            
            foreach (var player in topPlayers)
            {
                response.Players.Add(new LeaderboardResponsePacket.PlayerInfo
                {
                    Name = player.Username,
                    AccountId = player.AccountId,
                    ClubName = player.ClubName,
                    Trophy = player.Trophy,
                    AvatarId = player.Avatarid,
                    NameColorId = player.Namecolorid,
                    Premium = player.Premium
                });
            }

            session.Send(response);

        }
        catch (Exception ex)
        {
            Console.WriteLine("LB HATA: " + ex.Message + "\n tam  hali: " + ex.ToString());

        }

    }
        
}