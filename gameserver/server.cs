using System.Net;
using System.Net.Sockets;

public class GameServer()
{
    public void Start()
    {
      // Logger.genellog("Serer is started...");
      //  TcpClient client = new TcpClient();
        TcpListener? _listener;
        _listener = new TcpListener(IPAddress.Any, 5000);
        _listener.Start();
        while (true)
        {
            TcpClient client = _listener.AcceptTcpClient();
            Console.WriteLine("Yeni bir client bağlandı!");
             
             Session session = new Session(client);
             Thread clientThread = new Thread(session.Start);
            clientThread.Start();
        }

    }
    public void Stop()
    {
        
    }
}