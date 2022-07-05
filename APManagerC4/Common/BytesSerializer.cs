using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace APManagerC4
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    class BytesSerializableAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    class BytesIncludedAttribute : Attribute
    {
        public int Order { get; }

        public BytesIncludedAttribute(int order)
        {
            Order = order;
        }
    }

    class BytesSerializer
    {
        public static BytesSerializer Default { get; } = new();
        public const int SizeOfInt32 = sizeof(int);

        public Encoding Encoding { get; init; } = Encoding.UTF8;

        public byte[] SerializeToBytes(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var objType = obj.GetType();
            if (objType == typeof(string))
            {
                // 编码字符串，写入编码后的字节数组长度 + 字节数组长度
                byte[] encoded = Encoding.GetBytes((string)obj!);
                byte[] lengthInfo = BitConverter.GetBytes(encoded.Length);
                byte[] result = new byte[SizeOfInt32 + encoded.Length];
                Array.Copy(lengthInfo, 0, result, 0, lengthInfo.Length);
                Array.Copy(encoded, 0, result, SizeOfInt32, encoded.Length);
                return result;
            }
            else if (objType == typeof(int))
            {
                return BitConverter.GetBytes((int)obj!);
            }
            else if (objType == typeof(long))
            {
                return BitConverter.GetBytes((long)obj!);
            }
            else if (objType.IsArray)
            {
                var array = (Array)obj;
                byte[] lengthInfo = BitConverter.GetBytes(array.Length);
                var bytes = new List<byte>();
                for (int i = 0; i < array.Length; i++)
                {
                    var element = array.GetValue(i) ?? throw new NullReferenceException("Element in array can't be null");
                    bytes.AddRange(SerializeToBytes(element));
                }

                byte[] result = new byte[SizeOfInt32 + bytes.Count];
                Array.Copy(lengthInfo, 0, result, 0, lengthInfo.Length);
                Array.Copy(bytes.ToArray(), 0, result, SizeOfInt32, bytes.Count);
                return result.ToArray();
            }
            else if (objType.IsDefined(typeof(BytesSerializableAttribute)))
            {
                var bytes = new List<byte>();
                foreach (var fieldInfo in GetFieldInfos(obj.GetType()))
                {
                    var value = fieldInfo.GetValue(obj) ?? throw new NullReferenceException($"Value of '{fieldInfo}' cant't be null");
                    bytes.AddRange(SerializeToBytes(value));
                }
                return bytes.ToArray();
            }
            else
            {
                throw new Exception($"Type '{objType}' is not supported");
            }
        }
        public T DeserializeFromBytes<T>(byte[] bytes)
        {
            DeserializeFromBytesCore(bytes.AsSpan(), typeof(T), out var result);
            return (T)result;
        }

        public static void ClearFieldInfosCache()
        {
            _fieldInfosCache.Clear();
        }

        private static readonly BindingFlags _defaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Dictionary<Guid, FieldInfo[]> _fieldInfosCache = new();
        private static FieldInfo[] GetFieldInfos(Type type)
        {
            if (_fieldInfosCache.TryGetValue(type.GUID, out var result))
            {
                return result;
            }
            else
            {
                var fieldInfos = (from fieldInfo in type.GetFields(_defaultBindingFlags)
                                  let att = fieldInfo.GetCustomAttribute<BytesIncludedAttribute>()
                                  where att is not null
                                  orderby att.Order
                                  select fieldInfo).ToArray();
                _fieldInfosCache[type.GUID] = fieldInfos;
                return fieldInfos;
            }
        }
        private int DeserializeFromBytesCore(ReadOnlySpan<byte> bytes, Type targetType, out object result)
        {
            if (targetType == typeof(string))
            {
                int lengthInfo = BitConverter.ToInt32(bytes);
                result = Encoding.GetString(bytes[SizeOfInt32..(SizeOfInt32 + lengthInfo)]);
                return SizeOfInt32 + lengthInfo;
            }
            else if (targetType == typeof(int))
            {
                result = BitConverter.ToInt32(bytes);
                return SizeOfInt32;
            }
            else if (targetType == typeof(long))
            {
                result = BitConverter.ToInt64(bytes);
                return sizeof(long);
            }
            else if (targetType.IsArray)
            {
                int arraySize = BitConverter.ToInt32(bytes);

                int offset = 4;
                var elementType = targetType.GetElementType()!;
                var resultArray = Array.CreateInstance(elementType, arraySize);
                for (int i = 0; i < arraySize; i++)
                {
                    offset += DeserializeFromBytesCore(bytes[offset..], elementType, out var value);
                    resultArray.SetValue(value, i);
                }
                result = resultArray;
                return offset;
            }
            else if (targetType.IsDefined(typeof(BytesSerializableAttribute)))
            {
                int offset = 0;
                var obj = Activator.CreateInstance(targetType)!;
                foreach (var fieldInfo in GetFieldInfos(targetType))
                {
                    offset += DeserializeFromBytesCore(bytes[offset..], fieldInfo.FieldType, out var value);
                    fieldInfo.SetValue(obj, value);
                }
                result = obj;
                return offset;
            }
            else
            {
                throw new Exception($"Type '{targetType}' is not supported");
            }
        }
    }
}
