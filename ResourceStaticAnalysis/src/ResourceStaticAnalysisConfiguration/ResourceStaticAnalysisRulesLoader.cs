/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.Configuration
{
    /// <summary>
    /// Class that loads ResourceStaticAnalysis Engine rules and configuration.
    /// </summary>
    public class ResourceStaticAnalysisRulesLoader : MarshalByRefObject
    {
        /// <summary>
        /// Gets a Collection of Rule assemblies and their corresponding configuration file within the supplied folders.
        /// </summary>
        /// <param name="folders">Collection of folders to look for rule assemblies and configuration files in.</param>
        /// <returns>Dictionary containing the path to the Rule assembly to run and it's corresponding configuation.</returns>
        public Dictionary<String, ResourceStaticAnalysisRulesConfig> GetRuleConfiguration<T>(IEnumerable<String> folders) where  T : Rule
        {
            Dictionary<String, ResourceStaticAnalysisRulesConfig> ruleConfiguration = new Dictionary<String, ResourceStaticAnalysisRulesConfig>(StringComparer.OrdinalIgnoreCase);

            // Ensure we have expanded any Environment Variables in the folders.
            folders = folders.Select(f => Path.GetFullPath(Environment.ExpandEnvironmentVariables(f)));

            // First check for the existence of config files in the specified folders.
            List<String> configFiles = new List<String>();
            foreach (String folder in folders)
            {
                try
                {
                    configFiles.AddRange(Directory.EnumerateFiles(folder, String.Format("*{0}", ResourceStaticAnalysisRulesConfig.ConfigExtension),
                        SearchOption.TopDirectoryOnly));
                }
                catch (DirectoryNotFoundException ex)
                {
                    Trace.TraceError(ex.Message);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                }
            }

            // Load each config and determine if the path to the rule assembly specified in the config
            // is valid and if the assembly contains valid rules.
            foreach (String configFilePath in configFiles)
            {
                ResourceStaticAnalysisRulesConfig ruleConfig = ResourceStaticAnalysisRulesConfig.Load(configFilePath);
                if (ruleConfig != null && !String.IsNullOrWhiteSpace(ruleConfig.Path))
                {
                    String ruleAssemblyPath = ruleConfig.GetPathToAssembly();
                    ruleAssemblyPath = System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(ruleAssemblyPath));

                    ResourceStaticAnalysisRulesConfig existingConfig;
                    if (ruleConfiguration.TryGetValue(ruleAssemblyPath, out existingConfig))
                    {
                        Trace.TraceInformation(
                            "The configuration file '{0}' for assembly '{1}' will not be used because a previous configuration file " +
                            "'{2}' for that assembly has already been loaded.", ruleConfig.PathToConfigFile,
                            ruleAssemblyPath, existingConfig.PathToConfigFile);
                        continue;
                    }

                    List<ResourceStaticAnalysisRulesConfig.Rule> rulesCollection;
                    if (this.IsValidRuleAssembly<T>(ruleAssemblyPath, out rulesCollection))
                    {
                        foreach (ResourceStaticAnalysisRulesConfig.Rule rule in rulesCollection)
                        {
                            ResourceStaticAnalysisRulesConfig.Rule existingRule = ruleConfig.Rules.SingleOrDefault(r => rule.Type == r.Type);
                            if (existingRule != null)
                            {
                                existingRule.Name = rule.Name;
                                existingRule.Description = rule.Description;
                                existingRule.Category = rule.Category;
                            }
                            else
                            {
                                ruleConfig.Rules.Add(rule);
                            }
                        }

                        ruleConfiguration.Add(ruleAssemblyPath, ruleConfig);
                        Trace.TraceInformation(
                            "Resource Static Analysis Rule Assembly '{0}' with configuration File '{1}' has been included in the in the collection of rule containers to execute.",
                            ruleAssemblyPath, ruleConfig.PathToConfigFile);
                    }
                    else
                    {
                        Trace.TraceInformation("The assembly '{0}' specified in the configuration file '{1}' is not a valid Resource Static Analysis rule assembly.",
                            ruleAssemblyPath, ruleConfig.PathToConfigFile);
                    }
                }
            }

            // Get all the dll files under the required folders and if we don't have a config 
            // that specifies the dll - and if it is a valid rule assembly, use it.
            ICollection<String> dllFilesAlreadyAdded = ruleConfiguration.Keys;
            List<String> dllFiles = new List<String>();
            foreach (String folder in folders)
            {
                try
                {
                    dllFiles.AddRange(Directory.EnumerateFiles(folder, "*.dll", SearchOption.TopDirectoryOnly).Where(
                        file => !dllFilesAlreadyAdded.Contains(file, StringComparer.OrdinalIgnoreCase)));
                }
                catch (DirectoryNotFoundException ex)
                {
                    Trace.TraceInformation(ex.Message);
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation(ex.Message);
                }
            }

            foreach (String potentialFile in dllFiles)
            {
                List<ResourceStaticAnalysisRulesConfig.Rule> rulesCollection;
                if (this.IsValidRuleAssembly<T>(potentialFile, out rulesCollection))
                {
                    ruleConfiguration.Add(potentialFile, new ResourceStaticAnalysisRulesConfig(potentialFile) { Rules = rulesCollection });
                    Trace.TraceInformation(
                        "Resource Static Analysis Rule Assembly '{0}' has been included without any additional configuration.",
                        potentialFile);
                }
            }

            return ruleConfiguration;
        }

        /// <summary>
        /// Gets a value indicating if the assembly at the supplied path is a valid ResourceStaticAnalysis rule assembly.
        /// </summary>
        /// <param name="pathToAssembly">The path to the assembly to check.</param>
        /// <param name="rulesCollection">The collection of the full names of each of the rules in the assembly.</param>
        /// <returns>True if the assembly is a valid ResourceStaticAnalysis rule assembly, false otherwise.</returns>
        private bool IsValidRuleAssembly<T>(String pathToAssembly, out List<ResourceStaticAnalysisRulesConfig.Rule> rulesCollection)
            where T : Rule
        {
            rulesCollection = new List<ResourceStaticAnalysisRulesConfig.Rule>();
            bool returnValue = false;
            if (!String.IsNullOrWhiteSpace(pathToAssembly) && File.Exists(pathToAssembly))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(pathToAssembly);

                    var types = assembly.GetExportedTypes().Where((type) => type.IsClass && type.IsSubclassOf(typeof(Rule)) && !type.IsAbstract);
                    if (types.Any())
                    {
                        foreach (Type ruleType in types)
                        {
                            ResourceStaticAnalysisRulesConfig.Rule rule = new ResourceStaticAnalysisRulesConfig.Rule(ruleType.FullName);

                            T ruleInstance = FormatterServices.GetUninitializedObject(ruleType) as T;
                            rule.Name = ruleInstance.Name ?? ruleType.Name;
                            rule.Description = String.IsNullOrWhiteSpace(ruleInstance.Description) ? ruleType.FullName : ruleInstance.Description;
                            rule.Category = String.IsNullOrWhiteSpace(ruleInstance.Category) ? "(None)" : ruleInstance.Category;

                            rulesCollection.Add(rule);
                        }

                        returnValue = true;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                }
            }

            return returnValue;
        }
    }
}