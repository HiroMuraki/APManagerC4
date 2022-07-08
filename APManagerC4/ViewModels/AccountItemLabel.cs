using APManagerC4.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Input;
using Guid = HM.Common.Uid;

namespace APManagerC4.ViewModels
{
    public class AccountItemLabel : ObservableRecipient
    {
        public static RoutedCommand RequestToViewCommand { get; } = new();

        public int GroupID
        {
            get => _groupID;
            set => SetProperty(ref _groupID, value);
        }
        public Guid Uid
        {
            get => _guid;
            init => _guid = value;
        }
        public string Title
        {
            get => _title.Replace("_", "__");
            set => SetProperty(ref _title, value);
        }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value)
                {
                    var itemGroup = _itemGroups[GroupID];
                    var node = itemGroup.First;

                    while (node is not null)
                    {
                        var next = node.Next;

                        if (node.Value.TryGetTarget(out var label))
                        {
                            if (label.IsSelected)
                            {
                                label.IsSelected = false;
                            }
                        }
                        else
                        {
                            itemGroup.Remove(node);
                        }

                        node = next;
                    }

                }

                /* 由于设置该值的时候可能引起itemGroups项的变化，
                 * 因此进行定期清理 */
                var keys = _itemGroups.Keys.ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    if (_itemGroups[keys[i]].Count == 0)
                    {
                        _itemGroups.Remove(keys[i]);
                        --i;
                    }
                }

                SetProperty(ref _isSelected, value);
            }
        }

        public void RequestToView()
        {
            var message = new RequestToViewDetailMessage()
            {
                Uid = Uid,
            };
            Messenger.Send(message);
        }

        public AccountItemLabel(IMessenger messenger) : base(messenger)
        {
            Messenger.Register<AccountItemUpdatedMessage>(this, (sender, e) =>
            {
                if (Uid == e.Uid)
                {
                    Title = e.Data.Title;
                }
            });

            var selfWeakRef = new WeakReference<AccountItemLabel>(this);
            if (_itemGroups.TryGetValue(GroupID, out var itemGroup))
            {
                itemGroup.AddLast(selfWeakRef);
            }
            else
            {
                _itemGroups[GroupID] = new LinkedList<WeakReference<AccountItemLabel>>();
                _itemGroups[GroupID].AddLast(selfWeakRef);
            }
        }

        private static readonly Dictionary<int, LinkedList<WeakReference<AccountItemLabel>>> _itemGroups = new();
        private readonly Guid _guid;
        private string _title = string.Empty;
        private bool _isSelected;
        private int _groupID;
    }
}
