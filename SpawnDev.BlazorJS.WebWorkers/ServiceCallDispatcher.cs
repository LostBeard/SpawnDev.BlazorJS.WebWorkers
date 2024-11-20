using Microsoft.Extensions.DependencyInjection;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.JSObjects.WebRTC;
using System.Reflection;
using Array = SpawnDev.BlazorJS.JSObjects.Array;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Allows calling into and receiving calls from another instance of this app (same origin) in any scope<br/>
    /// - Window<br/>
    /// - DedicatedWorkerGlobalScope<br/>
    /// - SharedWorkerGlobalScope<br/>
    /// - ServiceWorkerGlobalScope
    /// </summary>
    public class ServiceCallDispatcher : AsyncCallDispatcher, IDisposable
    {
        class CallSideParameter
        {
            public string Name { get; }
            public Type Type { get; }
            public Func<object?> GetValue;
            public CallSideParameter(string name, Func<object?> getter, Type type)
            {
                Name = name;
                GetValue = getter;
                Type = type;
            }
        }
        class CallbackAction
        {
            public Delegate Target { get; set; }
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string RequestId { get; set; } = "";
            public Type[] ParameterTypes { get; set; }
        }
        static List<Type> _transferableTypes { get; } = new List<Type> {
            typeof(ArrayBuffer),
            typeof(AudioData),
            typeof(ImageBitmap),
            typeof(MediaSourceHandle),
            typeof(MessagePort),
            typeof(MIDIAccess),
            typeof(OffscreenCanvas),
            typeof(ReadableStream),
            typeof(RTCDataChannel),
            typeof(TransformStream),
            typeof(VideoFrame),
            typeof(WebTransportReceiveStream),
            typeof(WebTransportSendStream),
            typeof(WritableStream),
        };
        /// <summary>
        /// Returns true if the type is Transferable
        /// </summary>
        public static bool IsTransferable(Type type) => _transferableTypes.Contains(type);
        /// <summary>
        /// Returns true if the type is Transferable
        /// </summary>
        public static bool IsTransferable<T>() => IsTransferable(typeof(T));
        /// <summary>
        /// Returns true if the object type is Transferable
        /// </summary>
        public static bool IsTransferable<T>(T obj) => IsTransferable(typeof(T));
        protected IMessagePort? _port { get; set; }
        protected IMessagePortSimple? _portSimple { get; set; }
        /// <summary>
        /// Returns true if the target of this dispatcher is this instance
        /// </summary>
        public bool LocalInvoker { get; private set; }
        /// <summary>
        /// Information about the remote instance
        /// </summary>
        public ServiceCallDispatcherInfo? RemoteInfo { get; private set; } = null;
        /// <summary>
        /// Information about this instance
        /// </summary>
        public ServiceCallDispatcherInfo LocalInfo { get; }
        /// <summary>
        /// Returns true if there is at least 1 request waiting for a result
        /// </summary>
        public bool WaitingForResponse => _waiting.Count > 0;
        Dictionary<string, TaskCompletionSource<Array>> _waiting = new Dictionary<string, TaskCompletionSource<Array>>();
        protected IServiceProvider ServiceProvider;
        protected IServiceCollection ServiceDescriptors;
        //IServiceScope? ConnectionScope;
        /// <summary>
        /// Returns true if a message port is used and it supports transferable objects
        /// </summary>
        public bool MessagePortSupportsTransferable { get; private set; }
        public ServiceCallDispatcher(IWebAssemblyServices webAssemblyServices, IMessagePortSimple port)
        {
            WebAssmeblyServices = webAssemblyServices;
            ServiceProvider = webAssemblyServices.Services;
            ServiceDescriptors = webAssemblyServices.Descriptors;
            if (port is IMessagePort messagePort)
            {
                _port = messagePort;
                MessagePortSupportsTransferable = true;
            }
            _portSimple = port;
            _portSimple.OnMessage += _worker_OnMessage;
            _portSimple.OnMessageError += _port_OnError;
            additionalCallArgs.Add(new CallSideParameter("caller", () => this, typeof(ServiceCallDispatcher)));
            LocalInfo = new ServiceCallDispatcherInfo { InstanceId = JS.InstanceId, GlobalThisTypeName = JS.GlobalThisTypeName };
        }
        IWebAssemblyServices WebAssmeblyServices;
        public ServiceCallDispatcher(IWebAssemblyServices webAssemblyServices)
        {
            LocalInvoker = true;
            WebAssmeblyServices = webAssemblyServices;
            ServiceProvider = webAssemblyServices.Services;
            ServiceDescriptors = webAssemblyServices.Descriptors;
            additionalCallArgs.Add(new CallSideParameter("caller", () => this, typeof(ServiceCallDispatcher)));
            LocalInfo = new ServiceCallDispatcherInfo { InstanceId = JS.InstanceId, GlobalThisTypeName = JS.GlobalThisTypeName };
            RemoteInfo = LocalInfo;
            _oninit.SetResult(0);
        }
        protected static BlazorJSRuntime JS => BlazorJSRuntime.JS;
        private void _port_OnError()
        {
            JS.Log("_port_OnError");
        }
        private TaskCompletionSource<int> _oninit = new TaskCompletionSource<int>();
        /// <summary>
        /// Completes when connection has been established
        /// </summary>
        public Task WhenReady => _oninit.Task;
        private bool ReadyFlagSent = false;
        public void SendReadyFlag()
        {
            if (_portSimple == null) return;
            ReadyFlagSent = true;
            var needsInfo = RemoteInfo == null;
#if DEBUG && false
            JS.Log("SendReadyFlag sent", "init", LocalInfo, needsInfo);
#endif
            _portSimple.PostMessage(new object?[] { "init", LocalInfo, needsInfo });
        }
        public event Action<ServiceCallDispatcher, Array> OnMessage;
        public delegate void BusyStateChangedDelegate(ServiceCallDispatcher sender, bool busy);
        public event BusyStateChangedDelegate OnBusyStateChanged;
        private bool _busy = false;
        private void CheckBusyStateChanged(bool fire = false)
        {
            if (_busy && _waiting.Count == 0)
            {
                _busy = false;
                OnBusyStateChanged?.Invoke(this, _busy);
            }
            else if (!_busy && _waiting.Count > 0)
            {
                _busy = true;
                OnBusyStateChanged?.Invoke(this, _busy);
            }
            else if (fire)
            {
                OnBusyStateChanged?.Invoke(this, _busy);
            }
        }
        protected async void _worker_OnMessage(MessageEvent e)
        {
#if DEBUG && false
            JS.Log("_worker_OnMessage", e);
#endif
            try
            {
                var args = e.GetData<Array>();
                e.Dispose();
                var argsLength = args.Length;
                var msgType = args.Shift<string>(); // 0
                switch (msgType)
                {
                    case "init":
                        {
                            var remoteInfo = args.Shift<ServiceCallDispatcherInfo>(); // 1
                            var needsInfo = args.Shift<bool>(); // 2
                            if (RemoteInfo == null)
                            {
                                RemoteInfo = remoteInfo;
                                if (RemoteInfo != null)
                                {
                                    _oninit.TrySetResult(0);
                                    CheckBusyStateChanged(true);
                                }
                            }
                            if (needsInfo) SendReadyFlag();
                        }
                        break;
                    case "cancelToken":
                        var tokenId = args.Shift<string>(); // 1
                        if (RemoteCancellationTokens.TryGetValue(tokenId, out var remoteTokenSource))
                        {
                            RemoteCancellationTokens.Remove(tokenId);
                            remoteTokenSource.TokenSource.Cancel();
                            remoteTokenSource.Dispose();
                        }
                        break;
                    case "action":
                        {
                            var requestId = args.Shift<string>(); // 1
                            if (_waiting.TryGetValue(requestId, out var req))
                            {
                                var actionId = args.Shift<string>(); // 2
                                if (_actionHandles.TryGetValue(actionId, out var actionHandle))
                                {
                                    var actionArgs = new object?[actionHandle.ParameterTypes.Length];
                                    if (actionArgs.Length > 0)
                                    {
                                        argsLength = args.Length;
                                        if (actionArgs.Length != argsLength)
                                        {
                                            throw new Exception("Invalid argument count on Action callback");
                                        }
                                        for (var n = 0; n < actionArgs.Length; n++)
                                        {
                                            actionArgs[n] = args.GetItem(actionHandle.ParameterTypes[n], n);
                                        }
                                    }
                                    actionHandle.Target.DynamicInvoke(actionArgs);
                                }
                            }
                        }
                        break;
                    case "event":
                        {
                            OnMessage?.Invoke(this, args);
                        }
                        break;
                    case "callback":
                        {
                            var requestId = args.Shift<string>(); // 1
                            if (_waiting.TryGetValue(requestId, out var req))
                            {
                                _waiting.Remove(requestId);
                                if (!req.Task.IsCompleted && req.TrySetResult(args)) return;
                                CheckBusyStateChanged();
                            }
                        }
                        break;
                    case "call":
                        {
                            await HandleCallMessage(args, false);
                        }
                        break;
                    case "msg":
                        {
                            await HandleCallMessage(args, true);
                        }
                        break;
                    case "callKeyed":
                        {
                            var keyTypeName = args.Shift<string>(); // 1
                            var keyType = TypeExtensions.GetType(keyTypeName);
                            var key = args.Shift(keyType!);
                            await HandleCallMessage(args, false, true, key);
                        }
                        break;
                    case "msgKeyed":
                        {
                            var keyTypeName = args.Shift<string>(); // 1
                            var keyType = TypeExtensions.GetType(keyTypeName);
                            var key = args.Shift(keyType!);
                            await HandleCallMessage(args, true, true, key);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                BlazorJSRuntime.JS.Log("ERROR: ", e);
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"ERROR stack trace: {ex.StackTrace}");
            }
        }
        async Task HandleCallMessage(Array args, bool noReply, bool keyed = false, object? key = null)
        {
            string requestId = "";
            object? retValue = null;
            string? err = null;
            MethodInfo? methodInfo = null;
            try
            {
                requestId = args.Shift<string>(); // 1
                var serializedMethodInfo = args.Shift<string>(); // 2
                object?[]? callArgs0 = null;
                methodInfo = SerializableMethodInfo.DeserializeMethodInfo(serializedMethodInfo);
                if (methodInfo == null)
                {
                    throw new Exception($"Method signature not found.");
                }
                else if (methodInfo.ReflectedType == null)
                {
                    throw new Exception($"Invalid method signature not found. Invalid ReflectedType.");
                }
                var serviceType = methodInfo.ReflectedType;
                object? service = null;
                if (!methodInfo.IsStatic)
                {
                    // non-static methods calls must point at a registered service
                    if (keyed)
                    {
                        service = await FindServiceAsync(serviceType, key!);
                    }
                    else
                    {
                        service = await FindServiceAsync(serviceType);
                    }
                    if (service == null)
                    {
                        throw new Exception($"Service type not found: {(serviceType == null ? "" : serviceType.Name)}");
                    }
                }
                callArgs0 = await PostDeserializeArgs(requestId, methodInfo, args);
                retValue = await methodInfo.InvokeAsync(service, callArgs0!);
            }
            catch (Exception ex)
            {
                // the call failed
                err = ex.Message;
#if DEBUG
                Console.WriteLine($"Execution of remote call failed: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace ?? ""}");
#endif
            }
            // Post call cleanup
            // remove any uncancelled remote CancellationTokens for this request
            var tokensToRemove = RemoteCancellationTokens.Values.Where(o => o.RequestId == requestId).ToArray();
            foreach (var remoteTokenSource in tokensToRemove)
            {
                RemoteCancellationTokens.Remove(remoteTokenSource.TokenId);
                remoteTokenSource.Dispose();
            }
            try
            {
                if (!string.IsNullOrEmpty(requestId) && !noReply)
                {

                    // Send notification of completion because there is a requestId
                    object[] transfer = System.Array.Empty<object>();
                    if (retValue != null)
                    {
                        if (retValue is byte[] bytes)
                        {
                            // same as when sending a byte[] as an arg... change it to a Uint8Array reference so it is not serialized using Base64 and we can transfer its array buffer
                            var uint8Array = new Uint8Array(bytes);
                            retValue = uint8Array;
                            transfer = new object[] { uint8Array.Buffer };
                        }
                        else if (methodInfo != null)
                        {
                            var finalReturnType = methodInfo.GetFinalReturnType();
                            var conversionInfo = TypeConversionInfo.GetTypeConversionInfo(finalReturnType);
                            transfer = conversionInfo.GetTransferablePropertyValues(retValue);
                        }
                    }
                    var callbackMsg = new object?[] { "callback", requestId, err, retValue };
                    if (_port != null) _port?.PostMessage(callbackMsg, transfer);
                    else _portSimple?.PostMessage(callbackMsg);
                }
            }
            catch (Exception ex)
            {
                // failed to notify remote endpoint of result
                Console.WriteLine($"Failed to notify remote endpoint of result: {ex.Message}");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="data"></param>
        public void SendEvent(string eventName, object?[]? data = null)
        {
            var outMsg = new List<object?> { "event", eventName };
            if (data != null) outMsg.AddRange(data);
            _portSimple?.PostMessage(outMsg);
        }
        private async Task<object?> LocalCall(MethodInfo methodInfo, object?[]? args = null)
        {
            if (methodInfo == null)
            {
                throw new NullReferenceException(nameof(methodInfo));
            }
            object? service = null;
            if (!methodInfo.IsStatic)
            {
                // non-static methods calls must point at a registered service
                service = await FindServiceAsync(methodInfo.ReflectedType!);
                if (service == null)
                {
                    throw new NullReferenceException(nameof(service));
                }
            }
            return await methodInfo.InvokeAsync(service, args);
        }
        private async Task<object?> LocalCall(object key, MethodInfo methodInfo, object?[]? args = null)
        {
            if (methodInfo == null)
            {
                throw new NullReferenceException(nameof(methodInfo));
            }
            object? service = null;
            if (!methodInfo.IsStatic)
            {
                // non-static methods calls must point at a registered service
                service = await FindServiceAsync(methodInfo.ReflectedType!, key);
                if (service == null)
                {
                    throw new NullReferenceException(nameof(service));
                }
            }
            return await methodInfo.InvokeAsync(service, args);
        }
        /// <summary>
        /// Calls the MethodInfo on remote context
        /// </summary>
        /// <param name="key"></param>
        /// <param name="methodInfo"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public override async Task<object?> CallKeyed(object key, MethodInfo methodInfo, object?[]? args = null)
        {
#if DEBUG && false
            JS.Log($"Call", methodInfo.Name, LocalInvoker);
#endif
            if (LocalInvoker)
            {
                return await LocalCall(key, methodInfo, args);
            }
            await WhenReady;
            var serviceType = methodInfo.ReflectedType;
            if (_portSimple == null)
            {
                throw new Exception("ServiceCallDispatcher.Call: port is null.");
            }
            if (serviceType == null)
            {
                throw new Exception("ServiceCallDispatcher.Call: method does not have a reflected type.");
            }
            var originCallableAttribute = methodInfo!.GetCustomAttribute<OriginCallableAttribute>(true);
            var markedNoReply = originCallableAttribute?.NoReply ?? false;
            var requestId = Guid.NewGuid().ToString();
            var targetMethod = SerializableMethodInfo.SerializeMethodInfo(methodInfo);
            var msgData = PreSerializeArgs(requestId, methodInfo, args, out var transferable);
            var keyTypeName = TypeExtensions.GetFullName(key!.GetType());
            var msgOut = new List<object?> { markedNoReply ? "msgKeyed" : "callKeyed", keyTypeName, key, requestId, targetMethod };
            msgOut.AddRange(msgData);
            if (_port != null) _port.PostMessage(msgOut, transferable);
            else _portSimple?.PostMessage(msgOut);
            if (markedNoReply)
            {
                CheckBusyStateChanged();
                return null;
            }
            var workerTask = new TaskCompletionSource<Array>();
            _waiting.Add(requestId, workerTask);
            CheckBusyStateChanged();
            try
            {
                var returnArray = await workerTask.Task;
                // remove any request callbacks (currently only Action)
                var keysToRemove = _actionHandles.Values.Where(o => o.RequestId == requestId).Select(o => o.Id).ToArray();
                foreach (var keyToRemove in keysToRemove) _actionHandles.Remove(keyToRemove);
                // get result or exception
                string? err = returnArray.GetItem<string?>(0);
                if (!string.IsNullOrEmpty(err)) throw new Exception(err);
                var finalReturnType = methodInfo.GetFinalReturnType();
#if DEBUG && false
                JS.Log($"Call", methodInfo.Name, "return", finalReturnType.Name);
#endif
                if (finalReturnType.IsVoid())
                {
#if DEBUG && false
                    JS.Log($"Call", methodInfo.Name, "return IsVoid");
#endif
                    return null;
                }
                var ret = returnArray.GetItem(finalReturnType, 1);
#if DEBUG && false
                JS.Log($"Call", methodInfo.Name, "return", finalReturnType.Name, ret);
#endif
                return ret;
            }
            finally
            {
                CheckBusyStateChanged();
            }
        }
        /// <summary>
        /// Calls the MethodInfo on remote context
        /// </summary>
        /// <param name="methodBase"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public override async Task<object?> Call(MethodInfo methodBase, object?[]? args = null)
        {
#if DEBUG && false
            JS.Log($"Call", methodInfo.Name, LocalInvoker);
#endif
            if (LocalInvoker)
            {
                return await LocalCall(methodBase, args);
            }
            await WhenReady;
            var serviceType = methodBase.ReflectedType;
            if (_portSimple == null)
            {
                throw new Exception("ServiceCallDispatcher.Call: port is null.");
            }
            if (serviceType == null)
            {
                throw new Exception("ServiceCallDispatcher.Call: method does not have a reflected type.");
            }
            var originCallableAttribute = methodBase!.GetCustomAttribute<OriginCallableAttribute>(true);
            var markedNoReply = originCallableAttribute?.NoReply ?? false;
            var requestId = Guid.NewGuid().ToString();
            var targetMethod = SerializableMethodInfo.SerializeMethodInfo(methodBase);
            var msgData = PreSerializeArgs(requestId, methodBase, args, out var transferable);
            var msgOut = new List<object?> { markedNoReply ? "msg" : "call", requestId, targetMethod };
            msgOut.AddRange(msgData);
            if (_port != null) _port.PostMessage(msgOut, transferable);
            else _portSimple?.PostMessage(msgOut);
            if (markedNoReply)
            {
                CheckBusyStateChanged();
                return null;
            }
            var workerTask = new TaskCompletionSource<Array>();
            _waiting.Add(requestId, workerTask);
            CheckBusyStateChanged();
            try
            {
                var returnArray = await workerTask.Task;
                // remove any request callbacks (currently only Action)
                var keysToRemove = _actionHandles.Values.Where(o => o.RequestId == requestId).Select(o => o.Id).ToArray();
                foreach (var key in keysToRemove) _actionHandles.Remove(key);
                // get result or exception
                string? err = returnArray.GetItem<string?>(0);
                if (!string.IsNullOrEmpty(err)) throw new Exception(err);
                var finalReturnType = methodBase is MethodInfo methodInfo ? methodInfo.GetFinalReturnType() : typeof(void);
#if DEBUG && false
                JS.Log($"Call", methodInfo.Name, "return", finalReturnType.Name);
#endif
                if (finalReturnType.IsVoid())
                {
#if DEBUG && false
                    JS.Log($"Call", methodInfo.Name, "return IsVoid");
#endif
                    return null;
                }
                var ret = returnArray.GetItem(finalReturnType, 1);
#if DEBUG && false
                JS.Log($"Call", methodInfo.Name, "return", finalReturnType.Name, ret);
#endif
                return ret;
            }
            finally
            {
                CheckBusyStateChanged();
            }
        }
        static List<Type> GenericActions = new List<Type> {
            typeof(Action),
            typeof(Action<>),
            typeof(Action<,>),
            typeof(Action<,,>),
            typeof(Action<,,,>),
            typeof(Action<,,,,>),
            typeof(Action<,,,,,>),
            typeof(Action<,,,,,,>),
            typeof(Action<,,,,,,,>),
            typeof(Action<,,,,,,,,>),
            typeof(Action<,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,>),
        };
        static List<Type> GenericFuncs = new List<Type> {
            typeof(Func<>),
            typeof(Func<,>),
            typeof(Func<,,>),
            typeof(Func<,,,>),
            typeof(Func<,,,,>),
            typeof(Func<,,,,,>),
            typeof(Func<,,,,,,>),
            typeof(Func<,,,,,,,>),
            typeof(Func<,,,,,,,,>),
            typeof(Func<,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,>),
        };
        static bool IsFunc(Type type)
        {
            Type? generic = null;
            if (type.IsGenericTypeDefinition) generic = type;
            else if (type.IsGenericType) generic = type.GetGenericTypeDefinition();
            if (generic == null) return false;
            return GenericFuncs.Contains(generic);
        }
        static bool IsAction(Type type)
        {
            if (type == typeof(Action)) return true;
            Type? generic = null;
            if (type.IsGenericTypeDefinition) generic = type;
            else if (type.IsGenericType) generic = type.GetGenericTypeDefinition();
            if (generic == null) return false;
            return GenericActions.Contains(generic);
        }
        private Dictionary<string, CallbackAction> _actionHandles = new Dictionary<string, CallbackAction>();
        List<CallSideParameter> additionalCallArgs { get; } = new List<CallSideParameter>();
        /// <summary>
        /// Pre-serialization that prepares call arguments before they are sent to the remote endpoint via a postMessage call<br />
        /// Any transferable objects are found in this stage as well and returned in an out param "transferable"
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="methodInfo"></param>
        /// <param name="args"></param>
        /// <param name="transferable"></param>
        /// <returns></returns>
        private object?[] PreSerializeArgs(string requestId, MethodBase methodInfo, object?[]? args, out object[] transferable)
        {
            var transferableList = new List<object>();
            var parameterInfos = methodInfo.GetParameters();
            var transferableListAttributeParameter = parameterInfos.FirstOrDefault(o => typeof(IEnumerable<object>).IsAssignableFrom(o.ParameterType) && Attribute.IsDefined(o, typeof(TransferableListAttribute)));
            var transferableListAttributeFound = transferableListAttributeParameter != null;
            var argsLength = args == null ? 0 : args.Length;
            object?[]? ret = new object?[argsLength];
            for (var i = 0; i < argsLength; i++)
            {
                var arg = args![i];
                if (arg == null) continue;
                var methodParamInfo = parameterInfos[i];
                var methodParamType = methodParamInfo.ParameterType;
                Type? genericType = null;
                if (methodParamType.IsGenericTypeDefinition) genericType = methodParamType;
                else if (methodParamType.IsGenericType) genericType = methodParamType.GetGenericTypeDefinition();
#if DEBUG && false
                var genericTypeStr = genericType == null ? "NULL" : genericType.FullName;
                Console.WriteLine($"genericTypeStr: {genericTypeStr}");
#endif
                var coreType = genericType ?? methodParamType;
                var workerTransferSet = false;
                var isTransferList = Attribute.IsDefined(methodParamInfo, typeof(TransferableListAttribute));
                if (methodParamType.IsClass)
                {
                    var transferAttr = (WorkerTransferAttribute?)methodParamInfo.GetCustomAttribute(typeof(WorkerTransferAttribute), true);
                    if (transferAttr?.Transfer == true)
                    {
                        // this property has been marked as transferable
                        workerTransferSet = true;
                    }
                }
                var methodParamTypeName = methodParamInfo.ParameterType.Name;
                var genericTypes = methodParamType.GenericTypeArguments;
                // check if it is a [TransferableList] object[] parameter
                if (transferableListAttributeParameter == methodParamInfo && arg is IEnumerable<object> objectArray)
                {
                    transferableList.AddRange(objectArray);
                    // the parameter data is not actually passed, the parameter exists to tell the sender what data should be added to the transferables list
                    continue;
                }
                if (IsCallSideParameter(methodParamInfo))
                {
                    // resolved on the other side
                    // skip item ...
                    continue;
                }
                else if (arg is Delegate argDelegate && !string.IsNullOrEmpty(requestId))
                {
                    if (IsAction(coreType))
                    {
                        var cb = new CallbackAction
                        {
                            RequestId = requestId,
                            ParameterTypes = genericTypes,
                            Target = argDelegate,
                        };
                        _actionHandles[cb.Id] = cb;
                        ret[i] = cb.Id;
                    }
                    else if (IsFunc(coreType))
                    {
                        throw new Exception("Func delegate parameters are not currently supported.");
                    }
                    else
                    {
                        throw new Exception("Unsupported delegate parameter type. Only Action delegates can be sent as parameters.");
                    }
                }
                else if (arg is CancellationToken token)
                {
                    if (token.IsCancellationRequested)
                    {
                        // "" represents an already cancelled token
                        ret[i] = "";
                    }
                    else if (token.CanBeCanceled)
                    {
                        // a token id will be sent which can be referenced later to cancel the token
                        // listen for the token cancellation event and send the cancel request at that time
                        var tokenId = Guid.NewGuid().ToString();
                        token.Register(() =>
                        {
                            // send cancel message to worker
                            var callbackMsg = new List<object?> { "cancelToken", tokenId };
                            _portSimple?.PostMessage(callbackMsg);
                        });
                        ret[i] = tokenId;
                    }
                    else
                    {
                        // null represents CancellationToken.None (the default)
                        ret[i] = null;
                    }
                }
                else if (arg is byte[] bytes)
                {
                    // to get better performance when sending byte arrays we convert it to a Uint8Array reference first, and add its array buffer to the transferables list.
                    // it will still be read in on the other side as a byte array. this prevents 1 copying stage.
                    var uint8Array = new Uint8Array(bytes);
                    transferableList.Add(uint8Array.Buffer);
                    ret[i] = uint8Array;
                }
                else
                {
                    if (methodParamType.IsClass)
                    {
                        var transferableAttribute = methodParamType.GetCustomAttribute<TransferableAttribute>(true);
                        if (transferableAttribute != null)
                        {
                            // some transferable types MUST be transferred to be sent to a worker (Ex. OffscreenCanvas)
                            // if 
                            if (workerTransferSet || (transferableAttribute.TransferRequired && !transferableListAttributeFound))
                            {
                                transferableList.Add(arg);
                                ret[i] = arg;
                                continue;
                            }
                        }
                        else if (workerTransferSet && arg is TypedArray typedArray)
                        {
                            var arrayBuffer = typedArray.Buffer;
                            transferableList.Add(arrayBuffer);
                            ret[i] = arg;
                            continue;
                        }
                    }
                    if (workerTransferSet)
                    {
                        var conversionInfo = TypeConversionInfo.GetTypeConversionInfo(methodParamType);
                        var propTransferable = conversionInfo.GetTransferablePropertyValues(arg);
                        transferableList.AddRange(propTransferable);
                    }
                    ret[i] = arg;
                }
            }
            transferable = transferableList.ToArray();
            return ret;
        }
        /// <summary>
        /// Holds references to deserialized CancellationTokens from requests<br/>
        /// These will be disposed and removed when the request is completed<br/>
        /// </summary>
        Dictionary<string, RemoteCancellationTokenSource> RemoteCancellationTokens = new Dictionary<string, RemoteCancellationTokenSource>();
        class RemoteCancellationTokenSource : IDisposable
        {
            public string TokenId { get; private set; }
            public CancellationTokenSource TokenSource { get; private set; } = new CancellationTokenSource();
            public string RequestId { get; private set; }
            public RemoteCancellationTokenSource(string requestId, string tokenId)
            {
                RequestId = requestId;
                TokenId = tokenId;
            }
            public void Dispose()
            {
                TokenSource.Dispose();
            }
        }
        /// <summary>
        /// Imports method call arguments from Javascript and finishes deserializing them<br />
        /// Returns teh exact number of arguments the methodInfo uses, including default values if there was not enough passed in arguments<br />
        /// CallSide arguments will also be resolved if any.
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="methodInfo"></param>
        /// <param name="callArgs"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<object?[]?> PostDeserializeArgs(string requestId, MethodBase methodInfo, Array callArgs)
        {
            if (callArgs == null) return null;
            var callArgsLength = callArgs.Length;
            var methodsParamTypes = methodInfo.GetParameters();
            if (callArgsLength > methodsParamTypes.Count())
            {
                throw new Exception("PostDeserializeArgs argument count mismatch. Too many arguments.");
            }
            var ret = new object?[methodsParamTypes.Length];
            for (var i = 0; i < ret.Length; i++)
            {
                var methodParam = methodsParamTypes[i];
                var methodParamType = methodParam.ParameterType;
                var genericTypes = methodParamType.GenericTypeArguments;
                var (resolved, value) = await TryResolveCallSideParamValue(methodParam);
                if (resolved)
                {
                    ret[i] = value;
                }
                else if (typeof(CancellationToken) == methodParamType && !string.IsNullOrEmpty(requestId))
                {
                    // Create a local action that can be called to relay the call to the remote endpoint
                    var tokenId = callArgs.GetItem<string?>(i);
                    if (tokenId == null)
                    {
                        // default token
                        ret[i] = CancellationToken.None;
                    }
                    else if (tokenId == "")
                    {
                        // already cancelled
                        ret[i] = new CancellationToken(true);
                    }
                    else
                    {
                        // can be cancelled
                        var remoteCancellationToken = new RemoteCancellationTokenSource(requestId, tokenId);
                        RemoteCancellationTokens[tokenId] = remoteCancellationToken;
                        ret[i] = remoteCancellationToken.TokenSource.Token;
                    }
                }
                else if (typeof(Delegate).IsAssignableFrom(methodParamType) && !string.IsNullOrEmpty(requestId))
                {
                    // Create a local action that can be called to relay the call to the remote endpoint
                    var actionId = callArgs.GetItem<string>(i);
                    if (genericTypes.Length == 0)
                    {
                        ret[i] = new Action(() =>
                        {
                            //JS.Log($"Action called: {actionId}");
                            var callbackMsg = new List<object?> { "action", requestId, actionId };
                            _portSimple?.PostMessage(callbackMsg);
                        });
                    }
                    else
                    {
                        ret[i] = CreateTypedAction(genericTypes, new Action<object?[]>((args) =>
                        {
                            //JS.Log($"Action called: {actionId} {o}");
                            var callbackMsg = new List<object?> { "action", requestId, actionId };
                            callbackMsg.AddRange(args);
                            _portSimple?.PostMessage(callbackMsg);
                        }));
                    }
                }
                else if (i < callArgsLength)
                {
                    ret[i] = callArgs.GetItem(methodParamType, i);
                }
                else if (methodParam.HasDefaultValue)
                {
                    ret[i] = methodParam.DefaultValue;
                }
                else
                {
                    throw new Exception("PostDeserializeArgs argument count mismatch. Not enough arguments.");
                }
            }
            return ret;
        }
        class RuntimeService
        {
            public object? ServiceKey { get; set; }
            public bool IsKeyed { get; set; }
            public Type ServiceType { get; set; }
            public Type ImplementationType { get; set; }
            public object? Service { get; set; }
        }
        List<RuntimeService> RuntimeServices = new List<RuntimeService>();
        bool IsCallSideParameter(ParameterInfo methodParam)
        {
            var fromServiceAttr = methodParam.GetCustomAttribute<FromServicesAttribute>();
            if (fromServiceAttr != null) return true;
            if (GetCallSideParameter(methodParam) != null) return true;
            return false;
        }
        async Task<object?> FindServiceAsync(Type serviceType, object key)
        {
            var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && Object.Equals(o.ServiceKey, key));
            if (runtimeServiceInfo != null)
            {
                if (runtimeServiceInfo.Service == null)
                {
                    // try creating with the key as a parameter, if it fails we'll try without it
                    try
                    {
                        runtimeServiceInfo.Service = ActivatorUtilities.CreateInstance(ServiceProvider, runtimeServiceInfo.ImplementationType, key);
                    }
                    catch { }
                    if (runtimeServiceInfo.Service == null)
                    {
                        runtimeServiceInfo.Service = ActivatorUtilities.CreateInstance(ServiceProvider, runtimeServiceInfo.ImplementationType);
                    }
                    if (runtimeServiceInfo.Service is IAsyncService asyncService)
                    {
                        await asyncService.Ready;
                    }
                }
                return runtimeServiceInfo.Service;
            }
#if NET8_0_OR_GREATER
            var ret = await ServiceProvider.FindServiceAsync(serviceType, key);
            return ret;
#else
            return null;
#endif
        }
        private async Task<bool> _AddService(Type serviceType, Type implementationType)
        {
            var service = await FindServiceAsync(serviceType);
            if (service != null) return false;
            RuntimeServices.Add(new RuntimeService
            {
                ServiceType = serviceType,
                ImplementationType = implementationType ?? serviceType,
            });
            return true;
        }
        /// <summary>
        /// Add a service to the remote instance
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public override async Task<TService> AddService<TService, TImplementation>() where TService : class
        {
            if (!typeof(TService).IsInterface) throw new Exception($"{nameof(TService)} must be an interface");
            var added = await Run(() => _AddService(typeof(TService), typeof(TImplementation)));
            var ret = GetService<TService>();
            return ret;
        }
        public override async Task<bool> AddService<TService>()
        {
            var added = await Run(() => _AddService(typeof(TService), typeof(TService)));
            return added;
        }
        private async Task<bool> _AddService(Type serviceType, Type implementationType, string key)
        {
            var service = await FindServiceAsync(serviceType, key);
            if (service != null) return false;
            RuntimeServices.Add(new RuntimeService
            {
                ServiceType = serviceType,
                ImplementationType = implementationType ?? serviceType,
                ServiceKey = key,
                IsKeyed = true,
            });
            return true;
        }
        public override async Task<bool> AddKeyedService<TService>(string key)
        {
            var added = await Run(() => _AddService(typeof(TService), typeof(TService), key));
            return added;
        }

        static object[] DeserializeArray(Array args, Type[] argTypes, int count = -1)
        {
            if (args == null) throw new NullReferenceException(nameof(args));
            if (argTypes == null) throw new NullReferenceException(nameof(argTypes));
            if (count == -1) count = argTypes.Length;
            var callArgs = new object[count];
            for (var i = 0; i < args.Length; i++)
            {
                var argType = argTypes[i];
                callArgs[i] = args.GetItem(argType, i)!;
            }
            return callArgs;
        }
        private bool _RemoveService(Type serviceType)
        {
            var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && !o.IsKeyed);
            if (runtimeServiceInfo != null)
            {
                RuntimeServices.Remove(runtimeServiceInfo);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Removes the runtime service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns>true if the service was removed</returns>
        public override async Task<bool> RemoveService(Type serviceType)
        {
            if (LocalInvoker)
            {
                var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && !o.IsKeyed);
                if (runtimeServiceInfo != null)
                {
                    RuntimeServices.Remove(runtimeServiceInfo);
                    return true;
                }
                return false;
            }
            else
            {
                var removed = await Run(() => _RemoveService(serviceType));
                return removed;
            }
        }
        private bool _ServiceExists(Type serviceType)
        {
            var serviceDescriptor = ServiceDescriptors.FindServiceDescriptor(serviceType, true);
            if (serviceDescriptor != null) return true;
            var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && !o.IsKeyed);
            return runtimeServiceInfo != null;
        }
        /// <summary>
        /// Returns true if the service is found
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public override async Task<bool> ServiceExists(Type serviceType)
        {
            if (LocalInvoker)
            {
                var serviceDescriptor = ServiceDescriptors.FindServiceDescriptor(serviceType, true);
                if (serviceDescriptor != null) return true;
                var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && !o.IsKeyed);
                return runtimeServiceInfo != null;
            }
            else
            {
                var exists = await Run(() => _ServiceExists(serviceType));
                return exists;
            }
        }
        private bool _KeyedServiceExists(Type serviceType, Type? keyType, JSObject? jsKey)
        {
            var serviceKey = keyType == null ? null : JS.ReturnMe(keyType, jsKey);
            var serviceDescriptor = ServiceDescriptors.FindKeyedServiceDescriptor(serviceType, serviceKey!, true);
            if (serviceDescriptor != null) return true;
            var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && o.IsKeyed && Object.Equals(o.ServiceKey, serviceKey));
            return runtimeServiceInfo != null;
        }
        /// <summary>
        /// Returns true if the service is found
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="serviceKey"></param>
        /// <returns></returns>
        public override async Task<bool> KeyedServiceExists(Type serviceType, object serviceKey)
        {
            if (LocalInvoker)
            {
                var serviceDescriptor = ServiceDescriptors.FindKeyedServiceDescriptor(serviceType, serviceKey, true);
                if (serviceDescriptor != null) return true;
                var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && o.IsKeyed && Object.Equals(o.ServiceKey, serviceKey));
                return runtimeServiceInfo != null;
            }
            else
            {
                using var jsKey = serviceKey == null ? null : JS.ReturnMe<JSObject>(serviceKey);
                var keyType = serviceKey?.GetType();
                var exists = await Run(() => _KeyedServiceExists(serviceType, keyType, jsKey));
                return exists;
            }
        }
        private bool _RemoveKeyedService(Type serviceType, Type? keyType, JSObject? jsKey)
        {
            var serviceKey = keyType == null ? null : JS.ReturnMe(keyType, jsKey);
            var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && o.IsKeyed && Object.Equals(o.ServiceKey, serviceKey));
            if (runtimeServiceInfo != null)
            {
                RuntimeServices.Remove(runtimeServiceInfo);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Remove a runtime keyed service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="serviceKey"></param>
        /// <returns></returns>
        public override async Task<bool> RemoveKeyedService(Type serviceType, object serviceKey)
        {
            if (LocalInvoker)
            {
                var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && o.IsKeyed && Object.Equals(o.ServiceKey, serviceKey));
                if (runtimeServiceInfo != null)
                {
                    RuntimeServices.Remove(runtimeServiceInfo);
                    return true;
                }
                return false;
            }
            else
            {
                using var jsKey = serviceKey == null ? null : JS.ReturnMe<JSObject>(serviceKey);
                var keyType = serviceKey?.GetType();
                var removed = await Run(() => _RemoveKeyedService(serviceType, keyType, jsKey));
                return removed;
            }
        }
        private async Task _CreateKeyed(string constructorInfoJson, Type serviceType, Type? implementationType, Type? keyType, JSObject? jsKey, Array? args, Type[]? argTypes, [TransferableList] object[]? transferables)
        {
            var constructorInfo = SerializableMethodInfo.DeserializeConstructorInfoInfo(constructorInfoJson);
            var isKeyed = keyType != null;
            var serviceKey = keyType == null ? null : JS.ReturnMe(keyType, jsKey);
            var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && o.IsKeyed == isKeyed && (!isKeyed || Object.Equals(o.ServiceKey, serviceKey)));
            if (runtimeServiceInfo != null)
            {
                throw new Exception("Service already exists");
            }
            runtimeServiceInfo = new RuntimeService
            {
                ImplementationType = implementationType ?? serviceType,
                ServiceKey = serviceKey,
                IsKeyed = isKeyed,
                ServiceType = serviceType ?? implementationType,
            };
            if (argTypes == null || argTypes.Length == 0 || args == null || args.Length == 0)
            {
                if (isKeyed)
                {
                    try
                    {
                        runtimeServiceInfo.Service = ActivatorUtilities.CreateInstance(ServiceProvider, runtimeServiceInfo.ImplementationType, serviceKey!);
                    }
                    catch { }
                }
                if (runtimeServiceInfo.Service == null)
                {
                    runtimeServiceInfo.Service = ActivatorUtilities.CreateInstance(ServiceProvider, runtimeServiceInfo.ImplementationType);
                }
            }
            else
            {
                if (constructorInfo != null)
                {
                    var callArgs = await PostDeserializeArgs(null, constructorInfo, args);
                    runtimeServiceInfo.Service = constructorInfo.Invoke(callArgs);// ActivatorUtilities.CreateInstance(ServiceProvider, runtimeServiceInfo.ImplementationType, callArgs);
                }
                else
                {
                    var callArgs = DeserializeArray(args, argTypes);
                    runtimeServiceInfo.Service = ActivatorUtilities.CreateInstance(ServiceProvider, runtimeServiceInfo.ImplementationType, callArgs);
                }
            }
            RuntimeServices.Add(runtimeServiceInfo);
        }
        protected override async Task CreateKeyed(ConstructorInfo constructorInfo, Type? serviceType, object serviceKey, object[]? args)
        {
            var implementationType = constructorInfo.ReflectedType;
            var argTypes = constructorInfo.GetParameters().Select(o => o.ParameterType).ToArray();
            if (LocalInvoker)
            {
                var isKeyed = serviceKey != null;
                if (serviceKey != null)
                {
                    var serviceDescriptor = ServiceDescriptors.FindKeyedServiceDescriptor(serviceType, serviceKey!, true);
                    if (serviceDescriptor != null) throw new Exception("Service already exists");
                }
                else
                {
                    var serviceDescriptor = ServiceDescriptors.FindServiceDescriptor(serviceType, true);
                    if (serviceDescriptor != null) throw new Exception("Service already exists");
                }
                var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && o.IsKeyed == isKeyed && (!isKeyed || Object.Equals(o.ServiceKey, serviceKey)));
                if (runtimeServiceInfo != null)
                {
                    throw new Exception("Service already exists");
                }
                runtimeServiceInfo = new RuntimeService
                {
                    ImplementationType = implementationType ?? serviceType,
                    ServiceKey = serviceKey,
                    IsKeyed = isKeyed,
                    ServiceType = serviceType ?? implementationType,
                };
                if (argTypes == null || argTypes.Length == 0 || args == null || args.Length == 0)
                {
                    if (isKeyed)
                    {
                        try
                        {
                            runtimeServiceInfo.Service = ActivatorUtilities.CreateInstance(ServiceProvider, runtimeServiceInfo.ImplementationType, serviceKey!);
                        }
                        catch { }
                    }
                    if (runtimeServiceInfo.Service == null)
                    {
                        runtimeServiceInfo.Service = ActivatorUtilities.CreateInstance(ServiceProvider, runtimeServiceInfo.ImplementationType);
                    }
                }
                else
                {
                    runtimeServiceInfo.Service = ActivatorUtilities.CreateInstance(ServiceProvider, runtimeServiceInfo.ImplementationType, args);
                }
                RuntimeServices.Add(runtimeServiceInfo);
            }
            else
            {
                if (!WhenReady.IsCompleted) await WhenReady;
                var preparedArgs = PreSerializeArgs(null, constructorInfo, args, out var transferables);
                using var argsArray = JS.ReturnMe<Array>(preparedArgs);
                using var jsKey = serviceKey == null ? null : JS.ReturnMe<JSObject>(serviceKey);
                var keyType = serviceKey?.GetType();
                var constructorInfoJson = SerializableMethodInfo.SerializeMethodInfo(constructorInfo);
                await Run(() => _CreateKeyed(constructorInfoJson, serviceType, implementationType, keyType, jsKey, argsArray, argTypes, transferables));
            }
        }
        /// <summary>
        /// Add a service to the remote instance
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public override async Task<TService> AddKeyedService<TService, TImplementation>(string key) where TService : class
        {
            if (!typeof(TService).IsInterface) throw new Exception($"{nameof(TService)} must be an interface");
            var added = await Run(() => _AddService(typeof(TService), typeof(TImplementation), key));
            var ret = GetService<TService>(key);
            return ret;
        }
        async Task<object?> FindServiceAsync(Type serviceType)
        {
            if (serviceType == typeof(ServiceCallDispatcher))
            {
                return this;
            }
            var ret = await ServiceProvider.FindServiceAsync(serviceType);
            if (ret == null)
            {
                var runtimeServiceInfo = RuntimeServices.FirstOrDefault(o => o.ServiceType == serviceType && Object.Equals(o.ServiceKey, null));
                if (runtimeServiceInfo != null)
                {
                    if (runtimeServiceInfo.Service == null)
                    {
                        runtimeServiceInfo.Service = ActivatorUtilities.CreateInstance(ServiceProvider, runtimeServiceInfo.ImplementationType);
                        if (runtimeServiceInfo.Service is IAsyncService asyncService)
                        {
                            await asyncService.Ready;
                        }
                    }
                    return runtimeServiceInfo.Service;
                }
            }
            return ret;
        }
        async Task<(bool, object?)> TryResolveCallSideParamValue(ParameterInfo methodParam)
        {
            object? value = null;
            // service
            var fromServiceAttr = methodParam.GetCustomAttribute<FromServicesAttribute>();
            if (fromServiceAttr != null)
            {
                value = await FindServiceAsync(methodParam.ParameterType);
                return (true, value);
            }
            // call side
            var callSideParam = GetCallSideParameter(methodParam);
            if (callSideParam != null)
            {
                value = callSideParam.GetValue();
                return (true, value);
            }
            return (false, value);
        }
        CallSideParameter? GetCallSideParameter(ParameterInfo p)
        {

            return additionalCallArgs.Where(o => o.Name == p.Name && o.Type == p.ParameterType).FirstOrDefault();
        }
        public bool IsDisposed { get; private set; } = false;
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed) return;
            IsDisposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
        }
        ~ServiceCallDispatcher()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }
        public static Action<T0> CreateTypedActionT1<T0>(Action<object?[]> arg) => new Action<T0>((t0) => arg(new object[] { t0 }));
        public static Action<T0, T1> CreateTypedActionT2<T0, T1>(Action<object?[]> arg) => new Action<T0, T1>((t0, t1) => arg(new object[] { t0, t1 }));
        public static Action<T0, T1, T2> CreateTypedActionT3<T0, T1, T2>(Action<object?[]> arg) => new Action<T0, T1, T2>((t0, t1, t2) => arg(new object[] { t0, t1 }));
        public static Action<T0, T1, T2, T3> CreateTypedActionT4<T0, T1, T2, T3>(Action<object?[]> arg) => new Action<T0, T1, T2, T3>((t0, t1, t2, t3) => arg(new object[] { t0, t1, t2, t3 }));
        public static Action<T0, T1, T2, T3, T4> CreateTypedActionT5<T0, T1, T2, T3, T4>(Action<object?[]> arg) => new Action<T0, T1, T2, T3, T4>((t0, t1, t2, t3, t4) => arg(new object[] { t0, t1, t2, t3, t4 }));
        private object CreateTypedAction(Type[] typ1, Action<object?[]> arg)
        {
            var method = typeof(ServiceCallDispatcher).GetMethod($"CreateTypedActionT{typ1.Length}", BindingFlags.Public | BindingFlags.Static);
            var gmeth = method.MakeGenericMethod(typ1);
            var genericAction = gmeth.Invoke(null, new object[] { arg });
            return genericAction;
        }
    }
}
