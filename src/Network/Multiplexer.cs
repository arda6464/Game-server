using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class Multiplexer
{
    private TcpListener _listener;
    private AdminServer _adminServer;
    private GameServer _gameServer;
    private bool _isShuttingDown;

    public int Port { get; }

    public Multiplexer(int port, AdminServer adminServer, GameServer gameServer)
    {
        Port = port;
        _listener = new TcpListener(IPAddress.Any, port);
        _adminServer = adminServer;
        _gameServer = gameServer;
    }

    public void Start()
    {
        _listener.Start();
        Logger.genellog($"[MULTIPLEXER] Listening on port {Port}");
        _ = RunAsync();
    }

    private async Task RunAsync()
    {
        while (!_isShuttingDown)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
            catch (Exception ex)
            {
                if (_isShuttingDown)
                    break;
                Logger.errorslog($"[Multiplexer] Accept error: {ex.Message}");
                await Task.Delay(100);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int read = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (read <= 0)
            {
                client.Close();
                return;
            }

            string initialData = Encoding.ASCII.GetString(buffer, 0, read);
            bool isHttp =
                initialData.StartsWith("GET ")
                || initialData.StartsWith("POST ")
                || initialData.StartsWith("OPTIONS ")
                || initialData.StartsWith("HEAD ")
                || initialData.StartsWith("PUT ")
                || initialData.StartsWith("DELETE ");

            byte[] data = new byte[read];
            Array.Copy(buffer, 0, data, 0, read);

            if (isHttp)
                _adminServer?.HandleConnection(client, data);
            else
                _gameServer?.HandleConnection(client, data);
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Multiplexer] Connection error: {ex.Message}");
            client.Close();
        }
    }

    public void Stop()
    {
        _isShuttingDown = true;
        _listener?.Stop();
    }
}
