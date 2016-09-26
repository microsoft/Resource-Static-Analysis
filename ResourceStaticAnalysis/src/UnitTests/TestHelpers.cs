/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ResourceStaticAnalysis.UnitTests.TestObjectStructure;
using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Output;
using Microsoft.ResourceStaticAnalysis.Core.Output.Specialized;

namespace Microsoft.ResourceStaticAnalysis.UnitTests
{
    internal class TestHelpers
    {
        /// <summary>
        /// Creates a sample Engine configuration
        /// </summary>
        /// <param name="testContext">VS Test Context</param>
        /// <param name="testRuleFileName">Rule to be used (.dll or .cs)</param>
        /// <param name="numberOfResources">Number of Resources to generate (in increments of 6)</param>
        /// <returns></returns>
        internal static EngineConfig CreateSampleConfig(TestContext testContext, string testRuleFileName, int numberOfResources)
        {
            testRuleFileName = Path.Combine(testContext.DeploymentDirectory, testRuleFileName);

            var configuration = new EngineConfig();

            #region DataSourceProviderTypes
            configuration.AddDataSourceProvider(typeof(SampleDataSource));
            #endregion

            #region PropertyAdapterTypes
            configuration.AddPropertyAdapter<SamplePropertyAdapter>();
            configuration.AddPropertyAdapter<SampleClassificationObjectSelfPropertyAdapter>();
            #endregion

            #region COAdatpers
            configuration.AddCOAdapter(typeof(SampleDataAdapter));
            #endregion

            #region COTypes
            configuration.AddClassificationObject<SampleClassificationObjectType>();
            #endregion

            #region DataSourcePackages
            var package = new DataSourcePackage(); // This package will contain 2 data sources
            package.SetCOType<SampleClassificationObjectType>();

            var dataSource = new DataSourceInfo();
            dataSource.SetSourceType<SampleResourceCollection>();

            List<Tuple<string, string, string>> sampleResources = GenerateLargeNumberOfResources(numberOfResources);
            dataSource.SetSourceLocation(sampleResources);
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
            configuration.AddBinaryReference("UnitTests.dll");
            #endregion

            #region Configure output behavior
            var outputCfg = new OutputWriterConfig();
            outputCfg.SetDataSourceProvider<XMLDOMOutputWriter>();
            outputCfg.Schema = "ResourceStaticAnalysisOutput.xsd";
            outputCfg.Path = testContext.TestRunDirectory;
            outputCfg.AddPropertyToIncludeInOutput("ResourceId");
            outputCfg.AddPropertyToIncludeInOutput("SourceString");
            outputCfg.AddPropertyToIncludeInOutput("Comments");
            configuration.OutputConfigs.Add(outputCfg);
            #endregion

            return configuration;
        }

        /// <summary>
        /// Generate a List of <see cref="ClassificationObject"/>.
        /// </summary>
        /// <param name="numberOfResources">Number of Resources to generate (in increments of 6)</param>
        /// <returns>A list of <see cref="ClassificationObject"/>.</returns>
        internal static List<ClassificationObject> GenerateParesdCOs(int numberOfResources)
        {
            return ParseResStrInfoToCOs(GenerateLargeNumberOfResources(numberOfResources)).Cast<ClassificationObject>().ToList();
        }

        /// <summary>
        /// Generates a List of Tuple to be converted to ResourceEntries during execution (minimum of 6)
        /// </summary>
        /// <param name="numResources"></param>
        /// <returns></returns>
        private static List<Tuple<string, string, string>> GenerateLargeNumberOfResources(int numResources)
        {
            if (numResources < 6)
            {
                throw new Exception("Method generates a minimum of 6 Tuples");
            }
            List<Tuple<string, string, string>> results = new List<Tuple<string, string, string>>();
            for (int i = 1; i <= numResources; i += 6)
            {
                results.Add(new Tuple<string, string, string>("Item" + i.ToString(), "foo", "descriptive comment"));
                results.Add(new Tuple<string, string, string>("Item" + (i + 1).ToString(), "foo", "foo"));
                results.Add(new Tuple<string, string, string>("Item" + (i + 2).ToString(), "foo", "foo"));
                results.Add(new Tuple<string, string, string>("Item" + (i + 3).ToString(), "foobar", "foobar"));
                results.Add(new Tuple<string, string, string>("Item" + (i + 4).ToString(), "foobar", "foo"));
                results.Add(new Tuple<string, string, string>("Item" + (i + 5).ToString(), "xyz", "no vowels"));
            }

            return results;
        }

        /// <summary>
        /// This method is used to parse a list of tuple to a list of <see cref="SampleClassificationObjectType"/>.
        /// </summary>
        /// <param name="sampleResources">A list of tuple to be parsed.</param>
        /// <returns>A list of <see cref="SampleClassificationObjectType"/>.</returns>
        private static List<SampleClassificationObjectType> ParseResStrInfoToCOs(List<Tuple<string, string, string>> resourceTuples)
        {
            SampleResourceCollection sampleResourceCollection = new SampleResourceCollection(resourceTuples);

            List<SampleClassificationObjectType> sampleClassificationObjects = new List<SampleClassificationObjectType>();

            SamplePropertyAdapter samplePropertyAdapter = new SamplePropertyAdapter();
            sampleResourceCollection.Entries.ForEach(sampleResource =>
            {
                PropertyProvider propertyProvider = new PropertyProvider();
                propertyProvider.AddPropertyAdapterDataSourcePair(samplePropertyAdapter, sampleResource);
                SampleClassificationObjectType sampleClassificationObject = new SampleClassificationObjectType(propertyProvider);
                sampleClassificationObjects.Add(sampleClassificationObject);
            });

            return sampleClassificationObjects;
        }
    }
}
