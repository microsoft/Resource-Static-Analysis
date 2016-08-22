/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using Microsoft.ResourceStaticAnalysis.Configuration;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Misc;
using Microsoft.Practices.AssemblyManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;
using Microsoft.ResourceStaticAnalysis.DataAdapter;
using Microsoft.ResourceStaticAnalysis.ResourceStaticAnalysisExecutor;

namespace UnitTests
{
    internal class TestHelpers
    {
        /// <summary>
        /// Creates an engine configuration with two Data Sources.
        /// </summary>
        /// <param name="testContext">Used to store information that is provided to unit tests.</param>
        /// <param name="testRuleFileName">Name of the C# file that contains the Rule implementaiton.</param>
        /// <param name="testResourceFile">Resource File used to run the engine.</param>
        /// <returns>The configuration of the engine with two DataSources.</returns>
        internal static EngineConfig CreateSampleConfigWithTwoDataSources(TestContext testContext,
            string testRuleFileName, string testResourceFile)
        {
            testRuleFileName = Path.Combine(testContext.DeploymentDirectory, testRuleFileName);
            testResourceFile = Path.Combine(testContext.DeploymentDirectory, testResourceFile);
            var configuration = new EngineConfig();

            #region DataSourceProviderTypes

            configuration.AddDataSourceProvider<ResourceFileDataSource>();
            configuration.AddDataSourceProvider<ConfigDictionaryDataSource>();

            #endregion

            #region PropertyAdapterTypes

            configuration.AddPropertyAdapter<ResourceFileEntryPropertyAdapter>();
            configuration.AddPropertyAdapter<ConfigDictPropertyAdapter>();
            configuration.AddPropertyAdapter<LocResourceSelfPropertyAdapter>();
            
            #endregion

            #region COAdatpers

            configuration.AddCOAdapter<ResourceFileDataAdapter>();

            #endregion

            #region COTypes

            configuration.AddClassificationObject<LocResource>();

            #endregion

            #region DataSourcePackages

            var package = new DataSourcePackage(); // This package will contain 2 data sources
            package.SetCOType<LocResource>();

            var dataSource = new DataSourceInfo();
            dataSource.SetSourceType(typeof(ResourceFile));
            dataSource.SetSourceLocation(Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                testResourceFile));
            package.AddDataSource(dataSource);

            dataSource = new DataSourceInfo();
            dataSource.SetSourceType<ConfigDictionary>();
            var configDictionary = new ConfigDictionary
            {
                {"Project", "test RSA"},
            };
            dataSource.SetSourceLocation(configDictionary);
            package.AddDataSource(dataSource);

            configuration.AddDataSourcePackage(package);

            #endregion

            #region Add rules

            RuleContainerType rct = new RuleContainerType();
            if (testRuleFileName.EndsWith(".cs"))
            {
                rct = RuleContainerType.Source;
            }
            else
            {
                rct = RuleContainerType.Module;
            }

            configuration.AddRule(new RuleContainer(testRuleFileName, rct));

            #endregion

            #region And some configuration for dynamic compiler

            configuration.AddBinaryReference("System.Core.dll");
            configuration.AddBinaryReference("mscorlib.dll");
            configuration.AddBinaryReference("System.dll");
            configuration.AddBinaryReference("Microsoft.ResourceStaticAnalysis.Core.dll");
            configuration.AddBinaryReference("Microsoft.ResourceStaticAnalysis.LocResource.dll");
            configuration.AddBinaryReference("Microsoft.ResourceStaticAnalysis.DataAdapter.dll");

            #endregion

            return configuration;
        }

        /// <summary>
        /// Creates an engine configuration with one Data Source.
        /// </summary>
        /// <param name="testContext">Used to store information that is provided to unit tests.</param>
        /// <param name="testRuleFileName">Name of the C# file that contains the Rule implementaiton.</param>
        /// <param name="testResourceFile">Resource File used to run the engine.</param>
        /// <returns>The configuration of the engine with one DataSource.</returns>
        internal static EngineConfig CreateSampleConfigWithOneDataSources(TestContext testContext,
            string testRuleFileName, string testResourceFile)
        {
            testRuleFileName = Path.Combine(testContext.DeploymentDirectory, testRuleFileName);
            testResourceFile = Path.Combine(testContext.DeploymentDirectory, testResourceFile);
            var configuration = new EngineConfig();

            #region DataSourceProviderTypes

            configuration.AddDataSourceProvider<ResourceFileDataSource>();

            #endregion

            #region PropertyAdapterTypes

            configuration.AddPropertyAdapter<ResourceFileEntryPropertyAdapter>();
            configuration.AddPropertyAdapter<LocResourceSelfPropertyAdapter>();

            #endregion

            #region COAdatpers

            configuration.AddCOAdapter<ResourceFileDataAdapter>();

            #endregion

            #region COTypes

            configuration.AddClassificationObject<LocResource>();

            #endregion

            #region DataSourcePackages

            var package = new DataSourcePackage(); // This package will contain 1 data sources
            package.SetCOType<LocResource>();

            var dataSource = new DataSourceInfo();
            dataSource.SetSourceType(typeof(ResourceFile));
            dataSource.SetSourceLocation(Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                testResourceFile));
            package.AddDataSource(dataSource);

            configuration.AddDataSourcePackage(package);

            #endregion

            #region Add rules

            RuleContainerType rct = new RuleContainerType();
            if (testRuleFileName.EndsWith(".cs"))
            {
                rct = RuleContainerType.Source;
            }
            else
            {
                rct = RuleContainerType.Module;
            }

            configuration.AddRule(new RuleContainer(testRuleFileName, rct));

            #endregion

            #region And some configuration for dynamic compiler

            configuration.AddBinaryReference("System.Core.dll");
            configuration.AddBinaryReference("mscorlib.dll");
            configuration.AddBinaryReference("System.dll");
            configuration.AddBinaryReference("Microsoft.ResourceStaticAnalysis.Core.dll");
            configuration.AddBinaryReference("Microsoft.ResourceStaticAnalysis.LocResource.dll");
            configuration.AddBinaryReference("Microsoft.ResourceStaticAnalysis.DataAdapter.dll");

            #endregion

            return configuration;
        }

        /// <summary>
        /// Method that runs the ResourceStaticAnalysis.
        /// </summary>
        /// <param name="config">Object that contains the configuration of the engine.</param>
        /// <param name="deploymentDirectory">Unit test directory where the engine and its dependencies gets deployed.</param>
        /// <returns>The engine object if succeeds. Null if not.</returns>
        internal static ResourceStaticAnalysisEngine RunEngine(EngineConfig config, string deploymentDirectory)
        {
            try
            {
                var assemblyResolver = new AssemblyResolver(deploymentDirectory);
                assemblyResolver.Init();

                var engine = new ResourceStaticAnalysisEngine(config);
                if (engine.StartRun())
                {
                    engine.WaitForJobFinish();
                }

                return engine;
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }
            catch (ResourceStaticAnalysisEngineInitializationException ex) // Thrown when there are no rules to run on the input set.
            {
                Assert.Fail("ResourceStaticAnalysis engine failed because {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Execute the engine that is configured through ResourceStaticAnalysisExecutor project. <see cref="ResourceStaticAnalysisApplication"/>
        /// </summary>
        /// <param name="resourceFile">Resource File used to run the engine.</param>
        /// <param name="checksFolder">Unit Test folder where we have the dll dependencies with the rules built and config file.</param>
        /// <returns></returns>
        internal static Tuple<ResourceStaticAnalysisApplication, List<Exception>> RunEngine(string resourceFile, string checksFolder)
        {
            ResourceStaticAnalysisApplication resourceStaticAnalysisApplication = ResourceStaticAnalysisApplication.Instance;
            List<Exception> exceptions = new List<Exception>();
            resourceStaticAnalysisApplication.Initialize(new List<string>() { checksFolder });
            resourceStaticAnalysisApplication.Execute(resourceFile, false, out exceptions);

            return new Tuple<ResourceStaticAnalysisApplication, List<Exception>>(resourceStaticAnalysisApplication, exceptions);
        }
    }
}