using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace APManagerC4
{
    public class ObservableList<T> : ObservableCollection<T> where T : class
    {
        public void ForEach(Action<T>? action)
        {
            if (action is null)
            {
                return;
            }

            foreach (var item in this)
            {
                action(item);
            }
        }
        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
        public ObservableList<T> Sort(IComparer<T> comparer)
        {
            /* 将原列表转换为普通数组后使用comparer进行排序，
             * 将排序后的列表和原列表的对应下标元素进行比较，若不同则赋给原列表。
             * 之所以不清空后重新填入，是为了尽可能减少CollectionChanged事件的引发次数，避免不必要的数据更新
             * 因为这一原因，元素必须为引用类型 */
            var list = this.ToArray();
            Array.Sort(list, comparer);

            for (int i = 0; i < list.Length; i++)
            {
                if (!ReferenceEquals(this[i], list[i]))
                {
                    this[i] = list[i];
                }
            }

            return this;
        }

        public ObservableList(IEnumerable<T> items)
        {
            AddRange(items);
        }
        public ObservableList()
        {

        }
    }
}
