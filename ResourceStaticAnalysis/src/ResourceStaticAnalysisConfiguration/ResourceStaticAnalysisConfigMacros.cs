/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Microsoft.ResourceStaticAnalysis.Configuration
{
    /// <summary>
    /// Class to hold the collection of macros that can be used during configuration.
    /// </summary>
    [Serializable]
    [XmlRoot("Macros")]
    public class ResourceStaticAnalysisConfigMacros
    {
        /// <summary>
        /// Initializes a new instance of the ResourceStaticAnalysisConfigMacros class.
        /// </summary>
        public ResourceStaticAnalysisConfigMacros()
        {
            this.Macros = new List<ResourceStaticAnalysisConfigMacro>();
        }

        /// <summary>
        /// Collection of Macros
        /// </summary>
        [XmlElement("Macro")]
        public List<ResourceStaticAnalysisConfigMacro> Macros { get; set; }

        /// <summary>
        /// Load the macros from the specified file path.
        /// </summary>
        /// <param name="filePath">Path to the file containing the configuration.</param>
        /// <returns>ResourceStaticAnalysisConfigMacros object.</returns>
        public static ResourceStaticAnalysisConfigMacros Load(String filePath)
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
                return Load(fs);
            }
        }

        /// <summary>
        /// Load the macros from the specified stream.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <returns>ResourceStaticAnalysisConfigMacros object.</returns>
        public static ResourceStaticAnalysisConfigMacros Load(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream", "Stream can't be null!");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(ResourceStaticAnalysisConfigMacros));
            try
            {
                return serializer.Deserialize(stream) as ResourceStaticAnalysisConfigMacros;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Class to hold the information about a macro.
        /// </summary>
        [Serializable]
        public class ResourceStaticAnalysisConfigMacro
        {
            /// <summary>
            /// Name of the Macro.
            /// </summary>
            [XmlAttribute("name")]
            public String Name { get; set; }

            /// <summary>
            /// Value of the Macro.
            /// </summary>
            [XmlAttribute("value")]
            public String Value { get; set; }
        }
    }
}