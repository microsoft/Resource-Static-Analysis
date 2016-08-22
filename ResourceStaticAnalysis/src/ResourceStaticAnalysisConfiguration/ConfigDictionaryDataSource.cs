/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Globalization;
using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.Configuration
{
    /// <summary>
    /// A data source provider that understands how to read data (properties) from ConfigDictionary object (used as secondary data source).
    /// </summary>
    public class ConfigDictionaryDataSource : IDataSourceProvider
    {
        #region IDataSourceProvider Members
        /// <summary>
        /// Creates a ConfigDictionary instance that will be used to read property values from the dictionary for each Classification Object (e.g.: LocResource) 
        /// returned from enumerator of DataSource (e.g.: ResourceFileDataSource).
        /// </summary>
        public object CreateDataSourceInstance(SourceLocation sl)
        {
            if (!(sl.Value is ConfigDictionary))
                throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture, "Incorrect source location: {0}. Expected: {1}.",
                    sl.Value.GetType().FullName, typeof(ConfigDictionary).FullName
                    ));
            return sl.Value;
        }

        /// <summary>
        /// Returns the type of the underlying data source. For ConfigDictionary it will be the type of ConfigDictionary.
        /// </summary>
        public Type DataSourceType
        {
            get { return typeof(ConfigDictionary); }
        }

        #endregion
    }
}
