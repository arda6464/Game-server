public  class InvitePlayerToTeamResponsePacket: IPacket
{
    public bool Sended { get; set; }
    public TeamErrorCode ErrorCode { get; set; }
    
   public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.InvitePlayerTeamResponse);
        buffer.WriteBool(Sended);
        if(!Sended)
        buffer.WriteByte((byte)ErrorCode);
    }

    public void Deserialize(ByteBuffer buffer)
    {
       
    }
}