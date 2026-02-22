using System;


[PacketHandler(MessageType.FirstConnectionRequest)]
public static class FirstConnectionHandler
{
    
    public static void Handle(Session session, byte[] data)
    {
         string Keyversion = "ARDA64";
         bool Login = true;
         string Loginreason = string.Empty;
       


        // OKUMA
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(data);
        read.ReadShort(); // Packet ID atla
        
        var request = new FirstConnectionRequestPacket();
        request.Deserialize(read);
        
        string cihazadı = request.DeviceName;
        string device = request.DeviceModel;
        Console.WriteLine("cihaz adı: " + device);
        string ClientKey = request.ClientKey;
        
        read.Dispose();
        session.DeviceID = device;

        if (Keyversion != ClientKey)
        {
            Logger.genellog("Keyler oluşmadı cihaza izin verilmedi");
            Login = false;
            Loginreason = "Clientte değişiklik tespit edildi";
        }
         if(Maintance.MaintanceMode)
        {
            Maintance.SendMaintancePacket(session);
            return;
        }

        //  YAZMA
        var response = new FirstConnectionResponsePacket
        {
            Success = true,
            Message = Loginreason
        };
        session.Send(response);
        Console.WriteLine($"{device} adlı kullanıcı sunucuya giriş yaptı incelenmeye başlanıyor...");


    }
}