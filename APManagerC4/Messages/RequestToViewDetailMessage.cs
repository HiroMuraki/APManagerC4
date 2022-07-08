using CommunityToolkit.Mvvm.Messaging.Messages;
using Guid = HM.Common.Uid;

namespace APManagerC4.Messages
{
    /// <summary>
    /// 请求查看AccountItem详情的消息
    /// </summary>
    class RequestToViewDetailMessage : RequestMessage<bool>
    {
        public Guid Guid { get; init; }
        public bool ReadOnlyMode { get; init; }
    }
}
