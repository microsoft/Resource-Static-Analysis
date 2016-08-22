/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.ResourceStaticAnalysis.Core.Misc
{
    /// <summary>
    /// A thread-safe reference type cache, mimicing the String.Intern and String.IsInterened behavior.
    /// As opposed to the CLR's string cache, this one can be created and destroyed within the AppDomain
    /// which allows you to control how much it grows. Because of this no assumptions should be made regarding
    /// the persistance of the cache, i.e. no reference comparisons between objects can be guaranteed to work correctly
    /// if some code cleans destroys the cache during the session.
    /// </summary>
    public class ObjectCache<T> where T : class
    {
        private long _cacheHits = 0;
        private long _cacheQueries = 0;
        private long _bytesSaved = 0;
        private readonly Func<T, long> _getSize = obj => 0L;

        /// <summary>
        /// Number of successful retrievals from cache.
        /// </summary>
        public long CacheHits { get { return _cacheHits; } }

        /// <summary>
        /// Number of total "queries" into the cache
        /// </summary>
        public long CacheQueries { get { return _cacheQueries; } }

        /// <summary>
        /// Number of bytes saved by interning the objects of T.
        /// Works only if in the constructor you have provided a delegate to a function that knows how to
        /// measure the size of objects of T. If not provided, this will be always 0;
        /// </summary>
        public long BytesSaved { get { return _bytesSaved; } }


        private Dictionary<T, T> _cache;
        /// <summary>
        /// Used to control access to _cache
        /// </summary>
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        public ObjectCache(int initialSize)
        {
            _cache = new Dictionary<T, T>(initialSize);
        }
        public ObjectCache(int initialSize, IEqualityComparer<T> comparer)
        {
            _cache = new Dictionary<T, T>(initialSize, comparer);
        }
        public ObjectCache(int initialSize, Func<T, long> getSize)
        {
            if (getSize == null)
            {
                throw new ArgumentNullException(nameof(getSize));
            }
            _cache = new Dictionary<T, T>(initialSize);
            this._getSize = getSize;
        }
        public ObjectCache(int initialSize, IEqualityComparer<T> comparer, Func<T, long> getSize)
        {
            if (getSize == null)
            {
                throw new ArgumentNullException(nameof(getSize));
            }
            _cache = new Dictionary<T, T>(initialSize, comparer);
            this._getSize = getSize;
        }
        /// <summary>
        /// Works the same as String.Intern. Thread safe.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public T Intern(T o)
        {
            if (Object.ReferenceEquals(o, null))
            {
                return o;
            }
            bool cacheHit;
            T ret;

            Interlocked.Increment(ref _cacheQueries);
            _cacheLock.EnterUpgradeableReadLock();
            try
            {
                cacheHit = _cache.TryGetValue(o, out ret);
                if (!cacheHit)
                {
                    _cacheLock.EnterWriteLock();
                    try
                    {
                        _cache.Add(o, o);
                        ret = o;
                    }
                    finally
                    {
                        _cacheLock.ExitWriteLock();
                    }
                }
                else
                {
                    RecordSuccessfulHit(o, ret);
                }
                return ret;
            }
            finally
            {
                _cacheLock.ExitUpgradeableReadLock();
            }
            
        }

        private void RecordSuccessfulHit(T original, T cached)
        {
            if (Object.ReferenceEquals(original, cached))
            {
                Interlocked.Increment(ref _cacheHits);
                Interlocked.Add(ref _bytesSaved, _getSize(cached));
            }
        }
        /// <summary>
        /// Works the same as String.Intern but takes para by ref. Thread safe.
        /// </summary>
        /// <param name="o"></param>
        public void Intern(ref T o)
        {
            o = Intern(o);
        }
        /// <summary>
        /// Use this if you want to intern a bunch of strings at the same time.
        /// This method will reduce the number of times thread lock needs to be aquired.
        /// </summary>
        /// <param name="inArray">The items in the array will be replaced references to cached objects, or left untouched if cache misses.</param>
        public void Intern(T[] inArray)
        {
            Interlocked.Add(ref _cacheQueries, inArray.Length);
            T cached;

            for (int j = 0; j < inArray.Length; j++)
            {
                if (!Object.ReferenceEquals(inArray[j], null))
                {
                    _cacheLock.EnterUpgradeableReadLock();
                    try
                    {
                        if (_cache.TryGetValue(inArray[j], out cached))
                        {
                            inArray[j] = cached;
                            RecordSuccessfulHit(inArray[j], cached);
                        }
                        else
                        {
                            _cacheLock.EnterWriteLock();
                            try
                            {
                                _cache.Add(inArray[j], inArray[j]);
                            }
                            finally
                            {
                                _cacheLock.ExitWriteLock();
                            }
                        }
                    }
                    finally
                    {
                        _cacheLock.ExitUpgradeableReadLock();
                    }
                }
            }
        }

        /// <summary>
        /// Works the same as String.Interned
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public T IsInterned(T o)
        {
            T ret;
            if (Object.ReferenceEquals(o, null))
            {
                return null;
            }
            _cacheLock.EnterReadLock();
            try
            {
                _cache.TryGetValue(o, out ret);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
            return ret;
        }
        /// <summary>
        /// Clears the content of the cache. Thread safe.
        /// </summary>
        public void Clear()
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _cache.Clear();
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        /// <summary>
        /// Resizes the internal dictionary and copies the existing content
        /// </summary>
        public void Resize(int capacity)
        {
            _cacheLock.EnterUpgradeableReadLock();
            try
            {
                var newCache = new Dictionary<T, T>(capacity, _cache.Comparer);
                foreach (var pair in _cache)
                {
                    newCache.Add(pair.Key, pair.Value);
                }

                _cacheLock.EnterWriteLock();
                try
                {
                    _cache = newCache;
                }
                finally
                {
                    _cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                _cacheLock.ExitUpgradeableReadLock();
            }
        }
    }
}
