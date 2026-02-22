using System;

namespace Logic
{
    public static class LobbyLogic
    {
        public static void HomeVisited(Session session)
        {
            if (session.PlayerData == null || session.Account == null) return;

            QuestManager.CheckAndRefreshQuests(session.Account);

            Console.WriteLine($"[LobbyLogic] HomeVisited tetiklendi: {session.AccountId}");
        }
    }
}
