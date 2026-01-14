public static class GetLeaderboard
{
    public static void Handle(Session session)
    {
        try

        {
            var topPlayers = AccountManager.GetTop100Players();
            var playerRank = AccountManager.GetPlayerRank(session.AccountId);
            var acccount = AccountCache.Load(session.AccountId);
            if (acccount == null) return;

            ByteBuffer buffer = new ByteBuffer(2048);
            buffer.WriteInt((int)MessageType.LeaderboardResponse);
            buffer.WriteInt(topPlayers.Count);
            foreach (var player in topPlayers)
            {
                buffer.WriteString(player.Username);
                buffer.WriteString(player.AccountId);
                buffer.WriteString(player.ClubName ?? " ");


                buffer.WriteInt(player.Trophy);
                buffer.WriteInt(player.Avatarid);
                buffer.WriteInt(player.Namecolorid);
                buffer.WriteInt(player.Premium);
            }
            buffer.WriteInt(playerRank-1);
            buffer.WriteInt(acccount.Trophy);
            byte[] response = buffer.ToArray();
            buffer.Dispose();

            session.Send(response);

        }
        catch (Exception ex)
        {
            Console.WriteLine("LB HATA: " + ex.Message + "\n tam  hali: " + ex.ToString());

        }

    }
        
}