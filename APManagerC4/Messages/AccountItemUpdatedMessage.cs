using APManagerC4.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Uid = HM.Common.Uid;

namespace APManagerC4.Messages
{
    /// <summary>
    /// AccountItem更新消息
    /// </summary>
    class AccountItemUpdatedMessage : RequestMessage<bool>
    {
        public Uid Uid { get; init; }
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        public AccountItem Data { get; init; }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    }
}
