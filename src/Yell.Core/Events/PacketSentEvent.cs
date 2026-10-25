using Prism.Events;

namespace Yell.Core.Events
{
    public sealed class PacketSentEvent : PubSubEvent<byte[]>
    {
    }
}
