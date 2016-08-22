/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;

namespace Microsoft.ResourceStaticAnalysis.DataAdapter
{
    public class ResourceFileEntry
    {
        public string Comment { get; private set; }
        public string ResourceId { get; set; }
        public string SourceString { get; set; }
        public string Location { get; private set; }
        public string FilePath { get; private set; }
        public Dictionary<string, object> CustomProperties { get; private set; }

        public ResourceFileEntry(string resourceId, string sourceString, int startLineCount, int endLineCount, string filePath, string comment, Dictionary<string, object> customProperties = null)
        {
            this.ResourceId = resourceId;
            this.Location = String.Format("|{0}|{1}|", startLineCount, endLineCount);
            this.SourceString = sourceString;
            this.FilePath = filePath;
            this.Comment = comment;
            this.CustomProperties = customProperties;
        }
    }
}
