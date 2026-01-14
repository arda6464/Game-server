using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public static class AccountCache
{
    private static ConcurrentDictionary<string, AccountManager.AccountData> CachedAccounts = new();
    private static Timer _saveTimer;
    
    // Accounts.cs için public property
    public static ConcurrentDictionary<string, AccountManager.AccountData> GetCachedAccounts() => CachedAccounts;

    // Cache'deki hesap sayısı
    public static int Count() => CachedAccounts.Count;

    // Cache’i başlat ve 1 dakikada bir kaydet
    public static void Init()
    {
       
        AccountManager.LoadAccounts();
      Thread  _thread = new Thread(Update);
            _thread.Start();
        

       
       
    }
     static bool started = true;
    private static void Update()
    {
        while (started)
            {
                SaveAll();
          //  Logger.genellog("save alındı");
                Thread.Sleep(1000 * 120);
            }
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

     //   Logger.genellog("[AccountCache] Cache kaydedildi.");
    }

    // add cache
    public static void Cache(AccountManager.AccountData account)
    {
        CachedAccounts[account.AccountId] = account;
      //  Console.WriteLine($"{account.Username} adlı kullanıcı cache'ye eklendi");
    }

       // load'ı cache'den yap
   public static AccountManager.AccountData Load(string accountId)
{
    // 1. Null kontrolü
    if (string.IsNullOrEmpty(accountId))
    {
        Console.WriteLine($"AccountCache.Load: accountId null veya boş");
        return null;
    }
    
    // 2. Dictionary'den almayı dene
    if (CachedAccounts.TryGetValue(accountId, out var account))
    {
        // 3. Bulunan account'ı da kontrol et
        if (account == null)
        {
            Console.WriteLine($"AccountCache.Load: {accountId} bulundu ama null!");
            // Cache'den temizle
            CachedAccounts.TryRemove(accountId, out _);
            return null;
        }
        
        Console.WriteLine($"AccountCache.Load: {accountId} bulundu ve döndürüldü");
        return account;
    }
    
    // 4. Bulunamadı
    Console.WriteLine($"AccountCache.Load: {accountId} cache'de bulunamadı");
    return null;
}
    // Cache’de var mı?
    public static bool IsCached(string accountId)
    {
        return CachedAccounts.ContainsKey(accountId);
    }



    // Cache'i durdur ve son kaydı yap
    public static void Stop()
    {
        started = false;
        _saveTimer?.Dispose();
        SaveAll();
    }
    public static List<AccountManager.AccountData> GetAllAccounts()
    {
        return new List<AccountManager.AccountData>(CachedAccounts.Values);
    }

    
}
