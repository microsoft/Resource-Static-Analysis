/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestRules
{
    public class IncompatibleRule1 : IncompatibleRuleType
    {
        public IncompatibleRule1(RuleManager rm) : base(rm)
        {
        }
        protected override void Run()
        {
            string message = "Binary file contains checksum error";
            Check(resource => resource.CheckSumError == true, ref message);
        }
    }
}
