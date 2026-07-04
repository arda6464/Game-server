using System;

public static class MessageManager
{

    public delegate void PacketHandlerDelegate(Session session, byte[] data);
    private static readonly System.Collections.Generic.Dictionary<MessageType, PacketHandlerDelegate> _handlers = new System.Collections.Generic.Dictionary<MessageType, PacketHandlerDelegate>();

    public static void Init()
    {
        Console.WriteLine("[MessageManager] Handler'lar yükleniyor...");
        var methods = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m?.DeclaringType != null && m.DeclaringType.GetCustomAttributes(typeof(PacketHandlerAttribute), false).Length > 0 && m.Name == "Handle")
            .ToArray();

        foreach (var method in methods)
        {
            var attr = (PacketHandlerAttribute)method.DeclaringType.GetCustomAttributes(typeof(PacketHandlerAttribute), false)[0];
            if (!_handlers.ContainsKey(attr.Type))
            {
                var parameters = method.GetParameters();
                PacketHandlerDelegate? handler = null;

                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(Session))
                {
                    var action = (Action<Session>)Delegate.CreateDelegate(typeof(Action<Session>), method);
                    handler = (session, data) => action(session);
                }
                else if (parameters.Length == 2 && parameters[1].ParameterType == typeof(byte[]))
                {
                    var action = (Action<Session, byte[]>)Delegate.CreateDelegate(typeof(Action<Session, byte[]>), method);
                    handler = (session, data) => action(session, data);
                }

                if (handler != null)
                {
                    _handlers.Add(attr.Type, handler);
                }
                else
                {
                    Logger.errorslog($"[MessageManager] Gecersiz handler imzası: {method.DeclaringType.Name}.Handle");
                }
            }
        }
        Console.WriteLine($"[MessageManager] Toplam {_handlers.Count} handler yüklendi.");
    }

    public static void HandleMessage(Session session, byte[] data, int length)
    {
        int value;
        byte[] payload;
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            buffer.WriteBytes(new ReadOnlySpan<byte>(data, 0, length), true);
            value = buffer.ReadVarInt();
            int payloadLength = (int)(buffer.Length - buffer.Position);
            payload = buffer.ReadBytes(null!, payloadLength);
        }
        MessageType type = (MessageType)value;
        Console.WriteLine($"[PACKET] bir {type} alındı");

        // Trafiği kaydet
        TrafficMonitor.RecordIncoming(type, payload.Length);

        // ÖNCE YENİ SİSTEME BAK
        if (_handlers.TryGetValue(type, out var handler))
        {
            try
            {
                handler(session, payload);
                return; // Yeni sistemde işlendi, switch-case'e girme
            }
            catch (Exception ex)
            {
                var realEx = ex.InnerException ?? ex;
                Logger.errorslog($"[MessageManager] Handler hatası ({type}): {realEx.Message}\n{realEx.StackTrace}");
                return;
            }
        }

        if (type != MessageType.Ping)
            Console.WriteLine($"[MessageManager] {session.ID} kullanıcısından {type.ToString()} mesajı alındı.");

        // ESKİ SİSTEM (Switch-Case - Geriye uyumluluk için)
        // Eğer handler bulunamazsa
        Logger.errorslog($"[MESSAGE MANAGER] Handler bulunamadı: {type} ({value})");

    }



    public static void HandleUdpMessage(Session session, byte[] data, int sequenceNumber)
    {
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            buffer.WriteBytes(data);
            UdpMessageType messageType = (UdpMessageType)buffer.ReadVarInt();

            // Trafiği kaydet
            TrafficMonitor.RecordIncomingUdp(messageType, data.Length);

            // Connect ve Ping gibi kontrol paketleri her zaman işlenir (seqNo filtresi uygulanmaz)
            /* switch (messageType)
            {
                
            }

            // Unreliable paketler (Move, Input, Shoot) için eski seqNo kontrolü
           if (!session.IsNewUnreliableSequence(sequenceNumber))
            {
                if (sequenceNumber != 0)
                    Console.WriteLine($"[UDP] Eski paket reddedildi! Gelen: {sequenceNumber}, Son Başarılı: {session.LastIncomingUnreliableSeq} Account: {session.AccountId}");
                return;
            }*/

            switch (messageType)
            {
                case UdpMessageType.Connect:
                    UdpGameHandler.HandleConnect(session);
                    break;
                case UdpMessageType.Ping:
                    UdpGameHandler.HandlePing(session, buffer);
                    break;
                case UdpMessageType.Input:
                    UdpGameHandler.HandleInput(session, buffer, sequenceNumber);
                    break;
                case UdpMessageType.PickupRequest:
                    UdpGameHandler.HandlePickUpRequest(session, buffer, sequenceNumber);
                    break;
                case UdpMessageType.ChangeSlotRequest:
                    UdpGameHandler.HandleChangeSlotRequest(session, buffer, sequenceNumber);
                    break;
                default:
                    Console.WriteLine($"[UDP] Bilinmeyen paket: {messageType}");
                    break;
            }
        }
    }
}