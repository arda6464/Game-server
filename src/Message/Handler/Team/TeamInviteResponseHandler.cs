using System;

[PacketHandler(MessageType.InviteToTeamResponse)]
public static class TeamInviteResponseHandler
{
    public static void Handle(Session session, byte[] data)
    {
        TeamInviteHandler.ResponseHandle(session, data);
    }
}
