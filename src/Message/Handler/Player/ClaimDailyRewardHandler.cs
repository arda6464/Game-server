using System;
using System.Linq;

[PacketHandler(MessageType.ClaimDailyRewardRequest)]
public static class ClaimDailyRewardHandler
{
    public static void Handle(Session session)
    {
        var account = session.Account;
        if (account == null) return;

        lock (account.SyncLock)
        {
            var response = new DailyLoginClaimPacket();
            var dailyReward = account.DailyStreakWindow
                .FirstOrDefault(reward => reward != null && reward.Day == account.DailyRewardStreak);

            if (dailyReward == null)
            {
                Console.WriteLine("oyuncunun dailyrewardı bulunamadı.");
                response.IsSucessfull = false;
                session.Send(response);
                return;
            }

            if (!dailyReward.IsAvaiable)
            {
                Console.WriteLine("oyuncunun dailyreward günü gelmemiş: " + dailyReward.Day);
                response.IsSucessfull = false;
                session.Send(response);
                return;
            }

            if (dailyReward.IsClaimed)
            {
                Console.WriteLine("oyuncunun dailyreward ı zaten almış: " + dailyReward.Day);
                response.IsSucessfull = false;
                session.Send(response);
                return;
            }

            DeliveryManager.ApplyReward(account, dailyReward.Reward);
            response.Drop = dailyReward.Reward;
            response.IsSucessfull = true;
            response.Day = dailyReward.Day;

            dailyReward.IsClaimed = true;
            account.LastDailyRewardDate = DateTime.Today;
            AccountManager.SaveAccounts();

            session.Send(response);

            Logger.genellog($"[ClaimHandler] {account.Username} günlük ödülünü topladı: ({response.Drop.Type} kalem Gacha gönderildi)");
        }
    }
}
