using System;



public class Logger
{
    static string erorlogerpath = "erors.txt";
    static string accountlogpath = "accountslog.txt";
    static string battleslogpath = "battleslog";
    static string genellogpath = "genellog.txt";
    static string? logmessage;
   
    public void accountlog(string mesaj)
    {
         DateTime saat = DateTime.Now;
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        logmessage = $"[{saat}] {mesaj}";
        Console.WriteLine(logmessage);
        File.AppendAllText(accountlogpath, logmessage + Environment.NewLine);
        Console.ResetColor();
    }
    public static void errorslog(string mesaj)
    {
       DateTime saat = DateTime.Now;
        logmessage = $"[{saat}] {mesaj}";
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(logmessage);
        File.AppendAllText(erorlogerpath, logmessage + Environment.NewLine);
        Console.ResetColor();
    }
    public static void battlelog(string mesaj)
    {
       DateTime saat = DateTime.Now;
        logmessage = $"[{saat}] {mesaj}";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(logmessage);
        File.AppendAllText(battleslogpath, logmessage + Environment.NewLine);
        Console.ResetColor();
    }
     public static void genellog(string mesaj)
    {
        DateTime saat = DateTime.Now;
        logmessage = $"[{saat}] {mesaj}";
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(logmessage);
        File.AppendAllText(genellogpath, logmessage + Environment.NewLine); 
         Console.ResetColor();
    }

}