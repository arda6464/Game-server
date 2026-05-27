using System.Collections.Generic;

[PacketHandler(MessageType.GetUpdateNotesRequest)]
public class GetUpdateNotesRequestPacket : IPacket
{
    public void Serialize(ByteBuffer buffer) => throw new NotImplementedException();
    public void Deserialize(ByteBuffer buffer) { }
}

public class GetUpdateNotesResponsePacket : IPacket
{
    public List<GameUpdateNoteData> Updates { get; set; } = new List<GameUpdateNoteData>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.GetUpdateNotesResponse);
        buffer.WriteVarInt(Updates.Count);

        foreach (var update in Updates)
        {
            buffer.WriteVarString(update.Title ?? "");
            buffer.WriteVarString(update.Version ?? "");
            buffer.WriteVarString(update.Date ?? "");
            buffer.WriteVarInt(update.BannerId);
            buffer.WriteVarInt(update.Messages?.Count ?? 0);

            if (update.Messages == null) continue;
            foreach (var msg in update.Messages)
            {
                buffer.WriteVarString(msg.Title ?? "");
                buffer.WriteVarString(msg.Message ?? "");
                buffer.WriteVarInt(msg.IconId);
                buffer.WriteVarInt(msg.TypeId);
            }
        }
    }

    public void Deserialize(ByteBuffer buffer) => throw new NotImplementedException();
}
