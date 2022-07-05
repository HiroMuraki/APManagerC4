using APManagerC4.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Input;

namespace APManagerC4.ViewModels
{
    public class AccountItemViewer : ObservableRecipient
    {
        public static RoutedCommand ApplyModificationCommand { get; } = new();
        public static RoutedCommand DeleteItemCommand { get; } = new();

        public Guid Guid
        {
            get => _guid;
            set => SetProperty(ref _guid, value);
        }
        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }
        public string Website
        {
            get => _website;
            set => SetProperty(ref _website, value);
        }
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }
        public string LoginName
        {
            get => _loginName;
            set => SetProperty(ref _loginName, value);
        }
        public string LoginPassword
        {
            get => _loginPassword;
            set => SetProperty(ref _loginPassword, value);
        }
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }
        public string Remarks
        {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }
        public DateTime CreationTime
        {
            get => _creationTime;
            set => SetProperty(ref _creationTime, value);
        }
        public DateTime UpdateTime
        {
            get => _updateTime;
            set => SetProperty(ref _updateTime, value);
        }
        public bool IsShownItem => !ReferenceEquals(_originData, _default);
        public bool ReadOnlyMode
        {
            get => _readOnlyMode;
            set => SetProperty(ref _readOnlyMode, value);
        }
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public void Reset()
        {
            Load(_originData);
        }
        public void Unload()
        {
            ReadOnlyMode = true;
            Load(_default);
        }
        public void Load(Models.AccountItem item)
        {
            LoadAccountItemModel(item);
            OnPropertyChanged(nameof(IsShownItem));
            HasUnsavedChanges = false;
        }
        public void SaveChanges()
        {
            UpdateTime = DateTime.Now;
            _originData = new Models.AccountItem()
            {
                Guid = Guid,
                Website = Website,
                GroupName = GroupName,
                Title = Title,
                UserName = UserName,
                LoginName = LoginName,
                LoginPassword = LoginPassword,
                Email = Email,
                Phone = Phone,
                Remarks = Remarks,
                CreationTime = CreationTime.Ticks,
                UpdateTime = UpdateTime.Ticks
            };

            _dataCenter.Upate(_guid, _originData);
            HasUnsavedChanges = false;

            Messenger.Send(new AccountItemUpdatedMessage()
            {
                Guid = _guid,
                Data = _originData
            });
        }

        public AccountItemViewer(IDataCenter<Models.AccountItem> dataCenter, IMessenger messenger) : base(messenger)
        {
            ReadOnlyMode = true;
            _dataCenter = dataCenter;
            Messenger.Register<RequestToViewDetailMessage>(this, (sender, e) =>
            {
                Unload();
                var item = _dataCenter.Retrieve(e.Guid);
                if (item is null)
                {
                    return;
                }
                ReadOnlyMode = e.ReadOnlyMode;
                Load(item);
            });
            PropertyChanged += (sender, e) =>
            {
                if (HasUnsavedChanges || e.PropertyName == nameof(HasUnsavedChanges))
                {
                    return;
                }

                HasUnsavedChanges = true;
            };
        }

        private static readonly Models.AccountItem _default = new();
        private readonly IDataCenter<Models.AccountItem> _dataCenter;
        private bool _readOnlyMode;
        private bool _hasUnsavedChanges;
        private Models.AccountItem _originData = _default;
        private Guid _guid;
        private string _groupName = string.Empty;
        private string _website = string.Empty;
        private string _title = string.Empty;
        private string _userName = string.Empty;
        private string _loginName = string.Empty;
        private string _loginPassword = string.Empty;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private string _remarks = string.Empty;
        private DateTime _creationTime;
        private DateTime _updateTime;
        private void LoadAccountItemModel(Models.AccountItem data)
        {
            _originData = data;
            Guid = data.Guid;
            GroupName = data.GroupName;
            Website = data.Website;
            Title = data.Title;
            UserName = data.UserName;
            LoginName = data.LoginName;
            LoginPassword = data.LoginPassword;
            Email = data.Email;
            Phone = data.Phone;
            Remarks = data.Remarks;
            CreationTime = DateTime.FromBinary(data.CreationTime);
            UpdateTime = DateTime.FromBinary(data.UpdateTime);
        }
    }
}
