/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;

namespace Microsoft.ResourceStaticAnalysis.Rules.Functional
{
    /// <summary>
    /// DuplicatedResourceId Rule flags resources that contain the same Resource Id value.
    /// </summary>
    public class DuplicatedResourceId : LocResourceRule
    {
        private readonly StringAppendMessageCreator _mc;

        // Pattern added by the parsers.
        private readonly string _dupPattern = "_WRReviewer_Duplicate_ResourceId";

        public DuplicatedResourceId(RuleManager rm) : base(rm)
        {
            _mc = new StringAppendMessageCreator();
        }

        protected override void Run()
        {
            // Checks for the presence of a pattern added by the DataAdapter parsers to indicate a duplicate ResourceId
            // If present, the context is set to the duplicate number (the nth duplicate) for this ResourceId
            // If not present, the context is set to the string value
            // Returns true if the context and string value are different

            Check(
                lr =>
                    _mc.SetContext(
                        lr.ResourceId.Value.Substring(lr.ResourceId.Value.Contains(_dupPattern)
                            ? lr.ResourceId.Value.IndexOf(_dupPattern, StringComparison.Ordinal) + _dupPattern.Length
                            : 0)) != lr.ResourceId.Value,
                _mc.SetInit(
                    "One or more duplicate entries detected in the file for this resource ID. This is duplicate number:"));
        }

        /// <summary>
        /// Category which the rule belongs to.
        /// </summary>
        public override string Category
        {
            get { return "Functional"; }
        }

        /// <summary>
        /// Description of the Rule.
        /// </summary>
        public override string Description
        {
            get
            {
                return
                    "Checks for the presence of duplicated Resource Ids within the same Resource File";
            }
        }

        /// <summary>
        /// Name of the Rule.
        /// </summary>
        public override string Name
        {
            get { return "DuplicatedResourceId"; }
        }
    }
}
