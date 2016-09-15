/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.ResourceStaticAnalysis.Core.Engine;

namespace Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages
{
    /// <summary>
    /// Class holding a definition of data.
    /// One package may reference one or more data sources.
    /// </summary>
    [Serializable]
    public class DataSourcePackage : IDisposable
    {
        /// <summary>
        /// The array that stores all data sources (primary and secondary).
        /// </summary>
        [XmlArray("DataSources"), XmlArrayItem("DataSource")]
        public DataSourceInfo[] AllDataSources;

        /// <summary>
        /// Gets an enumeration of 0 or more data sources that are not marked as primary.
        /// </summary>
        [XmlIgnore]
        public IEnumerable<DataSourceInfo> SecondaryDataSources
        {
            get
            {
                return from dataSource in AllDataSources
                       where dataSource.IsPrimary == false
                       select dataSource;
            }
        }

        /// <summary>
        /// Gets the single instance of a data source which is marked as primary.
        /// If more than 1 data source is marked as primary, an exception will be thrown.
        /// If 0 data sources are marked as primary, null is returned.
        /// </summary>
        [XmlIgnore]
        public DataSourceInfo PrimaryDataSource
        {
            get
            {
                return (from dataSource in AllDataSources
                        where dataSource.IsPrimary
                        select dataSource).SingleOrDefault();
            }
        }

        [XmlElement("ClassificationObjectType")]
        public string ClassificationObjectTypeName;

        /// <summary>
        /// Default constructor required for serialization.
        /// </summary>
        public DataSourcePackage() { }
        
        /// <summary>
        /// Initializes all data sources belonging to the package.
        /// This makes sure that instances of data source objects are created.
        /// </summary>
        /// <exception cref="ResourceStaticAnalysisException">Thrown when expected data source provider is not available or where there is no primary data source.</exception>
        public void Initialize(IDictionary<string, IDataSourceProvider> dataSourceProviders)
        {
            foreach (DataSourceInfo ds in AllDataSources)
            {
                if (dataSourceProviders.ContainsKey(ds.SourceType))
                {
                    ds.Initialize(dataSourceProviders[ds.SourceType]);
                }
                else
                    throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture, "Could not initialize data source of type {0}, source location: {1}. Data source provider not found.",
                        ds.SourceType, ds.SourceLocation));
            }
            try
            {
                AllDataSources.Single(ds => ds.IsPrimary);
            }
            catch (InvalidOperationException)
            {
                throw new ResourceStaticAnalysisException("DataSource Package must contain exactly one data source marked as Primary.");
            }
        }

        /// <summary>
        /// Lists data source type and location for all data sources.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var dataSource in AllDataSources) sb.AppendFormat("{0}; ", dataSource.ToString());

            return sb.ToString();
        }

        #region IDisposable Members
        public void Dispose()
        {
            foreach (var dataSource in AllDataSources)
            {
                if (!Object.ReferenceEquals(dataSource, null))
                {
                    dataSource.Dispose();
                }
            }
        }
        #endregion

        #region Object model
        /// <summary>
        /// Sets the name of ClassificationObject that is being provided by this data source package.
        /// <param name="typeName">Fully specified name - including namespaces - of the CO type implemented by this data package.</param>
        /// </summary>
        public void SetCOTypeName(string typeName)
        {
            ClassificationObjectTypeName = typeName;
        }

        /// <summary>
        /// Sets the type of ClassificationObject that is being provided by this data source package.
        /// <param name="coType">Type of the ClassificationObject implemented by this data package.</param>
        /// </summary>
        public void SetCOType(Type coType)
        {
            SetCOTypeName(coType.FullName);
        }

        /// <summary>
        /// Sets the type of ClassificationObject that is being provided by this data source package.
        /// </summary>
        /// <typeparam name="COType">Type of the ClassificationObject implemented by this data package</typeparam>
        public void SetCOType<COType>()
        {
            SetCOType(typeof(COType));
        }

        /// <summary>
        /// Adds a new definition of data source to this package. If it is the first package to be added, it is set as primary.
        /// <param name="newDataSource">New data source to be added to the package</param>
        /// </summary>

        public void AddDataSource(DataSourceInfo newDataSource)
        {
            var currentList = new List<DataSourceInfo>();
            if (AllDataSources != null) currentList = AllDataSources.ToList();
            currentList.Add(newDataSource);
            if (currentList.Count == 1)
            { //If this is the first data source, mark it as primary by default
                newDataSource.IsPrimary = true;
            }
            AllDataSources = currentList.ToArray();
        }
        #endregion
    }

    public static class DataSourcePackageListExtension
    {
        /// <summary>
        /// If two DataSourcePackages contain the same DataSourceInfo then
        /// link them to single DataSourceInfo. Otherwise two instances of the same DataSourceInstance
        /// will be created when these packages are accessed.
        /// <param name="packageList">List of DataSourcePackages to normalize</param>
        /// </summary>
        public static void Normalize(this List<DataSourcePackage> packageList)
        {
            if (packageList == null)
            {
                throw new ResourceStaticAnalysisEngineConfigException("At least one Data Source Package must be provided to the engine.");
            }
            //list of unique data source info objects
            List<DataSourceInfo> infosList = new List<DataSourceInfo>();
            foreach (DataSourcePackage package in packageList)
            {
                DataSourceInfo existingInfo;
                for (int i = 0; i < package.AllDataSources.Length; i++)
                {
                    // check if this DataSourceInfo is on the list
                    existingInfo = infosList.SingleOrDefault(info => info.Equals(package.AllDataSources[i]));
                    if (existingInfo != null)
                    {
                        // subtitute with the known object
                        package.AllDataSources[i] = existingInfo;
                    }
                    else
                    {
                        // add the object to the list
                        infosList.Add(package.AllDataSources[i]);
                    }
                }
            }
        }
    }

    public delegate bool FilterFunc(ClassificationObject co);
    /// <summary>
    /// Exception thrown when the Classification Object passed into a rule is of
    /// an incompatible type, so the rule is not able to examine this object.
    /// This usually indicates an error in the logic of ResourceStaticAnalysis engine configuration
    /// that is allowing wrong type of Classification Object to be passed to a rule
    /// that does not expect such Classification Objects.
    /// </summary>
    public class UnknownClassificationObjectType : Exception
    {
        public UnknownClassificationObjectType(string message) : base(message) { }
    }
}
