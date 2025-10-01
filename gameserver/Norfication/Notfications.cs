public class Notification
{
    public int Id { get; set; }        // 10 = düz metin, 11 = banner, 12 = inbox
    public string Title { get; set; }
    public string Message { get; set; }
    public string ButtonText { get; set; }
    public bool IsViewed { get; set; }
    public string Url { get; set; }

    public Notification(int id, string title, string message, string url,string button = "Tamam")
    {
        Id = id;
        Title = title;
        Message = message;
        ButtonText = button;
        IsViewed = false;
         Url = url;
    }
}
