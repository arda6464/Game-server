using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public static class AccountCache
{
    private static ConcurrentDictionary<string, AccountManager.AccountData> CachedAccounts = new();
    private static Timer _saveTimer;

    // Cache’deki hesap sayısı
    public static int Count => CachedAccounts.Count;

    // Cache’i başlat ve 1 dakikada bir kaydet
    public static void Init()
    {
        // Mevcut hesapları AccountManager’den yükle
        AccountManager.LoadAccounts();
        

        // Periyodik save: 1 dakika
        _saveTimer = new Timer(_ => SaveAll(), null, 60000, 60000);
    }

    // Cache’deki tüm hesapları kaydet
    public static void SaveAll()
    {
        
            try
            {
                AccountManager.SaveAccounts(); // mevcut save metodunu kullan
            }
            catch (Exception ex)
            {
                Logger.errorslog($"Hata kaydederken: {ex.Message}");
            }
        
        Logger.genellog("[AccountCache] Cache kaydedildi.");
    }

    // add cache
    public static void Cache(AccountManager.AccountData account)
    {
        CachedAccounts[account.AccountId] = account;
        Console.WriteLine($"{account.Username} adlı kullanıcı cache'ye eklendi");
    }

       // load'ı cache'den yap
    public static AccountManager.AccountData Load(string accountId)
    {
        CachedAccounts.TryGetValue(accountId, out var account);
        return account;
    }

    // Cache’de var mı?
    public static bool IsCached(string accountId)
    {
        return CachedAccounts.ContainsKey(accountId);
    }

   

    // Cache’i durdur ve son kaydı yap
    public static void Stop()
    {
        _saveTimer?.Dispose();
        SaveAll();
    }

    
}
