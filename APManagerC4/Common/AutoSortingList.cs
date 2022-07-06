using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace APManagerC4
{
    public class AutoSortingList<TKey, TValue> : ICollection<TValue>, IEnumerable<TValue> where TKey : notnull
    {
        public Func<TValue, TKey> KeySelector { get; }
        public TValue this[int index] => _innerList.ElementAt(index).Value;
        public int Count => _innerList.Count;
        public bool IsReadOnly => false;

        public void Add(TValue item)
        {
            var key = KeySelector(item);
            _innerList.Add(key, item);
        }
        public void AddRange(IEnumerable<TValue> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
        public bool Remove(TValue item)
        {
            var key = KeySelector(item);
            return _innerList.Remove(key);
        }
        public bool Contains(TValue item)
        {
            return _innerList.ContainsValue(item);
        }
        public void ForEach(Action<TValue>? action)
        {
            foreach (var item in this)
            {
                action?.Invoke(item);
            }
        }
        public void Clear()
        {
            _innerList.Clear();
        }
        public IEnumerator<TValue> GetEnumerator()
        {
            foreach (var item in _innerList.Values)
            {
                yield return item;
            }
        }
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex] = item;
                arrayIndex++;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public AutoSortingList(Func<TValue, TKey> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);

            _innerList = new();
            KeySelector = keySelector;
        }
        public AutoSortingList(Func<TValue, TKey> keySelector, IComparer<TKey> keyComparer)
        {
            _innerList = new SortedList<TKey, TValue>(keyComparer);
            KeySelector = keySelector;
        }
        public AutoSortingList(Func<TValue, TKey> keySelector, IComparer<TKey> keyComparer, IEnumerable<TValue> items)
            : this(keySelector, keyComparer)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }


        private readonly SortedList<TKey, TValue> _innerList;
    }
}
