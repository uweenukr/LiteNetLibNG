using System;
using LiteNetLib;
using Mirror;

namespace LiteNetLibMirror
{
    public static class LiteNetLibTransportUtils
    {
        public const string ConnectKey = "MIRROR_LITENETLIB";

        /// <summary>
        /// convert Mirror channel to LiteNetLib channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static DeliveryMethod ConvertChannel(int channel)
        {
            switch (channel)
            {
                case Channel.Reliable:
                    return DeliveryMethod.ReliableOrdered;
                case Channel.Unreliable:
                    return DeliveryMethod.Unreliable;
                default:
                    throw new ArgumentException("Unexpected channel: " + channel);
            }
        }

        public static int ConvertChannel(DeliveryMethod channel)
        {
            switch (channel)
            {
                case DeliveryMethod.ReliableOrdered:
                    return Channel.Reliable;
                case DeliveryMethod.Unreliable:
                    return Channel.Unreliable;
                default:
                    throw new ArgumentException("Unexpected channel: " + channel);
            }

        }
    }
}
