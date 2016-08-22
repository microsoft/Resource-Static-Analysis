/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System.Text.RegularExpressions;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;

namespace Microsoft.ResourceStaticAnalysis.Rules.Context
{
    /// <summary>
    /// LongSourceString Rule flags resources that contain words with at least 50 characters without spaces.
    /// This type of strings usually do not represent a localizable string resource.
    /// </summary>
    public class LongSourceString : LocResourceRule
    {
        private const int LongStringLength = 50;
        private static RegexOptions _commonRegexOpts = RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant | RegexOptions.Compiled;

        private static Regex _longStringWithoutSpaces =
            new Regex(string.Format(@"\S{{{0}}} # at least X non-whitespace characters in a row", LongStringLength),
                _commonRegexOpts);

        public LongSourceString(RuleManager owner) : base(owner)
        {
        }

        protected override void Run()
        {
            // Check that the SourceString value as a string greater than 50 characters with no spaces in it.
            Check(lr => lr.SourceString.RegExp(_longStringWithoutSpaces),
                string.Format("Long string without spaces (greater than {0} chars)", LongStringLength));
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
                    "Checks for the presence of long strings without spaces";
            }
        }

        /// <summary>
        /// Name of the Rule.
        /// </summary>
        public override string Name
        {
            get { return "LongSourceString"; }
        }
    }
}
