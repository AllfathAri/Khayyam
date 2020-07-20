/*
 * Copyright (C) 2020 Arian Dashti.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Khayyam.Util
{
	/// <summary>
	/// A map of objects whose mapping entries are sequenced based on the order in which they were
	/// added. This data structure has fast <c>O(1)</c> search time, deletion time, and insertion time
	/// </summary>
	/// <remarks>
	/// This class is not thread safe.
	/// This class is not a really replication of JDK LinkedHashMap{K, V}, 
	/// this class is an adaptation of SequencedHashMap with generics.
	/// </remarks>
	[Serializable]
	public class LinkedHashMap<TKey, TValue> : IDictionary<TKey, TValue>, IDeserializationCallback
	{
		[Serializable]
		protected class Entry
		{
			private readonly TKey _key;
			private TValue _evalue;
			private Entry _next;
			private Entry _prev;

			public Entry(TKey key, TValue value)
			{
				_key = key;
				_evalue = value;
			}

			public TKey Key => _key;

			public TValue Value
			{
				get => _evalue;
				set => _evalue = value;
			}

			public Entry Next
			{
				get => _next;
				set => _next = value;
			}

			public Entry Prev
			{
				get => _prev;
				set => _prev = value;
			}

			#region System.Object Members

			[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
			public override int GetHashCode()
			{
				return (_key == null ? 0 : _key.GetHashCode()) ^ (_evalue == null ? 0 : _evalue.GetHashCode());
			}

			public override bool Equals(object obj)
			{
				if (!(obj is Entry other)) return false;
				if (other == this) return true;

				return (_key == null ? other.Key == null : _key.Equals(other.Key)) &&
				       (_evalue == null ? other.Value == null : _evalue.Equals(other.Value));
			}

			public override string ToString()
			{
				return "[" + _key + "=" + _evalue + "]";
			}

			#endregion
		}

		private readonly Entry _header;
		private readonly Dictionary<TKey, Entry> _entries;
		private long _version;

		/// <summary>
		/// Initializes a new instance of the <see cref="LinkedHashMap{K,V}"/> class that is empty, 
		/// has the default initial capacity, and uses the default equality comparer for the key type.
		/// </summary>
		public LinkedHashMap()
			: this(0, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinkedHashMap{K,V}"/> class that is empty, 
		/// has the specified initial capacity, and uses the default equality comparer for the key type.
		/// </summary>
		/// <param name="capacity">The initial number of elements that the <see cref="LinkedHashMap{K,V}"/> can contain.</param>
		public LinkedHashMap(int capacity)
			: this(capacity, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinkedHashMap{K,V}"/> class that is empty, has the default initial capacity, and uses the specified <see cref="IEqualityComparer{K}"/>.
		/// </summary>
		/// <param name="equalityComparer">The <see cref="IEqualityComparer{K}"/> implementation to use when comparing keys, or null to use the default EqualityComparer for the type of the key.</param>
		public LinkedHashMap(IEqualityComparer<TKey> equalityComparer)
			: this(0, equalityComparer)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinkedHashMap{K,V}"/> class that is empty, has the specified initial capacity, and uses the specified <see cref="IEqualityComparer{K}"/>.
		/// </summary>
		/// <param name="capacity">The initial number of elements that the <see cref="LinkedHashMap{K,V}"/> can contain.</param>
		/// <param name="equalityComparer">The <see cref="IEqualityComparer{K}"/> implementation to use when comparing keys, or null to use the default EqualityComparer for the type of the key.</param>
		public LinkedHashMap(int capacity, IEqualityComparer<TKey> equalityComparer)
		{
			_header = CreateSentinel();
			_entries = new Dictionary<TKey, Entry>(capacity, equalityComparer);
		}

		#region IDictionary<TKey,TValue> Members

		public virtual bool ContainsKey(TKey key)
		{
			return _entries.ContainsKey(key);
		}

		public virtual void Add(TKey key, TValue value)
		{
			var e = new Entry(key, value);
			_entries.Add(key, e);
			_version++;
			InsertEntry(e);
		}

		public virtual bool Remove(TKey key)
		{
			return RemoveImpl(key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			var result = _entries.TryGetValue(key, out var entry);
			value = result ? entry.Value : default;

			return result;
		}

		public TValue this[TKey key]
		{
			get => _entries[key].Value;
			set
			{
				if (_entries.TryGetValue(key, out var e))
					OverrideEntry(e, value);
				else
					Add(key, value);
			}
		}

		private void OverrideEntry(Entry e, TValue value)
		{
			_version++;
			RemoveEntry(e);
			e.Value = value;
			InsertEntry(e);
		}

		public virtual ICollection<TKey> Keys => new KeyCollection(this);

		public virtual ICollection<TValue> Values => new ValuesCollection(this);

		#endregion

		#region ICollection<KeyValuePair<TKey,TValue>> Members

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public virtual void Clear()
		{
			_version++;

			_entries.Clear();

			_header.Next = _header;
			_header.Prev = _header;
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return Contains(item.Key);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			foreach (KeyValuePair<TKey, TValue> pair in this)
				array.SetValue(pair, arrayIndex++);
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			return Remove(item.Key);
		}

		public virtual int Count => _entries.Count;

		public virtual bool IsReadOnly => false;

		#endregion

		#region IEnumerable Members

		public virtual IEnumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		#endregion

		#region IEnumerable<KeyValuePair<TKey,TValue>> Members

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return new Enumerator(this);
		}

		#endregion

		#region LinkedHashMap Members

		// ReSharper disable once PossibleUnintendedReferenceComparison
		private bool IsEmpty => _header.Next == _header;

		public virtual bool IsFixedSize => false;

		public virtual TKey FirstKey => (First == null) ? default : First.Key;

		public virtual TValue FirstValue => (First == null) ? default : First.Value;

		public virtual TKey LastKey => (Last == null) ? default : Last.Key;

		public virtual TValue LastValue => (Last == null) ? default : Last.Value;

		public virtual bool Contains(TKey key)
		{
			return ContainsKey(key);
		}

		[SuppressMessage("ReSharper", "PossibleUnintendedReferenceComparison")]
		public virtual bool ContainsValue(TValue value)
		{
			if (value == null)
			{
				for (var entry = _header.Next; entry != _header; entry = entry.Next)
				{
					if (entry.Value == null) return true;
				}
			}
			else
			{
				for (var entry = _header.Next; entry != _header; entry = entry.Next)
				{
					if (value.Equals(entry.Value)) return true;
				}
			}
			return false;
		}

		#endregion

		private static Entry CreateSentinel()
		{
			var s = new Entry(default, default);
			s.Prev = s;
			s.Next = s;
			return s;
		}

		private static void RemoveEntry(Entry entry)
		{
			entry.Next.Prev = entry.Prev;
			entry.Prev.Next = entry.Next;
		}

		private void InsertEntry(Entry entry)
		{
			entry.Next = _header;
			entry.Prev = _header.Prev;
			_header.Prev.Next = entry;
			_header.Prev = entry;
		}

		private Entry First => (IsEmpty) ? null : _header.Next;

		private Entry Last => (IsEmpty) ? null : _header.Prev;

		private bool RemoveImpl(TKey key)
		{
			if (!_entries.Remove(key, out var e)) 
				return false;

			_version++;
			RemoveEntry(e);
			return true;
		}

		void IDeserializationCallback.OnDeserialization(object sender)
		{
			((IDeserializationCallback)_entries).OnDeserialization(sender);
		}

		#region System.Object Members

		[SuppressMessage("ReSharper", "PossibleUnintendedReferenceComparison")]
		public override string ToString()
		{
			var buf = new StringBuilder();
			buf.Append('[');
			for (var pos = _header.Next; pos != _header; pos = pos.Next)
			{
				buf.Append(pos.Key);
				buf.Append('=');
				buf.Append(pos.Value);
				if (pos.Next != _header)
				{
					buf.Append(',');
				}
			}
			buf.Append(']');

			return buf.ToString();
		}

		#endregion

		private class KeyCollection : ICollection<TKey>
		{
			private readonly LinkedHashMap<TKey, TValue> _dictionary;

			public KeyCollection(LinkedHashMap<TKey, TValue> dictionary)
			{
				_dictionary = dictionary;
			}

			#region ICollection<TKey> Members

			void ICollection<TKey>.Add(TKey item)
			{
				throw new NotSupportedException("LinkedHashMap KeyCollection is readonly.");
			}

			void ICollection<TKey>.Clear()
			{
				throw new NotSupportedException("LinkedHashMap KeyCollection is readonly.");
			}

			bool ICollection<TKey>.Contains(TKey item)
			{
				return this.Contains(item);
			}

			public void CopyTo(TKey[] array, int arrayIndex)
			{
				foreach (var key in this)
					array.SetValue(key, arrayIndex++);
			}

			bool ICollection<TKey>.Remove(TKey item)
			{
				throw new NotSupportedException("LinkedHashMap KeyCollection is readonly.");
			}

			public int Count => _dictionary.Count;

			bool ICollection<TKey>.IsReadOnly => true;

			#endregion

			#region IEnumerable<TKey> Members

			IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
			{
				return new Enumerator(_dictionary);
			}

			#endregion

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<TKey>)this).GetEnumerator();
			}

			#endregion

			// ReSharper disable once MemberHidesStaticFromOuterClass
			private class Enumerator : ForwardEnumerator<TKey>
			{
				public Enumerator(LinkedHashMap<TKey, TValue> dictionary) : base(dictionary) { }

				public override TKey Current
				{
					get
					{
						if (Dictionary._version != Version)
							throw new InvalidOperationException("Enumerator was modified");

						return Current_.Key;
					}
				}
			}
		}

		private class ValuesCollection : ICollection<TValue>
		{
			private readonly LinkedHashMap<TKey, TValue> _dictionary;

			public ValuesCollection(LinkedHashMap<TKey, TValue> dictionary)
			{
				_dictionary = dictionary;
			}

			#region ICollection<TValue> Members

			void ICollection<TValue>.Add(TValue item)
			{
				throw new NotSupportedException("LinkedHashMap ValuesCollection is readonly.");
			}

			void ICollection<TValue>.Clear()
			{
				throw new NotSupportedException("LinkedHashMap ValuesCollection is readonly.");
			}

			bool ICollection<TValue>.Contains(TValue item)
			{
				foreach (var value in this)
				{
					if (value.Equals(item))
						return true;
				}
				return false;
			}

			public void CopyTo(TValue[] array, int arrayIndex)
			{
				foreach (var value in this)
					array.SetValue(value, arrayIndex++);
			}

			bool ICollection<TValue>.Remove(TValue item)
			{
				throw new NotSupportedException("LinkedHashMap ValuesCollection is readonly.");
			}

			public int Count => _dictionary.Count;

			bool ICollection<TValue>.IsReadOnly => true;

			#endregion

			#region IEnumerable<TKey> Members

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
			{
				return new Enumerator(_dictionary);
			}

			#endregion

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<TValue>)this).GetEnumerator();
			}

			#endregion

			// ReSharper disable once MemberHidesStaticFromOuterClass
			private class Enumerator : ForwardEnumerator<TValue>
			{
				public Enumerator(LinkedHashMap<TKey, TValue> dictionary) : base(dictionary) { }

				public override TValue Current
				{
					get
					{
						if (Dictionary._version != Version)
							throw new InvalidOperationException("Enumerator was modified");

						return Current_.Value;
					}
				}
			}
		}

		private abstract class ForwardEnumerator<T> : IEnumerator<T>
		{
			protected readonly LinkedHashMap<TKey, TValue> Dictionary;
			protected Entry Current_;
			protected readonly long Version;

			protected ForwardEnumerator(LinkedHashMap<TKey, TValue> dictionary)
			{
				Dictionary = dictionary;
				Version = dictionary._version;
				Current_ = dictionary._header;
			}

			#region IDisposable Members

			public void Dispose()
			{
			}

			#endregion

			#region IEnumerator Members

			[SuppressMessage("ReSharper", "PossibleUnintendedReferenceComparison")]
			public bool MoveNext()
			{
				if (Dictionary._version != Version)
					throw new InvalidOperationException("Enumerator was modified");

				if (Current_.Next == Dictionary._header)
					return false;

				Current_ = Current_.Next;

				return true;
			}

			public void Reset()
			{
				Current_ = Dictionary._header;
			}

			object IEnumerator.Current => ((IEnumerator<T>)this).Current;

			#region IEnumerator<T> Members

			public abstract T Current { get; }

			#endregion

			#endregion
		}

		private class Enumerator : ForwardEnumerator<KeyValuePair<TKey, TValue>>
		{
			public Enumerator(LinkedHashMap<TKey, TValue> dictionary) : base(dictionary) { }

			public override KeyValuePair<TKey, TValue> Current
			{
				get
				{
					if (Dictionary._version != Version)
						throw new InvalidOperationException("Enumerator was modified");

					return new KeyValuePair<TKey, TValue>(Current_.Key, Current_.Value);
				}
			}
		}

		protected abstract class BackwardEnumerator<T> : IEnumerator<T>
		{
			private readonly LinkedHashMap<TKey, TValue> _dictionary;
			private Entry _current;
			private readonly long _version;

			public BackwardEnumerator(LinkedHashMap<TKey, TValue> dictionary)
			{
				_dictionary = dictionary;
				_version = dictionary._version;
				_current = dictionary._header;
			}

			#region IDisposable Members

			public void Dispose()
			{
			}

			#endregion

			#region IEnumerator Members

			[SuppressMessage("ReSharper", "PossibleUnintendedReferenceComparison")]
			public bool MoveNext()
			{
				if (_dictionary._version != _version)
					throw new InvalidOperationException("Enumerator was modified");

				if (_current.Prev == _dictionary._header)
					return false;

				_current = _current.Prev;

				return true;
			}

			public void Reset()
			{
				_current = _dictionary._header;
			}

			object IEnumerator.Current => ((IEnumerator<T>)this).Current;

			#region IEnumerator<T> Members

			public abstract T Current { get; }

			#endregion

			#endregion
		}
	}
}