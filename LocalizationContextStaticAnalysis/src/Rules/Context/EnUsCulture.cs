/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;

namespace Microsoft.ResourceStaticAnalysis.Rules.Context
{
    /// <summary>
    /// EnUsCulture Rule flags resources that contains the 'en-us' pattern in the string.
    /// This could cause some localizability issues in other cultures.
    /// </summary>
    public class EnUsCulture : LocResourceRule
    {
        private readonly string enUsCulture = "en-us";

        public EnUsCulture(RuleManager owner) : base(owner) { }

        protected override void Run()
        {
            // Check that the SourceString value contains the en-us ll-cc.
            Check(lr => lr.SourceString.Value.IndexOf(enUsCulture, 0, StringComparison.CurrentCultureIgnoreCase) != -1,
                "English culture was detected in Source Term.");
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
                    "Checks for the en-us culture in the SourceTerm of the resource.";
            }
        }

        /// <summary>
        /// Name of the Rule.
        /// </summary>
        public override string Name
        {
            get { return "EnUsCulture"; }
        }
    }
}
