using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using APManagerC4.Messages;
using System.Windows.Input;

namespace APManagerC4.ViewModels
{
    public class AccountItem : ObservableRecipient
    {
        public static RoutedCommand RequestToViewCommand { get; } = new();

        public Guid Guid
        {
            get => _guid;
            init => _guid = value;
        }
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public void RequestToView(bool readonlyMode)
        {
            var message = new RequestToViewDetailMessage()
            {
                Guid = Guid,
                ReadOnlyMode = readonlyMode
            };
            Messenger.Send(message);
        }

        public AccountItem(IMessenger messenger) : base(messenger)
        {
            Messenger.Register<AccountItemUpdatedMessage>(this, (sender, e) =>
            {
                if (Guid == e.Guid)
                {
                    Title = e.Data.Title;
                }
            });
        }

        private readonly Guid _guid;
        private string _title = string.Empty;
        private bool _isSelected; 
    }
}
