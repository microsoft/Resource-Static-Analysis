/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Text.RegularExpressions;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine.Properties
{
    /// <summary>
    /// A property that is a string. Provides convenience methods to operate on strings.
    /// </summary>
    public class StringProperty : Property
    {
        public StringProperty(byte propertyId, string propertyValue) : base(propertyId) { _value = propertyValue; }

        string _value;

        public string Value
        {
            get
            {
                return _value;
            }
        }
        public override object GetValue()
        {
            return _value;
        }

        /// <summary>
        /// Check if regular expression matches against the string. 
        /// If more than one regex is provided applies OR logic.
        /// </summary>
        /// <param name="regexes">Regular expression objects. Create a static object to reuse the same object in all calls.</param>
        /// <returns>True if at least one regular expression matches the string.</returns>
        public bool RegExp(params Regex[] regexes)
        {
            //return pattern.IsMatch((string)value);
            return this.Value.RegExp(regexes);
        }

        /// <summary>
        /// Check if regular expression matches against the string. 
        /// If more than one regex is provided applies AND logic.
        /// </summary>
        /// <param name="regexes">One or more regular expressions to evaluate. AND logic is applied.</param>
        /// <returns>True if all of the regular expression match the string.</returns>
        public bool RegExpAnd(params Regex[] regexes)
        {
            return this.Value.RegExpAnd(regexes);
        }

        public string[] RegExpMatches(params Regex[] regexes)
        {
            return this.Value.RegExpMatches(regexes);
        }

        /// <summary>
        /// Checks if this string contains at least one of the strings.
        /// </summary>
        /// <param name="comparisonType">Type of string comparison to be used.</param>
        /// <param name="strings">Strings to check against.</param>
        /// <returns>True if at least one string is contained in this string.</returns>
        public bool Contains(StringComparison comparisonType, params string[] strings)
        {
            return ResourceStaticAnalysisToolbox.Contains(this.Value, comparisonType, strings);
        }

        /// <summary>
        /// Checks if this string contains at least one of the strings. Uses default Ordinal comparison.
        /// </summary>
        /// <param name="strings">Strings to check against.</param>
        /// <returns>True if at least one string is contained in this string.</returns>
        public bool Contains(params string[] strings)
        {
            return ResourceStaticAnalysisToolbox.Contains(this.Value, StringComparison.Ordinal, strings);
        }

        /// <summary>
        /// Checks if this string contains all of the strings.
        /// </summary>
        /// <param name="comparisonType">Type of string comparison to be used.</param>
        /// <param name="strings">Strings to check against.</param>
        /// <returns>True if all of the strings are contained in this string.</returns>
        public bool ContainsAnd(StringComparison comparisonType, params string[] strings)
        {
            return ResourceStaticAnalysisToolbox.ContainsAnd(this.Value, comparisonType, strings);
        }

        /// <summary>
        /// Checks if this string contains all of the strings. Uses default Ordinal comparison.
        /// </summary>
        /// <param name="strings">Strings to check against.</param>
        /// <returns>True if all of the strings are contained in this string.</returns>
        public bool ContainsAnd(params string[] strings)
        {
            return ResourceStaticAnalysisToolbox.ContainsAnd(this.Value, StringComparison.Ordinal, strings);
        }

        /// <summary>
        /// Checks if this string equals any of the strings provided.
        /// </summary>
        /// <param name="comparisonType">Type of string comparison to be used.</param>
        /// <param name="strings">Strings to check against.</param>
        /// <returns>True if at least one of the strings are equal to this string.</returns>
        public bool Equals(StringComparison comparisonType, params string[] strings)
        {
            return ResourceStaticAnalysisToolbox.Equals(this.Value, comparisonType, strings);
        }

        /// <summary>
        /// Checks if this string equals any of the strings provided. Uses default Ordinal comparison.
        /// </summary>
        /// <param name="strings">Strings to check against.</param>
        /// <returns>True if at least one of the strings are equal to this string.</returns>
        public bool Equals(params string[] strings)
        {
            return ResourceStaticAnalysisToolbox.Equals(this.Value, StringComparison.Ordinal, strings);
        }

        /// <summary>
        /// Counts occurences of regular expression matches in the string.
        /// </summary>
        /// <param name="pattern">This regex will be evaluated against the string.</param>
        /// <returns> Number of successful matches is returned.</returns>
        public int CountOccurences(Regex pattern)
        {
            return this.Value.CountOccurences(pattern);
        }

        /// <summary>
        /// Counts occurences of substring matches in the string.
        /// </summary>
        /// <param name="subString"></param>
        /// <param name="comparison"></param>
        /// <returns></returns>
        public int CountOccurences(string subString, StringComparison comparison)
        {
            return this.Value.CountOccurences(subString, comparison);
        }

        /// <summary>
        /// Counts occurences of substring matches in the string. Uses Ordinal comparison.
        /// </summary>
        /// <param name="subString"></param>
        /// <returns></returns>
        public int CountOccurences(string subString)
        {
            return this.Value.CountOccurences(subString);
        }

        /// <summary>
        /// Gets length of the undelying string.
        /// </summary>
        public int Length { get { return this.Value.Length; } }

        /// <summary>
        /// Checks if string is empty. Empty means: null, empty (no characters), or contains whitespace characters only.
        /// </summary>
        public bool IsEmpty { get { return this.Value.IsEmpty(); } }

        public static implicit operator string(StringProperty sp)
        {
            if (ReferenceEquals(sp, null))
            {
                return default(string);
            }
            return sp.Value;
        }

        public override int CompareTo(Property other)
        {
            return StringComparer.Ordinal.Compare(this.Value, ReferenceEquals(other, null) ? null : other.GetValue());
        }

        public override bool Equals(Property other)
        {
            if (ReferenceEquals(other, null))
                return false;
            else
                return this.Value.Equals(other.GetValue() as string, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Value.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
