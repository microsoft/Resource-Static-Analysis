/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestRules
{
    public class SampleRule4 : SampleRuleType
    {
        // Inherited constructor
        public SampleRule4(RuleManager rm) : base(rm)
        {
        }
        protected override void Run()
        {
            string message = "SourceString should not contain foobar";
            Check(resource => !(resource.SourceString.Contains("foobar")), ref message);
        }
    }
}
