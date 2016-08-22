/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;

namespace Microsoft.ResourceStaticAnalysis.UnitTests.TestObjectStructure
{
    /// <summary>
    /// A class that creates a collection of SampleResourceEntry objects
    /// Each resource and its associated metadata will be represented by a single <see cref="SampleResourceEntry"/>
    /// </summary>
    public class SampleResourceCollection
    {
        public List<SampleResourceEntry> Entries { get; set; }
        public string FilePath { get; set; }
        public SampleResourceCollection(List<Tuple<string, string, string>> propertyTuples)
        {
            Entries = new List<SampleResourceEntry>();
            foreach (var propertyTuple in propertyTuples)
            {
                Entries.Add(new SampleResourceEntry(propertyTuple.Item1, propertyTuple.Item2, propertyTuple.Item3));
            }
        }
    }

    /// <summary>
    /// A class that represents a single Resource and its relevant properties
    /// </summary>
    public class SampleResourceEntry
    {
        public SampleResourceEntry(string resourceId, string value, string comments)
        {
            ResourceId = resourceId;
            Value = value;
            Comments = comments;
        }
        public string ResourceId { get; private set; }
        public string Value { get; private set; }
        public string Comments { get; private set; }
    }
}


