/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System.Collections.Generic;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.Core.Output
{
    /// <summary>
    /// Output of a single call to Check().
    /// </summary>
    public struct OutputItem
    {
        public OPERAND ItemType { get; set; }
        public string Message { get; set; }
        public bool Result { get; set; }
        public CheckSeverity Severity { get; set; }
    }

    public enum OPERAND { Default, AND, OR };

    /// <summary>
    /// This is a result of a <seealso cref="Rule"/> having been applied to a <seealso cref="ClassificationObject"/>
    /// <seealso cref="OutputEntryForOneRule"/> must contain at least one <seealso cref="OutputItem"/>
    /// </summary>
    public class OutputEntryForOneRule
    {
        /// <summary>
        /// The CO to which this output entry is assigned.
        /// </summary>
        public ClassificationObject CO { get; set; }

        /// <summary>
        /// Details of the rule which created the output entry.
        /// </summary>
        public Rule Rule { get; set; }

        /// <summary>
        /// What is the summary result of rule running on the CO.
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// Output details for each of the calls to <see cref="Rule"/>.Check().
        /// </summary>
        public readonly List<OutputItem> OutputItems = new List<OutputItem>();

        /// <summary>
        /// Gets the severity of the entire Rule. This is the highest severity from all checks that evaluated true.
        /// Returns CheckSeverity.Normal if there are no checks matched.
        /// </summary>
        public CheckSeverity Severity
        {
            get
            {
                if (OutputItems.Count == 0)
                {
                    return CheckSeverity.Normal;
                }
                return OutputItems.Where(oi => oi.Result).Max(oi => oi.Severity);
            }
        }

        /// <summary>
        /// Adds single entry of output to the result.
        /// </summary>
        /// <remarks>This method is called by the engine after every call to Rule.Check().</remarks>
        /// <param name="oi">Result of single Rule.Check() call.</param>
        public void AddItem(OutputItem oi)
        {
            OutputItems.Add(oi);
        }

        /// <summary>
        /// Calculates the overall result of running the rule on a single CO.
        /// </summary>
        /// <remarks>The values of each Check() result are AND'ed to create the overall result.
        /// That is, if all Check() calls returned True, then the result for the whole rule is set to True.
        /// </remarks>
        /// <returns></returns>
        public bool SetTrueOrFalse()
        {
            RuleResult val = new RuleResult() { Result = false, OP = OPERAND.OR };//OR is default, so we have to start with false
            //Parse the output
            OutputItems.ForEach(oi => val.ParseNext(oi));
            CleanUpOperands();
            Result = val;
            return val;
        }

        /// <summary>
        /// Removes all OutputItems that act as operands.
        /// </summary>
        /// <remarks>
        /// Call this method before sending <see cref="OutputEntryForOneRule"/> to output writers.
        /// </remarks>
        private void CleanUpOperands()
        {
            OutputItems.RemoveAll(oi => oi.ItemType != OPERAND.Default);
        }

        /// <summary>
        /// Stores and computes Rule's result.
        /// </summary>
        /// <remarks>
        /// This class also contains the complete logic of how to treat AND and OR operators.
        /// </remarks>
        private class RuleResult
        {
            public bool Result { get; set; }
            public OPERAND OP { get; set; }
            internal void UpdateResult(bool p)
            {
                switch (OP)
                {
                    case OPERAND.OR: Result |= p; break;
                    case OPERAND.AND: Result &= p; break;
                    default: throw new ResourceStaticAnalysisException("Wrong operand");
                }
                //Reset to default operand automatically
                OP = OPERAND.OR;
            }
            internal void ParseNext(OutputItem oi)
            {
                switch (oi.ItemType)
                {
                    case OPERAND.OR: OP = OPERAND.OR; break;
                    case OPERAND.AND: OP = OPERAND.AND; break;
                    default: //Standard behavior
                        UpdateResult(oi.Result);
                        break;
                }
            }
            public static implicit operator bool(RuleResult right) { return right.Result; }
        }
    }

    public class OutputStore
    {
        /// <summary>
        /// Initialize storage for output. This class is responsible for passing all output from the engine activity
        /// to individual, specialized output writers.
        /// </summary>
        public OutputStore(Engine.ResourceStaticAnalysisEngine owner)
        {
            this._owningEngine = owner;
            this.outputWriters.AddRange(owner._outputWriters);
        }

        private Engine.ResourceStaticAnalysisEngine _owningEngine;

        /// <summary>
        /// Merges output from all rules, groups by CO and feeds to all registered output writers
        /// </summary>
        public void FlushOutput(IEnumerable<OutputEntryForOneRule> output)
        {
            var coGroups =
                from entry in output
                orderby entry.CO.Key
                group entry by entry.CO;

            foreach (var coEntry in coGroups)
            {
                outputWriters.ForEach(writer => writer.WriteEntry(coEntry.Key, coEntry));
            }
        }

        public void FinishOutputWriters()
        {
            foreach (IOutputWriter writer in outputWriters)
            {
                writer.Finish();
            }
        }

        List<IOutputWriter> outputWriters = new List<IOutputWriter>();
    }
}
