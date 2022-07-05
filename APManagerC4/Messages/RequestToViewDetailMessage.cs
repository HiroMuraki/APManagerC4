using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace APManagerC4.Messages
{
    public class RequestToViewDetailMessage : RequestMessage<bool>
    {
        public Guid Guid { get; init; }
        public bool ReadOnlyMode { get; init; }
    }
}
