using System.Net;
public class ReliablePacket
{
    public int SequenceNumber { get; set; }
    public byte[]? Data { get; set; }
    public DateTime LastSentTime { get; set; }
    public int RetryCount { get; set; }
    public IPEndPoint? Target { get; set; }
}
