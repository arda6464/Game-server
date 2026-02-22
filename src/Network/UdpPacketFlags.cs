namespace Network
{
    [Flags]
    public enum UdpPacketFlags : byte
    {
        None = 0,         // Unreliable (0x00)
        Reliable = 1,     // Karşıdan ACK bekleniyor (0x01)
        Ack = 2           // Bu paket sadece bir ACK onayıdır, payload kullanılmaz (0x02)
    }
}
