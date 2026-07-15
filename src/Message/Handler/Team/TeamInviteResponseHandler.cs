using System;

[PacketHandler(MessageType.InviteToTeamResponse)]
public class TeamInviteResponseHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        TeamInviteHandler.ResponseHandle(session, data);
    }
}
