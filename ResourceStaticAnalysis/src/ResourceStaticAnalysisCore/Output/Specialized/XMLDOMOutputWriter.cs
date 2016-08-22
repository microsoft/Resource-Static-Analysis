/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Engine.Properties;
using Microsoft.ResourceStaticAnalysis.Core.Properties;

namespace Microsoft.ResourceStaticAnalysis.Core.Output.Specialized
{
    /// <summary>
    /// A generic XML output writer that can write output for any type derived from Classification Object.
    /// It uses a DOM structure to build the entire XML document which may be memory consuming when there is a lot of output to be written.
    /// </summary>
    public class XMLDOMOutputWriter : XmlOutputWriter, IOutputWriter
    {
        private string _pathToOutputSchema = String.Empty;
        private string _fileName = String.Empty;
        #region IOutputWriter Members
        void IOutputWriter.Initialize(OutputWriterConfig owc)
        {
            _myConfig = owc;
            //Override default if available
            _fileName = Environment.ExpandEnvironmentVariables((_myConfig.Path.Length > 0) ? _myConfig.Path : Resources.DefaultOutputFileName);
            _pathToOutputSchema = owc.Schema;
            if (!Object.ReferenceEquals(owc.PropertiesToInclude, null) && owc.PropertiesToInclude.Count > 0)
            {
                _propertiesToWrite = owc.PropertiesToInclude;
            }
            _outputDoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement(Resources.ProductShortName, new XAttribute("version", Resources.ProductVersion),
                new XAttribute("start", DateTime.Now.ToString())));
        }

        private OutputWriterConfig _myConfig;
        private IEnumerable<string> _propertiesToWrite = null;

        void IOutputWriter.WriteEntry(ClassificationObject co, IEnumerable<OutputEntryForOneRule> output)
        {
            var coNode = new XElement("co", new XAttribute("key", co.Key.ToString()));
            _outputDoc.Root.Add(coNode);

            var propsNode = new XElement("props");
            coNode.Add(propsNode);
            IEnumerable<byte> propIdsToWrite;
            if (Object.ReferenceEquals(_propertiesToWrite, null))
            {
                propIdsToWrite = co.EnabledProperties;
            }
            else
            {
                propIdsToWrite = _propertiesToWrite.Select(name => co.PropertyEnumManager.GetIdFromName(name));
            }
            foreach (Property property in co.Properties.GetPropertySet(propIdsToWrite))
            {
                var propertyNode = new XElement("property",
                    new XAttribute("key", property.Name),
                    new XAttribute("name", property.Name));
                propsNode.Add(propertyNode);
                string value = null;
                try
                {
                    value = property.ToString();
                }
                catch (Exception)
                {
                    propertyNode.Add(new XAttribute("error", "Obtaining string from Property threw an exception. Check your code."));
                }
                if (value == null) value = String.Empty;
                propertyNode.Add(new XElement("value", value));
            }
            var rulesNode = new XElement("rules");
            coNode.Add(rulesNode);
            //only write OutputEntries for Rules that have matched
            foreach (OutputEntryForOneRule oefor in output)
            {
                var ruleNode = new XElement("rule");
                rulesNode.Add(ruleNode);
                ruleNode.Add(
                        new XAttribute("checks", oefor.OutputItems.Count.ToString()),
                        new XAttribute("result", oefor.Result.ToString()),
                        new XAttribute("name", oefor.Rule.GetType().Name),
                        new XAttribute("severity", oefor.Severity.ToString())
                    );


                foreach (OutputItem oi in oefor.OutputItems)
                {
                    var itemNode = new XElement("item");
                    ruleNode.Add(itemNode);
                    itemNode.Add(
                        new XAttribute("result", oi.Result.ToString()),
                        new XAttribute("message", oi.Message),
                        new XAttribute("severity", oi.Severity.ToString())
                    );
                }
            }
        }

        void IOutputWriter.Finish()
        {
            DateTime finishTime = DateTime.Now;
            _outputDoc.Root.Add(new XAttribute("finish", finishTime.ToString()));
            try
            {
                _outputDoc.Save(_fileName);
            }
            catch (Exception e)
            {
                Trace.TraceInformation("Exception caught when trying to save the output file. Make sure {1} config specifies a valid path for the ouptut file: {0}. Output has not been saved.", e.Message, Properties.Resources.ProductShortName);
            }
            Validate(_pathToOutputSchema);
        }

        #endregion

        private XDocument _outputDoc;

        protected override void Validate(string pathToSchema)
        {
            if (!String.IsNullOrEmpty(pathToSchema))
            {
                ValidateXmls(pathToSchema, _fileName);
            }
        }
    }
}