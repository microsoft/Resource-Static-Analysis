/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.Core.Engine;

namespace Microsoft.ResourceStaticAnalysis.Core.Misc
{
    /// <summary>
    /// Provides collection of ClassificationObjects with hash table access.
    /// Also enables some set operators like Intersect, Exclude, etc.
    /// </summary>
    /// <typeparam name="T">ClassificationObject or derrived class</typeparam>
    public class COHashTable<T> where T : ClassificationObject
    {
        private Dictionary<uint, T> coDictionary;

        public COHashTable()
        {
            coDictionary = new Dictionary<uint, T>();
        }

        public COHashTable(int size)
        {
            coDictionary = new Dictionary<uint, T>(size);
        }

        public COHashTable(IEnumerable<T> items)
        {
            coDictionary = new Dictionary<uint, T>(items.Count());
            foreach (var item in items)
            {
                coDictionary.Add(item.Key, item);
            }
        }

        private COHashTable(Dictionary<uint, T> dt)
        {
            coDictionary = new Dictionary<uint, T>(dt);
        }

        /// <summary>
        /// Checks if object already exists and skips it.
        /// </summary>
        /// <param name="co"></param>
        public void Add(T co)
        {
            if (!coDictionary.ContainsKey(co.Key))
                coDictionary.Add(co.Key, co);
        }

        /// <summary>
        /// Returns objects contained in the hash
        /// </summary>
        public ICollection<T> Objects
        {
            get { return coDictionary.Values; }
        }

        /// <summary>
        /// Number of COs stored
        /// </summary>
        public int Count
        {
            get { return this.coDictionary.Count; }
        }

        /// <summary>
        /// True if table contains the object.
        /// </summary>
        public bool Contains(T co)
        {
            if (this.coDictionary.ContainsKey(co.Key))
                return true;
            else
                return false;
        }
        /// <summary>
        /// Creates a shallow copy of the object.
        /// </summary>
        /// <returns></returns>
        public COHashTable<T> Clone()
        {
            return new COHashTable<T>(this.coDictionary);
        }

        /// <summary>
        /// Intersects two COHashTables and produces a collection.
        /// </summary>
        /// <param name="otherTable">Hashtable to intersect with</param>
        /// <returns>Returns empty if no common object exist.</returns>
        public ICollection<T> Intersect(COHashTable<T> otherTable)
        {
            COHashTable<T> smaller, bigger;

            if (this.Count > otherTable.Count)
            {
                smaller = otherTable;
                bigger = this;
            }
            else
            {
                smaller = this;
                bigger = otherTable;
            }
            return (
             from co in smaller.Objects
             where bigger.Contains(co)
             select co
             ).ToArray();
        }

        /// <summary>
        /// Intersects two COHashTables and produces a COHashTable.
        /// </summary>
        /// <param name="otherTable">Hashtable to intersect with</param>
        /// <returns>Returns empty if no common object exist.</returns>
        public COHashTable<T> IntersectToHashTable(COHashTable<T> otherTable)
        {
            COHashTable<T> smaller, bigger;

            if (this.Count > otherTable.Count)
            {
                smaller = otherTable;
                bigger = this;
            }
            else
            {
                smaller = this;
                bigger = otherTable;
            }
            var ret = new COHashTable<T>(smaller.Count);

            var intersection =
                 from co in smaller.Objects
                 where bigger.Contains(co)
                 select co;

            foreach (var co in intersection)
            {
                ret.Add(co);
            }
            return ret;
        }

        /// <summary>
        /// Works faster because it does not create a new COHashTable.
        /// To be used when you don't need to run heavy set operations on the return set.
        /// </summary>
        /// <param name="otherTable"></param>
        /// <returns></returns>
        public ICollection<T> Intersect(ICollection<T> otherTable)
        {
            return
                (from co in otherTable
                where Contains(co)
                select co).ToArray();
        }

        /// <summary>
        /// Creates intersection by removing from this table. Fast, because modifies the existing table.
        /// </summary>
        /// <param name="otherTable"></param>
        public void IntersectAndRemove(COHashTable<T> otherTable)
        {
            var toBeRemoved = Except(otherTable);
                
            foreach (var co in toBeRemoved)
                coDictionary.Remove(co.Key);
        }

        /// <summary>
        /// Excludes elements of otherTable from this table. Actually faster than removing from existing hashtable
        /// </summary>
        /// <param name="otherTable">Table with elements to remove from this table.</param>
        /// <returns>Objects contained in this table minus the ones in otherTable</returns>
        public ICollection<T> Except(COHashTable<T> otherTable)
        {
            return
                (from co in this.Objects
                where !otherTable.Contains(co)
                select co).ToArray();
        }

        /// <summary>
        /// Excludes elements from this table by removing them. Actually slower than adding to a new list.
        /// </summary>
        /// <param name="otherTable"></param>
        public void ExceptAndRemove(COHashTable<T> otherTable)
        {
            var toBeRemoved = Intersect(otherTable);
            foreach (var co in toBeRemoved)
                coDictionary.Remove(co.Key);
        }

        /// <summary>
        /// Union of two or more COHashtables
        /// </summary>
        /// <param name="otherTables"></param>
        /// <returns></returns>
        public COHashTable<T> Union(COHashTable<T>[] otherTables)
        {
            int maxSize = this.Count;
            if (Object.ReferenceEquals(otherTables, null))
            {
                throw new ArgumentNullException("otherTables");
            }
            else
            {
                maxSize += otherTables.Sum(t => t.Count);
            }
            COHashTable<T> ret = this.Clone();
            
            foreach (COHashTable<T> table in otherTables)
            {
                foreach (T co in table.Objects)
                {
                    ret.Add(co);
                }
            }
            return ret;
        }

        /// <summary>
        /// Removes a CO from the table.
        /// </summary>
        /// <param name="co"></param>
        public void Remove(T co)
        {
            this.coDictionary.Remove(co.Key);
        }
        internal static int COHashTableCountComparer(COHashTable<T> x, COHashTable<T> y)
        {
            return x.Count.CompareTo(y.Count);
        }
        /// <summary>
        /// Removes content of hash table.
        /// </summary>
        public void Clear()
        {
            coDictionary.Clear();
        }

        public static readonly COHashTable<T> Empty = new COHashTable<T>();
    }
}
