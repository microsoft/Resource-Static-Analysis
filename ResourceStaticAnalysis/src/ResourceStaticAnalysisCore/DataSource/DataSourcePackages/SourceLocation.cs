/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages
{
    /// <summary>
    /// Indicates location of a data source.
    /// For example, for physical files it would contain a path to physical file on disk.
    /// </summary>
    public class SourceLocation : IXmlSerializable, IEquatable<SourceLocation>
    {
        /// <summary>
        /// This constructor must be public for OM manipulation
        /// </summary>
        public SourceLocation() { }

        /// <summary>
        /// Details about the current location.
        /// </summary>
        private object _locationData;

        /// <summary>
        /// Value of this source location node.
        /// E.g. a string representing the path of a Resource file or a misc dictionary object
        /// To initialize this value, call ReadXml() by providing an XML string of a SourceLocation node with appropriate data.
        /// </summary>
        public object Value
        {
            get
            {
                return this._locationData;
            }
            set
            {
                _locationData = value;
                return;
            }
        }

        #region IXmlSerializable Members
        /// <summary>
        /// Not supported. Member of IXmlSerializable interface.
        /// </summary>
        /// <exception cref="NotImplementedException">Always thrown as this method is not implemented.</exception>
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            throw new NotImplementedException("SourceLocation does not support GetSchema()");
        }

        /// <summary>
        /// Reads an XML node, whose name is "SourceLocation" and contains either Path node with file path to a Resource file
        /// or MiscData node with a dictionary of key/value pairs.
        /// </summary>
        /// <param name="reader">Reader whose position is at SourceLocation</param>
        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement("SourceLocation");
            reader.MoveToContent();
            switch (reader.Name)
            {
                case "Path":
                    reader.ReadStartElement();
                    Value = reader.ReadString();
                    reader.ReadEndElement();
                    break;
                case "MiscData":
                    ConfigDictionary dictionary = new ConfigDictionary();
                    reader.ReadStartElement();
                    while (reader.Name != "MiscData")
                    {
                        reader.MoveToContent();
                        if (reader.Name == "Param")
                        {
                            reader.MoveToAttribute("Name");
                            string key = reader.ReadContentAsString();
                            reader.MoveToElement();
                            string value = reader.ReadString();
                            reader.ReadEndElement();
                            dictionary.Add(key, value);
                        }
                        reader.MoveToContent();
                    }
                    Value = dictionary;
                    break;
                default:
                    Value = null;
                    break;
            }
            while (reader.Name != "SourceLocation") reader.Read(); //Read until the closing tag.
            reader.ReadEndElement(); //Skip the outer closing element.
        }

        /// <summary>
        /// Not implemented currently.
        /// </summary>
        public void WriteXml(XmlWriter writer)
        {
            throw new NotImplementedException("SourceLocation does not support WriteXml");
        }
        #endregion

        #region IEquatable<SourceLocation> Members
        /// <summary>
        /// Compares whether the <see cref="Value"/> of this <see cref="SourceLocation"/> is the same as the <see cref="Value"/> value of the <paramref name="other"/> location.
        /// </summary>
        /// <param name="other"><see cref="SourceLocation"/> we want to compare with this location.</param>
        /// <returns>True if locations are equal. False otherwise.</returns>
        public bool Equals(SourceLocation other)
        {
            if (object.ReferenceEquals(Value, other.Value)) return true;
            return Value.Equals(other.Value);
        }
        #endregion

        /// <summary>
        /// Reveals the data source location.
        /// If location is not available, it returns a string: "Location not set".
        /// </summary>
        public override string ToString()
        {
            if (Value == null) return "<Location not set>";
            return Value.ToString();
        }
    }
}
