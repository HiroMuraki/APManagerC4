using CommunityToolkit.Mvvm.Messaging.Messages;
using Uid = HM.Common.Uid;

namespace APManagerC4.Messages
{
    /// <summary>
    /// 请求查看AccountItem详情的消息
    /// </summary>
    internal class RequestToViewDetailMessage : RequestMessage<bool>
    {
        public Uid Uid { get; init; }
        public bool ReadOnlyMode { get; init; }
    }
}
