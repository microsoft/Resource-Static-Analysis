/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;
using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Engine.Properties;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.DataAdapter
{
    /// <summary>
    /// Property adapter that understands how to retrieve properties from <see cref="ResourceFileDataSource"/> object type.
    /// </summary>
    public class ResourceFileEntryPropertyAdapter : PropertyAdapter
    {
        protected override IReadOnlySet<byte> SupportedProperties
        {
            get { return supportedProperties; }
        }

        public override Type DataSourceType
        {
            get { return typeof(ResourceFileEntry); }
        }

        public override Type ClassificationObjectType
        {
            get { return typeof(LocResource); }
        }

        public override bool GetProperty(byte propertyId, object dataSource, out Property returnValue)
        {
            returnValue = null;
            if (this.PropertyIsSupported((byte)propertyId, dataSource))
            {
                returnValue = GetResourceFileEntryProperty((LocResource.LocResourceProps)propertyId,
                    dataSource as ResourceFileEntry);
                return true;
            }
            else
                return false;
        }

        private static readonly ReadonlyHashSet<byte> supportedProperties = new ReadonlyHashSet<byte>(new byte[]
        {
            (byte) LocResource.LocResourceProps.ResourceId,
            (byte) LocResource.LocResourceProps.SourceString,
            (byte) LocResource.LocResourceProps.Comments,
            (byte) LocResource.LocResourceProps.FilePath
        });

        /// <summary>
        /// Define the logic of how to provide each of the properties below
        /// </summary>
        /// <param name="propertyId">Numerical id of the property. This replaces string property name. A Classification Object type should expose an enum type listing all properties.</param>
        /// <param name="entry">A single entry in the file</param>
        /// <returns>Returns a Property given the propertyId.</returns>
        static Property GetResourceFileEntryProperty(LocResource.LocResourceProps propertyId, ResourceFileEntry entry)
        {
            if (entry == null)
            {
                throw new NullReferenceException(String.Format("DataSource must not be null. DataSource name: {0}", entry.GetType().Name));
            }
            try
            {
                switch (propertyId)
                {
                    case LocResource.LocResourceProps.ResourceId:
                        return new StringProperty((byte)propertyId, String.IsNullOrEmpty(entry.ResourceId) ? string.Empty : entry.ResourceId);
                    case LocResource.LocResourceProps.SourceString:
                        return new StringProperty((byte)propertyId, String.IsNullOrEmpty(entry.SourceString) ? string.Empty : entry.SourceString);
                    case LocResource.LocResourceProps.Comments:
                        return new StringProperty((byte)propertyId, String.IsNullOrEmpty(entry.Comment) ? string.Empty : entry.Comment);
                    case LocResource.LocResourceProps.FilePath:
                        return new StringProperty((byte)propertyId, String.IsNullOrEmpty(entry.FilePath) ? String.Empty : entry.FilePath);
                    default:
                        {
                            string message = string.Format("Cannot provide property {0} from datasource of type {1}.",
                                propertyId.ToString(), entry.GetType().Name);
                            Trace.TraceInformation(message);
                            throw new InvalidOperationException(message);
                        }
                }
            }
            catch (NullReferenceException e)
            {
                Trace.TraceInformation("Exception in ProvideProperty('{1}'): {0}", e.Message, propertyId.ToString());
                return null;
            }
        }

        private readonly static byte[] emptyByteArray = new byte[0];
    }

    /// <summary>
    /// Property adapter that understands how to retrieve properties from <see cref="ConfigDictionary"/> object type.
    /// </summary>
    public class ConfigDictPropertyAdapter : PropertyAdapter
    {
        protected override IReadOnlySet<byte> SupportedProperties
        {
            get { return supportedProperties; }
        }

        /// <summary>
        /// Returns type of the supported underlying data source. Here it will be <see cref="ConfigDictionary"/>.
        /// </summary>
        public override Type DataSourceType
        {
            get { return typeof(ConfigDictionary); }
        }

        /// <summary>
        /// Returns the type of the associated classification object, for which properties are retrieved (this is not the same as data source type).
        /// </summary>
        public override Type ClassificationObjectType
        {
            get { return typeof(LocResource); }
        }

        /// <summary>
        /// Attempts to return a property with the specified name.
        /// </summary>
        /// <returns>True if property retrieval was successful. False otherwise.</returns>
        public override bool GetProperty(byte propertyId, object dataSource, out Property returnValue)
        {
            returnValue = null;
            if (PropertyIsSupported((byte)propertyId, dataSource))
            {
                returnValue = GetConfigDictionaryProperty((LocResource.LocResourceProps)propertyId,
                    dataSource as ConfigDictionary);
                return true;
            }
            else
                return false;
        }

        private static readonly ReadonlyHashSet<byte> supportedProperties = new ReadonlyHashSet<byte>(new byte[]
        {
            (byte) LocResource.LocResourceProps.ResourceId,
            (byte) LocResource.LocResourceProps.SourceString,
            (byte) LocResource.LocResourceProps.Comments,
            (byte) LocResource.LocResourceProps.FilePath
        });

        /// <summary>
        /// Gets a value of of the property specified by name from ConfigDictionary.
        /// </summary>
        /// <param name="propertyId">Name of the property to retrieve.</param>
        /// <param name="md">Dictionary (data source) containing the data for the property.</param>
        /// <exception cref="PropertyRetrievalException">Thrown when either data source is null or when the data source does not contain the string value for the specified property name.</exception>
        private static Property GetConfigDictionaryProperty(LocResource.LocResourceProps propertyId, ConfigDictionary md)
        {
            if (md == null)
            {
                throw new PropertyRetrievalException(String.Format(CultureInfo.CurrentCulture, "DataSource must not be null. DataSource name: {0}", md.GetType().Name));
            }
            string value;
            md.TryGetValue(propertyId.ToString(), out value);
            switch (propertyId)
            {
                case LocResource.LocResourceProps.ResourceId:
                    return new StringProperty((byte)propertyId, value);
                case LocResource.LocResourceProps.SourceString:
                    return new StringProperty((byte)propertyId, value);
                case LocResource.LocResourceProps.Comments:
                    return new StringProperty((byte)propertyId, value);
                case LocResource.LocResourceProps.FilePath:
                    return new StringProperty((byte)propertyId, value);
                default: throw new PropertyRetrievalException(String.Format(CultureInfo.CurrentCulture, "Cannot provide property {0} from datasource of type {1}.", propertyId.ToString(), md.GetType().Name));
            }
        }
    }

    /// <summary>
    /// A property adapter that creates LocResource properties based on other LocResource properties. The datasource for the adapter
    /// is the LocResource object itself
    /// </summary>
    public class LocResourceSelfPropertyAdapter : PropertyAdapter
    {
        private static readonly ReadonlyHashSet<byte> supportedProperties = new ReadonlyHashSet<byte>(new byte[]
        {
            (byte) LocResource.LocResourceProps.ResourceId,
            (byte) LocResource.LocResourceProps.SourceString,
            (byte) LocResource.LocResourceProps.Comments,
            (byte) LocResource.LocResourceProps.FilePath
        });

        protected override IReadOnlySet<byte> SupportedProperties
        {
            get { return supportedProperties; }
        }

        public override Type DataSourceType
        {
            get { return typeof(LocResource); }
        }

        public override Type ClassificationObjectType
        {
            get { return typeof(LocResource); }
        }

        public override bool GetProperty(byte propertyId, object dataSource, out Property returnValue)
        {
            returnValue = null;
            if (this.PropertyIsSupported((byte)propertyId, dataSource))
            {
                returnValue = GetSelfProperty((LocResource.LocResourceProps)propertyId, dataSource as LocResource);
                return true;
            }
            else
                return false;
        }

        private static Property GetSelfProperty(LocResource.LocResourceProps propertyId, LocResource lr)
        {
            if (Object.ReferenceEquals(lr, null))
            {
                throw new NullReferenceException(String.Format(CultureInfo.CurrentCulture, "DataSource must not be null. DataSource name: {0}", lr.GetType().Name));
            }

            switch (propertyId)
            {
                case LocResource.LocResourceProps.ResourceId:
                    return new StringProperty((byte)propertyId, lr.ResourceId);
                case LocResource.LocResourceProps.SourceString:
                    return new StringProperty((byte)propertyId, lr.SourceString);
                case LocResource.LocResourceProps.Comments:
                    return new StringProperty((byte)propertyId, lr.Comments);
                case LocResource.LocResourceProps.FilePath:
                    return new StringProperty((byte)propertyId, lr.FilePath);
                default: throw new PropertyRetrievalException(String.Format(CultureInfo.CurrentCulture, "Cannot provide property {0} from datasource of type {1}.", propertyId.ToString(), lr.GetType().Name));
            }
        }
    }
}
