using APManagerC4.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Input;
using AbstractDataProvider = APManagerC4.IDataProvider<APManagerC4.Models.LabelInfo>;
using DataCenter = APManagerC4.IDataCenter<APManagerC4.Models.AccountItem>;

namespace APManagerC4.ViewModels
{
    public class Manager : ObservableRecipient
    {
        public static readonly string DefaultItemCategory = "";
        public static readonly string DefaultItemTitle = "新建";

        class GroupComparer : IComparer<AccountItemLabelGroup>
        {
            public static GroupComparer Default { get; } = new();

            public int Compare(string? x, string? y)
            {
                ArgumentNullException.ThrowIfNull(x);
                ArgumentNullException.ThrowIfNull(y);

                if (x == DefaultItemCategory && y != DefaultItemCategory)
                {
                    return 1;
                }
                else if (y == DefaultItemCategory && x != DefaultItemCategory)
                {
                    return -1;
                }

                int r = x.CompareTo(y);

                return r == 0 ? -1 : r;
            }

            public int Compare(AccountItemLabelGroup? x, AccountItemLabelGroup? y)
            {
                return Compare(x?.Title, y?.Title);
            }
        }

        class LabelItemComparer : IComparer<AccountItemLabel>
        {
            public static LabelItemComparer Default { get; } = new();

            public int Compare(AccountItemLabel? x, AccountItemLabel? y)
            {
                return x?.Title.CompareTo(y?.Title) ?? -1;
            }
        }

        public static RoutedCommand NewItemCommand { get; } = new();
        public static RoutedCommand SaveChangesCommand { get; } = new();

        public string GroupKey
        {
            get => _groupKey;
            set
            {
                SetProperty(ref _groupKey, value);
                ReGroup(_groups.SelectMany(t => t.Items).ToArray());
            }
        }
        public string[] GroupKeys { get; } = { "Category", "Website", "UserName", "Email", "Phone", "LoginPassword" };
        public DataCenter DataCenter { get; }
        public AbstractDataProvider AbstractDataProvider { get; }
        public Predicate<Models.AccountItem>? Filter
        {
            get => _filter;
            set => SetProperty(ref _filter, value);
        }
        public ObservableList<AccountItemLabelGroup> Groups => _groups;

        public void FetchData()
        {
            IEnumerable<Models.LabelInfo> result;
            if (_filter is null)
            {
                result = AbstractDataProvider.Retrieve(t => true);
            }
            else
            {
                result = from item in DataCenter.Retrieve(_filter)
                         select new Models.LabelInfo()
                         {
                             Guid = item.Guid,
                             Title = item.Title,
                             Category = item.Category
                         };
            }

            ReGroup(result.Select(t => new AccountItemLabel(Messenger)
            {
                Guid = t.Guid,
                Title = t.Title
            }));
        }
        public void AddItem(Models.AccountItem item)
        {
            /* 获取目标分组的引用（若没有找到则新建），然后让其重新获取数据 */
            DataCenter.Add(item.Guid, item);

            var groupTitle = GetTargetGroupTitle(item);
            var group = _groups.FirstOrDefault(g => g?.Title == groupTitle, null);
            if (group is null)
            {
                group = new AccountItemLabelGroup(Messenger)
                {
                    Title = groupTitle
                };
                _groups.Add(group);
            }
            group.Items.Add(new AccountItemLabel(Messenger)
            {
                Guid = item.Guid,
                Title = item.Title
            });
            group.IsExpanded = true;

            var newItem = group.Items.First(t => t.Guid == item.Guid);
            newItem.IsSelected = true;
            newItem.RequestToView();
        }
        public void DeleteItem(Guid guid)
        {
            DataCenter.Delete(guid);

            var group = _groups.First(g => g.Items.Any(t => t?.Guid == guid));
            group.Items.Remove(group.Items.First(t => t.Guid == guid));

            if (!group.Items.Any())
            {
                _groups.Remove(group);
            }
        }
        public string?[] RetrieveOptions(Func<Models.AccountItem, string?> selector, Predicate<string>? predicate)
        {
            ArgumentNullException.ThrowIfNull(selector);

            var result = from item in DataCenter.Retrieve(t => true)
                         let value = selector(item)
                         where predicate?.Invoke(value) ?? true
                         orderby value
                         select value;

            return result.Distinct().ToArray();
        }

        public Manager(IMessenger messenger, DataCenter dataCenter, AbstractDataProvider abstractDataProvider) : base(messenger)
        {
            DataCenter = dataCenter;
            AbstractDataProvider = abstractDataProvider;

            Messenger.Register<AccountItemUpdatedMessage>(this, (sender, e) =>
            {
                /* 若数据所属分组应发生修改，则重新分组 */
                var targetGroupTitle = GetTargetGroupTitle(e.Data);
                var targetGroup = _groups.FirstOrDefault(g => g.Title == targetGroupTitle);
                if (targetGroup is null)
                {
                    targetGroup = new AccountItemLabelGroup(Messenger)
                    {
                        Title = targetGroupTitle
                    };
                    _groups.Add(targetGroup);
                }

                var preGroup = _groups.First(g => g.Items.Any(t => t.Guid == e.Guid));
                if (!ReferenceEquals(preGroup, targetGroup))
                {
                    var targetLabel = preGroup.Items.First(t => t.Guid == e.Guid);
                    preGroup.Items.Remove(targetLabel);
                    if (!preGroup.Items.Any())
                    {
                        _groups.Remove(preGroup);
                    }
                    targetGroup.Items.Add(targetLabel);
                    targetGroup.Items.Sort(LabelItemComparer.Default);
       
                }

                targetGroup.IsExpanded = true;
            });
        }

        private readonly ObservableList<AccountItemLabelGroup> _groups = new();
        private Predicate<Models.AccountItem>? _filter;
        private string _groupKey = nameof(Models.AccountItem.Category);
        private void ReGroup(IEnumerable<AccountItemLabel> labels)
        {
            _groups.Clear();
            _groups.AddRange(
                from aItem in labels
                let item = DataCenter.Retrieve(aItem.Guid)
                group aItem by GetTargetGroupTitle(item) into groups
                select new AccountItemLabelGroup(Messenger)
                {
                    Title = groups.Key,
                    Items = new ObservableList<AccountItemLabel>(groups)
                });

            _groups.Sort(GroupComparer.Default);
            _groups.ForEach(t => t.Items.Sort(LabelItemComparer.Default));
        }
        private string GetTargetGroupTitle(Models.AccountItem item)
        {
            var groupProp = typeof(Models.AccountItem).GetProperty(_groupKey);
            System.Diagnostics.Debug.Assert(groupProp is not null); // assert
            return groupProp.GetValue(item) as string ?? string.Empty;
        }
    }
}
