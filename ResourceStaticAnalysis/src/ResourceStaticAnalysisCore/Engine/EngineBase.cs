/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

namespace Microsoft.ResourceStaticAnalysis.Core.Engine
{
    /// <summary>
    /// Abstract class being a base for any engine implementation.
    /// Contains essential interface.
    /// </summary>
    public abstract class EngineBase
    {
        /// <summary>
        /// Cleans up the instance of Engine. Used to flush all referenced objects to make sure memory doesn't grow 
        /// indefinitely. This method is to be called by the Engine client.
        /// </summary>
        public abstract void Cleanup();

        /// <summary>
        /// Starts executing the engine based on the config data provided in the constructor. Returns immediately with false if engine is already running.
        /// If the engine could not be started for any other reason, an exception should be thrown by this method.
        /// NOTE: This should start the Engine asynchronously.
        /// </summary>
        /// <returns>True if engine has been started. False if engine was already running.</returns>
        public abstract bool StartRun();

        /// <summary>
        /// Waits for all processing threads started by an instance of Engine to complete.
        /// <list type="ol"><listheader>This includes:</listheader>
        /// <item>Waiting for all rules to finish processing</item>
        /// <item>Waiting for all output writers to complete output and flush it to backing store</item>
        /// </list>
        /// </summary>
        public abstract void WaitForJobFinish();
    }
}
