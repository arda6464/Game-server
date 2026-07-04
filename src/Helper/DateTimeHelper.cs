using System;

/// <summary>
/// DateTime <-> long (Unix Timestamp) dönüşümleri için yardımcı sınıf.
/// Network üzerinden tarih göndermek için DateTime yerine her zaman long kullan.
/// Tüm hesaplamalar UTC bazlıdır (sunucu/istemci saat dilimi farklarından etkilenmez).
/// </summary>
public static class DateTimeHelper
{
    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ----------------------------------------------------------------
    // DateTime -> long
    // ----------------------------------------------------------------

    /// <summary>
    /// DateTime.UtcNow değerini saniye cinsinden Unix timestamp'e çevirir.
    /// Network'e bunu gönder.
    /// </summary>
    public static long NowToUnixSeconds()
    {
        return ToUnixSeconds(DateTime.UtcNow);
    }

    /// <summary>
    /// DateTime.UtcNow değerini milisaniye cinsinden Unix timestamp'e çevirir.
    /// </summary>
    public static long NowToUnixMilliseconds()
    {
        return ToUnixMilliseconds(DateTime.UtcNow);
    }

    /// <summary>
    /// Verilen DateTime'ı saniye cinsinden Unix timestamp'e çevirir.
    /// DateTime Local ise otomatik UTC'ye çevrilir.
    /// </summary>
    public static long ToUnixSeconds(DateTime dateTime)
    {
        DateTime utc = EnsureUtc(dateTime);
        return (long)(utc - UnixEpoch).TotalSeconds;
    }

    /// <summary>
    /// Verilen DateTime'ı milisaniye cinsinden Unix timestamp'e çevirir.
    /// DateTime Local ise otomatik UTC'ye çevrilir.
    /// </summary>
    public static long ToUnixMilliseconds(DateTime dateTime)
    {
        DateTime utc = EnsureUtc(dateTime);
        return (long)(utc - UnixEpoch).TotalMilliseconds;
    }

    // ----------------------------------------------------------------
    // long -> DateTime
    // ----------------------------------------------------------------

    /// <summary>
    /// Saniye cinsinden Unix timestamp'ten UTC DateTime üretir.
    /// </summary>
    public static DateTime FromUnixSeconds(long unixSeconds)
    {
        return UnixEpoch.AddSeconds(unixSeconds);
    }

    /// <summary>
    /// Milisaniye cinsinden Unix timestamp'ten UTC DateTime üretir.
    /// </summary>
    public static DateTime FromUnixMilliseconds(long unixMilliseconds)
    {
        return UnixEpoch.AddMilliseconds(unixMilliseconds);
    }

    /// <summary>
    /// Saniye cinsinden Unix timestamp'ten, cihazın yerel saatine çevrilmiş DateTime üretir.
    /// UI'da kullanıcıya göstereceğin saat için bunu kullan.
    /// </summary>
    public static DateTime FromUnixSecondsToLocal(long unixSeconds)
    {
        return FromUnixSeconds(unixSeconds).ToLocalTime();
    }

    /// <summary>
    /// Milisaniye cinsinden Unix timestamp'ten, cihazın yerel saatine çevrilmiş DateTime üretir.
    /// </summary>
    public static DateTime FromUnixMillisecondsToLocal(long unixMilliseconds)
    {
        return FromUnixMilliseconds(unixMilliseconds).ToLocalTime();
    }

    // ----------------------------------------------------------------
    // Aradaki süre / kalan süre hesaplamaları
    // ----------------------------------------------------------------

    /// <summary>
    /// İki unix timestamp (saniye) arasındaki farkı TimeSpan olarak döner.
    /// </summary>
    public static TimeSpan GetDifference(long fromUnixSeconds, long toUnixSeconds)
    {
        return FromUnixSeconds(toUnixSeconds) - FromUnixSeconds(fromUnixSeconds);
    }

    /// <summary>
    /// Verilen unix timestamp (saniye), şu ana göre geçmişte mi kontrol eder.
    /// Süresi dolan event/buff/cooldown kontrolleri için kullanışlı.
    /// </summary>
    public static bool IsPast(long unixSeconds)
    {
        return unixSeconds <= NowToUnixSeconds();
    }

    /// <summary>
    /// Verilen unix timestamp (saniye) şu ana kadar ne kadar süre kaldığını saniye olarak döner.
    /// Süre geçmişse 0 döner (negatif dönmez).
    /// </summary>
    public static long GetRemainingSeconds(long targetUnixSeconds)
    {
        long remaining = targetUnixSeconds - NowToUnixSeconds();
        return remaining > 0 ? remaining : 0;
    }

    // ----------------------------------------------------------------
    // Formatlama
    // ----------------------------------------------------------------

    /// <summary>
    /// Unix timestamp'i (saniye) yerel saate çevirip istenen formatta string döner.
    /// Örnek format: "dd.MM.yyyy HH:mm"
    /// </summary>
    public static string FormatLocal(long unixSeconds, string format = "dd.MM.yyyy HH:mm")
    {
        return FromUnixSecondsToLocal(unixSeconds).ToString(format);
    }

    /// <summary>
    /// Saniye cinsinden bir süreyi "HH:mm:ss" formatında string'e çevirir.
    /// Geri sayım / cooldown UI'ları için kullanışlı.
    /// </summary>
    public static string SecondsToTimeString(long totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        TimeSpan span = TimeSpan.FromSeconds(totalSeconds);

        if (span.Days > 0)
            return string.Format("{0}g {1:D2}:{2:D2}:{3:D2}", span.Days, span.Hours, span.Minutes, span.Seconds);

        return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)span.TotalHours, span.Minutes, span.Seconds);
    }

    // ----------------------------------------------------------------
    // Yardımcı
    // ----------------------------------------------------------------

    private static DateTime EnsureUtc(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
            return dateTime;

        if (dateTime.Kind == DateTimeKind.Local)
            return dateTime.ToUniversalTime();

        // Unspecified ise UTC olduğunu varsay (network'ten gelen değerler için güvenli varsayım)
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }
}