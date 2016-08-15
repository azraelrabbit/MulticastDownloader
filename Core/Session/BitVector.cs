// <copyright file="BitVector.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Represent a file transfer bitmap.
    /// </summary>
    public class BitVector : IList<bool>
    {
        internal BitVector(long length)
        {
            this.LongCount = length;
            long arrLen = length >> 3;
            if ((length & 0xF) != 0)
            {
                ++arrLen;
            }

            this.RawBits = new byte[arrLen];
        }

        internal BitVector(long length, byte[] bits)
        {
            this.LongCount = length;
            this.RawBits = bits;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public int Count
        {
            get
            {
                return (int)this.LongCount;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the raw bits.
        /// </summary>
        /// <value>
        /// The raw bits.
        /// </value>
        public byte[] RawBits
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public long LongCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the <see cref="bool"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="bool"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>A boolean for the specified index.</returns>
        public bool this[int index]
        {
            get
            {
                return this[(long)index];
            }

            set
            {
                this[(long)index] = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="bool"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="bool"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>A boolean for the specified index.</returns>
        public bool this[long index]
        {
            get
            {
                long idx = index >> 3;
                int offset = (int)(index % 8);
                return (this.RawBits[idx] & (1 << offset)) != 0;
            }

            set
            {
                long idx = index >> 3;
                int offset = (int)(index % 8);
                if (value)
                {
                    this.RawBits[idx] |= (byte)(1 << offset);
                }
                else
                {
                    this.RawBits[idx] &= (byte)(~(1 << offset));
                }
            }
        }

        /// <summary>
        /// Computes the union of the collection of bit vectors in the parameter list.
        /// </summary>
        /// <param name="vectors">The vectors to union.</param>
        /// <returns>A <see cref="BitVector"/> containing the union of all sub-vector values.</returns>
        public static BitVector IntersectOf(ICollection<BitVector> vectors)
        {
            if (vectors == null)
            {
                throw new ArgumentNullException("vectors");
            }

            BitVector first = vectors.FirstOrDefault();
            if (first == null)
            {
                return new BitVector(0);
            }

            BitVector ret = new BitVector(first.LongCount);
            for (long i = 0; i < ret.LongCount; ++i)
            {
                int count = 0;
                foreach (BitVector bv in vectors)
                {
                    if (bv[i])
                    {
                        ++count;
                    }
                }

                if (count == vectors.Count)
                {
                    ret[i] = true;
                }
            }

            return ret;
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <remarks>This method is not supported.</remarks>
        public void Add(bool item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < this.RawBits.Length; ++i)
            {
                this.RawBits[i] = 0;
            }
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        /// true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.
        /// </returns>
        public bool Contains(bool item)
        {
            long longCount = this.RawBits.LongCount();
            int remainder = 8 - (int)(this.LongCount % 8);
            int uMask = 0;
            for (long i = 0; i < longCount; ++i)
            {
                int valid = 0xFF;
                if (i == longCount - 1)
                {
                    valid = 0xFF >> remainder;
                }

                if (item)
                {
                    uMask |= this.RawBits[i] & valid;
                }
                else
                {
                    uMask |= ~this.RawBits[i] & valid;
                }
            }

            return uMask > 0;
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        public void CopyTo(bool[] array, int arrayIndex)
        {
            for (int i = 0; i < this.Count; ++i)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<bool> GetEnumerator()
        {
            for (int i = 0; i < this.LongCount; ++i)
            {
                yield return this[i];
            }
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <returns>
        /// The index of <paramref name="item" /> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(bool item)
        {
            for (int i = 0; i < this.RawBits.Length; ++i)
            {
                if (this.RawBits[i] > 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <remarks>This method is not supported.</remarks>
        public void Insert(int index, bool item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        /// true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </returns>
        /// <remarks>This method is not supported.</remarks>
        public bool Remove(bool item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <remarks>This method is not supported.</remarks>
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
