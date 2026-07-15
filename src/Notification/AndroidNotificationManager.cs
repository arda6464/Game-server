using System;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

public static class AndroidNotificationManager
{
    private static bool _isInitialized = false;

    public static void Initialize()
    {
        try
        {
            // Öncelikli olarak çalışan dizinde ara
            string credentialsPath = "Data/firebase-service-account.json";

            if (!File.Exists(credentialsPath))
            {
                // src klasörünü kontrol et (Geliştirme ortamı için)
                string srcPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "src",
                    "Data/firebase-service-account.json"
                );
                if (File.Exists(srcPath))
                {
                    credentialsPath = srcPath;
                }
                else
                {
                    Logger.errorslog(
                        $"[AndroidNotificationManager] firebase-service-account.json bulunamadı! Bildirimler çalışmayacak."
                    );
                    return;
                }
            }

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(
                    new AppOptions() { Credential = GoogleCredential.FromFile(credentialsPath) }
                );
                _isInitialized = true;
                Logger.genellog("[AndroidNotificationManager] Firebase başarıyla başlatıldı.");
            }
            else
            {
                _isInitialized = true;
            }
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[AndroidNotificationManager] Başlatma hatası: {ex.Message}");
        }
    }

    public static async void SendNotification(string title, string message, string token)
    {
        if (!_isInitialized)
        {
            return;
        }

        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var notificationMessage = new Message()
        {
            Token = token,
            Notification = new FirebaseAdmin.Messaging.Notification()
            {
                Title = title,
                Body = message,
            },
        };

        try
        {
            string response = await FirebaseMessaging.DefaultInstance.SendAsync(
                notificationMessage
            );
            Logger.genellog(
                $"[AndroidNotificationManager] Bildirim başarıyla gönderildi. ID: {response}"
            );
        }
        catch (Exception ex)
        {
            Logger.errorslog(
                $"[AndroidNotificationManager] Bildirim gönderme hatası: {ex.Message}"
            );
        }
    }
}
