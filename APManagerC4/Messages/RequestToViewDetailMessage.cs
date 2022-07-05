using CommunityToolkit.Mvvm.Messaging.Messages;

namespace APManagerC4.Messages
{
    class RequestToViewDetailMessage : RequestMessage<bool>
    {
        public Guid Guid { get; init; }
        public bool ReadOnlyMode { get; init; }
    }
}
