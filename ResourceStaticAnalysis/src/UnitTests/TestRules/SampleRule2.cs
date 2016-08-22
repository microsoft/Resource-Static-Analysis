/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System.Collections.Generic;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestRules
{
    public class SampleRule2 : SampleRuleType
    {
        // Inherited constructor
        public SampleRule2(RuleManager rm) : base(rm)
        {
            mc = new StringAppendMessageCreator();
        }

        private StringAppendMessageCreator mc;
        protected override void Run()
        {
            Check(resource => mc.SetContext(GetVowels(resource.SourceString.ToString()).ToString()).Any(),
                mc.SetInit("Vowels found in SourceString: "));
        }

        private List<char> GetVowels(string s)
        {
            List<char> vowels = new List<char> { 'a', 'e', 'i', 'o', 'u' };
            List<char> results = s.ToCharArray().Where(potentialVowel => vowels.Contains(potentialVowel)).ToList();
            return results;
        }
    }
}
