/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestRules
{
    public class SampleRule7 : SampleRuleType
    {
        // Inherited constructor
        public SampleRule7(RuleManager rm) : base(rm)
        {
        }
        protected override void Run()
        {
            string message = "ResourceId is less than 5 characters";
            Check(resource => resource.ResourceId.ToString().Length < 5, ref message);
        }
    }
}
