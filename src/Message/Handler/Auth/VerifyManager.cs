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
    // Player ID (int) → VerificationData mapping
    private static ConcurrentDictionary<int, VerificationData> _dataStore = new ConcurrentDictionary<int, VerificationData>();

    public class VerificationData
    {
        public string Email { get; set; }
        public string Code { get; set; }
        public string Password{ get; set; }
        public VerificationType Type { get; set; }
    }

    public static void CreateData(int accountId, VerificationData data)
    {
        _dataStore[accountId] = data;
        Console.WriteLine($"[VerifyManager] Data created for Player ID: {accountId}");
    }

    public static VerificationData GetData(int accountId)
    {
        if (_dataStore.TryGetValue(accountId, out var data))
        {
            return data;
        }

        _dataStore.TryRemove(accountId, out _);
        return null;
    }

    public static void RemoveData(int accountId)
    {
        _dataStore.TryRemove(accountId, out _);
        Console.WriteLine($"[VerifyManager] Data removed for Player ID: {accountId}");
    }
}