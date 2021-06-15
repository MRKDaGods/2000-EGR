using System;
using System.Collections;
using System.Collections.Generic;

namespace MRK {
    public class CircularBuffer<T> : IEnumerable<T> {
		T[] m_Buffer;
		int m_Head;
		int m_Tail;

		public int Count { get; private set; }

		public CircularBuffer(int capacity) {
			m_Buffer = new T[capacity];
			m_Head = 0;
		}

		public void Add(T item) {
			m_Head = (m_Head + 1) % m_Buffer.Length;
			m_Buffer[m_Head] = item;
			if (Count == m_Buffer.Length) {
				m_Tail = (m_Tail + 1) % m_Buffer.Length;
			}
			else {
				Count++;
			}
		}

		public T this[int index] {
			get {
				if (index < 0 || index >= m_Buffer.Length) { 
					throw new ArgumentOutOfRangeException("index: " + index.ToString()); 
				}

				return m_Buffer[mod((m_Head - index), m_Buffer.Length)];
			}
		}


		int mod(int x, int m) {
			return (x % m + m) % m;
		}

		public IEnumerator<T> GetEnumerator() {
			if (Count == 0 || m_Buffer.Length == 0) {
				yield break;
			}

			for (int i = 0; i < Count; i++) {
				yield return this[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public IEnumerable<T> GetEnumerable() {
			IEnumerator<T> enumerator = GetEnumerator();
			while (enumerator.MoveNext()) {
				yield return enumerator.Current;
			}
		}
	}
}
