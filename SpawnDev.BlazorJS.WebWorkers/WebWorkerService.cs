using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Web;
using Array = SpawnDev.BlazorJS.JSObjects.Array;

namespace SpawnDev.BlazorJS.WebWorkers
{
    // chrome://inspect/#workers
    /// <summary>
    /// WebWorkerService provides access to WebWorkers, SharedWebWorkers, ServiceWorkers, and other running Windows from this origin<br/>
    /// </summary>
    public class WebWorkerService : IDisposable, IAsyncBackgroundService
    {
        const string instanceOwnerIdKey = "instanceOwnerIdKey";
        const string childIdKey = "tempIdKey";
        /// <summary>
        /// Completes successfully when asynchronous initialization has completed
        /// </summary>
        public Task Ready => _Ready ??= InitAsync();
        private Task? _Ready = null;
        /// <summary>
        /// If this instance is running in a DedicatedWorkerGlobalScope this is a connection to the parent instance
        /// </summary>
        public ServiceCallDispatcher? DedicatedWorkerParent { get; private set; } = null;
        /// <summary>        
        /// If this instance is running in a SharedWorkerGlobalScope this is all the incoming connections
        /// </summary>
        public List<ServiceCallDispatcher> SharedWorkerIncomingConnections { get; private set; } = new List<ServiceCallDispatcher>();
        /// <summary>
        /// Returns true if SharedWebWorker is supported
        /// </summary>
        public bool SharedWebWorkerSupported { get; private set; }
        /// <summary>
        /// Returns true if WebWorker is supported
        /// </summary>
        public bool WebWorkerSupported { get; private set; }
        /// <summary>
        /// Returns true if ServiceWorker is supported
        /// </summary>
        public bool ServiceWorkerSupported { get; private set; }
        /// <summary>
        /// Returns true if BroadcastChannel is supported
        /// </summary>
        public bool BroadcastChannelSupported { get; private set; }
        /// <summary>
        /// Returns true if LockManager is supported
        /// </summary>
        public bool LockManagerSupported { get; private set; }
        /// <summary>
        /// If true, InterConnect can be used for inter-instance communication negotiation
        /// </summary>
        public bool InterConnectSupported { get; private set; }
        /// <summary>
        /// If set to true and InterConnectSupported == true, the interconnect shared worker will be used to enable using MessagePort for inter-instance communication instead of BroadcastChannel>br/>
        /// MessagePort supports transferable objects, BroadcastChannel does not. 
        /// </summary>
        public bool InterConnectEnabled { get; set; } = true;
        /// <summary>
        /// If true, the fake window environment created to allow Blazor to load is cleaned up after the Blazor app has loaded.<br />
        /// The default is false for compatibility with other libraries.
        /// </summary>
        public bool RestoreEnvironment { get; set; } = false;
        /// <summary>
        /// A ServiceCallDispatcher that executes on this instance
        /// </summary>
        public ServiceCallDispatcher Local { get; } = default!;
        /// <summary>
        /// An instance of IServiceProvider this service belongs to
        /// </summary>
        public IServiceProvider ServiceProvider { get; }
        /// <summary>
        /// An instance of IServiceCollection this service belongs to
        /// </summary>
        public IServiceCollection ServiceDescriptors { get; }
        /// <summary>
        /// The baseUri of this Blazor app
        /// </summary>
        public string AppBaseUri { get; } = "";
        /// <summary>
        /// Returns true if this service has been initialized
        /// </summary>
        public bool BeenInit { get; private set; }
        /// <summary>
        /// The navigator.hardwareConcurrency value
        /// </summary>
        public int MaxWorkerCount { get; private set; } = 0;
        /// <summary>
        /// If this is a shared worker, this is the shared worker's name
        /// </summary>
        public string ThisSharedWorkerName { get; private set; } = "";
        /// <summary>
        /// This app instance's id
        /// </summary>
        public string InstanceId { get; }
        /// <summary>
        /// The script location used for new worker instances
        /// </summary>
        public string WebWorkerJSScript { get; } = "spawndev.blazorjs.webworkers.js";
        /// <summary>
        /// The script location used for new module worker instances
        /// </summary>
        public string WebWorkerModuleJSScript { get; } = "spawndev.blazorjs.webworkers.module.js";
        /// <summary>
        /// The script used for instance interconnect shared worker instance (if used)
        /// </summary>
        public string WebWorkerInterconnectJSScript { get; } = "spawndev.blazorjs.webworkers.interconnect.js";
        /// <summary>
        /// The instance of BlazorJSRuntime this service uses
        /// </summary>
        public BlazorJSRuntime JS { get; } = default!;
        /// <summary>
        /// The current global scope
        /// </summary>
        public GlobalScope GlobalScope { get; }
        /// <summary>
        /// If WebWorkers are supported it dispatches on a free WebWorker in the WebWorkerPool<br />
        /// If WebWorkers are not supported it dispatches on the local scope
        /// </summary>
        public WebWorkerPool TaskPool { get; } = default!;
        /// <summary>
        /// If this scope is a Window it dispatches on this scope<br />
        /// If this scope is a WebWorker and its parent is a Window it will dispatch on the parent's scope<br />
        /// Only available in a Window context, or in a WebWorker created by a Window
        /// </summary>
        public AsyncCallDispatcher? WindowTask { get; private set; }
        /// <summary>
        /// Configuration used for ServiceWorker registration
        /// </summary>
        public ServiceWorkerConfig ServiceWorkerConfig { get; private set; } = new ServiceWorkerConfig { Register = ServiceWorkerStartupRegistration.None };
        /// <summary>
        /// The HTML file that will be loaded by new WebWorker and SharedWebWorker instances<br/>
        /// to determine what Javascript &lt;script&gt; tags to load in the worker scope.<br/>
        /// If empty, the default will be used "./"<br/>
        /// </summary>
        public string WorkerIndexHtml { get; set; } = "";
        /// <summary>
        /// AppInstanceInfo for this app instance
        /// </summary>
        public AppInstanceInfo Info { get; } = default!;
        /// <summary>
        /// Returns true if this instance has notified other instances it exists
        /// </summary>
        public bool InstanceRegistered { get; private set; } = false;
        private bool InstanceRegistering = false;
        /// <summary>
        /// Navigator.Locks instance of LockManager or null if not supported
        /// </summary>
        public LockManager? Locks { get; private set; }
        private SharedWorker? Interconnect = null;
        private DateTime StartTime = DateTime.Now;
        private Callback? OnSharedWorkerConnectCallback = null;
        private BroadcastChannel? SharedBroadcastChannel = null;
        private TaskCompletionSource? InstanceLock = null;
        /// <summary>
        /// IBackgroundServiceManager singleton
        /// </summary>
        public IBackgroundServiceManager WebAssemblyServices { get; init; }

        NavigationManager NavigationManager;
        /// <summary>
        /// Creates a new instance of WebWorkerService
        /// </summary>
        public WebWorkerService(IBackgroundServiceManager webAssemblyServices, BlazorJSRuntime js, NavigationManager navigationManager)
        {
            JS = js;
            GlobalScope = JS.GlobalScope;
            InstanceId = JS.InstanceId;
            NavigationManager = navigationManager;
            WebAssemblyServices = webAssemblyServices;
            ServiceProvider = WebAssemblyServices.Services!;
            ServiceDescriptors = WebAssemblyServices.Descriptors;
            if (JS.IsBrowser)
            {
                WebWorkerSupported = !JS.IsUndefined("Worker");
                SharedWebWorkerSupported = !JS.IsUndefined("SharedWorker");
                ServiceWorkerSupported = !JS.IsUndefined("ServiceWorkerRegistration");
                AppBaseUri = JS.Get<string?>("document?.baseURI") ?? JS.Get<string>("documentBaseURI");
                var locationHref = JS.Get<string>("location.href");
                var locationUri = new Uri(locationHref);
                var workerScriptUri = new Uri(new Uri(AppBaseUri), WebWorkerJSScript);
                WebWorkerJSScript = workerScriptUri.ToString();
                WebWorkerModuleJSScript = new Uri(new Uri(AppBaseUri), WebWorkerModuleJSScript).ToString();
                Locks = JS.Get<LockManager>("navigator.locks");
                LockManagerSupported = Locks != null;
                var queryParams = HttpUtility.ParseQueryString(locationUri.Query);
                var isTaskPoolWorker = queryParams["taskPool"] == "1" && JS.IsScope(GlobalScope.DedicatedAndSharedWorkers);
                var instanceOwnerId = queryParams[instanceOwnerIdKey];
                var instanceChildId = queryParams[childIdKey];
                if (!string.IsNullOrEmpty(instanceOwnerId) && !string.IsNullOrEmpty(instanceChildId))
                {
                    queryParams.Remove(childIdKey);
                    queryParams.Remove(instanceOwnerIdKey);
                    locationHref = locationUri.GetLeftPart(UriPartial.Path) + (queryParams.Count == 0 ? "" : "?" + queryParams.ToString());
                    locationUri = new Uri(locationHref);
                    var newPath = NavigationManager.ToBaseRelativePath(locationHref);
                    // remove the instanceOwnerIdKey attribute from the url
                    NavigationManager.NavigateTo(newPath, false, true);
                }
                if (RestoreEnvironment && !JS.IsWindow)
                {
                    JS.Delete("window");
                    JS.Delete("document");
                }
                Info = new AppInstanceInfo
                {
                    OwnerId = instanceOwnerId,
                    ChildId = instanceChildId,
                    InstanceId = InstanceId,
                    Scope = GlobalScope,
                    Name = GetName(),
                    BaseUrl = AppBaseUri,
                    Url = locationHref,
                    TaskPoolWorker = isTaskPoolWorker,
                };
                BroadcastChannelSupported = !JS.IsUndefined(nameof(BroadcastChannel));
                if (BroadcastChannelSupported)
                {
                    // this is the BroadcastChannel all instances will send their instance info on at startup
                    SharedBroadcastChannel = new BroadcastChannel(nameof(WebWorkerService));
                    SharedBroadcastChannel.OnMessage += SharedBroadcastChannel_OnMessage;
                    // this is the BroadcastChannel the interconnect shared worker will message this instance on when messages are waiting
                    // - the message contains the from instance info object and a MessagePort
                    // if shared workers are not supported, instead of interconnect, a separate broadcast channel will be used and the request will come on this channel
                    // - the message contains the from instance info object and a broadcast channel name to use for the connection
                    InstanceBroadcastChannel = new BroadcastChannel(InstanceId);
                    InstanceBroadcastChannel.OnMessage += InstanceBroadcastChannel_OnMessage;
                }
                InterConnectSupported = SharedWebWorkerSupported && BroadcastChannelSupported;
#if DEBUG && false
                Console.WriteLine($"InterConnectSupported: {InterConnectSupported}");
                Console.WriteLine("hostEnvironment.BaseAddress: " + hostEnvironment.BaseAddress);
                Console.WriteLine("AppBaseUri: " + AppBaseUri);
                Console.WriteLine("WebWorkerJSScript: " + WebWorkerJSScript);
#endif
                var hardwareConcurrency = JS.Get<int?>("navigator.hardwareConcurrency");
                MaxWorkerCount = hardwareConcurrency ?? 1; // assume 1 if undefined/null (Safari 14.1 it is null, but it supports workers)
                if (IServiceCollectionExtensions.ServiceWorkerConfig != null)
                {
                    ServiceWorkerConfig = IServiceCollectionExtensions.ServiceWorkerConfig;
                }
                ;
                if (ServiceWorkerConfig == null) ServiceWorkerConfig = new ServiceWorkerConfig { Register = ServiceWorkerStartupRegistration.None };
                //if (string.IsNullOrEmpty(ServiceWorkerConfig.ScriptURL))
                //{
                //    ServiceWorkerConfig.ScriptURL = WebWorkerJSScript;
                //}
                Local = new ServiceCallDispatcher(WebAssemblyServices);
                if (isTaskPoolWorker)
                {
                    // task pool workers have their TaskPool.MaxWorkerPoolCount locked to 0.
                    // It can be unlocked, but this prevents apps that auto-start TaskPool workers from accidentally auto-starting task pool workers recursively
                    TaskPool = new WebWorkerPool(this, 0, 0, false, true, true);
                }
                else
                {
                    TaskPool = new WebWorkerPool(this, 0, 1, true);
                }
            }
        }
        BroadcastChannel? InstanceBroadcastChannel = null;
        private void EnableInterconnectWorker()
        {
            if (Interconnect == null)
            {
                Interconnect = new SharedWorker(WebWorkerInterconnectJSScript);
                Interconnect.Port.OnMessage += Interconnect_OnMessage;
                Interconnect.Port.Start();
            }
        }
        internal void SendInterconnectPort(string instanceId, MessagePort port)
        {
            EnableInterconnectWorker();
            Interconnect!.Port.PostMessage(new object[] { "dropOff", instanceId, Info }, new object[] { port });
        }
        private void GetInterconnectMessages()
        {
            EnableInterconnectWorker();
            // tell the interconnect we would like to pickup our messages
            Interconnect!.Port.PostMessage(new object[] { "pickUp", InstanceId });
        }
        private void InstanceBroadcastChannel_OnMessage(MessageEvent messageEvent)
        {
            // if interconnect is not available (likely due to missing SharedWorker support (Chrome Android))
            using var args = messageEvent.GetData<Array>();
            if (args != null && args.Length > 0)
            {
                var cmd = args.Shift<string>();
                if (cmd == "interconnect")
                {
                    // pick up the ports the interconnect has waiting for us.
                    GetInterconnectMessages();
                }
                else
                {
                    // message directly from another instance
                    var instanceInfo = args.Shift<AppInstanceInfo>();
                    if (!_Instances.TryGetValue(instanceInfo.InstanceId, out var fromInstance))
                    {
                        InstanceFound(instanceInfo, true);
                        _Instances.TryGetValue(instanceInfo.InstanceId, out fromInstance);
                    }
                    if (fromInstance == null) return;
                    switch (cmd)
                    {
                        case "broadcastChannelConnect":
                            var connectionId = args.Shift<string>();
                            fromInstance.AddIncomingInterconnectPort(new BroadcastChannel(connectionId));
                            break;
                    }
                }
            }
        }
        private class InterconnectIncomingMessage
        {
            public AppInstanceInfo FromInstanceInfo { get; set; } = default!;
            public long Time { get; set; }
        }
        private void Interconnect_OnMessage(MessageEvent messageEvent)
        {
            InterconnectIncomingMessage data;
            try
            {
                data = messageEvent.GetData<InterconnectIncomingMessage>();
            }
            catch
            {
                return;
            }
            if (data == null || data.FromInstanceInfo == null || string.IsNullOrEmpty(data.FromInstanceInfo.InstanceId)) return;
            var instanceInfo = data.FromInstanceInfo;
            if (!_Instances.TryGetValue(instanceInfo.InstanceId, out var fromInstance))
            {
                InstanceFound(instanceInfo, true);
                _Instances.TryGetValue(instanceInfo.InstanceId, out fromInstance);
            }
            if (fromInstance == null)
            {
                return;
            }
            using var ports = messageEvent.Ports;
            if (ports == null || ports.Length == 0) return;
            var port = ports[0];
            fromInstance.AddIncomingInterconnectPort(port);
        }
        private void SharedBroadcastChannel_OnMessage(MessageEvent messageEvent)
        {
            try
            {
                using var args = messageEvent.GetData<Array>();
                var argsLength = args.Length;
                var targetInstanceId = args.Shift<string>();
                if (targetInstanceId == AllInstancedId || targetInstanceId == InstanceId)
                {
                    var instanceInfo = args.Shift<AppInstanceInfo>();
                    if (!_Instances.TryGetValue(instanceInfo.InstanceId, out var fromInstance))
                    {
                        InstanceFound(instanceInfo, true);
                        _Instances.TryGetValue(instanceInfo.InstanceId, out fromInstance);
                    }
                    if (fromInstance == null)
                    {
                        return;
                    }
                    var cmd = args.Shift<string>();
                    switch (cmd)
                    {
                        case "register":
                            {
                                _ = UpdateInstancesViaLocks();
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Invalid message
                Console.WriteLine($"InstanceConnectChannel_OnMessage: {ex.Message}");
            }
            finally
            {
                messageEvent.Dispose();
            }
        }
        /// <summary>
        /// If true, incoming instance connections will be allowed
        /// </summary>
        public bool EnableIncomingInstanceConnections { get; set; } = true;
        const string AllInstancedId = "*";
        internal void BroadcastCall(string targetInstanceId, string cmd, params object?[]? args)
        {
            var allArgs = new List<object?>
            {
                targetInstanceId,
                Info,
                cmd
            };
            if (args != null && args.Length > 0) allArgs.AddRange(args);
            if (SharedBroadcastChannel != null)
            {
                SharedBroadcastChannel.PostMessage(allArgs);
            }
        }
        internal void BroadcastCall(string cmd, params object?[]? args) => BroadcastCall(AllInstancedId, cmd, args);
        /// <summary>
        /// If this instance is a dedicated worker the message is sent to the parent that created this worker<br/>
        /// If this instance is a shared worker the message is sent to all of the instances that have connected to this instance
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="data"></param>
        public void SendEventToParents(string eventName, object?[]? data = null)
        {
            if (DedicatedWorkerParent != null)
            {
                DedicatedWorkerParent.SendEvent(eventName, data);
            }
            foreach (var p in SharedWorkerIncomingConnections)
            {
                p.SendEvent(eventName, data);
            }
        }
        /// <summary>
        /// Sends an event with the specified name and associated data to all parent worker connections.
        /// </summary>
        /// <remarks>This method delivers the event to both dedicated and shared parent worker
        /// connections, if present. The event is sent to all connected parents; if no parents are connected, the method
        /// has no effect.</remarks>
        /// <param name="eventName">The name of the event to send to parent workers. Cannot be null or empty.</param>
        /// <param name="data">An array of event data objects to include with the event. Elements may be null if the event supports it.</param>
        /// <param name="transferableList">An array of objects to transfer ownership to parent workers as part of the event. The contents depend on the
        /// event and worker implementation.</param>
        public void SendEventToParents(string eventName, object?[] data, object[] transferableList)
        {
            if (DedicatedWorkerParent != null)
            {
                DedicatedWorkerParent.SendEvent(eventName, data, transferableList);
            }
            foreach (var p in SharedWorkerIncomingConnections)
            {
                p.SendEvent(eventName, data, transferableList);
            }
        }
        /// <summary>
        /// Fired after the service worker registration finishes successfully
        /// </summary>
        public event Action<ServiceWorkerRegistration> OnServiceWorkerRegistered = default!;
        /// <summary>
        /// The updatefound event of the ServiceWorkerRegistration interface is fired any time the ServiceWorkerRegistration.installing property acquires a new service worker.
        /// </summary>
        public event Action OnServiceWorkerUpdateFound = default!;
        /// <summary>
        /// This event fires if this instance is running in a WebWorker when the connection to the parent has been established
        /// </summary>
        public event Action OnDedicatedWorkerParentReady = default!;
        /// <summary>
        /// A list of running instances including this one<br/>
        /// Instances allows calling into any running instance
        /// </summary>
        public List<AppInstance> Instances => _Instances.Values.ToList();
        /// <summary>
        /// A list of known instances running in a window scope
        /// </summary>
        public List<AppInstance> Windows => _Instances.Values.Where(o => o.Info.Scope == GlobalScope.Window).ToList();
        private Dictionary<string, AppInstance> _Instances = new Dictionary<string, AppInstance>();
        /// <summary>
        /// Fires when a new instance is found
        /// </summary>
        public event Action<AppInstance> OnInstanceFound = default!;
        /// <summary>
        /// Fires when an instance is no longer running
        /// </summary>
        public event Action<AppInstance> OnInstanceLost = default!;
        /// <summary>
        /// Fires when one or more instances have been found or lost
        /// </summary>
        public event Action OnInstancesChanged = default!;
        private bool InstanceLost(string instanceId, bool fireChangedEvent)
        {
#if DEBUG && false
            JS.Log($"Tentative InstanceLost: {instanceId}", Instances.Select(o => o.Info).ToList());
#endif
            if (!_Instances.TryGetValue(instanceId, out var instance)) return false;
            if (instance.IsLocal) return false;
            _Instances.Remove(instanceId);
#if DEBUG && false
            JS.Log($"Instance lost: {instanceId}", instance.Info, Instances.Select(o => o.Info).ToList());
#endif
            OnInstanceLost?.Invoke(instance);
            instance.Dispose();
            if (fireChangedEvent) OnInstancesChanged?.Invoke();
            return true;
        }
        private bool InstanceFound(AppInstanceInfo instanceInfo, bool fireChangedEvent)
        {
            var instanceId = instanceInfo.InstanceId;
            if (_Instances.ContainsKey(instanceId)) return false;
            var instance = new AppInstance(this, instanceInfo);
            _Instances.Add(instanceId, instance);
#if DEBUG && false
            JS.Log($"Instance found: {instanceId}", instanceInfo, Instances.Select(o => o.Info).ToList());
#endif
            if (!instance.IsLocal)
            {
                if (Locks != null && !string.IsNullOrEmpty(instanceInfo.LockName))
                {
                    // this lock will be granted when the given instance terminates and releases their exclusive lock
                    _ = Locks.Request(instanceInfo.LockName, new LockRequestOptions { Mode = "shared" }, () =>
                    {
                        InstanceLost(instanceId, true);
                    });
                }
                else
                {
                    // TODO
                    // fallback termination detection
                    // most relatively recent browsers support locks.
                    // if needed, can test with old version of Safari on macOS as they do not support locks
                }
            }
            if (instance.Info.OwnerId == InstanceId && !string.IsNullOrEmpty(instance.Info.ChildId))
            {
                // this instance created 'instance'
                if (OpenWindowWaiters.TryGetValue(instance.Info.ChildId, out var openWindowWaiters))
                {
                    OpenWindowWaiters.Remove(instance.Info.ChildId);
                    openWindowWaiters(instance);
                }
            }
            OnInstanceFound?.Invoke(instance);
            if (fireChangedEvent) OnInstancesChanged?.Invoke();
            return true;
        }

        Dictionary<string, Action<AppInstance>> OpenWindowWaiters = new Dictionary<string, Action<AppInstance>>();


        /// <summary>
        /// Returns information about running instances using locks to obtain it
        /// </summary>
        /// <returns></returns>
        private async Task UpdateInstancesViaLocks()
        {
            // client id is used here as it will be available due to also using LockManager
            if (Locks != null)
            {
                var state = await Locks.Query();
                var previousClientIds = _Instances.Values.Select(o => o.Info.ClientId!).ToList();
                var currentClientIds = state.Held.Select(o => o.ClientId).ToList();
                var instancesLost = _Instances.Values.Where(o => !currentClientIds.Contains(o.Info.ClientId!)).ToList();
                var instancesFound = state.Held.Where(o => !previousClientIds.Contains(o.ClientId) && o.Name.StartsWith(IdentPrefix)).Select(o =>
                {
                    try
                    {
                        var ret = JsonSerializer.Deserialize<AppInstanceInfo>(o.Name.Substring(IdentPrefix.Length))!;
                        ret.ClientId = o.ClientId;
                        ret.LockName = o.Name;
                        return ret;
                    }
                    catch
                    {
                        return null;
                    }
                }).Where(o => o != null).ToList();
                // process instances found
                foreach (var instance in instancesFound)
                {
                    InstanceFound(instance!, false);
                }
                // process instances lost
                foreach (var instance in instancesLost)
                {
                    InstanceLost(instance.Info.InstanceId, false);
                }
                // fire changed event if instances were lost or found
                if (instancesFound.Count > 0 || instancesLost.Count > 0)
                {
                    OnInstancesChanged?.Invoke();
                }
            }
        }
        private static readonly string IdentPrefix = $"{nameof(AppInstanceInfo)}:";
        private string GetName()
        {
            if (JS.WindowThis != null) return JS.WindowThis.Name ?? "";
            if (JS.DedicateWorkerThis != null) return JS.DedicateWorkerThis.Name ?? "";
            if (JS.SharedWorkerThis != null) return JS.SharedWorkerThis.Name ?? "";
            return "";
        }
        /// <summary>
        /// Called by BlazorJSRuntime at startup
        /// </summary>
        /// <returns></returns>
        private async Task InitAsync()
        {
            if (BeenInit) return;
            BeenInit = true;
            if (JS.IsBrowser)
            {
                if (Locks != null)
                {
                    Info.ClientId = await Locks.GetClientId();
                }
                if (JS.GlobalThis is Window window)
                {
                    WindowTask = Local;
                    switch (ServiceWorkerConfig.Register)
                    {
                        case ServiceWorkerStartupRegistration.Register:
                            await RegisterServiceWorker();
                            break;
                        case ServiceWorkerStartupRegistration.Unregister:
                            await UnregisterServiceWorker();
                            break;
                    }
                    if (!string.IsNullOrEmpty(Info.OwnerId) && !string.IsNullOrEmpty(Info.ChildId))
                    {
                        // this window is owned by another instance
                        // todo I guess nothing really, it knows when this winow is started and is ready
                        // could load Info.Url if it is set
                    }
                }
                else if (JS.GlobalThis is DedicatedWorkerGlobalScope workerGlobalScope)
                {
                    DedicatedWorkerParent = new ServiceCallDispatcher(WebAssemblyServices, workerGlobalScope);
                    DedicatedWorkerParent.SendReadyFlag();
                    Async.Run(async () =>
                    {
                        await DedicatedWorkerParent.WhenReady;
                        var isParentAWindow = DedicatedWorkerParent.RemoteInfo!.GlobalThisTypeName == "Window";
                        if (isParentAWindow)
                        {
                            WindowTask = DedicatedWorkerParent;
                        }
                        OnDedicatedWorkerParentReady?.Invoke();
                        Info.ParentInstanceId = DedicatedWorkerParent.RemoteInfo.InstanceId;
                        await RegisterInstance();
                    });
                    return;
                }
                else if (JS.GlobalThis is SharedWorkerGlobalScope sharedWorkerGlobalScope)
                {
                    var missedConnections = JS.Call<MessagePort[]>("takeOverOnConnectEvent", OnSharedWorkerConnectCallback = Callback.Create<MessageEvent>(OnSharedWorkerConnect));
                    if (missedConnections != null)
                    {
                        foreach (var m in missedConnections)
                        {
                            AddIncomingPort(m);
                        }
                    }
                    var tmpName = sharedWorkerGlobalScope.Name;
                    if (!string.IsNullOrEmpty(tmpName))
                    {
                        ThisSharedWorkerName = tmpName;
                    }
                }
                else if (JS.GlobalThis is ServiceWorkerGlobalScope serviceWorkerGlobalScope)
                {
                    //Console.WriteLine($"WebWorkerService: ServiceWorkerGlobalScope");
                    // 
                }
                else
                {
                    //Console.WriteLine($"WebWorkerService: UNKNOWN");
                }
                await RegisterInstance();
            }
        }

        /// <summary>
        /// Returns true if this instance has notified other instances
        /// </summary>
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
        private async Task RegisterInstance()
        {
            if (InstanceRegistered || InstanceRegistering) return;
            InstanceRegistering = true;
            if (Locks != null)
            {
                // hold a lock with this instance's instanceId to allow instance termination detection
                var lockName = IdentPrefix + JsonSerializer.Serialize(Info);
                InstanceLock = await Locks.RequestHandle(lockName);
                Info.LockName = lockName;
            }
            InstanceFound(Info, true);
            BroadcastCall("register");
            InstanceRegistering = false;
            InstanceRegistered = true;
            await UpdateInstancesViaLocks();
        }
        /// <summary>
        /// Registers scriptURL as the service worker.<br/>
        /// The service worker script should import "spawndev.blazorjs.webworkers.js" to function as expected<br/>
        /// </summary>
        /// <param name="scriptURL"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Task RegisterServiceWorker(string scriptURL, ServiceWorkerRegistrationOptions? options = null)
        {
            ServiceWorkerConfig.ScriptURL = scriptURL;
            ServiceWorkerConfig.Options = options;
            return RegisterServiceWorker();
        }
        /// <summary>
        /// Registers ServiceWorkerConfig.ScriptURL as the service worker.<br/>
        /// ServiceWorkerConfig.ScriptURL, by default, is "spawndev.blazorjs.webworkers.js"<br/>
        /// </summary>
        /// <returns></returns>
        public async Task RegisterServiceWorker()
        {
            if (JS.WindowThis != null)
            {
                var workerMode = ServiceWorkerConfig.Options?.Type;
                if (string.IsNullOrEmpty(ServiceWorkerConfig.ScriptURL))
                {
                    ServiceWorkerConfig.ScriptURL = workerMode == "module" ? WebWorkerModuleJSScript : WebWorkerJSScript;
                }
                var kvps = new Dictionary<string, string>();
                if (ServiceWorkerConfig.ImportServiceWorkerAssets)
                {
                    if (!string.IsNullOrEmpty(ServiceWorkerConfig.ServiceWorkerAssetsManifest))
                    {
                        kvps.Add("importServiceWorkerAssets", ServiceWorkerConfig.ServiceWorkerAssetsManifest);
                    }
                    else
                    {
                        kvps.Add("importServiceWorkerAssets", "1");
                    }
                }
                var workerUrl = ServiceWorkerConfig.ScriptURL;
                workerUrl = new Uri(new Uri(AppBaseUri), workerUrl).ToString();
                var queryStr = string.Join("&", kvps.Select(kvp => $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));
                if (workerUrl.Contains("?"))
                {
                    workerUrl += $"&{queryStr}";
                }
                else
                {
                    workerUrl += $"?{queryStr}";
                }
                using var navigator = JS.Get<Navigator>("navigator");
                using var serviceWorkerContainer = navigator.ServiceWorker;
                var serviceWorkerRegistration = await serviceWorkerContainer.Register(workerUrl, ServiceWorkerConfig.Options);
                ServiceWorkerChanged(serviceWorkerRegistration);
            }
        }
        void ServiceWorkerChanged(ServiceWorkerRegistration serviceWorkerRegistration)
        {
            if (serviceWorkerRegistration == null) return;
            if (ActiveServiceWorkerRegistration != null)
            {
                ActiveServiceWorkerRegistration.OnUpdateFound -= ServiceWorker_OnUpdateFound;
            }
            ActiveServiceWorkerRegistration = serviceWorkerRegistration;
            ActiveServiceWorkerRegistration.OnUpdateFound += ServiceWorker_OnUpdateFound;
            OnServiceWorkerRegistered?.Invoke(ActiveServiceWorkerRegistration);
        }
        /// <summary>
        /// Returns the active ServiceWorkerRegistration if one has been registered using the WebWorkerService RegisterServiceWorker method.
        /// </summary>
        public ServiceWorkerRegistration? ActiveServiceWorkerRegistration { get; private set; }
        void ServiceWorker_OnUpdateFound()
        {
            OnServiceWorkerUpdateFound?.Invoke();
        }
        /// <summary>
        /// Unregisters a registered service worker
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UnregisterServiceWorker()
        {
            if (JS.WindowThis != null)
            {
                using var navigator = JS.WindowThis.Navigator;
                using var serviceWorker = navigator.ServiceWorker;
                using var registration = await serviceWorker.GetRegistration();
                if (registration != null)
                {
                    return await registration.Unregister();
                }
            }
            return false;
        }
        /// <summary>
        /// Creates a new a WebWorker instance and returns it when it when it is ready for use.
        /// </summary>
        /// <returns></returns>
        public async Task<WebWorker?> GetWebWorker()
        {
            if (!WebWorkerSupported) return null;
            var queryParams = new Dictionary<string, string>();
#if DEBUG
            queryParams["forceCompatMode"] = "0";
#endif
            if (!string.IsNullOrEmpty(WorkerIndexHtml))
            {
                queryParams["indexHtml"] = WorkerIndexHtml;
            }
            var scriptUrl = WebWorkerJSScript;
            if (queryParams.Count > 0)
            {
                scriptUrl += "?" + string.Join('&', queryParams.Select(o => $"{o.Key}={o.Value}"));
            }
            var worker = new Worker(scriptUrl);
            var webWorker = new WebWorker(worker, WebAssemblyServices);
            await webWorker.WhenReady;
            return webWorker;
        }
        /// <summary>
        /// Creates a new a WebWorker instance and returns.<br/>
        /// The property WhenReady will complete when Blazor is loaded and ready in the worker.<br/>
        /// Use Expressions, interface proxies, etc. to make calls into the worker.
        /// </summary>
        /// <returns>WebWorker or null if not supported.</returns>
        public WebWorker? GetWebWorkerSync(Dictionary<string, string>? queryParams = null)
        {
            if (!WebWorkerSupported) return null;
            queryParams ??= new Dictionary<string, string>();
#if DEBUG
            queryParams["forceCompatMode"] = "0";
#endif
            if (!string.IsNullOrEmpty(WorkerIndexHtml))
            {
                queryParams["indexHtml"] = WorkerIndexHtml;
            }
            var scriptUrl = WebWorkerJSScript;
            if (queryParams.Count > 0)
            {
                scriptUrl += "?" + string.Join('&', queryParams.Select(o => $"{o.Key}={o.Value}"));
            }
            var worker = new Worker(scriptUrl);
            var webWorker = new WebWorker(worker, WebAssemblyServices);
            return webWorker;
        }

        /// <summary>
        /// Creates a new a WebWorker instance and returns it when it is ready.<br/>
        /// Use Expressions, interface proxies, etc. to make calls into the worker.
        /// </summary>
        /// <returns>WebWorker or null if not supported.</returns>
        public async Task<WebWorker?> GetWebWorker(WebWorkerOptions webWorkerOptions)
        {
            var webWorker = GetWebWorkerSync(webWorkerOptions);
            if (webWorker == null) return null;
            await webWorker.WhenReady;
            return webWorker;
        }

        /// <summary>
        /// Creates a new a WebWorker instance and returns.<br/>
        /// The property WhenReady will complete when Blazor is loaded and ready in the worker.<br/>
        /// Use Expressions, interface proxies, etc. to make calls into the worker.
        /// </summary>
        /// <returns>WebWorker or null if not supported.</returns>
        public WebWorker? GetWebWorkerSync(WebWorkerOptions webWorkerOptions)
        {
            if (!WebWorkerSupported) return null;
            webWorkerOptions ??= new WebWorkerOptions();
            webWorkerOptions.WorkerOptions ??= new WorkerOptions();
            webWorkerOptions.QueryParams ??= new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(WorkerIndexHtml))
            {
                webWorkerOptions.QueryParams["indexHtml"] = WorkerIndexHtml;
            }
            if (string.IsNullOrEmpty(webWorkerOptions.ScriptUrl))
            {
                webWorkerOptions.ScriptUrl = webWorkerOptions.WorkerOptions.Type == "module" ? WebWorkerModuleJSScript : WebWorkerJSScript;
            }
            if (webWorkerOptions.QueryParams.Count > 0)
            {
                webWorkerOptions.ScriptUrl += "?" + string.Join('&', webWorkerOptions.QueryParams.Select(o => $"{o.Key}={o.Value}"));
            }
            var worker = new Worker(webWorkerOptions.ScriptUrl, webWorkerOptions.WorkerOptions);
            var webWorker = new WebWorker(worker, WebAssemblyServices);
            return webWorker;
        }
        /// <summary>
        /// Creates a new a SharedWebWorker instance and returns the SharedWebWorekr when it is ready.<br/>
        /// Use Expressions, interface proxies, etc. to make calls into the worker.
        /// </summary>
        /// <returns>SharedWebWorker or null if not supported.</returns>
        public async Task<SharedWebWorker?> GetSharedWebWorker(SharedWebWorkerOptions webWorkerOptions)
        {
            var webWorker = GetSharedWebWorkerSync(webWorkerOptions);
            if (webWorker == null) return null;
            await webWorker.WhenReady;
            return webWorker;
        }
        /// <summary>
        /// Creates a new a SharedWebWorker instance and returns.<br/>
        /// The property WhenReady will complete when Blazor is loaded and ready in the worker.<br/>
        /// Use Expressions, interface proxies, etc. to make calls into the worker.
        /// </summary>
        /// <returns>SharedWebWorker or null if not supported.</returns>
        public SharedWebWorker? GetSharedWebWorkerSync(SharedWebWorkerOptions webWorkerOptions)
        {
            if (!WebWorkerSupported) return null;
            webWorkerOptions ??= new SharedWebWorkerOptions();
            webWorkerOptions.WorkerOptions ??= new SharedWorkerOptions();
            webWorkerOptions.QueryParams ??= new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(WorkerIndexHtml))
            {
                webWorkerOptions.QueryParams["indexHtml"] = WorkerIndexHtml;
            }
            if (string.IsNullOrEmpty(webWorkerOptions.ScriptUrl))
            {
                webWorkerOptions.ScriptUrl = webWorkerOptions.WorkerOptions.Type == "module" ? WebWorkerModuleJSScript : WebWorkerJSScript;
            }
            if (webWorkerOptions.QueryParams.Count > 0)
            {
                webWorkerOptions.ScriptUrl += "?" + string.Join('&', webWorkerOptions.QueryParams.Select(o => $"{o.Key}={o.Value}"));
            }
            webWorkerOptions.WorkerOptions.Name ??= "";
            var worker = new SharedWorker(webWorkerOptions.ScriptUrl, webWorkerOptions.WorkerOptions);
            var webWorker = new SharedWebWorker(webWorkerOptions.WorkerOptions.Name, worker, WebAssemblyServices);
            return webWorker;
        }

        /// <summary>
        /// Returns a new SharedWebWorker instance. If a SharedWorker already existed by this name SharedWebWorker will be connected to that instance.<br/>        /// 
        /// </summary>
        /// <param name="sharedWorkerName">SharedWebWorkers are identified by name. 1 shared worker will be created per name.</param>
        /// <returns></returns>
        public async Task<SharedWebWorker?> GetSharedWebWorker(string sharedWorkerName = "")
        {
            if (!SharedWebWorkerSupported) return null;
            var queryParams = new Dictionary<string, string>();
#if DEBUG
            queryParams["forceCompatMode"] = "0";
#endif
            if (!string.IsNullOrEmpty(WorkerIndexHtml))
            {
                queryParams["indexHtml"] = WorkerIndexHtml;
            }
            var scriptUrl = WebWorkerJSScript;
            if (queryParams.Count > 0)
            {
                scriptUrl += "?" + string.Join('&', queryParams.Select(o => $"{o.Key}={o.Value}"));
            }
            var sharedWorker = new SharedWorker(scriptUrl, sharedWorkerName);
            var sharedWebWorker = new SharedWebWorker(sharedWorkerName, sharedWorker, WebAssemblyServices);
            await sharedWebWorker.WhenReady;
            return sharedWebWorker;
        }
        /// <summary>
        /// Returns a new SharedWebWorker instance. If a SharedWorker already existed by this name SharedWebWorker will be connected to that instance.<br/>
        /// The property WhenReady will complete when Blazor is loaded and ready in the worker
        /// </summary>
        /// <param name="sharedWorkerName"></param>
        /// <returns></returns>
        public SharedWebWorker? GetSharedWebWorkerSync(string sharedWorkerName = "")
        {
            if (!SharedWebWorkerSupported) return null;
            var queryParams = new Dictionary<string, string>();
#if DEBUG
            queryParams["forceCompatMode"] = "0";
#endif
            if (!string.IsNullOrEmpty(WorkerIndexHtml))
            {
                queryParams["indexHtml"] = WorkerIndexHtml;
            }
            var scriptUrl = WebWorkerJSScript;
            if (queryParams.Count > 0)
            {
                scriptUrl += "?" + string.Join('&', queryParams.Select(o => $"{o.Key}={o.Value}"));
            }
            var sharedWorker = new SharedWorker(scriptUrl, sharedWorkerName);
            var sharedWebWorker = new SharedWebWorker(sharedWorkerName, sharedWorker, WebAssemblyServices);
            return sharedWebWorker;
        }
        /// <summary>
        /// Disposes all disposable resources used by this object
        /// </summary>
        public void Dispose()
        {
            OnSharedWorkerConnectCallback?.Dispose();
        }
        private void OnSharedWorkerConnect(MessageEvent e)
        {
            e.Ports.ToList().ForEach(AddIncomingPort);
        }
        private void AddIncomingPort(MessagePort incomingPort)
        {
            var incomingHandler = new ServiceCallDispatcher(WebAssemblyServices, incomingPort);
            incomingPort.Start();
            SharedWorkerIncomingConnections.Add(incomingHandler);
            incomingHandler.SendReadyFlag();
        }
        /// <summary>
        /// Tries to open a new window instance with the given url (must be a path in this app)<br/>
        /// </summary>
        /// <param name="path">Optional relative path to your app's base path.</param>
        /// <param name="target">
        /// A string, without whitespace, specifying the name of the browsing context the resource is being loaded into. If the name doesn't identify an existing context, a new context is created and given the specified name. The special target keywords, _self, _blank (default), _parent, _top, and _unfencedTop can also be used. _unfencedTop is only relevant to fenced frames.<br/>
        /// This parameter is ignored if not running in a window scope
        /// </param>
        /// <param name="windowFeatures">
        /// A string containing a comma-separated list of window features in the form name=value. Boolean values can be set to true using one of: name, name=yes, name=true, or name=n where n is any non-zero integer. These features include options such as the window's default size and position, whether or not to open a minimal popup window, and so forth.<br/>
        /// This parameter is ignored if not running in a window scope
        /// </param>
        /// <param name="cancellationToken">
        /// Can be used to cancel waiting for the window opening<br/>
        /// The default timeout is 20 seconds
        /// </param>
        /// <returns></returns>
        public async Task<AppInstance?> OpenWindow(string? path = null, string? target = null, string? windowFeatures = null, CancellationToken cancellationToken = default)
        {
            AppInstance? ret = null;
            if (!JS.IsWindow && !JS.IsServiceWorkerGlobalScope)
            {
                // only works in window and service work scopes
                return ret;
            }
            var childId = Guid.NewGuid().ToString();
            var queryParams = new Dictionary<string, string>();
            queryParams[instanceOwnerIdKey] = InstanceId;
            queryParams[childIdKey] = childId;
            var pathUrl = new Uri(new Uri(AppBaseUri), path ?? "");
            var newWindowUrl = pathUrl.ToString(); ;
            newWindowUrl += (newWindowUrl.Contains("?") ? "&" : "?") + string.Join('&', queryParams.Select(o => $"{Uri.EscapeDataString(o.Key)}={Uri.EscapeDataString(o.Value)}"));
            if (JS.WindowThis != null)
            {
                // running in a window
                // use window.open
                using var window = JS.WindowThis!.Open(newWindowUrl, target!, windowFeatures!);
            }
            else if (JS.ServiceWorkerThis != null)
            {
                // running in a service worker
                // use client.openWindow
                // this only works in specific circumstances 
                // typically used inside a notifiction click event in a service worker
                using var window = await JS.ServiceWorkerThis!.Clients.OpenWindow(newWindowUrl);
            }
            var tcs = new TaskCompletionSource<AppInstance?>();
            var task = tcs.Task;
            // set a deault timeout if one wasn't set
            CancellationTokenSource? cts = null;
            if (cancellationToken == default)
            {
                cts = new CancellationTokenSource(20000);
                cancellationToken = cts.Token;
            }
            cancellationToken.Register(() =>
            {
                if (task.IsCompleted) return;
                if (OpenWindowWaiters.ContainsKey(childId))
                {
                    OpenWindowWaiters.Remove(childId);
                }
                tcs.TrySetException(new Exception("Timedout"));
            });
            var newInstanceHandler = new Action<AppInstance>((appInstance) =>
            {
                if (task.IsCompleted) return;
                if (OpenWindowWaiters.ContainsKey(childId))
                {
                    OpenWindowWaiters.Remove(childId);
                }
                tcs.TrySetResult(appInstance);
            });
            OpenWindowWaiters.Add(childId, newInstanceHandler);
            try
            {
                ret = await task;
            }
            finally
            {
                cts?.Dispose();
            }
            return ret;
        }
    }
}
