using APManagerC4.Models;
using HM.Common;
using HM.Cryptography;
using HM.Serialization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using OrderAttribute = System.Text.Json.Serialization.JsonPropertyOrderAttribute;
using PropertyName = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using Uid = HM.Common.Uid;

namespace APManagerC4
{
    /// <summary>
    /// 默认数据中心
    /// </summary>
    internal class TestDataCenter : IDataCenter<AccountItem>, IDataProvider<LabelInfo>
    {
        [BytesSerializable]
        private class EncryptedAccountItem
        {
            [BytesIncluded(0)] private string _title = string.Empty;
            [BytesIncluded(1)] private string _website = string.Empty;
            [BytesIncluded(2)] private string _category = string.Empty;
            [BytesIncluded(3)] private string _userName = string.Empty;
            [BytesIncluded(4)] private string _loginName = string.Empty;
            [BytesIncluded(5)] private string _loginPassword = string.Empty;
            [BytesIncluded(6)] private string _email = string.Empty;
            [BytesIncluded(7)] private string _phone = string.Empty;
            [BytesIncluded(8)] private string _remarks = string.Empty;
            [BytesIncluded(9)] private string _creationTime = string.Empty;
            [BytesIncluded(10)] private string _updateTime = string.Empty;

            [JsonIgnore]
            public Uid Uid { get; init; } = UidGenerator.Default.Next();
            [Order(0), PropertyName("title")]
            public string Title { get => _title; init => _title = value; }
            [Order(1), PropertyName("website")]
            public string Website { get => _website; init => _website = value; }
            [Order(2), PropertyName("groupName")]
            public string Category { get => _category; init => _category = value; }
            [Order(3), PropertyName("userName")]
            public string UserName { get => _userName; init => _userName = value; }
            [Order(4), PropertyName("loginName")]
            public string LoginName { get => _loginName; init => _loginName = value; }
            [Order(5), PropertyName("loginPassword")]
            public string LoginPassword { get => _loginPassword; init => _loginPassword = value; }
            [Order(6), PropertyName("email")]
            public string Email { get => _email; init => _email = value; }
            [Order(7), PropertyName("phone")]
            public string Phone { get => _phone; init => _phone = value; }
            [Order(8), PropertyName("remarks")]
            public string Remarks { get => _remarks; init => _remarks = value; }
            [Order(9), PropertyName("creationTime")]
            public string CreationTime { get => _creationTime; init => _creationTime = value; }
            [Order(10), PropertyName("updateTime")]
            public string UpdateTime { get => _updateTime; init => _updateTime = value; }


            public static EncryptedAccountItem FromAccountItem(AccountItem item, ITextEncrypter? textEncrypter)
            {
                return new EncryptedAccountItem()
                {
                    Uid = item.Uid,
                    Title = EncryptString(item.Title, textEncrypter),
                    Website = EncryptString(item.Website, textEncrypter),
                    Category = EncryptString(item.Category, textEncrypter),
                    UserName = EncryptString(item.UserName, textEncrypter),
                    LoginName = EncryptString(item.LoginName, textEncrypter),
                    LoginPassword = EncryptString(item.LoginPassword, textEncrypter),
                    Remarks = EncryptString(item.Remarks, textEncrypter),
                    Email = EncryptString(item.Email, textEncrypter),
                    Phone = EncryptString(item.Phone, textEncrypter),
                    CreationTime = EncryptString(item.CreationTime.ToString(), textEncrypter),
                    UpdateTime = EncryptString(item.UpdateTime.ToString(), textEncrypter)
                };
            }
            public AccountItem ToAccountItem(ITextEncrypter? textEncrypter)
            {
                return new AccountItem()
                {
                    Uid = Uid,
                    Title = DecryptString(Title, textEncrypter),
                    Category = DecryptString(Category, textEncrypter),
                    Email = DecryptString(Email, textEncrypter),
                    LoginName = DecryptString(LoginName, textEncrypter),
                    LoginPassword = DecryptString(LoginPassword, textEncrypter),
                    UserName = DecryptString(UserName, textEncrypter),
                    Phone = DecryptString(Phone, textEncrypter),
                    Remarks = DecryptString(Remarks, textEncrypter),
                    Website = DecryptString(Website, textEncrypter),
                    CreationTime = long.TryParse(DecryptString(CreationTime, textEncrypter), out var creationTime) ? creationTime : 0,
                    UpdateTime = long.TryParse(DecryptString(UpdateTime, textEncrypter), out var updateTime) ? updateTime : 0
                };
            }
            public LabelInfo ToLabelInfo(ITextEncrypter? textEncrypter)
            {
                return new LabelInfo()
                {
                    Uid = Uid,
                    Title = DecryptString(Title, textEncrypter),
                    Category = DecryptString(Category, textEncrypter)
                };
            }

            private static string EncryptString(string text, ITextEncrypter? textEncrypter)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return string.Empty;
                }
                return textEncrypter?.Encrypt(text) ?? text;
            }
            private static string DecryptString(string text, ITextEncrypter? textEncrypter)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return string.Empty;
                }
                return textEncrypter?.Decrypt(text) ?? text;
            }
        }

        public bool HasUnsavedChanges { get; private set; }

        public void Add(Uid guid, AccountItem item)
        {
            _data[item.Uid] = EncryptedAccountItem.FromAccountItem(item, _textEncrypter)!;
            HasUnsavedChanges = true;
        }
        public void Delete(Uid guid)
        {
            if (_data.Remove(guid))
            {
                HasUnsavedChanges = true;
            }
        }
        public AccountItem Retrieve(Uid guid)
        {
            return _data[guid].ToAccountItem(_textEncrypter);
        }
        public IEnumerable<AccountItem> Retrieve(Predicate<AccountItem>? predicate)
        {
            var result = from item in _data.Values
                         let data = item.ToAccountItem(_textEncrypter)
                         where predicate?.Invoke(data) ?? true
                         select data;

            foreach (var item in result)
            {
                yield return item;
            }
        }
        public IEnumerable<LabelInfo> Retrieve(Predicate<LabelInfo>? predicate)
        {
            var result = from item in _data.Values
                         let data = item.ToLabelInfo(_textEncrypter)
                         where predicate?.Invoke(data) ?? true
                         select data;

            foreach (var item in result)
            {
                yield return item;
            }
        }
        public void Update(Uid guid, AccountItem newData)
        {
            _data[guid] = EncryptedAccountItem.FromAccountItem(newData, _textEncrypter)!;
            HasUnsavedChanges = true;
        }

        public void Initialize(string password)
        {
            _textEncrypter = new AesTextEncrypter(PreprocessKey(password));
            try
            {
#if BYTES_SERIALIZATION || ALL_SERIALIZATION
                if (File.Exists(_dataFileName))
                {
                    using (var fs = new FileStream(_dataFileName, FileMode.Open, FileAccess.Read))
                    {
                        var buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, buffer.Length);
                        var data = _bytesSerializer.DeserializeFromBytes<EncryptedAccountItem[]>(buffer);
                        _data = data.ToDictionary(d => d.Uid);
                    }
                }
#elif JSON_SERIALIZATION || ALL_SERIALIZATION
            if (File.Exists("data.json"))
            {
                using (var reader = new StreamReader("data.json"))
                {
                    var data = JsonSerializer.Deserialize<EncryptedAccountItem[]>(reader.ReadToEnd());
                    if (data is null)
                    {
                        throw new JsonException();
                    }
                    _data = data.ToDictionary(d => d.Uid);
                }
            }
#endif
                HasUnsavedChanges = false;
            }
            catch
            {
                _data = new Dictionary<Uid, EncryptedAccountItem>();
                throw;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="IOException"></exception>
        public void SaveChanges()
        {
            string backupFileName = $"{_dataFileName}_{Guid.NewGuid()}.backup";
            try
            {
                if (File.Exists(_dataFileName))
                {
                    File.Copy(_dataFileName, backupFileName);
                }
#if BYTES_SERIALIZATION || ALL_SERIALIZATION
                using (var fs = new FileStream(_dataFileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(_bytesSerializer.SerializeToBytes(_data.Values.ToArray()));
                }
#elif JSON_SERIALIZATION || ALL_SERIALIZATION
            using (var writer = new StreamWriter(fileName))
            {
                string jsonText = JsonSerializer.Serialize(_data.ToArray());
                writer.Write(jsonText);
            }
#endif
                HasUnsavedChanges = false;
            }
            catch
            {
                HasUnsavedChanges = true;
                throw new IOException();
            }
        }
        public void ReEncrypt(string password)
        {
            var preEncrypter = _textEncrypter;
            var preData = _data;

            _textEncrypter = new AesTextEncrypter(PreprocessKey(password));
            _data = new Dictionary<Uid, EncryptedAccountItem>();

            foreach (var item in preData.Values)
            {
                var data = item.ToAccountItem(preEncrypter);
                _data[item.Uid] = EncryptedAccountItem.FromAccountItem(data, _textEncrypter)!;
            }

            HasUnsavedChanges = true;
        }

        private static readonly string _dataFileName = "data.dat";
        private readonly BytesSerializer _bytesSerializer = new() { TextEncoding = Encoding.ASCII };
        private ITextEncrypter? _textEncrypter;
        private Dictionary<Uid, EncryptedAccountItem> _data = new();
        private static byte[] PreprocessKey(string password)
        {
            using (var hashFunc = SHA256.Create())
            {
                return hashFunc.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        //private readonly static LinkedList<AccountItem> _fakeData = new();
        //private static string RandomString()
        //{
        //    var sb = new StringBuilder();
        //    var rnd = new Random();
        //    int len = rnd.Next(5, 15);
        //    for (int i = 0; i < len; i++)
        //    {
        //        sb.Append((char)rnd.Next(65, 65 + 26));
        //    }
        //    return sb.ToString();
        //}
        //static TestDataCenter()
        //{
        //    string[] groups = { "AAA", "BBB", "CCC", "DDD" };
        //    for (int i = 0; i < 10; i++)
        //    {
        //        _fakeData.AddLast(new AccountItem()
        //        {
        //            GroupName = groups[i % groups.Length],
        //            Title = RandomString(),
        //            Website = RandomString(),
        //            Email = RandomString(),
        //            Phone = RandomString(),
        //            LoginName = RandomString(),
        //            LoginPassword = RandomString(),
        //            Remarks = RandomString(),
        //            UserName = RandomString(),
        //            Uid = Uid.NewUid(),
        //            CreationTime = DateTime.Now.Ticks,
        //            UpdateTime = DateTime.Now.Ticks
        //        });
        //    }

        //}
    }
}
