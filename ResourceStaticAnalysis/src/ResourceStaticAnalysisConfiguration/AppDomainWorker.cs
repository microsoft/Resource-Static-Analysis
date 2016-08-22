/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;

namespace Microsoft.ResourceStaticAnalysis.Configuration
{
    /// <summary>
    /// Class that performs work in a seperate AppDomain.
    /// </summary>
    /// <typeparam name="T">Type of object to create within a seperate AppDomain.</typeparam>
    public class AppDomainWorker<T> : IDisposable
    {
        private AppDomain applicationDomain;
        private T worker;
        private static ClientSponsor sponsor = new ClientSponsor();

        private const String appDomainNameFormat = "{0}_{1}";

        /// <summary>
        /// Initialize a new instance of the AppDomainWorker class.
        /// </summary>
        public AppDomainWorker()
            : this(String.Format(appDomainNameFormat, Guid.NewGuid(), DateTime.Now), AppDomain.CurrentDomain.SetupInformation)
        {
        }

        /// <summary>
        /// Initialize a new instance of the AppDomainWorker class.
        /// </summary>
        /// <param name="appDomainName">Name to use as the friendly name of the new AppDomain.</param>
        /// <param name="appDomainSetupInformation">AppDomainSetup information to use when creating the new AppDomain.</param>
        public AppDomainWorker(String appDomainName, AppDomainSetup appDomainSetupInformation)
        {
            if (String.IsNullOrWhiteSpace(appDomainName))
            {
                appDomainName = String.Format(appDomainNameFormat, Guid.NewGuid(), DateTime.Now);
            }

            if (appDomainSetupInformation == null)
            {
                appDomainSetupInformation = AppDomain.CurrentDomain.SetupInformation;
            }

            this.applicationDomain = AppDomain.CreateDomain(appDomainName, AppDomain.CurrentDomain.Evidence,
                appDomainSetupInformation, AppDomain.CurrentDomain.PermissionSet);
        }

        /// <summary>
        /// Worker object.
        /// </summary>
        public T Worker
        {
            get
            {
                bool recreateWorker = false;
                MarshalByRefObject currentWorker = this.worker as MarshalByRefObject;
                if (currentWorker != null)
                {
                    try
                    {
                        ILease lease = (ILease)currentWorker.GetLifetimeService();
                        if (lease.CurrentState != LeaseState.Active)
                        {
                            recreateWorker = true;
                        }
                    }
                    catch (RemotingException)
                    {
                        recreateWorker = true;
                    }
                }

                if (recreateWorker || currentWorker == null)
                {
                    // Remove old worker lease
                    if (currentWorker != null)
                    {
                        try
                        {
                            sponsor.Unregister(currentWorker);
                        }
                        catch (RemotingException) { }
                    }

                    // Create new worker
                    this.worker = this.CreateInstance(this.applicationDomain);

                    // Add a lease to new object
                    currentWorker = this.worker as MarshalByRefObject;
                    if (currentWorker != null)
                    {
                        sponsor.Register(currentWorker);
                    }
                }

                return this.worker;
            }
        }

        /// <summary>
        /// Creates an instance of the required object within the supplied app domain.
        /// </summary>
        /// <param name="applicationDomain">AppDomain in which to create the instance in.</param>
        /// <returns></returns>
        private T CreateInstance(AppDomain applicationDomain)
        {
            return (T)this.applicationDomain.CreateInstanceAndUnwrap(
                System.Reflection.Assembly.GetExecutingAssembly().FullName, typeof(T).FullName);
        }

        /// <summary>
        /// Dispose of the AppDomainWorker object.
        /// </summary>
        public void Dispose()
        {
            MarshalByRefObject currentWorker = this.worker as MarshalByRefObject;
            if (currentWorker != null)
            {
                try
                {
                    sponsor.Unregister(currentWorker);
                }
                catch
                {
                    // Ignored
                }
            }

            IDisposable disposableWorker = this.worker as IDisposable;
            if (disposableWorker != null)
            {
                try
                {
                    disposableWorker.Dispose();
                }
                catch
                {
                    // Ignored
                }
            }

            this.worker = default(T);

            if (this.applicationDomain != null)
            {
                try
                {
                    AppDomain.Unload(this.applicationDomain);
                    this.applicationDomain = null;
                }
                catch
                {
                    // Ignored
                }
            }
        }
    }
}