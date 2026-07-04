[PacketHandler(MessageType.LeaderboardRequest)]
public static class GetLeaderboard
{
    public static void Handle(Session session)
    {
        try

        {
            var topPlayers = AccountManager.GetTop100Players();
            var playerRank = AccountManager.GetPlayerRank(session.ID);
            var acccount = session.Account;
            if (acccount == null) return;

            var response = new LeaderboardResponsePacket
            {
                PlayerRankIndex = playerRank - 1,
                PlayerTrophy = acccount.Trophy,
                 PlayerCountry = acccount.CountryCode,
                Players = topPlayers
            };



            session.Send(response);

        }
        catch (Exception ex)
        {
            Console.WriteLine("LB HATA: " + ex.Message + "\n tam  hali: " + ex.ToString());

        }

    }

}