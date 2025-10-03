public enum MessageType : int
{
    // Connection
    FirstConnectionRequest,
    FirstConnectionResponse,
     Disconnect,

    // Authentication
    AuthLoginRequest,
    AuthLoginResponse,
    NewAccountCreateResponse,

    // Account
    AccountData,
    LoginFailed,

    // Profile
    ChangeNameColorRequest,
    ChangeNameColorResponse,
    SetAvatarRequest,
    SetAvatarResponse,
  

    // Friends
    SendFriendRequest,
    SendFriendResponse,
    AcceptFriendRequest,
    AcceptFriendResponse,
    DeclineFriendRequest,
    DeclineFriendResponse,
    DeleteFriendRequest,
    DeleteFriendResponse,
    // notfications
    Notification,
}
