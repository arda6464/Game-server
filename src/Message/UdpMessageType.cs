/// <summary>
/// UDP üzerinden gönderilen paketlerin mesaj tipleri.
/// byte (1 byte) kullanılır — TCP'deki MessageType'tan (short/2 byte) farklıdır.
/// </summary>
public enum UdpMessageType : byte
{
    Connect ,
    ConnectResponse,
    Move,
    Shoot,
    Input,
    PickupRequest,
    PickupResponse,
    LootTaken ,
    GiveUp,
    ItemAdded,
    ChangeSlotRequest,
    ChangeSlot,
    Ping,
    Pong,
}
