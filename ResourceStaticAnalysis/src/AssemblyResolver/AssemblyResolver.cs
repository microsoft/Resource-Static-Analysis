/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Practices.AssemblyManagement
{
    /// <summary>
    /// Allows runtime assembly resolution in current AppDomain in your application.
    /// </summary>
    public class AssemblyResolver
    {
        /// <summary>
        /// Initialize Assembly Resolver in the current domain on the current thread.
        /// </summary>
        public void Init()
        {
            Init(null);
        }

        /// <summary>
        /// Initialize Assembly Resolver in the current domain on the current thread.
        /// </summary>
        /// <param name="paths">Paths to search when resolving assemblies. These are added to the set of existing paths, defined in app config.</param>
        public void Init(params string[] paths)
        {
            if (paths != null && paths.Length > 0)
            {
                lock (assemblyResolvePaths)
                {
                    Array.ForEach(paths, p => assemblyResolvePaths.Add(Environment.ExpandEnvironmentVariables(p)));
                }
            }

            lock (AppDomain.CurrentDomain)
            {
                AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
            }
        }

        /// <summary>
        /// Paths to search for assemblies
        /// </summary>
        public readonly HashSet<string> assemblyResolvePaths;

        /// <summary>
        /// Creates an instance of AssemblyResolver that is attached to the current AppDomain.
        /// After constructing, call Init() to register the specified paths in assembly resolver to allow your application to find assemblies in the provided locations.
        /// </summary>
        /// <param name="paths">Paths where assemblies need to be searched for.</param>
        public AssemblyResolver(params string[] paths)
        {
            assemblyResolvePaths = new HashSet<string>();
            if (paths != null)
            {
                foreach (string s in paths)
                {
                    assemblyResolvePaths.Add(Environment.ExpandEnvironmentVariables(s));
                }
            }
        }

        /// <summary>
        /// Handles AssemblyResolve events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="eventArgs">Arguments of the AssemblyResolve event.</param>
        /// <returns><see cref="Assembly"/> object that resolves the assembly reference.</returns>
        private Assembly HandleAssemblyResolve(object sender, ResolveEventArgs eventArgs)
        {
            try
            {
                Assembly assembly;
                // First, check if Assembly is already loaded in the current domain
                lock (AppDomain.CurrentDomain)
                {
                    assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == eventArgs.Name);
                    if (assembly != null)
                    {
                        return assembly;
                    }
                    // Assembly has not been loaded, search for assembly in pre-defined search paths
                    AssemblyName unresolvedAssemblyName = new AssemblyName(eventArgs.Name);
                    foreach (string path in assemblyResolvePaths)
                    {
                        string assemblyFileName = Path.Combine(
                            Environment.ExpandEnvironmentVariables(path),
                            unresolvedAssemblyName.Name + ".dll");
                        if (File.Exists(assemblyFileName))
                        {
                            assembly = Assembly.LoadFrom(assemblyFileName);

                            AssemblyName candidate = assembly.GetName();

                            if (
                                // Names of the two assemblies are the same
                                unresolvedAssemblyName.Name.Equals(candidate.Name)
                                &&
                                // Version of the candidate is newer or same
                                unresolvedAssemblyName.Version <= candidate.Version
                                )
                            {
                                return assembly;
                            }
                        }
                    }
                }
                return null;
            }
            finally { }
        }
    }

    static class Extensions
    {
        /// <summary>
        /// Adds the specified <paramref name="item"/> string to the set, making it quoted.
        /// <para/>For example: if item is 'one', it will be quoted as "one", then added to the set.
        /// </summary>
        /// <param name="item">String which should be surrounded by quotes (") and added to the set.</param>
        public static void AddQuoted(this HashSet<string> collection, string item)
        {
            string quotedValue = String.Format(CultureInfo.CurrentCulture, "\"{0}\"", item);
            collection.Add(quotedValue);
        }
    }
}
