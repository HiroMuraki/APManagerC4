using APManagerC4.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using DataCenter = APManagerC4.IDataCenter<APManagerC4.Models.AccountItem>;
using System.Windows.Input;

namespace APManagerC4.ViewModels
{
    public class Manager : ObservableRecipient
    {
        public static readonly string DefualtGroupName = "未分组";
        public static readonly string DefaultItemTitle = "新建条目";

        class GroupComparer : IComparer<AccountItemGroup>
        {
            public static GroupComparer Default { get; } = new();

            public int Compare(AccountItemGroup? x, AccountItemGroup? y)
            {
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
        public ObservableCollection<AccountItemGroup> Groups
        {
            get => _groups;
            set => SetProperty(ref _groups, value);
        }

        public void FetchDataIf(Predicate<Models.AccountItem> predicate)
        {
            FetchDataHelper(predicate);
            HasFilter = true;
        }
        public void FetchData()
        {
            FetchDataHelper(t => true);
            HasFilter = false;
        }
        public void NewItem()
        {
            var group = Groups.FirstOrDefault(g => g?.GroupName == DefualtGroupName, null);
            if (group is null)
            {
                group = new AccountItemGroup(Messenger)
                {
                    GroupName = DefualtGroupName
                };
                Groups.Add(group);
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
            foreach (var group in Groups)
            {
                if (group.Items.FirstOrDefault(t => t?.Guid == guid, null) is AccountItem t)
                {
                    DataCenter.Delete(t.Guid);
                    group.Fetch(DataCenter);
                    break;
                }
            }
            RemoveEmptyGroups();
        }

        public Manager(DataCenter dataCenter, IMessenger messenger) : base(messenger)
        {
            DataCenter = dataCenter;
            /* 检查条目的所属组是否被修改，若被修改则重新调整条目 */
            Messenger.Register<AccountItemUpdatedMessage>(this, (sender, e) =>
            {
                AccountItemGroup tGroup = null!;
                Models.AccountItem tItem = null!;
                int groupIndex = 0;
                int itemIndex = 0;

                for (; groupIndex < Groups.Count; groupIndex++)
                {
                    var group = Groups[groupIndex];
                    itemIndex = 0;
                    for (; itemIndex < group.Items.Length; itemIndex++)
                    {
                        if (e.Guid == group.Items[itemIndex].Guid)
                        {
                            tGroup = group;
                            tItem = DataCenter.Retrieve(group.Items[itemIndex].Guid);
                            break;
                        }
                    }
                }
                if (tGroup is null) throw new NullReferenceException();
                if (tItem is null) throw new NullReferenceException();

                if (tGroup.GroupName != tItem.GroupName)
                {
                    if (!Groups.Any(t => t.GroupName == e.Data.GroupName))
                    {
                        Groups.Add(new AccountItemGroup(Messenger)
                        {
                            GroupName = e.Data.GroupName
                        });
                    }
                    for (int i = 0; i < Groups.Count; i++)
                    {
                        var group = Groups[i];
                        if (group.GroupName == tGroup.GroupName || group.GroupName == tItem.GroupName)
                        {
                            group.Fetch(DataCenter);
                            if (group.GroupName == tItem.GroupName)
                            {
                                group.Items.First(t => t.Guid == tItem.Guid).IsSelected = true;
                            }
                            group.IsExpanded = true;
                        }
                    }
                    RemoveEmptyGroups();
                    SortGroups();
                }
            });
        }

        private bool _hasFilter;
        private ObservableCollection<AccountItemGroup> _groups = new();
        private void GenerateGroups(IEnumerable<Models.AccountItem> items)
        {
            var dict = new Dictionary<string, List<AccountItem>>();
            foreach (var item in items)
            {
                var vmItem = new AccountItem(Messenger)
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
                    dict[item.GroupName] = new List<AccountItem>();
                    dict[item.GroupName].Add(vmItem);
                }
            }

            Groups.Clear();
            foreach (var groupName in dict.Keys)
            {
                Groups.Add(new AccountItemGroup(Messenger)
                {
                    GroupName = groupName,
                    Items = dict[groupName].ToArray()
                });
            }
        }
        private void SortGroups()
        {
            var t = Groups.ToArray();
            Array.Sort(t, GroupComparer.Default);
            Groups.Clear();
            foreach (var item in t)
            {
                item.SortItems();
                Groups.Add(item);
            }
        }
        private void RemoveEmptyGroups()
        {
            for (int i = 0; i < Groups.Count; i++)
            {
                if (!Groups[i].Items.Any())
                {
                    Groups.RemoveAt(i);
                    i--;
                }
            }
        }
        public void FetchDataHelper(Predicate<Models.AccountItem> predicate)
        {
            var data = DataCenter.Retrieve(predicate);
            GenerateGroups(data);
            SortGroups();
        }
    }
}
