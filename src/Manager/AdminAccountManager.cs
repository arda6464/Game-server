using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

public class AdminAccount
{
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
    public string LastIp { get; set; } = "N/A";
}

public static class AdminAccountManager
{
    private static string _filePath = "admin_accounts.json";
    private static List<AdminAccount> _accounts = new List<AdminAccount>();

    public static void Initialize()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _accounts = JsonConvert.DeserializeObject<List<AdminAccount>>(json) ?? new List<AdminAccount>();
            }

            // Eğer hiç hesap yoksa default bir admin oluştur
            if (_accounts.Count == 0)
            {
                CreateAccount("admin", "admin123", "Owner");
                Logger.genellog("[AdminAuth] Hiç hesap bulunamadı, varsayılan hesap oluşturuldu: admin / admin123");
            }
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[AdminAuth] Başlatma hatası: {ex.Message}");
        }
    }

    public static bool CreateAccount(string username, string password, string role = "Admin")
    {
        if (_accounts.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            return false;

        var account = new AdminAccount
        {
            Username = username,
            PasswordHash = HashPassword(password),
            Role = role,
            CreatedAt = DateTime.Now,
            LastLogin = DateTime.MinValue
        };

        _accounts.Add(account);
        Save();
        return true;
    }

    public static bool DeleteAccount(string username)
    {
        var account = _accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (account == null) return false;

        // Owner silinemez (koruma)
        if (account.Role == "Owner") return false;

        _accounts.Remove(account);
        Save();
        return true;
    }

    public static List<AdminAccount> GetAccounts()
    {
        return _accounts.ToList();
    }

    public static AdminAccount? Authenticate(string username, string password, string ip = "N/A")
    {
        var account = _accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (account == null) return null;

        if (account.PasswordHash == HashPassword(password))
        {
            account.LastLogin = DateTime.Now;
            account.LastIp = ip;
            Save();
            return account;
        }

        return null;
    }

    private static void Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_accounts, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[AdminAuth] Kaydetme hatası: {ex.Message}");
        }
    }

    private static string HashPassword(string password)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
