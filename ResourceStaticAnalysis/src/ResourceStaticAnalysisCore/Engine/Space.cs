/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.ResourceStaticAnalysis.Core.Engine.Properties;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine
{
    /// <summary>
    /// Multidimensional space of classification objects. Used by ResourceStaticAnalysis to 'move' between objects of this space.
    /// Objects in such space can be thought of as points in non-Euclidean space.
    /// </summary>
    public class Space
    {
        /// <summary>
        /// Holds references to all COs belonging to the space
        /// </summary>
        private readonly COHashTable<ClassificationObject> _coHashTable = new COHashTable<ClassificationObject>();

        /// <summary>
        /// Holds all dimensions created so far and manages thread-safe access.
        /// </summary>
        private readonly ConcurrentDictionary<byte, Dimension<ClassificationObject>> _dimensions = new ConcurrentDictionary<byte, Dimension<ClassificationObject>>();
        
        /// <summary>
        /// Used to control access to _coHashTable 
        /// </summary>
        private static readonly ReaderWriterLockSlim CoHashTableLock = new ReaderWriterLockSlim();
        
        /// <summary>
        /// Used to control access to _dimensions in the delegate
        /// </summary>
        private static readonly ReaderWriterLockSlim DimensionsLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Creates a new instance of space for objects of the specified type.
        /// </summary>
        public Space()
        {
        }
        
        /// <summary>
        /// Used to generate unique Keys for Classification Objects assigned to this space.
        /// </summary>
        private uint coAutoNumber;
        
        /// <summary>
        /// Generates a unique Key for CO in this space.
        /// </summary>
        /// <returns></returns>
        internal uint GenerateUniqueCOKey()
        {
            return coAutoNumber++;
        }
        
        /// <summary>
        /// Interface that flushes all elements of this space so that it is empty and ready for reuse.
        /// </summary>
        public void Reset()
        {
            CoHashTableLock.EnterWriteLock();
            try
            {
                _coHashTable.Clear();
            }
            finally
            {
                CoHashTableLock.ExitWriteLock();
            }
            
            DimensionsLock.EnterWriteLock();
            try
            {
                _dimensions.Clear();
            }
            finally
            {
                DimensionsLock.ExitWriteLock();
            }
        }

        private void AddObject(ClassificationObject co)
        {
            co.Key = GenerateUniqueCOKey();
            co.ObjectSpace = this;
            _coHashTable.Add(co);
        }

        internal void AddObjectSafe(ClassificationObject co)
        {
            CoHashTableLock.EnterWriteLock();
            try
            {
                AddObject(co);
            }
            finally
            {
                CoHashTableLock.ExitWriteLock();
            }
        }

        internal void AddObjectsSafe(IEnumerable<ClassificationObject> coCollection)
        {
            CoHashTableLock.EnterWriteLock();
            try
            {
                foreach (ClassificationObject co in coCollection)
                {
                    AddObject(co);
                }
            }
            finally
            {
                CoHashTableLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Get an existing dimension or build a new one if that dimension does not yet exist.
        /// </summary>
        /// <param name="iD">ID of the dimension to get</param>
        /// <returns></returns>
        public Dimension<ClassificationObject> GetDimension(byte iD)
        {
            return _dimensions.GetOrAdd(iD,
                (iDToAdd) =>
                {
                    DimensionsLock.EnterWriteLock();
                    CoHashTableLock.EnterReadLock();
                    try
                    {
                        DateTime startTime = DateTime.Now;
                        // resolve dimension name using a CO
                        string dimName = iDToAdd.ToString();
                        if (this._coHashTable.Count > 0)
                        {
                            dimName = this._coHashTable.Objects.First().PropertyEnumManager.GetNameFromId(iDToAdd);
                        }
                        Trace.TraceInformation("Creating new dimension in space {0}. Dim name: {1}, objects being added: {2}", this.GetType().Name, dimName, this._coHashTable.Count);
                        // debug
                        var ret = new Dimension<ClassificationObject>(iDToAdd, this._coHashTable.Objects);
                        Trace.TraceInformation("Dimension created. Took: {0}. Keys added: {1}",
                            DateTime.Now.Subtract(startTime),
                            ret.Count
                            );
                        return ret;
                    }
                    finally
                    {
                        DimensionsLock.ExitWriteLock();
                        CoHashTableLock.ExitReadLock();
                    }});
        }

        /// <summary>
        /// Get all objects from dimension propertyName at coordinate propertyValue
        /// </summary>
        /// <param name="property">A Property object used to search for COs</param>
        /// <returns>A list of COs with the specified value of the property.</returns>
        public COHashTable<ClassificationObject> GetObjectsAt(Property property)
        {
            if (Object.ReferenceEquals(property, null))
                return COHashTable<ClassificationObject>.Empty;
            return this.GetDimension(property.Id).GetObjectsAt(property);
        }

        /// <summary>
        /// Gets an intersection of multiple dimensions at coordinates specified by 
        /// properties listed in params.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public COHashTable<ClassificationObject> GetDimensionIntersection(ICollection<Property> properties)
        {
            if (properties == null || properties.Count == 0)
                return COHashTable<ClassificationObject>.Empty;
            if (properties.Count == 1)
            {
                return GetObjectsAt(properties.First());
            }
            // the order of properties does not matter for the result
            // sorting them will make sure that different calls to the method, with a different sequence of properties, 
            // will result in a cache hit.
            var sortedProperties = properties.OrderBy(p => p.Id).ToArray();

            COHashTable<ClassificationObject> ret;

            ///Collect all sets of matching objects
            var resultSets = new COHashTable<ClassificationObject>[(sortedProperties.Length)];
            int i = 0;
            foreach (Property prop in sortedProperties)
            {
                resultSets[i++] = this.GetObjectsAt(prop);
            }
            ///Sort sets from smallest to largest. This significantly improves performance of intersect in most cases.
            Array.Sort<COHashTable<ClassificationObject>>(resultSets, COHashTable<ClassificationObject>.COHashTableCountComparer);

            ///create return set
            ret = resultSets[0].Clone();

            ///intersect all sets with the initial return set
            i = 1;
            while (i < resultSets.Length && ret.Count > 0)
            {
                ret.IntersectAndRemove(resultSets[i++]);
            }
            return ret;
        }

        private static readonly ClassificationObject[] EmptyTArray = new ClassificationObject[0];
        /// <summary>
        /// Get objects from x+/-1 point of the specified dimension.
        /// </summary>
        /// <param name="x">Value of property dimensionName from which to move next()/previous()</param>
        /// <param name="number"></param>
        /// <returns></returns>
        internal ICollection<ClassificationObject> NextPrevious(Property x, int number)
        {
            if (number == 0)
                return EmptyTArray;

            Dimension<ClassificationObject> dim = this.GetDimension(x.Id);
            // the index of the value to obtain from dimension
            // starting value is the index of x
            LinkedListNode<Property> yIndex = dim.GetPropertyIndex(x); ;
            if (number > 0)
            {
                do
                {
                    yIndex = yIndex.Next;
                    --number;

                } while (!Object.ReferenceEquals(yIndex, null) && number != 0);
            }
            else
            {
                do
                {
                    yIndex = yIndex.Previous;
                    ++number;

                } while (!Object.ReferenceEquals(yIndex, null) && number != 0);
            }
            if (Object.ReferenceEquals(yIndex, null))
                return EmptyTArray;
            return dim.GetObjectsAt(yIndex.Value).Objects;
        }

        /// <summary>
        /// Gets all objects from all points in range (mid-before, mid+after) of the specified dimension.
        /// This excludes objects belonging to 'mid'
        /// </summary>
        /// <param name="mid">Point of dimension to be surrounded by the range</param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        internal ICollection<ClassificationObject> Range(Property mid, int before, int after)
        {
            Dimension<ClassificationObject> dim = this.GetDimension(mid.Id);
            LinkedListNode<Property> midIndex = dim.GetPropertyIndex(mid);
            LinkedListNode<Property> pointer = midIndex.Previous;
            var pointsBefore = new Property[before];
            while (!Object.ReferenceEquals(pointer, null) && before > 0)
            {
                pointsBefore[--before] = pointer.Value;
                pointer = pointer.Previous;
            }
            pointer = midIndex.Next;
            var pointsAfter = new Property[after];
            int afterIndex = 0;
            while (!Object.ReferenceEquals(pointer, null) && after - afterIndex > 0)
            {
                pointsAfter[afterIndex++] = pointer.Value;
                pointer = pointer.Next;
            }
            var merged = pointsBefore.Skip(before).Union(pointsAfter.Take(afterIndex));
            var firstProperty = merged.FirstOrDefault();
            if (Object.ReferenceEquals(firstProperty, null))
                return EmptyTArray;
            var firstSet = this.GetObjectsAt(firstProperty);
            var ret = firstSet.Union(merged.Skip(1).Select(p => this.GetObjectsAt(p)).ToArray()).Objects;
            return ret;
        }
        public COHashTable<ClassificationObject> AllObjects
        {
            get
            {
                CoHashTableLock.EnterReadLock();
                try
                {
                    return this._coHashTable;
                }
                finally
                {
                    CoHashTableLock.ExitReadLock();
                }
                
            }
        }

        /// <summary>
        /// Shows number of elements currently in space.
        /// </summary>
        public override string ToString()
        {
            CoHashTableLock.EnterReadLock();
            try
            {
                return String.Format(CultureInfo.CurrentCulture, "Elements: {0}", _coHashTable.Count);
            }
            finally
            {
                CoHashTableLock.ExitReadLock();
            }
        }
    }
}
