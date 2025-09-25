using System;


public static class FirstConnectionHandler
{
    public static void Handle(Session session, byte[] data)
    {
        // OKUMA
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(data, true);
        int mesajtipi = byteBuffer.ReadInt(); //  gereksiz atlamak için

        string cihazadı = byteBuffer.ReadString();
         
        byteBuffer.Dispose(); // yoket   ramde kalmasın diye siliyoruz çünkü alacağımızı aldık
                              //todo device control add


      //  YAZMA
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.FirstConnection);
        buffer.WriteBool(true);

        byte[] gonderilcekveri = buffer.ToArray();
        buffer.Dispose();
        session.Send(gonderilcekveri);
        Console.WriteLine($"{cihazadı} adlı kullanıcı sunucuya giriş yaptı incelenmeye başlanıyor...");


    }
}