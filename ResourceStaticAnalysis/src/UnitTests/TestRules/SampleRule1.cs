/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestRules
{
    public class SampleRule1 : SampleRuleType
    {
        // Inherited constructor
        public SampleRule1(RuleManager rm): base(rm)
        {
        }
        protected override void Run()
        {
            string message = "Comment should be descriptive and should not exactly match the string value";
            Check(resource => !(resource.SourceString == resource.Comments), ref message);
        }
    }
}
