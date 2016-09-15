/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using Microsoft.ResourceStaticAnalysis.Core.Engine;

namespace Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages
{
    /// <summary>
    /// Information about a single data source that is part of a data package.
    /// </summary>
    [Serializable]
    public class DataSourceInfo : IEquatable<DataSourceInfo>, IDisposable
    {
        /// <summary>
        /// To enable serialization. Initializes SourceLocation member so that it can run ReadXml().
        /// </summary>
        public DataSourceInfo()
        {
            SourceLocation = new SourceLocation();
        }

        /// <summary>
        /// Constructs a new data source info with type and location information.
        /// </summary>
        public DataSourceInfo(string sourceType, SourceLocation sourceLocation)
        {
            SourceType = sourceType;
            SourceLocation = sourceLocation;
        }

        /// <summary>
        /// The data source type. This is a string representation of <seealso cref="System.Type"/>.
        /// </summary>
        [XmlAttribute("Type")]
        public string SourceType;

        /// <summary>
        /// Indicates that this data source is to be treated as primary.
        /// The data source marked as 'primary' is used by ResourceStaticAnalysis Engine as a provider of enumeration over the whole collection
        /// of Classification Objects in the data source package.
        /// </summary>
        [XmlAttribute("Primary")]
        public bool IsPrimary;
        
        /// <summary>
        /// The location of the data source. The same datasource can be provided in many different ways to the application.
        /// </summary>
        [XmlElement("SourceLocation")]
        public SourceLocation SourceLocation;
        
        /// <summary>
        /// Source instance is an already opened data source that will be used by Classification Object providers
        /// to load objecs and properties. The purpose of this class is to manage and prepare datasources for
        /// lower levels in the Engine.
        /// </summary>
        private object _sourceInstance;
        
        /// <summary>
        /// Public accessor for _sourceInstance
        /// </summary>
        public object SourceInstance
        {
            get
            {
                if (this._sourceInstance == null)
                {
                    throw new NullReferenceException(String.Format(CultureInfo.InvariantCulture,
                        "Source Instance has not been created. You must run Initialize() for DataSourceInfo object before accessing SourceInstance. " +
                        "Source Type: {0}, Source Location: {1}", this.SourceType, this.SourceLocation));
                }
                return this._sourceInstance;
            }
        }

        /// <summary>
        /// If sourceInstance is null attempt to create a SourceInstance from SourceLocation
        /// </summary>
        /// <exception cref="ResourceStaticAnalysisException">Thrown when data source instance could not be initialized properly. This could happen - for example - if a file comprising the datasource does not exist.</exception>
        public void Initialize(IDataSourceProvider provider)
        {
            if (this._sourceInstance == null)
            {
                lock (this)
                {
                    try
                    {
                        this._sourceInstance = provider.CreateDataSourceInstance(SourceLocation);
                    }
                    catch (FileNotFoundException e)
                    {
                        throw new ResourceStaticAnalysisException(string.Format(CultureInfo.InvariantCulture, "File '{1}' that is part of a datasource of type {0} cannot be found.",
                            provider.DataSourceType, e.FileName), e);
                    }
                    catch (Exception e)
                    {
                        throw new ResourceStaticAnalysisException(String.Format(CultureInfo.InvariantCulture, "Creating datasource instance of type {0} from source location {1} failed.",
                            provider.DataSourceType, SourceLocation), e);
                    }
                }
            }
        }

        /// <summary>
        /// Shows data source type and location.
        /// If source type is not set, it displays "Source  type not set" text instead.
        /// If location is not set, it displays "Location not set" text instead.
        /// </summary>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}: {1}",
                SourceType == null ? "<Source type not set>" : SourceType.ToString(),
                SourceLocation == null ? "<Location not set>" : SourceLocation.ToString());
        }

        /// <summary>
        /// Gets the System.Type of the source instance. this type is constructed dynamically at runtime.
        /// </summary>
        public Type SourceInstanceType
        {
            get
            {
                if (SourceInstance == null) throw new ResourceStaticAnalysisException("DataSourceInfo.SourceInstanceType: It is impossible to get source instance type becase source instance has not been initialized.");
                return SourceInstance.GetType();
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            if (!Object.ReferenceEquals(_sourceInstance, null) &&
                _sourceInstance is IDisposable)
            {
                (_sourceInstance as IDisposable).Dispose();
            }
            _sourceInstance = null;
        }
        #endregion

        #region IEquatable Members
        public bool Equals(DataSourceInfo other)
        {
            // Two data sources are equal if the SourceType and SourceLocation are equal
            // We can compare DataSources and if they refer to the same source
            // we can avoid loading the same sourceinstance multiple times
            return (this.SourceType == other.SourceType && this.SourceLocation.Equals(other.SourceLocation));
        }
        #endregion

        #region Object Model
        /// <summary>
        /// Sets the data source type to the full name of the specified type.
        /// </summary>
        public void SetSourceType(Type dataSourceType)
        {
            SourceType = dataSourceType.FullName;
        }

        /// <summary>
        /// Sets the data source type to the full name of the specified type.
        /// </summary>
        /// <typeparam name="T">Native type of data source that provides classification objects</typeparam>
        public void SetSourceType<T>()
        {
            SetSourceType(typeof(T));
        }

        /// <summary>
        /// Sets source location information.
        /// </summary>
        /// <param name="locationDetails">Any object that can act as a data source, e.g. a string representing a filepath, a key value pair etc.</param>
        public void SetSourceLocation(object locationDetails)
        {
            SourceLocation.Value = locationDetails;
        }
        #endregion
    }
}
