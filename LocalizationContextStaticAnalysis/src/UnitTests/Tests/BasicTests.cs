/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Misc;
using Microsoft.ResourceStaticAnalysis.DataAdapter;
using Microsoft.ResourceStaticAnalysis.ResourceStaticAnalysisExecutor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Tests
{
    /// <summary>
    /// Testing the basic functionality of ResourceStaticAnalysis
    /// </summary>
    [TestClass]
    [DeploymentItem(@"Sample.resx")]
    [DeploymentItem(@"UnitTests.config")]
    [DeploymentItem(@"TestRules\RuleThatAccessesAllLocResourceProperties.cs")]
    [DeploymentItem(@"TestRules\TestRule.cs")]

    public class BasicTests
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
        /// Test method to test that ResourceStaticAnalysis can be configred to run against a RESX file with Source type rule
        /// </summary>
        [TestMethod]
        public void ResourceStaticAnalysisCanBeConfiguredAndAccessAllLocPropertiesTwoDataSources()
        {
            EngineConfig inputCfg = TestHelpers.CreateSampleConfigWithTwoDataSources(TestContext, @"RuleThatAccessesAllLocResourceProperties.cs", "Sample.resx");
            ResourceStaticAnalysisEngine engine = TestHelpers.RunEngine(inputCfg, TestContext.DeploymentDirectory);
            Assert.IsFalse(engine.Monitor.NoOfExceptionsLogged > 0, "Engine reported some execution errors."); ;
        }

        /// <summary>
        /// Test method to test that ResourceStaticAnalysis can be configred to run against a RESX file with Source type rule
        /// </summary>
        [TestMethod]
        public void ResourceStaticAnalysisCanBeConfiguredAndAccessAllLocPropertiesOneDataSource()
        {
            EngineConfig inputCfg = TestHelpers.CreateSampleConfigWithOneDataSources(TestContext, @"RuleThatAccessesAllLocResourceProperties.cs", "Sample.resx");
            ResourceStaticAnalysisEngine engine = TestHelpers.RunEngine(inputCfg, TestContext.DeploymentDirectory);
            Assert.IsFalse(engine.Monitor.NoOfExceptionsLogged > 0, "Engine reported some execution errors.");           
        }

        /// <summary>
        /// Test method to test that ResourceStaticAnalysis can be configred to run against a RESX file with Source type rule
        /// </summary>
        [TestMethod]
        public void ResourceStaticAnalysisCanBeConfiguredAndRunAgainstSourceTwoDataSources()
        {
            EngineConfig inputCfg = TestHelpers.CreateSampleConfigWithTwoDataSources(TestContext, @"TestRule.cs", "Sample.resx");
            ResourceStaticAnalysisEngine engine = TestHelpers.RunEngine(inputCfg, TestContext.DeploymentDirectory);
            Assert.IsFalse(engine.Monitor.NoOfExceptionsLogged > 0, "Engine reported some execution errors.");
        }

        /// <summary>
        /// Test method to test that ResourceStaticAnalysis can be configred to run against a RESX file with Source type rule
        /// </summary>
        [TestMethod]
        public void ResourceStaticAnalysisCanBeConfiguredAndRunAgainstSourceOneDataSource()
        {
            EngineConfig inputCfg = TestHelpers.CreateSampleConfigWithOneDataSources(TestContext, @"TestRule.cs", "Sample.resx");
            ResourceStaticAnalysisEngine engine = TestHelpers.RunEngine(inputCfg, TestContext.DeploymentDirectory);
            Assert.IsFalse(engine.Monitor.NoOfExceptionsLogged > 0, "Engine reported some execution errors.");
        }

        /// <summary>
        /// Test method to test that ResourceStaticAnalysis can be configred to run against a RESX file with Module type rule
        /// </summary>
        [TestMethod]
        public void ResourceStaticAnalysisCanBeConfiguredAndRunAgainstModuleTwoDataSources()
        {
            EngineConfig inputCfg = TestHelpers.CreateSampleConfigWithTwoDataSources(TestContext, @"UnitTests.dll", "Sample.resx");
            ResourceStaticAnalysisEngine engine = TestHelpers.RunEngine(inputCfg, TestContext.DeploymentDirectory);
            Assert.IsFalse(engine.Monitor.NoOfExceptionsLogged > 0, "Engine reported some execution errors.");
        }

        /// <summary>
        /// Test method to test that ResourceStaticAnalysis can be configred to run against a RESX file with Module type rule
        /// </summary>
        [TestMethod]
        public void ResourceStaticAnalysisCanBeConfiguredAndRunAgainstModuleOneDataSource()
        {
            EngineConfig inputCfg = TestHelpers.CreateSampleConfigWithOneDataSources(TestContext, @"UnitTests.dll", "Sample.resx");
            ResourceStaticAnalysisEngine engine = TestHelpers.RunEngine(inputCfg, TestContext.DeploymentDirectory);
            Assert.IsFalse(engine.Monitor.NoOfExceptionsLogged > 0, "Engine reported some execution errors.");
        }

        /// <summary>
        /// Test method to test that ResourceStaticAnalysis can handle basic filtering expressions
        /// </summary>
        [TestMethod]
        public void ResourceStaticAnalysisCanHandleFilteringExpressions()
        {
            var tupleResults = TestHelpers.RunEngine("Sample.resx", TestContext.DeploymentDirectory);
            ResourceStaticAnalysisApplication resourceStaticAnalysisApplication = tupleResults.Item1;
            List<Exception> exceptions = tupleResults.Item2;
            Assert.IsFalse(exceptions != null, "Engine reported some execution errors.");
            Assert.IsTrue(resourceStaticAnalysisApplication.RulesConfiguration.First().Value.FilteringExpressions.Count == 1, "Engine did not process the filtering expression defined in config file.");
        }

        [TestMethod]
        public void TestDataSourcePackageToStringMethod()
        {
            try
            {
                var dsp = new DataSourcePackage();
                var info = new DataSourceInfo();
                info.SetSourceLocation("TestPath");
                info.SetSourceType<MatchCollection>(); // Should be possible to specify an arbitrary type.
                dsp.AddDataSource(info);

                info = new DataSourceInfo();
                var dictionary = new ConfigDictionary { { "Key 1", "Value 1" }, { "Key 2", "Value 2" } };

                info.SetSourceLocation(dictionary);
                info.SetSourceType<ResourceFileDataSource>();
                dsp.AddDataSource(info);

                TestContext.WriteLine(dsp.ToString());
            }
            catch (Exception ex)
            {
                Assert.Fail("Method Failed:{0}", ex.Message);
            }
        }

        /// <summary>
        /// Test method to test that ResourceStaticAnalysisApplication can be configred to run against a RESX file using the UnitTests.dll as Rule dll.
        /// </summary>
        [TestMethod]
        public void ResourceStaticAnalysisExecutorCanBeConfiguredAndExecutesRules()
        {
            try
            {
                ResourceStaticAnalysisApplication ResourceStaticAnalysisApplication = ResourceStaticAnalysisApplication.Instance;
                List<Exception> exceptions;
                ResourceStaticAnalysisApplication.Initialize(new List<string>() { TestContext.DeploymentDirectory });     // TestContext.DeploymentDirectory will pick UnitTests.dll 
                // which contains the two rules defined here.
                // RuleThatAccessesAllLocResourceProperties and AdjectivePlusPlaceholder rules

                ResourceStaticAnalysisApplication.Execute("Sample.resx", true, out exceptions);

                Assert.IsFalse(exceptions != null, "Engine reported some execution errors.");
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

    /// <summary>
    /// Logger that can log to TestContext. Use in UnitTests methods to intercept ResourceStaticAnalysis debug logs.
    /// </summary>
    public class TcLogger : TraceListener
    {

        readonly Regex _overheadRemover = new Regex(@"\[Agent:.+'\]");
        readonly Regex _characterReplacerBecauseTestContextIsFullOfBugs = new Regex("[\0]");

        TestContext Output { get; set; }
        public TcLogger(TestContext output)
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
}