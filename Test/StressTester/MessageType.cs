public enum MessageType : short
{
    #region Conntection
    FirstConnectionRequest,
    FirstConnectionResponse,
    Disconnect,
    Ping,
    Pong,
    Maintance,
    MessageCode,
    Alive,
    Presence,
    #endregion

    #region Authentication
    AuthLoginRequest,
    AuthLoginResponse,
    NewAccountCreateResponse,
    #endregion
    #region  Account
    AccountData,
    LoginFailed,
    LoginOKRequest,
    LoginOKResponse,
    SendVerifyCode,
    VerifyCodeResponse,
    AccountLogin,
    SignAccount,
    #endregion


    #region  Profile
    ChangeNameRequest,
    ChangeNameResponse,
    ChangeNameColorRequest,
    ChangeNameColorResponse,
    SetAvatarRequest,
    SetAvatarResponse,
    ShowProfileRequest,
    ShowProfileResponse,
    #endregion

    #region  Friends
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
    #endregion


    #region  Team
    CreateTeamRequest,
    CreateTeamResponse,
    JoinTeamRequest,
    JoinTeamResponse,
    LeaveTeamRequest,
    LeaveTeamResponse,
    SendTeamMessageRequest,
    SendTeamMessageResponse,
    InviteToTeamRequest,
    InviteToTeamResponse,
    #endregion





    #region  notfications
    Notification,
    AllNotficationViewed,
    #endregion

    #region  Leaderboards
    LeaderboardRequest,
    LeaderboardResponse,
    #endregion



    #region  Club

    ClubCreateRequest,
    ClubCreateResponse,
    GetRandomClubRequest,
    GetRandomClubResponse,
    ClubShowRequest,
    ClubShowResponse,
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
    KickMemberinClubRequest,
    KickMemberinClubResponse,
    MemberToUpperRequest,
    MemberToUpperResponse,
    MemberToLowerRequest,
    MemberToLowerResponse,

    #endregion



    #region  Battle
    MatchMakingRequest,
    MatchMakingCancelRequest,
    MatchFound,
    MatchMakingAddPlayer,
    StartMatch,
    Move,

    PlayerMoved,
    PlayerDead,
    ShootRequest,
    Shoot,
    HitRequest,
    PlayerHealthUpdate,

    #endregion

    #region  Shop
    GetAllMarketItemsRequest,
    GetAllMarketItemsResponse,

    #endregion



    #region  Support
    SupportMessageSend,
    SupportMessageResponse,
    SupporCreateTicketRequest,
    SupporCreateTicketResponse,
    ReportPlayerRequest,
    SupportGetAllTicketRequest,
    SupportGetAllTicketResponse,
    SupportTicketClosed,
    SupportTicketOpened,

    #endregion

    #region Pass
    NewQuest,
    DeleteQuest,
    QuestProgress

    #endregion


}
