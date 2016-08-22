/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.Core.Output;

namespace Microsoft.ResourceStaticAnalysis.Configuration
{
    /// <summary>
    /// Abstract class for executing the ResourceStaticAnalysis Engine.
    /// </summary>
    public abstract class ResourceStaticAnalysisConfig : MarshalByRefObject
    {
        /// <summary>
        /// ResourceStaticAnalysis Engine configuration.
        /// </summary>
        protected EngineConfig EngineConfiguration;

        /// <summary>
        /// Dictionary containing the Rules Configuration for each rule assembly.
        /// </summary>
        public Dictionary<String, ResourceStaticAnalysisRulesConfig> RulesConfiguration;

        /// <summary>
        /// Initialize the ResourceStaticAnalysisConfig class.
        /// </summary>
        /// <param name="folders">Collection of folder to look for rules to run in.</param>
        public virtual void Initialize(IEnumerable<String> folders)
        {
            if (folders == null || !folders.Any())
            {
                throw new ArgumentException("Value cannot be null or an empty collection", "folders");
            }

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            Trace.TraceInformation("Initialization of the Resource Static Analysis Engine started...");

            IEnumerable<RuleContainer> rules = this.CreateRuleContainers(folders);
            this.EngineConfiguration = this.CreateEngineConfig(rules);

            watch.Stop();
            Trace.TraceInformation(
                "Initialization of the Resource Static Analysis Engine completed in {0:00}:{1:00}:{2:00}.{3:00}",
                watch.Elapsed.Hours, watch.Elapsed.Minutes, watch.Elapsed.Seconds, watch.Elapsed.Milliseconds/10);
        }

        /// <summary>
        /// Execute the engine against the supplied data source object.
        /// </summary>
        /// <param name="dataSource">Datasource to validate.</param>
        /// <param name="outputRuleSummary">A value indicating if the rule summary should be logged to the output.</param>
        /// <param name="exceptions">Collection of exceptions that occurred during execution.</param>
        /// <returns>Collection of results.</returns>
        public virtual IEnumerable<OutputEntryForOneRule> Execute(object dataSource, bool outputRuleSummary,
            out List<Exception> exceptions)
        {
            ResourceStaticAnalysisEngine engine = null;
            List<Exception> exceptionsInternal = null;

            if (dataSource == null)
            {
                throw new ArgumentNullException("dataSource",
                    "The dataSource object to run the ResourceStaticAnalysis Engine on cannot be null.");
            }

            if (this.EngineConfiguration == null || this.EngineConfiguration.RuleContainers == null ||
                !this.EngineConfiguration.RuleContainers.Any())
            {
                throw new ArgumentException(
                    "The configuration for the ResourceStaticAnalysis Engine has not been set or no rules were added to the configuration to run.");
            }

            this.EngineConfiguration.DataSourcePkgs.FirstOrDefault().PrimaryDataSource.SetSourceLocation(dataSource);

            engine = new ResourceStaticAnalysisEngine(this.EngineConfiguration);
            engine.Monitor.LogExceptionDetails = true;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            Trace.TraceInformation("Starting Resource Static Analysis Engine...");

            if (engine.StartRun())
            {
                engine.WaitForJobFinish();
            }

            watch.Stop();
            Trace.TraceInformation("Resource Static Analysis Engine completed in {0:00}:{1:00}:{2:00}.{3:00}",
                watch.Elapsed.Hours, watch.Elapsed.Minutes, watch.Elapsed.Seconds, watch.Elapsed.Milliseconds / 10);

            if (engine.Monitor.NoOfExceptionsLogged > 0)
            {
                exceptionsInternal = engine.Monitor.ExceptionDetails.ToList();
            }

            engine.Cleanup();

            if (outputRuleSummary)
            {
                string perfResults = engine.Monitor.PrintRulePerformanceSummary();
                Trace.TraceInformation(perfResults);
            }

            exceptions = exceptionsInternal;
            return engine.CurrentRuleManager.OutputFromAllRules;
        }

        /// <summary>
        /// Creates the configuration to be used by the ResourceStaticAnalysis Engine.
        /// </summary>
        /// <param name="ruleContainers">Collection of rule containers to configure the ResourceStaticAnalysis Engine with.</param>
        /// <returns>EngineConfig.</returns>
        protected abstract EngineConfig CreateEngineConfig(IEnumerable<RuleContainer> ruleContainers);

        /// <summary>
        /// Create the collection of RuleContainers to configure ResourceStaticAnalysis Engine with.
        /// </summary>
        /// <param name="folders">Collection of folders to look for rules or configuration file to create the RuleContainers for.</param>
        /// <returns>Collection of RuleContainers.</returns>
        protected virtual IEnumerable<RuleContainer> CreateRuleContainers(IEnumerable<String> folders)
        {
            this.RulesConfiguration = this.LoadRuleConfiguration(folders);

            // Process all the rule assemblies found, creating the ResourceStaticAnalysis Engine Rule Containers for each entry and return the collection of containers.
            List<RuleContainer> ruleContainerCollection = new List<RuleContainer>();
            if (this.RulesConfiguration != null)
            {
                foreach (var entry in this.RulesConfiguration.Where(rc => rc.Value.Enabled)) //Only create RuleContainers for the assemblies where the config is enabled.
                {
                    RuleContainer ruleContainer = new RuleContainer(entry.Key, RuleContainerType.Module);
                    this.ProcessRuleConfig(entry.Value, ruleContainer);
                    ruleContainerCollection.Add(ruleContainer);
                }
            }

            return ruleContainerCollection;
        }

        /// <summary>
        /// Load the rule configuration for the require ResourceStaticAnalysis Engine rules.
        /// </summary>
        /// <param name="folders">The collection of folders to look for rule assemblies and configuration in.</param>
        /// <returns>Dictionary containing the path to each ResourceStaticAnalysis Engine assembly and it's corresponding configuration object.</returns>
        protected virtual Dictionary<String, ResourceStaticAnalysisRulesConfig> LoadRuleConfiguration(IEnumerable<String> folders)
        {
            //In a seperate appdomain - determine the assemblies that contain rules that should be run.
            using (AppDomainWorker<ResourceStaticAnalysisRulesLoader> appDomainWorker = new AppDomainWorker<ResourceStaticAnalysisRulesLoader>())
            {
                return appDomainWorker.Worker.GetRuleConfiguration<Rule>(folders);
            }
        }

        /// <summary>
        /// Process the ResourceStaticAnalysisRulesConfig to create the required filtering for the RuleContainer.
        /// </summary>
        /// <param name="rulesConfig">ResourceStaticAnalysisRulesConfig object to process.</param>
        /// <param name="ruleContainer">RuleContainer object to update with the filtering.</param>
        protected void ProcessRuleConfig(ResourceStaticAnalysisRulesConfig rulesConfig, RuleContainer ruleContainer)
        {
            if (rulesConfig == null || ruleContainer == null)
            {
                return;
            }

            ruleContainer.FilteringExpressionMethod = this.CreateFilteringExpression(rulesConfig.Cultures, rulesConfig.Projects, rulesConfig.FilteringExpressions);

            List<RuleFilteringInfo> ruleFilters = new List<RuleFilteringInfo>();
            foreach (ResourceStaticAnalysisRulesConfig.Rule rule in rulesConfig.Rules)
            {
                if (rule.Enabled)
                {
                    RuleFilteringInfo ruleFilteringInfo = this.CreateRuleFiltering(rule);
                    if (ruleFilteringInfo != null)
                    {
                        ruleFilters.Add(ruleFilteringInfo);
                    }
                }
                else
                {
                    ruleContainer.DisabledRules.Add(rule.Type);
                }
            }

            if (ruleFilters.Any())
            {
                ruleContainer.PerRuleFilteringExpressionMethods = ruleFilters;
            }

            if (!String.IsNullOrWhiteSpace(rulesConfig.WorkingFolder))
            {
                String workingFolder = Environment.ExpandEnvironmentVariables(rulesConfig.WorkingFolder);
                if (!Path.IsPathRooted(workingFolder) && !String.IsNullOrWhiteSpace(rulesConfig.PathToConfigFile))
                {
                    workingFolder = Path.Combine(Path.GetDirectoryName(rulesConfig.PathToConfigFile), workingFolder);
                }

                ruleContainer.WorkingFolder = Path.GetFullPath(workingFolder);
            }
        }

        /// <summary>
        /// Creates a RuleFilteringInfo object that describes any rule specific filtering that should be applied to a rule.
        /// </summary>
        /// <param name="rule">Rule configuration object.</param>
        /// <returns>RuleFilteringInfo.</returns>
        protected RuleFilteringInfo CreateRuleFiltering(ResourceStaticAnalysisRulesConfig.Rule rule)
        {
            var filteringExpressionMethod = this.CreateFilteringExpression(rule.Cultures, rule.Projects, rule.FilteringExpressions);

            RuleFilteringInfo ruleFilter = null;
            if (filteringExpressionMethod != null)
            {
                ruleFilter = new RuleFilteringInfo();
                ruleFilter.ReplaceExisting = rule.OverrideContainerFiltering;
                ruleFilter.RuleName = rule.Type;
                ruleFilter.FilteringExpressionMethod = filteringExpressionMethod;
            }

            return ruleFilter;
        }

        /// <summary>
        /// Create a Filtering Expression used to filter objects to not run on certain RuleContainers or Rules.
        /// </summary>
        /// <param name="cultures">Cultures.</param>
        /// <param name="projects">Projects.</param>
        /// <param name="filteringExpressions">FilteringExpresssions.</param>
        /// <returns>FilteringExpression.</returns>
        protected abstract COExpression<ClassificationObject> CreateFilteringExpression(
            ResourceStaticAnalysisRulesConfig.ValueList cultures, ResourceStaticAnalysisRulesConfig.ValueList projects,
            List<ResourceStaticAnalysisRulesConfig.FilteringExpression> filteringExpressions);
    }
}