/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.ResourceStaticAnalysis.Configuration
{
    /// <summary>
    /// Class that handles resolving macros used in the configuration for the ResourceStaticAnalysis Engine Checks run.
    /// </summary>
    public class ResourceStaticAnalysisConfigMacroHandler
    {
        private const char macroSymbol = '@';
        private const String macroPattern = "@([a-zA-Z0-9_]+)@";
        private static Regex macroRegex = new Regex("^" + macroPattern, RegexOptions.Singleline | RegexOptions.Compiled);
        private static Regex allMacroRegex = new Regex(macroPattern, RegexOptions.Singleline | RegexOptions.Compiled);
        private const String macroDefinitionFile = "ResourceStaticAnalysisConfigMacros.xml";
        private ResourceStaticAnalysisConfigMacros macroCollection;
        private static readonly ResourceStaticAnalysisConfigMacroHandler instance = new ResourceStaticAnalysisConfigMacroHandler();

        /// <summary>
        ///  Static ResourceStaticAnalysisConfigMacroHandler constructor.
        /// </summary>
        static ResourceStaticAnalysisConfigMacroHandler()
        {
        }

        /// <summary>
        /// Creates a new Instance of the ResourceStaticAnalysisConfigMacroHandler class.
        /// </summary>
        private ResourceStaticAnalysisConfigMacroHandler()
        {
            this.macroCollection = this.LoadConfigMacros();
            this.ValidateMacros();
        }

        /// <summary>
        /// Only instance of the ResourceStaticAnalysisConfigMacroHandler class.
        /// </summary>
        public static ResourceStaticAnalysisConfigMacroHandler Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Expand any macros in the supplied string.
        /// </summary>
        /// <param name="inputString">String to be expanded.</param>
        /// <returns>Expanded string.</returns>
        public String ExpandMacros(String inputString)
        {
            if (String.IsNullOrWhiteSpace(inputString))
            {
                return inputString;
            }

            StringBuilder returnValue = new StringBuilder();

            int previousPosition = 0;
            int length = inputString.Length;
            bool macroFound = false;

            while (previousPosition < length)
            {
                int macroStartPosition = inputString.IndexOf(macroSymbol, previousPosition);
                if (macroStartPosition == -1 || macroStartPosition == length - 1)
                {
                    break;
                }

                returnValue.Append(inputString.Substring(previousPosition, macroStartPosition - previousPosition));
                previousPosition = macroStartPosition + 1;

                if (inputString[macroStartPosition + 1] == macroSymbol)
                {
                    // This is an escaped parameter macro, don't collapse it here '@'
                    returnValue.Append(new[] { macroSymbol, macroSymbol });
                    previousPosition++;
                }
                else
                {
                    Match macroMatch = null;
                    macroMatch = macroRegex.Match(inputString.Substring(macroStartPosition));
                    if (macroMatch.Success)
                    {
                        String expanded = this.ExpandMacro(macroMatch.Groups[1].Value, true);

                        if (expanded != null)
                        {
                            returnValue.Append(expanded);
                            previousPosition = macroStartPosition + macroMatch.Length;
                            macroFound = true;
                        }
                        else
                        {
                            returnValue.Append(macroSymbol);
                        }
                    }
                    else
                    {
                        returnValue.Append(macroSymbol);
                    }
                }
            }
            if (macroFound)
            {
                returnValue.Append(inputString.Substring(previousPosition));
            }
            else
            {
                returnValue = new StringBuilder(inputString);
            }

            return returnValue.ToString();
        }

        /// <summary>
        /// Expand any macros in the supplied string.
        /// </summary>
        /// <param name="expand">String to be expanded.</param>
        /// <param name="recursive">True if macros should be recursively expanded, false otherwise.</param>
        /// <returns></returns>
        private String ExpandMacro(String expand, bool recursive)
        {
            if (this.macroCollection == null || this.macroCollection.Macros == null || String.IsNullOrWhiteSpace(expand))
            {
                return null;
            }

            ResourceStaticAnalysisConfigMacros.ResourceStaticAnalysisConfigMacro macro = this.macroCollection.Macros.FirstOrDefault(
                m => String.Equals(m.Name, expand, System.StringComparison.InvariantCultureIgnoreCase));
            if (macro != null)
            {
                String expandedTo = macro.Value;
                if (recursive)
                {
                    expandedTo = ExpandMacros(expandedTo);
                }

                return expandedTo;
            }
            return null;
        }

        /// <summary>
        /// Validate the macros in the collection to ensure they are valid and can be resolved.
        /// </summary>
        private void ValidateMacros()
        {
            if (macroCollection != null)
            {
                ResourceStaticAnalysisConfigMacros macros = new ResourceStaticAnalysisConfigMacros();
                foreach (var macro in macroCollection.Macros)
                {
                    List<String> usedKeys = new List<String>();
                    if (this.ValidateMacro(macro.Name, usedKeys))
                    {
                        macros.Macros.Add(macro);
                    }
                }

                this.macroCollection = macros;
            }
        }

        /// <summary>
        /// Validate the supplied macro, collection of already used macros.
        /// </summary>
        /// <param name="macro">The macro to validate.</param>
        /// <param name="usedKeys">Collection of already used keys.</param>
        /// <returns>True if the macro is valid, false otherwise.</returns>
        private bool ValidateMacro(String macro, List<String> usedKeys)
        {
            if (String.IsNullOrWhiteSpace(macro))
            {
                return false;
            }

            usedKeys.Add(macro);
            bool macroValid = true;

            String macroValue = this.ExpandMacro(macro, false);
            foreach (String subMacro in this.FindMacrosInString(macroValue))
            {
                if (usedKeys.Contains(subMacro, StringComparer.InvariantCultureIgnoreCase))
                {
                    macroValid = false;
                    break;
                }

                String subMacroValue = this.ExpandMacro(subMacro, false);
                if (String.IsNullOrWhiteSpace(subMacroValue))
                {
                    macroValid = false;
                    break;
                }

                List<String> usedSubKeys = new List<String>(usedKeys);
                if (!this.ValidateMacro(subMacro, usedSubKeys))
                {
                    macroValid = false;
                    break;
                }
            }

            return macroValid;
        }

        /// <summary>
        /// Find all the macros in a string and return them.
        /// </summary>
        /// <param name="inputString">String to search in.</param>
        /// <returns>Collection of macros in the string.</returns>
        private List<String> FindMacrosInString(String inputString)
        {
            List<String> macros = new List<String>();
            if (String.IsNullOrWhiteSpace(inputString))
            {
                return macros;
            }

            foreach (Match macroMatch in allMacroRegex.Matches(inputString))
            {
                macros.Add(macroMatch.Groups[1].Value);
            }

            return macros;
        }

        /// <summary>
        /// Load the ResourceStaticAnalysis Engine Config Macros from the macro definintion file.
        /// </summary>
        /// <remarks>Look first for the macro definition file located on disk in the same location as the executing assembly.
        /// If that isn't found or isn't valid, load the macro definition file embedded as a resource in the assembly.</remarks>
        /// <returns>ResourceStaticAnalysisConfigMacros object loaded from the macro definition file.</returns>
        private ResourceStaticAnalysisConfigMacros LoadConfigMacros()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            String pathToFile = Path.Combine(Path.GetDirectoryName(assembly.Location), macroDefinitionFile);

            ResourceStaticAnalysisConfigMacros macros = null;
            if (!File.Exists(pathToFile) || (macros = ResourceStaticAnalysisConfigMacros.Load(pathToFile)) == null)
            {
                try
                {
                    macros = ResourceStaticAnalysisConfigMacros.Load(assembly.GetManifestResourceStream(String.Format("{0}.{1}", this.GetType().Namespace, macroDefinitionFile)));
                }
                catch
                {
                    //Failed to load the macro file - ignore and move on.
                }
            }

            return macros ?? new ResourceStaticAnalysisConfigMacros();
        }
    }
}