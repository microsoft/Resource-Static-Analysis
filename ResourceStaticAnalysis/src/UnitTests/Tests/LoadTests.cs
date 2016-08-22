/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Practices.AssemblyManagement;
using Microsoft.ResourceStaticAnalysis.Core.Engine;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.Tests
{
    [TestClass]
    [DeploymentItem(@"ResourceStaticAnalysisOutput.xsd")]
    public class LoadTests
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
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

        /// <summary>
        /// Test method to test that ResourceStaticAnalysis can be configred and run against a module (.dll)
        /// Since Rules are each run on separate threads, multi-rule modules access multi-threading access to cache
        /// </summary>
        [TestMethod]
        [DeploymentItem(@"UnitTests.dll")]
        public void ResourceStaticAnalysisCanHandleLargeVolumeOfResources()
        {
            try
            {
                var assemblyResolver = new AssemblyResolver(TestContext.DeploymentDirectory);
                assemblyResolver.Init();
                var inputCfg = TestHelpers.CreateSampleConfig(TestContext, @"UnitTests.dll", 1000000);
                var engine = new ResourceStaticAnalysisEngine(inputCfg);
                if (engine.StartRun())
                {
                    engine.WaitForJobFinish();
                }
                Assert.IsFalse(engine.Monitor.NoOfExceptionsLogged > 0, "Engine reported some execution errors.");
                Assert.IsTrue(engine.Monitor.TotalNumResults > 100000, "Rule results were not as expected");
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
