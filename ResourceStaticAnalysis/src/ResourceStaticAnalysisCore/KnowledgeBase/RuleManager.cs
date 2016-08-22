/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Output;
using DynamicExpression = Microsoft.ResourceStaticAnalysis.Core.Misc.DynamicExpression;

namespace Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase
{
    /// <summary>
    /// Object used for managing rules available to ResourceStaticAnalysis engine.
    /// It is responsible for registering rules, passing classification objects to rules,
    /// compiling rules from source code.
    /// </summary>
    public class RuleManager
    {
        /// <summary>
        /// Internally available constructor.
        /// </summary>
        /// <param name="owner">Engine that owns this instance of <see cref="RuleManager"/></param>
        internal RuleManager(Engine.ResourceStaticAnalysisEngine owner)
        {
            _owningEngine = owner;
            Initialize();
        }
        /// <summary>
        /// Runtime compiler used to compile RuleContainers
        /// </summary>
        RuntimeRuleCompiler _rrc;
        /// <summary>
        /// RuleManager initialization routine. Called from constructor.
        /// </summary>
        /// <remarks>
        /// Do NOT confuse this method with rule initialization.
        /// Rule initialization takes place when the manager raises <see cref="RulesInitializationEvent"/> event via InitializeRules().
        /// </remarks>
        private void Initialize()
        {
            //Needs to be created before adding rules, because they initialize themselves with this value.
            OutputWriter = new OutputStore(_owningEngine);
            _rrc = new RuntimeRuleCompiler(this, _owningEngine._engineConfiguration.BinaryReferences, _owningEngine._engineConfiguration.SourceReferences);
        }
        /// <summary>
        /// Engine thread 3 calls this method to raise <see cref="RulesInitializationEvent"/>.
        /// </summary>
        internal void OnRulesInitializationEvent()
        {
            if (RulesInitializationEvent != null)
                RulesInitializationEvent();
        }

        /// <summary>
        /// This is an event that is happening before the engine runs on any classification object.
        /// <para/>This event gives rules a chance to initialize context before being executed on the first classification object.
        /// </summary>
        public event Action RulesInitializationEvent;

        /// <summary>
        /// Engine thread 3 calls this method to raise <see cref="RulesCleanupEvent"/>.
        /// </summary>
        internal void OnRulesCleanupEvent()
        {
            if (RulesCleanupEvent != null)
                RulesCleanupEvent();
        }

        /// <summary>
        /// This is an event that is happening just after the engine runs on the last classification object.
        /// <para/>This event gives rules a chance to cleanup any static memory allocations
        /// (remember it is possible that ResourceStaticAnalysis, with its rules, may be loaded and run many times by a single exe.
        /// </summary>
        public event Action RulesCleanupEvent;

        /// <summary>
        /// This event is fired in the multi-thread scenario: each rule runs only on one thread - rules are not thread safe
        /// </summary>
        internal event ClassifyCollectionOfCOsEventHandler CollectionOfCOsForClassification;

        /// <summary>
        /// Number of rules that are currently processing objects.
        /// </summary>
        internal int _noOfRulesRunning;

        /// <summary>
        /// Invoke rule handler
        /// </summary>
        /// <param name="cos">Resource items which need to be analyzed</param>
        /// <param name="callback">AsyncCallback object</param>
        public void ClassifyObjectsAsynchronously(List<ClassificationObject> cos, AsyncCallback callback)
        {
            var ruleHandlers = CollectionOfCOsForClassification.GetInvocationList();
            _noOfRulesRunning = ruleHandlers.Length;

            if (ruleHandlers != null && ruleHandlers.Count() > 0)
            {
                foreach (ClassifyCollectionOfCOsEventHandler ruleHandler in ruleHandlers)
                {
                    ruleHandler.BeginInvoke(cos, callback, null);
                }
            }
        }

        /// <summary>
        /// Output writer that is responsible for writing each output of each rule to some persistent storage (such as log file).
        /// </summary>
        public OutputStore OutputWriter { get; private set; }

        internal readonly Engine.ResourceStaticAnalysisEngine _owningEngine;

        private readonly List<Rule> _rules = new List<Rule>();
        private bool AddRule(Rule rule)
        {
            //check if rule of the same type already exists. if yes, we will override it
            var index = _rules.FindIndex(r => r.GetType() == rule.GetType());
            if (index < 0)
            {
                _rules.Add(rule);
                return false;
            }
            _rules[index] = rule;
            return true;
        }
        /// <summary>
        /// Merges output from all Rules registered with this manager.
        /// Uses LINQ so actual merge happens on enumeration
        /// </summary>
        public IEnumerable<OutputEntryForOneRule> OutputFromAllRules
        {
            get
            {
                return
                    from rule in _rules
                    from entry in rule.Output
                    select entry;
            }
        }
        /// <summary>
        /// Loads rules from a container: either a module (.dll) or source file (.cs).
        /// Also compiles a filtering expression, if provided for a rule
        /// </summary>
        /// <param name="containerDef">Container definition: provides path to container (.dll or .cs) and an optional filtering expression
        /// to be applied to all the rules
        /// </param>
        public void LoadRulesFromContainer(RuleContainer containerDef)
        {
            Exception containerException = null;
            string exceptionMessage = null;

            if (containerDef.EnabledRules.Count > 0 && containerDef.DisabledRules.Count > 0)
            {
                throw new RuleContainerException("EnabledRules and DisabledRules should not co-exist in a RuleContainer");
            }

            var pathToContainer = containerDef.PathToFile;
            if (!Path.IsPathRooted(pathToContainer))
            {
                pathToContainer = Path.Combine(Path.GetDirectoryName(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase), pathToContainer);
            }
            //if the file doesn't exists try looking in the GAC
            if (!File.Exists(pathToContainer))
            {
                pathToContainer = Assembly.Load(containerDef.PathToFile).CodeBase;
            }
            // if the container does not specify a filtering expression, we look for the default one in engine config
            if (containerDef.FilteringExpressionMethod == null && String.IsNullOrEmpty(containerDef.FilteringExpression))
            {
                containerDef.FilteringExpression = _owningEngine._engineConfiguration.DefaultFilteringExpression;
            }

            try
            {
                Assembly ruleModule = null;
                switch (containerDef.ContainerType)
                {
                    case RuleContainerType.Source:
                        // Need to compile source file
                        ruleModule = CompileRuleSourceFile(pathToContainer);
                        break;
                    case RuleContainerType.Module:
                        ruleModule = Assembly.LoadFrom(pathToContainer);
                        break;
                }

                if (containerDef.FilteringExpressionMethod != null)
                {
                    AddRulesFromAssembly(ruleModule, containerDef.FilteringExpressionMethod, containerDef.DisabledRules, containerDef.WorkingFolder, containerDef.EnabledRules);
                }
                else
                {
                    AddRulesFromAssembly(ruleModule, containerDef.FilteringExpression, containerDef.DisabledRules, containerDef.WorkingFolder, containerDef.EnabledRules);
                }

                //If there is any Rule filtering specified set that here.
                if (containerDef.PerRuleFilteringExpressionMethods != null)
                {
                    foreach (var ruleFilter in containerDef.PerRuleFilteringExpressionMethods)
                    {
                        var rule = this._rules.FirstOrDefault(r => r.GetType().FullName == ruleFilter.RuleName);
                        if (rule != null)
                        {
                            if (ruleFilter.ReplaceExisting)
                            {
                                rule.SetFilteringExpression(ruleFilter.FilteringExpressionMethod);
                            }
                            else
                            {
                                rule.AddFilteringExpression(ruleFilter.FilteringExpressionMethod);
                            }
                        }
                    }
                }
            }
            catch (TypeInitializationException e)
            {
                exceptionMessage = String.Format(CultureInfo.CurrentCulture, "An error occurred while trying to initialize rule objects from {0}", pathToContainer);
                containerException = e;
            }
            catch (FileNotFoundException e)
            {
                exceptionMessage = String.Format(CultureInfo.CurrentCulture, "Engine configuration error. File '{0}' could not be found.", pathToContainer);
                containerException = e;
            }
            catch (MissingFieldException e)
            {
                exceptionMessage = "Could not load rules from rule container correctly, please check that required fields exist.";
                containerException = e;
            }
            catch (Exception e)
            {
                exceptionMessage = String.Format(CultureInfo.CurrentCulture, "Could not load rules from rule container {0}.", pathToContainer);
                containerException = e;
            }
            finally
            {
                if (containerException != null)
                {
                    throw new RuleContainerException(exceptionMessage, containerException);
                }
            }

        }

        /// <summary>
        /// Attempts to compile the source code and returns the resulting Assembly object
        /// </summary>
        /// <param name="ruleCSharpSourceCodeFile">Path to a C# source code file with the code.</param>
        private Assembly CompileRuleSourceFile(string ruleCSharpSourceCodeFile)
        {
            var targetAssemblyName = Path.ChangeExtension(Path.GetFileName(ruleCSharpSourceCodeFile), "dll");

            var ruleModule = _rrc.CompileToAssembly(ruleCSharpSourceCodeFile, targetAssemblyName);
            if (ruleModule == null)
            {
                Trace.TraceError("Failed to compile {0} source code file for rules.", ruleCSharpSourceCodeFile);
                throw new RuntimeRuleCompilerException(String.Format(CultureInfo.CurrentCulture, "Failed to compile {0} source code file for rules.", ruleCSharpSourceCodeFile));
            }
            return ruleModule;
        }

        /// <summary>
        /// Identifies any public classes looking like Rule implementation, calls their appropriate constructors,
        /// and registers the constructed objects with this RuleManager.
        /// </summary>
        /// <param name="ruleModule">Either the module loaded from a rule.dll file on disk, or an Assembly compiled from the source code</param>
        /// <param name="filteringExpression">Filtering expression to be applied to objects before processing. Empty string means all objects should be
        /// processed - no filtering.</param>
        /// <param name="disabledRules">Collection of rules within the ruleModule that should be disabled (not run).</param>
        /// <param name="workingFolder">Working folder for the rule, where it should pickup any external configuration or required files for executing.</param>
        private void AddRulesFromAssembly(Assembly ruleModule, string filteringExpression, IEnumerable<string> disabledRules, string workingFolder, IEnumerable<string> enabledRules)
        {
            if (enabledRules != null && enabledRules.Any() && disabledRules != null && disabledRules.Any())
            {
                throw new RuleContainerException("EnabledRules and DisabledRules cannot co-exist in a RuleContainer");
            }

            foreach (var newRule in ruleModule.GetExportedTypes().Where(r => r.IsClass && r.IsSubclassOf(typeof(Rule)) && !r.IsAbstract &&
                (disabledRules == null || !disabledRules.Contains(r.FullName)) &&
                ((enabledRules == null || !enabledRules.Any()) || enabledRules.Contains(r.FullName))))
            {
                // create instance of rule
                var rule = (Rule)Activator.CreateInstance(newRule, new object[] { this });
                // check if the rule is applicable to this instance of the Engine
                bool knownCoType = false;
                foreach (var coType in _owningEngine._coTypes)
                {
                    if (rule.TypeOfClassificationObject.Equals(coType.Value))
                    {
                        knownCoType = true;
                        break;
                    }
                }

                if (knownCoType)
                {
                    COExpression<ClassificationObject> filteringMethod = null;
                    if (false == String.IsNullOrEmpty(filteringExpression))
                    {
                        filteringMethod = GetFilteringMethod(filteringExpression, rule);
                    }
                    if (filteringMethod != null)
                    {
                        rule.AddFilteringExpression(filteringMethod);
                    }

                    if (!string.IsNullOrWhiteSpace(workingFolder))
                    {
                        rule.WorkingFolder = workingFolder;
                    }

                    AddRule(rule);
                    Trace.TraceInformation("Adding rule to RuleManager: {0}", rule);
                }
                else
                {
                    Trace.TraceInformation("Rule {0} does not support any ClassificationObject types registered with the engine", rule);
                }
            }
        }

        /// <summary>
        /// Identifies any public classes looking like Rule implementation, calls their appropriate constructors,
        /// and registers the constructed objects with this RuleManager.
        /// <param name="ruleModule">Either the module loaded from a rule.dll file on disk, or an Assembly compiled from the source code.</param>
        /// <param name="filteringExpressionMethod">Filtering expression method to be applied to objects before processing.</param>
        /// <param name="disabledRules">Collection of rules within the ruleModule that should be disabled (not run).</param>
        /// <param name="workingFolder">Working folder for the rule, where it should pickup any external configuration or required files for executing.</param>
        private void AddRulesFromAssembly(Assembly ruleModule, COExpression<ClassificationObject> filteringExpressionMethod, IEnumerable<string> disabledRules, string workingFolder, IEnumerable<string> enabledRules)
        {
            if (enabledRules != null && enabledRules.Any() && disabledRules != null && disabledRules.Any())
            {
                throw new RuleContainerException("EnabledRules and DisabledRules cannot co-exist in a RuleContainer");
            }

            foreach (var newRule in ruleModule.GetExportedTypes().Where(r => r.IsClass && r.IsSubclassOf(typeof(Rule)) && !r.IsAbstract &&
                (disabledRules == null || !disabledRules.Contains(r.FullName)) && 
                ((enabledRules == null || !enabledRules.Any()) || enabledRules.Contains(r.FullName))))
            {
                // create instance of rule
                var rule = (Rule)Activator.CreateInstance(newRule, new object[] { this });

                // check if the rule is applicable to this instance of the Engine
                bool knownCoType = false;
                foreach (var coType in _owningEngine._coTypes)
                {
                    if (rule.TypeOfClassificationObject.Equals(coType.Value))
                    {
                        knownCoType = true;
                        break;
                    }
                }

                if (knownCoType)
                {
                    if (filteringExpressionMethod != null)
                    {
                        rule.AddFilteringExpression(filteringExpressionMethod);
                    }

                    if (!string.IsNullOrWhiteSpace(workingFolder))
                    {
                        rule.WorkingFolder = workingFolder;
                    }

                    AddRule(rule);
                    Trace.TraceInformation("Adding rule to RuleManager: {0}", rule);
                }
                else
                {
                    Trace.TraceInformation("Rule {0} does not support any ClassificationObject types registered with the engine", rule);
                }

            }
        }

        #region Compile filtering expressions from string
        /// <summary>
        /// Caches the compiled filtering expressions defined in Engine Config
        /// </summary>
        private readonly Dictionary<string, Dictionary<Type, COExpression<ClassificationObject>>> _filteringMethods = new Dictionary<string, Dictionary<Type, COExpression<ClassificationObject>>>();
        /// <summary>
        /// Used to control thread access to _filteringMethods
        /// </summary>
        private static readonly ReaderWriterLockSlim FilteringMethodLock = new ReaderWriterLockSlim();
        /// <summary>
        /// Compiles the filtering expression into a filtering method or returns a cached method if it has already been compiled
        /// </summary>
        /// <param name="filteringExpression">The lambda expression to be used for filtering.</param>
        private COExpression<ClassificationObject> GetFilteringMethod(string filteringExpression, Rule rule)
        {
            COExpression<ClassificationObject> ret;
            FilteringMethodLock.EnterUpgradeableReadLock();
            try
            {
                if (!_filteringMethods.ContainsKey(filteringExpression))
                {
                    FilteringMethodLock.EnterWriteLock();
                    try
                    {
                        _filteringMethods.Add(filteringExpression, new Dictionary<Type, COExpression<ClassificationObject>>());

                    }
                    finally
                    {
                        FilteringMethodLock.ExitWriteLock();
                    }
                }
                // need to compile the expression first as it's not cached
                if (!_filteringMethods[filteringExpression].TryGetValue(rule.TypeOfClassificationObject, out ret))
                {
                    ret = CompileFilteringExpression(filteringExpression, rule);
                    FilteringMethodLock.EnterWriteLock();
                    try
                    {
                        _filteringMethods[filteringExpression].Add(rule.TypeOfClassificationObject, ret);
                    }
                    finally 
                    {
                        FilteringMethodLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                FilteringMethodLock.ExitUpgradeableReadLock();
            }
            return ret;
        }
        private COExpression<ClassificationObject> CompileFilteringExpression(string filteringExpression, Rule rule)
        {
            LambdaExpression lambda = DynamicExpression.ParseLambda(rule.TypeOfClassificationObject, typeof(bool), filteringExpression);
            return rule.ExpressionCaster(lambda.Compile());
        }
        #endregion

        /// <summary>
        /// Attempts to compile the source code and register the compiled rule with RuleManager
        /// </summary>
        /// <param name="ruleCSharpSourceCodeFile">Source code file of your rule</param>
        public void LoadRulesFromSourceFile(string ruleCSharpSourceCodeFile)
        {
            var ruleModule = CompileRuleSourceFile(ruleCSharpSourceCodeFile);
            AddRulesFromAssembly(ruleModule, String.Empty, Enumerable.Empty<string>(), null, null);
        }

        /// <summary>
        /// Finds a registered rule with the specified full type name (including namespaces) and unregisters it
        /// from RuleManager
        /// </summary>
        /// <param name="ruleTypeName">Name of rule type to disable</param>
        public void DisableRule(string ruleTypeName)
        {
            //check if rule with the type name already exists. if yes, remove it
            var match = _rules.Find(rule => rule.GetType().FullName == ruleTypeName);
            if (match == null)
            {
                Trace.TraceWarning("RuleManager cannot disable rule {0}, because rule of this type has not been registered.", ruleTypeName);
            }
            else
            {
                _rules.Remove(match);
                match.UnregisterEvents();
                Trace.TraceInformation("RuleManager disabled rule {0}.", ruleTypeName);
            }
        }
        /// <summary>
        /// Number of rules currently registered with RuleManager
        /// </summary>
        public int RuleCount { get { return _rules.Count; } }

        #region RuleManager Performance counters
        /// <summary>
        /// Collection of all rule performance objects used to by all rules owned by RuleManager
        /// </summary>
        internal IEnumerable<Rule.RulePerformance> RulePerformanceSummary
        {
            get
            {
                return _rules.Select(r => r.MyPerformance);
            }
        }
        #endregion

    }

}
