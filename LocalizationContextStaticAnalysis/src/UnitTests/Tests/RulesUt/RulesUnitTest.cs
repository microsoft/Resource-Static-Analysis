/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Output;
using Microsoft.ResourceStaticAnalysis.ResourceStaticAnalysisExecutor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Tests.RulesUt
{
    /// <summary>
    /// Testing the basic functionality of the default Rules implemented in this solution.
    /// </summary>
    [TestClass]
    [DeploymentItem(@"Tests\RulesUt\DefaultUt.resx")]
    [DeploymentItem(@"Microsoft.ResourceStaticAnalysis.Rules.dll")]
    public class RulesUnitTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }
        private TcLogger _resourceStaticAnalysisRedirectedLogger;

        [TestInitialize]
        public void Initalize()
        {
            _resourceStaticAnalysisRedirectedLogger = new TcLogger(TestContext);
            Trace.Listeners.Add(_resourceStaticAnalysisRedirectedLogger);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _resourceStaticAnalysisRedirectedLogger.Flush();
        }

        /// <summary>
        /// Test method to test that ResourceStaticAnalysis can be configred to run against a RESX file with Module type rule
        /// </summary>
        [TestMethod]
        public void RulesOutputUnitTest()
        {
            try
            {
                ResourceStaticAnalysisApplication ResourceStaticAnalysisApplication = ResourceStaticAnalysisApplication.Instance;
                List<Exception> exceptions;
                ResourceStaticAnalysisApplication.Initialize(new List<string>() { TestContext.DeploymentDirectory });     // TestContext.DeploymentDirectory will pick UnitTests.dll 
                                                                                                                    // which contains the two rules defined here.
                                                                                                                    // RuleThatAccessesAllLocResourceProperties and AdjectivePlusPlaceholder rules

                IEnumerable<OutputEntryForOneRule> engineResults = ResourceStaticAnalysisApplication.Execute("DefaultUt.resx", true, out exceptions);

                Assert.IsFalse(exceptions != null, "Engine reported some execution errors.");

                #region CommentsContext Rule UT
                var commentsContext = engineResults.Where(a => a.Rule.Name.Equals("CommentsContext"));

                Assert.IsTrue(commentsContext.Count() == 2,
                    "CommentsContext Rule did not return right number of issues");

                Assert.IsTrue(commentsContext.ElementAt(0).OutputItems.ElementAt(0).Result,
                    "CommentsContext Rule returned false");
                Assert.IsFalse(commentsContext.ElementAt(0).OutputItems.ElementAt(1).Result,
                    "CommentsContext Rule returned true");
                Assert.IsTrue(commentsContext.ElementAt(0).CO.Properties[1].GetValue().Equals("String2"),
                    "CommentsContext Rule flagged an issue on a wrong resource");

                Assert.IsFalse(commentsContext.ElementAt(1).OutputItems.ElementAt(0).Result,
                    "CommentsContext Rule returned true");
                Assert.IsTrue(commentsContext.ElementAt(1).OutputItems.ElementAt(1).Result,
                    "CommentsContext Rule returned false");
                Assert.IsTrue(commentsContext.ElementAt(1).CO.Properties[1].GetValue().Equals("String3"),
                    "CommentsContext Rule flagged an issue on a wrong resource");
                #endregion

                #region DuplicatedResourceId Rule UT
                var duplicatedResourceId = engineResults.Where(a => a.Rule.Name.Equals("DuplicatedResourceId"));

                Assert.IsTrue(duplicatedResourceId.Count() == 2,
                    "DuplicatedResourceId Rule did not return right number of issues");

                Assert.IsTrue(duplicatedResourceId.ElementAt(0).OutputItems.ElementAt(0).Result,
                    "DuplicatedResourceId Rule returned false");
                Assert.IsTrue(duplicatedResourceId.ElementAt(0).CO.Properties[1].GetValue().Equals("String4_WRReviewer_Duplicate_ResourceId1"),
                    "DuplicatedResourceId Rule flagged an issue on a wrong resource");

                Assert.IsTrue(duplicatedResourceId.ElementAt(1).OutputItems.ElementAt(0).Result,
                    "DuplicatedResourceId Rule returned false");
                Assert.IsTrue(duplicatedResourceId.ElementAt(1).CO.Properties[1].GetValue().Equals("String4_WRReviewer_Duplicate_ResourceId2"),
                    "DuplicatedResourceId Rule flagged an issue on a wrong resource");
                #endregion

                #region DuplicatedSourceString Rule UT
                var duplicatedSourceString = engineResults.Where(a => a.Rule.Name.Equals("DuplicatedSourceString"));

                Assert.IsTrue(duplicatedSourceString.Count() == 2,
                    "DuplicatedSourceString Rule did not return right number of issues");

                Assert.IsTrue(duplicatedSourceString.ElementAt(0).OutputItems.ElementAt(0).Result,
                    "DuplicatedSourceString Rule returned false");
                Assert.IsTrue(duplicatedSourceString.ElementAt(0).CO.Properties[1].GetValue().Equals("String7_WRReviewer_Duplicate_SourceString1"),
                    "DuplicatedSourceString Rule flagged an issue on a wrong resource");

                Assert.IsTrue(duplicatedSourceString.ElementAt(1).OutputItems.ElementAt(0).Result,
                    "DuplicatedSourceString Rule returned false");
                Assert.IsTrue(duplicatedSourceString.ElementAt(1).CO.Properties[1].GetValue().Equals("String14_WRReviewer_Duplicate_SourceString1"),
                    "DuplicatedSourceString Rule flagged an issue on a wrong resource");
                #endregion

                #region EmptySourceString Rule UT
                var emptySourceString = engineResults.Where(a => a.Rule.Name.Equals("EmptySourceString"));

                Assert.IsTrue(emptySourceString.Count() == 1,
                    "EmptySourceString Rule did not return right number of issues");

                Assert.IsTrue(emptySourceString.ElementAt(0).OutputItems.ElementAt(0).Result,
                    "EmptySourceString Rule returned false");
                Assert.IsTrue(emptySourceString.ElementAt(0).CO.Properties[1].GetValue().Equals("String8"),
                    "EmptySourceString Rule flagged an issue on a wrong resource");
                #endregion

                #region EnUsCulture Rule UT
                var enUsCulture = engineResults.Where(a => a.Rule.Name.Equals("EnUsCulture"));

                Assert.IsTrue(enUsCulture.Count() == 1,
                    "EnUsCulture Rule did not return right number of issues");

                Assert.IsTrue(enUsCulture.ElementAt(0).OutputItems.ElementAt(0).Result,
                    "EnUsCulture Rule returned false");
                Assert.IsTrue(enUsCulture.ElementAt(0).CO.Properties[1].GetValue().Equals("String9"),
                    "EnUsCulture Rule flagged an issue on a wrong resource");
                #endregion

                #region LongSourceString Rule UT
                var longSourceString = engineResults.Where(a => a.Rule.Name.Equals("LongSourceString"));

                Assert.IsTrue(longSourceString.Count() == 1,
                    "LongSourceString Rule did not return right number of issues");

                Assert.IsTrue(longSourceString.ElementAt(0).OutputItems.ElementAt(0).Result,
                    "LongSourceString Rule returned false");
                Assert.IsTrue(longSourceString.ElementAt(0).CO.Properties[1].GetValue().Equals("String10"),
                    "LongSourceString Rule flagged an issue on a wrong resource");
                #endregion

                #region Placeholder Rule UT
                var placeHolderResults = engineResults.Where(a => a.Rule.Name.Equals("PlaceholderInsufficientContext"));

                Assert.IsTrue(placeHolderResults.Count() == 1,
                    "PlaceholderInsufficientContext Rule did not return right number of issues");

                Assert.IsTrue(placeHolderResults.ElementAt(0).Result,
                    "PlaceholderInsufficientContext Rule returned false");

                Assert.IsTrue(placeHolderResults.ElementAt(0).CO.Properties[1].GetValue().Equals("String1"),
                    "PlaceholderInsufficientContext Rule flagged an issue on a wrong resource");
                #endregion

                #region SpecialCharacters Rule UT
                var specialCharacters = engineResults.Where(a => a.Rule.Name.Equals("SpecialCharacters"));

                Assert.IsTrue(specialCharacters.Count() == 2,
                    "SpecialCharacters Rule did not return right number of issues");

                Assert.IsTrue(specialCharacters.ElementAt(0).OutputItems.ElementAt(0).Result,
                    "SpecialCharacters Rule returned false");
                Assert.IsTrue(specialCharacters.ElementAt(0).CO.Properties[1].GetValue().Equals("String11"),
                    "SpecialCharacters Rule flagged an issue on a wrong resource");

                Assert.IsTrue(specialCharacters.ElementAt(1).OutputItems.ElementAt(0).Result,
                    "SpecialCharacters Rule returned false");
                Assert.IsTrue(specialCharacters.ElementAt(1).CO.Properties[1].GetValue().Equals("String12"),
                    "SpecialCharacters Rule flagged an issue on a wrong resource");
                #endregion

            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }
            catch (ResourceStaticAnalysisEngineInitializationException ex)//Thrown when there are no rules to run on the input set.
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }
        }
    }
}
