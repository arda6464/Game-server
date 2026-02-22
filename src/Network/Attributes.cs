using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PacketHandlerAttribute : Attribute
{
    public MessageType Type { get; }

    public PacketHandlerAttribute(MessageType type)
    {
        Type = type;
    }
}
