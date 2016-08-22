/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.UnitTests.TestObjectStructure;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestRules
{
    public abstract class IncompatibleRuleType : Rule
    {
        public IncompatibleRuleType(RuleManager owner) : base(owner) { }

        public sealed override Func<object, COExpression<ClassificationObject>> ExpressionCaster
        {
            get { return ExpressionCasting<IncompatibleObject>.ExpressionCast; }
        }

        public override Type TypeOfClassificationObject
        {
            get { return typeof(IncompatibleObject); }
        }

        public bool Check(COExpression<IncompatibleObject> exp, ref string message)
        {
            return _AgnosticCheckandLog(this.ExpressionCaster(exp), ref message, CheckSeverity.Normal);
        }

        public bool Check(COExpression<IncompatibleObject> exp, MessageCreator mc)
        {
            return base._AgnosticCheckandLog(ExpressionCaster(exp), mc, CheckSeverity.Normal);
        }
    }
}
