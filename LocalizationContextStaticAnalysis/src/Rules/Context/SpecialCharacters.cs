/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;

namespace Microsoft.ResourceStaticAnalysis.Rules.Context
{
    /// <summary>
    /// SpecialCharacters Rule flags resources that contain special characters and/or numbers only.
    /// This type of strings usually do not represent a localizable string resource.
    /// </summary>
    public class SpecialCharacters : LocResourceRule
    {
        private readonly Regex _specialCharactersAndNumbersRegex =
            new Regex(@"([\[\]\^\$\.\|\?\*\+\(\)\\~`\!@#%&\-_+={}'""<>:;,]|[0-9])+");
        public SpecialCharacters(RuleManager owner) : base(owner) { }

        protected override void Run()
        {
            // Check that the SourceString contains only special characters and/or numbers.
            Check(
                lr =>
                    lr.SourceString.RegExpMatches(_specialCharactersAndNumbersRegex)
                        .Any(match => match.Equals(lr.SourceString.Value, StringComparison.CurrentCultureIgnoreCase)),
                "String contains special characters and/or numbers only.");
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
                    "Checks for the presence of Special Characters or Special Characters and Numbers in the SourceString value.";
            }
        }

        /// <summary>
        /// Name of the Rule.
        /// </summary>
        public override string Name
        {
            get { return "SpecialCharacters"; }
        }
    }
}
