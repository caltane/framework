﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Collections.ObjectModel;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization; 

namespace Signum.Entities
{


    [Serializable, DebuggerTypeProxy(typeof(MListDebugging<>)), DebuggerDisplay("Count = {Count}")]
    public class MList<T> : Modifiable, IList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged, IMListPrivate<T>
    {
        public bool IsNew
        {
            get { return this.innerList.All(a => a.RowId == null); }
        }

        [Serializable]
        public struct RowIdValue : IEquatable<RowIdValue>, IComparable<RowIdValue>, ISerializable
        {
            public readonly int? RowId;
            public readonly T Value;
            public readonly int? OldIndex; 

            public RowIdValue(T value)
            {
                this.Value = value;
                this.RowId = null;
                this.OldIndex = null;
            }

            public RowIdValue(T value, int rowId, int? oldIndex)
            {
                this.Value = value;
                this.RowId = rowId;
                this.OldIndex = oldIndex;
            }

            public bool Equals(RowIdValue other)
            {
                if (other.Value == null && this.Value == null)
                    return true;

                if (other.Value == null || this.Value == null)
                    return false; 

                return other.Value.Equals(this.Value);
            }

            public int CompareTo(RowIdValue other)
            {
                if (other.Value == null && this.Value == null)
                    return 0;

                if (this.Value == null)
                    return -1;

                if (other.Value == null)
                    return 1;

                return ((IComparable<T>)this.Value).CompareTo(other.Value);
            }

            public override int GetHashCode()
            {
                return Value == null ? 0 : Value.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is RowIdValue && base.Equals((RowIdValue)obj);
            }

            public override string ToString()
            {
                var pre = RowId == null ? "New" : RowId.Value.ToString(); 

                if(this.OldIndex != null)
                    pre += " Ix: " + this.OldIndex;

                return "({0}) {1}".Formato(pre, Value);
            }

            private RowIdValue(SerializationInfo info, StreamingContext ctxt)
            {
                this.RowId = null;
                this.Value = default(T);
                this.OldIndex = null;
                foreach (SerializationEntry item in info)
                {
                    switch (item.Name)
                    {
                        case "rowid": this.RowId = (int?)item.Value; break;
                        case "oldindex": this.OldIndex = (int?)item.Value; break;
                        case "value": this.Value = (T)item.Value; break;
                        default: throw new InvalidOperationException("Unexpected SerializationEntry");
                    }
                }
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("rowid", this.RowId, typeof(int?));
                info.AddValue("oldindex", this.OldIndex, typeof(int?));
                info.AddValue("value", this.Value, typeof(T));
            }
        }

        List<RowIdValue> innerList = new List<RowIdValue>();

        #region Events


        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        [NonSerialized]
        NotifyCollectionChangedEventHandler collectionChanged;
        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add { collectionChanged += value; }
            remove { collectionChanged -= value; }
        }

        public static T[] Empty = new T[0];

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                    OnPropertyChanged("Item[]");
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Reset:
                    OnPropertyChanged("Count");
                    OnPropertyChanged("Item[]");
                    break;
                default:
                    break;
            }

            if (this.collectionChanged != null)
            {
                this.collectionChanged(this, e);
            }

        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
           if(PropertyChanged!= null)
               PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); 
        }

        #endregion

        public MList()
        {
            innerList = new List<RowIdValue>();
        }

        public MList(IEnumerable<T> collection)
        {
            innerList = new List<RowIdValue>(collection.Select(t => new RowIdValue(t)));
        }

        public MList(IEnumerable<RowIdValue> collection)
        {
            innerList = new List<RowIdValue>(collection);
        }

        public MList(int capacity)
        {
            innerList = new List<RowIdValue>(capacity);
        }

        public int Count
        {
            get { return innerList.Count; }
        }

        public T this[int index]
        {
            get { return innerList[index].Value; }
            set
            {
                T old = innerList[index].Value;
                innerList[index] = new RowIdValue(value);
                SetSelfModified();
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old));
            }
        }

        public void Add(T item)
        {
            innerList.Add(new RowIdValue(item)); 
            SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void Add(params T[] items)
        {
            AddRange(items);
        }

        public void Add(IEnumerable<T> collection) //for object initializers
        {
            AddRange(collection);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
                this.innerList.Add(new RowIdValue(item));

            SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, collection, Empty));
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            throw new NotImplementedException("Use ToReadOnly instead");
        }

        public void Sort()
        {
            innerList.Sort();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void Sort<S>(Func<T, S> element)
            where S : IComparable<S>
        {
            innerList.Sort((a, b) => element(a.Value).CompareTo(element(b.Value)));
            if (WrongPosition())
                this.SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void SortDescending<S>(Func<T, S> element)
            where S : IComparable<S>
        {
            innerList.Sort((a, b) => element(b.Value).CompareTo(element(a.Value)));
            if (WrongPosition()) 
                this.SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void Sort(Comparison<T> comparison)
        {
            innerList.Sort((a, b) => comparison(a.Value, b.Value));
            if (WrongPosition()) 
                this.SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void Sort(IComparer<T> comparer)
        {
            innerList.Sort((a, b) => comparer.Compare(a.Value, b.Value));
            if (WrongPosition()) 
                this.SetSelfModified(); 
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public bool ResetRange(IEnumerable<T> newItems)
        {
            var list = newItems.Select(a => new RowIdValue(a)).ToList();

            bool modified = list.Count != this.Count;

            for (int i = 0; i < list.Count; i++)
			{
                if (this.innerList.Contains(list[i]))
                {
                    var index = this.innerList.IndexOf(list[i]);

                    list[i] = this.innerList[index];

                    this.innerList.RemoveAt(index);
                }
                else
                {
                    modified = true;
                }
			}

            var oldList = innerList;

            innerList = list;

            if (modified || oldList.Any() || WrongPosition())
                SetSelfModified();

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, innerList.Except(oldList).ToList(), oldList));
            return modified;
        }

        private bool WrongPosition()
        {
            for (int i = 0; i < innerList.Count; i++)
            {
                if (innerList[i].OldIndex.HasValue && innerList[i].OldIndex != i)
                    return true;
            }
            return false;
        }

        public void Clear()
        {
            if (innerList.Count > 0)
                SetSelfModified();

            var oldItems = innerList.ToList();

            innerList.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, Empty, oldItems)); 
        }

        public void CopyTo(T[] array)
        {   
            for (int i = 0; i < this.Count; i++)
            {
                array[i] = this.innerList[i].Value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in innerList)
            {
                yield return item.Value;
            }
        }

        public void Insert(int index, T item)
        {
            innerList.Insert(index, new RowIdValue(item));
            SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public bool Remove(T item)
        {
            int index = innerList.IndexOf(new RowIdValue(item));
            if (index == -1)
                return false;
         
            innerList.RemoveAt(index);
            SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            return true;
        }

        public int RemoveAll(Predicate<T> match)
        {
            List<T> removed = new List<T>(); 
            for (int i = 0; i < this.innerList.Count; )
            {
                var val = innerList[i].Value;
                if (match(val))
                {
                    innerList.RemoveAt(i);
                    removed.Add(val);
                }
                else
                {
                    i++;
                }
            }

            if (removed.Any())
            {
                SetSelfModified();
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, Empty, removed));
            }

            return removed.Count; 
        }

        public void RemoveAt(int index)
        {
            T item = innerList[index].Value;
            innerList.RemoveAt(index);
            SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        public void RemoveRange(int index, int count)
        {
            for (int i = 0; i < count; i++)
                RemoveAt(index); 
        }

        public int IndexOf(T item)
        {
            return innerList.IndexOf(new RowIdValue(item));
        }

        public int IndexOf(T item, int index)
        {
            return innerList.IndexOf(new RowIdValue(item), index);
        }

        public int IndexOf(T item, int index, int count)
        {
            return innerList.IndexOf(new RowIdValue(item), index, count);
        }

        public int LastIndexOf(T item)
        {
            return innerList.LastIndexOf(new RowIdValue(item));
        }

        public int LastIndexOf(T item, int index)
        {
            return innerList.LastIndexOf(new RowIdValue(item), index); 
        }

        public int LastIndexOf(T item, int index, int count)
        {
            return innerList.LastIndexOf(new RowIdValue(item), index, count);
        }

        public int FindIndex(Predicate<T> match)
        {
            return innerList.FindIndex(a => match(a.Value));
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return innerList.FindIndex(startIndex, a => match(a.Value));
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return innerList.FindIndex(startIndex, count, a => match(a.Value));
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return innerList.FindLastIndex(a => match(a.Value));
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return innerList.FindLastIndex(startIndex, a => match(a.Value));
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            return innerList.FindLastIndex(startIndex, count, a => match(a.Value));
        }

        public bool Contains(T item)
        {
            return innerList.Contains(new RowIdValue(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < this.Count; i++)
            {
                array[i + arrayIndex] = this.innerList[i].Value;
            }
        }

        public void ForEach(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            int count = innerList.Count;
            for (int i = 0; i < count; i++)
            {
                action(this.innerList[i].Value);
            }
        }

        public void ForEach(Action<T, int> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            int count = innerList.Count;
            for (int i = 0; i < count; i++)
            {
                action(this.innerList[i].Value, i);
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return "{0}{{ Count = {1} }}".Formato(GetType().TypeName(), Count);
        }

        #region IList Members

        int IList.Add(object value)
        {
            this.Add((T)value);
            return this.Count; 
        } 

        bool IList.Contains(object value)
        {
            return this.Contains((T)value); 
        }

        int IList.IndexOf(object value)
        {
            return this.IndexOf((T)value); 
        }

        void IList.Insert(int index, object value)
        {
            this.Insert(index, (T)value); 
        }

        bool IList.IsFixedSize
        {
            get { return false;  }
        }

        void IList.Remove(object value)
        {
            this.Remove((T)value); 
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)innerList).CopyTo(array, index); 
        }

        bool ICollection.IsSynchronized
        {
            get { return ((ICollection)innerList).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return ((ICollection)innerList).SyncRoot; }
        }

        #endregion 

        List<RowIdValue> IMListPrivate<T>.InnerList
        {
            get { return this.innerList; }
        }

        void IMListPrivate.InnerListModified(IList newItems, IList oldItems)
        {
            this.SetSelfModified();
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems ?? Empty, oldItems ?? Empty));
        }

        void IMListPrivate.SetRowId(int index, int rowId)
        {
            var prev = this.innerList[index]; 

            if(prev.RowId.HasValue)
                throw new InvalidOperationException("Index {0} already as RowId".Formato(index));

            this.innerList[index] = new RowIdValue(prev.Value, rowId, null);
        }

        void IMListPrivate.SetOldIndex(int index)
        {
            var prev = this.innerList[index];

            this.innerList[index] = new RowIdValue(prev.Value, prev.RowId.Value, index);
        }

        void IMListPrivate.ExecutePostRetrieving()
        {
            this.PostRetrieving(); 
        }

        protected internal override void PostRetrieving()
        {
            if (this.innerList.Any(a => a.OldIndex.HasValue))
                this.innerList.Sort(a => a.OldIndex.Value);
        }
    }

    public interface IMListPrivate
    {
        bool IsNew { get; }

        void ExecutePostRetrieving();
        void SetOldIndex(int index);
        void SetRowId(int index, int rowId);

        void InnerListModified(IList newItems, IList oldItems); 
    }

    public interface IMListPrivate<T>  : IMListPrivate
    {
        List<MList<T>.RowIdValue> InnerList { get; }
    }

    internal sealed class MListDebugging<T>
    {
        private ICollection<T> collection;

        public MListDebugging(ICollection<T> collection)
        {
            this.collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                T[] array = new T[this.collection.Count];
                this.collection.CopyTo(array, 0);
                return array;
            }
        }
    }

    public static class MListExtensions
    {
        public static MList<T> ToMList<T>(this IEnumerable<T> collection)
        {
            return new MList<T>(collection); 
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class PreserveOrderAttribute : Attribute
    {  
        public PreserveOrderAttribute()
        {
        }
    }
}
