/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Globalization;
using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Engine.Properties;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestObjectStructure
{
    /// <summary>
    /// Sample Property Adapter from <see cref="SampleResourceEntry"/> to <see cref="SampleClassificationObjectType"/>
    /// </summary>
    public class SamplePropertyAdapter : PropertyAdapter
    {
        protected override IReadOnlySet<byte> SupportedProperties
        {
            get { return supportedProperties; }
        }

        public override Type DataSourceType
        {
            get { return typeof(SampleResourceEntry); }
        }

        public override Type ClassificationObjectType
        {
            get { return typeof(SampleClassificationObjectType); }
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
                returnValue = GetProperty((SampleClassificationObjectType.SupportedProperties)propertyId, dataSource as SampleResourceEntry);
                return true;
            }
            else
                return false;
        }
        private static readonly ReadonlyHashSet<byte> supportedProperties = new ReadonlyHashSet<byte>(new byte[]
            {
                (byte) SampleClassificationObjectType.SupportedProperties.SourceString,
                (byte) SampleClassificationObjectType.SupportedProperties.ResourceId,
                (byte) SampleClassificationObjectType.SupportedProperties.Comments
            }
        );

        private static Property GetProperty(SampleClassificationObjectType.SupportedProperties propertyId, SampleResourceEntry resourceEntry)
        {
            if (resourceEntry == null)
            {
                throw new PropertyRetrievalException(String.Format(CultureInfo.CurrentCulture, "DataSource must not be null. DataSource name: {0}", resourceEntry.GetType().Name));
            }

            try
            {
                switch (propertyId)
                {
                    case SampleClassificationObjectType.SupportedProperties.Comments: return new StringProperty((byte)propertyId, resourceEntry.Comments);
                    case SampleClassificationObjectType.SupportedProperties.ResourceId: return new StringProperty((byte)propertyId, resourceEntry.ResourceId);
                    case SampleClassificationObjectType.SupportedProperties.SourceString: return new StringProperty((byte)propertyId, resourceEntry.Value);
                    default:
                        {
                            string message = String.Format(CultureInfo.CurrentCulture, "Cannot provide property {0} from datasource of type {1}.", propertyId.ToString(), resourceEntry.GetType().Name);
                            throw new InvalidOperationException(message);
                        }
                }
            }
            catch (NullReferenceException e)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Self Property Adapter - from <see cref="SampleClassificationObjectType"/> to <see cref="SampleClassificationObjectType"/>
    /// </summary>
    public class SampleClassificationObjectSelfPropertyAdapter : PropertyAdapter
    {
        private static readonly ReadonlyHashSet<byte> supportedProperties = new ReadonlyHashSet<byte>(new byte[]
            {
                (byte) SampleClassificationObjectType.SupportedProperties.Comments,
                (byte) SampleClassificationObjectType.SupportedProperties.ResourceId,
                (byte) SampleClassificationObjectType.SupportedProperties.SourceString
            }
            );
        protected override IReadOnlySet<byte> SupportedProperties
        {
            get { return supportedProperties; }
        }

        public override Type DataSourceType
        {
            get { return typeof(SampleClassificationObjectType); }
        }

        public override Type ClassificationObjectType
        {
            get { return typeof(SampleClassificationObjectType); }
        }

        public override bool GetProperty(byte propertyId, object dataSource, out Property returnValue)
        {
            returnValue = null;
            if (this.PropertyIsSupported((byte)propertyId, dataSource))
            {
                returnValue = GetSelfProperty((SampleClassificationObjectType.SupportedProperties)propertyId, dataSource as SampleClassificationObjectType);
                return true;
            }
            else
                return false;
        }
        private static Property GetSelfProperty(SampleClassificationObjectType.SupportedProperties propertyId, SampleClassificationObjectType cO)
        {
            if (Object.ReferenceEquals(cO, null))
                throw new NullReferenceException(String.Format(CultureInfo.CurrentCulture, "DataSource must not be null. DataSource name: {0}", cO.GetType().Name));
            switch (propertyId)
            {
                case SampleClassificationObjectType.SupportedProperties.Comments: return new StringProperty((byte)propertyId, cO.Comments);
                case SampleClassificationObjectType.SupportedProperties.ResourceId: return new StringProperty((byte)propertyId, cO.ResourceId);
                case SampleClassificationObjectType.SupportedProperties.SourceString: return new StringProperty((byte)propertyId, cO.SourceString);
                default:
                    {
                        string message = String.Format(CultureInfo.CurrentCulture, "Cannot provide property {0} from datasource of type {1}.", propertyId.ToString(), cO.GetType().Name);
                        throw new InvalidOperationException(message);
                    }
            }
        }
    }
}
