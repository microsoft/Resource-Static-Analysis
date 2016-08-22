/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.ResourceStaticAnalysis.DataAdapter.Parsers
{
    /// <summary>
    /// A class that represents a parsed .strings file
    /// Each resource and its associated metadata will be represented by a single <see cref="ResourceFileEntry"/>
    /// Both Apple recommended format and Microsoft-internal format are supported
    /// </summary>
    class AppleStringsFile
    { 
        public List<ResourceFileEntry> Entries { get; set; }
        public string FilePath { get; set; }
        public AppleStringsFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception("File not found or invalid filename specified");
            }
            FilePath = filePath;
            Entries = new List<ResourceFileEntry>();
            var parsedFile = File.ReadAllLines(filePath);
            string currentComment = string.Empty;
            int startLine = 0;
            int endLine = 0;
            int currentLine = 0;
            bool cStyleCommentBlock = false;

            foreach(string line in parsedFile)
            {
                currentLine++;
                try
                {
                    if (line.StartsWith("#"))
                    {
                        // Ignore simple comment lines
                    }
                   
                    // Comments
                    else if (line.StartsWith("/*"))
                    {
                        startLine = currentLine;
                        currentComment += line.Substring(line.IndexOf("/*") + 2);
                        // Single-line comment
                        if (line.EndsWith("*/"))
                        {
                            currentComment = currentComment.TrimEnd(new char[] {'*', '/'});
                        }
                        // Multi-line comment
                        else
                            cStyleCommentBlock = true;
                    }
                    else if (cStyleCommentBlock)
                    {
                        currentComment += line;
                        if (line.EndsWith("*/"))
                        {
                            currentComment = currentComment.TrimEnd(new char[] { '*', '/' });
                            cStyleCommentBlock = false;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(line) && line.Contains('='))
                    {
                        int indexOfEquals = line.IndexOf('=');
                        
                        endLine = currentLine;
                        if (currentComment.Length == 0)
                        {
                            startLine = currentLine;
                        }

                        // Remove any leading and trailing whitespace from comment
                        currentComment = currentComment.Trim();

                        // Parse resourceID
                        string resourceID = line.Substring(0, indexOfEquals).Trim();

                        // Parse string value
                        string value = line.Substring(line.IndexOf('=') + 1, line.Length - (line.IndexOf('=') + 1)).Trim();

                        string devComment = currentComment;

                        Entries.Add(new ResourceFileEntry(resourceID, value, startLine, endLine, filePath, devComment));

                        currentComment = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error parsing Apple .strings file", ex);
                }
            }

            // Need to identify duplicated ResourceId values and make them unique.
            ParserUtils.MakeResourceIdUnique(Entries, true);

            // Need to identify duplicated SourceString values and append a Tag and Index to the ResourceId.
            ParserUtils.MakeSourceStringUnique(Entries, true);
        }
    }
}
