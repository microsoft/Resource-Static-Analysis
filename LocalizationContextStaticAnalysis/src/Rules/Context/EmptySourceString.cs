/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;

namespace Microsoft.ResourceStaticAnalysis.Rules.Context
{
    /// <summary>
    /// EmptySourceString Rule flags resources that doesn't contain any value in the SourceString section.
    /// </summary>
    public class EmptySourceString : LocResourceRule
    {
        public EmptySourceString(RuleManager owner) : base(owner)
        {
        }

        protected override void Run()
        {
            // Check that the SourceString has a value in it.
            Check(lr => lr.SourceString.IsEmpty,
                "Resource doesn't have defined value for the SourceString. Please add a value to the Resource");
        }

        /// <summary>
        /// Category which the rule belongs to.
        /// </summary>
        public override string Category
        {
            get { return "Context"; }
        }

        /// <summary>
        /// Description of the Rule.
        /// </summary>
        public override string Description
        {
            get
            {
                return
                    "Checks for the absence of a value in the SourceString field.";
            }
        }

        /// <summary>
        /// Name of the Rule.
        /// </summary>
        public override string Name
        {
            get { return "EmptySourceString"; }
        }
    }
}
