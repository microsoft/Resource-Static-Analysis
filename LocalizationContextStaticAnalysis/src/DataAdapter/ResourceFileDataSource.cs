/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;

namespace Microsoft.ResourceStaticAnalysis.DataAdapter
{
    /// <summary>
    /// A data source provider that knows how to open a precompile resource file,
    /// use the Parsers and return a ResourceFile object
    /// </summary>
    public class ResourceFileDataSource : IDataSourceProvider
    {
        #region IDataSourceProvider Members
        /// <summary>
        /// Creates a ResourceFile instance
        /// </summary>
        public object CreateDataSourceInstance(SourceLocation location)
        {
            if (!(location.Value is string))
                throw new Exception(String.Format(
                    "Incorrect source location: {0}. Expected: {1}.",
                    location.Value.GetType().FullName, typeof(string).FullName
                    ));
            string path = location.Value as string;
            return new ResourceFile(path);
        }

        /// <summary>
        /// Returns the type of the underlying data source
        /// </summary>
        public Type DataSourceType
        {
            get { return typeof(ResourceFile); }
        }

        #endregion
    }
}
