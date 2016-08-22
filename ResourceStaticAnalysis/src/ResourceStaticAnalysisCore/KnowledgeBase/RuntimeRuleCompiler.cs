/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Microsoft.ResourceStaticAnalysis.Core.Engine;

namespace Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase
{
    /// <summary>
    /// Helper class used internally by ResourceStaticAnalysis to compile binary rules from source code (*.cs) files.
    /// </summary>
    internal class RuntimeRuleCompiler
    {
        internal RuntimeRuleCompiler(RuleManager owner, IEnumerable<string> binaryRefs, IEnumerable<string> sourceRefs)
        {
            _owningManager = owner;
            _binaryReferences = new HashSet<string>(binaryRefs.Select(EngineConfig.ExpandVariables));
            _sourceReferences = new HashSet<string>(sourceRefs.Select(EngineConfig.ExpandVariables));
            _codeProvider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });

            _tempAssemblyDirectory = owner._owningEngine._engineConfiguration.TempAssemblyDirectory;
            if (String.IsNullOrEmpty(_tempAssemblyDirectory))
            {
                _tempAssemblyDirectory = Environment.GetEnvironmentVariable("TEMP");
            }
            if (!Directory.Exists(_tempAssemblyDirectory))
            {
                try
                {
                    if (!Path.IsPathRooted(_tempAssemblyDirectory))
                        throw new DirectoryNotFoundException("Incorrect path to temporary assembly directory provided. Path must be rooted path.");
                    Directory.CreateDirectory(_tempAssemblyDirectory);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture, "Error initializing {0}. Could not create a directory to store temporary assemblies: {1}", GetType().FullName, _tempAssemblyDirectory),
                        e);
                }
            }

            // compile all source references into one assembly in a temporary location on disk
            if (_sourceReferences.Count <= 0) return;
            _tempAssemblyPath = Path.Combine(_tempAssemblyDirectory, "ResourceStaticAnalysisTempAssembly.dll");
            var compiledReferences = CompileToAssembly(_sourceReferences.First(), _tempAssemblyPath, _sourceReferences.Skip(1).ToArray());
            if (compiledReferences == null)
                throw new RuntimeRuleCompilerException(String.Format(CultureInfo.CurrentCulture, "Failed to compile required source references ({0}) into a temporary assembly: {1}",
                                                           String.Join(",", _sourceReferences.ToArray()),
                                                           _tempAssemblyPath));
            _binaryReferences.Add(compiledReferences.Location);
        }

        /// <summary>
        /// .Net framework object that enables to compile binary code from source C# code.
        /// </summary>
        private readonly CSharpCodeProvider _codeProvider;
        /// <summary>
        /// Rule manager that owns this instance of <see cref="RuntimeRuleCompiler"/>.
        /// </summary>
        private readonly RuleManager _owningManager;
        /// <summary>
        /// Path to a directory where compiled assemblies will be stored.
        /// </summary>
        private readonly string _tempAssemblyDirectory;
        /// <summary>
        /// Path to a temporary assembly generated from source references. to be cleaned up
        /// </summary>
        private readonly string _tempAssemblyPath;
        /// <summary>
        /// Using HashSet instead of List allows to eliminate duplicate references.
        /// </summary>
        private readonly HashSet<string> _binaryReferences;
        /// <summary>
        /// Adds a reference to a binary file (dll) that is required for successful compilation of Rule source files (*.cs).
        /// </summary>
        /// <param name="path">Binary dll location (absolute or relative)</param>
        internal void AddBinaryReference(string path)
        {
            _binaryReferences.Add(path);
        }
        /// <summary>
        /// Using HashSet instead of List allows to eliminate duplicate references.
        /// </summary>
        private readonly HashSet<string> _sourceReferences;
        /// <summary>
        /// Adds an additional source code file that is required for successful compilation of the specified rule main file.
        /// </summary>
        /// <param name="path">Path to the additional source code.</param>
        internal void AddSourceReference(string path)
        {
            _sourceReferences.Add(path);
        }

        /// <summary>
        /// Compiles a rule on the fly to an Assembly
        /// </summary>
        /// <param name="sourceCode">Path to the rule.cs file that we want to compile</param>
        /// <param name="targetName">target file</param>
        /// <returns>Assembly</returns>
        internal Assembly CompileToAssembly(string sourceCode, string targetName)
        {
            string targetPath = Path.Combine(_tempAssemblyDirectory, targetName);
            return CompileToAssembly(sourceCode, targetPath, null);
        }

        /// <summary>
        /// Compiles a rule on the fly to a .dll
        /// </summary>
        /// <param name="sourceCode">Path to the rule.cs file that we want to compile</param>
        /// <param name="targetName">target file</param>
        /// <param name="additionalSources">Additional files on which the sourceCode is dependent for successful compilation</param>
        internal Assembly CompileToAssembly(string sourceCode, string targetName, string[] additionalSources)
        {
            // check if target file exists and remove if necessary
            var absoluteCheckDllPath = Path.IsPathRooted(targetName) ? targetName : Path.Combine(Path.GetDirectoryName(sourceCode), targetName);
            if (File.Exists(absoluteCheckDllPath))
            {
                File.SetAttributes(absoluteCheckDllPath, FileAttributes.Normal);
                try
                {
                    File.Delete(absoluteCheckDllPath);
                }
                catch (UnauthorizedAccessException ex)
                {
                    #region Override the target name when when the file already exists
                    // When multiple configs with the same source name are passed into ResourceStaticAnalysis, it will lock the files
                    // thus the consecutive attempts to run ResourceStaticAnalysis.Run() will fail, because this compiler won't be able to generate the target dll another time.
                    
                    var fi = new FileInfo(absoluteCheckDllPath);
                    var alternateName = Path.GetRandomFileName();
                    var alternateFullPath = Path.Combine(fi.DirectoryName, alternateName);
                    alternateFullPath = Path.ChangeExtension(alternateFullPath, "dll");
                    Trace.TraceWarning(ex.Message);
                    Trace.TraceWarning("Target assembly file {0} already exists and cannot be deleted because it is in use. Using alternate file name to generate the binary: {1}", absoluteCheckDllPath, alternateFullPath);
                    absoluteCheckDllPath = alternateFullPath;
                    #endregion
                }
            }
            var parameters = new CompilerParameters(_binaryReferences.ToArray(), absoluteCheckDllPath);
            parameters.GenerateExecutable = false; //Do we want a physical exe?
            parameters.GenerateInMemory = false;
            parameters.TempFiles = new TempFileCollection(Path.GetTempPath(), false);
            // add folders where the compiler will look for assemblies
            parameters.CompilerOptions = String.Join(" ",
                (from path in _owningManager._owningEngine.AsimoAssemblyResolver.assemblyResolvePaths
                 select String.Format(CultureInfo.CurrentCulture, "/lib:\"{0}\"", path)).ToArray());

            var fileNamesToCompile = new HashSet<string> { sourceCode };

            #region Include optional additional sources
            if (additionalSources != null)
            {
                Array.ForEach(additionalSources, option => fileNamesToCompile.Add(option));
            }
            #endregion

            var results = _codeProvider.CompileAssemblyFromFile(parameters, fileNamesToCompile.ToArray());
            results.Errors.Cast<CompilerError>().ToList().ForEach(error => Trace.TraceWarning("RuntimeRuleCompiler error: [{0}]: {1}", error.ErrorNumber, error.ErrorText));
            if (results.Errors.HasErrors)
            {
                var errorList = new StringBuilder();
                foreach (CompilerError error in results.Errors)
                {
                    errorList.AppendLine(error.ErrorText);
                }
                Trace.TraceError("RuntimeRuleCompiler: Trying to compile [{0}] rule resulted in {1} compilation errors: {2}",
                    sourceCode,
                    results.Errors.Count,
                    errorList.ToString());
                return null;
            }
            Trace.TraceInformation("Compilation successful. Returning the assembly.");
            return results.CompiledAssembly;
        }
    }
}
