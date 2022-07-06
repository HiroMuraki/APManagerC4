using APManagerC4.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Input;

namespace APManagerC4.ViewModels
{
    public class AccountItemLabel : ObservableRecipient
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

        public void RequestToView()
        {
            var message = new RequestToViewDetailMessage()
            {
                Guid = Guid,
            };
            Messenger.Send(message);
        }

        public AccountItemLabel(IMessenger messenger) : base(messenger)
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
