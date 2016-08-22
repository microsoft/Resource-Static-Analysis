/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;
using Microsoft.ResourceStaticAnalysis.Core.Engine;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestObjectStructure
{
    /// <summary>
    /// This adapter knows how to create SampleClassificationObjectType objects using SampleResourceCollection data source
    /// </summary>
    public class SampleDataAdapter : ClassificationObjectAdapter
    {
        public SampleDataAdapter(IDictionary<Type, IList<PropertyAdapter>> propertyAdapters) : base(propertyAdapters) { }
        #region IClassificationObjectAdapter Members
        public override Type PrimaryDataSource
        {
            get { return typeof(SampleResourceCollection); }
        }
        
        public override IList<Type> SecondaryDataSources
        {
            get { return null; }
        }
        public override Type ClassificationObjectType
        {
            get { return typeof(SampleClassificationObjectType); }
        }
        public override ICollection<ClassificationObject> InitializeObjects(DataSourcePackage dsp)
        {
            // Load objects using SampleResourceCollection as primary source
            return this.LoadObjects(
                (SampleResourceCollection)dsp.PrimaryDataSource.SourceInstance);
        }
        #endregion

        private PropertyAdapter[] resourceFilePropertyAdapters;
        private PropertyAdapter[] selfPropertyAdapters;
        /// <summary>
        /// The constructor must specify what datasources are expected to be provided to the object factory
        /// </summary>
        /// <param name="resourceFile">SampleResourceCollection object from which Objects will be loaded</param>
        /// <returns>Objects loaded from the SampleResourceCollection and 'flattened', with all properties extracted.</returns>
        private ICollection<ClassificationObject> LoadObjects(SampleResourceCollection resourceFile)
        {
            objects = new List<ClassificationObject>(1024);
            try
            {
                // Get all property adapters that deliver SampleClassificationObjectType properties from a SampleResourceEntry object
                resourceFilePropertyAdapters = this.propertyAdapters.Where(pa => pa.DataSourceType.Equals(typeof(SampleResourceEntry))).ToArray();
                if (resourceFilePropertyAdapters.Length == 0)
                {
                    throw new InvalidOperationException("Could not find property adapters for data source type: " + typeof(SampleResourceEntry));
                }
                // Get all property adapters that deliver SampleClassificationObjectType properties from existing SampleClassificationObjectType properties. 
                // This adapter will use the SampleClassificationObjectType object as its data source
                selfPropertyAdapters = this.propertyAdapters.Where(pa => pa.DataSourceType.Equals(typeof(SampleClassificationObjectType))).ToArray();
                if (selfPropertyAdapters.Length == 0)
                {
                    throw new InvalidOperationException("Could not find property adapters for data source type: " + typeof(SampleClassificationObjectType));
                }

            }
            finally { }
            // Iterate recursively through all SampleResourceEntry objects in SampleResourceCollection.
            // Each SampleResourceEntry object becomes an instance of SampleClassificationObjectType
            foreach (SampleResourceEntry entry in resourceFile.Entries)
            {
                ProcessResourceEntry(entry);
            }
            return objects;
        }

        /// <summary>
        /// Create instances of SampleClassificationObjectType, adding them to the private objects collection.
        /// </summary>
        /// <param name="entry"></param>
        private void ProcessResourceEntry(SampleResourceEntry entry)
        {
            // Create Property provider by matching property adapters with data sources
            // PropertyProvider is called by ResourceStaticAnalysis when code attempts to retrieve a property that has not yet been created
            var pp = new PropertyProvider();
            
            // Add a pair: property adapter and an object that serves as source
            foreach (var adapter in resourceFilePropertyAdapters)
            {
                pp.AddPropertyAdapterDataSourcePair(adapter, entry);
            }
            // Initialize an instance of SampleClassificationObjectType using the PropertyProvider built above
            var obj = new SampleClassificationObjectType(pp);
            foreach (var adapter in selfPropertyAdapters)
            {
                // Now, add one more poperty adapter that uses the SampleClassificationObjectType object itself as data source
                // this one builds properties from existing properties.
                pp.AddPropertyAdapterDataSourcePair(adapter, obj);
            }

            // This reduces amount of memory used by object by compacting internal representation.
            pp.Compact();
            // Add to the list of all objects being constructed by this adapter.
            objects.Add(obj);
        }

        private ICollection<ClassificationObject> objects;
    }
}
