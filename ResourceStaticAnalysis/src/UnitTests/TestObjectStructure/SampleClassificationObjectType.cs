/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Engine.Properties;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestObjectStructure
{
    public class SampleClassificationObjectType : ClassificationObject
    {
        /// <summary>
        /// Creates an instances of a SampleClassificationObjectType with a Propety Provider that will be used to 
        /// generate properties on demand.
        /// </summary>
        /// <param name="propertyProvider">Object that provides CO properties based on underlying data sources.</param>
        public SampleClassificationObjectType(PropertyProvider propertyProvider) : base(propertyProvider)
        {
        }

        /// <summary>
        /// Resolves property ids and names based on the underlying Enum type
        /// </summary>
        public override PropertyEnumManager<byte> PropertyEnumManager
        {
            get { return _enumManager; }
        }

        private static readonly PropertyEnumManager<byte> _enumManager;

        /// <summary>
        /// Efficiently indexes properties stored inside CO
        /// </summary>
        protected override PropertyIndexer PropertyIndexer
        {
            get { return _propertyIndexer; }
        }

        private static readonly PropertyIndexer _propertyIndexer;

        static SampleClassificationObjectType()
        {
            _enumManager = new PropertyEnumManager<byte>(typeof(SupportedProperties));
            _propertyIndexer = new PropertyIndexer((byte)_enumManager.PropertyIds.Count);
        }

        public enum SupportedProperties : byte
        {
            SourceString,
            ResourceId,
            Comments
        }

        #region PropertyDefinitions
        /// <summary>
        /// Content of the resource
        /// </summary>
        public StringProperty SourceString
        {
            get
            {
                return (StringProperty)Properties[(byte)SupportedProperties.SourceString];
            }
        }

        /// <summary>
        /// Resource Unique Identifier
        /// </summary>
        public StringProperty ResourceId
        {
            get
            {
                return (StringProperty)Properties[(byte)SupportedProperties.ResourceId];
            }
        }
        /// <summary>
        /// Descriptive comment for the resource
        /// </summary>
        public StringProperty Comments
        {
            get
            {
                return (StringProperty)Properties[(byte)SupportedProperties.Comments];
            }
        }

        #endregion
    }
}
