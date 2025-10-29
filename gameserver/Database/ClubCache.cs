using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public static class ClubCache
{
    private static ConcurrentDictionary<int, Club> CachedClubs = new();
    private static bool started = true;

    // Cache'deki kulüp sayısı
    public static int Count => CachedClubs.Count;

    // Cache'i başlat
    public static void Init()
    {
        ClubManager.Allclubload();
        Thread _thread = new Thread(Update);
        _thread.Start();
    }

    // Auto-save thread
    private static void Update()
    {
        while (started)
        {
            SaveAll();
            Logger.genellog("[ClubCache] Cache kaydedildi");
            Thread.Sleep(1000 * 120); // 2 dakikada bir
        }
    }

    // Cache'deki tüm kulüpleri kaydet
    public static void SaveAll()
    {
        try
        {
            ClubManager.Save();
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[ClubCache] Hata kaydederken: {ex.Message}");
        }
    }

    // Cache'e kulüp ekle
    public static void Cache(Club club)
    {
        CachedClubs[club.ClubId] = club;
    }

    // Cache'den kulüp yükle
    public static Club Load(int clubId)
    {
        CachedClubs.TryGetValue(clubId, out var club);
        return club;
    }

    // Cache'de var mı?
    public static bool IsCached(int clubId)
    {
        return CachedClubs.ContainsKey(clubId);
    }

    // Cache'i durdur
    public static void Stop()
    {
        started = false;
        SaveAll();
    }
    
    // ClubManager için public property
    public static ConcurrentDictionary<int, Club> GetCachedClubs() => CachedClubs;
}

