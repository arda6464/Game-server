public class SocialSettingsHasChangedPacket : IPacket
{
    public bool LookingForTeam { get; set; }
    public bool MuteTeamInvites { get; set; }
    public int MuteTeamInviteEndTime { get; set; }
    public bool DoNotDisturb { get; set; }




    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public AccountManager.AccountData Account { get; set; }


    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.SocialSettingsHasChangedResponse);
        buffer.WriteBool(Success);
        if (!Success)
            buffer.WriteString(ErrorMessage ?? string.Empty);
        else
        {
            byte SocialsettingFlags = 0;
            if (Account.MuteTeamInvites) SocialsettingFlags |= 1 << 0;
            if (Account.LookingForTeam) SocialsettingFlags |= 1 << 1;
            if (Account.DoNotDisturb) SocialsettingFlags |= 1 << 2;

            int remaning = (int)(Account.MuteTeamInviteEndTime.ToUniversalTime() - DateTime.UtcNow).TotalSeconds;
            buffer.WriteByte(SocialsettingFlags);
            buffer.WriteVarInt(remaning);
        }
    }
    public void Deserialize(ByteBuffer buffer)
    {
        MuteTeamInvites = buffer.ReadBool();
        MuteTeamInviteEndTime = buffer.ReadVarInt();
        LookingForTeam = buffer.ReadBool();
        DoNotDisturb = buffer.ReadBool();
    }
}