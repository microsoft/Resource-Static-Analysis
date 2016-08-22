/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;
using Microsoft.ResourceStaticAnalysis.Core.Misc;
using Microsoft.ResourceStaticAnalysis.Core.Output;
using Microsoft.ResourceStaticAnalysis.Core.Properties;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine
{
    /// <summary>
    /// Main class that stores ResourceStaticAnalysis configuration.
    /// Can be deserialized from a disk file or constructed using Object model.
    /// </summary>
    [Serializable, XmlRoot(Namespace = "http://microsoft.com/ResourceStaticAnalysis/EngineConfig.xsd")]
    public class EngineConfig
    {
        /// <summary>
        /// A list of assembly names that contain ClassificationObject types to be used by engine.
        /// </summary>
        [XmlArray("ClassificationObjectTypes"), XmlArrayItem("Type")]
        public List<TypeSpecification> COTypes { get; set; }

        /// <summary>
        /// A list of assembly names that contain DataSourceProvider types to be used by engine.
        /// </summary>
        [XmlArray("DataSourceProviderTypes"), XmlArrayItem("Type")]
        public List<TypeSpecification> DataSourceProviderTypes { get; set; }

        /// <summary>
        /// A list of assembly names that contain ClassificationObjectAdapter types to be used by engine.
        /// </summary>
        [XmlArray("ClassificationObjectAdapterTypes"), XmlArrayItem("Type")]
        public List<TypeSpecification> COAdapterTypes { get; set; }

        /// <summary>
        /// A list of assembly names that contain PropertyAdapter types to be used by engine.
        /// </summary>
        [XmlArray("PropertyAdapterTypes"), XmlArrayItem("Type")]
        public List<TypeSpecification> PropertyAdapterTypes { get; set; }

        /// <summary>
        /// Packages that define the source for data of the specified types.
        /// </summary>
        [XmlArray("DataSourcePackages"), XmlArrayItem("DataSourcePackage")]
        public List<DataSourcePackage> DataSourcePkgs { get; set; }

        /// <summary>
        /// List of rule containers (binary or .cs) to be loaded
        /// </summary>
        [XmlArray("Rules"), XmlArrayItem("RuleContainer")]
        public List<RuleContainer> RuleContainers { get; set; }

        /// <summary>
        /// References to binary (dll or exe) files that are required to successfully compile rules.
        /// </summary>
        [XmlArray("BinaryRuleReferences"), XmlArrayItem("Reference")]
        public List<string> BinaryReferences { get; set; }

        /// <summary>
        /// References to binary (dll or exe) files that are required to successfully compile rules.
        /// </summary>
        [XmlArray("SourceRuleReferences"), XmlArrayItem("Reference")]
        public List<string> SourceReferences { get; set; }

        /// <summary>
        /// Filtering expression tells ResourceStaticAnalysis only to process Classification Objects that match the expression.
        /// THis allows you to load all Classifiaction Objects into memory but only process some of them: you can still use all COs
        /// and reference them from rules but you narrow down the scope of ResourceStaticAnalysis analysis.
        /// This is a string expression to be compiled into runtime. This is typically used in XML configuration
        /// </summary>
        [XmlElement("DefaultFilteringExpression")]
        public string DefaultFilteringExpression { get; set; }
        
        /// <summary>
        /// List rules you want to disable, i.e. if multiple rules are stored in an assembly you may want to control which ones are to be ignored
        /// </summary>
        [XmlArray("DisabledRules"), XmlArrayItem("RuleName")]
        public List<string> DisabledRules { get; set; }

        /// <summary>
        /// List of directory paths containing assemblies required to resolve assembly dependencies when executing all ResourceStaticAnalysis code.
        /// </summary>
        [XmlArray("AssemblyResolverPaths"), XmlArrayItem("Path")]
        public List<string> AssemblyResolverPaths { get; set; }

        private string _tempAssemblyDirectory;
        /// <summary>
        /// Path to a directory where temporary assemblies compiled during ResourceStaticAnalysis run are to be located
        /// </summary>
        [XmlElement("TempAssemblyDirectory")]
        public string TempAssemblyDirectory
        {
            get { return _tempAssemblyDirectory; }

            set
            {
                _tempAssemblyDirectory = ExpandVariables(value);
            }
        }

        /// <summary>
        /// Configuration of output writers to be used by ResourceStaticAnalysis
        /// </summary>
        [XmlArray("Output"), XmlArrayItem("OutputConfig")]
        public List<OutputWriterConfig> OutputConfigs { get; set; }

        internal static Func<string, string> ExpandVariables = s => Environment.ExpandEnvironmentVariables(s);

        #region Object model
        /// <summary>
        /// Adds new item to <see cref="DataSourceProviderTypes"/> list.
        /// DataSourceProvider types to be used by engine to talk to specific types of data sources.
        /// <param name="provider">New data source provider specification</param>
        /// </summary>
        public void AddDataSourceProvider(TypeSpecification provider)
        {
            if (DataSourceProviderTypes == null) DataSourceProviderTypes = new List<TypeSpecification>();
            DataSourceProviderTypes.Add(provider);
        }

        /// <summary>
        /// Adds new item to <see cref="DataSourceProviderTypes"/> list.
        /// </summary>
        /// <param name="typeName">Type described by the newly added <see cref="TypeSpecification"/></param>
        /// <param name="assemblyName">Assembly containing the described type</param>
        public void AddDataSourceProvider(string typeName, string assemblyName)
        {
            if (DataSourceProviderTypes == null) DataSourceProviderTypes = new List<TypeSpecification>();
            DataSourceProviderTypes.Add(TypeSpecification.CreateSpecification(typeName, assemblyName));
        }

        /// <summary>
        /// Adds new item to <see cref="DataSourceProviderTypes"/> list. Assembly name will be guessed from the type information contained in <paramref name="providerType"/>.
        /// </summary>
        /// <param name="providerType">Type described by the newly added <see cref="TypeSpecification"/></param>
        public void AddDataSourceProvider(Type providerType)
        {
            AddDataSourceProvider(providerType.FullName, providerType.Assembly.GetName().FullName);
        }

        /// <summary>
        /// Adds new item to <see cref="DataSourceProviderTypes"/> list. Assembly name will be guessed from the type information contained in <typeparamref name="TProvider"/>.
        /// </summary>
        /// <typeparam name="TProvider">Type described by the newly added <see cref="TypeSpecification"/></typeparam>
        public void AddDataSourceProvider<TProvider>()
        {
            AddDataSourceProvider(typeof(TProvider));
        }

        /// <summary>
        /// Adds new item to <see cref="COTypes"/> list. ClassificationObject types to be used by engine to talk to specific types of data sources.
        /// <param name="coTypeSpecification">New data source provider specification</param>
        /// </summary>
        public void AddClassificationObject(TypeSpecification coTypeSpecification)
        {
            if (COTypes == null)
                COTypes = new List<TypeSpecification>();

            COTypes.Add(coTypeSpecification);
        }

        /// <summary>
        /// Adds new item to <see cref="COTypes"/> list.
        /// <param name="typeName">Type described by the newly added <see cref="TypeSpecification"/></param>
        /// <param name="assemblyName">Assembly containing the described type</param>
        /// </summary>
        public void AddClassificationObject(string typeName, string assemblyName)
        {
            if (COTypes == null)
                COTypes = new List<TypeSpecification>();
            COTypes.Add(TypeSpecification.CreateSpecification(typeName, assemblyName));
        }

        /// <summary>
        /// Adds new item to <see cref="COTypes"/> list.
        /// <param name="coType">Type to be added to <see cref="COTypes"/></param>
        /// </summary>
        public void AddClassificationObject(Type coType)
        {
            AddClassificationObject(coType.FullName, coType.Assembly.GetName().FullName);
        }
        
        /// <summary>
        /// Adds new item to <see cref="COTypes"/> list. Assembly name will be guessed from the type information contained in <typeparamref name="TObject"/>.
        /// </summary>
        /// <typeparam name="TObject">Type to be added to <see cref="COTypes"/></typeparam>
        public void AddClassificationObject<TObject>()
        {
            AddClassificationObject(typeof(TObject));
        }

        /// <summary>
        /// Adds new item to <see cref="PropertyAdapterTypes"/> list.
        /// <param name="adapter">New property adapter specification</param>
        /// </summary>
        public void AddPropertyAdapter(TypeSpecification adapter)
        {
            if (PropertyAdapterTypes == null)
                PropertyAdapterTypes = new List<TypeSpecification>();
            PropertyAdapterTypes.Add(adapter);
        }

        /// <summary>
        /// Adds new item to <see cref="PropertyAdapterTypes"/> list.
        /// <param name="typeName">Type described by the newly added <see cref="TypeSpecification"/></param>
        /// <param name="assemblyName">Assembly containing the described type</param>
        /// </summary>
        public void AddPropertyAdapter(string typeName, string assemblyName)
        {
            if (PropertyAdapterTypes == null)
                PropertyAdapterTypes = new List<TypeSpecification>();
            PropertyAdapterTypes.Add(TypeSpecification.CreateSpecification(typeName, assemblyName));
        }

        /// <summary>
        /// Adds new item to <see cref="PropertyAdapterTypes"/> list.
        /// <param name="adapterType">Type described by the newly added <see cref="TypeSpecification"/></param>
        /// </summary>
        public void AddPropertyAdapter(Type adapterType)
        {
            if (PropertyAdapterTypes == null)
                PropertyAdapterTypes = new List<TypeSpecification>();
            PropertyAdapterTypes.Add(TypeSpecification.CreateSpecification(adapterType.FullName, adapterType.Assembly.GetName().FullName));
        }

        /// <summary>
        /// Adds new item to <see cref="PropertyAdapterTypes"/> list.
        /// </summary>
        /// <typeparam name="TAdapter">Type described by the newly added <see cref="TypeSpecification"/></typeparam>
        public void AddPropertyAdapter<TAdapter>()
        {
            AddPropertyAdapter(typeof(TAdapter));
        }

        /// <summary>
        /// Adds new item to <see cref="COAdapterTypes"/> list.
        /// <param name="adapter">New CO adapter specification</param>
        /// </summary>
        public void AddCOAdapter(TypeSpecification adapter)
        {
            if (COAdapterTypes == null)
                COAdapterTypes = new List<TypeSpecification>();
            COAdapterTypes.Add(adapter);
        }

        /// <summary>
        /// Adds new item to <see cref="COAdapterTypes"/> list.
        /// <param name="typeName">Type described by the newly added <see cref="TypeSpecification"/></param>
        /// <param name="assemblyName">Assembly containing the described type</param>
        /// </summary>
        public void AddCOAdapter(string typeName, string assemblyName)
        {
            if (COAdapterTypes == null)
                COAdapterTypes = new List<TypeSpecification>();
            COAdapterTypes.Add(TypeSpecification.CreateSpecification(typeName, assemblyName));
        }

        /// <summary>
        /// Adds new item to <see cref="COAdapterTypes"/> list. Assembly name will be guessed from the type information contained in <paramref name="adapterType"/>.
        /// <param name="adapterType">Type described by the newly added <see cref="TypeSpecification"/></param>
        /// </summary>
        public void AddCOAdapter(Type adapterType)
        {
            if (COAdapterTypes == null)
                COAdapterTypes = new List<TypeSpecification>();
            COAdapterTypes.Add(TypeSpecification.CreateSpecification(adapterType.FullName, adapterType.Assembly.GetName().FullName));
        }

        /// <summary>
        /// Adds new item to <see cref="COAdapterTypes"/> list. Assembly name will be guessed from the type information contained in <typeparamref name="TAdapter"/>.
        /// </summary>
        /// <typeparam name="TAdapter">Type described by the newly added <see cref="TypeSpecification"/></typeparam>
        public void AddCOAdapter<TAdapter>()
        {
            AddCOAdapter(typeof(TAdapter));
        }

        /// <summary>
        /// Adds a reference to binary file. For example: System.Core.dll.
        /// <param name="binaryFile">Path to binary file. May contain environment variables.</param>
        /// </summary>
        public void AddBinaryReference(string binaryFile)
        {
            if (BinaryReferences == null) BinaryReferences = new List<string>();
            BinaryReferences.Add(Environment.ExpandEnvironmentVariables(binaryFile));
        }

        /// <summary>
        /// Adds a reference to binary file.
        /// <param name="sourceFile">Path to source file. May contain environment variables.</param>
        /// </summary>
        public void AddSourceReference(string sourceFile)
        {
            if (SourceReferences == null) SourceReferences = new List<string>();
            SourceReferences.Add(sourceFile);
        }
        /// <summary>
        /// Adds new rule definition to <see cref="RuleContainers"/> list.
        /// <param name="newRule">Definition of the new rule</param>
        /// </summary>
        public void AddRule(RuleContainer newRule)
        {
            if (RuleContainers == null) RuleContainers = new List<RuleContainer>();
            RuleContainers.Add(newRule);
        }

        /// <summary>
        /// Adds new data package definition to <see cref="DataSourcePkgs"/> list.
        /// <param name="newPackage">New package of data to be added</param>
        /// </summary>
        public void AddDataSourcePackage(DataSourcePackage newPackage)
        {
            if (DataSourcePkgs == null) DataSourcePkgs = new List<DataSourcePackage>();
            DataSourcePkgs.Add(newPackage);
        }

        /// <summary>
        /// Adds assembly resolver paths to the <see cref="AssemblyResolverPaths"/> list.
        /// <param name="paths">Array of directory names where dynamic rule compiler should look for assemblies used when compiling rule source code</param>
        /// </summary>
        public void AddAssemblyResolverPaths(params string[] paths)
        {
            if (paths == null) return;
            if (AssemblyResolverPaths == null) AssemblyResolverPaths = new List<string>();
            AssemblyResolverPaths.AddRange(paths);
        }

        /// <summary>
        /// Add path to where temporary assemblies are to be saved.
        /// </summary>
        /// <param name="path">Path where to save the assemblies.</param>
        public void AddTempAssemblyDirectory(string path)
        {
            TempAssemblyDirectory = path;
        }

        /// <summary>
        /// Adds configuration for an output writer.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        public void AddOutputConfig(OutputWriterConfig config)
        {
            if (OutputConfigs == null) OutputConfigs = new List<OutputWriterConfig>();
            OutputConfigs.Add(config);
        }

        /// <summary>
        /// Required for serialization. Initializes default lists that would otherwise be null.
        /// </summary>
        public EngineConfig()
        {
            BinaryReferences = new List<string>();
            SourceReferences = new List<string>();
            RuleContainers = new List<RuleContainer>();
            OutputConfigs = new List<OutputWriterConfig>();
        }

        /// <summary>
        /// Offers a convenient method for loading a serialized version of configuration into memory.
        /// This deserialized object can then be passed into ResourceStaticAnalysis at construction to allow for engine configuration to happen.
        /// <param name="pathToXml">Uri or path to Xml file that contains a serialized version of engine configuration.</param>
        /// </summary>
        public static EngineConfig Deserialize(string pathToXml)
        {
            var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
            XmlSchema schema;
            using (var configSchemaReader = XmlReader.Create(ResourceStaticAnalysisToolbox.GetConfigSchemaStream()))
            {
                schema = XmlSchema.Read(configSchemaReader, ConfigSchemaValidator);
            }
            settings.Schemas.Add(schema);
            settings.ValidationEventHandler += ConfigXmlValidationProblem;
            using (var fileReader = XmlReader.Create(pathToXml, settings))
            {
                return Deserialize(fileReader);
            }
        }

        /// <summary>
        /// Offers a convenient method for loading a serialized version of configuration into memory.
        /// This deserialized object can then be passed into ResourceStaticAnalysis at construction to allow for engine configuration to happen.
        /// <param name="configXmlReader">XmlReader that is position at the beginning of stream that reads a serialized version of engine configuration.</param>
        /// </summary>
        public static EngineConfig Deserialize(XmlReader configXmlReader)
        {
            var configDeserializer = new XmlSerializer(typeof(EngineConfig), Resources.ConfigXMLNameSpace);
            var retVal = configDeserializer.Deserialize(configXmlReader) as EngineConfig;
            return retVal;
        }

        private static void ConfigXmlValidationProblem(object sender, ValidationEventArgs e)
        {
            Trace.TraceError("Engine config error at ({0}, {1}): {2}: {3}", e.Exception.LineNumber, e.Exception.LinePosition, e.Severity, e.Message);
        }

        private static void ConfigSchemaValidator(object sender, ValidationEventArgs e)
        {
            Trace.TraceError("Schema error at ({0}, {1}): {2}: {3}", e.Exception.LineNumber, e.Exception.LinePosition, e.Severity, e.Message);
        }
        #endregion
    }

    /// <summary>
    /// Represents information about a type to be loaded from module.
    /// </summary>
    [Serializable]
    public class TypeSpecification
    {
        /// <summary>
        /// Fully specified type name, including namespaces.
        /// </summary>
        [XmlElement("TypeName")]
        public string TypeName;
        
        /// <summary>
        /// Module path is optional and can be omitted.
        /// </summary>
        [XmlElement("ModulePath")]
        public string ModulePath;
        
        /// <summary>
        /// Name of the assembly containing the indicated type.
        /// <para/>
        /// <example>LocResource - to indicate LocResource.dll</example>
        /// <para/>Note that the binary file of the assembly has to be located in the path of the executable.
        /// </summary>
        [XmlElement("AssemblyName")]
        public string AssemblyName;
        
        /// <summary>
        /// Creates type information based on type name and assembly.
        /// </summary>
        public Type GetTypeFromModule()
        {
            return Type.GetType(String.Format(CultureInfo.CurrentCulture, "{0},{1}", TypeName, AssemblyName), true, true);
        }

        #region Object model
        /// <summary>
        /// Creates a new instance of <see cref="TypeSpecification"/> using the type and assembly names as input.
        /// </summary>
        /// <param name="typeName">Type described by this <see cref="TypeSpecification"/></param>
        /// <param name="assemblyName">Assembly containing the described type</param>
        /// <returns>Type specification</returns>
        public static TypeSpecification CreateSpecification(string typeName, string assemblyName)
        {
            return new TypeSpecification { TypeName = typeName, AssemblyName = assemblyName };
        }

        /// <summary>
        /// Shows the type name and assembly name.
        /// </summary>
        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "Type={0}, Assembly={1}", TypeName, AssemblyName);
        }
        #endregion
    }

    /// <summary>
    /// Stores path to rule module (.dll) or rule source (.cs) and an optional filtering expression.
    /// Filtering Expression is used to control what Classification Objects the rule applies to.
    /// </summary>
    [Serializable]
    public class RuleContainer
    {
        private List<string> _disabledRules;
        private List<string> _enabledRules;
        private string _workingFolder;

        /// <summary>
        /// Parameterless constructor for serialization
        /// </summary>
        public RuleContainer() { }

        /// <summary>
        /// Creates an instance of a RuleContainer
        /// </summary>
        /// <param name="pathToFile">Path to rule file (.cs or .dll)</param>
        /// <param name="containerType">Type of container: compiled or source code.</param>
        /// <param name="filteringExpressionMethod">An optional filtering expression method.</param>
        public RuleContainer(string pathToFile, RuleContainerType containerType, COExpression<ClassificationObject> filteringExpressionMethod)
        {
            if (String.IsNullOrEmpty(pathToFile))
            {
                throw new ArgumentNullException("pathToFile");
            }
            this.PathToFile = pathToFile;
            this.ContainerType = containerType;
            this.FilteringExpressionMethod = filteringExpressionMethod;
        }

        /// <summary>
        /// Creates an instance of a RuleContainer
        /// </summary>
        /// <param name="pathToFile">Path to rule file (.cs or .dll)</param>
        /// <param name="containerType">Type of container: compiled or source code.</param>
        /// <param name="filteringExpression">An optional filtering expression formed as a string. This string will be complied into a COExpression of ClassificationObject method
        /// when the rule is initialized from the container. Exceptions will be thrown if compilation fails.</param>
        public RuleContainer(string pathToFile, RuleContainerType containerType, string filteringExpression)
        {
            if (String.IsNullOrEmpty(pathToFile))
            {
                throw new ArgumentNullException(nameof(pathToFile));
            }
            this.PathToFile = pathToFile;
            this.ContainerType = containerType;
            this.FilteringExpression = filteringExpression;
        }

        /// <summary>
        /// Creates an instance of a RuleContainer
        /// </summary>
        /// <param name="pathToFile">Path to rule file (.cs or .dll)</param>
        /// <param name="containerType">Type of container: compiled or source code.</param>
        /// when the rule is initialized from the container. Exceptions will be thrown if compilation fails.</param>
        public RuleContainer(string pathToFile, RuleContainerType containerType)
        {
            if (String.IsNullOrEmpty(pathToFile))
            {
                throw new ArgumentNullException(nameof(pathToFile));
            }
            this.PathToFile = pathToFile;
            this.ContainerType = containerType;
        }

        /// <summary>
        /// Type of the rule. Determines whether the rule should be compiled (for Source),
        /// or loaded from assembly (for Module).
        /// </summary>
        [XmlAttribute("type")]
        public RuleContainerType ContainerType;

        private string _pathToFile = String.Empty;

        /// <summary>
        /// Path to the file containing rule definition.
        /// </summary>
        [XmlElement("Path")]
        public string PathToFile
        {
            get { return _pathToFile; }
            set
            {
                _pathToFile = EngineConfig.ExpandVariables(value);
            }
        }

        /// <summary>
        /// Path to the folder the rule should use as it's working folder.
        /// This will generally be where the rule picks up external configuration files
        /// or other data that are used within the rule itself.
        /// </summary>
        [XmlElement("WorkingFolder")]
        public string WorkingFolder
        {
            get { return this._workingFolder; }
            set
            {
                this._workingFolder = EngineConfig.ExpandVariables(value);
            }
        }

        /// <summary>
        /// Default filtering expression to be applied to this rule. This is a string expression that is compiled into a method at runtime and is typically
        /// used when reading config from an XML file
        /// Allows preliminary filtering of ClassificationObjects before the rule looks at them.
        /// </summary>
        [XmlElement("FilteringExpression")]
        public string FilteringExpression = String.Empty;

        /// <summary>
        /// Default filtering expression to be applied to this rule. This is an already compiled method and typically used when building configuration
        /// through the ResourceStaticAnalysis configuration object model.
        /// </summary>
        [XmlIgnore]
        public COExpression<ClassificationObject> FilteringExpressionMethod;

        /// <summary>
        /// A Collection of FilteringExpressionMethods than can be applied to an individual Rule within a container
        /// without impacting other rules within that container.
        /// </summary>
        [XmlIgnore]
        public List<RuleFilteringInfo> PerRuleFilteringExpressionMethods;

        /// <summary>
        /// A collection of rule type names that should not be loaded.
        /// All other rule types are implicitly loaded.
        /// Set at the EngineConfig level.
        /// </summary>
        [XmlIgnore]
        public List<string> DisabledRules
        {
            get
            {
                if (_disabledRules == null)
                {
                    _disabledRules = new List<string>();
                }

                return _disabledRules;
            }
        }

        /// <summary>
        /// A collection of rule type names that should be loaded.
        /// All other rule types are implicitly ignored.
        /// </summary>
        [XmlIgnore]
        public List<string> EnabledRules
        {
            get
            {
                if (_enabledRules == null)
                {
                    _enabledRules = new List<string>();
                }

                return _enabledRules;
            }
        }
    }
    /// <summary>
    /// Type of rule container section specified in engine configuration.
    /// </summary>
    public enum RuleContainerType
    {
        /// <summary>
        /// Indicates it is a binary module (assembly). For example a dll containing compiled rules.
        /// </summary>
        Module,
        /// <summary>
        /// Indicates this is a source code file that needs to be dynamically compiled by ResourceStaticAnalysis.
        /// <para/>For example: Rule.cs file.
        /// </summary>
        Source
    }

    /// <summary>
    ///  Class containing filtering information for a rule.
    /// </summary>
    [Serializable]
    public class RuleFilteringInfo
    {
        /// <summary>
        /// Name (Type) of the rule to applying the filtering expression to.
        /// </summary>
        public string RuleName { get; set; }

        /// <summary>
        /// Filtering expression to be applied to the required rule.
        /// </summary>
        public COExpression<ClassificationObject> FilteringExpressionMethod { get; set; }

        /// <summary>
        /// A value indicating if existing filtering expressions should be replaced by the filtering
        /// expression or if the filtering expression should be added to the existing expressions.
        /// </summary>
        public bool ReplaceExisting { get; set; }
    }
}
