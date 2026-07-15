
public struct WorldSnapshotEntry
{
    public int ID;
    public float X, Y, Z;
    public bool IsVisible;
}

public struct WorldSnapshotPacket : IPacket
{
    public uint ServerTick;
    public WorldSnapshotEntry[] Players;

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.WorldSnapshot);
        buffer.WriteUInt(ServerTick);
        buffer.WriteVarInt(Players.Length);
        foreach (var p in Players)
        {
            buffer.WriteVarInt(p.ID);
            buffer.WriteFloat(p.X);
            buffer.WriteFloat(p.Y);
            buffer.WriteFloat(p.Z);
            buffer.WriteBool(p.IsVisible);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        ServerTick = buffer.ReadUInt();
        int count = buffer.ReadVarInt();
        Players = new WorldSnapshotEntry[count];
        for (int i = 0; i < count; i++)
        {
            Players[i] = new WorldSnapshotEntry
            {
                ID = buffer.ReadVarInt(),
                X = buffer.ReadFloat(),
                Y = buffer.ReadFloat(),
                Z = buffer.ReadFloat(),
                IsVisible = buffer.ReadBool()
            };
        }
    }
}
