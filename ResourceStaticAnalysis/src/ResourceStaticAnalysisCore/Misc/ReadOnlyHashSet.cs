/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ResourceStaticAnalysis.Core.Misc
{
    public interface IReadOnlySet<T> : IEnumerable<T>
    {
        /// <summary>
        /// Number of items in the readonly collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Checks if readonly collection contains the item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        bool Contains(T item);

        /// <summary>
        /// Checks if content of both sets are the same
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        bool SetEquals(IReadOnlySet<T> other);
    }
    /// <summary>
    /// Reduced HashSet functionality to be used as perf efficient IReadOnlyCollection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ReadonlyHashSet<T> : IReadOnlySet<T>
    {
        HashSet<T> _set;
        public ReadonlyHashSet()
        {
            _set = new HashSet<T>();
        }
        public ReadonlyHashSet(IEnumerable<T> collection)
        {
            _set = new HashSet<T>(collection);
        }
        public ReadonlyHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            _set = new HashSet<T>(collection, comparer);
        }
        public ReadonlyHashSet(IEqualityComparer<T> comparer)
        {
            _set = new HashSet<T>(comparer);
        }
       
        #region IReadOnlySet<T> Members
        public bool SetEquals(IReadOnlySet<T> other)
        {
            if (other == null)
            {
                return false;
            }
            return this.Count == other.Count && this.All(item => other.Contains(item));
        }

        public int Count
        {
            get { return _set.Count; }
        }

        public bool Contains(T item)
        {
            return _set.Contains(item);
        }
        #endregion

        #region IEnumerable<T> Members
        public IEnumerator<T> GetEnumerator()
        {
            return _set.GetEnumerator();
        }
        #endregion

        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion
    }
}
