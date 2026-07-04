using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public static class AccountCache
{
    private static ConcurrentDictionary<int, AccountManager.AccountData> Accounts = new();
    private static Timer? _saveTimer;

    public static ConcurrentDictionary<int, AccountManager.AccountData> GetCachedAccounts() => Accounts;

    public static int Count() => Accounts.Count;

    public static void Init()
    {
        AccountManager.LoadAccounts();
        Thread _thread = new Thread(Update);
        _thread.Start();
    }

    private static bool started = true;
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
            AccountManager.SaveAccounts();
        }
        catch (Exception ex)
        {
            Logger.errorslog($"Hata kaydederken: {ex.Message}");
        }
    }

    // add cache
    public static void Cache(AccountManager.AccountData account)
    {
        if (account == null) return;
        Accounts[account.ID] = account;
    }

    // load'ı cache'den yap (int ID ile)
    public static AccountManager.AccountData Load(int id)
    {
        if (id <= 0) return null;
        if (Accounts.TryGetValue(id, out var account))
        {
            return account;
        }
        return null;
    }
    public static void Remove(int accountId)
    {

        if (Accounts.ContainsKey(accountId))
        {
            AccountManager.AccountData account = Accounts[accountId];
            Accounts.TryRemove(accountId, out var data);
            Logger.genellog($"[Cache] Hesap cache'den silindi: {accountId}");
        }

    }

    public static bool IsCached(int accountId)
    {
        return Accounts.ContainsKey(accountId);
    }

    public static void Stop()
    {
        started = false;
        _saveTimer?.Dispose();
        SaveAll();
    }

    public static List<AccountManager.AccountData> GetAllAccounts()
    {
        return new List<AccountManager.AccountData>(Accounts.Values);
    }
}
