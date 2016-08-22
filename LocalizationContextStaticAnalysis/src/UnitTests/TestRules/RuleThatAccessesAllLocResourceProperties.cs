/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System.Diagnostics;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;

namespace UnitTests.TestRules
{
    /// <summary>
    /// Rule used by a unit test to make sure access to all LocResource properties is possible and that properties do not throw exceptions.
    /// </summary>
    public class RuleThatAccessesAllLocResourceProperties : LocResourceRule
    {
        public RuleThatAccessesAllLocResourceProperties(RuleManager owner) : base(owner) { }
        private const int ResNumber = 700;
        private bool _notIntroduced = true;
        private int _currentResource;

        protected override void Run()
        {
            _currentResource++;
            if (_notIntroduced)
            {
                Trace.TraceInformation("This rule will dump all properties of {0}th LocResource to log.", ResNumber);
                _notIntroduced = false;
            }
            if (_currentResource != ResNumber) return;
            var lr = CurrentCO as LocResource;

            if (lr == null)
            {
                Trace.TraceError("Could not cast resource to LocResource. Cannot access properties. This is a bug in ResourceStaticAnalysis.");
                return;
            }

            Trace.TraceInformation("Properties of resource number {0}", _currentResource);
            Trace.TraceInformation("Comments ({0}):", lr.Comments.Value);
            Trace.TraceInformation("ResourceId={0}", lr.ResourceId.Value);
            Trace.TraceInformation("Project={0}", lr.Project.Value);
            Trace.TraceInformation("SourceString={0}", lr.SourceString.Value);
            Trace.TraceInformation("FilePath={0}", lr.FilePath.Value);
            Trace.TraceInformation("Tracing finished.");
        }
    }
}