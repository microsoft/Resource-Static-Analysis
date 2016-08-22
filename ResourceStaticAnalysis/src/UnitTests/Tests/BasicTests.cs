/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Practices.AssemblyManagement;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.Tests
{
    /// <summary>
    /// Testing the basic functionality of ResourceStaticAnalysis
    /// </summary>
    [TestClass]
    [DeploymentItem(@"ResourceStaticAnalysisOutput.xsd")]
    public class BasicTests
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Initalize()
        {
            _resourceStaticAnalysisRedirectedLogger = new TCLogger(TestContext);
            Trace.Listeners.Add(_resourceStaticAnalysisRedirectedLogger);
        }
        [TestCleanup]
        public void Cleanup()
        {
            _resourceStaticAnalysisRedirectedLogger.Flush();
        }
        private TCLogger _resourceStaticAnalysisRedirectedLogger;
       
        [TestMethod]
        public void ResourceStaticAnalysisCanReturnSchemaStream()
        {
            var reader = ResourceStaticAnalysisToolbox.GetConfigSchemaStream();
            Assert.IsNotNull(reader);
            var p = reader.ReadLine();
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-8\"?>", p);
        }

        /// <summary>
        /// Test method to test that ResourceStaticAnalysis can be configred and run against a Source rule (.cs)
        /// </summary>
        [TestMethod]
        [DeploymentItem(@"Microsoft.ResourceStaticAnalysis.Core.dll")]
        [DeploymentItem(@"UnitTests.dll")]
        [DeploymentItem(@"TestRules\SampleRule1.cs")]
        public void ResourceStaticAnalysisCanBeConfiguredAndRunAgainstSource()
        {
            try
            {
                var assemblyResolver = new AssemblyResolver(TestContext.DeploymentDirectory);
                assemblyResolver.Init();
                var inputCfg = TestHelpers.CreateSampleConfig(TestContext, @"SampleRule1.cs", 6);
                var engine = new ResourceStaticAnalysisEngine(inputCfg);
                if (engine.StartRun())
                {
                    engine.WaitForJobFinish();
                }
                Assert.IsFalse(engine.Monitor.NoOfExceptionsLogged > 0, "Engine reported some execution errors.");
                // Only 3 of the resources should have been flagged by the rule
                Assert.IsTrue(engine.Monitor.TotalNumResults.Equals(3), "Rule results were not as expected");
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }
            catch (ResourceStaticAnalysisEngineInitializationException ex) // Thrown when there are no rules to run on the input set.
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }
        }

        /// <summary>
        /// Test method to test that ResourceStaticAnalysis can be configred and run against a module (.dll)
        /// Since Rules are each run on separate threads, multi-rule modules access multi-threading access to cache
        /// </summary>
        [TestMethod]
        [DeploymentItem(@"UnitTests.dll")]
        public void ResourceStaticAnalysisCanBeConfiguredAndRunAgainstModule()
        {
            try
            {
                var assemblyResolver = new AssemblyResolver(TestContext.DeploymentDirectory);
                assemblyResolver.Init();
                var inputCfg = TestHelpers.CreateSampleConfig(TestContext, @"UnitTests.dll", 6);
                var engine = new ResourceStaticAnalysisEngine(inputCfg);
                if (engine.StartRun())
                {
                    engine.WaitForJobFinish();
                }
                Assert.IsFalse(engine.Monitor.NoOfExceptionsLogged > 0, "Engine reported some execution errors.");
                Assert.IsTrue(engine.Monitor.TotalNumResults.Equals(26), "Rule results were not as expected");
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }
            catch (ResourceStaticAnalysisEngineInitializationException ex) // Thrown when there are no rules to run on the input set.
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }
        }

        /// <summary>
        /// Test method that validate the application of a FilteringExpression
        /// </summary>
        [TestMethod]
        [DeploymentItem(@"Microsoft.ResourceStaticAnalysis.Core.dll")]
        [DeploymentItem(@"UnitTests.dll")]
        [DeploymentItem(@"TestRules\SampleRule1.cs")]
        public void ResourceStaticAnalysisCanHandleFilteringExpressions()
        {
            try
            {
                var assemblyResolver = new AssemblyResolver(TestContext.DeploymentDirectory);
                assemblyResolver.Init();
                var inputCfg = TestHelpers.CreateSampleConfig(TestContext, @"SampleRule1.cs", 6);
                // Add a filtering expression that will match only a single resource in the sample file
                inputCfg.DefaultFilteringExpression = "ResourceId.Value.Contains(\"Item6\")";
                var engine = new ResourceStaticAnalysisEngine(inputCfg);
                if (engine.StartRun())
                {
                    engine.WaitForJobFinish();
                }
                Assert.IsFalse(engine.Monitor.NoOfExceptionsLogged > 0, "Engine reported some execution errors.");
                // Only Item6 should be processed
                Assert.IsTrue(engine.Monitor.TotalNumResults.Equals(1), "Rule results were not as expected");
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }
            catch (ResourceStaticAnalysisEngineInitializationException ex) // Thrown when there are no rules to run on the input set.
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }
        }

        /// <summary>
        /// Test method to ensure that rules intended for different ClassificationObject types are not run
        /// </summary>
        [TestMethod]
        [DeploymentItem(@"Microsoft.ResourceStaticAnalysis.Core.dll")]
        [DeploymentItem(@"UnitTests.dll")]
        public void IncompatibleRulesAreFilteredCorrectly()
        {
            try
            {
                var assemblyResolver = new AssemblyResolver(TestContext.DeploymentDirectory);
                assemblyResolver.Init();
                var inputCfg = TestHelpers.CreateSampleConfig(TestContext, "UnitTests.dll", 6);
                var engine = new ResourceStaticAnalysisEngine(inputCfg);
                if (engine.StartRun())
                {
                    engine.WaitForJobFinish();
                }
                Assert.IsFalse(engine.Monitor.NoOfExceptionsLogged > 0, "Engine reported some execution errors.");
                Assert.IsTrue(engine.Monitor.RulePerformance.Count().Equals(8), "Rule results were not as expected");
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }
            catch (ResourceStaticAnalysisEngineInitializationException ex) // Thrown when there are no rules to run on the input set.
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }
        }
    }
}

/// <summary>
/// Logger that can log to TestContext. Use in UnitTests methods to intercept ResourceStaticAnalysis debug logs.
/// </summary>
public class TCLogger : TraceListener
{

    readonly Regex _overheadRemover = new Regex(@"\[Agent:.+'\]");
    readonly Regex _characterReplacerBecauseTestContextIsFullOfBugs = new Regex("[\0]");

    TestContext Output { get; set; }
    public TCLogger(TestContext output)
    {
        Output = output;
    }

    public override void Write(string message)
    {

        string fixedEntry = _overheadRemover.Replace(message, "[]");
        fixedEntry = _characterReplacerBecauseTestContextIsFullOfBugs.Replace(fixedEntry, "[zero(0)]");
        Output.WriteLine(fixedEntry);
    }

    public override void WriteLine(string message)
    {
        Write(message);
    }
}

