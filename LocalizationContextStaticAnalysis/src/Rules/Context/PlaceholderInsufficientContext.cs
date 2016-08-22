/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.Core.Misc;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;

namespace Microsoft.ResourceStaticAnalysis.Rules.Context
{
    /// <summary>
    /// PlaceholderInsufficientContext Rule flags resources that contain placeholders but not comments.
    /// Comments explaining the placeholder help translator to come up with a better translation.
    /// </summary>
    public class PlaceholderInsufficientContext : LocResourceRule
    {
        private readonly Regex _placeholderRegex = new Regex("{[0-9]*}");

        private readonly StringAppendMessageCreator _mc;

        public PlaceholderInsufficientContext(RuleManager rm) : base(rm)
        {
            _mc = new StringAppendMessageCreator();
        }

        protected override void Run()
        {
            // Check that the SourceString contains a placeholder that is not defined in the Comments.
            Check(
                lr =>
                    _mc.SetContext(
                        lr.SourceString.Value.RegExpMatches(_placeholderRegex)
                            .Where(sourceTerm => !lr.SourceString.Value.TrimEnd('\r', '\n').Equals(sourceTerm))
                            .Where(strph => lr.Comments.RegExpMatches(_placeholderRegex).All(cmph => cmph != strph)))
                        .Any(), _mc.SetInit("Placeholder(s) present in string but not in comment: "));
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
                    "Checks for the presence of placeholders in the Source but not in the Comment. We should add a comment for each placeholder for context information";
            }
        }

        /// <summary>
        /// Name of the Rule.
        /// </summary>
        public override string Name
        {
            get { return "PlaceholderInsufficientContext"; }
        }
    }
}
