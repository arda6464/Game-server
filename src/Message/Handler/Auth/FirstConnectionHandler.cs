using System;


[PacketHandler(MessageType.FirstConnectionRequest)]
public class FirstConnectionHandler : IGameMessage
{
    
    public void Handle(Session session, byte[]? data)
    {
         string Keyversion = "ARDA64";
         bool Login = true;
         string Loginreason = string.Empty;
       


        // OKUMA
        var request = data.DeserializePacket<FirstConnectionRequestPacket>();
        
        string cihazadı = request.DeviceName;
        string device = request.DeviceModel;
        Console.WriteLine("cihaz adı: " + cihazadı);
        Console.WriteLine("cihaz model: " + device);
        string ClientKey = request.ClientKey;
        session.DeviceID = device;

        if (Keyversion != ClientKey)
        {
            Logger.genellog("Keyler uyuşmadı cihaza giriş izni verilmedi");
            Login = false;
            Loginreason = "Clientte değişiklik tespit edildi";
        }
         if(Maintenance.MaintenanceMode)
        {
            Maintenance.SendMaintenancePacket(session);
            return;
        }

        //  YAZMA
        var response = new FirstConnectionResponsePacket
        {
            Success = Login,
            Message = Loginreason
        };
        session.Send(response);
        


    }
}
