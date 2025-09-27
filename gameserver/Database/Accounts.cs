using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static class AccountManager
{
    private static Dictionary<string, AccountData> accounts = new Dictionary<string, AccountData>();
    private static int maxAccountId = 1;
    private static string savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "accounts.json");

    public class AccountData
    {
        // securtiy data
        public int Id { get; set; }
        public string? AccountId { get; set; }
        public string? Token { get; set; }
        // acccount data
        public string? Username { get; set; }
        public string? Dil { get; set; }
        public int Premium { get; set; }
        public DateTime PremiumEndTime { get; set; }
        public bool Banned { get; set; }
        public string? Banreason { get; set; }
        public int Avatarid { get; set; }
        public int Namecolorid { get; set; }
        public int Level { get; set; }
        public int Gems { get; set; }
        public int Clubid { get; set; }

        public List<FriendInfo> Friends { get; set; } = new List<FriendInfo>();
        public List<FriendInfo> Requests { get; set; } = new List<FriendInfo>();
       public List<ClubMemberinfo> clubMemberinfos { get; set; } = new List<ClubMemberinfo>();
      public List<Notification> Notifications { get; set; } = new List<Notification>();
        // login data
        public DateTime LastLogin { get; set; }
        public string? LastIp { get; set; }
        public string? Device { get; set; }
        
    }

    // Tüm hesapları yükle
    public static void LoadAccounts()
    {
        Console.WriteLine("hesaplar yükleniyor...");
        if (File.Exists(savePath))
        {
            var json = File.ReadAllText(savePath);
            accounts = JsonConvert.DeserializeObject<Dictionary<string, AccountData>>(json)
                       ?? new Dictionary<string, AccountData>();

            // maxAccountId güncelle
            foreach (var account in accounts.Values)
            {
                if (account.Id >= maxAccountId)
                    maxAccountId = account.Id + 1;

                AccountCache.Cache(account);
            }

            Console.WriteLine($"[AccountManager] {accounts.Count} hesap yüklendi.");
        }
        else
        {
            Logger.errorslog("[AccountManager] accounts.json bulunamadı, yeni dosya oluşturulacak.");
            File.Create(savePath).Close();
            
        }
    }

    // Hesap kaydet
    public static void SaveAccounts()
    {
        var json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
        File.WriteAllText(savePath, json);
        Console.WriteLine("[AccountManager] Hesaplar kaydedildi.");
    }

    // Hesap oluştur
    public static AccountData CreateAccount(string dil, string username = "arda64best")
    {
        string accountId = TokenManager.GeneratePlayerId(); // senin TokenManager metodun
        var newAccount = new AccountData
        {
            Id = maxAccountId,
            AccountId = accountId,
            Username = username,
            Dil = dil,
            Premium = 0,
            Avatarid = 1,
            Namecolorid = 1,
            Token = TokenManager.GenerateNumericToken(),
            LastLogin = DateTime.Now,
            Clubid = -1
        };

        accounts[accountId] = newAccount;
        maxAccountId++;
         AccountCache.Cache(newAccount);

        SaveAccounts();

        Console.WriteLine($"[AccountManager] Yeni hesap oluşturuldu: {username} (ID: {newAccount.Id}, AccountId: {newAccount.AccountId}) token:{newAccount.Token}");

        return newAccount;
    }
    public static void Getaccountinfo(string id)
    {
        var account = LoadAccount(id);
        if (account != null)
            Console.WriteLine($"isim: {account.Username}\n avatarid : {account.Avatarid} \n colorid: {account.Namecolorid}\n  son giriş: {account.LastLogin} \n Dil: {account.Dil} \n clubid: {account.Clubid}"); //
        if (account.Clubid != -1)
        {
         //   var club = ClubManager.LoadClub(account.Clubid);
           // Console.WriteLine($"kulup adı :  {club.ClubName} \n toplam kişi: {club.Members.Count}\n toplam kupa {club.TotalKupa} ");
            }
    }

    // Hesap yükle (AccountId ile)
    public static AccountData LoadAccount(string accountId)
    {
        if (accounts.TryGetValue(accountId, out var account))
        {
            account.LastLogin = DateTime.Now;
            return account;
        }

        Console.WriteLine("[AccountManager] Kullanıcı bulunamadı: " + accountId);
        return null;
    }

    // Ban işlemi
    public static void Ban(AccountData account, string sebep = "sebep belirtilmedi")
    {
        account.Banned = true;
        account.Banreason = sebep;
        SaveAccounts();
        Logger.genellog($"[BAN] {account.Username}({account.AccountId}) banlandı. Sebep: {sebep}");
    }
}
