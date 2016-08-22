/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Misc;
using Microsoft.ResourceStaticAnalysis.Core.Output;

namespace Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase
{
    public abstract class Rule
    {
        private string _workingFolder;

        protected Rule(RuleManager owner)
        {
            //this.filteringExpressionMethod = filteringExpressionMethod ?? new COExpression<ClassificationObject>(co => true);
            Initialize(owner);
        }

        private void Initialize(RuleManager owner)
        {
            OwningManager = owner;
            RegisterEvents();
            currentOutputWriter = owner.OutputWriter;
            // create a performance counter
            MyPerformance = new RulePerformance(this.GetType().FullName);
        }

        /// <summary>
        /// Event called by rule directly before the execution of the Run() method.
        /// Created to allow performance tracking by RuleManager.
        /// </summary>
        public event EventHandler RuleStartsProcessingObject;

        /// <summary>
        /// Event called by rule directly after the execution of the Run() method.
        /// Created to allow performance tracking by RuleManager.
        /// </summary>
        public event StopPerfMeasurement RuleFinishedProcessingObject;

        public delegate TimeSpan StopPerfMeasurement(object sender, EventArgs args);

        /// <summary>
        /// Called by Rule's constructor.
        /// This call makes the Rule aware of events fired by RuleManager, thus making the rule active.
        /// </summary>
        public void RegisterEvents()
        {
            OwningManager.CollectionOfCOsForClassification += CollectionOfCOsForClassificationEventHandler;

            OwningManager.RulesInitializationEvent += Init;
            OwningManager.RulesCleanupEvent += Cleanup;
        }

        /// <summary>
        /// May be called by RuleManager when deregistering the rule from the manager.
        /// Effectively this unsubscribes the rule from events fired by RuleManager,
        /// thus making the rule inactive.
        /// </summary>
        public void UnregisterEvents()
        {
            OwningManager.CollectionOfCOsForClassification -= CollectionOfCOsForClassificationEventHandler;

            OwningManager.RulesInitializationEvent -= Init;
            OwningManager.RulesCleanupEvent -= Cleanup;
        }

        /// <summary>
        /// type of Classification Object the rule applies to.
        /// this is used as a temp solution for rules to not look at objects of other types
        /// </summary>
        public abstract Type TypeOfClassificationObject { get; }

        /// <summary>
        /// Default filtering expression allows all objects.
        /// </summary>
        protected readonly List<COExpression<ClassificationObject>> filteringExpressionMethods = new List<COExpression<ClassificationObject>>();
        
        /// <summary>
        /// Returns true if the filtering expressions for this rule match true for the given CO.
        /// </summary>
        /// <param name="co"></param>
        /// <returns></returns>
        private bool IsCOFiltered(ClassificationObject co)
        {
            return filteringExpressionMethods.TrueForAll(filter => filter.Invoke(co));
        }

        internal List<OutputEntryForOneRule> Output { get; private set; }

        private void CollectionOfCOsForClassificationEventHandler(IEnumerable<ClassificationObject> cos)
        {
            Output = new List<OutputEntryForOneRule>();
            var startTime = DateTime.Now;
            Trace.TraceInformation("{0} starting at {1}", this.GetType().FullName, startTime.ToString());
            try
            {
                foreach (var co in cos)
                {
                    var outputForRule = ClassifyOneObject(co);
                    if (!Object.ReferenceEquals(outputForRule, null) && outputForRule.Result)
                    {
                        Output.Add(outputForRule);
                    }
                }
            }
            finally
            {
                // let RuleManager know that this rule has finished processing.
                System.Threading.Interlocked.Decrement(ref OwningManager._noOfRulesRunning);
                Trace.TraceInformation("{0} finished. Time: {1}", this.GetType().FullName, DateTime.Now.Subtract(startTime).ToString());

                double pConformance = (double)(OwningManager.RuleCount - OwningManager._noOfRulesRunning) / (double)OwningManager.RuleCount;
                string percentComplete = pConformance.ToString("P");
                Trace.TraceInformation(percentComplete + " in current Domain. ");
            }
        }

        /// <summary>
        /// This method will be called by the engine (via RuleManager) for every
        /// <see cref="ClassificationObject"/> that enters the engine's processing pipe.
        /// NOTE: by design, one rule can only be processing one CO at a time! in multi thread scenario the same rule
        /// must not run on more than 1 thread!
        /// </summary>
        /// <param name="co">ClassificationObject to classify</param>
        private OutputEntryForOneRule ClassifyOneObject(ClassificationObject co)
        {
            try
            {
                if (this.TypeOfClassificationObject != co.GetType())
                    return null;

                // Skip COs that do not match the filtering expression
                if (!this.IsCOFiltered(co))
                    return null;

                _currentCO = co;

                currentOutputEntry = new OutputEntryForOneRule { Rule = this, Result = false, CO = co };

                if (RuleStartsProcessingObject != null)
                    RuleStartsProcessingObject(this, null);

                MyPerformance.StartMeasuring();

                Run();

                // stopping perf measuring in the finally block
                if (RuleFinishedProcessingObject != null)
                    RuleFinishedProcessingObject(this, null);

                if (currentOutputEntry != null)
                {
                    currentOutputEntry.SetTrueOrFalse();
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (StackOverflowException)
            {
                throw;
            }
            catch (Exception e)
            {
                Trace.TraceInformation("Error while executing rule {0} on ClassificationObject: {1}.\n{2}",
                    this.GetType().FullName,
                    co.ToString(),
                    e.GetExceptionDetails(false));
                this.OwningManager._owningEngine.Monitor.LogException(e);
            }
            finally
            {
                // stoping performance measurment
                MyPerformance.StopMeasuring();
            }

            return currentOutputEntry;
        }

        /// <summary>
        /// Perform checking and log result (MessageCreator constructor)
        /// </summary>
        /// <param name="exp">Lambda expression</param>
        /// <param name="mc">String append message creator</param>
        /// <param name="severity"></param>
        /// <returns></returns>
        protected internal bool _AgnosticCheckandLog(COExpression<ClassificationObject> exp, MessageCreator mc, CheckSeverity severity)
        {
            bool result;
            string message;
            ExecutingRuleException ruleException = null;
            try
            {
                result = exp(_currentCO);
                message = result ? mc.GetFullMessage() : mc.GetBaseMessage();
            }
            catch (Exception e)
            {
                if (e is OutOfMemoryException || e is StackOverflowException)
                    throw;
                result = false;
                try
                {
                    message = mc.GetBaseMessage();
                }
                catch
                {
                    message = String.Empty;
                    throw;
                }
                message = String.Format(CultureInfo.CurrentCulture, "Rule :{0}, Check() call: \"{1}\" threw an exception.",
                    this.GetType().Name, message);
                ruleException = new ExecutingRuleException(message, e);
            }
            if (!String.IsNullOrEmpty(message))
            {
                message = Engine.ResourceStaticAnalysisEngine.StringCache.Intern(message);
            }
            else
            {
                message = String.Empty;
            }
            OutputItem oi = new OutputItem { Message = message, Result = result, Severity = severity };
            currentOutputEntry.AddItem(oi);
            if (ruleException != null)
                throw ruleException;
            return oi.Result;
        }
        
        /// <summary>
        /// Perform checking and log result (String constructor)
        /// </summary>
        /// <param name="exp">Lambda expression</param>
        /// <param name="message">string representation of the message</param>
        /// <param name="severity">CheckSeverity</param>
        /// <returns></returns>
        protected internal bool _AgnosticCheckandLog(COExpression<ClassificationObject> exp, ref string message, CheckSeverity severity)
        {
            bool result;
            ExecutingRuleException ruleException = null;
            try
            {
                result = exp(_currentCO);
            }
            catch (Exception e)
            {
                if (e is OutOfMemoryException || e is StackOverflowException)
                    throw;
                result = false;
                message = String.Format(CultureInfo.CurrentCulture, "Rule :{0}, Check() call: \"{1}\" threw an exception.",
                    this.GetType().Name, message);
                ruleException = new ExecutingRuleException(message, e);
            }
            if (!String.IsNullOrEmpty(message))
            {
                message = Engine.ResourceStaticAnalysisEngine.StringCache.Intern(message);
            }
            else
            {
                message = String.Empty;
            }
            OutputItem oi = new OutputItem { Message = message, Result = result, Severity = severity };
            currentOutputEntry.AddItem(oi);
            if (ruleException != null)
                throw ruleException;
            return oi.Result;
        }

        protected abstract void Run();

        protected RuleManager OwningManager;

        private ClassificationObject _currentCO;

        /// <summary>
        /// Current Classification Object being processed by the rule
        /// </summary>
        public ClassificationObject CurrentCO
        {
            get
            {
                return this._currentCO;
            }
        }

        /// <summary>
        /// This delegate is used to cast Filtering Expressions of type
        /// Func&lt;T:ClassificationOjbect, bool&gt; to Func&lt;ClassificationObject, bool&gt;
        /// </summary>
        /// <returns></returns>
        public abstract Func<object, COExpression<ClassificationObject>> ExpressionCaster { get; }

        #region Filtering expressions
        /// <summary>
        /// Compiles the filtering expression into a function and merges this function with any existing filetring expression
        /// using AND operator. This can be used in a constructor in order to add any filtering conditions in addition to existing ones.
        /// </summary>
        /// <param name="filteringExpression">A lambda filtering expression valid for the type of Classification Object supported by the rule.</param>
        public void AddFilteringExpression(COExpression<ClassificationObject> filteringExpression)
        {
            AddOrReplaceFilteringExpression(filteringExpression, false);
        }

        /// <summary>
        /// Compiles the filtering expression into a function and replaces any existing filetring expression.
        /// This can be used in a constructor in order to replace filtering conditions for the rule.
        /// </summary>
        /// <param name="filteringExpression">A lambda filtering expression valid for the type of Classification Object supported by the rule.</param>
        public void SetFilteringExpression(COExpression<ClassificationObject> filteringExpression)
        {
            AddOrReplaceFilteringExpression(filteringExpression, true);
        }

        private void AddOrReplaceFilteringExpression(COExpression<ClassificationObject> filteringExpression, bool replace)
        {
            if (replace)
            {
                this.filteringExpressionMethods.Clear();
            }
            if (filteringExpression != null)
            {
                this.filteringExpressionMethods.Add(filteringExpression);
            }
        }
        #endregion

        private OutputStore currentOutputWriter;

        private OutputEntryForOneRule currentOutputEntry;

        private static readonly OutputItem ANDOperator = new OutputItem { ItemType = OPERAND.AND };

        private static readonly OutputItem OROperator = new OutputItem { ItemType = OPERAND.OR };

        /// <summary>
        /// Override this method in your class if your Rule requires initialization code.
        /// </summary>
        /// <remarks>
        /// This method will be called on your rule object once before any call to Run() is made.
        /// </remarks>
        protected virtual void Init() { }

        /// <summary>
        /// Override this method in your class if your Rule requires cleanup code.
        /// </summary>
        /// <remarks>
        /// This method will be called on your rule object once after the last call to Run() and before termination of the engine.
        /// </remarks>
        protected virtual void Cleanup() { }

        /// <summary>
        /// Path to the folder the rule should use as it's working folder.
        /// This will generally be where the rule picks up external configuration files
        /// or other data that are used within the rule itself.
        /// </summary>
        protected internal string WorkingFolder
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_workingFolder))
                {
                    return System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
                }

                return _workingFolder;
            }
            set
            {
                this._workingFolder = value;
            }
        }

        /// <summary>
        /// Name of the Rule.
        /// </summary>
        public virtual string Name
        {
            get { return GetType().Name; }
        }

        /// <summary>
        /// Description of the Rule.
        /// </summary>
        public virtual string Description
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Category which the Rule belongs to.
        /// </summary>
        public virtual string Category
        {
            get { return string.Empty; }
        }
        
        /// <summary>
        /// Version of the Rule.
        /// </summary>
        public virtual double Version
        {
            get { return 1.0; }
        }

        #region Rule performance measurment
        public RulePerformance MyPerformance { get; private set; }

        public class RulePerformance
        {
            public RulePerformance(string ruleName)
            {
                RuleName = ruleName;
                ObjectsProcessed = 0;
            }
            public string RuleName { get; private set; }

            public int ObjectsProcessed { get; private set; }
            private Stopwatch ruleStopwatch = new Stopwatch();

            public float AvgTicksPerObj
            {
                get
                {
                    if (ObjectsProcessed == 0) return 0;
                    return ruleStopwatch.ElapsedTicks / ObjectsProcessed;
                }
            }
            internal void StartMeasuring()
            {
                ruleStopwatch.Start();
            }
            internal void StopMeasuring()
            {
                ObjectsProcessed++;
                ruleStopwatch.Stop();
            }
            public override string ToString()
            {
                return String.Format(CultureInfo.CurrentCulture, "{0}: {1}", RuleName, AvgTicksPerObj);
            }
        }
        #endregion
    }

}
