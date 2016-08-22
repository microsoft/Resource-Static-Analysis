/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;

namespace Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource
{ 
    public abstract class LocResourceRule : Rule
    {
        public LocResourceRule(RuleManager owner) : base(owner) { }

        /// <summary>
        /// Call this method from within your Run() method.
        /// Each call to Check() produces one entry in the log.
        /// </summary>
        /// <param name="exp">Your rule's logic in form of a lambda expression</param>
        /// <param name="message">Message to be written out to log. Passed as reference so exp can modify the message, if needed.</param>
        /// <returns></returns>
        public bool Check(COExpression<LocResource> exp, ref string message)
        {
            return base._AgnosticCheckandLog(this.ExpressionCaster(exp), ref message, CheckSeverity.Normal);
        }
        public bool Check(COExpression<LocResource> exp, ref string message, CheckSeverity severity)
        {
            return base._AgnosticCheckandLog(this.ExpressionCaster(exp), ref message, severity);
        }
        /// <summary>
        /// Call this method from within your Run() method.
        /// Each call to Check() produces one entry in the log.
        /// </summary>
        /// <param name="exp">Your rule's logic in form of a lambda expression</param>
        /// <param name="message">Message to be written out to log. Passed as value, so it cannot be modified by exp.</param>
        /// <returns></returns>
        public bool Check(COExpression<LocResource> exp, string message)
        {
            return Check(exp, ref message);
        }
        public bool Check(COExpression<LocResource> exp, string message, CheckSeverity severity)
        {
            return Check(exp, ref message, severity);
        }
        /// <summary>
        /// Call this method from within your Run() method.
        /// Each call to Check() produces one entry in the log.
        /// Use your customized message creator in order to build a dynamic message based on the execution of exp.
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="mc">Message creator is executed after exp executes. This gives you a chance to modify the content of the message in the exp by 
        /// passing context information into mc and creating custom logic in your MC implementation of GetMessage().</param>
        /// <returns></returns>
        public bool Check(COExpression<LocResource> exp, MessageCreator mc)
        {
            return base._AgnosticCheckandLog(ExpressionCaster(exp), mc, CheckSeverity.Normal);
        }
        /// <summary>
        /// Call this method from within your Run() method.
        /// Each call to Check() produces one entry in the log.
        /// Use your customized message creator in order to build a dynamic message based on the execution of exp.
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="mc">Message creator is executed after exp executes. This gives you a chance to modify the content of the message in the exp by 
        /// passing context information into mc and creating custom logic in your MC implementation of GetMessage().</param>
        /// <param name="severity">Specify the severity of the check.</param>
        /// <returns></returns>
        public bool Check(COExpression<LocResource> exp, MessageCreator mc, CheckSeverity severity)
        {
            return base._AgnosticCheckandLog(ExpressionCaster(exp), mc, severity);
        }

        internal new LocResource CurrentCO
        {
            get
            {
                return (LocResource)base.CurrentCO;
            }
        }
        public sealed override Func<object, COExpression<ClassificationObject>> ExpressionCaster
        {
            get { return ExpressionCasting<LocResource>.ExpressionCast; }
        }

        public override Type TypeOfClassificationObject
        {
            get { return typeof(LocResource); }
        }
    }
}
