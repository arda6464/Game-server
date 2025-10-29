
public enum MessageType : int
{
    // Connection
    FirstConnectionRequest,
    FirstConnectionResponse,
    Disconnect,
    Ping,
    Pong,

    // Authentication
    AuthLoginRequest,
    AuthLoginResponse,
    NewAccountCreateResponse,

    // Account
    AccountData,
    LoginFailed,

    // Profile
    ChangeNameRequest,
    ChangeNameResponse,
    ChangeNameColorRequest,
    ChangeNameColorResponse,
    SetAvatarRequest,
    SetAvatarResponse,
    ShowProfileRequest,
    ShowProfileResponse,


    // Friends
    SendFriendRequest,
    SendFriendResponse,
    AcceptFriendRequest,
    AcceptFriendResponse,
    DeclineFriendRequest,
    DeclineFriendResponse,
    DeleteFriendRequest,
    DeleteFriendResponse,
    NewFriendsList,
    NewRequestList,
    // notfications
    Notification,
    // Club
    GetRandomClubRequest,
    GetRandomClubResponse,
    SendClubMessage,
    GetClubMessage,
    JoinClubRequest,
    JoinClubResponse,
    LeaveClubRequest,
    LeaveClubResponse,
    ClubEditRequest,
    ClubEditResponse,
    ClubSearchRequest,
    ClubSearchResponse,



    // Battle
    MatchMakingRequest,
    MatchMakingCancelRequest,
    MatchFound,
    MatchMakingAddPlayer,
    StartMatch,
    Move,
    Dead,
    PlayerMoved,
    PlayerDead,
    ShootRequest,
    Shoot,
    




}
