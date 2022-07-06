using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using AbstractDataProvider = APManagerC4.IDataProvider<APManagerC4.Models.LabelInfo>;

namespace APManagerC4.ViewModels
{
    public class AccountItemLabelGroup : ObservableRecipient
    {
        class ItemComparer : IComparer<AccountItemLabel>
        {
            public static ItemComparer Default { get; } = new();

            public int Compare(AccountItemLabel? x, AccountItemLabel? y)
            {
                return x?.Title.CompareTo(y?.Title) ?? -1;
            }
        }

        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }
        public AccountItemLabel[] Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public void SortItems()
        {
            Array.Sort(Items, ItemComparer.Default);
        }

        public AccountItemLabelGroup(IMessenger messenger) : base(messenger)
        {

        }

        private bool _isExpanded;
        private string _groupName = string.Empty;
        private AccountItemLabel[] _items = Array.Empty<AccountItemLabel>();
    }
}
