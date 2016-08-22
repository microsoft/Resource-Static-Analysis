/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System.Threading;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine.Properties
{
    /// <summary>
    /// This type improves performance and memeory usage of Classification Object in terms of how properties are stored.
    /// In creates indices for poperties based on their usage at runtime instead of allocating an array for each CO to store all known properties - 
    /// this reduces memory usage to only the properties that are actually used in the execution.
    /// Implementiations of ClassificationObject should store a static instance of a PropertyIndexer and return it via instance objects.
    /// </summary>
    public class PropertyIndexer
    {
        // property indices are stored here
        readonly short[] _propertyIndex;

        // lock used to control write access to the propertyIndex
        private static readonly ReaderWriterLockSlim PropertyIndexLock = new ReaderWriterLockSlim();

        // the highest property index created so far
        short _topIndex = -1;

        /// <summary>
        /// Create an instance that can index up to N properties, specified by parameter.
        /// Byte is assumed as sufficient to contain all properties implemented by CO.
        /// </summary>
        /// <param name="numberOfProperties">Number of total properties in a Classification Object type</param>
        public PropertyIndexer(byte numberOfProperties)
        {
            _propertyIndex = new short[numberOfProperties];
            for (int i = 0; i < numberOfProperties; i++)
            {
                _propertyIndex[i] = -1;
            }
        }

        /// <summary>
        /// Gets the index of property specified by propertyId
        /// </summary>
        /// <param name="propertyId"></param>
        /// <returns></returns>
        public byte GetPropertyIndex(byte propertyId)
        {
            short ret;
            PropertyIndexLock.EnterUpgradeableReadLock();
            try
            {
                ret = _propertyIndex[propertyId];
                // property has not been accessed yet, so we have to create an index for it
                if (ret < 0)
                {
                    PropertyIndexLock.EnterWriteLock();
                    try
                    {
                        _propertyIndex[propertyId] = ++_topIndex;
                        ret = _topIndex;
                    }
                    finally
                    {
                        PropertyIndexLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                PropertyIndexLock.ExitUpgradeableReadLock();
            }

            return (byte)ret;
        }
    }
}
