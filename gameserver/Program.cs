using System;
using System.Text;
using System.Threading;

class Program
{

    static void Main()
    {
        Console.Clear();
        AccountCache.Init();
        ClubManager.Allclubload();
        Thread cmdhandlerthread = new Thread(Cmdhandler.Start);
        cmdhandlerthread.Start();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var shutdownEvent = new ManualResetEvent(false);
        GameServer gameserver= new GameServer();
          
       
       
       /* Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("Shutdown signal received (Ctrl+C)...");

            e.Cancel = true;
            shutdownEvent.Set();
            
        };*/
        try
        {
            gameserver.Start();
          //  shutdownEvent.WaitOne();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unhandled exception occurred in the main execution thread: {ex.ToString()}");
        }
        finally
        {
            gameserver.Stop();
        }
    }
}