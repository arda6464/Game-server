using System;


public static class FirstConnectionHandler
{
    
    public static void Handle(Session session, byte[] data)
    {
         string Keyversion = "ARDA64";
         bool Login = true;
         string Loginreason = string.Empty;
       


        // OKUMA
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(data, true);
        int mesajtipi = byteBuffer.ReadInt(); //  gereksiz atlamak için
        string cihazadı = byteBuffer.ReadString();
        string device = byteBuffer.ReadString();
        Console.WriteLine("cihaz adı: " + device);
        string ClientKey = byteBuffer.ReadString();
        
 

        byteBuffer.Dispose(); // yoket   ramde kalmasın diye siliyoruz çünkü alacağımızı aldık
                              //todo device control add
        session.DeviceID = device;

        if (Keyversion != ClientKey)
        {
            Logger.genellog("Keyler oluşmadı cihaza izin verilmedi");
            Login = false;
            Loginreason = "Clientte değişiklik tespit edildi";
        }

       


        //  YAZMA
            ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.FirstConnectionResponse);
        buffer.WriteBool(Login);
        buffer.WriteString(Loginreason);


        byte[] gonderilcekveri = buffer.ToArray();
        buffer.Dispose();
        session.Send(gonderilcekveri);
        Console.WriteLine($"{device} adlı kullanıcı sunucuya giriş yaptı incelenmeye başlanıyor...");


    }
}