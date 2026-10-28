using System;

public static class MessageManager
{
    public delegate void PacketHandlerDelegate(Session session, byte[] data);
    private static readonly System.Collections.Generic.Dictionary<
        MessageType,
        PacketHandlerDelegate
    > _handlers = new System.Collections.Generic.Dictionary<MessageType, PacketHandlerDelegate>();

    public static void Init()
    {
        Console.WriteLine("[MessageManager] Handler'lar yükleniyor...");

        // 1. Yeni sistem: IGameMessage implement eden handler'lar
        var handlerTypes = System
            .Reflection.Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                !t.IsAbstract
                && !t.IsInterface
                && t.GetCustomAttributes(typeof(PacketHandlerAttribute), false).Length > 0
                && typeof(IGameMessage).IsAssignableFrom(t)
            );

        foreach (var type in handlerTypes)
        {
            var attr = (PacketHandlerAttribute)
                type.GetCustomAttributes(typeof(PacketHandlerAttribute), false)[0];
            if (!_handlers.ContainsKey(attr.Type))
            {
                var instance = (IGameMessage)Activator.CreateInstance(type);
                _handlers.Add(attr.Type, (session, data) => instance.Handle(session, data));
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
                Logger.errorslog(
                    $"[MessageManager] Handler hatası ({type}): {realEx.Message}\n{realEx.StackTrace}"
                );
                return;
            }
        }

        if (type != MessageType.Ping)
            Console.WriteLine(
                $"[MessageManager] {session.ID} kullanıcısından {type.ToString()} mesajı alındı."
            );

        // ESKİ SİSTEM (Switch-Case - Geriye uyumluluk için)
        // Eğer handler bulunamazsa
        Logger.errorslog($"[MESSAGE MANAGER] Handler bulunamadı: {type} ({value})");
    }

}
