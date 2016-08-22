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
using Microsoft.ResourceStaticAnalysis.Core.Engine;
using Microsoft.ResourceStaticAnalysis.Core.Properties;

namespace Microsoft.ResourceStaticAnalysis.Core.Misc
{
    /// <summary>
    /// Helper methods for interacting with ResourceStaticAnalysisCore
    /// </summary>
    public static class ResourceStaticAnalysisToolbox
    {
        /// <summary>
        /// Builds a string describing the details of the exception: type, message
        /// all inner exceptions (recursively) and (inner-most) stack trace, if specified.
        /// </summary>
        public static string GetExceptionDetails(this Exception e, bool includeStackTrace)
        {
            StringBuilder innerExceptionString = new StringBuilder();
            Exception inE = e.InnerException;
            string innerStackTrace = null;
            while (inE != null)
            {
                innerStackTrace = inE.StackTrace;
                innerExceptionString.AppendFormat("{0}: {1}\n", inE.GetType().FullName, inE.Message);
                inE = inE.InnerException;
            }
            var message = new StringBuilder();
            message.AppendFormat("Exception: \"{0}\", message: \"{1}\"\nInner exceptions:\n{2}",
                                 e.GetType().FullName, e.Message, innerExceptionString);
            if (includeStackTrace)
            {
                message.AppendFormat("\n{0}", innerStackTrace ?? e.StackTrace);
            }
            return message.ToString();
        }

        /// <summary>
        /// Check if regular expression matches against the string. 
        /// If more than one regex is provided applies OR logic.
        /// </summary>
        /// <param name="s">string to search</param>
        /// <param name="regexes">One or more regular expressions to evaluate. OR logic is applied.</param>
        /// <returns>True if at least one regular expression matches the string.</returns>
        public static bool RegExp(this string s, params Regex[] regexes)
        {
            var isEmpty = String.IsNullOrEmpty(s);
            if (isEmpty) s = String.Empty;
            foreach (var rg in regexes)
            {
                var result = rg.IsMatch(s);
                if (result) return true;
            }
            return false;
        }

        /// <summary>
        /// Check if regular expression matches against the string. 
        /// If more than one regex is provided applies AND logic.
        /// </summary>
        /// <param name="s">string to search</param>
        /// <param name="regexes">One or more regular expressions to evaluate. AND logic is applied.</param>
        /// <returns>True if all of the regular expression match the string.</returns>
        public static bool RegExpAnd(this string s, params Regex[] regexes)
        {
            if (s == null) s = String.Empty;
            foreach (var rg in regexes)
            {
                if (!s.RegExp(rg)) return false;
            }
            return true;
        }

        /// <summary>
        /// Uses Regex.Matches() method to retrieve all Matches by the regex on the strings and returns a collection of matches.
        /// </summary>
        /// <param name="s">string to search</param>
        /// <param name="regexes">One or more regular expressions to evaluate. AND logic is applied.</param>
        /// <returns>An collection containing matches. Empty collection if nothing matched</returns>
        public static string[] RegExpMatches(this string s, params Regex[] regexes)
        {
            if (String.IsNullOrEmpty(s))
                s = String.Empty;
            return (
                from rg in regexes
                from Match m in rg.Matches(s)
                select m.Value
                    ).ToArray();
        }

        /// <summary>
        /// Counts occurences of regular expression matches in the string.
        /// </summary>
        /// <param name="s">string to search</param>
        /// <param name="regex">This regex will be evaluated against the string.</param>
        /// <returns> Number of successful matches is returned.</returns>
        public static int CountOccurences(this string s, Regex regex)
        {
            if (s == null) s = String.Empty;
            return regex.Matches(s).Count;
        }

        /// <summary>
        /// Counts occurences of substring matches in the string.
        /// </summary>
        /// <param name="s">string to search</param>
        /// <param name="subString">substring to look for</param>
        /// <param name="comparison">Type of string comparison to be used.</param>
        /// <returns></returns>
        public static int CountOccurences(this string s, string subString, StringComparison comparison)
        {
            int i = 0, count = 0, start = 0;
            while ((i = s.IndexOf(subString, start, comparison)) > -1)
            {
                ++count;

                start = i + subString.Length;
                if (start >= s.Length)
                {
                    break;
                }
            }
            return count;
        }

        /// <summary>
        /// Counts occurences of substring matches in the string. Uses Ordinal comparison.
        /// </summary>
        /// <param name="s">string search</param>
        /// <param name="subString">substring to look for</param>
        /// <returns>Number of occurences of substring</returns>
        public static int CountOccurences(this string s, string subString)
        {
            return CountOccurences(s, subString, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if this string contains at least one of the strings.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="comparisonType">Type of string comparison to be used.</param>
        /// <param name="strings">Strings to check against.</param>
        /// <returns>True if at least one string is contained in this string.</returns>
        public static bool Contains(this string s, StringComparison comparisonType, params string[] strings)
        {
            if (String.IsNullOrEmpty(s)) return false;
            foreach (var sToCompare in strings)
            {
                if (s.IndexOf(sToCompare, comparisonType) > -1) return true;
            }
            return false;
        }
        /// <summary>
        /// Checks if this string contains all of the strings.
        /// </summary>
        /// <param name="s">string to search</param>
        /// <param name="comparisonType">Type of string comparison to be used.</param>
        /// <param name="strings">Strings to check against.</param>
        /// <returns>True if all of the strings are contained in this string.</returns>
        public static bool ContainsAnd(this string s, StringComparison comparisonType, params string[] strings)
        {
            if (String.IsNullOrEmpty(s)) return false;
            foreach (var sToCompare in strings)
            {
                if (s.IndexOf(sToCompare, comparisonType) < 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if this string equals any of the strings provided.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="comparisonType">Type of string comparison to be used.</param>
        /// <param name="strings">Strings to check against.</param>
        /// <returns>True if at least one of the strings are equal to this string.</returns>
        public static bool Equals(this string s, StringComparison comparisonType, params string[] strings)
        {
            if (s == null) s = String.Empty;
            foreach (var sToCompare in strings)
            {
                if (s.Equals(sToCompare, comparisonType)) return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if string starts with any of the strings provided.
        /// </summary>
        /// <param name="s">string to search</param>
        /// <param name="comparisonType">Type of string comparison to be used.</param>
        /// <param name="strings">Strings to look for.</param>
        /// <returns>True if at least one of the strings equals to the begining of this string.</returns>
        public static bool StartsWith(this string s, StringComparison comparisonType, params string[] strings)
        {
            if (s == null) s = String.Empty;
            foreach (var sToCompare in strings)
            {
                if (s.StartsWith(sToCompare, comparisonType)) return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if string ends with any of the strings provided.
        /// </summary>
        /// <param name="s">string to search</param>
        /// <param name="comparisonType">Type of string comparison to be used.</param>
        /// <param name="strings">Strings to look for.</param>
        /// <returns>True if at least one of the strings equals to the ending of this string.</returns>
        public static bool EndsWith(this string s, StringComparison comparisonType, params string[] strings)
        {
            if (s == null) s = String.Empty;
            foreach (var sToCompare in strings)
            {
                if (s.EndsWith(sToCompare, comparisonType)) return true;
            }
            return false;
        }

        /// <summary>
        /// Equivalent of Int32.TryParse. Converts a string to an int. Conversion can fail.
        /// </summary>
        /// <param name="s">string to convert</param>
        /// <param name="i">The "out" variable that has to be declared earlier in the code.</param>
        /// <returns>Returns true if converting to int succeeded. Returns false if it failde; in this case the 
        /// value of "i" will be 0.
        /// </returns>
        public static bool ToInt(this string s, out int i)
        {
            return Int32.TryParse(s, out i);
        }

        /// <summary>
        /// Convenience method with self-explanatory meaning.
        /// </summary>
        public static bool IsTrue(this bool b)
        {
            return b;
        }

        /// <summary>
        /// Convenience method with self-explanatory meaning.
        /// </summary>
        public static bool IsFalse(this bool b)
        {
            return !b;
        }

        /// <summary>
        /// Checks if string is empty. Empty means: null, empty (no characters), or contains whitespace characters only.
        /// </summary>
        /// <param name="s">string to search</param>
        /// <returns></returns>
        public static bool IsEmpty(this string s)
        {
            if (String.IsNullOrEmpty(s) == false)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    if (Char.IsWhiteSpace(s[i]) == false)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Same as IEnumerable.All but returns false if set is empty.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source">collection</param>
        /// <param name="predicate">logic to apply</param>
        /// <returns>True if all elements satisfy condition in predicate. False otherwise or if set is empty.</returns>
        public static bool AllAndNonEmpty<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            return (source.All(predicate) && source.Any());
        }

        /// <summary>
        /// String contains only valid XML characters
        /// </summary>
        public static Regex StringHasOnlyValidXmlCharacters =
            new Regex(@"^(?>[\x09\x0A\x0D\u0020-\ud7ff\ue000-\ufffd\u1000-\u10ff]*)$",
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        /// <summary>
        /// Returns a stream reader that points to the contents of ResourceStaticAnalysisConfig.xsd.
        /// </summary>
        public static StreamReader GetConfigSchemaStream()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "Microsoft.ResourceStaticAnalysis.Core.ResourceStaticAnalysisConfig.xsd");
            if (stream != null)
            {
                var sr = new StreamReader(stream);
                return sr;
            }
            throw new ResourceStaticAnalysisException("Unable to construct the stream for config schema.");
        }

        /// <summary>
        /// Gets the namespace that configuration XML schema uses.
        /// </summary>
        public static string ConfigSchemaNameSpace
        {
            get { return Resources.ConfigXMLNameSpace; }
        }
    }
}
