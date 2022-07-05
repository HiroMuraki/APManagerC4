using APManagerC4.Models;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrderAttribute = System.Text.Json.Serialization.JsonPropertyOrderAttribute;
using PropertyName = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using System.Text;

namespace APManagerC4
{
    class TestDataCenter : IDataCenter<AccountItem>
    {
        class ItemEncrypter
        {
            public AESTextEncrypter? Encrypter { get; init; }

            public EncryptedAccountItem? GetEncrypted(AccountItem? item)
            {
                if (item is null) return null;

                var result = new EncryptedAccountItem()
                {
                    Guid = item.Guid,
                    Title = EncryptString(item.Title),
                    Website = EncryptString(item.Website),
                    GroupName = EncryptString(item.GroupName),
                    UserName = EncryptString(item.UserName),
                    LoginName = EncryptString(item.LoginName),
                    LoginPassword = EncryptString(item.LoginPassword),
                    Remarks = EncryptString(item.Remarks),
                    Email = EncryptString(item.Email),
                    Phone = EncryptString(item.Phone),
                    CreationTime = EncryptString(item.CreationTime.ToString()),
                    UpdateTime = EncryptString(item.UpdateTime.ToString())
                };
                return result;
            }
            public AccountItem? GetDecrypted(EncryptedAccountItem? item)
            {
                if (item is null) return null;

                var result = new AccountItem()
                {
                    Guid = item.Guid,
                    Title = DecryptString(item.Title),
                    GroupName = DecryptString(item.GroupName),
                    Email = DecryptString(item.Email),
                    LoginName = DecryptString(item.LoginName),
                    LoginPassword = DecryptString(item.LoginPassword),
                    UserName = DecryptString(item.UserName),
                    Phone = DecryptString(item.Phone),
                    Remarks = DecryptString(item.Remarks),
                    Website = DecryptString(item.Website),
                    CreationTime = long.TryParse(DecryptString(item.CreationTime), out var creationTime) ? creationTime : 0,
                    UpdateTime = long.TryParse(DecryptString(item.UpdateTime), out var updateTime) ? updateTime : 0
                };
                return result;
            }

            private string EncryptString(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return string.Empty;
                }
                return Encrypter?.Encrypt(text) ?? text;
            }
            private string DecryptString(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return string.Empty;
                }
                return Encrypter?.Decrypt(text) ?? text;
            }
        }

        [BytesSerializable]
        class EncryptedAccountItem
        {
            [BytesIncluded(0)] string _title = string.Empty;
            [BytesIncluded(1)] string _website = string.Empty;
            [BytesIncluded(2)] string _groupName = string.Empty;
            [BytesIncluded(3)] string _userName = string.Empty;
            [BytesIncluded(4)] string _loginName = string.Empty;
            [BytesIncluded(5)] string _loginPassword = string.Empty;
            [BytesIncluded(6)] string _email = string.Empty;
            [BytesIncluded(7)] string _phone = string.Empty;
            [BytesIncluded(8)] string _remarks = string.Empty;
            [BytesIncluded(9)] string _creationTime = string.Empty;
            [BytesIncluded(10)] string _updateTime = string.Empty;

            [JsonIgnore]
            public Guid Guid { get; init; }
            [Order(0), PropertyName("title")]
            public string Title { get => _title; init => _title = value; }
            [Order(1), PropertyName("website")]
            public string Website { get => _website; init => _website = value; }
            [Order(2), PropertyName("groupName")]
            public string GroupName { get => _groupName; init => _groupName = value; }
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
        }

        public bool HasUnsavedChanges { get; private set; }

        public void Add(Guid guid, AccountItem item)
        {
            _data.Add(_itemEncrypter.GetEncrypted(item)!);
            HasUnsavedChanges = true;
        }
        public void Delete(Guid guid)
        {
            if (_data.Remove(_data.First(t => t.Guid == guid)))
            {
                HasUnsavedChanges = true;
            }
        }
        public AccountItem Retrieve(Guid guid)
        {
            var result = _data.FirstOrDefault(t => t?.Guid == guid, null);
            return _itemEncrypter.GetDecrypted(result) ?? throw new ArgumentException("No data for " + guid);
        }
        public IEnumerable<AccountItem> Retrieve(Predicate<AccountItem>? predicate)
        {
            var result = from item in _data
                         let data = _itemEncrypter.GetDecrypted(item)
                         where predicate?.Invoke(data) ?? true
                         select data;

            foreach (var item in result)
            {
                yield return item;
            }
        }
        public void Upate(Guid guid, AccountItem newData)
        {
            var index = _data.FindIndex(0, t => t.Guid == guid);
            if (index == -1)
            {
                throw new ArgumentException("No data for " + guid);
            }
            _data[index] = _itemEncrypter.GetEncrypted(newData)!;
            HasUnsavedChanges = true;
        }

        public void Initialize(string password)
        {
            _itemEncrypter = new ItemEncrypter()
            {
                Encrypter = new AESTextEncrypter(PreprocessKey(password))
            };

            _data = new();
            if (File.Exists("data.dat"))
            {
                using (var fs = new FileStream("data.dat", FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    var data = _bytesSerializer.DeserializeFromBytes<EncryptedAccountItem[]>(buffer);
                    _data = new(data);
                }
            }
            if (File.Exists("data.json"))
            {
                using (var reader = new StreamReader("data.json"))
                {
                    var data = JsonSerializer.Deserialize<EncryptedAccountItem[]>(reader.ReadToEnd());
                    if (data is null)
                    {
                        throw new JsonException();
                    }
                    _data = new(data);
                }
            }

            HasUnsavedChanges = false;
        }
        public void SaveChanges()
        {
            using (var writer = new StreamWriter("data.json"))
            {
                string jsonText = JsonSerializer.Serialize(_data.ToArray());
                writer.Write(jsonText);
            }
            using (var fs = new FileStream("data.dat", FileMode.Create, FileAccess.Write))
            {
                fs.Write(_bytesSerializer.SerializeToBytes(_data.ToArray()));
            }
            HasUnsavedChanges = false;
        }
        public void ReEncrypt(string password)
        {
            var preEncrypter = _itemEncrypter;
            var preData = _data;

            _itemEncrypter = new ItemEncrypter()
            {
                Encrypter = new AESTextEncrypter(PreprocessKey(password))
            };
            _data = new List<EncryptedAccountItem>();

            foreach (var item in preData)
            {
                var data = preEncrypter.GetDecrypted(item);
                _data.Add(_itemEncrypter.GetEncrypted(data)!);
            }

            HasUnsavedChanges = true;
        }

        private readonly BytesSerializer _bytesSerializer = new() { Encoding = Encoding.ASCII };
        private ItemEncrypter _itemEncrypter = new();
        private List<EncryptedAccountItem> _data = new();
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
        //            Guid = Guid.NewGuid(),
        //            CreationTime = DateTime.Now.Ticks,
        //            UpdateTime = DateTime.Now.Ticks
        //        });
        //    }

        //}
    }
}
