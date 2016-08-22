/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine
{
    public class EngineMonitor
    {
        /// <summary>
        /// Creates an instance of EngineMonitor for the given engine.
        /// </summary>
        /// <param name="engine">ResourceStaticAnalysisEngine object to be monitored</param>
        internal EngineMonitor(ResourceStaticAnalysisEngine engine)
        {
            this._engine = engine;
            LogExceptionDetails = false;
            
            Reset();

            #region Registering for engine events
            engine.LoadingStarting += (s, e) => loadingStart = DateTime.Now;
            engine.LoadingFinished += (s, countArgs) => { loadingEnd = DateTime.Now; objectsLoaded = countArgs.Value; };

            engine.ProcessingStarting += (s, e) => processingStart = DateTime.Now;
            engine.ProcessingFinished += (s, e) => processingEnd = DateTime.Now;

            engine.OutputStarting += (s, e) => outputStart = DateTime.Now;
            engine.OutputFinished += (s, e) => outputEnd = DateTime.Now;

            engine.EngineCleanup += (s, e) => this.Reset();
            #endregion
        }
                
        private readonly ResourceStaticAnalysisEngine _engine;

        /// <summary>
        /// Resets all monitor data so monitor can be used for another Engine run.
        /// </summary>
        internal void Reset()
        {
            loadingStart = loadingEnd = processingStart = processingEnd = outputStart = outputEnd = DateTime.MinValue;
            objectsLoaded = 0;
            Interlocked.Exchange(ref exceptionCount, 0);
            ExceptionListLock.EnterWriteLock();
            try
            {
                exceptionList = new List<Exception>();
            }
            finally
            {
                ExceptionListLock.ExitWriteLock();
            }
        }

        #region Timing data
        DateTime loadingStart, loadingEnd;
        DateTime processingStart, processingEnd;
        DateTime outputStart, outputEnd;

        int objectsLoaded;

        /// <summary>
        /// Get the time it took to perform loading.
        /// </summary>
        public TimeSpan LoadingElapsed
        {
            get
            {
                return loadingEnd == DateTime.MinValue ? TimeSpan.MinValue : loadingEnd - loadingStart;
            }
        }

        /// <summary>
        /// Get time it took for Engine to process all Classification Objects (time it took for all rules to execute).
        /// </summary>
        public TimeSpan ProcessingElapsed
        {
            get
            {
                return processingEnd == DateTime.MinValue ? TimeSpan.MinValue : processingEnd - processingStart;
            }
        }

        /// <summary>
        /// Get time it took for writing output to complete.
        /// </summary>
        public TimeSpan OutputElapsed
        {
            get
            {
                return outputEnd == DateTime.MinValue ? TimeSpan.MinValue : outputEnd - outputStart;
            }
        }
        #endregion

        #region Exception data
        /// <summary>
        /// If set to true EngineMonitor keeps all exception data in memory for further inspection.
        /// This can cause high memory usage in cases when a rule throws a lot of exceptions on many objects, etc.
        /// False by default.
        /// </summary>
        public bool LogExceptionDetails { get; set; }

        /// <summary>
        /// stores exception count
        /// </summary>
        int exceptionCount = 0;
        /// <summary>
        /// Stores exception objects, if LogExceptionDetails is set to true
        /// </summary>
        List<Exception> exceptionList = new List<Exception>();

        /// <summary>
        /// Controls access to the exception list.
        /// </summary>
        private static readonly ReaderWriterLockSlim ExceptionListLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Lets monitor know that an exception has been thrown and caught by the engine. If LogExceptionDetails is true,
        /// adds the ex object to the list for further inspection by user code. Otherwise, simply increments an exception counter.
        /// </summary>
        /// <param name="ex">Exception object that will be added to a list, if LogExceptionDetails is true.</param>
        internal void LogException(Exception ex)
        {
            Interlocked.Increment(ref exceptionCount);
            if (LogExceptionDetails)
            {
                ExceptionListLock.EnterWriteLock();
                try
                {
                    exceptionList.Add(ex);
                }
                finally
                {
                    ExceptionListLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Number of exceptions intercepted by Engine.
        /// </summary>
        public int NoOfExceptionsLogged { get { return exceptionCount; } }

        /// <summary>
        /// A collection of Exception objects caught by the Engine. Empty if LogExceptionDetails is set to false.
        /// </summary>
        public ReadOnlyCollection<Exception> ExceptionDetails
        {
            get
            {
                ExceptionListLock.EnterReadLock();
                try
                {
                    return exceptionList.AsReadOnly();
                }
                finally
                {
                    ExceptionListLock.ExitReadLock();
                }
            }
        }
        #endregion

        #region Output data
        /// <summary>
        /// Number of Classification Objects that were flagged by at least one Rule.
        /// (Causes enumeration of all output logged by engine so far, so accessing the property may be time consuming).
        /// </summary>
        public int ObjectsClassified
        {
            get
            {
                return _engine.CurrentRuleManager.OutputFromAllRules.Distinct().Count();
            }
        }

        /// <summary>
        /// Total number of checked results founded in Classification Objects.
        /// </summary>
        public int TotalNumResults
        {
            get
            {
                int count = 0;
                foreach (var output in _engine.CurrentRuleManager.OutputFromAllRules.Distinct())
                {
                    count += output.OutputItems.Count;
                }
                return count;
            }
        }
        #endregion

        #region Rule performance statistics
        /// <summary>
        /// A collection of RulePerformance objects that provide detail about execution of each rule.
        /// </summary>
        public IEnumerable<Rule.RulePerformance> RulePerformance
        {
            get
            {
                return _engine.CurrentRuleManager.RulePerformanceSummary;
            }
        }

        /// <summary>
        /// Produces a formatted string with rule performance summary.
        /// </summary>
        public string PrintRulePerformanceSummary()
        {
            var sb = new StringBuilder("Performance data for Rules (avg ticks per object - number of objects processed):", 2000);
            sb.AppendLine();

            foreach (var entry in RulePerformance.OrderBy(perf => perf.AvgTicksPerObj))
            {
                sb.AppendFormat("{0}:\t{1} - {2}", entry.RuleName, entry.AvgTicksPerObj, entry.ObjectsProcessed);
                sb.AppendLine();
            }

            //remove the last new line character
            return sb.ToString().TrimEnd();
        }
        #endregion
    }
}
