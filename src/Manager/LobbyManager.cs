using System.Collections.Concurrent;
using Newtonsoft.Json;

public class TeamMessage
{
    public TeamMessageFlags messageFlags;
    public TeamEventType eventType;
    public int SenderId { get; set; }
    public string? SenderName { get; set; }
    public int SenderAvatarID { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Content { get; set; }
    public int MessageId { get; set; }
    

}
public enum TeamMessageFlags : byte
{
    None = 0,
    HasTarget = 1,
    HasSystem = 2       // 2 olarak kalabilir, client'te de 2 olmalı
}
public enum TeamEventType : byte
{
    JoinMessage,
    LeaveMessage,
    KickMessage, // todo....
    CreateMessage,
}
public class Lobby
{

    public int ID { get; set; }
    public int OwnerID { get; set; }
    public int MaxPlayers { get; set; } = 3;
    public bool IsInGame { get; set; }
    public int MessageIdCounter { get; set; } = 1;
    public string Link {get;set;}


    public List<AccountManager.AccountData> Players { get; set; } = new();
    public List<TeamMessage> Messages { get; set; } = new(); // Kulüp mesajları

    [JsonIgnore]
    public object SyncLock = new object();


    public Lobby(int id, int ownerid, string link)
    {
        ID = id;
        OwnerID = ownerid;
        Link = link;
    }
    public void AddPlayers(AccountManager.AccountData player)
    {
        lock (SyncLock)
        {
            if (Players.Count >= MaxPlayers) return;
            Players.Add(player);
            Console.WriteLine($"{player.Username}({player.ID}) odaya katıldı total count: {Players.Count}");
        }
    }
    public void RemovePlayer(int id)
    {
        lock (SyncLock)
        {
            Players.RemoveAll(x => x.ID == id);
        }
    }


}



public static class LobbyManager
{

    public static ConcurrentDictionary<int, Lobby> Lobbies = new();


    public static Lobby CreateLobby(AccountManager.AccountData owner)
    {
        Random lobbyıd = new Random();
    SelectLobbyID:
        int id = lobbyıd.Next(100000, 999999);
        if (Lobbies.ContainsKey(id)) goto SelectLobbyID;
      
      string link = InviteManager.CreateTeamInvite(id, owner.ID);
        Lobby lobby = new Lobby(id, owner.ID, link);
        if (lobby == null)
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
        if (Lobbies.TryRemove(id, out _))
        {
            Logger.genellog($"[Lobby Manager] {id}'li lobby başarıyla silindi.");
        }
        else 
        {
            Logger.errorslog($"[Lobby Manager] {id}'li lobby silinmek istedi fakat öyle bi lobby yok!");
        }
    }

    public static bool LeaveTeam(int teamid, int id)
    {


        Lobby lobby = GetLobby(teamid);
        if (lobby == null) return false;
        
        lock (lobby.SyncLock)
        {
            lobby.RemovePlayer(id);
            Console.WriteLine($"{id} odadan ayrıldı total count: {lobby.Players.Count}");

            if (lobby.Players.Count == 0)
            {
                Console.WriteLine($"[Lobby Manager] Lobby boş, siliniyor: {lobby.ID}");
                DeleteLobby(lobby.ID);
                return true;
            }
            if (lobby.OwnerID == id && lobby.Players.Count > 0) 
            {
                TransferLeader(lobby, lobby.Players[0]);
            }
        }
        return true;
    }

    public static void TransferLeader(Lobby team, AccountManager.AccountData newownerid)
    {
        var acc = AccountCache.Load(newownerid.ID);
        if (acc == null) return;

        lock (team.SyncLock)
        {
            team.OwnerID = newownerid.ID;
        }
        Console.WriteLine($"[Lobby Manager] Liderlik transfer edildi:  {newownerid.Username} - {newownerid.ID} ");
    }
    public static TeamMessage GetMessage(Lobby lobby,int messageid)
    {
        lock (lobby.SyncLock)
        {
            return lobby.Messages.Find(m => m.MessageId == messageid);
        }
    }
}