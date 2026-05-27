using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public static class TrafficMonitor
{
    private class PacketStats
    {
        public long Count;
        public long TotalBytes;
        public int MinBytes = int.MaxValue;
        public int MaxBytes = int.MinValue;

        public void Record(int bytes)
        {
            Interlocked.Increment(ref Count);
            Interlocked.Add(ref TotalBytes, bytes);
            // Min/Max are less critical but could be fixed with CompareExchange if needed
            if (bytes < MinBytes) MinBytes = bytes;
            if (bytes > MaxBytes) MaxBytes = bytes;
        }
    }

    private static ConcurrentDictionary<string, PacketStats> IncomingStats = new();
    private static ConcurrentDictionary<string, PacketStats> OutgoingStats = new();

    // throughput history (KB/s)
    public struct ThroughputEntry { public DateTime Time; public double In; public double Out; }
    private static List<ThroughputEntry> History = new();
    private static long _lastInBytes;
    private static long _lastOutBytes;
    private static Timer _timer;

    static TrafficMonitor()
    {
        _timer = new Timer(_ => UpdateThroughput(), null, 1000, 1000);
    }

    private static void UpdateThroughput()
    {
        long currentIn = IncomingStats.Values.Sum(s => Interlocked.Read(ref s.TotalBytes));
        long currentOut = OutgoingStats.Values.Sum(s => Interlocked.Read(ref s.TotalBytes));

        double diffIn = (currentIn - _lastInBytes) / 1024.0;
        double diffOut = (currentOut - _lastOutBytes) / 1024.0;

        // Reset check
        if (diffIn < 0) diffIn = 0;
        if (diffOut < 0) diffOut = 0;

        _lastInBytes = currentIn;
        _lastOutBytes = currentOut;

        lock (History)
        {
            History.Add(new ThroughputEntry { Time = DateTime.Now, In = diffIn, Out = diffOut });
            if (History.Count > 60) History.RemoveAt(0);
        }
    }

    public static List<ThroughputEntry> GetHistory()
    {
        lock (History) return new List<ThroughputEntry>(History);
    }

    public static void RecordIncoming(MessageType type, int length)
    {
        string key = "[TCP] " + type.ToString();
        var stats = IncomingStats.GetOrAdd(key, _ => new PacketStats());
        stats.Record(length);
    }

    public static void RecordIncomingUdp(UdpMessageType type, int length)
    {
        string key = "[UDP] " + type.ToString();
        var stats = IncomingStats.GetOrAdd(key, _ => new PacketStats());
        stats.Record(length);
    }

    public static void RecordOutgoing(MessageType type, int length)
    {
        string key = "[TCP] " + type.ToString();
        var stats = OutgoingStats.GetOrAdd(key, _ => new PacketStats());
        stats.Record(length);
    }

    public static void RecordOutgoingUdp(UdpMessageType type, int length)
    {
        string key = "[UDP] " + type.ToString();
        var stats = OutgoingStats.GetOrAdd(key, _ => new PacketStats());
        stats.Record(length);
    }

    // Generic fallback
    public static void RecordOutgoingRaw(int length)
    {
        var stats = OutgoingStats.GetOrAdd("[RAW] Outbound", _ => new PacketStats());
        stats.Record(length);
    }

    public static object GetDetailedReport()
    {
        return new
        {
            incoming = IncomingStats.Select(kvp => new
            {
                type = kvp.Key,
                count = Interlocked.Read(ref kvp.Value.Count),
                bytes = Interlocked.Read(ref kvp.Value.TotalBytes),
                avg = Interlocked.Read(ref kvp.Value.Count) > 0 ? Interlocked.Read(ref kvp.Value.TotalBytes) / Interlocked.Read(ref kvp.Value.Count) : 0
            }).OrderByDescending(x => x.bytes).Take(20).ToList(),
            outgoing = OutgoingStats.Select(kvp => new
            {
                type = kvp.Key,
                count = Interlocked.Read(ref kvp.Value.Count),
                bytes = Interlocked.Read(ref kvp.Value.TotalBytes),
                avg = Interlocked.Read(ref kvp.Value.Count) > 0 ? Interlocked.Read(ref kvp.Value.TotalBytes) / Interlocked.Read(ref kvp.Value.Count) : 0
            }).OrderByDescending(x => x.bytes).Take(20).ToList()
        };
    }

    public static string GetReport()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== TRAFİK ANALİZ RAPORU ===");
        sb.AppendLine($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine();

        sb.AppendLine("--- GELEN PAKETLER (Inbound) ---");
        AppendStats(sb, IncomingStats);

        sb.AppendLine();
        sb.AppendLine("--- GİDEN PAKETLER (Outbound) ---");
        AppendStats(sb, OutgoingStats);

        return sb.ToString();
    }

    private static void AppendStats(StringBuilder sb, ConcurrentDictionary<string, PacketStats> statsDict)
    {
        var sorted = statsDict.OrderByDescending(x => Interlocked.Read(ref x.Value.TotalBytes)).ToList();

        if (!sorted.Any())
        {
            sb.AppendLine("Henüz veri yok.");
            return;
        }

        sb.AppendLine(string.Format("{0,-30} | {1,-8} | {2,-12} | {3,-10}", "Paket Adı", "Adet", "Toplam (KB)", "Ort (B)"));
        sb.AppendLine(new string('-', 70));

        foreach (var item in sorted)
        {
            long bytes = Interlocked.Read(ref item.Value.TotalBytes);
            long count = Interlocked.Read(ref item.Value.Count);
            double totalKb = bytes / 1024.0;
            double avg = count > 0 ? bytes / (double)count : 0;

            sb.AppendLine(string.Format("{0,-30} | {1,-8} | {2,-12:F2} | {3,-10:F0}",
                item.Key,
                count,
                totalKb,
                avg));
        }
    }

    public static void Reset()
    {
        IncomingStats.Clear();
        OutgoingStats.Clear();
        _lastInBytes = 0;
        _lastOutBytes = 0;
    }
}
