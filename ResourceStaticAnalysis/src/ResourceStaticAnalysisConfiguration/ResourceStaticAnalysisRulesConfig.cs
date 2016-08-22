/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.Configuration
{
    /// <summary>
    /// Class to hold the configuration of a rule container and its rules.
    /// </summary>
    [Serializable]
    [XmlRoot(RuleConfigurationElementName)]
    public class ResourceStaticAnalysisRulesConfig : IXmlSerializable
    {
        public const String ConfigExtension = ".config";

        private const String RuleConfigurationElementName = "RuleConfiguration";
        private const String RuleElementName = "Rule";
        private const String FilteringExpressionsElementName = "FilteringExpressions";
        private const String FilteringExpressionElementName = "FilteringExpression";
        private const String CulturesElementName = "cultures";
        private const String ProjectsElementName = "projects";

        private const String EnabledAttributeName = "enabled";
        private const String PathAttributeName = "path";
        private const String WorkingFolderAttributeName = "workingFolder";
        private const String TypeAttributeName = "type";
        private const String OverrideContainerFilteringAttributeName = "overrideContainerFiltering";
        private const String SeverityAttributeName = "severity";
        private const String NegateAttributeName = "negate";

        /// <summary>
        /// Load the configuration from the specified file path.
        /// </summary>
        /// <param name="filePath">Path to the file containing the configuration.</param>
        /// <returns>ResourceStaticAnalysisRulesConfig object.</returns>
        public static ResourceStaticAnalysisRulesConfig Load(String filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath", "FilePath can't be null!");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Unable to find the specified file.", filePath);
            }

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ResourceStaticAnalysisRulesConfig config = Load(XmlReader.Create(fs));
                if (config != null)
                {
                    config.PathToConfigFile = filePath;
                }

                return config;
            }
        }

        /// <summary>
        /// Load the configuration from the specified xml reader.
        /// </summary>
        /// <param name="reader">XmlReader.</param>
        /// <returns>ResourceStaticAnalysisRulesConfig object.</returns>
        public static ResourceStaticAnalysisRulesConfig Load(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader", "Reader can't be null!");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(ResourceStaticAnalysisRulesConfig), new XmlRootAttribute(RuleConfigurationElementName));

            try
            {
                while (reader.Read())
                {
                    if (String.Equals(reader.Name, RuleConfigurationElementName))
                    {
                        return serializer.Deserialize(reader) as ResourceStaticAnalysisRulesConfig;
                    }
                }
            }
            catch { /*Ignore this exception*/ }

            return null;
        }

        /// <summary>
        /// Writes the configuration to the specified file path.
        /// </summary>
        /// <param name="filePath">Path to the file to save the configuration in.</param>
        public void Save(String filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath", "FilePath can't be null!");
            }

            //Create the directory structure for the file if it doesn't exist.
            String directory = System.IO.Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(ResourceStaticAnalysisRulesConfig));
            XmlWriterSettings settings = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true };
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add(String.Empty, String.Empty);

            using (XmlWriter xmlWriter = XmlWriter.Create(filePath, settings))
            {
                serializer.Serialize(xmlWriter, this, ns);
            }

            this.PathToConfigFile = filePath;
        }

        /// <summary>
        /// Initializes a new instance of the ResourceStaticAnalysisRulesConfig class.
        /// </summary>
        public ResourceStaticAnalysisRulesConfig()
        {
            this.Rules = new List<Rule>();
            this.FilteringExpressions = new List<FilteringExpression>();
            this.Cultures = new ValueList();
            this.Projects = new ValueList();
            this.Enabled = true;
        }

        /// <summary>
        /// Initializes a new instance of the ResourceStaticAnalysisRulesConfig class.
        /// </summary>
        /// <param name="path">Path to the assembly for which the config file is created for.</param>
        public ResourceStaticAnalysisRulesConfig(String path)
            : this()
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The path to the ResourceStaticAnalysis rule assembly cannot be null or empty", "path");
            }

            this.Path = path;
        }

        /// <summary>
        /// Path to the assembly.
        /// </summary>
        [XmlAttribute(PathAttributeName)]
        public String Path { get; set; }

        /// <summary>
        /// True if the rule container is enabled, false otherwise.
        /// </summary>
        [XmlAttribute(EnabledAttributeName)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Path to the working folder the assembly should use.
        /// </summary>
        [XmlAttribute(WorkingFolderAttributeName)]
        public String WorkingFolder { get; set; }

        /// <summary>
        /// Type of rule container.
        /// </summary>
        [XmlAttribute(TypeAttributeName)]
        public String Type { get; set; }

        /// <summary>
        /// Collection of Rules.
        /// </summary>
        [XmlElement(RuleElementName)]
        public List<Rule> Rules { get; set; }

        /// <summary>
        /// True if the Rules element is specified (and serialized during saving).
        /// </summary>
        private bool RulesSpecified
        {
            get { return this.Rules != null && this.Rules.Any(r => r.RuleSpecified); }
        }

        /// <summary>
        /// Collection of Filtering expressions used to control when a rule is executed on a resource.
        /// </summary>
        [XmlArray]
        [XmlArrayItem(ElementName = FilteringExpressionElementName)]
        public List<FilteringExpression> FilteringExpressions { get; set; }

        /// <summary>
        /// True if the FilteringExpressions element is specified (and serialized during saving).
        /// </summary>
        private bool FilteringExpressionsSpecified
        {
            get { return this.FilteringExpressions != null && this.FilteringExpressions.Any(); }
        }

        /// <summary>
        /// Cultures that the rule container should run or not run on.
        /// </summary>
        [XmlElement(CulturesElementName)]
        public ValueList Cultures { get; set; }

        /// <summary>
        /// True if the Cultures element is specified (and serialized during saving).
        /// </summary>
        private bool CulturesSpecified
        {
            get { return !String.IsNullOrWhiteSpace(this.Cultures.Value); }
        }

        /// <summary>
        /// Projects that the rule container should run or not run on.
        /// </summary>
        [XmlElement(ProjectsElementName)]
        public ValueList Projects { get; set; }

        /// <summary>
        /// True if the Projects element is specified (and serialized during saving).
        /// </summary>
        private bool ProjectsSpecified
        {
            get { return !String.IsNullOrWhiteSpace(this.Projects.Value); }
        }

        /// <summary>
        /// The path to the configuration file that this object was loaded from.
        /// </summary>
        /// <remarks>This can be null if the config was loaded from a stream, or built in-memory without saving.</remarks>
        [XmlIgnore]
        internal String PathToConfigFile { get; private set; }

        /// <summary>
        /// Get the path to the assembly being configured.
        /// </summary>
        /// <returns>Path to the assembly.</returns>
        internal String GetPathToAssembly()
        {
            //If the path to the rule assembly is relative - then it should be relative to where the 
            //config was loaded from.
            String ruleAssemblyPath = Environment.ExpandEnvironmentVariables(this.Path);
            if (!System.IO.Path.IsPathRooted(ruleAssemblyPath) && !String.IsNullOrWhiteSpace(this.PathToConfigFile))
            {
                ruleAssemblyPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.PathToConfigFile), ruleAssemblyPath);
            }

            return ruleAssemblyPath;
        }

        /// <summary>
        /// An XmlSchema that describes the XML representation of the object that is produced by the 
        /// WriteXml method and consumed by the ReadXml method.
        /// </summary>
        /// <returns>This method returns NULL.</returns>
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The XmlReader stream from which the object is deserialized.</param>
        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();

            this.Path = reader.GetAttribute(PathAttributeName);
            this.Type = reader.GetAttribute(TypeAttributeName);
            this.WorkingFolder = reader.GetAttribute(WorkingFolderAttributeName);

            bool enabled;
            if (Boolean.TryParse(reader.GetAttribute(EnabledAttributeName), out enabled))
            {
                this.Enabled = enabled;
            }

            bool isEmpty = reader.IsEmptyElement;
            if (!isEmpty)
            {
                reader.ReadStartElement();
                while (reader.IsStartElement())
                {
                    XmlSerializer xmlSerializer;
                    if (String.Equals(reader.Name, RuleElementName, StringComparison.Ordinal))
                    {
                        xmlSerializer = new XmlSerializer(typeof(Rule), new XmlRootAttribute(RuleElementName));
                        Rule rule = xmlSerializer.Deserialize(reader) as Rule;
                        if (rule != null)
                        {
                            this.Rules.Add(rule);
                        }
                    }
                    else if (String.Equals(reader.Name, FilteringExpressionsElementName, StringComparison.Ordinal))
                    {
                        xmlSerializer = new XmlSerializer(typeof(List<FilteringExpression>), new XmlRootAttribute(FilteringExpressionsElementName));
                        this.FilteringExpressions = (List<FilteringExpression>)xmlSerializer.Deserialize(reader);
                    }
                    else if (String.Equals(reader.Name, CulturesElementName, StringComparison.Ordinal))
                    {
                        xmlSerializer = new XmlSerializer(typeof(ValueList), new XmlRootAttribute(CulturesElementName));
                        this.Cultures = xmlSerializer.Deserialize(reader) as ValueList;
                    }
                    else if (String.Equals(reader.Name, ProjectsElementName, StringComparison.Ordinal))
                    {
                        xmlSerializer = new XmlSerializer(typeof(ValueList), new XmlRootAttribute(ProjectsElementName));
                        this.Projects = xmlSerializer.Deserialize(reader) as ValueList;
                    }
                    else
                    {
                        //Unknown item - just skip it.
                        reader.Read();
                    }
                }
                reader.ReadEndElement();
            }
            else
            {
                reader.Read();
            }
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The XmlWriter stream to which the object is serialized.</param>
        public void WriteXml(XmlWriter writer)
        {
            XmlSerializerNamespaces nameSpaces = new XmlSerializerNamespaces();
            nameSpaces.Add(String.Empty, String.Empty);

            if (!String.IsNullOrWhiteSpace(this.Path))
            {
                writer.WriteAttributeString(PathAttributeName, this.GetPathToAssembly());
            }

            if (!String.IsNullOrWhiteSpace(this.Type))
            {
                writer.WriteAttributeString(TypeAttributeName, this.Type);
            }

            if (!this.Enabled)
            {
                writer.WriteAttributeString(EnabledAttributeName, this.Enabled.ToString());
            }

            if (!String.IsNullOrWhiteSpace(this.WorkingFolder))
            {
                writer.WriteAttributeString(WorkingFolderAttributeName, this.WorkingFolder);
            }

            if (this.RulesSpecified)
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Rule));
                foreach (Rule rule in this.Rules.Where(r => r.RuleSpecified))
                {
                    xmlSerializer.Serialize(writer, rule, nameSpaces);
                }
            }

            if (this.FilteringExpressionsSpecified)
            {
                (new XmlSerializer(typeof(List<FilteringExpression>), new XmlRootAttribute(FilteringExpressionsElementName)))
                    .Serialize(writer, this.FilteringExpressions, nameSpaces);
            }

            if (this.CulturesSpecified)
            {
                (new XmlSerializer(typeof(ValueList), new XmlRootAttribute(CulturesElementName)))
                    .Serialize(writer, this.Cultures, nameSpaces);
            }

            if (this.ProjectsSpecified)
            {
                (new XmlSerializer(typeof(ValueList), new XmlRootAttribute(ProjectsElementName)))
                    .Serialize(writer, this.Projects, nameSpaces);
            }
        }

        /// <summary>
        /// Class to hold the configuration of a rule.
        /// </summary>
        [Serializable]
        public class Rule
        {
            /// <summary>
            /// Initializes a new instance of the Rule class. 
            /// </summary>
            public Rule()
            {
                this.Enabled = true;
                this.Cultures = new ValueList();
                this.Projects = new ValueList();
                this.FilteringExpressions = new List<FilteringExpression>();
            }

            public Rule(String type)
                : this()
            {
                this.Type = type;
            }

            /// <summary>
            /// Fully qualified type of the rule.
            /// </summary>
            [XmlAttribute(TypeAttributeName)]
            public String Type { get; set; }

            /// <summary>
            /// True if the rule is enabled, false otherwise.
            /// </summary>
            [XmlAttribute(EnabledAttributeName)]
            public bool Enabled { get; set; }

            /// <summary>
            /// True if the Enabled attribute is specified (and serialized during saving).
            /// </summary>
            public bool EnabledSpecified
            {
                get { return !this.Enabled; }
            }

            /// <summary>
            /// True if the rule filtering should replace any existing filtering
            /// or should be appended to the existing filtering.
            /// </summary>
            [XmlAttribute(OverrideContainerFilteringAttributeName)]
            public bool OverrideContainerFiltering { get; set; }

            /// <summary>
            /// True if the OverrideContainerFiltering attribute is specified (and serialized during saving).
            /// </summary>
            public bool OverrideContainerFilteringSpecified
            {
                get { return this.OverrideContainerFiltering; }
            }

            /// <summary>
            /// Collection of Filtering expressions used to control when a rule is executed on a resource.
            /// </summary>
            [XmlArray]
            [XmlArrayItem(ElementName = FilteringExpressionElementName)]
            public List<FilteringExpression> FilteringExpressions { get; set; }

            /// <summary>
            /// True if the FilteringExpressions element is specified (and serialized during saving).
            /// </summary>
            public bool FilteringExpressionsSpecified
            {
                get { return this.FilteringExpressions != null && this.FilteringExpressions.Any(); }
            }

            /// <summary>
            /// Cultures that the rule container should run or not run on.
            /// </summary>
            [XmlElement(CulturesElementName)]
            public ValueList Cultures { get; set; }

            /// <summary>
            /// True if the Cultures element is specified (and serialized during saving).
            /// </summary>
            public bool CulturesSpecified
            {
                get { return !String.IsNullOrWhiteSpace(this.Cultures.Value); }
            }

            /// <summary>
            /// Projects that the rule should run or not run on.
            /// </summary>
            [XmlElement(ProjectsElementName)]
            public ValueList Projects { get; set; }

            /// <summary>
            /// True if the Projects element is specified (and serialized during saving).
            /// </summary>
            public bool ProjectsSpecified
            {
                get { return !String.IsNullOrWhiteSpace(this.Projects.Value); }
            }

            /// <summary>
            /// Severity to report the rule as.
            /// </summary>
            [XmlAttribute(SeverityAttributeName)]
            public CheckSeverity Severity { get; set; }

            /// <summary>
            /// True if the Severity attrinute is specified (and serialized during saving).
            /// </summary>
            public bool SeveritySpecified
            {
                get { return this.Severity != CheckSeverity.None; }
            }

            /// <summary>
            /// Name of the Rule.
            /// </summary>
            [XmlIgnore]
            public String Name { get; internal set; }

            /// <summary>
            /// Description of the Rule.
            /// </summary>
            [XmlIgnore]
            public String Description { get; internal set; }

            /// <summary>
            /// Category of the Rule.
            /// </summary>
            [XmlIgnore]
            public String Category { get; internal set; }

            /// <summary>
            /// True if any of the properties of the Rule are specified.
            /// </summary>
            /// <remarks>Controls if the Rule object is serialized or not. If no properties of the rule are specified other than
            /// the type property then the rule object will not be serialized.</remarks>
            internal bool RuleSpecified
            {
                get
                {
                    return (this.EnabledSpecified || this.OverrideContainerFilteringSpecified || this.SeveritySpecified ||
                        this.FilteringExpressionsSpecified || this.CulturesSpecified || this.ProjectsSpecified);
                }
            }
        }

        /// <summary>
        /// Class that holds a string value that can be negated.
        /// </summary>
        [Serializable]
        public class ValueList
        {
            /// <summary>
            /// True if the value should be negated.
            /// </summary>
            [XmlAttribute(NegateAttributeName)]
            public bool Negate { get; set; }

            /// <summary>
            /// True if the Negate attribute is specified (and serialized during saving).
            /// </summary>
            public bool NegateSpecified
            {
                get { return this.Negate; }
            }

            /// <summary>
            /// Value.
            /// </summary>
            [XmlText]
            public String Value { get; set; }

            /// <summary>
            /// Get the value of the instance as a List with any macros contained within the value expanded.
            /// </summary>
            /// <returns></returns>
            public IEnumerable<String> GetValues()
            {
                if (String.IsNullOrWhiteSpace(this.Value))
                {
                    return Enumerable.Empty<String>();
                }

                return ResourceStaticAnalysisConfigMacroHandler.Instance.ExpandMacros(this.Value).Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Distinct();
            }
        }

        /// <summary>
        /// Class that represents an ResourceStaticAnalysis Classification Filtering string.
        /// </summary>
        [Serializable]
        public class FilteringExpression : IComparable<FilteringExpression>, IComparable
        {
            /// <summary>
            /// Initializes a new instance of a FilteringExpression object.
            /// </summary>
            public FilteringExpression()
            {
            }

            /// <summary>
            /// Initializes a new instance of a FilteringExpression object.
            /// </summary>
            /// <param name="value">String value of the expression.</param>
            public FilteringExpression(String value)
            {
                this.Value = value;
            }

            /// <summary>
            /// String value of the expression.
            /// </summary>
            [XmlText]
            public String Value { get; set; }

            /// <summary>
            /// Returns a string that represents the current FilteringExpression object.
            /// </summary>
            /// <returns>A string that represents the current FilteringExpression object.</returns>
            public override String ToString()
            {
                return this.Value;
            }

            /// <summary>
            /// Returns the hash code for this object.
            /// </summary>
            /// <returns>A hash code for the current object.</returns>
            public override int GetHashCode()
            {
                return this.Value == null ? 0 : this.Value.GetHashCode();
            }

            /// <summary>
            /// Determines whether the specified object is equal to the current object.
            /// </summary>
            /// <param name="obj">The object to compare with the current object.</param>
            /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
            public override bool Equals(object obj)
            {
                return this.CompareTo(obj) == 0;
            }

            /// <summary>
            /// Compares the current object with another object of the same type.
            /// </summary>
            /// <param name="other">A FilteringExpression object to compare with this object.</param>
            /// <returns>A value that indicates the relative order of the objects being compared.</returns>
            public int CompareTo(FilteringExpression other)
            {
                if (other == null)
                {
                    return 1;
                }

                return String.Compare(this.Value, other.Value, System.StringComparison.Ordinal);
            }

            /// <summary>
            /// Compares the current instance with another object of the same type and returns an integer that indicates 
            /// whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
            /// </summary>
            /// <param name="obj">An object to compare with this instance. </param>
            /// <returns>A value that indicates the relative order of the objects being compared.</returns>
            public int CompareTo(object obj)
            {
                FilteringExpression otherFilteringExpression = obj as FilteringExpression;

                if (otherFilteringExpression == null)
                {
                    return 1;
                }

                return this.CompareTo(otherFilteringExpression);
            }

            /// <summary>
            /// Compile the string expression into a COExpression object.
            /// </summary>
            /// <param name="expression">The expression to compile.</param>
            /// <returns>Compiled COExpression object.</returns>
            public static COExpression<ClassificationObject> Compile<T>(String expression) where T : ClassificationObject
            {
                System.Linq.Expressions.LambdaExpression lambda = DynamicExpression.ParseLambda(typeof(T),
                    typeof(bool), expression);

                return ExpressionCasting<T>.ExpressionCast(lambda.Compile());
            }

            /// <summary>
            /// Compile the string expression into a COExpression object.
            /// </summary>
            /// <param name="expression">String value of the expression to compile.</param>
            /// <param name="compiledExpression"></param>
            /// <returns>A value indicating if the expression compiled successfully.</returns>
            public static bool TryCompile<T>(String expression, out COExpression<ClassificationObject> compiledExpression) 
                where T : ClassificationObject
            {
                Exception exception;
                if (!FilteringExpression.TryCompile<T>(expression, out compiledExpression, out exception))
                {
                    if (exception != null)
                    {
                            throw new Exception(String.Format("Failed to compile the Filtering Expression for the string value '{0}'. Reason: {1}",
                            expression, (exception.InnerException ?? exception).Message), exception);
                    }

                    return false;
                }

                return true;
            }

            /// <summary>
            /// Compile the string expression into a COExpression object.
            /// </summary>
            /// <param name="expression">String value of the expression to compile.</param>
            /// <param name="compiledExpression">Compiled COExpression object.</param>
            /// <param name="exception">The exception that occurred when compiling the expression.</param>
            /// <returns>A value indicating if the expression compiled successfully.</returns>
            public static bool TryCompile<T>(String expression, out COExpression<ClassificationObject> compiledExpression, out Exception exception)
                where T : ClassificationObject
            {
                compiledExpression = null;
                exception = null;

                try
                {
                    compiledExpression = FilteringExpression.Compile<T>(expression);
                    return true;
                }
                catch (Exception e)
                {
                    exception = e;
                    return false;
                }
            }
        }
    }
}