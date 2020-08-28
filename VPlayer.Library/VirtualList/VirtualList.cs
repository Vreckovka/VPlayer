using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VPlayer.Library.VirtualList
{
  namespace VirtualLists
  {
    /// <summary>
    /// Virtual lists are lists from which the content is loaded on demand.
    /// </summary>
    /// <remarks>
    /// Use visual lists if it is expensive to populate the list and only
    /// a subset of the list's elements are used. The virtual list uses an
    /// object generator to populate the list on demand.
    /// </remarks>
    /// <typeparam name="T">Objects that are stored inside the list.</typeparam>
    public class VirtualList<T> : IList<T>, IList where T : class
    {
      #region Internal attributes
      /// <summary>
      /// Object that is used to generate the requested objects.
      /// </summary>
      /// <remarks>
      /// This object can also hold a IMultipleObjectGenerator reference.
      /// </remarks>
      private readonly IObjectGenerator<T> _generator;

      /// <summary>
      /// Internal array that holds the cached items.
      /// </summary>
      private readonly T[] _cachedItems;
      #endregion


      #region Constructor
      /// <summary>
      /// Create the virtual list.
      /// </summary>
      /// <param name="generator"></param>
      public VirtualList(IObjectGenerator<T> generator)
      {
        // Determine the number of items
        int maxItems = generator.Count;

        // Save generator and items
        _generator = generator;
        _cachedItems = new T[maxItems];
      }
      #endregion


      #region IList<T> Members
      public int IndexOf(T item)
      {
        return IndexOf(item);
      }

      public void Insert(int index, T item)
      {
        throw new NotSupportedException("VirtualList is a read-only collection.");
      }

      public void RemoveAt(int index)
      {
        throw new NotSupportedException("VirtualList is a read-only collection.");
      }

      public T this[int index]
      {
        get
        {
          if (index < _cachedItems.Length)
          {
            // Cache item if it isn't cached already
            if (!IsItemCached(index))
              CacheItem(index);

            // Return the cached object
            return _cachedItems[index];
          }

          return null;
        }
        set { throw new NotSupportedException("VirtualList is a read-only collection."); }
      }
      #endregion


      #region ICollection<T> Members
      public void Add(T item)
      {
        throw new NotSupportedException("VirtualList is a read-only collection.");
      }

      public void Clear()
      {
        throw new NotSupportedException("VirtualList is a read-only collection.");
      }

      public bool Contains(T item)
      {
        return (IndexOf(item) != -1);
      }

      public void CopyTo(T[] array, int arrayIndex)
      {
        _cachedItems.CopyTo(array, arrayIndex);
      }

      public int Count
      {
        get { return _cachedItems.Length; }
      }

      public bool IsReadOnly
      {
        get { return true; }
      }

      public bool Remove(T item)
      {
        throw new NotSupportedException("VirtualList is a read-only collection.");
      }
      #endregion


      #region IEnumerable<T> Members
      public IEnumerator<T> GetEnumerator()
      {
        return new VirtualEnumerator(this);
      }
      #endregion


      #region IList Members
      public int Add(object value)
      {
        throw new NotSupportedException("VirtualList is a read-only collection.");
      }

      public bool Contains(object value)
      {
        return IndexOf(value) != -1;
      }

      public int IndexOf(object value)
      {
        int items = _cachedItems.Length;
        for (int index = 0; index < items; ++index)
        {
          var item = _cachedItems[index];
          // Check if item is found
          if (item != null && item.Equals(value))
            return index;
        }

        // Item not found
        return -1;
      }

      public void Insert(int index, object value)
      {
        throw new NotSupportedException("VirtualList is a read-only collection.");
      }

      public bool IsFixedSize
      {
        get { return true; }
      }

      public void Remove(object value)
      {
        throw new NotSupportedException("VirtualList is a read-only collection.");
      }

      object IList.this[int index]
      {
        get { return this[index]; }
        set { throw new NotSupportedException("VirtualList is a read-only collection."); }
      }
      #endregion


      #region ICollection Members
      public void CopyTo(Array array, int index)
      {
        _cachedItems.CopyTo(array, index);
      }

      public bool IsSynchronized
      {
        get { return false; }
      }

      public object SyncRoot
      {
        get { return this; }
      }
      #endregion


      #region IEnumerable Members
      IEnumerator IEnumerable.GetEnumerator()
      {
        return new VirtualEnumerator(this);
      }
      #endregion


      #region Internal helper methods required for caching
      private bool IsItemCached(int index)
      {

        // If the object is NULL, then it is empty
        return (_cachedItems[index] != null);

      }
      #endregion

      public void CacheItem(int index)
      {
        // Obtain only a single object
        _cachedItems[index] = _generator.CreateObject(index);
      }


      #region Internal IEnumerator implementation
      private class VirtualEnumerator : IEnumerator<T>
      {
        private readonly VirtualList<T> _collection;
        private int _cursor;

        public VirtualEnumerator(VirtualList<T> collection)
        {
          _collection = collection;
          _cursor = 0;
        }

        public T Current
        {
          get { return _collection[_cursor]; }
        }


        object IEnumerator.Current
        {
          get { return Current; }
        }

        public bool MoveNext()
        {
          // Check if we are behind
          if (_cursor == _collection.Count)
            return false;

          // Increment cursor
          ++_cursor;
          return true;
        }

        public void Reset()
        {
          // Reset cursor
          _cursor = 0;
        }


        public void Dispose()
        {
          // NOP
        }

      }
      #endregion


    }
  }
}