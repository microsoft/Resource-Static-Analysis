/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.ResourceStaticAnalysis.DataAdapter.Parsers
{
    /// <summary>
    /// A class that represents a parsed .resx file or its variants (.resw, .ress, .resx.pp)
    /// Each resource and its associated metadata will be represented by a single <see cref="ResourceFileEntry"/>
    /// </summary>
    public class ResXFile
    {
        public List<ResourceFileEntry> Entries { get; set; }
        public string FilePath { get; set; }

        public ResXFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception("File not found or invalid filename specified");
            }
            FilePath = filePath;
            Entries = new List<ResourceFileEntry>();

            try
            {
                XDocument xDoc = XDocument.Load(filePath, LoadOptions.SetLineInfo);
                var resxNodes = from x in xDoc.Descendants("data")
                                where x.Attribute("mimetype") == null
                                select new ResourceFileEntry(
                                    (string)x.Attribute("name"), // ResourceId.
                                    (string)x.Descendants("value").FirstOrDefault(), // Value.
                                    ((IXmlLineInfo)x.Attribute("name")).LineNumber, // Start Line.
                                    ((IXmlLineInfo)x.NextNode)?.LineNumber - 1 ?? ((IXmlLineInfo)x.LastNode).LineNumber + 1, // End Line.
                                    filePath, // FilePath.
                                    x.Descendants("comment").FirstOrDefault()?.Value == null ? string.Empty : (string)x.Descendants("comment").FirstOrDefault()  // Comments.
                                    );

                Entries = resxNodes.ToList();

                // Need to identify duplicated ResourceId values and make them unique.
                ParserUtils.MakeResourceIdUnique(Entries, true);

                // Need to identify duplicated SourceString values and make them unique.
                ParserUtils.MakeSourceStringUnique(Entries, true);

            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing .resx file: " + ex.Message);
            }
        }
    }
}
