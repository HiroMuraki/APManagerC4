using Uid = HM.Common.Uid;

namespace APManagerC4
{
    /// <summary>
    /// 增删改查协定
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDataCenter<T>
    {
        T Retrieve(Uid guid);
        IEnumerable<T> Retrieve(Predicate<T> predicate);
        void Add(Uid guid, T accountItem);
        void Update(Uid guid, T newData);
        void Delete(Uid guid);
    }
}
