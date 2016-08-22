/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.DataAdapter.Parsers;
using Microsoft.ResourceStaticAnalysis.ResourceStaticAnalysisExecutor;

namespace Microsoft.ResourceStaticAnalysis.BatchModeExecutor
{
    /// <summary>
    /// Console application for running ResourceStaticAnalysisExecutor across multiple files
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args.Length > 4)
            {
                Console.WriteLine("Usage is: BatchModeExecutor.exe <Rules Path directory> <directory to scan> <optional include subdirectories? (true|false)> <optional path to output directory>");
                return;
            }

            string checksDirectory = args[0];
            string directory = args[1];

            // Validate input directory
            if (!Directory.Exists(directory))
            {
                Console.WriteLine("Directory {0} does not exist", directory);
                return;
            }

            bool subDirectories = false;

            string output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"BatchModeExecutorOutput");
            List<string> allDirectories = new List<string>();
            int directoriesScanned = 0;
            int filesScanned = 0;
            int filesWithErrors = 0;
            List<Exception> reportedExceptions = new List<Exception>();
            List<string> keywordsToExcludeDirectories = new List<string>();

            if (args.Length >= 3)
            {
                bool checkArgument;
                subDirectories = Boolean.TryParse(args[2], out checkArgument);
                if (!checkArgument)
                {
                    Console.WriteLine("Usage is: BatchModeExecutor.exe <directory to scan> <optional include subdirectories? (true|false)> <optional path to output directory>");
                    return;
                }
            }

            if (args.Length == 4)
            {
                output = args[3];
            }

            string[] parsedExclusionFile = File.ReadAllLines("ExcludedPaths.txt");
            foreach (string line in parsedExclusionFile)
            {
                if (!line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
                {
                    keywordsToExcludeDirectories.Add(line);
                }
            }

            Console.WriteLine("Directory to be scanned is {0}", directory);
            Console.WriteLine("Include Subdirectories is set to {0}", subDirectories.ToString());
            Console.WriteLine("Output directory is {0}", output);

            if (!Directory.Exists(output))
            {
                try
                {
                    Directory.CreateDirectory(output);
                }
                catch (Exception ex)
                {
                    throw new IOException("Failed to create output directory", ex);
                }
            }

            try
            {
                allDirectories.Add(directory);

                if (subDirectories == true)
                {
                    foreach (
                        string singleDirectory in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
                    {
                        if (
                            !keywordsToExcludeDirectories.Any(
                                kw => singleDirectory.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            allDirectories.Add(singleDirectory);
                        }
                    }
                }

                try
                {
                    if (!Directory.GetFiles(checksDirectory).Any(checksFile => checksFile.EndsWith(".dll") || checksFile.EndsWith(".exe") || checksFile.EndsWith(".cs")))
                    {
                        Console.WriteLine("Error: Checks directory does not contain a rules source or module: {0}", checksDirectory);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: Failed to validate rules file(s) in {0}. Exception:{1}", checksDirectory, ex.ToString());
                    return;
                }

                // Iterate through each applicable directory
                foreach (string singleDirectory in allDirectories)
                {
                    Console.WriteLine("Scanning directory: {0}", singleDirectory);
                    List<FileInfo> directoryFiles = GetFilesByExtensions(new DirectoryInfo(singleDirectory));
                    foreach (FileInfo applicableFile in directoryFiles)
                    {
                        try
                        {
                            // Assume a local file path
                            string outputDirectory = Path.Combine(output, applicableFile.DirectoryName.Remove(0, 3));
                            if (!Directory.Exists(outputDirectory))
                            {
                                try
                                {
                                    Directory.CreateDirectory(outputDirectory);
                                }
                                catch (Exception ex)
                                {
                                    throw new IOException("Failed to create output directory", ex);
                                }
                            }

                            ResourceStaticAnalysisApplication resourceStaticAnalysisApplication = ResourceStaticAnalysisApplication.Instance;
                            List<Exception> exceptions = new List<Exception>();
                            resourceStaticAnalysisApplication.Initialize(new List<string>() { checksDirectory });
                            string outputFile = Path.Combine(outputDirectory, applicableFile.Name + ".xml");
                            resourceStaticAnalysisApplication.ConfigureOutput(outputFile);
                            resourceStaticAnalysisApplication.Execute(applicableFile.FullName, true, out exceptions);
                            if (exceptions != null)
                            {
                                filesWithErrors++;
                                foreach (Exception ex in exceptions)
                                {
                                    reportedExceptions.Add(ex);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            reportedExceptions.Add(ex);
                            filesWithErrors++;
                        }

                        filesScanned++;
                    }

                    directoriesScanned++;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error bulk processing files", ex);
            }

            Console.WriteLine("Bulk operation complete...");
            Console.WriteLine("Direcotries Scanned:{0}", directoriesScanned);
            Console.WriteLine("Files scanned:{0}", filesScanned);
            Console.WriteLine("Files successfully scanned:{0}", filesScanned - filesWithErrors);
            Console.WriteLine("Files with errors:{0}", filesWithErrors);
            Console.WriteLine("Error details:");

            foreach (Exception re in reportedExceptions)
            {
                Console.WriteLine(re.ToString());
            }
        }

        public static List<FileInfo> GetFilesByExtensions(DirectoryInfo dirInfo)
        {
            var supportedFileTypes = SupportedFileTypes.ListSupportedFileTypes();
            return dirInfo.EnumerateFiles().Where(f => supportedFileTypes.Contains(f.Extension.ToLower())).ToList();
        }
    }
}
