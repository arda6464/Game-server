public class Lobby
{

    public int ID { get; set; }
    public string OwnerID { get; set; }
    public int MaxPlayers { get; set; } = 3;
    public bool IsInGame { get; set; }


    public List<AccountManager.AccountData> Players { get; set; } = new();
    public List<ClubMessage> Messages { get; set; } = new(); // Kulüp mesajları


    public Lobby(int id, string ownerid)
    {
        ID = id;
        OwnerID = ownerid;
    }
    public void AddPlayers(AccountManager.AccountData player)
    {
        if (Players.Count >= MaxPlayers) return;
        Players.Add(player);
        Console.WriteLine($"{player.Username}({player.AccountId}) odaya katıldı total count: {Players.Count}" );
    }
    public void RemovePlayer(string accId)
    {
        Players.RemoveAll(x => x.AccountId == accId);
    }
    

}



public static class LobbyManager
{

    public static Dictionary<int, Lobby> Lobbies = new();


   public static Lobby CreateLobby(AccountManager.AccountData owner)
    {
    Random lobbyıd = new Random();
SelectLobbyID:
    int id = lobbyıd.Next(100000, 999999);
    if(Lobbies.ContainsKey(id)) goto SelectLobbyID;

        Lobby lobby = new Lobby(id, owner.AccountId);
    if(lobby == null)
            {
            Logger.errorslog("[LobbyManager] Lobby oluşturulamadı!");
            return null;
        }
    lobby.AddPlayers(owner);
        Lobbies[lobby.ID] = lobby;
    Logger.genellog($"[Lobby Manager] Lobby oluşturuldu: {lobby.ID}");



    return lobby;
}
    public static Lobby GetLobby(int id)
    {
        return Lobbies.ContainsKey(id) ? Lobbies[id] : null;
    }
    public static void DeleteLobby(int id)
    {
        if (Lobbies.ContainsKey(id)) Lobbies.Remove(id);
        else Logger.errorslog($"[Lobby Manager] {id}'li lobby silinmek istedi fakat öyle bi lobby yok!");
    }

   public static bool LeaveTeam(int teamid, string accid)
{
  
    
   Lobby lobby = GetLobby(teamid);
        if (lobby == null) return false;
    lobby.RemovePlayer(accid);
       Console.WriteLine($"{accid} odadan ayrıldı total count: {lobby.Players.Count}" );
    
        if (lobby.Players.Count == 0)
        {
            Console.WriteLine($"[Lobby Manager] Lobby boş, siliniyor: {lobby.ID}");
            DeleteLobby(lobby.ID);
        } 
    return true;
}
}