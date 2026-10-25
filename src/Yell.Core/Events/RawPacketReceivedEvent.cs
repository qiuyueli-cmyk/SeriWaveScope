using Prism.Events;

namespace Yell.Core.Events
{
    /// <summary>
    /// 采集模块收到一个完整协议帧后发布，监控模块订阅。
    /// 模块之间通过事件解耦，不互相引用。
    /// </summary>
    public sealed class RawPacketReceivedEvent : PubSubEvent<byte[]>
    {
    }
}
