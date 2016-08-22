/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.ResourceStaticAnalysis.Core.Engine;

namespace Microsoft.ResourceStaticAnalysis.Core.Output
{
    [Serializable]
    public class OutputWriterConfig
    {
        /// <summary>
        /// Type, corresponding to the typename of specific IOutputWriter class,
        /// to which this config entry pertains.
        /// </summary>
        [XmlElement]
        public TypeSpecification Kind;

        /// <summary>
        /// Stores the output path that the specific IOutputWriter should write to.
        /// </summary>
        /// <remarks>
        /// If your IOutputWriter does not write to a file, this element may be ignored,
        /// so it is not mandatory to support this value.
        /// </remarks>
        [XmlElement]
        public string Path = string.Empty;

        /// <summary>
        /// Path to the XML Schema file to be used to validate the output against.
        /// Can be empty - validation won't be performed.
        /// </summary>
        [XmlElement]
        public string Schema = string.Empty;

        /// <summary>
        /// An optional list of CO properties to be included in output.
        /// If nothing is provided, all properties are displayed.
        /// </summary>
        [XmlArray("PropertiesToInclude"), XmlArrayItem("Property")]
        public List<string> PropertiesToInclude;

        #region Object model
        /// <summary>
        /// Allows specifying explicitly, which properties are to be included in the output produced by this output writer.
        /// <para/>If no properties are specified (default), then all available properties will be written to output.
        /// </summary>
        /// <param name="propertyName">Name of the property whose value is to be written to output</param>
        public void AddPropertyToIncludeInOutput(string propertyName)
        {
            if (PropertiesToInclude == null) PropertiesToInclude = new List<string>(); //Make sure the list is initialized
            PropertiesToInclude.Add(propertyName);
        }

        /// <summary>
        /// Defines the type of the output writer class that will handle
        /// writing output of ResourceStaticAnalysis to external storage (such as XML file).
        /// Assembly name will be guessed from the type information contained in <paramref name="outputWriterType"/>.
        /// </summary>
        /// <param name="outputWriterType">Type inheriting from IOutputWriter class. Must be instantiable.</param>
        public void SetDataSourceProvider(Type outputWriterType)
        {
            Kind = TypeSpecification.CreateSpecification(outputWriterType.FullName, outputWriterType.Assembly.GetName().FullName);
        }

        /// <summary>
        /// Defines the type of the output writer class that will handle
        /// writing output of ResourceStaticAnalysis to external storage (such as XML file).
        /// Assembly name will be guessed from the type information contained in <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type inheriting from IOutputWriter class. Must be instantiable.</typeparam>
        public void SetDataSourceProvider<T>()
        {
            SetDataSourceProvider(typeof(T));
        }
        #endregion
    }
}
