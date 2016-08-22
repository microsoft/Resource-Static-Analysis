/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;

namespace Microsoft.ResourceStaticAnalysis.Rules.Context
{
    /// <summary>
    /// CommentsContext Rule flags resources that do not have any comment define in the 
    /// comments section of the resource or the comment is equal to the SourceString value.
    /// </summary>
    public class CommentsContext : LocResourceRule
    {
        public CommentsContext(RuleManager rm) : base(rm) { }

        protected override void Run()
        {
            // Check that the Resource has comments in it.
            Check(lr => lr.Comments.IsEmpty,
                "Resource doesn't have defined any comment. Please add a comment to the Resource");

            // Check if the Comment value is equal to the Source.
            Check(lr => lr.Comments.Equals(lr.SourceString),
                "Comment value is the same as the Source. Please add a more meaningful comment to the Resource");
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
                    "Checks for the presence of comments. We should add a comment per resource for context information";
            }
        }

        /// <summary>
        /// Name of the Rule.
        /// </summary>
        public override string Name
        {
            get { return "CommentsContext"; }
        }
    }
}
