/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;

namespace Microsoft.ResourceStaticAnalysis.Core.Misc
{
    /// <summary>
    /// Custom configuration dictionary, storing a list of key-value string pairs.
    /// Can be used by ResourceStaticAnalysis configuration piece to pass in custom properties classification objects of a particular data source (<see cref="DataSourceInfo"/>) should have.
    /// </summary>
    public class ConfigDictionary : Dictionary<string, string>,IEquatable<object>
    {
        /// <summary>
        /// Create a config dictionary based on a chunk of XML.
        /// "Name" attribute becomes Key and text element becomes string value.
        /// </summary>
        /// <param name="node">Must contain a list of XML nodes with a "Name" attribute and a text value.</param>
        public ConfigDictionary(XmlNode node)
        {
            AddParams(node);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ConfigDictionary() { }

        /// <summary>
        /// Adds parameters from the specified XmlNode.
        /// <para/>This node is expected to cotnain a list of XmL nodes with "Name" attribute and a text value.
        /// <para/>From the nodes, each "Name" attribute is treated as key (property name), wherease text value of the node is treated as value (property value). Then the key-value pair is added to this dictionary.
        /// </summary>
        /// <param name="node">Must contain a list of XML nodes with a "Name" attribute and a text value.</param>
        public void AddParams(XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    XmlElement xe = child as XmlElement;
                    string key = xe.GetAttribute("Name");
                    if (!String.IsNullOrEmpty(key))
                    {
                        string value = xe.InnerText;
                        if (!String.IsNullOrEmpty(value))
                        {
                            this.Add(key, value);
                        }
                    }
                }
            }
        }

        #region IEquatable<object> Members
        /// <summary>
        /// Determines whether the specific <paramref name="other"/> object is considered as equal to this instance.
        /// </summary>
        /// <param name="other">The other object to compare</param>
        /// <returns>True if this object and the other object are considered equal (may or may not be the same object).</returns>
        public override bool Equals(object other)
        {
            if (!Object.ReferenceEquals(other, null) && this.GetType() == other.GetType())
            {
                return this.Equals(other as ConfigDictionary);
            }
            return false;
        }

        /// <summary>
        /// Compares the contents of keys and values of this <see cref="ConfigDictionary"/> with the <paramref name="other"/> dictionary to determine whether they contain exactly the same configuration.
        /// </summary>
        /// <param name="other">The other <see cref="ConfigDictionary"/> to compare</param>
        /// <returns>True if both dictionaries are equivalent (contain exactly the same key-value pairs). False otherwise.</returns>
        public bool Equals(ConfigDictionary other)
        {

            if (Keys.Except(other.Keys).Count() == 0 //the set of keys is the same
             && Keys.Count(key => this[key] != other[key]) == 0) //values assigned to the keys are equal
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a hash code for this ConfigDictionary.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion
    }
}
