/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using Microsoft.ResourceStaticAnalysis.Core.Engine;

namespace Microsoft.ResourceStaticAnalysis.Core.Output
{
    public interface IOutputWriter
    {
        void Initialize(OutputWriterConfig owc);

        void WriteEntry(ClassificationObject co, IEnumerable<OutputEntryForOneRule> output);

        void Finish();
    }

    public class OutputWriterInitializationException : Exception
    {
        public OutputWriterInitializationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
