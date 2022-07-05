using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using APManagerC4.Messages;

namespace APManagerC4.ViewModels
{
    public class AccountItemGroup : ObservableRecipient
    {
        class ItemComparer : IComparer<AccountItem>
        {
            public static ItemComparer Default { get; } = new();

            public int Compare(AccountItem? x, AccountItem? y)
            {
                return x?.Title.CompareTo(y?.Title) ?? -1;
            }
        }

        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }
        public AccountItem[] Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public void Fetch(IDataCenter<Models.AccountItem> dataCenter)
        {
            Items = (from i in dataCenter.Retrieve(item => item.GroupName == GroupName)
                     select new AccountItem(Messenger)
                     {
                         Guid = i.Guid,
                         Title = i.Title
                     }).ToArray();
        }
        public void SortItems()
        {
            Array.Sort(Items, ItemComparer.Default);
        }

        public AccountItemGroup(IMessenger messenger) : base(messenger)
        {

        }

        private bool _isExpanded;
        private string _groupName = string.Empty;
        private AccountItem[] _items = Array.Empty<AccountItem>();
    }
}
