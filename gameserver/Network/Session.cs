using System;
using System.IO;
using System.Net.Sockets;


public class Session
{
    private TcpClient client;
    private NetworkStream stream;
    public string AccountId { get; set; }
    public DateTime LastPingSent { get; set; }
    public int LastPing { get; set;  }

    public Session(TcpClient c)
    {
        client = c;
        this.stream = client.GetStream();
    }
    public void Start()
{
    byte[] buffer = new byte[4096];

    try
    {
        while (true)
        {
            int bytesRead = 0;
            try
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("[Session] Stream kapalı, client çıkış yaptı.");
                break;
            }
            catch (IOException)
            {
                Console.WriteLine("[Session] Client bağlantısı kesildi.");
                break;
            }

            if (bytesRead <= 0)
            {
                Console.WriteLine("[Session] Client bağlantıyı kapattı.");
                break;
            }

        
            MessageManager.HandleMessage(this, buffer);
        }
    }
    finally
    {
        Close();
    }
}
    public void Send(byte[] buffer)
    {
        if (!client.Connected)
        {
            throw new InvalidOperationException("Bağlantı kapalı");
        }
        try
        {

            stream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gönderme hatası: {ex.Message}");
        }
    }
    public void Close()
    {
          try
        {
            stream.Close();
            client.Close();
        }
        catch {}

        if (!string.IsNullOrEmpty(AccountId))
        {
            SessionManager.RemoveSession(AccountId);
        }

        Console.WriteLine($"[Session] {AccountId ?? "Unknown"} bağlantısı kapatıldı.");
    }
}