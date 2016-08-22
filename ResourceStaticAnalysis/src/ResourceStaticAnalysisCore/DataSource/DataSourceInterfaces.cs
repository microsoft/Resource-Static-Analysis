/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Engine.Properties;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.Core.DataSource
{
    /// <summary>
    /// Property adapter creates a Property object based on a data source object and/or based on existing
    /// properties in a Classification Object.
    /// </summary>
    public abstract class PropertyAdapter
    {
        /// <summary>
        /// Stores names of properties that this provider provides
        /// </summary>
        protected abstract IReadOnlySet<byte> SupportedProperties { get; }

        /// <summary>
        /// Returns the type of datasource that this provider uses to obtain properties from.
        /// </summary>
        public abstract Type DataSourceType { get; }

        /// <summary>
        /// Returns the type of Classification Object that this provider creates properties for.
        /// </summary>
        public abstract Type ClassificationObjectType { get; }

        /// <summary>
        /// Given property Id and datasource returns true or false if this property can be created using this provider.
        /// </summary>
        /// <param name="propertyId">Id of property to create.</param>
        /// <param name="dataSource">Data source to obtain the property from.</param>
        /// <param name="returnValue">If property can be created, the value is placed here.</param>
        /// <returns>True if property can be successfully obtained.</returns>
        public abstract bool GetProperty(byte propertyId, object dataSource, out Property returnValue);

        /// <summary>
        /// Checks if the property is supported by this provider.
        /// </summary>
        /// <param name="propertyId">Id of property to check</param>
        /// <returns></returns>
        public bool PropertyIsSupported(byte propertyId, object dataSource)
        {
            return this.SupportedProperties.Contains(propertyId) && this.DataSourceType.Equals(dataSource.GetType());
        }
        
    }

    /// <summary>
    /// Property Provider contains a set of PropertyAdapter - DataSource pairs that it uses to deliver a property specified by name.
    /// Instances of Property Providers are created by IDataAdapters when creating Classification Objects.
    /// </summary>
    public class PropertyProvider
    {
        private struct AdapterDataSourcePair
        {
            public PropertyAdapter Adapter;
            /// <summary>
            /// The source of information about classification objects.
            /// </summary>
            public object DataSource;

            /// <summary>
            /// Initializes a new instance of Adapter-DataSource pair.
            /// </summary>
            public AdapterDataSourcePair(PropertyAdapter adapter, object dataSource)
            {
                Adapter = adapter;
                DataSource = dataSource;
            }
        }

        private readonly List<AdapterDataSourcePair> _adapterDataSourcePairs = new List<AdapterDataSourcePair>();

        public void AddPropertyAdapterDataSourcePair(PropertyAdapter propertyAdapter, object dataSource)
        {
            this._adapterDataSourcePairs.Add(new AdapterDataSourcePair(propertyAdapter, dataSource));
        }

        /// <summary>
        /// Call after adding all PropertyAdapterDataSourcePairs. This reduces amount of memory used by object
        /// by compacting internal representation.
        /// </summary>
        public void Compact()
        {
            this._adapterDataSourcePairs.Capacity = this._adapterDataSourcePairs.Count;
        }

        public bool GetProperty(byte propertyId, out Property returnValue)
        {
            bool propertyRetrieved = false;
            returnValue = null;
            // search all pairs and stop on the first one that successfully retrieves the property
            for (int i = 0; i < _adapterDataSourcePairs.Count; i++)
            {
                if (_adapterDataSourcePairs[i].Adapter.GetProperty((byte)propertyId, _adapterDataSourcePairs[i].DataSource, out returnValue))
                {
                    propertyRetrieved = true;
                    break;
                }
            }
            return propertyRetrieved;
        }
    }


    /// <summary>
    /// Takes the parameters of datasource and creates an instance of datasource (i.e. opens a file, obtains recordset from database, etc.)
    /// </summary>
    public interface IDataSourceProvider
    {
        object CreateDataSourceInstance(SourceLocation location);

        /// <summary>
        /// The data source type that the provider creates
        /// </summary>
        Type DataSourceType { get; }
    }
    /// <summary>
    /// Initiates object instances based on the DataSourcePackage configuration.
    /// Initiation creates an instance of a CO based on the main data source and assigns PropertyProvider
    /// that will later provide properties from the configured data sources.
    /// </summary>
    public abstract class ClassificationObjectAdapter
    {
        protected readonly IList<PropertyAdapter> propertyAdapters;
        /// <summary>
        /// Creates an instance of classification object adapter, using the specified collection of property adapters.
        /// </summary>
        /// <param name="propertyAdapters"></param>
        public ClassificationObjectAdapter(IDictionary<Type, IList<PropertyAdapter>> propertyAdapters)
        {
            this.propertyAdapters = propertyAdapters[this.ClassificationObjectType];
        }

        /// <summary>
        /// Returns the type of Classification Object that the DataAdapter produces.
        /// </summary>
        public abstract Type ClassificationObjectType { get; }

        /// <summary>
        /// The primary data source from which the objects are initialized
        /// </summary>
        public abstract Type PrimaryDataSource { get; }

        /// <summary>
        /// Secondary datasources are used to create some or all properties of the CO but not to initialize instances of objects.
        /// </summary>
        public abstract IList<Type> SecondaryDataSources { get; }

        /// <summary>
        /// Initializes Classification Objects based on the content of the data source package
        /// </summary>
        /// <param name="dsp">Data source package to use</param>
        /// <returns></returns>
        public abstract ICollection<ClassificationObject> InitializeObjects(DataSourcePackage dsp);

        /// <summary>
        /// Checks that CO type, Primary Data Source Type and Secondary Data Source types match between the DataSourcePackage
        /// and the DataAdapter. Throws exceptions if one of the types doesn't match. Returns true if everythings fine.
        /// </summary>
        protected bool ValidateTypes(DataSourcePackage package)
        {
            if (!package.ClassificationObjectTypeName.Equals(this.ClassificationObjectType.FullName))
                throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture,
                    "DataSource Package CO type {0} does not match DataAdapter CO type {1}",
                    package.ClassificationObjectTypeName,
                    this.ClassificationObjectType.FullName
                    ));
            if (!this.PrimaryDataSource.IsAssignableFrom(package.PrimaryDataSource.SourceInstanceType))
                throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture,
                    "DataSource Package primary data source type {0} does not match DataAdapter primary data source type {1}",
                    package.PrimaryDataSource,
                    this.PrimaryDataSource
                    ));
            if (package.SecondaryDataSources.Any())
            {
                if (!(package.SecondaryDataSources.Count() == this.SecondaryDataSources.Count() &&
                this.SecondaryDataSources.All(ds => ds.IsAssignableFrom(package.SecondaryDataSources.ElementAt(this.SecondaryDataSources.IndexOf(ds)).SourceInstanceType))))
                    throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture,
                        "DataSource Package secondary data source types {0} do not match DataAdapter secondary data source types {1}",
                        String.Join(",", package.SecondaryDataSources.Select(ds => ds.SourceInstanceType.FullName).ToArray()),
                        String.Join(",", this.SecondaryDataSources.Select(ds => ds.FullName).ToArray())
                        ));
            }
            
            return true;
        }

        /// <summary>
        /// Checks if the adapter can initialize classification objects from the provided data source package.
        /// </summary>
        public bool PackageIsSupported(DataSourcePackage package)
        {
            try
            {
                ValidateTypes(package);
            }
            catch (Exception e)
            {
                Trace.TraceWarning("PackageIsSupported(): {0}", e.Message);
                return false;
            }
            return true;
        }
    }
}
