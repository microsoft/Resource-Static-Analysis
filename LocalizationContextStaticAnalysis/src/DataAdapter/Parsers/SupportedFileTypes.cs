/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System.Collections.Generic;
using System.IO;

namespace Microsoft.ResourceStaticAnalysis.DataAdapter.Parsers
{
    public static class SupportedFileTypes
    {
        public static string ParserToUse(string file)
        {
            string fileExtension = Path.GetExtension(file).ToLower();

            // Special case for ResX-based file types
            if (fileExtension == ".resw")
            {
                fileExtension = ".resx";
            }

            if(!ListSupportedFileTypes().Contains(fileExtension))
            {
                fileExtension = string.Empty;
            }

            return fileExtension;
        }

        public static HashSet<string> ListSupportedFileTypes()
        {
            HashSet<string> supportedFileTypes = new HashSet<string> { ".resx", ".strings" };
            return supportedFileTypes;
        }
    }
}
