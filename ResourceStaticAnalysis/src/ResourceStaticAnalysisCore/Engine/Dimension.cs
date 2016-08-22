/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.Core.Engine.Properties;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine
{
    /// <summary>
    /// Dimension is a hashtable index using property values as keys and COs as values.
    /// Dimension is build on one selected property. THe purpose is to enable quicker selection
    /// of COs with same value of property.
    /// </summary>
    public class Dimension<T> where T : ClassificationObject
    {
        /// <summary>
        /// Stores the Hashtable with references to Classification Objects
        /// as well as a ref to LinkedListNode object for efficient dimension indexing
        /// </summary>
        private struct PropertyEntry
        {
            public PropertyEntry(COHashTable<T> coSet, LinkedListNode<Property> indexEntry)
            {
                COSet = coSet;
                IndexEntry = indexEntry;
            }

            public COHashTable<T> COSet;
            public LinkedListNode<Property> IndexEntry;
        }
        /// <summary>
        /// Should be the same as the name of ClassificationObject property that the dimension is build on.
        /// </summary>
        private readonly byte _id;

        /// <summary>
        /// Mapping of property values to COs.
        /// Keys are values of the property, values are ClassificationObjects
        /// </summary>
        private readonly Dictionary<Property, PropertyEntry> _mapping;

        /// <summary>
        /// Build a dimension from a CO collection using the property "Id" as base
        /// </summary>
        /// <param name="propertyId">Id of CO property to be used as base</param>
        /// <param name="coCollection">Collection of COs to build from</param>
        public Dimension(byte propertyId, IEnumerable<T> coCollection)
        {
            if (coCollection == null)
            {
                throw new ArgumentNullException(nameof(coCollection));
            }
            this._id = propertyId;

            _mapping =
                (
                from co in coCollection
                group co by co.Properties[propertyId] into propertyGroup
                select new KeyValuePair<Property, PropertyEntry>
                    (
                        propertyGroup.Key,
                        new PropertyEntry(new COHashTable<T>(propertyGroup), new LinkedListNode<Property>(propertyGroup.Key))
                    )
                 ).ToDictionary(pair => pair.Key, pair => pair.Value);
            LinkedList<Property> keyIndex = new LinkedList<Property>();
            //a query to sort all Property Index Entries by Property Value
            var sortedIndexEntries =
                from propertyEntry in _mapping.Values
                let indexEntry = propertyEntry.IndexEntry
                orderby indexEntry.Value
                select indexEntry;
            foreach (var entry in sortedIndexEntries)
            {
                keyIndex.AddLast(entry);
            }
        }

        /// <summary>
        /// Number of "Points" in the Dimension, i.e. distinct property values.
        /// </summary>
        public int Count { get { return _mapping.Count; } }

        /// <summary>
        /// Returns the LinkendListNode object corresponding to the properties location in the Dimension.
        /// This can be used to move along the Dimension relatively to the given property.
        /// </summary>
        /// <param name="p"></param>
        /// <returns>Null if property does not exist in the dimension.</returns>
        public LinkedListNode<Property> GetPropertyIndex(Property p)
        {
            PropertyEntry ret;
            if (_mapping.TryGetValue(p, out ret))
            {
                return ret.IndexEntry;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a set of Classification Objects that have the same value of the property.
        /// </summary>
        /// <param name="p"></param>
        /// <returns>Empty set if property does not exist in the dimension.</returns>
        public COHashTable<T> GetObjectsAt(Property p)
        {
            PropertyEntry ret;
            if (_mapping.TryGetValue(p, out ret))
            {
                return ret.COSet;
            }
            else
            {
                return COHashTable<T>.Empty;
            }
        }
    }
}
