using System;

namespace Neural
{
    public class PacketException : Exception
    {
        public PacketException(string message) : base(message)
        {
        }
    }
}
