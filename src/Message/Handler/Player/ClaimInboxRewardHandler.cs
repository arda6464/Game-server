using System;
using System.Linq;
[PacketHandler(MessageType.ClaimInboxRewardRequest)]
public static class ClaimInboxRewardHandler
{
    
    public static void Handle(Session session, byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            Logger.errorslog($"[ClaimHandler] {session.Account?.Username ?? "Bilinmeyen Kullanıcı"} boş veri gönderdi.");
            return;
        }

        var packet = new ClaimInboxRewardPacket();
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            buffer.WriteBytes(data);
            packet.Deserialize(buffer);
        }

        var account = session.Account;
        if (account == null) return;

        lock (account.SyncLock)
        {
            // Inbox bildirimini bul (IndexID üzerinden veya liste sırasından)
            var notification = account.inboxesNotfications.FirstOrDefault(n => n.IndexID == packet.NotificationIndexId);
            
            // Eğer ID ile bulunamazsa (eski bildirimler vb.) index olarak dene
            if (notification == null && packet.NotificationIndexId >= 0 && packet.NotificationIndexId < account.inboxesNotfications.Count)
            {
                notification = account.inboxesNotfications[packet.NotificationIndexId];
            }

            if (notification != null && !notification.IsClaimed && notification.Rewards.Count > 0)
            {
                var gachaResponse = new GachaResponsePacket();

                // Ödülleri hazırla ve uygula
                foreach (var reward in notification.Rewards)
                {
                    DeliveryManager.ApplyReward(account, reward);
                    gachaResponse.Drops.Add(new GachaSystem.GachaReward(reward));
                }

                // Alındı olarak işaretle
                notification.IsClaimed = true;
                               
                    packet.Success = true;
                
                // İstemciye Gacha (Drop) animasyonu için paketi gönder
                session.Send(gachaResponse);
                session.Send(packet); // Başarı durumunu gönder

                
                Logger.genellog($"[ClaimHandler] {account.Username} ödüllerini topladı: {notification.Title} ({gachaResponse.Drops.Count} kalem Gacha gönderildi)");
            }
            else
            {
                Logger.errorslog($"[ClaimHandler] {account.Username} için geçersiz bildirim ID'si veya ödül yok: {packet.NotificationIndexId}");
            }
        }
    }
}
