using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using HM.Wpf;

namespace APManagerC4.ViewModels
{
    public class AccountItemLabelGroup : ObservableRecipient
    {
        public string Title
        {
            get => _title.Replace("_", "__");
            set => SetProperty(ref _title, value);
        }
        public ObservableList<AccountItemLabel> Items { get; init; } = new();
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public AccountItemLabelGroup(IMessenger messenger) : base(messenger)
        {

        }

        private bool _isExpanded;
        private string _title = string.Empty;
    }
}
