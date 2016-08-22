/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

namespace Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase
{
    /// <summary>
    /// Used to describe severity of a particular check.
    /// </summary>
    public enum CheckSeverity : byte
    {
        // make sure to list items in order from least critical to most critical. this is used in comparison logic.
        None,
        Low,
        Normal,
        High,
        Critical
    }
}
