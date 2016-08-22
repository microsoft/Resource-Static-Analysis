/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;
using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.DataAdapter
{
    /// <summary>
    /// This adapter knows how to create LocResource objects using two data sources:
    /// 1. A ResourceFile (List of <see cref="ResourceFileEntry"/> objects) as the primary source.
    /// Each <see cref="ResourceFileEntry"/> object becomes a LocResource object.
    /// 2. A ConfigDictionary type: this is a type representing a dictionary of string values.
    /// It allows you to add additional data about a file, such as project to which it belongs, build number etc.
    /// This data is used to generate certain properties of LocResource objects that could not be
    /// generated based on the content of the precompile resource file itself.
    /// </summary>
    public class ResourceFileDataAdapter : ClassificationObjectAdapter
    {
        public ResourceFileDataAdapter(IDictionary<Type, IList<PropertyAdapter>> propertyAdapters) : base(propertyAdapters) { }
        #region IClassificationObjectAdapter Members
        public override Type PrimaryDataSource
        {
            get { return typeof(ResourceFile); }
        }
        private static readonly List<Type> secondaryDataSources = new List<Type> { typeof(ConfigDictionary) };
        public override IList<Type> SecondaryDataSources
        {
            get { return secondaryDataSources; }
        }
        public override Type ClassificationObjectType
        {
            get { return typeof(LocResource); }
        }
        public override ICollection<ClassificationObject> InitializeObjects(DataSourcePackage dsp)
        {
            if (dsp.SecondaryDataSources.FirstOrDefault() == null)
            {
                // There is only a Primary DataSource. Create an empty ConfigDictionary as second Data Source.
                return this.LoadObjects((ResourceFile) dsp.PrimaryDataSource.SourceInstance, null);
            }
            
            // Load objects using ResourceFile as primary source and Config dictionary as secondary source.
            return this.LoadObjects(
                (ResourceFile)dsp.PrimaryDataSource.SourceInstance,
                (ConfigDictionary)dsp.SecondaryDataSources.FirstOrDefault().SourceInstance
                );
        }
        #endregion

        private PropertyAdapter[] resourceFilePropertyAdapters;
        private PropertyAdapter[] configDictPropertyAdapters;
        private PropertyAdapter[] selfPropertyAdapters;

        /// <summary>
        /// The constructor must specify what datasources are expected to be provided to the object factory
        /// For example, at this moment all LocResource properties are created from ResourceFiles but some properties (e.g. project)
        /// cannot be retrieved from ResourceFiles so we need additional datasource(s) (in this case a simple struct with strings)
        /// </summary>
        /// <param name="resourceFile">The ResourceFile from which to load objects</param>
        /// <param name="md">Extra properties</param>
        /// <returns>Objects loaded from the Resource file and 'flattened', with all properties extracted.</returns>
        private ICollection<ClassificationObject> LoadObjects(ResourceFile resourceFile, ConfigDictionary md)
        {
            myConfig = md;
            objects = new List<ClassificationObject>(1024);
            try
            {
                // Get all property adapters that deliver LocResource properties from ResourceFileEntry objects
                resourceFilePropertyAdapters = this.propertyAdapters.Where(pa => pa.DataSourceType.Equals(typeof(ResourceFileEntry))).ToArray();
                if (resourceFilePropertyAdapters.Length == 0)
                {
                    throw new InvalidOperationException("Could not find property adapters for data source type: " + typeof(ResourceFileEntry));
                }

                if (md != null)
                {
                    // Get all property adapters that deliver LocResource properties from a ConfigDictionary object
                    configDictPropertyAdapters = this.propertyAdapters.Where(pa => pa.DataSourceType.Equals(typeof(ConfigDictionary))).ToArray();
                    if (configDictPropertyAdapters.Length == 0)
                    {
                        throw new InvalidOperationException("Could not find property adapters for data source type: " + typeof(ConfigDictionary));
                    }
                }

                // Get all property adapters that deliver LocResource properties from existing LocResource poperties. This adapter will use the LocResource
                // object as its data source
                selfPropertyAdapters = this.propertyAdapters.Where(pa => pa.DataSourceType.Equals(typeof(LocResource))).ToArray();
                if (selfPropertyAdapters.Length == 0)
                {
                    throw new InvalidOperationException("Could not find property adapters for data source type: " + typeof(LocResource));
                }

            }
            finally { }
            // Code specific to the ResourceFileDataSource which is the primary data source
            // iterate recursively through all ResourceFileEntry objects in ResourceFile
            // each ResourceFileEntry object becomes an instance of LocResource 
            foreach (ResourceFileEntry entry in resourceFile.Entries)
            {
                ProcessResourceFileEntry(entry);
            }
            return objects;
        }

        /// <summary>
        /// Create instances of LocResource, adding them to the private objects collection.
        /// </summary>
        /// <param name="entry"></param>
        private void ProcessResourceFileEntry(ResourceFileEntry entry)
        {
            // Create Property provider by matching property adapters with data sources
            // PropertyProvider is called by ResourceStaticAnalysis when code attempts to retrieve a property that has not yet been created
            var pp = new PropertyProvider();

            // Add a pair: property adapter and an object that serves as source
            foreach (var adapter in resourceFilePropertyAdapters)
            {
                pp.AddPropertyAdapterDataSourcePair(adapter, entry);
            }

            if (configDictPropertyAdapters != null)
            {
                foreach (var adapter in configDictPropertyAdapters)
                {
                    pp.AddPropertyAdapterDataSourcePair(adapter, myConfig);
                }
            }
            
            // Initialize an instance of LocResource using the PropertyProvider built above
            var lr = new LocResource(pp);
            foreach (var adapter in selfPropertyAdapters)
            {
                // Now, add one more poperty adapter that uses the LocResource object itself as data source.
                // This one builds properties from existing properties.
                pp.AddPropertyAdapterDataSourcePair(adapter, lr);
            }

            // This reduces amount of memory used by object by compacting internal representation.
            pp.Compact();

            // Add to the list of all objects being constructed by this adapter.
            objects.Add(lr);
        }

        private ICollection<ClassificationObject> objects;
        private ConfigDictionary myConfig;
    }
}