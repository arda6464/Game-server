using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            Count++;
            TotalBytes += bytes;
            if (bytes < MinBytes) MinBytes = bytes;
            if (bytes > MaxBytes) MaxBytes = bytes;
        }
    }

    private static ConcurrentDictionary<MessageType, PacketStats> IncomingStats = new();
    private static ConcurrentDictionary<MessageType, PacketStats> OutgoingStats = new();

    // throughput history (KB/s)
    public struct ThroughputEntry { public DateTime Time; public double In; public double Out; }
    private static List<ThroughputEntry> History = new();
    private static long _lastInBytes;
    private static long _lastOutBytes;
    private static System.Threading.Timer _timer;

    static TrafficMonitor()
    {
        _timer = new System.Threading.Timer(_ => UpdateThroughput(), null, 1000, 1000);
    }

    private static void UpdateThroughput()
    {
        long currentIn = IncomingStats.Values.Sum(s => s.TotalBytes);
        long currentOut = OutgoingStats.Values.Sum(s => s.TotalBytes);

        double diffIn = (currentIn - _lastInBytes) / 1024.0;
        double diffOut = (currentOut - _lastOutBytes) / 1024.0;

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
        var stats = IncomingStats.GetOrAdd(type, _ => new PacketStats());
        stats.Record(length);
    }

    public static void RecordOutgoing(byte[] data)
    {
        if (data.Length < 2) return;

        try
        {
            short opcode = BitConverter.ToInt16(data, 0);
            MessageType type = (MessageType)opcode;

            var stats = OutgoingStats.GetOrAdd(type, _ => new PacketStats());
            stats.Record(data.Length);
        }
        catch
        {
            // Invalid packet format or unknown opcode
        }
    }

    public static object GetDetailedReport()
    {
        return new
        {
            incoming = IncomingStats.Select(kvp => new
            {
                type = kvp.Key.ToString(),
                count = kvp.Value.Count,
                bytes = kvp.Value.TotalBytes,
                avg = kvp.Value.Count > 0 ? kvp.Value.TotalBytes / kvp.Value.Count : 0
            }).OrderByDescending(x => x.bytes).ToList(),
            outgoing = OutgoingStats.Select(kvp => new
            {
                type = kvp.Key.ToString(),
                count = kvp.Value.Count,
                bytes = kvp.Value.TotalBytes,
                avg = kvp.Value.Count > 0 ? kvp.Value.TotalBytes / kvp.Value.Count : 0
            }).OrderByDescending(x => x.bytes).ToList()
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

    private static void AppendStats(StringBuilder sb, ConcurrentDictionary<MessageType, PacketStats> statsDict)
    {
        var sorted = statsDict.OrderByDescending(x => x.Value.TotalBytes).ToList();

        if (!sorted.Any())
        {
            sb.AppendLine("Henüz veri yok.");
            return;
        }

        sb.AppendLine(string.Format("{0,-30} | {1,-8} | {2,-12} | {3,-10}", "Paket Adı", "Adet", "Toplam (KB)", "Ort (B)"));
        sb.AppendLine(new string('-', 70));

        foreach (var item in sorted)
        {
            double totalKb = item.Value.TotalBytes / 1024.0;
            double avg = item.Value.TotalBytes / (double)item.Value.Count;

            sb.AppendLine(string.Format("{0,-30} | {1,-8} | {2,-12:F2} | {3,-10:F0}",
                item.Key.ToString(),
                item.Value.Count,
                totalKb,
                avg));
        }
    }

    public static void Reset()
    {
        IncomingStats.Clear();
        OutgoingStats.Clear();
    }
}
