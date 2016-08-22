/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CustomTraceListeners;
using Microsoft.ResourceStaticAnalysis.DataAdapter.Parsers;

namespace Microsoft.ResourceStaticAnalysis.ResourceStaticAnalysisExecutor
{
    /// <summary>
    /// Console application for standalone execution of ResourceStaticAnalysis against a specified file
    /// using specified rules configuration
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            CustomFormatConsoleTraceListener traceListener = new CustomFormatConsoleTraceListener();
            Trace.AutoFlush = true;
            Trace.Listeners.Add(traceListener);
            
            if (args.Length != 2 )
            {
                Trace.TraceError("Usage is ResourceStaticAnalysisExecutor.exe <Rules Path directory> <Path to file to be scanned>");
                return;
            }

            string checksDirectory = args[0];
            if (Directory.Exists(checksDirectory))
            {
                if (
                    Directory.GetFiles(checksDirectory)
                        .Any(
                            fileName =>
                                Path.GetExtension(fileName).Equals(".dll", StringComparison.CurrentCultureIgnoreCase)) == false)
                {
                    Trace.TraceError("Cannot find dll with compiled rules");
                    return;
                }
            }
            else
            {
                Trace.TraceError("Cannot find the Rules Path directory");
                return;
            }

            string file = args[1];

            if (!File.Exists(file))
            {
                Trace.TraceError("Cannot find file to be scanned: {0}", file);
                return;
            }

            FileInfo fi = new FileInfo(file);
            string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ResourceStaticAnalysisOutput" + fi.DirectoryName.Remove(0, 2));
            string outputFile = Path.Combine(outputDirectory, fi.Name + ".xml");

            // Write configuration to Console
            Trace.TraceInformation("checksDirectory={0}", checksDirectory);
            Trace.TraceInformation("FileToBeScanned={0}", file);
            Trace.TraceInformation("OutputFile={0}", outputFile);

            try
            {
                if (!Directory.GetFiles(checksDirectory).Any(assembly => assembly.EndsWith(".dll") || assembly.EndsWith(".exe")))
                {
                    Trace.TraceError("Checks directory does not contain a rules assembly: {0}", checksDirectory);
                    return;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to validate rules assemblies in {0}. Exception:{1}", checksDirectory, ex.ToString());
                return;
            }

            bool parserAvailable = !string.IsNullOrEmpty(SupportedFileTypes.ParserToUse(file));

            if (parserAvailable)
            {
                try
                {
                    ResourceStaticAnalysisApplication ResourceStaticAnalysisApplication = ResourceStaticAnalysisApplication.Instance;
                    List<Exception> exceptions = new List<Exception>();
                    ResourceStaticAnalysisApplication.Initialize(new List<string>() {checksDirectory});
                    ResourceStaticAnalysisApplication.ConfigureOutput(outputFile);
                    ResourceStaticAnalysisApplication.Execute(file, true, out exceptions);
                    if (exceptions != null)
                    {
                        foreach (Exception ex in exceptions)
                        {
                            Trace.TraceError("{0}", ex.ToString());
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception implementing ResourceStaticAnalysisApplication", ex.ToString());
                    return;
                }
            }
            else
            {
                Trace.TraceError("Unsupported File Type");
            }
        }
    }
}
