/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.ClasificationObjects.LocResource;

namespace UnitTests.TestRules
{
    /// <summary>
    /// Rule checking for the following patterns:
    /// Gender diff = different adj possibly
    ///    The 
    ///    New <m>
    ///    My %s
    /// </summary>
    public class AdjectivePlusPlaceholder : LocResourceRule
    {
        public AdjectivePlusPlaceholder(RuleManager owner) : base(owner) { }

        protected override void Run()
        {
            ///<check>
            ///Gender diff = different adj possibly
            ///The
            ///New <m>
            ///My %s
            ///</check>
            Check(lr => lr.SourceString == "cos" && lr.SourceString.Value.Length > 5 && lr.SourceString.Value.StartsWith("M", StringComparison.InvariantCulture), "Source starts with 'M' and is longer than 5");
        }

        protected override void Init()
        {
            base.Init();
            string csharpPlaceholder = @"(\{\d(:(.)+)?\})";
            string jsPlaceholder = @"(\|\d)|(\^\d)";     // |0 or ^0
            string cppPlaceholder = @"(%(s|S|d|D|x|X|f|F))";
            string cformatPlaceholder = @"(%\d(!(s|S|d|D|x|X|f|F)!)?)";
            string regexString = String.Format(CultureInfo.CurrentCulture, "({0}|{1}|{2}|{3})", csharpPlaceholder, jsPlaceholder, cppPlaceholder, cformatPlaceholder);
            //The or New or My (also lowercase), followed by a space and a placeholder
            string adjectivePlusPlaceholderString = String.Format(CultureInfo.CurrentCulture, @"(The|the|New|new|My|my) {0}", regexString);
            adjectivePlusPlaceholderDetector = new Regex(adjectivePlusPlaceholderString, RegexOptions.Compiled);
        }

        Regex adjectivePlusPlaceholderDetector;
    }
}
