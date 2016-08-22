/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.Configuration;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Misc;
using Microsoft.ResourceStaticAnalysis.Core.Output;
using Microsoft.ResourceStaticAnalysis.Core.Output.Specialized;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;
using Microsoft.ResourceStaticAnalysis.DataAdapter;

namespace Microsoft.ResourceStaticAnalysis.ResourceStaticAnalysisExecutor
{
    /// <summary>
    /// A class used to configure, initiate and execute the ResourceStaticAnalysis.
    /// Uses a thread-safe singleton pattern
    /// </summary>
    public sealed class ResourceStaticAnalysisApplication : ResourceStaticAnalysisConfig
    {
        private static readonly Lazy<ResourceStaticAnalysisApplication> instance = new Lazy<ResourceStaticAnalysisApplication>(() => new ResourceStaticAnalysisApplication());
        
        private string projectName = "Unknown";

        /// <summary>
        /// Only instance of the ResourceStaticAnalysisApplication class.
        /// </summary>
        public static ResourceStaticAnalysisApplication Instance
        {
            get
            {
                return instance.Value;
            }
        }

        /// <summary>
        /// Constructor for the ResourceStaticAnalysisApplication class.
        /// </summary>
        private ResourceStaticAnalysisApplication()
        {

        }

        /// <summary>
        /// Creates the configuration to be used by the ResourceStaticAnalysis.
        /// </summary>
        /// <returns>EngineConfig.</returns>
        protected override EngineConfig CreateEngineConfig(IEnumerable<RuleContainer> ruleContainers)
        {
            EngineConfig configuration = new EngineConfig();

            //DataSourceProviderTypes
            configuration.AddDataSourceProvider(typeof(ResourceFileDataSource));
            configuration.AddDataSourceProvider<ConfigDictionaryDataSource>();

            //PropertyAdapterTypes
            configuration.AddPropertyAdapter(typeof(ResourceFileEntryPropertyAdapter));
            configuration.AddPropertyAdapter<ConfigDictPropertyAdapter>();
            configuration.AddPropertyAdapter<LocResourceSelfPropertyAdapter>();

            //COAdatpers
            configuration.AddCOAdapter(typeof(ResourceFileDataAdapter));

            //COTypes
            configuration.AddClassificationObject<LocResource>();

            // Create a package
            var package = new DataSourcePackage();
            // Set the type of CO that the package provides
            package.SetCOType<LocResource>();
            // Create a data source to be part of the package
            var dataSource = new DataSourceInfo();
            // Set the type of data source
            dataSource.SetSourceType(typeof(ResourceFile));
            // Add data source to the package
            package.AddDataSource(dataSource);
            // Create another data source
            dataSource = new DataSourceInfo();
            // Set type to be a ConfigDictionary - some custom object
            dataSource.SetSourceType<ConfigDictionary>();
            // Create an instance of the ConfigDictionary object
            var staticProperties = new ConfigDictionary
                                               {
                                                   {"Project", projectName}
                                               };
            // Set the location. in this case the loaction is the object itself
            dataSource.SetSourceLocation(staticProperties);
            // Add data source to package
            package.AddDataSource(dataSource);
            // Add the package to the configuration
            configuration.AddDataSourcePackage(package);

            // Add the rules
            if (ruleContainers != null)
            {
                ruleContainers.ToList().ForEach(r => configuration.AddRule(r));
            }

            return configuration;
        }

        protected override COExpression<ClassificationObject> CreateFilteringExpression(ResourceStaticAnalysisRulesConfig.ValueList cultures, ResourceStaticAnalysisRulesConfig.ValueList projects, List<ResourceStaticAnalysisRulesConfig.FilteringExpression> filteringExpressions)
        {
            // This data structure does not support cultres and projects.
            List<COExpression<ClassificationObject>> filteringMethods = new List<COExpression<ClassificationObject>>();

            COExpression<LocResource> mergedFilter = lr => filteringMethods.TrueForAll(method => method(lr));
            return ExpressionCasting<LocResource>.ExpressionCast(mergedFilter);
        }

        /// <summary>
        /// Configures the Output for the ResourceStaticAnalysis.
        /// </summary>
        /// <param name="outputFile">The file to write output to</param>
        public void ConfigureOutput(string outputFile)
        {
            OutputWriterConfig outputCfg = new OutputWriterConfig();
            outputCfg.SetDataSourceProvider<XMLDOMOutputWriter>();
            outputCfg.Schema = "ResourceStaticAnalysisOutput.xsd";

            // Configure where the log file is created.
            string outputDirectory = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(outputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                catch (Exception ex)
                {
                    throw new IOException(ex.ToString());
                }
            }

            outputCfg.Path = outputFile;
            // Add properties to include in output
            outputCfg.AddPropertyToIncludeInOutput("SourceString");
            outputCfg.AddPropertyToIncludeInOutput("ResourceId");
            outputCfg.AddPropertyToIncludeInOutput("Comments");

            this.EngineConfiguration.OutputConfigs.Add(outputCfg);
        }

        /// <summary>
        /// Sets the Project property for this class which will be used as a static property
        /// for each LocResource object in the file
        /// </summary>
        /// <param name="name">The name of the Project</param>
        public void SetProjectName(string name)
        {
            projectName = name;
        }
    }
}
