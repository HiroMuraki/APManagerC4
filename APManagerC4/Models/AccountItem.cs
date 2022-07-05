namespace APManagerC4.Models
{
    /// <summary>
    /// 核心数据模型
    /// </summary>
    public record class AccountItem : IEquatable<AccountItem>
    {
        public Guid Guid
        {
            get => _guid;
            init
            {
                _guid = value;
            }
        }
        public string GroupName
        {
            get => _groupName;
            init
            {
                _groupName = value;
            }
        }
        public string Website
        {
            get => _website;
            init
            {
                _website = value;
            }
        }
        public string Title
        {
            get => _title;
            init
            {
                _title = value;
            }
        }
        public string UserName
        {
            get => _userName;
            init
            {
                _userName = value;
            }
        }
        public string LoginName
        {
            get => _loginName;
            init
            {
                _loginName = value;
            }
        }
        public string LoginPassword
        {
            get => _loginPassword;
            init
            {
                _loginPassword = value;
            }
        }
        public string Email
        {
            get => _email;
            init
            {
                _email = value;
            }
        }
        public string Phone
        {
            get => _phone;
            init
            {
                _phone = value;
            }
        }
        public string Remarks
        {
            get => _remarks;
            init
            {
                _remarks = value;
            }
        }
        public long CreationTime
        {
            get => _creationTime;
            init
            {
                _creationTime = value;
            }
        }
        public long UpdateTime
        {
            get => _updateTime;
            init
            {
                _updateTime = value;
            }
        }

        private readonly Guid _guid;
        private readonly string _groupName = string.Empty;
        private readonly string _website = string.Empty;
        private readonly string _title = string.Empty;
        private readonly string _userName = string.Empty;
        private readonly string _loginName = string.Empty;
        private readonly string _loginPassword = string.Empty;
        private readonly string _email = string.Empty;
        private readonly string _phone = string.Empty;
        private readonly string _remarks = string.Empty;
        private readonly long _creationTime;
        private readonly long _updateTime;
    }
}
