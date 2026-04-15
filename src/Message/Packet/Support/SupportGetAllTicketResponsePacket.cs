using System.Collections.Generic;

public class SupportGetAllTicketResponsePacket : IPacket
{
    public class TicketInfo
    {
        public byte No { get; set; }
        public string Title { get; set; }
        public bool IsClosed { get; set; }
        public string ClosedReason { get; set; }
        public int ClosedAt { get; set; }
        public List<MessageInfo> Messages { get; set; } = new List<MessageInfo>();
    }

    public class MessageInfo
    {
        public string Name { get; set; }
        public string Content { get; set; }
    }

    public bool TicketBan { get; set; }
    public List<TicketInfo> Tickets { get; set; } = new List<TicketInfo>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.SupportGetAllTicketResponse);
        buffer.WriteBool(TicketBan);
        buffer.WriteByte((byte)Tickets.Count);
        foreach (var ticket in Tickets)
        {
            buffer.WriteByte(ticket.No);
            buffer.WriteVarString(ticket.Title ?? " ");
            buffer.WriteBool(ticket.IsClosed);
            if(ticket.IsClosed)
            {
                buffer.WriteVarString(ticket.ClosedReason ?? "");
                buffer.WriteVarInt(ticket.ClosedAt);
            }
            buffer.WriteByte((byte)ticket.Messages.Count);
            foreach (var msg in ticket.Messages)
            {
                buffer.WriteVarString(msg.Name);
                buffer.WriteVarString(msg.Content);
            }
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
