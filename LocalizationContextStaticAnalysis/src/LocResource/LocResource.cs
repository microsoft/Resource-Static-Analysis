/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Engine.Properties;

namespace Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource
{
    /// <summary>
    /// LocResource is an abstraction of a Localizable Resource.
    /// </summary>
    public partial class LocResource : ClassificationObject
    {
        /// <summary>
        /// Creates an instances of a LocResource with a Propety Provider that will be used to 
        /// generate properties on demand.
        /// </summary>
        /// <param name="propertyProvider">Object that provides CO properties based on underlying data sources.</param>
        public LocResource(PropertyProvider propertyProvider) : base(propertyProvider)
        {
        }

        /// <summary>
        /// Resolves property ids and names based on the underlying Enum type
        /// </summary>
        public override PropertyEnumManager<byte> PropertyEnumManager
        {
            get { return enumManager; }
        }

        private static readonly PropertyEnumManager<byte> enumManager;

        /// <summary>
        /// Efficiently indexes properties stored inside CO
        /// </summary>
        protected override PropertyIndexer PropertyIndexer
        {
            get { return propertyIndexer; }
        }

        private static readonly PropertyIndexer propertyIndexer;

        /// <summary>
        /// User friendly information that helps identify the object - used for debugging
        /// </summary>
        public override string ToString()
        {
            return ResourceId.Value;
        }

        static LocResource()
        {
            enumManager = new PropertyEnumManager<byte>(typeof(LocResourceProps));
            propertyIndexer = new PropertyIndexer((byte)enumManager.PropertyIds.Count);
        }

        #region All properties listed here
        #region Description
        // The Classification Object properties below are explicitly exposed for use in Rules.
        // Internally all COs story properties in the "Properties" dictionary. However in order to enable
        // Strongly typed property access in Rules each derrived class of Classification Object has to
        // explicitly implement properties and refer to the internal dictionary.
        #endregion

        /// <summary>
        /// Contains a list of all properties exposed by LocResource.
        /// This type has to be updated each time a property is added/removed/renamed.
        /// We are using this to avoid using strings with property names for performance and robustness.
        /// </summary>
        public enum LocResourceProps : byte
        {
            Comments,
            ResourceId,
            SourceString,
            FilePath,
            Project
        }

        /// <summary>
        /// Source string of a LocResourceItem.
        /// </summary>
        public StringProperty Comments
        {
            get
            {
                return (StringProperty)Properties[(byte)LocResourceProps.Comments];
            }
        }

        /// <summary>
        /// Source string of a LocResourceItem.
        /// </summary>
        public StringProperty SourceString
        {
            get
            {
                return (StringProperty)Properties[(byte)LocResourceProps.SourceString];
            }
        }

        /// <summary>
        /// FilePath string of a LocResourceItem.
        /// </summary>
        public StringProperty FilePath
        {
            get
            {
                return (StringProperty)Properties[(byte)LocResourceProps.FilePath];
            }
        }

        /// <summary>
        /// Resource Id string of a LocResourceItem
        /// </summary>
        public StringProperty ResourceId
        {
            get
            {
                return (StringProperty)Properties[(byte)LocResourceProps.ResourceId];
            }
        }

        /// <summary>
        /// Name of the project to which the Resource file belongs.
        /// </summary>
        public StringProperty Project
        {
            get
            {
                return (StringProperty)Properties[(byte)LocResourceProps.Project];
            }
        }

        #endregion
    }
}