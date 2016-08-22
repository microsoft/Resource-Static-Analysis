/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ResourceStaticAnalysis.DataAdapter.Parsers;

namespace Microsoft.ResourceStaticAnalysis.DataAdapter
{
    /// <summary>
    /// A class that creates a parsed precompile resource file based on associated Parsers
    /// Each resource and its associated metadata will be represented by a single <see cref="ResourceFileEntry"/>
    /// </summary>
    public class ResourceFile
    {
        public List<ResourceFileEntry> Entries { get; set; }
        public string FilePath { get; set; }
        public ResourceFile(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new Exception("File not found or invalid filename specified");
            }
            FilePath = filename;
            Entries = new List<ResourceFileEntry>();
            string parserType = SupportedFileTypes.ParserToUse(filename);

            switch (parserType)
            {
                case ".resx":
                    ResXFile tempResX = new ResXFile(filename);
                    foreach (ResourceFileEntry cfe in tempResX.Entries)
                    {
                        Entries.Add(cfe);
                    }
                    break;
                case ".strings":
                    AppleStringsFile tempAppleStringsFile = new AppleStringsFile(filename);
                    foreach (ResourceFileEntry cfe in tempAppleStringsFile.Entries)
                    {
                        Entries.Add(cfe);
                    }
                    break;
                default:
                    throw new Exception("FileType not supported");
            }
        }
    }
}


