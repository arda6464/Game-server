public class Notification
{
    public int Id { get; set; }        // 11 = toast, 10 = banner, 12 = inbox
    public string Title { get; set; }
    public string Message { get; set; }
    public string ButtonText;
    public bool IsViewed;
    public string Url;
    public int iconid = 2;


   public Notification(int id, string title, string message/*, string url, string button = "Tamam"*/)
    {
        Id = id;
        Title = title;
        Message = message;
       /* ButtonText = button;
        IsViewed = false;
        Url = url;*/

    }
     public Notification()
    {
    }
}
public class İnboxNotfication
{
    public int ID;
    public string Sender;
    public string Message;
    public bool İsViewed = false;
    public DateTime Timespam; 
}

