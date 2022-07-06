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
        public static readonly string DefualtGroupName = "未分组";
        public static readonly string DefaultItemTitle = "新建条目";

        class GroupComparer : IComparer<AccountItemLabelGroup>
        {
            public static GroupComparer Default { get; } = new();

            public int Compare(AccountItemLabelGroup? x, AccountItemLabelGroup? y)
            {
                if (x?.Title == DefualtGroupName && y?.Title != DefualtGroupName)
                {
                    return 1;
                }
                else if (y?.Title == DefualtGroupName && x?.Title != DefualtGroupName)
                {
                    return -1;
                }
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
                ReGroup();
                OnGroupsUpdated();
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
        public AccountItemLabelGroup[] Groups => _groups.ToArray();

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
            _groups.Clear();
            _groups.Add(new AccountItemLabelGroup(Messenger)
            {
                Title = string.Empty,
                Items = result.Select(t => new AccountItemLabel(Messenger)
                {
                    Guid = t.Guid,
                    Title = t.Title
                }).ToArray()
            });

            ReGroup();
            SortGroups();
            OnGroupsUpdated();
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
                OnGroupsUpdated();
            }
            UpdateGroupItems(group);
            group.IsExpanded = true;

            var newItem = group.Items.First(t => t.Guid == item.Guid);
            newItem.IsSelected = true;
            newItem.RequestToView();
        }
        public void DeleteItem(Guid guid)
        {
            foreach (var group in _groups)
            {
                if (group.Items.FirstOrDefault(t => t?.Guid == guid, null) is AccountItemLabel t)
                {
                    DataCenter.Delete(t.Guid);
                    UpdateGroupItems(group);
                    break;
                }
            }
            RemoveEmptyGroups();
            SortGroups();
            OnGroupsUpdated();
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
                /* 获取更新的AccountItem的Guid所在的AccountLabel与所属组，
                 * 若该AccountItem的所属组与AccountLabel的所属组不同，
                 * 则让当前组与AccountItem所属组的对应Group重新获取数据 */
                var preGroup = _groups.First(g => g.Items.Any(t => t.Guid == e.Guid));
                var targetGroupTitle = GetTargetGroupTitle(e.Data);

                if (preGroup.Title != targetGroupTitle)
                {
                    // 若所属组不存在则创建一个
                    if (!_groups.Any(t => t.Title == targetGroupTitle))
                    {
                        _groups.Add(new AccountItemLabelGroup(Messenger)
                        {
                            Title = targetGroupTitle
                        });
                    }

                    // 当前组重新获取数据
                    UpdateGroupItems(preGroup);
                    preGroup.IsExpanded = true;

                    // 新组重新获取数据
                    var tGroup = _groups.First(g => g.Title == targetGroupTitle);
                    UpdateGroupItems(tGroup);
                    tGroup.IsExpanded = true;
                    tGroup.Items.First(t => t.Guid == e.Guid).IsSelected = true; // 让其保持选中状态

                    RemoveEmptyGroups();
                    SortGroups();
                    OnGroupsUpdated();
                }
            });
        }

        private readonly List<AccountItemLabelGroup> _groups = new();
        private Predicate<Models.AccountItem>? _filter;
        private string _groupKey = nameof(Models.AccountItem.Category);
        private void ReGroup()
        {
            var labels = _groups.SelectMany(g => g.Items).ToArray();
            _groups.Clear();
            _groups.AddRange(
                from aItem in labels
                let item = DataCenter.Retrieve(aItem.Guid)
                group aItem by GetTargetGroupTitle(item) into groups
                select new AccountItemLabelGroup(Messenger)
                {
                    Title = groups.Key,
                    Items = groups.ToArray()
                });
        }
        private void SortGroups()
        {
            _groups.Sort(GroupComparer.Default);
            foreach (var item in _groups)
            {
                item.SortItems();
            }
        }
        private void RemoveEmptyGroups()
        {
            for (int i = 0; i < _groups.Count; i++)
            {
                if (!_groups[i].Items.Any())
                {
                    _groups.RemoveAt(i);
                    i--;
                }
            }
        }
        private void OnGroupsUpdated()
        {
            OnPropertyChanged(nameof(Groups));
        }
        private void UpdateGroupItems(AccountItemLabelGroup labelGroup)
        {
            /* 若过滤函数为空且按分类分组，则只需要获取数据摘要信息即可，
             * 否则进行完全查找 */
            IEnumerable<AccountItemLabel> result;

            if (_filter is null && _groupKey == nameof(Models.AccountItem.Category))
            {
                result = from item in AbstractDataProvider.Retrieve(t => true)
                         where item.Category == labelGroup.Title
                         select new AccountItemLabel(Messenger)
                         {
                             Guid = item.Guid,
                             Title = item.Title
                         };
            }
            else
            {
                result = from item in DataCenter.Retrieve(_filter ?? (t => true))
                         where GetTargetGroupTitle(item) == labelGroup.Title
                         select new AccountItemLabel(Messenger)
                         {
                             Guid = item.Guid,
                             Title = item.Title
                         };
            }
            labelGroup.Items = result.ToArray();
        }
        private string GetTargetGroupTitle(Models.AccountItem item)
        {
            var groupProp = typeof(Models.AccountItem).GetProperty(_groupKey);
            System.Diagnostics.Debug.Assert(groupProp is not null); // assert
            return groupProp.GetValue(item) as string ?? string.Empty;
        }
    }
}
