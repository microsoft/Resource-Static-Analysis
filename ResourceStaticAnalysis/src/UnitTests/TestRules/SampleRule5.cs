/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestRules
{
    public class SampleRule5 : SampleRuleType
    {
        // Inherited constructor
        public SampleRule5(RuleManager rm) : base(rm)
        {
        }
        protected override void Run()
        {
            string message = "ResourceId should start with Item";
            Check(resource => resource.ResourceId.ToString().StartsWith("Item"), ref message);
        }
    }
}
