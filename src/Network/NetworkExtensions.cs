using System;
using System.Net.Sockets;

namespace Network
{
    public static class NetworkExtensions
    {
        public static void Write(this NetworkStream stream, ArraySegment<byte> segment)
        {
            stream.Write(segment.Array, segment.Offset, segment.Count);
        }

        public static int Send(this Socket socket, ArraySegment<byte> segment, SocketFlags flags = SocketFlags.None)
        {
            return socket.Send(segment.Array, segment.Offset, segment.Count, flags);
        }
    }
}
