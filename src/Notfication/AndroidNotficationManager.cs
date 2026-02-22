using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System;
using System.IO;
using System.Threading.Tasks;

public static class AndroidNotficationManager
{
    private static bool _isInitialized = false;

    public static void Initialize()
    {
        try
        {
            // Öncelikli olarak çalışan dizinde ara
            string credentialsPath = "firebase-service-account.json";
            
            if (!File.Exists(credentialsPath))
            {
                // src klasörünü kontrol et (Geliştirme ortamı için)
                string srcPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "firebase-service-account.json");
                if (File.Exists(srcPath))
                {
                    credentialsPath = srcPath;
                }
                else
                {
                    Logger.errorslog($"[AndroidNotficationManager] firebase-service-account.json bulunamadı! Bildirimler çalışmayacak.");
                    return;
                }
            }

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(credentialsPath)
                });
                _isInitialized = true;
                Logger.genellog("[AndroidNotficationManager] Firebase başarıyla başlatıldı.");
            }
            else
            {
                _isInitialized = true;
            }
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[AndroidNotficationManager] Başlatma hatası: {ex.Message}");
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
            Notification = new Notification()
            {
                Title = title,
                Body = message
            }
        };

        try
        {
            string response = await FirebaseMessaging.DefaultInstance.SendAsync(notificationMessage);
            Logger.genellog($"[AndroidNotficationManager] Bildirim başarıyla gönderildi. ID: {response}");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[AndroidNotficationManager] Bildirim gönderme hatası: {ex.Message}");
        }
    }
}