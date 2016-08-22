/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace Microsoft.ResourceStaticAnalysis.Core.Output
{
    public abstract class XmlOutputWriter
    {
        /// <summary>
        /// Validate the output against an XML Schema
        /// </summary>
        /// <param name="pathToSchema">Full path to the schema file.</param>
        protected abstract void Validate(string pathToSchema);

        /// <summary>
        /// Read the specified xml files and validate against the schema
        /// </summary>
        /// <param name="pathToSchema">Path to XML Schema file</param>
        /// <param name="Xmls">Paths to XML files to be validated</param>
        /// <returns>True if all Xml files are valid.</returns>
        protected bool ValidateXmls(string pathToSchema, params string[] Xmls)
        {
            try
            {
                pathToSchema = Environment.ExpandEnvironmentVariables(pathToSchema);
                StreamReader SR = new StreamReader(pathToSchema);
                XmlSchema Schema = new XmlSchema();
                Schema = XmlSchema.Read(SR,
                    new ValidationEventHandler(ReaderSettings_ValidationEventHandler));
                bool failed = false;
                foreach (string Xml in Xmls)
                {
                    if (!ValidateXml(Xml, Schema)) failed = true;
                }
                if (failed)
                {
                    Trace.TraceError("One or more XMLs failed schema validation. See entries above for details.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Schema validation could not be completed. Original error message: {0}", e.Message);
            }
            return true;
        }

        private bool _xmlValidationFailed = false;
        private XmlTextReader _reader;

        private bool ValidateXml(string xmlPath, XmlSchema schema)
        {
            _reader = new XmlTextReader(xmlPath);

            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ValidationType = ValidationType.Schema;
            readerSettings.Schemas.Add(schema);

            readerSettings.ValidationEventHandler += ReaderSettings_ValidationEventHandler;

            using (XmlReader objXmlReader = XmlReader.Create(_reader, readerSettings))
            {
                while (objXmlReader.Read())
                { /*Empty loop*/}

                if (_xmlValidationFailed)
                {
                    _xmlValidationFailed = false;
                    Trace.TraceInformation("File {0} does not conform to the XML Schema. See entries above for details.", xmlPath);
                    return false;
                }
            }
            return true;
        }
        private void ReaderSettings_ValidationEventHandler(object sender, ValidationEventArgs args)
        {
            // Implement your logic for each validation iteration
            Trace.TraceInformation("Line: {0} - Position: {1} - {2}", _reader.LineNumber, _reader.LinePosition, args.Message);
            _xmlValidationFailed = true;
        }
    }
}
