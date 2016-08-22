/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestRules
{
    public class SampleRule6 : SampleRuleType
    {
        // Inherited constructor
        public SampleRule6(RuleManager rm) : base(rm)
        {
        }

        protected override void Run()
        {
            string message = "Need to provide comments for context";
            Check(resource => !(String.IsNullOrEmpty(resource.Comments)), ref message);
        }
    }
}
