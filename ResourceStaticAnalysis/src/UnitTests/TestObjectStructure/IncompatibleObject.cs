/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Engine.Properties;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestObjectStructure
{
    public class IncompatibleObject : ClassificationObject
    {
        /// <summary>
        /// Creates an instances of a IncompatibleObject with a Propety Provider that will be used to 
        /// generate properties on demand.
        /// </summary>
        /// <param name="propertyProvider">Object that provides CO properties based on underlying data sources.</param>
        public IncompatibleObject(PropertyProvider propertyProvider) : base(propertyProvider)
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

        static IncompatibleObject()
        {
            _enumManager = new PropertyEnumManager<byte>(typeof(SupportedProperties));
            _propertyIndexer = new PropertyIndexer((byte)_enumManager.PropertyIds.Count);
        }

        public enum SupportedProperties : byte
        {
            FileName,
            BinaryContent,
            CheckSumError
        }

        #region PropertyDefinitions
        
        /// <summary>
        /// Name of file
        /// </summary>
        public StringProperty FileName
        {
            get
            {
                return (StringProperty)Properties[(byte)SupportedProperties.FileName];
            }
        }

        /// <summary>
        /// Raw binary content
        /// </summary>
        public StringProperty BinaryContent
        {
            get
            {
                return (StringProperty)Properties[(byte)SupportedProperties.BinaryContent];
            }
        }
        /// <summary>
        /// Determines if there was a CheckSum error
        /// </summary>
        public StringProperty CheckSumError
        {
            get
            {
                return (StringProperty)Properties[(byte)SupportedProperties.CheckSumError];
            }
        }

        #endregion
    }
}

