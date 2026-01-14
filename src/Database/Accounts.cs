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
        // security data
        public int Id { get; set; }
        public string? AccountId { get; set; }
        public string? Token { get; set; }
        // account data
        public string? Username { get; set; }
        public int Trophy { get; set; }
        public string? Dil { get; set; }
        public int Premium { get; set; }
        public DateTime PremiumEndTime { get; set; }
        public bool Banned { get; set; }
        public string? Banreason { get; set; }
        public int Avatarid { get; set; }
        public int Namecolorid { get; set; }
        public int Level { get; set; }
        public int Gems { get; set; }
        public string? ClubName { get; set; }
        public int Clubid { get; set; }
        public ClubRole clubRole { get; set; }
        public bool TicketBan { get; set; } = false;
        public bool ChatBan { get; set; } = false;

        public List<FriendInfo> Friends { get; set; } = new List<FriendInfo>();
        public List<FriendInfo> Requests { get; set; } = new List<FriendInfo>();
        public List<Notfication> Notfications { get; set; } = new List<Notfication>();
        public List<Notfication> inboxesNotfications { get; set; } = new List<Notfication>();
        public List<Role.Roles> Roles { get; set; } = new List<Role.Roles>();
        public List<SupportTicketData> Tickets { get; set; } =  new List<SupportTicketData>();
        // login data
        public DateTime LastLogin { get; set; }
        public string? LastIp { get; set; }
        public string? Device { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

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
        // Cache'den de hesapları güncelle (eğer cache'deki daha güncel ise)
        foreach (var cachedAccount in AccountCache.GetCachedAccounts())
        {
            if (!accounts.ContainsKey(cachedAccount.Key))
            {
                accounts[cachedAccount.Key] = cachedAccount.Value;
            }
        }

        var json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
        File.WriteAllText(savePath, json);
        //Console.WriteLine("[AccountManager] Hesaplar kaydedildi.");
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
            Clubid = -1,
            Trophy = 0,
            

        };

        accounts[accountId] = newAccount;
        maxAccountId++;
        AccountCache.Cache(newAccount);
        Notfication notification = new Notfication
        {
            Id = 10,
            Title = "1.2!",
            Message = "Düzeltilen bazı şeyler:\n Kulüp oluşturma  hatası\n Markette ürünleri tam görememe hatası\n Avatar değiştirememe hatası\n kulüp artık daha stabil bir görüntü(hala hatalı)",
            ButtonText = "Tamam",
            IsViewed = false
        };
        newAccount.Notfications.Add(notification);
        SaveAccounts();

        Console.WriteLine($"[AccountManager] Yeni hesap oluşturuldu: {username} (ID: {newAccount.Id}, AccountId: {newAccount.AccountId}) token:{newAccount.Token}");

        return newAccount;
    }
    public static void Getaccountinfo(string id)
    {
        var account = LoadAccount(id);
        if (account != null)
            Console.WriteLine($"isim: {account.Username}\n avatarid : {account.Avatarid} \n colorid: {account.Namecolorid}\n  son giriş: {account.LastLogin} \n Dil: {account.Dil} \n clubid: {account.Clubid}\n club name: {account.ClubName}"); //
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

    
    public static void DeleteNotfications()
    {
        foreach (AccountData account in accounts.Values)
        {
            account.Notfications.Clear();

        }
        Console.WriteLine("Accountsların notficationları silindi");
    }
    public static void AddRole(AccountData account, Role.Roles role)
    {
        if (account.Roles.Contains(role))
        {
            Logger.genellog($"{account.Username} ({account.AccountId}) kişisine {role}'ü eklenmeye çalıştı fakat zaten var olduğu için eklenmedi");
            return;
        }
        if (account.Roles.Count == 4)
        {
            Logger.genellog($"{account.Username} ({account.AccountId}) kişisine {role}'ü eklenmeye çalıştı fakat zaten var olduğu için eklenmedi");
        }
        account.Roles.Add(role);
        Logger.genellog($"{account.Username} ({account.AccountId}) kişisine {role}'ü eklendi!");
    }
    public static void RemoveRole(AccountData account, Role.Roles role)
    {
        if (!account.Roles.Contains(role))
        {
            Logger.genellog($"{account.Username} ({account.AccountId}) kişisinden {role}'ü kaldırılmaya çalıştı fakat zaten o role sahip olmadığı için kaldırılmadı");
            return;
        }
        account.Roles.Add(role);
        Logger.genellog($"{account.Username} ({account.AccountId}) kişisinden {role}'ü kaldırıldı!");
    }


     public static List<AccountData> GetTop100Players()
    {
        return accounts.Values
            .Where(a => !a.Banned) // Banlıları çıkar
            .OrderByDescending(a => a.Trophy) // Sadece kupaya göre sırala
            .Take(100) // İlk 100
            .ToList();
    }

    // Oyuncunun sıralamasını bul
    public static int GetPlayerRank(string accountId)
    {

        var sortedPlayers = accounts.Values
            .Where(a => !a.Banned)
            .OrderByDescending(a => a.Trophy)
            .ToList();

        // Oyuncunun sırasını bul (1'den başlar)
        int rank = sortedPlayers.FindIndex(a => a.AccountId == accountId) + 1;
        return rank;
    }
    public static bool CheckMail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            Console.WriteLine("[CheckMail] Geçersiz e-posta adresi");
            return false;
        }


        string normalizedEmail = email.Trim().ToLower();


        foreach (var account in accounts.Values)
        {
            if (!string.IsNullOrEmpty(account.Email) &&
                account.Email.Trim().ToLower() == normalizedEmail)
            {
                Console.WriteLine($"[CheckMail] E-posta zaten kayıtlı: {email} (Kullanıcı: {account.Username} ({account.AccountId}))");
                return true;
            }
        }

        //  Console.WriteLine($"[CheckMail] E-posta kayıtlı değil: {email}");
        return false;
    }
public static AccountData FindAccountByEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
    {
        Console.WriteLine("[FindAccountByEmail] Geçersiz e-posta adresi");
        return null;
    }

    string normalizedEmail = email.Trim().ToLower();
    
    foreach (var account in accounts.Values)
    {
        if (!string.IsNullOrEmpty(account.Email) && 
            account.Email.Trim().ToLower() == normalizedEmail)
        {
            Console.WriteLine($"[FindAccountByEmail] Hesap bulundu: {account.Username} (ID: {account.AccountId})");
            return account;
        }
    }
    
    Console.WriteLine($"[FindAccountByEmail] E-posta ile hesap bulunamadı: {email}");
    return null;
}
    
    
   
    
   
          
}
