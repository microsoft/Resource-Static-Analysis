/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestRules
{
    public class SampleRule8 : SampleRuleType
    {
        // Inherited constructor
        public SampleRule8(RuleManager rm) : base(rm)
        {
        }
        protected override void Run()
        {
            string message = "ResourceId is greater than 256 characters";
            Check(resource => resource.ResourceId.ToString().Length > 256, ref message);
        }
    }
}
