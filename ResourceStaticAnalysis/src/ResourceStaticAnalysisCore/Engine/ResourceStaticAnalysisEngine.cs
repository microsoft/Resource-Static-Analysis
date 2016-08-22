/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Practices.AssemblyManagement;
using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.DataSource.DataSourcePackages;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.Core.Misc;
using Microsoft.ResourceStaticAnalysis.Core.Output;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine
{
    /// <summary>
    /// Main class that contains most logic of ResourceStaticAnalysis core.
    /// </summary>
    /// <seealso cref="EngineConfig"/>
    public partial class ResourceStaticAnalysisEngine : EngineBase
    {
        #region Static members
        /// <summary>
        /// All engines share the same StringCache so any code can call cache without referencing 
        /// a particular engine instance which simplifies coding. However, any engine can clean the cache at any time
        /// so engines should not rely on cache preserving values.
        /// Also, multiple engines should not really run at the same time...
        /// </summary>
        public static ObjectCache<string> StringCache
        {
            get { return stringCache; }
        }

        /// <summary>
        /// Cache for interning strings. Setting a large initial size as ResourceStaticAnalysis typically processes
        /// huge ammounts of strings
        /// </summary>
        private static readonly ObjectCache<string> stringCache = new ObjectCache<string>(1000, StringComparer.Ordinal, s => s.Length * sizeof(char));
        #endregion

        #region Private members
        /// <summary>
        ///  When the value is true, preloaded COs will be used as input to the engine
        ///  The constructor need not create instances of datasource providers, property adapters, etc.
        /// </summary>
        private bool _usePreLoadedCOs = false;

        private int _engineIsRunning;

        /// <summary>
        /// Timer used to debug the performance. Initialized in <see cref="BeginProcessClassificationObjects"/>
        /// </summary>
        private RuleManager _currentRuleManager;

        /// <summary>
        /// How many objects added for processing by 2nd thread.
        /// </summary>
        private int _coLoaded;

        /// <summary>
        /// Maps instances of Property Adapters to Classification Object type they support.
        /// </summary>
        private readonly Dictionary<Type, IList<PropertyAdapter>> _propertyAdapters = new Dictionary<Type, IList<PropertyAdapter>>();

        private readonly ManualResetEvent _coProcessingDone = new ManualResetEvent(false);

        private readonly ManualResetEvent _allOutputFinished = new ManualResetEvent(false);
        
        /// <summary>
        /// DataAdapters are stored here; mapped from ClassificationObject type they create to a list of instances of adapters.
        /// There can be more than one type of data adapter producing the same CO type, as they may cover different data source combinations.
        /// </summary>
        private readonly Dictionary<Type, IList<ClassificationObjectAdapter>> _coAdapters = new Dictionary<Type, IList<ClassificationObjectAdapter>>();
        
        /// <summary>
        /// The queue of DataSourcePackage objects that needs to be provided to ResourceStaticAnalysis Engine;
        /// </summary>
        private readonly Queue<DataSourcePackage> _dataSourcePackages;
        
        /// <summary>
        /// Stores instances of Space for each type of Classification Object loaded by the engine.
        /// </summary>
        private readonly Dictionary<Type, Space> _spaces = new Dictionary<Type, Space>();
        private readonly List<ClassificationObject> _preProcessedInput;
        #endregion

        #region Internal members
        /// <summary>
        /// ClassificationObject types known to the engine. These are configured using ResourceStaticAnalysis config.
        /// </summary>
        internal Dictionary<string, Type> _coTypes = new Dictionary<string, Type>();
        
        /// <summary>
        /// Stores instances of output writers as defined in engine config.
        /// </summary>
        internal List<IOutputWriter> _outputWriters = new List<IOutputWriter>();
        
        /// <summary>
        /// Engine config specified during construction time. Engine then organizes the creation of Spaces and COs based on the packages
        /// </summary>
        internal EngineConfig _engineConfiguration;
        #endregion

        #region Public API
        /// <summary>
        /// New constructor created by PiotrCi.
        /// This tests using XML configuration packages.
        /// This constructor is independent from the 0-args constructor and is incompatible with the older model!
        /// </summary>
        /// <param name="configuration">Configuration read from disk or created in memory</param>
        public ResourceStaticAnalysisEngine(EngineConfig configuration)
            : this(configuration, false)
        {
        }

        /// <summary>
        /// Constructor to initialize the ResourceStaticAnalysis Engine by configuration
        /// </summary>
        /// <param name="configuration">the enging config</param>
        /// <param name="preLoadedCOs">Initialize the engine to accept preloaded COs instead of data sources</param>
        public ResourceStaticAnalysisEngine(EngineConfig configuration, bool preLoadedCOs)
        {
            _engineConfiguration = configuration;
            AsimoAssemblyResolver = new AssemblyResolver(configuration.AssemblyResolverPaths == null ? null : _engineConfiguration.AssemblyResolverPaths.ToArray());
            AsimoAssemblyResolver.Init();

            Monitor = new EngineMonitor(this);

            //clear string cache
            StringCache.Clear();
            try
            {
                //Don't need to load objects when using preloaded COs.
                if (preLoadedCOs)
                {
                    _usePreLoadedCOs = true;
                    //Create instances of output writers
                    RegisterOutputWriters();
                }
                else
                {
                    Trace.TraceInformation("Initializing engine from {0}.", System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
                    Trace.TraceInformation("Engine configuration: {0}.", configuration.ToString());
                    //Create instances of datasource providers
                    RegisterDataSourceProviders();
                    //Create instances of property adapters to be used by CO adapters
                    RegisterPropertyAdapters();
                    //Create instances of data adapters
                    RegisterCOAdapters();
                    //Create instances of output writers
                    RegisterOutputWriters();
                    //Register ClassificationObject types
                    RegisterCOTypes();
                    //normalize dataPackages. if two DataSourcePackages contain the same DataSourceInfo then
                    //link them to single DataSourceInfo. otherwise two instances of the same DataSourceInstance
                    //will be created when these packages are accessed.
                    _engineConfiguration.DataSourcePkgs.Normalize();

                    _dataSourcePackages = new Queue<DataSourcePackage>(_engineConfiguration.DataSourcePkgs);
                    Trace.TraceInformation("Found {0} data source packages.", this._dataSourcePackages.Count);
                }

                //load rules from assemblies modules
                Trace.TraceInformation("Loading rules from containers.");
                if (_engineConfiguration.RuleContainers == null)
                {
                    throw new ResourceStaticAnalysisEngineConfigException("At least one Rule Container must be specified in config.");
                }

                Trace.TraceInformation("Adding rules into RuleManager.");
                configuration.RuleContainers.ForEach(CurrentRuleManager.LoadRulesFromContainer);
                Trace.TraceInformation("Disabling rules based on config.");
                if (_engineConfiguration.DisabledRules != null)
                {
                    configuration.DisabledRules.ForEach(f => CurrentRuleManager.DisableRule(f));
                }

                if (preLoadedCOs)
                {
                    Trace.TraceInformation("RuleManager has {0} rules registered.", CurrentRuleManager.RuleCount);
                }
                else
                {
                    Trace.TraceInformation("Found {0} data source packages. RuleManager has {1} rules registered.",
                    this._dataSourcePackages.Count, CurrentRuleManager.RuleCount);
                }
            }
            catch (Exception e)
            {
                string message = String.Format(CultureInfo.CurrentCulture, "Initializing ResourceStaticAnalysis Engine failed.");
                throw new ResourceStaticAnalysisEngineInitializationException(message, e);
            }
            _preProcessedInput = new List<ClassificationObject>(1000);
            Trace.TraceInformation("Engine finished initializing.");
        }

        /// <summary>
        /// Reference to <see cref="AssemblyResolver"/> used by ResourceStaticAnalysis to resolve its references.
        /// </summary>
        public readonly AssemblyResolver AsimoAssemblyResolver;

        /// <summary>
        /// Engine monitor objects that stores different statistics related to Engine's execution.
        /// </summary>
        public EngineMonitor Monitor { get; private set; }

        /// <summary>
        /// A CO List which is initiated and passed directly to the Engine instead of using data sources
        /// </summary>
        public List<ClassificationObject> ParsedCOList = new List<ClassificationObject>();

        /// <summary>
        /// Cleans up the instance of ResourceStaticAnalysis. Used to flush all referenced objects to make sure memory doesn't grow indefinitely.
        /// </summary>
        public override void Cleanup()
        {
            StringCache.Clear();
            StringCache.Resize(0);
            if (EngineCleanup != null)
            {
                Trace.TraceInformation("Firing EngineCleanup event.");
                EngineCleanup(this, new EventArgs());
            }
            else
            {
                Trace.TraceWarning("No listeners of EngineCleanup event. Event not fired.");
            }
        }

        /// <summary>
        /// Waits for all processing threads started by an instance of Engine to complete.
        /// <list type="ol"><listheader>This includes:</listheader>
        /// <item>Waiting for all rules to finish processing</item>
        /// <item>Waiting for all output writers to complete output and flush it to backing store</item>
        /// </list>
        /// </summary>
        public override void WaitForJobFinish()
        {
            CoProcessingDone.WaitOne();  // block this thread until the engine is done
            AllOutputFinished.WaitOne(); //wait for output to finish
        }

        /// <summary>
        /// Starts executing the Engine based on the config data provided in the constructor. Returns immediately with false if engine is already running.
        /// </summary>
        /// <returns>True if engine has been started. False if engine was already running.</returns>
        /// <exception cref="ResourceStaticAnalysisEngineInitializationException">Thrown when:
        /// <para/>there is no rules registered for execution
        /// <para/>or expected data source provider is not available
        /// <para/>or where there is no primary data source
        /// <para/>or when data source instance could not be initialized properly. This could happen - for example - if a file comprising the datasource does not exist.
        /// <para/>Inner exception is of type <see cref="ResourceStaticAnalysisException"/> and contains more details about the specific cause of failure.
        /// </exception>
        public override bool StartRun()
        {
            if (CurrentRuleManager.RuleCount == 0)
            {
                Trace.TraceError("Engine state incorrect. No rules registered.");
                throw new ResourceStaticAnalysisEngineInitializationException("Engine state incorrect. No rules registered.");
            }
            if (0 == Interlocked.Exchange(ref this._engineIsRunning, 1))
            {
                Trace.TraceInformation("Starting engine on ThreadPool worker thread.");
                StartProcessing();
                return true;
            }
            else
            {
                Trace.TraceWarning("Engine already running on ThreadPool worker thread. Start aborted.");
                return false;
            }
        }

        /// <summary>
        /// Event that gets signalled by the engine when processing of all ClassificationObjects has been completed. Output writing may still be in progress.
        /// </summary>
        public ManualResetEvent CoProcessingDone { get { return _coProcessingDone; } }
        /// <summary>
        /// Event that gets signalled by the engine when output writing to all output writers has finished. This means that all ResourceStaticAnalysis tasks have been completed.
        /// </summary>
        public ManualResetEvent AllOutputFinished { get { return _allOutputFinished; } }
        /// <summary>
        /// Gets a reference to RuleManager owned by this engine.
        /// </summary>
        public RuleManager CurrentRuleManager
        {
            get
            {
                if (_currentRuleManager == null)
                {
                    _currentRuleManager = new RuleManager(this);
                }
                return _currentRuleManager;
            }
        }
        #endregion

        #region Private API
        private void RegisterPropertyAdapters()
        {
            if (_engineConfiguration.PropertyAdapterTypes == null) throw new ResourceStaticAnalysisEngineConfigException("At least one Property Adapter must be specified in config.");

            try
            {
                foreach (TypeSpecification ts in _engineConfiguration.PropertyAdapterTypes)
                {
                    Type paType = ts.GetTypeFromModule();
                    var propertyAdapter = (PropertyAdapter)Activator.CreateInstance(paType, false);
                    Type coType = propertyAdapter.ClassificationObjectType;
                    Trace.TraceInformation("Creating PropertyAdapter for ClassificationObject type: {0}, DataSource Type {3}\nPropertyAdapter type: {1}, assembly: {2}",
                    coType.FullName,
                    propertyAdapter.GetType().FullName,
                    ts.AssemblyName,
                    propertyAdapter.DataSourceType.FullName);

                    if (_propertyAdapters.ContainsKey(coType))
                    {
                        // add to the list of data adapters for this co type
                        _propertyAdapters[coType].Add(propertyAdapter);
                    }
                    else
                    {
                        _propertyAdapters.Add(coType, new List<PropertyAdapter>(new[] { propertyAdapter }));
                    }
                }
                if (_propertyAdapters.Count < 1)
                {
                    throw new ResourceStaticAnalysisException("No property adapters have been registered. Check engine configuration to make sure at least one property adapter is defined.");
                }
            }
            catch (Exception e)
            {
                throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture, "Could not load Data Adapters from assembly."), e);
            }
        }
        private void RegisterCOTypes()
        {
            if (_engineConfiguration.COTypes == null)
            {
                throw new ResourceStaticAnalysisEngineConfigException("At least one Classification Object type must be specified in config.");
            }

            foreach (var ts in this._engineConfiguration.COTypes)
            {
                if (_coTypes.ContainsKey(ts.TypeName))
                {
                    throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture, "Classification Object type {0} is already registered with the engine.",
                          ts.TypeName));
                }
                _coTypes.Add(ts.TypeName, ts.GetTypeFromModule());
            }
        }

        private void RegisterOutputWriters()
        {
            if (_engineConfiguration.OutputConfigs == null)
                throw new ResourceStaticAnalysisEngineConfigException("At least one Output configuration must be specified in config.");

            foreach (OutputWriterConfig owc in this._engineConfiguration.OutputConfigs)
            {
                IOutputWriter ow = null;
                try
                {
                    ow = (IOutputWriter)Activator.CreateInstance(owc.Kind.GetTypeFromModule());
                    Trace.TraceInformation("Initializing OutputWriter: {0}.", ow.GetType().FullName);
                    ow.Initialize(owc);
                    _outputWriters.Add(ow);
                }
                catch (TypeLoadException e)
                {
                    throw new ResourceStaticAnalysisEngineInitializationException(String.Format(CultureInfo.CurrentCulture, "ERROR: Error while trying to create IOutputWriter of type {0}. This type is not a known type.",
                       owc.Kind), e);
                }
                catch (FileNotFoundException e)
                {
                    throw new ResourceStaticAnalysisEngineInitializationException(String.Format(CultureInfo.CurrentCulture, "ERROR: Error while trying to create IOutputWriter of type {0}. Failed loading assemblies.",
                        owc.Kind), e);
                }
                catch (Exception e)
                {
                    if (ow == null)
                        throw new ResourceStaticAnalysisEngineInitializationException(String.Format(CultureInfo.CurrentCulture, "ERROR: Error while trying to create IOutputWriter of type {0}. This type is not a IOutputWriter.",
                            owc.Kind), e);
                    else
                        throw new ResourceStaticAnalysisEngineInitializationException(String.Format(CultureInfo.CurrentCulture, "ERROR: Error while trying to create IOutputWriter of type {0}.",
                            owc.Kind), e);
                }
            }
        }
        private void RegisterCOAdapters()
        {
            if (_engineConfiguration.COAdapterTypes == null)
            {
                throw new ResourceStaticAnalysisEngineConfigException("At least one Classification Object Adapter type must be specified in config.");
            }

            try
            {
                foreach (TypeSpecification ts in _engineConfiguration.COAdapterTypes)
                {
                    Type daType = ts.GetTypeFromModule();
                    var coAdapter = (ClassificationObjectAdapter)Activator.CreateInstance(daType, new object[] { this._propertyAdapters });
                    Type coType = coAdapter.ClassificationObjectType;
                    Trace.TraceInformation("Creating DataAdapter for ClassificationObject type: {0}\nDataAdapter type: {1}, assembly: {2}",
                       coType.FullName,
                       coAdapter.GetType().FullName,
                       ts.AssemblyName);

                    if (_coAdapters.ContainsKey(coType))
                    {
                        // add to the list of data adapters for this co type
                        _coAdapters[coType].Add(coAdapter);
                    }
                    else
                    {
                        _coAdapters.Add(coType, new List<ClassificationObjectAdapter>(new[] { coAdapter }));
                    }
                }
                if (_coAdapters.Count < 1)
                    throw new ResourceStaticAnalysisException("No data adapters have been registered. Check engine configuration to make sure at least one data adapter is defined.");
            }
            catch (Exception e)
            {
                throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture, "Could not load Data Adapters from assembly."), e);
            }
        }

        /// <summary>
        /// DataSourceProviders are stored here; mapped from Data Source type name they create to instance of provider.
        /// </summary>
        private readonly Dictionary<string, IDataSourceProvider> _dataSourceProviders = new Dictionary<string, IDataSourceProvider>();
        
        /// <summary>
        /// Creates instances of data source provides based on the DataSourceProvider assemblies and stores them in a dictionary
        /// </summary>
        private void RegisterDataSourceProviders()
        {
            if (_engineConfiguration.DataSourceProviderTypes == null) throw new ResourceStaticAnalysisEngineConfigException("At least one data source provider type must be specified in config.");

            try
            {
                foreach (TypeSpecification ts in _engineConfiguration.DataSourceProviderTypes)
                {
                    Type dspType = ts.GetTypeFromModule();
                    var dataSourceProvider = (IDataSourceProvider)Activator.CreateInstance(dspType, false);
                    Type dataSourceType = dataSourceProvider.DataSourceType;
                    Trace.TraceInformation("Creating DataSourceProvider for DataSource type: {0}\nDataSourceProvider type: {1}, assembly: {2}",
                        dataSourceType.FullName,
                        dspType.FullName,
                        ts.AssemblyName);

                    if (_dataSourceProviders.ContainsKey(dataSourceType.FullName))
                    {
                        throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture, "Cannot add DataSourceProvider {0} because data source provider for datasource type {1} is already registered. Registered provider: {2}",
                            dspType.FullName,
                            dataSourceType.FullName,
                            this._dataSourceProviders[dataSourceType.FullName].GetType().FullName));
                    }
                    Trace.TraceInformation("Initializing Data Source Provider {0}, assembly: {1}.", ts.TypeName, ts.AssemblyName);
                    _dataSourceProviders.Add(dataSourceType.FullName, dataSourceProvider);
                }
                if (this._dataSourceProviders.Count < 1)
                {
                    throw new ResourceStaticAnalysisException("No data source providers have been registered. Check engine configuration to make sure at least one data source provider is defined.");
                }
            }
            catch (Exception e)
            {
                throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture, "Could not load DataSourceProviders from assembly."), e);
            }
        }

        /// <summary>
        /// initial loaded objects' count and _preProcessedInput
        /// check if we have already created a space for this object type
        /// if not created a space, we are going to create one plus use an object of the new type to register for cleanup
        /// it is used by two places in LoadAndQueueCOs.
        /// </summary>
        /// <param name="loadedObjects"></param>
        private void PrepareResourceItems(ICollection<ClassificationObject> loadedObjects)
        {
            // Pre-process the objects.
            // Consider changing to event-based if there is need to make the post-processing more complex
            // Currently pre-processing is only adding objects to the total queue of all objects to be processed by rules
            _preProcessedInput.AddRange(loadedObjects);
            _coLoaded += loadedObjects.Count;

            // try to get existing Space for this Classification Object type and create one if it does not exist
            var firstCO = loadedObjects.First();
            var coType = firstCO.GetType();

            Space targetSpace;
            // check if we have already created a space for this object type.
            // if not we are going to create one plus use an object of the new type to register for cleanup.
            if (!_spaces.TryGetValue(coType, out targetSpace))
            {
                targetSpace = new Space();
                _spaces.Add(coType, targetSpace);
                // register any cleanup code for the new CO type
                firstCO.RegisterTypeForEngineCleanup(this);
            }
            targetSpace.AddObjectsSafe(loadedObjects);
        }

        /// <summary>
        /// Starts processing based on the supplied configuration.
        /// </summary>
        /// <exception cref="ResourceStaticAnalysisEngineInitializationException">Thrown from <see cref="LoadAndQueueCOs"/> when:
        /// expected data source provider is not available
        /// or where there is no primary data source
        /// or when data source instance could not be initialized properly. This could happen - for example - if a file comprising the datasource does not exist.
        /// Inner exception is of type <see cref="ResourceStaticAnalysisException"/> and contains more details about the specific cause of failure.
        /// </exception>
        private void StartProcessing()
        {
            // Synchronously load objects.
            LoadAndQueueCOs();
            // Start asynchrounous part by generating properties. when this finishes, processing of objects will start.
            BeginProcessClassificationObjects();
            return;
        }

        private void BeginProcessClassificationObjects()
        {
            Trace.TraceInformation("Processing Classification Objects started. Number of objects: {0}", _preProcessedInput.Count);
            if (ProcessingStarting != null)
            {
                ProcessingStarting(this, null);
            }

            // Synchronous for now.
            try
            {
                Trace.TraceInformation("---> Loaded " + _coLoaded + " resource items, start running rules...");
                CurrentRuleManager.OnRulesInitializationEvent(); //Initialize all rules.
            }
            catch (Exception ex)
            {
                Monitor.LogException(ex);
                throw new ResourceStaticAnalysisEngineInitializationException("Failed to initialize ResourceStaticAnalysis Rules", ex);
            }

            //NOTE: By design, each Rule may only process one CO at a time - Rules are not thread-safe, by choice
            //(thread-unsafe design provides for easier authoring of rules by users)
            //Therefore in multi-thread scenario each rule will be invoked as a separate job on the entire set of
            //COs. this may be less efficient in some cases, but allows us to preserve the current design.
            CurrentRuleManager.ClassifyObjectsAsynchronously(_preProcessedInput, ClassifyObjectsDone);
            
            //we let the thread return, we will finalize engine asynchronously when all objects have been processed
            return;
        }

        private void EndProcessClassificationObjects()
        {
            try
            {
                if (ProcessingFinished != null)
                {
                    ProcessingFinished(this, null);
                }
                Trace.TraceInformation("Processing Classification Objects finished. (took: {0})", Monitor.ProcessingElapsed);
                CurrentRuleManager.OnRulesCleanupEvent();

                if (OutputStarting != null)
                {
                    OutputStarting(this, null);
                }
                Trace.TraceInformation("Creating output started.");

                // flush output to all the registered output writers
                // this is done in synchronous manner to support simple architecture for output writers
                // output writers can be asynchronous but it has to be transparent to the engine, i.e. they would have to 
                // block on IOutputWriter.Finish to wait for asynchronous operations to finalize

                CurrentRuleManager.OutputWriter.FlushOutput(CurrentRuleManager.OutputFromAllRules);
                CurrentRuleManager.OutputWriter.FinishOutputWriters();

                if (OutputFinished != null)
                {
                    OutputFinished(this, null);
                }
                Trace.TraceInformation("Creating output finished. (took: {0})", Monitor.OutputElapsed);
            }
            catch (OutOfMemoryException e)
            {
                Trace.TraceError("Ran out of memory writing output logs. Further processing will be aborted...");
                Monitor.LogException(e);
                AbortProcessing();
            }
            finally
            {
                AllOutputFinished.Set();
                CoProcessingDone.Set();
            }
        }

        private void ClassifyObjectsDone(IAsyncResult result)
        {
            ClassifyCollectionOfCOsEventHandler coA = null;
            try
            {
                coA = (ClassifyCollectionOfCOsEventHandler)((System.Runtime.Remoting.Messaging.AsyncResult)result).AsyncDelegate;
                coA.EndInvoke(result);
            }
            catch (RuleManagerException e)
            {
                Trace.TraceError("Rule {0} failed processing object {1}. {2}",
                    coA.Target.GetType().FullName,
                    (coA.Target as Rule).CurrentCO.ToString(),
                    e.GetExceptionDetails(false));
                Monitor.LogException(e);
            }
            catch (OutOfMemoryException e)
            {
                Trace.TraceError("Ran out of memory when processing Classification Objects. Further processing will be aborted...");
                Monitor.LogException(e);
                AbortProcessing();
            }
            finally
            {
                if (Interlocked.CompareExchange(ref CurrentRuleManager._noOfRulesRunning, -1, 0) == 0)
                    EndProcessClassificationObjects();
                else
                    Trace.TraceInformation("Number of rules still running: {0}", CurrentRuleManager._noOfRulesRunning);
            }
        }

        private void AbortProcessing()
        {
            CoProcessingDone.Set();
            AllOutputFinished.Set();
        }

        /// <summary>
        /// This is the second thread of ResourceStaticAnalysis. It communicates with
        /// Classification Object and Space factories in order to load objects from
        /// data packages.
        /// It also does all the required pre-processing on objects, such as marking objects
        /// for processing.
        /// It then starts Enigne3rdThread in order to start Classification Object processing by rules
        /// </summary>
        /// <exception cref="ResourceStaticAnalysisEngineInitializationException">Thrown when:
        /// <para/>expected data source provider is not available
        /// <para/>or where there is no primary data source
        /// <para/>or when data source instance could not be initialized properly. This could happen - for example - if a file comprising the datasource does not exist.
        /// <para/>Inner exception is of type <see cref="ResourceStaticAnalysisException"/> and contains more details about the specific cause of failure.
        /// </exception>
        private void LoadAndQueueCOs()
        {
            Trace.TraceInformation("Loading and queueing objects started.");
            if (LoadingStarting != null)
            {
                LoadingStarting(this, null);
            }

            /* this loads all CO objects of type specified in dsPkg and from data sources
             * specified in dsPkg into an appropriate space supporting that type of CO.
             * if the space does not already exist in coSpaces it will be created.
             * otherwise COs will be added to the existing space.
             * 
             * Consider re-designing this part to be multi-thread so packages
             * are loaded in separate threads to speed up
             */
            _coLoaded = 0;
            if (_dataSourcePackages != null && _dataSourcePackages.Count > 0)
            {
                Trace.TraceInformation("Engine is processing packages...");
                while (_dataSourcePackages.Count > 0)
                {
                    DataSourcePackage dataSourcePackage;
                    lock ((_dataSourcePackages as ICollection).SyncRoot)
                    {
                        dataSourcePackage = _dataSourcePackages.Dequeue();
                    }
                    if (dataSourcePackage != null)
                    {
                        try
                        {
                            Trace.TraceInformation("Engine is processing package: {0}", dataSourcePackage.ToString());
                            dataSourcePackage.Initialize(this._dataSourceProviders);

                            var loadedObjects = LoadClassificationObjects(dataSourcePackage, this.CurrentRuleManager.RuleCount);

                            if (loadedObjects.Count < 1)
                                continue;

                            PrepareResourceItems(loadedObjects);
                        }
                        catch (Exception e)
                        {
                            throw new ResourceStaticAnalysisEngineInitializationException(String.Format(CultureInfo.CurrentCulture, "Error processing DataSourcePackage: {0}.",
                                dataSourcePackage.ToString()), e);
                        }
                    }
                }
            }
            else if (_usePreLoadedCOs)
            {
                Trace.TraceInformation("Engine is processing objects...");
                List<ClassificationObject> loadedObjects = ParsedCOList;

                if (loadedObjects != null && loadedObjects.Count > 0)
                    PrepareResourceItems(loadedObjects);
            }

            _preProcessedInput.TrimExcess();

            // now that we know the number of objects to process, set the string cache capacity to limit the
            // number of cache resizes during execution
            StringCache.Resize(_preProcessedInput.Count * 5);


            if (_engineConfiguration.DataSourcePkgs != null)
            {
                //All DataSourcePackages have been loaded, we can dispose of the packages and underlying
                //data sources
                _engineConfiguration.DataSourcePkgs.ForEach(dsp =>
                {
                    if (!Object.ReferenceEquals(dsp, null))
                    {
                        dsp.Dispose();
                    }
                });
            }

            if (LoadingFinished != null)
            {
                LoadingFinished(this, new EventArgs<int>(_preProcessedInput.Count));
            }
            Trace.TraceInformation("Loading and queueing objects finished. (took {1}). Objects loaded: {0}.", _coLoaded, Monitor.LoadingElapsed);
        }

        /// <summary>
        /// Load co objects. 
        /// </summary>
        /// <param name="package"></param>
        /// <param name="maxOutputPerObject"></param>
        /// <returns></returns>
        private ICollection<ClassificationObject> LoadClassificationObjects(DataSourcePackage package, int maxOutputPerObject)
        {
            Type coType;
            IList<ClassificationObjectAdapter> adaptersList;
            if (!_coTypes.TryGetValue(package.ClassificationObjectTypeName, out coType))
            {
                throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture, "Unknown Classification Object type {0}. Make sure CO types are properly registered with engine.",
                    package.ClassificationObjectTypeName
                    ));
            }
            if (!this._coAdapters.TryGetValue(coType, out adaptersList))
            {
                throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture, "No adapters registered for Classification Object type {0}.",
                    package.ClassificationObjectTypeName
                    ));
            }

            foreach (var coAdapter in adaptersList)
            {
                if (coAdapter.PackageIsSupported(package))
                {
                    return coAdapter.InitializeObjects(package) ?? new ClassificationObject[0];
                }
            }

            throw new ResourceStaticAnalysisException(String.Format(CultureInfo.CurrentCulture, "No adapters registered for Classification Object type {0} that support the current data source package:\n"
                   + "Primary data source: {1}, secondary data sources: {2}",
                    package.ClassificationObjectTypeName,
                    package.PrimaryDataSource.SourceInstanceType.FullName,
                    String.Join(",", package.SecondaryDataSources.Select(ds => ds.SourceInstanceType.FullName).ToArray())
                    ));
        }

        #endregion

        #region Engine Events
        /// <summary>
        /// Register for this event in order to be able to execute cleanup code when Engine is cleaning up.
        /// Static helper caches and other non-instance fields should be cleaned up when this event is fired
        /// to enable multiple ResourceStaticAnalysis runs in one CLR session.
        /// </summary>
        public event EventHandler EngineCleanup;

        /// <summary>
        /// Fired when engine is about to start loading Classification Objects from data sources.
        /// </summary>
        public event EventHandler LoadingStarting;

        /// <summary>
        /// Fired when engine finished loading Classification Objects. EventArgs contain the number of objects loaded by engine.
        /// </summary>
        public event EventHandler<EventArgs<int>> LoadingFinished;

        /// <summary>
        /// Fired when engine is about to start processing Classification Objects.
        /// </summary>
        public event EventHandler ProcessingStarting;

        /// <summary>
        /// Fired when engine finished processing Classification Objects.
        /// </summary>
        public event EventHandler ProcessingFinished;

        /// <summary>
        /// Fired when engine is about to start writing output.
        /// </summary>
        public event EventHandler OutputStarting;

        /// <summary>
        /// Fired when engine finished writing output.
        /// </summary>
        public event EventHandler OutputFinished;
        #endregion
    }
}
