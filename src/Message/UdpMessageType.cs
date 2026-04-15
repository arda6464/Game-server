/// <summary>
/// UDP üzerinden gönderilen paketlerin mesaj tipleri.
/// byte (1 byte) kullanılır — TCP'deki MessageType'tan (short/2 byte) farklıdır.
/// </summary>
public enum UdpMessageType : byte
{
    Connect = 0,
    ConnectResponse = 1,
    Move = 2,
    Shoot = 3,
    Input = 4,
    Ping = 5,
    Pong=6, 

}
