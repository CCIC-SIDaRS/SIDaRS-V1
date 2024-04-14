using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.ThreadSafety
{
    class ConcurrentList<T> : IList<T>
    {
        private List<T> _internalList;
        private readonly object _lock = new object();

        public ConcurrentList()
        {
            _internalList = new List<T>();
        }

        public T this[int index]
        {
            get
            {
                lock (_lock)
                {
                    return _internalList[index];
                }
            }
            set
            {
                lock (_lock)
                {
                    _internalList[index] = value;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _internalList.Count;
                }
            }
        }

        private  List<T> Clone()
        {
            lock (_lock)
            {
                return new List<T>(_internalList);
            }
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                _internalList.Add(item);
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                return _internalList.Remove(item);
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                return _internalList.Contains(item);
            }
        }

        public int IndexOf(T item)
        {
            lock (_lock)
            {
                return _internalList.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (_lock)
            {
                _internalList.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock(_lock)
            {
                try
                {
                    _internalList.RemoveAt(index);
                }catch(ArgumentOutOfRangeException)
                {
                    throw new IndexOutOfRangeException();
                }
                
            }
        }

        public void Clear()
        {
            lock(_lock)
            {
                _internalList.Clear();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock(_lock)
            {
                _internalList.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }
    }
}
