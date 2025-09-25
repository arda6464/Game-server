using System;
using System.IO;
using System.Net.Sockets;


public class Session
{
    private TcpClient client;
     private NetworkStream stream;
    public Session(TcpClient c)
    {
        client = c;
         this.stream = client.GetStream();
    }
    public void Start()
    {
        byte[] buffer = new byte[4096];
        while (true)
        {
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead <= 0)
                break;
            MessageManager.HandleMessage(this, buffer);
        }
    }
    public void Send(byte[] buffer)
    {
        try
        {
            
            stream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gönderme hatası: {ex.Message}");
        }
    }
}