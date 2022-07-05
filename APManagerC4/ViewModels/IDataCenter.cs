using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APManagerC4.ViewModels
{
    public interface IDataCenter<T>
    {
        T Retrieve(Guid guid);
        IEnumerable<T> Retrieve(Predicate<T> predicate);
        void Add(Guid guid, T accountItem);
        void Upate(Guid guid, T newData);
        void Delete(Guid guid);
    }
}
