/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;
using Microsoft.ResourceStaticAnalysis.Core.Engine;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestObjectStructure
{
    public class SampleDataSource : IDataSourceProvider
    {
        #region IDataSourceProvider Members

        public object CreateDataSourceInstance(SourceLocation sl)
        {
            if (!(sl.Value is List<Tuple<string, string, string>>))
                throw new ResourceStaticAnalysisEngineConfigException(String.Format(CultureInfo.CurrentCulture, "Incorrect source location: {0}. Expected: {1}.",
                    sl.Value.GetType().FullName, typeof(List<Tuple<string, string, string>>).FullName
                    ));
            List<Tuple<string, string, string>> resources = sl.Value as List<Tuple<string, string, string>>;
            return new SampleResourceCollection(resources);
        }

        public Type DataSourceType
        {
            get { return typeof(SampleResourceCollection); }
        }

        #endregion
    }
}
