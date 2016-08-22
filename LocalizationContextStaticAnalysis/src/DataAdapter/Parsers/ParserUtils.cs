/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ResourceStaticAnalysis.DataAdapter.Parsers
{
    class ParserUtils
    {
        /// <summary>
        /// A static function that determines the uniqueness of an identifier parsed from file.
        /// If necessary a unique ID will be generated based on an index.
        /// <param name="entries">List of existing <see cref="ResourceFileEntry"/> objects to check against</param>
        /// <param name="tagDuplicates">Flag to tag duplicate IDs with a token to determine it is a duplicate</param>
        /// </summary>
        public static void MakeResourceIdUnique(List<ResourceFileEntry> entries, bool tagDuplicates)
        {
            // Check whether there are duplicated ResorceId values.
            var repeatedResources = entries.GroupBy(n => n.ResourceId).Where(x => x.Count() > 1).ToList();

            string tag = string.Empty;
            if (tagDuplicates)
            {
                tag = "_WRReviewer_Duplicate_ResourceId";
            }

            if (repeatedResources.Any())
            {
                foreach (IGrouping<string, ResourceFileEntry> resourceEntries in repeatedResources)
                {
                    int index = 0;
                    foreach (ResourceFileEntry resourceEntry in resourceEntries)
                    {
                        // We don't want to rename the first Resource that will be considered as "genuine".
                        if (index != 0)
                        {
                            resourceEntry.ResourceId = resourceEntry.ResourceId + tag + index;
                        }

                        index++;
                    }
                }
            }
        }

        /// <summary>
        /// A static function that determines the uniqueness of a SourceString value parsed from file.
        /// If necessary a unique ID will be generated based on an index to recognize the resource that has the issue.
        /// </summary>
        /// <param name="entries">List of existing <see cref="ResourceFileEntry"/> objects to check against</param>
        /// <param name="tagDuplicates">Flag to tag duplicate IDs with a token to determine it is a duplicate</param>
        public static void MakeSourceStringUnique(List<ResourceFileEntry> entries, bool tagDuplicates)
        {
            // Check whether there are duplicated SourceString values.
            var repeatedResources = entries.GroupBy(n => n.SourceString).Where(x => x.Count() > 1).ToList();

            string tag = string.Empty;
            if (tagDuplicates)
            {
                tag = "_WRReviewer_Duplicate_SourceString";
            }

            if (repeatedResources.Any())
            {
                int index = 1;
                foreach (IGrouping<string, ResourceFileEntry> resourceEntries in repeatedResources)
                {
                    bool firstResource = true;
                    foreach (ResourceFileEntry resourceEntry in resourceEntries)
                    {
                        // We don't want to rename the first Resource that will be considered as "genuine".
                        if (firstResource)
                        {
                            firstResource = false;
                        }
                        else
                        {
                            resourceEntry.ResourceId = resourceEntry.ResourceId + tag + index;
                        }
                    }

                    index++;
                }
            }
        }
    }
}
