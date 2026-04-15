using System;
using System.Collections.Concurrent;
using System.Net;

public enum InviteType
{
    Team,
    Friend
}

public class InviteData
{
    public InviteType Type { get; set; }
    public int TargetID { get; set; } // TeamID or PlayerID
    public DateTime ExpiresAt { get; set; }
    public int OwnerID { get; set; } // Player ID
    public int Clicks { get; set; } = 0;
}

public static class InviteManager
{
    private static ConcurrentDictionary<string, InviteData> _invites = new();
    private static string? _detectedIp;

    private static string GetServerIp()
    {
        if (!string.IsNullOrEmpty(Config.Instance?.WebsiteUrl) && 
            !Config.Instance.WebsiteUrl.Contains("yourdomain.com") && 
            !Config.Instance.WebsiteUrl.Contains("IP_ADRESI"))
            return Config.Instance.WebsiteUrl.TrimEnd('/');

        if (_detectedIp == null)
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !ip.ToString().StartsWith("127."))
                    {
                        _detectedIp = ip.ToString();
                        break;
                    }
                }
            }
            catch { }
            _detectedIp ??= "127.0.0.1";
        }

        string port = Config.Instance?.Port.ToString() ?? "5000";
        return $"http://{_detectedIp}:{port}";
    }

    public static string CreateTeamInvite(int teamId, int ownerId, int expiryMinutes = 60)
    {
        var invite = new InviteData
        {
            Type = InviteType.Team,
            TargetID = teamId,
            OwnerID = ownerId,
            ExpiresAt = DateTime.Now.AddMinutes(expiryMinutes)
        };
        
        string token = $"team_{teamId}_{Guid.NewGuid().ToString().Substring(0, 4)}";
        _invites[token] = invite;
        
        string link = $"{GetServerIp()}/invite/{token}";
        Console.WriteLine($"[InviteManager] Team invite: {link}");
        return link;
    }

    public static string CreateFriendInvite(int ownerId, int expiryMinutes = 60 * 24)
    {
        var invite = new InviteData
        {
            Type = InviteType.Friend,
            TargetID = ownerId,
            OwnerID = ownerId,
            ExpiresAt = DateTime.Now.AddMinutes(expiryMinutes)
        };
        
        string token = $"friend_{ownerId}";
        _invites[token] = invite;
        
        string link = $"{GetServerIp()}/invite/{token}";
        Console.WriteLine($"[InviteManager] Friend invite: {link}");
        return link;
    }

    public static InviteData? GetInvite(string token)
    {
        if (_invites.TryGetValue(token, out var invite))
        {
            if (invite.ExpiresAt > DateTime.Now)
                return invite;
            
            _invites.TryRemove(token, out _);
        }
        return null;
    }

    public static bool RemoveInvite(string token)
    {
        return _invites.TryRemove(token, out _);
    }

    public static void Cleanup()
    {
        var now = DateTime.Now;
        foreach (var key in _invites.Keys)
        {
            if (_invites[key].ExpiresAt < now)
            {
                _invites.TryRemove(key, out _);
            }
        }
    }
}
