using System;
using System.Collections.Generic;
using System.Text;

namespace SS.Utilities
{
    /// <summary>
    /// Dictionary that allows multiple values per key.
    /// Each bucket internally is a LinkedList, hence the similarity many of the methods have to LinkedLists.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class MultiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, ICollection<KeyValuePair<TKey, TValue>>
    {
        private Dictionary<TKey, LinkedList<TValue>> _dictionary;
        private int _count = 0;

        public MultiDictionary()
        {
            _dictionary = new Dictionary<TKey, LinkedList<TValue>>();
        }

        public MultiDictionary(IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, LinkedList<TValue>>(comparer);
        }

        public MultiDictionary(int capacity)
        {
            _dictionary = new Dictionary<TKey, LinkedList<TValue>>(capacity);
        }

        public MultiDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, LinkedList<TValue>>(capacity, comparer);
        }

        /// <summary>
        /// inserts a new key, value pair into the table.
        /// this value will show up at the front of the list when this key is queried for.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddFirst(TKey key, TValue value)
        {
            // try to find the bucket
            LinkedList<TValue> bucketList;
            if (_dictionary.TryGetValue(key, out bucketList) == false)
            {
                // did not exist, create it
                bucketList = new LinkedList<TValue>();
                _dictionary.Add(key, bucketList);
            }

            bucketList.AddFirst(value);
            _count++;
        }

        /// <summary>
        /// inserts a new key, value pair into the table.
        /// this value will show up at the end of the list when this key is queried for.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddLast(TKey key, TValue value)
        {
            LinkedList<TValue> bucketList;
            if (_dictionary.TryGetValue(key, out bucketList) == false)
            {
                bucketList = new LinkedList<TValue>();
                _dictionary.Add(key, bucketList);
            }

            bucketList.AddLast(value);
            _count++;
        }
        
        /// <summary>
        /// removes one instance of the key, value pair
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Remove(TKey key, TValue value)
        {
            LinkedList<TValue> bucketList;
            if (_dictionary.TryGetValue(key, out bucketList) == false)
            {
                // key does not exist, nothing to remove
                return false;
            }

            bool wasRemoved = bucketList.Remove(value);
            if (wasRemoved)
            {
                _count--;

                if (bucketList.Count == 0)
                {
                    // it was the last item in the bucket, remove the bucket too
                    _dictionary.Remove(key);
                }
            }

            return wasRemoved;
        }

        /// <summary>
        /// Gets all of the items that match the specified key
        /// </summary>
        /// <param name="key">the key of the values to get</param>
        /// <param name="values">an enumerable collection of items stored for the specified key</param>
        /// <returns>true if items are found, otherwise false</returns>
        public bool TryGetValues(TKey key, out IEnumerable<TValue> values)
        {
            LinkedList<TValue> bucketList;
            if (_dictionary.TryGetValue(key, out bucketList))
            {
                values = bucketList;
                return true;
            }
            else
            {
                values = null;
                return false;
            }
        }
        
        /// <summary>
        /// Gets all of the items that match the specified key and adds them to the end of an existing list.
        /// </summary>
        /// <param name="key">the key of the values to get</param>
        /// <param name="appendToList">an existing list to add matching items to</param>
        /// <returns>whether items were found and added to the list</returns>
        public bool TryGetAppendValues(TKey key, LinkedList<TValue> appendToList)
        {
            if (appendToList == null)
                throw new ArgumentNullException("appendToList");

            LinkedList<TValue> bucketList;
            if (_dictionary.TryGetValue(key, out bucketList))
            {
                foreach (TValue v in bucketList)
                    appendToList.AddLast(v);

                return true;
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the first value assocated with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="firstValue"></param>
        /// <returns></returns>
        public bool TryGetFirstValue(TKey key, out TValue firstValue)
        {
            LinkedList<TValue> bucketList;
            if (_dictionary.TryGetValue(key, out bucketList) == false)
            {
                firstValue = default(TValue);
                return false;
            }

            if(bucketList.Count == 0) // this should never come back as true if this class is implemented correctly, putting this here to be defensive...
            {
                firstValue = default(TValue);
                return false;
            }

            firstValue = bucketList.First.Value;
            return true;
        }

        /// <summary>
        /// Get or set all of the items in a bucket.  Note: setting will remove any items that are currently in the bucket.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IEnumerable<TValue> this[TKey key]
        {
            get
            {
                // return the bucket
                return _dictionary[key];
            }
            set
            {
                // try to find the bucket
                LinkedList<TValue> bucketList;
                if (_dictionary.TryGetValue(key, out bucketList) == false)
                {
                    // did not exist, create it
                    bucketList = new LinkedList<TValue>();
                    _dictionary[key] = bucketList;
                }
                else
                {
                    // bucket already exists, remove anything already in it
                    _count -= bucketList.Count;
                    bucketList.Clear();
                }

                // add each value into the bucket
                foreach (TValue tval in value)
                {
                    bucketList.AddLast(tval);
                    _count++;
                }

                if (bucketList.Count == 0)
                {
                    // no items in the bucket, which means the bucket shouldn't exist
                    _dictionary.Remove(key);
                }
            }
        }

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        private struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private IEnumerator<KeyValuePair<TKey, LinkedList<TValue>>> _dictionaryEnumerator;
            private IEnumerator<TValue> _linkedListEnumerator;

            public Enumerator(MultiDictionary<TKey, TValue> multiDictionary)
            {
                _dictionaryEnumerator = multiDictionary._dictionary.GetEnumerator();
                _linkedListEnumerator = null;
            }

            #region IEnumerator<KeyValuePair<TKey,TValue>> Members

            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    return new KeyValuePair<TKey, TValue>(
                        _dictionaryEnumerator.Current.Key,
                        _linkedListEnumerator.Current);
                }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                if (_linkedListEnumerator == null || _linkedListEnumerator.MoveNext() == false)
                {
                    if (_dictionaryEnumerator.MoveNext() == false)
                    {
                        return false;
                    }

                    _linkedListEnumerator = _dictionaryEnumerator.Current.Value.GetEnumerator();
                    return _linkedListEnumerator.MoveNext();
                }

                return true;
            }

            public void Reset()
            {
                _dictionaryEnumerator.Reset();
                _linkedListEnumerator = null;
            }

            #endregion
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            AddLast(item.Key, item.Value);
        }

        public void Clear()
        {
            _dictionary.Clear();
            _count = 0;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            LinkedList<TValue> bucketList;
            if (_dictionary.TryGetValue(item.Key, out bucketList) == false)
            {
                return false;
            }

            return bucketList.Contains(item.Value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if(arrayIndex >= array.Length)
            {
                throw new ArgumentException("arrayIndex is equal to or greater than the length of array");
            }
            if (array.Length - arrayIndex < _count)
            {
                throw new ArgumentException("The number of elements in the source ICollection is greater than the available space from arrayIndex to the end of the destination array");
            }

            foreach (KeyValuePair<TKey, TValue> kvp in this)
            {
                array[arrayIndex++] = kvp;
            }
        }

        public int Count
        {
            get { return _count; }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key, item.Value);
        }

        #endregion
    }
}
