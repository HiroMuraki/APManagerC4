using APManagerC4.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DataCenter = APManagerC4.IDataCenter<APManagerC4.Models.AccountItem>;
using AbstractDataProvider = APManagerC4.IDataProvider<APManagerC4.Models.LabelInfo>;

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
                if (x?.GroupName == DefualtGroupName && y?.GroupName != DefualtGroupName)
                {
                    return 1;
                }
                else if (y?.GroupName == DefualtGroupName && x?.GroupName != DefualtGroupName)
                {
                    return -1;
                }
                return x?.GroupName.CompareTo(y?.GroupName) ?? -1;
            }
        }

        public static RoutedCommand NewItemCommand { get; } = new();
        public static RoutedCommand SaveChangesCommand { get; } = new();

        public bool HasFilter
        {
            get => _hasFilter;
            private set => SetProperty(ref _hasFilter, value);
        }
        public DataCenter DataCenter { get; }
        public AbstractDataProvider AbstractDataProvider { get; }
        public AccountItemLabelGroup[] Groups => _groups.ToArray();

        public void FetchDataIf(Predicate<Models.AccountItem> predicate)
        {
            FetchDataHelper(predicate);
            OnGroupsModificated();
            HasFilter = true;
        }
        public void FetchData()
        {
            FetchDataHelper(t => true);
            OnGroupsModificated();
            HasFilter = false;
        }
        public void NewItem()
        {
            var group = _groups.FirstOrDefault(g => g?.GroupName == DefualtGroupName, null);
            if (group is null)
            {
                group = new AccountItemLabelGroup(Messenger)
                {
                    GroupName = DefualtGroupName
                };
                _groups.Add(group);
                OnGroupsModificated();
            }
            group.IsExpanded = true;

            long time = DateTime.Now.Ticks;
            var model = new Models.AccountItem()
            {
                Guid = Guid.NewGuid(),
                Title = DefaultItemTitle,
                GroupName = DefualtGroupName,
                CreationTime = time,
                UpdateTime = time
            };
            DataCenter.Add(model.Guid, model);

            group.Fetch(DataCenter);
            var newItem = group.Items.First(t => t.Guid == model.Guid);
            newItem.IsSelected = true;
            newItem.RequestToView(HasFilter);
        }
        public void DeleteItem(Guid guid)
        {
            foreach (var group in _groups)
            {
                if (group.Items.FirstOrDefault(t => t?.Guid == guid, null) is AccountItemLabel t)
                {
                    DataCenter.Delete(t.Guid);
                    group.Fetch(DataCenter);
                    break;
                }
            }
            RemoveEmptyGroups();
            SortGroups();
            OnGroupsModificated();
        }

        public Manager(DataCenter dataCenter, IMessenger messenger) : base(messenger)
        {
            DataCenter = dataCenter;
            Messenger.Register<AccountItemUpdatedMessage>(this, (sender, e) =>
            {
                /* 获取更新的AccountItem的Guid所在的AccountLabel与所属组，
                 * 若该AccountItem的所属组与AccountLabel的所属组不同，
                 * 则让当前组与AccountItem所属组的对应Group重新获取数据 */

                var preGroup = _groups.First(g => g.Items.Any(t => t.Guid == e.Guid));
                var label = preGroup.Items.First(t => t.Guid == e.Guid);
                var tItem = DataCenter.Retrieve(label.Guid);

                if (preGroup.GroupName != tItem.GroupName)
                {
                    // 若所属组不存在则创建一个
                    if (!_groups.Any(t => t.GroupName == e.Data.GroupName))
                    {
                        _groups.Add(new AccountItemLabelGroup(Messenger)
                        {
                            GroupName = e.Data.GroupName
                        });
                    }

                    // 当前组重新获取数据
                    preGroup.Fetch(DataCenter);
                    preGroup.IsExpanded = true;

                    // 查找新组后，新组重新获取数据
                    var tGroup = _groups.First(g => g.GroupName == tItem.GroupName);
                    tGroup.Fetch(DataCenter);
                    tGroup.IsExpanded = true;
                    tGroup.Items.First(t => t.Guid == tItem.Guid).IsSelected = true; // 让其保持选中状态

                    RemoveEmptyGroups();
                    SortGroups();
                    OnGroupsModificated();
                }
            });
        }

        private bool _hasFilter;
        private readonly List<AccountItemLabelGroup> _groups = new();
        private void GenerateGroups(IEnumerable<Models.AccountItem> items)
        {
            var dict = new Dictionary<string, List<AccountItemLabel>>();
            foreach (var item in items)
            {
                var vmItem = new AccountItemLabel(Messenger)
                {
                    Guid = item.Guid,
                    Title = item.Title
                };

                if (dict.TryGetValue(item.GroupName, out var group))
                {
                    group.Add(vmItem);
                }
                else
                {
                    dict[item.GroupName] = new List<AccountItemLabel>();
                    dict[item.GroupName].Add(vmItem);
                }
            }

            _groups.Clear();
            foreach (var groupName in dict.Keys)
            {
                _groups.Add(new AccountItemLabelGroup(Messenger)
                {
                    GroupName = groupName,
                    Items = dict[groupName].ToArray()
                });
            }
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
        private void OnGroupsModificated()
        {
            OnPropertyChanged(nameof(Groups));
        }
        private void FetchDataHelper(Predicate<Models.AccountItem> predicate)
        {
            var data = DataCenter.Retrieve(predicate);
            GenerateGroups(data);
            SortGroups();
        }
    }
}
