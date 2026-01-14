using System;
using System.Collections.Concurrent;

public enum VerificationType
{
    Login = 2,
    Create = 1,
    ForgotPassword=3
}

public static class VerifyManager
{
    // AccountId → VerificationData mapping
    private static ConcurrentDictionary<string, VerificationData> _dataStore = new ConcurrentDictionary<string, VerificationData>();

    public class VerificationData
    {
        public string Email { get; set; }
        public string Code { get; set; }
        public string Password{ get; set; }
        public VerificationType Type { get; set; }
       
    }

    // 1. CREATE DATA - Tek metod
    public static void CreateData(string accountId, VerificationData data)
    {
        // Store'a ekle
        _dataStore[accountId] = data;

        Console.WriteLine($"[VerifyManager] Data created for: {accountId}");
    }

    // 2. GET DATA - Tek metod
    public static VerificationData GetData(string accountId)
    {
        if (_dataStore.TryGetValue(accountId, out var data))
        {
            return data;
        }

        // Süresi dolmuşsa temizle
        _dataStore.TryRemove(accountId, out _);
        return null;
    }

    // Bonus: Temizleme (opsiyonel)
    public static void RemoveData(string accountId)
    {
        _dataStore.TryRemove(accountId, out _);
        Console.WriteLine($"[VerifyManager] Data removed for: {accountId}");
    }

    
}