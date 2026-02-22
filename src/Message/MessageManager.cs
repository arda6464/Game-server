using System;

public static class MessageManager
{
   
    private static System.Collections.Generic.Dictionary<MessageType, System.Reflection.MethodInfo> _handlers = new System.Collections.Generic.Dictionary<MessageType, System.Reflection.MethodInfo>();

    public static void Init()
    {
        Console.WriteLine("[MessageManager] Handler'lar yükleniyor...");
        var methods = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.DeclaringType.GetCustomAttributes(typeof(PacketHandlerAttribute), false).Length > 0 && m.Name == "Handle")
            .ToArray();

        foreach (var method in methods)
        {
            var attr = (PacketHandlerAttribute)method.DeclaringType.GetCustomAttributes(typeof(PacketHandlerAttribute), false)[0];
            if (!_handlers.ContainsKey(attr.Type))
            {
                _handlers.Add(attr.Type, method);
             //   Console.WriteLine($"[MessageManager] Yüklendi: {attr.Type} -> {method.DeclaringType.Name}");
            }
        }
        Console.WriteLine($"[MessageManager] Toplam {_handlers.Count} handler yüklendi.");
    }

    public static void HandleMessage(Session session, byte[] data)
    {
        short value = BitConverter.ToInt16(data, 0);
        MessageType type = (MessageType)value;

        // Trafiği kaydet
        TrafficMonitor.RecordIncoming(type, data.Length);

        // ÖNCE YENİ SİSTEME BAK
        if (_handlers.ContainsKey(type))
        {
            try 
            {
                // Parametre sayısına göre dinamik çağrı (bazı handlerlar sadece Session alır, bazıları Session + Data)
                var method = _handlers[type];
                var parameters = method.GetParameters();

                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(Session))
                {
                    method.Invoke(null, new object[] { session });
                }
                else if (parameters.Length == 2 && parameters[1].ParameterType == typeof(byte[]))
                {
                    method.Invoke(null, new object[] { session, data });
                }
                else
                {
                    Logger.errorslog($"[MessageManager] Handler parametre hatası: {type}");
                }
                return; // Yeni sistemde işlendi, switch-case'e girme
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[MessageManager] Handler hatası ({type}): {ex.Message}");
                return;
            }
        }

        if (type != MessageType.Ping)
         Console.WriteLine($"[MessageManager] {session.AccountId} kullanıcısından {type.ToString()} mesajı alındı.");
        
        // ESKİ SİSTEM (Switch-Case - Geriye uyumluluk için)
        // Eğer handler bulunamazsa
        Logger.errorslog($"[MESSAGE MANAGER] Handler bulunamadı: {type} ({value})");
       
    }


    
    public static void HandleUdpMessage(Session session, byte[] data, ushort sequenceNumber)
    {
         using (ByteBuffer buffer = new ByteBuffer())
         {
             buffer.WriteBytes(data);
             short messageType = buffer.ReadShort();
             
             switch ((MessageType)messageType)
             {
                 case MessageType.UdpMove:
                     UdpGameHandler.HandleMove(session, buffer, sequenceNumber);
                     break;
                 case MessageType.UdpShoot:
                     UdpGameHandler.HandleShoot(session, buffer, sequenceNumber);
                     break;
                 case MessageType.UdpInput:
                     UdpGameHandler.HandleInput(session, buffer, sequenceNumber);
                     break;
                 default:
                      Console.WriteLine($"[UDP] Bilinmeyen paket: {(MessageType)messageType}");
                     break;
             }
         }
    }
}